using System.Diagnostics;
using System.IO.Compression;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests;

public sealed class PackageAndPerformanceTests
{
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
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("using System;");
        builder.AppendLine("public static class Program {");
        builder.AppendLine("public static void Main() {");
        for (var i = 0; i < 4000; i++)
        {
            builder.AppendLine($"Console.WriteLine({i});");
        }

        builder.AppendLine("}}");
        var sw = Stopwatch.StartNew();
        var diagnostics = await Infrastructure.AnalyzerTestHost.GetDiagnosticsAsync(builder.ToString());
        sw.Stop();
        Assert.Empty(diagnostics.Where(d => d.Id.StartsWith("AASL", StringComparison.Ordinal)));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(20), $"Unrelated compilation took {sw.Elapsed}");
    }

    [Fact]
    public async Task Analyzer_handles_many_logging_calls()
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("using Serilog;");
        builder.AppendLine("public static class Program {");
        builder.AppendLine("public static void Main() {");
        for (var i = 0; i < 500; i++)
        {
            builder.AppendLine($"Log.Logger.Information(\"Value {{Value{i}}}\", {i});");
        }

        builder.AppendLine("}}");
        var sw = Stopwatch.StartNew();
        var diagnostics = await Infrastructure.AnalyzerTestHost.GetDiagnosticsAsync(builder.ToString());
        sw.Stop();
        Assert.Empty(diagnostics.Where(d => d.Id.StartsWith("AASL", StringComparison.Ordinal)));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(20), $"Logging compilation took {sw.Elapsed}");
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
        var diagnostics = await Infrastructure.AnalyzerTestHost.GetDiagnosticsAsync(
            source,
            editorConfig: "dotnet_code_quality.AASL.ignored_properties_regex = (unclosed");
        Assert.Contains(diagnostics, d => d.Id == "AASL0009");
    }

    [Fact]
    public void Sample_output_does_not_copy_analyzer_assemblies()
    {
        var repo = FindRepoRoot();
        var output = Path.Combine(repo, "samples", "Net10Example", "bin", "Release", "net10.0");
        if (!Directory.Exists(output))
        {
            return;
        }

        var files = Directory.GetFiles(output, "*.dll").Select(Path.GetFileName).ToArray();
        Assert.DoesNotContain("Alexaka1.Analyzers.StructuredLogging.dll", files, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Alexaka1.Analyzers.StructuredLogging.CodeFixes.dll", files, StringComparer.OrdinalIgnoreCase);
    }

    private static string Pack()
    {
        var repo = FindRepoRoot();
        var packProject = Path.Combine(repo, "pack", "Alexaka1.Analyzers.StructuredLogging", "Package.csproj");
        var output = Path.Combine(Path.GetTempPath(), "sla-pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        var psi = new ProcessStartInfo("dotnet", $"pack \"{packProject}\" -c Release -o \"{output}\" --nologo")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        psi.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, stdout + Environment.NewLine + stderr);
        var nupkg = Directory.GetFiles(output, "Alexaka1.Analyzers.StructuredLogging.*.nupkg").Single();
        return nupkg;
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
