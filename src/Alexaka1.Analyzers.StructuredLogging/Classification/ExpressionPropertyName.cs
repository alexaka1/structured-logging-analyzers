using System.Text;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alexaka1.Analyzers.StructuredLogging.Classification;

internal static class ExpressionPropertyName
{
    public enum Kind
    {
        Leaf,
        Qualified
    }

    public static string? TrySuggest(ExpressionSyntax expression, PropertyNamingStyle style, Kind kind)
    {
        var parts = CollectNameParts(expression);
        if (parts.Count == 0)
        {
            return null;
        }

        string raw;
        if (kind == Kind.Qualified && parts.Count > 1)
        {
            raw = ConcatenateParts(parts);
        }
        else
        {
            raw = parts[parts.Count - 1];
        }

        var suggested = PropertyNaming.SuggestFromExpression(raw, style);
        return string.IsNullOrEmpty(suggested) || IsPositionalName(suggested) ? null : suggested;
    }

    public static string Fallback(PropertyNamingStyle style)
    {
        var suggested = PropertyNaming.SuggestFromExpression("Value", style);
        return string.IsNullOrEmpty(suggested) ? "Value" : suggested;
    }

    public static string Uniquify(string name, HashSet<string> used)
    {
        if (used.Add(name))
        {
            return name;
        }

        for (var i = 2; i < 1000; i++)
        {
            var candidate = name + i.ToString(CultureInfo.InvariantCulture);
            if (used.Add(candidate))
            {
                return candidate;
            }
        }

        return name + Guid.NewGuid().ToString("N");
    }

    public static bool IsPositionalName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        for (var i = 0; i < name.Length; i++)
        {
            if (!char.IsDigit(name[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static string ConcatenateParts(List<string> parts)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (i == parts.Count - 1 &&
                part.StartsWith("Get", StringComparison.Ordinal) &&
                part.Length > 3 &&
                char.IsUpper(part[3]))
            {
                part = part.Substring(3);
            }

            builder.Append(part);
        }

        return builder.ToString();
    }

    private static List<string> CollectNameParts(ExpressionSyntax expression)
    {
        var parts = new List<string>();
        CollectNameParts(expression, parts);
        return parts;
    }

    private static void CollectNameParts(ExpressionSyntax expression, List<string> parts)
    {
        switch (expression)
        {
            case IdentifierNameSyntax identifier:
                parts.Add(identifier.Identifier.ValueText);
                break;
            case MemberAccessExpressionSyntax member:
                CollectNameParts(member.Expression, parts);
                parts.Add(member.Name.Identifier.ValueText);
                break;
            case ConditionalAccessExpressionSyntax conditionalAccess:
                CollectNameParts(conditionalAccess.Expression, parts);
                CollectNameParts(conditionalAccess.WhenNotNull, parts);
                break;
            case MemberBindingExpressionSyntax memberBinding:
                parts.Add(memberBinding.Name.Identifier.ValueText);
                break;
            case InvocationExpressionSyntax invocation:
                if (invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" } &&
                    invocation.ArgumentList.Arguments.Count == 1)
                {
                    CollectNameParts(invocation.ArgumentList.Arguments[0].Expression, parts);
                    break;
                }

                CollectNameParts(invocation.Expression, parts);
                break;
            case ElementAccessExpressionSyntax element:
                CollectNameParts(element.Expression, parts);
                parts.Add("Item");
                break;
            case ElementBindingExpressionSyntax:
                parts.Add("Item");
                break;
            case ConditionalExpressionSyntax conditional:
                CollectNameParts(conditional.WhenTrue, parts);
                if (parts.Count == 0)
                {
                    CollectNameParts(conditional.WhenFalse, parts);
                }

                break;
            case ParenthesizedExpressionSyntax parenthesized:
                CollectNameParts(parenthesized.Expression, parts);
                break;
            case CastExpressionSyntax cast:
                CollectNameParts(cast.Expression, parts);
                break;
            case AwaitExpressionSyntax awaitExpression:
                CollectNameParts(awaitExpression.Expression, parts);
                break;
            case ObjectCreationExpressionSyntax creation:
                AddTypeName(creation.Type, parts);
                break;
            case ThisExpressionSyntax:
            case BaseExpressionSyntax:
                break;
        }
    }

    private static void AddTypeName(TypeSyntax type, List<string> parts)
    {
        switch (type)
        {
            case IdentifierNameSyntax identifier:
                parts.Add(identifier.Identifier.ValueText);
                break;
            case QualifiedNameSyntax qualified:
                AddTypeName(qualified.Right, parts);
                break;
            case GenericNameSyntax generic:
                parts.Add(generic.Identifier.ValueText);
                break;
            case AliasQualifiedNameSyntax alias:
                AddTypeName(alias.Name, parts);
                break;
            case NullableTypeSyntax nullable:
                AddTypeName(nullable.ElementType, parts);
                break;
            default:
                var text = type.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    parts.Add(text);
                }

                break;
        }
    }
}
