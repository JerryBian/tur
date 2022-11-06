using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Tur.Option;

namespace Tur.Sink;

public abstract class SinkBase : ITurSink
{
    private readonly BlockingCollection<SinkEntry> _messageQueue;
    private readonly OptionBase _option;
    private readonly Task _workerTask;

    protected SinkBase(OptionBase option)
    {
        _option = option;
        _messageQueue = new BlockingCollection<SinkEntry>(1024);
        _workerTask = Task.Run(async () => await ProcessMessageAsync());
    }

    public async ValueTask DisposeAsync()
    {
        _messageQueue.CompleteAdding();
        await _workerTask;
    }

    public async Task NewLineAsync(bool verboseOnly = false, int count = 1)
    {
        if (verboseOnly && !_option.EnableVerbose)
        {
            return;
        }

        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                await EnqueueAsync(Environment.NewLine, SinkType.Default);
            }
        }
    }

    public async Task DefaultAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !_option.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.Default);
    }

    public async Task DefaultLineAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !_option.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.DefaultLine);
    }

    public async Task LightAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !_option.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.Light);
    }

    public async Task LightLineAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !_option.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.LightLine);
    }

    public async Task InfoAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !_option.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.Info);
    }

    public async Task InfoLineAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !_option.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.InfoLine);
    }

    public async Task WarnAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !_option.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.Warn);
    }

    public async Task WarnLineAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !_option.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.WarnLine);
    }

    public async Task ErrorAsync(string message, bool verboseOnly = false, Exception ex = null)
    {
        if (verboseOnly && !_option.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.Error, ex);
    }

    public async Task ErrorLineAsync(string message, bool verboseOnly = false, Exception ex = null)
    {
        if (verboseOnly && !_option.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.ErrorLine, ex);
    }

    public async Task ClearLineAsync(bool verboseOnly = false)
    {
        if (verboseOnly && !_option.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(string.Empty, SinkType.ClearLine);
    }

    private async Task ProcessMessageAsync()
    {
        try
        {
            foreach (SinkEntry sinkEntry in _messageQueue.GetConsumingEnumerable())
            {
                await ProcessSinkEntryAsync(sinkEntry);
            }
        }
        catch
        {
            try
            {
                _messageQueue.CompleteAdding();
            }
            catch
            {
                // ignored
            }
        }
    }

    private async Task EnqueueAsync(string message, SinkType type, Exception ex = null)
    {
        SinkEntry entry = new(message, type, ex);
        if (!_messageQueue.IsAddingCompleted)
        {
            try
            {
                _messageQueue.Add(entry);
                return;
            }
            catch (InvalidOperationException)
            {
            }
        }

        try
        {
            await ProcessSinkEntryAsync(entry);
        }
        catch
        {
            // ignored
        }
    }

    protected abstract Task ProcessSinkEntryAsync(SinkEntry entry);
}