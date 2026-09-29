---
"Alexaka1.Analyzers.StructuredLogging": minor
---

First stable release.

- Twelve rules, AASL0001 to AASL0012, check structured logging across Microsoft.Extensions.Logging, Serilog, NLog, and ZLogger. ZLogger support is limited to 1.x string-template overloads; 2.x interpolated-string-handler APIs are not analyzed.
- Checks cover destructuring, contextual logger types, exception placement, duplicate properties, constant templates, positional properties, property naming, trailing periods, and Semantic Conventions naming limitations in generated logging.
- Analysis includes `[LoggerMessage]`, `LoggerMessage.Define` / `DefineScope`, and Blazor `.razor` `@code` blocks and `.razor.cs` code-behind.
- AASL0001, AASL0004, AASL0005, AASL0006, and AASL0007 are warnings by default. AASL0002, AASL0003, AASL0008, AASL0009, AASL0010, AASL0011, and AASL0012 are suggestions.
- Code fixes are available for most rules, with unsafe rewrites withheld when they could change logged output.
- Configure `property_naming` and `ignored_properties_regex` in `.editorconfig` at the `AASL` prefix or `AASL0009` / `AASL0010` rule scope. Naming styles include `pascal_case`, `camel_case`, `snake_case`, `elastic_naming`, and `semantic_conventions` / `semconv`; valid rule-scoped settings take precedence.
- The analyzer-only package adds no runtime assets to your application. It runs in Roslyn 4.8.0 or newer hosts, including Visual Studio 2022 17.8 and newer and `dotnet build` with a compatible SDK compiler.
