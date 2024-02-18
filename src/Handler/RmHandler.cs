using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tur.Core;
using Tur.Logging;
using Tur.Option;

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
        var items = await File.ReadAllLinesAsync(_option.FromFile, CancellationToken);
        foreach (var fullPath in items.Where(x => !string.IsNullOrWhiteSpace(x)).Select(Path.GetFullPath).OrderByDescending(x => x))
        {
            if (CancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                if (File.Exists(fullPath))
                {
                    if (_option.DryRun)
                    {
                        _logger.Write($"{fullPath}", Logging.TurLogLevel.Information, LogConstants.Succeed, "F, DRY RUN");
                    }
                    else
                    {
                        File.Delete(fullPath);
                        _logger.Write($"{fullPath}", Logging.TurLogLevel.Information, LogConstants.Succeed, "F");
                    }
                }
                else if (Directory.Exists(fullPath))
                {
                    if (_option.DryRun)
                    {
                        _logger.Write($"{fullPath}", Logging.TurLogLevel.Information, LogConstants.Succeed, "D, DRY RUN");
                    }
                    else
                    {
                        Directory.Delete(fullPath, true);
                        _logger.Write($"{fullPath}", Logging.TurLogLevel.Information, LogConstants.Succeed, "D");
                    }
                }
                else
                {
                    _logger.Write($"{fullPath}", Logging.TurLogLevel.Warning, LogConstants.Skip, _option.DryRun ? "DRY RUN" : "");
                }
            }
            catch (Exception ex)
            {
                if (!_option.IgnoreError)
                {
                    throw;
                }

                _logger.Write($"This item is skipped due to error: {fullPath}", TurLogLevel.Warning, error: ex);
            }
        }
    }

    private void DeleteFromFilter()
    {
        var buildOption = CreateBuildOptions();
        buildOption.IncludeFiles = true;
        buildOption.IncludeDirectories = _option.Dir || _option.EmptyDir;
        var builder = new TurSystemBuilder(_option.Destination, buildOption, CancellationToken);
        foreach (var item in builder.Build())
        {
            if (CancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                if (item.IsDirectory)
                {
                    if (_option.Dir || (_option.EmptyDir && !Directory.EnumerateFileSystemEntries(item.FullPath, "*", SearchOption.AllDirectories).Any()))
                    {
                        if (_option.DryRun)
                        {
                            _logger.Write($"{item.RelativePath}", Logging.TurLogLevel.Information, LogConstants.Succeed, "D, DRY RUN");
                        }
                        else
                        {
                            Directory.Delete(item.FullPath, true);
                            _logger.Write($"{item.RelativePath}", Logging.TurLogLevel.Information, LogConstants.Succeed, "D");
                        }
                    }
                }
                else
                {
                    if (_option.File)
                    {
                        if (_option.DryRun)
                        {
                            _logger.Write($"{item.RelativePath}", Logging.TurLogLevel.Information, LogConstants.Succeed, "F, DRY RUN");
                        }
                        else
                        {
                            File.Delete(item.FullPath);
                            _logger.Write($"{item.RelativePath}", Logging.TurLogLevel.Information, LogConstants.Succeed, "F");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_option.IgnoreError)
                {
                    throw;
                }

                _logger.Write($"This item is skipped due to error: {item.RelativePath}", TurLogLevel.Warning, error: ex);
            }
        }

        if (!Directory.EnumerateFileSystemEntries(_option.Destination, "*", SearchOption.TopDirectoryOnly).Any())
        {
            Directory.Delete(_option.Destination);
        }
    }

    protected override bool PreCheck()
    {
        if (!_option.Dir && !_option.File)
        {
            _option.Dir = _option.File = true;
        }

        if (string.IsNullOrEmpty(_option.Destination) && string.IsNullOrEmpty(_option.FromFile))
        {
            _logger.Write("Either --from-file or destination directory must be provided.", TurLogLevel.Error);
            return false;
        }

        if (!string.IsNullOrEmpty(_option.Destination))
        {
            _option.Destination = Path.GetFullPath(_option.Destination);
            if (!Directory.Exists(_option.Destination))
            {
                _logger.Write($"Target directory not exists: {_option.Destination}.", TurLogLevel.Error);
                return false;
            }
        }

        if (!string.IsNullOrEmpty(_option.FromFile))
        {
            _option.FromFile = Path.GetFullPath(_option.FromFile);
            if (!File.Exists(_option.FromFile))
            {
                _logger.Write($"File list provided via --from-file not exists: {_option.Destination}.", TurLogLevel.Error);
                return false;
            }
        }

        return base.PreCheck();
    }

    protected override async Task<int> HandleInternalAsync()
    {
        if (!string.IsNullOrEmpty(_option.FromFile))
        {
            await DeleteFromFileListAsync();
        }

        DeleteFromFilter();

        return 0;
    }
}