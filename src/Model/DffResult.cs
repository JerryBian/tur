using System.Collections.Generic;
using System.Linq;

namespace Tur.Model
{
    public class DffResult
    {
        public bool HasDuplicate => DuplicateItems.Any();

        public List<List<FileSystemItem>> DuplicateItems { get; } = new();
    }
}
