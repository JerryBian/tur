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

        var dffCmd = CreateDffCommand();
        rootCommand.AddCommand(dffCmd);

        var syncCmd = CreateSyncCommand();
        rootCommand.AddCommand(syncCmd);

        var rmCmd = CreateRmCommand();
        rootCommand.AddCommand(rmCmd);

        return await rootCommand.InvokeAsync(_args);
    }

    private Command CreateDffCommand()
    {
        Command cmd = new("dff", "Duplicate files finder for target directories.");
        var includeOption = CreateIncludeOption();
        cmd.AddOption(includeOption);

        var excludeOption = CreateExcludeOption();
        cmd.AddOption(excludeOption);

        var outputOption = CreateOutputOption();
        cmd.AddOption(outputOption);

        var lastModifyAfterOption = CreateLastModifyAfterOption();
        cmd.AddOption(lastModifyAfterOption);

        var lastModifyBeforeOption = CreateLastModifyBeforeOption();
        cmd.AddOption(lastModifyBeforeOption);

        var createAfterOption = CreateCreateAfterOption();
        cmd.AddOption(createAfterOption);

        var createBeforeOption = CreateCreateBeforeOption();
        cmd.AddOption(createBeforeOption);

        var ignoreOption = CreateIgnoreErrorOption();
        cmd.AddOption(ignoreOption);

        Argument<string[]> dirArg = new("dir", "The target directories to analysis.")
        {
            Arity = ArgumentArity.OneOrMore
        };
        cmd.AddArgument(dirArg);

        cmd.SetHandler(async (context) =>
        {
            var dir = context.ParseResult.GetValueForArgument(dirArg);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var includes = context.ParseResult.GetValueForOption(includeOption);
            var excludes = context.ParseResult.GetValueForOption(excludeOption);
            var lastModifyAfter = context.ParseResult.GetValueForOption(lastModifyAfterOption);
            var lastModifyBefore = context.ParseResult.GetValueForOption(lastModifyBeforeOption);
            var createAfter = context.ParseResult.GetValueForOption(createAfterOption);
            var createBefore = context.ParseResult.GetValueForOption(createBeforeOption);
            var ignoreError = context.ParseResult.GetValueForOption(ignoreOption);

            DffOption option = new(_args)
            {
                Dir = dir,
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
        var includeOption = CreateIncludeOption();
        cmd.AddOption(includeOption);

        var excludeOption = CreateExcludeOption();
        cmd.AddOption(excludeOption);

        var outputOption = CreateOutputOption();
        cmd.AddOption(outputOption);

        var lastModifyAfterOption = CreateLastModifyAfterOption();
        cmd.AddOption(lastModifyAfterOption);

        var lastModifyBeforeOption = CreateLastModifyBeforeOption();
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

        var createAfterOption = CreateCreateAfterOption();
        cmd.AddOption(createAfterOption);

        var dryRunOption = CreateDryRunOption();
        cmd.AddOption(dryRunOption);

        var createBeforeOption = CreateCreateBeforeOption();
        cmd.AddOption(createBeforeOption);

        var ignoreOption = CreateIgnoreErrorOption();
        cmd.AddOption(ignoreOption);

        cmd.SetHandler(async (context) =>
            {
                var output = context.ParseResult.GetValueForOption(outputOption);
                var includes = context.ParseResult.GetValueForOption(includeOption);
                var excludes = context.ParseResult.GetValueForOption(excludeOption);
                var dest = context.ParseResult.GetValueForArgument(destArg);
                var file = context.ParseResult.GetValueForOption(fileOption);
                var dir = context.ParseResult.GetValueForOption(dirOption);
                var emptyDir = context.ParseResult.GetValueForOption(emptyDirOption);
                var fromFile = context.ParseResult.GetValueForOption(fromFileOption);
                var lastModifyAfter = context.ParseResult.GetValueForOption(lastModifyAfterOption);
                var lastModifyBefore = context.ParseResult.GetValueForOption(lastModifyBeforeOption);
                var createAfter = context.ParseResult.GetValueForOption(createAfterOption);
                var createBefore = context.ParseResult.GetValueForOption(createBeforeOption);
                var ignoreError = context.ParseResult.GetValueForOption(ignoreOption);
                var dryRun = context.ParseResult.GetValueForOption(dryRunOption);

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
                    IgnoreError = ignoreError,
                    DryRun = dryRun
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
        var includeOption = CreateIncludeOption();
        cmd.AddOption(includeOption);

        var excludeOption = CreateExcludeOption();
        cmd.AddOption(excludeOption);

        var outputOption = CreateOutputOption();
        cmd.AddOption(outputOption);

        var lastModifyAfterOption = CreateLastModifyAfterOption();
        cmd.AddOption(lastModifyAfterOption);

        var lastModifyBeforeOption = CreateLastModifyBeforeOption();
        cmd.AddOption(lastModifyBeforeOption);

        var dryRunOption = CreateDryRunOption();
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

        Argument<string> destDirArg = new("dest", "The destination directory.")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        cmd.AddArgument(destDirArg);

        var createAfterOption = CreateCreateAfterOption();
        cmd.AddOption(createAfterOption);

        var createBeforeOption = CreateCreateBeforeOption();
        cmd.AddOption(createBeforeOption);

        var ignoreOption = CreateIgnoreErrorOption();
        cmd.AddOption(ignoreOption);

        cmd.SetHandler(async (context) =>
            {
                var output = context.ParseResult.GetValueForOption(outputOption);
                var includes = context.ParseResult.GetValueForOption(includeOption);
                var excludes = context.ParseResult.GetValueForOption(excludeOption);
                var delete = context.ParseResult.GetValueForOption(deleteOption);
                var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                var srcDir = context.ParseResult.GetValueForArgument(srcDirArg);
                var destDir = context.ParseResult.GetValueForArgument(destDirArg);
                var sizeOnly = context.ParseResult.GetValueForOption(sizeOnlyOption);
                var lastModifyAfter = context.ParseResult.GetValueForOption(lastModifyAfterOption);
                var lastModifyBefore = context.ParseResult.GetValueForOption(lastModifyBeforeOption);
                var createAfter = context.ParseResult.GetValueForOption(createAfterOption);
                var createBefore = context.ParseResult.GetValueForOption(createBeforeOption);
                var ignoreError = context.ParseResult.GetValueForOption(ignoreOption);

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

    private Option<bool> CreateDryRunOption()
    {
        return new(new[] { "-n", "--dry-run" }, "Perform a trial run with no changes made.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }
}