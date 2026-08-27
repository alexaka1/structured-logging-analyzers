# Structured Logging Analyzers

Roslyn analyzers and code fixes for structured logging message templates.
Supports Serilog, NLog, Microsoft.Extensions.Logging, and ZLogger.

This package is analyzer-only. It adds no runtime or compile-time assembly
reference to consuming applications.

## Install

Use the latest version from NuGet. `x.y.z` is a placeholder.

```xml
<ItemGroup>
  <PackageReference Include="Alexaka1.Analyzers.StructuredLogging"
                    Version="x.y.z"
                    PrivateAssets="all" />
</ItemGroup>
```

.NET 10 CLI:

```shell
dotnet package add Alexaka1.Analyzers.StructuredLogging
```

Add `--prerelease` when the current version is a preview.

Central package management:

```xml
<PackageVersion Include="Alexaka1.Analyzers.StructuredLogging" Version="x.y.z" />
```

```xml
<PackageReference Include="Alexaka1.Analyzers.StructuredLogging" PrivateAssets="all" />
```

## Recommended Microsoft analyzers

> [!TIP]
> This package complements the .NET SDK logging rules. After installing,
> enable these `CA*` diagnostics if they are not already on. Several are
> disabled or only suggestions by default.

```editorconfig
[*.cs]
dotnet_diagnostic.CA1727.severity = warning
dotnet_diagnostic.CA1848.severity = suggestion
dotnet_diagnostic.CA2017.severity = warning
dotnet_diagnostic.CA2023.severity = warning
dotnet_diagnostic.CA2253.severity = warning
dotnet_diagnostic.CA2254.severity = warning
```

Projects targeting .NET Standard or .NET Framework also need
`<EnableNETAnalyzers>true</EnableNETAnalyzers>`. Overlap with AASL rules
and `SYSLIB10xx` notes:
[microsoft-recommendations.md](https://github.com/alexaka1/structured-logging-analyzers/blob/main/docs/microsoft-recommendations.md).

## Configuration

```editorconfig
[*.cs]
dotnet_diagnostic.AASL0001.severity = warning
dotnet_diagnostic.AASL0002.severity = none

dotnet_code_quality.AASL.property_naming = pascal_case
dotnet_code_quality.AASL.ignored_properties_regex = ^Legacy\.
```

Naming values: `pascal_case` (default), `camel_case`, `snake_case`,
`elastic_naming`, `semantic_conventions` (alias `semconv`).
Use `semantic_conventions` for Semantic Conventions names such as
`service.name` and `http.response.status_code`. The same options
can be scoped to
`AASL0009` or `AASL0010` (for example
`dotnet_code_quality.AASL0009.property_naming`).
A rule-scoped key applies only to that diagnostic. Prefix-level keys
apply to both and win when set.

## Diagnostics

| ID | Description |
|---|---|
| AASL0001 | Anonymous objects must be destructured |
| AASL0002 | Complex objects should be destructured |
| AASL0003 | Complex objects in log context should be destructured |
| AASL0004 | Contextual logger type mismatch |
| AASL0005 | Exception passed as a template argument |
| AASL0006 | Duplicate template properties |
| AASL0007 | Template is not a compile-time constant |
| AASL0008 | Prefer named properties over positional ones |
| AASL0009 | Template property naming |
| AASL0010 | Context property naming |
| AASL0011 | Log messages should not end with a period |
| AASL0012 | Generated logging cannot use Semantic Conventions property names |

`[LoggerMessage]` declarations and `LoggerMessage.Define` / `DefineScope` are
included. When template naming is `semantic_conventions`, AASL0012 warns on
`[LoggerMessage]`. `LoggerMessage.Define` / `DefineScope` bind by position and
are not reported. Destructure (`@`) and
exception-placement rules are not applied there; use the recommended
.NET SDK `CA*` / `SYSLIB10xx` rules in
[Recommended Microsoft analyzers](#recommended-microsoft-analyzers).

See [compatibility notes](https://github.com/alexaka1/structured-logging-analyzers/blob/main/docs/compatibility.md)
for trigger, span, and intentional-difference details.
