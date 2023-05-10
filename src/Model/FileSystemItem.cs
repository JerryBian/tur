using System;
using System.IO;

namespace Tur.Model
{
    public class FileSystemItem
    {
        private readonly Lazy<long> _size;
        private readonly Lazy<DateTime> _createTime;
        private readonly Lazy<DateTime> _lastWriteTime;

        public FileSystemItem(bool isDir)
        {
            IsDir = isDir;
            _size = new Lazy<long>(GetSize, true);
            _createTime = new Lazy<DateTime>(GetCreateTime, true);
            _lastWriteTime = new Lazy<DateTime>(GetLastWriteTime, true);
        }

        public bool IsDir { get; }

        public string FullPath { get; set; }

        public long Size => _size.Value;

        public DateTime CreateTime => _createTime.Value;

        public DateTime LastWriteTime => _lastWriteTime.Value;

        private long GetSize()
        {
            return !IsDir ? new FileInfo(FullPath).Length : throw new NotImplementedException();
        }

        private DateTime GetCreateTime()
        {
            return !IsDir ? File.GetCreationTime(FullPath) : throw new NotImplementedException();
        }

        private DateTime GetLastWriteTime()
        {
            return !IsDir ? File.GetLastWriteTime(FullPath) : throw new NotImplementedException();
        }
    }
}
