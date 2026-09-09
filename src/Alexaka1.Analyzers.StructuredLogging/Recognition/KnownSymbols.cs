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
        INamedTypeSymbol? loggerMessage,
        INamedTypeSymbol? serilogLogger,
        INamedTypeSymbol? serilogLog,
        bool hasAnyLoggingLibrary,
        bool hasMicrosoftLogging,
        bool hasGenericMicrosoftLogger)
    {
        Exception = exception;
        GenericLogger = genericLogger;
        Logger = logger;
        LogLevel = logLevel;
        LoggerMessageAttribute = loggerMessageAttribute;
        LoggerMessage = loggerMessage;
        SerilogLogger = serilogLogger;
        SerilogLog = serilogLog;
        HasAnyLoggingLibrary = hasAnyLoggingLibrary;
        HasMicrosoftLogging = hasMicrosoftLogging;
        HasGenericMicrosoftLogger = hasGenericMicrosoftLogger;
    }

    public INamedTypeSymbol? Exception { get; }

    public INamedTypeSymbol? GenericLogger { get; }

    public INamedTypeSymbol? Logger { get; }

    public INamedTypeSymbol? LogLevel { get; }

    public INamedTypeSymbol? LoggerMessageAttribute { get; }

    public INamedTypeSymbol? LoggerMessage { get; }

    public INamedTypeSymbol? SerilogLogger { get; }

    public INamedTypeSymbol? SerilogLog { get; }

    public bool HasAnyLoggingLibrary { get; }

    public bool HasMicrosoftLogging { get; }

    public bool HasGenericMicrosoftLogger { get; }

    public static KnownSymbols Resolve(Compilation compilation, CancellationToken cancellationToken = default)
    {
        var hasSourceMessageTemplateAttribute = false;
        foreach (var _ in compilation.GetSymbolsWithName(
                     "MessageTemplateFormatMethodAttribute",
                     SymbolFilter.Type,
                     cancellationToken))
        {
            hasSourceMessageTemplateAttribute = true;
            break;
        }

        var genericLoggers = compilation.GetTypesByMetadataName("Microsoft.Extensions.Logging.ILogger`1");
        var loggers = compilation.GetTypesByMetadataName("Microsoft.Extensions.Logging.ILogger");
        var loggerExtensions = compilation.GetTypesByMetadataName("Microsoft.Extensions.Logging.LoggerExtensions");
        var loggerMessages = compilation.GetTypesByMetadataName("Microsoft.Extensions.Logging.LoggerMessage");
        var serilogLoggers = compilation.GetTypesByMetadataName("Serilog.ILogger");
        var serilogAttributes =
            compilation.GetTypesByMetadataName("Serilog.Core.MessageTemplateFormatMethodAttribute");
        var nlogAttributes =
            compilation.GetTypesByMetadataName("NLog.MessageTemplateFormatMethodAttribute");
        var nlogLoggers = compilation.GetTypesByMetadataName("NLog.ILogger");
        var zloggerExtensions = compilation.GetTypesByMetadataName("ZLogger.ZLoggerExtensions");
        var hasMicrosoftLogging =
            loggers.Length > 0 || loggerExtensions.Length > 0 || loggerMessages.Length > 0;
        var hasAnyLoggingLibrary = hasMicrosoftLogging ||
                                   serilogLoggers.Length > 0 ||
                                   serilogAttributes.Length > 0 ||
                                   nlogAttributes.Length > 0 ||
                                   nlogLoggers.Length > 0 ||
                                   zloggerExtensions.Length > 0 ||
                                   hasSourceMessageTemplateAttribute;

        return new KnownSymbols(
            compilation.GetTypeByMetadataName("System.Exception"),
            GetUnambiguousType(genericLoggers),
            GetUnambiguousType(loggers),
            compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LogLevel"),
            compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LoggerMessageAttribute"),
            GetUnambiguousType(loggerMessages),
            GetUnambiguousType(serilogLoggers),
            GetUnambiguousType(compilation.GetTypesByMetadataName("Serilog.Log")),
            hasAnyLoggingLibrary,
            hasMicrosoftLogging,
            genericLoggers.Length > 0);
    }

    private static INamedTypeSymbol? GetUnambiguousType(ImmutableArray<INamedTypeSymbol> types) =>
        types.Length == 1 ? types[0] : null;
}
