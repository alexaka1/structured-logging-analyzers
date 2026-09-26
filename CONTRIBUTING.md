# Contributing

This is a personal project maintained at my discretion. It began as a
Roslyn reimplementation of the ReSharper/Rider Structured Logging plugin's
inspections and matched them at a point in time. It does not track the
plugin and may gain rules of its own.

Issues and matching pull requests are welcome. I make no commitment to
review or merge either.

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
- Preserve documented behavior unless the change is called out in
  `docs/behavior.md`.
- Do not reimplement .NET SDK `CA*` or `SYSLIB10xx` diagnostics unless the
  extra behavior is documented in `docs/microsoft-recommendations.md`.
- Add a changeset (`pnpm changeset`) when a pull request changes the
  published `Alexaka1.Analyzers.StructuredLogging` package. CI versioning and GitHub
  releases are driven by those files.
- By submitting a change, you agree it is licensed under the MIT license in
  [LICENSE](LICENSE). Copied third-party material must be listed in
  [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) with its notice.
