// Copyright (c) 2026 alexaka1

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Alexaka1.Analyzers.StructuredLogging;
using Alexaka1.Analyzers.StructuredLogging.Comparison;

var repoRoot = FindRepoRoot();
var corpusDir = Path.Combine(repoRoot, "test", "comparison", "corpus");
var reportsDir = Path.Combine(repoRoot, "test", "comparison", "reports");
Directory.CreateDirectory(reportsDir);

string? inspectXml = null;
for (var i = 0; i < args.Length; i++)
{
    if (args[i] is "--inspectcode" && i + 1 < args.Length)
    {
        inspectXml = args[++i];
    }
}

var roslyn = await CollectRoslynAsync(corpusDir).ConfigureAwait(false);
var inspect = inspectXml is not null && File.Exists(inspectXml)
    ? InspectCodeParser.Parse(inspectXml)
    : Array.Empty<Finding>();

var pluginLoaded = inspectXml is not null && File.Exists(inspectXml) && InspectCodeParser.PluginLoaded(inspectXml);

var inspectVsRoslyn = Compare("inspectcode", inspect, "roslyn", roslyn);
var messageMismatches = MessageMismatches(inspect, roslyn);

var json = new
{
    generatedUtc = DateTime.UtcNow.ToString("o"),
    inspectCode = "2025.3.5",
    plugin = "ReSharper.Structured.Logging 2025.1.0.373",
    inspectcodePluginLoaded = pluginLoaded,
    inspectcodeReport = inspectXml is null ? null : Path.GetRelativePath(repoRoot, inspectXml).Replace('\\', '/'),
    inspectcodeCount = inspect.Count,
    roslynCount = roslyn.Count,
    inspectcodeVsRoslyn = inspectVsRoslyn,
    messageMismatches,
    roslyn = roslyn.Select(Serialize),
    inspectcode = inspect.Select(Serialize),
};

var jsonPath = Path.Combine(reportsDir, "comparison.json");
var mdPath = Path.Combine(reportsDir, "comparison.md");
File.WriteAllText(
    jsonPath,
    JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }));
File.WriteAllText(mdPath, RenderMarkdown(repoRoot, pluginLoaded, inspectXml, inspectVsRoslyn, messageMismatches, roslyn, inspect));

Console.WriteLine(File.ReadAllText(mdPath));
Console.WriteLine($"Wrote {jsonPath}");
Console.WriteLine($"Wrote {mdPath}");

static object Serialize(Finding f) => new { f.Fixture, f.SlaId, f.Source, f.Message, f.Line };

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Alexaka1.Analyzers.StructuredLogging.slnx")))
        {
            return dir.FullName;
        }

        dir = dir.Parent;
    }

    return Directory.GetCurrentDirectory();
}

static async Task<List<Finding>> CollectRoslynAsync(string corpusDir)
{
    var files = Directory.GetFiles(corpusDir, "*.cs")
        .Where(p => !p.EndsWith("PropertiesNamingAnalyzer_SerilogInvalidSyntax.cs", StringComparison.Ordinal))
        .OrderBy(p => p, StringComparer.Ordinal)
        .ToArray();

    var trees = files.Select(path => CSharpSyntaxTree.ParseText(
            File.ReadAllText(path),
            new CSharpParseOptions(LanguageVersion.Latest),
            path))
        .ToArray();

    var compilation = CSharpCompilation.Create(
        "ComparisonCorpus",
        trees,
        CreateReferences(),
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var withAnalyzers = compilation.WithAnalyzers(
        ImmutableArray.Create<DiagnosticAnalyzer>(new StructuredLoggingAnalyzer()));
    var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);

    return diagnostics
        .Where(d => d.Id.StartsWith("SLA", StringComparison.Ordinal) && d.Location.IsInSource)
        .Select(d =>
        {
            var line = d.Location.GetLineSpan().StartLinePosition.Line + 1;
            return new Finding(
                Path.GetFileName(d.Location.SourceTree!.FilePath),
                d.Id,
                "roslyn",
                d.GetMessage(),
                line);
        })
        .OrderBy(f => f.Fixture, StringComparer.Ordinal)
        .ThenBy(f => f.SlaId, StringComparer.Ordinal)
        .ThenBy(f => f.Line)
        .ToList();
}

static List<string> MessageMismatches(IReadOnlyList<Finding> inspect, IReadOnlyList<Finding> roslyn)
{
    var unused = roslyn.ToList();
    var mismatches = new List<string>();
    foreach (var left in inspect)
    {
        var index = unused.FindIndex(r => r.Fixture == left.Fixture && r.SlaId == left.SlaId);
        if (index < 0)
        {
            continue;
        }

        var right = unused[index];
        unused.RemoveAt(index);
        if (!string.Equals(left.Message, right.Message, StringComparison.Ordinal))
        {
            mismatches.Add($"`{Key(left)}`: InspectCode “{left.Message}” vs Roslyn “{right.Message}”");
        }
    }

    return mismatches;
}

