// Fixture: ComplexTypeDestructure/SerilogNumericWithoutDestructure.cs
using System;
using Serilog;

namespace Comparison_ComplexTypeDestructure_SerilogNumericWithoutDestructure
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{$MyProperty}", 3);
        }
    }
}
