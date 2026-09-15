namespace Portfolio.Content;

/// <summary>
/// A hyperlink attached to a content entry, e.g. source code or a write-up.
/// </summary>
public sealed class ContentLink
{
    public string Label { get; init; } = "";

    public string Url { get; init; } = "";

    /// <summary>Key into the icon sprite; falls back to a generic link glyph.</summary>
    public string? Icon { get; init; }
}
