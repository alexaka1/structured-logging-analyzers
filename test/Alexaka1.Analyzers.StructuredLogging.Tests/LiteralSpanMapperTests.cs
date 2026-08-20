// Copyright (c) 2026 alexaka1

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
        var source = map.Expression.SyntaxTree.GetText().ToString(span!.Value);
        Assert.Equal("{Name}", source);
    }

    [Fact]
    public void Escaped_newline_does_not_use_raw_offsets()
    {
        var map = Map("class C { string S = \"Escaped \\r\\n {Name}\"; }");
        var start = map.Value.IndexOf("{Name}", StringComparison.Ordinal);
        var span = map.TryGetSpan(start, "{Name}".Length);
        Assert.NotNull(span);
        var source = map.Expression.SyntaxTree.GetText().ToString(span!.Value);
        Assert.Equal("{Name}", source);
    }

    [Fact]
    public void Verbatim_string_maps_hole()
    {
        var map = Map("class C { string S = @\"Hello {Name}\"; }");
        var start = map.Value.IndexOf("{Name}", StringComparison.Ordinal);
        var span = map.TryGetSpan(start, "{Name}".Length);
        var source = map.Expression.SyntaxTree.GetText().ToString(span!.Value);
        Assert.Equal("{Name}", source);
    }

    [Fact]
    public void Concatenation_maps_second_fragment()
    {
        var map = Map("class C { string S = \"Test\" + \" {Name} prop\"; }");
        var start = map.Value.IndexOf("{Name}", StringComparison.Ordinal);
        var span = map.TryGetSpan(start, "{Name}".Length);
        var source = map.Expression.SyntaxTree.GetText().ToString(span!.Value);
        Assert.Equal("{Name}", source);
    }

    [Fact]
    public void Raw_string_maps_hole()
    {
        var map = Map("class C { string S = \"\"\"Hello {Name}\"\"\"; }");
        var start = map.Value.IndexOf("{Name}", StringComparison.Ordinal);
        var span = map.TryGetSpan(start, "{Name}".Length);
        var source = map.Expression.SyntaxTree.GetText().ToString(span!.Value);
        Assert.Equal("{Name}", source);
    }

    private static TemplateSourceMap Map(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "MapTests",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();
        ExpressionSyntax expression = root.DescendantNodes().OfType<BinaryExpressionSyntax>().FirstOrDefault()
            ?? (ExpressionSyntax)root.DescendantNodes().OfType<LiteralExpressionSyntax>().First();
        Assert.True(LiteralSpanMapper.TryMap(model, expression, CancellationToken.None, out var map));
        return map;
    }
}
