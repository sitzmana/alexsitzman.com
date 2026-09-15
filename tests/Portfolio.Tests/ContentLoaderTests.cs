using Portfolio.Content;

namespace Portfolio.Tests;

/// <summary>
/// Exercises the loader against temporary content trees so the rules that content
/// authors rely on are pinned down: ordering, slugs, drafts, and loud failures.
/// </summary>
public sealed class ContentLoaderTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("portfolio-content-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private void Write(string relativePath, string text)
    {
        var full = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    private ContentLoader Loader(bool includeDrafts = false) => new(_root, includeDrafts);

    [Fact]
    public void Missing_collection_folder_yields_no_entries() =>
        Assert.Empty(Loader().LoadCollection<Project>("projects"));

    [Fact]
    public void Numeric_prefix_orders_entries_and_is_stripped_from_the_slug()
    {
        Write("projects/030-gamma.md", "---\ntitle: Gamma\n---\n");
        Write("projects/010-alpha.md", "---\ntitle: Alpha\n---\n");
        Write("projects/020-beta.md", "---\ntitle: Beta\n---\n");

        var projects = Loader().LoadCollection<Project>("projects");

        Assert.Equal(["alpha", "beta", "gamma"], projects.Select(p => p.Slug));
    }

    [Fact]
    public void Explicit_order_wins_over_the_file_prefix()
    {
        Write("projects/010-alpha.md", "---\ntitle: Alpha\norder: 99\n---\n");
        Write("projects/020-beta.md", "---\ntitle: Beta\n---\n");

        var projects = Loader().LoadCollection<Project>("projects");

        Assert.Equal(["Beta", "Alpha"], projects.Select(p => p.Title));
    }

    [Fact]
    public void Draft_entries_are_excluded_unless_requested()
    {
        Write("projects/a.md", "---\ntitle: Shipped\n---\n");
        Write("projects/b.md", "---\ntitle: Hidden\ndraft: true\n---\n");

        Assert.Equal(["Shipped"], Loader().LoadCollection<Project>("projects").Select(p => p.Title));
        Assert.Equal(2, Loader(includeDrafts: true).LoadCollection<Project>("projects").Length);
    }

    [Fact]
    public void Body_markdown_becomes_html()
    {
        Write("projects/a.md", "---\ntitle: A\n---\nSome **bold** prose.\n");

        var project = Loader().LoadCollection<Project>("projects").Single();

        Assert.Contains("<strong>bold</strong>", project.BodyHtml, StringComparison.Ordinal);
        Assert.True(project.HasBody);
    }

    [Fact]
    public void An_entry_without_a_body_reports_no_body()
    {
        Write("projects/a.md", "---\ntitle: A\n---\n");

        Assert.False(Loader().LoadCollection<Project>("projects").Single().HasBody);
    }

    [Fact]
    public void Raw_html_in_content_is_not_passed_through()
    {
        Write("projects/a.md", "---\ntitle: A\n---\n<script>alert(1)</script>\n");

        var html = Loader().LoadCollection<Project>("projects").Single().BodyHtml;

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Folder_style_entries_collect_sibling_assets()
    {
        Write("projects/010-pi/index.md", "---\ntitle: Pi\n---\n![Rack](rack.avif)\n");
        Write("projects/010-pi/rack.avif", "binary-ish");

        var loader = Loader();
        var project = loader.LoadCollection<Project>("projects").Single();

        Assert.Equal("pi", project.Slug);
        Assert.Equal("projects/pi/rack.avif", Assert.Single(loader.Assets).OutputPath);
    }

    [Fact]
    public void Typed_fields_bind_from_front_matter()
    {
        Write(
            "projects/a.md",
            """
            ---
            title: Pi Cluster
            featured: true
            summary: A short line.
            tags: [Kubernetes, Security]
            links:
              - label: Source code
                url: https://example.invalid/repo
                icon: github
            ---
            """);

        var project = Loader().LoadCollection<Project>("projects").Single();

        Assert.True(project.Featured);
        Assert.Equal("A short line.", project.Summary);
        Assert.Equal(["Kubernetes", "Security"], project.Tags);
        Assert.Equal("Source code", Assert.Single(project.Links).Label);
    }

    [Fact]
    public void A_misspelled_front_matter_key_fails_the_build()
    {
        Write("projects/a.md", "---\ntitle: A\nfeatuerd: true\n---\n");

        var ex = Assert.Throws<ContentException>(() => Loader().LoadCollection<Project>("projects"));

        Assert.Contains("projects/a.md", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_title_fails_with_the_file_name()
    {
        Write("projects/a.md", "---\nsummary: no title here\n---\n");

        var ex = Assert.Throws<ContentException>(() => Loader().LoadCollection<Project>("projects"));

        Assert.Contains("missing a 'title'", ex.Message, StringComparison.Ordinal);
        Assert.Contains("content/projects/a.md", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Colliding_slugs_fail_rather_than_overwrite()
    {
        Write("projects/010-pi.md", "---\ntitle: One\n---\n");
        Write("projects/020-pi.md", "---\ntitle: Two\n---\n");

        var ex = Assert.Throws<ContentException>(() => Loader().LoadCollection<Project>("projects"));

        Assert.Contains("Duplicate slug 'pi'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unterminated_front_matter_names_the_offending_file()
    {
        Write("projects/a.md", "---\ntitle: A\n");

        var ex = Assert.Throws<ContentException>(() => Loader().LoadCollection<Project>("projects"));

        Assert.Contains("content/projects/a.md", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Home_page_route_is_the_site_root()
    {
        Write("site.yml", "name: Test\ntitle: Tester\nbaseUrl: https://example.invalid\n");
        Write("pages/010-home.md", "---\ntitle: Home\n---\n");
        Write("pages/020-about.md", "---\ntitle: About\nnavLabel: About\n---\n");

        var content = Loader().LoadAll();

        Assert.Equal("/", content.Pages.Single(p => p.Slug == "home").Route);
        Assert.Equal("/about/", content.Pages.Single(p => p.Slug == "about").Route);
        Assert.Equal(["About"], content.NavPages.Select(p => p.NavLabel));
    }

    [Fact]
    public void A_content_tree_without_a_home_page_fails()
    {
        Write("site.yml", "name: Test\ntitle: Tester\nbaseUrl: https://example.invalid\n");
        Write("pages/020-about.md", "---\ntitle: About\n---\n");

        var ex = Assert.Throws<ContentException>(() => Loader().LoadAll());

        Assert.Contains("No home page", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_site_file_fails_with_a_clear_message()
    {
        var ex = Assert.Throws<ContentException>(() => Loader().LoadSite());

        Assert.Contains("content/site.yml", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Featured_projects_fall_back_to_all_projects_when_none_are_marked()
    {
        Write("site.yml", "name: Test\ntitle: Tester\nbaseUrl: https://example.invalid\n");
        Write("pages/010-home.md", "---\ntitle: Home\n---\n");
        Write("projects/a.md", "---\ntitle: A\n---\n");
        Write("projects/b.md", "---\ntitle: B\n---\n");

        Assert.Equal(2, Loader().LoadAll().FeaturedProjects.Count());
    }
}
