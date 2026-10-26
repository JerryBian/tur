using System;
using System.Diagnostics;
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
    }

    [Fact]
    public async Task Test_Case_12()
    {
        // SizeOnly skips same-name same-size files even when content differs.
        var srcBytes = GetRandomBytes(128);
        var destBytes = GetRandomBytes(128);
        _ = await MockFileAsync(_srcDir, srcBytes, "same.txt");
        _ = await MockFileAsync(_destDir, destBytes, "same.txt");

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            SizeOnly = true
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        Assert.Equal(destBytes, await File.ReadAllBytesAsync(Path.Combine(_destDir, "same.txt")));
    }

    [Fact]
    public async Task Test_Case_13()
    {
        // Without SizeOnly, same-size files with different content are overwritten.
        var srcBytes = GetRandomBytes(128);
        var destBytes = GetRandomBytes(128);
        _ = await MockFileAsync(_srcDir, srcBytes, "same.txt");
        _ = await MockFileAsync(_destDir, destBytes, "same.txt");

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        Assert.Equal(srcBytes, await File.ReadAllBytesAsync(Path.Combine(_destDir, "same.txt")));
    }

    [Fact]
    public async Task Test_Case_14()
    {
        // DryRun copies nothing but still creates the destination root.
        _ = await MockDirAsync(_srcDir, 1, 2, 1, 128);

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            DryRun = true
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        Assert.True(Directory.Exists(_destDir));
        Assert.Empty(Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetDirectories(_destDir, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Test_Case_15()
    {
        // Missing source directory returns a non-zero exit code.
        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = Path.Combine(_srcDir, GetRandomName()),
            DestDir = _destDir
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Test_Case_16()
    {
        // Delete removes extraneous files and empty directories from the destination.
        _ = await MockFileAsync(_srcDir, GetRandomBytes(64), "keep.txt");
        _ = await MockFileAsync(_destDir, GetRandomBytes(64), "keep.txt");
        _ = await MockFileAsync(_destDir, GetRandomBytes(64), "extra.txt");
        _ = MockSubDir(_destDir, "extra-dir");

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            Delete = true
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        var destFiles = Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories);
        _ = Assert.Single(destFiles);
        Assert.Equal(Path.Combine(_destDir, "keep.txt"), destFiles[0]);
        Assert.Empty(Directory.GetDirectories(_destDir, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Test_Case_17()
    {
        // A copy failure without --ignore-error aborts the rest and exits non-zero.
        _ = await MockFileAsync(_srcDir, GetRandomBytes(64), "conflict");
        _ = MockSubDir(_destDir, "conflict");

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Test_Case_18()
    {
        // --exclude-from merges patterns from a file (comments and blank lines ignored).
        _ = await MockFileAsync(_srcDir, fileName: "skip.me", fileLength: 10);
        _ = await MockFileAsync(_srcDir, fileName: "take.me", fileLength: 10);
        var excludeFile = Path.Combine(Path.GetTempPath(), GetRandomName());
        await File.WriteAllTextAsync(excludeFile, "# comment line\n\n**/skip.me\n");

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            ExcludeFrom = excludeFile
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(_destDir, "take.me")));
        Assert.False(File.Exists(Path.Combine(_destDir, "skip.me")));
        File.Delete(excludeFile);
    }

    [Fact]
    public async Task Test_Case_19()
    {
        // --delete-excluded removes excluded items from the destination even when present in source.
        _ = await MockFileAsync(_srcDir, GetRandomBytes(32), "keep.txt");
        _ = await MockFileAsync(_srcDir, GetRandomBytes(32), "drop.log");
        _ = await MockFileAsync(_destDir, GetRandomBytes(32), "keep.txt");
        _ = await MockFileAsync(_destDir, GetRandomBytes(96), "drop.log");

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            DeleteExcluded = true,
            Excludes = new[] { "**/*.log" }
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(_destDir, "keep.txt")));
        Assert.False(File.Exists(Path.Combine(_destDir, "drop.log")));
    }

    [Fact]
    public async Task Test_Case_20()
    {
        // --bwlimit paces the copy: 64KB at 32KB/s must take at least ~1s.
        _ = await MockFileAsync(_srcDir, GetRandomBytes(64 * 1024), "big.bin");

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            BwLimit = 32 * 1024
        };

        var sw = Stopwatch.StartNew();
        await using (SyncHandler handler = new(option, CancellationToken.None))
        {
            _ = await handler.HandleAsync();
        }

        sw.Stop();
        Assert.True(File.Exists(Path.Combine(_destDir, "big.bin")));
        Assert.True(sw.ElapsedMilliseconds >= 700, $"Throttled copy finished too fast: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Test_Case_21()
    {
        // --threads limits parallelism but must not change the result.
        _ = await MockDirAsync(_srcDir, 2, 2, 2, 128);

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            Threads = 2
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        Assert.Equal(Directory.GetFiles(_srcDir, "*", SearchOption.AllDirectories).Length,
            Directory.GetFiles(_destDir, "*", SearchOption.AllDirectories).Length);
        Assert.Equal(Directory.GetDirectories(_srcDir, "*", SearchOption.AllDirectories).Length,
            Directory.GetDirectories(_destDir, "*", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public async Task Test_Case_22()
    {
        // Without filters the delete phase reuses the copy-phase dest enumeration;
        // nested extraneous directories must still be removed children-first.
        _ = await MockFileAsync(_srcDir, GetRandomBytes(32), "keep.txt");
        _ = await MockFileAsync(_destDir, GetRandomBytes(32), "keep.txt");
        _ = await MockFileAsync(Path.Combine(_destDir, "extra", "nested"), GetRandomBytes(32), "inner.txt");
        _ = MockSubDir(Path.Combine(_destDir, "extra"), "empty-nested");

        SyncOption option = new(Array.Empty<string>())
        {
            SrcDir = _srcDir,
            DestDir = _destDir,
            Delete = true
        };

        await using SyncHandler handler = new(option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(_destDir, "keep.txt")));
        Assert.False(Directory.Exists(Path.Combine(_destDir, "extra")));
        Assert.Empty(Directory.GetDirectories(_destDir, "*", SearchOption.AllDirectories));
    }
}