namespace Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

/// <summary>
/// Consumer library versions compiled as metadata references for analyzer tests.
/// The test project still PackageReferences a single "current" version for
/// <c>typeof</c>-based default hosts; this matrix restores additional versions
/// without loading them into the test process.
/// </summary>
internal static class PackageVersionMatrix
{
    public const string SerilogId = "Serilog";
    public const string NLogId = "NLog";
    public const string MelId = "Microsoft.Extensions.Logging.Abstractions";
    public const string ZLoggerId = "ZLogger";

    public const string Serilog2 = "2.12.0";
    public const string Serilog3 = "3.1.1";
    public const string SerilogCurrent = "4.2.0";

    // renovate: datasource=nuget depName=Serilog
    public const string SerilogLatest = "4.4.0";

    public const string NLog5 = "5.4.0";
    public const string NLog6 = "6.0.0";

    // renovate: datasource=nuget depName=NLog
    public const string NLogLatest = "6.2.0";

    public const string Mel6 = "6.0.0";
    public const string MelCurrent = "8.0.2";
    public const string Mel9 = "9.0.0";

    // renovate: datasource=nuget depName=Microsoft.Extensions.Logging.Abstractions
    public const string MelLatest = "10.0.0";

    public const string ZLogger1 = "1.7.1";

    // renovate: datasource=nuget depName=ZLogger
    public const string ZLoggerLatest = "2.5.10";

    public static readonly string[] Serilog = [Serilog2, Serilog3, SerilogCurrent, SerilogLatest];

    public static readonly string[] NLog = [NLog5, NLog6, NLogLatest];

    public static readonly string[] MicrosoftExtensionsLogging = [Mel6, MelCurrent, Mel9, MelLatest];

    public static readonly string[] ZLoggerFormatString = [ZLogger1];
}
