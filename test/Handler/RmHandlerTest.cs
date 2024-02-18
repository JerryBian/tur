using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tur.Handler;
using Tur.Option;
using Xunit;

namespace Tur.Test.Handler;

public class RmHandlerTest : TestBase
{
    private readonly string _dir;
    private readonly RmOption _option;

    public RmHandlerTest()
    {
        _dir = Path.Combine(Path.GetTempPath(), GetRandomName());
        _ = Directory.CreateDirectory(_dir);
        _option = new RmOption(Array.Empty<string>());
    }

    [Fact]
    public async Task Test_Case_1()
    {
        _option.Destination = _dir;
        _ = await MockFileAsync(_dir);
        _ = MockSubDir(_dir, "test");
        _ = MockSubDir(_dir, "test2");
        _ = await MockFileAsync(Path.Combine(_dir, "test"));

        await using RmHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        Assert.True(!Directory.Exists(_dir));
    }

    [Fact]
    public async Task Test_Case_2()
    {
        _option.Destination = _dir;
        _option.File = true;
        _ = await MockFileAsync(_dir);
        _ = MockSubDir(_dir, "test");
        _ = MockSubDir(_dir, "test2");
        _ = await MockFileAsync(Path.Combine(_dir, "test"));

        await using RmHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        Assert.True(!Directory.GetFiles(_dir).Any());
        Assert.Equal(2, Directory.GetDirectories(_dir).Length);
    }

    [Fact]
    public async Task Test_Case_3()
    {
        _option.Destination = _dir;
        _option.Dir = true;
        _ = await MockFileAsync(_dir);
        _ = MockSubDir(_dir, "test");
        _ = MockSubDir(_dir, "test2");
        _ = await MockFileAsync(Path.Combine(_dir, "test"));

        await using RmHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        _ = Assert.Single(Directory.GetFiles(_dir));
        Assert.Empty(Directory.GetDirectories(_dir));
    }

    [Fact]
    public async Task Test_Case_4()
    {
        _option.Destination = _dir;
        _option.File = true;
        _option.Excludes = new[] { "**" };
        _ = await MockFileAsync(_dir);
        var file = await MockFileAsync(_dir);
        var deletionFileList = Path.GetTempFileName();
        await File.WriteAllTextAsync(deletionFileList, file, new UTF8Encoding(false));
        _option.FromFile = deletionFileList;
        _ = MockSubDir(_dir, "test");
        _ = MockSubDir(_dir, "test2");
        _ = await MockFileAsync(Path.Combine(_dir, "test"));

        await using RmHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        _ = Assert.Single(Directory.GetFiles(_dir));
        Assert.Equal(2, Directory.GetDirectories(_dir).Length);
    }

    [Fact]
    public async Task Test_Case_5()
    {
        _option.Destination = _dir;
        _option.File = true;
        _option.Excludes = new[] { "**" };
        _option.EmptyDir = true;
        _ = MockSubDir(_dir, "test");
        _ = MockSubDir(_dir, "test2");
        _ = await MockFileAsync(_dir);
        var file = await MockFileAsync(Path.Combine(_dir, "test2"));
        var deletionFileList = Path.GetTempFileName();
        await File.WriteAllTextAsync(deletionFileList, file, new UTF8Encoding(false));
        _option.FromFile = deletionFileList;

        _ = await MockFileAsync(Path.Combine(_dir, "test"));
        await using RmHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        _ = Assert.Single(Directory.GetFiles(_dir));
        Assert.True(Directory.GetDirectories(_dir).Count() == 2);
    }

    [Fact]
    public async Task Test_Case_6()
    {
        _option.Destination = _dir;
        _option.Includes = new[] { "**/hello*" };
        _ = MockSubDir(_dir, "hello1");
        _ = MockSubDir(_dir, "test2");
        _ = MockSubDir(Path.Combine(_dir, "test2"), "hello2");
        _ = await MockFileAsync(_dir);
        _ = await MockFileAsync(_dir, fileName: "hello3", fileLength: 26);
        await using RmHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        _ = Assert.Single(Directory.GetFiles(_dir, "*", SearchOption.AllDirectories));
        _ = Assert.Single(Directory.GetDirectories(_dir, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Test_Case_7()
    {
        _option.Destination = GetRandomName();
        _option.IgnoreError = true;
        await using RmHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Test_Case_8()
    {
        _option.Destination = _dir;
        _option.Includes = new[] { "**/hello*" };
        _option.Excludes = new[] { "**/hello1" };
        _ = MockSubDir(_dir, "hello1");
        _ = MockSubDir(_dir, "test2");
        _ = MockSubDir(Path.Combine(_dir, "test2"), "hello2");
        _ = await MockFileAsync(_dir);
        _ = await MockFileAsync(_dir, fileName: "hello3", fileLength: 26);
        await using RmHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        _ = Assert.Single(Directory.GetFiles(_dir, "*", SearchOption.AllDirectories));
        Assert.Equal(2, Directory.GetDirectories(_dir, "*", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public async Task Test_Case_9()
    {
        _option.FromFile = GetRandomName();
        await using RmHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Test_Case_10()
    {
        _option.Destination = _dir;
        _option.File = true;
        _option.IgnoreError = true;
        _option.Excludes = new[] { "**" };
        _option.EmptyDir = true;
        _ = MockSubDir(_dir, "test");
        _ = MockSubDir(_dir, "test2");
        _ = await MockFileAsync(_dir);
        var file = await MockFileAsync(Path.Combine(_dir, "test2"));
        var deletionFileList = Path.GetTempFileName();
        var dir1 = Path.Combine(Path.GetTempPath(), GetRandomName());
        var dir2 = Path.Combine(Path.GetTempPath(), GetRandomName());
        _ = Directory.CreateDirectory(dir2);
        await File.WriteAllLinesAsync(deletionFileList, new[] { file, dir2, Environment.NewLine, dir1 },
            new UTF8Encoding(false));
        _option.FromFile = deletionFileList;

        _ = await MockFileAsync(Path.Combine(_dir, "test"));
        await using RmHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        _ = Assert.Single(Directory.GetFiles(_dir));
        Assert.True(Directory.GetDirectories(_dir).Count() == 2);
        Assert.False(Directory.Exists(dir2));
    }

    [Fact]
    public async Task Test_Case_11()
    {
        _option.Destination = _dir;
        _option.EmptyDir = true;
        _ = MockSubDir(_dir, "test");
        _ = MockSubDir(_dir, "test2");
        _ = await MockFileAsync(_dir);
        await using RmHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        Assert.False(Directory.Exists(_dir));
    }

    public override void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}