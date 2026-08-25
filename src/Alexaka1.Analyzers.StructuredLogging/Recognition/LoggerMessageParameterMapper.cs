using Microsoft.CodeAnalysis;

namespace Alexaka1.Analyzers.StructuredLogging.Recognition;

internal readonly struct LoggerMessageParameters
{
    public LoggerMessageParameters(
        IParameterSymbol? logger,
        IParameterSymbol? logLevel,
        IParameterSymbol? exception,
        ImmutableArray<IParameterSymbol> templateParameters)
    {
        Logger = logger;
        LogLevel = logLevel;
        Exception = exception;
        TemplateParameters = templateParameters;
    }

    public IParameterSymbol? Logger { get; }

    public IParameterSymbol? LogLevel { get; }

    public IParameterSymbol? Exception { get; }

    public ImmutableArray<IParameterSymbol> TemplateParameters { get; }
}

internal static class LoggerMessageParameterMapper
{
    public static LoggerMessageParameters Classify(IMethodSymbol method, KnownSymbols known)
    {
        IParameterSymbol? logger = null;
        IParameterSymbol? logLevel = null;
        IParameterSymbol? exception = null;
        var template = ImmutableArray.CreateBuilder<IParameterSymbol>();

        foreach (var parameter in method.Parameters)
        {
            if (logger is null && IsLogger(parameter.Type, known))
            {
                logger = parameter;
                continue;
            }

            if (logLevel is null && IsLogLevel(parameter.Type, known))
            {
                logLevel = parameter;
                continue;
            }

            if (exception is null && IsException(parameter.Type, known))
            {
                exception = parameter;
                continue;
            }

            template.Add(parameter);
        }

        return new LoggerMessageParameters(logger, logLevel, exception, template.ToImmutable());
    }

    public static IParameterSymbol? FindByPlaceholder(LoggerMessageParameters parameters, string propertyName)
    {
        IParameterSymbol? match = null;
        foreach (var parameter in parameters.TemplateParameters)
        {
            if (!NamesMatch(parameter.Name, propertyName))
            {
                continue;
            }

            if (match is not null)
            {
                return null;
            }

            match = parameter;
        }

        return match;
    }

    public static bool IsSpecialPlaceholder(LoggerMessageParameters parameters, string propertyName)
    {
        return Matches(parameters.Logger, propertyName) ||
               Matches(parameters.LogLevel, propertyName) ||
               Matches(parameters.Exception, propertyName);
    }

    public static bool NamesMatch(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsLogger(ITypeSymbol type, KnownSymbols known)
    {
        if (known.Logger is not null &&
            (SymbolEqualityComparer.Default.Equals(type, known.Logger) ||
             Implements(type, known.Logger)))
        {
            return true;
        }

        return type.ToDisplayString() == "Microsoft.Extensions.Logging.ILogger" ||
               ImplementsDisplay(type, "Microsoft.Extensions.Logging.ILogger");
    }

    public static bool IsLogLevel(ITypeSymbol type, KnownSymbols known)
    {
        if (known.LogLevel is not null && SymbolEqualityComparer.Default.Equals(type, known.LogLevel))
        {
            return true;
        }

        return type.ToDisplayString() == "Microsoft.Extensions.Logging.LogLevel";
    }

    public static bool IsException(ITypeSymbol type, KnownSymbols known)
    {
        if (known.Exception is null)
        {
            return false;
        }

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, known.Exception))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsLoggerMessageAttribute(INamedTypeSymbol? type, KnownSymbols known)
    {
        if (type is null)
        {
            return false;
        }

        if (known.LoggerMessageAttribute is not null &&
            SymbolEqualityComparer.Default.Equals(type, known.LoggerMessageAttribute))
        {
            return true;
        }

        return type.ToDisplayString() == "Microsoft.Extensions.Logging.LoggerMessageAttribute";
    }

    public static bool IsLoggerMessageDefine(IMethodSymbol method, KnownSymbols known)
    {
        var containing = method.ContainingType;
        if (containing is null)
        {
            return false;
        }

        var isType = known.LoggerMessage is not null
            ? SymbolEqualityComparer.Default.Equals(containing, known.LoggerMessage)
            : containing.ToDisplayString() == "Microsoft.Extensions.Logging.LoggerMessage";
        return isType && (method.Name is "Define" or "DefineScope");
    }

    private static bool Matches(IParameterSymbol? parameter, string propertyName)
    {
        return parameter is not null && NamesMatch(parameter.Name, propertyName);
    }

    private static bool Implements(ITypeSymbol type, INamedTypeSymbol iface)
    {
        foreach (var candidate in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate, iface))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ImplementsDisplay(ITypeSymbol type, string display)
    {
        foreach (var candidate in type.AllInterfaces)
        {
            if (candidate.ToDisplayString() == display)
            {
                return true;
            }
        }

        return false;
    }
}
