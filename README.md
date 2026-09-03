# structured-logging-analyzers

[![NuGet](https://img.shields.io/nuget/v/Alexaka1.Analyzers.StructuredLogging?logo=nuget)](https://www.nuget.org/packages/Alexaka1.Analyzers.StructuredLogging)

Fast Roslyn analyzers and code fixes for structured logging across
Microsoft.Extensions.Logging, Serilog, NLog, and ZLogger.
ZLogger support covers the 1.x string-template overloads. ZLogger 2.x
interpolated-string-handler APIs have no message template and are not analyzed.

This package inspects structured logging templates in any C# IDE or
`dotnet build`. It is not affiliated with JetBrains or the maintainers of the
[ReSharper/Rider Structured Logging plugin](https://github.com/olsh/resharper-structured-logging).
See [PROVENANCE.md](PROVENANCE.md) and [docs/compatibility.md](docs/compatibility.md).

Package ID: `Alexaka1.Analyzers.StructuredLogging` (diagnostic prefix `AASL`).
Use the latest version from [NuGet](https://www.nuget.org/packages/Alexaka1.Analyzers.StructuredLogging). `x.y.z` is a placeholder.

```xml
<ItemGroup>
  <PackageReference Include="Alexaka1.Analyzers.StructuredLogging"
                    Version="x.y.z"
                    PrivateAssets="all" />
</ItemGroup>
```

```shell
dotnet package add Alexaka1.Analyzers.StructuredLogging
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
[docs/microsoft-recommendations.md](docs/microsoft-recommendations.md).

## Configuration

```editorconfig
[*.cs]
dotnet_diagnostic.AASL0001.severity = warning
dotnet_code_quality.AASL.property_naming = pascal_case
dotnet_code_quality.AASL.ignored_properties_regex = ^Legacy\.
```

Naming values: `pascal_case` (default), `camel_case`, `snake_case`,
`elastic_naming`, and `semantic_conventions` (alias `semconv`).
Use `semantic_conventions` for Semantic Conventions names such as
`service.name` and `http.response.status_code`. The same options can be
scoped to `AASL0009` or `AASL0010` (for example
`dotnet_code_quality.AASL0009.property_naming`). A rule-scoped key applies
only to that diagnostic. Prefix-level keys apply to both and win when set.

Build and test:

```shell
dotnet run --project test/Alexaka1.Analyzers.StructuredLogging.Tests/Alexaka1.Analyzers.StructuredLogging.Tests.csproj -c Release --no-launch-profile
dotnet pack pack/Alexaka1.Analyzers.StructuredLogging/Package.csproj
```

The production assemblies target `netstandard2.0` and analyze projects targeting
`net10.0` (including Blazor `.razor` / `.razor.cs`), `netstandard2.0`, and
SDK-style `net472`. The compile-time Roslyn API floor is 4.8.0 (Visual Studio
2022 17.8); see [docs/ide-compiler-policy.md](docs/ide-compiler-policy.md).
Allocation, telemetry, and concurrency gates are in
[docs/performance-policy.md](docs/performance-policy.md). Logging-library version
coverage is exercised by
[PackageVersionMatrixTests.cs](test/Alexaka1.Analyzers.StructuredLogging.Tests/Frameworks/PackageVersionMatrixTests.cs)
and [LatestStablePackageTests.cs](test/Alexaka1.Analyzers.StructuredLogging.Tests/Frameworks/LatestStablePackageTests.cs).

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
| [AASL0008](docs/rules/AASL0008.md) | Prefer named properties instead of positional ones |
| [AASL0009](docs/rules/AASL0009.md) | Template property naming |
| [AASL0010](docs/rules/AASL0010.md) | Context property naming |
| [AASL0011](docs/rules/AASL0011.md) | Log messages should not end with a period |
| [AASL0012](docs/rules/AASL0012.md) | Generated logging cannot use Semantic Conventions property names |

`[LoggerMessage]` declarations and `LoggerMessage.Define` / `DefineScope` are
included. Blazor `.razor` `@code` and `.razor.cs` code-behind are analyzed and
fixed.
SDK `CA*` / `SYSLIB10xx` rules are recommended alongside this package;
see [Recommended Microsoft analyzers](#recommended-microsoft-analyzers).
