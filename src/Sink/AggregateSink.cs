using System;
using System.Linq;
using System.Threading.Tasks;
using Tur.Extension;

namespace Tur.Sink;

public class AggregateSink : ITurSink
{
    private readonly ITurSink[] _sinks;

    public AggregateSink(params ITurSink[] sinks)
    {
        if (sinks.Length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sinks), "At least one sink must be provided.");
        }

        _sinks = sinks;
    }

    public async ValueTask DisposeAsync()
    {
        var tasks = _sinks.Select(x => x.DisposeAsync());
        foreach (var task in tasks)
        {
            await task;
        }
    }

    public async Task NewLineAsync(bool verboseOnly = false, int count = 1)
    {
        await Task.WhenAll(_sinks.Select(x => x.NewLineAsync(verboseOnly, count).OkForCancel()));
    }

    public async Task DefaultAsync(string message, bool verboseOnly = false)
    {
        await Task.WhenAll(_sinks.Select(x => x.DefaultAsync(message, verboseOnly).OkForCancel()));
    }

    public async Task DefaultLineAsync(string message, bool verboseOnly = false)
    {
        await Task.WhenAll(_sinks.Select(x => x.DefaultLineAsync(message, verboseOnly).OkForCancel()));
    }

    public async Task LightAsync(string message, bool verboseOnly = false)
    {
        await Task.WhenAll(_sinks.Select(x => x.LightAsync(message, verboseOnly).OkForCancel()));
    }

    public async Task LightLineAsync(string message, bool verboseOnly = false)
    {
        await Task.WhenAll(_sinks.Select(x => x.LightLineAsync(message, verboseOnly).OkForCancel()));
    }

    public async Task InfoAsync(string message, bool verboseOnly = false)
    {
        await Task.WhenAll(_sinks.Select(x => x.InfoAsync(message, verboseOnly).OkForCancel()));
    }

    public async Task InfoLineAsync(string message, bool verboseOnly = false)
    {
        await Task.WhenAll(_sinks.Select(x => x.InfoLineAsync(message, verboseOnly).OkForCancel()));
    }

    public async Task WarnAsync(string message, bool verboseOnly = false)
    {
        await Task.WhenAll(_sinks.Select(x => x.WarnAsync(message, verboseOnly).OkForCancel()));
    }

    public async Task WarnLineAsync(string message, bool verboseOnly = false)
    {
        await Task.WhenAll(_sinks.Select(x => x.WarnLineAsync(message, verboseOnly).OkForCancel()));
    }

    public async Task ErrorAsync(string message, bool verboseOnly = false, Exception ex = null)
    {
        await Task.WhenAll(_sinks.Select(x => x.ErrorAsync(message, verboseOnly).OkForCancel()));
    }

    public async Task ErrorLineAsync(string message, bool verboseOnly = false, Exception ex = null)
    {
        await Task.WhenAll(_sinks.Select(x => x.ErrorLineAsync(message, verboseOnly).OkForCancel()));
    }

    public async Task ClearLineAsync(bool verboseOnly = false)
    {
        await Task.WhenAll(_sinks.Select(x => x.ClearLineAsync(verboseOnly).OkForCancel()));
    }
}