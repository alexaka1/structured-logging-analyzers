---
"Alexaka1.Analyzers.StructuredLogging": minor
---

Add a guarded AASL0008 rename for positional template holes on logging invocations, using names derived from argument expressions when an identifier can be derived. Keep AASL0003, AASL0004, AASL0005, AASL0006, and AASL0012 without code fixes.

Argument mapping now expands compiler-synthesized params arrays so MEL-style `LogInformation("{0}", orderId)` can be renamed. AASL0001/AASL0002 stay Serilog-like and still do not report on MEL, ZLogger, or `LoggerMessage.Define` / `DefineScope` (`@` is not valid there).
