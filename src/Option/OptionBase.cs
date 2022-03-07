namespace Tur.Option;

public abstract class OptionBase
{
    public string OutputDir { get; set; }

    public string[] Includes { get; set; }

    public string[] Excludes { get; set; }

    public bool EnableVerbose { get; set; }

    public bool Recursive { get; set; }

    public string RawArgs { get; set; }

    public bool EmitResult { get; set; }

    public string CmdName { get; set; }
}