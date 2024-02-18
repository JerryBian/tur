using System;
using System.Text;

namespace Tur.Logging
{
    public class ConsoleAppender : BlockingAppender
    {
        private readonly bool _userInteractive;

        public ConsoleAppender(bool userInteractive)
        {
            Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8;
            _userInteractive = userInteractive;
        }

        private void WriteWithColor(string message, ConsoleColor color)
        {
            try
            {
                Console.ForegroundColor = color;
                Console.Write(message);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        protected override void Handle(TurLogItem item)
        {
            if (_userInteractive)
            {
                if (!string.IsNullOrWhiteSpace(item.Prefix))
                {
                    WriteWithColor("[", ConsoleColor.DarkGray);
                    var prefixColor = item.LogLevel == TurLogLevel.Information ? ConsoleColor.Green : (item.LogLevel == TurLogLevel.Warning ? ConsoleColor.Yellow : ConsoleColor.Red);
                    WriteWithColor(item.Prefix, prefixColor);
                    WriteWithColor("]", ConsoleColor.DarkGray);
                    Console.Write("  ");
                }

                WriteWithColor(item.Message, item.LogLevel == TurLogLevel.Information ? ConsoleColor.Blue : (item.LogLevel == TurLogLevel.Warning ? ConsoleColor.DarkYellow : ConsoleColor.DarkRed));

                if (!string.IsNullOrWhiteSpace(item.Suffix))
                {
                    Console.Write("  ");
                    WriteWithColor($"({item.Suffix})", ConsoleColor.DarkGray);
                }

                if (item.Error != null)
                {
                    Console.Write(Environment.NewLine);
                    WriteWithColor($"    Error: {item.Error.Message}", ConsoleColor.DarkRed);
                }

                Console.Write(Environment.NewLine);
                return;
            }

            var fullMessage = item.Message;
            if (!string.IsNullOrWhiteSpace(item.Prefix))
            {
                fullMessage = $"[{item.Prefix}]  {fullMessage}";
            }

            if (!string.IsNullOrWhiteSpace(item.Suffix))
            {
                fullMessage = $"{fullMessage}  {item.Suffix}";
            }

            Console.Write(fullMessage);
            Console.Write(Environment.NewLine);
        }
    }
}
