# structured-logging-analyzers

Fast Roslyn analyzers and code fixes for structured logging across
Microsoft.Extensions.Logging, Serilog, NLog, and ZLogger.

This package inspects structured logging templates in any C# IDE or
`dotnet build`. It is not affiliated with JetBrains or the maintainers of the
[ReSharper/Rider Structured Logging plugin](https://github.com/olsh/resharper-structured-logging).
See [PROVENANCE.md](PROVENANCE.md) and [docs/compatibility.md](docs/compatibility.md).

Package ID: `Alexaka1.Analyzers.StructuredLogging` (diagnostic prefix `AASL`).

```xml
<ItemGroup>
  <PackageReference Include="Alexaka1.Analyzers.StructuredLogging"
                    Version="0.1.0-preview.2"
                    PrivateAssets="all" />
</ItemGroup>
```

```editorconfig
[*.cs]
dotnet_diagnostic.AASL0001.severity = warning
dotnet_code_quality.AASL.property_naming = pascal_case
dotnet_code_quality.AASL.ignored_properties_regex = ^Legacy\.
```

Naming values: `pascal_case` (default), `camel_case`, `snake_case`,
`elastic_naming`, and `semantic_conventions` (for `service.name`,
`http.response.status_code`). Scope Semantic Conventions names to context
properties only with `dotnet_code_quality.AASL0010.property_naming`.

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
| [AASL0001](docs/rules/AASL0001.md) | Anonymous objects must be destructured |
| [AASL0002](docs/rules/AASL0002.md) | Complex objects should be destructured |
| [AASL0003](docs/rules/AASL0003.md) | Complex objects in log context should be destructured |
| [AASL0004](docs/rules/AASL0004.md) | Contextual logger type mismatch |
| [AASL0005](docs/rules/AASL0005.md) | Exception passed as a template argument |
| [AASL0006](docs/rules/AASL0006.md) | Duplicate template properties |
| [AASL0007](docs/rules/AASL0007.md) | Template is not a compile-time constant |
| [AASL0008](docs/rules/AASL0008.md) | Prefer named properties over positional ones |
| [AASL0009](docs/rules/AASL0009.md) | Template property naming |
| [AASL0010](docs/rules/AASL0010.md) | Context property naming |
| [AASL0011](docs/rules/AASL0011.md) | Log messages should not end with a period |
| [AASL0012](docs/rules/AASL0012.md) | Generated logging cannot use Semantic Conventions property names |

`[LoggerMessage]` declarations and `LoggerMessage.Define` / `DefineScope` are
included. When template naming is `semantic_conventions`, AASL0012 (generated
logging cannot use Semantic Conventions property names) warns on those APIs.
See [docs/microsoft-recommendations.md](docs/microsoft-recommendations.md)
for .NET SDK `CA*` / `SYSLIB10xx` rules to enable alongside this package.
