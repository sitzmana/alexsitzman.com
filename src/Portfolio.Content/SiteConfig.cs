namespace Portfolio.Content;

/// <summary>
/// Site-wide settings. Source: <c>content/site.yml</c>.
/// </summary>
public sealed class SiteConfig
{
    public string Name { get; init; } = "";

    /// <summary>Wordmark shown in the header.</summary>
    public string Initials { get; init; } = "";

    /// <summary>Professional headline shown in the hero.</summary>
    public string Title { get; init; } = "";

    public string? Location { get; init; }

    public string? Email { get; init; }

    /// <summary>Absolute production origin, used for canonical URLs and the sitemap.</summary>
    public string BaseUrl { get; init; } = "";

    /// <summary>Meta description fallback for pages that do not set one.</summary>
    public string Description { get; init; } = "";

    public ContentLink[] Social { get; init; } = [];
}
