using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Text;

using Alexaka1.Analyzers.StructuredLogging.Parsing;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

internal static class AnalyzerTestHost
{
    private static readonly DiagnosticAnalyzer Analyzer = new StructuredLoggingAnalyzer();
    private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

    public static Task VerifyAsync(
        string markedSource,
        string? editorConfig = null,
        LanguageVersion languageVersion = LanguageVersion.Latest,
        IReadOnlyList<(string Path, string Text)>? additionalSources = null,
        ImmutableArray<MetadataReference>? references = null,
        bool requireSuccessfulCompilation = false,
        string? sourcePath = null)
    {
        var (source, expected) = Markup.Parse(markedSource);
        return VerifyAsync(source, expected, editorConfig, languageVersion, additionalSources, references,
            requireSuccessfulCompilation, sourcePath);
    }

    public static async Task VerifyAsync(
        string source,
        IReadOnlyList<ExpectedDiagnostic> expected,
        string? editorConfig = null,
        LanguageVersion languageVersion = LanguageVersion.Latest,
        IReadOnlyList<(string Path, string Text)>? additionalSources = null,
        ImmutableArray<MetadataReference>? references = null,
        bool requireSuccessfulCompilation = false,
        string? sourcePath = null)
    {
        var diagnostics = await GetDiagnosticsAsync(
            source,
            editorConfig,
            languageVersion,
            additionalSources,
            references,
            requireSuccessfulCompilation,
            sourcePath).ConfigureAwait(false);
        var actual = diagnostics
            .Where(d => d.Id.StartsWith(DiagnosticIds.Prefix, StringComparison.Ordinal))
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ThenBy(d => d.Id, StringComparer.Ordinal)
            .ToList();

        var expectedOrdered = expected
            .OrderBy(e => e.Span.Start)
            .ThenBy(e => e.Id, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expectedOrdered.Count, actual.Count);
        for (var i = 0; i < expectedOrdered.Count; i++)
        {
            var exp = expectedOrdered[i];
            var act = actual[i];
            Assert.Equal(exp.Id, act.Id);
            Assert.Equal(exp.Span, act.Location.SourceSpan);
            if (exp.MessageSubstring != null)
            {
                Assert.Contains(exp.MessageSubstring, act.GetMessage(), StringComparison.Ordinal);
            }
        }
    }

