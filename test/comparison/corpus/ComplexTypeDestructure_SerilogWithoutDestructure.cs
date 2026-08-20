// Fixture: ComplexTypeDestructure/SerilogWithoutDestructure.cs
using System;
using Serilog;

namespace Comparison_ComplexTypeDestructure_SerilogWithoutDestructure
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{MyProperty}", new Random());
        }
    }
}
