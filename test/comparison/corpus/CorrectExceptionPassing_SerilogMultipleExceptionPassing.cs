// Fixture: CorrectExceptionPassing/SerilogMultipleExceptionPassing.cs
using System;
using Serilog;

namespace Comparison_CorrectExceptionPassing_SerilogMultipleExceptionPassing
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information(new Exception(), "{One} {OtherException}", 1, new Exception());
        }
    }
}
