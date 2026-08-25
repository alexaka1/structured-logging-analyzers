using Alexaka1.Analyzers.StructuredLogging.Classification;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests;

public sealed class PropertyNamingTests
{
    [Theory]
    [InlineData("myProperty", "MyProperty")]
    [InlineData("My.Property", "MyProperty")]
    [InlineData("My Property", "MyProperty")]
    [InlineData("test", "Test")]
    [InlineData("user_id", "UserId")]
    public void PascalCase(string input, string expected)
    {
        Assert.Equal(expected, PropertyNaming.Suggest(input, PropertyNamingStyle.PascalCase));
    }

    [Theory]
    [InlineData("myProperty", "myProperty")]
    [InlineData("UserId", "userId")]
    [InlineData("test", "test")]
    public void CamelCase(string input, string expected)
    {
        Assert.Equal(expected, PropertyNaming.Suggest(input, PropertyNamingStyle.CamelCase));
    }

    [Theory]
    [InlineData("myProperty", "my_property")]
    [InlineData("UserId", "user_id")]
    public void SnakeCase(string input, string expected)
    {
        Assert.Equal(expected, PropertyNaming.Suggest(input, PropertyNamingStyle.SnakeCase));
    }

    [Theory]
    [InlineData("myProperty", "my.property")]
    [InlineData("UserId", "user.id")]
    [InlineData("http.response.status_code", "http.response.status.code")]
    public void Elastic(string input, string expected)
    {
        Assert.Equal(expected, PropertyNaming.Suggest(input, PropertyNamingStyle.ElasticNaming));
    }

    [Theory]
    [InlineData("service.name", "service.name")]
    [InlineData("http.request.method", "http.request.method")]
    [InlineData("http.response.status_code", "http.response.status_code")]
    [InlineData("exception.type", "exception.type")]
    [InlineData("db.system.name", "db.system.name")]
    [InlineData("OrderId", "order_id")]
    [InlineData("http.response.StatusCode", "http.response.status_code")]
    [InlineData("myProperty", "my_property")]
    [InlineData("HTTP.StatusCode", "http.status_code")]
    [InlineData("ORDER_ID", "order_id")]
    [InlineData("order-id", "order_id")]
    [InlineData("service..name", "service.name")]
    [InlineData(".service.name.", "service.name")]
    [InlineData("caf\u00e9", "caf")]
    [InlineData("MyCaf\u00e9", "my_caf")]
    [InlineData("1name", "1name")]
    public void SemanticConventions(string input, string expected)
    {
        Assert.Equal(expected, PropertyNaming.Suggest(input, PropertyNamingStyle.SemanticConventions));
    }
}
