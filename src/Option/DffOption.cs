namespace Tur.Option;

public class DffOption : OptionBase
{
    public DffOption(string outputDir, string[] includes, string[] excludes, bool enableVerbose, bool recursive,
        string[] args) : base(outputDir, includes, excludes, enableVerbose, recursive, args, "dff")
    {
    }

    public string Dir { get; set; }
}