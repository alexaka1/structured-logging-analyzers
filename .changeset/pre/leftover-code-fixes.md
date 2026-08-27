---
"Alexaka1.Analyzers.StructuredLogging": minor
---

Add guarded code fixes for AASL0003 (PushProperty destructureObjects), AASL0004 (containing-type logger category), AASL0005 (move exception before the template), and AASL0006 (unique names for duplicate holes on invocations). AASL0012 stays without a fix: it is an API-choice warning on [LoggerMessage] when template naming is semantic_conventions.
