# Alexaka1.Analyzers.StructuredLogging

## 0.1.0-preview.11

### Patch Changes

- [#66](https://github.com/alexaka1/structured-logging-analyzers/pull/66) [`8e19b17`](https://github.com/alexaka1/structured-logging-analyzers/commit/8e19b17d2c304d1bd289afe849ccd136c94c1f9f) Thanks [@alexaka1](https://github.com/alexaka1)! - Report AASL0005 when an exception is passed as a template argument even if another exception already occupies the exception slot. The move-before-template fix is not offered in that case.

- [#70](https://github.com/alexaka1/structured-logging-analyzers/pull/70) [`ddf5dc2`](https://github.com/alexaka1/structured-logging-analyzers/commit/ddf5dc2978fafc516106466ecab4052594c85ba5) Thanks [@alexaka1](https://github.com/alexaka1)! - Fix analyzer crashes, incorrect diagnostics, and an unsafe code fix for named
  exception arguments, cross-file constants, half-written calls, unresolved
  context types, explicit params arrays, collection expressions, and constant
  template tails.

- [#70](https://github.com/alexaka1/structured-logging-analyzers/pull/70) [`ddf5dc2`](https://github.com/alexaka1/structured-logging-analyzers/commit/ddf5dc2978fafc516106466ecab4052594c85ba5) Thanks [@alexaka1](https://github.com/alexaka1)! - Keep empty-format holes aligned, preserve digit-to-uppercase naming words, honor valid rule-scoped naming settings, and analyze ZLogger and LoggerMessage template forms that were previously skipped.

- [#70](https://github.com/alexaka1/structured-logging-analyzers/pull/70) [`ddf5dc2`](https://github.com/alexaka1/structured-logging-analyzers/commit/ddf5dc2978fafc516106466ecab4052594c85ba5) Thanks [@alexaka1](https://github.com/alexaka1)! - Speed up `[LoggerMessage]` analysis for private constant templates, keep cross-file diagnostics in the logging method's document, analyze Razor design-time documents, and skip syntax analysis when neither a supported logging library nor a source-declared `MessageTemplateFormatMethodAttribute` is present. Custom attributed wrappers declared in the project remain analyzed without a logging-library reference.

## 0.1.0-preview.10

### Patch Changes

- [#67](https://github.com/alexaka1/structured-logging-analyzers/pull/67) [`d79db0c`](https://github.com/alexaka1/structured-logging-analyzers/commit/d79db0c176ab63c627ee1cc7ed552cbdea997ab9) Thanks [@alexaka1](https://github.com/alexaka1)! - Improve structured-logging diagnostics and code fixes across Microsoft.Extensions.Logging and Serilog calls, including exception placement, named arguments, contextual loggers, constant interpolated templates, and logging scopes. Escaped braces keep their runtime meaning, invalid regex settings no longer interrupt analysis, and source rewrites are withheld when the original text cannot be mapped safely.

## 0.1.0-preview.9

### Patch Changes

- [#61](https://github.com/alexaka1/structured-logging-analyzers/pull/61) [`ab47e6d`](https://github.com/alexaka1/structured-logging-analyzers/commit/ab47e6dc184380f79d6948520c34ee1f4e455c1a) Thanks [@alexaka1](https://github.com/alexaka1)! - Follow the public message-template format and alignment grammar, and map diagnostics and fixes to exact spans in indentation-trimmed multiline raw strings.

## 0.1.0-preview.8

### Patch Changes

- [#49](https://github.com/alexaka1/structured-logging-analyzers/pull/49) [`f61c55f`](https://github.com/alexaka1/structured-logging-analyzers/commit/f61c55fb68dd118aa4640ad278751586147cb888) Thanks [@alexaka1](https://github.com/alexaka1)! - Document the Visual Studio 2022 17.8 / Roslyn 4.8 host floor in the package readme and add dedicated IDE/compiler and performance policy pages.

## 0.1.0-preview.7

### Patch Changes

- [#42](https://github.com/alexaka1/structured-logging-analyzers/pull/42) [`99b8a81`](https://github.com/alexaka1/structured-logging-analyzers/commit/99b8a813cca99b2aa2435acdd545482ed53da264) Thanks [@alexaka1](https://github.com/alexaka1)! - Analyze Razor source-generated C# so AASL diagnostics and code fixes apply to Blazor `.razor` `@code` blocks as well as `.razor.cs` code-behind. `LoggerMessage.g.cs` and other non-Razor-generated files remain skipped.

## 0.1.0-preview.6

### Patch Changes

- [#44](https://github.com/alexaka1/structured-logging-analyzers/pull/44) [`2501179`](https://github.com/alexaka1/structured-logging-analyzers/commit/250117977dac1d5d2ac316cdfd940263bd59339d) Thanks [@alexaka1](https://github.com/alexaka1)! - Use a version placeholder in README and NuGet install snippets so they do not need to be updated on each release.

## 0.1.0-preview.5

### Minor Changes

- [#33](https://github.com/alexaka1/structured-logging-analyzers/pull/33) [`be21792`](https://github.com/alexaka1/structured-logging-analyzers/commit/be2179282ed9b334dd32b358bff5f34b6cbe0119) Thanks [@alexaka1](https://github.com/alexaka1)! - Add a guarded AASL0008 rename for positional template holes on logging invocations, using names derived from argument expressions when an identifier can be derived. Keep AASL0003, AASL0004, AASL0005, AASL0006, and AASL0012 without code fixes.
  
  Argument mapping now expands compiler-synthesized params arrays so MEL-style `LogInformation("{0}", orderId)` can be renamed. Expansion is withheld if any params element cannot be mapped, so later holes are not renamed from the wrong argument. AASL0001/AASL0002 stay Serilog-like and still do not report on MEL, ZLogger, or `LoggerMessage.Define` / `DefineScope` (`@` is not valid there).

## 0.1.0-preview.4

### Patch Changes

- [#27](https://github.com/alexaka1/structured-logging-analyzers/pull/27) [`de137ca`](https://github.com/alexaka1/structured-logging-analyzers/commit/de137ca5ae5ff4925c6f97311583590ca8579ca7) Thanks [@alexaka1](https://github.com/alexaka1)! - Offer a qualified-name alternative for the AASL0007 interpolation conversion (including `?.` member access), share FixAll equivalence keys for rename fixes, and assert the code-fix contract (compiler errors, overload binding, idempotent FixAll).

- [#23](https://github.com/alexaka1/structured-logging-analyzers/pull/23) [`36975b8`](https://github.com/alexaka1/structured-logging-analyzers/commit/36975b86bc5b79c082ba8635faac36582e30862a) Thanks [@alexaka1](https://github.com/alexaka1)! - Recommend enabling the .NET SDK logging `CA*` rules when installing this package, if they are not already on.

## 0.1.0-preview.3

### Minor Changes

- [#14](https://github.com/alexaka1/structured-logging-analyzers/pull/14) [`2359435`](https://github.com/alexaka1/structured-logging-analyzers/commit/23594350a4ac75d31e6f3332b2cc6be0e733f664) Thanks [@alexaka1](https://github.com/alexaka1)! - Add a `semantic_conventions` property naming style for AASL0009 (template property naming) and AASL0010 (context property naming) (`service.name`, `http.response.status_code`). AASL0012 warns when `[LoggerMessage]` is used with that template naming style.

### Patch Changes

- [#12](https://github.com/alexaka1/structured-logging-analyzers/pull/12) [`ea87e7f`](https://github.com/alexaka1/structured-logging-analyzers/commit/ea87e7fa70abed152a500d051302ce24b6a85be9) Thanks [@alexaka1](https://github.com/alexaka1)! - Namespace `.editorconfig` naming options under `dotnet_code_quality.AASL`, matching the diagnostic ID prefix.

## 0.1.0-preview.2

Initial preview of the Roslyn analyzers and code fixes for structured logging.
