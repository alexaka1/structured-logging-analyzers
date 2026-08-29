#!/usr/bin/env -S dotnet --

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

const string packageJson = "package.json";
const string versionFile = "pack/Alexaka1.Analyzers.StructuredLogging/Version.props";
const string versionElementPattern = @"<Version>[^<]*</Version>";

if (!File.Exists(packageJson))
{
    return Fail($"Error: Missing {packageJson}");
}

if (!File.Exists(versionFile))
{
    return Fail($"Error: Missing {versionFile}");
}

var contents = await File.ReadAllTextAsync(versionFile);
if (!Regex.IsMatch(contents, versionElementPattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
{
    return Fail($"Error: No <Version> element found in {versionFile}");
}

var changeset = Run("pnpm", "run", "changeset:version");
if (changeset != 0)
{
    return changeset;
}

using var json = JsonDocument.Parse(await File.ReadAllTextAsync(packageJson));
var version = json.RootElement.TryGetProperty("version", out var versionProperty)
    ? versionProperty.GetString()
    : null;
if (string.IsNullOrEmpty(version))
{
    return Fail($"Error: No version in {packageJson}");
}

contents = await File.ReadAllTextAsync(versionFile);
var updated = Regex.Replace(
    contents,
    versionElementPattern,
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
        UseShellExecute = false,
    };

    if (OperatingSystem.IsWindows())
    {
        psi.FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
        psi.ArgumentList.Add("/c");
        psi.ArgumentList.Add(fileName);
    }
    else
    {
        psi.FileName = fileName;
    }

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
