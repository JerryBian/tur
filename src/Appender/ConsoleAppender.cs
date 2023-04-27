using System;
using System.IO;
using System.Text;
using Tur.Model;

namespace Tur.Appender
{
    public class ConsoleAppender : BlockingAppender
    {
        public ConsoleAppender()
        {
            Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8;
        }

        protected override void Handle(LogItem item)
        {
            TextWriter writer = item.IsStdError ? Console.Error : Console.Out;
            foreach (LogSegmentItem segment in item.Unwrap())
            {
                if (segment.Level == LogSegmentLevel.Verbose)
                {
                    WriteMessage(writer, segment.Message, segment.Error, ConsoleColor.DarkGray);
                    continue;
                }

                if (segment.Level == LogSegmentLevel.Default)
                {
                    WriteMessage(writer, segment.Message, segment.Error);
                    continue;
                }

                if (segment.Level == LogSegmentLevel.Success)
                {
                    WriteMessage(writer, segment.Message, segment.Error, ConsoleColor.DarkGreen);
                    continue;
                }

                if (segment.Level == LogSegmentLevel.Warn)
                {
                    WriteMessage(writer, segment.Message, segment.Error, ConsoleColor.DarkYellow);
                    continue;
                }

                if (segment.Level == LogSegmentLevel.Error)
                {
                    WriteMessage(writer, segment.Message, segment.Error, ConsoleColor.DarkRed);
                    continue;
                }
            }

            Console.Out.WriteLine();
        }

        private void WriteMessage(TextWriter writer, string message, Exception error = null, ConsoleColor? foregroundColor = null)
        {
            if (foregroundColor != null)
            {
                Console.ForegroundColor = foregroundColor.Value;
            }

            writer.Write(message);

            if (foregroundColor != null)
            {
                Console.ResetColor();
            }

            if (error != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                writer.Write($"{Environment.NewLine}{error.Message}{Environment.NewLine}");
                Console.ResetColor();
            }
        }
    }
}
