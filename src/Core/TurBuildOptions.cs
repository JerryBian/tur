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

        public bool FollowLinks { get; set; }

        // Number of concurrent directory scans. 1 (default) keeps the historical
        // sequential walk; higher values hide per-directory latency on network shares.
        public int ScanParallelism { get; set; } = 1;

        public long? MinFileSize { get; set; }

        public long? MaxFileSize { get; set; }

        public int? MaxDepth { get; set; }

        public DateTime? LastModifyAfter { get; set; }

        public DateTime? LastModifyBefore { get; set; }

        public DateTime? CreateAfter { get; set; }

        public DateTime? CreateBefore { get; set; }

        public List<string> IncludeGlobPatterns { get; } = new();

        public List<string> ExcludeGlobPatterns { get; } = new();
    }
}
