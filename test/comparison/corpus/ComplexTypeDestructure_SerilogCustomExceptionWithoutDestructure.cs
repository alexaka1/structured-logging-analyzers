// Fixture: ComplexTypeDestructure/SerilogCustomExceptionWithoutDestructure.cs
using System;
using Serilog;

namespace Comparison_ComplexTypeDestructure_SerilogCustomExceptionWithoutDestructure
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Error(new MyException(), "{MyProperty}", new Random());
        }
    }

    public class MyException : Exception
    {
    }
}
