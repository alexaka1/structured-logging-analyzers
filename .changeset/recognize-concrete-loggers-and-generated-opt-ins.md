---
"Alexaka1.Analyzers.StructuredLogging": patch
---

Report AASL0004 for mismatched `ForContext<T>()` calls on concrete
`Serilog.Core.Logger` and other `Serilog.ILogger` implementations, with a
code fix to use the containing type on direct calls.

Honor `generated_code = false` and `generated_code = no` so files with
generated names or auto-generated headers can opt back into analysis.
