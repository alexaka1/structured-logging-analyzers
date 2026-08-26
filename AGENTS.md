# Repository Guidelines

## Project Structure & Modules
- `src/Alexaka1.Analyzers.StructuredLogging`: `netstandard2.0` diagnostic analyzers (`AASL0001`–`AASL0012`).
- `src/Alexaka1.Analyzers.StructuredLogging.CodeFixes`: Roslyn code fixes for the analyzers that have safe transformations.
- `pack/Alexaka1.Analyzers.StructuredLogging`: Analyzer-only NuGet pack project (`Alexaka1.Analyzers.StructuredLogging`). No `lib/` assets.
- `test/Alexaka1.Analyzers.StructuredLogging.Tests`: xUnit.net v3 tests hosted on .NET 10.
- `test/comparison`: Optional InspectCode comparison against the published ReSharper marketplace plugin.
- `samples/`: Consuming projects targeting `net10.0`, `netstandard2.0`, and SDK-style `net472`.
- `docs/`: Rule pages, compatibility notes, and the NuGet package readme. Root solution: `Alexaka1.Analyzers.StructuredLogging.slnx`.
- `.changeset/`: Pending release notes consumed by Changesets.
- `build/`: Release helper scripts (`version.sh`, `extract-changelog.sh`, `detect-duplicate-release.sh`).

## Build, Test, and Development
- Test (native xUnit.net v3 runner): `dotnet run --project test/Alexaka1.Analyzers.StructuredLogging.Tests/Alexaka1.Analyzers.StructuredLogging.Tests.csproj -c Release --no-launch-profile`
- Pack: `dotnet pack pack/Alexaka1.Analyzers.StructuredLogging/Package.csproj -c Release`
- Samples: `dotnet build samples/Net10Example/Net10Example.csproj -c Release`
- Comparison (optional): `./test/comparison/run-comparison.sh`
- Changeset (user-visible package changes): `pnpm changeset`

Keep analyzer assemblies free of Workspaces references. Do not add runtime or `lib/` assets to the NuGet package.

## Coding Style & Naming
- Follow `.editorconfig`. Treat nullable warnings as actionable.
- C#: file-scoped namespaces; `PascalCase` for types/members; `camelCase` for locals/params.
- Diagnostic IDs stay in the `AASL` prefix. Preserve documented compatibility unless `docs/compatibility.md` is updated.

## Testing Guidelines
- Framework: xUnit.net v3 (`xunit.v3`) with `Microsoft.CodeAnalysis.CSharp.Workspaces` test hosts.
- CI runs the stand-alone test executable via `dotnet run` (native in-process runner). `xunit.runner.visualstudio` remains for IDE Test Explorer / optional VSTest.
- Group analyzer cases under `Parity/`, `Fixes/`, `Frameworks/`, and `SourceGenerated/`.
- Add tests for new diagnostics, invocation shapes, and code fixes.
- Logging-library version coverage lives in `Frameworks/PackageVersionMatrixTests.cs` (floors / current pins) and `Frameworks/LatestStablePackageTests.cs` (`*Latest` constants, including majors). Restored NuGet compile assets are metadata references. Do not add a test project per Serilog/NLog/MEL/ZLogger version. See `docs/package-version-testing.md`.
- After analyzer behavior changes, run `./test/comparison/run-comparison.sh` when InspectCode is available.

## Commit & Pull Requests
- Commits: imperative, concise; squash feature branches to a single commit.
- PRs: describe behavior, test impact, and any compatibility change.
- Requirements: green Continuous Integration.
- Code scanning: `.github/workflows/codeql.yml` is CodeQL advanced setup for `csharp` and `actions`.

## Publishing
- Add a changeset on PRs that change the published package: `pnpm changeset`.
- Versioning: `.github/workflows/version.yml` uses Changesets `select-mode`, `version`, and `publish` actions. Pending changesets open a Version package PR. With none remaining, `changeset git-tag` pushes `v<version>` (single-package tag format).
- Releases: `.github/workflows/publish-nuget.yml` runs on `v*.*.*` tags. It fails if a GitHub release or nuget.org package for that version already exists, then attaches the nupkg to a GitHub release (draft, then publish, for immutable releases) and pushes to nuget.org using [trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) (GitHub OIDC). There is no long-lived NuGet API key.
- nuget.org policy: repository `alexaka1/structured-logging-analyzers`, workflow file `publish-nuget.yml`, environment `nuget`.
- GitHub release tags and the Version package PR use the `release` environment and GitHub App secrets `RELEASE_BOT_CLIENT_ID` / `RELEASE_BOT_PRIVATE_KEY`.
- Set repository variable `NUGET_USER` to the nuget.org profile name (not an email).
