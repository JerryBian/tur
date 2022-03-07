using ByteSizeLib;

namespace Tur.Extension;

public static class NumberExtension
{
    public static string Human(this double val)
    {
        return ByteSize.FromBytes(val).ToString("#.#");
    }

    public static string Human(this long val)
    {
        return ByteSize.FromBytes(val).ToString("#.#");
    }
}