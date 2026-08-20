// Fixture: CorrectExceptionPassing/SerilogCorrectExceptionPassing.cs
using System;
using Serilog;

namespace Comparison_CorrectExceptionPassing_SerilogCorrectExceptionPassing
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information(new Exception(), "{One}", 1);
        }
    }
}
