using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Portfolio.Generator;

/// <summary>Output paths of the hashed CSS and JS bundles.</summary>
internal sealed record AssetManifest(string StylesheetPath, string ScriptPath);

/// <summary>
/// Concatenates the stylesheet parts and the enhancement script into one file each,
/// named by content hash so they can be cached immutably.
/// </summary>
internal static class AssetPipeline
{
    /// <summary>Concatenation order matters: tokens define the custom properties the rest use.</summary>
    private static readonly string[] StyleOrder =
    [
        "tokens.css",
        "base.css",
        "layout.css",
        "components.css",
        "motion.css",
    ];

    public static AssetManifest Build(string assetsRoot, string outputRoot)
    {
        var css = Concatenate(Path.Combine(assetsRoot, "styles"), StyleOrder);
        var js = File.ReadAllText(Path.Combine(assetsRoot, "scripts", "enhance.js"));

        var cssPath = WriteHashed(outputRoot, "assets", "site", ".css", css);
        var jsPath = WriteHashed(outputRoot, "assets", "enhance", ".js", js);

        return new AssetManifest(cssPath, jsPath);
    }

    private static string Concatenate(string directory, string[] fileNames)
    {
        var builder = new StringBuilder();
        foreach (var name in fileNames)
        {
            var path = Path.Combine(directory, name);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Stylesheet part not found: {path}", path);
            }

            builder.Append(CultureInfo.InvariantCulture, $"/* {name} */\n");
            builder.Append(File.ReadAllText(path).TrimEnd());
            builder.Append("\n\n");
        }

        return builder.ToString();
    }

    private static string WriteHashed(
        string outputRoot,
        string folder,
        string name,
        string extension,
        string content)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)))[..10];
        var fileName = $"{name}.{hash}{extension}";
        var directory = Path.Combine(outputRoot, folder);

        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, fileName), content, new UTF8Encoding(false));

        return $"/{folder}/{fileName}";
    }
}
