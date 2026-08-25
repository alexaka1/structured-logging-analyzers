---
"Alexaka1.Analyzers.StructuredLogging": minor
---

Add a `semantic_conventions` property naming style for AASL0009 (template property naming) and AASL0010 (context property naming) (`service.name`, `http.response.status_code`). AASL0012 warns when generated logging (`[LoggerMessage]`, `LoggerMessage.Define`) is used with that template naming style.
