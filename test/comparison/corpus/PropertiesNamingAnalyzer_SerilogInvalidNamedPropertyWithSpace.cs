// Fixture: PropertiesNamingAnalyzer/SerilogInvalidNamedPropertyWithSpace.cs
using Serilog;

namespace Comparison_PropertiesNamingAnalyzer_SerilogInvalidNamedPropertyWithSpace
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{My Property}", 1);
        }
    }
}
