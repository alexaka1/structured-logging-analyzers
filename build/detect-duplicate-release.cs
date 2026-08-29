#!/usr/bin/env -S dotnet --

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args is not [var tag, var version, var packageId])
{
    return Fail("Usage: detect-duplicate-release.cs TAG VERSION NUGET_PACKAGE_ID");
}

var token = FirstNonEmpty(Environment.GetEnvironmentVariable("GH_TOKEN"),
    Environment.GetEnvironmentVariable("GITHUB_TOKEN"));
if (string.IsNullOrEmpty(token))
{
    return Fail("Error: GH_TOKEN or GITHUB_TOKEN is required to query GitHub releases.");
}

var nugetSource = Environment.GetEnvironmentVariable("NUGET_SOURCE");
if (string.IsNullOrEmpty(nugetSource))
{
    return Fail("Error: NUGET_SOURCE is required to query NuGet.");
}

var repository = FirstNonEmpty(
    Environment.GetEnvironmentVariable("GITHUB_REPOSITORY"),
    Environment.GetEnvironmentVariable("GH_REPO"),
    TryGetRepositoryFromGit());
if (!string.IsNullOrEmpty(repository)
    && (repository.Contains("://", StringComparison.Ordinal) ||
        repository.StartsWith("git@", StringComparison.Ordinal)))
{
    repository = ParseGitHubRepository(repository);
}

if (string.IsNullOrEmpty(repository) || !repository.Contains('/', StringComparison.Ordinal))
{
    return Fail("Error: GITHUB_REPOSITORY or GH_REPO is required to query GitHub releases.");
}

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("structured-logging-analyzers-release-scripts");

var apiUrl = (Environment.GetEnvironmentVariable("GITHUB_API_URL") ?? "https://api.github.com").TrimEnd('/');
var releaseUrl = $"{apiUrl}/repos/{repository}/releases/tags/{Uri.EscapeDataString(tag)}";
using (var request = new HttpRequestMessage(HttpMethod.Get, releaseUrl))
{
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");

    using var response = await http.SendAsync(request);
    if (response.StatusCode == HttpStatusCode.OK)
    {
        return Fail($"Error: GitHub release '{tag}' already exists.");
    }

    if (response.StatusCode != HttpStatusCode.NotFound)
    {
        var body = await response.Content.ReadAsStringAsync();
        Console.Error.WriteLine($"Error: Failed to query GitHub release '{tag}'.");
        Console.Error.WriteLine(body);
        return 1;
    }
}

bool exists;
try
{
    exists = await NuGetPackageExists(http, nugetSource, packageId, version);
}
catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException
                               or KeyNotFoundException)
{
    return Fail($"Error: Failed to query NuGet package {packageId}.\n{ex.Message}");
}

if (exists)
{
    return Fail($"Error: NuGet package {packageId} {version} already exists on {nugetSource}.");
}

return 0;

static async Task<bool> NuGetPackageExists(HttpClient http, string source, string packageId, string version)
{
    using var indexResponse = await http.GetAsync(source);
    if (!indexResponse.IsSuccessStatusCode)
    {
        var body = await indexResponse.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"Failed to query NuGet source '{source}': {(int)indexResponse.StatusCode} {indexResponse.ReasonPhrase}\n{body}");
    }

    using var index = await JsonDocument.ParseAsync(await indexResponse.Content.ReadAsStreamAsync());
    string? baseAddress = null;
    foreach (var resource in index.RootElement.GetProperty("resources").EnumerateArray())
    {
        if (resource.GetProperty("@type").GetString() == "PackageBaseAddress/3.0.0")
        {
            baseAddress = resource.GetProperty("@id").GetString();
            break;
        }
    }

    if (string.IsNullOrEmpty(baseAddress))
    {
        throw new InvalidOperationException($"NuGet source '{source}' does not expose PackageBaseAddress/3.0.0.");
    }

    if (!baseAddress.EndsWith('/'))
    {
        baseAddress += "/";
    }

    var versionsUrl = $"{baseAddress}{packageId.ToLowerInvariant()}/index.json";
    using var versionsResponse = await http.GetAsync(versionsUrl);
    if (versionsResponse.StatusCode == HttpStatusCode.NotFound)
    {
        return false;
    }

    if (!versionsResponse.IsSuccessStatusCode)
    {
        var body = await versionsResponse.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"Failed to query NuGet package versions: {(int)versionsResponse.StatusCode} {versionsResponse.ReasonPhrase}\n{body}");
    }

    using var versions = await JsonDocument.ParseAsync(await versionsResponse.Content.ReadAsStreamAsync());
    foreach (var published in versions.RootElement.GetProperty("versions").EnumerateArray())
    {
        if (string.Equals(published.GetString(), version, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }

    return false;
}

static string? TryGetRepositoryFromGit()
{
    var psi = new ProcessStartInfo
    {
        FileName = "git",
        ArgumentList = { "remote", "get-url", "origin" },
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };

    using var process = Process.Start(psi);
    if (process is null)
    {
        return null;
    }

    var stdout = process.StandardOutput.ReadToEnd().Trim();
    process.WaitForExit();
    if (process.ExitCode != 0 || string.IsNullOrEmpty(stdout))
    {
        return null;
    }

    return ParseGitHubRepository(stdout);
}

static string? ParseGitHubRepository(string remote)
{
    remote = remote.Trim();
    if (remote.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
    {
        remote = remote[..^4];
    }

    var ssh = Regex.Match(remote, @"^git@[^:]+:([^/]+)/(.+)$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    if (ssh.Success)
    {
        return $"{ssh.Groups[1].Value}/{ssh.Groups[2].Value}";
    }

    if (Uri.TryCreate(remote, UriKind.Absolute, out var uri))
    {
        var parts = uri.AbsolutePath.Trim('/').Split('/');
        if (parts.Length >= 2)
        {
            return $"{parts[0]}/{parts[1]}";
        }
    }

    return null;
}

static string? FirstNonEmpty(params string?[] values)
{
    foreach (var value in values)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }
    }

    return null;
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}
