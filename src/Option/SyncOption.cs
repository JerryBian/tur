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

    public string ExcludeFrom { get; set; }

    public bool DeleteExcluded { get; set; }

    // Copy throughput limit in bytes per second; 0 means unlimited.
    public long BwLimit { get; set; }
}