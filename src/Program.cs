using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tur;

public class Program
{
    public static async Task Main(string[] args)
    {
        using CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            cts.Cancel();
            eventArgs.Cancel = true;
        };

        MainService mainService = new(args, cts.Token);
        _ = await mainService.RunAsync();
    }
}