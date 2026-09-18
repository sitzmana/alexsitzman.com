using Portfolio.Content;
using Portfolio.Generator;
using System.Text.Json;

namespace Portfolio.Tests;

/// <summary>
/// Drives the full pipeline — content files in, static HTML out — against temporary
/// content trees. These are the tests that back the "add a file, get content" promise.
/// </summary>
public sealed class SiteBuilderTests : IDisposable
{
    private readonly string _content = Directory.CreateTempSubdirectory("portfolio-e2e-content-").FullName;
    private readonly string _output = Directory.CreateTempSubdirectory("portfolio-e2e-output-").FullName;

    public SiteBuilderTests()
    {
        Write("site.yml",
            """
            name: Test Person
            initials: TP.
            title: Test Engineer
            email: test@example.invalid
            baseUrl: https://example.invalid
            description: A test site.
            social:
              - label: GitHub
                url: https://example.invalid/profile
            hero:
              greeting: Hello, I'm
              summary: Test systems, from files.
            pageNavigation:
              contentsLabel: Jump to a section
              backToTopLabel: Return to start
            sections:
              projects:
                title: Selected work
              contact:
                title: Get in touch
            """);

        Write("pages/010-home.md", "---\ntitle: Home\nnavLabel: Home\nslug: home\nsections: [hero, projects, contact]\n---\n");
    }

    public void Dispose()
    {
        Directory.Delete(_content, recursive: true);
        Directory.Delete(_output, recursive: true);
    }

