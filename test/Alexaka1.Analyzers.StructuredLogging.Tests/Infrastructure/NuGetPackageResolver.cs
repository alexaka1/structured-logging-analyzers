using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

internal static class NuGetPackageResolver
{
    private static readonly ConcurrentDictionary<string, object> RestoreLocks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, ImmutableArray<string>> AssemblyCache = new(StringComparer.OrdinalIgnoreCase);

    public static ImmutableArray<MetadataReference> GetReferences(params (string Id, string Version)[] packages)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddTrustedPlatformAssemblies(paths);
        AddAssembly(paths, typeof(object));

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            seenNames.Add(Path.GetFileNameWithoutExtension(path) ?? string.Empty);
        }

        // First simple name wins. Call sites pass one package today; BCL names stay preferred.
        foreach (var (id, version) in packages)
        {
            foreach (var path in GetCompileAssemblies(id, version))
            {
                var name = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
                if (!seenNames.Add(name))
                {
                    continue;
                }

                paths.Add(path);
            }
        }

        return paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToImmutableArray();
    }

    internal static ImmutableArray<string> GetCompileAssemblies(string packageId, string version)
    {
        var key = packageId + "/" + version;
        return AssemblyCache.GetOrAdd(key, _ => RestoreAndReadCompileAssemblies(packageId, version));
    }

    internal static void AddTrustedPlatformAssemblies(HashSet<string> paths)
    {
        var trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (string.IsNullOrEmpty(trusted))
        {
            return;
        }

        foreach (var path in trusted.Split(Path.PathSeparator))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            switch (name)
            {
                case "mscorlib":
                case "netstandard":
                case "System.Private.CoreLib":
                case "System.Runtime":
                case "System.Console":
                case "System.Linq":
                case "System.Linq.Expressions":
                case "System.Collections":
                case "System.Collections.Concurrent":
                case "System.Memory":
                case "System.Threading":
                case "System.Threading.Tasks":
                case "System.Runtime.Extensions":
                case "System.Runtime.InteropServices":
                case "System.ComponentModel":
                case "System.ObjectModel":
                case "System.Text.RegularExpressions":
                case "System.Net.Primitives":
                case "System.Net.Http":
                case "System.Private.Uri":
                case "System.Linq.Queryable":
                    paths.Add(path);
                    break;
            }
        }
    }

    internal static void AddAssembly(HashSet<string> paths, Type type)
    {
        if (!string.IsNullOrEmpty(type.Assembly.Location))
        {
            paths.Add(type.Assembly.Location);
        }
    }

    private static ImmutableArray<string> RestoreAndReadCompileAssemblies(string packageId, string version)
    {
        var stubDir = Path.Combine(FindRepoRoot(), "artifacts", "package-refs", Sanitize(packageId), Sanitize(version));
        Directory.CreateDirectory(stubDir);
        var gate = RestoreLocks.GetOrAdd(stubDir, static _ => new object());
        lock (gate)
        {
            var assetsPath = Path.Combine(stubDir, "obj", "project.assets.json");
            if (!File.Exists(assetsPath))
            {
                RestoreStub(stubDir, packageId, version);
            }

            if (!File.Exists(assetsPath))
            {
                throw new InvalidOperationException(
                    $"Restore of {packageId} {version} did not produce project.assets.json at {assetsPath}.");
            }

            return ReadCompileAssemblies(assetsPath);
        }
    }

    private static void RestoreStub(string stubDir, string packageId, string version)
    {
        var csproj = Path.Combine(stubDir, "PackageRef.csproj");
        var nugetConfig = Path.Combine(FindRepoRoot(), "nuget.config");
        File.WriteAllText(csproj, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>disable</ImplicitUsings>
                <EnableDefaultItems>false</EnableDefaultItems>
                <IsPackable>false</IsPackable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="{packageId}" Version="{version}" />
              </ItemGroup>
            </Project>
            """);

        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = stubDir
        };
        psi.ArgumentList.Add("restore");
        psi.ArgumentList.Add(csproj);
        psi.ArgumentList.Add("--nologo");
        psi.ArgumentList.Add("--verbosity");
        psi.ArgumentList.Add("quiet");
        if (File.Exists(nugetConfig))
        {
            psi.ArgumentList.Add("--configfile");
            psi.ArgumentList.Add(nugetConfig);
        }

        psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        psi.Environment["DOTNET_NOLOGO"] = "1";
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet restore.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(120_000))
        {
            Exception? killFailure = null;
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                killFailure = ex;
            }

            throw new TimeoutException($"Timed out restoring {packageId} {version}.", killFailure);
        }

        process.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet restore failed for {packageId} {version} (exit {process.ExitCode}).{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
        }
    }

    private static ImmutableArray<string> ReadCompileAssemblies(string assetsPath)
    {
        using var stream = File.OpenRead(assetsPath);
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;
        var packageFolders = new List<string>();
        if (root.TryGetProperty("packageFolders", out var folders))
        {
            foreach (var folder in folders.EnumerateObject())
            {
                packageFolders.Add(folder.Name);
            }
        }

        if (packageFolders.Count == 0)
        {
            throw new InvalidOperationException($"No packageFolders in {assetsPath}.");
        }

        var libraries = root.GetProperty("libraries");
        var targets = root.GetProperty("targets");
        JsonElement target = default;
        var foundTarget = false;
        foreach (var entry in targets.EnumerateObject())
        {
            target = entry.Value;
            foundTarget = true;
            break;
        }

        if (!foundTarget)
        {
            throw new InvalidOperationException($"No restore targets in {assetsPath}.");
        }

        var paths = ImmutableArray.CreateBuilder<string>();
        foreach (var package in target.EnumerateObject())
        {
            if (!package.Value.TryGetProperty("compile", out var compile))
            {
                continue;
            }

            if (!libraries.TryGetProperty(package.Name, out var library) ||
                !library.TryGetProperty("path", out var libraryPathElement))
            {
                continue;
            }

            var libraryPath = libraryPathElement.GetString();
            if (string.IsNullOrEmpty(libraryPath))
            {
                continue;
            }

            foreach (var asset in compile.EnumerateObject())
            {
                var relative = asset.Name.Replace('/', Path.DirectorySeparatorChar);
                if (!relative.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var folder in packageFolders)
                {
                    var candidate = Path.GetFullPath(Path.Combine(folder, libraryPath, relative));
                    if (File.Exists(candidate))
                    {
                        paths.Add(candidate);
                        break;
                    }
                }
            }
        }

        if (paths.Count == 0)
        {
            throw new InvalidOperationException($"No compile assemblies found in {assetsPath}.");
        }

        return paths.ToImmutable();
    }

    internal static string FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "Alexaka1.Analyzers.StructuredLogging.slnx")) ||
                File.Exists(Path.Combine(dir, "nuget.config")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static string Sanitize(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] is not '.' and not '-')
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }
}
