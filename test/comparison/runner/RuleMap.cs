// Copyright (c) 2026 alexaka1

namespace Alexaka1.Analyzers.StructuredLogging.Comparison;

public static class RuleMap
{
    public static readonly IReadOnlyDictionary<string, string> ReSharperToSla = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["AnonymousObjectDestructuringProblem"] = "AASL0001",
        ["ComplexObjectDestructuringProblem"] = "AASL0002",
        ["ComplexObjectInContextDestructuringProblem"] = "AASL0003",
        ["ContextualLoggerProblem"] = "AASL0004",
        ["ExceptionPassedAsTemplateArgumentProblem"] = "AASL0005",
        ["TemplateDuplicatePropertyProblem"] = "AASL0006",
        ["TemplateIsNotCompileTimeConstantProblem"] = "AASL0007",
        ["PositionalPropertyUsedProblem"] = "AASL0008",
        ["InconsistentLogPropertyNaming"] = "AASL0009",
        ["InconsistentContextLogPropertyNaming"] = "AASL0010",
        ["LogMessageIsSentenceProblem"] = "AASL0011",
    };

    public static readonly HashSet<string> PluginTypeIds = ReSharperToSla.Keys.ToHashSet(StringComparer.Ordinal);
}
