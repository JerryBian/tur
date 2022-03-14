using System;
using System.Diagnostics;
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

    public static bool HasMainWindow
    {
        get
        {
            using var process = Process.GetCurrentProcess();
            return process.MainWindowHandle != IntPtr.Zero;
        }
    }
}