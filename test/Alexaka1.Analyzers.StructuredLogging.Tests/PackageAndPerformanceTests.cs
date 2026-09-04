using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests;

[CollectionDefinition("PackageAndPerformance", DisableParallelization = true)]
public sealed class PackageAndPerformanceCollection;

[Collection("PackageAndPerformance")]
public sealed class PackageAndPerformanceTests
{
    // These ceilings cover the allocation delta between this analyzer and a
    // no-op analyzer over the same compilation on a warmed process. Retune
    // after SDK or Roslyn upgrades. Keep docs/performance-policy.md in sync.
    private static readonly TimeSpan MaxWallClock = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan MaxAnalyzerExecution = TimeSpan.FromMilliseconds(500);
    private const long UnrelatedAllocationDeltaLimitBytes = 16 * 1024 * 1024;
    private const long LoggingAllocationDeltaLimitBytes = 8 * 1024 * 1024;
    private readonly ITestOutputHelper _output;

    public PackageAndPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Packed_nupkg_has_analyzers_and_no_lib()
    {
        using var package = Pack();
        using var zip = ZipFile.OpenRead(package.PackagePath);
        var entries = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToArray();

        var analyzerEntries = entries
            .Where(e => e.StartsWith("analyzers/", StringComparison.Ordinal))
            .OrderBy(e => e, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "analyzers/dotnet/cs/Alexaka1.Analyzers.StructuredLogging.CodeFixes.dll",
                "analyzers/dotnet/cs/Alexaka1.Analyzers.StructuredLogging.dll"
            ],
            analyzerEntries);
        Assert.DoesNotContain(entries, e => e.StartsWith("lib/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries,
            e => e.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                 Path.GetFileNameWithoutExtension(e)
                     .StartsWith("Microsoft.CodeAnalysis", StringComparison.OrdinalIgnoreCase));

        var metadata = ReadPackageMetadata(zip);
        var dependencies = metadata.Element(NuspecNamespace + "dependencies");
        Assert.True(
            dependencies is null || !dependencies.Elements().Any(),
            "The analyzer-only package must not declare NuGet dependencies.");
        Assert.Equal(
            "true",
            metadata.Element(NuspecNamespace + "developmentDependency")?.Value,
            ignoreCase: true);
    }

    [Fact]
    public void Packed_analyzer_excludes_workspaces_references()
    {
        using var package = Pack();
        using var zip = ZipFile.OpenRead(package.PackagePath);

        var analyzerReferences = ReadAssemblyReferences(
            zip,
            "analyzers/dotnet/cs/Alexaka1.Analyzers.StructuredLogging.dll");
        Assert.DoesNotContain(analyzerReferences, IsWorkspacesAssembly);

        var codeFixReferences = ReadAssemblyReferences(
            zip,
            "analyzers/dotnet/cs/Alexaka1.Analyzers.StructuredLogging.CodeFixes.dll");
        Assert.Contains(codeFixReferences, IsWorkspacesAssembly);
    }

