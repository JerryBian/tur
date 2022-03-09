using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tur.Extension;
using Tur.Option;
using Tur.Sink;

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
            $"tur_{option.CmdName}_{Path.GetRandomFileName().Replace(".", string.Empty)}.txt");
        _exportedDupFileSink = new FileSink(_exportedDupFile, option);
    }

    protected override async Task HandleInternalAsync()
    {
        try
        {
            var groupedItems = await ScanAndGroupFilesAsync();
            await AggregateOutputSink.InfoLineAsync(
                $"Found {groupedItems.Count} groups with exactly same file size.");
            await AggregateOutputSink.NewLineAsync();

            for (var i = 0; i < groupedItems.Count; i++)
            {
                var found = false;
                if (CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var currentGroup = i + 1;
                var group = groupedItems.ElementAt(i);
                await AggregateOutputSink.LightAsync($"{Constants.ArrowUnicode} ");
                await AggregateOutputSink.LightAsync($"[{currentGroup}/{groupedItems.Count}] ", true);
                await AggregateOutputSink.DefaultAsync("Working on group");
                await AggregateOutputSink.InfoAsync(currentGroup.ToString());
                await AggregateOutputSink.DefaultAsync(" [");
                await AggregateOutputSink.InfoAsync(group.Key.Human());
                await AggregateOutputSink.DefaultAsync("], it contains ");
                await AggregateOutputSink.InfoAsync($"{group.Value.Count}");
                await AggregateOutputSink.DefaultLineAsync(" files.");

                var processedFiles = new HashSet<string>();
                foreach (var file1 in group.Value)
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    processedFiles.Add(file1);
                    var targetFiles = group.Value.Where(x => !processedFiles.Contains(x)).ToList();
                    if (!targetFiles.Any() || CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var sameFiles = new List<string>();
                    await AggregateOutputSink.LightLineAsync(
                        $"  {Constants.SquareUnicode} Comparing below {targetFiles.Count} files with {file1}",
                        true);
                    foreach (var file2 in targetFiles)
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
                            processedFiles.Add(file2);
                            await AggregateOutputSink.WarnLineAsync($"[{Constants.CheckUnicode}]", true);
                            sameFiles.Add(file2);
                        }
                        else
                        {
                            await AggregateOutputSink.LightLineAsync($"[{Constants.XUnicode}]", true);
                        }
                    }

                    if (sameFiles.Count > 1)
                    {
                        sameFiles.Add(file1);
                        foreach (var sameFile in sameFiles)
                        {
                            await _exportedDupFileSink.DefaultLineAsync(Path.Combine(_option.Dir, sameFile));
                        }

                        await _exportedDupFileSink.NewLineAsync();
                    }
                }

                await AggregateOutputSink.DefaultAsync("  Group ");
                await AggregateOutputSink.InfoAsync(currentGroup.ToString());
                await AggregateOutputSink.DefaultAsync(" finished. ");

                if (found)
                {
                    await AggregateOutputSink.WarnLineAsync("Duplicate files found.");
                }
                else
                {
                    await AggregateOutputSink.DefaultLineAsync("No duplicate files found.");
                }

                await AggregateOutputSink.NewLineAsync();
            }

            await AggregateOutputSink.LightLineAsync(
                $"Exported duplicate files(if any) can be found at {_exportedDupFile}");
        }
        catch (Exception ex)
        {
            await AggregateOutputSink.ErrorLineAsync(ex.Message, ex: ex);
        }
    }

    private async Task<Dictionary<long, List<string>>> ScanAndGroupFilesAsync()
    {
        var groupedItems = new Dictionary<long, List<string>>();
        await AggregateOutputSink.LightLineAsync($"Scanning directory: {_option.Dir}", true);
        await AggregateOutputSink.NewLineAsync(true);

        foreach (var file in EnumerableFiles(_option.Dir, true))
        {
            if (CancellationToken.IsCancellationRequested)
            {
                break;
            }

            var fullPath = Path.Combine(_option.Dir, file);
            var fileSize = new FileInfo(fullPath).Length;
            if (!groupedItems.ContainsKey(fileSize))
            {
                groupedItems.Add(fileSize, new List<string>());
            }

            groupedItems[fileSize].Add(file);
        }

        groupedItems = groupedItems.Where(x => x.Value.Count() > 1).ToDictionary(x => x.Key, x => x.Value);
        return groupedItems;
    }

    public override async ValueTask DisposeAsync()
    {
        await _exportedDupFileSink.DisposeAsync();
        await base.DisposeAsync();
    }
}