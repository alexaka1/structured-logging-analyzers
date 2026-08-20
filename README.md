# structured-logging-analyzers

Fast Roslyn analyzers and code fixes for structured logging across
Microsoft.Extensions.Logging, Serilog, NLog, and ZLogger.

This package inspects structured logging templates in any C# IDE or
`dotnet build`. It is not affiliated with JetBrains or the maintainers of the
[ReSharper/Rider Structured Logging plugin](https://github.com/olsh/resharper-structured-logging).
See [PROVENANCE.md](PROVENANCE.md) and [docs/compatibility.md](docs/compatibility.md).

Package ID: `Alexaka1.Analyzers.StructuredLogging` (diagnostic prefix `SLA`).

```xml
<ItemGroup>
  <PackageReference Include="Alexaka1.Analyzers.StructuredLogging"
                    Version="1.0.0-preview.1"
                    PrivateAssets="all" />
</ItemGroup>
```

```editorconfig
[*.cs]
dotnet_diagnostic.SLA0001.severity = warning
structured_logging_property_naming = pascal_case
structured_logging_ignored_properties_regex = ^Legacy\.
```

Build and test:

```shell
dotnet test Alexaka1.Analyzers.StructuredLogging.slnx
dotnet pack pack/Alexaka1.Analyzers.StructuredLogging/Package.csproj
```

The production assemblies target `netstandard2.0` and analyze projects targeting
`net10.0`, `netstandard2.0`, and SDK-style `net472`.

## Diagnostics

| ID | Description |
|---|---|
| [SLA0001](docs/rules/SLA0001.md) | Anonymous objects must be destructured |
| [SLA0002](docs/rules/SLA0002.md) | Complex objects should be destructured |
| [SLA0003](docs/rules/SLA0003.md) | Complex objects in log context should be destructured |
| [SLA0004](docs/rules/SLA0004.md) | Contextual logger type mismatch |
| [SLA0005](docs/rules/SLA0005.md) | Exception passed as a template argument |
| [SLA0006](docs/rules/SLA0006.md) | Duplicate template properties |
| [SLA0007](docs/rules/SLA0007.md) | Template is not a compile-time constant |
| [SLA0008](docs/rules/SLA0008.md) | Prefer named properties over positional ones |
| [SLA0009](docs/rules/SLA0009.md) | Template property naming |
| [SLA0010](docs/rules/SLA0010.md) | Context property naming |
| [SLA0011](docs/rules/SLA0011.md) | Log messages should not end with a period |

`[LoggerMessage]` declarations and `LoggerMessage.Define` / `DefineScope` are
included. See [docs/microsoft-recommendations.md](docs/microsoft-recommendations.md)
for .NET SDK `CA*` / `SYSLIB10xx` rules to enable alongside this package.
