// Fixture: ComplexTypeDestructure/SerilogEnumerableWithoutDestructure.cs
using System;
using System.Collections;
using System.Collections.Generic;
using Serilog;

namespace Comparison_ComplexTypeDestructure_SerilogEnumerableWithoutDestructure
{
    public static class Program
    {
		public static void Main()
		{
			IEnumerable list = new List<string>() { "test" };
			Log.Logger.Information("{MyProperty}", list);
		}
    }
}
