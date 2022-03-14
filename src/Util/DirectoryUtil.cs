using System.IO;

namespace Tur.Util;

public static class DirectoryUtil
{
    public static void Copy(string srcDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var dir in Directory.GetDirectories(srcDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destDir, Path.GetRelativePath(srcDir, dir)));
        }

        foreach (var file in Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetRelativePath(srcDir, file)), true);
        }
    }
}