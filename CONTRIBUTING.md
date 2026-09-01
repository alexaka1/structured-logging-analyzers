# Contributing

This repository is in a finished state for its intended scope. The
structured-logging inspections from the ReSharper/Rider plugin are
available as Roslyn analyzers, with additional coverage and fixes
beyond that parity. I do not plan on making new features.

If you need something, please open an issue and a matching pull request.
I may look at them, but I make no commitment to review or merge either.

## If you send a change

Please:

- Keep analyzer assemblies free of Workspaces references.
- Do not add runtime or `lib/` assets to the NuGet package.
- Keep the Roslyn compile-time API at 4.8.0 unless
  [docs/ide-compiler-policy.md](docs/ide-compiler-policy.md) is updated.
- Keep allocation, telemetry, and concurrency assertions aligned with
  [docs/performance-policy.md](docs/performance-policy.md).
- Add tests for new diagnostics, invocation shapes, and code fixes.
- When a logging library ships a new major, Renovate bumps the `*Latest`
  constant in
  `test/Alexaka1.Analyzers.StructuredLogging.Tests/Infrastructure/PackageVersionMatrix.cs`.
  Keep the previous major as a floor if that API must stay covered. Do not
  create another test project. See
  [PackageVersionMatrixTests.cs](test/Alexaka1.Analyzers.StructuredLogging.Tests/Frameworks/PackageVersionMatrixTests.cs)
  and [LatestStablePackageTests.cs](test/Alexaka1.Analyzers.StructuredLogging.Tests/Frameworks/LatestStablePackageTests.cs).
- Preserve documented compatibility behavior unless a change is called out
  in `docs/compatibility.md`.
- Do not reimplement .NET SDK `CA*` or `SYSLIB10xx` diagnostics unless the
  extra behavior is documented in `docs/microsoft-recommendations.md`.
- Add a changeset (`pnpm changeset`) when a pull request changes the
  published `Alexaka1.Analyzers.StructuredLogging` package. CI versioning and GitHub
  releases are driven by those files.
