using Markdig;
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
        .UsePipeTables()
        .UseEmphasisExtras()
        .UseSmartyPants()
        .DisableHtml() // Content files are prose, not a template escape hatch.
        .Build();

    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithEnforceNullability()
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

        if (!pages.Any(p => p.Route == "/"))
        {
            throw new ContentException(
                "content/pages",
                "No home page. Add content/pages/home.md.");
        }

        return new PortfolioContent
        {
            Site = LoadSite(),
            Pages = pages,
            Projects = LoadCollection<Project>("projects"),
            Certifications = LoadCollection<Certification>("certifications"),
            Now = LoadCollection<NowItem>("now"),
            Skills = LoadCollection<SkillGroup>("skills"),
            Interests = LoadCollection<Interest>("interests"),
            Facts = LoadCollection<Fact>("facts"),
            Assets = [.. _assets],
        };
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
            return Yaml.Deserialize<SiteConfig>(File.ReadAllText(path))
                ?? throw new ContentException(relative, "File is empty.");
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

            var entry = Parse<T>(file.Path, name);
            if (entry.Draft && !_includeDrafts)
            {
                continue;
            }

            if (file.IsFolderStyle)
            {
                CollectAssets(Path.GetDirectoryName(file.Path)!, $"{folder}/{entry.Slug}");
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

    private T Parse<T>(string path, string name)
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
        entry.BodyHtml = Markdown.ToHtml(body.Trim(), Pipeline).Trim();

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
            _assets.Add(new ContentAsset(file, $"{outputPrefix}/{relative}"));
        }
    }

    private string Relative(string path) =>
        "content/" + Path.GetRelativePath(_root, path).Replace('\\', '/');

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
