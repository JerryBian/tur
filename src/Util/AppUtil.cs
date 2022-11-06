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
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            return version == null ? "1.0.0" : version.ToString(3);
        }
    }

    public static bool HasMainWindow
    {
        get
        {
            using Process process = Process.GetCurrentProcess();
            return process.MainWindowHandle != IntPtr.Zero;
        }
    }
}