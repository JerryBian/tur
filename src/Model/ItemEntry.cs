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
        if (!string.IsNullOrEmpty(RelativePath))
        {
            return RelativePath;
        }

        return FullPath;
    }

    public override bool Equals(object obj)
    {
        var other = obj as ItemEntry;
        if (other == null)
        {
            return false;
        }

        return FullPath == other.FullPath;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + IsDir.GetHashCode();
            hash = hash * 23 + FullPath.GetHashCode();
            return hash;
        }
    }
}