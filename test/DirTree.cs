using System.Collections.Generic;
using System.IO;

namespace Tur.Test;

public class DirTree
{
    public DirTree(string name, string basePath)
    {
        Name = name;
        BasePath = basePath;
        Files = new List<string>();
        SubDirTrees = new List<DirTree>();
    }

    public string BasePath { get; }

    public string Name { get; }

    public List<string> Files { get; }

    public List<DirTree> SubDirTrees { get; }

    public string FullPath => Path.Combine(BasePath, Name);
}