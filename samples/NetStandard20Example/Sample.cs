using System;

using Serilog;

public static class Sample
{
    public static void LogIt()
    {
        Log.Logger.Information("Hello {Name}", "world");
        // Intentional AASL0009: build-time diagnostic coverage for this TFM.
        Log.Logger.Information("Hello {name}", "world");
        // Intentional AASL0005: exceptions belong in the exception argument.
        Log.Logger.Error("Failed {Exception}", new InvalidOperationException("Sample failure"));
        // Intentional AASL0008: positional holes should have names on this TFM too.
        Log.Logger.Information("Order {0}", 1);
        // Intentional AASL0011: event messages should not end in a period.
        Log.Logger.Information("Finished.");
    }
}
