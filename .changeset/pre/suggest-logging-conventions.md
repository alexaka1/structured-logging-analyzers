---
"Alexaka1.Analyzers.StructuredLogging": patch
---

AASL0002, AASL0003, AASL0008, AASL0009, AASL0010, AASL0011, and AASL0012 now report as suggestions instead of warnings by default. Destructuring heuristics and logging conventions no longer fail `TreatWarningsAsErrors` builds over style. Rules that catch runtime bugs remain warnings, and all rules stay enabled by default. To restore warnings for any of these rules, set `dotnet_diagnostic.<id>.severity = warning` in `.editorconfig`.
