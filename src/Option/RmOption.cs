namespace Tur.Option;

public class RmOption : OptionBase
{
    public RmOption(string outputDir, string[] includes, string[] excludes, bool enableVerbose,
        string[] args) : base(outputDir, includes, excludes, enableVerbose, args, "rm")
    {
    }

    public bool Yes { get; set; }

    public bool File { get; set; }

    public bool Dir { get; set; }

    public bool EmptyDir { get; set; }

    public string FromFile { get; set; }

    public string Destination { get; set; }
}