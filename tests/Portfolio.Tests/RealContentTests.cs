using Portfolio.Components;
using Portfolio.Content;

namespace Portfolio.Tests;

/// <summary>
/// Loads the repository's real <c>content/</c> tree. This is the guard that makes the
/// file-driven workflow safe: a malformed or mis-keyed content file fails CI here,
/// with the offending path, instead of silently vanishing from the site.
/// </summary>
public sealed class RealContentTests
{
    private static readonly PortfolioContent Content = new ContentLoader(RepositoryPaths.ContentRoot).LoadAll();

    [Fact]
    public void Every_content_file_parses() => Assert.NotNull(Content);

    [Fact]
    public void Site_config_has_the_fields_the_layout_depends_on()
    {
        Assert.False(string.IsNullOrWhiteSpace(Content.Site.Name));
        Assert.False(string.IsNullOrWhiteSpace(Content.Site.Title));
        Assert.False(string.IsNullOrWhiteSpace(Content.Site.Initials));
        Assert.StartsWith("https://", Content.Site.BaseUrl, StringComparison.Ordinal);
    }

    [Fact]
    public void Home_about_and_now_pages_exist() =>
        Assert.All(new[] { "/", "/about/", "/now/" },
            route => Assert.Contains(Content.Pages, item => item.Route == route));

    [Fact]
    public void The_site_tour_is_a_content_page_with_derived_navigation()
    {
        var tour = Assert.Single(Content.Pages, item => item.Route == "/site/");

        Assert.Equal("This site", tour.NavLabel);
        Assert.True(tour.BodyHeadings.Length >= 3);
        Assert.NotNull(Content.Site.PageNavigation);
        Assert.All(tour.BodyHeadings, heading => Assert.Contains($"id=\"{heading.Id}\"", tour.BodyHtml, StringComparison.Ordinal));
    }

    [Fact]
    public void Every_page_section_key_is_known()
    {
        var unknown = Content.Pages
            .SelectMany(p => p.Sections.Select(s => (p.SourcePath, Section: s)))
            .Where(x => !SectionRegistry.IsKnown(x.Section))
            .ToArray();

        Assert.Empty(unknown);
    }

    [Fact]
    public void Collections_carried_over_from_the_previous_site_are_present()
    {
        Assert.All(new[] { "virtual-internet-project", "kubernetes-raspberry-pi-cluster", "das-driver" },
            slug => Assert.Contains(Content.Projects, project => project.Slug == slug));
        Assert.Contains(Content.Certifications, cert => cert.Slug == "certified-kubernetes-administrator");
        Assert.Contains(Content.Certifications, cert => cert.Slug == "certified-linux-systems-administrator");
        Assert.NotEmpty(Content.Now);
        Assert.NotEmpty(Content.Skills);
        Assert.Equal(3, Content.Interests.Length);
        Assert.NotEmpty(Content.Facts);
        Assert.All(new[] { "website", "mariaheschelescom", "gomud", "dot-files", "terraform" },
            slug => Assert.Contains(Content.Repositories, repository => repository.Slug == slug));
        Assert.All(new[] { "gomud", "terraform" },
            slug => Assert.Contains(Content.Repositories, repository => repository.Slug == slug && repository.Fork));
        Assert.Equal(12, Content.Listening.Length);
        Assert.Equal(6, Assert.Single(Content.Activity).Days.Sum(day => day.Count));
    }

    [Fact]
    public void Every_skill_from_the_previous_site_survived_the_regrouping()
    {
        string[] expected =
        [
            "Kubernetes", "Azure", "DevOps", "Network Security", "Go", "Python",
            "AWS", "JavaScript", "Git", "CI/CD", "C++", "MicroServices",
        ];

        var actual = Content.Skills.SelectMany(g => g.Items).ToArray();

        Assert.All(expected, skill => Assert.Contains(skill, actual, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Every_entry_has_a_non_empty_slug() =>
        Assert.All(AllEntries(), e => Assert.False(string.IsNullOrWhiteSpace(e.Slug), e.SourcePath));

    [Fact]
    public void Every_listed_entry_has_a_summary_or_a_body()
    {
        var bare = Content.Projects.Cast<ContentEntry>()
            .Concat(Content.Now)
            .Where(e => string.IsNullOrWhiteSpace(e.Summary) && !e.HasBody)
            .Select(e => e.SourcePath);

        Assert.Empty(bare);
    }

    [Fact]
    public void Every_link_is_absolute_https_or_a_mailto()
    {
        var bad = AllEntries()
            .SelectMany(e => e.Links.Select(l => (e.SourcePath, l.Url)))
            .Concat(Content.Site.Social.Select(l => ("content/site.yml", l.Url)))
            .Where(x => !x.Url.StartsWith("https://", StringComparison.Ordinal)
                        && !x.Url.StartsWith("mailto:", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(bad);
    }

    [Fact]
    public void Every_link_has_a_label()
    {
        var unlabelled = AllEntries()
            .SelectMany(e => e.Links.Select(l => (e.SourcePath, l.Label)))
            .Where(x => string.IsNullOrWhiteSpace(x.Label));

        Assert.Empty(unlabelled);
    }

    private static IEnumerable<ContentEntry> AllEntries() =>
    [
        .. Content.Pages,
        .. Content.Projects.Cast<ContentEntry>(),
        .. Content.Certifications,
        .. Content.Now,
        .. Content.Skills,
        .. Content.Interests,
        .. Content.Facts,
        .. Content.Repositories,
        .. Content.Listening,
        .. Content.Activity,
    ];
}
