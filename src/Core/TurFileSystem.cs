using System;

namespace Tur.Core
{
    public class TurFileSystem
    {
        public string Name { get; set; }

        public string FullPath { get; set; }

        public string RelativePath { get; set; }

        public bool IsDirectory { get; set; }

        public long Length { get; set; }

        public DateTime? CreationTime { get; set; }

        public DateTime? LastModifyTime { get; set; }

        public override bool Equals(object obj)
        {
            return obj is TurFileSystem other && FullPath == other.FullPath;
        }

        public override int GetHashCode()
        {
            return FullPath.GetHashCode();
        }
    }
}
