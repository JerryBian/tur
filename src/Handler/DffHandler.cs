using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tur.Extension;
using Tur.Option;
using Tur.Sink;
using Tur.Util;

namespace Tur.Handler;

public class DffHandler : HandlerBase
{
    private readonly string _exportedDupFile;
    private readonly ITurSink _exportedDupFileSink;
    private readonly DffOption _option;

    public DffHandler(DffOption option, CancellationToken cancellationToken) : base(option, cancellationToken)
    {
        _option = option;
        _exportedDupFile = Path.Combine(option.OutputDir,
            $"tur-{option.CmdName}-{GetRandomFile()}.txt");
        _exportedDupFileSink = new FileSink(_exportedDupFile, option);
    }

    protected override async Task<int> HandleInternalAsync()
    {
        try
        {
            Dictionary<long, List<string>> groupedItems = await ScanAndGroupFilesAsync();
            await AggregateOutputSink.InfoLineAsync(
                $"{Constants.ArrowUnicode} Found {groupedItems.Count} groups with exactly same file size.");
            await AggregateOutputSink.NewLineAsync();

            for (int i = 0; i < groupedItems.Count; i++)
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await ProcessGroupAsync(i, groupedItems);
            }

            await AggregateOutputSink.LightLineAsync(
                $"{Constants.ArrowUnicode} Duplicate files list: {_exportedDupFile}");
            await AggregateOutputSink.NewLineAsync();
        }
        catch (Exception ex)
        {
            await AggregateOutputSink.ErrorLineAsync(ex.Message, ex: ex);
            return 0;
        }

        return 1;
    }

    private async Task ProcessGroupAsync(int i, Dictionary<long, List<string>> groupedItems)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        bool found = false;
        int currentGroup = i + 1;
        KeyValuePair<long, List<string>> group = groupedItems.ElementAt(i);
        await AggregateOutputSink.LightAsync($"{Constants.ArrowUnicode} ");
        await AggregateOutputSink.LightAsync($"[{currentGroup}/{groupedItems.Count}] ", true);
        await AggregateOutputSink.DefaultAsync("Working on group ");
        await AggregateOutputSink.InfoAsync(currentGroup.ToString());
        await AggregateOutputSink.DefaultAsync(" [");
        await AggregateOutputSink.InfoAsync(HumanUtil.GetSize(group.Key));
        await AggregateOutputSink.DefaultAsync("], it contains ");
        await AggregateOutputSink.InfoAsync($"{group.Value.Count}");
        await AggregateOutputSink.DefaultLineAsync(" files.");

        HashSet<string> processedFiles = new();
        foreach (string file1 in group.Value)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                break;
            }

            _ = processedFiles.Add(file1);
            List<string> targetFiles = group.Value.Where(x => !processedFiles.Contains(x)).ToList();
            if (!targetFiles.Any() || CancellationToken.IsCancellationRequested)
            {
                break;
            }

            List<string> sameFiles = new();
            await AggregateOutputSink.LightLineAsync(
                $"  {Constants.SquareUnicode} Comparing below {targetFiles.Count} files with {file1}",
                true);
            foreach (string file2 in targetFiles)
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await AggregateOutputSink.LightAsync($"    {Constants.DotUnicode} {file2} ", true);
                if (await IsSameFileAsync(Path.Combine(_option.Dir, file1),
                        Path.Combine(_option.Dir, file2)))
                {
                    found = true;
                    _ = processedFiles.Add(file2);
                    await AggregateOutputSink.WarnLineAsync($"[{Constants.CheckUnicode}]", true);
                    sameFiles.Add(file2);
                }
                else
                {
                    await AggregateOutputSink.LightLineAsync($"[{Constants.XUnicode}]", true);
                }
            }

            if (sameFiles.Any())
            {
                sameFiles.Add(file1);
                sameFiles = sameFiles.Select(x => Path.Combine(_option.Dir, x)).ToList();

                foreach (string sameFile in sameFiles)
                {
                    await _exportedDupFileSink.DefaultLineAsync(sameFile);
                }

                await _exportedDupFileSink.NewLineAsync();
                _option.ExportedList?.Add(sameFiles);
            }
        }

        stopwatch.Stop();

        await AggregateOutputSink.DefaultAsync("  Group ");
        await AggregateOutputSink.InfoAsync(currentGroup.ToString());
        await AggregateOutputSink.DefaultAsync(" finished - ");

        if (found)
        {
            await AggregateOutputSink.WarnAsync("[Duplicate files found]. ");
        }
        else
        {
            await AggregateOutputSink.DefaultAsync("[No duplicate files found]. ");
        }

        await AggregateOutputSink.LightLineAsync($"Elapsed: [{stopwatch.Elapsed.Human()}]");
        await AggregateOutputSink.NewLineAsync();
    }

    private async Task<Dictionary<long, List<string>>> ScanAndGroupFilesAsync()
    {
        Dictionary<long, List<string>> groupedItems = new();
        await AggregateOutputSink.LightLineAsync($"{Constants.ArrowUnicode} Scanning directory: {_option.Dir}", true);
        await AggregateOutputSink.NewLineAsync(true);

        foreach (string file in EnumerateFiles(_option.Dir, true))
        {
            if (CancellationToken.IsCancellationRequested)
            {
                break;
            }

            string fullPath = Path.Combine(_option.Dir, file);
            long fileSize = new FileInfo(fullPath).Length;
            if (!groupedItems.ContainsKey(fileSize))
            {
                groupedItems.Add(fileSize, new List<string>());
            }

            groupedItems[fileSize].Add(file);
        }

        groupedItems = groupedItems.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);
        return groupedItems;
    }

    public override async ValueTask DisposeAsync()
    {
        await _exportedDupFileSink.DisposeAsync();
        await base.DisposeAsync();
    }
}