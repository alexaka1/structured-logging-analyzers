// Fixture: ComplexTypeDestructure/SerilogContextExplicitDestructure.cs
using System;
using Serilog;
using Serilog.Context;

namespace Comparison_ComplexTypeDestructure_SerilogContextExplicitDestructure
{
    public static class Program
    {
        public static void Main()
        {
            LogContext.PushProperty("Test", new Random(), true);
        }
    }
}
