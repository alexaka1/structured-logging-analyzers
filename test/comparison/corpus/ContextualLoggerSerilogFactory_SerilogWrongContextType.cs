// Fixture: ContextualLoggerSerilogFactory/SerilogWrongContextType.cs
using Serilog;

namespace Comparison_ContextualLoggerSerilogFactory_SerilogWrongContextType
{
class A
{
    private static readonly ILogger Logger = Logger.ForContext<B>();
}

class B {} 
}
