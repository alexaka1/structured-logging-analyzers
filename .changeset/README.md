# Changesets

This folder is used by `@changesets/cli` to version `pack/Alexaka1.Analyzers.StructuredLogging` and generate its `CHANGELOG.md`.

Add a changeset when a pull request changes the published `Alexaka1.Analyzers.StructuredLogging` package:

```shell
pnpm changeset
```

See the [Changesets documentation](https://github.com/changesets/changesets) for details.

The packed `RepositoryUrl` points at this directory so Renovate can find
`CHANGELOG.md`. See [docs/renovate-release-notes.md](../docs/renovate-release-notes.md).
