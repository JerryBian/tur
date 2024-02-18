using System;

namespace Tur.Logging
{
    public class TurLogItem
    {
        public string Message { get; set; }

        public string Prefix { get; set; }

        public string Suffix { get; set; }

        public TurLogLevel LogLevel { get; set; }

        public Exception Error { get; set; }
    }
}
