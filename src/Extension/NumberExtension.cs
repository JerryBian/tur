using Humanizer;

namespace Tur.Extension;

public static class NumberExtension
{
    public static string SizeHuman(this double val)
    {
        return val.Bytes().ToString("#.#");
    }

    public static string SizeHuman(this long val)
    {
        return val.Bytes().ToString("#.#");
    }
}