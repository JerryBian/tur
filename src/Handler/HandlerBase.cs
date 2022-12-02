﻿using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tur.Extension;
using Tur.Option;
using Tur.Sink;

namespace Tur.Handler;

public abstract class HandlerBase : IAsyncDisposable
{
    private readonly string _logFile;
    private readonly int _maxBytesScan;
    private readonly OptionBase _option;
    protected readonly ITurSink AggregateOutputSink;
    protected readonly CancellationToken CancellationToken;

    protected readonly ITurSink ConsoleSink;
    protected readonly ITurSink LogFileSink;

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

        ConsoleSink = new ConsoleSink(_option);
        LogFileSink = new FileSink(_logFile, option);
        AggregateOutputSink = new AggregateSink(ConsoleSink, LogFileSink);

        GCMemoryInfo gcMemoryInfo = GC.GetGCMemoryInfo();
        _maxBytesScan = Convert.ToInt32(Math.Min(gcMemoryInfo.TotalAvailableMemoryBytes / 10, 3 * 1024 * 1024));
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (CancellationToken.IsCancellationRequested)
        {
            await AggregateOutputSink.WarnLineAsync("  User requested to cancel ...");
        }

        await ConsoleSink.LightLineAsync($"{Constants.ArrowUnicode} Log file: {_logFile}");
        await AggregateOutputSink.DisposeAsync();
    }

    protected string GetRandomFile()
    {
        return Path.GetRandomFileName().Replace(".", string.Empty);
    }

    protected IEnumerable<string> EnumerateFiles(string dir, bool applyFilter = false, bool returnAbsolutePath = false)
    {
        if (!Directory.Exists(dir))
        {
            yield break;
        }

        foreach (string f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(dir, f);

            if (!applyFilter)
            {
                yield return returnAbsolutePath ? f : relativePath;
            }

            Matcher matcher = new();
            if (_option.Includes is not { Length: > 0 })
            {
                _ = matcher.AddInclude("**");
            }
            else
            {
                foreach (string include in _option.Includes)
                {
                    _ = matcher.AddInclude(include);
                }
            }

            if (_option.Excludes != null)
            {
                foreach (string exclude in _option.Excludes)
                {
                    _ = matcher.AddExclude(exclude);
                }
            }

            PatternMatchingResult matchResult = matcher.Match(dir, f);
            if (matchResult.HasMatches)
            {
                bool flag;
                if (_option.LastModifyAfter == default &&
                    _option.LastModifyBefore == default &&
                    _option.CreateBefore == default &&
                    _option.CreateAfter == default)
                {
                    flag = true;
                }
                else
                {
                    flag = true;

                    if (_option.LastModifyBefore != default || _option.LastModifyAfter != default)
                    {
                        DateTime fileLastModify = File.GetLastWriteTime(f);
                        DateTime lastModifyAfter = _option.LastModifyAfter != default ? _option.LastModifyAfter : DateTime.MinValue;
                        DateTime lastModifyBefore = _option.LastModifyBefore != default ? _option.LastModifyBefore : DateTime.MaxValue;
                        flag = fileLastModify >= lastModifyAfter && fileLastModify <= lastModifyBefore;
                    }

                    if (flag && (_option.CreateAfter != default || _option.CreateBefore != default))
                    {
                        DateTime fileCreateAt = File.GetCreationTime(f);
                        DateTime createAfter = _option.CreateAfter != default ? _option.CreateAfter : DateTime.MinValue;
                        DateTime createBefore = _option.CreateBefore != default ? _option.CreateBefore : DateTime.MaxValue;
                        flag = fileCreateAt >= createAfter && fileCreateAt <= createBefore;
                    }
                }

                if (flag)
                {
                    yield return returnAbsolutePath ? f : relativePath;
                }
            }
        }
    }

    protected async Task CopyAsync(string srcFile, string destFile, Func<int, double, Task> progressChanged)
    {
        try
        {
            if (!File.Exists(srcFile))
            {
                return;
            }

            string destDir = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(destDir))
            {
                _ = Directory.CreateDirectory(destDir);
            }

            await using FileStream src = new(srcFile, FileMode.Open, FileAccess.Read);
            long srcFileLength = src.Length;
            byte[] buffer = new byte[Math.Min(_maxBytesScan, srcFileLength)];
            await using FileStream dest = new(destFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            dest.SetLength(0);
            long bytesWritten = 0L;
            int currentBlockSize;

            Stopwatch stopwatch = Stopwatch.StartNew();
            while ((currentBlockSize = await src.ReadAsync(buffer, CancellationToken)) > 0)
            {
                await dest.WriteAsync(buffer, 0, currentBlockSize, CancellationToken);
                bytesWritten += currentBlockSize;
                
                await progressChanged(Convert.ToInt32(bytesWritten / (double)srcFileLength * 100),
                    bytesWritten / stopwatch.Elapsed.TotalSeconds);
            }
        }
        catch (Exception ex)
        {
            if (!_option.IgnoreError)
            {
                throw;
            }

            await ConsoleSink.WarnAsync($"Copying [{srcFile}] to [{destFile}] failed, operation skipped.");
            await LogFileSink.WarnAsync($"Copying [{srcFile}] to [{destFile}] failed, operation skipped. Error={ex}");
        }
    }

    protected async Task<bool> IsSameFileAsync(string file1, string file2)
    {
        try
        {
            if (string.Equals(file1, file2))
            {
                return true;
            }

            FileInfo fileInfo1 = new(file1);
            FileInfo fileInfo2 = new(file2);
            if (fileInfo1.Length != fileInfo2.Length)
            {
                return false;
            }

            int maxBytesScan = Convert.ToInt32(Math.Min(_maxBytesScan, fileInfo1.Length));
            int iterations = (int)Math.Ceiling((double)fileInfo1.Length / maxBytesScan);
            await using FileStream f1 = fileInfo1.OpenRead();
            await using FileStream f2 = fileInfo2.OpenRead();
            byte[] first = new byte[maxBytesScan];
            byte[] second = new byte[maxBytesScan];

            for (int i = 0; i < iterations; i++)
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                int firstBytes = await f1.ReadAsync(first.AsMemory(0, maxBytesScan), CancellationToken);
                int secondBytes = await f2.ReadAsync(second.AsMemory(0, maxBytesScan), CancellationToken);
                if (firstBytes != secondBytes)
                {
                    return false;
                }

                if (!AreBytesEqual(first, second))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            if (!_option.IgnoreError)
            {
                throw;
            }

            await ConsoleSink.WarnAsync($"Comparing [{file1}] with [{file2}] failed, the match result is marked as false.");
            await LogFileSink.WarnAsync($"Comparing [{file1}] with [{file2}] failed, the match result is marked as false. Error={ex}");
            return false;
        }
    }

    private bool AreBytesEqual(ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2)
    {
        return b1.SequenceEqual(b2);
    }

    protected IEnumerable<string> EnumerateDirectories(string dir, bool applyFilter = false,
        bool returnAbsolutePath = false)
    {
        if (!Directory.Exists(dir))
        {
            return Enumerable.Empty<string>();
        }

        IEnumerable<string> dirs = Directory
            .EnumerateDirectories(dir, "*", SearchOption.AllDirectories);
        if (applyFilter)
        {
            Matcher matcher = new();
            if (_option.Includes is not { Length: > 0 })
            {
                _ = matcher.AddInclude("**");
            }
            else
            {
                foreach (string include in _option.Includes)
                {
                    _ = matcher.AddInclude(include);
                }
            }

            if (_option.Excludes != null)
            {
                foreach (string exclude in _option.Excludes)
                {
                    _ = matcher.AddExclude(exclude);
                }
            }

            PatternMatchingResult matchResult = matcher.Match(dir, dirs);
            return returnAbsolutePath
                ? matchResult.Files.Select(x => Path.Combine(dir, x.Path))
                : matchResult.Files.Select(x => Path.GetRelativePath(dir, Path.Combine(dir, x.Path)));
        }

        return returnAbsolutePath
            ? dirs
            : dirs
            .Select(x => Path.GetRelativePath(dir, x));
    }

    public virtual async Task<int> HandleAsync()
    {
        await WriteLogFileHeaderAsync();
        Stopwatch stopwatch = Stopwatch.StartNew();
        int exitCode = await HandleInternalAsync();
        stopwatch.Stop();
        await AggregateOutputSink.DefaultLineAsync(
            $"{Constants.ArrowUnicode} All done. Elapsed: [{stopwatch.Elapsed.Human()}]");
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