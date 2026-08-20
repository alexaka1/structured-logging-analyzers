# Structured Logging Analyzers

Roslyn analyzers and code fixes for structured logging message templates.
Supports Serilog, NLog, Microsoft.Extensions.Logging, and ZLogger.

This package is analyzer-only. It adds no runtime or compile-time assembly
reference to consuming applications.

## Install

```xml
<ItemGroup>
  <PackageReference Include="StructuredLogging.Analyzers"
                    Version="1.0.0-preview.1"
                    PrivateAssets="all" />
</ItemGroup>
```

.NET 10 CLI:

```shell
dotnet package add StructuredLogging.Analyzers
```

Central package management:

```xml
<PackageVersion Include="StructuredLogging.Analyzers" Version="1.0.0-preview.1" />
```

```xml
<PackageReference Include="StructuredLogging.Analyzers" PrivateAssets="all" />
```

## Configuration

```editorconfig
[*.cs]
dotnet_diagnostic.SLA0001.severity = warning
dotnet_diagnostic.SLA0002.severity = none

structured_logging_property_naming = pascal_case
structured_logging_ignored_properties_regex = ^Legacy\.
```

Naming values: `pascal_case` (default), `camel_case`, `snake_case`,
`elastic_naming`.

## Diagnostics

| ID | Description |
|---|---|
| SLA0001 | Anonymous objects must be destructured |
| SLA0002 | Complex objects should be destructured |
| SLA0003 | Complex objects in log context should be destructured |
| SLA0004 | Contextual logger type mismatch |
| SLA0005 | Exception passed as a template argument |
| SLA0006 | Duplicate template properties |
| SLA0007 | Template is not a compile-time constant |
| SLA0008 | Prefer named properties over positional ones |
| SLA0009 | Template property naming |
| SLA0010 | Context property naming |
| SLA0011 | Log messages should not end with a period |

`[LoggerMessage]` declarations and `LoggerMessage.Define` / `DefineScope` are
included. Destructure (`@`) and exception-placement rules are not applied
there; use the .NET SDK `CA*` / `SYSLIB10xx` recommendations in
[microsoft-recommendations.md](https://github.com/alexaka1/structured-logging-analyzers/blob/main/docs/microsoft-recommendations.md).

See [compatibility notes](https://github.com/alexaka1/structured-logging-analyzers/blob/main/docs/compatibility.md)
for ReSharper ID mapping and intentional differences, and
[Microsoft shipped logging rules](https://github.com/alexaka1/structured-logging-analyzers/blob/main/docs/microsoft-recommendations.md)
for CA/SYSLIB rules to enable alongside this package.
