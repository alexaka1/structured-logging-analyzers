// Fixture: CorrectExceptionPassing/SerilogIncorrectExceptionPassing.cs
using System;
using Serilog;

namespace Comparison_CorrectExceptionPassing_SerilogIncorrectExceptionPassing
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("{One} {Exc}", 1, new Exception());
        }
    }
}
