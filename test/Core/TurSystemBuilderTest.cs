using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tur.Core;
using Xunit;

namespace Tur.Test.Core;

public class TurSystemBuilderTest : TestBase
{
    private readonly string _dir;

    public TurSystemBuilderTest()
    {
        _dir = Path.Combine(Path.GetTempPath(), GetRandomName());
        _ = Directory.CreateDirectory(_dir);
    }

    [Fact]
    public async Task Test_Case_1()
    {
        // Files-only enumeration reports relative paths and sizes.
        var sub = MockSubDir(_dir, "sub");
        var file1 = await MockFileAsync(_dir, fileName: "a.txt", fileLength: 10);
        var file2 = await MockFileAsync(sub, fileName: "b.txt", fileLength: 20);

        TurBuildOptions options = new() { IncludeFiles = true, IncludeFileSize = true };
        TurSystemBuilder builder = new(_dir, options, CancellationToken.None);
        var items = builder.Build().ToList();

        Assert.Equal(2, items.Count);
        Assert.All(items, x => Assert.False(x.IsDirectory));

        var a = items.Single(x => x.RelativePath == "a.txt");
        Assert.Equal(file1, a.FullPath);
        Assert.Equal(10, a.Length);

        var b = items.Single(x => x.RelativePath == Path.Combine("sub", "b.txt"));
        Assert.Equal(file2, b.FullPath);
        Assert.Equal(20, b.Length);
    }

    [Fact]
    public async Task Test_Case_2()
    {
        // Directories-only enumeration, children yielded before parents, root never yielded.
        var sub = MockSubDir(_dir, "sub");
        var nested = MockSubDir(sub, "nested");
        _ = await MockFileAsync(_dir, fileName: "a.txt");

        TurBuildOptions options = new() { IncludeDirectories = true };
        TurSystemBuilder builder = new(_dir, options, CancellationToken.None);
        var items = builder.Build().ToList();

        Assert.Equal(2, items.Count);
        Assert.All(items, x => Assert.True(x.IsDirectory));
        Assert.Equal(nested, items[0].FullPath);
        Assert.Equal(sub, items[1].FullPath);
        Assert.DoesNotContain(items, x => x.FullPath == _dir);
    }

    [Fact]
    public void Test_Case_3()
    {
        // Neither files nor directories requested -> empty.
        TurBuildOptions options = new();
        TurSystemBuilder builder = new(_dir, options, CancellationToken.None);
        Assert.Empty(builder.Build());
    }

    [Fact]
    public async Task Test_Case_4()
    {
        // Include/exclude glob patterns combine.
        _ = await MockFileAsync(_dir, fileName: "a.txt");
        _ = await MockFileAsync(_dir, fileName: "b.log");
        _ = await MockFileAsync(_dir, fileName: "c.txt");

        TurBuildOptions options = new() { IncludeFiles = true };
        options.IncludeGlobPatterns.Add("**/*.txt");
        options.ExcludeGlobPatterns.Add("**/c.txt");
        TurSystemBuilder builder = new(_dir, options, CancellationToken.None);
        var items = builder.Build().ToList();

        _ = Assert.Single(items);
        Assert.Equal("a.txt", items[0].Name);
    }

    [Fact]
    public async Task Test_Case_5()
    {
        // Attributes and size are only populated when requested.
        _ = await MockFileAsync(_dir, fileName: "a.txt", fileLength: 10);

        TurBuildOptions withAttributes = new() { IncludeFiles = true, IncludeAttributes = true };
        var items = new TurSystemBuilder(_dir, withAttributes, CancellationToken.None).Build().ToList();
        _ = Assert.Single(items);
        Assert.NotNull(items[0].CreationTime);
        Assert.NotNull(items[0].LastModifyTime);
        Assert.Equal(0, items[0].Length);

        TurBuildOptions withoutAttributes = new() { IncludeFiles = true };
        items = new TurSystemBuilder(_dir, withoutAttributes, CancellationToken.None).Build().ToList();
        _ = Assert.Single(items);
        Assert.Null(items[0].CreationTime);
        Assert.Null(items[0].LastModifyTime);
    }

    [Fact]
    public async Task Test_Case_6()
    {
        // Last-modify time window filters.
        var old = await MockFileAsync(_dir, fileName: "old.txt");
        var fresh = await MockFileAsync(_dir, fileName: "fresh.txt");
        File.SetLastWriteTime(old, DateTime.Now.AddDays(-2));

        TurBuildOptions options = new()
        {
            IncludeFiles = true,
            IncludeAttributes = true,
            LastModifyBefore = DateTime.Now.AddDays(-1)
        };
        var items = new TurSystemBuilder(_dir, options, CancellationToken.None).Build().ToList();
        _ = Assert.Single(items);
        Assert.Equal(old, items[0].FullPath);

        options = new TurBuildOptions
        {
            IncludeFiles = true,
            IncludeAttributes = true,
            LastModifyAfter = DateTime.Now.AddDays(-1)
        };
        items = new TurSystemBuilder(_dir, options, CancellationToken.None).Build().ToList();
        _ = Assert.Single(items);
        Assert.Equal(fresh, items[0].FullPath);
    }

