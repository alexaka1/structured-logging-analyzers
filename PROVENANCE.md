# Provenance

## Upstream plugin

The inspections implemented here were originally shipped as the JetBrains
ReSharper/Rider extension
[resharper-structured-logging](https://github.com/olsh/resharper-structured-logging)
by Oleg Shevchenko.

Behavioral analysis used commit
`2c05392577cbf5f582dcb3820c22a8da6e9617d5` on
`https://github.com/olsh/resharper-structured-logging`.

## What was ported

The following plugin behaviors were reimplemented against C# inputs:

- All 11 documented inspections (12 inspection identities; constructor and
  `ForContext<T>` share one diagnostic).
- Existing quick fixes that have a Roslyn equivalent:
  - insert `@` destructuring
  - rename a template property
  - rename a `LogContext.PushProperty` name
  - remove a trailing period
- Additional guarded fixes that the original plugin did not ship:
  - `destructureObjects: true` on `LogContext.PushProperty`
  - containing-type rewrite for `ILogger<T>` / `ForContext<T>`
  - move an exception argument before the message template
  - unique names for duplicate template properties on invocations
- Invocation recognition for Serilog, NLog, Microsoft.Extensions.Logging,
  ZLogger, and `MessageTemplateFormatMethodAttribute`.

## What was implemented independently

The message-template parser and C# literal span mapper are original
implementations. They were written from:

- The public [message template](https://messagetemplates.org/) syntax.
- Documented Serilog/NLog/MEL/ZLogger calling conventions.
- Characterization tests derived from upstream golden fixtures.

They are **not** a modernization or mechanical rewrite of the Apache-2.0
Serilog parser files in the upstream plugin.

## Intentional differences

See [docs/compatibility.md](docs/compatibility.md).

## Out of scope

Roslyn cannot reproduce ReSharper suppression comments, ReSharper settings
pages, inspection wiki integration, JetBrains PSI presentation, or live-template
hotspot sessions.
