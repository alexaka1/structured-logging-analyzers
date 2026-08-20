// Copyright (c) 2026 alexaka1

namespace Alexaka1.Analyzers.StructuredLogging.Comparison;

public static class RuleMap
{
    public static readonly IReadOnlyDictionary<string, string> ReSharperToSla = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["AnonymousObjectDestructuringProblem"] = "SLA0001",
        ["ComplexObjectDestructuringProblem"] = "SLA0002",
        ["ComplexObjectInContextDestructuringProblem"] = "SLA0003",
        ["ContextualLoggerProblem"] = "SLA0004",
        ["ExceptionPassedAsTemplateArgumentProblem"] = "SLA0005",
        ["TemplateDuplicatePropertyProblem"] = "SLA0006",
        ["TemplateIsNotCompileTimeConstantProblem"] = "SLA0007",
        ["PositionalPropertyUsedProblem"] = "SLA0008",
        ["InconsistentLogPropertyNaming"] = "SLA0009",
        ["InconsistentContextLogPropertyNaming"] = "SLA0010",
        ["LogMessageIsSentenceProblem"] = "SLA0011",
    };

    public static readonly HashSet<string> PluginTypeIds = ReSharperToSla.Keys.ToHashSet(StringComparer.Ordinal);
}
