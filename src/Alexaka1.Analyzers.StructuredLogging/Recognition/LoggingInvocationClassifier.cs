using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alexaka1.Analyzers.StructuredLogging.Recognition;

internal sealed class LoggingInvocationClassifier
{
    private const string MessageTemplateFormatMethodAttribute = "MessageTemplateFormatMethodAttribute";
    private const string MelExtensions = "Microsoft.Extensions.Logging.LoggerExtensions";
    private const string ZLoggerExtensions = "ZLogger.ZLoggerExtensions";

    private readonly KnownSymbols _known;

    private readonly ConcurrentDictionary<IMethodSymbol, string?> _templateParameterNames =
        new(SymbolEqualityComparer.Default);

    public LoggingInvocationClassifier(KnownSymbols known)
    {
        _known = known;
    }

    public string? GetTemplateParameterName(IMethodSymbol method)
    {
        return _templateParameterNames.GetOrAdd(method.OriginalDefinition, ResolveTemplateParameterName);
    }

    public static bool IsSerilogPushProperty(IMethodSymbol method)
    {
        if (method.Name != "PushProperty")
        {
            return false;
        }

        var containing = method.ContainingType;
        if (containing is null || containing.Name != "LogContext")
        {
            return false;
        }

        var contextNamespace = containing.ContainingNamespace;
        return contextNamespace.Name == "Context" &&
               contextNamespace.ContainingNamespace is
                   { Name: "Serilog", ContainingNamespace.IsGlobalNamespace: true };
    }

    public static bool IsSerilogForContext(IMethodSymbol method)
    {
        return method.Name == "ForContext" &&
               method.TypeParameters.Length == 1 &&
               method.ContainingType != null &&
               (method.ContainingType.ToDisplayString() == "Serilog.ILogger" ||
                method.ContainingType.ToDisplayString() == "Serilog.Log");
    }

    public bool IsGenericMicrosoftLogger(ITypeSymbol type, out ITypeSymbol? typeArgument)
    {
        typeArgument = null;
        if (type is not INamedTypeSymbol named || named.TypeArguments.Length != 1)
        {
            return false;
        }

        if (_known.GenericLogger != null &&
            SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, _known.GenericLogger))
        {
            typeArgument = named.TypeArguments[0];
            return true;
        }

        if (named.OriginalDefinition.ToDisplayString() == "Microsoft.Extensions.Logging.ILogger<TCategoryName>" ||
            (named.OriginalDefinition.MetadataName == "ILogger`1" &&
             named.ContainingNamespace?.ToDisplayString() == "Microsoft.Extensions.Logging"))
        {
            typeArgument = named.TypeArguments[0];
            return true;
        }

        return false;
    }

    public static IMethodSymbol? ResolveMethod(SemanticModel model, InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        return ResolveMethod(model, invocation, cancellationToken, out _, out _);
    }

    public static IMethodSymbol? ResolveMethod(
        SemanticModel model,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken,
        out bool isCandidate,
        out ImmutableArray<ISymbol> candidateSymbols)
    {
        var info = model.GetSymbolInfo(invocation, cancellationToken);
        candidateSymbols = info.CandidateSymbols;
        if (info.Symbol is IMethodSymbol method)
        {
            isCandidate = false;
            return method;
        }

        foreach (var candidate in info.CandidateSymbols)
        {
            if (candidate is IMethodSymbol candidateMethod)
            {
                isCandidate = true;
                return candidateMethod;
            }
        }

        isCandidate = false;
        return null;
    }

    private string? ResolveTemplateParameterName(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            var attrClass = attribute.AttributeClass;
            if (attrClass is null)
            {
                continue;
            }

            if (attrClass.Name != MessageTemplateFormatMethodAttribute)
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is string parameterName)
            {
                return parameterName;
            }
        }

        var containing = method.ContainingType?.ToDisplayString();
        if (containing == MelExtensions)
        {
            return method.Name == "BeginScope" ? "messageFormat" : "message";
        }

        if (containing == "Microsoft.Extensions.Logging.LoggerMessage" &&
            method.Name is "Define" or "DefineScope")
        {
            return "formatString";
        }

        if (IsZLogger(method))
        {
            var messageIsString = false;
            foreach (var parameter in method.Parameters)
            {
                if (parameter.Type.SpecialType != SpecialType.System_String)
                {
                    continue;
                }

                if (parameter.Name == "format")
                {
                    return parameter.Name;
                }

                messageIsString |= parameter.Name == "message";
            }

            return messageIsString ? "message" : null;
        }

        return null;
    }

    public static bool SupportsDestructuringOperator(IMethodSymbol method)
    {
        if (IsMicrosoftLoggerExtensions(method) || IsZLogger(method))
        {
            return false;
        }

        var containing = method.ContainingType?.ToDisplayString();
        return containing != "Microsoft.Extensions.Logging.LoggerMessage";
    }

    private static bool IsMicrosoftLoggerExtensions(IMethodSymbol method)
    {
        return method.ContainingType?.ToDisplayString() == MelExtensions;
    }

    private static bool IsZLogger(IMethodSymbol method)
    {
        var containing = method.ContainingType?.ToDisplayString();
        return containing == ZLoggerExtensions ||
               (containing != null && containing.StartsWith("ZLogger.", StringComparison.Ordinal) &&
                method.Name.StartsWith("ZLog", StringComparison.Ordinal));
    }
}
