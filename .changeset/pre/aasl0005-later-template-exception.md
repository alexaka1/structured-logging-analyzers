---
"Alexaka1.Analyzers.StructuredLogging": patch
---

Report AASL0005 when an exception is passed as a template argument even if another exception already occupies the exception slot. The move-before-template fix is not offered in that case.
