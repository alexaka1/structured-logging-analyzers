# How Renovate discovers release notes

This document explains how Renovate discovers release notes for the
`Alexaka1.Analyzers.StructuredLogging` NuGet package and why the packed
repository URL identifies the package directory.

## Summary

Renovate can use both changelog files and GitHub Release bodies. Its source code
tries them in this order:

1. Find a matching entry in a recognized changelog file.
2. If no matching changelog entry exists, find a matching GitHub Release.
3. If neither source works, use a compare link when one is available.

The implementation is in Renovate's
[`addReleaseNotes()` function](https://github.com/renovatebot/renovate/blob/98988e1b2a4fc0dc9f165d5709288ad462eb79f2/lib/workers/repository/update/pr/changelog/release-notes.ts).

## 1. Locate the source repository

For NuGet dependencies, Renovate reads the latest stable (or latest) package
nuspec and uses:

- `metadata.repository@url` as `sourceUrl`

This behavior is in Renovate's
[NuGet v3 datasource](https://github.com/renovatebot/renovate/blob/98988e1b2a4fc0dc9f165d5709288ad462eb79f2/lib/modules/datasource/nuget/v3.ts).

The pack project publishes this repository URL:

```text
https://github.com/alexaka1/structured-logging-analyzers/tree/main/pack/Alexaka1.Analyzers.StructuredLogging
```

Renovate's
[`addMetaData()` function](https://github.com/renovatebot/renovate/blob/98988e1b2a4fc0dc9f165d5709288ad462eb79f2/lib/modules/datasource/metadata.ts)
recognizes the GitHub tree URL and normalizes it into:

```text
sourceUrl       = https://github.com/alexaka1/structured-logging-analyzers
sourceDirectory = pack/Alexaka1.Analyzers.StructuredLogging
```

`PackageProjectUrl` stays the repository root so nuget.org still links to the
project homepage.

## 2. Determine which versions need notes

Renovate does not simply request the latest changelog entry. Its
[`getInRangeReleases()` function](https://github.com/renovatebot/renovate/blob/98988e1b2a4fc0dc9f165d5709288ad462eb79f2/lib/workers/repository/update/pr/changelog/releases.ts)
gets the available releases from the dependency datasource and filters them to
the update range:

```text
currentVersion < release.version <= newVersion
```

If an update skips several versions, Renovate can include separate notes for
every compatible intermediate release.

## 3. Find a changelog file

For GitHub repositories, Renovate's
[`getReleaseNotesMd()` function](https://github.com/renovatebot/renovate/blob/98988e1b2a4fc0dc9f165d5709288ad462eb79f2/lib/workers/repository/update/pr/changelog/github/index.ts):

1. Reads the repository's default branch.
2. Requests its Git tree.
3. Uses recursive traversal when `sourceDirectory` is set.
4. Filters the tree for recognized changelog filenames.
5. Fetches the selected file as a Git blob.

The changelog for this package is nested at:

```text
pack/Alexaka1.Analyzers.StructuredLogging/CHANGELOG.md
```

Identifying `pack/Alexaka1.Analyzers.StructuredLogging` as `sourceDirectory`
makes Renovate request the recursive Git tree and scope changelog filename
matching to the package directory. Without that directory, Renovate only
inspects the repository root and would miss this file.

Changesets writes that changelog next to the package, and `build/version.sh`
keeps `Version.props` in sync with the same version headings.

## 4. Extract the matching changelog section

Renovate parses the complete changelog as Markdown. For each release in the
update range, it:

1. Tries heading levels from `#` through `#######`.
2. Divides the document into sections at that heading level.
3. Looks for the release version in each heading.
4. Returns only the body belonging to the matching heading.

For example:

```markdown
## 0.1.0-preview.1

Initial preview of the Roslyn analyzers and code fixes for structured logging.
```

For version `0.1.0-preview.1`, Renovate returns the content after that heading.

The parsing and section selection are implemented by
[`getReleaseNotesMd()`](https://github.com/renovatebot/renovate/blob/98988e1b2a4fc0dc9f165d5709288ad462eb79f2/lib/workers/repository/update/pr/changelog/release-notes.ts).

## 5. Fall back to GitHub Release bodies

If no matching changelog section is found, Renovate fetches the repository's
releases and attempts to match a release using:

1. A package-prefixed tag.
2. Exactly `<version>` or `v<version>`.
3. The dependency's `gitRef` or `v<gitRef>`.
4. A user-configured `extractVersion` regular expression.

The GitHub release list is fetched by Renovate's
[`getReleaseList()` function](https://github.com/renovatebot/renovate/blob/98988e1b2a4fc0dc9f165d5709288ad462eb79f2/lib/workers/repository/update/pr/changelog/github/index.ts),
and matching is performed in
[`getReleaseNotes()`](https://github.com/renovatebot/renovate/blob/98988e1b2a4fc0dc9f165d5709288ad462eb79f2/lib/workers/repository/update/pr/changelog/release-notes.ts).

For this package, Renovate sees:

```text
packageName = Alexaka1.Analyzers.StructuredLogging
version     = 0.1.0-preview.1
```

The corresponding GitHub Release tag is:

```text
Alexaka1.Analyzers.StructuredLogging@0.1.0-preview.1
```

That tag matches Renovate's package-prefixed form
`^(?:packageName|depName|release)[@_-]v?`, so GitHub Releases are a fallback
when a changelog heading is missing.

## Result

The package-directory repository URL lets Renovate display the matching entry
from `pack/Alexaka1.Analyzers.StructuredLogging/CHANGELOG.md` without requiring
custom `changelogUrl` configuration in every consuming repository.

Renovate reads changelog files from the repository default branch, not from the
nupkg. Merging this changelog to `main` is what makes downstream update PRs
see it.

Verify the metadata and parser locally with:

```shell
./scripts/verify-renovate-changelog.sh
```

## References

- [Renovate and changelogs](https://docs.renovatebot.com/key-concepts/changelogs/)
- [Renovate NuGet datasource](https://docs.renovatebot.com/nuget/)
- [Changelog entry point](https://github.com/renovatebot/renovate/blob/98988e1b2a4fc0dc9f165d5709288ad462eb79f2/lib/workers/repository/update/pr/changelog/index.ts)
- [Release-range filtering](https://github.com/renovatebot/renovate/blob/98988e1b2a4fc0dc9f165d5709288ad462eb79f2/lib/workers/repository/update/pr/changelog/releases.ts)
- [Changelog and GitHub Release parsing](https://github.com/renovatebot/renovate/blob/98988e1b2a4fc0dc9f165d5709288ad462eb79f2/lib/workers/repository/update/pr/changelog/release-notes.ts)
- [GitHub changelog and release fetching](https://github.com/renovatebot/renovate/blob/98988e1b2a4fc0dc9f165d5709288ad462eb79f2/lib/workers/repository/update/pr/changelog/github/index.ts)
