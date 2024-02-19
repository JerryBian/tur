using System;
using System.Collections.Generic;
using System.Text;

namespace Tur.Logging
{
    public class ConsoleAppender : BlockingAppender
    {
        private readonly bool _verbose;
        private readonly bool _userInteractive;
        private readonly Lazy<Dictionary<string, ConsoleColor>> _prefixColors;
        private readonly Lazy<Dictionary<TurLogLevel, ConsoleColor>> _messageColors;

        public ConsoleAppender(bool userInteractive, bool verbose)
        {
            _verbose = verbose;
            Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8;
            _userInteractive = userInteractive;
            _prefixColors = new Lazy<Dictionary<string, ConsoleColor>>(() =>
            {
                return new Dictionary<string, ConsoleColor>
                {
                    {Constants.ArrowUnicode, ConsoleColor.DarkCyan },
                    {Constants.DotUnicode, ConsoleColor.DarkGray },
                    {Constants.SquareUnicode, ConsoleColor.DarkGray },
                    {Constants.CheckUnicode, ConsoleColor.DarkGreen },
                    {Constants.XUnicode, ConsoleColor.DarkRed },
                    {Constants.DashUnicode, ConsoleColor.DarkMagenta }
                };
            });
            _messageColors = new Lazy<Dictionary<TurLogLevel, ConsoleColor>>(() =>
            {
                return new Dictionary<TurLogLevel, ConsoleColor>
                {
                    { TurLogLevel.Warning, ConsoleColor.Yellow },
                    { TurLogLevel.Information, ConsoleColor.Blue },
                    { TurLogLevel.Error, ConsoleColor.Red },
                    { TurLogLevel.Trace, ConsoleColor.Gray },
                };
            });
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
            if (item.LogLevel == TurLogLevel.Trace && !_verbose)
            {
                return;
            }

            if (_userInteractive)
            {
                if (!string.IsNullOrWhiteSpace(item.Prefix))
                {
                    var prefixMargin = string.Empty;
                    for (var i = 0; i < item.Message.Length; i++)
                    {
                        if (!char.IsWhiteSpace(item.Message[i]))
                        {
                            break;
                        }

                        prefixMargin += " ";
                    }

                    if (!string.IsNullOrEmpty(prefixMargin))
                    {
                        Console.Write(prefixMargin);
                    }

                    if (item.PrefixSurroundWithBrackets)
                    {
                        WriteWithColor("[", ConsoleColor.DarkGray);
                    }

                    var prefixColor = _prefixColors.Value.GetValueOrDefault(item.Prefix, ConsoleColor.Black);
                    WriteWithColor(item.Prefix, prefixColor);

                    if (item.PrefixSurroundWithBrackets)
                    {
                        WriteWithColor("]", ConsoleColor.DarkGray);
                    }

                    Console.Write("  ");
                }

                if (!string.IsNullOrEmpty(item.Message))
                {
                    item.Message = item.Message.Trim();
                }

                WriteWithColor(item.Message, _messageColors.Value.GetValueOrDefault(item.LogLevel, ConsoleColor.Black));

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

            var fullMessage = item.Message.Trim();
            if (!string.IsNullOrWhiteSpace(item.Prefix))
            {
                fullMessage = item.PrefixSurroundWithBrackets ? $"[{item.Prefix}]  {fullMessage}" : $"{item.Prefix}  {fullMessage}";
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
