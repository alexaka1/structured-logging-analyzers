// Fixture: AnonymousTypeDestructure/SerilogWithoutDestructure.cs
using Serilog;

namespace Comparison_AnonymousTypeDestructure_SerilogWithoutDestructure
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{MyProperty}", new { Test = 1 });
        }
    }
}
