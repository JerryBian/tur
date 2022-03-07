namespace Tur.Option;

public class SyncOption : OptionBase
{
    public SyncOption()
    {
        CmdName = "sync";
    }

    public bool Delete { get; set; }

    public bool DryRun { get; set; }

    public string SrcDir { get; set; }

    public string DestDir { get; set; }
}