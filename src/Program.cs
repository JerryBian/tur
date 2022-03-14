using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Tur;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            cts.Cancel();
            eventArgs.Cancel = true;
        };

        var mainService = new MainService(args, cts.Token);
        await mainService.RunAsync();
    }
}