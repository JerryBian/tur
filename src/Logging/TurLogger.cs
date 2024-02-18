using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tur.Option;

namespace Tur.Logging
{
    public class TurLogger : ITurLogger
    {
        private readonly CancellationToken _cancellationToken;
        private readonly IAppender _consoleAppender;
        private readonly IAppender _fileAppender;
        private readonly string _fileLogPath;

        public TurLogger(OptionBase options, CancellationToken cancellationToken)
        {
            _consoleAppender = new ConsoleAppender(!options.NoUserInteractive);
            _fileLogPath = Path.Combine(options.OutputDir, $"tur-{options.CmdName}-{Path.GetRandomFileName().Replace(".", string.Empty)}.log");
            _fileAppender = new FileAppender(_fileLogPath);
            _cancellationToken = cancellationToken;
        }

        public async ValueTask DisposeAsync()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                Write($"{Constants.ArrowUnicode} User requested to cancel ...", TurLogLevel.Warning);
            }

            _ = _consoleAppender.TryAdd($"{Constants.ArrowUnicode} Log file: {_fileLogPath}");
            await _consoleAppender.DisposeAsync();
            await _fileAppender.DisposeAsync();
        }

        public void Write(string message, TurLogLevel level = TurLogLevel.Information, string prefix = null, string suffix = null, Exception error = null)
        {
            try
            {
                _ = _consoleAppender.TryAdd(message, level, prefix, suffix, error);
            }
            catch { }

            try
            {
                _ = _fileAppender.TryAdd(message, level, prefix, suffix, error);
            }
            catch { }
        }
    }
}
