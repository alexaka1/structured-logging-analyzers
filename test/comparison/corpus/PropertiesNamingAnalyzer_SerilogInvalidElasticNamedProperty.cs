// Fixture: PropertiesNamingAnalyzer/SerilogInvalidElasticNamedProperty.cs
using Serilog;

namespace Comparison_PropertiesNamingAnalyzer_SerilogInvalidElasticNamedProperty
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{myProperty}", 1);
        }
    }
}
