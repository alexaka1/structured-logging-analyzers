using Alexaka1.Analyzers.StructuredLogging.Classification;

using Microsoft.CodeAnalysis.CSharp;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests;

public sealed class ExpressionPropertyNameTests
{
    [Theory]
    [InlineData("orderId", "OrderId", "OrderId")]
    [InlineData("order.Id", "Id", "OrderId")]
    [InlineData("order?.Id", "Id", "OrderId")]
    [InlineData("order.GetId()", "Id", "OrderId")]
    [InlineData("nameof(order.Id)", "Id", "OrderId")]
    [InlineData("nameof(OrderId)", "OrderId", "OrderId")]
    [InlineData("new Order()", "Order", "Order")]
    [InlineData("(int)orderId", "OrderId", "OrderId")]
    public void Suggests_leaf_and_qualified_pascal_names(string expression, string leaf, string qualified)
    {
        var syntax = SyntaxFactory.ParseExpression(expression);
        Assert.Equal(leaf,
            ExpressionPropertyName.TrySuggest(syntax, PropertyNamingStyle.PascalCase,
                ExpressionPropertyName.Kind.Leaf));
        Assert.Equal(qualified,
            ExpressionPropertyName.TrySuggest(syntax, PropertyNamingStyle.PascalCase,
                ExpressionPropertyName.Kind.Qualified));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("\"text\"")]
    [InlineData("true")]
    [InlineData("this")]
    [InlineData("new { Test = 1 }")]
    public void Withholds_when_no_identifier(string expression)
    {
        var syntax = SyntaxFactory.ParseExpression(expression);
        Assert.Null(ExpressionPropertyName.TrySuggest(syntax, PropertyNamingStyle.PascalCase,
            ExpressionPropertyName.Kind.Leaf));
        Assert.Null(ExpressionPropertyName.TrySuggest(syntax, PropertyNamingStyle.PascalCase,
            ExpressionPropertyName.Kind.Qualified));
    }

    [Fact]
    public void Uniquify_appends_numeric_suffix()
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        Assert.Equal("Id", ExpressionPropertyName.Uniquify("Id", used));
        Assert.Equal("Id2", ExpressionPropertyName.Uniquify("Id", used));
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("12", true)]
    [InlineData("Id", false)]
    [InlineData("Id2", false)]
    [InlineData("", false)]
    public void Detects_positional_names(string name, bool expected)
    {
        Assert.Equal(expected, ExpressionPropertyName.IsPositionalName(name));
    }
}
