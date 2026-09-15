using System.Diagnostics;
using Portfolio.Content;
using Portfolio.Generator;

var repositoryRoot = RepositoryRoot.Find();

// Relative paths resolve against the repository root, not the working directory,
// because `dotnet run` executes with the project folder as its current directory.
var options = new BuildOptions
{
    ContentRoot = Resolve(Arg("--content"), "content"),
    AssetsRoot = Resolve(Arg("--assets"), "assets"),
    StaticRoot = Resolve(Arg("--static"), "static"),
    OutputRoot = Resolve(Arg("--output"), "dist"),
    IncludeDrafts = Has("--drafts"),
};

if (Has("--help") || Has("-h"))
{
    Console.WriteLine(
        """
        Portfolio static site generator

          dotnet run --project src/Portfolio.Generator [options]

          --output <dir>    Output folder (default: ./dist)
          --content <dir>   Content folder (default: ./content)
          --assets <dir>    Styles and scripts (default: ./assets)
          --static <dir>    Files copied verbatim (default: ./static)
          --drafts          Include entries marked draft: true
          --serve           Build, then serve ./dist and rebuild on change
          --port <n>        Preview port (default: 5000)
        """);
    return 0;
}

try
{
    var elapsed = Stopwatch.StartNew();
    var result = await new SiteBuilder(options).BuildAsync();
    elapsed.Stop();

    Console.WriteLine(
        $"Built {result.Routes.Count} routes, {result.FilesWritten.Count} files, "
        + $"{result.TotalBytes / 1024.0:F1} KB in {elapsed.ElapsedMilliseconds} ms -> {options.OutputRoot}");

    foreach (var route in result.Routes)
    {
        Console.WriteLine($"  {route}");
    }
}
catch (ContentException ex)
{
    Console.Error.WriteLine($"Content error: {ex.Message}");
    return 1;
}

if (!Has("--serve"))
{
    return 0;
}

var port = int.TryParse(Arg("--port"), out var parsed) ? parsed : 5000;
await PreviewServer.RunAsync(options, port);
return 0;

string? Arg(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

bool Has(string name) => args.Contains(name, StringComparer.Ordinal);

string Resolve(string? supplied, string defaultFolder) => Path.GetFullPath(
    supplied is null
        ? Path.Combine(repositoryRoot, defaultFolder)
        : Path.IsPathRooted(supplied) ? supplied : Path.Combine(repositoryRoot, supplied));
