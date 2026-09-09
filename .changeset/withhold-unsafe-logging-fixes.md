---
"Alexaka1.Analyzers.StructuredLogging": patch
---

Withhold AASL0007 interpolation conversion when the logging method cannot accept the extracted values, including `LoggerMessage.Define`, `DefineScope`, and wrappers without a suitable overload. These calls still receive the diagnostic.

Offer the AASL0010 context-property rename fix only for string literal names. Constant identifiers and concatenations still receive the diagnostic without a code action that leaves the source unchanged.
