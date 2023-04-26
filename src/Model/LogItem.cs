using System;
using System.Collections.Generic;

namespace Tur.Model
{
    public class LogItem
    {
        private readonly List<LogSegmentItem> _segments;

        public LogItem()
        {
            _segments = new List<LogSegmentItem>();
        }

        public bool IsStdError { get; set; }

        public void AddSegment(LogSegmentLevel level, string message, Exception error = null)
        {
            _segments.Add(new LogSegmentItem(level, message, error));
        }

        public void AddLine()
        {
            _segments.Add(new LogSegmentItem(LogSegmentLevel.Default, Environment.NewLine, null));
        }

        public List<LogSegmentItem> Unwrap()
        {
            return _segments;
        }
    }
}
