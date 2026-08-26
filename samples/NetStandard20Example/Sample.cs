using Serilog;

public static class Sample
{
    public static void LogIt()
    {
        Log.Logger.Information("Hello {Name}", "world");
        Log.Logger.Information("Hello {name}", "world");
    }
}
