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
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            if (GetFixCandidate(
                    root,
                    model,
                    diagnostic,
                    context.CancellationToken) is null)
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

        var candidate = GetFixCandidate(root, model, diagnostic, cancellationToken);
        if (candidate is null)
        {
            return document;
        }

        var fix = candidate.Value;
        var arguments = TemplateArgumentResolver.MapArguments(
            model,
            fix.Invocation,
            fix.Method,
            cancellationToken);
        var holeIndex = HoleIndexForException(arguments, fix.Template, fix.ExceptionArgument);

        var annotated = fix.MovedInvocation.WithAdditionalAnnotations(InvocationAnnotation);
        var updatedRoot = root.ReplaceNode(fix.Invocation, annotated);
        var updatedDocument = document.WithSyntaxRoot(updatedRoot);
        if (holeIndex < 0)
        {
            return updatedDocument;
        }

        return await RemoveHoleAsync(updatedDocument, holeIndex, cancellationToken).ConfigureAwait(false);
    }

    private static FixCandidate? GetFixCandidate(
        SyntaxNode root,
        SemanticModel model,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var invocation = FindInvocation(root, diagnostic.Location.SourceSpan);
        if (invocation is null)
        {
            return null;
        }

        var method = LoggingInvocationClassifier.ResolveMethod(model, invocation, cancellationToken);
        if (method is null)
        {
            return null;
        }

        var classifier = new LoggingInvocationClassifier(
            KnownSymbols.Resolve(model.Compilation, cancellationToken));
        var templateParameterName = classifier.GetTemplateParameterName(method);
        if (templateParameterName is null)
        {
            return null;
        }

        var template = TemplateArgumentResolver.FindTemplate(
            model,
            invocation,
            method,
            templateParameterName,
            cancellationToken);
        if (template is null)
        {
            return null;
        }

        var exceptionArgument = FindExceptionArgument(invocation, diagnostic.Location.SourceSpan);
        if (exceptionArgument is null ||
            HasEarlierExceptionArgument(
                model,
                invocation,
                method,
                template.Value.Parameter,
                exceptionArgument,
                cancellationToken))
        {
            return null;
        }

        var moved = MoveException(invocation, template.Value.Argument, exceptionArgument);
        return moved is null
            ? null
            : new FixCandidate(invocation, method, template.Value, exceptionArgument, moved);
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

        var classifier = new LoggingInvocationClassifier(KnownSymbols.Resolve(model.Compilation, cancellationToken));
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
        var original = invocation.ArgumentList.Arguments;
        var exceptionIndex = original.IndexOf(exceptionArgument);
        var templateIndex = original.IndexOf(templateArgument);
        if (exceptionIndex < 0 || templateIndex < 0 || exceptionIndex <= templateIndex)
        {
            return null;
        }

        var nodes = new List<ArgumentSyntax>(original.Count);
        foreach (var argument in original)
        {
            nodes.Add(argument);
        }

        var separators = new List<SyntaxToken>(original.SeparatorCount);
        foreach (var separator in original.GetSeparators())
        {
            separators.Add(separator);
        }

        var moved = RelocateExceptionArgument(original, exceptionIndex, exceptionArgument);
        nodes.RemoveAt(exceptionIndex);
        separators.RemoveAt(exceptionIndex - 1);
        nodes.Insert(templateIndex, moved);
        separators.Insert(
            templateIndex,
            SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space));

        return invocation.WithArgumentList(
            invocation.ArgumentList.WithArguments(SyntaxFactory.SeparatedList(nodes, separators)));
    }

    private static ArgumentSyntax RelocateExceptionArgument(
        SeparatedSyntaxList<ArgumentSyntax> original,
        int exceptionIndex,
        ArgumentSyntax exceptionArgument)
    {
        var leading = new List<SyntaxTrivia>();
        if (exceptionIndex > 0)
        {
            AppendCommentTrivia(leading, original.GetSeparator(exceptionIndex - 1).TrailingTrivia);
        }

        AppendLeadingTrivia(leading, exceptionArgument.GetLeadingTrivia());

        var trailing = new List<SyntaxTrivia>();
        AppendCommentTrivia(trailing, exceptionArgument.GetTrailingTrivia());
        PromoteSingleLineComments(trailing, leading);
        EnsureLeadingCommentSpacing(leading);
        EnsureTrailingCommentSpacing(trailing);

        return exceptionArgument.WithNameColon(null)
            .WithLeadingTrivia(SyntaxFactory.TriviaList(leading))
            .WithTrailingTrivia(SyntaxFactory.TriviaList(trailing));
    }

    private static void AppendCommentTrivia(List<SyntaxTrivia> comments, SyntaxTriviaList trivia)
    {
        for (var i = 0; i < trivia.Count; i++)
        {
            var item = trivia[i];
            if (!item.IsKind(SyntaxKind.MultiLineCommentTrivia) &&
                !item.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                continue;
            }

            comments.Add(item);
            if (!item.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                continue;
            }

            if (i + 1 < trivia.Count && trivia[i + 1].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                comments.Add(trivia[++i]);
                if (i + 1 < trivia.Count && trivia[i + 1].IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    comments.Add(trivia[++i]);
                }
            }
            else
            {
                comments.Add(SyntaxFactory.EndOfLine("\n"));
            }
        }
    }

    private static void AppendLeadingTrivia(List<SyntaxTrivia> comments, SyntaxTriviaList trivia)
    {
        foreach (var item in trivia)
        {
            if (item.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                item.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                item.IsKind(SyntaxKind.EndOfLineTrivia) ||
                item.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                comments.Add(item);
            }
        }
    }

    private static void PromoteSingleLineComments(List<SyntaxTrivia> trailing, List<SyntaxTrivia> leading)
    {
        var kept = new List<SyntaxTrivia>();
        for (var i = 0; i < trailing.Count; i++)
        {
            var item = trailing[i];
            if (!item.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                if (!item.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    kept.Add(item);
                }

                continue;
            }

            leading.Add(item);
            if (i + 1 < trailing.Count && trailing[i + 1].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                leading.Add(trailing[++i]);
                if (i + 1 < trailing.Count && trailing[i + 1].IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    leading.Add(trailing[++i]);
                }
            }
            else
            {
                leading.Add(SyntaxFactory.EndOfLine("\n"));
            }
        }

        trailing.Clear();
        trailing.AddRange(kept);
    }

    private static void EnsureLeadingCommentSpacing(List<SyntaxTrivia> comments)
    {
        if (comments.Count == 0)
        {
            return;
        }

        var last = comments[comments.Count - 1];
        if (last.IsKind(SyntaxKind.EndOfLineTrivia) || last.IsKind(SyntaxKind.WhitespaceTrivia))
        {
            return;
        }

        comments.Add(last.IsKind(SyntaxKind.SingleLineCommentTrivia)
            ? SyntaxFactory.EndOfLine("\n")
            : SyntaxFactory.Space);
    }

    private static void EnsureTrailingCommentSpacing(List<SyntaxTrivia> comments)
    {
        if (comments.Count == 0)
        {
            return;
        }

        if (!comments[0].IsKind(SyntaxKind.WhitespaceTrivia) &&
            !comments[0].IsKind(SyntaxKind.EndOfLineTrivia))
        {
            comments.Insert(0, SyntaxFactory.Space);
        }

        var last = comments[comments.Count - 1];
        if (last.IsKind(SyntaxKind.SingleLineCommentTrivia))
        {
            comments.Add(SyntaxFactory.EndOfLine("\n"));
        }
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

    private static bool HasEarlierExceptionArgument(
        SemanticModel model,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        IParameterSymbol templateParameter,
        ArgumentSyntax exceptionArgument,
        CancellationToken cancellationToken)
    {
        var known = KnownSymbols.Resolve(model.Compilation, cancellationToken);
        if (known.Exception is null)
        {
            return false;
        }

        var arguments = TemplateArgumentResolver.MapArguments(
            model,
            invocation,
            method,
            cancellationToken);
        foreach (var argument in arguments)
        {
            if (argument.Argument == exceptionArgument)
            {
                continue;
            }

            if (argument.Parameter.Ordinal < templateParameter.Ordinal &&
                LoggerMessageParameterMapper.IsException(argument.Parameter.Type, known))
            {
                return true;
            }
        }

        return false;
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

    private readonly struct FixCandidate
    {
        public FixCandidate(
            InvocationExpressionSyntax invocation,
            IMethodSymbol method,
            BoundTemplateArgument template,
            ArgumentSyntax exceptionArgument,
            InvocationExpressionSyntax movedInvocation)
        {
            Invocation = invocation;
            Method = method;
            Template = template;
            ExceptionArgument = exceptionArgument;
            MovedInvocation = movedInvocation;
        }

        public InvocationExpressionSyntax Invocation { get; }

        public IMethodSymbol Method { get; }

        public BoundTemplateArgument Template { get; }

        public ArgumentSyntax ExceptionArgument { get; }

        public InvocationExpressionSyntax MovedInvocation { get; }
    }
}
