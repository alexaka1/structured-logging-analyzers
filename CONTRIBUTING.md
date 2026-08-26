# Contributing

Please:

- Keep analyzer assemblies free of Workspaces references.
- Do not add runtime or `lib/` assets to the NuGet package.
- Do not copy Apache-2.0 Serilog parser sources from the upstream plugin
  into the analyzer.
- Add tests for new diagnostics, invocation shapes, and code fixes.
- When a logging library ships a new major, Renovate bumps the `*Latest`
  constant in
  `test/Alexaka1.Analyzers.StructuredLogging.Tests/Infrastructure/PackageVersionMatrix.cs`.
  Keep the previous major as a floor if that API must stay covered. Do not
  create another test project. See
  [docs/package-version-testing.md](docs/package-version-testing.md).
- Preserve documented compatibility behavior unless a change is called out
  in `docs/compatibility.md`.
- After analyzer behavior changes, run `./test/comparison/run-comparison.sh`
  (InspectCode plus the published marketplace plugin vs Roslyn).
- Do not reimplement .NET SDK `CA*` or `SYSLIB10xx` diagnostics unless the
  extra behavior is documented in `docs/microsoft-recommendations.md`.
- Add a changeset (`pnpm changeset`) when a pull request changes the
  published `Alexaka1.Analyzers.StructuredLogging` package. CI versioning and GitHub
  releases are driven by those files.
