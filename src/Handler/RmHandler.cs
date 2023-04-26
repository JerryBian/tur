using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Tur.Model;
using Tur.Option;
using Tur.Util;

namespace Tur.Handler;

public class RmHandler : HandlerBase
{
    private readonly RmOption _option;

    public RmHandler(RmOption option, CancellationToken cancellationToken) : base(option, cancellationToken)
    {
        _option = option;
    }

    private async Task DeleteFromFileListAsync()
    {
        if (string.IsNullOrEmpty(_option.FromFile))
        {
            return;
        }

        if (!File.Exists(_option.FromFile))
        {
            if (_option.IgnoreError)
            {
                return;
            }

            throw new Exception($"The file specifed by --from-file({_option.FromFile}) not exists.");
        }

        LogItem logItem1 = new();
        logItem1.AddSegment(LogSegmentLevel.Verbose, Constants.ArrowUnicode);
        logItem1.AddSegment(LogSegmentLevel.Default, $" Deleting files in {_option.FromFile} ...");
        AddLog(logItem1);

        ActionBlock<FileSystemItem> block = CreateDeleteBlock();

        await foreach (string item in File.ReadLinesAsync(_option.FromFile, CancellationToken))
        {
            FileSystemItem result = default;

            if (File.Exists(item))
            {
                result = new FileSystemItem(false)
                {
                    FullPath = Path.GetFullPath(item)
                };
            }
            else if (Directory.Exists(item))
            {
                result = new FileSystemItem(true)
                {
                    FullPath = Path.GetFullPath(item)
                };
            }

            if (result == default)
            {
                if (!_option.IgnoreError)
                {
                    throw new Exception($"Invalid path in file specified by --from-file({_option.FromFile}).");
                }
            }
            else
            {
                _ = await block.SendAsync(result);
            }
        }

        block.Complete();
        await block.Completion;

        LogItem logItem2 = new();
        logItem2.AddSegment(LogSegmentLevel.Success, Constants.CheckUnicode);
        logItem2.AddSegment(LogSegmentLevel.Default, $" Deleted files in {_option.FromFile}.");
        logItem2.AddLine();
        AddLog(logItem1);
    }

    private async Task DeleteFromFilterAsync()
    {
        if (string.IsNullOrEmpty(_option.Destination))
        {
            return;
        }

        if (!Directory.Exists(_option.Destination))
        {
            if (_option.IgnoreError)
            {
                return;
            }

            throw new Exception($"The directory specifed({_option.Destination}) not exists.");
        }


        if (_option.File || (!_option.File && !_option.Dir))
        {
            LogItem logItem1 = new();
            logItem1.AddSegment(LogSegmentLevel.Verbose, Constants.ArrowUnicode);
            logItem1.AddSegment(LogSegmentLevel.Default, $" Deleting files in destination...");
            AddLog(logItem1);

            ActionBlock<FileSystemItem> block = CreateDeleteBlock();
            foreach (FileSystemItem file in FileUtil.EnumerateFiles(
                _option.Destination,
                _option.IgnoreError,
                _option.Includes?.ToList(),
                _option.Excludes?.ToList(),
                _option.CreateBefore,
                _option.CreateAfter,
                _option.LastModifyBefore,
                _option.LastModifyAfter))
            {
                _ = await block.SendAsync(file);
            }

            block.Complete();
            await block.Completion;

            LogItem logItem2 = new();
            logItem2.AddSegment(LogSegmentLevel.Success, Constants.CheckUnicode);
            logItem2.AddSegment(LogSegmentLevel.Default, $" Deleted files in destination.");
            logItem2.AddLine();
            AddLog(logItem2);
        }

        if (_option.Dir || (!_option.File && !_option.Dir))
        {
            LogItem logItem1 = new();
            logItem1.AddSegment(LogSegmentLevel.Verbose, Constants.ArrowUnicode);
            logItem1.AddSegment(LogSegmentLevel.Default, $" Deleting directories in destination...");
            AddLog(logItem1);

            ActionBlock<FileSystemItem> block = CreateDeleteBlock();
            foreach (FileSystemItem dir in FileUtil.EnumerateDirectories(
                _option.Destination,
                _option.IgnoreError,
                _option.Includes?.ToList(),
                _option.Excludes?.ToList()))
            {
                _ = await block.SendAsync(dir);
            }

            block.Complete();
            await block.Completion;

            LogItem logItem2 = new();
            logItem2.AddSegment(LogSegmentLevel.Success, Constants.CheckUnicode);
            logItem2.AddSegment(LogSegmentLevel.Default, $" Deleted directories in destination.");
            logItem2.AddLine();
            AddLog(logItem2);
        }
    }

