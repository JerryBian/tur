using System.Collections.Generic;
using Tur.Core;
using Xunit;

namespace Tur.Test.Core;

public class TurFileSystemTest
{
    [Fact]
    public void Test_Case_1()
    {
        // Equality is based on FullPath only.
        TurFileSystem a = new() { FullPath = @"X:\tmp\a.txt", Length = 1 };
        TurFileSystem b = new() { FullPath = @"X:\tmp\a.txt", Length = 999, IsDirectory = true };
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Test_Case_2()
    {
        TurFileSystem a = new() { FullPath = @"X:\tmp\a.txt" };
        TurFileSystem b = new() { FullPath = @"X:\tmp\b.txt" };
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Test_Case_3()
    {
        TurFileSystem a = new() { FullPath = @"X:\tmp\a.txt" };
        Assert.False(a.Equals(null));
        Assert.False(a.Equals("X:\\tmp\\a.txt"));
    }

    [Fact]
    public void Test_Case_4()
    {
        // Hash-based collections deduplicate by FullPath.
        TurFileSystem a = new() { FullPath = @"X:\tmp\a.txt" };
        TurFileSystem b = new() { FullPath = @"X:\tmp\a.txt" };
        HashSet<TurFileSystem> set = new() { a, b };
        _ = Assert.Single(set);
    }
}
