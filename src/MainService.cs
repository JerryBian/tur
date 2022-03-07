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

    public async Task RunAsync()
    {
        var rootCommand = new RootCommand("Root desc");
        rootCommand.AddAlias("tur");

        var dffCmd = CreateDffCommand();
        rootCommand.AddCommand(dffCmd);

        var syncCmd = CreateSyncCommand();
        rootCommand.AddCommand(syncCmd);

        await rootCommand.InvokeAsync(_args);
    }

    private Command CreateDffCommand()
    {
        var cmd = new Command("dff", "dff desc");
        var includeOption = CreateIncludeOption();
        cmd.AddOption(includeOption);

        var excludeOption = CreateExcludeOption();
        cmd.AddOption(excludeOption);

        var outputOption = CreateOutputOption();
        cmd.AddOption(outputOption);

        var verboseOption = CreateVerboseOption();
        cmd.AddOption(verboseOption);

        var recursiveOption = CreateRecursiveOption();
        cmd.AddOption(recursiveOption);

        var dirArg = new Argument<string>("dir", "dir desc")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        cmd.AddArgument(dirArg);

        cmd.SetHandler(async (string[] includes, string[] excludes, bool enableVerbose, string output,
            bool recursive, string dir) =>
        {
            if (string.IsNullOrEmpty(output))
            {
                output = Path.GetTempPath();
            }

            Directory.CreateDirectory(output);

            var option = new DffOption
            {
                Dir = Path.GetFullPath(dir),
                Includes = includes,
                Excludes = excludes,
                OutputDir = output, EnableVerbose = enableVerbose,
                Recursive = recursive,
                RawArgs = string.Join(" ", _args),
                CmdName = "dff"
            };

            await using var handler = new DffHandler(option, _cancellationToken);
            await handler.HandleAsync();
        }, includeOption, excludeOption, verboseOption, outputOption, recursiveOption, dirArg);

        return cmd;
    }

    private Command CreateSyncCommand()
    {
        var cmd = new Command("sync", "sync desc");
        var includeOption = CreateIncludeOption();
        cmd.AddOption(includeOption);

        var excludeOption = CreateExcludeOption();
        cmd.AddOption(excludeOption);

        var outputOption = CreateOutputOption();
        cmd.AddOption(outputOption);

        var verboseOption = CreateVerboseOption();
        cmd.AddOption(verboseOption);

        var recursiveOption = CreateRecursiveOption();
        cmd.AddOption(recursiveOption);

        var dryRunOption = new Option<bool>(new[] {"-n", "--dry-run"}, "dry run desc")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
        cmd.AddOption(dryRunOption);

        var deleteOption = new Option<bool>(new[] {"-d", "--delete"}, "delete desc")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
        cmd.AddOption(deleteOption);

        var srcDirArg = new Argument<string>("src", "src desc")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        cmd.AddArgument(srcDirArg);

        var destDirArg = new Argument<string>("dest", "dest desc")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        cmd.AddArgument(destDirArg);

        cmd.SetHandler(async (string[] includes, string[] excludes, bool enableVerbose, string output,
                bool recursive, bool delete, bool dryRun, string srcDir, string destDir) =>
            {
                if (string.IsNullOrEmpty(output))
                {
                    output = Path.GetTempPath();
                }

                Directory.CreateDirectory(output);

                var option = new SyncOption
                {
                    Delete = delete,
                    DryRun = dryRun,
                    SrcDir = srcDir,
                    DestDir = destDir,
                    Includes = includes,
                    Excludes = excludes,
                    OutputDir = output,
                    EnableVerbose = enableVerbose,
                    Recursive = recursive,
                    RawArgs = string.Join(" ", _args),
                    CmdName = "sync"
                };

                await using var handler = new SyncHandler(option, _cancellationToken);
                await handler.HandleAsync();
            }, includeOption, excludeOption, verboseOption, outputOption, recursiveOption, deleteOption, dryRunOption,
            srcDirArg, destDirArg);

        return cmd;
    }

    private System.CommandLine.Option CreateOutputOption()
    {
        return new Option<string>(new[] {"-o", "--output"}, "output desc")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private System.CommandLine.Option CreateIncludeOption()
    {
        return new Option<string[]>(new[] {"-i", "--include"}, "include desc")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrMore
        };
    }

    private System.CommandLine.Option CreateExcludeOption()
    {
        return new Option<string[]>(new[] {"-e", "--exclude"}, "exclude desc")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private System.CommandLine.Option CreateVerboseOption()
    {
        return new Option<bool>(new[] {"-v", "--verbose"}, "verbose desc")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private System.CommandLine.Option CreateRecursiveOption()
    {
        return new Option<bool>(new[] {"-r", "--recursive"}, "recursive desc")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrOne
        };
    }
}