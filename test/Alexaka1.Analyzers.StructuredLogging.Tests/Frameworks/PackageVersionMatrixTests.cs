using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Frameworks;

public sealed class PackageVersionMatrixTests
{
    public static IEnumerable<object[]> SerilogVersions() => Rows(PackageVersionMatrix.Serilog);

    public static IEnumerable<object[]> NLogVersions() => Rows(PackageVersionMatrix.NLog);

    public static IEnumerable<object[]> MelVersions() => Rows(PackageVersionMatrix.MicrosoftExtensionsLogging);

    public static IEnumerable<object[]> ZLoggerFormatStringVersions() => Rows(PackageVersionMatrix.ZLoggerFormatString);

    [Theory]
    [MemberData(nameof(SerilogVersions))]
    public Task Serilog_named_property_is_recognized(string version)
    {
        return AnalyzerTestHost.VerifyPackageVersionAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|AASL0009:{myProperty}|}", 1);
                }
            }
            """,
            PackageVersionMatrix.SerilogId,
            version);
    }

    [Theory]
    [MemberData(nameof(SerilogVersions))]
    public Task Serilog_push_property_is_recognized(string version)
    {
        return AnalyzerTestHost.VerifyPackageVersionAsync(
            /*lang=csharp*/ """
            using Serilog.Context;
            class C
            {
                void M()
                {
                    using (LogContext.PushProperty({|AASL0010:"test"|}, 1)) { }
                }
            }
            """,
            PackageVersionMatrix.SerilogId,
            version);
    }

    [Theory]
    [MemberData(nameof(NLogVersions))]
    public Task NLog_named_property_is_recognized(string version)
    {
        return AnalyzerTestHost.VerifyPackageVersionAsync(
            /*lang=csharp*/ """
            using NLog;
            class C
            {
                void M(Logger logger)
                {
                    logger.Info("{|AASL0009:{myProperty}|}", 1);
                }
            }
            """,
            PackageVersionMatrix.NLogId,
            version);
    }

    [Theory]
    [MemberData(nameof(MelVersions))]
    public Task Microsoft_extensions_logging_named_property_is_recognized(string version)
    {
        return AnalyzerTestHost.VerifyPackageVersionAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger)
                {
                    logger.LogInformation("{|AASL0009:{myProperty}|}", 1);
                }
            }
            """,
            PackageVersionMatrix.MelId,
            version);
    }

    [Theory]
    [MemberData(nameof(MelVersions))]
    public Task LoggerMessage_classic_constructor_is_recognized(string version)
    {
        return AnalyzerTestHost.VerifyPackageVersionAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(1, LogLevel.Information, "Processing {|AASL0009:{orderId}|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            PackageVersionMatrix.MelId,
            version);
    }

    [Theory]
    [MemberData(nameof(ZLoggerFormatStringVersions))]
    public Task ZLogger_format_string_named_property_is_recognized(string version)
    {
        return AnalyzerTestHost.VerifyPackageVersionAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            using ZLogger;
            class A
            {
                public A(ILogger<A> log)
                {
                    log.ZLogInformation("{|AASL0009:{myProperty}|}", 1);
                }
            }
            """,
            PackageVersionMatrix.ZLoggerId,
            version);
    }

    [Fact]
    public async Task ZLogger_2_interpolated_call_compiles_without_template_rules()
    {
        var source = /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            using ZLogger;
            class A
            {
                public A(ILogger<A> log)
                {
                    var name = "n";
                    log.ZLogInformation($"Hello {name}");
                }
            }
            """;
        var references = NuGetPackageResolver.GetReferences(
            (PackageVersionMatrix.ZLoggerId, PackageVersionMatrix.ZLoggerLatest));
        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(
            source,
            references: references,
            requireSuccessfulCompilation: true);
        Assert.DoesNotContain(diagnostics, d => d.Id.StartsWith("AASL", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ZLogger_2_message_attribute_is_not_treated_as_logger_message()
    {
        var source = /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            using ZLogger;
            static partial class Log
            {
                [ZLoggerMessage(LogLevel.Information, "Hello {name}")]
                public static partial void Hello(ILogger logger, string name);
            }
            """;
        var references = NuGetPackageResolver.GetReferences(
            (PackageVersionMatrix.ZLoggerId, PackageVersionMatrix.ZLoggerLatest));
        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(
            source,
            references: references,
            requireSuccessfulCompilation: true);
        Assert.DoesNotContain(diagnostics, d => d.Id is "AASL0009" or "AASL0011");
    }

    [Fact]
    public void Test_project_package_pins_are_in_the_matrix()
    {
        var csproj = File.ReadAllText(
            Path.Combine(NuGetPackageResolver.FindRepoRoot(),
                "test",
                "Alexaka1.Analyzers.StructuredLogging.Tests",
                "Alexaka1.Analyzers.StructuredLogging.Tests.csproj"));
        Assert.Contains($"Version=\"{PackageVersionMatrix.SerilogCurrent}\"", csproj, StringComparison.Ordinal);
        Assert.Contains($"Version=\"{PackageVersionMatrix.NLog5}\"", csproj, StringComparison.Ordinal);
        Assert.Contains($"Version=\"{PackageVersionMatrix.MelCurrent}\"", csproj, StringComparison.Ordinal);
        Assert.Contains($"Version=\"{PackageVersionMatrix.ZLogger1}\"", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolver_loads_compile_asset_for_pinned_serilog()
    {
        var assemblies = NuGetPackageResolver.GetCompileAssemblies(
            PackageVersionMatrix.SerilogId,
            PackageVersionMatrix.SerilogCurrent);
        Assert.Contains(assemblies, path =>
            path.EndsWith("Serilog.dll", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<object[]> Rows(IEnumerable<string> versions) =>
        versions.Select(version => new object[] { version });
}
