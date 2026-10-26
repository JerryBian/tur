using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Tur.Handler;
using Tur.Option;
using Tur.Util;

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
        var config = ConfigUtil.Load();
        RootCommand rootCommand = new("Command line tool to manage files.");

        var dffCmd = CreateDffCommand(config);
        rootCommand.Subcommands.Add(dffCmd);

        var syncCmd = CreateSyncCommand(config);
        rootCommand.Subcommands.Add(syncCmd);

        var rmCmd = CreateRmCommand(config);
        rootCommand.Subcommands.Add(rmCmd);

        return await rootCommand.Parse(_args).InvokeAsync();
    }

    private Command CreateDffCommand(TurConfig config)
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

        Option<string> writeListOption = new("--write-list")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Write duplicates to delete (all but the kept one per group) to the specified file, one path per line. Feed it to 'tur rm --from-file'."
        };
        cmd.Options.Add(writeListOption);

        Option<string> keepOption = new("--keep")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Which duplicate to keep with --write-list: first (default, alphabetical), newest, oldest."
        };
        cmd.Options.Add(keepOption);

        var minSizeOption = CreateMinSizeOption();
        cmd.Options.Add(minSizeOption);

        var maxSizeOption = CreateMaxSizeOption();
        cmd.Options.Add(maxSizeOption);

        var maxDepthOption = CreateMaxDepthOption();
        cmd.Options.Add(maxDepthOption);

        var threadsOption = CreateThreadsOption();
        cmd.Options.Add(threadsOption);

        var jsonOption = CreateJsonOption();
        cmd.Options.Add(jsonOption);

        var progressOption = CreateProgressOption();
        cmd.Options.Add(progressOption);

        var followLinksOption = CreateFollowLinksOption();
        cmd.Options.Add(followLinksOption);

        var dirArg = new Argument<string[]>("dir")
        {
            Description = "The target directories to analyze.",
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
            var writeList = context.GetValue(writeListOption);
            var keep = context.GetValue(keepOption);
            var maxDepth = context.GetValue(maxDepthOption);
            var threads = context.GetValue(threadsOption);
            var json = context.GetValue(jsonOption);
            var progress = context.GetValue(progressOption);
            var followLinks = context.GetValue(followLinksOption);

            if (!TryMergeSize(context.GetValue(minSizeOption), config.MinSize, "--min-size", out var minSize))
            {
                return 1;
            }

            if (!TryMergeSize(context.GetValue(maxSizeOption), config.MaxSize, "--max-size", out var maxSize))
            {
                return 1;
            }

            DffOption option = new(_args)
            {
                Dir = dir,
                WriteList = writeList,
                Keep = string.IsNullOrEmpty(keep) ? config.Keep : keep,
                LastModifyAfter = lastModifyAfter,
                LastModifyBefore = lastModifyBefore,
                CreateAfter = createAfter,
                CreateBefore = createBefore,
                OutputDir = string.IsNullOrEmpty(output) ? config.Output : output,
                Includes = includes ?? config.Includes,
                Excludes = excludes ?? config.Excludes,
                IgnoreError = MergeBool(context, ignoreOption, ignoreError, config.IgnoreError),
                Verbose = MergeBool(context, verboseOption, verbose, config.Verbose),
                NoUserInteractive = MergeBool(context, noUserInteractionOption, noUserInteraction, config.NoUserInteraction),
                Json = MergeBool(context, jsonOption, json, config.Json),
                Progress = MergeBool(context, progressOption, progress, config.Progress),
                FollowLinks = MergeBool(context, followLinksOption, followLinks, config.FollowLinks),
                MinSize = minSize,
                MaxSize = maxSize,
                MaxDepth = maxDepth ?? config.MaxDepth,
                Threads = threads ?? config.Threads
            };

            await using DffHandler handler = new(option, cancellationToken);
            return await handler.HandleAsync();
        });

        return cmd;
    }

    private Command CreateRmCommand(TurConfig config)
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

        Option<bool> trashOption = new("--trash")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Move items to the system trash/recycle bin instead of deleting permanently."
        };
        cmd.Options.Add(trashOption);

        Option<bool> yesOption = new("-y", "--yes")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Skip the confirmation prompt. Prompts never appear when input is redirected (subprocess safe)."
        };
        cmd.Options.Add(yesOption);

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

        var minSizeOption = CreateMinSizeOption();
        cmd.Options.Add(minSizeOption);

        var maxSizeOption = CreateMaxSizeOption();
        cmd.Options.Add(maxSizeOption);

        var maxDepthOption = CreateMaxDepthOption();
        cmd.Options.Add(maxDepthOption);

        var threadsOption = CreateThreadsOption();
        cmd.Options.Add(threadsOption);

        var jsonOption = CreateJsonOption();
        cmd.Options.Add(jsonOption);

        var progressOption = CreateProgressOption();
        cmd.Options.Add(progressOption);

        var followLinksOption = CreateFollowLinksOption();
        cmd.Options.Add(followLinksOption);

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
                var trash = context.GetValue(trashOption);
                var yes = context.GetValue(yesOption);
                var maxDepth = context.GetValue(maxDepthOption);
                var threads = context.GetValue(threadsOption);
                var json = context.GetValue(jsonOption);
                var progress = context.GetValue(progressOption);
                var followLinks = context.GetValue(followLinksOption);

                if (!TryMergeSize(context.GetValue(minSizeOption), config.MinSize, "--min-size", out var minSize))
                {
                    return 1;
                }

                if (!TryMergeSize(context.GetValue(maxSizeOption), config.MaxSize, "--max-size", out var maxSize))
                {
                    return 1;
                }

                if (string.IsNullOrEmpty(output))
                {
                    output = config.Output;
                }

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
                    Trash = MergeBool(context, trashOption, trash, config.Trash),
                    Yes = MergeBool(context, yesOption, yes, config.Yes),
                    LastModifyAfter = lastModifyAfter,
                    LastModifyBefore = lastModifyBefore,
                    CreateAfter = createAfter,
                    CreateBefore = createBefore,
                    OutputDir = output,
                    Includes = includes ?? config.Includes,
                    Excludes = excludes ?? config.Excludes,
                    IgnoreError = MergeBool(context, ignoreOption, ignoreError, config.IgnoreError),
                    DryRun = dryRun,
                    Verbose = MergeBool(context, verboseOption, verbose, config.Verbose),
                    NoUserInteractive = MergeBool(context, noUserInteractionOption, noUserInteraction, config.NoUserInteraction),
                    Json = MergeBool(context, jsonOption, json, config.Json),
                    Progress = MergeBool(context, progressOption, progress, config.Progress),
                    FollowLinks = MergeBool(context, followLinksOption, followLinks, config.FollowLinks),
                    MinSize = minSize,
                    MaxSize = maxSize,
                    MaxDepth = maxDepth ?? config.MaxDepth,
                    Threads = threads ?? config.Threads
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

    private Command CreateSyncCommand(TurConfig config)
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

        Option<string> excludeFromOption = new("--exclude-from")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Read exclude glob patterns from the specified file (one per line, blank lines and '#' comments ignored)."
        };
        cmd.Options.Add(excludeFromOption);

        Option<bool> deleteExcludedOption = new("--delete-excluded")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Also delete excluded files from the destination directory."
        };
        cmd.Options.Add(deleteExcludedOption);

        Option<string> bwLimitOption = new("--bwlimit")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Limit copy throughput per second, e.g. 10mb. Omitted or 0 means unlimited."
        };
        cmd.Options.Add(bwLimitOption);

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

        var minSizeOption = CreateMinSizeOption();
        cmd.Options.Add(minSizeOption);

        var maxSizeOption = CreateMaxSizeOption();
        cmd.Options.Add(maxSizeOption);

        var maxDepthOption = CreateMaxDepthOption();
        cmd.Options.Add(maxDepthOption);

        var threadsOption = CreateThreadsOption();
        cmd.Options.Add(threadsOption);

        var jsonOption = CreateJsonOption();
        cmd.Options.Add(jsonOption);

        var progressOption = CreateProgressOption();
        cmd.Options.Add(progressOption);

        var followLinksOption = CreateFollowLinksOption();
        cmd.Options.Add(followLinksOption);

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
                var excludeFrom = context.GetValue(excludeFromOption);
                var deleteExcluded = context.GetValue(deleteExcludedOption);
                var maxDepth = context.GetValue(maxDepthOption);
                var threads = context.GetValue(threadsOption);
                var json = context.GetValue(jsonOption);
                var progress = context.GetValue(progressOption);
                var followLinks = context.GetValue(followLinksOption);

                if (!TryMergeSize(context.GetValue(minSizeOption), config.MinSize, "--min-size", out var minSize))
                {
                    return 1;
                }

                if (!TryMergeSize(context.GetValue(maxSizeOption), config.MaxSize, "--max-size", out var maxSize))
                {
                    return 1;
                }

                if (!TryMergeSize(context.GetValue(bwLimitOption), config.BwLimit, "--bwlimit", out var bwLimit))
                {
                    return 1;
                }

                if (string.IsNullOrEmpty(output))
                {
                    output = config.Output;
                }

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
                    ExcludeFrom = excludeFrom,
                    DeleteExcluded = MergeBool(context, deleteExcludedOption, deleteExcluded, config.DeleteExcluded),
                    BwLimit = bwLimit ?? 0,
                    LastModifyAfter = lastModifyAfter,
                    LastModifyBefore = lastModifyBefore,
                    CreateAfter = createAfter,
                    CreateBefore = createBefore,
                    OutputDir = output,
                    Includes = includes ?? config.Includes,
                    Excludes = excludes ?? config.Excludes,
                    IgnoreError = MergeBool(context, ignoreOption, ignoreError, config.IgnoreError),
                    Verbose = MergeBool(context, verboseOption, verbose, config.Verbose),
                    NoUserInteractive = MergeBool(context, noUserInteractionOption, noUserInteraction, config.NoUserInteraction),
                    Json = MergeBool(context, jsonOption, json, config.Json),
                    Progress = MergeBool(context, progressOption, progress, config.Progress),
                    FollowLinks = MergeBool(context, followLinksOption, followLinks, config.FollowLinks),
                    MinSize = minSize,
                    MaxSize = maxSize,
                    MaxDepth = maxDepth ?? config.MaxDepth,
                    Threads = threads ?? config.Threads
                };

                await using SyncHandler handler = new(option, cancellationToken);
                return await handler.HandleAsync();
            });

        return cmd;
    }

    private static bool MergeBool(ParseResult context, Option<bool> option, bool cliValue, bool? configValue)
    {
        var provided = context.GetResult(option) != null;
        return ConfigUtil.MergeBool(provided, cliValue, configValue);
    }

    private static bool TryMergeSize(string cliText, string configText, string optionName, out long? size)
    {
        size = null;
        var text = string.IsNullOrEmpty(cliText) ? configText : cliText;
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        if (HumanUtil.TryParseSize(text, out var bytes))
        {
            size = bytes;
            return true;
        }

        Console.Error.WriteLine($"Invalid {optionName} value: '{text}'. Examples: 1024, 10mb, 1.5g.");
        return false;
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
            Description = "Last modify time before filter. e.g., 2022-08-02T16:20:21"
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
            Description = "Create time before filter. e.g., 2022-12-02T16:20:21"
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
        return new Option<bool>("-n", "--dry-run")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Perform a trial run with no changes made."
        };
    }

    private Option<bool> CreateVerboseOption()
    {
        return new Option<bool>("-v", "--verbose")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Enable logging in detailed mode."
        };
    }

    private Option<bool> CreateNoUserInteractionOption()
    {
        return new Option<bool>("--no-user-interaction")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Indicates running environment is not user interactive mode."
        };
    }

    private static Option<string> CreateMinSizeOption()
    {
        return new Option<string>("--min-size")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Only process files with size greater than or equal to this value. e.g., 1024, 10mb"
        };
    }

    private static Option<string> CreateMaxSizeOption()
    {
        return new Option<string>("--max-size")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Only process files with size less than or equal to this value. e.g., 1024, 10mb"
        };
    }

    private static Option<int?> CreateMaxDepthOption()
    {
        return new Option<int?>("--max-depth")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Do not descend into directories deeper than this level (direct children of the root = 1)."
        };
    }

    private static Option<int?> CreateThreadsOption()
    {
        return new Option<int?>("--threads")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Maximum degree of parallelism for file processing. Unlimited by default."
        };
    }

    private static Option<bool> CreateJsonOption()
    {
        return new Option<bool>("--json")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Write machine-readable NDJSON lines to stdout instead of human-readable output."
        };
    }

    private static Option<bool> CreateProgressOption()
    {
        return new Option<bool>("--progress")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Show a transient progress line (interactive terminals only; never written when output is redirected)."
        };
    }

    private static Option<bool> CreateFollowLinksOption()
    {
        return new Option<bool>("--follow-links")
        {
            Required = false,
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Follow symbolic links/junctions (loops are detected and skipped). Links are skipped by default."
        };
    }
}
