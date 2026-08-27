using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests;

[CollectionDefinition("PackageAndPerformance", DisableParallelization = true)]
public sealed class PackageAndPerformanceCollection;

[Collection("PackageAndPerformance")]
public sealed class PackageAndPerformanceTests
{
    // These ceilings cover GetAnalysisResultAsync allocations on a warmed
    // process, after CompilationWithAnalyzers is constructed. Retune after
    // SDK or Roslyn upgrades. Keep docs/performance-policy.md in sync.
    private static readonly TimeSpan MaxWallClock = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan MaxAnalyzerExecution = TimeSpan.FromSeconds(5);
    private const long UnrelatedAllocationLimitBytes = 48 * 1024 * 1024;
    private const long LoggingAllocationLimitBytes = 32 * 1024 * 1024;
    private readonly ITestOutputHelper _output;

    public PackageAndPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Packed_nupkg_has_analyzers_and_no_lib()
    {
        var nupkg = Pack();
        using var zip = ZipFile.OpenRead(nupkg);
        var entries = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToArray();
        Assert.Contains(entries, e => e.Equals("analyzers/dotnet/cs/Alexaka1.Analyzers.StructuredLogging.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(entries, e => e.Equals("analyzers/dotnet/cs/Alexaka1.Analyzers.StructuredLogging.CodeFixes.dll", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries, e => e.StartsWith("lib/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries, e => e.EndsWith("Microsoft.CodeAnalysis.dll", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Analyzer_handles_large_unrelated_compilation()
    {
        var outcome = await RunPerformanceGateAsync(
            "unrelated compilation",
            CreateUnrelatedSource(4000),
            UnrelatedAllocationLimitBytes,
            TestContext.Current.CancellationToken);
        Assert.Empty(AaslDiagnostics(outcome.Diagnostics));
    }

    [Fact]
    public async Task Analyzer_handles_many_logging_calls()
    {
        var outcome = await RunPerformanceGateAsync(
            "logging compilation",
            CreateLoggingSource(500),
            LoggingAllocationLimitBytes,
            TestContext.Current.CancellationToken);
        Assert.Empty(AaslDiagnostics(outcome.Diagnostics));
    }

    [Fact]
    public async Task Analyzer_reports_concurrent_execution_and_action_telemetry()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var cold = await AnalyzerTestHost.AnalyzeAsync(CreateLoggingSource(200), cancellationToken: cancellationToken);
        var warm = await AnalyzerTestHost.AnalyzeAsync(CreateLoggingSource(200), cancellationToken: cancellationToken);

        AssertTelemetryShape(cold.Telemetry);
        AssertTelemetryShape(warm.Telemetry);
        _output.WriteLine(
            $"cold wall={cold.WallClock.TotalMilliseconds:F0}ms exec={cold.Telemetry.ExecutionTime.TotalMilliseconds:F0}ms; warm wall={warm.WallClock.TotalMilliseconds:F0}ms exec={warm.Telemetry.ExecutionTime.TotalMilliseconds:F0}ms");
        Assert.True(
            cold.Telemetry.ExecutionTime < MaxAnalyzerExecution,
            $"Cold analyzer execution took {cold.Telemetry.ExecutionTime}");
        Assert.True(
            warm.Telemetry.ExecutionTime < MaxAnalyzerExecution,
            $"Warm analyzer execution took {warm.Telemetry.ExecutionTime}");
        Assert.Empty(AaslDiagnostics(cold.Diagnostics));
        Assert.Empty(AaslDiagnostics(warm.Diagnostics));
    }

    [Fact]
    public async Task Analyzer_concurrent_runs_are_deterministic()
    {
        var source = /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{myProperty}", 1);
                    Log.Logger.Information("{Value}", 1);
                    Log.Logger.Information("Done.");
                    Log.Logger.Information("{0}", 1);
                }
            }
            """;
        var additional = Enumerable.Range(0, 8)
            .Select(i => (
                $"/0/File{i}.cs",
                $$"""
                using Serilog;
                public static class C{{i}}
                {
                    public static void M()
                    {
                        Log.Logger.Information("{myProperty}", {{i}});
                        Log.Logger.Information("Done.");
                    }
                }
                """))
            .ToArray();

        var cancellationToken = TestContext.Current.CancellationToken;
        var sequential = await AnalyzerTestHost.AnalyzeAsync(
            source,
            additionalSources: additional,
            concurrentAnalysis: false,
            cancellationToken: cancellationToken);
        AssertTelemetryShape(sequential.Telemetry);

        var concurrentTasks = Enumerable.Range(0, 8)
            .Select(_ => AnalyzerTestHost.AnalyzeAsync(
                source,
                additionalSources: additional,
                cancellationToken: cancellationToken))
            .ToArray();
        var concurrent = await Task.WhenAll(concurrentTasks);

        var expected = DiagnosticKeys(sequential.Diagnostics);
        Assert.NotEmpty(expected);
        foreach (var run in concurrent)
        {
            AssertTelemetryShape(run.Telemetry);
            Assert.Equal(expected, DiagnosticKeys(run.Diagnostics));
        }
    }

    [Fact]
    public async Task Invalid_editorconfig_does_not_throw()
    {
        var source = /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{myProperty}", 1);
                }
            }
            """;
        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(
            source,
            editorConfig: "dotnet_code_quality.AASL.ignored_properties_regex = (unclosed");
        Assert.Contains(diagnostics, d => d.Id == "AASL0009");
    }

    [Theory]
    [InlineData("samples/Net10Example/Net10Example.csproj", "samples/Net10Example/bin/Release/net10.0", "Net10Example.dll")]
    [InlineData("samples/Net10BlazorExample/Net10BlazorExample.csproj", "samples/Net10BlazorExample/bin/Release/net10.0", "Net10BlazorExample.dll")]
    [InlineData("samples/NetStandard20Example/NetStandard20Example.csproj", "samples/NetStandard20Example/bin/Release/netstandard2.0", "NetStandard20Example.dll")]
    [InlineData("samples/Net472Example/Net472Example.csproj", "samples/Net472Example/bin/Release/net472", "Net472Example.dll")]
    public void Sample_build_reports_aasl_diagnostics_and_does_not_copy_analyzer_assemblies(
        string relativeProject,
        string relativeOutput,
        string outputAssembly)
    {
        var repo = FindRepoRoot();
        var project = Path.Combine(repo, relativeProject.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(project), "Sample project missing: " + project);

        var projectDir = Path.GetDirectoryName(project)!;
        var sarif = Path.Combine(projectDir, "dotnet-build-error.sarif");
        try
        {
            RunDotNet($"clean \"{project}\" -c Release --nologo");
            var log = RunDotNet(
                $"build \"{project}\" -c Release --no-incremental --nologo -v:minimal -p:ErrorLog=dotnet-build-error.sarif%2cversion=2.1");
            Assert.True(File.Exists(sarif), $"Expected ErrorLog SARIF at {sarif}. Build output:\n{log}");
            Assert.Equal(Ordered(ExpectedSampleDiagnostics[relativeProject]), ParseActiveAaslDiagnostics(sarif));
        }
        finally
        {
            if (File.Exists(sarif))
            {
                File.Delete(sarif);
            }
        }

        var output = Path.Combine(repo, relativeOutput.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(Directory.Exists(output), "Sample build did not produce output directory: " + output);
        var dlls = Directory.GetFiles(output, "*.dll");
        Assert.Contains(dlls, path => string.Equals(Path.GetFileName(path), outputAssembly, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dlls, path => string.Equals(Path.GetFileName(path), "Alexaka1.Analyzers.StructuredLogging.dll", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dlls, path => string.Equals(Path.GetFileName(path), "Alexaka1.Analyzers.StructuredLogging.CodeFixes.dll", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<AnalysisOutcome> RunPerformanceGateAsync(
        string label,
        string source,
        long maxAllocatedBytes,
        CancellationToken cancellationToken)
    {
        _ = await AnalyzerTestHost.AnalyzeAsync(
            source,
            concurrentAnalysis: false,
            cancellationToken: cancellationToken);
        var outcome = await AnalyzerTestHost.AnalyzeAsync(
            source,
            concurrentAnalysis: false,
            measureAllocations: true,
            cancellationToken: cancellationToken);

        AssertTelemetryShape(outcome.Telemetry);
        _output.WriteLine(
            $"{label} wall={outcome.WallClock.TotalMilliseconds:F0}ms exec={outcome.Telemetry.ExecutionTime.TotalMilliseconds:F0}ms alloc={outcome.AllocatedBytes} concurrent={outcome.Telemetry.Concurrent}");
        Assert.True(outcome.WallClock < MaxWallClock, $"{label} took {outcome.WallClock}");
        Assert.True(
            outcome.Telemetry.ExecutionTime < MaxAnalyzerExecution,
            $"{label} analyzer execution took {outcome.Telemetry.ExecutionTime}");
        Assert.True(
            outcome.AllocatedBytes < maxAllocatedBytes,
            $"{label} allocated {outcome.AllocatedBytes} bytes (limit {maxAllocatedBytes})");
        return outcome;
    }

    private static void AssertTelemetryShape(AnalyzerTelemetryInfo telemetry)
    {
        Assert.True(telemetry.Concurrent, "Analyzer must enable concurrent execution.");
        Assert.Equal(1, telemetry.CompilationStartActionsCount);
        Assert.Equal(4, telemetry.SyntaxNodeActionsCount);
        Assert.Equal(0, telemetry.SyntaxTreeActionsCount);
        Assert.Equal(0, telemetry.SemanticModelActionsCount);
        Assert.Equal(0, telemetry.OperationActionsCount);
        Assert.Equal(0, telemetry.SymbolActionsCount);
    }

    private static string CreateUnrelatedSource(int callCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine("using System;");
        builder.AppendLine("public static class Program {");
        builder.AppendLine("public static void Main() {");
        for (var i = 0; i < callCount; i++)
        {
            builder.AppendLine($"Console.WriteLine({i});");
        }

        builder.AppendLine("}}");
        return builder.ToString();
    }

    private static string CreateLoggingSource(int callCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine("using Serilog;");
        builder.AppendLine("public static class Program {");
        builder.AppendLine("public static void Main() {");
        for (var i = 0; i < callCount; i++)
        {
            builder.AppendLine($"Log.Logger.Information(\"Value {{Value{i}}}\", {i});");
        }

        builder.AppendLine("}}");
        return builder.ToString();
    }

    private static IEnumerable<Diagnostic> AaslDiagnostics(IEnumerable<Diagnostic> diagnostics) =>
        diagnostics.Where(d => d.Id.StartsWith("AASL", StringComparison.Ordinal));

    private static string[] DiagnosticKeys(IEnumerable<Diagnostic> diagnostics) =>
        AaslDiagnostics(diagnostics)
            .Select(d => $"{d.Id}:{d.Location.GetLineSpan().Path}:{d.Location.SourceSpan.Start}:{d.Location.SourceSpan.Length}:{d.GetMessage()}")
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

    private static readonly Dictionary<string, SarifDiagnostic[]> ExpectedSampleDiagnostics = new(StringComparer.Ordinal)
    {
        ["samples/Net10Example/Net10Example.csproj"] =
        [
            new("AASL0009", "LogMessages.cs", 14, 31),
            new("AASL0011", "LogMessages.cs", 14, 40),
            new("AASL0009", "LogMessages.cs", 17, 48),
            new("AASL0009", "LogMessages.cs", 20, 29),
            new("AASL0009", "LogMessages.cs", 26, 78),
            new("AASL0009", "LogMessages.cs", 36, 81),
            new("AASL0009", "LogMessages.cs", 42, 83),
            new("AASL0009", "LogMessages.cs", 49, 82),
            new("AASL0011", "LogMessages.cs", 49, 91),
            new("AASL0009", "LogMessages.cs", 52, 50),
            new("AASL0009", "Program.cs", 7, 27)
        ],
        ["samples/Net10BlazorExample/Net10BlazorExample.csproj"] =
        [
            new("AASL0009", "Counter.razor.cs", 17, 40),
            new("AASL0011", "Counter.razor.cs", 17, 47),
            new("AASL0009", "Home.razor", 17, 38)
        ],
        ["samples/NetStandard20Example/NetStandard20Example.csproj"] =
        [
            new("AASL0009", "Sample.cs", 9, 39)
        ],
        ["samples/Net472Example/Net472Example.csproj"] =
        [
            new("AASL0009", "Sample.cs", 9, 39)
        ]
    };

    private readonly record struct SarifDiagnostic(string RuleId, string FileName, int Line, int Column);

    private static SarifDiagnostic[] ParseActiveAaslDiagnostics(string sarifPath)
    {
        using var stream = File.OpenRead(sarifPath);
        using var doc = JsonDocument.Parse(stream);
        var diagnostics = new List<SarifDiagnostic>();
        if (!doc.RootElement.TryGetProperty("runs", out var runs))
        {
            return [];
        }

        foreach (var run in runs.EnumerateArray())
        {
            if (!run.TryGetProperty("results", out var results))
            {
                continue;
            }

            foreach (var result in results.EnumerateArray())
            {
                if (!IsActiveSarifResult(result))
                {
                    continue;
                }

                if (!result.TryGetProperty("ruleId", out var ruleId) ||
                    ruleId.GetString() is not { } id ||
                    !id.StartsWith("AASL", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.True(
                    TryReadPrimaryLocation(result, out var fileName, out var line, out var column),
                    $"Active AASL diagnostic '{id}' has no readable primary location.");

                diagnostics.Add(new SarifDiagnostic(id, fileName, line, column));
            }
        }

        return Ordered(diagnostics);
    }

    private static SarifDiagnostic[] Ordered(IEnumerable<SarifDiagnostic> diagnostics) =>
        diagnostics
            .OrderBy(d => d.RuleId, StringComparer.Ordinal)
            .ThenBy(d => d.FileName, StringComparer.Ordinal)
            .ThenBy(d => d.Line)
            .ThenBy(d => d.Column)
            .ToArray();

    private static bool TryReadPrimaryLocation(
        JsonElement result,
        out string fileName,
        out int line,
        out int column)
    {
        fileName = "";
        line = 0;
        column = 0;
        if (!result.TryGetProperty("locations", out var locations) ||
            locations.GetArrayLength() == 0)
        {
            return false;
        }

        var physical = locations[0].GetProperty("physicalLocation");
        var uri = physical.GetProperty("artifactLocation").GetProperty("uri").GetString();
        if (string.IsNullOrEmpty(uri))
        {
            return false;
        }

        var region = physical.GetProperty("region");
        fileName = Path.GetFileName(uri.Replace('\\', '/'));
        line = region.GetProperty("startLine").GetInt32();
        column = region.GetProperty("startColumn").GetInt32();
        return true;
    }

    private static bool IsActiveSarifResult(JsonElement result)
    {
        if (!result.TryGetProperty("suppressions", out var suppressions) ||
            suppressions.ValueKind != JsonValueKind.Array)
        {
            return true;
        }

        return suppressions.GetArrayLength() == 0;
    }

    private static string Pack()
    {
        var repo = FindRepoRoot();
        var packProject = Path.Combine(repo, "pack", "Alexaka1.Analyzers.StructuredLogging", "Package.csproj");
        var output = Path.Combine(Path.GetTempPath(), "sla-pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        RunDotNet($"pack \"{packProject}\" -c Release -o \"{output}\" --nologo");
        return Directory.GetFiles(output, "Alexaka1.Analyzers.StructuredLogging.*.nupkg").Single();
    }

    private static string RunDotNet(string arguments)
    {
        var psi = new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = FindRepoRoot()
        };
        psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        psi.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        using var process = Process.Start(psi)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        Task.WaitAll(stdoutTask, stderrTask);
        process.WaitForExit();
        var output = stdoutTask.Result + Environment.NewLine + stderrTask.Result;
        Assert.True(process.ExitCode == 0, output);
        return output;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "Alexaka1.Analyzers.StructuredLogging.slnx")) ||
                File.Exists(Path.Combine(dir, "pack", "Alexaka1.Analyzers.StructuredLogging", "Package.csproj")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
