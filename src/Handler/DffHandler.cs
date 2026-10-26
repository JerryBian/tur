using Humanizer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tur.Core;
using Tur.Logging;
using Tur.Option;
using Tur.Util;

namespace Tur.Handler;

public class DffHandler : HandlerBase
{
    private readonly DffOption _option;
    private ProgressRenderer _progress;
    private int _hashedCount;

    public DffHandler(DffOption option, CancellationToken cancellationToken) : base(option, cancellationToken)
    {
        _option = option;
    }

    protected override IEnumerable<string> GetScanRoots()
    {
        return _option.Dir ?? Array.Empty<string>();
    }

    protected override async Task<int> HandleInternalAsync()
    {
        var files = new ConcurrentBag<TurFileSystem>();
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = _option.Threads ?? -1 };
        _ = Parallel.ForEach(_option.Dir, parallelOptions, x =>
        {
            var buildOptions = CreateBuildOptions();
            buildOptions.IncludeFileSize = buildOptions.IncludeFiles = true;
            if (_option.WriteList != null)
            {
                // The keep policy needs timestamps to pick the keeper.
                buildOptions.IncludeAttributes = true;
            }

            var builder = new TurSystemBuilder(x, buildOptions, CancellationToken, LogSkipped);
            foreach (var item in builder.Build())
            {
                files.Add(item);
            }
        });

        if (CancellationToken.IsCancellationRequested)
        {
            return 0;
        }

        using ProgressRenderer progress = new(_option.Progress && !_option.Verbose && !_option.Json);
        _progress = progress;

        var hasFoundBefore = false;
        List<string> deleteList = new();
        foreach (var item in files.GroupBy(x => x.Length))
        {
            if (CancellationToken.IsCancellationRequested)
            {
                break;
            }

            var duplicateItems = await GetDuplicateFilesAsync(item.ToList());
            if (duplicateItems.Count != 0)
            {
                if (hasFoundBefore)
                {
                    _logger.Log(string.Empty);
                }

                hasFoundBefore = true;
                var prefix = duplicateItems.Count > 1 ? "  " : "";
                _logger.Log($"Found {"duplicate group".ToQuantity(duplicateItems.Count)} for file size {HumanUtil.GetSize(item.Key)}:", TurLogLevel.Information, Constants.ArrowUnicode, false);
                for (var i = 0; i < duplicateItems.Count; i++)
                {
                    if (duplicateItems.Count > 1)
                    {
                        _logger.Log($"  Group {i + 1}:", TurLogLevel.Information, Constants.SquareUnicode, false);
                    }

                    foreach (var duplicateItem in duplicateItems[i])
                    {
                        _logger.Log($"  {prefix}{duplicateItem.FullPath}", TurLogLevel.Information, Constants.DotUnicode, false);
                    }
                }

                if (_option.WriteList != null)
                {
                    foreach (var group in duplicateItems)
                    {
                        var keeper = ChooseKeeper(group);
                        foreach (var duplicateItem in group.Where(x => !ReferenceEquals(x, keeper)))
                        {
                            deleteList.Add(duplicateItem.FullPath);
                        }
                    }
                }
            }
        }

        if (_option.WriteList != null)
        {
            var listPath = Path.GetFullPath(_option.WriteList);
            var listDir = Path.GetDirectoryName(listPath);
            if (!string.IsNullOrEmpty(listDir))
            {
                _ = Directory.CreateDirectory(listDir);
            }

            await File.WriteAllLinesAsync(
                listPath,
                deleteList.OrderBy(x => x, StringComparer.Ordinal),
                new UTF8Encoding(false),
                CancellationToken);
            _logger.Log($"Deletion list written to: {listPath} ({deleteList.Count} items). Feed it to 'tur rm --from-file'.", TurLogLevel.Information, Constants.CheckUnicode, false);
        }

        if (!hasFoundBefore)
        {
            _logger.Log("No duplicate files found.", TurLogLevel.Information, Constants.ArrowUnicode, false);
        }

