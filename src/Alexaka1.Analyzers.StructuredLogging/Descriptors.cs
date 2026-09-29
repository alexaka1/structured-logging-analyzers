using Microsoft.CodeAnalysis;

namespace Alexaka1.Analyzers.StructuredLogging;

internal static class Descriptors
{
    // Constant constructor arguments let release tracking (RS2000/RS2001) check AnalyzerReleases.*.md.
    public static readonly DiagnosticDescriptor AnonymousObjectMustBeDestructured = new(
        DiagnosticIds.AnonymousObjectMustBeDestructured,
        "Anonymous objects must be destructured",
        "Anonymous objects must be destructured",
        DiagnosticIds.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticIds.HelpBase + DiagnosticIds.AnonymousObjectMustBeDestructured + ".md");

    public static readonly DiagnosticDescriptor ComplexObjectShouldBeDestructured = new(
        DiagnosticIds.ComplexObjectShouldBeDestructured,
        "Complex objects should be destructured",
        "Complex objects with default ToString() implementation probably need to be destructured",
        DiagnosticIds.Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticIds.HelpBase + DiagnosticIds.ComplexObjectShouldBeDestructured + ".md");

    public static readonly DiagnosticDescriptor ComplexObjectInContextShouldBeDestructured = new(
        DiagnosticIds.ComplexObjectInContextShouldBeDestructured,
        "Complex objects in log context should be destructured",
        "Complex objects with default ToString() implementation probably need to be destructured",
        DiagnosticIds.Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticIds.HelpBase + DiagnosticIds.ComplexObjectInContextShouldBeDestructured + ".md");

    public static readonly DiagnosticDescriptor ContextualLoggerMismatch = new(
        DiagnosticIds.ContextualLoggerMismatch,
        "Incorrect type is used for contextual logger",
        "Incorrect type is used for contextual logger",
        DiagnosticIds.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticIds.HelpBase + DiagnosticIds.ContextualLoggerMismatch + ".md");

    public static readonly DiagnosticDescriptor ExceptionPassedAsTemplateArgument = new(
        DiagnosticIds.ExceptionPassedAsTemplateArgument,
        "Exception should be passed to the exception argument",
        "Exception should be passed to the exception argument",
        DiagnosticIds.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticIds.HelpBase + DiagnosticIds.ExceptionPassedAsTemplateArgument + ".md");

    public static readonly DiagnosticDescriptor DuplicateTemplateProperty = new(
        DiagnosticIds.DuplicateTemplateProperty,
        "Duplicate properties in message template",
        "Duplicate properties in message template",
        DiagnosticIds.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticIds.HelpBase + DiagnosticIds.DuplicateTemplateProperty + ".md");

    public static readonly DiagnosticDescriptor TemplateIsNotCompileTimeConstant = new(
        DiagnosticIds.TemplateIsNotCompileTimeConstant,
        "Message template should be compile time constant",
        "Message template should be compile time constant",
        DiagnosticIds.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticIds.HelpBase + DiagnosticIds.TemplateIsNotCompileTimeConstant + ".md");

    public static readonly DiagnosticDescriptor PositionalPropertyUsed = new(
        DiagnosticIds.PositionalPropertyUsed,
        "Prefer named properties instead of positional ones",
        "Prefer named properties instead of positional ones",
        DiagnosticIds.Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticIds.HelpBase + DiagnosticIds.PositionalPropertyUsed + ".md");

    public static readonly DiagnosticDescriptor InconsistentTemplatePropertyNaming = new(
        DiagnosticIds.InconsistentTemplatePropertyNaming,
        "Template property name does not match naming rules",
        "Property name '{0}' does not match naming rules. Suggested name is '{1}'.",
        DiagnosticIds.Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticIds.HelpBase + DiagnosticIds.InconsistentTemplatePropertyNaming + ".md");

    public static readonly DiagnosticDescriptor InconsistentContextPropertyNaming = new(
        DiagnosticIds.InconsistentContextPropertyNaming,
        "Context property name does not match naming rules",
        "Property name '{0}' does not match naming rules. Suggested name is '{1}'.",
        DiagnosticIds.Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticIds.HelpBase + DiagnosticIds.InconsistentContextPropertyNaming + ".md");

    public static readonly DiagnosticDescriptor LogMessageIsSentence = new(
        DiagnosticIds.LogMessageIsSentence,
        "Log event messages should be fragments, not sentences",
        "Log event messages should be fragments, not sentences. Avoid a trailing period/full stop.",
        DiagnosticIds.Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticIds.HelpBase + DiagnosticIds.LogMessageIsSentence + ".md");

    public static readonly DiagnosticDescriptor GeneratedLoggingCannotUseSemanticConventions = new(
        DiagnosticIds.GeneratedLoggingCannotUseSemanticConventions,
        "Generated logging cannot use Semantic Conventions property names",
        "Generated logging cannot use Semantic Conventions property names. [LoggerMessage] binds template holes to C# parameter names, which cannot contain '.'.",
        DiagnosticIds.Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: DiagnosticIds.HelpBase + DiagnosticIds.GeneratedLoggingCannotUseSemanticConventions + ".md");

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
}
