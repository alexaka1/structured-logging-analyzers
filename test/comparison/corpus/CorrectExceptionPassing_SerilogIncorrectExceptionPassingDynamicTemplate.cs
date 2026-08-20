// Fixture: CorrectExceptionPassing/SerilogIncorrectExceptionPassingDynamicTemplate.cs
using System;
using Serilog;

namespace Comparison_CorrectExceptionPassing_SerilogIncorrectExceptionPassingDynamicTemplate
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information($"{DateTime.Now} {{Error}}", new Exception());
        }
    }
}
