// Copyright (c) 2026 alexaka1

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StructuredLogging.Analyzers.Recognition;

internal readonly struct LoggerMessageTemplate
{
    public LoggerMessageTemplate(AttributeSyntax attribute, ExpressionSyntax? expression, string text)
    {
        Attribute = attribute;
        Expression = expression;
        Text = text;
    }

    public AttributeSyntax Attribute { get; }

    public ExpressionSyntax? Expression { get; }

    public string Text { get; }
}

internal static class LoggerMessageAttributeReader
{
    public static bool TryGet(
        SemanticModel model,
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
            var text = ReadMessageText(model, attribute, expression, cancellationToken) ?? string.Empty;
            template = new LoggerMessageTemplate(syntax, expression, text);
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

    private static string? ReadMessageText(
        SemanticModel model,
        AttributeData data,
        ExpressionSyntax? expression,
        CancellationToken cancellationToken)
    {
        if (expression is not null)
        {
            var constant = model.GetConstantValue(expression, cancellationToken);
            if (constant.HasValue && constant.Value is string fromExpression)
            {
                return fromExpression;
            }
        }

        foreach (var named in data.NamedArguments)
        {
            if (named.Key == "Message" && named.Value.Value is string namedMessage)
            {
                return namedMessage;
            }
        }

        var ctor = data.AttributeConstructor;
        if (ctor is not null)
        {
            for (var i = 0; i < ctor.Parameters.Length && i < data.ConstructorArguments.Length; i++)
            {
                if (string.Equals(ctor.Parameters[i].Name, "message", StringComparison.OrdinalIgnoreCase) &&
                    data.ConstructorArguments[i].Value is string ctorMessage)
                {
                    return ctorMessage;
                }
            }
        }

        for (var i = data.ConstructorArguments.Length - 1; i >= 0; i--)
        {
            if (data.ConstructorArguments[i].Value is string fallback)
            {
                return fallback;
            }
        }

        return string.Empty;
    }
}
