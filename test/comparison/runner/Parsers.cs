// Copyright (c) 2026 alexaka1

using System.Xml.Linq;

namespace StructuredLogging.Comparison;

public readonly record struct Finding(
    string Fixture,
    string SlaId,
    string Source,
    string Message,
    int Line);

public static class InspectCodeParser
{
    public static IReadOnlyList<Finding> Parse(string xmlPath)
    {
        var doc = XDocument.Load(xmlPath);
        var findings = new List<Finding>();
        foreach (var issue in doc.Descendants("Issue"))
        {
            var typeId = (string?)issue.Attribute("TypeId") ?? "";
            if (!RuleMap.ReSharperToSla.TryGetValue(typeId, out var sla))
            {
                continue;
            }

            var file = (string?)issue.Attribute("File") ?? "";
            var fixture = NormalizeFixture(file);
            var message = (string?)issue.Attribute("Message") ?? "";
            var line = int.TryParse((string?)issue.Attribute("Line"), out var parsed) ? parsed : 0;
            findings.Add(new Finding(fixture, sla, "inspectcode", message, line));
        }

        return findings;
    }

    public static bool PluginLoaded(string xmlPath)
    {
        var doc = XDocument.Load(xmlPath);
        if (doc.Descendants("IssueType")
            .Select(e => (string?)e.Attribute("Id"))
            .Any(id => id is not null && RuleMap.PluginTypeIds.Contains(id)))
        {
            return true;
        }

        return doc.Descendants("Issue")
            .Select(e => (string?)e.Attribute("TypeId"))
            .Any(id => id is not null && RuleMap.PluginTypeIds.Contains(id));
    }

    public static string NormalizeFixture(string file)
    {
        var normalized = file.Replace('\\', '/');
        return Path.GetFileName(normalized);
    }
}
