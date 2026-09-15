namespace Portfolio.Content;

/// <summary>
/// Everything loaded from <c>content/</c>, passed to components as a single value.
/// </summary>
public sealed class PortfolioContent
{
    public required SiteConfig Site { get; init; }

    public required Page[] Pages { get; init; }

    public required Project[] Projects { get; init; }

    public required Certification[] Certifications { get; init; }

    public required NowItem[] Now { get; init; }

    public required SkillGroup[] Skills { get; init; }

    public required Interest[] Interests { get; init; }

    public required Fact[] Facts { get; init; }

    public required ContentAsset[] Assets { get; init; }

    /// <summary>Pages that declared a <c>navLabel</c>, in page order.</summary>
    public IEnumerable<Page> NavPages => Pages.Where(p => !string.IsNullOrWhiteSpace(p.NavLabel));

    /// <summary>Featured projects, or all of them when none are marked featured.</summary>
    public IEnumerable<Project> FeaturedProjects =>
        Projects.Any(p => p.Featured) ? Projects.Where(p => p.Featured) : Projects;

    /// <summary>Now items grouped under their heading, preserving file order within each group.</summary>
    public IEnumerable<IGrouping<string, NowItem>> NowByGroup =>
        Now.GroupBy(n => n.Group, StringComparer.Ordinal);
}
