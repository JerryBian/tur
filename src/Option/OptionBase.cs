using System;
using System.IO;

namespace Tur.Option;

public abstract class OptionBase
{
    protected OptionBase(string[] args, string cmd)
    {
        CmdName = cmd;
        RawArgs = args;
        OutputDir = Path.GetTempPath();
    }

    public string OutputDir { get; set; } = Path.GetTempPath();

    public string[] Includes { get; set; }

    public string[] Excludes { get; set; }

    public DateTime LastModifyBefore { get; set; }

    public DateTime LastModifyAfter { get; set; }

    public DateTime CreateBefore { get; set; }

    public DateTime CreateAfter { get; set; }

    public bool EnableVerbose { get; set; }

    public string[] RawArgs { get; }

    public string CmdName { get; }

    public bool IgnoreError { get; set; }

    public bool NoConsole { get; set; }
}