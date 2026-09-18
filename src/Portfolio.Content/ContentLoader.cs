using System.Globalization;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Portfolio.Content;

/// <summary>
/// Reads the <c>content/</c> tree into typed models.
/// </summary>
/// <remarks>
/// Every entry is one Markdown file with YAML front matter, named either
/// <c>&lt;collection&gt;/&lt;slug&gt;.md</c> or <c>&lt;collection&gt;/&lt;slug&gt;/index.md</c>.
/// The folder form lets images sit next to the text that references them; those
/// siblings are collected into <see cref="Assets"/> and copied verbatim.
/// </remarks>
public sealed class ContentLoader
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAutoLinks()
        .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
        .UsePipeTables()
        .UseEmphasisExtras()
        .UseSmartyPants()
        .DisableHtml() // Content files are prose, not a template escape hatch.
        .Build();

    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithEnforceNullability()
        .WithDuplicateKeyChecking()
        .Build();

    private readonly string _root;
    private readonly bool _includeDrafts;
    private readonly List<ContentAsset> _assets = [];

    public ContentLoader(string contentRoot, bool includeDrafts = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRoot);

        _root = Path.GetFullPath(contentRoot);
        if (!Directory.Exists(_root))
        {
            throw new DirectoryNotFoundException($"Content root not found: {_root}");
        }

        _includeDrafts = includeDrafts;
    }

    /// <summary>Files found beside folder-style entries, to be copied into the output.</summary>
    public IReadOnlyList<ContentAsset> Assets => _assets;

    public PortfolioContent LoadAll()
    {
        _assets.Clear();

        var pages = LoadCollection<Page>("pages");
        foreach (var page in pages)
        {
            page.Route = page.Slug is "home" or "index" ? "/" : $"/{page.Slug}/";
        }

        var duplicateRoute = pages.GroupBy(p => p.Route, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateRoute is not null)
        {
            throw new ContentException(duplicateRoute.Last().SourcePath,
                $"Duplicate route '{duplicateRoute.Key}' also produced by {duplicateRoute.First().SourcePath}.");
        }

        if (!pages.Any(p => p.Route == "/"))
        {
            throw new ContentException(
                "content/pages",
                "No home page. Add content/pages/home.md.");
        }

        var content = new PortfolioContent
        {
            Site = LoadSite(),
            Pages = pages,
            Projects = LoadCollection<Project>("projects"),
            Certifications = LoadCollection<Certification>("certifications"),
            Now = LoadCollection<NowItem>("now"),
            Skills = LoadCollection<SkillGroup>("skills"),
            Interests = LoadCollection<Interest>("interests"),
            Facts = LoadCollection<Fact>("facts"),
            Repositories = LoadCollection<RepositoryEntry>("repositories"),
            Listening = LoadCollection<Track>("listening"),
            Activity = LoadCollection<ActivitySnapshot>("activity"),
            Assets = [.. _assets],
        };
        if (content.SectionRoute("explorer") is not null && content.Site.Explorer is null)
            throw new ContentException("content/site.yml", "The explorer section needs an 'explorer' copy configuration.");
        return content;
    }

    public SiteConfig LoadSite()
    {
        var path = Path.Combine(_root, "site.yml");
        if (!File.Exists(path))
        {
            throw new ContentException("content/site.yml", "Required file is missing.");
        }

        var relative = Relative(path);
        try
        {
            var site = Yaml.Deserialize<SiteConfig>(File.ReadAllText(path))
                ?? throw new ContentException(relative, "File is empty.");
            if (string.IsNullOrWhiteSpace(site.Name) || string.IsNullOrWhiteSpace(site.Title))
            {
                throw new ContentException(relative, "'name' and 'title' must be non-empty.");
            }

            if (!Uri.TryCreate(site.BaseUrl, UriKind.Absolute, out var origin)
                || origin.Scheme != Uri.UriSchemeHttps || string.IsNullOrEmpty(origin.Host)
                || origin.AbsolutePath != "/" || origin.Query.Length != 0
                || origin.Fragment.Length != 0 || origin.UserInfo.Length != 0)
            {
                throw new ContentException(relative, "'baseUrl' must be an HTTPS origin without a path, query, or fragment.");
            }

            ValidateLinks(site.Social, relative);
            ValidateLinks(site.Hero.Links, relative, allowLocal: true);
            foreach (var (key, copy) in site.Sections)
            {
                if (copy is null || string.IsNullOrWhiteSpace(copy.Title))
                {
                    throw new ContentException(relative, $"Section '{key}' needs a non-empty title.");
                }
                ValidateLinks(copy.Links, relative, allowLocal: true);
                if (key.Equals("listening", StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrWhiteSpace(copy.PreviousLabel) || string.IsNullOrWhiteSpace(copy.NextLabel)))
                {
                    throw new ContentException(relative, "Section 'listening' needs non-empty 'previousLabel' and 'nextLabel'.");
                }
            }

            if (site.PageNavigation is { } navigation
                && (string.IsNullOrWhiteSpace(navigation.ContentsLabel)
                    || string.IsNullOrWhiteSpace(navigation.BackToTopLabel)))
            {
                throw new ContentException(relative,
                    "'pageNavigation' needs non-empty 'contentsLabel' and 'backToTopLabel'.");
            }

            if (site.Explorer is { } explorer)
            {
                // Every property is authored UI copy; omissions must fail before controls are rendered.
                foreach (var property in typeof(ExplorerCopy).GetProperties())
                {
                    if (property.GetValue(explorer) is not string value || string.IsNullOrWhiteSpace(value))
                        throw new ContentException(relative, $"Explorer copy needs a non-empty '{property.Name}' label.");
                }
                if (!explorer.ResultsTemplate.Contains("{shown}", StringComparison.Ordinal)
                    || !explorer.ResultsTemplate.Contains("{total}", StringComparison.Ordinal))
                    throw new ContentException(relative, "Explorer 'resultsTemplate' must contain {shown} and {total}.");
            }

            return site;
        }
        catch (YamlException ex)
        {
            throw new ContentException(relative, Describe(ex), ex);
        }
    }

    /// <summary>
    /// Loads every entry in <paramref name="folder"/>. A missing folder yields no
    /// entries so a site can omit collections it does not use.
    /// </summary>
    public T[] LoadCollection<T>(string folder)
        where T : ContentEntry
    {
        var directory = Path.Combine(_root, folder);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var entries = new List<T>();

        foreach (var file in EnumerateEntryFiles(directory))
        {
            var name = file.IsFolderStyle
                ? Path.GetFileName(Path.GetDirectoryName(file.Path)!)
                : Path.GetFileNameWithoutExtension(file.Path);

            var entry = Parse<T>(file.Path, name, folder);
            if (entry.Draft && !_includeDrafts)
            {
                continue;
            }

            if (file.IsFolderStyle)
            {
                CollectAssets(Path.GetDirectoryName(file.Path)!, AssetPrefix(folder, entry.Slug));
            }

            entry.SortKey = entry.Order ?? Slug.SortPrefix(name) ?? int.MaxValue;
            entries.Add(entry);
        }

        var duplicate = entries
            .GroupBy(e => e.Slug, StringComparer.Ordinal)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            var paths = string.Join(", ", duplicate.Select(e => e.SourcePath));
            throw new ContentException(
                $"content/{folder}",
                $"Duplicate slug '{duplicate.Key}' from: {paths}. Rename one file or set a distinct 'slug'.");
        }

        return [.. entries
            .OrderBy(e => e.SortKey)
            .ThenBy(e => e.Title, StringComparer.OrdinalIgnoreCase)];
    }

    private static Dictionary<string, string> AddHeadingAnchors(
        MarkdownDocument document, ContentEntry entry, string folder)
    {
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var contents = new List<ContentHeading>();
        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            var renderer = new HtmlRenderer(writer) { EnableHtmlForInline = false, EnableHtmlEscape = false };
            Pipeline.Setup(renderer);
            if (heading.Inline is { } inline) renderer.Render(inline);
            var title = writer.ToString().Trim();
            var attributes = heading.GetAttributes();
            if (title.Length == 0 || attributes.Id is not { } id)
            {
                throw new ContentException(entry.SourcePath, "Markdown headings must contain readable text.");
            }

            // Slugs contain only single hyphens, so the scope boundary is unambiguous.
            var scopedId = $"{folder}-{entry.Slug}--{id}";
            ids.Add(id, scopedId);
            attributes.Id = scopedId;
            attributes.AddProperty("tabindex", "-1");
            if (heading.Level == 2 && heading.Parent == document)
                contents.Add(new ContentHeading(scopedId, title));
        }
        entry.BodyHeadings = [.. contents];
        return ids;
    }

    private T Parse<T>(string path, string name, string folder)
        where T : ContentEntry
    {
        var relative = Relative(path);
        var (yaml, body) = ReadAndSplit(path, relative);

        T entry;
        try
        {
            entry = yaml.Length == 0
                ? Activator.CreateInstance<T>()
                : Yaml.Deserialize<T>(yaml) ?? Activator.CreateInstance<T>();
        }
        catch (YamlException ex)
        {
            throw new ContentException(relative, Describe(ex), ex);
        }

        if (string.IsNullOrWhiteSpace(entry.Title))
        {
            throw new ContentException(relative, "Front matter is missing a 'title'.");
        }

        entry.SourcePath = relative;
        entry.Slug = string.IsNullOrWhiteSpace(entry.Slug) ? Slug.From(name) : Slug.From(entry.Slug);
        var document = Markdown.Parse(body.Trim(), Pipeline);
        var headingIds = AddHeadingAnchors(document, entry, folder);
        var prefix = AssetPrefix(folder, entry.Slug);
        var assetBase = new Uri("https://content.invalid/" + (prefix.Length > 0 ? prefix + "/" : ""));
        foreach (var link in document.Descendants<LinkInline>())
        {
            if (!link.IsImage && link.Url is { } fragment && fragment.StartsWith('#')
                && headingIds.TryGetValue(Uri.UnescapeDataString(fragment[1..]), out var headingId))
            {
                link.Url = "#" + headingId;
                continue;
            }
            if (string.IsNullOrEmpty(link.Url) || link.Url.StartsWith('#')
                || link.Url.StartsWith('/') || Uri.TryCreate(link.Url, UriKind.Absolute, out _))
            {
                continue;
            }
            if (!Uri.TryCreate(assetBase, link.Url, out var resolved))
            {
                throw new ContentException(relative, $"Invalid Markdown URL '{link.Url}'.");
            }
            link.Url = resolved.PathAndQuery + resolved.Fragment;
        }
        entry.BodyHtml = document.ToHtml(Pipeline).Trim();
        ValidateLinks(entry.Links, relative);
        if (entry.Tags.Any(string.IsNullOrWhiteSpace))
            throw new ContentException(relative, "Tags must be non-empty text.");
        if (entry is Project { Visual: not null } project
            && project.Visual is not ("network" or "stack" or "signal"))
        {
            throw new ContentException(relative, $"Unknown visual '{project.Visual}'. Valid visuals: network, stack, signal.");
        }

        if (entry is Track track && (string.IsNullOrWhiteSpace(track.Artist) || track.Links.Length == 0))
        {
            throw new ContentException(relative, "A listening entry needs an 'artist' and at least one labelled link.");
        }
        if (entry is RepositoryEntry repository && repository.Links.Length == 0)
        {
            throw new ContentException(relative, "A repository entry needs at least one labelled link.");
        }
        if (entry is ActivitySnapshot activity)
        {
            if (activity.Start == default || activity.End < activity.Start
                || activity.Start.TimeOfDay != TimeSpan.Zero || activity.End.TimeOfDay != TimeSpan.Zero
                || (activity.End - activity.Start).Days >= 366)
            {
                throw new ContentException(relative, "Activity needs date-only 'start' and 'end' covering at most 366 calendar days.");
            }
            var dates = new HashSet<DateTime>();
            foreach (var day in activity.Days)
            {
                if (day is null || day.Date < activity.Start || day.Date > activity.End
                    || day.Date.TimeOfDay != TimeSpan.Zero || day.Count <= 0 || !dates.Add(day.Date.Date))
                {
                    throw new ContentException(relative, "Activity days need unique dates inside the period and positive counts.");
                }
            }
        }

        if (entry.Slug.Length == 0)
        {
            throw new ContentException(
                relative,
                $"'{name}' does not reduce to a usable slug. Rename the file or set 'slug'.");
        }

        return entry;
    }

    private static (string Yaml, string Body) ReadAndSplit(string path, string relative)
    {
        try
        {
            return FrontMatter.Split(File.ReadAllText(path));
        }
        catch (FormatException ex)
        {
            throw new ContentException(relative, ex.Message, ex);
        }
        catch (IOException ex)
        {
            throw new ContentException(relative, "File could not be read.", ex);
        }
    }

    private static IEnumerable<(string Path, bool IsFolderStyle)> EnumerateEntryFiles(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly)
                     .OrderBy(p => p, StringComparer.Ordinal))
        {
            yield return (file, false);
        }

        foreach (var sub in Directory.EnumerateDirectories(directory)
                     .OrderBy(p => p, StringComparer.Ordinal))
        {
            var index = Path.Combine(sub, "index.md");
            if (File.Exists(index))
            {
                yield return (index, true);
            }
        }
    }

    private void CollectAssets(string directory, string outputPrefix)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(file).Equals(".md", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relative = Path.GetRelativePath(directory, file).Replace('\\', '/');
            _assets.Add(new ContentAsset(file, $"{outputPrefix}/{relative}".TrimStart('/')));
        }
    }

    private string Relative(string path) =>
        "content/" + Path.GetRelativePath(_root, path).Replace('\\', '/');

    private static string AssetPrefix(string folder, string slug) =>
        folder == "pages" ? slug is "home" or "index" ? "" : slug : $"{folder}/{slug}";

    private static void ValidateLinks(ContentLink[] links, string sourcePath, bool allowLocal = false)
    {
        foreach (var link in links)
        {
            if (link is null || string.IsNullOrWhiteSpace(link.Label) || string.IsNullOrWhiteSpace(link.Url))
            {
                throw new ContentException(sourcePath, "Every link needs a non-empty 'label' and 'url'.");
            }

            var local = allowLocal && link.Url.StartsWith("/", StringComparison.Ordinal)
                && !link.Url.StartsWith("//", StringComparison.Ordinal) && !link.Url.Contains('\\');
            var absolute = Uri.TryCreate(link.Url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps && !string.IsNullOrEmpty(uri.Host)
                    || uri.Scheme == Uri.UriSchemeMailto && uri.AbsolutePath.Length > 0);
            if ((!local && !absolute) || link.Url.Any(char.IsControl))
            {
                throw new ContentException(sourcePath, $"Invalid URL for link '{link.Label}'. Use HTTPS or mailto"
                    + (allowLocal ? ", or a root-relative path." : "."));
            }
        }
    }

    private static string Describe(YamlException ex)
    {
        var detail = ex.InnerException?.Message ?? ex.Message;
        return $"Invalid front matter at line {ex.Start.Line}, column {ex.Start.Column}. {detail}";
    }
}

/// <summary>A file copied verbatim from the content tree into the output.</summary>
/// <param name="SourcePath">Absolute path on disk.</param>
/// <param name="OutputPath">Output path relative to the site root, using forward slashes.</param>
public sealed record ContentAsset(string SourcePath, string OutputPath);
