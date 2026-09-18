using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portfolio.Components;
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
        cancellationToken.ThrowIfCancellationRequested();
        ValidateOutput();
        var staging = Directory.CreateTempSubdirectory("portfolio-build-");
        try
        {
            var result = await new SiteBuilder(options with { OutputRoot = staging.FullName })
                .BuildCoreAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Publish(staging.FullName);
            return result;
        }
        finally
        {
            Retry(() => DeleteDirectory(staging));
        }
    }

    private async Task<BuildResult> BuildCoreAsync(CancellationToken cancellationToken)
    {
        var loader = new ContentLoader(options.ContentRoot, options.IncludeDrafts);
        var content = loader.LoadAll();

        foreach (var key in content.Site.Sections.Keys)
        {
            SectionRegistry.Resolve(key, "content/site.yml");
            if (key != key.ToLowerInvariant())
            {
                throw new ContentException("content/site.yml", $"Section copy key '{key}' must be lowercase.");
            }
        }
        foreach (var page in content.Pages)
        {
            var sections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in page.Sections)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new ContentException(page.SourcePath, "Section keys must not be empty.");
                }
                SectionRegistry.Resolve(key, page.SourcePath);
                var normalized = key.ToLowerInvariant();
                if (!sections.Add(normalized))
                {
                    throw new ContentException(page.SourcePath, $"Duplicate section '{key}'.");
                }
                if (normalized != "hero" && !content.Site.Sections.ContainsKey(normalized))
                {
                    throw new ContentException("content/site.yml",
                        $"Missing section copy for '{normalized}', used by {page.SourcePath}.");
                }
            }
        }

        var assets = AssetPipeline.Build(options.AssetsRoot, options.OutputRoot);
        var template = new DocumentTemplate(content.Site, assets);
        var personJsonLd = BuildPersonJsonLd(content.Site);

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        var routes = new List<string>();

        foreach (var page in content.Pages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var body = await RenderAsync<PageHost>(
                renderer,
                new Dictionary<string, object?> { ["Content"] = content, ["Page"] = page });

            var document = template.Render(new DocumentModel
            {
                Title = page.MetaTitle ?? $"{page.Title} — {content.Site.Name}",
                Description = page.MetaDescription ?? content.Site.Description,
                CanonicalUrl = DocumentTemplate.Combine(content.Site.BaseUrl, page.Route),
                BodyHtml = body,
                StructuredData = personJsonLd,
                NoIndex = page.NoIndex,
            });

            WriteRoute(page.Route, document);
            if (!page.NoIndex)
            {
                routes.Add(page.Route);
            }
        }

        foreach (var project in content.Projects.Where(p => p.HasBody))
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                StructuredData = personJsonLd,
            });

            WriteRoute(route, document);
            routes.Add(route);
        }

        WriteNotFound(content, template, personJsonLd);
        CopyContentAssets(content);
        CopyStaticFiles();
        WriteSitemap(content.Site.BaseUrl, routes);
        WriteRobots(content.Site.BaseUrl);

        cancellationToken.ThrowIfCancellationRequested();
        return new BuildResult(
            Directory.EnumerateFiles(options.OutputRoot, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(options.OutputRoot, path).Replace('\\', '/'))
                .Order(StringComparer.Ordinal).ToArray(),
            routes, MeasureBytes());
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

    private void ValidateOutput()
    {
        var output = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.OutputRoot));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (output.Equals(Path.GetPathRoot(output), comparison))
        {
            throw new InvalidOperationException("The filesystem root cannot be used as generated output.");
        }
        var repository = RepositoryRoot.Find();
        var sources = new[] { options.ContentRoot, options.AssetsRoot, options.StaticRoot }
            .Concat(new[] { "src", "tests", "docs", "scripts", ".github", ".git" }
                .Select(folder => Path.Combine(repository, folder)));
        foreach (var source in sources)
        {
            var input = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
            if (output.Equals(input, comparison)
                || input.StartsWith(output + Path.DirectorySeparatorChar, comparison)
                || output.StartsWith(input + Path.DirectorySeparatorChar, comparison))
            {
                throw new InvalidOperationException($"Output '{output}' must not overlap source '{input}'.");
            }
        }

        for (var directory = new DirectoryInfo(output); directory is not null; directory = directory.Parent)
        {
            if (directory.LinkTarget is not null)
            {
                throw new InvalidOperationException($"Output must not traverse symbolic link '{directory.FullName}'.");
            }
        }
        if (Directory.Exists(output)) ValidateTree(new DirectoryInfo(output));
    }

    private static void ValidateTree(DirectoryInfo directory)
    {
        foreach (var entry in directory.EnumerateFileSystemInfos())
        {
            if (entry.LinkTarget is not null || entry.Name == ".git")
            {
                throw new InvalidOperationException($"Output contains a protected entry: {entry.FullName}");
            }
            if (entry is DirectoryInfo child) ValidateTree(child);
        }
    }

    private void Publish(string staging)
    {
        Directory.CreateDirectory(options.OutputRoot);
        var paths = Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(staging, path))
            .ToHashSet(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        // Assets precede HTML so a local refresh never references an uninstalled bundle.
        foreach (var relative in paths.OrderBy(path => Path.GetExtension(path) == ".html"))
        {
            var source = Path.Combine(staging, relative);
            var destination = Path.Combine(options.OutputRoot, relative);
            if (File.Exists(destination) && new FileInfo(source).Length == new FileInfo(destination).Length
                && File.ReadAllBytes(source).AsSpan().SequenceEqual(File.ReadAllBytes(destination)))
            {
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            Retry(() =>
            {
                if (File.Exists(destination)) ClearReadOnly(new FileInfo(destination));
                File.Copy(source, destination, overwrite: true);
            });
        }
        foreach (var file in Directory.EnumerateFiles(options.OutputRoot, "*", SearchOption.AllDirectories))
        {
            if (!paths.Contains(Path.GetRelativePath(options.OutputRoot, file)))
            {
                Retry(() => DeleteFile(new FileInfo(file)));
            }
        }
        foreach (var directory in Directory.EnumerateDirectories(options.OutputRoot, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Retry(() => DeleteDirectory(new DirectoryInfo(directory)));
            }
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

    private string WriteNotFound(PortfolioContent content, DocumentTemplate template, string personJsonLd)
    {
        var body = new StringBuilder()
            .Append("<main id=\"main\" class=\"main\" tabindex=\"-1\"><div class=\"shell notfound\">")
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
            StructuredData = personJsonLd,
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
            CopyNew(asset.SourcePath, destination);
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
            CopyNew(file, destination);
        }
    }

    private static void CopyNew(string source, string destination)
    {
        if (File.Exists(destination))
        {
            throw new ContentException(source, $"Output collision at '{destination}'.");
        }
        File.Copy(source, destination);
    }

    private string WriteSitemap(string baseUrl, IEnumerable<string> routes)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var document = new XDocument(new XElement(ns + "urlset",
            routes.Select(route => new XElement(ns + "url",
                new XElement(ns + "loc", DocumentTemplate.Combine(baseUrl, route))))));
        return WriteFile("sitemap.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + document + "\n");
    }

    private string WriteRobots(string baseUrl) => WriteFile(
        "robots.txt",
        $"User-agent: *\nAllow: /\n\nSitemap: {DocumentTemplate.Combine(baseUrl, "/sitemap.xml")}\n");

    private string WriteFile(string relativePath, string contents)
    {
        var full = Path.Combine(options.OutputRoot, relativePath);
        if (File.Exists(full))
        {
            throw new ContentException(relativePath, "More than one input produces this output file.");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, contents, Utf8NoBom);
        return relativePath.Replace('\\', '/');
    }

    private long MeasureBytes() =>
        Directory.EnumerateFiles(options.OutputRoot, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);

    private static string BuildPersonJsonLd(SiteConfig site) => JsonSerializer.Serialize(new Dictionary<string, object>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "Person",
        ["name"] = site.Name,
        ["jobTitle"] = site.Title,
        ["url"] = site.BaseUrl,
        ["sameAs"] = site.Social.Select(s => s.Url).ToArray(),
    });
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
