# Repository Guidelines

## Project Structure & Modules
- `src/Alexaka1.Analyzers.StructuredLogging`: `netstandard2.0` diagnostic analyzers (`SLA0001`–`SLA0011`).
- `src/Alexaka1.Analyzers.StructuredLogging.CodeFixes`: Roslyn code fixes for the analyzers that have safe transformations.
- `pack/Alexaka1.Analyzers.StructuredLogging`: Analyzer-only NuGet package (`Alexaka1.Analyzers.StructuredLogging`). No `lib/` assets.
- `test/Alexaka1.Analyzers.StructuredLogging.Tests`: xUnit tests hosted on .NET 10.
- `test/comparison`: Optional InspectCode comparison against the published ReSharper marketplace plugin.
- `samples/`: Consuming projects targeting `net10.0`, `netstandard2.0`, and SDK-style `net472`.
- `docs/`: Rule pages, compatibility notes, and the NuGet package readme. Root solution: `Alexaka1.Analyzers.StructuredLogging.slnx`.
- NuGet package ID and root namespace: `Alexaka1.Analyzers.StructuredLogging`. GitHub repository: `alexaka1/structured-logging-analyzers`.

## Build, Test, and Development
- Test: `dotnet test Alexaka1.Analyzers.StructuredLogging.slnx -c Release`
- Pack: `dotnet pack pack/Alexaka1.Analyzers.StructuredLogging/Package.csproj -c Release`
- Samples: `dotnet build samples/Net10Example/Net10Example.csproj -c Release`
- Comparison (optional): `./test/comparison/run-comparison.sh`

Keep analyzer assemblies free of Workspaces references. Do not add runtime or `lib/` assets to the NuGet package.

## Coding Style & Naming
- Follow `.editorconfig`. Treat nullable warnings as actionable.
- C#: file-scoped namespaces; `PascalCase` for types/members; `camelCase` for locals/params.
- Diagnostic IDs stay in the `SLA` prefix. Preserve documented compatibility unless `docs/compatibility.md` is updated.

## Testing Guidelines
- Framework: xUnit with `Microsoft.CodeAnalysis.CSharp.Workspaces` test hosts.
- Group analyzer cases under `Parity/`, `Fixes/`, `Frameworks/`, and `SourceGenerated/`.
- Add tests for new diagnostics, invocation shapes, and code fixes.
- After analyzer behavior changes, run `./test/comparison/run-comparison.sh` when InspectCode is available.

## Commit & Pull Requests
- Commits: imperative, concise; squash feature branches to a single commit.
- PRs: describe behavior, test impact, and any compatibility change.
- Requirements: green Continuous Integration.

## Publishing
- NuGet publishes from `.github/workflows/publish-nuget.yml` using [trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) (GitHub OIDC). There is no long-lived NuGet API key.
- Trigger: push a `v*.*.*` tag, or run **Publish NuGet Package** and supply a version.
- nuget.org policy: package `Alexaka1.Analyzers.StructuredLogging`, repository `alexaka1/structured-logging-analyzers`, workflow file `publish-nuget.yml`, environment `nuget`.
- Set repository variable `NUGET_USER` to the nuget.org profile name (not an email).
