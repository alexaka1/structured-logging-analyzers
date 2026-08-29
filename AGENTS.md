# Structured Logging Analyzers

This repo builds fast Roslyn analyzers and code fixes for structured logging across Microsoft.Extensions.Logging, Serilog, NLog, and ZLogger.

You can think of it as the useful parts of a logging IDE plugin, except they run anywhere Roslyn runs: Visual Studio, Rider, C# Dev Kit, and `dotnet build`.

## What makes this project special?

People install this package into their builds and editors. That gives us a lot of power and almost no room to be careless. There are three things we cannot compromise on.

### 1. Fast enough to disappear

Analyzers run while people type. Most method calls are not logging calls. Reject irrelevant syntax early, resolve expensive state once per compilation, and do not make every keystroke pay for an elegant abstraction.

The performance contract lives in `docs/performance-policy.md`. If your change affects registrations, semantic queries, allocations, caching, or concurrency, read it first.

### 2. Correct across real C#

Logging calls come in annoying shapes: named arguments, reordered arguments, `params`, extension methods, source-generated methods, raw strings, Razor-generated C#, and half-written code in an editor.

The analyzer should understand those shapes without crashing and without guessing. Use semantic parameter binding where argument order can lie. Treat invalid syntax and error symbols as normal input.

### 3. Boring to install

This is an analyzer-only NuGet package. It should add diagnostics and code fixes, then stay out of the consuming app.

The package contains the analyzer and code-fix assemblies under `analyzers/dotnet/cs`. It has no `lib` assets, ships no Roslyn assemblies, and copies no analyzer DLLs to application output.

## A note from the maintainer

I like small systems that make the correct behavior obvious. Do not preserve complexity because it already exists. Do not add machinery because it looks like good architecture.

Understand the actual constraint. Then build the smallest thing that handles it end to end.

Channel both "measure twice, cut once" and YAGNI. A compiler extension is a bad place for speculative abstractions.

These are strong defaults, not a substitute for the task in front of you. If a rule here conflicts with an explicit request, call out the conflict before breaking the rule.

## A small glossary

When communicating, use these terms:

- **you** means the agent reading this file and changing the repository.
- **maintainer** means the developer directing the work and the people responsible for what ships.
- **analyzer** means the `netstandard2.0` assembly that reports `AASL` diagnostics. It must not reference Workspaces.
- **code fix** means the separate Workspaces-based assembly that offers safe source transformations.
- **host** means the Roslyn compiler or IDE process loading the package.
- **consumer** means a project that installs the NuGet package.
- **logging library** means Microsoft.Extensions.Logging, Serilog, NLog, or ZLogger. Roslyn is not a logging library.
- **template** means the structured logging message template being parsed.
- **rule** means one documented diagnostic in the `AASL0001` through `AASL0012` family.
- **compatibility contract** means the behavior documented in `docs/compatibility.md`, including intentional differences from the ReSharper plugin.

## The three ways to hurt yourself

1. **Putting Workspaces in the analyzer.** The analyzer runs inside compiler hosts and must remain independent of Workspaces. Workspaces belongs in `Alexaka1.Analyzers.StructuredLogging.CodeFixes`, nowhere else.
2. **Shipping runtime assets.** A normal project reference or package tweak can quietly add `lib` files, Roslyn dependencies, or analyzer DLLs to consumer output. Pack the real package and inspect the package tests whenever layout could change.
3. **Raising the host floor by accident.** Production code compiles against Roslyn 4.8.0 so it loads in Visual Studio 2022 17.8 and newer hosts. The .NET 10 test host is not permission to use newer Roslyn APIs.

## Hit every surface

The most common bad change here is one that works for the example in front of you and misses the other supported entry points. Before calling analyzer work done, walk this list and say which parts applied:

- **Libraries.** Microsoft.Extensions.Logging, Serilog, NLog, and ZLogger do not expose identical APIs.
- **Logging shapes.** Normal invocations, `[LoggerMessage]`, `LoggerMessage.Define`, `DefineScope`, contextual loggers, and primary constructors each have their own binding rules.
- **Arguments.** Positional, named, reordered, optional, and `params` arguments must map to template holes semantically.
- **Strings.** Regular, verbatim, raw, interpolated, concatenated, escaped, and malformed strings are all real input.
- **Generated code.** User-authored `[LoggerMessage]` declarations and Razor code should be analyzed. Generated implementations and unrelated generated trees should not.
- **Configuration.** Test defaults, prefix-level keys, rule-level keys, invalid values, and invalid regexes.
- **Fixes.** A diagnostic working does not prove its fix is safe. Verify exact spans, rewritten source, trivia, and unsupported shapes.
- **Package.** Analyzer loading, package entries, sample output, host compatibility, and performance are product behavior.
- **Docs.** A changed rule is not finished until its rule page and compatibility contract describe what now ships.

## How it works

One compilation-start action resolves known logging symbols and registers four syntax-node actions. Recognized invocations are bound to logging APIs, constant templates are parsed once, and independent rules inspect the shared parse result.

