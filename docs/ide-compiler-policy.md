# Supported IDE and compiler policy

This is the host contract for `Alexaka1.Analyzers.StructuredLogging`. Logging-library
version coverage lives in [package-version-testing.md](package-version-testing.md).
Analyzer allocation, telemetry, and concurrency gates live in
[performance-policy.md](performance-policy.md).

## Compile-time API floor

Production packages pin:

| Package | Role | Pin |
|---|---|---|
| `Microsoft.CodeAnalysis.CSharp` | Analyzer compile-time API | `4.8.0` |
| `Microsoft.CodeAnalysis.CSharp.Workspaces` | Code-fix compile-time API | `4.8.0` |
| `Microsoft.CodeAnalysis.Analyzers` | RS* rules while building this repo | `3.x` (`<4`) |

Microsoft’s [Roslyn version support table](https://learn.microsoft.com/visualstudio/extensibility/roslyn-version-support)
maps `Microsoft.CodeAnalysis` **4.8.0** to **Visual Studio 2022 17.8**, and
**5.0.0** to **Visual Studio 2026 18.0**.

The analyzer and code-fix projects target `netstandard2.0` and compile with
`LangVersion` 12.0 (C# 12). That language version shipped with Roslyn 4.8 /
VS 2022 17.8, which is also the syntax floor for class and struct primary
constructors that `AASL0004` inspects.

Do not bump the compile-time API to 5.x unless Visual Studio 2022 17.8 is
intentionally dropped. The analyzer is **forward-compatible** with newer
compiler hosts: a 4.8-compiled analyzer loads under the .NET 10 SDK compiler
and under Visual Studio 2026.

## Why this floor

- Broader IDE support than Roslyn 5.x (VS 2026-only).
- C# 12 syntax used by the analyzer (primary constructors, raw string
  literals in consumer code).
- [RS1041](https://github.com/dotnet/roslyn-analyzers/blob/main/docs/rules/RS1041.md):
  compiler extensions target `netstandard2.0` so they load under both .NET
  and .NET Framework hosts.
- [RS1038](https://github.com/dotnet/roslyn-analyzers/blob/main/docs/rules/RS1038.md):
  the analyzer assembly must not reference Workspaces. Code fixes live in a
  second assembly that does.

Roslyn is **not** a consumer logging library. Do not add it to the
Serilog / NLog / MEL / ZLogger version matrix.

## Supported hosts

The package is a NuGet analyzer. It runs wherever the C# compiler loads
`analyzers/dotnet/cs` from the package, provided that host’s Roslyn is 4.8
or newer.

| Host | Floor | Notes |
|---|---|---|
| Visual Studio 2022 | 17.8 | Matches the 4.8 API pin. |
| Visual Studio 2026 | 18.0 | Newer Roslyn; analyzer stays on 4.8 APIs. |
| `dotnet build` / `dotnet format` | SDK compiler that loads Roslyn 4.8+ | CI and samples use the .NET 10 SDK. |
| Rider, C# Dev Kit, other Roslyn IDEs | Host Roslyn 4.8 or newer | Load this package as a NuGet analyzer, not the JetBrains plugin. |

Consumer **project** TFM is independent of the analyzer TFM. Samples cover
`net10.0` (console and Blazor), `netstandard2.0`, and SDK-style `net472`.
Support for `net472` means `PackageReference`. `packages.config` install
scripts are out of scope.

Blazor `.razor` `@code` and `.razor.cs` code-behind are analyzed. See
[compatibility.md](compatibility.md#razor--blazor).

## Package layout (host loading)

```text
analyzers/dotnet/cs/Alexaka1.Analyzers.StructuredLogging.dll
analyzers/dotnet/cs/Alexaka1.Analyzers.StructuredLogging.CodeFixes.dll
```

No `lib/` assets. Analyzer dependencies stay `PrivateAssets="all"`. The
packed nupkg must not contain `Microsoft.CodeAnalysis.dll`. Sample builds
must not copy the analyzer assemblies into application output. Those
invariants are asserted in `PackageAndPerformanceTests`.

## Renovate

`renovate.json5` keeps the floor in place:

- `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.CSharp.Workspaces`
  are frozen at 4.8.0.
- `Microsoft.CodeAnalysis.Analyzers` is limited to `allowedVersions: "<4"`.
  5.x is version-aligned with Roslyn 5 / VS 2026 and would otherwise join
  the grouped `dotnet-monorepo` update.
- Other `Microsoft.CodeAnalysis.*` major updates are disabled.

3.x minors of the Analyzers package may still ship. Changing the floor is a
documented policy change in this file, not an automerge.

## Changing the floor

1. Update this page and the pins in `Analyzer.csproj` / `CodeFixes.csproj`.
2. Update the Renovate rules in `renovate.json5`.
3. Retune [performance gates](performance-policy.md) after the SDK or Roslyn
   upgrade.
4. Rebuild samples on the new host SDK.

The .NET 10 SDK samples are the **host** smoke test for a newer compiler,
not a reason to raise the compile-time API pin.
