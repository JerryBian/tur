using System.Threading.Tasks;

namespace Tur;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        MainService mainService = new(args);
        return await mainService.RunAsync();
    }
}