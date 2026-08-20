// Copyright (c) 2026 alexaka1

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StructuredLogging.Analyzers;
using Xunit;

namespace StructuredLogging.Tests.Infrastructure;

internal static class AnalyzerTestHost
{
    private static readonly DiagnosticAnalyzer Analyzer = new StructuredLoggingAnalyzer();
    private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

    public static Task VerifyAsync(
        string markedSource,
        string? editorConfig = null,
        LanguageVersion languageVersion = LanguageVersion.Latest,
        IReadOnlyList<(string Path, string Text)>? additionalSources = null)
    {
        var (source, expected) = Markup.Parse(markedSource);
        return VerifyAsync(source, expected, editorConfig, languageVersion, additionalSources);
    }

    public static async Task VerifyAsync(
        string source,
        IReadOnlyList<ExpectedDiagnostic> expected,
        string? editorConfig = null,
        LanguageVersion languageVersion = LanguageVersion.Latest,
        IReadOnlyList<(string Path, string Text)>? additionalSources = null)
    {
        var diagnostics = await GetDiagnosticsAsync(source, editorConfig, languageVersion, additionalSources).ConfigureAwait(false);
        var actual = diagnostics
            .Where(d => d.Id.StartsWith("SLA", StringComparison.Ordinal))
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
        ImmutableArray<MetadataReference>? references = null)
    {
        var (compilation, _, options) = CreateCompilation(source, editorConfig, languageVersion, additionalSources, references);
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create(Analyzer),
            options);
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }

    public static async Task VerifyFixAsync(
        string markedSource,
        string expectedSource,
        string diagnosticId,
        Type codeFixType,
        string? editorConfig = null,
        int codeActionIndex = 0)
    {
        var (source, expected) = Markup.Parse(markedSource);
        var diagnostics = await GetDiagnosticsAsync(source, editorConfig).ConfigureAwait(false);
        var matching = diagnostics.FirstOrDefault(d => d.Id == diagnosticId && expected.Any(e => e.Id == d.Id && e.Span == d.Location.SourceSpan))
                       ?? diagnostics.FirstOrDefault(d => d.Id == diagnosticId);
        Assert.NotNull(matching);

        var document = CreateDocument(source, editorConfig);
        var provider = (CodeFixProvider)Activator.CreateInstance(codeFixType)!;
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            matching!,
            (action, _) => actions.Add(action),
            CancellationToken.None);
        await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        Assert.NotEmpty(actions);

        var action = actions[Math.Min(codeActionIndex, actions.Count - 1)];
        var operations = await action.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        var change = operations.OfType<ApplyChangesOperation>().Single();
        var updated = change.ChangedSolution.GetDocument(document.Id)!;
        var text = await updated.GetTextAsync().ConfigureAwait(false);
        Assert.Equal(Normalize(expectedSource), Normalize(text.ToString()));

        var secondPass = await GetDiagnosticsAsync(text.ToString(), editorConfig).ConfigureAwait(false);
        Assert.DoesNotContain(secondPass, d => d.Id == diagnosticId && d.Location.SourceSpan == matching!.Location.SourceSpan);
    }

    internal static (Compilation Compilation, SyntaxTree Tree, AnalyzerOptions Options) CreateCompilation(
        string source,
        string? editorConfig,
        LanguageVersion languageVersion,
        IReadOnlyList<(string Path, string Text)>? additionalSources = null,
        ImmutableArray<MetadataReference>? references = null)
    {
        var parseOptions = new CSharpParseOptions(languageVersion);
        var tree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "/0/Test.cs");
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

    private static Document CreateDocument(string source, string? editorConfig)
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.Latest))
            .AddMetadataReferences(projectId, References)
            .AddDocument(documentId, "Test.cs", source);

        _ = editorConfig;
        return solution.GetDocument(documentId)!;
    }

    private static ImmutableArray<MetadataReference> CreateReferences()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        foreach (var path in trusted.Split(Path.PathSeparator))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            switch (name)
            {
                case "mscorlib":
                case "netstandard":
                case "System.Private.CoreLib":
                case "System.Runtime":
                case "System.Console":
                case "System.Linq":
                case "System.Linq.Expressions":
                case "System.Collections":
                case "System.Collections.Concurrent":
                case "System.Memory":
                case "System.Threading":
                case "System.Threading.Tasks":
                case "System.Runtime.Extensions":
                case "System.Runtime.InteropServices":
                case "System.ComponentModel":
                case "System.ObjectModel":
                case "System.Text.RegularExpressions":
                case "System.Net.Primitives":
                case "System.Net.Http":
                case "System.Private.Uri":
                case "System.Linq.Queryable":
                    paths.Add(path);
                    break;
            }
        }

        void AddAssembly(Type type)
        {
            if (!string.IsNullOrEmpty(type.Assembly.Location))
            {
                paths.Add(type.Assembly.Location);
            }
        }

        AddAssembly(typeof(object));
        AddAssembly(typeof(Serilog.Log));
        AddAssembly(typeof(Serilog.Context.LogContext));
        AddAssembly(typeof(Serilog.ILogger));
        AddAssembly(typeof(Microsoft.Extensions.Logging.ILogger));
        AddAssembly(typeof(Microsoft.Extensions.Logging.LoggerExtensions));
        AddAssembly(typeof(NLog.LogManager));
        AddAssembly(typeof(ZLogger.ZLoggerExtensions));

        return paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToImmutableArray();
    }

    internal static ImmutableArray<MetadataReference> CreateReferencesWithLoggingAbstractions(string loggingAbstractionsPath)
    {
        var builder = ImmutableArray.CreateBuilder<MetadataReference>();
        foreach (var reference in References)
        {
            var path = reference.Display;
            if (path is not null &&
                path.IndexOf("Microsoft.Extensions.Logging.Abstractions", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            builder.Add(reference);
        }

        builder.Add(MetadataReference.CreateFromFile(loggingAbstractionsPath));
        return builder.ToImmutable();
    }

    internal static IReadOnlyList<(string Version, string Path)> FindLoggingAbstractionsAssemblies()
    {
        var found = new List<(string Version, string Path)>();
        var current = typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location;
        if (string.IsNullOrEmpty(current))
        {
            return found;
        }

        var versionDir = Directory.GetParent(current)?.Parent?.Parent;
        var packageDir = versionDir?.Parent;
        if (packageDir is null || !packageDir.Exists)
        {
            found.Add((versionDir?.Name ?? "current", current));
            return found;
        }

        foreach (var version in packageDir.EnumerateDirectories())
        {
            var dll = version
                .EnumerateFiles("Microsoft.Extensions.Logging.Abstractions.dll", SearchOption.AllDirectories)
                .FirstOrDefault(file => file.FullName.IndexOf($"{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) >= 0);
            if (dll is not null)
            {
                found.Add((version.Name, dll.FullName));
            }
        }

        if (found.Count == 0)
        {
            found.Add(("current", current));
        }

        return found.OrderBy(item => item.Version, StringComparer.OrdinalIgnoreCase).ToList();
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
                    if (id.StartsWith("SLA", StringComparison.Ordinal))
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
