using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tur.Test
{
    public class MainServiceTest
    {
        [Fact]
        public async Task Dff_UnSupported_Option_Fail()
        {
            var mainService = new MainService(new[] {"dff", "--whatever"}, CancellationToken.None);
            var result = await mainService.RunAsync();
            Assert.Equal(0, result);
        }
    }
}
