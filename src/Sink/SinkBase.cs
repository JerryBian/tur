﻿using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Tur.Option;

namespace Tur.Sink;

public abstract class SinkBase : ITurSink
{
    private readonly BlockingCollection<SinkEntry> _messageQueue;
    protected readonly OptionBase SinkOption;
    private readonly Task _workerTask;

    protected SinkBase(OptionBase option)
    {
        SinkOption = option;
        _messageQueue = new BlockingCollection<SinkEntry>(1024);
        _workerTask = Task.Run(ProcessMessageAsync);
    }

    public async ValueTask DisposeAsync()
    {
        _messageQueue.CompleteAdding();
        await _workerTask;
    }

    public async Task NewLineAsync(bool verboseOnly = false, int count = 1)
    {
        if (verboseOnly && !SinkOption.EnableVerbose)
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
        if (verboseOnly && !SinkOption.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.Default);
    }

    public async Task DefaultLineAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !SinkOption.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.DefaultLine);
    }

    public async Task LightAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !SinkOption.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.Light);
    }

    public async Task LightLineAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !SinkOption.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.LightLine);
    }

    public async Task InfoAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !SinkOption.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.Info);
    }

    public async Task InfoLineAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !SinkOption.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.InfoLine);
    }

    public async Task WarnAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !SinkOption.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.Warn);
    }

    public async Task WarnLineAsync(string message, bool verboseOnly = false)
    {
        if (verboseOnly && !SinkOption.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.WarnLine);
    }

    public async Task ErrorAsync(string message, bool verboseOnly = false, Exception ex = null)
    {
        if (verboseOnly && !SinkOption.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.Error, ex);
    }

    public async Task ErrorLineAsync(string message, bool verboseOnly = false, Exception ex = null)
    {
        if (verboseOnly && !SinkOption.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(message, SinkType.ErrorLine, ex);
    }

    public async Task ClearLineAsync(bool verboseOnly = false, int cursorTop = -1)
    {
        if (verboseOnly && !SinkOption.EnableVerbose)
        {
            return;
        }

        await EnqueueAsync(string.Empty, SinkType.ClearLine, state: cursorTop);
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

    private async Task EnqueueAsync(string message, SinkType type, Exception ex = null, int state = -1)
    {
        SinkEntry entry = new(message, type, ex) { State = state };
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