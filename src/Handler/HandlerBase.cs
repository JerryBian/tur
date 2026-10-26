using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tur.Core;
using Tur.Extension;
using Tur.Logging;
using Tur.Option;
using Tur.Util;

namespace Tur.Handler;

public abstract class HandlerBase : IAsyncDisposable
{
    // Defaults applied automatically when the scan roots live on a network share
    // (SMB/NFS). Latency-bound links need concurrency to hide round trips.
    protected const int RemoteScanParallelism = 8;
    protected const int RemoteCopyParallelism = 16;
    protected const int RemoteHashParallelism = 8;

    private readonly OptionBase _option;
    private readonly object _remoteLock = new();
    private bool? _isRemote;
    protected readonly ITurLogger _logger;
    protected readonly CancellationToken CancellationToken;

    protected HandlerBase(OptionBase option, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(option.OutputDir))
        {
            option.OutputDir = Path.GetTempPath();
        }

        _option = option;
        CancellationToken = cancellationToken;
        _logger = new TurLogger(option, CancellationToken);
    }

    public async Task<int> HandleAsync()
    {
        WriteLogHeader();
        var stopwatch = Stopwatch.StartNew();
        var exitCode = 1;
        try
        {
            if (PreCheck())
            {
                exitCode = await HandleInternalAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Log("Unexpected error.", TurLogLevel.Error, error: ex);
        }

        stopwatch.Stop();

        _logger.Log(string.Empty);
        var status = exitCode == 0 ? "All done." : "Completed with errors.";
        _logger.Log($"{status} Elapsed: [{stopwatch.Elapsed.Human()}]", prefix: Constants.ArrowUnicode, prefixSurroundWithBrackets: false);
        PostCheck();
        return exitCode;
    }

    private void WriteLogHeader()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var versionStr = version == null ? "1.0.0" : version.ToString(3);
        StringBuilder sb = new();
        _ = sb.AppendLine($"## Processed by tur {versionStr} ##");
        _ = sb.AppendLine($"## Command: tur {string.Join(" ", _option.RawArgs)} ##");

        _logger.Log(sb.ToString(), TurLogLevel.Trace);
        _logger.Log(string.Empty, TurLogLevel.Trace);
    }

    protected virtual void PostCheck()
    {

    }

    protected virtual bool PreCheck()
    {
        if (!string.IsNullOrEmpty(_option.OutputDir))
        {
            _option.OutputDir = Path.GetFullPath(_option.OutputDir);
            if (!Directory.Exists(_option.OutputDir))
            {
                _ = Directory.CreateDirectory(_option.OutputDir);
            }
        }

        return true;
    }

    protected void LogSkipped(string path, Exception ex)
    {
        _logger.Log(path, TurLogLevel.Warning, Constants.XUnicode, suffix: "SKIPPED", error: ex);
    }

    /// <summary>The directories this handler will enumerate; used for remote detection.</summary>
    protected virtual IEnumerable<string> GetScanRoots()
    {
        yield break;
    }

    /// <summary>True when any scan root lives on a network share. Computed once.</summary>
    protected bool IsRemoteEnvironment()
    {
        lock (_remoteLock)
        {
            _isRemote ??= PathUtil.AnyRemote(GetScanRoots());
            return _isRemote.Value;
        }
    }

    protected TurBuildOptions CreateBuildOptions()
    {
        var buildOptions = new TurBuildOptions();
        if (_option.Includes != null && _option.Includes.Length != 0)
        {
            buildOptions.IncludeGlobPatterns.AddRange(_option.Includes);
        }

        if (_option.Excludes != null && _option.Excludes.Length != 0)
        {
            buildOptions.ExcludeGlobPatterns.AddRange(_option.Excludes);
        }

        if (_option.CreateBefore != default)
        {
            buildOptions.CreateBefore = _option.CreateBefore;
        }

        if (_option.CreateAfter != default)
        {
            buildOptions.CreateAfter = _option.CreateAfter;
        }

        if (_option.LastModifyBefore != default)
        {
            buildOptions.LastModifyBefore = _option.LastModifyBefore;
        }

        if (_option.LastModifyAfter != default)
        {
            buildOptions.LastModifyAfter = _option.LastModifyAfter;
        }

        // Time filters can only be applied when file attributes are read during enumeration.
        if (_option.CreateBefore != default || _option.CreateAfter != default
            || _option.LastModifyBefore != default || _option.LastModifyAfter != default)
        {
            buildOptions.IncludeAttributes = true;
        }

        buildOptions.FollowLinks = _option.FollowLinks;
        buildOptions.MaxDepth = _option.MaxDepth;
        buildOptions.ScanParallelism = _option.Threads ?? (IsRemoteEnvironment() ? RemoteScanParallelism : 1);
        if (_option.MinSize.HasValue)
        {
            buildOptions.MinFileSize = _option.MinSize;
            buildOptions.IncludeFileSize = true;
        }

        if (_option.MaxSize.HasValue)
        {
            buildOptions.MaxFileSize = _option.MaxSize;
            buildOptions.IncludeFileSize = true;
        }

        buildOptions.IgnoreError = _option.IgnoreError;
        return buildOptions;
    }

    protected abstract Task<int> HandleInternalAsync();

    public async ValueTask DisposeAsync()
    {
        await _logger.DisposeAsync();
    }
}