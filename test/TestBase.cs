using System;
using System.IO;
using System.Threading.Tasks;

namespace Tur.Test;

public abstract class TestBase : IDisposable
{
    private readonly Random _random;

    protected TestBase()
    {
        _random = new Random();
    }

    public virtual void Dispose()
    {
    }

    public byte[] GetRandomBytes(long length)
    {
        var bytes = new byte[length];
        _random.NextBytes(bytes);
        return bytes;
    }

    public async Task<string> MockFileAsync(string dir, byte[] bytes = null, string fileName = null,
        long fileLength = -1)
    {
        _ = Directory.CreateDirectory(dir);

        fileLength = fileLength < 0 ? _random.Next(1024 * 1024) : fileLength;
        bytes ??= GetRandomBytes(fileLength);
        fileName ??= GetRandomName();

        var fullPath = Path.Combine(dir, fileName);
        await File.WriteAllBytesAsync(fullPath, bytes);
        return fullPath;
    }

    public string MockSubDir(string dir, string subDirName = null)
    {
        subDirName ??= GetRandomName();
        var fullPath = Path.Combine(dir, subDirName);
        _ = Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    public async Task<DirTree> MockDirAsync(string dir, int topDirs, int topFiles, int nestingLevel = 0,
        long fileLength = -1)
    {
        DirTree dirTree = new(Path.GetFileName(dir), Path.GetDirectoryName(dir));

        if (nestingLevel < 0)
        {
            return dirTree;
        }

        _ = Directory.CreateDirectory(dir);

        for (var i = 0; i < topDirs; i++)
        {
            var file = await MockFileAsync(dir, fileLength: fileLength);
            dirTree.Files.Add(Path.GetFileName(file));
        }

        for (var j = 0; j < topDirs; j++)
        {
            var subDir = MockSubDir(dir);
            dirTree.SubDirTrees.Add(await MockDirAsync(subDir, topDirs, topFiles, nestingLevel - 1, fileLength));
        }

        return dirTree;
    }

    public string GetRandomName()
    {
        return Path.GetRandomFileName().Replace(".", string.Empty);
    }
}