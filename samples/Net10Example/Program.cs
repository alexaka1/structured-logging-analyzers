using Microsoft.Extensions.Logging;
using Serilog;

var logger = Log.Logger;
logger.Information("Hello {Name}", "world");
logger.Information("Hello {name}", "world");

internal sealed class Worker(ILogger<Worker> log)
{
    private readonly ILogger<Worker> _log = log;
}
