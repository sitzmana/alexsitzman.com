using Portfolio.Content;
using Portfolio.Generator;

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
    public async Task Builds_a_home_page_with_content_from_the_files()
    {
        await BuildAsync();

        var html = ReadOutput("index.html");

        Assert.StartsWith("<!DOCTYPE html>", html, StringComparison.Ordinal);
        Assert.Contains("I am Test Person.", html, StringComparison.Ordinal);
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
        Assert.Contains("src=\"rack.svg\"", ReadOutput("projects/pi/index.html"), StringComparison.Ordinal);
    }
}
