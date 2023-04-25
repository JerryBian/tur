using System;
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
            foreach(var segment in item.Unwrap())
            {
                if(segment.Level == LogSegmentLevel.Verbose)
                {
                    WriteMessage(segment.Message, ConsoleColor.DarkGray);
                    continue;
                }

                if(segment.Level == LogSegmentLevel.Default)
                {
                    WriteMessage(segment.Message);
                    continue;
                }

                if(segment.Level == LogSegmentLevel.Success)
                {
                    WriteMessage(segment.Message, ConsoleColor.DarkGreen);
                    continue;
                }

                if(segment.Level == LogSegmentLevel.Warn)
                {
                    WriteMessage(segment.Message, ConsoleColor.DarkYellow);
                    continue;
                }

                if(segment.Level == LogSegmentLevel.Error)
                {
                    WriteMessage(segment.Message, ConsoleColor.DarkRed, false, segment.Error);
                    continue;
                }
            }

            Console.Out.WriteLine();
        }

        private void WriteMessage(string message, ConsoleColor? foregroundColor = null, bool stdOut = true, Exception error = null)
        {
            if(foregroundColor != null)
            {
                Console.ForegroundColor = foregroundColor.Value;
            }

            if(stdOut)
            {
                Console.Out.Write(message);
            }
            else
            {
                Console.Error.WriteLine(message);
                if(error != null)
                {
                    Console.Error.WriteLine(error);
                } 
            }

            if (foregroundColor != null)
            {
                Console.ResetColor();
            }
        }
    }
}
