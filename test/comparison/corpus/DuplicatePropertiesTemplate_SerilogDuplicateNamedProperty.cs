// Fixture: DuplicatePropertiesTemplate/SerilogDuplicateNamedProperty.cs
using Serilog;

namespace Comparison_DuplicatePropertiesTemplate_SerilogDuplicateNamedProperty
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{Test} {Test}", 1, 2);
        }
    }
}