    [Fact]
    public async Task Test_Case_7()
    {
        // Creation time window filter (skipped when the file system does not persist creation time).
        var old = await MockFileAsync(_dir, fileName: "old.txt");
        _ = await MockFileAsync(_dir, fileName: "fresh.txt");
        var oldTime = DateTime.Now.AddDays(-2);
        File.SetCreationTime(old, oldTime);
        if (Math.Abs((File.GetCreationTime(old) - oldTime).TotalSeconds) > 5)
        {
            return;
        }

        TurBuildOptions options = new()
        {
            IncludeFiles = true,
            IncludeAttributes = true,
            CreateBefore = DateTime.Now.AddDays(-1)
        };
        var items = new TurSystemBuilder(_dir, options, CancellationToken.None).Build().ToList();
        _ = Assert.Single(items);
        Assert.Equal(old, items[0].FullPath);
    }

    [Fact]
    public async Task Test_Case_8()
    {
        // A cancelled token yields nothing even though files exist.
        _ = await MockFileAsync(_dir, fileName: "a.txt");
        using CancellationTokenSource cts = new();
        cts.Cancel();

        TurBuildOptions options = new() { IncludeFiles = true, IncludeDirectories = true };
        TurSystemBuilder builder = new(_dir, options, cts.Token);
        Assert.Empty(builder.Build());
    }

    [Fact]
    public void Test_Case_9()
    {
        // Missing root directory surfaces an error during enumeration.
        var missing = Path.Combine(_dir, GetRandomName());
        TurBuildOptions options = new() { IncludeFiles = true };
        TurSystemBuilder builder = new(missing, options, CancellationToken.None);
        Assert.ThrowsAny<Exception>(() => _ = builder.Build().ToList());
    }

    [Fact]
    public void Test_Case_11()
    {
        // With IgnoreError a failed directory is skipped (empty result) and reported
        // through the onError callback instead of throwing.
        var missing = Path.Combine(_dir, GetRandomName());
        string reportedPath = null;
        Exception reportedError = null;

        TurBuildOptions options = new() { IncludeFiles = true, IgnoreError = true };
        TurSystemBuilder builder = new(missing, options, CancellationToken.None,
            (path, ex) =>
            {
                reportedPath = path;
                reportedError = ex;
            });

        Assert.Empty(builder.Build());
        Assert.Equal(Path.GetFullPath(missing), reportedPath);
        Assert.NotNull(reportedError);
    }

    [Fact]
    public void Test_Case_10()
    {
        // Empty directory yields nothing.
        TurBuildOptions options = new() { IncludeFiles = true, IncludeDirectories = true };
        TurSystemBuilder builder = new(_dir, options, CancellationToken.None);
        Assert.Empty(builder.Build());
    }

    [Fact]
    public async Task Test_Case_12()
    {
        // MaxDepth limits how deep the tree is enumerated (root children = depth 1).
        var sub = MockSubDir(_dir, "sub");
        var nested = MockSubDir(sub, "nested");
        var rootFile = await MockFileAsync(_dir, fileName: "root.txt");
        var subFile = await MockFileAsync(sub, fileName: "sub.txt");
        var nestedFile = await MockFileAsync(nested, fileName: "nested.txt");

        TurBuildOptions options = new() { IncludeFiles = true, IncludeDirectories = true, MaxDepth = 1 };
        var items = new TurSystemBuilder(_dir, options, CancellationToken.None).Build().ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.FullPath == rootFile);
        Assert.Contains(items, x => x.FullPath == sub);

        options.MaxDepth = 2;
        items = new TurSystemBuilder(_dir, options, CancellationToken.None).Build().ToList();
        Assert.Equal(4, items.Count);
        Assert.Contains(items, x => x.FullPath == subFile);
        Assert.Contains(items, x => x.FullPath == nested);

