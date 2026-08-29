# Microsoft shipped logging rules

This package **recommends** enabling the .NET SDK logging rules below
when you install it. They complement, rather than replace, AASL
diagnostics. Skip any rule you have already configured.

The `CA*` analyzers ship with the .NET SDK. They are not bundled in this
NuGet package, and several of them are disabled or only suggestions by
default. This package does not reimplement them unless it adds a
meaningful extra (configurable naming, cross-framework coverage, or a
safe code fix).

Projects targeting .NET Standard or .NET Framework also need
`<EnableNETAnalyzers>true</EnableNETAnalyzers>` for the `CA*` rules to
run. `[LoggerMessage]` `SYSLIB10xx` diagnostics come from the generator
and need no extra enablement.

## Recommended SDK quality rules

```editorconfig
[*.cs]
dotnet_diagnostic.CA1727.severity = warning
dotnet_diagnostic.CA1848.severity = suggestion
dotnet_diagnostic.CA2017.severity = warning
dotnet_diagnostic.CA2023.severity = warning
dotnet_diagnostic.CA2253.severity = warning
dotnet_diagnostic.CA2254.severity = warning
```

| Rule | .NET 10 default | What it covers | Overlap with this package |
|---|---|---|---|
| [CA2254](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2254) | Suggestion | MEL templates must be constant expressions | `AASL0007` still runs on Serilog/NLog/ZLogger and on `LoggerMessage.Define` when the format is not constant. `[LoggerMessage]` attribute arguments are already constants, so `AASL0007` is not reported there. |
| [CA2017](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2017) | Warning | MEL placeholder/argument count | Not reimplemented. `[LoggerMessage]` uses [SYSLIB1014](https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/syslib1014) / [SYSLIB1015](https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/syslib1015). |
| [CA2253](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2253) | Suggestion | Numeric MEL placeholders | `AASL0008` still runs across frameworks. It adds a rename fix for `[LoggerMessage]` when an unambiguous parameter exists, and for invocations when the aligned argument has a derivable identifier. |
| [CA1727](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1727) | Disabled | PascalCase MEL placeholders | `AASL0009` adds camel/snake/Elastic/Semantic Conventions naming, an ignore regex, and a rename fix, and applies to Serilog/NLog/ZLogger as well. Disable CA1727 if templates use Semantic Conventions names such as `{service.name}`. |
| [CA2023](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2023) | Warning | Malformed braces in MEL templates | Not reimplemented. Malformed holes are treated as text. |
| [CA1848](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1848) | Disabled | Prefer `LoggerMessage` delegates / source generation | Not reimplemented. Performance guidance only. If template naming is `semantic_conventions`, `AASL0012` warns on `[LoggerMessage]`. |

On .NET 10+, CA2017 and CA2023 are already warnings when SDK analyzers
are enabled. Still set CA1727, CA1848, CA2253, and CA2254 if those are
not already configured.

## `LoggerMessage` generator diagnostics (`SYSLIB10xx`)

The generator already validates method shape, missing/unused parameters,
the first exception/logger/level parameters, duplicate event IDs, and
template names that differ only by case ([SYSLIB1021](https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/source-generator-overview)).
This package does not duplicate those diagnostics. `AASL0012` is the extra:
it warns when `semantic_conventions` naming is configured and the code uses
`[LoggerMessage]`, which cannot bind dotted names to C# parameters.
`LoggerMessage.Define` / `DefineScope` bind values by position, so dotted names
can work there.

See the [source-generator diagnostic index](https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/source-generator-overview).
