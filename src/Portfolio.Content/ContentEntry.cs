namespace Portfolio.Content;

/// <summary>
/// Fields every content file supports. Collection-specific types add to these.
/// </summary>
public abstract class ContentEntry
{
    /// <summary>URL segment, derived from the file or folder name unless overridden.</summary>
    public string Slug { get; set; } = "";

    /// <summary>Repository-relative path of the source file, used in diagnostics.</summary>
    public string SourcePath { get; set; } = "";

    public string Title { get; init; } = "";

    /// <summary>Short plain-text description shown in listings.</summary>
    public string? Summary { get; init; }

    public string[] Tags { get; init; } = [];

    public ContentLink[] Links { get; init; } = [];

    /// <summary>
    /// Explicit sort position. Shares one number line with the <c>020-</c> file-name
    /// prefix and overrides it. Unset entries sort after positioned ones, by title.
    /// </summary>
    public int? Order { get; init; }

    /// <summary>Resolved sort position: <see cref="Order"/>, else the file prefix, else last.</summary>
    public int SortKey { get; set; } = int.MaxValue;

    /// <summary>Excluded from the build entirely while true.</summary>
    public bool Draft { get; init; }

    /// <summary>Markdown body rendered to HTML. Empty when the file has no body.</summary>
    public string BodyHtml { get; set; } = "";

    /// <summary>True when the file has prose beyond its front matter.</summary>
    public bool HasBody => BodyHtml.Length > 0;
}
