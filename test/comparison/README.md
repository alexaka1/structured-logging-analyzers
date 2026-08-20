# ReSharper CLI vs Roslyn comparison

This folder runs the published ReSharper plugin through InspectCode and
compares its findings with `Alexaka1.Analyzers.StructuredLogging` on the same
characterization corpus.

The published plugin is downloaded from the JetBrains Marketplace. This
repository does not contain the plugin source. See
[resharper-structured-logging](https://github.com/olsh/resharper-structured-logging).

## What is compared

- **InspectCode** loads `ReSharper.Structured.Logging` from
  `test/comparison/plugins` (downloaded from the JetBrains Marketplace;
  latest published plugin is still Wave 251 / ReSharper 2025.1).
- **Roslyn** runs `SLA0001`–`SLA0011` on `test/comparison/corpus`.

Keys are `file.cs:diagnostic-id`. Spans are not required to match:
SLA0011 highlights the trailing period, while the plugin highlights the
whole string literal.

## Run

```bash
./test/comparison/run-comparison.sh
```

The script:

1. Downloads plugin nupkg `2025.1.0.373` if missing.
2. Installs InspectCode **2025.3.5** (latest 2025.x). The Wave 251 plugin
   loaded on 2025.3.5 in the original comparison environment; if it does
   not load, the script falls back to InspectCode **2025.1.9**.
3. Builds the corpus.
4. Writes `test/comparison/reports/comparison.md`.
