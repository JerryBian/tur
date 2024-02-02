using System;
using System.IO;
using System.Text;
using Tur.Model;

namespace Tur.Appender
{
    public class FileAppender : BlockingAppender
    {
        private readonly string _file;

        public FileAppender(string file)
        {
            _file = file;
        }

        protected override void Handle(LogItem item)
        {
            foreach (var segment in item.Unwrap())
            {
                if (segment.Level == LogSegmentLevel.Error)
                {
                    var text = segment.Message;
                    if (segment.Error != null)
                    {
                        text = $"{text}{Environment.NewLine}{segment.Error}{Environment.NewLine}";
                    }

                    File.AppendAllText(_file, text, Encoding.UTF8);
                    continue;
                }

                File.AppendAllText(_file, segment.Message);
            }

            File.AppendAllText(_file, Environment.NewLine);
        }
    }
}
