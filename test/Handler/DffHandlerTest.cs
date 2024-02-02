using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tur.Handler;
using Tur.Option;
using Xunit;

namespace Tur.Test.Handler;

public class DffHandlerTest : TestBase
{
    private readonly string _dir;
    private readonly DffOption _option;

    public DffHandlerTest()
    {
        _dir = Path.Combine(Path.GetTempPath(), GetRandomName());
        _ = Directory.CreateDirectory(_dir);
        _option = new DffOption(Array.Empty<string>()) { Dir = _dir };
    }

    [Fact]
    public async Task Test_Case_1()
    {
        _ = await MockFileAsync(_dir, fileLength: 10);
        _ = await MockFileAsync(_dir, fileLength: 20);
        await using DffHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Test_Case_2()
    {
        var bytes = GetRandomBytes(10);
        _ = await MockFileAsync(_dir, bytes);
        _ = await MockFileAsync(_dir, bytes);
        await using DffHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Test_Case_3()
    {
        var bytes = GetRandomBytes(10);
        var folder1 = "folder1";
        var file1 = "file1";
        var file2 = "file2";
        _ = MockSubDir(_dir, folder1);
        _ = await MockFileAsync(Path.Combine(_dir, folder1), bytes, file1);
        _ = await MockFileAsync(_dir, bytes, file2);
        _ = await MockDirAsync(_dir, 3, 4, 2, 20);

        await using DffHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Test_Case_4()
    {
        _option.Dir = Path.GetRandomFileName();
        await using DffHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        Assert.Equal(0, exitCode);
        Assert.False(Directory.GetFiles(_dir, "*.txt").Any());
    }

    public override void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}