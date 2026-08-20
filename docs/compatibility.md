# Compatibility specification

This document is the behavioral contract for the Roslyn analyzers. It maps
each ReSharper inspection to a diagnostic, records preserved quirks, and
lists intentional corrections.

Diagnostic prefix: `SLA` (Structured Logging Analyzer).
Package ID: `Alexaka1.Analyzers.StructuredLogging`.

## ReSharper CLI comparison

Run `./test/comparison/run-comparison.sh` to compare InspectCode plus the
published [marketplace plugin](https://github.com/olsh/resharper-structured-logging)
(`ReSharper.Structured.Logging` 2025.1.0.373) with these analyzers on the
characterization corpus under `test/comparison/corpus`.

- Latest 2025.x InspectCode is tried first (currently 2025.3.5, Wave 253).
  The published plugin depends on Wave 251. InspectCode 2025.3.5 still loaded
  it in the original comparison environment; the script falls back to
  InspectCode 2025.1.9 if the plugin does not load.
- The upstream plugin unit tests omit
  `TemplateIsNotCompileTimeConstantProblem`; Roslyn still reports `SLA0007`
  on non-constant templates.
- Elastic naming and ignored-property regex cases use ReSharper settings
  layers in the plugin. Default InspectCode/Roslyn comparison uses PascalCase
  with no ignore regex.
- `SLA0011` highlights the trailing period; the plugin highlights the
  whole literal. Comparison is by file and rule id, not span.

See `test/comparison/README.md` and `test/comparison/reports/comparison.md`.

## Host differences (not portable)

| Plugin feature | Roslyn equivalent |
|---|---|
| `// ReSharper disable once TemplateIsNotCompileTimeConstantProblem` | `#pragma warning disable SLA0007` or `.editorconfig` severity |
| ReSharper options page | `.editorconfig` keys below |
| Inspection wiki / PSI highlighting | Diagnostic descriptors and rule docs |
| Live-template hotspots for interpolation conversion | Deterministic names; extra names as separate code actions |

## Configuration

```editorconfig
[*.cs]
dotnet_diagnostic.SLA0001.severity = warning

structured_logging_property_naming = pascal_case
structured_logging_ignored_properties_regex =
```

`structured_logging_property_naming` values:

- `pascal_case` (default)
- `camel_case`
- `snake_case`
- `elastic_naming`

Invalid configuration is ignored; analyzers do not throw.

## Argument mapping

**Correction.** Template properties are paired with arguments using semantic
parameter binding (named, optional, `params`, and reordered arguments). The
plugin used source position after the template argument.

## Mixed templates

**Preserved.** Templates that mix positional and named holes are treated as
named templates. `SLA0008` is not reported for mixed templates.

## Non-constant templates

**Preserved / combined pipeline.** Most template-parsing rules skip dynamic
templates. Exception placement (`SLA0005`) still runs. The analyzer does not
return early after `SLA0007`.

## Complex-type classification

**Preserved, with characterization tests.** An argument needs destructuring
when walking its class hierarchy does not find an override of `Object.ToString()`,
with these exceptions:

- `object` itself
- predefined numeric types
- `string`
- `Guid`
- nullable types unwrap to the underlying type
- exact `System.Collections.Generic.Dictionary<TKey,TValue>` inspects `TKey`
  (plugin quirk)
- other generic enumerables inspect the element type
- interfaces, structs, type parameters, and error types are not flagged
- anonymous objects are flagged by creation syntax, not `ToString()` heuristics

## Naming

Replacement for JetBrains `StringUtil` naming:

- Split on non-alphanumeric separators and camelCase / acronym boundaries.
- `pascal_case`: capitalize each word; remaining letters in a word are
  lowercased (`MY_IGNORED` → `MyIgnored`). JetBrains `StringUtil` keeps
  some all-caps prefixes (`MYIgnored`). This only shows up when such a
  name is not ignored by regex.
- `camel_case`: PascalCase then decapitalize the first letter.
- `camel_case`: PascalCase then decapitalize the first letter.
- `snake_case`: lowercase words joined by `_`.
- `elastic_naming`: snake_case with `_` replaced by `.`.

## Contextual loggers

**Improvement.** Primary constructors are analyzed (upstream issue #130).

## Parser

Independent implementation of message-template syntax:

- Text with `{property}` holes
- `{{` / `}}` escapes
- `@` destructure and `$` stringify
- alignment and format
- property names: letters, digits, `_`, `.`, space
- malformed holes become text
- all-positional vs named vs mixed classification as above

## Literal mapping

Code fixes map logical template offsets through:

- regular string literals
- verbatim string literals
- raw string literals
- constant concatenations
- escaped braces, quotes, backslashes, and Unicode escapes

## Fixes

| Diagnostic | Fix | Notes |
|---|---|---|
| SLA0001, SLA0002 | Insert `@` after `{` | Full; Serilog-like invocations only |
| SLA0008 | Rename positional hole | `[LoggerMessage]` when the remaining parameters match the holes |
| SLA0009 | Rename hole to suggested name | Full; also `[LoggerMessage]` attribute strings |
| SLA0010 | Replace `PushProperty` name | Full |
| SLA0011 | Remove trailing `.` | Full; span is the period |
| SLA0007 | Convert interpolation | Partial: deterministic names, no hotspots |

No first-milestone fixes for SLA0003, SLA0004, SLA0005, or SLA0006.

## Source-generated logging (`[LoggerMessage]`)

This is an extension beyond original-plugin parity (upstream
[#81](https://github.com/olsh/resharper-structured-logging/issues/81) and
[#64](https://github.com/olsh/resharper-structured-logging/issues/64)).

Analysis targets the attributed method and the attribute message string, not
generated implementations. The Microsoft generator marks the partial
implementation with `[GeneratedCode]`, so the analyzer inspects user syntax
trees (including those partial declarations) and skips generated files such
as `LoggerMessage.g.cs`. Copied `LoggerMessage.Define` strings in generated
output are therefore not double-reported.

Recognized forms: static partial methods, `this ILogger` extensions, instance
methods using an `ILogger` field or primary-constructor parameter, fixed and
parameterized `LogLevel`, named `Message` / constructor arguments, omitted
`Message`, generic methods, and `LoggerMessage.Define` / `DefineScope`.
Generic methods still receive style diagnostics; the Microsoft generator
currently reports [SYSLIB1011](https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/syslib1011)
instead of emitting an implementation.

Placeholder-to-parameter matching is case-insensitive and ignores parameter
order. The first `ILogger`, `LogLevel`, and `Exception` parameters are
special; later instances are ordinary template parameters. Format specifiers
such as `{Value:E}` are preserved.

| Rule | `[LoggerMessage]` / `Define` |
|---|---|
| SLA0006 duplicate properties | Apply (exact names; case-only duplicates are SYSLIB1021) |
| SLA0008 positional properties | Apply; rename fix when parameters are unambiguous |
| SLA0009 property naming | Apply |
| SLA0011 trailing period | Apply |
| SLA0001 / SLA0002 destructuring | **Not applied.** MEL templates do not accept Serilog `@`. |
| SLA0004 contextual logger | Still applied to the containing type / injected `ILogger<T>` |
| SLA0007 compile-time constant | Not applied to attribute arguments. Applied to `Define`/`DefineScope` when the format is not constant. |
| SLA0005 exception placement | **Not applied.** Use SYSLIB1013 and related generator diagnostics. |
| SLA0010 context properties | Not applicable |

Holes that match the first logger, level, or exception parameter are skipped
so they are not double-reported with SYSLIB1002 / 1013 / 1018.

A fix does not rewrite a `const string` message unless that constant is
declared on the same type and referenced only by the logging method.
Shared constants still produce diagnostics; the rename and trailing-period
fixes are withheld.

.NET SDK `CA*` / `SYSLIB10xx` rules are recommendations on top of this
package; see `docs/microsoft-recommendations.md`.

## Diagnostic catalog

| ID | ReSharper ID | Default | Message |
|---|---|---|---|
| SLA0001 | AnonymousObjectDestructuringProblem | Warning | Anonymous objects must be destructured |
| SLA0002 | ComplexObjectDestructuringProblem | Warning | Complex objects with default ToString() implementation probably need to be destructured |
| SLA0003 | ComplexObjectInContextDestructuringProblem | Warning | Complex objects with default ToString() implementation probably need to be destructured |
| SLA0004 | ContextualLoggerProblem | Warning | Incorrect type is used for contextual logger |
| SLA0005 | ExceptionPassedAsTemplateArgumentProblem | Warning | Exception should be passed to the exception argument |
| SLA0006 | TemplateDuplicatePropertyProblem | Warning | Duplicate properties in message template |
| SLA0007 | TemplateIsNotCompileTimeConstantProblem | Warning | Message template should be compile time constant |
| SLA0008 | PositionalPropertyUsedProblem | Warning | Prefer named properties instead of positional ones |
| SLA0009 | InconsistentLogPropertyNaming | Warning | Property name '{0}' does not match naming rules. Suggested name is '{1}'. |
| SLA0010 | InconsistentContextLogPropertyNaming | Warning | Property name '{0}' does not match naming rules. Suggested name is '{1}'. |
| SLA0011 | LogMessageIsSentenceProblem | Warning | Log event messages should be fragments, not sentences. Avoid a trailing period/full stop. |

### SLA0001 Anonymous object must be destructured

- Trigger: logging invocation whose template is constant; a later argument is an anonymous-object creation; the aligned named hole uses default destructuring.
- Span: the hole in the template.
- Exclusions: positional-only templates; non-constant templates; holes with `@` or `$`.

### SLA0002 Complex object must be destructured

- Trigger: aligned named hole with default destructuring whose argument needs destructuring.
- Span: the hole.
- Exclusions: stringify/destructure holes; types classified as adequate `ToString()`.

### SLA0003 Complex object in log context

- Trigger: `Serilog.Context.LogContext.PushProperty` with exactly two arguments and a value that needs destructuring.
- Span: the invocation.
- Exclusions: explicit `destructureObjects` argument present.

### SLA0004 Contextual logger mismatch

- Trigger: constructor (including primary) parameter of type `ILogger<T>` where `T` is not the containing type; or `ILogger.ForContext<T>()` where `T` is not the containing type.
- Span: the `ILogger<T>` type usage, or the `ForContext<T>()` invocation.

### SLA0005 Exception passed as template argument

- Trigger: an argument whose type is `Exception` or a subtype appears at or after the template argument, and an overload exists with an exception parameter before that argument index.
- Span: the exception argument.
- Preserved quirk: an exception *before* the template suppresses the diagnostic for later exceptions.

### SLA0006 Duplicate template properties

- Trigger: two or more named holes with the same property name.
- Span: each duplicate hole.
- Exclusions: positional-only templates.

### SLA0007 Template is not compile-time constant

- Trigger: the template argument's expression is not a compile-time constant.
- Span: the template expression.
- Other rules still run.

### SLA0008 Positional properties

- Trigger: every hole is positional.
- Span: each positional hole.
- Exclusions: mixed templates.

### SLA0009 Template property naming

- Trigger: named hole whose name does not match the configured convention and is not ignored by regex.
- Span: the hole.

### SLA0010 Context property naming

- Trigger: `LogContext.PushProperty` with a constant name that does not match the convention.
- Span: the name argument.

### SLA0011 Message ends with a period

- Trigger: last constant template fragment matches `(?<!\.)\.$`.
- Span: the trailing period (more precise than the plugin's whole-literal span).
- Exclusions: ellipses (`...`).
