// Fixture: PropertiesNamingAnalyzer/SerilogInvalidSyntax.cs
using Serilog;

namespace Comparison_PropertiesNamingAnalyzer_SerilogInvalidSyntax
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information(%"{MyProperty}", 1);
        }
    }
}