Configuration is cached per syntax tree. Regexes are cached per compilation. Diagnostics point at the narrowest useful token. Code fixes live in a separate assembly and use shared literal mapping instead of doing ad hoc string surgery.

Generated-code analysis stays enabled because Razor and `[LoggerMessage]` need it. The analyzer filters generated trees itself.

Read `docs/compatibility.md` before changing rule behavior and `docs/ide-compiler-policy.md` before touching Roslyn or target frameworks.

## Where code lives

- `src/Alexaka1.Analyzers.StructuredLogging` - analyzers, template parsing, symbol resolution, configuration, and diagnostics.
- `src/Alexaka1.Analyzers.StructuredLogging.CodeFixes` - safe Roslyn code fixes and source rewriting.
- `test/Alexaka1.Analyzers.StructuredLogging.Tests` - xUnit.net v3 tests running as a .NET 10 executable.
- `test/comparison` - legacy InspectCode comparison against the spiritual ancestor ReSharper plugin.
- `pack/Alexaka1.Analyzers.StructuredLogging` - the analyzer-only NuGet package.
- `samples` - real consumer projects for .NET 10, Blazor, .NET Standard 2.0, and SDK-style .NET Framework 4.7.2.
- `docs/rules` - public documentation for each diagnostic.
- `.changeset` - pending user-facing release notes.

## Formatting

```sh
node --run fmt
```

## Verifying

Start with the smallest proof that the change works. Run the focused test class or method while iterating. Before calling a behavior change complete, run the full test executable:

```sh
dotnet run --project test/Alexaka1.Analyzers.StructuredLogging.Tests/Alexaka1.Analyzers.StructuredLogging.Tests.csproj -c Release --no-launch-profile
```

Changes to registration, caching, concurrency, the hot path, packaging, or analyzer loading also need the package and performance suite:

```sh
dotnet run --project test/Alexaka1.Analyzers.StructuredLogging.Tests/Alexaka1.Analyzers.StructuredLogging.Tests.csproj -c Release --no-launch-profile -- -class Alexaka1.Analyzers.StructuredLogging.Tests.PackageAndPerformanceTests
```

Pack when package layout could move:

```sh
dotnet pack pack/Alexaka1.Analyzers.StructuredLogging/Package.csproj -c Release
```

Build the affected samples when consumer compatibility could move:

```sh
dotnet build samples/Net10Example/Net10Example.csproj -c Release
dotnet build samples/Net10BlazorExample/Net10BlazorExample.csproj -c Release
```

Do not weaken an assertion to make a test pass. Decide whether the implementation or the documented contract is wrong, then fix the right one.

If you could not run a relevant check, say exactly which check you skipped and why.

## Tests

Every behavior change needs a focused test that fails without it.

- `Parity` owns analyzer behavior and characterized compatibility.
- `Fixes` owns code actions and rewritten source.
- `Frameworks` owns logging-library integrations and version coverage.
- `SourceGenerated` owns `[LoggerMessage]`, generator, and Razor behavior.

Do not add one test project per package version. Version coverage belongs in `PackageVersionMatrixTests.cs` and `LatestStablePackageTests.cs`, using restored compile assets as metadata references.

Performance and package assertions are ordinary product tests, not optional benchmarks to ignore when they become inconvenient.

## Plans and work artifacts

- Do not commit implementation plans, research notes, agent transcripts, or scratch files.
- Put durable architecture, constraints, and decisions in the relevant page under `docs`. Update that page when the product changes so the next person finds current facts instead of abandoned intent.
- Keep active task tracking in the issue or pull request that owns the work. Do not preserve a second checklist in the repository.
- A merged pull request is the implementation record. Tests and product docs are the lasting explanation.

## Taste

- Keep the analyzer hot path cheap. Syntax first, semantics only after the call looks relevant.
- Parse once and share the result.
- Prefer the dependency and Roslyn APIs already in the repo over custom infrastructure.
- Prefer a boring private method over a hierarchy, factory, or new configuration knob.
- Code fixes are allowed to be conservative. A missing fix is better than a fix that changes meaning.
- Nullable warnings are bugs until proven otherwise.
- Comments explain Roslyn quirks, invariants, or non-obvious tradeoffs. They do not narrate the next line.
- Delete obsolete paths. Do not leave forwarding wrappers, compatibility branches, or "temporary" duplicate implementations.
- Keep components modular, but do not turn every concept into an interface.
- If a rule here fights the task in front of you, say so loudly and get human sign-off before breaking it.

## Pull requests and releases

- Never create a pull request, commit, tag, or release unless the developer explicitly asks.
- One concern per pull request. If the description says "also", split it.
- Use a concise Conventional Commit title.
- Describe the behavior change, the tests that prove it, and any compatibility impact.
- User-visible package changes need a changeset from `pnpm changeset`.
- Keep changeset text about what users observe, not which classes moved.
- Release automation owns package versions, tags, GitHub releases, and NuGet publishing.

Assume unrelated working-tree changes belong to the developer. Do not rewrite, revert, format, stage, or otherwise "clean up" work outside the task.
