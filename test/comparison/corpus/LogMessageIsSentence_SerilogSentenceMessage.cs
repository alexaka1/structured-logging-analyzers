// Fixture: LogMessageIsSentence/SerilogSentenceMessage.cs
using Serilog;

namespace Comparison_LogMessageIsSentence_SerilogSentenceMessage
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("Hello {Name}.", "World");
        }
    }
}
