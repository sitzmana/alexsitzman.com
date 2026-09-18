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
    public void Body_headings_are_plain_text_with_unique_scoped_focusable_anchors()
    {
        Write("projects/020-guide.md",
            """
            ---
            title: Guide
            ---
            ## First **step**
            ### A detail
            ## First **step**
            > ## Quoted heading
            ## Third `step`
            """);

        var project = Assert.Single(Loader().LoadCollection<Project>("projects"));

        Assert.Equal(["First step", "First step", "Third step"], project.BodyHeadings.Select(item => item.Title));
        Assert.Equal(["projects-guide--first-step", "projects-guide--first-step-1", "projects-guide--third-step"],
            project.BodyHeadings.Select(item => item.Id));
        Assert.Contains("<h3 id=\"projects-guide--a-detail\" tabindex=\"-1\">", project.BodyHtml, StringComparison.Ordinal);
        Assert.Contains("<h2 id=\"projects-guide--quoted-heading\" tabindex=\"-1\">", project.BodyHtml, StringComparison.Ordinal);
        Assert.All(project.BodyHeadings,
            heading => Assert.Contains($"id=\"{heading.Id}\" tabindex=\"-1\"", project.BodyHtml, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("A **bold** &amp; `literal` heading", "A bold & literal heading")]
    [InlineData("A [linked title](https://example.invalid/)", "A linked title")]
    public void Contents_labels_use_readable_text_not_inline_markup(string markdown, string expected)
    {
        Write("pages/guide.md", $"---\ntitle: Guide\n---\n## {markdown}\n");

        var heading = Assert.Single(Assert.Single(Loader().LoadCollection<Page>("pages")).BodyHeadings);

        Assert.Equal(expected, heading.Title);
    }

    [Fact]
    public void Heading_fragments_are_rewritten_without_changing_section_links_or_images()
    {
        Write("pages/guide.md",
            """
            ---
            title: Guide
            ---
            [First](#first-step) [Detail](#a-detail) [Section](#contact)
            [Another page](/other/#first-step) ![Image fragment](#first-step)
            ## First step
            ### A detail
            """);

        var html = Assert.Single(Loader().LoadCollection<Page>("pages")).BodyHtml;

        Assert.Contains("href=\"#pages-guide--first-step\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"#pages-guide--a-detail\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"#contact\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/other/#first-step\"", html, StringComparison.Ordinal);
        Assert.Contains("src=\"#first-step\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Encoded_fragments_resolve_to_unicode_headings()
    {
        Write("pages/guide.md", "---\ntitle: Guide\n---\n[Read](#caf%C3%A9)\n\n## Caf\u00e9\n");

        var entry = Assert.Single(Loader().LoadCollection<Page>("pages"));

        Assert.Equal("pages-guide--caf\u00e9", Assert.Single(entry.BodyHeadings).Id);
        Assert.Contains("href=\"#pages-guide--caf%C3%A9\"", entry.BodyHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_collection_headings_cannot_collide_with_each_other()
    {
        const string markdown = "---\ntitle: Guide\n---\n## Overview\n";
        Write("projects/guide.md", markdown);
        Write("projects/other.md", markdown);
        Write("interests/guide.md", markdown);

        var projects = Loader().LoadCollection<Project>("projects");
        var interest = Assert.Single(Loader().LoadCollection<Interest>("interests"));
        var ids = projects.SelectMany(item => item.BodyHeadings).Concat(interest.BodyHeadings)
            .Select(heading => heading.Id).ToArray();

        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Heading_scopes_remain_unambiguous_when_slugs_and_headings_share_words()
    {
        Write("interests/home-lab.md", "---\ntitle: Lab\n---\n## Notes\n");
        Write("interests/home.md", "---\ntitle: Home\n---\n## Lab notes\n");

        var ids = Loader().LoadCollection<Interest>("interests")
            .SelectMany(item => item.BodyHeadings).Select(heading => heading.Id).ToArray();

        Assert.Equal(2, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData("##")]
    [InlineData("### ")]
    public void Empty_markdown_headings_fail_with_the_source_file(string heading)
    {
        Write("projects/guide.md", $"---\ntitle: Guide\n---\n{heading}\n");

        var error = Assert.Throws<ContentException>(() => Loader().LoadCollection<Project>("projects"));

        Assert.Equal("content/projects/guide.md", error.SourcePath);
        Assert.Contains("readable text", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("contentsLabel: Contents")]
    [InlineData("backToTopLabel: Top")]
    [InlineData("contentsLabel: ' '\n  backToTopLabel: Top")]
    [InlineData("contentsLable: Contents\n  backToTopLabel: Top")]
    public void Incomplete_or_misspelled_page_navigation_copy_fails_at_the_source(string fields)
    {
        Write("site.yml",
            $"name: Test\ntitle: Tester\nbaseUrl: https://example.invalid\npageNavigation:\n  {fields}\n");

        Assert.Equal("content/site.yml", Assert.Throws<ContentException>(() => Loader().LoadSite()).SourcePath);
    }

    [Fact]
    public void Body_heading_metadata_cannot_be_authored_as_a_second_catalog()
    {
        Write("pages/guide.md", "---\ntitle: Guide\nbodyHeadings: []\n---\n");

        Assert.Equal("content/pages/guide.md",
            Assert.Throws<ContentException>(() => Loader().LoadCollection<Page>("pages")).SourcePath);
    }

    [Fact]
    public void Evidence_uses_declared_topics_and_real_destinations_without_inventing_connections()
    {
        Write("site.yml", "name: Test\ntitle: Tester\nbaseUrl: https://example.invalid\n");
        Write("pages/home.md", "---\ntitle: Home\nsections: [projects]\n---\n");
        Write("pages/proof.md", "---\ntitle: Proof\nsections: [certifications, repositories, now]\n---\n");
        Write("projects/same.md", "---\ntitle: Project\nfeatured: true\ntags: [Kubernetes]\n---\n");
        Write("projects/other.md", "---\ntitle: Other\nlinks: [{label: Source, url: 'https://example.invalid/other'}]\n---\n");
        Write("projects/draft.md", "---\ntitle: Draft\ndraft: true\n---\n");
        Write("certifications/same.md", "---\ntitle: Credential\nfeatured: true\ntags: [Kubernetes]\n---\n");
        Write("repositories/repo.md", "---\ntitle: Repo\nlanguage: Go\ntags: [go]\nfork: true\nlinks: [{label: Source, url: 'https://example.invalid/repo'}]\n---\n");
        Write("now/focus.md", "---\ntitle: Focus\ntags: [Azure]\n---\n");
        var content = Loader().LoadAll();
        var evidence = content.Evidence.ToArray();
        Assert.Equal(5, evidence.Length);
        Assert.Equal(evidence.Length, evidence.Select(item => item.Key).Distinct().Count());
        Assert.Equal("/#project-same", evidence.Single(item => item.Key == "project-same").Url);
        Assert.Equal("https://example.invalid/other", evidence.Single(item => item.Key == "project-other").Url);
        Assert.Equal("/proof/#credential-same", evidence.Single(item => item.Kind == EvidenceKind.Credential).Url);
        Assert.Equal("/proof/#repository-repo", evidence.Single(item => item.Kind == EvidenceKind.Repository).Url);
        Assert.Single(evidence.Single(item => item.Kind == EvidenceKind.Repository).Topics);
        Assert.Equal("/proof/#now-focus", evidence.Single(item => item.Kind == EvidenceKind.Now).Url);
        Assert.DoesNotContain(evidence, item => item.Topics.Contains("Python"));
        Assert.True(Assert.Single(content.Certifications).Featured);
    }

    [Fact]
    public void Evidence_prefers_project_detail_pages_and_never_invents_missing_routes()
    {
        Write("site.yml", "name: Test\ntitle: Tester\nbaseUrl: https://example.invalid\n");
        Write("pages/home.md", "---\ntitle: Home\n---\n");
        Write("projects/story.md", "---\ntitle: Story\n---\nA real write-up.");
        Write("certifications/credential.md", "---\ntitle: Credential\n---\n");
        var entries = Loader().LoadAll().Evidence.ToArray();
        Assert.Equal("/projects/story/", entries.Single(item => item.Kind == EvidenceKind.Project).Url);
        Assert.Null(entries.Single(item => item.Kind == EvidenceKind.Credential).Url);
    }

    [Fact]
    public void An_explorer_page_requires_authored_control_copy()
    {
        Write("site.yml", "name: Test\ntitle: Tester\nbaseUrl: https://example.invalid\n");
        Write("pages/home.md", "---\ntitle: Home\nsections: [explorer]\n---\n");
        Assert.Equal("content/site.yml", Assert.Throws<ContentException>(() => Loader().LoadAll()).SourcePath);
    }

    [Theory]
    [InlineData("explorer:\n  searchLabel: Search\n")]
    [InlineData("explorer:\n  searhcLabel: Search\n")]
    public void Missing_or_misspelled_explorer_copy_fails_at_the_source(string copy)
    {
        Write("site.yml", $"name: Test\ntitle: Tester\nbaseUrl: https://example.invalid\n{copy}");
        Assert.Equal("content/site.yml", Assert.Throws<ContentException>(() => Loader().LoadSite()).SourcePath);
    }

    [Theory]
    [InlineData("tags: ['']")]
    [InlineData("tags: [null]")]
    public void Empty_evidence_topics_fail_at_the_source(string fields)
    {
        Write("projects/broken.md", $"---\ntitle: Project\n{fields}\n---\n");
        Assert.Equal("content/projects/broken.md",
            Assert.Throws<ContentException>(() => Loader().LoadCollection<Project>("projects")).SourcePath);
    }

    [Fact]
    public void Listening_files_keep_order_drafts_artists_and_original_links()
    {
        Write("listening/030-last.md", "---\ntitle: Last\nartist: Artist\nlinks: [{label: Listen, url: 'https://example.invalid/last'}]\n---\n");
        Write("listening/010-first/index.md", "---\ntitle: First\nartist: Another Artist\nlinks: [{label: Listen, url: 'https://example.invalid/first'}]\n---\n");
        Write("listening/020-draft.md", "---\ntitle: Draft\ndraft: true\nartist: Artist\nlinks: [{label: Listen, url: 'https://example.invalid/draft'}]\n---\n");
        var tracks = Loader().LoadCollection<Track>("listening");
        Assert.Equal(["first", "last"], tracks.Select(track => track.Slug));
        Assert.Equal("Another Artist", tracks[0].Artist);
        Assert.Equal("https://example.invalid/first", Assert.Single(tracks[0].Links).Url);
    }

    [Theory]
    [InlineData("artist: ' '\nlinks: [{label: Listen, url: 'https://example.invalid/track'}]")]
    [InlineData("artist: Artist")]
    [InlineData("artist: Artist\nlinks: [{label: '', url: 'https://example.invalid/track'}]")]
    public void Invalid_listening_entries_fail_with_the_file_name(string fields)
    {
        Write("listening/broken.md", $"---\ntitle: Track\n{fields}\n---\n");
        var error = Assert.Throws<ContentException>(() => Loader().LoadCollection<Track>("listening"));
        Assert.Equal("content/listening/broken.md", error.SourcePath);
    }

    [Fact]
    public void A_repository_needs_a_labelled_link()
    {
        Write("repositories/broken.md", "---\ntitle: Repository\nfork: true\n---\n");
        var error = Assert.Throws<ContentException>(() => Loader().LoadCollection<RepositoryEntry>("repositories"));
        Assert.Equal("content/repositories/broken.md", error.SourcePath);
    }

    [Theory]
    [InlineData("start: 2026-01-01\nend: 2026-12-31\ndays: [{date: 2026-02-19, count: 1}]")]
    [InlineData("start: 2024-01-01\nend: 2024-12-31\ndays: []")]
    [InlineData("start: 9999-12-31\nend: 9999-12-31\ndays: []")]
    public void Activity_supports_inclusive_calendar_periods(string fields)
    {
        Write("activity/snapshot.md", $"---\ntitle: Activity\n{fields}\n---\n");
        Assert.Single(Loader().LoadCollection<ActivitySnapshot>("activity"));
    }

    [Theory]
    [InlineData("end: 2026-01-01")]
    [InlineData("start: 2026-09-17\nend: 2026-01-01")]
    [InlineData("start: 2024-01-01\nend: 2025-01-01")]
    [InlineData("start: 2026-01-01T12:00:00\nend: 2026-09-17")]
    [InlineData("start: 2026-01-01\nend: 2026-09-17\ndays: [{date: 2026-09-18, count: 1}]")]
    [InlineData("start: 2026-01-01\nend: 2026-09-17\ndays: [{date: 2025-12-31, count: 1}]")]
    [InlineData("start: 2026-01-01\nend: 2026-09-17\ndays: [{date: 2026-02-19, count: 0}]")]
    [InlineData("start: 2026-01-01\nend: 2026-09-17\ndays: [{date: 2026-02-19, count: -1}]")]
    [InlineData("start: 2026-01-01\nend: 2026-09-17\ndays: [{date: 2026-02-19, count: 1}, {date: 2026-02-19, count: 2}]")]
    [InlineData("start: 2026-01-01\nend: 2026-09-17\ndays: [{date: 2026-02-19T12:00:00, count: 1}]")]
    [InlineData("start: 2026-01-01\nend: 2026-09-17\ndays: [null]")]
    public void Invalid_activity_fails_with_the_source_file(string fields)
    {
        Write("activity/broken.md", $"---\ntitle: Activity\n{fields}\n---\n");
        var error = Assert.Throws<ContentException>(() => Loader().LoadCollection<ActivitySnapshot>("activity"));
        Assert.Equal("content/activity/broken.md", error.SourcePath);
    }

    [Theory]
    [InlineData("previousLabel: Previous")]
    [InlineData("nextLabel: Next")]
    [InlineData("previousLabel: ' '\n    nextLabel: Next")]
    public void Shelf_controls_cannot_have_missing_labels(string labels)
    {
        Write("site.yml", $"name: Test\ntitle: Tester\nbaseUrl: https://example.invalid\nsections:\n  listening:\n    title: Listening\n    {labels}\n");
        var error = Assert.Throws<ContentException>(() => Loader().LoadSite());
        Assert.Equal("content/site.yml", error.SourcePath);
    }

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
    public void Duplicate_front_matter_keys_fail_instead_of_silently_overwriting()
    {
        Write("projects/a.md", "---\ntitle: First\ntitle: Second\n---\n");
        var error = Assert.Throws<ContentException>(() => Loader().LoadCollection<Project>("projects"));
        Assert.Equal("content/projects/a.md", error.SourcePath);
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

    [Fact]
    public void Home_and_index_cannot_both_claim_the_root_route()
    {
        Write("pages/010-home.md", "---\ntitle: Home\n---\n");
        Write("pages/020-index.md", "---\ntitle: Other home\n---\n");
        var error = Assert.Throws<ContentException>(() => Loader().LoadAll());
        Assert.Contains("Duplicate route '/'", error.Message, StringComparison.Ordinal);
        Assert.Contains("020-index.md", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("http://example.invalid")]
    [InlineData("https://example.invalid/subpath")]
    [InlineData("https://example.invalid/?query=1")]
    [InlineData("https://example.invalid/#fragment")]
    public void Invalid_canonical_origins_fail_at_the_source(string origin)
    {
        Write("site.yml", $"name: Test\ntitle: Tester\nbaseUrl: '{origin}'\n");
        var error = Assert.Throws<ContentException>(() => Loader().LoadSite());
        Assert.Equal("content/site.yml", error.SourcePath);
    }

    [Theory]
    [InlineData("label: ''\n    url: https://example.invalid")]
    [InlineData("label: Missing address\n    url: ''")]
    [InlineData("label: Invalid protocol\n    url: javascript:alert(1)")]
    public void Invalid_entry_links_fail_with_the_file_name(string link)
    {
        Write("projects/a.md", $"---\ntitle: A\nlinks:\n  - {link}\n---\n");
        var error = Assert.Throws<ContentException>(() => Loader().LoadCollection<Project>("projects"));
        Assert.Equal("content/projects/a.md", error.SourcePath);
    }

    [Fact]
    public void Unknown_project_artwork_fails_instead_of_rendering_empty()
    {
        Write("projects/a.md", "---\ntitle: A\nvisual: typo\n---\n");
        var error = Assert.Throws<ContentException>(() => Loader().LoadCollection<Project>("projects"));
        Assert.Contains("Unknown visual", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_collection_markdown_uses_root_relative_assets()
    {
        Write("interests/a/index.md", "---\ntitle: A\n---\n![Photo](photo.svg)\n[Local](notes.txt)\n[Anchor](#details)");
        var entry = Assert.Single(Loader().LoadCollection<Interest>("interests"));
        Assert.Contains("src=\"/interests/a/photo.svg\"", entry.BodyHtml, StringComparison.Ordinal);
        Assert.Contains("href=\"/interests/a/notes.txt\"", entry.BodyHtml, StringComparison.Ordinal);
        Assert.Contains("href=\"#details\"", entry.BodyHtml, StringComparison.Ordinal);
    }
}
