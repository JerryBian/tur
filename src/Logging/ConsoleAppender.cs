using System;
using System.Collections.Generic;
using System.Text;

namespace Tur.Logging
{
    public class ConsoleAppender : BlockingAppender
    {
        // Plain ASCII stand-ins used when stdout is redirected, so consumers
        // decoding the pipe with a legacy code page never see mojibake.
        private static readonly Dictionary<string, string> _asciiPrefixes = new Dictionary<string, string>
        {
            { Constants.ArrowUnicode, "->" },
            { Constants.SquareUnicode, "*" },
            { Constants.DotUnicode, "-" },
            { Constants.CheckUnicode, "+" }
        };

        private readonly bool _verbose;
        private readonly bool _colorsEnabled;
        private readonly bool _redirected;
        private readonly Lazy<Dictionary<string, ConsoleColor>> _prefixColors;
        private readonly Lazy<Dictionary<TurLogLevel, ConsoleColor>> _messageColors;

        public ConsoleAppender(bool userInteractive, bool verbose)
        {
            _verbose = verbose;

            // UTF-8 keeps unicode prefixes intact on legacy console code pages and
            // in piped output. It can fail when no console is attached, which is harmless.
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch { }

            _redirected = Console.IsOutputRedirected;
            _colorsEnabled = userInteractive && !_redirected && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
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

        private void Write(string message, ConsoleColor color)
        {
            if (!_colorsEnabled)
            {
                Console.Write(message);
                return;
            }

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

        private string ResolvePrefix(string prefix)
        {
            if (_redirected && prefix != null && _asciiPrefixes.TryGetValue(prefix, out var ascii))
            {
                return ascii;
            }

            return prefix;
        }

        protected override void Handle(TurLogItem item)
        {
            if (item.LogLevel == TurLogLevel.Trace && !_verbose)
            {
                return;
            }

            // Never print a log line on top of the transient progress line.
            ProgressRenderer.ClearActive();

            var message = item.Message ?? string.Empty;

            // Keep the message's own leading indentation so nested output (e.g. the dff
            // group listing) aligns identically in a terminal and in a pipe. Never mutate
            // the item: the file appender consumes the same instance on another thread.
            var indent = 0;
            while (indent < message.Length && char.IsWhiteSpace(message[indent]))
            {
                indent++;
            }

            var body = message.Substring(indent).TrimEnd();
            var prefix = ResolvePrefix(item.Prefix);
            var hasPrefix = !string.IsNullOrWhiteSpace(prefix);
            var hasSuffix = !string.IsNullOrWhiteSpace(item.Suffix);

            if (!_colorsEnabled)
            {
                // Single-write path for redirected/plain output: each Console.Write is
                // lock-synchronized, so assembling the whole line is measurably faster.
                StringBuilder sb = new();
                if (indent > 0)
                {
                    _ = sb.Append(' ', indent);
                }

                if (hasPrefix)
                {
                    if (item.PrefixSurroundWithBrackets)
                    {
                        _ = sb.Append('[');
                    }

                    _ = sb.Append(prefix);

                    if (item.PrefixSurroundWithBrackets)
                    {
                        _ = sb.Append(']');
                    }

                    _ = sb.Append("  ");
                }

                _ = sb.Append(body);

                if (hasSuffix)
                {
                    _ = sb.Append("  (").Append(item.Suffix).Append(')');
                }

                if (item.Error != null)
                {
                    _ = sb.Append(Environment.NewLine).Append("    Error: ").Append(item.Error.Message);
                }

                _ = sb.Append(Environment.NewLine);
                Console.Write(sb.ToString());
                return;
            }

            if (indent > 0)
            {
                Console.Write(new string(' ', indent));
            }

            if (hasPrefix)
            {
                if (item.PrefixSurroundWithBrackets)
                {
                    Write("[", ConsoleColor.DarkGray);
                }

                Write(prefix, _prefixColors.Value.GetValueOrDefault(item.Prefix, ConsoleColor.Black));

                if (item.PrefixSurroundWithBrackets)
                {
                    Write("]", ConsoleColor.DarkGray);
                }

                Console.Write("  ");
            }

            if (!string.IsNullOrEmpty(body))
            {
                Write(body, _messageColors.Value.GetValueOrDefault(item.LogLevel, ConsoleColor.Black));
            }

            if (hasSuffix)
            {
                Console.Write("  ");
                Write($"({item.Suffix})", ConsoleColor.DarkGray);
            }

            if (item.Error != null)
            {
                Console.Write(Environment.NewLine);
                Write($"    Error: {item.Error.Message}", ConsoleColor.DarkRed);
            }

            Console.Write(Environment.NewLine);
        }
    }
}
