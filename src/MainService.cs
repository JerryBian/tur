using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tur.Handler;
using Tur.Option;

namespace Tur;

public class MainService
{
    private readonly string[] _args;
    private readonly CancellationToken _cancellationToken;

    public MainService(string[] args, CancellationToken cancellationToken)
    {
        _args = args;
        _cancellationToken = cancellationToken;
    }

    public async Task<int> RunAsync()
    {
        RootCommand rootCommand = new("Command line tool to manage files.");
        rootCommand.AddAlias("tur");

        Command dffCmd = CreateDffCommand();
        rootCommand.AddCommand(dffCmd);

        Command syncCmd = CreateSyncCommand();
        rootCommand.AddCommand(syncCmd);

        Command rmCmd = CreateRmCommand();
        rootCommand.AddCommand(rmCmd);

        return await rootCommand.InvokeAsync(_args);
    }

    private Command CreateDffCommand()
    {
        Command cmd = new("dff", "Duplicate files finder.");
        Option<string[]> includeOption = CreateIncludeOption();
        cmd.AddOption(includeOption);

        Option<string[]> excludeOption = CreateExcludeOption();
        cmd.AddOption(excludeOption);

        Option<string> outputOption = CreateOutputOption();
        cmd.AddOption(outputOption);

        Option<bool> verboseOption = CreateVerboseOption();
        cmd.AddOption(verboseOption);

        Option<long> minOption = CreateMinModifyTimeSpamOption();
        cmd.AddOption(minOption);

        Option<long> maxOption = CreateMaxModifyTimeSpamOption();
        cmd.AddOption(maxOption);

        Argument<string> dirArg = new("dir", "The target directory to analysis.")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        cmd.AddArgument(dirArg);

        cmd.SetHandler(async (context) =>
        {
            string dir = context.ParseResult.GetValueForArgument(dirArg);
            string output = context.ParseResult.GetValueForOption(outputOption);
            string[] includes = context.ParseResult.GetValueForOption(includeOption);
            string[] excludes = context.ParseResult.GetValueForOption(excludeOption);
            bool enableVerbose = context.ParseResult.GetValueForOption(verboseOption);
            long min = context.ParseResult.GetValueForOption(minOption);
            long max = context.ParseResult.GetValueForOption(maxOption);

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                await Console.Error.WriteLineAsync($"Destination director does not exist: {dir}");
                return;
            }

            DffOption option = new(output, includes, excludes, enableVerbose, _args)
            {
                Dir = Path.GetFullPath(dir),
                MinModifyTimeSpam = min,
                MaxModifyTimeSpam = max
            };

            await using DffHandler handler = new(option, _cancellationToken);
            _ = await handler.HandleAsync();
            context.ExitCode = 0;
        });

        cmd.SetHandler(async (string[] includes, string[] excludes, bool enableVerbose, string output,
            string dir, long min, long max) =>
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                await Console.Error.WriteLineAsync($"Destination director does not exist: {dir}");
                return;
            }

            DffOption option = new(output, includes, excludes, enableVerbose, _args)
            {
                Dir = Path.GetFullPath(dir),
                MinModifyTimeSpam = min,
                MaxModifyTimeSpam = max
            };

