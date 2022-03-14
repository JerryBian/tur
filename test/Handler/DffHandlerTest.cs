using System;
using System.Collections.Generic;
using System.IO;
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
        Directory.CreateDirectory(_dir);
        _option = new DffOption(null, null, null, true, Array.Empty<string>()) {Dir = _dir};
    }

    [Fact]
    public async Task Test_Case_1()
    {
        await MockFileAsync(_dir, fileLength: 10);
        await MockFileAsync(_dir, fileLength: 20);
        await using var handler = new DffHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        Assert.Null(_option.ExportedList);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Test_Case_2()
    {
        var bytes = GetRandomBytes(10);
        await MockFileAsync(_dir, bytes);
        await MockFileAsync(_dir, bytes);
        await using var handler = new DffHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        Assert.Null(_option.ExportedList);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Test_Case_3()
    {
        _option.ExportedList = new List<List<string>>();
        var bytes = GetRandomBytes(10);
        var folder1 = "folder1";
        var file1 = "file1";
        var file2 = "file2";
        MockSubDir(_dir, folder1);
        await MockFileAsync(Path.Combine(_dir, folder1), bytes, file1);
        await MockFileAsync(_dir, bytes, file2);
        await MockDirAsync(_dir, 3, 4, 2, 20);

        await using var handler = new DffHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        Assert.NotNull(_option.ExportedList);
        Assert.Equal(1, exitCode);
        Assert.Single(_option.ExportedList);
        Assert.Equal(2, _option.ExportedList[0].Count);
        Assert.Contains(Path.Combine(_dir, folder1, file1), _option.ExportedList[0]);
        Assert.Contains(Path.Combine(_dir, file2), _option.ExportedList[0]);
    }

    public override void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}