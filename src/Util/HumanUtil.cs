using Tur.Extension;

namespace Tur.Util;

public static class HumanUtil
{
    public static string GetSize(long length)
    {
        return length <= 0 ? "0 b" : length.SizeHuman();
    }

    public static string GetRatesPerSecond(long length, double seconds)
    {
        return length <= 0 || seconds <= 0 ? "-" : (length / seconds).SizeHuman() + "/s";
    }
}