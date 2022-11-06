namespace Tur.Model;

public class ItemEntry
{
    public ItemEntry(string fullPath, string relativePath, bool isDir = false)
    {
        IsDir = isDir;
        FullPath = fullPath;
        RelativePath = relativePath;
    }

    public bool IsDir { get; }

    public string RelativePath { get; }

    public string FullPath { get; }

    public string GetDisplayPath()
    {
        return !string.IsNullOrEmpty(RelativePath) ? RelativePath : FullPath;
    }

    public override bool Equals(object obj)
    {
        return obj is ItemEntry other && FullPath == other.FullPath;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 23) + IsDir.GetHashCode();
            hash = (hash * 23) + FullPath.GetHashCode();
            return hash;
        }
    }
}