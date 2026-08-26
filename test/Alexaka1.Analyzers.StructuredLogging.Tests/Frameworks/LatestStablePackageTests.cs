using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Frameworks;

/// <summary>
/// Always-run suite against nuget.org latest stable of each logging library,
/// including new majors. Versions come from <c>*Latest</c> constants that
/// Renovate updates. The test project <c>PackageReference</c> pins stay frozen
/// on majors so the default <c>typeof</c> host does not jump.
/// </summary>
public sealed class LatestStablePackageTests
{
    [Fact]
    public void Latest_packages_restore()
    {
        Assert.Contains(
            NuGetPackageResolver.GetCompileAssemblies(
                PackageVersionMatrix.SerilogId,
                PackageVersionMatrix.SerilogLatest),
            path => path.EndsWith("Serilog.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            NuGetPackageResolver.GetCompileAssemblies(
                PackageVersionMatrix.NLogId,
                PackageVersionMatrix.NLogLatest),
            path => path.EndsWith("NLog.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            NuGetPackageResolver.GetCompileAssemblies(
                PackageVersionMatrix.MelId,
                PackageVersionMatrix.MelLatest),
            path => path.EndsWith(
                "Microsoft.Extensions.Logging.Abstractions.dll",
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            NuGetPackageResolver.GetCompileAssemblies(
                PackageVersionMatrix.ZLoggerId,
                PackageVersionMatrix.ZLoggerLatest),
            path => path.EndsWith("ZLogger.dll", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public Task Serilog_latest_named_property_is_recognized()
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
            PackageVersionMatrix.SerilogLatest);
    }

    [Fact]
    public Task NLog_latest_named_property_is_recognized()
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
            PackageVersionMatrix.NLogLatest);
    }

    [Fact]
    public Task Microsoft_extensions_logging_latest_named_property_is_recognized()
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
            PackageVersionMatrix.MelLatest);
    }

    [Fact]
    public Task LoggerMessage_latest_classic_constructor_is_recognized()
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
            PackageVersionMatrix.MelLatest);
    }

    [Fact]
    public async Task ZLogger_latest_interpolated_call_compiles_without_template_rules()
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
}
