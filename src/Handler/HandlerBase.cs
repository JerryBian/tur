using System;
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

namespace Tur.Handler;

public abstract class HandlerBase : IAsyncDisposable
{
    private readonly OptionBase _option;
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
            _logger.Write("Unexpected error.", TurLogLevel.Error, error: ex);
        }

        stopwatch.Stop();

        _logger.Write(string.Empty);
        _logger.Write($"{Constants.ArrowUnicode} All done. Elapsed: [{stopwatch.Elapsed.Human()}]");
        return exitCode;
    }

    private void WriteLogHeader()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var versionStr = version == null ? "1.0.0" : version.ToString(3);
        StringBuilder sb = new();
        _ = sb.AppendLine($"## Processed by tur {versionStr} ##");
        _ = sb.AppendLine($"## Command: tur {string.Join(" ", _option.RawArgs)} ##");

        _logger.Write(sb.ToString());
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

        buildOptions.IgnoreError = _option.IgnoreError;
        return buildOptions;
    }

    protected abstract Task<int> HandleInternalAsync();

    public async ValueTask DisposeAsync()
    {
        await _logger.DisposeAsync();
    }
}