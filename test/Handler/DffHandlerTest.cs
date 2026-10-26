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
        _option = new DffOption(Array.Empty<string>()) { Dir = [_dir] };
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
        _option.Dir = [Path.GetRandomFileName()];
        await using DffHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        Assert.Equal(1, exitCode);
        Assert.False(Directory.GetFiles(_dir, "*.txt").Any());
    }

    [Fact]
    public async Task Test_Case_5()
    {
        // Duplicate files are detected and reported in the log file.
        var outputDir = Path.Combine(Path.GetTempPath(), GetRandomName());
        _option.OutputDir = outputDir;
        var bytes = GetRandomBytes(32);
        var file1 = await MockFileAsync(_dir, bytes, "dup1.dat");
        var file2 = await MockFileAsync(_dir, bytes, "dup2.dat");
        _ = await MockFileAsync(_dir, GetRandomBytes(32), "unique.dat");

        DffHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        await handler.DisposeAsync();

        Assert.Equal(0, exitCode);
        var logFile = Assert.Single(Directory.GetFiles(outputDir, "tur-dff-*.log"));
        var content = await File.ReadAllTextAsync(logFile);
        Assert.Contains("Found 1 duplicate group for file size", content);
        Assert.Contains(file1, content);
        Assert.Contains(file2, content);
        Assert.DoesNotContain("unique.dat", content);
        Directory.Delete(outputDir, true);
    }

    [Fact]
    public async Task Test_Case_6()
    {
        // Same-size files with different content are not duplicates.
        var outputDir = Path.Combine(Path.GetTempPath(), GetRandomName());
        _option.OutputDir = outputDir;
        _ = await MockFileAsync(_dir, GetRandomBytes(64), "a.dat");
        _ = await MockFileAsync(_dir, GetRandomBytes(64), "b.dat");

        DffHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        await handler.DisposeAsync();

        Assert.Equal(0, exitCode);
        var logFile = Assert.Single(Directory.GetFiles(outputDir, "tur-dff-*.log"));
        var content = await File.ReadAllTextAsync(logFile);
        Assert.DoesNotContain("duplicate group", content);
        Assert.Contains("No duplicate files found.", content);
        Directory.Delete(outputDir, true);
    }

    [Fact]
    public async Task Test_Case_7()
    {
        // Last-modify filter narrows the analysis scope.
        var outputDir = Path.Combine(Path.GetTempPath(), GetRandomName());
        _option.OutputDir = outputDir;
        var bytes = GetRandomBytes(16);
        var old1 = await MockFileAsync(_dir, bytes, "old1.dat");
        var old2 = await MockFileAsync(_dir, bytes, "old2.dat");
        _ = await MockFileAsync(_dir, bytes, "new1.dat");
        _ = await MockFileAsync(_dir, bytes, "new2.dat");
        File.SetLastWriteTime(old1, DateTime.Now.AddDays(-2));
        File.SetLastWriteTime(old2, DateTime.Now.AddDays(-2));
        _option.LastModifyBefore = DateTime.Now.AddDays(-1);

        DffHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        await handler.DisposeAsync();

        Assert.Equal(0, exitCode);
        var logFile = Assert.Single(Directory.GetFiles(outputDir, "tur-dff-*.log"));
        var content = await File.ReadAllTextAsync(logFile);
        Assert.Contains("Found 1 duplicate group for file size", content);
        Assert.Contains(old1, content);
        Assert.DoesNotContain("new1.dat", content);
        Directory.Delete(outputDir, true);
    }

    [Fact]
    public async Task Test_Case_8()
    {
        // --write-list emits every duplicate except the keeper (default: alphabetically first).
        var outputDir = Path.Combine(Path.GetTempPath(), GetRandomName());
        var listFile = Path.Combine(outputDir, "dups.txt");
        _option.OutputDir = outputDir;
        _option.WriteList = listFile;
        var bytes = GetRandomBytes(48);
        _ = await MockFileAsync(_dir, bytes, "bbb.dat");
        _ = await MockFileAsync(_dir, bytes, "ccc.dat");
        _ = await MockFileAsync(_dir, bytes, "aaa.dat");

        DffHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        await handler.DisposeAsync();

        Assert.Equal(0, exitCode);
        var lines = await File.ReadAllLinesAsync(listFile);
        Assert.Equal(2, lines.Length);
        Assert.Contains(Path.Combine(_dir, "bbb.dat"), lines);
        Assert.Contains(Path.Combine(_dir, "ccc.dat"), lines);
        Directory.Delete(outputDir, true);
    }

    [Fact]
    public async Task Test_Case_9()
    {
        // --keep newest keeps the most recently modified copy.
        var outputDir = Path.Combine(Path.GetTempPath(), GetRandomName());
        var listFile = Path.Combine(outputDir, "dups.txt");
        _option.OutputDir = outputDir;
        _option.WriteList = listFile;
        _option.Keep = "newest";
        var bytes = GetRandomBytes(48);
        var x1 = await MockFileAsync(_dir, bytes, "x1.dat");
        var x2 = await MockFileAsync(_dir, bytes, "x2.dat");
        var x3 = await MockFileAsync(_dir, bytes, "x3.dat");
        File.SetLastWriteTime(x1, DateTime.Now.AddDays(-3));
        File.SetLastWriteTime(x2, DateTime.Now.AddDays(-2));
        File.SetLastWriteTime(x3, DateTime.Now.AddDays(-1));

        DffHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        await handler.DisposeAsync();

        Assert.Equal(0, exitCode);
        var lines = await File.ReadAllLinesAsync(listFile);
        Assert.Equal(2, lines.Length);
        Assert.Contains(x1, lines);
        Assert.Contains(x2, lines);
        Assert.DoesNotContain(x3, lines);
        Directory.Delete(outputDir, true);
    }

    [Fact]
    public async Task Test_Case_10()
    {
        // Invalid --keep value fails fast.
        _option.Keep = "bogus";
        await using DffHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Test_Case_11()
    {
        // --min-size filters small files out of the analysis; the list stays empty.
        var outputDir = Path.Combine(Path.GetTempPath(), GetRandomName());
        var listFile = Path.Combine(outputDir, "dups.txt");
        _option.OutputDir = outputDir;
        _option.WriteList = listFile;
        _option.MinSize = 32;
        var bytes = GetRandomBytes(16);
        _ = await MockFileAsync(_dir, bytes, "s1.dat");
        _ = await MockFileAsync(_dir, bytes, "s2.dat");

        DffHandler handler = new(_option, CancellationToken.None);
        var exitCode = await handler.HandleAsync();
        await handler.DisposeAsync();

        Assert.Equal(0, exitCode);
        Assert.Empty(await File.ReadAllLinesAsync(listFile));
        Directory.Delete(outputDir, true);
    }

    public override void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}