    public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        string source,
        string? editorConfig = null,
        LanguageVersion languageVersion = LanguageVersion.Latest,
        IReadOnlyList<(string Path, string Text)>? additionalSources = null,
        ImmutableArray<MetadataReference>? references = null,
        bool requireSuccessfulCompilation = false,
        string? sourcePath = null)
    {
        var (compilation, _, options) = CreateCompilation(source, editorConfig, languageVersion, additionalSources,
            references, sourcePath);
        if (requireSuccessfulCompilation)
        {
            AssertCompilationSucceeded(compilation, "Analyzer test compilation");
        }

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create(Analyzer),
            options);
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }

    public static async Task<ImmutableArray<Diagnostic>> GetWorkspaceDiagnosticsAsync(
        string source,
        string editorConfig,
        string? sourcePath = null,
        IReadOnlyList<(string Path, string Text)>? additionalSources = null)
    {
        var document = CreateDocument(source, editorConfig, sourcePath);
        var solution = document.Project.Solution;
        if (additionalSources is not null)
        {
            foreach (var (path, text) in additionalSources)
            {
                var documentId = DocumentId.CreateNewId(document.Project.Id);
                solution = solution.AddDocument(
                    documentId,
                    Path.GetFileName(path),
                    text,
                    filePath: path);
            }
        }

        var project = solution.GetProject(document.Project.Id);
        Assert.NotNull(project);
        var updatedDocument = project.GetDocument(document.Id);
        Assert.NotNull(updatedDocument);
        return await GetAnalyzerDiagnosticsAsync(updatedDocument).ConfigureAwait(false);
    }

    public static async Task<AnalysisOutcome> AnalyzeAsync(
        string source,
        string? editorConfig = null,
        LanguageVersion languageVersion = LanguageVersion.Latest,
        IReadOnlyList<(string Path, string Text)>? additionalSources = null,
        bool concurrentAnalysis = true,
        bool measureAllocations = false,
        ImmutableArray<MetadataReference>? references = null,
        CancellationToken cancellationToken = default)
    {
        var analyzer = new StructuredLoggingAnalyzer();
        var (compilation, _, options) = CreateCompilation(
            source,
            editorConfig,
            languageVersion,
            additionalSources,
            references);
        var exceptions = new List<Exception>();
        var analysisOptions = new CompilationWithAnalyzersOptions(
            options,
            onAnalyzerException: (exception, _, _) =>
            {
                lock (exceptions)
                {
                    exceptions.Add(exception);
                }
            },
            concurrentAnalysis,
            logAnalyzerExecutionTime: true);
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer),
            analysisOptions);

        long allocatedBefore = 0;
        if (measureAllocations)
        {
            allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        }

        var wallClock = Stopwatch.StartNew();
        var result = await compilationWithAnalyzers.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);
        wallClock.Stop();

        var allocatedBytes = measureAllocations
            ? GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore
            : 0;

        if (exceptions.Count > 0)
        {
            Assert.Fail("Analyzer threw: " + string.Join("; ", exceptions.Select(e => e.ToString())));
        }

        if (!result.AnalyzerTelemetryInfo.TryGetValue(analyzer, out var telemetry))
        {
            telemetry = await compilationWithAnalyzers
                .GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken)
                .ConfigureAwait(false);
        }

        return new AnalysisOutcome(
            result.GetAllDiagnostics(analyzer),
            telemetry,
            wallClock.Elapsed,
            allocatedBytes);
    }

    public static async Task<ControlledAnalysisOutcome> AnalyzeAgainstControlAsync(
        string source,
        DiagnosticAnalyzer controlAnalyzer,
        CancellationToken cancellationToken = default)
    {
        var analyzer = new StructuredLoggingAnalyzer();
        var (compilation, _, options) = CreateCompilation(
            source,
            editorConfig: null,
            LanguageVersion.Latest,
            additionalSources: null,
            references: null,
            sourcePath: null);

        var control = await AnalyzeCompilationAsync(
            compilation,
            options,
            controlAnalyzer,
            cancellationToken).ConfigureAwait(false);
        var outcome = await AnalyzeCompilationAsync(
            compilation,
            options,
            analyzer,
            cancellationToken).ConfigureAwait(false);

        return new ControlledAnalysisOutcome(outcome, control.AllocatedBytes);
    }

    private static async Task<AnalysisOutcome> AnalyzeCompilationAsync(
        Compilation compilation,
        AnalyzerOptions options,
        DiagnosticAnalyzer analyzer,
        CancellationToken cancellationToken)
    {
        var exceptions = new List<Exception>();
        var analysisOptions = new CompilationWithAnalyzersOptions(
            options,
            onAnalyzerException: (exception, _, _) => exceptions.Add(exception),
            concurrentAnalysis: false,
            logAnalyzerExecutionTime: true);
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create(analyzer),
            analysisOptions);

        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var wallClock = Stopwatch.StartNew();
        var result = await compilationWithAnalyzers.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);
        wallClock.Stop();
        var allocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;

        if (exceptions.Count > 0)
        {
            Assert.Fail("Analyzer threw: " + string.Join("; ", exceptions.Select(e => e.ToString())));
        }

        if (!result.AnalyzerTelemetryInfo.TryGetValue(analyzer, out var telemetry))
        {
            telemetry = await compilationWithAnalyzers
                .GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken)
                .ConfigureAwait(false);
        }

        return new AnalysisOutcome(
            result.GetAllDiagnostics(analyzer),
            telemetry,
            wallClock.Elapsed,
            allocatedBytes);
    }

    public static Task VerifyPackageVersionAsync(
        string markedSource,
        string packageId,
        string version,
        string? editorConfig = null)
    {
        var references = NuGetPackageResolver.GetReferences((packageId, version));
        return VerifyAsync(
            markedSource,
            editorConfig,
            references: references,
            requireSuccessfulCompilation: true);
    }

    internal static void AssertCompilationSucceeded(Compilation compilation, string context)
    {
        // Test compilations do not run source generators, so unimplemented
        // [LoggerMessage] / [ZLoggerMessage] partials are expected (CS8795, CS0759).
        var errors = compilation.GetDiagnostics()
            .Where(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.Id is not ("CS8795" or "CS0759"))
            .Select(diagnostic => diagnostic.Id + ": " + diagnostic.GetMessage())
            .ToList();
        Assert.True(errors.Count == 0, context + " failed: " + string.Join("; ", errors));
    }

    private static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(Document document)
    {
        var compilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        Assert.NotNull(compilation);
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create(Analyzer),
            document.Project.AnalyzerOptions);
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }

    public static async Task VerifyFixAsync(
        string markedSource,
        string expectedSource,
        string diagnosticId,
        Type codeFixType,
        string? editorConfig = null,
        int codeActionIndex = 0,
        int? expectedActionCount = null,
        int? remainingCount = null,
        string? sourcePath = null,
        bool assertTemplateArgumentCountMatches = false)
    {
        var (source, expected) = Markup.Parse(markedSource);
        var diagnostics = await GetDiagnosticsAsync(source, editorConfig, sourcePath: sourcePath).ConfigureAwait(false);
        var matching = diagnostics.FirstOrDefault(d =>
            d.Id == diagnosticId && expected.Any(e => e.Id == d.Id && e.Span == d.Location.SourceSpan));
        Assert.NotNull(matching);

        var document = CreateDocument(source, editorConfig, sourcePath);
        var beforeErrors = await GetCompilerErrorKeysAsync(document).ConfigureAwait(false);
        var beforeCount = diagnostics.Count(d => d.Id == diagnosticId);
        var provider = (CodeFixProvider)Activator.CreateInstance(codeFixType)!;
        var updated = await ApplyCodeActionAsync(document, matching, provider, codeActionIndex, expectedActionCount)
            .ConfigureAwait(false);
        var text = await updated.GetTextAsync().ConfigureAwait(false);
        Assert.Equal(Normalize(expectedSource), Normalize(text.ToString()));

        await AssertFixContractAsync(
                updated,
                editorConfig,
                diagnosticId,
                beforeCount,
                remainingCount: remainingCount ?? beforeCount - 1,
                beforeErrors,
                sourcePath,
                assertTemplateArgumentCountMatches)
            .ConfigureAwait(false);
    }

    public static async Task VerifyNoFixAsync(
        string markedSource,
        string diagnosticId,
        Type codeFixType,
        string? editorConfig = null,
        string? sourcePath = null,
        IReadOnlyList<(string Path, string Text)>? additionalSources = null)
    {
        var (source, expected) = Markup.Parse(markedSource);
        var diagnostics = await GetDiagnosticsAsync(
                source,
                editorConfig,
                additionalSources: additionalSources,
                sourcePath: sourcePath)
            .ConfigureAwait(false);
        var matching = diagnostics.FirstOrDefault(d =>
            d.Id == diagnosticId && expected.Any(e => e.Id == d.Id && e.Span == d.Location.SourceSpan));
        Assert.NotNull(matching);

        var document = CreateDocument(source, editorConfig, sourcePath, additionalSources);
        var provider = (CodeFixProvider)Activator.CreateInstance(codeFixType)!;
        // Hosts only invoke a provider for IDs it advertises. Providers that do not
        // filter internally would still register actions if called directly.
        if (!provider.FixableDiagnosticIds.Contains(diagnosticId))
        {
            return;
        }

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            matching,
            (action, _) => actions.Add(action),
            CancellationToken.None);
        await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        Assert.Empty(actions);
    }

    public static async Task VerifyFixAllAsync(
        string markedSource,
        string expectedSource,
        string diagnosticId,
        Type codeFixType,
        string? editorConfig = null,
        int codeActionIndex = 0,
        string? sourcePath = null)
    {
        var (source, _) = Markup.Parse(markedSource);
        var document = CreateDocument(source, editorConfig, sourcePath);
        var diagnostics = await GetAnalyzerDiagnosticsAsync(document).ConfigureAwait(false);
        var matching = diagnostics.Where(d => d.Id == diagnosticId).ToArray();
        Assert.True(matching.Length >= 2, "FixAll tests require at least two diagnostics of the target id.");

        var beforeErrors = await GetCompilerErrorKeysAsync(document).ConfigureAwait(false);
        var provider = (CodeFixProvider)Activator.CreateInstance(codeFixType)!;
        var fixAllProvider = provider.GetFixAllProvider();
        Assert.NotNull(fixAllProvider);

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            matching[0],
            (action, _) => actions.Add(action),
            CancellationToken.None);
        await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        Assert.True(actions.Count > codeActionIndex, $"Expected at least {codeActionIndex + 1} code action(s).");
        var equivalenceKey = actions[codeActionIndex].EquivalenceKey;

        var fixAllContext = new FixAllContext(
            document,
            provider,
            FixAllScope.Document,
            equivalenceKey,
            provider.FixableDiagnosticIds,
            new TestDiagnosticProvider(diagnostics),
            CancellationToken.None);
        var fixAllAction = await fixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(false);
        Assert.NotNull(fixAllAction);

        var operations = await fixAllAction.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        var change = operations.OfType<ApplyChangesOperation>().Single();
        var updated = change.ChangedSolution.GetDocument(document.Id)!;
        var text = await updated.GetTextAsync().ConfigureAwait(false);
        Assert.Equal(Normalize(expectedSource), Normalize(text.ToString()));

        await AssertFixContractAsync(
                updated,
                editorConfig,
                diagnosticId,
                matching.Length,
                remainingCount: 0,
                beforeErrors,
                sourcePath)
            .ConfigureAwait(false);

        var remainingDiagnostics = await GetAnalyzerDiagnosticsAsync(updated).ConfigureAwait(false);
        Assert.DoesNotContain(remainingDiagnostics, d => d.Id == diagnosticId);
        var secondFixAll = await fixAllProvider.GetFixAsync(
            new FixAllContext(
                updated,
                provider,
                FixAllScope.Document,
                equivalenceKey,
                provider.FixableDiagnosticIds,
                new TestDiagnosticProvider(remainingDiagnostics),
                CancellationToken.None)).ConfigureAwait(false);
        if (secondFixAll is not null)
        {
            var secondOperations = await secondFixAll.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
            var secondChange = secondOperations.OfType<ApplyChangesOperation>().SingleOrDefault();
            if (secondChange is not null)
            {
                var secondDocument = secondChange.ChangedSolution.GetDocument(document.Id)!;
                var secondText = await secondDocument.GetTextAsync().ConfigureAwait(false);
                Assert.Equal(Normalize(expectedSource), Normalize(secondText.ToString()));
            }
        }
    }

    private static async Task<Document> ApplyCodeActionAsync(
        Document document,
        Diagnostic diagnostic,
        CodeFixProvider provider,
        int codeActionIndex,
        int? expectedActionCount)
    {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            CancellationToken.None);
        await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        Assert.NotEmpty(actions);
        if (expectedActionCount is { } count)
        {
            Assert.Equal(count, actions.Count);
        }

        Assert.True(actions.Count > codeActionIndex, $"Expected a code action at index {codeActionIndex}.");
        var action = actions[codeActionIndex];
        var operations = await action.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        var change = operations.OfType<ApplyChangesOperation>().Single();
        return change.ChangedSolution.GetDocument(document.Id)!;
    }

    private static async Task AssertFixContractAsync(
        Document updated,
        string? editorConfig,
        string diagnosticId,
        int beforeCount,
        int remainingCount,
        IReadOnlyCollection<string> beforeErrors,
        string? sourcePath = null,
        bool assertTemplateArgumentCountMatches = false)
    {
        var text = await updated.GetTextAsync().ConfigureAwait(false);
        var secondPass = await GetDiagnosticsAsync(text.ToString(), editorConfig, sourcePath: sourcePath)
            .ConfigureAwait(false);
        var remaining = secondPass.Count(d => d.Id == diagnosticId);
        Assert.Equal(remainingCount, remaining);
        Assert.True(remaining < beforeCount);

        var afterErrors = await GetCompilerErrorKeysAsync(updated).ConfigureAwait(false);
        var newErrors = afterErrors.Where(error => !beforeErrors.Contains(error)).ToList();
        Assert.True(newErrors.Count == 0, "Fix introduced compiler errors: " + string.Join("; ", newErrors));
        AssertLoggingInvocationsBind(await updated.GetSemanticModelAsync().ConfigureAwait(false));
        if (assertTemplateArgumentCountMatches)
        {
            AssertTemplateArgumentCountsMatch(await updated.GetSyntaxRootAsync().ConfigureAwait(false));
        }
    }

    private static void AssertTemplateArgumentCountsMatch(SyntaxNode? root)
    {
        Assert.NotNull(root);
        var checkedInvocations = 0;
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var arguments = invocation.ArgumentList.Arguments;
            for (var templateIndex = 0; templateIndex < arguments.Count; templateIndex++)
            {
                if (arguments[templateIndex].Expression is not LiteralExpressionSyntax literal ||
                    !literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    continue;
                }

                var holeCount = MessageTemplateParser.Parse(literal.Token.ValueText).Properties.Length;
                Assert.Equal(holeCount, arguments.Count - templateIndex - 1);
                checkedInvocations++;
                break;
            }
        }

        Assert.True(checkedInvocations > 0, "Expected a rewritten logging template to check.");
    }

    private static async Task<HashSet<string>> GetCompilerErrorKeysAsync(Document document)
    {
        var compilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        Assert.NotNull(compilation);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var diagnostic in compilation.GetDiagnostics())
        {
            if (diagnostic.Severity != DiagnosticSeverity.Error)
            {
                continue;
            }

            if (diagnostic.Id is "CS8795" or "CS0759")
            {
                continue;
            }

            keys.Add(diagnostic.Id + ":" + diagnostic.GetMessage());
        }

        return keys;
    }

    private static void AssertLoggingInvocationsBind(SemanticModel? model)
    {
        Assert.NotNull(model);
        foreach (var invocation in model.SyntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var name = invocation.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => null
            };
            if (name is null || !IsLoggingMethodName(name))
            {
                continue;
            }

            var symbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            Assert.NotNull(symbol);
            Assert.True(symbol.MethodKind is MethodKind.Ordinary or MethodKind.ReducedExtension);
        }
    }

    private static bool IsLoggingMethodName(string name)
    {
        return name is "Information" or "Debug" or "Warning" or "Error" or "Fatal" or "Verbose" or "Write" or
            "Log" or "LogDebug" or "LogInformation" or "LogWarning" or "LogError" or "LogCritical" or "LogTrace" or
            "Info" or "Trace" or "PushProperty" or "ZLogInformation" or "ZLogDebug" or "ZLogError" or "ZLogWarning";
    }

    private sealed class TestDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        private readonly ImmutableArray<Diagnostic> _diagnostics;

        public TestDiagnosticProvider(ImmutableArray<Diagnostic> diagnostics)
        {
            _diagnostics = diagnostics;
        }

        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            _ = document;
            return Task.FromResult<IEnumerable<Diagnostic>>(_diagnostics);
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(
            Project project,
            CancellationToken cancellationToken)
        {
            _ = project;
            return Task.FromResult(Enumerable.Empty<Diagnostic>());
        }

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(
            Project project,
            CancellationToken cancellationToken)
        {
            _ = project;
            return Task.FromResult<IEnumerable<Diagnostic>>(_diagnostics);
        }
    }

    internal static (Compilation Compilation, SyntaxTree Tree, AnalyzerOptions Options) CreateCompilation(
        string source,
        string? editorConfig,
        LanguageVersion languageVersion,
        IReadOnlyList<(string Path, string Text)>? additionalSources = null,
        ImmutableArray<MetadataReference>? references = null,
        string? sourcePath = null)
    {
        var parseOptions = new CSharpParseOptions(languageVersion);
        var tree = CSharpSyntaxTree.ParseText(source, parseOptions, path: sourcePath ?? "/0/Test.cs");
        var trees = new List<SyntaxTree> { tree };
        if (additionalSources is not null)
        {
            foreach (var (path, text) in additionalSources)
            {
                trees.Add(CSharpSyntaxTree.ParseText(text, parseOptions, path: path));
            }
        }

        var compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            trees,
            references ?? References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        AnalyzerOptions options;
        if (string.IsNullOrEmpty(editorConfig))
        {
            options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
        }
        else
        {
            var additional = new EditorConfigText("/.editorconfig", editorConfig);
            options = new AnalyzerOptions(
                ImmutableArray.Create<AdditionalText>(additional),
                new EditorConfigOptionsProvider(tree, editorConfig));
        }

        return (compilation, tree, options);
    }

    private static Document CreateDocument(
        string source,
        string? editorConfig,
        string? sourcePath = null,
        IReadOnlyList<(string Path, string Text)>? additionalSources = null)
    {
        var path = sourcePath ?? "/0/Test.cs";
        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(name))
        {
            name = "Test.cs";
        }

        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.Latest))
            .AddMetadataReferences(projectId, References)
            .AddDocument(documentId, name, source, filePath: path);

        if (additionalSources is not null)
        {
            foreach (var (additionalPath, text) in additionalSources)
            {
                solution = solution.AddDocument(
                    DocumentId.CreateNewId(projectId),
                    Path.GetFileName(additionalPath),
                    text,
                    filePath: additionalPath);
            }
        }

        if (!string.IsNullOrEmpty(editorConfig))
        {
            var configId = DocumentId.CreateNewId(projectId);
            var configText = editorConfig.IndexOf('[') >= 0
                ? editorConfig
                : "[*.cs]" + Environment.NewLine + editorConfig;
            solution = solution.AddAnalyzerConfigDocument(
                configId,
                ".editorconfig",
                SourceText.From(configText, Encoding.UTF8),
                filePath: "/0/.editorconfig");
        }

        return solution.GetDocument(documentId)!;
    }

    private static ImmutableArray<MetadataReference> CreateReferences()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        NuGetPackageResolver.AddTrustedPlatformAssemblies(paths);
        NuGetPackageResolver.AddAssembly(paths, typeof(object));
        NuGetPackageResolver.AddAssembly(paths, typeof(Serilog.Log));
        NuGetPackageResolver.AddAssembly(paths, typeof(Serilog.Context.LogContext));
        NuGetPackageResolver.AddAssembly(paths, typeof(Serilog.ILogger));
        NuGetPackageResolver.AddAssembly(paths, typeof(Microsoft.Extensions.Logging.ILogger));
        NuGetPackageResolver.AddAssembly(paths, typeof(Microsoft.Extensions.Logging.LoggerExtensions));
        NuGetPackageResolver.AddAssembly(paths, typeof(NLog.LogManager));
        NuGetPackageResolver.AddAssembly(paths, typeof(ZLogger.ZLoggerExtensions));

        return paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToImmutableArray();
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n");

    private sealed class EditorConfigText : AdditionalText
    {
        private readonly SourceText _text;

        public EditorConfigText(string path, string content)
        {
            Path = path;
            _text = SourceText.From(content, Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }

    private sealed class EditorConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _options;

        public EditorConfigOptionsProvider(SyntaxTree tree, string editorConfig)
        {
            _ = tree;
            _options = new DictionaryAnalyzerConfigOptions(Parse(editorConfig));
        }

        public override AnalyzerConfigOptions GlobalOptions => _options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;

        private static Dictionary<string, string> Parse(string editorConfig)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rawLine in editorConfig.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line[0] is '#' or ';' or '[')
                {
                    continue;
                }

                var eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }

            return values;
        }
    }

    private sealed class DictionaryAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _values;

        public DictionaryAnalyzerConfigOptions(Dictionary<string, string> values)
        {
            _values = values;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _values.TryGetValue(key, out value!);
        }
    }
}

