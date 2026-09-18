using Portfolio.Generator;

namespace Portfolio.Tests;

public sealed class RebuildQueueTests
{
    [Fact]
    public async Task Changes_arriving_during_a_build_trigger_one_followup_build()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var followup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        await using var queue = new RebuildQueue(async token =>
        {
            if (Interlocked.Increment(ref count) == 1)
            {
                started.SetResult();
                await release.Task.WaitAsync(token);
            }
            else
            {
                followup.TrySetResult();
            }
        });
        queue.Request();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        for (var index = 0; index < 50; index++) queue.Request();
        release.SetResult();
        await followup.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, count);
    }
}
