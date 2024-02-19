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
    private int _deletedFiles;
    private int _deletedDirectories;

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
                        _logger.Log($"{fullPath}", TurLogLevel.Information, Constants.CheckUnicode, suffix: "F, DRY RUN");
                    }
                    else
                    {
                        File.Delete(fullPath);
                        _ = Interlocked.Increment(ref _deletedFiles);
                        _logger.Log($"{fullPath}", Logging.TurLogLevel.Information, Constants.CheckUnicode, suffix: "F");
                    }
                }
                else if (Directory.Exists(fullPath))
                {
                    if (_option.DryRun)
                    {
                        _logger.Log($"{fullPath}", Logging.TurLogLevel.Information, Constants.CheckUnicode, suffix: "D, DRY RUN");
                    }
                    else
                    {
                        Directory.Delete(fullPath, true);
                        _ = Interlocked.Increment(ref _deletedDirectories);
                        _logger.Log($"{fullPath}", Logging.TurLogLevel.Information, Constants.CheckUnicode, suffix: "D");
                    }
                }
                else
                {
                    _logger.Log($"{fullPath}", Logging.TurLogLevel.Warning, Constants.DashUnicode, suffix: _option.DryRun ? "DRY RUN" : "");
                }
            }
            catch (Exception ex)
            {
                if (_option.IgnoreError)
                {
                    _logger.Log(fullPath, TurLogLevel.Warning, Constants.XUnicode, suffix: "SKIPPED", error: ex);
                }
                else
                {
                    _logger.Log(fullPath, TurLogLevel.Error, Constants.XUnicode, error: ex);
                    break;
                }
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
                            _logger.Log($"{item.RelativePath}", Logging.TurLogLevel.Information, Constants.CheckUnicode, suffix: "D, DRY RUN");
                        }
                        else
                        {
                            Directory.Delete(item.FullPath, true);
                            _ = Interlocked.Increment(ref _deletedDirectories);
                            _logger.Log($"{item.RelativePath}", Logging.TurLogLevel.Information, Constants.CheckUnicode, suffix: "D");
                        }
                    }
                }
                else
                {
                    if (_option.File)
                    {
                        if (_option.DryRun)
                        {
                            _logger.Log($"{item.RelativePath}", Logging.TurLogLevel.Information, Constants.CheckUnicode, suffix: "F, DRY RUN");
                        }
                        else
                        {
                            File.Delete(item.FullPath);
                            _ = Interlocked.Increment(ref _deletedFiles);
                            _logger.Log($"{item.RelativePath}", Logging.TurLogLevel.Information, Constants.CheckUnicode, suffix: "F");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_option.IgnoreError)
                {
                    _logger.Log(item.RelativePath, TurLogLevel.Warning, Constants.XUnicode, suffix: "SKIPPED", error: ex);
                }
                else
                {
                    _logger.Log(item.RelativePath, TurLogLevel.Error, Constants.XUnicode, error: ex);
                    break;
                }
            }
        }

        if (!Directory.EnumerateFileSystemEntries(_option.Destination, "*", SearchOption.TopDirectoryOnly).Any())
        {
            Directory.Delete(_option.Destination);
            _logger.Log(_option.Destination, Logging.TurLogLevel.Information, Constants.CheckUnicode, suffix: "D");
        }
    }

    protected override void PostCheck()
    {
        base.PostCheck();
        _logger.Log($"{_deletedFiles} files and {_deletedDirectories} directories deleted.", TurLogLevel.Information, Constants.ArrowUnicode, false);
    }

    protected override bool PreCheck()
    {
        if (!_option.Dir && !_option.File)
        {
            _option.Dir = _option.File = true;
        }

        if (string.IsNullOrEmpty(_option.Destination) && string.IsNullOrEmpty(_option.FromFile))
        {
            _logger.Log("Either --from-file or destination directory must be provided.", TurLogLevel.Error, Constants.XUnicode, false);
            return false;
        }

        if (!string.IsNullOrEmpty(_option.Destination))
        {
            _option.Destination = Path.GetFullPath(_option.Destination);
            if (!Directory.Exists(_option.Destination))
            {
                _logger.Log($"Target directory not exists: {_option.Destination}.", TurLogLevel.Error, Constants.XUnicode, false);
                return false;
            }
        }

        if (!string.IsNullOrEmpty(_option.FromFile))
        {
            _option.FromFile = Path.GetFullPath(_option.FromFile);
            if (!File.Exists(_option.FromFile))
            {
                _logger.Log($"File list provided via --from-file not exists: {_option.Destination}.", TurLogLevel.Error, Constants.XUnicode, false);
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