namespace Portfolio.Tests;

public sealed class FrontMatterTests
{
    [Fact]
    public void Splits_yaml_and_body()
    {
        var (yaml, body) = Portfolio.Content.FrontMatter.Split("---\ntitle: Hi\n---\nBody text\n");

        Assert.Equal("title: Hi\n", yaml.ReplaceLineEndings("\n"));
        Assert.Equal("Body text\n", body.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Treats_a_file_without_front_matter_as_all_body()
    {
        var (yaml, body) = Portfolio.Content.FrontMatter.Split("Just prose.");

        Assert.Empty(yaml);
        Assert.Equal("Just prose.", body);
    }

    [Fact]
    public void Tolerates_a_utf8_bom_before_the_opening_fence()
    {
        var (yaml, _) = Portfolio.Content.FrontMatter.Split("\uFEFF---\ntitle: Hi\n---\n");

        Assert.Contains("title: Hi", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_unterminated_front_matter()
    {
        var ex = Assert.Throws<FormatException>(
            () => Portfolio.Content.FrontMatter.Split("---\ntitle: Hi\nno closing fence"));

        Assert.Contains("never closed", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Allows_an_empty_body()
    {
        var (yaml, body) = Portfolio.Content.FrontMatter.Split("---\ntitle: Hi\n---\n");

        Assert.Contains("title: Hi", yaml, StringComparison.Ordinal);
        Assert.Equal("", body.Trim());
    }
}
