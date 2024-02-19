using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tur.Core;
using Tur.Extension;
using Tur.Logging;
using Tur.Option;
using Tur.Util;

namespace Tur.Handler;

public class SyncHandler : HandlerBase
{
    private readonly SyncOption _option;

    public SyncHandler(SyncOption option, CancellationToken cancellationToken) : base(option, cancellationToken)
    {
        _option = option;
    }

    protected override bool PreCheck()
    {
        _option.SrcDir = Path.GetFullPath(_option.SrcDir);
        if (!Directory.Exists(_option.SrcDir))
        {
            _logger.Write($"Source directory not exists: {_option.SrcDir}", TurLogLevel.Error);
            return false;
        }

        _option.DestDir = Path.GetFullPath(_option.DestDir);
        _ = Directory.CreateDirectory(_option.DestDir);

        return true;
    }

    protected override async Task<int> HandleInternalAsync()
    {
        var suffix = _option.DryRun ? "DRY RUN" : "";
        Dictionary<string, TurFileSystem> sourceItems = new();
        Dictionary<string, TurFileSystem> destItems = new();
        Parallel.Invoke(
            () => sourceItems = GetFileSystem(_option.SrcDir, true),
            () => destItems = GetFileSystem(_option.DestDir, false));

        var shouldBreak = false;
        await Parallel.ForEachAsync(sourceItems.TakeWhile(x => !Volatile.Read(ref shouldBreak)), async (srcItem, ct) =>
        {
            if (CancellationToken.IsCancellationRequested)
            {
                Volatile.Write(ref shouldBreak, true);
                return;
            }

            try
            {
                if (destItems.TryGetValue(srcItem.Key, out var dest))
                {
                    if(!dest.IsDirectory)
                    {
                        if (_option.SizeOnly && dest.Length == srcItem.Value.Length)
                        {
                            _logger.Write($"{srcItem.Value.RelativePath}", TurLogLevel.Information, LogConstants.Skip, suffix);
                            return;
                        }

                        if (await FileUtil.IsSameFileAsync(srcItem.Value.FullPath, dest.FullPath, _option.IgnoreError))
                        {
                            _logger.Write($"{srcItem.Value.RelativePath}", TurLogLevel.Information, LogConstants.Skip, suffix);
                            return;
                        }
                    } 
                }
                
                var destFullPath = Path.Combine(_option.DestDir, srcItem.Key);
                if (srcItem.Value.IsDirectory)
                {
                    if (!_option.DryRun)
                    {
                        _ = Directory.CreateDirectory(destFullPath);
                    }

                    return;
                }

                var destItemDir = Path.GetDirectoryName(destFullPath);
                if (!string.IsNullOrEmpty(destItemDir) && !_option.DryRun)
                {
                    _ = Directory.CreateDirectory(destItemDir);
                }

                var sw = Stopwatch.StartNew();
                if (!_option.DryRun)
                {
                    File.Copy(srcItem.Value.FullPath, destFullPath, true);
                    File.SetCreationTime(destFullPath, srcItem.Value.CreationTime.Value);
                    File.SetLastWriteTime(destFullPath, srcItem.Value.LastModifyTime.Value);
                }
                sw.Stop();
                _logger.Write(
                    $"{srcItem.Value.RelativePath}",
                    TurLogLevel.Information,
                    LogConstants.Succeed,
                    $"{HumanUtil.GetSize(srcItem.Value.Length)}, {sw.Elapsed.Human()}{(_option.DryRun ? ", " + suffix : "")}");
            }
            catch(Exception ex)
            {
                if(_option.IgnoreError)
                {
                    _logger.Write($"Unexpected error, skipping this item: {srcItem.Key}.", TurLogLevel.Warning, error: ex);
                    return;
                }

                throw;
            }
        });

        if (CancellationToken.IsCancellationRequested)
        {
            return 0;
        }

        if (_option.Delete)
        {
            var buildOptions = new TurBuildOptions
            {
                IgnoreError = _option.IgnoreError,
                IncludeFiles = true,
                IncludeDirectories = true
            };
            var builder = new TurSystemBuilder(_option.DestDir, buildOptions, CancellationToken);
            foreach (var destItem in builder.Build())
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var srcItem = Path.Combine(_option.SrcDir, destItem.RelativePath);
                if (destItem.IsDirectory)
                {
                    if (!Directory.Exists(srcItem))
                    {
                        if (_option.DryRun)
                        {
                            _logger.Write(destItem.RelativePath, TurLogLevel.Information, LogConstants.Succeed, "D, DEST REMOVED, DRY RUN");
                        }
                        else
                        {
                            Directory.Delete(destItem.FullPath);
                            _logger.Write(destItem.RelativePath, TurLogLevel.Information, LogConstants.Succeed, "D, DEST REMOVED");
                        }
                    }
                }
                else
                {
                    if (!File.Exists(srcItem))
                    {
                        if (_option.DryRun)
                        {
                            _logger.Write(destItem.RelativePath, TurLogLevel.Information, LogConstants.Succeed, "F, DEST REMOVED, DRY RUN");
                        }
                        else
                        {
                            File.Delete(destItem.FullPath);
                            _logger.Write(destItem.RelativePath, TurLogLevel.Information, LogConstants.Succeed, "D, DEST REMOVED");
                        }
                    }
                }
            }
        }

        return 0;
    }

    private Dictionary<string, TurFileSystem> GetFileSystem(string dir, bool includeAttributes)
    {
        var buildOptions = CreateBuildOptions();
        buildOptions.IncludeDirectories = buildOptions.IncludeFiles = true;
        buildOptions.IncludeFileSize = true;
        buildOptions.IncludeAttributes = includeAttributes;


        var builder = new TurSystemBuilder(dir, buildOptions, CancellationToken);
        return builder.Build().ToDictionary(x => x.RelativePath, x => x);
    }
}