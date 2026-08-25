using Microsoft.CodeAnalysis.CSharp.Syntax;
using Alexaka1.Analyzers.StructuredLogging.Parsing;

namespace Alexaka1.Analyzers.StructuredLogging.Recognition;

internal static class PropertyArgumentMapper
{
    public static ExpressionSyntax? ArgumentForHole(
        IReadOnlyList<BoundTemplateArgument> arguments,
        BoundTemplateArgument template,
        int holeIndex)
    {
        var later = new List<BoundTemplateArgument>();
        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            if (argument.Ordinal > template.Ordinal)
            {
                later.Add(argument);
            }
        }

        later.Sort((a, b) => a.Ordinal.CompareTo(b.Ordinal));
        if (later.Count == 1 && later[0].Parameter.IsParams)
        {
            var elements = GetArrayElements(later[0].Expression);
            if (elements.Count > 0)
            {
                return holeIndex >= 0 && holeIndex < elements.Count ? elements[holeIndex] : null;
            }
        }

        if (holeIndex < 0 || holeIndex >= later.Count)
        {
            return null;
        }

        return later[holeIndex].Expression;
    }

    public static ExpressionSyntax? ArgumentForNamedHole(
        IReadOnlyList<BoundTemplateArgument> arguments,
        BoundTemplateArgument template,
        PropertyHole[] namedProperties,
        PropertyHole hole)
    {
        var index = -1;
        for (var i = 0; i < namedProperties.Length; i++)
        {
            if (namedProperties[i].StartIndex == hole.StartIndex &&
                namedProperties[i].Length == hole.Length)
            {
                index = i;
                break;
            }
        }

        return index < 0 ? null : ArgumentForHole(arguments, template, index);
    }

    private static List<ExpressionSyntax> GetArrayElements(ExpressionSyntax expression)
    {
        var elements = new List<ExpressionSyntax>();
        switch (expression)
        {
            case ArrayCreationExpressionSyntax array when array.Initializer != null:
                foreach (var item in array.Initializer.Expressions)
                {
                    elements.Add(item);
                }

                break;
            case ImplicitArrayCreationExpressionSyntax implicitArray:
                foreach (var item in implicitArray.Initializer.Expressions)
                {
                    elements.Add(item);
                }

                break;
            case InitializerExpressionSyntax initializer:
                foreach (var item in initializer.Expressions)
                {
                    elements.Add(item);
                }

                break;
        }

        return elements;
    }
}
