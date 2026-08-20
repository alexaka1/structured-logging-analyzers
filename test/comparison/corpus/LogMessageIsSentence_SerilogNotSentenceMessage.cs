// Fixture: LogMessageIsSentence/SerilogNotSentenceMessage.cs
using Serilog;

namespace Comparison_LogMessageIsSentence_SerilogNotSentenceMessage
{
    public static class Program
    {
        public static void Main()
        {
            Log.Logger.Information("Loading {Name}...", "World");
        }
    }
}
