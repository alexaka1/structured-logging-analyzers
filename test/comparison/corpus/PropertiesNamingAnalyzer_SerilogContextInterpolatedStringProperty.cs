// Fixture: PropertiesNamingAnalyzer/SerilogContextInterpolatedStringProperty.cs
using Serilog;
using Serilog.Context;

namespace Comparison_PropertiesNamingAnalyzer_SerilogContextInterpolatedStringProperty
{
    public static class Program
    {
        public static void Main()
        {
            var s = "world";
            LogContext.PushProperty($"Hello{s}", 1);
        }
    }
}
