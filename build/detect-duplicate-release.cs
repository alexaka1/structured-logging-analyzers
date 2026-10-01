#!/usr/bin/env -S dotnet --

using System.Net;
using System.Text.Json;

if (args is not [var version, var packageId])
{
    return Fail("Usage: detect-duplicate-release.cs VERSION NUGET_PACKAGE_ID");
}

var nugetSource = Environment.GetEnvironmentVariable("NUGET_SOURCE");
if (string.IsNullOrEmpty(nugetSource))
{
    return Fail("Error: NUGET_SOURCE is required to query NuGet.");
}

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("structured-logging-analyzers-release-scripts");

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

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}
