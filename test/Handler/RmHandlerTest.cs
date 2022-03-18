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
        Directory.CreateDirectory(_dir);
        _option = new RmOption(null, null, null, true, Array.Empty<string>()) {Yes = true};
    }

    [Fact]
    public async Task Test_Case_1()
    {
        _option.Destination = _dir;
        await MockFileAsync(_dir);
        MockSubDir(_dir, "test");
        MockSubDir(_dir, "test2");
        await MockFileAsync(Path.Combine(_dir, "test"));

        await using var handler = new RmHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(1, exitCode);
        Assert.True(!Directory.GetFiles(_dir).Any());
        Assert.True(!Directory.GetDirectories(_dir).Any());
    }

    [Fact]
    public async Task Test_Case_2()
    {
        _option.Destination = _dir;
        _option.File = true;
        await MockFileAsync(_dir);
        MockSubDir(_dir, "test");
        MockSubDir(_dir, "test2");
        await MockFileAsync(Path.Combine(_dir, "test"));

        await using var handler = new RmHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(1, exitCode);
        Assert.True(!Directory.GetFiles(_dir).Any());
        Assert.Equal(2, Directory.GetDirectories(_dir).Length);
    }

    [Fact]
    public async Task Test_Case_3()
    {
        _option.Destination = _dir;
        _option.Dir = true;
        await MockFileAsync(_dir);
        MockSubDir(_dir, "test");
        MockSubDir(_dir, "test2");
        await MockFileAsync(Path.Combine(_dir, "test"));

        await using var handler = new RmHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(1, exitCode);
        Assert.Single(Directory.GetFiles(_dir));
        Assert.Empty(Directory.GetDirectories(_dir));
    }

    [Fact]
    public async Task Test_Case_4()
    {
        _option.Destination = _dir;
        _option.File = true;
        _option.Excludes = new[] {"**"};
        await MockFileAsync(_dir);
        var file = await MockFileAsync(_dir);
        var deletionFileList = Path.GetTempFileName();
        await File.WriteAllTextAsync(deletionFileList, file, new UTF8Encoding(false));
        _option.FromFile = deletionFileList;
        MockSubDir(_dir, "test");
        MockSubDir(_dir, "test2");
        await MockFileAsync(Path.Combine(_dir, "test"));

        await using var handler = new RmHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(1, exitCode);
        Assert.Single(Directory.GetFiles(_dir));
        Assert.Equal(2, Directory.GetDirectories(_dir).Length);
    }

    [Fact]
    public async Task Test_Case_5()
    {
        _option.Destination = _dir;
        _option.File = true;
        _option.Excludes = new[] {"**"};
        _option.EmptyDir = true;
        MockSubDir(_dir, "test");
        MockSubDir(_dir, "test2");
        await MockFileAsync(_dir);
        var file = await MockFileAsync(Path.Combine(_dir, "test2"));
        var deletionFileList = Path.GetTempFileName();
        await File.WriteAllTextAsync(deletionFileList, file, new UTF8Encoding(false));
        _option.FromFile = deletionFileList;

        await MockFileAsync(Path.Combine(_dir, "test"));
        await using var handler = new RmHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(1, exitCode);
        Assert.Single(Directory.GetFiles(_dir));
        Assert.Single(Directory.GetDirectories(_dir));
    }

    [Fact]
    public async Task Test_Case_6()
    {
        _option.Destination = _dir;
        _option.Includes = new[] {"**/hello*"};
        MockSubDir(_dir, "hello1");
        MockSubDir(_dir, "test2");
        MockSubDir(Path.Combine(_dir, "test2"), "hello2");
        await MockFileAsync(_dir);
        await MockFileAsync(_dir, fileName: "hello3", fileLength: 26);
        await using var handler = new RmHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(1, exitCode);
        Assert.Single(Directory.GetFiles(_dir, "*", SearchOption.AllDirectories));
        Assert.Single(Directory.GetDirectories(_dir, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Test_Case_7()
    {
        _option.Destination = GetRandomName();
        await using var handler = new RmHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Test_Case_8()
    {
        _option.Destination = _dir;
        _option.Includes = new[] {"**/hello*"};
        _option.Excludes = new[] {"**/hello1"};
        MockSubDir(_dir, "hello1");
        MockSubDir(_dir, "test2");
        MockSubDir(Path.Combine(_dir, "test2"), "hello2");
        await MockFileAsync(_dir);
        await MockFileAsync(_dir, fileName: "hello3", fileLength: 26);
        await using var handler = new RmHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(1, exitCode);
        Assert.Single(Directory.GetFiles(_dir, "*", SearchOption.AllDirectories));
        Assert.Equal(2, Directory.GetDirectories(_dir, "*", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public async Task Test_Case_9()
    {
        _option.FromFile = GetRandomName();
        await using var handler = new RmHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Test_Case_10()
    {
        _option.Destination = _dir;
        _option.File = true;
        _option.Excludes = new[] {"**"};
        _option.EmptyDir = true;
        MockSubDir(_dir, "test");
        MockSubDir(_dir, "test2");
        await MockFileAsync(_dir);
        var file = await MockFileAsync(Path.Combine(_dir, "test2"));
        var deletionFileList = Path.GetTempFileName();
        var dir1 = Path.Combine(Path.GetTempPath(), GetRandomName());
        var dir2 = Path.Combine(Path.GetTempPath(), GetRandomName());
        Directory.CreateDirectory(dir2);
        await File.WriteAllLinesAsync(deletionFileList, new[] {file, dir2, Environment.NewLine, dir1},
            new UTF8Encoding(false));
        _option.FromFile = deletionFileList;

        await MockFileAsync(Path.Combine(_dir, "test"));
        await using var handler = new RmHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(1, exitCode);
        Assert.Single(Directory.GetFiles(_dir));
        Assert.Single(Directory.GetDirectories(_dir));
        Assert.False(Directory.Exists(dir2));
    }

    [Fact]
    public async Task Test_Case_11()
    {
        _option.Destination = _dir;
        _option.EmptyDir = true;
        MockSubDir(_dir, "test");
        MockSubDir(_dir, "test2");
        await MockFileAsync(_dir);
        await using var handler = new RmHandler(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(1, exitCode);
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