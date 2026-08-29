#!/usr/bin/env -S dotnet --

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

var changeset = Run("pnpm", "run", "changeset:version");
if (changeset != 0)
{
    return changeset;
}

const string packageJson = "package.json";
const string versionFile = "pack/Alexaka1.Analyzers.StructuredLogging/Version.props";

if (!File.Exists(packageJson))
{
    return Fail($"Error: Missing {packageJson}");
}

using var json = JsonDocument.Parse(await File.ReadAllTextAsync(packageJson));
var version = json.RootElement.TryGetProperty("version", out var versionProperty)
    ? versionProperty.GetString()
    : null;
if (string.IsNullOrEmpty(version))
{
    return Fail($"Error: No version in {packageJson}");
}

if (!File.Exists(versionFile))
{
    return Fail($"Error: Missing {versionFile}");
}

var contents = await File.ReadAllTextAsync(versionFile);
var updated = Regex.Replace(
    contents,
    @"<Version>[^<]*</Version>",
    $"<Version>{version}</Version>",
    RegexOptions.CultureInvariant,
    TimeSpan.FromSeconds(1));
if (updated == contents && !contents.Contains($"<Version>{version}</Version>", StringComparison.Ordinal))
{
    return Fail($"Error: No <Version> element found in {versionFile}");
}

await File.WriteAllTextAsync(versionFile, updated);
Console.WriteLine($"Synced {versionFile} to {version}");
return 0;

static int Run(string fileName, params string[] arguments)
{
    var psi = new ProcessStartInfo
    {
        FileName = OperatingSystem.IsWindows() ? fileName + ".cmd" : fileName,
        UseShellExecute = false,
    };
    foreach (var argument in arguments)
    {
        psi.ArgumentList.Add(argument);
    }

    using var process = Process.Start(psi);
    if (process is null)
    {
        Console.Error.WriteLine($"Error: Failed to start {fileName}.");
        return 1;
    }

    process.WaitForExit();
    return process.ExitCode;
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}
