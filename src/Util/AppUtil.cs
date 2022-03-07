using System.Reflection;

namespace Tur.Util;

public static class AppUtil
{
    public static string AppVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version == null)
            {
                return "1.0.0";
            }

            return version.ToString(3);
        }
    }
}