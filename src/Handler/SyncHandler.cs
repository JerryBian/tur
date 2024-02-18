using System.Diagnostics;
using System.IO;
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
        var buildOptions = CreateBuildOptions();
        buildOptions.IncludeDirectories = buildOptions.IncludeFiles = true;
        buildOptions.IncludeFileSize = true;
        buildOptions.IncludeAttributes = true;
        var suffix = _option.DryRun ? "DRY RUN" : "";

        var builder = new TurSystemBuilder(_option.SrcDir, buildOptions, CancellationToken);
        foreach (var srcItem in builder.Build())
        {
            if (CancellationToken.IsCancellationRequested)
            {
                break;
            }

            var destItem = Path.Combine(_option.DestDir, srcItem.RelativePath);
            if (!srcItem.IsDirectory)
            {
                if (File.Exists(destItem))
                {
                    if (_option.SizeOnly && destItem.Length == new FileInfo(destItem).Length)
                    {
                        _logger.Write($"{srcItem.RelativePath}", Logging.TurLogLevel.Information, LogConstants.Skip, suffix);
                        continue;
                    }

                    if (await FileUtil.IsSameFileAsync(srcItem.FullPath, destItem, _option.IgnoreError))
                    {
                        _logger.Write($"{srcItem.RelativePath}", Logging.TurLogLevel.Information, LogConstants.Skip, suffix);
                        continue;
                    }
                }

                var destItemDir = Path.GetDirectoryName(destItem);
                if (!string.IsNullOrEmpty(destItemDir) && !_option.DryRun)
                {
                    _ = Directory.CreateDirectory(destItemDir);
                }

                var sw = Stopwatch.StartNew();
                if (!_option.DryRun)
                {
                    File.Copy(srcItem.FullPath, destItem, true);
                    File.SetCreationTime(destItem, srcItem.CreationTime.Value);
                    File.SetLastWriteTime(destItem, srcItem.LastModifyTime.Value);
                }
                sw.Stop();
                _logger.Write($"{srcItem.RelativePath}", Logging.TurLogLevel.Information, LogConstants.Succeed, $"{HumanUtil.GetSize(srcItem.Length)}, {sw.Elapsed.Human()}{(_option.DryRun ? ", " + suffix : "")}");
            }
            else
            {
                if (!_option.DryRun)
                {
                    _ = Directory.CreateDirectory(destItem);
                }
            }
        }

        if (CancellationToken.IsCancellationRequested)
        {
            return 0;
        }

        if (_option.Delete)
        {
            buildOptions = new TurBuildOptions
            {
                IgnoreError = _option.IgnoreError,
                IncludeFiles = true,
                IncludeDirectories = true
            };
            builder = new TurSystemBuilder(_option.DestDir, buildOptions, CancellationToken);
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
}