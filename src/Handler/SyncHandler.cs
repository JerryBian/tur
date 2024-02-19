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

    private int _copiedFiles;
    private int _createdDirectories;
    private int _deletedFiles;
    private int _deletedDirectories;

    public SyncHandler(SyncOption option, CancellationToken cancellationToken) : base(option, cancellationToken)
    {
        _option = option;
    }

    protected override bool PreCheck()
    {
        _option.SrcDir = Path.GetFullPath(_option.SrcDir);
        if (!Directory.Exists(_option.SrcDir))
        {
            _logger.Log($"Source directory not exists: {_option.SrcDir}", TurLogLevel.Error, Constants.XUnicode, false);
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
                    if (!dest.IsDirectory && dest.Length == srcItem.Value.Length)
                    {
                        if (_option.SizeOnly)
                        {
                            _logger.Log($"{srcItem.Value.RelativePath}", TurLogLevel.Trace, Constants.DashUnicode, suffix: suffix);
                            return;
                        }

                        if (await FileUtil.IsSameFileAsync(srcItem.Value.FullPath, dest.FullPath, _option.IgnoreError))
                        {
                            _logger.Log($"{srcItem.Value.RelativePath}", TurLogLevel.Trace, Constants.DashUnicode, suffix: suffix);
                            return;
                        }
                    }
                }

                var destFullPath = Path.Combine(_option.DestDir, srcItem.Key);
                if (srcItem.Value.IsDirectory)
                {
                    if (!_option.DryRun && !Directory.Exists(destFullPath))
                    {
                        _ = Directory.CreateDirectory(destFullPath);
                        _ = Interlocked.Increment(ref _createdDirectories);
                    }

                    return;
                }

                var destItemDir = Path.GetDirectoryName(destFullPath);
                if (!string.IsNullOrEmpty(destItemDir) && !_option.DryRun && !Directory.Exists(destItemDir))
                {
                    _ = Directory.CreateDirectory(destItemDir);
                    _ = Interlocked.Increment(ref _createdDirectories);
                }

                var sw = Stopwatch.StartNew();
                if (!_option.DryRun)
                {
                    File.Copy(srcItem.Value.FullPath, destFullPath, true);
                    File.SetCreationTime(destFullPath, srcItem.Value.CreationTime.Value);
                    File.SetLastWriteTime(destFullPath, srcItem.Value.LastModifyTime.Value);
                    _ = Interlocked.Increment(ref _copiedFiles);
                }
                sw.Stop();
                _logger.Log(
                    $"{srcItem.Value.RelativePath}",
                    TurLogLevel.Information,
                    Constants.CheckUnicode,
                    suffix: $"{HumanUtil.GetSize(srcItem.Value.Length)}, {sw.Elapsed.Human()}{(_option.DryRun ? ", " + suffix : "")}");
            }
            catch (Exception ex)
            {
                if (_option.IgnoreError)
                {
                    _logger.Log(srcItem.Key, TurLogLevel.Warning, Constants.XUnicode, suffix: "SKIPPED", error: ex);
                }
                else
                {
                    _logger.Log(srcItem.Key, TurLogLevel.Error, Constants.XUnicode, error: ex);
                    Volatile.Write(ref shouldBreak, true);
                }
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

                try
                {
                    var srcItem = Path.Combine(_option.SrcDir, destItem.RelativePath);
                    if (destItem.IsDirectory)
                    {
                        if (!Directory.Exists(srcItem))
                        {
                            if (_option.DryRun)
                            {
                                _logger.Log(destItem.RelativePath, TurLogLevel.Information, Constants.CheckUnicode, suffix: "D, DEST REMOVED, DRY RUN");
                            }
                            else
                            {
                                Directory.Delete(destItem.FullPath);
                                _ = Interlocked.Increment(ref _deletedDirectories);
                                _logger.Log(destItem.RelativePath, TurLogLevel.Information, Constants.CheckUnicode, suffix: "D, DEST REMOVED");
                            }
                        }
                    }
                    else
                    {
                        if (!File.Exists(srcItem))
                        {
                            if (_option.DryRun)
                            {
                                _logger.Log(destItem.RelativePath, TurLogLevel.Information, Constants.CheckUnicode, suffix: "F, DEST REMOVED, DRY RUN");
                            }
                            else
                            {
                                File.Delete(destItem.FullPath);
                                _ = Interlocked.Increment(ref _deletedFiles);
                                _logger.Log(destItem.RelativePath, TurLogLevel.Information, Constants.CheckUnicode, suffix: "D, DEST REMOVED");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_option.IgnoreError)
                    {
                        _logger.Log(destItem.FullPath, TurLogLevel.Warning, Constants.XUnicode, suffix: "SKIPPED", error: ex);
                    }
                    else
                    {
                        _logger.Log(destItem.FullPath, TurLogLevel.Error, Constants.XUnicode, error: ex);
                        return 1;
                    }
                }
            }
        }

        return 0;
    }

    protected override void PostCheck()
    {
        base.PostCheck();
        _logger.Log($"{_copiedFiles} files copied, {_createdDirectories} directories created. {_deletedFiles} files and {_deletedDirectories} directories deleted.", TurLogLevel.Information, Constants.ArrowUnicode, false);
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