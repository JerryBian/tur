using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tur.Core;
using Tur.Extension;
using Tur.Logging;
using Tur.Option;
using Tur.Util;

namespace Tur.Handler;

public class SyncHandler : HandlerBase
{
    private readonly SyncOption _option;

    private int _copiedFiles;
    private long _copiedBytes;
    private int _createdDirectories;
    private int _deletedFiles;
    private int _deletedDirectories;
    private bool _hasError;
    private readonly object _rateLock = new();
    private long _rateWindowStart;
    private long _rateWindowBytes;

    public SyncHandler(SyncOption option, CancellationToken cancellationToken) : base(option, cancellationToken)
    {
        _option = option;
    }

    protected override IEnumerable<string> GetScanRoots()
    {
        if (!string.IsNullOrEmpty(_option.SrcDir))
        {
            yield return _option.SrcDir;
        }

        if (!string.IsNullOrEmpty(_option.DestDir))
        {
            yield return _option.DestDir;
        }
    }

    protected override bool PreCheck()
    {
        _option.SrcDir = Path.GetFullPath(_option.SrcDir);
        if (!Directory.Exists(_option.SrcDir))
        {
            _logger.Log($"Source directory not exists: {_option.SrcDir}", TurLogLevel.Error, Constants.XUnicode, false);
            return false;
        }

        if (!string.IsNullOrEmpty(_option.ExcludeFrom))
        {
            _option.ExcludeFrom = Path.GetFullPath(_option.ExcludeFrom);
            if (!File.Exists(_option.ExcludeFrom))
            {
                _logger.Log($"File provided via --exclude-from not exists: {_option.ExcludeFrom}.", TurLogLevel.Error, Constants.XUnicode, false);
                return false;
            }

            var patterns = File.ReadAllLines(_option.ExcludeFrom)
                .Select(x => x.Trim())
                .Where(x => x.Length != 0 && !x.StartsWith('#'))
                .ToArray();
            _option.Excludes = _option.Excludes == null ? patterns : _option.Excludes.Concat(patterns).ToArray();
        }

        _option.DestDir = Path.GetFullPath(_option.DestDir);
        _ = Directory.CreateDirectory(_option.DestDir);

        return true;
    }

