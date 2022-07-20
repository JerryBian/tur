using System;
using System.IO;

namespace Tur.Option;

public abstract class OptionBase
{
    protected OptionBase(string outputDir, string[] includes, string[] excludes, bool enableVerbose,
        string[] args, string cmd)
    {
        if (!string.IsNullOrEmpty(outputDir))
        {
            if (!Directory.Exists(outputDir))
            {
                throw new Exception($"Directory specified in -o/--output does not exist: {outputDir}");
            }

            OutputDir = Path.GetFullPath(outputDir);
        }
        else
        {
            OutputDir = Path.GetTempPath();
        }

        Directory.CreateDirectory(OutputDir);
        Includes = includes;
        Excludes = excludes;
        EnableVerbose = enableVerbose;
        RawArgs = args;
        CmdName = cmd;
    }

    public string OutputDir { get; set; }

    public string[] Includes { get; set; }

    public string[] Excludes { get; set; }

    public long MinModifyTimeSpam { get; set; }
    public long MaxModifyTimeSpam { get; set; }

    public bool EnableVerbose { get; set; }

    public string[] RawArgs { get; set; }

    public string CmdName { get; }
}