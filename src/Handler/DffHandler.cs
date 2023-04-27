using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Tur.Model;
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
        try
        {
            List<List<FileSystemItem>> items = await ScanByLengthAsync();
            TransformBlock<List<FileSystemItem>, DffResult> compareBlock = CreateCompareBlock();
            ActionBlock<DffResult> sinkBlock = CreateSinkBlock();

            _ = compareBlock.LinkTo(sinkBlock, new DataflowLinkOptions { PropagateCompletion = true });

            foreach (List<FileSystemItem> item in items)
            {
                _ = await compareBlock.SendAsync(item);
            }

            compareBlock.Complete();
            await sinkBlock.Completion;
        }
        catch (Exception ex)
        {
            LogItem logItem = new() { IsStdError = true };
            logItem.AddSegment(LogSegmentLevel.Error, "Unexpected error.", ex);
            AddLog(logItem);

            return 1;
        }

        return 0;
    }

    private ActionBlock<DffResult> CreateSinkBlock()
    {
        ActionBlock<DffResult> block = new(r =>
        {
            if (!r.HasDuplicate)
            {
                return;
            }

            long length = r.DuplicateItems.First().First().Size;
            LogItem logItem = new();
            logItem.AddSegment(LogSegmentLevel.Verbose, $"{Constants.ArrowUnicode} ");
            logItem.AddSegment(LogSegmentLevel.Verbose, "[");
            logItem.AddSegment(LogSegmentLevel.Default, HumanUtil.GetSize(length));
            logItem.AddSegment(LogSegmentLevel.Verbose, "]");
            logItem.AddSegment(LogSegmentLevel.Success, $" {r.DuplicateItems.Count} groups of duplicate:");
            logItem.AddLine();

            foreach (List<FileSystemItem> items in r.DuplicateItems)
            {
                foreach (FileSystemItem item in items)
                {
                    logItem.AddSegment(LogSegmentLevel.Verbose, $"  {Constants.DotUnicode} ");
                    logItem.AddSegment(LogSegmentLevel.Default, item.FullPath);
                    logItem.AddLine();
                }
            }

            AddLog(logItem);
        }, DefaultExecutionDataflowBlockOptions);

        return block;
    }

    private TransformBlock<List<FileSystemItem>, DffResult> CreateCompareBlock()
    {
        TransformBlock<List<FileSystemItem>, DffResult> block = new(async items =>
        {
            List<HashSet<FileSystemItem>> matchedGroups = new();
            for (int i = 0; i < items.Count - 1; i++)
            {
                FileSystemItem item1 = items[i];
                if (matchedGroups.Any(x => x.Contains(item1)))
                {
                    continue;
                }

                for (int j = i + 1; j < items.Count; j++)
                {
                    FileSystemItem item2 = items[j];
                    if (matchedGroups.Any(x => x.Contains(item2)))
                    {
                        continue;
                    }

                    if (await FileUtil.IsSameFileAsync(item1.FullPath, item2.FullPath, _option.IgnoreError))
                    {
                        HashSet<FileSystemItem> group = matchedGroups.FirstOrDefault(x => x.Contains(item1));
                        if (group == null)
                        {
                            group = new HashSet<FileSystemItem>
                            {
                                item1
                            };
                            matchedGroups.Add(group);
                        }

                        _ = group.Add(item2);
                    }
                }
            }

            DffResult result = new();
            matchedGroups.ForEach(x => result.DuplicateItems.Add(x.ToList()));
            return result;
        }, DefaultExecutionDataflowBlockOptions);

        return block;
    }

    private async Task<List<List<FileSystemItem>>> ScanByLengthAsync()
    {
        ConcurrentDictionary<long, List<FileSystemItem>> result = new();
        ActionBlock<FileSystemItem> block = new(item =>
        {
            _ = result.AddOrUpdate(item.Size, new List<FileSystemItem> { item }, (x, val) =>
            {
                val.Add(item);
                return val;
            });
        }, DefaultExecutionDataflowBlockOptions);

        foreach (FileSystemItem item in FileUtil.EnumerateFiles(
            _option.Dir,
            _option.IgnoreError,
            _option.Includes?.ToList(),
            _option.Excludes?.ToList(),
            _option.CreateBefore,
            _option.CreateAfter,
            _option.LastModifyBefore,
            _option.LastModifyAfter))
        {
            _ = await block.SendAsync(item);
        }

        block.Complete();
        await block.Completion;

        return result.TakeWhile(x => x.Value.Count > 1).Select(x => x.Value).ToList();
    }
}