    protected override async Task<int> HandleInternalAsync()
    {
        var suffix = _option.DryRun ? "DRY RUN" : "";
        Dictionary<string, TurFileSystem> sourceItems = new();
        Dictionary<string, TurFileSystem> destItems = new();

        Parallel.Invoke(
            () => sourceItems = GetFileSystem(_option.SrcDir, true),
            () => destItems = GetFileSystem(_option.DestDir, false));

        // The copy-phase dest enumeration is reusable for the delete phase only when no
        // include/exclude/time/size filtering was applied to it (filtered listings would
        // hide extraneous entries that must still be considered for deletion).
        var canReuseDestEnumeration =
            (_option.Includes == null || _option.Includes.Length == 0)
            && (_option.Excludes == null || _option.Excludes.Length == 0)
            && !_option.MinSize.HasValue
            && !_option.MaxSize.HasValue
            && _option.CreateAfter == default
            && _option.CreateBefore == default
            && _option.LastModifyAfter == default
            && _option.LastModifyBefore == default;

        var shouldBreak = false;
        using ProgressRenderer progress = new(_option.Progress && !_option.Verbose && !_option.Json);
        var totalItems = sourceItems.Count;
        var processedItems = 0;
        // Explicit processor-count default: blocking File.Copy calls on an "unlimited"
        // ForEachAsync stall on slow thread-pool injection for large trees. Network
        // shares are latency-bound: higher concurrency hides the round trips.
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = _option.Threads ?? (IsRemoteEnvironment() ? RemoteCopyParallelism : Environment.ProcessorCount)
        };
        await Parallel.ForEachAsync(sourceItems.TakeWhile(x => !Volatile.Read(ref shouldBreak)), parallelOptions, async (srcItem, ct) =>
        {
            if (CancellationToken.IsCancellationRequested)
            {
                Volatile.Write(ref shouldBreak, true);
                return;
            }

            try
            {
                if (destItems.TryGetValue(srcItem.Key, out var dest))
                {
                    if (!dest.IsDirectory && dest.Length == srcItem.Value.Length)
                    {
                        if (_option.SizeOnly)
                        {
                            _logger.Log($"{srcItem.Value.RelativePath}", TurLogLevel.Trace, Constants.DashUnicode, suffix: suffix);
                            return;
                        }

                        if (await FileUtil.IsSameFileAsync(srcItem.Value.FullPath, dest.FullPath, _option.IgnoreError))
                        {
                            _logger.Log($"{srcItem.Value.RelativePath}", TurLogLevel.Trace, Constants.DashUnicode, suffix: suffix);
                            return;
                        }
                    }
                }

                var destFullPath = Path.Combine(_option.DestDir, srcItem.Key);
                if (srcItem.Value.IsDirectory)
                {
                    if (!_option.DryRun && !Directory.Exists(destFullPath))
                    {
                        _ = Directory.CreateDirectory(destFullPath);
                        _ = Interlocked.Increment(ref _createdDirectories);
                    }

                    return;
                }

                var destItemDir = Path.GetDirectoryName(destFullPath);
                if (!string.IsNullOrEmpty(destItemDir) && !_option.DryRun && !Directory.Exists(destItemDir))
                {
                    _ = Directory.CreateDirectory(destItemDir);
                    _ = Interlocked.Increment(ref _createdDirectories);
                }

                var sw = Stopwatch.StartNew();
                if (!_option.DryRun)
                {
                    await CopyWithLimitAsync(srcItem.Value.FullPath, destFullPath);
                    File.SetCreationTime(destFullPath, srcItem.Value.CreationTime.Value);
                    File.SetLastWriteTime(destFullPath, srcItem.Value.LastModifyTime.Value);
                    _ = Interlocked.Increment(ref _copiedFiles);
                    _ = Interlocked.Add(ref _copiedBytes, srcItem.Value.Length);
                }
                sw.Stop();
                _logger.Log(
                    $"{srcItem.Value.RelativePath}",
                    TurLogLevel.Information,
                    Constants.CheckUnicode,
                    suffix: $"{HumanUtil.GetSize(srcItem.Value.Length)}, {sw.Elapsed.Human()}{(_option.DryRun ? ", " + suffix : "")}");
            }
            catch (Exception ex)
            {
                if (_option.IgnoreError)
                {
                    _logger.Log(srcItem.Key, TurLogLevel.Warning, Constants.XUnicode, suffix: "SKIPPED", error: ex);
                }
                else
                {
                    _logger.Log(srcItem.Key, TurLogLevel.Error, Constants.XUnicode, error: ex);
                    Volatile.Write(ref _hasError, true);
                    Volatile.Write(ref shouldBreak, true);
                }
            }
            finally
            {
                var done = Interlocked.Increment(ref processedItems);
                progress.Report($"[sync] {done}/{totalItems} items, {HumanUtil.GetSize(Interlocked.Read(ref _copiedBytes))} copied");
            }
        });

        if (CancellationToken.IsCancellationRequested)
        {
            return 0;
        }

        if (_option.Delete || _option.DeleteExcluded)
        {
            Matcher excludedMatcher = null;
            if (_option.DeleteExcluded && _option.Excludes is { Length: > 0 })
            {
                excludedMatcher = new Matcher();
                _ = excludedMatcher.AddInclude("**");
                excludedMatcher.AddExcludePatterns(_option.Excludes);
            }

            IEnumerable<TurFileSystem> destItemsToDelete;
            if (canReuseDestEnumeration)
            {
                // Without globs/time/size filters the copy-phase dest enumeration already
                // holds the full unfiltered listing — reuse it instead of walking the whole
                // destination tree a second time. Ordering by descending RelativePath length
                // guarantees children are processed before their parent directories.
                destItemsToDelete = destItems.Values.OrderByDescending(x => x.RelativePath.Length);
            }
            else
            {
                var buildOptions = new TurBuildOptions
                {
                    IgnoreError = _option.IgnoreError,
                    IncludeFiles = true,
                    IncludeDirectories = true,
                    FollowLinks = _option.FollowLinks,
                    MaxDepth = _option.MaxDepth
                };
                var builder = new TurSystemBuilder(_option.DestDir, buildOptions, CancellationToken, LogSkipped);
                destItemsToDelete = builder.Build();
            }

            foreach (var destItem in destItemsToDelete)
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var srcItem = Path.Combine(_option.SrcDir, destItem.RelativePath);
                    var isExcluded = excludedMatcher != null && !excludedMatcher.Match(_option.DestDir, destItem.FullPath).HasMatches;
                    if (destItem.IsDirectory)
                    {
                        var srcExists = Directory.Exists(srcItem);
                        if (!srcExists || isExcluded)
                        {
                            if (_option.DryRun)
                            {
                                _logger.Log(destItem.RelativePath, TurLogLevel.Information, Constants.CheckUnicode, suffix: "D, DEST REMOVED, DRY RUN");
                            }
                            else
                            {
                                // Explicitly excluded directories are removed with their content;
                                // extraneous ones keep the historical non-recursive behavior.
                                Directory.Delete(destItem.FullPath, isExcluded);
                                _ = Interlocked.Increment(ref _deletedDirectories);
                                _logger.Log(destItem.RelativePath, TurLogLevel.Information, Constants.CheckUnicode, suffix: isExcluded ? "D, EXCLUDED REMOVED" : "D, DEST REMOVED");
                            }
                        }
                    }
                    else
                    {
                        if (!File.Exists(srcItem) || isExcluded)
                        {
                            if (_option.DryRun)
                            {
                                _logger.Log(destItem.RelativePath, TurLogLevel.Information, Constants.CheckUnicode, suffix: "F, DEST REMOVED, DRY RUN");
                            }
                            else
                            {
                                File.Delete(destItem.FullPath);
                                _ = Interlocked.Increment(ref _deletedFiles);
                                _logger.Log(destItem.RelativePath, TurLogLevel.Information, Constants.CheckUnicode, suffix: isExcluded ? "F, EXCLUDED REMOVED" : "D, DEST REMOVED");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_option.IgnoreError)
                    {
                        _logger.Log(destItem.FullPath, TurLogLevel.Warning, Constants.XUnicode, suffix: "SKIPPED", error: ex);
                    }
                    else
                    {
                        _logger.Log(destItem.FullPath, TurLogLevel.Error, Constants.XUnicode, error: ex);
                        Volatile.Write(ref _hasError, true);
                        return 1;
                    }
                }
            }
        }

