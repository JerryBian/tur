using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tur.Extension;
using Tur.Option;

namespace Tur.Handler;

public class SyncHandler : HandlerBase
{
    private readonly SyncOption _option;

    public SyncHandler(SyncOption option, CancellationToken cancellationToken) : base(option, cancellationToken)
    {
        _option = option;
    }

    protected override async Task HandleInternalAsync()
    {
        try
        {
            await AggregateOutputSink.LightLineAsync($"Source: {_option.SrcDir}", true);
            await AggregateOutputSink.LightLineAsync($"Destination: {_option.DestDir}", true);
            await AggregateOutputSink.NewLineAsync(true);

            foreach (var file in EnumerableFiles(_option.SrcDir))
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var srcFile = Path.Combine(_option.SrcDir, file);
                var destFile = Path.Combine(_option.DestDir, file);
                var srcFileLength = new FileInfo(srcFile).Length;

                await AggregateOutputSink.LightAsync($"{Constants.ArrowUnicode} ");
                await AggregateOutputSink.DefaultAsync("Working on file ");
                await AggregateOutputSink.InfoAsync(file);
                await AggregateOutputSink.DefaultAsync(" [");
                await AggregateOutputSink.InfoAsync(srcFileLength.Human());
                await AggregateOutputSink.DefaultLineAsync("]");

                if (File.Exists(destFile))
                {
                    await AggregateOutputSink.LightLineAsync($"  {Constants.SquareUnicode} Destination file exists",
                        true);
                    if (await IsSameFileAsync(srcFile, destFile))
                    {
                        await AggregateOutputSink.LightLineAsync(
                            $" {Constants.SquareUnicode} Two sides have exactly same file", true);
                        await AggregateOutputSink.DefaultLineAsync(" No need to sync");
                        await AggregateOutputSink.NewLineAsync();
                        continue;
                    }
                }

                if (CancellationToken.IsCancellationRequested)
                {
                    continue;
                }

                if (_option.DryRun)
                {
                    await AggregateOutputSink.WarnLineAsync(" Dry run: copied to destination.");
                }
                else
                {
                    await AggregateOutputSink.DefaultLineAsync("  Sync started...");
                    var stopwatch = Stopwatch.StartNew();
                    await CopyAsync(srcFile, destFile, async (p, s) =>
                    {
                        var line = $" {Constants.SquareUnicode} {p}% ({s.Human()}/s)";
                        await ConsoleSink.ClearLineAsync(true);
                        await ConsoleSink.InfoAsync(line, true);
                    }).OkForCancel();
                    stopwatch.Stop();

                    var line =
                        $"  {Constants.SquareUnicode} 100% ({(srcFileLength / stopwatch.Elapsed.TotalSeconds).Human()}/s)";
                    await ConsoleSink.ClearLineAsync(true);
                    await ConsoleSink.InfoAsync(line, true);

                    await AggregateOutputSink.NewLineAsync(true);
                    await AggregateOutputSink.DefaultLineAsync(
                        $"  Sync completed, elapsed {stopwatch.Elapsed.Human()}");
                }

                await AggregateOutputSink.NewLineAsync();
            }

            if (_option.Delete && !CancellationToken.IsCancellationRequested)
            {
                await AggregateOutputSink.LightAsync($"{Constants.ArrowUnicode} ");
                await AggregateOutputSink.WarnLineAsync("Cleanup destination started...");

                var i = 0;
                var j = 0;

                foreach (var dir in EnumerableDirectories(_option.DestDir))
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var destDirFullPath = Path.Combine(_option.DestDir, dir);
                    var srcDirFullPath = Path.Combine(_option.SrcDir, dir);
                    if (Directory.Exists(srcDirFullPath))
                    {
                        break;
                    }

                    i++;
                    var allFiles = Directory.GetFiles(destDirFullPath, "*", SearchOption.AllDirectories);
                    var allDirs = Directory.GetDirectories(destDirFullPath, "*", SearchOption.AllDirectories);

                    if (!_option.DryRun)
                    {
                        i += allDirs.Length;
                        j += allFiles.Length;
                    }

                    await AggregateOutputSink.LightAsync($"  {Constants.SquareUnicode} [");
                    await AggregateOutputSink.DefaultAsync("D");
                    await AggregateOutputSink.LightAsync($"] {dir}");

                    if (_option.DryRun)
                    {
                        await AggregateOutputSink.LightAsync("[");
                        await AggregateOutputSink.ErrorAsync("Dry Run");
                        await AggregateOutputSink.LightLineAsync("]");
                    }
                    else
                    {
                        Directory.Delete(destDirFullPath, true);
                        await AggregateOutputSink.LightAsync("[");
                        await AggregateOutputSink.ErrorAsync(Constants.XUnicode);
                        await AggregateOutputSink.LightLineAsync("]");

                        foreach (var file in allFiles)
                        {
                            await AggregateOutputSink.LightAsync($"  {Constants.SquareUnicode} [");
                            await AggregateOutputSink.DefaultAsync("F");
                            await AggregateOutputSink.LightAsync($"] {Path.GetRelativePath(_option.DestDir, file)}");
                            await AggregateOutputSink.LightAsync("[");
                            if (_option.DryRun)
                            {
                                await AggregateOutputSink.ErrorAsync("Dry Run");
                            }
                            else
                            {
                                await AggregateOutputSink.ErrorAsync(Constants.XUnicode);
                            }

                            await AggregateOutputSink.LightLineAsync("]");
                        }

                        foreach (var item in allFiles)
                        {
                            await AggregateOutputSink.LightAsync($"  {Constants.SquareUnicode} [");
                            await AggregateOutputSink.DefaultAsync("D");
                            await AggregateOutputSink.LightAsync($"] {Path.GetRelativePath(_option.DestDir, item)}");
                            await AggregateOutputSink.LightAsync("[");
                            if (_option.DryRun)
                            {
                                await AggregateOutputSink.ErrorAsync("Dry Run");
                            }
                            else
                            {
                                await AggregateOutputSink.ErrorAsync(Constants.XUnicode);
                            }

                            await AggregateOutputSink.LightLineAsync("]");
                        }
                    }
                }

                foreach (var file in EnumerableFiles(_option.DestDir))
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var srcFileFullPath = Path.Combine(_option.SrcDir, file);
                    if (File.Exists(srcFileFullPath))
                    {
                        continue;
                    }

                    j++;
                    await AggregateOutputSink.LightAsync($"  {Constants.SquareUnicode} [");
                    await AggregateOutputSink.DefaultAsync("F");
                    await AggregateOutputSink.LightAsync($"] {file}");

                    if (_option.DryRun)
                    {
                        await AggregateOutputSink.LightAsync("[");
                        await AggregateOutputSink.ErrorAsync("Dry Run");
                    }
                    else
                    {
                        File.Delete(Path.Combine(_option.DestDir, file));
                        await AggregateOutputSink.LightAsync("[");
                        await AggregateOutputSink.ErrorAsync(Constants.XUnicode);
                    }

                    await AggregateOutputSink.LightLineAsync("]");
                }

                await AggregateOutputSink.WarnLineAsync(
                    $"  Cleanup destination ({i} directories, {j} files) finished.");
                await AggregateOutputSink.NewLineAsync();
            }
        }
        catch (Exception ex)
        {
            await AggregateOutputSink.ErrorLineAsync(ex.Message, ex: ex);
        }
    }
}