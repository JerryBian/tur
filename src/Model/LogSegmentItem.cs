using System;

namespace Tur.Model
{
    public class LogSegmentItem
    {
        public LogSegmentItem(LogSegmentLevel level, string message, Exception error)
        {
            Level = level;
            Message = message;
            Error = error;
        }

        public LogSegmentLevel Level { get; set; }

        public string Message { get; set; }

        public Exception Error { get; set; }
    }
}
