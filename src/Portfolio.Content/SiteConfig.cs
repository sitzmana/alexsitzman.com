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

    public HeroContent Hero { get; init; } = new();

    public ExplorerCopy? Explorer { get; init; }

    public PageNavigationCopy? PageNavigation { get; init; }

    public Dictionary<string, SectionCopy> Sections { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class PageNavigationCopy
{
    public string ContentsLabel { get; init; } = "";

    public string BackToTopLabel { get; init; } = "";
}

public sealed class HeroContent
{
    public string? Greeting { get; init; }

    public string? Summary { get; init; }

    public string? SceneLabel { get; init; }

    public string? SceneHint { get; init; }

    public ContentLink[] Links { get; init; } = [];
}

public sealed class SectionCopy
{
    public ContentLink[] Links { get; init; } = [];

    public string? Label { get; init; }

    public string Title { get; init; } = "";

    public string? Lead { get; init; }

    public string? Note { get; init; }

    public string? PreviousLabel { get; init; }

    public string? NextLabel { get; init; }
}
