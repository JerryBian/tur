using System;
using System.IO;

namespace Tur.Option;

public abstract class OptionBase
{
    protected OptionBase(string outputDir, string[] includes, string[] excludes, bool enableVerbose, bool recursive,
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
        Recursive = recursive;
        RawArgs = args;
        CmdName = cmd;
    }

    public string OutputDir { get; }

    public string[] Includes { get; }

    public string[] Excludes { get; }

    public bool EnableVerbose { get; }

    public bool Recursive { get; }

    public string[] RawArgs { get; }

    public string CmdName { get; }
}