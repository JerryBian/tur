using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tur.Extension;
using Tur.Option;
using Tur.Util;

namespace Tur.Handler;

/// <summary>
///     1. Create directory structure in destination
///     2. Copy filtered files to destination
///     3. If -d/--delete, remove any file not in source
///     4. If -d/--delete, remove any directory not in source
/// </summary>
public class SyncHandler : HandlerBase
{
    private readonly SyncOption _option;

    public SyncHandler(SyncOption option, CancellationToken cancellationToken) : base(option, cancellationToken)
    {
        _option = option;
    }

    private async Task CreateDestDirAsync()
    {
        await AggregateOutputSink.LightAsync($"{Constants.ArrowUnicode} ");
        await AggregateOutputSink.DefaultLineAsync("Creating directories in destination ... ");

        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach (string srcDir in EnumerateDirectories(_option.SrcDir))
        {
            if (CancellationToken.IsCancellationRequested)
            {
                continue;
            }

            string destDir = Path.Combine(_option.DestDir, srcDir);
            if (!Directory.Exists(destDir))
            {
                await AggregateOutputSink.LightAsync($"    {Constants.SquareUnicode} [D] {srcDir} ", true);
                if (_option.DryRun)
                {
                    await AggregateOutputSink.LightLineAsync("[Dry]", true);
                }
                else
                {
                    _ = Directory.CreateDirectory(destDir);
                    await AggregateOutputSink.LightLineAsync($"[{Constants.CheckUnicode}]", true);
                }
            }
        }

        stopwatch.Stop();
        await AggregateOutputSink.DefaultLineAsync(
            $"  Finished destination directory creation. Elapsed: [{stopwatch.Elapsed.Human()}]");
        await AggregateOutputSink.NewLineAsync();
    }

    private async Task CopyFilesAsync()
    {
        await AggregateOutputSink.LightAsync($"{Constants.ArrowUnicode} ");
        await AggregateOutputSink.DefaultLineAsync("Copying files from source to destination ... ");

        Stopwatch sw = Stopwatch.StartNew();
        foreach (string relativeSrcFile in EnumerateFiles(_option.SrcDir, true))
        {
            if (CancellationToken.IsCancellationRequested)
            {
                continue;
            }

            string srcFile = Path.Combine(_option.SrcDir, relativeSrcFile);
            string destFile = Path.Combine(_option.DestDir, relativeSrcFile);
            long srcFileLength = new FileInfo(srcFile).Length;
            if (File.Exists(destFile))
            {
                if (_option.SizeOnly && srcFileLength == new FileInfo(destFile).Length)
                {
                    continue;
                }

                if (await IsSameFileAsync(srcFile, destFile))
                {
                    continue;
                }
            }

            await AggregateOutputSink.LightAsync($"    {Constants.SquareUnicode} [F] {relativeSrcFile} ", true);
            if (_option.DryRun)
            {
                await AggregateOutputSink.LightLineAsync("[Dry]", true);
            }
            else
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                await CopyAsync(srcFile, destFile, async (p, s) =>
                {
                    if (!AppUtil.HasMainWindow)
                    {
                        return;
                    }

                    string line = $"[{HumanUtil.GetSize(srcFileLength)}, {p}%, {s.SizeHuman()}/s]";
                    await ConsoleSink.ClearLineAsync(true);
                    await ConsoleSink.LightAsync($"    {Constants.SquareUnicode} [F] {relativeSrcFile} ", true);
                    await ConsoleSink.InfoAsync(line, true);
                }).OkForCancel();
                stopwatch.Stop();

                string line =
                    $"[{HumanUtil.GetSize(srcFileLength)}, {HumanUtil.GetRatesPerSecond(srcFileLength, stopwatch.Elapsed.TotalSeconds)}, {stopwatch.Elapsed.Human()}]";
                await ConsoleSink.ClearLineAsync(true);
                if (AppUtil.HasMainWindow)
                {
                    await ConsoleSink.LightAsync($"    {Constants.SquareUnicode} [F] {relativeSrcFile} ", true);
                }

                await AggregateOutputSink.InfoLineAsync(line, true);
            }
        }

