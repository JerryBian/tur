using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Tur.Util;

/// <summary>
/// Best-effort detection of remote (SMB/NFS/SSHFS) file system paths. Used to pick
/// I/O strategies: high-latency links benefit from parallel enumeration and large
/// sequential buffers, while local disks keep the lean defaults. Detection failures
/// always fall back to "local" — they must never break a run.
/// </summary>
public static class PathUtil
{
    private static readonly HashSet<string> _remoteFsTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "cifs", "smbfs", "smb2", "smb3", "nfs", "nfs4", "fuse.sshfs", "afs"
    };

    public static bool AnyRemote(IEnumerable<string> paths)
    {
        if (paths == null)
        {
            return false;
        }

        foreach (var path in paths)
        {
            if (!string.IsNullOrEmpty(path) && IsRemotePath(path))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsRemotePath(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var fullPath = Path.GetFullPath(path);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return IsRemoteWindows(fullPath);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return IsRemoteUnderMounts(fullPath, "/proc/mounts");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return IsRemoteOsx(fullPath);
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private const uint DriveRemote = 0x4;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [SupportedOSPlatform("windows")]
    private static extern uint GetDriveTypeW(string lpRootPathName);

    [SupportedOSPlatform("windows")]
    private static bool IsRemoteWindows(string fullPath)
    {
        // UNC path (\\server\share\...) is always remote.
        if (fullPath.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return true;
        }

        var root = Path.GetPathRoot(fullPath);
        if (string.IsNullOrEmpty(root))
        {
            return false;
        }

        // Also covers mapped network drives (e.g. Z:).
        return GetDriveTypeW(root) == DriveRemote;
    }

    /// <summary>
    /// Linux check via a mounts file ("/proc/mounts" in production). The mount point
    /// with the longest prefix match wins, so an ext4 volume mounted underneath a
    /// CIFS share is correctly reported as local. Exposed for testing.
    /// </summary>
    public static bool IsRemoteUnderMounts(string fullPath, string mountsFile)
    {
        if (!File.Exists(mountsFile))
        {
            return false;
        }

        var bestMount = string.Empty;
        var bestType = string.Empty;
        foreach (var raw in File.ReadLines(mountsFile))
        {
            var parts = raw.Split(' ');
            if (parts.Length < 3)
            {
                continue;
            }

            var mountPoint = UnescapeMountPoint(parts[1]);
            if (!IsUnder(fullPath, mountPoint) || mountPoint.Length <= bestMount.Length)
            {
                continue;
            }

            bestMount = mountPoint;
            bestType = parts[2];
        }

        return _remoteFsTypes.Contains(bestType);
    }

    private static string UnescapeMountPoint(string token)
    {
        // /proc/mounts octal-escapes: space=\040, tab=\011, newline=\012, backslash=\134.
        if (token.IndexOf('\\') < 0)
        {
            return token;
        }

        return token.Replace("\\040", " ").Replace("\\011", "\t").Replace("\\012", "\n").Replace("\\134", "\\");
    }

    private static bool IsUnder(string fullPath, string mountPoint)
    {
        if (string.IsNullOrEmpty(mountPoint))
        {
            return false;
        }

        if (fullPath.Equals(mountPoint, StringComparison.Ordinal))
        {
            return true;
        }

        if (mountPoint == "/")
        {
            return fullPath.StartsWith("/", StringComparison.Ordinal);
        }

        return fullPath.StartsWith(mountPoint + "/", StringComparison.Ordinal);
    }

    [SupportedOSPlatform("macos")]
    private static bool IsRemoteOsx(string fullPath)
    {
        // Best effort: parse `mount` output lines like
        //   //user@srv/share on /Volumes/share (smbfs, local, nodev, ...)
        ProcessStartInfo psi = new("mount")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            return false;
        }

        var output = process.StandardOutput.ReadToEnd();
        _ = process.WaitForExit(5000);

        foreach (var line in output.Split('\n'))
        {
            var onIndex = line.IndexOf(" on ", StringComparison.Ordinal);
            var parenIndex = line.IndexOf(" (", StringComparison.Ordinal);
            if (onIndex < 0 || parenIndex <= onIndex + 4)
            {
                continue;
            }

            var mountPoint = line.Substring(onIndex + 4, parenIndex - onIndex - 4).Trim();
            var rest = line.Substring(parenIndex + 2);
            var comma = rest.IndexOf(',');
            var fsType = (comma < 0 ? rest : rest.Substring(0, comma)).Trim();
            if (IsUnder(fullPath, mountPoint) && _remoteFsTypes.Contains(fsType))
            {
                return true;
            }
        }

        return false;
    }
}
