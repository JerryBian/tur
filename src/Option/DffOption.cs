using System.Collections.Generic;

namespace Tur.Option;

public class DffOption : OptionBase
{
    public DffOption(string outputDir, string[] includes, string[] excludes, bool enableVerbose,
        string[] args) : base(outputDir, includes, excludes, enableVerbose, args, "dff")
    {
    }

    public string Dir { get; set; }

    // Unit Tests only
    public List<List<string>> ExportedList { get; set; }
}