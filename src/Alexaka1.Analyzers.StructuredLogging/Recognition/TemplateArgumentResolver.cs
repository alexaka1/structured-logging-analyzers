using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Alexaka1.Analyzers.StructuredLogging.Recognition;

internal readonly struct BoundTemplateArgument
{
    public BoundTemplateArgument(
        IParameterSymbol parameter,
        ArgumentSyntax argument,
        ExpressionSyntax expression,
        int ordinal,
        bool expandedParamsElement = false)
    {
        Parameter = parameter;
        Argument = argument;
        Expression = expression;
        Ordinal = ordinal;
        ExpandedParamsElement = expandedParamsElement;
    }

    public IParameterSymbol Parameter { get; }

    public ArgumentSyntax Argument { get; }

    public ExpressionSyntax Expression { get; }

    public int Ordinal { get; }

    /// <summary>
    /// True when this argument was unpacked from a compiler-synthesized params array
    /// (MEL-style <c>LogInformation("{0}", orderId)</c>), not passed as the params array itself.
    /// </summary>
    public bool ExpandedParamsElement { get; }
}

internal static class TemplateArgumentResolver
{
    public static BoundTemplateArgument? FindTemplate(
        SemanticModel model,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        string templateParameterName,
        CancellationToken cancellationToken)
    {
        var parameter = FindParameter(method, templateParameterName);
        if (parameter is null)
        {
            return null;
        }

        foreach (var mapped in MapArguments(model, invocation, method, cancellationToken))
        {
            if (SymbolEqualityComparer.Default.Equals(mapped.Parameter, parameter) ||
                mapped.Parameter.Name == templateParameterName)
            {
                return mapped;
            }
        }

        return null;
    }

    public static List<BoundTemplateArgument> MapArguments(
        SemanticModel model,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        CancellationToken cancellationToken)
    {
        var results = new List<BoundTemplateArgument>(invocation.ArgumentList.Arguments.Count);
        var operation = model.GetOperation(invocation, cancellationToken);
        if (operation is IInvocationOperation invocationOp)
        {
            foreach (var argumentOp in invocationOp.Arguments)
            {
                if (argumentOp.Parameter is null)
                {
                    continue;
                }

                if (argumentOp.IsImplicit)
                {
                    AddImplicitParamsElements(results, argumentOp);
                    continue;
                }

                if (argumentOp.Syntax is not ArgumentSyntax argumentSyntax)
                {
                    continue;
                }

                results.Add(new BoundTemplateArgument(
                    argumentOp.Parameter,
                    argumentSyntax,
                    argumentSyntax.Expression,
                    argumentOp.Parameter.Ordinal));
            }

            return results;
        }

        // Fallback when IOperation is unavailable: match by name then by position.
        var used = new HashSet<int>();
        var arguments = invocation.ArgumentList.Arguments;
        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            IParameterSymbol? parameter = null;
            if (argument.NameColon != null)
            {
                var name = argument.NameColon.Name.Identifier.ValueText;
                foreach (var candidate in method.Parameters)
                {
                    if (candidate.Name == name)
                    {
                        parameter = candidate;
                        break;
                    }
                }
            }
            else
            {
                for (var p = 0; p < method.Parameters.Length; p++)
                {
                    if (used.Contains(p))
                    {
                        continue;
                    }

                    parameter = method.Parameters[p];
                    break;
                }

                if (parameter != null &&
                    parameter.IsParams &&
                    results.Count >= parameter.Ordinal)
                {
                    // keep params parameter
                }
            }

            if (parameter is null)
            {
                continue;
            }

            used.Add(parameter.Ordinal);
            results.Add(new BoundTemplateArgument(parameter, argument, argument.Expression, parameter.Ordinal));
        }

        return results;
    }

    private static void AddImplicitParamsElements(
        List<BoundTemplateArgument> results,
        IArgumentOperation argumentOp)
    {
        if (argumentOp.Parameter is not { IsParams: true })
        {
            return;
        }

        var value = argumentOp.Value;
        if (value is IConversionOperation conversion)
        {
            value = conversion.Operand;
        }

        if (value is not IArrayCreationOperation { Initializer: { } initializer })
        {
            return;
        }

        foreach (var element in initializer.ElementValues)
        {
            var expression = UnwrapExpression(element);
            if (expression is null)
            {
                continue;
            }

            var argumentSyntax = expression.FirstAncestorOrSelf<ArgumentSyntax>();
            if (argumentSyntax is null)
            {
                continue;
            }

            results.Add(new BoundTemplateArgument(
                argumentOp.Parameter,
                argumentSyntax,
                argumentSyntax.Expression,
                argumentOp.Parameter.Ordinal,
                expandedParamsElement: true));
        }
    }

    private static ExpressionSyntax? UnwrapExpression(IOperation element)
    {
        var current = element;
        while (current is IConversionOperation conversion)
        {
            current = conversion.Operand;
        }

        return current.Syntax as ExpressionSyntax ?? (current.Syntax as ArgumentSyntax)?.Expression;
    }

    private static IParameterSymbol? FindParameter(IMethodSymbol method, string name)
    {
        foreach (var parameter in method.Parameters)
        {
            if (parameter.Name == name)
            {
                return parameter;
            }
        }

        return null;
    }
}
