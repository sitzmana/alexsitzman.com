using System.Threading.Channels;

namespace Portfolio.Generator;

internal sealed class RebuildQueue : IAsyncDisposable
{
    private readonly Channel<bool> _requests = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        SingleReader = true,
        FullMode = BoundedChannelFullMode.DropWrite,
    });
    private readonly CancellationTokenSource _stop = new();
    private readonly Func<CancellationToken, Task> _rebuild;
    private readonly Task _worker;

    public RebuildQueue(Func<CancellationToken, Task> rebuild)
    {
        _rebuild = rebuild;
        _worker = RunAsync();
    }

    public void Request() => _requests.Writer.TryWrite(true);

    private async Task RunAsync()
    {
        var token = _stop.Token;
        while (await _requests.Reader.WaitToReadAsync(token))
        {
            while (_requests.Reader.TryRead(out _)) { }
            await Task.Delay(120, token);
            while (_requests.Reader.TryRead(out _)) { }
            // Events arriving during this build remain queued for the next pass.
            await _rebuild(token);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _requests.Writer.TryComplete();
        await _stop.CancelAsync();
        try
        {
            await _worker;
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
        }
        finally
        {
            _stop.Dispose();
        }
    }
}
