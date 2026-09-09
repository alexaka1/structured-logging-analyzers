using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Context;

var logger = Log.Logger;
logger.Information("Hello {Name}", "world");
// Intentional AASL0009: build-time diagnostic coverage for this TFM.
logger.Information("Hello {name}", "world");

// Intentional AASL0001: anonymous values need destructuring.
logger.Information("Position {Position}", new { X = 1, Y = 2 });
// Intentional AASL0002: complex values need destructuring.
logger.Information("Order {Order}", new Order());
// Intentional AASL0003: complex context values need destructuring.
using var orderContext = LogContext.PushProperty("Order", new Order());
// Intentional AASL0010: context property names use PascalCase.
using var userContext = LogContext.PushProperty("userId", 1);
// Intentional AASL0005: exceptions belong in the exception argument.
logger.Error("Failed {Exception}", new InvalidOperationException("Sample failure"));
// Intentional AASL0006: duplicate names lose distinct property values.
logger.Information("Orders {OrderId} {OrderId}", 1, 2);
var orderId = 1;
// Intentional AASL0007: interpolation creates a nonconstant template.
logger.Information($"Order {orderId}");
// Intentional AASL0008: positional holes should have names.
logger.Information("Order {0}", orderId);

internal sealed class Worker(ILogger<Worker> log)
{
    private readonly ILogger<Worker> _log = log;
}

internal sealed class Order;

internal sealed class OrderWorker
{
    // Intentional AASL0004: the logger category should match OrderWorker.
    public OrderWorker(ILogger<Worker> log)
    {
    }
}
