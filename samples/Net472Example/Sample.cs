using Serilog;

public static class Sample
{
    public static void LogIt()
    {
        Log.Logger.Information("Hello {Name}", "world");
        // Intentional AASL0009: build-time diagnostic coverage for this TFM.
        Log.Logger.Information("Hello {name}", "world");
    }
}
