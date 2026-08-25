namespace Alexaka1.Analyzers.StructuredLogging;

internal static class DiagnosticIds
{
    public const string Prefix = "AASL";

    public const string AnonymousObjectMustBeDestructured = "AASL0001";
    public const string ComplexObjectShouldBeDestructured = "AASL0002";
    public const string ComplexObjectInContextShouldBeDestructured = "AASL0003";
    public const string ContextualLoggerMismatch = "AASL0004";
    public const string ExceptionPassedAsTemplateArgument = "AASL0005";
    public const string DuplicateTemplateProperty = "AASL0006";
    public const string TemplateIsNotCompileTimeConstant = "AASL0007";
    public const string PositionalPropertyUsed = "AASL0008";
    public const string InconsistentTemplatePropertyNaming = "AASL0009";
    public const string InconsistentContextPropertyNaming = "AASL0010";
    public const string LogMessageIsSentence = "AASL0011";
    public const string GeneratedLoggingCannotUseSemanticConventions = "AASL0012";

    public const string Category = "StructuredLogging";
    public const string HelpBase = "https://github.com/alexaka1/structured-logging-analyzers/blob/main/docs/rules/";
}
