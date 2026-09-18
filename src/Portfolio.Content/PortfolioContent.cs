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

    public RepositoryEntry[] Repositories { get; init; } = [];

    public Track[] Listening { get; init; } = [];

    public ActivitySnapshot[] Activity { get; init; } = [];

    public required ContentAsset[] Assets { get; init; }

    /// <summary>Pages that declared a <c>navLabel</c>, in page order.</summary>
    public IEnumerable<Page> NavPages => Pages.Where(p => !string.IsNullOrWhiteSpace(p.NavLabel));

    /// <summary>Featured projects, or all of them when none are marked featured.</summary>
    public IEnumerable<Project> FeaturedProjects =>
        Projects.Any(p => p.Featured) ? Projects.Where(p => p.Featured) : Projects;

    public string? ProjectListingRoute => SectionRoute("projects");

    public string? SectionRoute(string section) =>
        Pages.FirstOrDefault(p => p.Sections.Contains(section, StringComparer.OrdinalIgnoreCase))?.Route;

    public IEnumerable<EvidenceItem> Evidence
    {
        get
        {
            foreach (var project in Projects)
            {
                var url = project.HasBody ? $"/projects/{project.Slug}/"
                    : FeaturedProjects.Contains(project) ? SectionAnchor("projects", $"project-{project.Slug}") : null;
                yield return Item(project, EvidenceKind.Project, url);
            }
            foreach (var certification in Certifications)
                yield return Item(certification, EvidenceKind.Credential,
                    SectionAnchor("certifications", $"credential-{certification.Slug}"));
            foreach (var repository in Repositories)
                yield return Item(repository, EvidenceKind.Repository,
                    SectionAnchor("repositories", $"repository-{repository.Slug}"));
            foreach (var item in Now)
                yield return Item(item, EvidenceKind.Now, SectionAnchor("now", $"now-{item.Slug}"));
        }
    }

    private string? SectionAnchor(string section, string id) =>
        SectionRoute(section) is { } route ? $"{route}#{id}" : null;

    private static EvidenceItem Item(ContentEntry entry, EvidenceKind kind, string? url)
    {
        IEnumerable<string> topics = entry.Tags;
        if (entry is RepositoryEntry { Language: { } language } && !string.IsNullOrWhiteSpace(language))
            topics = topics.Append(language);
        return new(entry, kind, [.. topics.Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)], url ?? entry.Links.FirstOrDefault()?.Url);
    }

    /// <summary>Now items grouped under their heading, preserving file order within each group.</summary>
    public IEnumerable<IGrouping<string, NowItem>> NowByGroup =>
        Now.GroupBy(n => n.Group, StringComparer.Ordinal);
}
