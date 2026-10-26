using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tur.Core;
using Tur.Logging;
using Tur.Option;
using Tur.Util;

namespace Tur.Handler;

public class RmHandler : HandlerBase
{
    private readonly RmOption _option;
    private int _deletedFiles;
    private int _deletedDirectories;
    private bool _hasError;

    public RmHandler(RmOption option, CancellationToken cancellationToken) : base(option, cancellationToken)
    {
        _option = option;
    }

    protected override IEnumerable<string> GetScanRoots()
    {
        if (!string.IsNullOrEmpty(_option.Destination))
        {
            yield return _option.Destination;
        }
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
                        DeleteFileCore(fullPath);
                        _ = Interlocked.Increment(ref _deletedFiles);
                        _logger.Log($"{fullPath}", Logging.TurLogLevel.Information, Constants.CheckUnicode, suffix: _option.Trash ? "F, TRASH" : "F");
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
                        DeleteDirectoryCore(fullPath, true);
                        _ = Interlocked.Increment(ref _deletedDirectories);
                        _logger.Log($"{fullPath}", Logging.TurLogLevel.Information, Constants.CheckUnicode, suffix: _option.Trash ? "D, TRASH" : "D");
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
                    _hasError = true;
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
        var builder = new TurSystemBuilder(_option.Destination, buildOption, CancellationToken, LogSkipped);
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
                            DeleteDirectoryCore(item.FullPath, true);
                            _ = Interlocked.Increment(ref _deletedDirectories);
                            _logger.Log($"{item.RelativePath}", Logging.TurLogLevel.Information, Constants.CheckUnicode, suffix: _option.Trash ? "D, TRASH" : "D");
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
                            DeleteFileCore(item.FullPath);
                            _ = Interlocked.Increment(ref _deletedFiles);
                            _logger.Log($"{item.RelativePath}", Logging.TurLogLevel.Information, Constants.CheckUnicode, suffix: _option.Trash ? "F, TRASH" : "F");
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
                    _hasError = true;
                    break;
                }
            }
        }

        if (!Directory.EnumerateFileSystemEntries(_option.Destination, "*", SearchOption.TopDirectoryOnly).Any())
        {
            DeleteDirectoryCore(_option.Destination, false);
            _logger.Log(_option.Destination, Logging.TurLogLevel.Information, Constants.CheckUnicode, suffix: _option.Trash ? "D, TRASH" : "D");
        }
    }

    private void DeleteFileCore(string fullPath)
    {
        if (_option.Trash)
        {
            RecycleBin.MoveToTrash(fullPath, _option.TrashRoot);
        }
        else
        {
            File.Delete(fullPath);
        }
    }

    private void DeleteDirectoryCore(string fullPath, bool recursive)
    {
        if (_option.Trash)
        {
            RecycleBin.MoveToTrash(fullPath, _option.TrashRoot);
        }
        else
        {
            Directory.Delete(fullPath, recursive);
        }
    }

    private bool ConfirmDeletion()
    {
        if (_option.DryRun || _option.Yes || _option.NoUserInteractive)
        {
            return true;
        }

        // Never block non-interactive callers: prompt only on a real terminal.
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return true;
        }

        List<string> targets = new();
        if (!string.IsNullOrEmpty(_option.FromFile))
        {
            targets.Add($"items listed in {_option.FromFile}");
        }

        if (!string.IsNullOrEmpty(_option.Destination))
        {
            targets.Add(_option.Destination);
        }

        var mode = _option.Trash ? "move to trash" : "PERMANENTLY delete";
        _logger.Log($"About to {mode}: {string.Join(" and ", targets)}", TurLogLevel.Warning, Constants.XUnicode, false);
        Console.Write("Continue? [y/N] ");
        var answer = Console.ReadLine();
        if (answer != null && (answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)
                               || answer.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        _logger.Log("Aborted by user.", TurLogLevel.Warning, Constants.ArrowUnicode, false);
        return false;
    }

    protected override void PostCheck()
    {
        base.PostCheck();
        var summary = $"{_deletedFiles} files and {_deletedDirectories} directories deleted.";
        if (_option.DryRun)
        {
            summary += " (dry run)";
        }

        _logger.Log(summary, TurLogLevel.Information, Constants.ArrowUnicode, false);
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

        if (!ConfirmDeletion())
        {
            return false;
        }

        return base.PreCheck();
    }

    protected override async Task<int> HandleInternalAsync()
    {
        if (!string.IsNullOrEmpty(_option.FromFile))
        {
            await DeleteFromFileListAsync();
        }

        if (!string.IsNullOrEmpty(_option.Destination))
        {
            DeleteFromFilter();
        }

        return _hasError ? 1 : 0;
    }
}