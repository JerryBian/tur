using System;
using System.IO;
using System.Text;

namespace Tur.Logging
{
    public class FileAppender : BlockingAppender
    {
        private readonly string _file;

        public FileAppender(string file)
        {
            _file = file;
        }

        protected override void Handle(TurLogItem item)
        {
            var fullMessage = item.Message;
            if (!string.IsNullOrWhiteSpace(item.Prefix))
            {
                fullMessage = $"[{item.Prefix}]  {fullMessage}";
            }

            if (!string.IsNullOrWhiteSpace(item.Suffix))
            {
                fullMessage = $"{fullMessage}  {item.Suffix}";
            }

            if (item.Error != null)
            {
                if (fullMessage.EndsWith(Environment.NewLine, StringComparison.OrdinalIgnoreCase))
                {
                    fullMessage += Environment.NewLine;
                }

                fullMessage += item.Error;
            }

            fullMessage += Environment.NewLine;
            File.AppendAllText(_file, fullMessage, Encoding.UTF8);
        }
    }
}
