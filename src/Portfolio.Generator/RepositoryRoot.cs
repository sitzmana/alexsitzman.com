namespace Portfolio.Generator;

/// <summary>
/// Locates the repository root so the generator can be run from any working directory.
/// </summary>
internal static class RepositoryRoot
{
    public static string Find()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "content", "site.yml")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "content", "site.yml")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repository root (no content/site.yml found).");
    }
}
