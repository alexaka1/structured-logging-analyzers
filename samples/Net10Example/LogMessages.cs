using Microsoft.Extensions.Logging;

internal static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Processing {OrderId}")]
    public static partial void ProcessingOrder(ILogger logger, int orderId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Processing {orderId}.")]
    public static partial void ProcessingOrderLegacyName(this ILogger logger, int orderId);

    [LoggerMessage(3, LogLevel.Warning, "Retry {attempt}")]
    public static partial void Retry(ILogger logger, int attempt);

    [LoggerMessage("Dynamic {item}")]
    public static partial void Dynamic(ILogger logger, LogLevel level, string item);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug)]
    public static partial void OmittedMessage(ILogger logger);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Saw {value:E}")]
    public static partial void Saw(ILogger logger, double value);
}

internal sealed partial class FieldWorker
{
    private readonly ILogger _logger;

    public FieldWorker(ILogger logger) => _logger = logger;

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Field {orderId}")]
    public partial void FromField(int orderId);
}

internal sealed partial class PrimaryWorker(ILogger logger)
{
    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Primary {orderId}")]
    public partial void FromPrimary(int orderId);
}

internal static class DefineSamples
{
    private static readonly Action<ILogger, int, Exception?> s_define =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(20), "Define {orderId}.");

    private static readonly Func<ILogger, string, IDisposable?> s_scope =
        LoggerMessage.DefineScope<string>("Scope {name}");
}
