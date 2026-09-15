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

        var app = builder.Build();
        var files = new PhysicalFileProvider(options.OutputRoot);

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

        using var watcher = StartWatching(options);

        Console.WriteLine($"Preview: http://localhost:{port}  (Ctrl+C to stop)");
        await app.RunAsync();
    }

    private static IDisposable StartWatching(BuildOptions options)
    {
        var debounce = new SemaphoreSlim(1, 1);
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
                EnableRaisingEvents = true,
            };

            FileSystemEventHandler handler = async (_, _) => await RebuildAsync(options, debounce);
            watcher.Changed += handler;
            watcher.Created += handler;
            watcher.Deleted += handler;
            watcher.Renamed += async (_, _) => await RebuildAsync(options, debounce);

            watchers.Add(watcher);
        }

        return new CompositeDisposable(watchers);
    }

    private static async Task RebuildAsync(BuildOptions options, SemaphoreSlim gate)
    {
        if (!await gate.WaitAsync(0))
        {
            return;
        }

        try
        {
            // Editors write in several steps; let the burst settle before reading.
            await Task.Delay(120);
            await new SiteBuilder(options).BuildAsync();
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
        finally
        {
            gate.Release();
        }
    }

    private sealed class CompositeDisposable(List<FileSystemWatcher> watchers) : IDisposable
    {
        public void Dispose()
        {
            foreach (var watcher in watchers)
            {
                watcher.Dispose();
            }
        }
    }
}
