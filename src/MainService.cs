﻿using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Tur.Handler;
using Tur.Option;

namespace Tur;

public class MainService
{
    private readonly string[] _args;

    public MainService(string[] args)
    {
        _args = args;
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

        Option<DateTime> lastModifyAfterOption = CreateLastModifyAfterOption();
        cmd.AddOption(lastModifyAfterOption);

        Option<DateTime> lastModifyBeforeOption = CreateLastModifyBeforeOption();
        cmd.AddOption(lastModifyBeforeOption);

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
            DateTime lastModifyAfter = context.ParseResult.GetValueForOption(lastModifyAfterOption);
            DateTime lastModifyBefore = context.ParseResult.GetValueForOption(lastModifyBeforeOption);

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                await Console.Error.WriteLineAsync($"Destination director does not exist: {dir}");
                return;
            }

            DffOption option = new(output, includes, excludes, enableVerbose, _args)
            {
                Dir = Path.GetFullPath(dir),
                LastModifyAfter = lastModifyAfter,
                LastModifyBefore = lastModifyBefore
            };

            await using DffHandler handler = new(option, context.GetCancellationToken());
            context.ExitCode = await handler.HandleAsync();
        });

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

        Option<DateTime> lastModifyAfterOption = CreateLastModifyAfterOption();
        cmd.AddOption(lastModifyAfterOption);

        Option<DateTime> lastModifyBeforeOption = CreateLastModifyBeforeOption();
        cmd.AddOption(lastModifyBeforeOption);

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
                string output = context.ParseResult.GetValueForOption(outputOption);
                string[] includes = context.ParseResult.GetValueForOption(includeOption);
                string[] excludes = context.ParseResult.GetValueForOption(excludeOption);
                bool enableVerbose = context.ParseResult.GetValueForOption(verboseOption);
                string dest = context.ParseResult.GetValueForArgument(destArg);
                bool file = context.ParseResult.GetValueForOption(fileOption);
                bool dir = context.ParseResult.GetValueForOption(dirOption);
                bool emptyDir = context.ParseResult.GetValueForOption(emptyDirOption);
                bool yes = context.ParseResult.GetValueForOption(yesOption);
                string fromFile = context.ParseResult.GetValueForOption(fromFileOption);
                DateTime lastModifyAfter = context.ParseResult.GetValueForOption(lastModifyAfterOption);
                DateTime lastModifyBefore = context.ParseResult.GetValueForOption(lastModifyBeforeOption);

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
                    LastModifyAfter = lastModifyAfter,
                    LastModifyBefore = lastModifyBefore
                };

                await using RmHandler handler = new(option, context.GetCancellationToken());
                context.ExitCode = await handler.HandleAsync();
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

        Option<DateTime> lastModifyAfterOption = CreateLastModifyAfterOption();
        cmd.AddOption(lastModifyAfterOption);

        Option<DateTime> lastModifyBeforeOption = CreateLastModifyBeforeOption();
        cmd.AddOption(lastModifyBeforeOption);

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
                string output = context.ParseResult.GetValueForOption(outputOption);
                string[] includes = context.ParseResult.GetValueForOption(includeOption);
                string[] excludes = context.ParseResult.GetValueForOption(excludeOption);
                bool enableVerbose = context.ParseResult.GetValueForOption(verboseOption);
                bool delete = context.ParseResult.GetValueForOption(deleteOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                string srcDir = context.ParseResult.GetValueForArgument(srcDirArg);
                string destDir = context.ParseResult.GetValueForArgument(destDirArg);
                bool sizeOnly = context.ParseResult.GetValueForOption(sizeOnlyOption);
                DateTime lastModifyAfter = context.ParseResult.GetValueForOption(lastModifyAfterOption);
                DateTime lastModifyBefore = context.ParseResult.GetValueForOption(lastModifyBeforeOption);

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
                    LastModifyBefore = lastModifyBefore,
                    LastModifyAfter = lastModifyAfter
                };

                await using SyncHandler handler = new(option, context.GetCancellationToken());
                context.ExitCode = await handler.HandleAsync();
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

    private Option<string[]> CreateIncludeOption()
    {
        return new Option<string[]>(new[] { "-i", "--include" }, "Glob patterns for included files.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrMore
        };
    }

    private Option<string[]> CreateExcludeOption()
    {
        return new Option<string[]>(new[] { "-e", "--exclude" }, "Glob patterns for excluded files.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private Option<bool> CreateVerboseOption()
    {
        return new Option<bool>(new[] { "-v", "--verbose" }, "Display detailed logs.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private Option<DateTime> CreateLastModifyAfterOption()
    {
        return new Option<DateTime>(new[] { "--last-modify-after" }, "Last modify after filter. e.g., 2022-10-01T10:20:21")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private Option<DateTime> CreateLastModifyBeforeOption()
    {
        return new Option<DateTime>(new[] { "--last-modify-before" }, "Last modify before fitler. e.g., 2022-08-02T16:20:21")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }
}