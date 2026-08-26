using Microsoft.Extensions.Logging;
using Serilog;

var logger = Log.Logger;
logger.Information("Hello {Name}", "world");
// Intentional AASL0009: build-time diagnostic coverage for this TFM.
logger.Information("Hello {name}", "world");

internal sealed class Worker(ILogger<Worker> log)
{
    private readonly ILogger<Worker> _log = log;
}
