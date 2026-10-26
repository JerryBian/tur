using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tur.Util;
using Xunit;

namespace Tur.Test.Util;

public class FileUtilTest : TestBase
{
    private readonly string _dir;

    public FileUtilTest()
    {
        _dir = Path.Combine(Path.GetTempPath(), GetRandomName());
        _ = Directory.CreateDirectory(_dir);
    }

    [Fact]
    public async Task Test_Case_1()
    {
        // Same path is trivially the same file.
        var file = await MockFileAsync(_dir, fileLength: 64);
        Assert.True(await FileUtil.IsSameFileAsync(file, file, false));
    }

    [Fact]
    public async Task Test_Case_2()
    {
        // Same content in two distinct files.
        var bytes = GetRandomBytes(128);
        var file1 = await MockFileAsync(_dir, bytes, "f1.dat");
        var file2 = await MockFileAsync(_dir, bytes, "f2.dat");
        Assert.True(await FileUtil.IsSameFileAsync(file1, file2, false));
    }

    [Fact]
    public async Task Test_Case_3()
    {
        // Same length but different content.
        var file1 = await MockFileAsync(_dir, GetRandomBytes(128), "f1.dat");
        var file2 = await MockFileAsync(_dir, GetRandomBytes(128), "f2.dat");
        Assert.False(await FileUtil.IsSameFileAsync(file1, file2, false));
    }

    [Fact]
    public async Task Test_Case_4()
    {
        // Different length short-circuits to false.
        var file1 = await MockFileAsync(_dir, fileLength: 64);
        var file2 = await MockFileAsync(_dir, fileLength: 128);
        Assert.False(await FileUtil.IsSameFileAsync(file1, file2, false));
    }

    [Fact]
    public async Task Test_Case_5()
    {
        // Two empty files are considered the same.
        var file1 = await MockFileAsync(_dir, Array.Empty<byte>(), "e1.dat");
        var file2 = await MockFileAsync(_dir, Array.Empty<byte>(), "e2.dat");
        Assert.True(await FileUtil.IsSameFileAsync(file1, file2, false));
    }

    [Fact]
    public async Task Test_Case_6()
    {
        // Missing file with ignoreError returns false instead of throwing.
        var file1 = await MockFileAsync(_dir, fileLength: 64);
        var missing = Path.Combine(_dir, GetRandomName());
        Assert.False(await FileUtil.IsSameFileAsync(file1, missing, true));
    }

    [Fact]
    public async Task Test_Case_7()
    {
        // Missing file without ignoreError throws.
        var file1 = await MockFileAsync(_dir, fileLength: 64);
        var missing = Path.Combine(_dir, GetRandomName());
        await Assert.ThrowsAnyAsync<Exception>(() => FileUtil.IsSameFileAsync(file1, missing, false));
    }

    [Fact]
    public async Task Test_Case_8()
    {
        // File larger than the internal scan chunk exercises the multi-iteration compare path.
        var bytes = GetRandomBytes(3 * 1024 * 1024 + 17);
        var file1 = await MockFileAsync(_dir, bytes, "big1.dat");
        var file2 = await MockFileAsync(_dir, bytes, "big2.dat");
        Assert.True(await FileUtil.IsSameFileAsync(file1, file2, false));
    }

    [Fact]
    public async Task Test_Case_9()
    {
        // Large files differing only in the tail must not compare equal.
        var bytes = GetRandomBytes(3 * 1024 * 1024 + 17);
        var other = (byte[])bytes.Clone();
        other[^1] = (byte)(other[^1] + 1);
        var file1 = await MockFileAsync(_dir, bytes, "big1.dat");
        var file2 = await MockFileAsync(_dir, other, "big2.dat");
        Assert.False(await FileUtil.IsSameFileAsync(file1, file2, false));
    }

    [Fact]
    public async Task Test_Case_10()
    {
        // Partial hash covers only the head; full hash covers everything.
        var head = GetRandomBytes(8192);
        var bytes1 = head.Concat(GetRandomBytes(1024)).ToArray();
        var bytes2 = head.Concat(GetRandomBytes(1024)).ToArray();
        var file1 = await MockFileAsync(_dir, bytes1, "h1.dat");
        var file2 = await MockFileAsync(_dir, bytes2, "h2.dat");
        var file3 = await MockFileAsync(_dir, bytes1, "h3.dat");

        Assert.Equal(
            await FileUtil.GetHashAsync(file1, true, false, CancellationToken.None),
            await FileUtil.GetHashAsync(file2, true, false, CancellationToken.None));

        var full1 = await FileUtil.GetHashAsync(file1, false, false, CancellationToken.None);
        var full2 = await FileUtil.GetHashAsync(file2, false, false, CancellationToken.None);
        Assert.NotEqual(full1, full2);
        Assert.Equal(64, full1.Length);

        // Same content -> identical full hash.
        Assert.Equal(full1, await FileUtil.GetHashAsync(file3, false, false, CancellationToken.None));
    }

    [Fact]
    public async Task Test_Case_11()
    {
        // Unreadable file: null with ignoreError, throws without.
        var missing = Path.Combine(_dir, GetRandomName());
        Assert.Null(await FileUtil.GetHashAsync(missing, false, true, CancellationToken.None));
        await Assert.ThrowsAnyAsync<Exception>(() => FileUtil.GetHashAsync(missing, false, false, CancellationToken.None));
    }

    public override void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}
