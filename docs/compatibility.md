# Compatibility specification

This document is the behavioral contract for the Roslyn analyzers. It records
preserved quirks and intentional corrections.

Diagnostic prefix: `AASL` (`Alexaka1.Analyzers.StructuredLogging`).
Package ID: `Alexaka1.Analyzers.StructuredLogging`.
Host and compiler floors: [ide-compiler-policy.md](ide-compiler-policy.md).
Allocation and concurrency gates: [performance-policy.md](performance-policy.md).

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
  `TemplateIsNotCompileTimeConstantProblem`; Roslyn still reports `AASL0007`
  on non-constant templates.
- Elastic naming and ignored-property regex cases use ReSharper settings
  layers in the plugin. Default InspectCode/Roslyn comparison uses PascalCase
  with no ignore regex.
- `AASL0011` highlights the trailing period; the plugin highlights the
  whole literal. Comparison is by file and rule id, not span.
- The plugin returns on the first `Exception` argument, including the
  dedicated exception slot, so a later template-argument exception is
  missed. `AASL0005` reports that later exception.

See `test/comparison/README.md` and `test/comparison/reports/comparison.md`.
The committed report is a frozen snapshot of source commit
[`e59964669bbc1dbe84d945eb815be499709eb1bc`](https://github.com/alexaka1/structured-logging-analyzers/commit/e59964669bbc1dbe84d945eb815be499709eb1bc),
not a current parity guarantee.

## Diagnostic ID mapping

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
| *(no plugin equivalent)* | [AASL0012](rules/AASL0012.md) |

The comparison runner uses the same map in `test/comparison/runner/RuleMap.cs`.

## Host differences (not portable)

| Plugin feature | Roslyn equivalent |
|---|---|
| `// ReSharper disable once TemplateIsNotCompileTimeConstantProblem` | `#pragma warning disable AASL0007` or `.editorconfig` severity |
| ReSharper options page | `.editorconfig` keys below |
| Inspection wiki / PSI highlighting | Diagnostic descriptors and rule docs |
| Live-template hotspots for interpolation conversion | Deterministic names; extra names as separate code actions |

## Package consumption

The NuGet package is analyzer-only. It contains exactly the analyzer and
CodeFixes assemblies under `analyzers/dotnet/cs`, no `lib/` assets, no Roslyn
assemblies, and no package dependencies. A real `PackageReference` consumer
restored from a local package feed must load the analyzer during `dotnet build`
and must not copy either analyzer or Roslyn assemblies to application output.
This compiler check does not emulate an IDE CodeFixes host.

## Invocation registration

The invocation analyzer runs when the compilation references a supported
logging library or declares any type named
`MessageTemplateFormatMethodAttribute`. The attribute may be declared in any
namespace. If it exists only in a referenced assembly and the compilation
does not reference a supported logging library, attributed wrappers are not
analyzed.

## Configuration

```editorconfig
[*.cs]
dotnet_diagnostic.AASL0001.severity = warning

dotnet_code_quality.AASL.property_naming = pascal_case
dotnet_code_quality.AASL.ignored_properties_regex =
```

`dotnet_code_quality.AASL.property_naming` values:

- `pascal_case` (default)
- `camel_case`
- `snake_case`
- `elastic_naming`
- `semantic_conventions` (alias `semconv`)

The same options can be scoped to `AASL0009` or `AASL0010`, for example
`dotnet_code_quality.AASL0009.property_naming`. A rule-scoped key applies
only to that diagnostic. Prefix-level keys apply to both and win when set.

Invalid configuration is ignored; analyzers do not throw. An invalid
prefix-level naming value does not mask a valid rule-scoped value.

## Argument mapping

**Correction.** Template properties are paired with arguments using semantic
parameter binding (named, optional, `params`, and reordered arguments). The
plugin used source position after the template argument.

This includes named and reordered arguments to
`Serilog.Context.LogContext.PushProperty`; its `name`, `value`, and
`destructureObjects` parameters are identified by parameter name. Microsoft
`ILogger` extension methods are also bound by their reduced or static form,
so exception placement is checked consistently for both
`logger.LogError(template, value)` and
`LoggerExtensions.LogError(logger, template, value)`.

Serilog `ILogger.ForContext(string propertyName, object value, bool
destructureObjects)`, the instance-method twin of `PushProperty`, is not
covered by AASL0003 or AASL0010.

When overload resolution fails in half-written code, candidate methods are
only used when their bound template expression is clearly a string. This
keeps diagnostics on a recognizable string template without treating an
unresolved value argument as the template.

Explicit `params` arrays are unpacked when their elements are available in
source, including C# collection expressions without spread elements. Each
element is paired with its template hole.

ZLogger 1.x value overloads name the template parameter `format`, while
zero-value overloads name it `message`. Both forms receive template rules
when the selected parameter is a string.

## Mixed templates

**Preserved.** Templates that mix positional and named holes are treated as
named templates. `AASL0008` is not reported for mixed templates.

## Non-constant templates

**Preserved / combined pipeline.** Most template-parsing rules skip dynamic
templates. Exception placement (`AASL0005`) still runs. The analyzer does not
return early after `AASL0007`.

## Exception placement

**Correction.** An exception bound to a parameter before the template (the
dedicated exception slot) does not suppress [AASL0005](rules/AASL0005.md)
for later exceptions that are still template arguments, including when
named arguments put `messageTemplate` before `exception`. The plugin
returns on the first `Exception` argument in source order.

"Before" and "after" refer to parameter binding, not source argument order.
Named and reordered arguments therefore behave like their positional forms.
Type parameters constrained to `Exception` are treated as exception types.

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

- Split on non-alphanumeric separators, camelCase and acronym boundaries,
  and a digit followed by an uppercase letter (`Utf8Bytes` is `Utf8` +
  `Bytes`). A letter followed by a digit stays in the same word.
- `pascal_case`: capitalize each word; remaining letters in a word are
  lowercased (`MY_IGNORED` → `MyIgnored`). JetBrains `StringUtil` keeps
  some all-caps prefixes (`MYIgnored`). This only shows up when such a
  name is not ignored by regex.
- `camel_case`: PascalCase then decapitalize the first letter.
- `snake_case`: lowercase words joined by `_`.
- `elastic_naming`: snake_case with `_` replaced by `.`.
- `semantic_conventions`: lowercase ASCII; `.` is a namespace delimiter and `_`
  joins words inside a component (`service.name`,
  `http.response.status_code`). This matches Semantic Conventions
  attribute naming. Names that already match are left unchanged.
  Otherwise existing `.` separators are kept, non-ASCII characters are
  treated as separators, and remaining ASCII words are joined with `_`
  (`MyProperty` → `my_property`). Names that cannot be rewritten to a
  valid form (for example a leading digit with no letter to start the
  name) are not reported, matching the other styles. `elastic_naming` is
  not a substitute: it would rewrite `http.response.status_code` to
  `http.response.status.code`.

## Contextual loggers

**Improvement.** Primary constructors are analyzed (upstream issue #130).
Unresolved category and `ForContext<T>` type arguments are skipped until the
compiler can bind the type.

## Parser

The analyzer parses each recognized constant template once from its logical
string value. The shared result records each hole's name, raw text, logical
offsets, destructuring hint, alignment, format, and positional classification.
Independent rules inspect that result. See [PROVENANCE.md](../PROVENANCE.md) for
implementation lineage.

The parser follows the public message-template grammar for:

- Text with `{property}` holes
- `{{` / `}}` escapes
- `@` destructure and `$` stringify
- Alignment with an optional leading `-` followed by one or more digits,
  including zero and widths larger than `Int32.MaxValue`
- A non-empty format containing any character except `}`, including extra `:`
  or `,` (`{Timestamp:HH:mm:ss}`, `{Value:#,0}`). A trailing `:` means no
  format, so `{Value:}` and `{Value,10:}` remain holes. `{Bad: {Good}}` is one
  hole whose format is ` {Good`, not a nested property.
- malformed holes become text; a later or nested valid hole is still parsed
- A hole is positional only when its name parses as a non-negative `Int32`.
  `{00}` is positional; `{ 0}` and `{999999999999}` are named
- all-positional vs named vs mixed classification as above

For recovery and naming diagnostics, the parser also recognizes property names
containing `.`, spaces, or non-ASCII letters. These names extend the public
grammar so rules can report and safely rewrite library-specific or invalid
names instead of treating the whole hole as text.

## Literal mapping

`LiteralSpanMapper` converts the parser's logical offsets back to exact C#
source spans for diagnostics and fixes. It maps:

- regular string literals
- verbatim string literals
- single-line and indentation-trimmed multiline raw string literals
- constant concatenations
- escaped braces, quotes, backslashes, and Unicode escapes

Interpolated strings that contain only constant text are mapped from their
runtime value. For example, `$"user {{UserId}}"` is analyzed as the template
`user {UserId}`, while the diagnostic span still points into the escaped source
text. Constant regular, verbatim, and raw interpolated text follows the same
runtime-value rule.

Diagnostics can still be reported when source text cannot be mapped exactly,
including a constant field or a concatenation containing a constant fragment.
Source-rewriting fixes are withheld in those cases; shared constants also
remain diagnostic-only unless their use is exclusive to the logging method.
For a concatenation whose unmappable final constant fragment ends in one
period, `AASL0011` points at that fragment and remains diagnostic-only.

## Fixes

| Diagnostic | Fix | Notes |
|---|---|---|
| [AASL0001](rules/AASL0001.md), [AASL0002](rules/AASL0002.md) | Insert `@` after `{` | Full for safely mappable Serilog-like invocations only (not MEL, ZLogger, or `LoggerMessage.Define` / `DefineScope`). Constant fields and concatenations with unmappable fragments keep the diagnostic but have no fix. |
| [AASL0003](rules/AASL0003.md) | Add `destructureObjects: true` | `LogContext.PushProperty` with exactly two arguments; named and reordered `name` / `value` arguments are bound by parameter. |
| [AASL0004](rules/AASL0004.md) | Replace logger category with the containing type | `ILogger<T>` constructor / primary-constructor parameters and matching fields/properties in that type. `ForContext<T>()` type argument only; conditional-access calls are diagnosed, but the type-argument fix is offered for direct member-access calls. Nested types are left alone. |
| [AASL0005](rules/AASL0005.md) | Move exception before the template | Inserts the exception argument immediately before the message template (after EventId/LogLevel) and removes the aligned hole when the template is a mappable constant. Interpolated or otherwise unmappable templates move the argument only. Reduced and explicit static MEL extension calls are both supported. Not offered when another argument already binds the exception parameter, regardless of argument order, or when the diagnosed exception precedes the template in source. |
| [AASL0006](rules/AASL0006.md) | Rename duplicate holes to unique names | Invocations only. Subsequent holes are uniquified (`{Test}` `{Test2}`), or renamed from argument identifiers when those can be derived. Leaf and qualified primary suggestions share one used-name set. Qualified names are a second action when they differ. Not offered for `[LoggerMessage]` (renaming a hole would not add a C# parameter). |
| [AASL0008](rules/AASL0008.md) | Rename positional hole | `[LoggerMessage]` when the remaining parameters match the holes. Invocations when the aligned argument has a derivable identifier (`order.Id` → `{Id}`). Qualified names as a second action when they differ. Not offered for literals, anonymous objects, `LoggerMessage.Define` / `DefineScope`, or a params array passed as a single variable. |
| [AASL0009](rules/AASL0009.md) | Rename hole to suggested name | Full when the source map is rewriteable; also `[LoggerMessage]` attribute strings. Unmappable and shared-constant templates remain diagnostic-only. |
| [AASL0010](rules/AASL0010.md) | Replace `PushProperty` name | Full for a constant `name` argument, including named/reordered calls |
| [AASL0011](rules/AASL0011.md) | Remove trailing `.` | Full when the diagnostic span contains exactly the period; unmappable and shared-constant templates remain diagnostic-only. |
| [AASL0007](rules/AASL0007.md) | Convert interpolation | Partial: deterministic leaf names; extra action uses qualified names when they differ. Escaped braces remain escaped. No fix is offered when an interpolation value is ref-like or a pointer, because the generated object argument would not compile. |

No code fix for [AASL0012](rules/AASL0012.md): the diagnostic fires on `[LoggerMessage]` whenever template naming is `semantic_conventions`, so a template rewrite cannot clear it. Converting the method to `LoggerMessage.Define` or a `Log*` call is an API choice (and fights CA1848).

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
`Message`, named constructor arguments before a positional message, generic
methods, and `LoggerMessage.Define` / `DefineScope`.
Generic methods still receive style diagnostics; the Microsoft generator
currently reports [SYSLIB1011](https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/syslib1011)
instead of emitting an implementation.

Placeholder-to-parameter matching is case-insensitive and ignores parameter
order. The first `ILogger`, `LogLevel`, and `Exception` parameters are
special; later instances are ordinary template parameters. Format specifiers
such as `{Value:E}` are preserved.

| Rule | `[LoggerMessage]` / `Define` |
|---|---|
| [AASL0006](rules/AASL0006.md) duplicate properties | Apply (exact names; case-only duplicates are SYSLIB1021) |
| [AASL0008](rules/AASL0008.md) positional properties | Apply; rename fix when parameters are unambiguous |
| [AASL0009](rules/AASL0009.md) property naming | Apply |
| [AASL0011](rules/AASL0011.md) trailing period | Apply |
| [AASL0012](rules/AASL0012.md) generated logging vs Semantic Conventions | Apply to `[LoggerMessage]` when template naming is `semantic_conventions`. Not applied to `Define` / `DefineScope`. |
| [AASL0001](rules/AASL0001.md) / [AASL0002](rules/AASL0002.md) destructuring | **Not applied.** MEL templates do not accept Serilog `@`. |
| [AASL0004](rules/AASL0004.md) contextual logger | Still applied to the containing type / injected `ILogger<T>` |
| [AASL0007](rules/AASL0007.md) compile-time constant | Not applied to attribute arguments. Applied to `Define`/`DefineScope` when the format is not constant. |
| [AASL0005](rules/AASL0005.md) exception placement | **Not applied.** Use SYSLIB1013 and related generator diagnostics. |
| [AASL0010](rules/AASL0010.md) context properties | Not applicable |

Holes that match the first logger, level, or exception parameter are skipped
so they are not double-reported with SYSLIB1002 / 1013 / 1018.

A fix does not rewrite a `const string` message unless that constant is
private, declared on the same type and syntax tree, and referenced only by the
logging method. Non-private, shared, and cross-tree constants still produce
diagnostics; the rename and trailing-period fixes are withheld. Cross-tree
diagnostics point at each method's attribute argument instead of the constant
declaration, so document-scoped IDE analysis can display them without
duplicating a diagnostic on the declaration.
Constant attribute expressions that cannot be mapped to literal source, such
as `$"{Shared}"`, still produce diagnostics on the attribute argument. Their
rename and trailing-period fixes are withheld.

## Microsoft logging scopes

`ILogger.BeginScope` is a supported Microsoft logging entry point. Its
`messageFormat` argument receives the same named-property, positional,
trailing-period, and compile-time-constant checks as other `LoggerExtensions`
templates. Serilog-only destructuring rules do not apply to scopes.

This package recommends enabling the .NET SDK `CA*` / `SYSLIB10xx`
logging rules if they are not already on; see
`docs/microsoft-recommendations.md`.

## Razor / Blazor

Razor source-generated C# is analyzed even though the SDK marks those trees
as generated. Supported names include build-time `*_razor.g.cs` and
`*.razor.g.cs`, Visual Studio `*.razor.<hash>.ide.g.cs`, and VS Code or C# Dev
Kit `*.razor__virtual.cs`, plus the `.cshtml` equivalents.
Diagnostics from `@code` map back to the `.razor` file through `#line`
directives. Code fixes that rewrite the C# template apply to those trees
(including files with `<auto-generated/>` or `generated_code = true`); hosts
map the resulting text changes back to `.razor`. Code-behind `.razor.cs`
files are ordinary C# and are analyzed and fixed the same way as other
compilations. `LoggerMessage.g.cs` and other non-Razor-generated files are
still skipped.
