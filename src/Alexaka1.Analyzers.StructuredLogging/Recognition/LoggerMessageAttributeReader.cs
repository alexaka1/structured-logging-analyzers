using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alexaka1.Analyzers.StructuredLogging.Recognition;

internal readonly struct LoggerMessageTemplate
{
    public LoggerMessageTemplate(AttributeSyntax attribute, ExpressionSyntax? expression)
    {
        Attribute = attribute;
        Expression = expression;
    }

    public AttributeSyntax Attribute { get; }

    public ExpressionSyntax? Expression { get; }
}

internal static class LoggerMessageAttributeReader
{
    public static bool TryGet(
        IMethodSymbol method,
        KnownSymbols known,
        CancellationToken cancellationToken,
        out LoggerMessageTemplate template)
    {
        template = default;
        foreach (var attribute in method.GetAttributes())
        {
            if (!LoggerMessageParameterMapper.IsLoggerMessageAttribute(attribute.AttributeClass, known))
            {
                continue;
            }

            var syntax = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) as AttributeSyntax;
            if (syntax is null)
            {
                continue;
            }

            var expression = FindMessageExpression(attribute, syntax);
            template = new LoggerMessageTemplate(syntax, expression);
            return true;
        }

        return false;
    }

    private static ExpressionSyntax? FindMessageExpression(AttributeData data, AttributeSyntax syntax)
    {
        if (syntax.ArgumentList is null)
        {
            return null;
        }

        var arguments = syntax.ArgumentList.Arguments;
        foreach (var argument in arguments)
        {
            if (argument.NameEquals?.Name.Identifier.ValueText == "Message")
            {
                return argument.Expression;
            }
        }

        var ctor = data.AttributeConstructor;
        if (ctor is null)
        {
            return InferLastStringArgument(arguments);
        }

        var messageIndex = -1;
        for (var i = 0; i < ctor.Parameters.Length; i++)
        {
            if (string.Equals(ctor.Parameters[i].Name, "message", StringComparison.OrdinalIgnoreCase))
            {
                messageIndex = i;
                break;
            }
        }

        if (messageIndex < 0)
        {
            return null;
        }

        foreach (var argument in arguments)
        {
            if (argument.NameEquals is not null)
            {
                continue;
            }

            var colon = argument.NameColon?.Name.Identifier.ValueText;
            if (colon is "message" or "Message")
            {
                return argument.Expression;
            }
        }

        var positional = 0;
        foreach (var argument in arguments)
        {
            if (argument.NameEquals is not null || argument.NameColon is not null)
            {
                continue;
            }

            if (positional == messageIndex)
            {
                return argument.Expression;
            }

            positional++;
        }

        return null;
    }

    private static ExpressionSyntax? InferLastStringArgument(SeparatedSyntaxList<AttributeArgumentSyntax> arguments)
    {
        ExpressionSyntax? last = null;
        foreach (var argument in arguments)
        {
            if (argument.NameEquals is not null)
            {
                continue;
            }

            last = argument.Expression;
        }

        return last;
    }
}
