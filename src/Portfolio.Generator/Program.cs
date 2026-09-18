using System.Diagnostics;
using Portfolio.Content;
using Portfolio.Generator;

var repositoryRoot = RepositoryRoot.Find();

var valueOptions = new[] { "--output", "--content", "--assets", "--static", "--port" };
var flags = new[] { "--drafts", "--serve", "--help", "-h" };
var seen = new HashSet<string>(StringComparer.Ordinal);
for (var index = 0; index < args.Length; index++)
{
    var argument = args[index];
    if (!seen.Add(argument))
    {
        Console.Error.WriteLine($"Argument error: '{argument}' was supplied more than once.");
        return 1;
    }
    if (valueOptions.Contains(argument, StringComparer.Ordinal))
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith('-'))
        {
            Console.Error.WriteLine($"Argument error: '{argument}' requires a value.");
            return 1;
        }
        index++;
    }
    else if (!flags.Contains(argument, StringComparer.Ordinal))
    {
        Console.Error.WriteLine($"Argument error: unknown option '{argument}'. Use --help.");
        return 1;
    }
}

var port = 5000;
if (Arg("--port") is { } suppliedPort
    && (!int.TryParse(suppliedPort, out port) || port is < 1 or > 65535))
{
    Console.Error.WriteLine("Argument error: --port must be an integer from 1 to 65535.");
    return 1;
}

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
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
{
    Console.Error.WriteLine($"Build error: {ex.Message}");
    return 1;
}

if (!Has("--serve"))
{
    return 0;
}

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
