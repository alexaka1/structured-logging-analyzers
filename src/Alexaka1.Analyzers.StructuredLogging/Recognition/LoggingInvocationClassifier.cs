using System.Collections.Concurrent;
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
        var containing = method.ContainingType;
        return containing != null &&
               containing.ToDisplayString() == "Serilog.Context.LogContext" &&
               method.Name == "PushProperty";
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

    public static IMethodSymbol? ResolveMethod(SemanticModel model, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        var info = model.GetSymbolInfo(invocation, cancellationToken);
        if (info.Symbol is IMethodSymbol method)
        {
            return method;
        }

        foreach (var candidate in info.CandidateSymbols)
        {
            if (candidate is IMethodSymbol candidateMethod)
            {
                return candidateMethod;
            }
        }

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

        if (containing == ZLoggerExtensions ||
            (containing != null && containing.StartsWith("ZLogger.", StringComparison.Ordinal) &&
             method.Name.StartsWith("ZLog", StringComparison.Ordinal)))
        {
            return "format";
        }

        return null;
    }
}
