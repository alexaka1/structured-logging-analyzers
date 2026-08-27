using Microsoft.CodeAnalysis;

namespace Alexaka1.Analyzers.StructuredLogging.Recognition;

internal sealed class KnownSymbols
{
    private KnownSymbols(
        INamedTypeSymbol? exception,
        INamedTypeSymbol? genericLogger,
        INamedTypeSymbol? logger,
        INamedTypeSymbol? logLevel,
        INamedTypeSymbol? loggerMessageAttribute,
        INamedTypeSymbol? loggerMessage)
    {
        Exception = exception;
        GenericLogger = genericLogger;
        Logger = logger;
        LogLevel = logLevel;
        LoggerMessageAttribute = loggerMessageAttribute;
        LoggerMessage = loggerMessage;
    }

    public INamedTypeSymbol? Exception { get; }

    public INamedTypeSymbol? GenericLogger { get; }

    public INamedTypeSymbol? Logger { get; }

    public INamedTypeSymbol? LogLevel { get; }

    public INamedTypeSymbol? LoggerMessageAttribute { get; }

    public INamedTypeSymbol? LoggerMessage { get; }

    public static KnownSymbols Resolve(Compilation compilation)
    {
        return new KnownSymbols(
            compilation.GetTypeByMetadataName("System.Exception"),
            compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.ILogger`1"),
            compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.ILogger"),
            compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LogLevel"),
            compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LoggerMessageAttribute"),
            compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LoggerMessage"));
    }
}
