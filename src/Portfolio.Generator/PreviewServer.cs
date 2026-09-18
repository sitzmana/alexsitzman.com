using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Portfolio.Content;

namespace Portfolio.Generator;

/// <summary>
/// Local preview only. Serves the generated folder and rebuilds when content,
/// styles, or scripts change. Production hosting is plain static files.
/// </summary>
internal static class PreviewServer
{
    public static async Task RunAsync(BuildOptions options, int port)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseUrls($"http://localhost:{port}");

        await using var app = builder.Build();
        using var files = new PhysicalFileProvider(options.OutputRoot);

        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = files,
            ServeUnknownFileTypes = false,
            OnPrepareResponse = ctx =>
                ctx.Context.Response.Headers.CacheControl = "no-store",
        });

        // Terminal middleware rather than MapFallback: an endpoint selected by routing
        // causes the default-files and static-file middleware to skip, which would break
        // directory URLs such as "/" and "/about/".
        app.Run(async context =>
        {
            var notFound = Path.Combine(options.OutputRoot, "404.html");
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "text/html; charset=utf-8";
            if (File.Exists(notFound))
            {
                await context.Response.SendFileAsync(notFound);
            }
        });

        await using var watcher = StartWatching(options);

        Console.WriteLine($"Preview: http://localhost:{port}  (Ctrl+C to stop)");
        await app.RunAsync();
    }

    private static IAsyncDisposable StartWatching(BuildOptions options)
    {
        var queue = new RebuildQueue(token => RebuildAsync(options, token));
        var watchers = new List<FileSystemWatcher>();

        foreach (var root in new[] { options.ContentRoot, options.AssetsRoot, options.StaticRoot })
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            var watcher = new FileSystemWatcher(root)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            };

            FileSystemEventHandler handler = (_, _) => queue.Request();
            watcher.Changed += handler;
            watcher.Created += handler;
            watcher.Deleted += handler;
            watcher.Renamed += (_, _) => queue.Request();
            watcher.Error += (_, args) =>
            {
                Console.Error.WriteLine($"File watcher error: {args.GetException().Message}. Requesting a full rebuild.");
                queue.Request();
            };
            watcher.EnableRaisingEvents = true;

            watchers.Add(watcher);
        }

        return new WatchSubscription(watchers, queue);
    }

    private static async Task RebuildAsync(BuildOptions options, CancellationToken token)
    {
        try
        {
            await new SiteBuilder(options).BuildAsync(token);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Rebuilt.");
        }
        catch (ContentException ex)
        {
            Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss}] Content error: {ex.Message}");
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss}] {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss}] {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss}] {ex.Message}");
        }
    }

    private sealed class WatchSubscription(List<FileSystemWatcher> watchers, RebuildQueue queue) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            foreach (var watcher in watchers)
            {
                watcher.Dispose();
            }
            await queue.DisposeAsync();
        }
    }
}
