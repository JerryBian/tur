using System;
using System.Text;
using System.Threading.Tasks;
using Tur.Option;
using Tur.Util;

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

        if (entry.Type == SinkType.ClearLine && AppUtil.HasMainWindow)
        {
            var lineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, lineCursor);
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, lineCursor);
            return;
        }

        throw new NotImplementedException();
    }
}