using System.Text;

namespace Portfolio.Content;

/// <summary>
/// Converts a file or folder name into a URL-safe slug.
/// </summary>
public static class Slug
{
    /// <summary>
    /// Lowercases, strips a leading numeric sort prefix (<c>010-</c>), and reduces
    /// everything that is not a letter or digit to single hyphens.
    /// </summary>
    public static string From(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var trimmed = StripSortPrefix(name);
        var builder = new StringBuilder(trimmed.Length);
        var pendingHyphen = false;

        foreach (var ch in trimmed)
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (pendingHyphen && builder.Length > 0)
                {
                    builder.Append('-');
                }

                pendingHyphen = false;
                builder.Append(char.ToLowerInvariant(ch));
            }
            else
            {
                pendingHyphen = true;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Reads the <c>010</c> from a <c>010-name</c> prefix so files can be ordered on disk.
    /// Returns null when there is no prefix.
    /// </summary>
    public static int? SortPrefix(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var dash = name.IndexOf('-', StringComparison.Ordinal);
        return dash > 0 && int.TryParse(name.AsSpan(0, dash), out var value) ? value : null;
    }

    private static string StripSortPrefix(string name)
    {
        var dash = name.IndexOf('-', StringComparison.Ordinal);
        return dash > 0 && int.TryParse(name.AsSpan(0, dash), out _)
            ? name[(dash + 1)..]
            : name;
    }
}
