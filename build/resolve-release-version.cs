#!/usr/bin/env -S dotnet --

using System.Text.RegularExpressions;

if (args is not [var tag])
{
    return Fail("Usage: resolve-release-version.cs TAG");
}

if (!Regex.IsMatch(tag, @"^v[0-9]+\.[0-9]+\.[0-9]", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
{
    return Fail($"Error: Unexpected tag '{tag}'. Expected v<semver>.");
}

var version = tag[1..];
if (string.IsNullOrEmpty(version))
{
    return Fail($"Error: Failed to resolve package version from '{tag}'.");
}

Console.WriteLine(version);
return 0;

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}
