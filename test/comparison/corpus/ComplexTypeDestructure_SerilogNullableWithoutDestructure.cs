// Fixture: ComplexTypeDestructure/SerilogNullableWithoutDestructure.cs
using System;
using Serilog;

namespace Comparison_ComplexTypeDestructure_SerilogNullableWithoutDestructure
{
    public static class Program
    {
        public static void Main()
        {
        	int? a = 1;
            Log.Logger.Information("{$MyProperty}", a);
        }
    }
}
