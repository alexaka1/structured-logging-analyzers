using Alexaka1.Analyzers.StructuredLogging.Parsing;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests;

public sealed class MessageTemplateParserTests
{
    [Fact]
    public void Empty_is_empty()
    {
        var parsed = MessageTemplateParser.Parse(string.Empty);
        Assert.Empty(parsed.Properties);
        Assert.Null(parsed.NamedProperties);
        Assert.Null(parsed.PositionalProperties);
    }

    [Fact]
    public void Text_only_has_no_holes()
    {
        var parsed = MessageTemplateParser.Parse("hello world");
        Assert.Empty(parsed.Properties);
        Assert.Same(ParsedTemplate.Empty, parsed);
    }

    [Fact]
    public void Simple_named_hole()
    {
        var parsed = MessageTemplateParser.Parse("Hello {Name}");
        Assert.NotNull(parsed.NamedProperties);
        Assert.Null(parsed.PositionalProperties);
        var hole = Assert.Single(parsed.NamedProperties!);
        Assert.Equal("Name", hole.PropertyName);
        Assert.Equal(DestructuringKind.Default, hole.Destructuring);
        Assert.Equal(6, hole.StartIndex);
        Assert.Equal("{Name}", hole.RawText);
        Assert.False(hole.IsPositional);
    }

    [Fact]
    public void Destructure_and_stringify_hints()
    {
        var parsed = MessageTemplateParser.Parse("{@User} {$User}");
        Assert.Equal(DestructuringKind.Destructure, parsed.NamedProperties![0].Destructuring);
        Assert.Equal(DestructuringKind.Stringify, parsed.NamedProperties[1].Destructuring);
        Assert.Equal("User", parsed.NamedProperties[0].PropertyName);
        Assert.Equal(2, parsed.NamedProperties[0].NameStartIndex);
        Assert.Equal(10, parsed.NamedProperties[1].NameStartIndex);
    }

    [Fact]
    public void Format_and_alignment()
    {
        var parsed = MessageTemplateParser.Parse("{Value,10:N2}");
        var hole = Assert.Single(parsed.NamedProperties!);
        Assert.Equal("Value", hole.PropertyName);
        Assert.Equal("10", hole.Alignment);
        Assert.Equal("N2", hole.Format);
    }

    [Fact]
    public void Escaped_braces_are_not_holes()
    {
        var parsed = MessageTemplateParser.Parse("Use {{Name}} as text");
        Assert.Empty(parsed.Properties);
    }

    [Fact]
    public void Triple_braces_contain_a_hole()
    {
        var parsed = MessageTemplateParser.Parse("{{{Name}}}");
        var hole = Assert.Single(parsed.NamedProperties!);
        Assert.Equal("Name", hole.PropertyName);
        Assert.Equal(2, hole.StartIndex);
    }

    [Fact]
    public void Escaped_closing_braces_after_a_hole_are_text()
    {
        var parsed = MessageTemplateParser.Parse("{Name}}}");
        var hole = Assert.Single(parsed.NamedProperties!);
        Assert.Equal("Name", hole.PropertyName);
    }

    [Fact]
    public void Positional_only_template()
    {
        var parsed = MessageTemplateParser.Parse("{0} {1}");
        Assert.Null(parsed.NamedProperties);
        Assert.NotNull(parsed.PositionalProperties);
        Assert.Equal(2, parsed.PositionalProperties!.Length);
        Assert.False(parsed.IsMixed);
    }

    [Fact]
    public void Mixed_template_is_named()
    {
        var parsed = MessageTemplateParser.Parse("{0} {Name}");
        Assert.NotNull(parsed.NamedProperties);
        Assert.Null(parsed.PositionalProperties);
        Assert.True(parsed.IsMixed);
        Assert.Equal(2, parsed.NamedProperties!.Length);
    }

    [Fact]
    public void Space_and_dot_names()
    {
        var parsed = MessageTemplateParser.Parse("{My Property} {My.Property}");
        Assert.Equal("My Property", parsed.NamedProperties![0].PropertyName);
        Assert.Equal("My.Property", parsed.NamedProperties[1].PropertyName);
    }

    [Fact]
    public void Malformed_holes_are_text()
    {
        Assert.Empty(MessageTemplateParser.Parse("{").Properties);
        Assert.Empty(MessageTemplateParser.Parse("{}").Properties);
        Assert.Empty(MessageTemplateParser.Parse("{@}").Properties);
        Assert.Empty(MessageTemplateParser.Parse("{Name").Properties);
        Assert.Empty(MessageTemplateParser.Parse("{Name,}").Properties);
        Assert.Empty(MessageTemplateParser.Parse("{Name:}").Properties);
        Assert.Empty(MessageTemplateParser.Parse("{Name,1:}").Properties);
        Assert.Empty(MessageTemplateParser.Parse("{Name,+1}").Properties);
        Assert.Empty(MessageTemplateParser.Parse("{Name,1-}").Properties);
    }

    [Fact]
    public void Unclosed_brace_does_not_throw()
    {
        var parsed = MessageTemplateParser.Parse("Hello {Name");
        Assert.Empty(parsed.Properties);
    }

    [Fact]
    public void Duplicate_names_are_preserved()
    {
        var parsed = MessageTemplateParser.Parse("{Test} {Test}");
        Assert.Equal(2, parsed.NamedProperties!.Length);
        Assert.Equal("Test", parsed.NamedProperties[0].PropertyName);
        Assert.Equal("Test", parsed.NamedProperties[1].PropertyName);
    }

    [Fact]
    public void Malformed_hole_does_not_hide_a_later_valid_hole()
    {
        var parsed = MessageTemplateParser.Parse("{Bad,} {Good}");
        var hole = Assert.Single(parsed.NamedProperties!);
        Assert.Equal("Good", hole.PropertyName);
    }

    [Fact]
    public void Format_accepts_every_character_except_closing_brace()
    {
        var parsed = MessageTemplateParser.Parse("{Value:€°∑}");
        var hole = Assert.Single(parsed.NamedProperties!);
        Assert.Equal("€°∑", hole.Format);
    }

    [Theory]
    [InlineData("{Value,0}", "0")]
    [InlineData("{Value,-0}", "-0")]
    [InlineData("{Value,999999999999999999999999999999}", "999999999999999999999999999999")]
    public void Alignment_follows_the_public_digit_grammar(string template, string expected)
    {
        var parsed = MessageTemplateParser.Parse(template);
        var hole = Assert.Single(parsed.NamedProperties!);
        Assert.Equal(expected, hole.Alignment);
    }
}