        return Volatile.Read(ref _hasError) ? 1 : 0;
    }

    protected override void PostCheck()
    {
        base.PostCheck();
        var summary = $"{_copiedFiles} files ({HumanUtil.GetSize(Interlocked.Read(ref _copiedBytes))}) copied, {_createdDirectories} directories created.";
        if (_option.Delete)
        {
            summary += $" {_deletedFiles} files and {_deletedDirectories} directories deleted.";
        }

        if (_option.DryRun)
        {
            summary += " (dry run)";
        }

        _logger.Log(summary, TurLogLevel.Information, Constants.ArrowUnicode, false);
    }

    private const int CopyBufferSize = 1024 * 1024;

    private async Task CopyWithLimitAsync(string src, string dest)
    {
        // Local + unlimited: let the OS do the copy (CopyFileExW on Windows is
        // particularly efficient). Network shares or throttled copies use a managed
        // loop with a large pooled buffer and sequential-scan hints — high-latency
        // links (SMB/NFS) want few, large round trips instead of 80KB dribbles.
        if (_option.BwLimit <= 0 && !IsRemoteEnvironment())
        {
            File.Copy(src, dest, true);
            return;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
        try
        {
            await using FileStream source = new(src, FileMode.Open, FileAccess.Read, FileShare.Read, CopyBufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);
            await using FileStream target = new(dest, FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);
            int read;
            while ((read = await source.ReadAsync(buffer.AsMemory(0, CopyBufferSize), CancellationToken)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), CancellationToken);
                if (_option.BwLimit > 0)
                {
                    await ThrottleAsync(read);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task ThrottleAsync(int bytesWritten)
    {
        var delayMs = 0;
        lock (_rateLock)
        {
            var now = Stopwatch.GetTimestamp();
            var elapsedSeconds = _rateWindowStart == 0 ? double.MaxValue : (now - _rateWindowStart) / (double)Stopwatch.Frequency;
            if (elapsedSeconds >= 1.0)
            {
                _rateWindowStart = now;
                _rateWindowBytes = bytesWritten;
            }
            else
            {
                _rateWindowBytes += bytesWritten;
            }

            if (_rateWindowBytes > _option.BwLimit)
            {
                delayMs = (int)Math.Ceiling((_rateWindowBytes - _option.BwLimit) * 1000.0 / _option.BwLimit);
            }
        }

        if (delayMs > 0)
        {
            await Task.Delay(delayMs, CancellationToken);
        }
    }

    private Dictionary<string, TurFileSystem> GetFileSystem(string dir, bool includeAttributes)
    {
        var buildOptions = CreateBuildOptions();
        buildOptions.IncludeDirectories = buildOptions.IncludeFiles = true;
        buildOptions.IncludeFileSize = true;
        buildOptions.IncludeAttributes = includeAttributes;


        var builder = new TurSystemBuilder(dir, buildOptions, CancellationToken, LogSkipped);
        return builder.Build().ToDictionary(x => x.RelativePath, x => x);
    }
}