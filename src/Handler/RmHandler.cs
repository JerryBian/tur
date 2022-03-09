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
    private readonly string _backupDir;
    private readonly RmOption _option;

    public RmHandler(RmOption option, CancellationToken cancellationToken) : base(option, cancellationToken)
    {
        _option = option;
        _backupDir = Path.Combine(_option.OutputDir, $"tur_{_option.CmdName}_{Path.GetRandomFileName()}");
    }

    protected override async Task HandleInternalAsync()
    {
        try
        {
            var items = await GetItemsAsync();
            if (items == null || !items.Any())
            {
                await AggregateOutputSink.WarnLineAsync("Nothing to delete.");
                return;
            }

            var fileItems = items.Where(x => !x.IsDir).ToList();
            if (fileItems.Any())
            {
                await AggregateOutputSink.LightAsync($"{Constants.ArrowUnicode} ");
                await AggregateOutputSink.DefaultAsync("Below [");
                await AggregateOutputSink.InfoAsync(fileItems.Count.ToString());
                await AggregateOutputSink.DefaultLineAsync("] files are on the deletion list.");
                foreach (var item in fileItems)
                {
                    await AggregateOutputSink.LightLineAsync(
                        $"  {Constants.SquareUnicode} {Path.GetRelativePath(_option.Destination, item.FullPath)}");
                }
            }

            var dirItems = items.Where(x => x.IsDir).ToList();
            if (dirItems.Any())
            {
                await AggregateOutputSink.LightAsync($"{Constants.ArrowUnicode} ");
                await AggregateOutputSink.DefaultAsync("Below [");
                await AggregateOutputSink.InfoAsync(dirItems.Count.ToString());
                await AggregateOutputSink.DefaultLineAsync("] directories are on the deletion list.");
                foreach (var item in dirItems)
                {
                    await AggregateOutputSink.LightLineAsync(
                        $"  {Constants.SquareUnicode} {Path.GetRelativePath(_option.Destination, item.FullPath)}");
                }
            }

            if (!_option.Yes)
            {
                await AggregateOutputSink.InfoAsync("Do you want to delete above items? Y/y or N/n: ");
                var key = Console.ReadKey();
                if (key.Key != ConsoleKey.Y)
                {
                    await AggregateOutputSink.NewLineAsync();
                    await AggregateOutputSink.DefaultLineAsync("User decided not to delete above items, exit ...");
                    return;
                }
            }
            else
            {
                await AggregateOutputSink.WarnLineAsync(
                    "The -y/--yes option has been provided, delete will happen without confirmation.");
            }

            if (fileItems.Any())
            {
                await AggregateOutputSink.LightAsync($"{Constants.ArrowUnicode} ");
                await AggregateOutputSink.DefaultAsync("Deleting [");
                await AggregateOutputSink.InfoAsync(fileItems.Count.ToString());
                await AggregateOutputSink.DefaultAsync("] files.");

                foreach (var item in fileItems)
                {
                    await AggregateOutputSink.LightAsync(
                        $"  {Constants.SquareUnicode} {Path.GetRelativePath(_option.Destination, item.FullPath)} ",
                        true);
                    if (File.Exists(item.FullPath))
                    {
                        if (_option.Backup)
                        {
                            var dir = Path.GetDirectoryName(item.FullPath);
                            if (!string.IsNullOrEmpty(dir))
                            {
                                var backupDir = Path.Combine(_backupDir,
                                    Path.GetRelativePath(_option.Destination, dir));
                                Directory.CreateDirectory(backupDir);
                                var backupFile = Path.GetFileName(item.FullPath);
                                if (!string.IsNullOrEmpty(backupFile))
                                {
                                    File.Copy(item.FullPath, Path.Combine(backupDir, backupFile), true);
                                }
                            }
                        }

                        if (item.FullPath != null)
                        {
                            File.Delete(item.FullPath);
                        }
                    }

                    await AggregateOutputSink.LightAsync("[", true);
                    await AggregateOutputSink.ErrorAsync(Constants.XUnicode, true);
                    await AggregateOutputSink.LightLineAsync("]", true);
                }

                await AggregateOutputSink.DefaultAsync("  Files deleted.");
            }

            if (dirItems.Any())
            {
                await AggregateOutputSink.LightAsync($"{Constants.ArrowUnicode} ");
                await AggregateOutputSink.DefaultAsync("Deleting [");
                await AggregateOutputSink.InfoAsync(dirItems.Count.ToString());
                await AggregateOutputSink.DefaultAsync("] directories.");

                foreach (var item in dirItems)
                {
                    await AggregateOutputSink.LightAsync(
                        $"  {Constants.SquareUnicode} {Path.GetRelativePath(_option.Destination, item.FullPath)} ",
                        true);
                    if (Directory.Exists(item.FullPath))
                    {
                        if (_option.Backup)
                        {
                            var dir = Path.GetDirectoryName(item.FullPath);
                            if (!string.IsNullOrEmpty(dir))
                            {
                                var backupDir = Path.Combine(_backupDir,
                                    Path.GetRelativePath(_option.Destination, dir));
                                Directory.CreateDirectory(backupDir);
                                var backupFile = Path.GetFileName(item.FullPath);
                                if (!string.IsNullOrEmpty(backupFile))
                                {
                                    CopyDir(item.FullPath, Path.Combine(backupDir, backupFile));
                                }
                            }
                        }

                        if (item.FullPath != null)
                        {
                            Directory.Delete(item.FullPath, true);
                        }
                    }

                    await AggregateOutputSink.LightAsync("[", true);
                    await AggregateOutputSink.ErrorAsync(Constants.XUnicode, true);
                    await AggregateOutputSink.LightLineAsync("]", true);
                }

                await AggregateOutputSink.DefaultAsync("  Directories deleted.");
            }

            if (_option.EmptyDir)
            {
                var emptyDirs = EnumerableDirectories(_backupDir).Where(x =>
                    !Directory.EnumerateFiles(Path.Combine(_option.Destination, x)).Any()).ToList();

                if (emptyDirs.Any())
                {
                    await AggregateOutputSink.LightAsync($"{Constants.ArrowUnicode} ");
                    await AggregateOutputSink.DefaultAsync("Deleting [");
                    await AggregateOutputSink.InfoAsync(emptyDirs.Count.ToString());
                    await AggregateOutputSink.DefaultAsync("] empty directories.");

                    foreach (var item in emptyDirs)
                    {
                        await AggregateOutputSink.LightAsync($"  {Constants.SquareUnicode} {item} ");
                        var fullPath = Path.Combine(_option.Destination, item);
                        if (Directory.Exists(fullPath))
                        {
                            if (_option.Backup)
                            {
                                var dir = Path.GetDirectoryName(fullPath);
                                if (!string.IsNullOrEmpty(dir))
                                {
                                    var backupDir = Path.Combine(_backupDir,
                                        Path.GetRelativePath(_option.Destination, dir));
                                    Directory.CreateDirectory(backupDir);
                                }
                            }

                            Directory.Delete(fullPath, true);
                        }

                        await AggregateOutputSink.LightAsync("[", true);
                        await AggregateOutputSink.ErrorAsync(Constants.XUnicode, true);
                        await AggregateOutputSink.LightLineAsync("]", true);
                    }

                    await AggregateOutputSink.DefaultAsync("  Empty directories deleted.");
                }
            }

            if (_option.Backup)
            {
                await AggregateOutputSink.LightLineAsync($"Backup folder at: {_backupDir}");
            }
        }
        catch (Exception ex)
        {
            await AggregateOutputSink.ErrorLineAsync(ex.Message, ex: ex);
        }
    }

    private async Task<List<ItemEntry>> GetItemsAsync()
    {
        var items = new HashSet<ItemEntry>();
        if (!string.IsNullOrEmpty(_option.FromFile))
        {
            if (!File.Exists(_option.FromFile))
            {
                await AggregateOutputSink.ErrorLineAsync(
                    $"The file specified by --from-file doesn't exist: {_option.FromFile}");
                return null;
            }

            foreach (var file in await File.ReadAllLinesAsync(_option.FromFile, CancellationToken))
            {
                var path = Path.GetFullPath(file);
                if (File.Exists(path))
                {
                    var entry = new ItemEntry(path);
                    if (!items.Contains(entry))
                    {
                        items.Add(entry);
                    }

                    continue;
                }

                if (Directory.Exists(path))
                {
                    var entry = new ItemEntry(path);
                    if (!items.Contains(entry))
                    {
                        items.Add(entry);
                    }
                }
            }
        }

        var searchFiles = _option.File || !_option.File && !_option.Dir;
        var searchDirs = _option.Dir || !_option.File && !_option.Dir;

        if (searchDirs)
        {
            foreach (var dir in EnumerableDirectories(_option.Destination, true))
            {
                foreach (var file in Directory.GetFiles(Path.Combine(_option.Destination, dir), "*",
                             SearchOption.AllDirectories))
                {
                    var entry = new ItemEntry(file);
                    if (!items.Contains(entry))
                    {
                        items.Add(entry);
                    }
                }

                foreach (var subDir in Directory.GetDirectories(Path.Combine(_option.Destination, dir), "*",
                             SearchOption.AllDirectories))
                {
                    var entry = new ItemEntry(subDir, true);
                    if (!items.Contains(entry))
                    {
                        items.Add(entry);
                    }
                }

                var dirEntry = new ItemEntry(dir, true);
                if (!items.Contains(dirEntry))
                {
                    items.Add(dirEntry);
                }
            }
        }

        if (searchFiles)
        {
            foreach (var file in EnumerableFiles(_option.Destination, true))
            {
                var entry = new ItemEntry(file);
                if (!items.Contains(entry))
                {
                    items.Add(entry);
                }
            }
        }

        return items.ToList();
    }
}