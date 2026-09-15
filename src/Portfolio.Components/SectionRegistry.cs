using Portfolio.Components.Sections;
using Portfolio.Content;

namespace Portfolio.Components;

/// <summary>
/// Maps the <c>sections:</c> keys in a page's front matter to components.
/// This is the only place a new section type has to be registered.
/// </summary>
public static class SectionRegistry
{
    private static readonly Dictionary<string, Type> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hero"] = typeof(HeroSection),
        ["projects"] = typeof(ProjectsSection),
        ["certifications"] = typeof(CertificationsSection),
        ["skills"] = typeof(SkillsSection),
        ["now"] = typeof(NowSection),
        ["interests"] = typeof(InterestsSection),
        ["facts"] = typeof(FactsSection),
        ["connect"] = typeof(ConnectSection),
        ["contact"] = typeof(ContactSection),
    };

    /// <summary>Valid <c>sections:</c> keys, alphabetically.</summary>
    public static IReadOnlyCollection<string> Keys { get; } = [.. Map.Keys.Order(StringComparer.Ordinal)];

    public static bool IsKnown(string key) => Map.ContainsKey(key);

    /// <summary>
    /// Resolves a section key, failing the build with the offending file and the
    /// list of valid keys rather than rendering a silently empty page.
    /// </summary>
    public static Type Resolve(string key, string sourcePath) =>
        Map.TryGetValue(key, out var type)
            ? type
            : throw new ContentException(
                sourcePath,
                $"Unknown section '{key}'. Valid sections: {string.Join(", ", Keys)}.");
}
