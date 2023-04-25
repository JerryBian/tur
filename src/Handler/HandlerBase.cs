using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tur.Appender;
using Tur.Extension;
using Tur.Model;
using Tur.Option;

namespace Tur.Handler;

public abstract class HandlerBase : IAsyncDisposable
{
    private readonly string _logFile;

    private readonly OptionBase _option;
    protected readonly CancellationToken CancellationToken;

    private readonly IAppender _consoleAppender;
    private readonly IAppender _fileAppender;

    protected HandlerBase(OptionBase option, CancellationToken cancellationToken)
    {
        _option = option;
        CancellationToken = cancellationToken;

        if (string.IsNullOrEmpty(option.OutputDir))
        {
            option.OutputDir = Path.GetTempPath();
        }

        _logFile = Path.Combine(option.OutputDir,
            $"tur-{option.CmdName}-{GetRandomFile()}.log");
        _consoleAppender = new ConsoleAppender();
        _fileAppender = new FileAppender(_logFile);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (CancellationToken.IsCancellationRequested)
        {
            LogItem logItem = new();
            logItem.AddSegment(LogSegmentLevel.Warn, "  User requested to cancel ...");
            AddLog(logItem);
        }

        LogItem logItem2 = new();
        logItem2.AddSegment(LogSegmentLevel.Verbose, $"{Constants.ArrowUnicode} Log file: {_logFile}");

        await _consoleAppender.DisposeAsync(); 
        await _fileAppender.DisposeAsync();
    }

    protected void AddLog(LogItem logItem)
    {
        _consoleAppender.TryAdd(logItem);
        _fileAppender.TryAdd(logItem);
    }

    protected string GetRandomFile()
    {
        return Path.GetRandomFileName().Replace(".", string.Empty);
    }

    public async Task<int> HandleAsync()
    {
        await WriteLogFileHeaderAsync();
        Stopwatch stopwatch = Stopwatch.StartNew();
        int exitCode = await HandleInternalAsync();
        stopwatch.Stop();

        LogItem logItem = new();
        logItem.AddSegment(LogSegmentLevel.Default, $"{Constants.ArrowUnicode} All done. Elapsed: [{stopwatch.Elapsed.Human()}]");
        return exitCode;
    }

    private async Task WriteLogFileHeaderAsync()
    {
        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        string versionStr = version == null ? "1.0.0" : version.ToString(3);
        StringBuilder sb = new();
        _ = sb.AppendLine($"### Processed by tur {versionStr}");
        _ = sb.AppendLine($"### Command: tur {string.Join(" ", _option.RawArgs)}");
        _ = sb.AppendLine();

        await File.WriteAllTextAsync(_logFile, sb.ToString(), new UTF8Encoding(false), CancellationToken)
            .OkForCancel();
    }

    protected abstract Task<int> HandleInternalAsync();
}