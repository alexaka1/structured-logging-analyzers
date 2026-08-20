// Fixture: PropertiesNamingAnalyzer/SerilogValidNamedProperty.cs
using Serilog;

namespace Comparison_PropertiesNamingAnalyzer_SerilogValidNamedProperty
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{MyProperty}", 1);
        }
    }
}
