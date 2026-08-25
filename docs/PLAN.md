# Roslyn Migration Feasibility Report

## 1. Premise

[`resharper-structured-logging`](https://github.com/olsh/resharper-structured-logging) is a ReSharper/Rider extension providing structured-logging inspections and quick fixes for Serilog, NLog, Microsoft.Extensions.Logging, and ZLogger.

The current plugin is coupled to JetBrains SDK and Wave releases. Rider 2026.2 still reports inspections but no longer exposes the plugin’s quick fixes, as documented in [upstream issue #154](https://github.com/olsh/resharper-structured-logging/issues/154). Continuing as a JetBrains plugin would require ongoing ReSharper, Rider, Kotlin, Gradle, and marketplace maintenance.

The intended replacement is an IDE-independent C# Roslyn analyzer package with these requirements:

- Support projects targeting `net10.0`.
- Ideally support `netstandard2.0` and `net472` projects.
- Reproduce all existing inspections.
- Provide automatic fixes where safely possible.
- Test existing behavior, uncovered cases, and fixes.
- Remain performant during live and build analysis.
- Ship as one analyzer-only NuGet package.
- Add no runtime or compile reference to consuming applications.
- Include installation and configuration examples.
- Retain MIT licensing with correct attribution and provenance.

## 2. Executive finding

The migration is technically feasible.

All 11 documented inspections can be implemented as Roslyn `DiagnosticAnalyzer`s. Four existing fix families can be ported with equivalent outcomes. The interpolated-template conversion can be ported only partially because its ReSharper hotspot interaction has no Roslyn equivalent.

Strict plugin-level compatibility is impossible because Roslyn cannot reproduce:

- ReSharper suppression comments.
- ReSharper settings and options pages.
- Inspection [wiki integration](https://github.com/olsh/resharper-structured-logging/blob/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/ReSharper.Structured.Logging/Wiki/StructuredLoggingWikiDataProvider.cs).
- JetBrains PSI diagnostic presentation.
- Interactive live-template hotspot sessions.

Behavioral compatibility is achievable: equivalent diagnostics and safe source transformations for the same C# inputs, with documented host and configuration differences.

## 3. Existing implementation

The current implementation is the JetBrains extension in [olsh/resharper-structured-logging](https://github.com/olsh/resharper-structured-logging) at commit [`2c05392`](https://github.com/olsh/resharper-structured-logging/commit/2c05392577cbf5f582dcb3820c22a8da6e9617d5):

- The [ReSharper project](https://github.com/olsh/resharper-structured-logging/blob/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/ReSharper.Structured.Logging/ReSharper.Structured.Logging.csproj) targets `net472`.
- It references `JetBrains.ReSharper.SDK`.
- [Directory.Build.props](https://github.com/olsh/resharper-structured-logging/blob/2c05392577cbf5f582dcb3820c22a8da6e9617d5/Directory.Build.props) pins JetBrains SDK version `2025.1.0`.
- ReSharper distribution uses a Wave-dependent NuGet package.
- Rider distribution uses the [Kotlin/Gradle plugin shell](https://github.com/olsh/resharper-structured-logging/tree/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/rider).
- Analyzer behavior depends on JetBrains PSI, daemon, settings, and quick-fix APIs.

The repository contains:

- [11 analyzer classes](https://github.com/olsh/resharper-structured-logging/tree/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/ReSharper.Structured.Logging/Analyzer).
- [11 documented rule pages](https://github.com/olsh/resharper-structured-logging/tree/2c05392577cbf5f582dcb3820c22a8da6e9617d5/rules).
- 12 active inspection identities because some analyzers emit multiple inspection types.
- [5 quick-fix classes](https://github.com/olsh/resharper-structured-logging/tree/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/ReSharper.Structured.Logging/QuickFixes).
- No context actions.
- One orphan highlighting type, [`TemplateFormatStringProblem`](https://github.com/olsh/resharper-structured-logging/blob/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/ReSharper.Structured.Logging/Highlighting/TemplateFormatStringNonExistingArgumentWarning.cs#L23), that no repository analyzer emits.

Template-method recognition currently lives in [`TemplateParameterNameAttributeProvider`](https://github.com/olsh/resharper-structured-logging/blob/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/ReSharper.Structured.Logging/Caching/TemplateParameterNameAttributeProvider.cs) and uses:

- `MessageTemplateFormatMethodAttribute`.
- Hard-coded Microsoft `LoggerExtensions` parameter names.
- Hard-coded ZLogger parameter names.
- Hard-coded Serilog [`LogContext.PushProperty`](https://github.com/olsh/resharper-structured-logging/blob/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/ReSharper.Structured.Logging/Extensions/PsiExtensions.cs#L165) recognition.

## 4. Rule portability

| Rule | Roslyn diagnostic | Existing fix | Portability |
|---|---|---|---|
| Anonymous object must be destructured | Supported | Insert `@` | Full behavioral port |
| Complex object must be destructured | Supported | Insert `@` | Near-full; type heuristics require characterization |
| Complex object in log context | Supported | None | Full diagnostic port |
| Contextual logger mismatch | Supported | None | Full current behavior |
| Exception passed as template argument | Supported | None | Near-full; overload-candidate behavior needs tests |
| Duplicate template properties | Supported | None | Full |
| Template is not compile-time constant | Supported | Convert interpolation | Diagnostic full; fix partial |
| Positional properties | Supported | None | Full current behavior |
| Template property naming | Supported | Rename property | Near-full; naming edge cases require characterization |
| Context property naming | Supported | Replace property name | Full |
| Message ends with a period | Supported | Remove period | Full behavioral port |

No diagnostic rule is fundamentally unportable.

### Directly portable fixes

The following fixes can provide equivalent outcomes:

- Add `@` to a template property.
- Rename a template property.
- Rename a `LogContext.PushProperty` property.
- Remove the final period from the last template fragment.

These fixes require a reliable mapping between decoded string content and source text. Raw source offsets are insufficient because C# escaping changes the relationship between logical template positions and source positions.

The mapper must support:

- Regular string literals.
- Verbatim string literals.
- Raw string literals.
- Constant concatenations.
- Escaped braces.
- Escaped quotes and backslashes.
- Unicode escapes.

### Partially portable fix

[`TemplateIsNotCompileTimeConstantFix`](https://github.com/olsh/resharper-structured-logging/blob/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/ReSharper.Structured.Logging/QuickFixes/TemplateIsNotCompileTimeConstantFix.cs) transforms interpolation into a constant template:

```csharp
logger.LogInformation($"Processed {order.Id}");
```

becomes:

```csharp
logger.LogInformation("Processed {OrderId}", order.Id);
```

The transformation is possible, but ReSharper’s interactive name-selection hotspots cannot be reproduced by a standard Roslyn code fix. The Roslyn fix should use deterministic names and may expose alternative names as separate code actions.

The original fix also has known defects:

- [Issue #103](https://github.com/olsh/resharper-structured-logging/issues/103): incorrect selection range.
- [Issue #116](https://github.com/olsh/resharper-structured-logging/issues/116): corruption around single quotes.
- No automated tests cover the fix.

The replacement should be treated as a new, guarded implementation rather than a transliteration.

### Rules without original fixes

No original quick fix exists for:

- Context-object destructuring.
- Contextual logger mismatch.
- Exception placement.
- Duplicate properties.
- Positional properties.

Possible future fixes include:

- Add explicit `destructureObjects: true` or `false`.
- Replace `ForContext<T>` with the containing type.
- Derive positional property names from argument expressions.
- Move an exception to an exception overload.
- Suggest unique names for duplicate properties.

These are enhancements and should not be part of the initial parity milestone. Several are only safe under restricted semantic conditions.

## 5. Compatibility boundary

Behavioral parity requires explicit decisions about legacy implementation details.

### Argument mapping

The current destructuring analyzers associate template properties with arguments by source position after the template argument. Named, reordered, optional, and `params` arguments can disturb that relationship.

The Roslyn implementation can either:

- Preserve source-position behavior for compatibility; or
- Use semantic parameter/argument mapping and document the correction.

### Mixed templates

The existing parser distinguishes all-positional templates from mixed positional/named templates. Positional diagnostics are not consistently emitted for mixed templates.

This behavior must be frozen as compatibility or corrected deliberately.

### Nonconstant templates

Most template-parsing rules skip dynamic templates. Exception analysis does not require constant template text and can still report a diagnostic.

A combined Roslyn pipeline must not return early after the nonconstant-template diagnostic.

### Complex-type classification

The existing implementation uses JetBrains type and collection helpers to decide whether an object’s inherited `ToString()` is adequate. Roslyn can reproduce the intended hierarchy walk, but edge cases involving generic collections, nullable values, anonymous types, type parameters, records, and error types require characterization tests.

### Naming

PascalCase, camelCase, snake_case, and Elastic naming currently depend on JetBrains `StringUtil` in [`PropertyNameProvider`](https://github.com/olsh/resharper-structured-logging/blob/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/ReSharper.Structured.Logging/Utils/PropertyNameProvider.cs). A replacement algorithm must be validated against acronyms, digits, punctuation, Unicode, dots, spaces, and existing test outputs.

### Contextual loggers

The current [constructor analyzer](https://github.com/olsh/resharper-structured-logging/blob/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/ReSharper.Structured.Logging/Analyzer/ContextualLoggerConstructorAnalyzer.cs) does not support primary constructors. Primary-constructor support is tracked in [issue #130](https://github.com/olsh/resharper-structured-logging/issues/130). Supporting them would be an intentional improvement over the plugin.

## 6. Target-framework strategy

Production analyzer assemblies should target `netstandard2.0`.

Analyzer execution depends on the compiler host, not the target framework of the source project. A `netstandard2.0` analyzer can inspect SDK-style projects targeting:

- `net10.0`
- `netstandard2.0`
- `net472`
- Other current .NET target frameworks

Microsoft’s [RS1041 guidance](https://github.com/dotnet/roslyn-analyzers/blob/main/docs/rules/RS1041.md) recommends `netstandard2.0` for compiler extensions so they can load under both .NET and .NET Framework hosts.

The production project should reference the oldest Roslyn API version that supports the implementation. Roslyn 5.0 should not be selected automatically because Microsoft’s [version support table](https://learn.microsoft.com/visualstudio/extensibility/roslyn-version-support) states that it requires Visual Studio 2026. Roslyn 4.8 or an earlier compatible API baseline would provide broader host support while still loading under the .NET 10 compiler.

Support for `net472` should initially mean SDK-style projects using `PackageReference`. Historical `packages.config` installation would require legacy PowerShell installation scripts and should be considered separately.

## 7. Architecture

A single NuGet package should contain separate analyzer and code-fix assemblies.

Microsoft’s [RS1038 guidance](https://github.com/dotnet/roslyn-analyzers/blob/main/docs/rules/RS1038.md) states that compiler analyzers should not reference Workspaces assemblies. Code fixes require Workspaces APIs, so placing both in one assembly would reduce compiler-host reliability.

Recommended structure:

```text
src/
  Alexaka1.Analyzers.StructuredLogging/
    netstandard2.0
    Compiler APIs only

  Alexaka1.Analyzers.StructuredLogging.CodeFixes/
    netstandard2.0
    Workspaces APIs

pack/
  Alexaka1.Analyzers.StructuredLogging/

test/
  Alexaka1.Analyzers.StructuredLogging.Tests/

samples/
  Net10Example/
  NetStandard20Example/
  Net472Example/
```

Package structure:

```text
analyzers/dotnet/cs/Alexaka1.Analyzers.StructuredLogging.dll
analyzers/dotnet/cs/Alexaka1.Analyzers.StructuredLogging.CodeFixes.dll
README.md
license and provenance files
```

The package should have no `lib/` assets.

### Analysis pipeline

```text
Compilation start
  → resolve known framework symbols once
  → register invocation analysis
  → reject unrelated methods
  → identify template argument
  → map semantic arguments
  → extract constant template text when available
  → parse template once
  → run all applicable independent rules
  → report token-specific diagnostics
```

Contextual-constructor analysis can use a dedicated syntax or symbol action.

Shared infrastructure should include:

- Logging invocation classifier.
- Template parameter resolver.
- Message-template parser.
- Literal content/source-span mapper.
- Argument/property mapper.
- Type classifier.
- Naming service.
- Analyzer configuration reader.

The parser should be internal to the analyzer assembly to avoid an additional runtime dependency.

## 8. Performance design

The implementation should:

- Enable concurrent execution.
- Exclude generated code by default.
- Resolve metadata symbols once per compilation.
- Reject unrelated invocations before expensive semantic work.
- Parse each recognized constant template once.
- Keep individual rules independent.
- Respect cancellation tokens.
- Avoid reflection and filesystem access.
- Avoid process-wide mutable caches.
- Avoid repeated LINQ allocations in hot paths.
- Avoid `RegexOptions.Compiled` for small analyzer expressions.
- Apply a timeout to configurable regular expressions.
- Handle invalid configuration without analyzer exceptions.
- Report precise token spans.

Performance validation should include:

- Large compilations dominated by unrelated method calls.
- Large numbers of logging calls.
- Cold and warm analyzer telemetry.
- Concurrent execution.
- Deterministic diagnostic ordering.
- Allocation and execution-time regression thresholds.

## 9. Testing assessment

The current suite contains approximately:

- 38 [analyzer golden cases](https://github.com/olsh/resharper-structured-logging/tree/2c05392577cbf5f582dcb3820c22a8da6e9617d5/test/data/Analyzers).
- 8 [quick-fix golden cases](https://github.com/olsh/resharper-structured-logging/tree/2c05392577cbf5f582dcb3820c22a8da6e9617d5/test/data/QuickFixes).

Coverage is concentrated on Serilog and complex-object destructuring.

Significant gaps include:

- No tests for the nonconstant-template analyzer.
- No tests for interpolation conversion.
- No NLog fixtures.
- No Microsoft.Extensions.Logging template-call fixtures.
- Minimal ZLogger coverage.
- No camelCase or snake_case tests.
- No mixed positional/named tests.
- No named or reordered argument tests.
- No raw-string tests.
- Limited verbatim-string coverage.
- No package-consumption tests.
- No build-time Roslyn tests.
- No performance tests.
- No `FixAll` tests.
- No `net10.0` consumer matrix.

### Required test layers

1. Parser unit tests.
2. Literal content/source-span mapping tests.
3. Invocation-recognition tests.
4. Rule-specific analyzer tests.
5. Code-fix tests.
6. `FixAll` tests.
7. Framework integration tests.
8. Target-framework reference-assembly tests.
9. Packed NuGet tests.
10. Rider and Visual Studio smoke tests.
11. Performance and concurrency tests.

Every code-fix test should verify:

- Exact transformed source.
- No new compiler errors.
- Intended diagnostic disappears.
- Transformation is idempotent.
- Trivia and source style remain valid.
- Overload binding remains valid.

## 10. Packaging and installation

Consumer project:

```xml
<ItemGroup>
  <PackageReference Include="Alexaka1.Analyzers.StructuredLogging"
                    Version="1.0.0"
                    PrivateAssets="all" />
</ItemGroup>
```

.NET 10 CLI:

```shell
dotnet package add Alexaka1.Analyzers.StructuredLogging
```

Severity configuration:

```editorconfig
[*.cs]
dotnet_diagnostic.AASL0001.severity = warning
dotnet_diagnostic.AASL0002.severity = none
```

Naming configuration:

```editorconfig
structured_logging_property_naming = pascal_case
structured_logging_ignored_properties_regex = ^Legacy\.
```

The final package and diagnostic identifiers should be reserved before the first public prerelease.

Package validation must confirm:

- No `lib/` directory.
- Analyzer dependencies are private.
- Analyzer assemblies do not appear in application output.
- Diagnostics run during `dotnet build`.
- Fixes load in supported IDEs.

## 11. Licensing

The existing root [`LICENSE`](https://github.com/olsh/resharper-structured-logging/blob/2c05392577cbf5f582dcb3820c22a8da6e9617d5/LICENSE) already contains the canonical MIT license text. No modernization of the license wording is needed.

The repository is not uniformly MIT. Nine files under [`src/ReSharper.Structured.Logging/Serilog/`](https://github.com/olsh/resharper-structured-logging/tree/2c05392577cbf5f582dcb3820c22a8da6e9617d5/src/ReSharper.Structured.Logging/Serilog) are derived from Serilog and carry Apache-2.0 headers. The current package’s MIT-only declaration does not fully describe those files.

The existing Roslyn prototype modernizes the parser while removing its Apache notices. Cosmetic rewriting does not change the original licensing.

### MIT-only approach

To publish an MIT-only analyzer:

- Implement the message-template parser independently from documented syntax and behavioral tests.
- Do not translate or mechanically modernize the Apache implementation.
- Preserve Oleg Shevchenko’s 2019 MIT notice for substantial retained plugin code.
- Add a separate 2026 notice naming the actual owner of new contributions.
- Do not claim ownership of the 2019 code.
- Use optional `SPDX-License-Identifier: MIT` headers on new files.
- Set `PackageLicenseExpression` to `MIT`.
- Include accurate package authors and repository metadata.
- Add a contribution policy declaring MIT inbound contributions.

### Mixed-license approach

If Apache-derived parser code remains:

- Preserve all Apache copyright and license headers.
- Include the complete Apache-2.0 license.
- Mark modified files.
- Add source version and provenance to `THIRD-PARTY-NOTICES.md`.
- Declare the package’s combined licensing accurately, normally `MIT AND Apache-2.0`.

The MIT-only approach is preferable for the new package.

## 12. Repository location

The production analyzer should live in a new standalone repository.

Reserved identity:

```text
GitHub:            alexaka1/structured-logging-analyzers
NuGet:             Alexaka1.Analyzers.StructuredLogging
Root namespace:    Alexaka1.Analyzers.StructuredLogging
Diagnostic prefix: AASL
```

The repository slug stays kebab-case. The NuGet ID and C# root namespace use the author-prefixed form so they do not collide with [fedarovich/structured-logging-analyzers](https://github.com/fedarovich/structured-logging-analyzers) (`StructuredLogging.Analyzers`).

The product, package, and repository name should avoid “ReSharper” and “Rider”. Those names describe JetBrains products and are not the identity of the Roslyn analyzer.

A new repository provides:

- Independent product identity.
- Clean release history.
- Dedicated issue tracking.
- Independent CI and package metadata.
- No Wave, Gradle, Kotlin, or marketplace infrastructure.
- No unrelated historical binaries.
- Clear separation from the upstream plugin’s vendor identity.

The existing fork should remain as a historical and provenance reference. The new README should identify:

- The upstream project.
- The source commit used for behavioral analysis.
- Which behavior was ported.
- Which components were independently implemented.
- Intentional compatibility differences.
- Non-affiliation with JetBrains and upstream maintainers.

A new repository does not remove attribution or licensing obligations.

## 13. Existing Roslyn prototype

The open [prototype PR #1](https://github.com/alexaka1/resharper-structured-logging/pull/1) is not suitable for publication or merger as the production implementation.

Its current limitations include:

- No code fixes.
- Missing context-object destructuring.
- Changed or collapsed diagnostic identities.
- Invocation-wide instead of token-specific diagnostics.
- Early return after a nonconstant template.
- Missing packaged Core dependency.
- Coarse tests rather than migrated parity fixtures.
- Apache-derived parser with notices removed.
- Incomplete package licensing and repository metadata.

Selected symbol-resolution ideas may be reviewed independently, but the production implementation should begin from a clean repository and architecture.

# Implementation plan

## Phase 1: Repository and legal foundation

- Create the standalone repository.
- Add MIT license and accurate notices.
- Add provenance and contribution documents.
- Reserve the NuGet package ID.
- Reserve a diagnostic prefix.
- Record the upstream source commit.
- Exclude the prototype parser.

Completion criterion: project identity, ownership, licensing, and provenance are accurate.

## Phase 2: Compatibility specification

For each rule, define:

- New diagnostic ID.
- Default severity.
- Message.
- Trigger and exclusions.
- Diagnostic span.
- Existing fix behavior.
- Preserved quirks.
- Intentional corrections.

Completion criterion: all 11 rules have an approved behavioral contract.

## Phase 3: Project and package skeleton

- Create analyzer, code-fix, test, package, and sample projects.
- Target production assemblies at `netstandard2.0`.
- Select the minimum Roslyn API baseline.
- Produce an empty package containing no `lib/` assets.
- Consume the package from all three sample TFMs.

Completion criterion: the package loads correctly and copies nothing into application output.

## Phase 4: Independent parser and span mapper

- Implement the parser from format behavior and tests.
- Support malformed templates and escaped braces.
- Implement logical-text-to-source mapping.
- Cover regular, verbatim, raw, and concatenated literals.

Completion criterion: parser and span behavior match the approved compatibility corpus without Apache-derived implementation.

## Phase 5: Invocation recognition

Implement recognition for:

- Custom attributed methods.
- Serilog.
- NLog.
- Microsoft.Extensions.Logging.
- ZLogger.
- Extension and static calls.
- Named and reordered arguments.
- Optional parameters and `params`.

Completion criterion: every framework and invocation form passes recognition tests.

## Phase 6: Diagnostics

Implement in groups:

1. Template-only rules.
2. Destructuring and exception rules.
3. Contextual logger and naming rules.

Migrate all existing fixtures and add uncovered cases.

Completion criterion: every rule satisfies its diagnostic contract across the framework and TFM matrices.

## Phase 7: Existing fixes

Implement:

1. Add destructuring.
2. Rename template property.
3. Rename context property.
4. Remove trailing period.
5. Guarded interpolated-template conversion.

Add `FixAll` where edits are independent.

Completion criterion: each fix compiles, is idempotent, preserves source, and removes the intended diagnostic.

## Phase 8: Optional new fixes

Evaluate guarded fixes for rules that previously had none. Keep them outside the parity milestone.

Completion criterion: fixes are offered only where semantic validity can be demonstrated.

## Phase 9: Package and performance validation

- Run the complete TFM matrix.
- Run all logging-framework tests.
- Inspect packed NuGet contents.
- Verify build-time diagnostics.
- Verify output isolation.
- Measure analyzer telemetry and allocations.
- Smoke-test Rider and Visual Studio.

Completion criterion: package, compatibility, and performance requirements pass.

## Phase 10: Documentation and prerelease

Publish:

- Installation instructions.
- Central package management example.
- `.editorconfig` reference.
- Rule documentation.
- Old/new diagnostic ID mapping.
- Compatibility differences.
- Supported IDE/compiler policy.
- Performance policy.

Release a prerelease before declaring behavioral parity.

## Release criteria

A stable release requires:

- All 11 rules implemented.
- All original fixes represented or explicitly documented.
- Framework coverage for Serilog, NLog, MEL, and ZLogger.
- Passing `net10.0`, `netstandard2.0`, and SDK-style `net472` samples.
- No analyzer assemblies in consumer output.
- No missing analyzer dependencies.
- No analyzer exceptions on invalid code or configuration.
- Passing concurrency and performance gates.
- Accurate MIT licensing and provenance.


## Source-generated logging support

The scope should include Microsoft.Extensions.Logging source-generated declarations:

```csharp
public static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Processing {OrderId}.")]
    public static partial void ProcessingOrder(
        ILogger logger,
        int orderId);
}
```

This is an extension beyond original-plugin parity. Upstream support remains open in [issue #81](https://github.com/olsh/resharper-structured-logging/issues/81).

### Supported forms

The analyzer should recognize:

- Static partial logging methods.
- Extension logging methods using `this ILogger`.
- Instance methods using an `ILogger` field.
- Instance methods using an `ILogger` primary-constructor parameter, supported by .NET 9 onward.
- Fixed and parameterized log levels.
- `Message` supplied as a named attribute property.
- `Message` supplied through attribute constructor arguments.
- Omitted messages.
- Generic logging methods.
- Related `LoggerMessage.Define` and `LoggerMessage.DefineScope` calls, tracked upstream in [issue #64](https://github.com/olsh/resharper-structured-logging/issues/64).

Analysis must target the attributed method declaration and attribute string, not the generated implementation.

### Parameter mapping

Source-generated logging uses different mapping semantics from ordinary logging calls:

- Placeholder-to-parameter matching is case-insensitive.
- Parameter order does not need to match placeholder order.
- The first `ILogger`, `LogLevel`, and `Exception` parameters are special.
- Additional instances of these types are ordinary template parameters.
- Format specifiers such as `{Value:E}` must be preserved.

The shared argument mapper therefore needs separate strategies:

```text
Invocation mapping:
  property position → invocation argument

LoggerMessage mapping:
  property name → partial-method parameter, case-insensitive
```

### Applicable rules

| Rule | Source-generated declarations |
|---|---|
| Duplicate properties | Apply |
| Positional properties | Apply |
| Property naming | Apply |
| Trailing period | Apply, with the existing fragment policy |
| Complex/anonymous destructuring | Apply only if the MEL template dialect supports a valid equivalent fix |
| Contextual logger | Continue applying to the containing type and injected logger |
| Compile-time constant | Usually redundant because attribute arguments must be constants |
| Exception placement | Defer to generator `SYSLIB` diagnostics; avoid duplicate warnings |
| Context-property rules | Not applicable |

Destructuring rules must not blindly recommend Serilog’s `@` operator for Microsoft source-generated logging. They should be enabled only when the target logging dialect accepts the syntax and provides the intended runtime behavior.

### Avoiding duplicate diagnostics

The Microsoft logging generator already reports `SYSLIB` diagnostics for:

- Invalid partial-method declarations.
- Missing template parameters.
- Parameters without placeholders.
- Incorrect use of the special exception parameter.
- Unsupported parameter shapes.
- Other source-generation constraints.

The analyzer should complement those diagnostics rather than duplicate them. Its source-generated scope should focus on the project’s additional style and structured-logging rules.

### Architecture addition

Add a declaration-analysis path:

```text
Method declaration
  → resolve Microsoft.Extensions.Logging.LoggerMessageAttribute
  → read Message from named or constructor argument
  → obtain constant string and source expression
  → parse template
  → map placeholders to method parameters by name
  → run applicable rules
  → report locations inside the attribute string
```

Generated implementation files remain excluded from analysis.

### Fix support

Safe fixes inside `[LoggerMessage]` strings include:

- Rename template property.
- Remove trailing period.
- Rename positional properties when an unambiguous parameter exists.

A fix must not rewrite a referenced `const string` declaration automatically unless that declaration has been proven to belong exclusively to the logging method.

### Required tests

Add cases for:

- Static partial methods.
- Extension methods.
- Instance logger fields.
- Primary-constructor logger parameters.
- Fixed and dynamic log levels.
- Named and positional attribute arguments.
- Omitted `Message`.
- Reordered parameters.
- Case-insensitive placeholder matching.
- Format specifiers.
- First and subsequent `Exception` parameters.
- Generic methods.
- Invalid methods that already produce `SYSLIB` diagnostics.
- Coexistence without duplicate diagnostics.
- Attribute messages stored in constants.
- Fixes in regular, verbatim, and raw attribute strings.
- .NET 6 through .NET 10 logging abstractions.

### Plan amendment

Insert a source-generated logging phase after invocation recognition:

1. Resolve `LoggerMessageAttribute` by symbol identity.
2. Extract its effective message value.
3. Implement case-insensitive parameter mapping.
4. Model special logger, level, and exception parameters.
5. Add `LoggerMessage.Define` and `DefineScope` recognition.
6. Apply the appropriate subset of rules.
7. Add attribute-string code fixes.
8. Validate against Microsoft’s generator diagnostics.
9. Add source-generation samples to the `net10.0` example.

The release objective becomes behavioral parity with the original plugin plus first-class support for Microsoft source-generated logging.

No single maintained analyzer is a drop-in substitute. A combination can cover much of it, especially for Microsoft.Extensions.Logging.

| Alternative | Coverage | Concern |
|---|---|---|
| [.NET SDK analyzers](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/) | CA2254 constant templates, CA2017 argument count, CA2253 positional placeholders, CA1727 PascalCase, CA2023 malformed templates, CA1848 source-generated logging | MEL only; missing destructuring, sentence style, context properties |
| `LoggerMessage` generator diagnostics | Strong validation of source-generated declarations, parameters, exceptions, duplicates/casing and method shape | Only `[LoggerMessage]`; does not enforce all style rules |
| [Meziantou.Analyzer](https://github.com/meziantou/Meziantou.Analyzer) | MA0180 contextual `ILogger<T>` with fix; MA0124/MA0135/MA0139 property/type governance for MEL and Serilog | Requires configuration for type-governance rules; no general template-style parity |
| [SerilogAnalyzer](https://github.com/Suchiman/SerilogAnalyzer) | Exception placement, syntax, binding, constants, duplicates, PascalCase, anonymous destructuring, `ForContext<T>`; several fixes | Last NuGet release was 0.15 in 2018; modern .NET 10 support should be verified |
| [StructuredLogging.Analyzers](https://github.com/fedarovich/structured-logging-analyzers) | Four basic MEL checks: interpolation, event ID, caught exception, nested interpolation | Only two releases from 2021; very incomplete |
| ZLogger’s bundled generator | Validates `[ZLoggerMessage]` generated methods | Not a general structured-logging policy analyzer |
| Cerbi Governance Analyzer | Cross-framework placeholder governance for MEL, Serilog and NLog | Governance/profile product, not equivalent behavior |

### Coverage against this plugin

| Plugin capability | Existing substitute |
|---|---|
| Constant template | CA2254; Serilog004 |
| Duplicate properties | Serilog005; partial generator diagnostics |
| Positional properties | CA2253 |
| PascalCase properties | CA1727; Serilog006 |
| Configurable camel/snake/Elastic naming | None |
| Anonymous destructuring | Serilog007 |
| Complex-object destructuring | None |
| Context-object destructuring | None |
| Contextual `ILogger<T>` | Meziantou MA0180 |
| Serilog `ForContext<T>` | Serilog008 |
| Exception passed as template property | Serilog001; `[LoggerMessage]` generator diagnostics |
| Context property naming | None |
| Trailing-period rule | None |
| Unified Serilog/NLog/MEL/ZLogger behavior | None |
| NLog-specific broad coverage | No credible complete replacement found |

For a mainly MEL codebase, I would start with:

```editorconfig
[*.cs]
dotnet_diagnostic.CA1727.severity = warning
dotnet_diagnostic.CA1848.severity = suggestion
dotnet_diagnostic.CA2017.severity = warning
dotnet_diagnostic.CA2023.severity = warning
dotnet_diagnostic.CA2253.severity = warning
dotnet_diagnostic.CA2254.severity = warning
dotnet_diagnostic.MA0180.severity = warning
```

and:

```xml
<PackageReference Include="Meziantou.Analyzer"
                  Version="3.*"
                  PrivateAssets="all" />
```

For Serilog, adding `SerilogAnalyzer` provides substantial overlap, but its age makes it a short-term solution rather than a strong foundation.

So:

- MEL plus `[LoggerMessage]`: existing tooling may be sufficient if the missing style rules are unimportant.
- Serilog only: `SerilogAnalyzer` covers much of the plugin, with maintenance risk.
- NLog/ZLogger or all four frameworks: no adequate substitute.
- Full feature parity: the proposed new analyzer still has a clear purpose.
