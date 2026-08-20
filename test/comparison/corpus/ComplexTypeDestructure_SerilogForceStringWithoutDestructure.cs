// Fixture: ComplexTypeDestructure/SerilogForceStringWithoutDestructure.cs
using System;
using Serilog;

namespace Comparison_ComplexTypeDestructure_SerilogForceStringWithoutDestructure
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{$MyProperty}", new Random());
        }
    }
}
