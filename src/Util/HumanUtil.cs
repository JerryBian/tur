using System;
using System.Globalization;
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

    /// <summary>
    /// Parses a human size string such as "1024", "10kb", "1.5 MB", "2g".
    /// Units are binary (1kb = 1024 bytes). Empty unit means bytes.
    /// </summary>
    public static bool TryParseSize(string text, out long bytes)
    {
        bytes = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim();
        var i = 0;
        while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.'))
        {
            i++;
        }

        if (i == 0 || !double.TryParse(text.Substring(0, i), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value < 0)
        {
            return false;
        }

        var unit = text.Substring(i).Trim().ToLowerInvariant();
        double multiplier;
        switch (unit)
        {
            case "":
            case "b":
                multiplier = 1;
                break;
            case "k":
            case "kb":
                multiplier = 1024;
                break;
            case "m":
            case "mb":
                multiplier = 1024 * 1024;
                break;
            case "g":
            case "gb":
                multiplier = 1024 * 1024 * 1024;
                break;
            case "t":
            case "tb":
                multiplier = 1024L * 1024 * 1024 * 1024;
                break;
            default:
                return false;
        }

        var result = value * multiplier;
        if (result > long.MaxValue)
        {
            return false;
        }

        bytes = (long)result;
        return true;
    }
}