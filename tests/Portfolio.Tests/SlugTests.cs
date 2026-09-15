using Portfolio.Content;

namespace Portfolio.Tests;

public sealed class SlugTests
{
    [Theory]
    [InlineData("hiking", "hiking")]
    [InlineData("020-hiking", "hiking")]
    [InlineData("Playing Guitar", "playing-guitar")]
    [InlineData("DAS Driver Windows/NetBSD", "das-driver-windows-netbsd")]
    [InlineData("Cloud & Platform", "cloud-platform")]
    [InlineData("  spaced  out  ", "spaced-out")]
    public void Produces_url_safe_slugs(string input, string expected) =>
        Assert.Equal(expected, Slug.From(input));

    [Fact]
    public void Keeps_a_numeric_name_that_is_not_a_sort_prefix() =>
        Assert.Equal("2024", Slug.From("2024"));

    [Theory]
    [InlineData("020-hiking", 20)]
    [InlineData("hiking", null)]
    [InlineData("2024-retrospective", 2024)]
    public void Reads_the_sort_prefix(string input, int? expected) =>
        Assert.Equal(expected, Slug.SortPrefix(input));
}
