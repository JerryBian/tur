namespace Tur.Option;

public class SyncOption : OptionBase
{
    public SyncOption(string outputDir, string[] includes, string[] excludes, bool enableVerbose,
        string[] args) : base(outputDir, includes, excludes, enableVerbose, args, "sync")
    {
    }

    public bool Delete { get; set; }

    public bool DryRun { get; set; }

    public string SrcDir { get; set; }

    public string DestDir { get; set; }

    public bool SizeOnly { get; set; }
}