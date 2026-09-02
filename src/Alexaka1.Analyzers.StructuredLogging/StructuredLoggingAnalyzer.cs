using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Alexaka1.Analyzers.StructuredLogging.Classification;
using Alexaka1.Analyzers.StructuredLogging.Configuration;
using Alexaka1.Analyzers.StructuredLogging.Mapping;
using Alexaka1.Analyzers.StructuredLogging.Parsing;
using Alexaka1.Analyzers.StructuredLogging.Recognition;

namespace Alexaka1.Analyzers.StructuredLogging;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StructuredLoggingAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Descriptors.All;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        // Analyze user partials whose implementation is generated ([LoggerMessage]).
        // Skip generated trees ourselves so copied Define strings are not double-reported.
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var known = KnownSymbols.Resolve(context.Compilation);
        var classifier = new LoggingInvocationClassifier(known);
        var regexCache = new RegexCache();
        var settingsCache = new ConcurrentDictionary<SyntaxTree, AnalyzerSettings>();
        var generatedTrees = new ConcurrentDictionary<SyntaxTree, bool>();
        var analyzerOptions = context.Options;
        var generatedTreeDetector =
            new Func<SyntaxTree, bool>(tree => GeneratedCode.IsGenerated(tree, analyzerOptions));
        var constTemplateExclusivity = new ConcurrentDictionary<ISymbol, bool>(SymbolEqualityComparer.Default);

        context.RegisterSyntaxNodeAction(
            ctx =>
            {
                if (IsGeneratedTree(ctx, generatedTrees, generatedTreeDetector))
                {
                    return;
                }

                AnalyzeInvocation(ctx, known, classifier, regexCache, settingsCache);
            },
            SyntaxKind.InvocationExpression);

        context.RegisterSyntaxNodeAction(
            ctx =>
            {
                if (IsGeneratedTree(ctx, generatedTrees, generatedTreeDetector))
                {
                    return;
                }

                AnalyzeLoggerMessageMethod(ctx, known, regexCache, settingsCache, constTemplateExclusivity);
            },
            SyntaxKind.MethodDeclaration);

        context.RegisterSyntaxNodeAction(
            ctx =>
            {
                if (IsGeneratedTree(ctx, generatedTrees, generatedTreeDetector))
                {
                    return;
                }

                AnalyzeConstructor(ctx, classifier);
            },
            SyntaxKind.ConstructorDeclaration);

        context.RegisterSyntaxNodeAction(
            ctx =>
            {
                if (IsGeneratedTree(ctx, generatedTrees, generatedTreeDetector))
                {
                    return;
                }

                AnalyzeTypePrimaryConstructor(ctx, classifier);
            },
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration);
    }

    private static bool IsGeneratedTree(
        SyntaxNodeAnalysisContext context,
        ConcurrentDictionary<SyntaxTree, bool> generatedTrees,
        Func<SyntaxTree, bool> generatedTreeDetector)
    {
        return generatedTrees.GetOrAdd(context.Node.SyntaxTree, generatedTreeDetector);
    }

    private static AnalyzerSettings GetSettings(
        SyntaxTree tree,
        AnalyzerOptions options,
        ConcurrentDictionary<SyntaxTree, AnalyzerSettings> cache)
    {
        return cache.GetOrAdd(tree, t => AnalyzerSettings.From(options.AnalyzerConfigOptionsProvider, t));
    }

    private static void ReportGeneratedLoggingSemanticConventions(
        SyntaxNodeAnalysisContext context,
        AnalyzerSettings settings,
        Location location)
    {
        if (!settings.TemplateNamingIsSemanticConventions)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.GeneratedLoggingCannotUseSemanticConventions,
            location));
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        KnownSymbols known,
        LoggingInvocationClassifier classifier,
        RegexCache regexCache,
        ConcurrentDictionary<SyntaxTree, AnalyzerSettings> settingsCache)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var typeArguments = GetTypeArgumentList(invocation);
        if (invocation.ArgumentList.Arguments.Count == 0 &&
            (typeArguments is null || typeArguments.Arguments.Count == 0))
        {
            return;
        }

        var method =
            LoggingInvocationClassifier.ResolveMethod(context.SemanticModel, invocation, context.CancellationToken);
        if (method is null)
        {
            return;
        }

        var settings = GetSettings(invocation.SyntaxTree, context.Options, settingsCache);

        if (LoggingInvocationClassifier.IsSerilogForContext(method))
        {
            AnalyzeForContext(context, invocation, method);
        }

        if (LoggingInvocationClassifier.IsSerilogPushProperty(method))
        {
            AnalyzePushProperty(context, invocation, method, settings, regexCache);
        }

        var templateParameterName = classifier.GetTemplateParameterName(method);
        if (templateParameterName is null)
        {
            return;
        }

        var templateOpt = TemplateArgumentResolver.FindTemplate(
            context.SemanticModel,
            invocation,
            method,
            templateParameterName,
            context.CancellationToken,
            out var arguments);
        if (templateOpt is null)
        {
            return;
        }

        var template = templateOpt.Value;
        if (template.Expression.ContainsDiagnostics)
        {
            return;
        }

        var isConstant = IsCompileTimeConstant(context.SemanticModel, template.Expression, context.CancellationToken);
        if (!isConstant)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.TemplateIsNotCompileTimeConstant,
                template.Expression.GetLocation()));
        }

        AnalyzeException(context, invocation, method, template, arguments, known,
            skip: LoggerMessageParameterMapper.IsLoggerMessageDefine(method, known));

        if (!isConstant)
        {
            return;
        }

        if (!LiteralSpanMapper.TryMap(context.SemanticModel, template.Expression, context.CancellationToken,
                out var map))
        {
            if (!LiteralSpanMapper.TryGetConstantText(context.SemanticModel, template.Expression,
                    context.CancellationToken, out var constantText))
            {
                return;
            }

            map = new TemplateSourceMap(
                constantText,
                Array.Empty<MappedChar>(),
                template.Expression,
                allowRewrite: false);
        }

        var parsed = MessageTemplateParser.Parse(map.Value);
        var allowDestructuring = LoggingInvocationClassifier.SupportsDestructuringOperator(method);
        AnalyzeTemplateRules(context, invocation, template, arguments, parsed, map, settings, regexCache,
            allowDestructuring);
        TemplateStyleRules.AnalyzeTrailingPeriod(
            context,
            context.SemanticModel,
            template.Expression,
            map,
            allowRewrite: true,
            context.CancellationToken);
    }

    private static void AnalyzeTemplateRules(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        BoundTemplateArgument template,
        List<BoundTemplateArgument> arguments,
        ParsedTemplate parsed,
        TemplateSourceMap map,
        AnalyzerSettings settings,
        RegexCache regexCache,
        bool allowDestructuring)
    {
        if (parsed.PositionalProperties != null)
        {
            TemplateStyleRules.AnalyzePositional(
                context,
                map,
                parsed.PositionalProperties,
                templateParameters: null,
                PropertyArgumentMapper.ArgumentsForPositionalNames(
                    arguments,
                    template,
                    parsed.PositionalProperties.Length),
                settings,
                allowRewrite: true);
        }

        if (parsed.NamedProperties is null)
        {
            return;
        }

        var named = parsed.NamedProperties;
        var argumentExpressions = new ExpressionSyntax?[named.Length];
        for (var i = 0; i < named.Length; i++)
        {
            argumentExpressions[i] = PropertyArgumentMapper.ArgumentForHole(arguments, template, i);
        }

        TemplateStyleRules.AnalyzeNamed(
            context,
            map,
            named,
            settings,
            regexCache,
            skipHole: null,
            allowRewrite: true,
            argumentExpressions,
            uniquifyDuplicates: true);

        if (allowDestructuring)
        {
            AnalyzeAnonymousAndComplex(context, invocation, template, arguments, named, map);
        }
    }

    private static void AnalyzeLoggerMessageMethod(
        SyntaxNodeAnalysisContext context,
        KnownSymbols known,
        RegexCache regexCache,
        ConcurrentDictionary<SyntaxTree, AnalyzerSettings> settingsCache,
        ConcurrentDictionary<ISymbol, bool> constTemplateExclusivity)
    {
        if (known.LoggerMessageAttribute is null && known.Logger is null)
        {
            return;
        }

        var methodDecl = (MethodDeclarationSyntax)context.Node;
        if (methodDecl.AttributeLists.Count == 0)
        {
            return;
        }

        var method = context.SemanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken);
        if (method is null)
        {
            return;
        }

        if (!LoggerMessageAttributeReader.TryGet(method, known, context.CancellationToken, out var template))
        {
            return;
        }

        if (template.Attribute.SyntaxTree != methodDecl.SyntaxTree ||
            !methodDecl.Span.Contains(template.Attribute.SpanStart))
        {
            return;
        }

        var settings = GetSettings(methodDecl.SyntaxTree, context.Options, settingsCache);
        ReportGeneratedLoggingSemanticConventions(context, settings, template.Attribute.Name.GetLocation());

        if (template.Expression is not null && template.Expression.ContainsDiagnostics)
        {
            return;
        }

        var parameters = LoggerMessageParameterMapper.Classify(method, known);
        var source = ConstTemplateMapper.Resolve(
            context.SemanticModel,
            template.Expression,
            method,
            constTemplateExclusivity,
            context.CancellationToken);

        if (source.Map is null)
        {
            return;
        }

        var parsed = MessageTemplateParser.Parse(source.Map.Value);

        bool Skip(PropertyHole hole) =>
            LoggerMessageParameterMapper.IsSpecialPlaceholder(parameters, hole.PropertyName);

        if (parsed.PositionalProperties != null)
        {
            TemplateStyleRules.AnalyzePositional(
                context,
                source.Map,
                parsed.PositionalProperties,
                parameters.TemplateParameters,
                argumentExpressions: null,
                settings,
                source.AllowRewrite);
        }

        if (parsed.NamedProperties != null)
        {
            TemplateStyleRules.AnalyzeNamed(
                context,
                source.Map,
                parsed.NamedProperties,
                settings,
                regexCache,
                Skip,
                source.AllowRewrite);
        }

        if (source.Expression is not null)
        {
            TemplateStyleRules.AnalyzeTrailingPeriod(
                context,
                context.SemanticModel,
                source.Expression,
                source.Map,
                source.AllowRewrite,
                context.CancellationToken);
        }
    }

    private static void AnalyzeAnonymousAndComplex(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        BoundTemplateArgument template,
        List<BoundTemplateArgument> arguments,
        PropertyHole[] named,
        TemplateSourceMap map)
    {
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.Expression is not AnonymousObjectCreationExpressionSyntax)
            {
                continue;
            }

            var bound = FindBound(arguments, argument);
            if (bound is null || bound.Value.Ordinal <= template.Ordinal)
            {
                continue;
            }

            var holeIndex = IndexAfterTemplate(arguments, template, bound.Value);
            if (holeIndex < 0 || holeIndex >= named.Length)
            {
                continue;
            }

            var hole = named[holeIndex];
            if (hole.Destructuring != DestructuringKind.Default)
            {
                continue;
            }

            var properties = ImmutableDictionary<string, string?>.Empty
                .Add(FixProperties.InsertLogicalIndex, (hole.StartIndex + 1).ToString(CultureInfo.InvariantCulture));
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.AnonymousObjectMustBeDestructured,
                TemplateStyleRules.HoleLocation(map, hole),
                properties));
        }

        for (var i = 0; i < named.Length; i++)
        {
            var hole = named[i];
            if (hole.Destructuring != DestructuringKind.Default)
            {
                continue;
            }

            var expression = PropertyArgumentMapper.ArgumentForHole(arguments, template, i);
            if (expression is null)
            {
                continue;
            }

            if (expression is AnonymousObjectCreationExpressionSyntax)
            {
                continue;
            }

            var type = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken).Type;
            if (!TypeClassifier.NeedsDestructuring(type))
            {
                continue;
            }

            var properties = ImmutableDictionary<string, string?>.Empty
                .Add(FixProperties.InsertLogicalIndex, (hole.StartIndex + 1).ToString(CultureInfo.InvariantCulture));
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.ComplexObjectShouldBeDestructured,
                TemplateStyleRules.HoleLocation(map, hole),
                properties));
        }
    }

    private static void AnalyzeException(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        BoundTemplateArgument template,
        List<BoundTemplateArgument> arguments,
        KnownSymbols known,
        bool skip)
    {
        if (skip)
        {
            return;
        }

        var exceptionType = known.Exception;
        if (exceptionType is null)
        {
            return;
        }

        var invalid = FindInvalidExceptionArgument(context, arguments, template, exceptionType);
        if (invalid is null)
        {
            return;
        }

        var invalidIndex = invocation.ArgumentList.Arguments.IndexOf(invalid);
        if (invalidIndex < 0)
        {
            return;
        }

        if (!HasExceptionOverload(method, exceptionType, invalidIndex))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.ExceptionPassedAsTemplateArgument,
            invalid.Expression.GetLocation()));
    }

    private static ArgumentSyntax? FindInvalidExceptionArgument(
        SyntaxNodeAnalysisContext context,
        List<BoundTemplateArgument> arguments,
        BoundTemplateArgument template,
        INamedTypeSymbol exceptionType)
    {
        foreach (var bound in arguments)
        {
            var type = context.SemanticModel.GetTypeInfo(bound.Expression, context.CancellationToken).Type;
            if (type is null || !IsOrDerivedFrom(type, exceptionType))
            {
                continue;
            }

            if (bound.Ordinal < template.Ordinal)
            {
                continue;
            }

            return bound.Argument;
        }

        return null;
    }

    private static bool HasExceptionOverload(IMethodSymbol method, INamedTypeSymbol exceptionType,
        int invalidArgumentIndex)
    {
        var type = method.ContainingType;
        if (type is null)
        {
            return false;
        }

        foreach (var member in type.GetMembers(method.Name))
        {
            if (member is not IMethodSymbol candidate)
            {
                continue;
            }

            // A reduced extension invocation omits the receiver from its
            // argument list, while members returned from the containing type
            // retain the extension's `this` parameter at ordinal zero. An
            // explicit static call includes that receiver in its arguments.
            var parameterOffset = method.ReducedFrom is null ? 0 : 1;

            foreach (var parameter in candidate.Parameters)
            {
                if (invalidArgumentIndex + parameterOffset <= parameter.Ordinal)
                {
                    break;
                }

                if (IsOrDerivedFrom(parameter.Type, exceptionType))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void AnalyzePushProperty(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        AnalyzerSettings settings,
        RegexCache regexCache)
    {
        var boundArguments = TemplateArgumentResolver.MapArguments(
            context.SemanticModel,
            invocation,
            method,
            context.CancellationToken);
        BoundTemplateArgument? nameArgument = null;
        BoundTemplateArgument? valueArgument = null;
        var hasDestructureArgument = false;
        foreach (var bound in boundArguments)
        {
            if (bound.Parameter.Name == "name")
            {
                nameArgument = bound;
            }
            else if (bound.Parameter.Name == "value")
            {
                valueArgument = bound;
            }
            else if (bound.Parameter.Name == "destructureObjects")
            {
                hasDestructureArgument = true;
            }
        }

        if (nameArgument is { } name)
        {
            var nameExpression = name.Expression;
            var constant = context.SemanticModel.GetConstantValue(nameExpression, context.CancellationToken);
            if (constant.HasValue && constant.Value is string propertyName && !string.IsNullOrEmpty(propertyName))
            {
                if (!settings.IsIgnored(propertyName, regexCache, DiagnosticIds.InconsistentContextPropertyNaming))
                {
                    var suggested = PropertyNaming.Suggest(
                        propertyName,
                        settings.GetNaming(DiagnosticIds.InconsistentContextPropertyNaming));
                    if (!string.Equals(suggested, propertyName, StringComparison.Ordinal))
                    {
                        var properties = ImmutableDictionary<string, string?>.Empty
                            .Add(FixProperties.SuggestedName, suggested);
                        context.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.InconsistentContextPropertyNaming,
                            name.Expression.GetLocation(),
                            properties,
                            propertyName,
                            suggested));
                    }
                }
            }
        }

        if (valueArgument is not { } value || hasDestructureArgument)
        {
            return;
        }

        var valueType = context.SemanticModel.GetTypeInfo(value.Expression, context.CancellationToken).Type;
        if (!TypeClassifier.NeedsDestructuring(valueType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.ComplexObjectInContextShouldBeDestructured,
            invocation.GetLocation()));
    }

    private static void AnalyzeForContext(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method)
    {
        var typeArguments = GetTypeArgumentList(invocation);
        if (typeArguments is null || typeArguments.Arguments.Count != 1)
        {
            return;
        }

        var containingType = invocation.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (containingType is null)
        {
            return;
        }

        var containingSymbol = context.SemanticModel.GetDeclaredSymbol(containingType, context.CancellationToken);
        var typeArg = context.SemanticModel.GetTypeInfo(typeArguments.Arguments[0], context.CancellationToken).Type;
        if (containingSymbol is null || typeArg is null)
        {
            return;
        }

        if (SymbolEqualityComparer.Default.Equals(containingSymbol, typeArg.OriginalDefinition) ||
            containingSymbol.ToDisplayString() == typeArg.ToDisplayString())
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.ContextualLoggerMismatch,
            invocation.GetLocation()));
        _ = method;
    }

    private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context, LoggingInvocationClassifier classifier)
    {
        var constructor = (ConstructorDeclarationSyntax)context.Node;
        var containing = context.SemanticModel.GetDeclaredSymbol(constructor, context.CancellationToken)
            ?.ContainingType;
        AnalyzeLoggerParameters(context, constructor.ParameterList, containing, classifier);
    }

    private static void AnalyzeTypePrimaryConstructor(SyntaxNodeAnalysisContext context,
        LoggingInvocationClassifier classifier)
    {
        var typeDecl = (TypeDeclarationSyntax)context.Node;
        if (typeDecl.ParameterList is null)
        {
            return;
        }

        var containing = context.SemanticModel.GetDeclaredSymbol(typeDecl, context.CancellationToken);
        AnalyzeLoggerParameters(context, typeDecl.ParameterList, containing, classifier);
    }

    private static void AnalyzeLoggerParameters(
        SyntaxNodeAnalysisContext context,
        ParameterListSyntax parameterList,
        INamedTypeSymbol? containing,
        LoggingInvocationClassifier classifier)
    {
        if (containing is null)
        {
            return;
        }

        foreach (var parameter in parameterList.Parameters)
        {
            if (parameter.Type is null)
            {
                continue;
            }

            var type = context.SemanticModel.GetTypeInfo(parameter.Type, context.CancellationToken).Type;
            if (type is null || !classifier.IsGenericMicrosoftLogger(type, out var typeArgument) ||
                typeArgument is null)
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(containing, typeArgument.OriginalDefinition) ||
                containing.ToDisplayString() == typeArgument.ToDisplayString())
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.ContextualLoggerMismatch,
                parameter.Type.GetLocation()));
        }
    }

    private static bool IsCompileTimeConstant(SemanticModel model, ExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        var constant = model.GetConstantValue(expression, cancellationToken);
        return constant.HasValue && constant.Value is string;
    }

    private static BoundTemplateArgument? FindBound(List<BoundTemplateArgument> arguments, ArgumentSyntax argument)
    {
        for (var i = 0; i < arguments.Count; i++)
        {
            if (arguments[i].Argument == argument)
            {
                return arguments[i];
            }
        }

        return null;
    }

    private static int IndexAfterTemplate(
        List<BoundTemplateArgument> arguments,
        BoundTemplateArgument template,
        BoundTemplateArgument target)
    {
        var index = 0;
        for (var i = 0; i < arguments.Count; i++)
        {
            if (arguments[i].Ordinal <= template.Ordinal)
            {
                continue;
            }

            if (arguments[i].Argument == target.Argument)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    private static TypeArgumentListSyntax? GetTypeArgumentList(InvocationExpressionSyntax invocation)
    {
        switch (invocation.Expression)
        {
            case GenericNameSyntax generic:
                return generic.TypeArgumentList;
            case MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName }:
                return genericName.TypeArgumentList;
            case MemberBindingExpressionSyntax { Name: GenericNameSyntax genericName }:
                return genericName.TypeArgumentList;
            default:
                return null;
        }
    }

    private static bool IsOrDerivedFrom(ITypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }
}

internal static class FixProperties
{
    public const string SuggestedName = nameof(SuggestedName);
    public const string PropertyName = nameof(PropertyName);
    public const string InsertLogicalIndex = nameof(InsertLogicalIndex);
    public const string NameLogicalStart = nameof(NameLogicalStart);
    public const string NameLogicalLength = nameof(NameLogicalLength);
    public const string AllowRewrite = nameof(AllowRewrite);
    public const string QualifiedSuggestedName = nameof(QualifiedSuggestedName);
}
