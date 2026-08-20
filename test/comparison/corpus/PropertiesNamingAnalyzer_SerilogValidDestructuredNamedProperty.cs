// Fixture: PropertiesNamingAnalyzer/SerilogValidDestructuredNamedProperty.cs
using Serilog;

namespace Comparison_PropertiesNamingAnalyzer_SerilogValidDestructuredNamedProperty
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{@MyProperty}", 1);
        }
    }
}
