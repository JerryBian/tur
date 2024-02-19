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
            _consoleAppender = new ConsoleAppender(!options.NoUserInteractive, options.Verbose);
            _fileLogPath = Path.Combine(options.OutputDir, $"tur-{options.CmdName}-{Path.GetRandomFileName().Replace(".", string.Empty)}.log");
            _fileAppender = new FileAppender(_fileLogPath);
            _cancellationToken = cancellationToken;
        }

        public async ValueTask DisposeAsync()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                Log($"User requested to cancel ...", TurLogLevel.Warning, Constants.ArrowUnicode, false);
            }

            var item = new TurLogItem
            {
                LogLevel = TurLogLevel.Information,
                Message = $"Log file: {_fileLogPath}",
                Prefix = Constants.ArrowUnicode,
                PrefixSurroundWithBrackets = false
            };
            _consoleAppender.Add(item);
            await _consoleAppender.DisposeAsync();
            await _fileAppender.DisposeAsync();
        }

        public void Log(
            string message,
            TurLogLevel level = TurLogLevel.Information,
            string prefix = null,
            bool prefixSurroundWithBrackets = true,
            string suffix = null,
            Exception error = null)
        {
            var item = new TurLogItem
            {
                Suffix = suffix,
                PrefixSurroundWithBrackets = prefixSurroundWithBrackets,
                Error = error,
                LogLevel = level,
                Message = message,
                Prefix = prefix
            };

            try
            {
                _consoleAppender.Add(item);
            }
            catch { }

            try
            {
                _fileAppender.Add(item);
            }
            catch { }
        }
    }
}
