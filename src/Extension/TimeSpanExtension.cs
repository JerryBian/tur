using Humanizer;
using Humanizer.Localisation;
using System;

namespace Tur.Extension;

public static class TimeSpanExtension
{
    public static string Human(this TimeSpan timeSpan)
    {
        return timeSpan.Humanize(minUnit: TimeUnit.Second, maxUnit: TimeUnit.Day);
    }
}