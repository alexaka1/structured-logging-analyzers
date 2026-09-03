---
"Alexaka1.Analyzers.StructuredLogging": patch
---

Speed up `[LoggerMessage]` analysis for private constant templates, keep cross-file diagnostics in the logging method's document, analyze Razor design-time documents, and skip syntax analysis when neither a supported logging library nor a source-declared `MessageTemplateFormatMethodAttribute` is present. Custom attributed wrappers declared in the project remain analyzed without a logging-library reference.
