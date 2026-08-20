// Fixture: ContextualLoggerSerilogFactory/SerilogCorrectContextType.cs
using Serilog;

namespace Comparison_ContextualLoggerSerilogFactory_SerilogCorrectContextType
{
class A
{
    private static readonly ILogger Logger = Logger.ForContext<A>();
}

class B {} 
}
