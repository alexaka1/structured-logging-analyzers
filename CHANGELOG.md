# Alexaka1.Analyzers.StructuredLogging

## 0.1.0-preview.3

### Minor Changes

- [#14](https://github.com/alexaka1/structured-logging-analyzers/pull/14) [`2359435`](https://github.com/alexaka1/structured-logging-analyzers/commit/23594350a4ac75d31e6f3332b2cc6be0e733f664) Thanks [@alexaka1](https://github.com/alexaka1)! - Add a `semantic_conventions` property naming style for AASL0009 (template property naming) and AASL0010 (context property naming) (`service.name`, `http.response.status_code`). AASL0012 warns when `[LoggerMessage]` is used with that template naming style.

### Patch Changes

- [#12](https://github.com/alexaka1/structured-logging-analyzers/pull/12) [`ea87e7f`](https://github.com/alexaka1/structured-logging-analyzers/commit/ea87e7fa70abed152a500d051302ce24b6a85be9) Thanks [@alexaka1](https://github.com/alexaka1)! - Namespace `.editorconfig` naming options under `dotnet_code_quality.AASL`, matching the diagnostic ID prefix.

## 0.1.0-preview.2

Initial preview of the Roslyn analyzers and code fixes for structured logging.
