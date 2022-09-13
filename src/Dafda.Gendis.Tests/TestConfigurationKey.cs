using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dafda.Gendis.App.Configuration;
using Xunit;

namespace Dafda.Gendis.Tests;

public class TestConfigurationKey
{
    [Theory]
    [InlineData("foo")]
    [InlineData("bar")]
    [InlineData("baz")]
    [InlineData("qux")]
    public void parse_returns_expected_key(string expected)
    {
        var result = ConfigurationKey.Parse(expected, "dummy");
        Assert.Equal(expected, result.Key);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("bar")]
    [InlineData("baz")]
    [InlineData("qux")]
    public void parse_returns_expected_value(string expected)
    {
        var result = ConfigurationKey.Parse("dummy", expected);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("FOO", "foo")]
    [InlineData("fOO", "foo")]
    [InlineData("FoO", "foo")]
    [InlineData("BAR", "bar")]
    public void parse_returns_expected_casing_of_key(string input, string expected)
    {
        var result = ConfigurationKey.Parse(input, "dummy");
        Assert.Equal(expected, result.Key);
    }

    [Theory]
    [InlineData("FOO_BAR", "foo.bar")]
    [InlineData("FOO__BAR", "foo.bar")]
    public void parse_returns_key_with_expected_separator_convention(string input, string expected)
    {
        var result = ConfigurationKey.Parse(input, "dummy");
        Assert.Equal(expected, result.Key);
    }

    [Theory]
    [InlineData("XXX_FOO", "XXX_", "foo")]
    [InlineData("YYY_BAR", "YYY_", "bar")]
    public void parse_returns_expected_key_when_defined_with_prefix_convention(string input, string prefix, string expected)
    {
        var result = ConfigurationKey.Parse(input, "dummy", prefix);
        Assert.Equal(expected, result.Key);
    }
}