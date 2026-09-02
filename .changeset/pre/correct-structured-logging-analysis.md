---
"Alexaka1.Analyzers.StructuredLogging": patch
---

Improve structured-logging diagnostics and code fixes across Microsoft.Extensions.Logging and Serilog calls, including exception placement, named arguments, contextual loggers, constant interpolated templates, and logging scopes. Escaped braces keep their runtime meaning, invalid regex settings no longer interrupt analysis, and source rewrites are withheld when the original text cannot be mapped safely.
