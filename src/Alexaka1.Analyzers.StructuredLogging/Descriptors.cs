using Microsoft.CodeAnalysis;

namespace Alexaka1.Analyzers.StructuredLogging;

internal static class Descriptors
{
    public static readonly DiagnosticDescriptor AnonymousObjectMustBeDestructured = Create(
        DiagnosticIds.AnonymousObjectMustBeDestructured,
        "Anonymous objects must be destructured",
        "Anonymous objects must be destructured",
        DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor ComplexObjectShouldBeDestructured = Create(
        DiagnosticIds.ComplexObjectShouldBeDestructured,
        "Complex objects should be destructured",
        "Complex objects with default ToString() implementation probably need to be destructured",
        DiagnosticSeverity.Info);

    public static readonly DiagnosticDescriptor ComplexObjectInContextShouldBeDestructured = Create(
        DiagnosticIds.ComplexObjectInContextShouldBeDestructured,
        "Complex objects in log context should be destructured",
        "Complex objects with default ToString() implementation probably need to be destructured",
        DiagnosticSeverity.Info);

    public static readonly DiagnosticDescriptor ContextualLoggerMismatch = Create(
        DiagnosticIds.ContextualLoggerMismatch,
        "Incorrect type is used for contextual logger",
        "Incorrect type is used for contextual logger",
        DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor ExceptionPassedAsTemplateArgument = Create(
        DiagnosticIds.ExceptionPassedAsTemplateArgument,
        "Exception should be passed to the exception argument",
        "Exception should be passed to the exception argument",
        DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor DuplicateTemplateProperty = Create(
        DiagnosticIds.DuplicateTemplateProperty,
        "Duplicate properties in message template",
        "Duplicate properties in message template",
        DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor TemplateIsNotCompileTimeConstant = Create(
        DiagnosticIds.TemplateIsNotCompileTimeConstant,
        "Message template should be compile time constant",
        "Message template should be compile time constant",
        DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor PositionalPropertyUsed = Create(
        DiagnosticIds.PositionalPropertyUsed,
        "Prefer named properties instead of positional ones",
        "Prefer named properties instead of positional ones",
        DiagnosticSeverity.Info);

    public static readonly DiagnosticDescriptor InconsistentTemplatePropertyNaming = Create(
        DiagnosticIds.InconsistentTemplatePropertyNaming,
        "Template property name does not match naming rules",
        "Property name '{0}' does not match naming rules. Suggested name is '{1}'.",
        DiagnosticSeverity.Info);

    public static readonly DiagnosticDescriptor InconsistentContextPropertyNaming = Create(
        DiagnosticIds.InconsistentContextPropertyNaming,
        "Context property name does not match naming rules",
        "Property name '{0}' does not match naming rules. Suggested name is '{1}'.",
        DiagnosticSeverity.Info);

    public static readonly DiagnosticDescriptor LogMessageIsSentence = Create(
        DiagnosticIds.LogMessageIsSentence,
        "Log event messages should be fragments, not sentences",
        "Log event messages should be fragments, not sentences. Avoid a trailing period/full stop.",
        DiagnosticSeverity.Info);

    public static readonly DiagnosticDescriptor GeneratedLoggingCannotUseSemanticConventions = Create(
        DiagnosticIds.GeneratedLoggingCannotUseSemanticConventions,
        "Generated logging cannot use Semantic Conventions property names",
        "Generated logging cannot use Semantic Conventions property names. [LoggerMessage] binds template holes to C# parameter names, which cannot contain '.'.",
        DiagnosticSeverity.Info);

    public static readonly ImmutableArray<DiagnosticDescriptor> All = ImmutableArray.Create(
        AnonymousObjectMustBeDestructured,
        ComplexObjectShouldBeDestructured,
        ComplexObjectInContextShouldBeDestructured,
        ContextualLoggerMismatch,
        ExceptionPassedAsTemplateArgument,
        DuplicateTemplateProperty,
        TemplateIsNotCompileTimeConstant,
        PositionalPropertyUsed,
        InconsistentTemplatePropertyNaming,
        InconsistentContextPropertyNaming,
        LogMessageIsSentence,
        GeneratedLoggingCannotUseSemanticConventions);

    private static DiagnosticDescriptor Create(string id, string title, string message, DiagnosticSeverity severity)
    {
        return new DiagnosticDescriptor(
            id,
            title,
            message,
            DiagnosticIds.Category,
            severity,
            isEnabledByDefault: true,
            helpLinkUri: DiagnosticIds.HelpBase + id + ".md");
    }
}
