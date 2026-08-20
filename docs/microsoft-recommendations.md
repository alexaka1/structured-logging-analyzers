# Microsoft shipped logging rules

These analyzers complement, rather than replace, rules that already ship with
the .NET SDK and the `LoggerMessage` source generator. Enable the SDK rules
in `.editorconfig` when they apply to your codebase. This package does not
reimplement them unless it adds a meaningful extra (configurable naming,
cross-framework coverage, or a safe code fix).

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

| Rule | What it covers | Overlap with this package |
|---|---|---|
| [CA2254](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2254) | MEL templates must be constant expressions | `SLA0007` still runs on Serilog/NLog/ZLogger and on `LoggerMessage.Define` when the format is not constant. `[LoggerMessage]` attribute arguments are already constants, so `SLA0007` is not reported there. |
| [CA2017](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2017) | MEL placeholder/argument count | Not reimplemented. `[LoggerMessage]` uses [SYSLIB1014](https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/syslib1014) / [SYSLIB1015](https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/syslib1015). |
| [CA2253](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2253) | Numeric MEL placeholders | `SLA0008` still runs across frameworks. For `[LoggerMessage]` it adds a rename fix when an unambiguous parameter exists. |
| [CA1727](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1727) | PascalCase MEL placeholders | `SLA0009` adds camel/snake/Elastic naming, an ignore regex, and a rename fix, and applies to Serilog/NLog/ZLogger as well. |
| [CA2023](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2023) | Malformed braces in MEL templates | Not reimplemented. Malformed holes are treated as text. |
| [CA1848](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1848) | Prefer `LoggerMessage` delegates / source generation | Not reimplemented. Performance guidance only. |

## `LoggerMessage` generator diagnostics (`SYSLIB10xx`)

The generator already validates method shape, missing/unused parameters,
the first exception/logger/level parameters, duplicate event IDs, and
template names that differ only by case ([SYSLIB1021](https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/source-generator-overview)).
This package does not duplicate those diagnostics.

See the [source-generator diagnostic index](https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/source-generator-overview).

## Optional extra for MEL `ILogger<T>`

[Meziantou.Analyzer](https://github.com/meziantou/Meziantou.Analyzer) `MA0180`
can rewrite a mismatched `ILogger<T>` category. This package’s `SLA0004`
flags the same class of mistake without a type rewrite fix.
