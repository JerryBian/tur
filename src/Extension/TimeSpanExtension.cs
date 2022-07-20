using System;
using Humanizer;
using Humanizer.Localisation;

namespace Tur.Extension;

public static class TimeSpanExtension
{
    public static string Human(this TimeSpan timeSpan)
    {
        return timeSpan.Humanize(minUnit: TimeUnit.Second, maxUnit: TimeUnit.Day);
    }

    private static DateTime StartDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
    public static DateTime ToDateTime(this long seconds)
    {
        return StartDateTime.AddSeconds(seconds).ToLocalTime();
    }

    public static long ToLongTimeSpan(this DateTime dateTime)
    {
        return (dateTime.ToLocalTime().Ticks - StartDateTime.ToLocalTime().Ticks) / 10000000;
    }
}