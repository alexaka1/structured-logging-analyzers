; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
AASL0001 | StructuredLogging | Warning | AnonymousObjectMustBeDestructured
AASL0002 | StructuredLogging | Info | ComplexObjectShouldBeDestructured
AASL0003 | StructuredLogging | Info | ComplexObjectInContextShouldBeDestructured
AASL0004 | StructuredLogging | Warning | ContextualLoggerMismatch
AASL0005 | StructuredLogging | Warning | ExceptionPassedAsTemplateArgument
AASL0006 | StructuredLogging | Warning | DuplicateTemplateProperty
AASL0007 | StructuredLogging | Warning | TemplateIsNotCompileTimeConstant
AASL0008 | StructuredLogging | Info | PositionalPropertyUsed
AASL0009 | StructuredLogging | Info | InconsistentTemplatePropertyNaming
AASL0010 | StructuredLogging | Info | InconsistentContextPropertyNaming
AASL0011 | StructuredLogging | Info | LogMessageIsSentence
AASL0012 | StructuredLogging | Info | GeneratedLoggingCannotUseSemanticConventions
