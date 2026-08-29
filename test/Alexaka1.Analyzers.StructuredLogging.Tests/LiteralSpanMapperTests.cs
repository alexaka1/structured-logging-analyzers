using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Alexaka1.Analyzers.StructuredLogging.Mapping;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests;

public sealed class LiteralSpanMapperTests
{
    [Fact]
    public void Regular_string_maps_hole()
    {
        var map = Map("class C { string S = \"Hello {Name}\"; }");
        var start = map.Value.IndexOf("{Name}", StringComparison.Ordinal);
        var span = map.TryGetSpan(start, "{Name}".Length);
        Assert.NotNull(span);
        var source = map.Expression.SyntaxTree.GetText(TestContext.Current.CancellationToken).ToString(span.Value);
        Assert.Equal("{Name}", source);
    }

    [Fact]
    public void Escaped_newline_does_not_use_raw_offsets()
    {
        var map = Map("class C { string S = \"Escaped \\r\\n {Name}\"; }");
        var start = map.Value.IndexOf("{Name}", StringComparison.Ordinal);
        var span = map.TryGetSpan(start, "{Name}".Length);
        Assert.NotNull(span);
        var source = map.Expression.SyntaxTree.GetText(TestContext.Current.CancellationToken).ToString(span.Value);
        Assert.Equal("{Name}", source);
    }

    [Fact]
    public void Verbatim_string_maps_hole()
    {
        var map = Map("class C { string S = @\"Hello {Name}\"; }");
        var start = map.Value.IndexOf("{Name}", StringComparison.Ordinal);
        var span = map.TryGetSpan(start, "{Name}".Length);
        var source = map.Expression.SyntaxTree.GetText(TestContext.Current.CancellationToken).ToString(span!.Value);
        Assert.Equal("{Name}", source);
    }

    [Fact]
    public void Concatenation_maps_second_fragment()
    {
        var map = Map("class C { string S = \"Test\" + \" {Name} prop\"; }");
        var start = map.Value.IndexOf("{Name}", StringComparison.Ordinal);
        var span = map.TryGetSpan(start, "{Name}".Length);
        var source = map.Expression.SyntaxTree.GetText(TestContext.Current.CancellationToken).ToString(span!.Value);
        Assert.Equal("{Name}", source);
    }

    [Fact]
    public void Raw_string_maps_hole()
    {
        var map = Map("class C { string S = \"\"\"Hello {Name}\"\"\"; }");
        var start = map.Value.IndexOf("{Name}", StringComparison.Ordinal);
        var span = map.TryGetSpan(start, "{Name}".Length);
        var source = map.Expression.SyntaxTree.GetText(TestContext.Current.CancellationToken).ToString(span!.Value);
        Assert.Equal("{Name}", source);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void Multiline_raw_string_maps_indented_hole(string newLine)
    {
        var source = /*lang=csharp*/ """"
                                     class C
                                     {
                                         string S = """
                                             Hello
                                               {Name}
                                             """;
                                     }
                                     """".Replace("\n", newLine, StringComparison.Ordinal);
        var map = Map(source);
        var start = map.Value.IndexOf("{Name}", StringComparison.Ordinal);
        var span = map.TryGetSpan(start, "{Name}".Length);
        Assert.NotNull(span);
        var mappedSource = map.Expression.SyntaxTree.GetText(TestContext.Current.CancellationToken)
            .ToString(span.Value);
        Assert.Equal("{Name}", mappedSource);
    }

    [Fact]
    public void Multiline_raw_string_maps_hole_on_first_content_line()
    {
        var source = /*lang=csharp*/ """"
                                     class C
                                     {
                                         string S = """
                                             {Name}
                                             """;
                                     }
                                     """";
        AssertMappedHole(source, "{Name}");
    }

    [Fact]
    public void Extra_quote_raw_string_maps_hole()
    {
        var source = /*lang=csharp*/ """""
                                     class C
                                     {
                                         string S = """"
                                             Hello {Name}
                                             """";
                                     }
                                     """"";
        AssertMappedHole(source, "{Name}");
    }

    [Fact]
    public void Regular_string_maps_unicode_escaped_braces()
    {
        var map = Map("class C { string S = \"\\u007BName\\u007D\"; }");
        Assert.Equal("{Name}", map.Value);
        var start = map.Value.IndexOf("{Name}", StringComparison.Ordinal);
        var span = map.TryGetSpan(start, "{Name}".Length);
        Assert.NotNull(span);
        var source = map.Expression.SyntaxTree.GetText(TestContext.Current.CancellationToken).ToString(span.Value);
        Assert.Equal("\\u007BName\\u007D", source);
    }

    [Fact]
    public void Concatenation_maps_hole_split_across_fragments()
    {
        var map = Map("class C { string S = \"Hel{\" + \"Name}\"; }");
        var start = map.Value.IndexOf("{Name}", StringComparison.Ordinal);
        Assert.Equal("{Name}", map.Value.Substring(start, "{Name}".Length));
        var text = map.Expression.SyntaxTree.GetText(TestContext.Current.CancellationToken);
        Assert.Equal("{", text.ToString(map.Map[start].SourceSpan));
        Assert.Equal("}", text.ToString(map.Map[start + "{Name}".Length - 1].SourceSpan));
    }

    private static TemplateSourceMap Map(string source)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "MapTests",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot(cancellationToken);
        ExpressionSyntax expression = root.DescendantNodes().OfType<BinaryExpressionSyntax>().FirstOrDefault()
                                      ?? (ExpressionSyntax)root.DescendantNodes().OfType<LiteralExpressionSyntax>()
                                          .First();
        Assert.True(LiteralSpanMapper.TryMap(model, expression, cancellationToken, out var map));
        return map;
    }

    private static void AssertMappedHole(string source, string hole)
    {
        var map = Map(source);
        var start = map.Value.IndexOf(hole, StringComparison.Ordinal);
        var span = map.TryGetSpan(start, hole.Length);
        Assert.NotNull(span);
        var mappedSource = map.Expression.SyntaxTree.GetText(TestContext.Current.CancellationToken)
            .ToString(span.Value);
        Assert.Equal(hole, mappedSource);
    }
}
