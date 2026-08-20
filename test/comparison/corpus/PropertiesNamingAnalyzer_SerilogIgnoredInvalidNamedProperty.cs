// Fixture: PropertiesNamingAnalyzer/SerilogIgnoredInvalidNamedProperty.cs
using Serilog;

namespace Comparison_PropertiesNamingAnalyzer_SerilogIgnoredInvalidNamedProperty
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{MY_IGNORED.Property_}", 1);
        }
    }
}
