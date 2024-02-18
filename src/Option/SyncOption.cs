namespace Tur.Option;

public class SyncOption : OptionBase
{
    public SyncOption(string[] args) : base(args, "sync")
    {
    }

    public bool Delete { get; set; }

    public bool DryRun { get; set; }

    public string SrcDir { get; set; }

    public string DestDir { get; set; }

    public bool SizeOnly { get; set; }
}