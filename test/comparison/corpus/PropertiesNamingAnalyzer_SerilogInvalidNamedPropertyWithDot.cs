// Fixture: PropertiesNamingAnalyzer/SerilogInvalidNamedPropertyWithDot.cs
using Serilog;

namespace Comparison_PropertiesNamingAnalyzer_SerilogInvalidNamedPropertyWithDot
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{My.Property}", 1);
        }
    }
}
