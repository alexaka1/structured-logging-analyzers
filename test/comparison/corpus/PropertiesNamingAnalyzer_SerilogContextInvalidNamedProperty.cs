// Fixture: PropertiesNamingAnalyzer/SerilogContextInvalidNamedProperty.cs
using Serilog;
using Serilog.Context;

namespace Comparison_PropertiesNamingAnalyzer_SerilogContextInvalidNamedProperty
{
    public static class Program
    {
        public static void Main()
        {
            LogContext.PushProperty("test", 1);
        }
    }
}