    [Fact]
    public void Packed_package_is_consumed_by_real_package_reference()
    {
        using var package = Pack();
        using var consumer = TemporaryDirectory.Create("sla-consumer-");
        using var feed = TemporaryDirectory.Create("sla-feed-");
        using var packages = TemporaryDirectory.Create("sla-packages-");

        var packageFileName = Path.GetFileName(package.PackagePath);
        File.Copy(package.PackagePath, Path.Combine(feed.DirectoryPath, packageFileName));
        var metadata = ReadPackageMetadata(package);
        var packageId = metadata.Element(NuspecNamespace + "id")?.Value
                        ?? throw new InvalidOperationException("Packed package nuspec has no id.");
        var packageVersion = metadata.Element(NuspecNamespace + "version")?.Value
                             ?? throw new InvalidOperationException("Packed package nuspec has no version.");
        var projectPath = Path.Combine(consumer.DirectoryPath, "Consumer.csproj");
        var sourcePath = Path.Combine(consumer.DirectoryPath, "Program.cs");
        var sarifPath = Path.Combine(consumer.DirectoryPath, "consumer.sarif");

        File.WriteAllText(
            projectPath,
            $"""
             <Project Sdk="Microsoft.NET.Sdk">
               <PropertyGroup>
                 <OutputType>Exe</OutputType>
                 <TargetFramework>net10.0</TargetFramework>
                 <ImplicitUsings>enable</ImplicitUsings>
                 <Nullable>enable</Nullable>
                 <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
               </PropertyGroup>
               <ItemGroup>
                 <PackageReference Include="{packageId}" Version="{packageVersion}" />
                 <PackageReference Include="Serilog" Version="4.4.0" />
               </ItemGroup>
             </Project>
             """);
        File.WriteAllText(
            sourcePath,
            /*lang=csharp*/ """
                            using Serilog;

                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{myProperty}", 1);
                                }
                            }
                            """);

        try
        {
            RunDotNet(
                $"restore \"{projectPath}\" --source \"{feed.DirectoryPath}\" --source https://api.nuget.org/v3/index.json --packages \"{packages.DirectoryPath}\" --no-cache --nologo --verbosity quiet",
                packages.DirectoryPath);
            RunDotNet(
                $"build \"{projectPath}\" --configuration Release --no-restore --nologo -v:minimal -p:ErrorLog=\"{sarifPath}%2cversion=2.1\"",
                packages.DirectoryPath);

            Assert.True(File.Exists(sarifPath), $"Expected ErrorLog SARIF at {sarifPath}.");
            var diagnostics = ParseActiveAaslDiagnostics(sarifPath);
            Assert.Contains(diagnostics, diagnostic => diagnostic.RuleId == "AASL0009");

            var outputDirectory = Path.Combine(consumer.DirectoryPath, "bin", "Release", "net10.0");
            Assert.True(Directory.Exists(outputDirectory), "Consumer build did not produce an output directory.");
            var outputDlls = Directory.GetFiles(outputDirectory, "*.dll", SearchOption.AllDirectories);
            Assert.Contains(outputDlls, path =>
                string.Equals(Path.GetFileName(path), "Consumer.dll", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(outputDlls, path =>
                Path.GetFileName(path).StartsWith("Alexaka1.Analyzers.StructuredLogging",
                    StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(outputDlls, path =>
                Path.GetFileNameWithoutExtension(path)
                    .StartsWith("Microsoft.CodeAnalysis", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            TryMoveToTrash(sarifPath);
        }
    }

    [Fact]
    public async Task Analyzer_handles_large_unrelated_compilation()
    {
        var outcome = await RunPerformanceGateAsync(
            "unrelated compilation",
            CreateUnrelatedSource(4000),
            UnrelatedAllocationDeltaLimitBytes,
            TestContext.Current.CancellationToken);
        Assert.Empty(AaslDiagnostics(outcome.Diagnostics));
    }

    [Fact]
    public async Task Analyzer_registers_no_node_actions_without_logging_references()
    {
        var outcome = await AnalyzerTestHost.AnalyzeAsync(
            CreateUnrelatedSource(4000),
            // The source matches the unrelated-compilation gate; only its reference set differs.
            references: NuGetPackageResolver.GetReferences(),
            cancellationToken: TestContext.Current.CancellationToken);

        AssertTelemetryShape(outcome.Telemetry, expectedSyntaxNodeActions: 0);
        Assert.Empty(AaslDiagnostics(outcome.Diagnostics));
    }

    [Fact]
    public async Task Analyzer_handles_many_logging_calls()
    {
        var outcome = await RunPerformanceGateAsync(
            "logging compilation",
            CreateLoggingSource(500),
            LoggingAllocationDeltaLimitBytes,
            TestContext.Current.CancellationToken);
        Assert.Empty(AaslDiagnostics(outcome.Diagnostics));
    }

    [Fact]
    public async Task Analyzer_handles_many_logger_message_constants()
    {
        const string label = "logger message constants";
        var source = CreateLoggerMessageConstantSource(480);
        var cancellationToken = TestContext.Current.CancellationToken;
        _ = await AnalyzerTestHost.AnalyzeAsync(
            source,
            concurrentAnalysis: false,
            cancellationToken: cancellationToken);
        var outcome = await AnalyzerTestHost.AnalyzeAsync(
            source,
            concurrentAnalysis: false,
            cancellationToken: cancellationToken);

        AssertTelemetryShape(outcome.Telemetry);
        _output.WriteLine(
            $"{label} wall={outcome.WallClock.TotalMilliseconds:F0}ms exec={outcome.Telemetry.ExecutionTime.TotalMilliseconds:F0}ms concurrent={outcome.Telemetry.Concurrent}");
        Assert.True(
            outcome.Telemetry.ExecutionTime < MaxAnalyzerExecution,
            $"{label} analyzer execution took {outcome.Telemetry.ExecutionTime}");
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
    [InlineData("samples/Net10Example/Net10Example.csproj", "samples/Net10Example/bin/Release/net10.0",
        "Net10Example.dll")]
    [InlineData("samples/Net10BlazorExample/Net10BlazorExample.csproj",
        "samples/Net10BlazorExample/bin/Release/net10.0", "Net10BlazorExample.dll")]
    [InlineData("samples/NetStandard20Example/NetStandard20Example.csproj",
        "samples/NetStandard20Example/bin/Release/netstandard2.0", "NetStandard20Example.dll")]
    [InlineData("samples/Net472Example/Net472Example.csproj", "samples/Net472Example/bin/Release/net472",
        "Net472Example.dll")]
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
                TryMoveToTrash(sarif);
            }
        }

        var output = Path.Combine(repo, relativeOutput.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(Directory.Exists(output), "Sample build did not produce output directory: " + output);
        var dlls = Directory.GetFiles(output, "*.dll");
        Assert.Contains(dlls,
            path => string.Equals(Path.GetFileName(path), outputAssembly, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dlls,
            path => string.Equals(Path.GetFileName(path), "Alexaka1.Analyzers.StructuredLogging.dll",
                StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dlls,
            path => string.Equals(Path.GetFileName(path), "Alexaka1.Analyzers.StructuredLogging.CodeFixes.dll",
                StringComparison.OrdinalIgnoreCase));
    }

    private async Task<AnalysisOutcome> RunPerformanceGateAsync(
        string label,
        string source,
        long maxAllocationDeltaBytes,
        CancellationToken cancellationToken)
    {
        _ = await AnalyzerTestHost.AnalyzeAgainstControlAsync(
            source,
            EmptyAnalyzer.Instance,
            cancellationToken: cancellationToken);
        var measured = await AnalyzerTestHost.AnalyzeAgainstControlAsync(
            source,
            EmptyAnalyzer.Instance,
            cancellationToken: cancellationToken);
        var outcome = measured.Analyzer;

        AssertTelemetryShape(outcome.Telemetry);
        _output.WriteLine(
            $"{label} wall={outcome.WallClock.TotalMilliseconds:F0}ms exec={outcome.Telemetry.ExecutionTime.TotalMilliseconds:F0}ms analyzer-alloc={outcome.AllocatedBytes} control-alloc={measured.ControlAllocatedBytes} delta-alloc={measured.AllocationDeltaBytes} concurrent={outcome.Telemetry.Concurrent}");
        Assert.True(outcome.WallClock < MaxWallClock, $"{label} took {outcome.WallClock}");
        Assert.True(
            outcome.Telemetry.ExecutionTime < MaxAnalyzerExecution,
            $"{label} analyzer execution took {outcome.Telemetry.ExecutionTime}");
        Assert.True(
            measured.AllocationDeltaBytes < maxAllocationDeltaBytes,
            $"{label} allocation delta was {measured.AllocationDeltaBytes} bytes (limit {maxAllocationDeltaBytes}; analyzer {outcome.AllocatedBytes}; control {measured.ControlAllocatedBytes})");
        return outcome;
    }

    private static string[] ReadAssemblyReferences(ZipArchive zip, string entryName)
    {
        var entry = zip.GetEntry(entryName);
        Assert.NotNull(entry);
        using var entryStream = entry.Open();
        using var assemblyStream = new MemoryStream();
        entryStream.CopyTo(assemblyStream);
        assemblyStream.Position = 0;
        using var peReader = new PEReader(assemblyStream);
        var metadata = peReader.GetMetadataReader();
        return metadata.AssemblyReferences
            .Select(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name))
            .ToArray();
    }

    private static bool IsWorkspacesAssembly(string assemblyName) =>
        string.Equals(assemblyName, "Microsoft.CodeAnalysis.Workspaces", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(assemblyName, "Microsoft.CodeAnalysis.CSharp.Workspaces", StringComparison.OrdinalIgnoreCase);

    private static void AssertTelemetryShape(
        AnalyzerTelemetryInfo telemetry,
        int expectedSyntaxNodeActions = 4)
    {
        Assert.True(telemetry.Concurrent, "Analyzer must enable concurrent execution.");
        Assert.Equal(1, telemetry.CompilationStartActionsCount);
        Assert.Equal(expectedSyntaxNodeActions, telemetry.SyntaxNodeActionsCount);
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

    private static string CreateLoggerMessageConstantSource(int methodCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine("using Microsoft.Extensions.Logging;");
        builder.AppendLine("public static partial class Log {");
        for (var i = 0; i < methodCount; i++)
        {
            builder.AppendLine($"private const string Message{i} = \"Value {{Value{i}}}\";");
        }

        for (var i = 0; i < methodCount; i++)
        {
            builder.AppendLine($"[LoggerMessage(EventId = {i}, Level = LogLevel.Information, Message = Message{i})]");
            builder.AppendLine($"public static partial void Write{i}(ILogger logger, int Value{i});");
        }

        builder.AppendLine("}");
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
            .Select(d =>
                $"{d.Id}:{d.Location.GetLineSpan().Path}:{d.Location.SourceSpan.Start}:{d.Location.SourceSpan.Length}:{d.GetMessage()}")
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

    private static readonly Dictionary<string, SarifDiagnostic[]> ExpectedSampleDiagnostics =
        new(StringComparer.Ordinal)
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
                new("AASL0009", "Program.cs", 8, 27)
            ],
            ["samples/Net10BlazorExample/Net10BlazorExample.csproj"] =
            [
                new("AASL0009", "Counter.razor.cs", 16, 40),
                new("AASL0011", "Counter.razor.cs", 16, 47),
                new("AASL0009", "Home.razor", 18, 38)
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

    private static PackedPackage Pack()
    {
        var repo = FindRepoRoot();
        var packProject = Path.Combine(repo, "pack", "Alexaka1.Analyzers.StructuredLogging", "Package.csproj");
        var output = Path.Combine(Path.GetTempPath(), "sla-pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try
        {
            RunDotNet($"pack \"{packProject}\" -c Release -o \"{output}\" --nologo");
            var nupkg = Directory.GetFiles(output, "Alexaka1.Analyzers.StructuredLogging.*.nupkg").Single();
            return new PackedPackage(output, nupkg);
        }
        catch
        {
            TryMoveToTrash(output);
            throw;
        }
    }

    private static string RunDotNet(string arguments, string? packagesDirectory = null)
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
        if (packagesDirectory is not null)
        {
            psi.Environment["NUGET_PACKAGES"] = packagesDirectory;
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(TimeSpan.FromMinutes(2)))
        {
            var killFailure = TryKill(process);
            _ = process.WaitForExit(TimeSpan.FromSeconds(5));
            var timeoutOutput = CompletedProcessOutput(stdoutTask, stderrTask);
            throw new TimeoutException(
                $"dotnet {arguments} timed out after two minutes.{Environment.NewLine}{timeoutOutput}",
                killFailure);
        }

        if (!Task.WaitAll([stdoutTask, stderrTask], TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException($"dotnet {arguments} output did not complete after the process exited.");
        }

        var output = stdoutTask.Result + Environment.NewLine + stderrTask.Result;
        Assert.True(process.ExitCode == 0, output);
        return output;
    }

    private static Exception? TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            return null;
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            return exception;
        }
    }

    private static string CompletedProcessOutput(Task<string> stdoutTask, Task<string> stderrTask)
    {
        if (!Task.WaitAll([stdoutTask, stderrTask], TimeSpan.FromSeconds(5)))
        {
            return "Process output was not available before the timeout.";
        }

        return stdoutTask.Result + Environment.NewLine + stderrTask.Result;
    }

    private static readonly XNamespace NuspecNamespace = "http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd";

#pragma warning disable RS1036 // This analyzer exists only as a no-op allocation control.
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    private sealed class EmptyAnalyzer : DiagnosticAnalyzer
    {
        public static readonly EmptyAnalyzer Instance = new();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
        }
    }
#pragma warning restore RS1036

    private static XElement ReadPackageMetadata(PackedPackage package)
    {
        using var zip = ZipFile.OpenRead(package.PackagePath);
        return ReadPackageMetadata(zip);
    }

    private static XElement ReadPackageMetadata(ZipArchive zip)
    {
        var nuspec = Assert.Single(zip.Entries,
            entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        using var stream = nuspec.Open();
        var document = XDocument.Load(stream);
        var metadata = document.Root?.Element(NuspecNamespace + "metadata");
        Assert.NotNull(metadata);
        return metadata;
    }

    private static void TryMoveToTrash(string path)
    {
        try
        {
            MoveToTrash(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Could not clean up '{path}': {exception.Message}");
        }
    }

    private static void MoveToTrash(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return;
        }

        var candidates = new List<string>();
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(profile))
        {
            candidates.Add(Path.Combine(profile, ".Trash"));
        }

        candidates.Add(Path.Combine(Path.GetTempPath(), "sla-trash-" + Guid.NewGuid().ToString("N")));
        Exception? lastError = null;
        foreach (var trashDirectory in candidates)
        {
            try
            {
                Directory.CreateDirectory(trashDirectory);
                var destination = Path.Combine(
                    trashDirectory,
                    Path.GetFileName(path) + "-" + Guid.NewGuid().ToString("N"));
                if (Directory.Exists(path))
                {
                    Directory.Move(path, destination);
                }
                else
                {
                    File.Move(path, destination);
                }

                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                lastError = exception;
            }
        }

        throw new IOException($"Could not move temporary path to recoverable trash: {path}", lastError);
    }

    private sealed class PackedPackage : IDisposable
    {
        private readonly string _directoryPath;
        private bool _disposed;

        public PackedPackage(string directoryPath, string packagePath)
        {
            _directoryPath = directoryPath;
            PackagePath = packagePath;
        }

        public string PackagePath { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            TryMoveToTrash(_directoryPath);
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private bool _disposed;

        private TemporaryDirectory(string directoryPath)
        {
            DirectoryPath = directoryPath;
        }

        public string DirectoryPath { get; }

        public static TemporaryDirectory Create(string prefix)
        {
            var directory = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return new TemporaryDirectory(directory);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            TryMoveToTrash(DirectoryPath);
        }
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
