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
        rootCommand.Aliases.Add("tur");

        var dffCmd = CreateDffCommand();
        rootCommand.Subcommands.Add(dffCmd);

        var syncCmd = CreateSyncCommand();
        rootCommand.Subcommands.Add(syncCmd);

        var rmCmd = CreateRmCommand();
        rootCommand.Subcommands.Add(rmCmd);

        return await rootCommand.Parse(_args).InvokeAsync();
    }

    private Command CreateDffCommand()
    {
        Command cmd = new("dff", "Duplicate files finder for target directories.");
        var includeOption = CreateIncludeOption();
        cmd.Options.Add(includeOption);

        var excludeOption = CreateExcludeOption();
        cmd.Options.Add(excludeOption);

        var outputOption = CreateOutputOption();
        cmd.Options.Add(outputOption);

        var lastModifyAfterOption = CreateLastModifyAfterOption();
        cmd.Options.Add(lastModifyAfterOption);

        var lastModifyBeforeOption = CreateLastModifyBeforeOption();
        cmd.Options.Add(lastModifyBeforeOption);

        var createAfterOption = CreateCreateAfterOption();
        cmd.Options.Add(createAfterOption);

        var createBeforeOption = CreateCreateBeforeOption();
        cmd.Options.Add(createBeforeOption);

        var ignoreOption = CreateIgnoreErrorOption();
        cmd.Options.Add(ignoreOption);

        var verboseOption = CreateVerboseOption();
        cmd.Options.Add(verboseOption);

        var noUserInteractionOption = CreateNoUserInteractionOption();
        cmd.Options.Add(noUserInteractionOption);

        var dirArg = new Argument<string[]>("dir")
        {
            Description = "The target directories to analysis.",
            Arity = ArgumentArity.OneOrMore
        };
        cmd.Arguments.Add(dirArg);

        cmd.SetAction(async (context, cancellationToken) =>
        {
            var dir = context.GetValue(dirArg);
            var output = context.GetValue(outputOption);
            var includes = context.GetValue(includeOption);
            var excludes = context.GetValue(excludeOption);
            var lastModifyAfter = context.GetValue(lastModifyAfterOption);
            var lastModifyBefore = context.GetValue(lastModifyBeforeOption);
            var createAfter = context.GetValue(createAfterOption);
            var createBefore = context.GetValue(createBeforeOption);
            var ignoreError = context.GetValue(ignoreOption);
            var verbose = context.GetValue(verboseOption);
            var noUserInteraction = context.GetValue(noUserInteractionOption);

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
                IgnoreError = ignoreError,
                Verbose = verbose,
                NoUserInteractive = noUserInteraction
            };

            await using DffHandler handler = new(option, cancellationToken);
            return await handler.HandleAsync();
        });

        return cmd;
    }

    private Command CreateRmCommand()
    {
        Command cmd = new("rm", "Remove files or directories.");
        var includeOption = CreateIncludeOption();
        cmd.Options.Add(includeOption);

        var excludeOption = CreateExcludeOption();
        cmd.Options.Add(excludeOption);

        var outputOption = CreateOutputOption();
        cmd.Options.Add(outputOption);

        var lastModifyAfterOption = CreateLastModifyAfterOption();
        cmd.Options.Add(lastModifyAfterOption);

        var lastModifyBeforeOption = CreateLastModifyBeforeOption();
        cmd.Options.Add(lastModifyBeforeOption);

        var verboseOption = CreateVerboseOption();
        cmd.Options.Add(verboseOption);

        var noUserInteractionOption = CreateNoUserInteractionOption();
        cmd.Options.Add(noUserInteractionOption);

        Option<bool> fileOption = new("-f", "--file" )
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Delete files only."
        };
        cmd.Options.Add(fileOption);

        Option<bool> dirOption = new("-d", "--dir")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Delete directories only."
        };
        cmd.Options.Add(dirOption);

        Option<bool> emptyDirOption = new("--empty-dir")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Delete empty directories only."
        };
        cmd.Options.Add(emptyDirOption);

        Option<string> fromFileOption =
            new("--from-file")
            {
                Required = false,
                Arity = ArgumentArity.ZeroOrOne,
                Description = "Delete all files/directories listed in specified file."
            };
        cmd.Options.Add(fromFileOption);

        Argument<string> destArg = new("dest")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "The destination directory."
        };
        cmd.Arguments.Add(destArg);

        var createAfterOption = CreateCreateAfterOption();
        cmd.Options.Add(createAfterOption);

        var dryRunOption = CreateDryRunOption();
        cmd.Options.Add(dryRunOption);

        var createBeforeOption = CreateCreateBeforeOption();
        cmd.Options.Add(createBeforeOption);

        var ignoreOption = CreateIgnoreErrorOption();
        cmd.Options.Add(ignoreOption);

        cmd.SetAction(async (context, cancellationToken) =>
            {
                var output = context.GetValue(outputOption);
                var includes = context.GetValue(includeOption);
                var excludes = context.GetValue(excludeOption);
                var dest = context.GetValue(destArg);
                var file = context.GetValue(fileOption);
                var dir = context.GetValue(dirOption);
                var emptyDir = context.GetValue(emptyDirOption);
                var fromFile = context.GetValue(fromFileOption);
                var lastModifyAfter = context.GetValue(lastModifyAfterOption);
                var lastModifyBefore = context.GetValue(lastModifyBeforeOption);
                var createAfter = context.GetValue(createAfterOption);
                var createBefore = context.GetValue(createBeforeOption);
                var ignoreError = context.GetValue(ignoreOption);
                var dryRun = context.GetValue(dryRunOption);
                var verbose = context.GetValue(verboseOption);
                var noUserInteraction = context.GetValue(noUserInteractionOption);

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
                    DryRun = dryRun,
                    Verbose = verbose,
                    NoUserInteractive = noUserInteraction
                };

                if (!string.IsNullOrEmpty(dest))
                {
                    option.Destination = Path.GetFullPath(dest);
                }

                await using RmHandler handler = new(option, cancellationToken);
                return await handler.HandleAsync();
            });

        return cmd;
    }

    private Command CreateSyncCommand()
    {
        Command cmd = new("sync", "Synchronize files from source to destination directory.");
        var includeOption = CreateIncludeOption();
        cmd.Options.Add(includeOption);

        var excludeOption = CreateExcludeOption();
        cmd.Options.Add(excludeOption);

        var outputOption = CreateOutputOption();
        cmd.Options.Add(outputOption);

        var lastModifyAfterOption = CreateLastModifyAfterOption();
        cmd.Options.Add(lastModifyAfterOption);

        var lastModifyBeforeOption = CreateLastModifyBeforeOption();
        cmd.Options.Add(lastModifyBeforeOption);

        var verboseOption = CreateVerboseOption();
        cmd.Options.Add(verboseOption);

        var noUserInteractionOption = CreateNoUserInteractionOption();
        cmd.Options.Add(noUserInteractionOption);

        var dryRunOption = CreateDryRunOption();
        cmd.Options.Add(dryRunOption);

        Option<bool> deleteOption =
            new("-d", "--delete")
            {
                Required = false,
                Arity = ArgumentArity.ZeroOrOne,
                Description = "Delete extraneous files from destination directory."
            };
        cmd.Options.Add(deleteOption);

        Option<bool> sizeOnlyOption =
            new("--size-only")
            {
                Required = false,
                Arity = ArgumentArity.ZeroOrOne,
                Description = "Skip files that match in both name and size."
            };
        cmd.Options.Add(sizeOnlyOption);

        Argument<string> srcDirArg = new("src")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "The source directory."
        };
        cmd.Arguments.Add(srcDirArg);

        Argument<string> destDirArg = new("dest")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "The destination directory."
        };
        cmd.Arguments.Add(destDirArg);

        var createAfterOption = CreateCreateAfterOption();
        cmd.Options.Add(createAfterOption);

        var createBeforeOption = CreateCreateBeforeOption();
        cmd.Options.Add(createBeforeOption);

        var ignoreOption = CreateIgnoreErrorOption();
        cmd.Options.Add(ignoreOption);

        cmd.SetAction(async (context, cancellationToken) =>
            {
                var output = context.GetValue(outputOption);
                var includes = context.GetValue(includeOption);
                var excludes = context.GetValue(excludeOption);
                var delete = context.GetValue(deleteOption);
                var dryRun = context.GetValue(dryRunOption);
                var srcDir = context.GetValue(srcDirArg);
                var destDir = context.GetValue(destDirArg);
                var sizeOnly = context.GetValue(sizeOnlyOption);
                var lastModifyAfter = context.GetValue(lastModifyAfterOption);
                var lastModifyBefore = context.GetValue(lastModifyBeforeOption);
                var createAfter = context.GetValue(createAfterOption);
                var createBefore = context.GetValue(createBeforeOption);
                var ignoreError = context.GetValue(ignoreOption);
                var verbose = context.GetValue(verboseOption);
                var noUserInteraction = context.GetValue(noUserInteractionOption);

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
                    IgnoreError = ignoreError,
                    Verbose = verbose,
                    NoUserInteractive = noUserInteraction
                };

                await using SyncHandler handler = new(option, cancellationToken);
                return await handler.HandleAsync();
            });

        return cmd;
    }

    private Option<string> CreateOutputOption()
    {
        Option<string> option = new("-o", "--output")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "The output directory for logs or any file generated during processing."
        };

        return option;
    }

    private Option<string[]> CreateIncludeOption()
    {
        return new Option<string[]>("-i", "--include")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrMore,
            Description = "Glob patterns for included files."
        };
    }

    private Option<string[]> CreateExcludeOption()
    {
        return new Option<string[]>("-e", "--exclude")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Glob patterns for excluded files."
        };
    }

    private Option<DateTime> CreateLastModifyAfterOption()
    {
        return new Option<DateTime>("--last-modify-after")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Last modify time after filter. e.g., 2022-10-01T10:20:21"
        };
    }

    private Option<DateTime> CreateLastModifyBeforeOption()
    {
        return new Option<DateTime>("--last-modify-before")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Last modify time before fitler. e.g., 2022-08-02T16:20:21"
        };
    }

    private Option<DateTime> CreateCreateAfterOption()
    {
        return new Option<DateTime>("--create-after")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Create time after filter. e.g., 2022-07-01T10:20:21"
        };
    }

    private Option<DateTime> CreateCreateBeforeOption()
    {
        return new Option<DateTime>("--create-before")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Create time before fitler. e.g., 2022-12-02T16:20:21"
        };
    }

    private Option<bool> CreateIgnoreErrorOption()
    {
        return new Option<bool>("--ignore-error")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Ignore errors during file processing."
        };
    }

    private Option<bool> CreateDryRunOption()
    {
        return new("-n", "--dry-run")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Perform a trial run with no changes made."
        };
    }

    private Option<bool> CreateVerboseOption()
    {
        return new("-v", "--verbose")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Enable logging in detailed mode."
        };
    }

    private Option<bool> CreateNoUserInteractionOption()
    {
        return new("--no-user-interaction")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Indicates running environment is not user interactive mode."
        };
    }
}