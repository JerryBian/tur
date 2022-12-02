using System;

namespace Tur.Sink;

public class SinkEntry
{
    public SinkEntry(string message, SinkType type, Exception ex = null)
    {
        Message = message;
        Type = type;
        Exception = ex;
    }

    public string Message { get; set; }

    public SinkType Type { get; set; }

    public Exception Exception { get; set; }

    public int State { get; set; }
}