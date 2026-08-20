// Copyright (c) 2026 alexaka1

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alexaka1.Analyzers.StructuredLogging.Recognition;

internal readonly struct BoundTemplateArgument
{
    public BoundTemplateArgument(
        IParameterSymbol parameter,
        ArgumentSyntax argument,
        ExpressionSyntax expression,
        int ordinal)
    {
        Parameter = parameter;
        Argument = argument;
        Expression = expression;
        Ordinal = ordinal;
    }

    public IParameterSymbol Parameter { get; }

    public ArgumentSyntax Argument { get; }

    public ExpressionSyntax Expression { get; }

    public int Ordinal { get; }
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
        if (operation is Microsoft.CodeAnalysis.Operations.IInvocationOperation invocationOp)
        {
            foreach (var argumentOp in invocationOp.Arguments)
            {
                if (argumentOp.Parameter is null || argumentOp.IsImplicit)
                {
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
