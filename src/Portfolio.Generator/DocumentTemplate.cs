using System.Globalization;
using System.Text;
using Portfolio.Content;

namespace Portfolio.Generator;

/// <summary>
/// Wraps rendered component markup in the HTML document shell.
/// </summary>
/// <remarks>
/// The shell is a string rather than a component because a component cannot emit a
/// doctype, and because the head has to reference build-time asset hashes.
/// </remarks>
internal sealed class DocumentTemplate(SiteConfig site, AssetManifest assets)
{
    public string Render(DocumentModel model)
    {
        var builder = new StringBuilder(16 * 1024);

        builder.Append("<!DOCTYPE html>\n");
        builder.Append("<html lang=\"en\" class=\"no-js\">\n<head>\n");
        builder.Append("<meta charset=\"utf-8\">\n");
        builder.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        builder.Append(CultureInfo.InvariantCulture, $"<title>{Escape(model.Title)}</title>\n");
        builder.Append(CultureInfo.InvariantCulture, $"<meta name=\"description\" content=\"{Escape(model.Description)}\">\n");
        builder.Append(CultureInfo.InvariantCulture, $"<link rel=\"canonical\" href=\"{Escape(model.CanonicalUrl)}\">\n");

        if (model.NoIndex)
        {
            builder.Append("<meta name=\"robots\" content=\"noindex\">\n");
        }

        builder.Append("<meta name=\"color-scheme\" content=\"dark\">\n");
        builder.Append(CultureInfo.InvariantCulture, $"<meta name=\"theme-color\" content=\"{Escape(assets.ThemeColor)}\">\n");

        AppendOpenGraph(builder, model);

        // This class alone never hides content; the loaded enhancement opts in per element.
        builder.Append("<script>document.documentElement.classList.replace('no-js','js')</script>\n");

        builder.Append(CultureInfo.InvariantCulture, $"<link rel=\"stylesheet\" href=\"{assets.StylesheetPath}\">\n");
        builder.Append("<link rel=\"icon\" href=\"/favicon.svg\" type=\"image/svg+xml\">\n");
        builder.Append("<link rel=\"sitemap\" type=\"application/xml\" href=\"/sitemap.xml\">\n");
        builder.Append(CultureInfo.InvariantCulture, $"<script type=\"application/ld+json\">{model.StructuredData}</script>\n");
        builder.Append("</head>\n<body>\n");

        builder.Append("<a class=\"skip-link\" href=\"#main\">Skip to content</a>\n");
        builder.Append("<div class=\"scroll-progress\" aria-hidden=\"true\"></div>\n");
        builder.Append(model.BodyHtml);
        builder.Append('\n');
        builder.Append(CultureInfo.InvariantCulture, $"<script src=\"{assets.ScriptPath}\" defer></script>\n");
        builder.Append("</body>\n</html>\n");

        return builder.ToString();
    }

    private void AppendOpenGraph(StringBuilder builder, DocumentModel model)
    {
        builder.Append(CultureInfo.InvariantCulture, $"<meta property=\"og:title\" content=\"{Escape(model.Title)}\">\n");
        builder.Append(CultureInfo.InvariantCulture, $"<meta property=\"og:description\" content=\"{Escape(model.Description)}\">\n");
        builder.Append(CultureInfo.InvariantCulture, $"<meta property=\"og:url\" content=\"{Escape(model.CanonicalUrl)}\">\n");
        builder.Append(CultureInfo.InvariantCulture, $"<meta property=\"og:site_name\" content=\"{Escape(site.Name)}\">\n");
        builder.Append("<meta property=\"og:type\" content=\"website\">\n");
        builder.Append(CultureInfo.InvariantCulture, $"<meta property=\"og:image\" content=\"{Escape(Combine(site.BaseUrl, "/og.svg"))}\">\n");
        builder.Append("<meta name=\"twitter:card\" content=\"summary_large_image\">\n");
    }

    internal static string Combine(string baseUrl, string path) =>
        $"{baseUrl.TrimEnd('/')}{path}";

    private static string Escape(string? value) => string.IsNullOrEmpty(value)
        ? ""
        : value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}

internal sealed record DocumentModel
{
    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string CanonicalUrl { get; init; }

    public required string BodyHtml { get; init; }

    public required string StructuredData { get; init; }

    public bool NoIndex { get; init; }
}
