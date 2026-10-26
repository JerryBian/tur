using Tur.Util;
using Xunit;

namespace Tur.Test.Util;

public class HumanUtilTest
{
    [Fact]
    public void Test_Case_1()
    {
        Assert.Equal("0 b", HumanUtil.GetSize(0));
    }

    [Fact]
    public void Test_Case_2()
    {
        Assert.Equal("0 b", HumanUtil.GetSize(-100));
    }

    [Fact]
    public void Test_Case_3()
    {
        Assert.Equal("500 B", HumanUtil.GetSize(500));
    }

    [Fact]
    public void Test_Case_4()
    {
        Assert.Equal("1 KB", HumanUtil.GetSize(1024));
    }

    [Fact]
    public void Test_Case_5()
    {
        Assert.Equal("1 MB", HumanUtil.GetSize(1024 * 1024));
    }

    [Fact]
    public void Test_Case_6()
    {
        Assert.Equal("-", HumanUtil.GetRatesPerSecond(0, 1));
    }

    [Fact]
    public void Test_Case_7()
    {
        Assert.Equal("-", HumanUtil.GetRatesPerSecond(-1, 1));
    }

    [Fact]
    public void Test_Case_8()
    {
        Assert.Equal("-", HumanUtil.GetRatesPerSecond(1024, 0));
    }

    [Fact]
    public void Test_Case_9()
    {
        Assert.Equal("1 KB/s", HumanUtil.GetRatesPerSecond(2048, 2));
    }

    [Fact]
    public void Test_Case_10()
    {
        Assert.Equal("2 KB/s", HumanUtil.GetRatesPerSecond(1024, 0.5));
    }

    [Fact]
    public void Test_Case_11()
    {
        Assert.True(HumanUtil.TryParseSize("1024", out var bytes));
        Assert.Equal(1024, bytes);
        Assert.True(HumanUtil.TryParseSize("1kb", out bytes));
        Assert.Equal(1024, bytes);
        Assert.True(HumanUtil.TryParseSize(" 1.5 MB ", out bytes));
        Assert.Equal(1536 * 1024, bytes);
        Assert.True(HumanUtil.TryParseSize("2g", out bytes));
        Assert.Equal(2L * 1024 * 1024 * 1024, bytes);
        Assert.True(HumanUtil.TryParseSize("0", out bytes));
        Assert.Equal(0, bytes);
    }

    [Fact]
    public void Test_Case_12()
    {
        Assert.False(HumanUtil.TryParseSize(null, out _));
        Assert.False(HumanUtil.TryParseSize("", out _));
        Assert.False(HumanUtil.TryParseSize("abc", out _));
        Assert.False(HumanUtil.TryParseSize("-5", out _));
        Assert.False(HumanUtil.TryParseSize("5xb", out _));
        Assert.False(HumanUtil.TryParseSize("999999999999tb", out _));
    }
}
