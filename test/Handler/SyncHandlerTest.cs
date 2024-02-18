using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        _ = await MockDirAsync(_srcDir, 1, 1, 0, 128);

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        _ = await handler.HandleAsync();

        Assert.True(Directory.Exists(_destDir));
        _ = Assert.Single(Directory.GetDirectories(_srcDir, "*", SearchOption.AllDirectories));
        _ = Assert.Single(Directory.GetFiles(_srcDir, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Test_Case_2()
    {
        _ = await MockDirAsync(_srcDir, 2, 2, 2, 128);

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        _ = await handler.HandleAsync();

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
        _ = await MockFileAsync(dirTree.SubDirTrees[0].FullPath, fileName: "1.go");
        _ = await MockFileAsync(dirTree.SubDirTrees[0].SubDirTrees[2].FullPath, fileName: "2.js");

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            Includes = new[] { "**/*.go" },
            Excludes = new[] { "**/*.js" }
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        _ = await handler.HandleAsync();

        Assert.True(Directory.Exists(_destDir));
        _ = Assert.Single(Directory.GetDirectories(_destDir, "*", SearchOption.AllDirectories));
        _ = Assert.Single(Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Test_Case_4()
    {
        var dirTree = await MockDirAsync(_srcDir, 3, 5, 4, 128);
        _ = await MockFileAsync(dirTree.SubDirTrees[0].FullPath, fileName: "1.go");
        _ = await MockFileAsync(dirTree.SubDirTrees[0].SubDirTrees[2].FullPath, fileName: "2.js");
        _ = MockSubDir(Path.Combine(_destDir, "__1", "__2"));
        _ = await MockFileAsync(Path.Combine(_destDir, "1_2"));

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            Delete = true,
            Includes = new[] { "**/*.go" },
            Excludes = new[] { "**/*.js" }
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        _ = await handler.HandleAsync();

        Assert.True(Directory.Exists(_destDir));
        _ = Assert.Single(Directory.GetDirectories(_destDir, "*", SearchOption.AllDirectories));
        _ = Assert.Single(Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Test_Case_5()
    {
        var dirTree = await MockDirAsync(_srcDir, 3, 5, 4, 128);
        _ = await MockFileAsync(dirTree.SubDirTrees[0].FullPath, fileName: "1.go");
        _ = await MockFileAsync(dirTree.SubDirTrees[0].SubDirTrees[2].FullPath, fileName: "2.js");
        _ = MockSubDir(Path.Combine(_destDir, "__1", "__2"));
        _ = await MockFileAsync(Path.Combine(_destDir, "1_2"));
        _ = await MockFileAsync(Path.Combine(_destDir, "3_4"));

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            Delete = true,
            DryRun = true,
            Includes = new[] { "**/*.go" },
            Excludes = new[] { "**/*.js" }
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        _ = await handler.HandleAsync();

        Assert.True(Directory.Exists(_destDir));
        Assert.True(Directory.GetDirectories(_destDir, "*", SearchOption.AllDirectories).Count() > 1);
        Assert.True(Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories).Count() > 1);
    }

    [Fact]
    public async Task Test_Case_6()
    {
        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            LastModifyBefore = DateTime.Now.AddMinutes(-10)
        };
        _ = await MockFileAsync(_srcDir);

        await using SyncHandler handler = new(option, CancellationToken.None);
        _ = await handler.HandleAsync();
        Assert.True(Directory.Exists(_destDir));
    }

    [Fact]
    public async Task Test_Case_7()
    {
        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            LastModifyBefore = DateTime.Now.AddMinutes(1)
        };
        _ = await MockFileAsync(_srcDir);

        await using SyncHandler handler = new(option, CancellationToken.None);
        _ = await handler.HandleAsync();
        Assert.True(Directory.Exists(_destDir));
        _ = Assert.Single(Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Test_Case_8()
    {
        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            LastModifyAfter = DateTime.Now.AddMinutes(1)
        };
        _ = await MockFileAsync(_srcDir);

        await using SyncHandler handler = new(option, CancellationToken.None);
        _ = await handler.HandleAsync();
        Assert.True(Directory.Exists(_destDir));
    }

    [Fact]
    public async Task Test_Case_9()
    {
        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            LastModifyAfter = DateTime.Now.AddMinutes(-1)
        };
        _ = await MockFileAsync(_srcDir);

        await using SyncHandler handler = new(option, CancellationToken.None);
        _ = await handler.HandleAsync();
        Assert.True(Directory.Exists(_destDir));
        _ = Assert.Single(Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Test_Case_10()
    {
        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir
        };
        _ = await MockFileAsync(_srcDir);
        await Task.Delay(1000);

        await using SyncHandler handler = new(option, CancellationToken.None);
        _ = await handler.HandleAsync();
        Assert.True(Directory.Exists(_destDir));
        var destFiles = Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories);
        _ = Assert.Single(destFiles);
        var destFile = destFiles.First();
        var srcFile = Directory.GetFiles(_srcDir, "*", SearchOption.AllDirectories).First();
        Assert.Equal(File.GetLastWriteTime(srcFile), File.GetLastWriteTime(destFile));
        Assert.Equal(File.GetCreationTime(srcFile), File.GetCreationTime(destFile));
        Assert.NotEqual(File.GetLastAccessTime(srcFile), File.GetLastAccessTime(destFile));
    }

    [Fact]
    public async Task Test_Case_11()
    {
        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir
        };
        _ = await MockFileAsync(_srcDir);
        await Task.Delay(1000);

        await using SyncHandler handler = new(option, CancellationToken.None);
        _ = await handler.HandleAsync();
        Assert.True(Directory.Exists(_destDir));
        var destFiles = Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories);
        _ = Assert.Single(destFiles);
        var destFile = destFiles.First();
        var srcFile = Directory.GetFiles(_srcDir, "*", SearchOption.AllDirectories).First();
        Assert.Equal(File.GetLastWriteTime(srcFile), File.GetLastWriteTime(destFile));
        Assert.Equal(File.GetCreationTime(srcFile), File.GetCreationTime(destFile));
        Assert.NotEqual(File.GetLastAccessTime(srcFile), File.GetLastAccessTime(destFile));
    }
}