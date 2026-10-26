using System;
using Tur.Extension;
using Xunit;

namespace Tur.Test.Extension;

public class TimeSpanExtensionTest
{
    // The project references Humanizer.Core (English resources only),
    // so humanized output is deterministic regardless of machine culture.

    [Fact]
    public void Test_Case_1()
    {
        Assert.Equal("45 seconds", TimeSpan.FromSeconds(45).Human());
    }

    [Fact]
    public void Test_Case_2()
    {
        Assert.Equal("1 minute", TimeSpan.FromMinutes(1).Human());
    }

    [Fact]
    public void Test_Case_3()
    {
        Assert.Equal("2 hours", TimeSpan.FromHours(2).Human());
    }

    [Fact]
    public void Test_Case_4()
    {
        // maxUnit is Hour, so whole days are rendered as hours.
        Assert.Equal("24 hours", TimeSpan.FromDays(1).Human());
    }
}
