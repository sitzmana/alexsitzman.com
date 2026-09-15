namespace Portfolio.Content;

/// <summary>
/// Splits a content file into its YAML front matter and Markdown body.
/// </summary>
public static class FrontMatter
{
    private const string Fence = "---";

    /// <summary>
    /// Separates the leading <c>---</c> fenced YAML block from the remaining body.
    /// A file with no front matter is returned whole as the body.
    /// </summary>
    public static (string Yaml, string Body) Split(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // A UTF-8 BOM survives File.ReadAllText and would hide the opening fence.
        var content = text.TrimStart('\uFEFF');
        using var reader = new StringReader(content);

        var first = reader.ReadLine();
        if (first is null || first.TrimEnd() != Fence)
        {
            return ("", content);
        }

        var yaml = new StringWriter();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.TrimEnd() == Fence)
            {
                return (yaml.ToString(), reader.ReadToEnd());
            }

            yaml.WriteLine(line);
        }

        throw new FormatException(
            "Front matter opened with '---' but was never closed. Add a closing '---' line.");
    }
}
