# Contributing

Please:

- Keep analyzer assemblies free of Workspaces references.
- Do not add runtime or `lib/` assets to the NuGet package.
- Do not copy Apache-2.0 Serilog parser sources from the upstream plugin
  into the analyzer.
- Add tests for new diagnostics, invocation shapes, and code fixes.
- Preserve documented compatibility behavior unless a change is called out
  in `docs/compatibility.md`.
- After analyzer behavior changes, run `./test/comparison/run-comparison.sh`
  (InspectCode plus the published marketplace plugin vs Roslyn).
- Do not reimplement .NET SDK `CA*` or `SYSLIB10xx` diagnostics unless the
  extra behavior is documented in `docs/microsoft-recommendations.md`.
