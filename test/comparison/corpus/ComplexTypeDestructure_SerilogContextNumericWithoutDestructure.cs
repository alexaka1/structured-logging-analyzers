// Fixture: ComplexTypeDestructure/SerilogContextNumericWithoutDestructure.cs
using System;
using Serilog;
using Serilog.Context;

namespace Comparison_ComplexTypeDestructure_SerilogContextNumericWithoutDestructure
{
    public static class Program
    {
        public static void Main()
        {
            LogContext.PushProperty("Test", 1);
        }
    }
}
