using Tur.Extension;
using Xunit;

namespace Tur.Test.Extension;

public class NumberExtensionTest
{
    [Fact]
    public void Test_Case_1()
    {
        // Humanizer renders zero bytes with a lowercase "b" symbol.
        Assert.Equal("0 b", 0L.SizeHuman());
    }

    [Fact]
    public void Test_Case_2()
    {
        Assert.Equal("500 B", 500L.SizeHuman());
    }

    [Fact]
    public void Test_Case_3()
    {
        Assert.Equal("1 KB", 1024L.SizeHuman());
    }

    [Fact]
    public void Test_Case_4()
    {
        Assert.Equal("1 MB", (1024L * 1024L).SizeHuman());
    }

    [Fact]
    public void Test_Case_5()
    {
        Assert.Equal("2 KB", 2048.0.SizeHuman());
    }

    [Fact]
    public void Test_Case_6()
    {
        Assert.Equal("1 GB", (1024L * 1024L * 1024L).SizeHuman());
    }
}
