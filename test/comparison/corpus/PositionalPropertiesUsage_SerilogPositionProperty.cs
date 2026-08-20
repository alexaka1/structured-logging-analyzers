// Fixture: PositionalPropertiesUsage/SerilogPositionProperty.cs
using Serilog;

namespace Comparison_PositionalPropertiesUsage_SerilogPositionProperty
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{0}", 1);
        }
    }
}
