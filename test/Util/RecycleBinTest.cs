using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tur.Util;
using Xunit;

namespace Tur.Test.Util;

public class RecycleBinTest : TestBase
{
    private readonly string _dir;
    private readonly string _trashRoot;

    public RecycleBinTest()
    {
        _dir = Path.Combine(Path.GetTempPath(), GetRandomName());
        _trashRoot = Path.Combine(Path.GetTempPath(), GetRandomName());
        _ = Directory.CreateDirectory(_dir);
    }

    [Fact]
    public async Task Test_Case_1()
    {
        // File is moved into files/ and a matching .trashinfo is written.
        var file = await MockFileAsync(_dir, fileName: "a.txt", fileLength: 16);

        RecycleBin.MoveToTrash(file, _trashRoot);

        Assert.False(File.Exists(file));
        Assert.True(File.Exists(Path.Combine(_trashRoot, "files", "a.txt")));

        var infoFile = Path.Combine(_trashRoot, "info", "a.txt.trashinfo");
        Assert.True(File.Exists(infoFile));
        var info = await File.ReadAllTextAsync(infoFile);
        Assert.StartsWith("[Trash Info]", info);
        Assert.Contains("Path=", info);
        Assert.Contains("DeletionDate=", info);
    }

    [Fact]
    public async Task Test_Case_2()
    {
        // Whole directory subtree is moved.
        var sub = MockSubDir(_dir, "sub");
        _ = await MockFileAsync(sub, fileName: "b.txt");

        RecycleBin.MoveToTrash(sub, _trashRoot);

        Assert.False(Directory.Exists(sub));
        Assert.True(File.Exists(Path.Combine(_trashRoot, "files", "sub", "b.txt")));
    }

    [Fact]
    public async Task Test_Case_3()
    {
        // Same name from different directories gets uniquified in the trash.
        var sub1 = MockSubDir(_dir, "s1");
        var sub2 = MockSubDir(_dir, "s2");
        var f1 = await MockFileAsync(sub1, fileName: "dup.txt");
        var f2 = await MockFileAsync(sub2, fileName: "dup.txt");

        RecycleBin.MoveToTrash(f1, _trashRoot);
        RecycleBin.MoveToTrash(f2, _trashRoot);

        var names = Directory.GetFiles(Path.Combine(_trashRoot, "files"))
            .Select(Path.GetFileName)
            .OrderBy(x => x, System.StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "dup 1.txt", "dup.txt" }, names);
    }

    public override void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }

        if (Directory.Exists(_trashRoot))
        {
            Directory.Delete(_trashRoot, true);
        }
    }
}
