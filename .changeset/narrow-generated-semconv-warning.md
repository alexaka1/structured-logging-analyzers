---
"Alexaka1.Analyzers.StructuredLogging": patch
---

Report AASL0012 on `[LoggerMessage]` only when a named template hole's Semantic Conventions suggestion contains a dot. Hole-less messages, identifier-compatible suggestions, special parameter placeholders, and unresolved templates no longer trigger the warning. AASL0009 still checks property naming.
