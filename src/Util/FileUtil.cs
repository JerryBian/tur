using Tur.Extension;

namespace Tur.Util;

public static class FileUtil
{
    public static string GetSize(long length)
    {
        if (length <= 0)
        {
            return "0 b";
        }

        return length.Human();
    }

    public static string GetRatesPerSecond(long length, double seconds)
    {
        if (length <= 0 || seconds <= 0)
        {
            return "-";
        }

        return (length / seconds).Human() + "/s";
    }
}