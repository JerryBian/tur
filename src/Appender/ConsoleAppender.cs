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
            var items = item.Unwrap();
            for(var i=0;i<items.Count;i++)
            {
                var segment = items[i];
                var isLastSegment = i == items.Count - 1;
                if (segment.Level == LogSegmentLevel.Verbose)
                {
                    WriteMessage(writer, segment.Message, segment.Error, ConsoleColor.DarkGray, isLastSegment);
                    continue;
                }

                if (segment.Level == LogSegmentLevel.Default)
                {
                    WriteMessage(writer, segment.Message, segment.Error, writeLine: isLastSegment);
                    continue;
                }

                if (segment.Level == LogSegmentLevel.Success)
                {
                    WriteMessage(writer, segment.Message, segment.Error, ConsoleColor.DarkGreen, isLastSegment);
                    continue;
                }

                if (segment.Level == LogSegmentLevel.Warn)
                {
                    WriteMessage(writer, segment.Message, segment.Error, ConsoleColor.DarkYellow, isLastSegment);
                    continue;
                }

                if (segment.Level == LogSegmentLevel.Error)
                {
                    WriteMessage(writer, segment.Message, segment.Error, ConsoleColor.DarkRed, isLastSegment);
                    continue;
                }
            }
        }

        private void WriteMessage(TextWriter writer, string message, Exception error = null, ConsoleColor? foregroundColor = null, bool writeLine = false)
        {
            if (foregroundColor != null)
            {
                Console.ForegroundColor = foregroundColor.Value;
            }

            if(writeLine && error == null)
            {
                writer.WriteLine(message);
            }
            else
            {
                writer.Write(message);
            }

            if (foregroundColor != null)
            {
                Console.ResetColor();
            }

            if (error != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                writer.WriteLine($"{Environment.NewLine}{error.Message}");
                Console.ResetColor();
            }
        }
    }
}
