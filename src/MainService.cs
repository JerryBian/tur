using System;
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

        Option<DateTime> lastModifyAfterOption = CreateLastModifyAfterOption();
        cmd.AddOption(lastModifyAfterOption);

        Option<DateTime> lastModifyBeforeOption = CreateLastModifyBeforeOption();
        cmd.AddOption(lastModifyBeforeOption);

        Option<DateTime> createAfterOption = CreateCreateAfterOption();
        cmd.AddOption(createAfterOption);

        Option<DateTime> createBeforeOption = CreateCreateBeforeOption();
        cmd.AddOption(createBeforeOption);

        Option<bool> ignoreOption = CreateIgnoreErrorOption();
        cmd.AddOption(ignoreOption);

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
            DateTime lastModifyAfter = context.ParseResult.GetValueForOption(lastModifyAfterOption);
            DateTime lastModifyBefore = context.ParseResult.GetValueForOption(lastModifyBeforeOption);
            DateTime createAfter = context.ParseResult.GetValueForOption(createAfterOption);
            DateTime createBefore = context.ParseResult.GetValueForOption(createBeforeOption);
            bool ignoreError = context.ParseResult.GetValueForOption(ignoreOption);

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                await Console.Error.WriteLineAsync($"Destination director does not exist: {dir}");
                return;
            }

            DffOption option = new(_args)
            {
                Dir = Path.GetFullPath(dir),
                LastModifyAfter = lastModifyAfter,
                LastModifyBefore = lastModifyBefore,
                CreateAfter = createAfter,
                CreateBefore = createBefore,
                OutputDir = output,
                Includes = includes,
                Excludes = excludes,
                IgnoreError = ignoreError
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

        Option<DateTime> lastModifyAfterOption = CreateLastModifyAfterOption();
        cmd.AddOption(lastModifyAfterOption);

        Option<DateTime> lastModifyBeforeOption = CreateLastModifyBeforeOption();
        cmd.AddOption(lastModifyBeforeOption);

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
            Arity = ArgumentArity.ZeroOrOne
        };
        cmd.AddArgument(destArg);

        Option<DateTime> createAfterOption = CreateCreateAfterOption();
        cmd.AddOption(createAfterOption);

        Option<DateTime> createBeforeOption = CreateCreateBeforeOption();
        cmd.AddOption(createBeforeOption);

        Option<bool> ignoreOption = CreateIgnoreErrorOption();
        cmd.AddOption(ignoreOption);

        cmd.SetHandler(async (context) =>
            {
                string output = context.ParseResult.GetValueForOption(outputOption);
                string[] includes = context.ParseResult.GetValueForOption(includeOption);
                string[] excludes = context.ParseResult.GetValueForOption(excludeOption);
                string dest = context.ParseResult.GetValueForArgument(destArg);
                bool file = context.ParseResult.GetValueForOption(fileOption);
                bool dir = context.ParseResult.GetValueForOption(dirOption);
                bool emptyDir = context.ParseResult.GetValueForOption(emptyDirOption);
                string fromFile = context.ParseResult.GetValueForOption(fromFileOption);
                DateTime lastModifyAfter = context.ParseResult.GetValueForOption(lastModifyAfterOption);
                DateTime lastModifyBefore = context.ParseResult.GetValueForOption(lastModifyBeforeOption);
                DateTime createAfter = context.ParseResult.GetValueForOption(createAfterOption);
                DateTime createBefore = context.ParseResult.GetValueForOption(createBeforeOption);
                bool ignoreError = context.ParseResult.GetValueForOption(ignoreOption);

                if (string.IsNullOrEmpty(output))
                {
                    output = Path.GetTempPath();
                }

                _ = Directory.CreateDirectory(output);

                RmOption option = new(_args)
                {
                    File = file,
                    Dir = dir,
                    EmptyDir = emptyDir,
                    FromFile = fromFile,
                    LastModifyAfter = lastModifyAfter,
                    LastModifyBefore = lastModifyBefore,
                    CreateAfter = createAfter,
                    CreateBefore = createBefore,
                    OutputDir = output,
                    Includes = includes,
                    Excludes = excludes,
                    IgnoreError = ignoreError
                };

                if (!string.IsNullOrEmpty(dest))
                {
                    option.Destination = Path.GetFullPath(dest);
                }

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

        Option<DateTime> createAfterOption = CreateCreateAfterOption();
        cmd.AddOption(createAfterOption);

        Option<DateTime> createBeforeOption = CreateCreateBeforeOption();
        cmd.AddOption(createBeforeOption);

        Option<bool> preserveCreateOption = new("--preserve-create", "Preserve creation time for destination file.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        cmd.AddOption(preserveCreateOption);

        Option<bool> preserveLastModifyOption = new("--preserve-last-modify", "Preserve last modify time for destination file.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        cmd.AddOption(preserveLastModifyOption);

        Option<bool> ignoreOption = CreateIgnoreErrorOption();
        cmd.AddOption(ignoreOption);

        cmd.SetHandler(async (context) =>
            {
                string output = context.ParseResult.GetValueForOption(outputOption);
                string[] includes = context.ParseResult.GetValueForOption(includeOption);
                string[] excludes = context.ParseResult.GetValueForOption(excludeOption);
                bool delete = context.ParseResult.GetValueForOption(deleteOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                string srcDir = context.ParseResult.GetValueForArgument(srcDirArg);
                string destDir = context.ParseResult.GetValueForArgument(destDirArg);
                bool sizeOnly = context.ParseResult.GetValueForOption(sizeOnlyOption);
                DateTime lastModifyAfter = context.ParseResult.GetValueForOption(lastModifyAfterOption);
                DateTime lastModifyBefore = context.ParseResult.GetValueForOption(lastModifyBeforeOption);
                DateTime createAfter = context.ParseResult.GetValueForOption(createAfterOption);
                DateTime createBefore = context.ParseResult.GetValueForOption(createBeforeOption);
                bool preserveCreate = context.ParseResult.GetValueForOption(preserveCreateOption);
                bool preserveLastModify = context.ParseResult.GetValueForOption(preserveLastModifyOption);
                bool ignoreError = context.ParseResult.GetValueForOption(ignoreOption);

                if (string.IsNullOrEmpty(output))
                {
                    output = Path.GetTempPath();
                }

                _ = Directory.CreateDirectory(output);

                SyncOption option = new(_args)
                {
                    Delete = delete,
                    DryRun = dryRun,
                    SrcDir = srcDir,
                    DestDir = destDir,
                    SizeOnly = sizeOnly,
                    PreserveCreateTime = preserveCreate,
                    PreserveLastModifyTime = preserveLastModify,
                    LastModifyAfter = lastModifyAfter,
                    LastModifyBefore = lastModifyBefore,
                    CreateAfter = createAfter,
                    CreateBefore = createBefore,
                    OutputDir = output,
                    Includes = includes,
                    Excludes = excludes,
                    IgnoreError = ignoreError
                };

                await using SyncHandler handler = new(option, context.GetCancellationToken());
                context.ExitCode = await handler.HandleAsync();
            });

        return cmd;
    }

    private Option<string> CreateOutputOption()
    {
        Option<string> option = new(new[] { "-o", "--output" },
            "The output directory for logs or any file generated during processing.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };

        return option;
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

    private Option<DateTime> CreateLastModifyAfterOption()
    {
        return new Option<DateTime>(new[] { "--last-modify-after" }, "Last modify time after filter. e.g., 2022-10-01T10:20:21")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private Option<DateTime> CreateLastModifyBeforeOption()
    {
        return new Option<DateTime>(new[] { "--last-modify-before" }, "Last modify time before fitler. e.g., 2022-08-02T16:20:21")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private Option<DateTime> CreateCreateAfterOption()
    {
        return new Option<DateTime>(new[] { "--create-after" }, "Create time after filter. e.g., 2022-07-01T10:20:21")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private Option<DateTime> CreateCreateBeforeOption()
    {
        return new Option<DateTime>(new[] { "--create-before" }, "Create time before fitler. e.g., 2022-12-02T16:20:21")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private Option<bool> CreateIgnoreErrorOption()
    {
        return new Option<bool>(new[] { "--ignore-error" }, "Ignore error during file processing.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }
}