// Fixture: PropertiesNamingAnalyzer/SerilogInvalidNamedProperty.cs
using Serilog;

namespace Comparison_PropertiesNamingAnalyzer_SerilogInvalidNamedProperty
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{myProperty}", 1);
        }
    }
}