internal readonly record struct AnalysisOutcome(
    ImmutableArray<Diagnostic> Diagnostics,
    AnalyzerTelemetryInfo Telemetry,
    TimeSpan WallClock,
    long AllocatedBytes);

internal readonly record struct ControlledAnalysisOutcome(
    AnalysisOutcome Analyzer,
    long ControlAllocatedBytes)
{
    public long AllocationDeltaBytes => Analyzer.AllocatedBytes - ControlAllocatedBytes;
}

internal readonly struct ExpectedDiagnostic
{
    public ExpectedDiagnostic(string id, TextSpan span, string? messageSubstring = null)
    {
        Id = id;
        Span = span;
        MessageSubstring = messageSubstring;
    }

    public string Id { get; }

    public TextSpan Span { get; }

    public string? MessageSubstring { get; }
}

internal static class Markup
{
    public static (string Source, List<ExpectedDiagnostic> Expected) Parse(string marked)
    {
        var expected = new List<ExpectedDiagnostic>();
        var source = new StringBuilder(marked.Length);
        var i = 0;
        while (i < marked.Length)
        {
            if (i + 6 < marked.Length && marked[i] == '{' && marked[i + 1] == '|')
            {
                var idStart = i + 2;
                var colon = marked.IndexOf(':', idStart);
                if (colon > idStart)
                {
                    var id = marked.Substring(idStart, colon - idStart);
                    if (id.StartsWith(DiagnosticIds.Prefix, StringComparison.Ordinal))
                    {
                        var contentStart = colon + 1;
                        var close = marked.IndexOf("|}", contentStart, StringComparison.Ordinal);
                        if (close >= 0)
                        {
                            var content = marked.Substring(contentStart, close - contentStart);
                            var start = source.Length;
                            source.Append(content);
                            expected.Add(new ExpectedDiagnostic(id, new TextSpan(start, content.Length)));
                            i = close + 2;
                            continue;
                        }
                    }
                }
            }

            source.Append(marked[i]);
            i++;
        }

        return (source.ToString(), expected);
    }
}