static ComparisonResult Compare(string leftName, IReadOnlyList<Finding> left, string rightName, IReadOnlyList<Finding> right)
{
    var leftKeys = left.Select(Key).ToList();
    var rightKeys = right.Select(Key).ToList();
    var leftBag = Count(leftKeys);
    var rightBag = Count(rightKeys);

    var matches = new List<string>();
    var leftOnly = new List<string>();
    var rightOnly = new List<string>();

    foreach (var key in leftBag.Keys.Union(rightBag.Keys).OrderBy(k => k, StringComparer.Ordinal))
    {
        leftBag.TryGetValue(key, out var l);
        rightBag.TryGetValue(key, out var r);
        var shared = Math.Min(l, r);
        for (var i = 0; i < shared; i++)
        {
            matches.Add(key);
        }

        for (var i = 0; i < l - shared; i++)
        {
            leftOnly.Add(key);
        }

        for (var i = 0; i < r - shared; i++)
        {
            rightOnly.Add(key);
        }
    }

    return new ComparisonResult(leftName, rightName, matches, leftOnly, rightOnly);
}

static string Key(Finding f) => $"{f.Fixture}:{f.SlaId}";

static Dictionary<string, int> Count(IEnumerable<string> keys)
{
    var bag = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var key in keys)
    {
        bag[key] = bag.TryGetValue(key, out var n) ? n + 1 : 1;
    }

    return bag;
}

static ImmutableArray<MetadataReference> CreateReferences()
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

static string RenderMarkdown(
    string repoRoot,
    bool pluginLoaded,
    string? inspectXml,
    ComparisonResult inspectVsRoslyn,
    List<string> messageMismatches,
    List<Finding> roslyn,
    IReadOnlyList<Finding> inspect)
{
    var sb = new StringBuilder();
    sb.AppendLine("# ReSharper vs Roslyn comparison");
    sb.AppendLine();
    sb.AppendLine($"Generated (UTC): {DateTime.UtcNow:O}");
    sb.AppendLine();
    sb.AppendLine("Corpus: characterization fixtures in `test/comparison/corpus`.");
    sb.AppendLine();
    sb.AppendLine("## Tools");
    sb.AppendLine();
    sb.AppendLine("- InspectCode: JetBrains Inspect Code 2025.3.5 (Wave 253)");
    sb.AppendLine("- Plugin: `ReSharper.Structured.Logging` 2025.1.0.373 (Wave 251) loaded as an InspectCode extension");
    sb.AppendLine($"- InspectCode report: `{(inspectXml is null ? "(not provided)" : Path.GetRelativePath(repoRoot, inspectXml).Replace('\\', '/'))}`");
    sb.AppendLine($"- Plugin issue types present in report: **{pluginLoaded}**");
    sb.AppendLine($"- InspectCode plugin findings: {inspect.Count}");
    sb.AppendLine($"- Roslyn SLA findings: {roslyn.Count}");
    sb.AppendLine();
    sb.AppendLine("Keys are `file.cs:SLAxxxx`. Spans are not compared: SLA0011 highlights the trailing period, while the plugin highlights the whole literal.");
    sb.AppendLine();
    sb.AppendLine("## InspectCode vs Roslyn");
    sb.AppendLine();
    if (inspectXml is null)
    {
        sb.AppendLine("InspectCode XML was not supplied. Roslyn results are listed below; rerun `run-comparison.sh` to fill this section.");
    }
    else if (!pluginLoaded)
    {
        sb.AppendLine("The InspectCode report does not contain Structured Logging issue types. The marketplace plugin Wave 251 likely did not load into this CLI. See `run-comparison.sh` fallback to a Wave-matching CLI.");
    }
    else
    {
        WriteDiff(sb, inspectVsRoslyn);
        if (messageMismatches.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("Matching findings use the same diagnostic message text.");
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("### Message text differences (same file and rule)");
            sb.AppendLine();
            foreach (var line in messageMismatches)
            {
                sb.AppendLine($"- {line}");
            }
        }
    }

    sb.AppendLine();
    sb.AppendLine("## Roslyn findings");
    sb.AppendLine();
    sb.AppendLine("| File | ID | Line | Message |");
    sb.AppendLine("|---|---|---:|---|");
    foreach (var f in roslyn)
    {
        sb.AppendLine($"| `{f.Fixture}` | {f.SlaId} | {f.Line} | {Escape(f.Message)} |");
    }

    if (inspect.Count > 0)
    {
        sb.AppendLine();
        sb.AppendLine("## InspectCode findings");
        sb.AppendLine();
        sb.AppendLine("| File | ID | Line | Message |");
        sb.AppendLine("|---|---|---:|---|");
        foreach (var f in inspect)
        {
            sb.AppendLine($"| `{f.Fixture}` | {f.SlaId} | {f.Line} | {Escape(f.Message)} |");
        }
    }

    return sb.ToString();
}

static void WriteDiff(StringBuilder sb, ComparisonResult result)
{
    sb.AppendLine($"- Matches: **{result.Matches.Count}**");
    sb.AppendLine($"- {result.LeftName} only: **{result.LeftOnly.Count}**");
    sb.AppendLine($"- {result.RightName} only: **{result.RightOnly.Count}**");
    if (result.LeftOnly.Count > 0)
    {
        sb.AppendLine();
        sb.AppendLine($"### {result.LeftName} only");
        sb.AppendLine();
        foreach (var key in result.LeftOnly)
        {
            sb.AppendLine($"- `{key}`");
        }
    }

    if (result.RightOnly.Count > 0)
    {
        sb.AppendLine();
        sb.AppendLine($"### {result.RightName} only");
        sb.AppendLine();
        foreach (var key in result.RightOnly)
        {
            sb.AppendLine($"- `{key}`");
        }
    }
}

static string Escape(string value) => value.Replace("|", "\\|").Replace("\n", " ");

internal sealed record ComparisonResult(
    string LeftName,
    string RightName,
    List<string> Matches,
    List<string> LeftOnly,
    List<string> RightOnly);
