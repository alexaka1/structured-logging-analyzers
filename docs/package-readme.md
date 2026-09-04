# Structured Logging Analyzers

Roslyn analyzers and code fixes for structured logging message templates.
Supports Serilog, NLog, Microsoft.Extensions.Logging, and ZLogger.
ZLogger support covers the 1.x string-template overloads. ZLogger 2.x
interpolated-string-handler APIs have no message template and are not analyzed.

This package is analyzer-only. It adds no runtime or compile-time assembly
reference to consuming applications. The compile-time Roslyn API floor is
4.8.0 ([Visual Studio 2022 17.8](https://learn.microsoft.com/visualstudio/extensibility/roslyn-version-support)
or a newer compiler host). See the
[IDE/compiler policy](https://github.com/alexaka1/structured-logging-analyzers/blob/main/docs/ide-compiler-policy.md).

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
A rule-scoped key applies only to that diagnostic and wins when set. The
prefix-level key applies to each rule that has no valid rule-scoped value.
Invalid values are ignored and fall through to the next level.

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
| AASL0008 | Prefer named properties instead of positional ones |
| AASL0009 | Template property naming |
| AASL0010 | Context property naming |
| AASL0011 | Log messages should not end with a period |
| AASL0012 | Generated logging cannot use Semantic Conventions property names |
