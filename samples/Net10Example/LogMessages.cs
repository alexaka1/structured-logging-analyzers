using Microsoft.Extensions.Logging;

internal static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Processing {OrderId}")]
    public static partial void ProcessingOrder(ILogger logger, int orderId);

    // Intentional AASL0009: generated templates use PascalCase property names.
    // Intentional AASL0011: generated event messages should not end in a period.
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Processing {orderId}.")]
    public static partial void ProcessingOrderLegacyName(this ILogger logger, int orderId);

    // Intentional AASL0009: positional attribute arguments still check property names.
    [LoggerMessage(3, LogLevel.Warning, "Retry {attempt}")]
    public static partial void Retry(ILogger logger, int attempt);

    // Intentional AASL0009: dynamic log levels still check property names.
    [LoggerMessage("Dynamic {item}")]
    public static partial void Dynamic(ILogger logger, LogLevel level, string item);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug)]
    public static partial void OmittedMessage(ILogger logger);

    // Intentional AASL0009: formatted holes still check property names.
    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Saw {value:E}")]
    public static partial void Saw(ILogger logger, double value);
}

internal sealed partial class FieldWorker
{
    private readonly ILogger _logger;

    public FieldWorker(ILogger logger) => _logger = logger;

    // Intentional AASL0009: field-backed generated loggers check property names.
    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Field {orderId}")]
    public partial void FromField(int orderId);
}

internal sealed partial class PrimaryWorker(ILogger logger)
{
    // Intentional AASL0009: primary-constructor loggers check property names.
    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Primary {orderId}")]
    public partial void FromPrimary(int orderId);
}

internal static class DefineSamples
{
    // Intentional AASL0009: Define templates use PascalCase property names.
    // Intentional AASL0011: Define event messages should not end in a period.
    private static readonly Action<ILogger, int, Exception?> s_define =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(20), "Define {orderId}.");

    // Intentional AASL0009: DefineScope templates use PascalCase property names.
    private static readonly Func<ILogger, string, IDisposable?> s_scope =
        LoggerMessage.DefineScope<string>("Scope {name}");
}