            await using DffHandler handler = new(option, _cancellationToken);
            _ = await handler.HandleAsync();
        }, includeOption, excludeOption, verboseOption, outputOption, dirArg, minOption, maxOption);

        return cmd;
    }

    private Command CreateRmCommand()
    {
        Command cmd = new("rm", "Remove files or directories.");
        Option<string[]> includeOption = CreateIncludeOption();
        cmd.AddOption(includeOption);

        Option<string[]> excludeOption = CreateExcludeOption();
        cmd.AddOption(excludeOption);

        Option<string> outputOption = CreateOutputOption();
        cmd.AddOption(outputOption);

        Option<bool> verboseOption = CreateVerboseOption();
        cmd.AddOption(verboseOption);

        Option<long> minOption = CreateMinModifyTimeSpamOption();
        cmd.AddOption(minOption);

        Option<long> maxOption = CreateMaxModifyTimeSpamOption();
        cmd.AddOption(maxOption);

        Option<bool> yesOption = new(new[] { "-y", "--yes" }, "Perform deletion without confirmation.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
        cmd.AddOption(yesOption);

        Option<bool> fileOption = new(new[] { "-f", "--file" }, "Delete files only.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
        cmd.AddOption(fileOption);

        Option<bool> dirOption = new(new[] { "-d", "--dir" }, "Delete directories only.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
        cmd.AddOption(dirOption);

        Option<bool> emptyDirOption = new(new[] { "--empty-dir" }, "Delete all empty directories.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
        cmd.AddOption(emptyDirOption);

        Option<string> fromFileOption =
            new(new[] { "--from-file" }, "Delete all files/directories listed in specified file.")
            {
                IsRequired = false,
                Arity = ArgumentArity.ZeroOrOne
            };
        cmd.AddOption(fromFileOption);

        Argument<string> destArg = new("dest", "The destination directory.")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        cmd.AddArgument(destArg);

        cmd.SetHandler(async (context) =>
            {
                var output = context.ParseResult.GetValueForOption(outputOption);
                var includes = context.ParseResult.GetValueForOption(includeOption);
                var excludes = context.ParseResult.GetValueForOption(excludeOption);
                var enableVerbose = context.ParseResult.GetValueForOption(verboseOption);
                var dest = context.ParseResult.GetValueForArgument(destArg);
                var file = context.ParseResult.GetValueForOption(fileOption);
                var dir = context.ParseResult.GetValueForOption(dirOption);
                var emptyDir = context.ParseResult.GetValueForOption(emptyDirOption);
                var yes = context.ParseResult.GetValueForOption(yesOption);
                var fromFile = context.ParseResult.GetValueForOption(fromFileOption);
                var min = context.ParseResult.GetValueForOption(minOption);
                var max = context.ParseResult.GetValueForOption(maxOption);

                if (string.IsNullOrEmpty(output))
                {
                    output = Path.GetTempPath();
                }

                _ = Directory.CreateDirectory(output);

                RmOption option = new(output, includes, excludes, enableVerbose, _args)
                {
                    Destination = Path.GetFullPath(dest),
                    File = file,
                    Dir = dir,
                    EmptyDir = emptyDir,
                    Yes = yes,
                    FromFile = fromFile,
                    MinModifyTimeSpam = min,
                    MaxModifyTimeSpam = max
                };

                await using RmHandler handler = new(option, _cancellationToken);
                _ = await handler.HandleAsync();
            });

        return cmd;
    }

    private Command CreateSyncCommand()
    {
        Command cmd = new("sync", "Synchronize files from source to destination directory.");
        Option<string[]> includeOption = CreateIncludeOption();
        cmd.AddOption(includeOption);

        Option<string[]> excludeOption = CreateExcludeOption();
        cmd.AddOption(excludeOption);

        Option<string> outputOption = CreateOutputOption();
        cmd.AddOption(outputOption);

        Option<bool> verboseOption = CreateVerboseOption();
        cmd.AddOption(verboseOption);

        Option<long> minOption = CreateMinModifyTimeSpamOption();
        cmd.AddOption(minOption);

        Option<long> maxOption = CreateMaxModifyTimeSpamOption();
        cmd.AddOption(maxOption);

        Option<bool> dryRunOption = new(new[] { "-n", "--dry-run" }, "Perform a trial run with no changes made.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
        cmd.AddOption(dryRunOption);

        Option<bool> deleteOption =
            new(new[] { "-d", "--delete" }, "Delete extraneous files from destination directory.")
            {
                IsRequired = false,
                Arity = ArgumentArity.ZeroOrOne
            };
        cmd.AddOption(deleteOption);

        Option<bool> sizeOnlyOption =
            new(new[] { "--size-only" }, "Skip files that match in both name and size.")
            {
                IsRequired = false,
                Arity = ArgumentArity.ZeroOrOne
            };
        cmd.AddOption(sizeOnlyOption);

        Argument<string> srcDirArg = new("src", "The source directory.")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        cmd.AddArgument(srcDirArg);

        Argument<string> destDirArg = new("dest", "The Destination directory.")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        cmd.AddArgument(destDirArg);

        cmd.SetHandler(async (context) =>
            {
                var output = context.ParseResult.GetValueForOption(outputOption);
                var includes = context.ParseResult.GetValueForOption(includeOption);
                var excludes = context.ParseResult.GetValueForOption(excludeOption);
                var enableVerbose = context.ParseResult.GetValueForOption(verboseOption);
                var delete = context.ParseResult.GetValueForOption(deleteOption);
                var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                var srcDir = context.ParseResult.GetValueForArgument(srcDirArg);
                var destDir = context.ParseResult.GetValueForArgument(destDirArg);
                var sizeOnly = context.ParseResult.GetValueForOption(sizeOnlyOption);
                var min = context.ParseResult.GetValueForOption(minOption);
                var max = context.ParseResult.GetValueForOption(maxOption);

                if (string.IsNullOrEmpty(output))
                {
                    output = Path.GetTempPath();
                }

                _ = Directory.CreateDirectory(output);

                SyncOption option = new(output, includes, excludes, enableVerbose, _args)
                {
                    Delete = delete,
                    DryRun = dryRun,
                    SrcDir = srcDir,
                    DestDir = destDir,
                    SizeOnly = sizeOnly,
                    MaxModifyTimeSpam = max,
                    MinModifyTimeSpam = min
                };

                await using SyncHandler handler = new(option, _cancellationToken);
                _ = await handler.HandleAsync();
            });

        return cmd;
    }

    private Option<string> CreateOutputOption()
    {
        return new Option<string>(new[] { "-o", "--output" },
            "The output directory for logs or any file generated during processing.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private System.CommandLine.Option<string[]> CreateIncludeOption()
    {
        return new Option<string[]>(new[] { "-i", "--include" }, "Glob patterns for included files.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrMore
        };
    }

    private System.CommandLine.Option<string[]> CreateExcludeOption()
    {
        return new Option<string[]>(new[] { "-e", "--exclude" }, "Glob patterns for excluded files.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private System.CommandLine.Option<bool> CreateVerboseOption()
    {
        return new Option<bool>(new[] { "-v", "--verbose" }, "Display detailed logs.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private System.CommandLine.Option<long> CreateMinModifyTimeSpamOption()
    {
        return new Option<long>(new[] { "--minT" }, "Min modify timespam filter")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private System.CommandLine.Option<long> CreateMaxModifyTimeSpamOption()
    {
        return new Option<long>(new[] { "--maxT" }, "Max modify timespam filter")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }
}