        options.MaxDepth = 3;
        items = new TurSystemBuilder(_dir, options, CancellationToken.None).Build().ToList();
        Assert.Equal(5, items.Count);
        Assert.Contains(items, x => x.FullPath == nestedFile);
    }

    [Fact]
    public async Task Test_Case_13()
    {
        // File size window filter.
        _ = await MockFileAsync(_dir, fileName: "small.bin", fileLength: 10);
        _ = await MockFileAsync(_dir, fileName: "mid.bin", fileLength: 100);
        _ = await MockFileAsync(_dir, fileName: "big.bin", fileLength: 1000);

        TurBuildOptions options = new()
        {
            IncludeFiles = true,
            IncludeFileSize = true,
            MinFileSize = 50,
            MaxFileSize = 500
        };
        var items = new TurSystemBuilder(_dir, options, CancellationToken.None).Build().ToList();
        _ = Assert.Single(items);
        Assert.Equal("mid.bin", items[0].Name);
    }

    [Fact]
    public async Task Test_Case_14()
    {
        // Reparse points (symlinks/junctions) are skipped by default; with
        // FollowLinks they are traversed exactly once and loops are detected.
        var target = MockSubDir(_dir, "target");
        _ = await MockFileAsync(target, fileName: "t.txt", fileLength: 8);

        var link = Path.Combine(_dir, "link");
        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (Exception)
        {
            return; // symlink creation not permitted on this platform
        }

        try
        {
            TurBuildOptions options = new() { IncludeFiles = true, IncludeDirectories = true };
            var items = new TurSystemBuilder(_dir, options, CancellationToken.None).Build().ToList();
            Assert.Equal(2, items.Count);
            Assert.DoesNotContain(items, x => x.Name == "link");

            options = new TurBuildOptions { IncludeFiles = true, IncludeDirectories = true, FollowLinks = true };
            items = new TurSystemBuilder(_dir, options, CancellationToken.None).Build().ToList();
            _ = Assert.Single(items, x => x.Name == "t.txt");
            _ = Assert.Single(items, x => x.IsDirectory);

            var loop = Path.Combine(_dir, "loop");
            Directory.CreateSymbolicLink(loop, _dir);
            try
            {
                items = new TurSystemBuilder(_dir, options, CancellationToken.None).Build().ToList();
                Assert.DoesNotContain(items, x => x.Name == "loop");
                _ = Assert.Single(items, x => x.Name == "t.txt");
            }
            finally
            {
                Directory.Delete(loop);
            }
        }
        finally
        {
            Directory.Delete(link);
        }
    }

    [Fact]
    public async Task Test_Case_15()
    {
        // Parallel scan yields exactly the same entry set as the sequential walk.
        _ = await MockDirAsync(_dir, 2, 2, 3, 64);

        TurBuildOptions sequential = new() { IncludeFiles = true, IncludeDirectories = true, IncludeFileSize = true };
        var expected = new TurSystemBuilder(_dir, sequential, CancellationToken.None).Build()
            .Select(x => x.RelativePath)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        TurBuildOptions parallel = new() { IncludeFiles = true, IncludeDirectories = true, IncludeFileSize = true, ScanParallelism = 4 };
        var actual = new TurSystemBuilder(_dir, parallel, CancellationToken.None).Build()
            .Select(x => x.RelativePath)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        Assert.True(expected.Count > 10);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Test_Case_16()
    {
        // Parallel scan keeps the error semantics: throws by default, reports + empty with IgnoreError.
        var missing = Path.Combine(_dir, GetRandomName());

        TurBuildOptions options = new() { IncludeFiles = true, ScanParallelism = 4 };
        Assert.ThrowsAny<Exception>(() => _ = new TurSystemBuilder(missing, options, CancellationToken.None).Build().ToList());

        var reported = 0;
        options = new TurBuildOptions { IncludeFiles = true, IgnoreError = true, ScanParallelism = 4 };
        TurSystemBuilder builder = new(missing, options, CancellationToken.None,
            (p, e) => _ = Interlocked.Increment(ref reported));
        Assert.Empty(builder.Build());
        Assert.Equal(1, reported);
    }

    [Fact]
    public async Task Test_Case_17()
    {
        // Parallel scan honors MaxDepth exactly like the sequential walk.
        var sub = MockSubDir(_dir, "sub");
        var nested = MockSubDir(sub, "nested");
        _ = await MockFileAsync(_dir, fileName: "root.txt");
        _ = await MockFileAsync(sub, fileName: "sub.txt");
        _ = await MockFileAsync(nested, fileName: "nested.txt");

        TurBuildOptions options = new() { IncludeFiles = true, IncludeDirectories = true, MaxDepth = 2, ScanParallelism = 4 };
        var items = new TurSystemBuilder(_dir, options, CancellationToken.None).Build().ToList();

        Assert.Equal(4, items.Count);
        Assert.DoesNotContain(items, x => x.Name == "nested.txt");
        Assert.Contains(items, x => x.Name == "nested");
    }

    public override void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}
