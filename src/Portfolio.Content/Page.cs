namespace Portfolio.Content;

/// <summary>
/// A routable page. Source: <c>content/pages/</c>. Adding a file here adds a route.
/// </summary>
public sealed class Page : ContentEntry
{
    /// <summary>
    /// Section keys rendered in order beneath the page body. Each key maps to a
    /// component in the renderer's section registry; unknown keys fail the build.
    /// </summary>
    public string[] Sections { get; init; } = [];

    /// <summary>Overrides the &lt;title&gt; tag. Defaults to "<see cref="ContentEntry.Title"/> — site name".</summary>
    public string? MetaTitle { get; init; }

    /// <summary>Overrides the meta description. Defaults to the site description.</summary>
    public string? MetaDescription { get; init; }

    /// <summary>Visible heading. Defaults to <see cref="ContentEntry.Title"/>; blank hides it.</summary>
    public string? Heading { get; init; }

    /// <summary>Short kicker shown above the heading.</summary>
    public string? Eyebrow { get; init; }

    /// <summary>Label used in the primary navigation. Omit to keep the page out of the nav.</summary>
    public string? NavLabel { get; init; }

    /// <summary>Route path, e.g. "/" or "/about/". Derived from the slug.</summary>
    public string Route { get; set; } = "/";

    /// <summary>Excludes the page from sitemap.xml.</summary>
    public bool NoIndex { get; init; }
}
