using System;
using System.IO;
using System.Runtime.InteropServices;
using Tur.Util;
using Xunit;

namespace Tur.Test.Util;

public class PathUtilTest : TestBase
{
    private readonly string _dir;

    public PathUtilTest()
    {
        _dir = Path.Combine(Path.GetTempPath(), GetRandomName());
        _ = Directory.CreateDirectory(_dir);
    }

    [Fact]
    public void Test_Case_1()
    {
        // A local temp directory is never remote; degenerate inputs are safe.
        Assert.False(PathUtil.IsRemotePath(_dir));
        Assert.False(PathUtil.AnyRemote(new[] { _dir }));
        Assert.False(PathUtil.AnyRemote(null));
        Assert.False(PathUtil.AnyRemote(Array.Empty<string>()));
        Assert.False(PathUtil.IsRemotePath(null));
        Assert.False(PathUtil.IsRemotePath(string.Empty));
    }

    [Fact]
    public void Test_Case_2()
    {
        // Mount table parsing: longest-prefix mount wins, segment boundaries respected.
        var mounts = Path.Combine(_dir, "mounts");
        File.WriteAllText(mounts, string.Join("\n",
            "/dev/sda1 / ext4 rw 0 0",
            "//srv/share /mnt/share cifs rw,relatimes 0 0",
            "tmpfs /tmp tmpfs rw 0 0",
            "host:/export /mnt/nfs nfs4 rw 0 0",
            "/dev/sdb1 /mnt/share/local ext4 rw 0 0",
            "//srv/sp /mnt/my\\040share cifs rw 0 0"));

        Assert.True(PathUtil.IsRemoteUnderMounts("/mnt/share/docs/a.txt", mounts));
        Assert.True(PathUtil.IsRemoteUnderMounts("/mnt/nfs/a", mounts));
        Assert.True(PathUtil.IsRemoteUnderMounts("/mnt/share", mounts));
        Assert.False(PathUtil.IsRemoteUnderMounts("/mnt/sharefoo/a", mounts));
        Assert.False(PathUtil.IsRemoteUnderMounts("/tmp/x", mounts));
        Assert.False(PathUtil.IsRemoteUnderMounts("/", mounts));
        // ext4 mounted underneath the CIFS share wins by longest prefix.
        Assert.False(PathUtil.IsRemoteUnderMounts("/mnt/share/local/deep", mounts));
        // Octal-escaped mount point (\040 = space).
        Assert.True(PathUtil.IsRemoteUnderMounts("/mnt/my share/f", mounts));
        // Missing mounts file degrades to local.
        Assert.False(PathUtil.IsRemoteUnderMounts("/mnt/share/x", Path.Combine(_dir, "nope")));
    }

    [Fact]
    public void Test_Case_3()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // UNC paths are remote without touching the network.
        Assert.True(PathUtil.IsRemotePath(@"\\somehost\share\dir"));
        // The local drive holding the temp dir is not remote.
        Assert.False(PathUtil.IsRemotePath(_dir));
    }

    public override void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}
