#!/usr/bin/env -S dotnet --

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

const string packageJson = "package.json";
const string versionFile = "pack/Alexaka1.Analyzers.StructuredLogging/Version.props";
const string unshippedAnalyzerReleases =
    "src/Alexaka1.Analyzers.StructuredLogging/AnalyzerReleases.Unshipped.md";
const string shippedAnalyzerReleases =
    "src/Alexaka1.Analyzers.StructuredLogging/AnalyzerReleases.Shipped.md";
const string versionElementPattern = @"<Version>[^<]*</Version>";

if (!File.Exists(packageJson))
{
    return Fail($"Error: Missing {packageJson}");
}

if (!File.Exists(versionFile))
{
    return Fail($"Error: Missing {versionFile}");
}

if (!File.Exists(unshippedAnalyzerReleases))
{
    return Fail($"Error: Missing {unshippedAnalyzerReleases}");
}

if (!File.Exists(shippedAnalyzerReleases))
{
    return Fail($"Error: Missing {shippedAnalyzerReleases}");
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

if (await RollOverAnalyzerReleasesAsync(
        version,
        unshippedAnalyzerReleases,
        shippedAnalyzerReleases))
{
    Console.WriteLine($"Moved unshipped analyzer releases to {version}");
}

return 0;

static async Task<bool> RollOverAnalyzerReleasesAsync(
    string version,
    string unshippedFile,
    string shippedFile)
{
    if (!Regex.IsMatch(
            version,
            @"\A[0-9]+\.[0-9]+\.[0-9]+\z",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1)))
    {
        Console.WriteLine(
            $"Skipped analyzer release roll-over because {version} is not a stable major.minor.patch version.");
        return false;
    }

    var unshipped = await File.ReadAllTextAsync(unshippedFile);
    var section = Regex.Match(
        unshipped,
        @"^### (?:New|Removed|Changed) Rules[ \t]*\r?\n(?:[ \t]*\r?\n)*Rule ID[ \t]*\|",
        RegexOptions.Multiline | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));
    if (!section.Success)
    {
        return false;
    }

    var unshippedHeader = ReadHeaderComment(unshipped, unshippedFile);
    var shipped = await File.ReadAllTextAsync(shippedFile);
    var shippedHeader = ReadHeaderComment(shipped, shippedFile);
    var releaseHeading = $"## Release {version}";

    if (ContainsLine(shipped, releaseHeading))
    {
        Console.WriteLine(
            $"Skipped updating {shippedFile} because {releaseHeading} already existed.");
        await File.WriteAllTextAsync(unshippedFile, unshippedHeader.TrimEnd() + "\n");
        return true;
    }

    var releaseTables = unshipped[section.Index..].Trim();
    var previousReleases = shipped[shippedHeader.Length..].Trim();

    var updatedShipped =
        $"{shippedHeader.TrimEnd()}\n\n{releaseHeading}\n\n{releaseTables}";
    if (previousReleases.Length > 0)
    {
        updatedShipped += $"\n\n{previousReleases}";
    }

    await File.WriteAllTextAsync(shippedFile, updatedShipped + "\n");
    await File.WriteAllTextAsync(unshippedFile, unshippedHeader.TrimEnd() + "\n");
    return true;
}

static bool ContainsLine(string contents, string expectedLine)
{
    using var reader = new StringReader(contents);
    while (reader.ReadLine() is { } line)
    {
        if (string.Equals(line.Trim(), expectedLine, StringComparison.Ordinal))
        {
            return true;
        }
    }

    return false;
}

static string ReadHeaderComment(string contents, string fileName)
{
    var match = Regex.Match(
        contents,
        @"\A(?:;[^\r\n]*(?:\r?\n|\z))+",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));
    if (!match.Success)
    {
        throw new InvalidDataException($"No header comment found in {fileName}.");
    }

    return match.Value;
}

static int Run(string fileName, params string[] arguments)
{
    var psi = new ProcessStartInfo
    {
        FileName = fileName,
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
