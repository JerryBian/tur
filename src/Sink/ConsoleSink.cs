using System;
using System.Text;
using System.Threading.Tasks;
using Tur.Option;

namespace Tur.Sink;

public class ConsoleSink : SinkBase
{
    public ConsoleSink(OptionBase option) : base(option)
    {
        Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8;
    }

    protected override async Task ProcessSinkEntryAsync(SinkEntry entry)
    {
        if (entry.Type == SinkType.Default)
        {
            await Console.Out.WriteAsync(entry.Message);
            return;
        }

        if (entry.Type == SinkType.DefaultLine)
        {
            await Console.Out.WriteLineAsync(entry.Message);
            return;
        }

        if (entry.Type == SinkType.Info)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            await Console.Out.WriteAsync(entry.Message);
            Console.ResetColor();
            return;
        }

        if (entry.Type == SinkType.InfoLine)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            await Console.Out.WriteLineAsync(entry.Message);
            Console.ResetColor();
            return;
        }

        if (entry.Type == SinkType.Light)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            await Console.Out.WriteAsync(entry.Message);
            Console.ResetColor();
            return;
        }

        if (entry.Type == SinkType.LightLine)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            await Console.Out.WriteLineAsync(entry.Message);
            Console.ResetColor();
            return;
        }

        if (entry.Type == SinkType.WarnLine)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            await Console.Out.WriteLineAsync(entry.Message);
            Console.ResetColor();
            return;
        }

        if (entry.Type == SinkType.Warn)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            await Console.Out.WriteAsync(entry.Message);
            Console.ResetColor();
            return;
        }

        if (entry.Type == SinkType.Error)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            await Console.Error.WriteAsync(entry.Message);
            Console.ResetColor();
            return;
        }

        if (entry.Type == SinkType.ErrorLine)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            await Console.Error.WriteLineAsync(entry.Message);
            Console.ResetColor();
            return;
        }

        if (entry.Type == SinkType.ClearLine && !SinkOption.NoConsole)
        {
            var width = Console.BufferWidth;
            int lineCursor = Console.CursorTop;
            if (entry.State >= 0)
            {
                for (var i = entry.State; i <= lineCursor; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', width));
                }
            }
            else
            {
                Console.SetCursorPosition(0, lineCursor);
                Console.Write(new string(' ', width));
            }

            var cursorTop = entry.State >= 0 ? entry.State : lineCursor;
            Console.SetCursorPosition(0, cursorTop);
            return;
        }

        throw new NotImplementedException();
    }
}