using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portfolio.Components.Hosts;
using Portfolio.Content;

namespace Portfolio.Generator;

/// <summary>
/// Renders the content tree to a folder of static HTML.
/// </summary>
internal sealed class SiteBuilder(BuildOptions options)
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public async Task<BuildResult> BuildAsync(CancellationToken cancellationToken = default)
    {
        var loader = new ContentLoader(options.ContentRoot, options.IncludeDrafts);
        var content = loader.LoadAll();

        PrepareOutput();

        var assets = AssetPipeline.Build(options.AssetsRoot, options.OutputRoot);
        var template = new DocumentTemplate(content.Site, assets);

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        var written = new List<string>();
        var routes = new List<string>();

        foreach (var page in content.Pages)
        {
            var body = await RenderAsync<PageHost>(
                renderer,
                new Dictionary<string, object?> { ["Content"] = content, ["Page"] = page });

            var document = template.Render(new DocumentModel
            {
                Title = page.MetaTitle ?? $"{page.Title} — {content.Site.Name}",
                Description = page.MetaDescription ?? content.Site.Description,
                CanonicalUrl = DocumentTemplate.Combine(content.Site.BaseUrl, page.Route),
                BodyHtml = body,
                StructuredData = BuildPersonJsonLd(content.Site),
                NoIndex = page.NoIndex,
            });

            written.Add(WriteRoute(page.Route, document));
            if (!page.NoIndex)
            {
                routes.Add(page.Route);
            }
        }

        foreach (var project in content.Projects.Where(p => p.HasBody))
        {
            var route = $"/projects/{project.Slug}/";
            var body = await RenderAsync<ProjectHost>(
                renderer,
                new Dictionary<string, object?> { ["Content"] = content, ["Project"] = project });

            var document = template.Render(new DocumentModel
            {
                Title = $"{project.Title} — {content.Site.Name}",
                Description = project.Summary ?? content.Site.Description,
                CanonicalUrl = DocumentTemplate.Combine(content.Site.BaseUrl, route),
                BodyHtml = body,
                StructuredData = BuildPersonJsonLd(content.Site),
            });

            written.Add(WriteRoute(route, document));
            routes.Add(route);
        }

        written.Add(WriteNotFound(content, template));
        CopyContentAssets(content);
        CopyStaticFiles();
        written.Add(WriteSitemap(content.Site.BaseUrl, routes));
        written.Add(WriteRobots(content.Site.BaseUrl));

        cancellationToken.ThrowIfCancellationRequested();
        return new BuildResult(written, routes, MeasureBytes());
    }

    private static async Task<string> RenderAsync<TComponent>(
        HtmlRenderer renderer,
        Dictionary<string, object?> parameters)
        where TComponent : IComponent =>
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<TComponent>(
                ParameterView.FromDictionary(parameters));
            return output.ToHtmlString();
        });

    private void PrepareOutput()
    {
        Directory.CreateDirectory(options.OutputRoot);

        // Only ever clears the generated output folder, never the sources.
        var root = new DirectoryInfo(options.OutputRoot);

        foreach (var file in root.EnumerateFiles())
        {
            Retry(() => DeleteFile(file));
        }

        foreach (var directory in root.EnumerateDirectories())
        {
            Retry(() => DeleteDirectory(directory));
        }
    }

    /// <summary>
    /// Deletes a generated tree, clearing the read-only attribute as it goes.
    /// A file-sync client marks synced folders read-only, which makes a plain
    /// recursive delete fail with "access is denied".
    /// </summary>
    private static void DeleteDirectory(DirectoryInfo directory)
    {
        if (!directory.Exists)
        {
            return;
        }

        ClearReadOnly(directory);

        foreach (var file in directory.EnumerateFiles())
        {
            DeleteFile(file);
        }

        foreach (var child in directory.EnumerateDirectories())
        {
            DeleteDirectory(child);
        }

        directory.Delete();
    }

    private static void DeleteFile(FileInfo file)
    {
        ClearReadOnly(file);
        file.Delete();
    }

    private static void ClearReadOnly(FileSystemInfo entry)
    {
        if (entry.Attributes.HasFlag(FileAttributes.ReadOnly))
        {
            entry.Attributes &= ~FileAttributes.ReadOnly;
        }
    }

    /// <summary>
    /// Sync clients and editors hold brief handles on files under the output folder.
    /// A short retry turns a spurious failure into a successful rebuild.
    /// </summary>
    private static void Retry(Action action, int attempts = 5)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                action();
                return;
            }
            catch (Exception ex) when (attempt < attempts && ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(40 * attempt);
            }
        }
    }

    private string WriteRoute(string route, string html)
    {
        var relative = route.Trim('/') is { Length: > 0 } segment
            ? Path.Combine(segment.Replace('/', Path.DirectorySeparatorChar), "index.html")
            : "index.html";

        return WriteFile(relative, html);
    }

    private string WriteNotFound(PortfolioContent content, DocumentTemplate template)
    {
        var body = new StringBuilder()
            .Append("<main id=\"main\" class=\"main\"><div class=\"shell notfound\">")
            .Append("<p class=\"page-head__eyebrow\">404</p>")
            .Append("<h1 class=\"page-head__title\">This page does not exist</h1>")
            .Append("<p class=\"page-head__lead\">The link may be out of date, or the page may have moved.</p>")
            .Append("<p><a class=\"button button--primary\" href=\"/\">Back to the home page</a></p>")
            .Append("</div></main>")
            .ToString();

        var document = template.Render(new DocumentModel
        {
            Title = $"Page not found — {content.Site.Name}",
            Description = "The requested page could not be found.",
            CanonicalUrl = DocumentTemplate.Combine(content.Site.BaseUrl, "/404.html"),
            BodyHtml = body,
            StructuredData = BuildPersonJsonLd(content.Site),
            NoIndex = true,
        });

        return WriteFile("404.html", document);
    }

    private void CopyContentAssets(PortfolioContent content)
    {
        foreach (var asset in content.Assets)
        {
            var destination = Path.Combine(
                options.OutputRoot,
                asset.OutputPath.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(asset.SourcePath, destination, overwrite: true);
        }
    }

    private void CopyStaticFiles()
    {
        var source = options.StaticRoot;
        if (!Directory.Exists(source))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destination = Path.Combine(options.OutputRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    private string WriteSitemap(string baseUrl, IEnumerable<string> routes)
    {
        var builder = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        builder.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");

        foreach (var route in routes)
        {
            var url = DocumentTemplate.Combine(baseUrl, route);
            builder.Append(CultureInfo.InvariantCulture, $"  <url><loc>{url}</loc></url>\n");
        }

        builder.Append("</urlset>\n");
        return WriteFile("sitemap.xml", builder.ToString());
    }

    private string WriteRobots(string baseUrl) => WriteFile(
        "robots.txt",
        $"User-agent: *\nAllow: /\n\nSitemap: {DocumentTemplate.Combine(baseUrl, "/sitemap.xml")}\n");

    private string WriteFile(string relativePath, string contents)
    {
        var full = Path.Combine(options.OutputRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, contents, Utf8NoBom);
        return relativePath.Replace('\\', '/');
    }

    private long MeasureBytes() =>
        Directory.EnumerateFiles(options.OutputRoot, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);

    private static string BuildPersonJsonLd(SiteConfig site) => JsonSerializer.Serialize(new
    {
        context = "https://schema.org",
        type = "Person",
        name = site.Name,
        jobTitle = site.Title,
        url = site.BaseUrl,
        sameAs = site.Social.Select(s => s.Url).ToArray(),
    },
    GeneratorJson.Options)
        .Replace("\"context\"", "\"@context\"", StringComparison.Ordinal)
        .Replace("\"type\"", "\"@type\"", StringComparison.Ordinal);
}

internal static class GeneratorJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}

internal sealed record BuildOptions
{
    public required string ContentRoot { get; init; }

    public required string AssetsRoot { get; init; }

    public required string StaticRoot { get; init; }

    public required string OutputRoot { get; init; }

    public bool IncludeDrafts { get; init; }
}

internal sealed record BuildResult(
    IReadOnlyList<string> FilesWritten,
    IReadOnlyList<string> Routes,
    long TotalBytes);
