# Provenance

## Behavioral source

This analyzer reproduces inspection behavior originally shipped by Oleg
Shevchenko in the JetBrains ReSharper/Rider extension
[resharper-structured-logging](https://github.com/olsh/resharper-structured-logging).

The reference revision for behavioral analysis is
[`2c05392577cbf5f582dcb3820c22a8da6e9617d5`](https://github.com/olsh/resharper-structured-logging/commit/2c05392577cbf5f582dcb3820c22a8da6e9617d5).
The [compatibility contract](docs/compatibility.md) identifies the reproduced
rules, intentional corrections, Roslyn-specific extensions, and unsupported
JetBrains features.

## Implementation lineage

The analyzer, code fixes, message-template parser, and C# literal span mapper
were implemented in this repository for Roslyn. Their implementation inputs
were:

- The public [message-template specification](https://messagetemplates.org/).
- Public Serilog, NLog, Microsoft.Extensions.Logging, and ZLogger calling
  conventions.
- Behavioral characterization tests derived from upstream golden fixtures and
  InspectCode output.

The upstream plugin contains parser files derived from Serilog under
Apache-2.0, including its
[`MessageTemplateParser`](https://github.com/olsh/resharper-structured-logging/blob/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/ReSharper.Structured.Logging/Serilog/Parsing/MessageTemplateParser.cs).
The message-template parser and literal span mapper in this repository do not
contain source copied or mechanically translated from those files.

Behavioral compatibility does not imply source-code lineage. Product behavior
and intentional differences are documented in
[docs/compatibility.md](docs/compatibility.md).
