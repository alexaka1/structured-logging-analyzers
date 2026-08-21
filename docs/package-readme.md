# Structured Logging Analyzers

Roslyn analyzers and code fixes for structured logging message templates.
Supports Serilog, NLog, Microsoft.Extensions.Logging, and ZLogger.

This package is analyzer-only. It adds no runtime or compile-time assembly
reference to consuming applications.

## Install

```xml
<ItemGroup>
  <PackageReference Include="Alexaka1.Analyzers.StructuredLogging"
                    Version="0.1.0-preview.2"
                    PrivateAssets="all" />
</ItemGroup>
```

.NET 10 CLI:

```shell
dotnet package add Alexaka1.Analyzers.StructuredLogging
```

Central package management:

```xml
<PackageVersion Include="Alexaka1.Analyzers.StructuredLogging" Version="0.1.0-preview.2" />
```

```xml
<PackageReference Include="Alexaka1.Analyzers.StructuredLogging" PrivateAssets="all" />
```

## Configuration

```editorconfig
[*.cs]
dotnet_diagnostic.AASL0001.severity = warning
dotnet_diagnostic.AASL0002.severity = none

dotnet_code_quality.AASL.property_naming = pascal_case
dotnet_code_quality.AASL.ignored_properties_regex = ^Legacy\.
```

Naming values: `pascal_case` (default), `camel_case`, `snake_case`,
`elastic_naming`. The same options can be scoped to `AASL0009` or
`AASL0010` (for example `dotnet_code_quality.AASL0009.property_naming`).
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

`[LoggerMessage]` declarations and `LoggerMessage.Define` / `DefineScope` are
included. Destructure (`@`) and exception-placement rules are not applied
there; use the .NET SDK `CA*` / `SYSLIB10xx` recommendations in
[microsoft-recommendations.md](https://github.com/alexaka1/structured-logging-analyzers/blob/main/docs/microsoft-recommendations.md).

See [compatibility notes](https://github.com/alexaka1/structured-logging-analyzers/blob/main/docs/compatibility.md)
for ReSharper ID mapping and intentional differences, and
[Microsoft shipped logging rules](https://github.com/alexaka1/structured-logging-analyzers/blob/main/docs/microsoft-recommendations.md)
for CA/SYSLIB rules to enable alongside this package.
