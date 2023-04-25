using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Tur.Extension;
using Tur.Model;
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

    private async Task CopyFilesAsync()
    {
        LogItem logItem = new();
        logItem.AddSegment(LogSegmentLevel.Verbose, $"{Constants.ArrowUnicode} ");
        logItem.AddSegment(LogSegmentLevel.Default, "Copying files from source to destination...");
        logItem.AddLine();
        AddLog(logItem);

        ActionBlock<FileSystemItem> copyBlock = new(async item =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            string relativePath = Path.GetRelativePath(_option.SrcDir, item.FullPath);
            string destFullPath = Path.Combine(_option.DestDir, relativePath);

            if (File.Exists(destFullPath))
            {
                if (item.Size == new FileInfo(destFullPath).Length)
                {
                    if (_option.SizeOnly)
                    {
                        return;
                    }
                }

                if (await FileUtil.IsSameFileAsync(item.FullPath, destFullPath, _option.IgnoreError))
                {
                    return;
                }
            }

            if (_option.DryRun)
            {
                LogItem logItem = new();
                logItem.AddSegment(LogSegmentLevel.Verbose, "[");
                logItem.AddSegment(LogSegmentLevel.Success, Constants.CheckUnicode);
                logItem.AddSegment(LogSegmentLevel.Verbose, "] ");
                logItem.AddSegment(LogSegmentLevel.Default, relativePath);
                AddLog(logItem);
            }
            else
            {
                string dir = Path.GetDirectoryName(destFullPath);
                _ = Directory.CreateDirectory(dir);
                File.Copy(item.FullPath, destFullPath, true);
                File.SetCreationTime(destFullPath, _option.PreserveCreateTime ? item.CreateTime : DateTime.Now);
                File.SetLastWriteTime(destFullPath, _option.PreserveLastModifyTime ? item.LastWriteTime : DateTime.Now);
                File.SetLastAccessTime(destFullPath, DateTime.Now);

                sw.Stop();
                LogItem logItem = new();
                logItem.AddSegment(LogSegmentLevel.Verbose, "[");
                logItem.AddSegment(LogSegmentLevel.Success, $"{HumanUtil.GetSize(item.Size)}, {sw.Elapsed.Human()}, {(item.Size / sw.Elapsed.TotalSeconds).SizeHuman()}");
                logItem.AddSegment(LogSegmentLevel.Verbose, "] ");
                logItem.AddSegment(LogSegmentLevel.Default, relativePath);
                AddLog(logItem);
            }
        });

        foreach (FileSystemItem item in FileUtil.EnumerateFiles(
            _option.SrcDir,
            _option.IgnoreError,
            _option.Includes?.ToList(),
            _option.Excludes?.ToList(),
            _option.CreateBefore,
            _option.CreateAfter,
            _option.LastModifyBefore,
            _option.LastModifyAfter))
        {
            _ = await copyBlock.SendAsync(item);
        }

        copyBlock.Complete();
        await copyBlock.Completion;

        logItem.AddSegment(LogSegmentLevel.Verbose, $"{Constants.ArrowUnicode} ");
        logItem.AddSegment(LogSegmentLevel.Default, "Copied files from source to destination.");
        logItem.AddLine();
        AddLog(logItem);
    }

    private async Task CleanDestFilesAsync()
    {
        if (!_option.Delete)
        {
            return;
        }

        LogItem logItem = new();
        logItem.AddSegment(LogSegmentLevel.Verbose, $"{Constants.ArrowUnicode} ");
        logItem.AddSegment(LogSegmentLevel.Default, "Cleaning extra files in destination...");
        logItem.AddLine();
        AddLog(logItem);

        ActionBlock<FileSystemItem> block = new(item =>
        {
            string relativePath = Path.GetRelativePath(_option.DestDir, item.FullPath);
            string srcFullPath = Path.Combine(_option.SrcDir, relativePath);
            if (!File.Exists(srcFullPath))
            {
                if (!_option.DryRun)
                {
                    if (File.Exists(item.FullPath))
                    {
                        File.Delete(item.FullPath);
                    }
                }

                LogItem logItem = new();
                logItem.AddSegment(LogSegmentLevel.Verbose, "[");
                logItem.AddSegment(LogSegmentLevel.Success, Constants.XUnicode);
                logItem.AddSegment(LogSegmentLevel.Verbose, "] ");
                logItem.AddSegment(LogSegmentLevel.Default, relativePath);
                AddLog(logItem);
            }
        });

        foreach (FileSystemItem item in FileUtil.EnumerateFiles(_option.DestDir, _option.IgnoreError))
        {
            _ = await block.SendAsync(item);
        }

        block.Complete();
        await block.Completion;

        LogItem logItem2 = new();
        logItem2.AddSegment(LogSegmentLevel.Verbose, $"{Constants.ArrowUnicode} ");
        logItem2.AddSegment(LogSegmentLevel.Default, "Cleaned extra files in destination...");
        logItem2.AddLine();
        AddLog(logItem2);
    }

    private async Task CleanDestDirsAsync()
    {
        if (!_option.Delete)
        {
            return;
        }

        LogItem logItem = new();
        logItem.AddSegment(LogSegmentLevel.Verbose, $"{Constants.ArrowUnicode} ");
        logItem.AddSegment(LogSegmentLevel.Default, "Cleaning extra directories in destination...");
        logItem.AddLine();
        AddLog(logItem);

        ActionBlock<FileSystemItem> block = new(item =>
        {
            string relativePath = Path.GetRelativePath(_option.DestDir, item.FullPath);
            string srcFullPath = Path.Combine(_option.SrcDir, relativePath);
            if (!Directory.Exists(srcFullPath))
            {
                if (!_option.DryRun)
                {
                    if (Directory.Exists(item.FullPath))
                    {
                        Directory.Delete(item.FullPath, true);
                    }
                }

                LogItem logItem = new();
                logItem.AddSegment(LogSegmentLevel.Verbose, "[");
                logItem.AddSegment(LogSegmentLevel.Success, Constants.XUnicode);
                logItem.AddSegment(LogSegmentLevel.Verbose, "] ");
                logItem.AddSegment(LogSegmentLevel.Default, relativePath);
                AddLog(logItem);
            }
        });

        foreach (FileSystemItem item in FileUtil.EnumerateDirectories(_option.DestDir, _option.IgnoreError))
        {
            _ = await block.SendAsync(item);
        }

        block.Complete();
        await block.Completion;

        LogItem logItem2 = new();
        logItem2.AddSegment(LogSegmentLevel.Verbose, $"{Constants.ArrowUnicode} ");
        logItem2.AddSegment(LogSegmentLevel.Default, "Cleaned extra directories in destination...");
        logItem2.AddLine();
        AddLog(logItem2);
    }

    protected override async Task<int> HandleInternalAsync()
    {
        try
        {
            LogItem logItem = new();
            logItem.AddSegment(LogSegmentLevel.Verbose, $"{Constants.ArrowUnicode} Src: ");
            logItem.AddSegment(LogSegmentLevel.Default, _option.SrcDir);
            logItem.AddLine();
            logItem.AddSegment(LogSegmentLevel.Verbose, $"{Constants.ArrowUnicode} Dest: ");
            logItem.AddSegment(LogSegmentLevel.Default, _option.DestDir);
            logItem.AddLine();
            AddLog(logItem);

            await CopyFilesAsync();
            await CleanDestFilesAsync();
            await CleanDestDirsAsync();
        }
        catch (Exception ex)
        {
            LogItem logItem = new();
            logItem.AddSegment(LogSegmentLevel.Error, "Unexpected error.", ex);
            AddLog(logItem);

            return 1;
        }

        return 0;
    }
}