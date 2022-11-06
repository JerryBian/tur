using System;
using System.Collections.Generic;
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
        _option = new DffOption(null, null, null, true, Array.Empty<string>()) { Dir = _dir };
    }

    [Fact]
    public async Task Test_Case_1()
    {
        _ = await MockFileAsync(_dir, fileLength: 10);
        _ = await MockFileAsync(_dir, fileLength: 20);
        await using DffHandler handler = new(_option, CancellationToken.None);
        int exitCode = await handler.HandleAsync();
        Assert.Null(_option.ExportedList);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Test_Case_2()
    {
        byte[] bytes = GetRandomBytes(10);
        _ = await MockFileAsync(_dir, bytes);
        _ = await MockFileAsync(_dir, bytes);
        await using DffHandler handler = new(_option, CancellationToken.None);
        int exitCode = await handler.HandleAsync();
        Assert.Null(_option.ExportedList);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Test_Case_3()
    {
        _option.ExportedList = new List<List<string>>();
        byte[] bytes = GetRandomBytes(10);
        string folder1 = "folder1";
        string file1 = "file1";
        string file2 = "file2";
        _ = MockSubDir(_dir, folder1);
        _ = await MockFileAsync(Path.Combine(_dir, folder1), bytes, file1);
        _ = await MockFileAsync(_dir, bytes, file2);
        _ = await MockDirAsync(_dir, 3, 4, 2, 20);

        await using DffHandler handler = new(_option, CancellationToken.None);
        int exitCode = await handler.HandleAsync();
        Assert.NotNull(_option.ExportedList);
        Assert.Equal(1, exitCode);
        _ = Assert.Single(_option.ExportedList);
        Assert.Equal(2, _option.ExportedList[0].Count);
        Assert.Contains(Path.Combine(_dir, folder1, file1), _option.ExportedList[0]);
        Assert.Contains(Path.Combine(_dir, file2), _option.ExportedList[0]);
    }

    [Fact]
    public async Task Test_Case_4()
    {
        _option.Dir = Path.GetRandomFileName();
        await using DffHandler handler = new(_option, CancellationToken.None);
        int exitCode = await handler.HandleAsync();
        Assert.Equal(1, exitCode);
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