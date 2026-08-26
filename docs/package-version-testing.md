# Testing multiple logging library versions

The analyzer never references Serilog, NLog, Microsoft.Extensions.Logging, or
ZLogger. It inspects consumer compilations through Roslyn symbols. A test
project can `PackageReference` only one version of each of those packages, so
a project-per-version matrix is the wrong tool: it would explode combinatorially
and still not exercise the analyzer the way a consumer compiler does.

## What other analyzer libraries do

Roslyn SDK tests (`Microsoft.CodeAnalysis.Testing`) restore extra NuGet
identities as `MetadataReference`s:

```csharp
ReferenceAssemblies = ReferenceAssemblies.Net.Net80
    .AddPackages([new PackageIdentity("Serilog", "4.2.0")]);
```

The same idea shows up in Meziantou.Analyzer and most SDK-style analyzer
repos: keep one test host, and swap the **compilation references** of the
code under analysis.

This repository already has a custom `AnalyzerTestHost` (markup spans, code
fixes, FixAll). The matrix uses that host plus `NuGetPackageResolver`, which
runs `dotnet restore` on a stub project and reads compile assets from
`project.assets.json`. That is the same restore graph a consumer of that
package version would get.

## Layout

| Layer | Role |
|---|---|
| `test/...Tests.csproj` `PackageReference`s | Single **current** version loaded into the test process for `typeof(...)` default hosts. Most parity and fix tests use this. That pin is also a matrix row, read from the csproj at test time. |
| `PackageVersionMatrix.cs` | Additional versions: floor, previous major, and `*Latest` canaries. Stub restores target `net10.0`. |
| `Frameworks/PackageVersionMatrixTests.cs` | Same invocation shapes against each restored version. Compilation must succeed. |
| `samples/` | Real SDK hosts (`net10.0`, `netstandard2.0`, `net472`). `Net472Example` covers Framework-shaped reference sets. Newer Roslyn ships with the SDK, not with the analyzer compile-time package. |
| `test/comparison/` | Frozen InspectCode parity corpus. Not a version matrix. |

Do not add a test project per Serilog/NLog/MEL/ZLogger version.

## Adding a version

1. Add a floor, previous-major, or `*Latest` constant to `PackageVersionMatrix.cs`. The test project's `PackageReference` is included automatically.
2. If the public API still matches the existing source, the `[Theory]` already covers it.
3. If the API family changed (ZLogger 1 format strings vs ZLogger 2 interpolated handlers / `[ZLoggerMessage]`), add a dedicated test with source that compiles against that family.

The `*Latest` constants are Renovate regex-manager targets so newest releases
can move without rewriting the test project's `PackageReference`s.

## Roslyn is not a consumer library

`Microsoft.CodeAnalysis.CSharp` 4.8.0 is the **compile-time API floor** (Visual
Studio 2022 17.8). Bumping it to 5.x would require VS 2026. The analyzer is
forward-compatible with newer compilers; samples building on the .NET 10 SDK
are the host smoke test. Do not put Roslyn into the logging-library matrix.

## Renovate

Major upgrades of logging libraries in the test `.csproj` would change the
default `typeof` host and drop coverage of the previous API. Majors are
disabled there. Latest majors are tracked in `PackageVersionMatrix.cs` instead.
Minor and patch bumps of the test-project pins stay in the matrix because those
versions are read from the csproj.
