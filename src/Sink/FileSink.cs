using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Tur.Option;

namespace Tur.Sink;

public class FileSink : SinkBase
{
    private readonly string _file;

    public FileSink(string file, OptionBase option) : base(option)
    {
        _file = file;
    }

    protected override async Task ProcessSinkEntryAsync(SinkEntry entry)
    {
        try
        {
            string str = string.Empty;
            if (!string.IsNullOrEmpty(entry.Message))
            {
                str += entry.Message;
            }

            if ((entry.Type == SinkType.Error || entry.Type == SinkType.ErrorLine) && entry.Exception != null)
            {
                str += Environment.NewLine + entry.Exception;
            }

            if (entry.Type is SinkType.DefaultLine or SinkType.InfoLine or
                SinkType.WarnLine or SinkType.ErrorLine or
                SinkType.LightLine)
            {
                str += Environment.NewLine;
            }

            await File.AppendAllTextAsync(_file, str, new UTF8Encoding(false));
        }
        catch
        {
            // ignored
        }
    }
}