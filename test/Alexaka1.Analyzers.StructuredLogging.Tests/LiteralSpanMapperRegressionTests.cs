using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Alexaka1.Analyzers.StructuredLogging.Mapping;
using Alexaka1.Analyzers.StructuredLogging.Parsing;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests;

public sealed class LiteralSpanMapperRegressionTests
{
    [Fact]
    public void Constant_interpolated_text_maps_period_to_literal_character()
    {
        var (tree, model, expression) = ParseExpression("class C { string S = $\"Operation done.\"; }");

        Assert.True(LiteralSpanMapper.TryMap(model, expression, TestContext.Current.CancellationToken, out var map));
        Assert.Equal("Operation done.", map.Value);
        Assert.True(map.AllowRewrite);

        var period = map.TryGetSpan(map.Value.Length - 1, 1);
        Assert.NotNull(period);
        Assert.Equal(".", tree.GetText(TestContext.Current.CancellationToken).ToString(period.Value));
    }

    [Fact]
    public void Constant_interpolated_text_unescapes_braces_for_template_parsing()
    {
        var (_, model, expression) = ParseExpression("class C { string S = $\"user {{userId}}\"; }");

        Assert.True(LiteralSpanMapper.TryMap(model, expression, TestContext.Current.CancellationToken, out var map));
        Assert.Equal("user {userId}", map.Value);

        var parsed = MessageTemplateParser.Parse(map.Value);
        var hole = Assert.Single(parsed.Properties);
        Assert.Equal("userId", hole.PropertyName);
        Assert.Equal("{{userId}}", expression.SyntaxTree.GetText(TestContext.Current.CancellationToken)
            .ToString(map.TryGetSpan(hole.StartIndex, hole.Length)!.Value));
    }

    [Fact]
    public void Regular_constant_interpolated_text_maps_csharp_escapes()
    {
        var source = "class C { string S = $\"line\\n \\\"done\\\" \\\\ path.\"; }";
        var (tree, model, expression) = ParseExpression(source);

        AssertMappedPeriod(tree, model, expression, "line\n \"done\" \\ path.");
    }

    [Fact]
    public void Verbatim_constant_interpolated_text_maps_quotes_and_backslashes()
    {
        var source = "class C { string S = $@\"line \"\"done\"\" \\\\ path.\"; }";
        var (tree, model, expression) = ParseExpression(source);

        AssertMappedPeriod(tree, model, expression, "line \"done\" \\\\ path.");
    }

    [Fact]
    public void Raw_constant_interpolated_text_maps_quotes_and_backslashes()
    {
        var source = /*lang=csharp*/ """"
                                     class C { string S = $"""line \n "done" \\ path."""; }
                                     """";
        var (tree, model, expression) = ParseExpression(source);

        AssertMappedPeriod(tree, model, expression, "line \\n \"done\" \\\\ path.");
    }

    [Fact]
    public void Multiline_raw_constant_interpolated_text_maps_indentation()
    {
        var source = /*lang=csharp*/ """"
                                     class C
                                     {
                                         string S = $"""
                                             line
                                               done.
                                             """;
                                     }
                                     """";
        var (tree, model, expression) = ParseExpression(source);

        AssertMappedPeriod(tree, model, expression, "line\n  done.");
    }

    private static void AssertMappedPeriod(
        SyntaxTree tree,
        SemanticModel model,
        InterpolatedStringExpressionSyntax expression,
        string expectedValue)
    {
        Assert.True(LiteralSpanMapper.TryMap(model, expression, TestContext.Current.CancellationToken, out var map));
        Assert.Equal(expectedValue, map.Value);
        Assert.True(map.AllowRewrite);

        var period = map.TryGetSpan(map.Value.Length - 1, 1);
        Assert.NotNull(period);
        Assert.Equal(".", tree.GetText(TestContext.Current.CancellationToken).ToString(period.Value));
    }

    private static (SyntaxTree Tree, SemanticModel Model, InterpolatedStringExpressionSyntax Expression)
        ParseExpression(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "MapperRegressionTests",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree);
        var expression = tree.GetRoot().DescendantNodes().OfType<InterpolatedStringExpressionSyntax>().Single();
        return (tree, model, expression);
    }
}
