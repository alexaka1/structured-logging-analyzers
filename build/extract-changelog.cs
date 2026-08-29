#!/usr/bin/env -S dotnet --

using System.Text.RegularExpressions;

if (args is not [var inputFile, var outputFile])
{
    return Fail("Usage: extract-changelog.cs CHANGELOG.md OUTPUT.md");
}

if (!File.Exists(inputFile))
{
    return Fail($"Error: {inputFile} was not found.");
}

var text = await File.ReadAllTextAsync(inputFile);
var heading = new Regex(@"^## [0-9]+\.[0-9]+\.[0-9]", RegexOptions.Multiline | RegexOptions.CultureInvariant,
    TimeSpan.FromSeconds(1));
var matches = heading.Matches(text);
if (matches.Count == 0)
{
    return Fail($"Error: No version heading found in {inputFile}.");
}

var start = matches[0].Index;
var end = matches.Count > 1 ? matches[1].Index : text.Length;
await File.WriteAllTextAsync(outputFile, text[start..end]);
return 0;

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}
