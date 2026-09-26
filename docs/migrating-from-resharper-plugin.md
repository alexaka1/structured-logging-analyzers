# Migrating from the ReSharper plugin

This page is for users of the ReSharper/Rider Structured Logging plugin.
This package is a NuGet analyzer that works in Rider, Visual Studio,
C# Dev Kit, and `dotnet build`. It is not affiliated with JetBrains or the
plugin's maintainers.

This project characterized plugin 2025.1.0.373 at commit `2c05392` at a
point in time. It does not track later plugin changes and adds rules of its
own. See [PROVENANCE.md](../PROVENANCE.md).

## Inspection to diagnostic mapping

| ReSharper / Rider inspection | AASL |
|---|---|
| `AnonymousObjectDestructuringProblem` | [AASL0001](rules/AASL0001.md) |
| `ComplexObjectDestructuringProblem` | [AASL0002](rules/AASL0002.md) |
| `ComplexObjectInContextDestructuringProblem` | [AASL0003](rules/AASL0003.md) |
| `ContextualLoggerProblem` | [AASL0004](rules/AASL0004.md) |
| `ExceptionPassedAsTemplateArgumentProblem` | [AASL0005](rules/AASL0005.md) |
| `TemplateDuplicatePropertyProblem` | [AASL0006](rules/AASL0006.md) |
| `TemplateIsNotCompileTimeConstantProblem` | [AASL0007](rules/AASL0007.md) |
| `PositionalPropertyUsedProblem` | [AASL0008](rules/AASL0008.md) |
| `InconsistentLogPropertyNaming` | [AASL0009](rules/AASL0009.md) |
| `InconsistentContextLogPropertyNaming` | [AASL0010](rules/AASL0010.md) |
| `LogMessageIsSentenceProblem` | [AASL0011](rules/AASL0011.md) |
| New in this project | [AASL0012](rules/AASL0012.md) |

## Plugin features and their equivalents

| Plugin feature | Roslyn equivalent |
|---|---|
| `// ReSharper disable once TemplateIsNotCompileTimeConstantProblem` | `#pragma warning disable AASL0007` or `.editorconfig` severity |
| ReSharper options page | [`.editorconfig` keys](behavior.md#configuration) |
| Inspection wiki / PSI highlighting | Diagnostic descriptors and rule docs |
| Live-template hotspots for interpolation conversion | Deterministic names; extra names as separate code actions |

## Behavioral differences

- AASL0007 is reported on non-constant templates. The plugin's test suite
  omitted that inspection.
- AASL0011 highlights only the trailing period. The plugin highlights the
  whole literal.
- AASL0005 reports an exception passed as a template argument even when
  another exception already fills the dedicated exception parameter. The
  plugin stops at the first exception in source order.
- Template holes are paired with arguments by semantic parameter binding,
  including named, optional, `params`, and reordered arguments. The plugin
  paired them by source position after the template.
- `pascal_case` lowercases the rest of an all-caps word, so `MY_IGNORED`
  becomes `MyIgnored`. The plugin kept some all-caps prefixes, producing
  `MYIgnored`.
- Primary constructors are analyzed for contextual logger mismatches. The
  plugin did not support them.
- `[LoggerMessage]`, `LoggerMessage.Define`, `DefineScope`, and Razor code
  are analyzed. The plugin did not analyze them.
- Suppression uses `#pragma warning disable` or `.editorconfig`. ReSharper
  comment suppressions are not recognized.
- Interpolation-to-template conversion offers deterministic names as
  separate code actions instead of the plugin's live-template hotspots.

See the [behavior specification](behavior.md) for the full specification.
