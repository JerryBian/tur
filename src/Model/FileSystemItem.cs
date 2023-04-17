using System;
using System.IO;

namespace Tur.Model
{
    public class FileSystemItem
    {
        private readonly Lazy<long> _size;

        public FileSystemItem(bool isDir)
        {
            IsDir = isDir;
            _size = new Lazy<long>(GetSize, true);
        }

        public bool IsDir { get; }

        public string FullPath { get; set; }

        public long Size => _size.Value;

        public bool HasError { get; set; }

        public Exception Error { get; set; }

        private long GetSize()
        {
            if(!IsDir)
            {
                return new FileInfo(FullPath).Length;
            }

            throw new NotImplementedException();
        }
    }
}
