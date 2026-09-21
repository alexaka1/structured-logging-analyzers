---
"Alexaka1.Analyzers.StructuredLogging": patch
---

The AASL0009 and AASL0006 rename fixes are no longer offered for template holes whose names contain whitespace. Serilog treats these holes as literal text, so renaming them would change the logged output. The diagnostics are still reported.
