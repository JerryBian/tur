using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Tur.Util;

/// <summary>
/// Moves files/directories to the OS trash instead of deleting them permanently.
/// Windows: Recycle Bin via SHFileOperation. macOS: ~/.Trash. Linux: freedesktop
/// trash spec under $XDG_DATA_HOME/Trash. A custom trashRoot overrides the platform
/// location with a portable files/+info/ layout (used by tests).
/// </summary>
public static class RecycleBin
{
    public static void MoveToTrash(string fullPath, string trashRoot = null)
    {
        if (string.IsNullOrEmpty(fullPath))
        {
            throw new ArgumentException("Path must not be empty.", nameof(fullPath));
        }

        if (!string.IsNullOrEmpty(trashRoot))
        {
            MoveToDirectory(fullPath, trashRoot, writeTrashInfo: true);
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            MoveToWindowsRecycleBin(fullPath);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            MoveToDirectory(fullPath, Path.Combine(GetHomeDir(), ".Trash"), writeTrashInfo: false);
        }
        else
        {
            var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (string.IsNullOrEmpty(dataHome))
            {
                dataHome = Path.Combine(GetHomeDir(), ".local", "share");
            }

            MoveToDirectory(fullPath, Path.Combine(dataHome, "Trash"), writeTrashInfo: true);
        }
    }

    private static string GetHomeDir()
    {
        var home = Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
        {
            home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (string.IsNullOrEmpty(home))
        {
            throw new IOException("Unable to determine the user home directory for the trash location.");
        }

        return home;
    }

    private static void MoveToDirectory(string fullPath, string trashDir, bool writeTrashInfo)
    {
        var filesDir = writeTrashInfo ? Path.Combine(trashDir, "files") : trashDir;
        _ = Directory.CreateDirectory(filesDir);

        var name = Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(name))
        {
            throw new IOException($"Cannot trash a filesystem root: '{fullPath}'.");
        }

        var target = GetUniqueTarget(filesDir, name);
        if (Directory.Exists(fullPath))
        {
            Directory.Move(fullPath, target);
        }
        else
        {
            File.Move(fullPath, target);
        }

        if (writeTrashInfo)
        {
            var infoDir = Path.Combine(trashDir, "info");
            _ = Directory.CreateDirectory(infoDir);
            var info = new StringBuilder()
                .AppendLine("[Trash Info]")
                .AppendLine($"Path={EncodePath(fullPath)}")
                .AppendLine($"DeletionDate={DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)}")
                .ToString();
            File.WriteAllText(Path.Combine(infoDir, Path.GetFileName(target) + ".trashinfo"), info, new UTF8Encoding(false));
        }
    }

    private static string GetUniqueTarget(string dir, string name)
    {
        var candidate = Path.Combine(dir, name);
        var counter = 1;
        var stem = Path.GetFileNameWithoutExtension(name);
        var ext = Path.GetExtension(name);
        while (File.Exists(candidate) || Directory.Exists(candidate))
        {
            candidate = Path.Combine(dir, $"{stem} {counter}{ext}");
            counter++;
        }

        return candidate;
    }

    private static string EncodePath(string path)
    {
        StringBuilder sb = new();
        foreach (var b in Encoding.UTF8.GetBytes(path))
        {
            var c = (char)b;
            if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')
                || c == '-' || c == '_' || c == '.' || c == '/')
            {
                _ = sb.Append(c);
            }
            else
            {
                _ = sb.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }
        }

        return sb.ToString();
    }

    private const uint FO_DELETE = 0x0003;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOERRORUI = 0x0400;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCTW
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string pTo;
        public ushort fFlags;
        public int fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [SupportedOSPlatform("windows")]
    private static extern int SHFileOperationW(ref SHFILEOPSTRUCTW lpFileOp);

    [SupportedOSPlatform("windows")]
    private static void MoveToWindowsRecycleBin(string fullPath)
    {
        SHFILEOPSTRUCTW op = new()
        {
            wFunc = FO_DELETE,
            pFrom = fullPath + "\0\0",
            fFlags = (ushort)(FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI)
        };

        var result = SHFileOperationW(ref op);
        if (result != 0 || op.fAnyOperationsAborted != 0)
        {
            throw new IOException($"Failed to move '{fullPath}' to the Recycle Bin (SHFileOperation result 0x{result:X}).");
        }
    }
}