        sw.Stop();
        await AggregateOutputSink.DefaultLineAsync($"  Finished copying files. Elapsed: [{sw.Elapsed.Human()}]");
        await AggregateOutputSink.NewLineAsync();
    }

    private async Task CleanDestFilesAsync()
    {
        if (!_option.Delete)
        {
            return;
        }

        await AggregateOutputSink.LightAsync($"{Constants.ArrowUnicode} ");
        await AggregateOutputSink.DefaultLineAsync("Cleanup files in destination ... ");

        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach (string relativeDestFile in EnumerateFiles(_option.DestDir))
        {
            if (CancellationToken.IsCancellationRequested)
            {
                continue;
            }

            string srcFile = Path.Combine(_option.SrcDir, relativeDestFile);
            string destFile = Path.Combine(_option.DestDir, relativeDestFile);
            if (!File.Exists(srcFile))
            {
                await AggregateOutputSink.LightAsync($"    {Constants.SquareUnicode} [F] {relativeDestFile} ");
                if (_option.DryRun)
                {
                    await AggregateOutputSink.LightLineAsync("[Dry]", true);
                }
                else
                {
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }

                    await AggregateOutputSink.LightAsync("[");
                    await AggregateOutputSink.ErrorAsync(Constants.XUnicode);
                    await AggregateOutputSink.LightLineAsync("]");
                }
            }
        }

        stopwatch.Stop();
        await AggregateOutputSink.DefaultLineAsync(
            $"  Finished destination files cleanup. Elapsed: [{stopwatch.Elapsed.Human()}]");
        await AggregateOutputSink.NewLineAsync();
    }

    private async Task CleanDestDirsAsync()
    {
        if (!_option.Delete)
        {
            return;
        }

        await AggregateOutputSink.LightAsync($"{Constants.ArrowUnicode} ");
        await AggregateOutputSink.DefaultLineAsync("Cleanup directories in destination ... ");

        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach (string relativeDestDir in EnumerateDirectories(_option.DestDir))
        {
            if (CancellationToken.IsCancellationRequested)
            {
                continue;
            }

            string srcDir = Path.Combine(_option.SrcDir, relativeDestDir);
            string destDir = Path.Combine(_option.DestDir, relativeDestDir);
            if (!Directory.Exists(srcDir))
            {
                await AggregateOutputSink.LightAsync($"    {Constants.SquareUnicode} [D] {relativeDestDir} ");
                if (_option.DryRun)
                {
                    await AggregateOutputSink.LightLineAsync("[Dry]", true);
                }
                else
                {
                    if (Directory.Exists(destDir))
                    {
                        Directory.Delete(destDir, true);
                    }

                    await AggregateOutputSink.LightAsync("[");
                    await AggregateOutputSink.ErrorAsync(Constants.XUnicode);
                    await AggregateOutputSink.LightLineAsync("]");
                }
            }
        }

        stopwatch.Stop();
        await AggregateOutputSink.DefaultLineAsync(
            $"  Finished destination directories cleanup. Elapsed: [{stopwatch.Elapsed.Human()}]");
        await AggregateOutputSink.NewLineAsync();
    }

    protected override async Task<int> HandleInternalAsync()
    {
        try
        {
            await AggregateOutputSink.LightLineAsync($"{Constants.ArrowUnicode} Source: {_option.SrcDir}", true);
            await AggregateOutputSink.LightLineAsync($"{Constants.ArrowUnicode} Destination: {_option.DestDir}", true);
            await AggregateOutputSink.NewLineAsync(true);

            if (Path.GetRelativePath(_option.DestDir, _option.SrcDir) == Path.GetFileName(_option.SrcDir))
            {
                await AggregateOutputSink.WarnLineAsync(
                    "No actions required: the destination already exist in source.");
                return 0;
            }

            // TODO: Need to resolve this case
            //if (Path.GetRelativePath(_option.SrcDir, _option.DestDir) != _option.DestDir)
            //{
            //    await AggregateOutputSink.ErrorLineAsync("Not supported: the source contains destination.");
            //    return;
            //}

            await CreateDestDirAsync();
            await CopyFilesAsync();
            await CleanDestFilesAsync();
            await CleanDestDirsAsync();
        }
        catch (Exception ex)
        {
            await AggregateOutputSink.ErrorLineAsync(ex.Message, ex: ex);
            return 1;
        }

        return 0;
    }
}