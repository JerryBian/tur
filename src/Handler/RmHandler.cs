using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tur.Model;
using Tur.Option;

namespace Tur.Handler;

public class RmHandler : HandlerBase
{
    private readonly RmOption _option;

    public RmHandler(RmOption option, CancellationToken cancellationToken) : base(option, cancellationToken)
    {
        _option = option;
    }

    private async Task<List<ItemEntry>> GetFromFileListAsync()
    {
        List<ItemEntry> result = new();
        if (!string.IsNullOrEmpty(_option.FromFile))
        {
            await AggregateOutputSink.DefaultLineAsync(
                $"{Constants.ArrowUnicode} Reading files in {_option.FromFile} ...", true);
            if (!File.Exists(_option.FromFile))
            {
                throw new Exception($"File not found for --from-file: {_option.FromFile}");
            }

            foreach (string item in await File.ReadAllLinesAsync(_option.FromFile, CancellationToken))
            {
                if (File.Exists(item))
                {
                    result.Add(new ItemEntry(Path.GetFullPath(item), null));
                    continue;
                }

                if (Directory.Exists(item))
                {
                    result.Add(new ItemEntry(Path.GetFullPath(item), null, true));
                }
            }

            await AggregateOutputSink.DefaultLineAsync(
                "  Finished reading file in --from-file", true);
            await AggregateOutputSink.NewLineAsync(true);
        }

        return result;
    }

    private async Task<List<ItemEntry>> GetFromFilterAsync()
    {
        List<ItemEntry> result = new();

        bool filterFiles = _option.File || (!_option.File && !_option.Dir);
        if (filterFiles)
        {
            await AggregateOutputSink.DefaultLineAsync(
                $"{Constants.ArrowUnicode} Looking for files on deletion ...", true);
            foreach (string file in EnumerateFiles(_option.Destination, true, true))
            {
                result.Add(new ItemEntry(file, Path.GetRelativePath(_option.Destination, file)));
            }

            await AggregateOutputSink.DefaultLineAsync(
                $"{Constants.ArrowUnicode} Finished the files lookup", true);
            await AggregateOutputSink.NewLineAsync(true);
        }

        bool filterDirs = _option.Dir || (!_option.File && !_option.Dir);
        if (filterDirs)
        {
            await AggregateOutputSink.DefaultLineAsync(
                $"{Constants.ArrowUnicode} Looking for directories on deletion ...", true);
            foreach (string dir in EnumerateDirectories(_option.Destination, true, true))
            {
                result.Add(new ItemEntry(dir, Path.GetRelativePath(_option.Destination, dir), true));
            }

            await AggregateOutputSink.DefaultLineAsync(
                $"{Constants.ArrowUnicode} Finished the directories lookup.", true);
            await AggregateOutputSink.NewLineAsync(true);
        }

        return result;
    }

    private async Task DeleteFilesAsync(List<ItemEntry> entries)
    {
        if (!entries.Any())
        {
            return;
        }

        await AggregateOutputSink.DefaultAsync(
            $"{Constants.ArrowUnicode} Following [");
        await AggregateOutputSink.InfoAsync(entries.Count.ToString());
        await AggregateOutputSink.DefaultLineAsync("] files are on the deletion list ...");
        foreach (ItemEntry entry in entries)
        {
            await AggregateOutputSink.LightLineAsync($"  {Constants.SquareUnicode} {entry.GetDisplayPath()}");
        }

        await AggregateOutputSink.NewLineAsync();
        if (!_option.Yes)
        {
            await ConsoleSink.InfoAsync("Are you sure to delete? Y/y for yes, others for no: ");
            if (Console.ReadKey().Key != ConsoleKey.Y)
            {
                await ConsoleSink.NewLineAsync();
                await ConsoleSink.WarnLineAsync("No file will be deleted.");
                await ConsoleSink.NewLineAsync();
                return;
            }
        }

        await AggregateOutputSink.NewLineAsync();
        await ConsoleSink.NewLineAsync();
        await AggregateOutputSink.DefaultLineAsync(
            $"{Constants.ArrowUnicode} Deletion started ...");
        foreach (ItemEntry entry in entries)
        {
            await AggregateOutputSink.LightAsync($"  {Constants.SquareUnicode} {entry.GetDisplayPath()} ");
            if (File.Exists(entry.FullPath))
            {
                File.Delete(entry.FullPath);
            }

            await AggregateOutputSink.LightAsync("[");
            await AggregateOutputSink.ErrorAsync(Constants.XUnicode);
            await AggregateOutputSink.LightLineAsync("]");
        }

        await AggregateOutputSink.DefaultLineAsync(
            "  Finished deletion on files");
        await AggregateOutputSink.NewLineAsync();
    }

