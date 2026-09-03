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
        var known = KnownSymbols.Resolve(context.Compilation, context.CancellationToken);
        if (!known.HasAnyLoggingLibrary && !known.HasGenericMicrosoftLogger)
        {
            return;
        }

        var classifier = new LoggingInvocationClassifier(known);
        var regexCache = new RegexCache();
        var settingsCache = new ConcurrentDictionary<SyntaxTree, AnalyzerSettings>();
        var generatedTrees = new ConcurrentDictionary<SyntaxTree, bool>();
        var analyzerOptions = context.Options;
        var generatedTreeDetector =
            new Func<SyntaxTree, bool>(tree => GeneratedCode.IsGenerated(tree, analyzerOptions));
        var constTemplateExclusivity = new ConcurrentDictionary<ISymbol, bool>(SymbolEqualityComparer.Default);
        var semanticModels = new ConcurrentDictionary<SyntaxTree, SemanticModel>();

        if (known.HasAnyLoggingLibrary)
        {
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
        }

        if (known.HasMicrosoftLogging)
        {
            context.RegisterSyntaxNodeAction(
                ctx =>
                {
                    if (IsGeneratedTree(ctx, generatedTrees, generatedTreeDetector))
                    {
                        return;
                    }

                    AnalyzeLoggerMessageMethod(
                        ctx,
                        known,
                        regexCache,
                        settingsCache,
                        constTemplateExclusivity,
                        semanticModels);
                },
                SyntaxKind.MethodDeclaration);
        }

        if (known.HasGenericMicrosoftLogger)
        {
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

        var method = LoggingInvocationClassifier.ResolveMethod(
            context.SemanticModel,
            invocation,
            context.CancellationToken,
            out var isCandidateMethod,
            out var candidateSymbols);
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

        BoundTemplateArgument? templateOpt = null;
        List<BoundTemplateArgument>? arguments = null;
        if (isCandidateMethod)
        {
            foreach (var candidateSymbol in candidateSymbols)
            {
                if (candidateSymbol is not IMethodSymbol candidateMethod)
                {
                    continue;
                }

                var candidateTemplateName = classifier.GetTemplateParameterName(candidateMethod);
                if (candidateTemplateName is null)
                {
                    continue;
                }

                var candidateTemplate = TemplateArgumentResolver.FindTemplate(
                    context.SemanticModel,
                    invocation,
                    candidateMethod,
                    candidateTemplateName,
                    context.CancellationToken,
                    out var candidateArguments);
                if (candidateTemplate is null ||
                    !IsClearlyString(context.SemanticModel, candidateTemplate.Value.Expression,
                        context.CancellationToken))
                {
                    continue;
                }

                method = candidateMethod;
                templateOpt = candidateTemplate;
                arguments = candidateArguments;
                break;
            }
        }
        else
        {
            var templateParameterName = classifier.GetTemplateParameterName(method);
            if (templateParameterName is null)
            {
                return;
            }

            templateOpt = TemplateArgumentResolver.FindTemplate(
                context.SemanticModel,
                invocation,
                method,
                templateParameterName,
                context.CancellationToken,
                out arguments);
        }

        if (templateOpt is null || arguments is null)
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

        AnalyzeException(context, method, template, arguments, known,
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
            AnalyzeAnonymousAndComplex(context, argumentExpressions, named, map);
        }
    }

    private static void AnalyzeLoggerMessageMethod(
        SyntaxNodeAnalysisContext context,
        KnownSymbols known,
        RegexCache regexCache,
        ConcurrentDictionary<SyntaxTree, AnalyzerSettings> settingsCache,
        ConcurrentDictionary<ISymbol, bool> constTemplateExclusivity,
        ConcurrentDictionary<SyntaxTree, SemanticModel> semanticModels)
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
            semanticModels,
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
        ExpressionSyntax?[] argumentExpressions,
        PropertyHole[] named,
        TemplateSourceMap map)
    {
        for (var i = 0; i < named.Length; i++)
        {
            var hole = named[i];
            if (hole.Destructuring != DestructuringKind.Default)
            {
                continue;
            }

            var expression = argumentExpressions[i];
            if (expression is null)
            {
                continue;
            }

            var descriptor = Descriptors.AnonymousObjectMustBeDestructured;
            if (expression is not AnonymousObjectCreationExpressionSyntax)
            {
                var type = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken).Type;
                if (!TypeClassifier.NeedsDestructuring(type))
                {
                    continue;
                }

                descriptor = Descriptors.ComplexObjectShouldBeDestructured;
            }

            var properties = ImmutableDictionary<string, string?>.Empty
                .Add(FixProperties.InsertLogicalIndex, (hole.StartIndex + 1).ToString(CultureInfo.InvariantCulture));
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                TemplateStyleRules.HoleLocation(map, hole),
                properties));
        }
    }

    private static void AnalyzeException(
        SyntaxNodeAnalysisContext context,
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

        if (!HasExceptionOverload(method, exceptionType, template.Parameter.Name))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.ExceptionPassedAsTemplateArgument,
            invalid.Value.Expression.GetLocation()));
    }

    private static BoundTemplateArgument? FindInvalidExceptionArgument(
        SyntaxNodeAnalysisContext context,
        List<BoundTemplateArgument> arguments,
        BoundTemplateArgument template,
        INamedTypeSymbol exceptionType)
    {
        foreach (var argument in arguments)
        {
            var type = context.SemanticModel.GetTypeInfo(argument.Expression, context.CancellationToken).Type;
            if (type is null || !IsOrDerivedFrom(type, exceptionType))
            {
                continue;
            }

            if (argument.Ordinal <= template.Ordinal)
            {
                continue;
            }

            return argument;
        }

        return null;
    }

    private static bool HasExceptionOverload(IMethodSymbol method, INamedTypeSymbol exceptionType,
        string templateParameterName)
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

            IParameterSymbol? templateParameter = null;
            foreach (var parameter in candidate.Parameters)
            {
                if (parameter.Name == templateParameterName)
                {
                    templateParameter = parameter;
                    break;
                }
            }

            if (templateParameter is null)
            {
                continue;
            }

            foreach (var parameter in candidate.Parameters)
            {
                if (templateParameter.Ordinal <= parameter.Ordinal)
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
        if (containingSymbol is null || typeArg is null || typeArg.TypeKind == TypeKind.Error)
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
                typeArgument is null || typeArgument.TypeKind == TypeKind.Error)
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

    private static bool IsClearlyString(
        SemanticModel model,
        ExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        var constant = model.GetConstantValue(expression, cancellationToken);
        return constant is { HasValue: true, Value: string } ||
               model.GetTypeInfo(expression, cancellationToken).Type?.SpecialType == SpecialType.System_String;
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
        return IsOrDerivedFrom(type, baseType, depth: 0);
    }

    private static bool IsOrDerivedFrom(ITypeSymbol type, INamedTypeSymbol baseType, int depth)
    {
        if (depth >= 16)
        {
            return false;
        }

        if (type is ITypeParameterSymbol typeParameter)
        {
            foreach (var constraint in typeParameter.ConstraintTypes)
            {
                if (IsOrDerivedFrom(constraint, baseType, depth + 1))
                {
                    return true;
                }
            }
        }

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
