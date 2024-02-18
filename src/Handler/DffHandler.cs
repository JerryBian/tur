using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public DffHandler(DffOption option, CancellationToken cancellationToken) : base(option, cancellationToken)
    {
        _option = option;
    }

    protected override async Task<int> HandleInternalAsync()
    {
        var files = new ConcurrentBag<TurFileSystem>();
        _ = Parallel.ForEach(_option.Dir, x =>
        {
            var buildOptions = CreateBuildOptions();
            buildOptions.IncludeFileSize = buildOptions.IncludeFiles = true;
            var builder = new TurSystemBuilder(x, buildOptions, CancellationToken);
            foreach (var item in builder.Build())
            {
                files.Add(item);
            }
        });

        if (CancellationToken.IsCancellationRequested)
        {
            return 0;
        }

        foreach (var item in files.GroupBy(x => x.Length))
        {
            if (CancellationToken.IsCancellationRequested)
            {
                break;
            }

            var duplicateItems = await GetDuplicateFilesAsync(item.ToList());
            if (duplicateItems.Count != 0)
            {
                var prefix = duplicateItems.Count > 1 ? "  " : "";
                _logger.Write($"Found {duplicateItems.Count} duplicate groups:", TurLogLevel.Information, HumanUtil.GetSize(item.Key));
                for (var i = 0; i < duplicateItems.Count; i++)
                {
                    if (duplicateItems.Count > 1)
                    {
                        _logger.Write($"  Group {i + 1}:", TurLogLevel.Information);
                    }

                    foreach (var duplicateItem in duplicateItems[i])
                    {
                        _logger.Write($"  {prefix}{Constants.SquareUnicode} {duplicateItem.FullPath}");
                    }
                }
            }
        }

        return 0;
    }

    private async Task<List<HashSet<TurFileSystem>>> GetDuplicateFilesAsync(List<TurFileSystem> items)
    {
        List<HashSet<TurFileSystem>> matchedGroups = new();
        for (var i = 0; i < items.Count - 1; i++)
        {
            var item1 = items[i];
            if (matchedGroups.Any(x => x.Contains(item1)))
            {
                continue;
            }

            for (var j = i + 1; j < items.Count; j++)
            {
                var item2 = items[j];
                if (matchedGroups.Any(x => x.Contains(item2)))
                {
                    continue;
                }

                if (await FileUtil.IsSameFileAsync(item1.FullPath, item2.FullPath, _option.IgnoreError))
                {
                    var group = matchedGroups.FirstOrDefault(x => x.Contains(item1));
                    if (group == null)
                    {
                        group = new HashSet<TurFileSystem>
                        {
                            item1
                        };
                        matchedGroups.Add(group);
                    }

                    _ = group.Add(item2);
                }
            }
        }

        return matchedGroups;
    }

    protected override bool PreCheck()
    {
        _option.Dir ??= new[] { Path.GetFullPath(Environment.CurrentDirectory) };

        for (var i = 0; i < _option.Dir.Length; i++)
        {
            _option.Dir[i] = Path.GetFullPath(_option.Dir[i]);
            _logger.Write($"Target directory not exists: {_option.Dir[i]}");
        }

        return base.PreCheck();
    }
}