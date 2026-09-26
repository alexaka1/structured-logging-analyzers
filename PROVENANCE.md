# Provenance

## Behavioral source

This analyzer began as a Roslyn reimplementation of the inspections shipped
by Oleg Shevchenko in the JetBrains ReSharper/Rider extension
[resharper-structured-logging](https://github.com/olsh/resharper-structured-logging)
and has since added rules and fixes of its own.

The reference revision for behavioral analysis is
[`2c05392577cbf5f582dcb3820c22a8da6e9617d5`](https://github.com/olsh/resharper-structured-logging/commit/2c05392577cbf5f582dcb3820c22a8da6e9617d5).
The [migration page](docs/migrating-from-resharper-plugin.md) identifies which
diagnostics correspond to plugin inspections and how behavior differs.

## Implementation lineage

The analyzer, code fixes, message-template parser, and C# literal span mapper
were implemented in this repository for Roslyn. Their implementation inputs
were:

- The public [message-template specification](https://messagetemplates.org/).
- Public Serilog, NLog, Microsoft.Extensions.Logging, and ZLogger calling
  conventions.
- Logging snippets embedded in the rule tests were copied from upstream test
  data. InspectCode output was used for behavioral characterization. The
  copied snippets' notice is in
  [THIRD-PARTY-NOTICES.md](https://github.com/alexaka1/structured-logging-analyzers/blob/main/THIRD-PARTY-NOTICES.md).

The upstream plugin contains parser files derived from Serilog under
Apache-2.0, including its
[`MessageTemplateParser`](https://github.com/olsh/resharper-structured-logging/blob/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/ReSharper.Structured.Logging/Serilog/Parsing/MessageTemplateParser.cs).
The message-template parser and literal span mapper in this repository do not
contain source copied or mechanically translated from those files.

Behavioral compatibility does not imply source-code lineage. Product behavior
is documented in [docs/behavior.md](docs/behavior.md). Differences from the
plugin are documented on the
[migration page](docs/migrating-from-resharper-plugin.md).

## Licensing

This repository and the package are MIT licensed, copyright Alex Martossy
(alexaka1). The upstream plugin is MIT licensed, copyright 2019 Oleg Shevchenko.
The upstream notice is retained for the copied test snippets in
[THIRD-PARTY-NOTICES.md](https://github.com/alexaka1/structured-logging-analyzers/blob/main/THIRD-PARTY-NOTICES.md).
No upstream source is shipped in the package.
