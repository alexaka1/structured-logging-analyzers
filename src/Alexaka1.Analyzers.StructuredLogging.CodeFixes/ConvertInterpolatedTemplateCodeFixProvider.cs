// Copyright (c) 2026 alexaka1

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Alexaka1.Analyzers.StructuredLogging.Classification;
using Alexaka1.Analyzers.StructuredLogging.Configuration;

namespace Alexaka1.Analyzers.StructuredLogging.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertInterpolatedTemplateCodeFixProvider))]
[Shared]
public sealed class ConvertInterpolatedTemplateCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.TemplateIsNotCompileTimeConstant);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var interpolated = node as InterpolatedStringExpressionSyntax ??
                               node.FirstAncestorOrSelf<InterpolatedStringExpressionSyntax>();
            if (interpolated is null || !HasInterpolation(interpolated))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Convert to compile-time constant message template",
                    ct => ApplyAsync(context.Document, diagnostic, ct),
                    nameof(ConvertInterpolatedTemplateCodeFixProvider)),
                diagnostic);
        }
    }

    private static async Task<Document> ApplyAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
        {
            return document;
        }

        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        var interpolated = node as InterpolatedStringExpressionSyntax ??
                           node.FirstAncestorOrSelf<InterpolatedStringExpressionSyntax>();
        var argument = interpolated?.FirstAncestorOrSelf<ArgumentSyntax>();
        var invocation = argument?.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (interpolated is null || argument is null || invocation is null)
        {
            return document;
        }

        var style = AnalyzerSettings.From(
            document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider,
            interpolated.SyntaxTree).GetNaming(DiagnosticIds.InconsistentTemplatePropertyNaming);

        if (!TryBuild(interpolated, style, out var template, out var valueExpressions))
        {
            return document;
        }

        var literal = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(template));
        var newTemplateArgument = argument.WithExpression(literal);

        var newArguments = invocation.ArgumentList.Arguments.ToList();
        var templateIndex = newArguments.IndexOf(argument);
        if (templateIndex < 0)
        {
            return document;
        }

        newArguments[templateIndex] = newTemplateArgument;
        for (var i = 0; i < valueExpressions.Count; i++)
        {
            newArguments.Insert(templateIndex + 1 + i, SyntaxFactory.Argument(valueExpressions[i]));
        }

        var updatedInvocation = invocation.WithArgumentList(
            invocation.ArgumentList.WithArguments(SyntaxFactory.SeparatedList(newArguments)));
        var updatedRoot = root.ReplaceNode(invocation, updatedInvocation);
        return document.WithSyntaxRoot(updatedRoot);
    }

    private static bool HasInterpolation(InterpolatedStringExpressionSyntax interpolated)
    {
        foreach (var content in interpolated.Contents)
        {
            if (content is InterpolationSyntax)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryBuild(
        InterpolatedStringExpressionSyntax interpolated,
        PropertyNamingStyle style,
        out string template,
        out List<ExpressionSyntax> values)
    {
        var builder = new StringBuilder();
        values = new List<ExpressionSyntax>();
        var used = new HashSet<string>(StringComparer.Ordinal);
        template = string.Empty;

        foreach (var content in interpolated.Contents)
        {
            switch (content)
            {
                case InterpolatedStringTextSyntax text:
                    AppendEscaped(builder, text.TextToken.ValueText);
                    break;
                case InterpolationSyntax interpolation:
                    var name = UniqueName(SuggestName(interpolation.Expression, style), used);
                    builder.Append('{');
                    builder.Append(name);
                    if (interpolation.AlignmentClause != null)
                    {
                        builder.Append(',');
                        builder.Append(interpolation.AlignmentClause.Value.ToString().Trim());
                    }

                    if (interpolation.FormatClause != null)
                    {
                        builder.Append(':');
                        builder.Append(interpolation.FormatClause.FormatStringToken.ValueText);
                    }

                    builder.Append('}');
                    values.Add(interpolation.Expression.WithoutTrivia());
                    break;
                default:
                    return false;
            }
        }

        template = builder.ToString();
        return true;
    }

    private static void AppendEscaped(StringBuilder builder, string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is '{' or '}')
            {
                builder.Append(c);
            }

            builder.Append(c);
        }
    }

    private static string SuggestName(ExpressionSyntax expression, PropertyNamingStyle style)
    {
        var raw = ExtractName(expression) ?? "Value";
        var suggested = PropertyNaming.SuggestFromExpression(raw, style);
        return string.IsNullOrEmpty(suggested) ? "Value" : suggested;
    }

    private static string? ExtractName(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case IdentifierNameSyntax identifier:
                return identifier.Identifier.ValueText;
            case MemberAccessExpressionSyntax member:
                return member.Name.Identifier.ValueText;
            case InvocationExpressionSyntax invocation when invocation.Expression is MemberAccessExpressionSyntax member:
                return member.Name.Identifier.ValueText;
            case InvocationExpressionSyntax invocation when invocation.Expression is IdentifierNameSyntax identifier:
                return identifier.Identifier.ValueText;
            case ElementAccessExpressionSyntax:
                return "Item";
            case ConditionalExpressionSyntax conditional:
                return ExtractName(conditional.WhenTrue) ?? ExtractName(conditional.WhenFalse);
            case ParenthesizedExpressionSyntax parenthesized:
                return ExtractName(parenthesized.Expression);
            case CastExpressionSyntax cast:
                return ExtractName(cast.Expression);
            case AwaitExpressionSyntax awaitExpression:
                return ExtractName(awaitExpression.Expression);
            case ObjectCreationExpressionSyntax creation:
                return creation.Type.ToString();
            case ThisExpressionSyntax:
                return "This";
            case BaseExpressionSyntax:
                return "Base";
            default:
                return null;
        }
    }

    private static string UniqueName(string name, HashSet<string> used)
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
}
