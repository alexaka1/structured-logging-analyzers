using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Frameworks;

public sealed class PackageVersionMatrixTests
{
    public static TheoryData<string> SerilogVersions() => Theory(PackageVersionMatrix.Serilog);

    public static TheoryData<string> NLogVersions() => Theory(PackageVersionMatrix.NLog);

    public static TheoryData<string> MelVersions() => Theory(PackageVersionMatrix.MicrosoftExtensionsLogging);

    public static TheoryData<string> ZLoggerFormatStringVersions() => Theory(PackageVersionMatrix.ZLoggerFormatString);

    public static TheoryData<string> ZLoggerInterpolatedVersions() => Theory(PackageVersionMatrix.ZLoggerInterpolated);

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
                    logger.Info("{|AASL0009:{myProperty}|}", System.Guid.NewGuid());
                }
            }
            """,
            PackageVersionMatrix.NLogId,
            version);
    }

    [Fact]
    public async Task NLog_4_primitive_overload_lacks_template_attribute()
    {
        var source = /*lang=csharp*/ """
            using NLog;
            class C
            {
                void M(Logger logger)
                {
                    logger.Info("{myProperty}", 1);
                }
            }
            """;
        var references = NuGetPackageResolver.GetReferences(
            (PackageVersionMatrix.NLogId, PackageVersionMatrix.NLog4));
        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(
            source,
            references: references,
            requireSuccessfulCompilation: true);
        Assert.DoesNotContain(diagnostics, d => d.Id == "AASL0009");
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

    [Theory]
    [MemberData(nameof(ZLoggerInterpolatedVersions))]
    public async Task ZLogger_interpolated_call_compiles_without_template_rules(string version)
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
        var references = NuGetPackageResolver.GetReferences((PackageVersionMatrix.ZLoggerId, version));
        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(
            source,
            references: references,
            requireSuccessfulCompilation: true);
        Assert.DoesNotContain(diagnostics, d => d.Id.StartsWith("AASL", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(ZLoggerInterpolatedVersions))]
    public async Task ZLogger_message_attribute_is_not_treated_as_logger_message(string version)
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
        var references = NuGetPackageResolver.GetReferences((PackageVersionMatrix.ZLoggerId, version));
        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(
            source,
            references: references,
            requireSuccessfulCompilation: true);
        Assert.DoesNotContain(diagnostics, d => d.Id is "AASL0009" or "AASL0011");
    }

    [Fact]
    public void Matrix_includes_test_project_pins()
    {
        Assert.Contains(
            PackageVersionMatrix.TestProjectVersion(PackageVersionMatrix.SerilogId),
            PackageVersionMatrix.Serilog);
        Assert.Contains(
            PackageVersionMatrix.TestProjectVersion(PackageVersionMatrix.NLogId),
            PackageVersionMatrix.NLog);
        Assert.Contains(
            PackageVersionMatrix.TestProjectVersion(PackageVersionMatrix.MelId),
            PackageVersionMatrix.MicrosoftExtensionsLogging);
        Assert.Contains(
            PackageVersionMatrix.TestProjectVersion(PackageVersionMatrix.ZLoggerId),
            PackageVersionMatrix.ZLoggerFormatString);
    }

    [Fact]
    public void Resolver_loads_compile_asset_for_test_project_serilog()
    {
        var assemblies = NuGetPackageResolver.GetCompileAssemblies(
            PackageVersionMatrix.SerilogId,
            PackageVersionMatrix.TestProjectVersion(PackageVersionMatrix.SerilogId));
        Assert.Contains(assemblies, path =>
            path.EndsWith("Serilog.dll", StringComparison.OrdinalIgnoreCase));
    }

    private static TheoryData<string> Theory(IEnumerable<string> versions)
    {
        var data = new TheoryData<string>();
        foreach (var version in versions)
        {
            data.Add(version);
        }

        return data;
    }
}
