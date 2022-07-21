using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tur.Extension;
using Tur.Handler;
using Tur.Option;
using Xunit;

namespace Tur.Test.Handler;

public class SyncHandlerTest : TestBase
{
    private readonly string _destDir;
    private readonly string _srcDir;

    public SyncHandlerTest()
    {
        _srcDir = Path.Combine(Path.GetTempPath(), GetRandomName());
        _destDir = Path.Combine(Path.GetTempPath(), GetRandomName());
    }

    public override void Dispose()
    {
        if (Directory.Exists(_srcDir))
        {
            Directory.Delete(_srcDir, true);
        }

        if (Directory.Exists(_destDir))
        {
            Directory.Delete(_destDir, true);
        }
    }

    [Fact]
    public async Task Test_Case_1()
    {
        await MockDirAsync(_srcDir, 1, 1, 0, 128);

        var option = new SyncOption(null, null, null, false, Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir
        };

        await using var handler = new SyncHandler(option, CancellationToken.None);
        await handler.HandleAsync();

        Assert.True(Directory.Exists(_destDir));
        Assert.Single(Directory.GetDirectories(_srcDir, "*", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(_srcDir, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Test_Case_2()
    {
        await MockDirAsync(_srcDir, 2, 2, 2, 128);

        var option = new SyncOption(null, null, null, false, Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir
        };

        await using var handler = new SyncHandler(option, CancellationToken.None);
        await handler.HandleAsync();

        Assert.True(Directory.Exists(_destDir));
        Assert.Equal(Directory.GetDirectories(_srcDir, "*", SearchOption.AllDirectories).Length,
            Directory.GetDirectories(_destDir, "*", SearchOption.AllDirectories).Length);
        Assert.Equal(Directory.GetFiles(_srcDir, "*", SearchOption.AllDirectories).Length,
            Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public async Task Test_Case_3()
    {
        var dirTree = await MockDirAsync(_srcDir, 3, 5, 4, 128);
        await MockFileAsync(dirTree.SubDirTrees[0].FullPath, fileName: "1.go");
        await MockFileAsync(dirTree.SubDirTrees[0].SubDirTrees[2].FullPath, fileName: "2.js");

        var option = new SyncOption(null, new[] {"**/*.go"}, new[] {"**/*.js"}, false, Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir
        };

        await using var handler = new SyncHandler(option, CancellationToken.None);
        await handler.HandleAsync();

        Assert.True(Directory.Exists(_destDir));
        Assert.Equal(Directory.GetDirectories(_srcDir, "*", SearchOption.AllDirectories).Length,
            Directory.GetDirectories(_destDir, "*", SearchOption.AllDirectories).Length);
        Assert.Single(Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Test_Case_4()
    {
        var dirTree = await MockDirAsync(_srcDir, 3, 5, 4, 128);
        await MockFileAsync(dirTree.SubDirTrees[0].FullPath, fileName: "1.go");
        await MockFileAsync(dirTree.SubDirTrees[0].SubDirTrees[2].FullPath, fileName: "2.js");
        MockSubDir(Path.Combine(_destDir, "__1", "__2"));
        await MockFileAsync(Path.Combine(_destDir, "1_2"));

        var option = new SyncOption(null, new[] {"**/*.go"}, new[] {"**/*.js"}, false, Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            Delete = true
        };

        await using var handler = new SyncHandler(option, CancellationToken.None);
        await handler.HandleAsync();

        Assert.True(Directory.Exists(_destDir));
        Assert.Equal(Directory.GetDirectories(_srcDir, "*", SearchOption.AllDirectories).Length,
            Directory.GetDirectories(_destDir, "*", SearchOption.AllDirectories).Length);
        Assert.Single(Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Test_Case_5()
    {
        var dirTree = await MockDirAsync(_srcDir, 3, 5, 4, 128);
        await MockFileAsync(dirTree.SubDirTrees[0].FullPath, fileName: "1.go");
        await MockFileAsync(dirTree.SubDirTrees[0].SubDirTrees[2].FullPath, fileName: "2.js");
        MockSubDir(Path.Combine(_destDir, "__1", "__2"));
        await MockFileAsync(Path.Combine(_destDir, "1_2"));
        await MockFileAsync(Path.Combine(_destDir, "3_4"));

        var option = new SyncOption(null, new[] {"**/*.go"}, new[] {"**/*.js"}, false, Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            Delete = true,
            DryRun = true
        };

        await using var handler = new SyncHandler(option, CancellationToken.None);
        await handler.HandleAsync();

        Assert.True(Directory.Exists(_destDir));
        Assert.Equal(5,
            Directory.GetDirectories(_destDir, "*", SearchOption.AllDirectories).Length);
        Assert.Equal(2, Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public async Task Test_Case_6()
    {
        var option = new SyncOption(null, null, null, false, Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir
        };
        option.MaxModifyTimeSpam = DateTime.Now.AddMinutes(-1).ToLongTimeSpan();
        await MockFileAsync(_srcDir);

        await using var handler = new SyncHandler(option, CancellationToken.None);
        await handler.HandleAsync();
        Assert.False(Directory.Exists(_destDir));
    }

    [Fact]
    public async Task Test_Case_7()
    {
        var option = new SyncOption(null, null, null, false, Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir
        };
        option.MaxModifyTimeSpam = DateTime.Now.AddMinutes(1).ToLongTimeSpan();
        await MockFileAsync(_srcDir);

        await using var handler = new SyncHandler(option, CancellationToken.None);
        await handler.HandleAsync();
        Assert.True(Directory.Exists(_destDir));
        Assert.Single(Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Test_Case_8()
    {
        var option = new SyncOption(null, null, null, false, Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir
        };
        option.MinModifyTimeSpam = DateTime.Now.AddMinutes(1).ToLongTimeSpan();
        await MockFileAsync(_srcDir);

        await using var handler = new SyncHandler(option, CancellationToken.None);
        await handler.HandleAsync();
        Assert.False(Directory.Exists(_destDir));
    }

    [Fact]
    public async Task Test_Case_9()
    {
        var option = new SyncOption(null, null, null, false, Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir
        };
        option.MinModifyTimeSpam = DateTime.Now.AddMinutes(-1).ToLongTimeSpan();
        await MockFileAsync(_srcDir);

        await using var handler = new SyncHandler(option, CancellationToken.None);
        await handler.HandleAsync();
        Assert.True(Directory.Exists(_destDir));
        Assert.Single(Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories));
    }
}