namespace Portfolio.Tests;

/// <summary>
/// Locates repository folders from the test binary's location, so tests run the
/// same way from the CLI, an IDE, and CI.
/// </summary>
internal static class RepositoryPaths
{
    public static string Root { get; } = Find();

    public static string ContentRoot => Path.Combine(Root, "content");

    private static string Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "content", "site.yml")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"No content/site.yml found above {AppContext.BaseDirectory}.");
    }
}
