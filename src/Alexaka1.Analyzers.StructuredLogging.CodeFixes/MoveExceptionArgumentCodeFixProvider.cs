using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Alexaka1.Analyzers.StructuredLogging.Mapping;
using Alexaka1.Analyzers.StructuredLogging.Parsing;
using Alexaka1.Analyzers.StructuredLogging.Recognition;

namespace Alexaka1.Analyzers.StructuredLogging.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MoveExceptionArgumentCodeFixProvider))]
[Shared]
public sealed class MoveExceptionArgumentCodeFixProvider : CodeFixProvider
{
    private static readonly SyntaxAnnotation InvocationAnnotation = new("AASL0005.invocation");

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.ExceptionPassedAsTemplateArgument);

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
            var invocation = FindInvocation(root, diagnostic.Location.SourceSpan);
            if (invocation is null)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Pass exception to the exception argument",
                    ct => ApplyAsync(context.Document, diagnostic, ct),
                    nameof(MoveExceptionArgumentCodeFixProvider)),
                diagnostic);
        }
    }

    private static async Task<Document> ApplyAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
        {
            return document;
        }

        var invocation = FindInvocation(root, diagnostic.Location.SourceSpan);
        if (invocation is null)
        {
            return document;
        }

        var compilation = model.Compilation;
        var method = LoggingInvocationClassifier.ResolveMethod(model, invocation, cancellationToken);
        if (method is null)
        {
            return document;
        }

        var classifier = new LoggingInvocationClassifier(KnownSymbols.Resolve(compilation));
        var templateParameterName = classifier.GetTemplateParameterName(method);
        if (templateParameterName is null)
        {
            return document;
        }

        var template = TemplateArgumentResolver.FindTemplate(
            model,
            invocation,
            method,
            templateParameterName,
            cancellationToken);
        if (template is null)
        {
            return document;
        }

        var exceptionArgument = FindExceptionArgument(invocation, diagnostic.Location.SourceSpan);
        if (exceptionArgument is null)
        {
            return document;
        }

        var arguments = TemplateArgumentResolver.MapArguments(model, invocation, method, cancellationToken);
        var holeIndex = HoleIndexForException(arguments, template.Value, exceptionArgument);
        var moved = MoveException(invocation, template.Value.Argument, exceptionArgument);
        if (moved is null)
        {
            return document;
        }

        var annotated = moved.WithAdditionalAnnotations(InvocationAnnotation);
        var updatedRoot = root.ReplaceNode(invocation, annotated);
        var updatedDocument = document.WithSyntaxRoot(updatedRoot);
        if (holeIndex < 0)
        {
            return updatedDocument;
        }

        return await RemoveHoleAsync(updatedDocument, holeIndex, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> RemoveHoleAsync(
        Document document,
        int holeIndex,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        InvocationExpressionSyntax? invocation = null;
        if (root is not null)
        {
            foreach (var node in root.GetAnnotatedNodes(InvocationAnnotation))
            {
                if (node is InvocationExpressionSyntax annotated)
                {
                    invocation = annotated;
                    break;
                }
            }
        }
        if (root is null || model is null || invocation is null)
        {
            return document;
        }

        var method = LoggingInvocationClassifier.ResolveMethod(model, invocation, cancellationToken);
        if (method is null)
        {
            return document;
        }

        var classifier = new LoggingInvocationClassifier(KnownSymbols.Resolve(model.Compilation));
        var templateParameterName = classifier.GetTemplateParameterName(method);
        if (templateParameterName is null)
        {
            return document;
        }

        var template = TemplateArgumentResolver.FindTemplate(
            model,
            invocation,
            method,
            templateParameterName,
            cancellationToken);
        if (template is null ||
            !LiteralSpanMapper.TryMap(model, template.Value.Expression, cancellationToken, out var map))
        {
            return document;
        }

        var parsed = MessageTemplateParser.Parse(map.Value);
        if (holeIndex >= parsed.Properties.Length)
        {
            return document;
        }

        var hole = parsed.Properties[holeIndex];
        var (logicalStart, logicalLength) = ExpandHoleRemoval(map.Value, hole);
        var span = map.TryGetSpan(logicalStart, logicalLength);
        if (span is null)
        {
            return document;
        }

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return document.WithText(text.WithChanges(new TextChange(span.Value, string.Empty)));
    }

    private static InvocationExpressionSyntax? MoveException(
        InvocationExpressionSyntax invocation,
        ArgumentSyntax templateArgument,
        ArgumentSyntax exceptionArgument)
    {
        var args = new List<ArgumentSyntax>(invocation.ArgumentList.Arguments.Count);
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            args.Add(argument);
        }
        var exceptionIndex = args.IndexOf(exceptionArgument);
        var templateIndex = args.IndexOf(templateArgument);
        if (exceptionIndex < 0 || templateIndex < 0 || exceptionIndex <= templateIndex)
        {
            return null;
        }

        args.RemoveAt(exceptionIndex);
        var moved = exceptionArgument.WithNameColon(null);
        var leading = moved.GetLeadingTrivia();
        for (var i = 0; i < leading.Count; i++)
        {
            if (leading[i].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                moved = moved.WithLeadingTrivia(SyntaxFactory.Space);
                break;
            }
        }

        args.Insert(templateIndex, moved);
        return invocation.WithArgumentList(
            invocation.ArgumentList.WithArguments(SyntaxFactory.SeparatedList(args)));
    }

    private static int HoleIndexForException(
        List<BoundTemplateArgument> arguments,
        BoundTemplateArgument template,
        ArgumentSyntax exceptionArgument)
    {
        var index = 0;
        for (var i = 0; i < arguments.Count; i++)
        {
            if (arguments[i].Ordinal <= template.Ordinal)
            {
                continue;
            }

            if (arguments[i].Argument == exceptionArgument)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    private static (int Start, int Length) ExpandHoleRemoval(string template, PropertyHole hole)
    {
        var start = hole.StartIndex;
        var length = hole.Length;
        if (start > 0 && char.IsWhiteSpace(template[start - 1]))
        {
            start--;
            length++;
        }
        else if (start + hole.Length < template.Length &&
                 char.IsWhiteSpace(template[start + hole.Length]))
        {
            length++;
        }

        return (start, length);
    }

    private static ArgumentSyntax? FindExceptionArgument(InvocationExpressionSyntax invocation, TextSpan span)
    {
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.Expression.Span.Contains(span) || span.Contains(argument.Expression.Span))
            {
                return argument;
            }
        }

        return null;
    }

    private static InvocationExpressionSyntax? FindInvocation(SyntaxNode root, TextSpan span)
    {
        var node = root.FindNode(span, getInnermostNodeForTie: true);
        return node as InvocationExpressionSyntax ?? node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
    }
}
