# Testing multiple logging library versions

## TL;DR

Two tracks. One test host. No project-per-version.

| Track | What | Renovate |
|---|---|---|
| Test `.csproj` `PackageReference`s | One current version in the test process (`typeof` hosts). | Minor/patch yes. **Majors frozen.** |
| `*Latest` constants | Always-run **latest stable**, including new majors. | Regex manager owns these. Majors are PRs, not automerged. |
| Floor / previous major | Historical API families. | Hand-edited when a major must stay covered after `*Latest` moves on. |

Latest-stable tests: `Frameworks/LatestStablePackageTests.cs`.
Historical matrix: `Frameworks/PackageVersionMatrixTests.cs`.

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
| `PackageVersionMatrix.cs` `*Latest` | nuget.org latest stable of each logging library, including majors. Renovate regex-manager targets. |
| `Frameworks/LatestStablePackageTests.cs` | Always-run Facts against those `*Latest` constants. This is the suite that breaks when a new major ships a different API. |
| `PackageVersionMatrix.cs` floors | Historical versions (Serilog 2/3, NLog 4, MEL 6/9, ZLogger 2, …). |
| `Frameworks/PackageVersionMatrixTests.cs` | Same invocation shapes against each restored version, including floors and the current test-project pin. Compilation must succeed. |
| `samples/` | Real SDK hosts (`net10.0`, `netstandard2.0`, `net472`). `Net472Example` covers Framework-shaped reference sets. Newer Roslyn ships with the SDK, not with the analyzer compile-time package. |
| `test/comparison/` | Frozen InspectCode parity corpus. Not a version matrix. |

Do not add a test project per Serilog/NLog/MEL/ZLogger version.

## Adding a version

1. A new nuget.org release of Serilog/NLog/MEL/ZLogger is a Renovate PR against the matching `*Latest` constant. Merge it when `LatestStablePackageTests` still pass (or after updating those Facts for a new API family).
2. If the old major must stay covered, copy the previous `*Latest` value into a floor constant (for example `Serilog4`) and keep it in the historical `[Theory]` arrays.
3. If the public API still matches the existing source, the `[Theory]` already covers it.
4. If the API family changed (ZLogger 1 format strings vs ZLogger 2 interpolated handlers / `[ZLoggerMessage]`), add a dedicated test with source that compiles against that family.

NLog 4.x still ships structured templates, but several primitive `Logger.Info(string, int)`-style overloads omit
`MessageTemplateFormatMethodAttribute`. The matrix covers an attributed generic overload on every NLog row, and
characterizes the unattributed primitive overload on 4.x. The latest-stable NLog test uses a primitive `int`
argument because current NLog attributes those overloads.

## Roslyn is not a consumer library

`Microsoft.CodeAnalysis.CSharp` 4.8.0 is the **compile-time API floor** (Visual
Studio 2022 17.8). Bumping it to 5.x would require VS 2026. The analyzer is
forward-compatible with newer compilers; samples building on the .NET 10 SDK
are the host smoke test. Do not put Roslyn into the logging-library matrix.

## Renovate

`Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.CSharp.Workspaces`
are frozen at 4.8.0. `Microsoft.CodeAnalysis.Analyzers` is limited to the 3.x
line (`allowedVersions: "<4"`), and majors of other `Microsoft.CodeAnalysis.*`
packages are disabled. Those rules keep Roslyn 5.x out of the grouped
`dotnet-monorepo` update. Before they landed, Analyzers 5.x (version-aligned
with Roslyn 5 / VS 2026) still grouped with that monorepo and opened a major
PR. 3.x minors of the Analyzers package may still ship.

Major upgrades of logging libraries in the test `.csproj` would change the
default `typeof` host and drop coverage of the previous API. Majors are
disabled there. Latest majors are the `*Latest` constants in
`PackageVersionMatrix.cs`. Minor and patch bumps of the test-project pins stay
in the matrix because those versions are read from the csproj.

Regex-manager updates of `*Latest` include majors. Those major PRs are not
automerge: a new major can change overloads, attributes, or template APIs, and
`LatestStablePackageTests` is the gate.

`samples/` and `test/comparison/` are ignored through `nuget.ignorePaths` as
well as top-level `ignorePaths`. `config:best-practices` includes
`:ignoreModulesAndTests`, which sets a dedicated `nuget.ignorePaths` that does
not merge with top-level `ignorePaths`, so NuGet-only paths must be listed in
the `nuget` block.
