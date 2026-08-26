using System.Xml.Linq;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

/// <summary>
/// Consumer library versions compiled as metadata references for analyzer tests.
/// The test project still PackageReferences a single current version for
/// <c>typeof</c>-based default hosts; this matrix restores additional versions
/// without loading them into the test process. The current pin is read from
/// that test project so minor/patch Renovate bumps stay in the matrix.
/// <c>*Latest</c> is the always-run latest-stable suite (including majors).
/// Renovate's regex manager owns those constants. Historical floors stay here.
/// </summary>
internal static class PackageVersionMatrix
{
    public const string SerilogId = "Serilog";
    public const string NLogId = "NLog";
    public const string MelId = "Microsoft.Extensions.Logging.Abstractions";
    public const string ZLoggerId = "ZLogger";

    public const string Serilog2 = "2.12.0";
    public const string Serilog3 = "3.1.1";

    // renovate: datasource=nuget depName=Serilog
    public const string SerilogLatest = "4.4.0";

    public const string NLog4 = "4.7.15";
    public const string NLog6 = "6.0.0";

    // renovate: datasource=nuget depName=NLog
    public const string NLogLatest = "6.2.0";

    public const string Mel6 = "6.0.0";
    public const string Mel9 = "9.0.0";

    // renovate: datasource=nuget depName=Microsoft.Extensions.Logging.Abstractions
    public const string MelLatest = "10.0.0";

    public const string ZLogger2 = "2.5.10";

    // renovate: datasource=nuget depName=ZLogger
    public const string ZLoggerLatest = "2.5.10";

    public static string[] Serilog => Unique(Serilog2, Serilog3, TestProjectVersion(SerilogId), SerilogLatest);

    public static string[] NLog => Unique(NLog4, TestProjectVersion(NLogId), NLog6, NLogLatest);

    public static string[] MicrosoftExtensionsLogging => Unique(Mel6, TestProjectVersion(MelId), Mel9, MelLatest);

    public static string[] ZLoggerFormatString => Unique(TestProjectVersion(ZLoggerId));

    public static string[] ZLoggerInterpolated => Unique(ZLogger2, ZLoggerLatest);

    /// <summary>
    /// Gets the version pinned for a package in the test project.
    /// </summary>
    /// <param name="packageId">The package identifier.</param>
    /// <returns>The package version specified by the test project.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the test project has no reference for the specified package.</exception>
    public static string TestProjectVersion(string packageId)
    {
        if (!TestProjectPins.Value.TryGetValue(packageId, out var version))
        {
            throw new InvalidOperationException($"Test project has no PackageReference for '{packageId}'.");
        }

        return version;
    }

    private static readonly Lazy<Dictionary<string, string>> TestProjectPins = new(ReadTestProjectPins);

    /// <summary>
    /// Loads pinned versions for supported packages from the test project file.
    /// </summary>
    /// <returns>A case-insensitive mapping of package IDs to their pinned versions.</returns>
    private static Dictionary<string, string> ReadTestProjectPins()
    {
        var path = Path.Combine(
            NuGetPackageResolver.FindRepoRoot(),
            "test",
            "Alexaka1.Analyzers.StructuredLogging.Tests",
            "Alexaka1.Analyzers.StructuredLogging.Tests.csproj");
        var document = XDocument.Load(path);
        var pins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in document.Descendants("PackageReference"))
        {
            var id = (string?)reference.Attribute("Include");
            var version = (string?)reference.Attribute("Version");
            if (id is SerilogId or NLogId or MelId or ZLoggerId && !string.IsNullOrEmpty(version))
            {
                pins[id] = version;
            }
        }

        return pins;
    }

    /// <summary>
    /// Removes duplicate version values while preserving their first-occurrence order.
    /// </summary>
    /// <param name="versions">The version values to deduplicate.</param>
    /// <returns>The version values with case-insensitive duplicates removed.</returns>
    private static string[] Unique(params string[] versions)
    {
        var unique = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var version in versions)
        {
            if (seen.Add(version))
            {
                unique.Add(version);
            }
        }

        return unique.ToArray();
    }
}
