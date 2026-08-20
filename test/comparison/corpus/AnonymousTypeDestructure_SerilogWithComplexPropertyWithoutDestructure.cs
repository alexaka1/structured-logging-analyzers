// Fixture: AnonymousTypeDestructure/SerilogWithComplexPropertyWithoutDestructure.cs
using Serilog;
using System;

namespace Comparison_AnonymousTypeDestructure_SerilogWithComplexPropertyWithoutDestructure
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{MyProperty}", new { Test = 1, Complex = new Random() });
        }
    }
}
