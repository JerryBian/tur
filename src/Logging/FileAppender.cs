using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Tur.Logging
{
    public class FileAppender : BlockingAppender
    {
        private readonly string _file;
        private StreamWriter _writer;

        public FileAppender(string file)
        {
            _file = file;
        }

        protected override void Handle(TurLogItem item)
        {
            try
            {
                // Keep the file handle open across writes: re-opening per line
                // dominated runtime on operations logging tens of thousands of lines.
                // AutoFlush keeps the log crash-safe; FileShare.Read lets users tail it.
                _writer ??= new StreamWriter(
                    new FileStream(_file, FileMode.Append, FileAccess.Write, FileShare.Read),
                    new UTF8Encoding(false))
                {
                    AutoFlush = true
                };

                var fullMessage = item.Message ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(item.Prefix))
                {
                    fullMessage = item.PrefixSurroundWithBrackets
                        ? $"[{item.Prefix}]  {fullMessage}"
                        : $"{item.Prefix}  {fullMessage}";
                }

                if (!string.IsNullOrWhiteSpace(item.Suffix))
                {
                    fullMessage = $"{fullMessage}  {item.Suffix}";
                }

                if (item.Error != null)
                {
                    if (fullMessage.EndsWith(Environment.NewLine, StringComparison.OrdinalIgnoreCase))
                    {
                        fullMessage += Environment.NewLine;
                    }

                    fullMessage += item.Error;
                }

                fullMessage += Environment.NewLine;
                _writer.Write(fullMessage);
            }
            catch { }
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            try
            {
                _writer?.Dispose();
            }
            catch { }
        }
    }
}