    private void Write(string relativePath, string text)
    {
        var full = Path.Combine(_content, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    private Task<BuildResult> BuildAsync() => new SiteBuilder(new BuildOptions
    {
        ContentRoot = _content,
        AssetsRoot = Path.Combine(RepositoryPaths.Root, "assets"),
        StaticRoot = Path.Combine(RepositoryPaths.Root, "static"),
        OutputRoot = _output,
    }).BuildAsync();

    private string ReadOutput(string relativePath) =>
        File.ReadAllText(Path.Combine(_output, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    [Fact]
    public async Task Contents_combine_authored_headings_and_sections_in_reading_order()
    {
        Write("pages/020-guide.md",
            "---\ntitle: Guide\nnavLabel: Guide\nsections: [CONTACT]\n---\n## First **step**\nProse.\n\n## Next step\nMore prose.\n");
        await BuildAsync();

        var html = ReadOutput("guide/index.html");
        var first = html.IndexOf("href=\"#pages-guide--first-step\"", StringComparison.Ordinal);
        var next = html.IndexOf("href=\"#pages-guide--next-step\"", StringComparison.Ordinal);
        var contact = html.IndexOf("href=\"#contact\"", StringComparison.Ordinal);

        Assert.Contains("Jump to a section", html, StringComparison.Ordinal);
        Assert.True(first >= 0 && first < next && next < contact);
        Assert.True(contact < html.IndexOf("<h2 id=\"pages-guide--first-step\"", StringComparison.Ordinal));
        Assert.Contains("<span>First step</span>", html, StringComparison.Ordinal);
        Assert.Contains("data-section=\"contact\" tabindex=\"-1\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"colophon__top\" href=\"#top\">Return to start</a>", html, StringComparison.Ordinal);
        Assert.Contains("id=\"top\" tabindex=\"-1\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Section_contents_follow_file_order_and_copy_changes_without_a_separate_list()
    {
        var site = Path.Combine(_content, "site.yml");
        File.AppendAllText(site, "\n  skills:\n    title: Practical tools\n");
        Write("pages/020-guide.md", "---\ntitle: Guide\nsections: [CONTACT, skills, projects]\n---\n");
        await BuildAsync();
        var html = ReadOutput("guide/index.html");

        Assert.True(html.IndexOf("href=\"#contact\"", StringComparison.Ordinal)
            < html.IndexOf("href=\"#skills\"", StringComparison.Ordinal));
        Assert.True(html.IndexOf("href=\"#skills\"", StringComparison.Ordinal)
            < html.IndexOf("href=\"#projects\"", StringComparison.Ordinal));
        Assert.Contains("<span>Practical tools</span>", html, StringComparison.Ordinal);

        File.WriteAllText(site, File.ReadAllText(site).Replace("Practical tools", "The current toolkit", StringComparison.Ordinal));
        Write("pages/020-guide.md", "---\ntitle: Guide\nsections: [skills, projects, contact]\n---\n");
        await BuildAsync();
        html = ReadOutput("guide/index.html");
        Assert.Contains("<span>The current toolkit</span>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Practical tools", html, StringComparison.Ordinal);
        Assert.True(html.IndexOf("href=\"#skills\"", StringComparison.Ordinal)
            < html.IndexOf("href=\"#projects\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Home_contents_follow_the_hero_and_precede_the_sections()
    {
        File.AppendAllText(Path.Combine(_content, "site.yml"), "\n  skills:\n    title: Tools\n");
        Write("pages/010-home.md", "---\ntitle: Home\nsections: [hero, skills, projects, contact]\n---\n");
        await BuildAsync();
        var html = ReadOutput("index.html");
        var contents = html.IndexOf("class=\"page-contents__panel\"", StringComparison.Ordinal);

        Assert.True(contents > html.IndexOf("class=\"hero__title\"", StringComparison.Ordinal));
        Assert.True(contents < html.IndexOf("id=\"skills\"", StringComparison.Ordinal));
        Assert.DoesNotContain("href=\"#hero\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("## One\n")]
    [InlineData("## One\n\n## Two\n")]
    public async Task Short_pages_do_not_get_an_unnecessary_contents_panel(string body)
    {
        Write("pages/020-guide.md", $"---\ntitle: Guide\n---\n{body}");
        await BuildAsync();

        Assert.DoesNotContain("class=\"page-contents__panel\"", ReadOutput("guide/index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Project_contents_update_when_markdown_headings_change()
    {
        Write("projects/guide.md", "---\ntitle: Guide\n---\n## Problem\n\n## Decisions\n\n## Outcome\n");
        await BuildAsync();
        var html = ReadOutput("projects/guide/index.html");

        Assert.Contains("href=\"#projects-guide--problem\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"#projects-guide--decisions\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"#projects-guide--outcome\"", html, StringComparison.Ordinal);

        Write("projects/guide.md", "---\ntitle: Guide\n---\n## Context\n\n## Outcome\n");
        await BuildAsync();
        html = ReadOutput("projects/guide/index.html");
        Assert.DoesNotContain("class=\"page-contents__panel\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("projects-guide--problem", html, StringComparison.Ordinal);
        Assert.Contains("id=\"projects-guide--context\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Body_headings_do_not_reuse_layout_or_section_ids()
    {
        Write("pages/020-guide.md",
            "---\ntitle: Guide\nsections: [projects]\n---\n## main\n\n## projects\n\n## page-contents-title\n\n## top\n");
        await BuildAsync();
        var html = ReadOutput("guide/index.html");

        foreach (var id in new[] { "main", "projects", "page-contents-title", "top" })
        {
            Assert.Equal(1, html.Split($"id=\"{id}\"", StringSplitOptions.None).Length - 1);
            Assert.Contains($"id=\"pages-guide--{id}\"", html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Explorer_and_highlight_follow_content_files_and_section_routes()
    {
        Write("site.yml", File.ReadAllText(Path.Combine(RepositoryPaths.ContentRoot, "site.yml")));
        Write("pages/020-proof.md", "---\ntitle: Proof\nsections: [certifications]\n---\n");
        Write("pages/030-library.md", "---\ntitle: Library\nnavLabel: Library\nsections: [explorer]\n---\n");
        Write("pages/010-home.md", "---\ntitle: Home\nsections: [hero, projects, skills, credential-highlight]\n---\n");
        Write("projects/lab.md", "---\ntitle: Systems Lab\nsummary: A project.\ntags: [Kubernetes]\n---\n");
        Write("certifications/proof.md", "---\ntitle: Specialist\nfeatured: true\ntags: [Kubernetes]\n---\n");
        Write("skills/platform.md", "---\ntitle: Platform\nitems: [Kubernetes, Unlinked]\n---\n");
        await BuildAsync();
        var library = ReadOutput("library/index.html");
        Assert.Contains("id=\"evidence-project-lab\"", library, StringComparison.Ordinal);
        Assert.Contains("id=\"evidence-credential-proof\"", library, StringComparison.Ordinal);
        Assert.Contains("href=\"/proof/#credential-proof\"", library, StringComparison.Ordinal);
        Assert.Contains("<fieldset class=\"explorer__controls\" disabled", library, StringComparison.Ordinal);
        Assert.Contains("data-search=\"Systems Lab", library, StringComparison.Ordinal);
        Assert.Contains("Specialist", ReadOutput("index.html"), StringComparison.Ordinal);
        Assert.Contains("href=\"/library/?topic=Kubernetes\"", ReadOutput("index.html"), StringComparison.Ordinal);
        Assert.DoesNotContain("topic=Unlinked", ReadOutput("index.html"), StringComparison.Ordinal);
        Write("projects/added.md", "---\ntitle: Added by file\ntags: [Rust]\n---\n");
        await BuildAsync();
        Assert.Contains("Added by file", ReadOutput("library/index.html"), StringComparison.Ordinal);
        Assert.Contains("<option value=\"Rust\">Rust</option>", ReadOutput("library/index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Project_files_drive_both_the_hero_index_and_matching_destinations()
    {
        Write("projects/010-first.md", "---\ntitle: First\nfeatured: true\ncategory: Networks\n---\n");
        Write("projects/020-second.md", "---\ntitle: Second\nfeatured: true\n---\n");
        Write("projects/030-draft.md", "---\ntitle: Draft\nfeatured: true\ndraft: true\n---\n");
        await BuildAsync();
        var html = ReadOutput("index.html");
        Assert.Contains("href=\"/#project-first\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"project-first\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/#project-second\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"project-second\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("project-draft", html, StringComparison.Ordinal);
        File.Delete(Path.Combine(_content, "projects", "020-second.md"));
        await BuildAsync();
        Assert.DoesNotContain("project-second", ReadOutput("index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_project_index_follows_the_projects_page_route()
    {
        Write("pages/010-home.md", "---\ntitle: Home\nsections: [hero]\n---\n");
        Write("pages/020-work.md", "---\ntitle: Work\nsections: [projects]\n---\n");
        Write("projects/010-first.md", "---\ntitle: First\n---\n");
        await BuildAsync();
        Assert.Contains("href=\"/work/#project-first\"", ReadOutput("index.html"), StringComparison.Ordinal);
        Assert.Contains("id=\"project-first\"", ReadOutput("work/index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task No_project_index_is_rendered_without_a_projects_page()
    {
        Write("pages/010-home.md", "---\ntitle: Home\nsections: [hero]\n---\n");
        Write("projects/010-first.md", "---\ntitle: First\n---\n");
        await BuildAsync();
        Assert.DoesNotContain("data-project-jump", ReadOutput("index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Personal_collections_render_from_files_without_embeds_or_duplicate_records()
    {
        File.AppendAllText(Path.Combine(_content, "site.yml"),
            "\n  repositories:\n    title: Repositories\n  activity:\n    title: Activity\n  listening:\n    title: Listening\n    previousLabel: Previous records\n    nextLabel: Next records\n    note: A dated collection.\n");
        Write("pages/020-about.md", "---\ntitle: About\nsections: [repositories, activity, listening]\n---\n");
        Write("repositories/repo.md", "---\ntitle: Repository\nlanguage: Go\nfork: true\nlinks: [{label: Source, url: 'https://example.invalid/repo'}]\n---\n");
        Write("listening/track.md", "---\ntitle: Track title\nartist: Artist name\nlinks: [{label: Listen, url: 'https://example.invalid/track'}]\n---\n");
        Write("activity/snapshot.md", "---\ntitle: Public activity\nstart: 9999-12-30\nend: 9999-12-31\ndays: [{date: 9999-12-30, count: 2147483647}, {date: 9999-12-31, count: 2147483647}]\n---\n");
        await BuildAsync();
        var html = ReadOutput("about/index.html");
        Assert.Contains(">Fork<", html, StringComparison.Ordinal);
        Assert.Contains("Go", html, StringComparison.Ordinal);
        Assert.Contains(">4294967294<", html, StringComparison.Ordinal);
        Assert.Equal(1, html.Split("href=\"https://example.invalid/track\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("Artist name", html, StringComparison.Ordinal);
        Assert.Contains("A dated collection.", html, StringComparison.Ordinal);
        Assert.Contains("Previous records", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<iframe", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Builds_a_home_page_with_content_from_the_files()
    {
        await BuildAsync();

        var html = ReadOutput("index.html");

        Assert.StartsWith("<!DOCTYPE html>", html, StringComparison.Ordinal);
        Assert.Contains("Test Person.", html, StringComparison.Ordinal);
        Assert.Contains("Test systems, from files.", html, StringComparison.Ordinal);
        Assert.Contains("Test Engineer", html, StringComparison.Ordinal);
        Assert.Contains("mailto:test@example.invalid", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Adding_a_page_file_creates_a_route_and_a_nav_entry()
    {
        Write("pages/020-uses.md", "---\ntitle: Uses\nnavLabel: Uses\nsections: [contact]\n---\nWhat I use.\n");

        var result = await BuildAsync();

        Assert.Contains("/uses/", result.Routes);
        Assert.Contains("What I use.", ReadOutput("uses/index.html"), StringComparison.Ordinal);
        // The new page appears in the navigation on every page, not just its own.
        Assert.Contains("href=\"/uses/\"", ReadOutput("index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Adding_a_project_file_creates_a_card()
    {
        Write("projects/010-mesh.md", "---\ntitle: Service Mesh Lab\nsummary: A short line.\ntags: [Istio]\n---\n");

        await BuildAsync();
        var html = ReadOutput("index.html");

        Assert.Contains("Service Mesh Lab", html, StringComparison.Ordinal);
        Assert.Contains("Istio", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_project_with_a_body_also_gets_its_own_page()
    {
        Write("projects/010-mesh.md", "---\ntitle: Mesh\nsummary: Short.\n---\nThe long engineering story.\n");

        var result = await BuildAsync();

        Assert.Contains("/projects/mesh/", result.Routes);
        Assert.Contains("The long engineering story.", ReadOutput("projects/mesh/index.html"), StringComparison.Ordinal);
        Assert.Contains("href=\"/projects/mesh/\"", ReadOutput("index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_project_without_a_body_gets_no_page_and_no_link()
    {
        Write("projects/010-mesh.md", "---\ntitle: Mesh\nsummary: Short.\n---\n");

        var result = await BuildAsync();

        Assert.DoesNotContain("/projects/mesh/", result.Routes);
        Assert.DoesNotContain("href=\"/projects/mesh/\"", ReadOutput("index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_project_without_a_body_links_its_title_to_the_existing_writeup()
    {
        Write("projects/010-mesh.md",
            "---\ntitle: Mesh\nlinks:\n  - label: Write-up\n    url: https://example.invalid/story\n---\n");
        await BuildAsync();
        Assert.Contains("class=\"card__link\" href=\"https://example.invalid/story\"", ReadOutput("index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Draft_entries_stay_out_of_the_output()
    {
        Write("projects/010-secret.md", "---\ntitle: Unannounced\ndraft: true\n---\n");

        await BuildAsync();

        Assert.DoesNotContain("Unannounced", ReadOutput("index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unknown_section_key_fails_the_build_and_lists_the_valid_ones()
    {
        Write("pages/020-broken.md", "---\ntitle: Broken\nsections: [notasection]\n---\n");

        var ex = await Assert.ThrowsAsync<ContentException>(BuildAsync);

        Assert.Contains("Unknown section 'notasection'", ex.Message, StringComparison.Ordinal);
        Assert.Contains("certifications", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Writes_a_sitemap_robots_and_404_page()
    {
        await BuildAsync();

        Assert.Contains("https://example.invalid/", ReadOutput("sitemap.xml"), StringComparison.Ordinal);
        Assert.Contains("Sitemap: https://example.invalid/sitemap.xml", ReadOutput("robots.txt"), StringComparison.Ordinal);
        Assert.Contains("noindex", ReadOutput("404.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Every_page_carries_the_accessibility_and_metadata_scaffolding()
    {
        await BuildAsync();
        var html = ReadOutput("index.html");

        Assert.Contains("<html lang=\"en\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"skip-link\" href=\"#main\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"main\"", html, StringComparison.Ordinal);
        Assert.Contains("<link rel=\"canonical\" href=\"https://example.invalid/\">", html, StringComparison.Ordinal);
        Assert.Contains("og:title", html, StringComparison.Ordinal);
        Assert.Contains("\"@type\":\"Person\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stylesheet_and_script_are_content_hashed_for_immutable_caching()
    {
        await BuildAsync();
        var html = ReadOutput("index.html");

        Assert.Matches(@"/assets/site\.[0-9a-f]{10}\.css", html);
        Assert.Matches(@"/assets/enhance\.[0-9a-f]{10}\.js", html);
    }

    [Fact]
    public async Task Rebuilding_removes_files_whose_content_was_deleted()
    {
        Write("pages/020-temporary.md", "---\ntitle: Temporary\nsections: [contact]\n---\n");
        await BuildAsync();
        Assert.True(File.Exists(Path.Combine(_output, "temporary", "index.html")));

        File.Delete(Path.Combine(_content, "pages", "020-temporary.md"));
        await BuildAsync();

        Assert.False(Directory.Exists(Path.Combine(_output, "temporary")));
    }

    [Fact]
    public async Task Images_beside_a_content_file_are_copied_next_to_its_page()
    {
        Write("projects/010-pi/index.md", "---\ntitle: Pi\nsummary: Short.\n---\n![Rack](rack.svg)\n");
        Write("projects/010-pi/rack.svg", "<svg xmlns='http://www.w3.org/2000/svg'></svg>");

        await BuildAsync();

        Assert.True(File.Exists(Path.Combine(_output, "projects", "pi", "rack.svg")));
        Assert.Contains("src=\"/projects/pi/rack.svg\"", ReadOutput("projects/pi/index.html"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("heading: ''\nsections: []")]
    [InlineData("sections: [hero]")]
    public async Task Page_body_is_not_discarded_when_its_heading_is_hidden(string frontMatter)
    {
        Write("pages/020-story.md", $"---\ntitle: Story\n{frontMatter}\n---\nBody must survive.\n");
        await BuildAsync();
        Assert.Contains("Body must survive.", ReadOutput("story/index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Folder_style_page_assets_resolve_at_the_page_route()
    {
        Write("pages/020-story/index.md", "---\ntitle: Story\n---\n![Diagram](images/diagram.svg)\n");
        Write("pages/020-story/images/diagram.svg", "<svg></svg>");
        await BuildAsync();
        Assert.True(File.Exists(Path.Combine(_output, "story", "images", "diagram.svg")));
        Assert.Contains("src=\"/story/images/diagram.svg\"", ReadOutput("story/index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Home_folder_assets_resolve_at_the_root()
    {
        File.Delete(Path.Combine(_content, "pages", "010-home.md"));
        Write("pages/010-home/index.md", "---\ntitle: Home\n---\n![Diagram](diagram.svg)\n");
        Write("pages/010-home/diagram.svg", "<svg></svg>");
        await BuildAsync();
        Assert.True(File.Exists(Path.Combine(_output, "diagram.svg")));
        Assert.Contains("src=\"/diagram.svg\"", ReadOutput("index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_failed_build_preserves_the_last_good_output()
    {
        await BuildAsync();
        var before = ReadOutput("index.html");
        Write("pages/020-broken.md", "---\ntitle: Broken\nsections: [notasection]\n---\n");
        await Assert.ThrowsAsync<ContentException>(BuildAsync);
        Assert.Equal(before, ReadOutput("index.html"));
        Assert.False(Directory.Exists(Path.Combine(_output, "broken")));
    }

    [Fact]
    public async Task Rebuilding_unchanged_content_does_not_rewrite_published_files()
    {
        await BuildAsync();
        var path = Path.Combine(_output, "index.html");
        var sentinel = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(path, sentinel);
        await BuildAsync();
        Assert.Equal(sentinel, File.GetLastWriteTimeUtc(path));
    }

    [Fact]
    public async Task Publishing_preserves_files_after_a_case_only_destination_rename()
    {
        await BuildAsync();
        var original = Path.Combine(_output, "favicon.svg");
        var intermediate = Path.Combine(_output, "temporary-icon");
        File.Move(original, intermediate);
        File.Move(intermediate, Path.Combine(_output, "favicon.SVG"));
        await BuildAsync();
        Assert.True(File.Exists(original));
    }

    [Fact]
    public async Task Output_cannot_overlap_the_content_tree()
    {
        var builder = new SiteBuilder(new BuildOptions
        {
            ContentRoot = _content,
            AssetsRoot = Path.Combine(RepositoryPaths.Root, "assets"),
            StaticRoot = Path.Combine(RepositoryPaths.Root, "static"),
            OutputRoot = _content,
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildAsync());
        Assert.True(File.Exists(Path.Combine(_content, "site.yml")));
    }

    [Fact]
    public async Task An_asset_cannot_overwrite_a_generated_page()
    {
        Write("projects/010-pi/index.md", "---\ntitle: Pi\n---\nReal content.");
        Write("projects/010-pi/index.html", "Must not replace the generated document.");
        var error = await Assert.ThrowsAsync<ContentException>(BuildAsync);
        Assert.Contains("index.html", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Section_copy_comes_from_site_yaml()
    {
        var path = Path.Combine(_content, "site.yml");
        File.WriteAllText(path, File.ReadAllText(path).Replace("Selected work", "Handmade experiments", StringComparison.Ordinal));
        await BuildAsync();
        Assert.Contains("Handmade experiments", ReadOutput("index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Missing_section_copy_names_site_yaml()
    {
        Write("pages/020-skills.md", "---\ntitle: Skills\nsections: [skills]\n---\n");
        var error = await Assert.ThrowsAsync<ContentException>(BuildAsync);
        Assert.Equal("content/site.yml", error.SourcePath);
        Assert.Contains("skills", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Duplicate_sections_fail_instead_of_producing_duplicate_ids()
    {
        Write("pages/010-home.md", "---\ntitle: Home\nsections: [projects, PROJECTS]\n---\n");
        var error = await Assert.ThrowsAsync<ContentException>(BuildAsync);
        Assert.Contains("Duplicate section", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Project_details_do_not_mark_home_as_the_current_page()
    {
        Write("projects/010-pi.md", "---\ntitle: Pi\n---\nProject story.");
        await BuildAsync();
        Assert.DoesNotContain("aria-current=\"page\"", ReadOutput("projects/pi/index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Json_ld_cannot_close_its_script_element()
    {
        var path = Path.Combine(_content, "site.yml");
        File.WriteAllText(path, File.ReadAllText(path).Replace("name: Test Person",
            "name: '</script><span>Test</span>'", StringComparison.Ordinal));
        await BuildAsync();
        Assert.DoesNotContain("</script><span>Test</span>", ReadOutput("index.html"), StringComparison.Ordinal);
        Assert.Contains("\\u003C/script\\u003E", ReadOutput("index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Json_ld_property_names_do_not_rewrite_content_values()
    {
        var path = Path.Combine(_content, "site.yml");
        File.WriteAllText(path, File.ReadAllText(path)
            .Replace("name: Test Person", "name: context", StringComparison.Ordinal)
            .Replace("title: Test Engineer", "title: type", StringComparison.Ordinal));
        await BuildAsync();
        var html = ReadOutput("index.html");
        Assert.Contains("\"name\":\"context\"", html, StringComparison.Ordinal);
        Assert.Contains("\"jobTitle\":\"type\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Metadata_describes_the_actual_theme_and_sitemap()
    {
        await BuildAsync();
        var html = ReadOutput("index.html");
        Assert.Contains("content=\"dark\"", html, StringComparison.Ordinal);
        Assert.Contains("rel=\"sitemap\" type=\"application/xml\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("application/rss+xml", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Hosting_config_uses_real_404_responses_and_revalidates_directory_urls()
    {
        await BuildAsync();
        using var config = JsonDocument.Parse(ReadOutput("staticwebapp.config.json"));
        var root = config.RootElement;
        Assert.False(root.TryGetProperty("navigationFallback", out _));
        Assert.Equal(404, root.GetProperty("responseOverrides").GetProperty("404").GetProperty("statusCode").GetInt32());
        Assert.Equal("public, max-age=0, must-revalidate",
            root.GetProperty("globalHeaders").GetProperty("Cache-Control").GetString());
        Assert.Equal("public, max-age=31536000, immutable",
            root.GetProperty("routes")[0].GetProperty("headers").GetProperty("Cache-Control").GetString());
    }

    [Fact]
    public async Task Build_result_accounts_for_every_output_file()
    {
        var result = await BuildAsync();
        Assert.Equal(Directory.EnumerateFiles(_output, "*", SearchOption.AllDirectories).Count(), result.FilesWritten.Count);
        Assert.Contains("favicon.svg", result.FilesWritten);
    }
}
