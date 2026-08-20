// Copyright (c) 2026 alexaka1

using StructuredLogging.Analyzers.Classification;
using Xunit;

namespace StructuredLogging.Tests;

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
    public void Elastic(string input, string expected)
    {
        Assert.Equal(expected, PropertyNaming.Suggest(input, PropertyNamingStyle.ElasticNaming));
    }
}