    private async Task DeleteDirsAsync(List<ItemEntry> entries)
    {
        if (!entries.Any())
        {
            return;
        }

        await AggregateOutputSink.DefaultAsync(
            $"{Constants.ArrowUnicode} Following [");
        await AggregateOutputSink.InfoAsync(entries.Count.ToString());
        await AggregateOutputSink.DefaultLineAsync("] directories are on the deletion list ...");
        foreach (ItemEntry entry in entries)
        {
            await AggregateOutputSink.LightLineAsync($"  {Constants.SquareUnicode} {entry.GetDisplayPath()}");
        }

        await AggregateOutputSink.NewLineAsync();
        if (!_option.Yes)
        {
            await ConsoleSink.InfoAsync("Are you sure to delete? Y/y for yes, others for no: ");
            if (Console.ReadKey().Key != ConsoleKey.Y)
            {
                await ConsoleSink.NewLineAsync();
                await ConsoleSink.WarnLineAsync("No directory will be deleted.");
                await ConsoleSink.NewLineAsync();
                return;
            }
        }

        await AggregateOutputSink.NewLineAsync();
        await ConsoleSink.NewLineAsync();
        await AggregateOutputSink.DefaultLineAsync(
            $"{Constants.ArrowUnicode} Deletion started ...");

        foreach (ItemEntry entry in entries)
        {
            await AggregateOutputSink.LightAsync($"  {Constants.SquareUnicode} {entry.GetDisplayPath()} ");
            if (Directory.Exists(entry.FullPath))
            {
                Directory.Delete(entry.FullPath, true);
            }

            await AggregateOutputSink.LightAsync("[");
            await AggregateOutputSink.ErrorAsync(Constants.XUnicode);
            await AggregateOutputSink.LightLineAsync("]");
        }

        await AggregateOutputSink.DefaultLineAsync(
            "  Finished deletion on directories");
        await AggregateOutputSink.NewLineAsync();
    }

    public async Task CleanupEmptyDirsAsync()
    {
        if (!_option.EmptyDir)
        {
            return;
        }

        await AggregateOutputSink.DefaultLineAsync($"{Constants.ArrowUnicode} Cleanup empty directories started ...");
        List<string> emptyDirs = EnumerateDirectories(_option.Destination, returnAbsolutePath: true)
            .Where(x => !EnumerateFiles(x).Any()).ToList();
        if (emptyDirs.Any())
        {
            await AggregateOutputSink.DefaultAsync("    Following [", true);
            await AggregateOutputSink.InfoAsync(emptyDirs.Count.ToString());
            await AggregateOutputSink.DefaultLineAsync("] empty directories are on the deletion list ...");
            foreach (string emptyDir in emptyDirs)
            {
                await AggregateOutputSink.LightLineAsync(
                    $"  {Constants.SquareUnicode} {Path.GetRelativePath(_option.Destination, emptyDir)}");
            }

            await AggregateOutputSink.NewLineAsync();

            if (!_option.Yes)
            {
                await ConsoleSink.InfoAsync("Are you sure to delete? Y/y for yes, others for no: ");
                if (Console.ReadKey().Key != ConsoleKey.Y)
                {
                    await ConsoleSink.NewLineAsync();
                    await ConsoleSink.WarnLineAsync("No empty directory will be deleted.");
                    await ConsoleSink.NewLineAsync();
                    return;
                }
            }

            await AggregateOutputSink.NewLineAsync();
            await ConsoleSink.NewLineAsync();
            await AggregateOutputSink.DefaultLineAsync(
                $"{Constants.ArrowUnicode} Deletion started ...");

            foreach (string emptyDir in emptyDirs)
            {
                await AggregateOutputSink.LightAsync(
                    $"  {Constants.SquareUnicode} {Path.GetRelativePath(_option.Destination, emptyDir)} ");
                if (Directory.Exists(emptyDir))
                {
                    Directory.Delete(emptyDir, true);
                }

                await AggregateOutputSink.LightAsync("[");
                await AggregateOutputSink.ErrorAsync(Constants.XUnicode);
                await AggregateOutputSink.LightLineAsync("]");
            }
        }

        // Delete root destination if empty
        if (!EnumerateFiles(_option.Destination).Any())
        {
            Directory.Delete(_option.Destination, true);
        }

        await AggregateOutputSink.DefaultLineAsync("  Finished empty directory cleanup");
        await AggregateOutputSink.NewLineAsync();
    }

    protected override async Task<int> HandleInternalAsync()
    {
        try
        {
            List<ItemEntry> entries = await GetFromFileListAsync();
            entries.AddRange(await GetFromFilterAsync());

            List<ItemEntry> fileEntries = entries.Where(x => !x.IsDir).Distinct().ToList();
            await DeleteFilesAsync(fileEntries);

            List<ItemEntry> dirEntries = entries.Where(x => x.IsDir).Distinct().ToList();
            await DeleteDirsAsync(dirEntries);

            await CleanupEmptyDirsAsync();
        }
        catch (Exception ex)
        {
            await AggregateOutputSink.ErrorLineAsync(ex.Message, ex: ex);
            return 1;
        }

        return 0;
    }
}