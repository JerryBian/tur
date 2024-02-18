using System;
using System.Collections.Generic;

namespace Tur.Core
{
    public class TurBuildOptions
    {
        public bool IncludeFileSize { get; set; }

        public bool IgnoreError { get; set; }

        public bool IncludeFiles { get; set; }

        public bool IncludeDirectories { get; set; }

        public bool IncludeAttributes { get; set; }

        public DateTime? LastModifyAfter { get; set; }

        public DateTime? LastModifyBefore { get; set; }

        public DateTime? CreateAfter { get; set; }

        public DateTime? CreateBefore { get; set; }

        public List<string> IncludeGlobPatterns { get; } = new();

        public List<string> ExcludeGlobPatterns { get; } = new();
    }
}
