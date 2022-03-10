namespace Tur.Option;

public class RmOption : OptionBase
{
    public RmOption(string outputDir, string[] includes, string[] excludes, bool enableVerbose, bool recursive,
        string[] args) : base(outputDir, includes, excludes, enableVerbose, recursive, args, "rm")
    {
    }

    public bool Backup { get; set; }

    public bool Yes { get; set; }

    public bool File { get; set; }

    public bool Dir { get; set; }

    public bool EmptyDir { get; set; }

    public string FromFile { get; set; }

    public string Destination { get; set; }
}