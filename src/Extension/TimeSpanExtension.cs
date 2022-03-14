using System;
using System.Collections.Generic;
using System.Linq;

namespace Tur.Extension;

public static class TimeSpanExtension
{
    public static string Human(this TimeSpan timeSpan)
    {
        var result = new List<string>();
        string str;
        if (timeSpan.Days > 0)
        {
            str = $"{timeSpan.Days} day";
            if (timeSpan.Days > 1)
            {
                str += "s";
            }

            result.Add(str);
        }

        if (timeSpan.Hours > 0)
        {
            str = $"{timeSpan.Hours} hour";
            if (timeSpan.Hours > 1)
            {
                str += "s";
            }

            result.Add(str);
        }

        if (timeSpan.Minutes > 0)
        {
            str = $"{timeSpan.Minutes} minute";
            if (timeSpan.Minutes > 1)
            {
                str += "s";
            }

            result.Add(str);
        }

        if (timeSpan.Seconds > 0)
        {
            str = $"{timeSpan.Seconds} second";
            if (timeSpan.Seconds > 1)
            {
                str += "s";
            }

            result.Add(str);
        }

        if (timeSpan.Milliseconds > 0)
        {
            str = $"{timeSpan.Milliseconds} ms";
            result.Add(str);
        }

        if (!result.Any())
        {
            return "less than 1 ms";
        }

        return string.Join(" ", result);
    }
}