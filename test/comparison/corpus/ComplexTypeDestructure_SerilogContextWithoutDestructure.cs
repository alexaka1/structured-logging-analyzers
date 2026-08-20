// Fixture: ComplexTypeDestructure/SerilogContextWithoutDestructure.cs
using System;
using Serilog;
using Serilog.Context;

namespace Comparison_ComplexTypeDestructure_SerilogContextWithoutDestructure
{
    public static class Program
    {
        public static void Main()
        {
            LogContext.PushProperty("Test", new Random());
        }
    }
}
