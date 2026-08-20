// Fixture: ComplexTypeDestructure/SerilogDictionaryWithoutDestructure.cs
using System;
using System.Collections.Generic;
using Serilog;

namespace Comparison_ComplexTypeDestructure_SerilogDictionaryWithoutDestructure
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{$MyProperty}", new Dictionary<int, string>());
        }
    }
}