    private async Task DeleteFromEmptyDirAsync()
    {
        if (!_option.EmptyDir || string.IsNullOrEmpty(_option.Destination))
        {
            return;
        }

        if (!Directory.Exists(_option.Destination))
        {
            if (_option.IgnoreError)
            {
                return;
            }

            throw new Exception($"The directory specifed({_option.FromFile}) not exists.");
        }

        LogItem logItem1 = new();
        logItem1.AddSegment(LogSegmentLevel.Verbose, Constants.ArrowUnicode);
        logItem1.AddSegment(LogSegmentLevel.Default, $" Deleting empty directory in destination...");
        AddLog(logItem1);

        ActionBlock<FileSystemItem> block = CreateDeleteBlock();
        foreach (FileSystemItem dir in FileUtil.EnumerateDirectories(_option.Destination, _option.IgnoreError))
        {
            if (!FileUtil.EnumerateFiles(dir.FullPath, _option.IgnoreError).Any())
            {
                _ = await block.SendAsync(dir);
            }
        }

        if (!FileUtil.EnumerateFiles(_option.Destination, _option.IgnoreError).Any())
        {
            _ = await block.SendAsync(new FileSystemItem(true) { FullPath = _option.Destination });
        }

        block.Complete();
        await block.Completion;

        LogItem logItem2 = new();
        logItem2.AddSegment(LogSegmentLevel.Verbose, Constants.ArrowUnicode);
        logItem2.AddSegment(LogSegmentLevel.Default, $" Deleted empty directory in destination.");
        AddLog(logItem2);
    }

    protected override async Task<int> HandleInternalAsync()
    {
        try
        {
            await DeleteFromFileListAsync();
            await DeleteFromFilterAsync();
            await DeleteFromEmptyDirAsync();
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

    private ActionBlock<FileSystemItem> CreateDeleteBlock()
    {
        ActionBlock<FileSystemItem> block = new(item =>
        {
            bool noOp = true;
            Exception error = null;
            try
            {
                if (item.IsDir)
                {
                    if (Directory.Exists(item.FullPath))
                    {
                        noOp = false;
                        Directory.Delete(item.FullPath, true);
                    }
                }
                else
                {
                    if (File.Exists(item.FullPath))
                    {
                        noOp = false;
                        File.Delete(item.FullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_option.IgnoreError)
                {
                    throw;
                }

                error = ex;
            }

            if (!noOp)
            {
                LogItem logItem = new();
                logItem.AddSegment(LogSegmentLevel.Verbose, "  [");
                if (error != null)
                {
                    logItem.AddSegment(LogSegmentLevel.Error, Constants.XUnicode);
                }
                else
                {
                    logItem.IsStdError = true;
                    logItem.AddSegment(LogSegmentLevel.Success, Constants.CheckUnicode);
                }
                logItem.AddSegment(LogSegmentLevel.Verbose, "]");
                logItem.AddSegment(LogSegmentLevel.Default, $" {item.FullPath}", error);
                AddLog(logItem);
            }
        }, new ExecutionDataflowBlockOptions { BoundedCapacity = Constants.BoundedCapacity, MaxDegreeOfParallelism = Environment.ProcessorCount });

        return block;
    }
}