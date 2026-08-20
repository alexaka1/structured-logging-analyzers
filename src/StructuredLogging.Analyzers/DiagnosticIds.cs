// Copyright (c) 2026 alexaka1

namespace StructuredLogging.Analyzers;

internal static class DiagnosticIds
{
    public const string AnonymousObjectMustBeDestructured = "SLA0001";
    public const string ComplexObjectShouldBeDestructured = "SLA0002";
    public const string ComplexObjectInContextShouldBeDestructured = "SLA0003";
    public const string ContextualLoggerMismatch = "SLA0004";
    public const string ExceptionPassedAsTemplateArgument = "SLA0005";
    public const string DuplicateTemplateProperty = "SLA0006";
    public const string TemplateIsNotCompileTimeConstant = "SLA0007";
    public const string PositionalPropertyUsed = "SLA0008";
    public const string InconsistentTemplatePropertyNaming = "SLA0009";
    public const string InconsistentContextPropertyNaming = "SLA0010";
    public const string LogMessageIsSentence = "SLA0011";

    public const string Category = "StructuredLogging";
    public const string HelpBase = "https://github.com/alexaka1/structured-logging-analyzers/blob/main/docs/rules/";
}
