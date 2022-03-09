namespace Tur.Model;

public class ItemEntry
{
    public ItemEntry(string fullPath, bool isDir = false)
    {
        IsDir = isDir;
        FullPath = fullPath;
    }

    public bool IsDir { get; }

    public string FullPath { get; }

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