        return 0;
    }

    private TurFileSystem ChooseKeeper(HashSet<TurFileSystem> group)
    {
        switch ((_option.Keep ?? "first").ToLowerInvariant())
        {
            case "newest":
                return group.OrderByDescending(x => x.LastModifyTime ?? DateTime.MinValue).First();
            case "oldest":
                return group.OrderBy(x => x.LastModifyTime ?? DateTime.MaxValue).First();
            default:
                return group.OrderBy(x => x.FullPath, StringComparer.Ordinal).First();
        }
    }

    private async Task<List<HashSet<TurFileSystem>>> GetDuplicateFilesAsync(List<TurFileSystem> items)
    {
        List<HashSet<TurFileSystem>> matchedGroups = new();
        if (items.Count < 2)
        {
            return matchedGroups;
        }

        // Stage 1 (same size) is done by the caller. Stage 2: cheap head hash,
        // stage 3: full content hash — avoids O(n^2) full-file comparisons.
        var partialGroups = await GroupByHashAsync(items, true);
        foreach (var partialGroup in partialGroups)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (partialGroup.Count < 2)
            {
                continue;
            }

            var fullGroups = await GroupByHashAsync(partialGroup, false);
            foreach (var fullGroup in fullGroups.Where(x => x.Count > 1))
            {
                matchedGroups.Add(new HashSet<TurFileSystem>(fullGroup));
            }
        }

        return matchedGroups;
    }

    private async Task<List<List<TurFileSystem>>> GroupByHashAsync(List<TurFileSystem> items, bool partial)
    {
        // Hash in parallel (I/O bound); results land at fixed indexes so grouping
        // stays deterministic regardless of completion order. Network shares get a
        // concurrency floor to hide per-read latency.
        var hashes = new string[items.Count];
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = _option.Threads ?? (IsRemoteEnvironment() ? RemoteHashParallelism : Environment.ProcessorCount)
        };
        await Parallel.ForEachAsync(Enumerable.Range(0, items.Count), parallelOptions, async (index, ct) =>
        {
            if (CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var item = items[index];
            var hash = await FileUtil.GetHashAsync(item.FullPath, partial, _option.IgnoreError, CancellationToken);
            hashes[index] = hash;
            if (hash == null)
            {
                LogSkipped(item.FullPath, new IOException("File could not be read for hashing."));
            }

            var done = Interlocked.Increment(ref _hashedCount);
            _progress?.Report($"[dff] {done} files hashed");
        });

        Dictionary<string, List<TurFileSystem>> map = new(StringComparer.Ordinal);
        for (var i = 0; i < items.Count; i++)
        {
            var hash = hashes[i];
            if (hash == null)
            {
                continue;
            }

            if (!map.TryGetValue(hash, out var bucket))
            {
                bucket = new List<TurFileSystem>();
                map[hash] = bucket;
            }

            bucket.Add(items[i]);
        }

        return map.Values.ToList();
    }

    protected override bool PreCheck()
    {
        if (_option.Keep != null
            && !_option.Keep.Equals("first", StringComparison.OrdinalIgnoreCase)
            && !_option.Keep.Equals("newest", StringComparison.OrdinalIgnoreCase)
            && !_option.Keep.Equals("oldest", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Log($"Invalid --keep value: {_option.Keep}. Valid values: first, newest, oldest.", TurLogLevel.Error, Constants.XUnicode, false);
            return false;
        }

        _option.Dir ??= new[] { Path.GetFullPath(Environment.CurrentDirectory) };

        for (var i = 0; i < _option.Dir.Length; i++)
        {
            _option.Dir[i] = Path.GetFullPath(_option.Dir[i]);
            if (!Directory.Exists(_option.Dir[i]))
            {
                _logger.Log($"Target directory not exists: {_option.Dir[i]}", TurLogLevel.Error, Constants.XUnicode, false);
                return false;
            }

        }

        return base.PreCheck();
    }
}