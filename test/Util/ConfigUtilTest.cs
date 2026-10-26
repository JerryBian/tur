using System;
using System.IO;
using System.Threading.Tasks;
using Tur.Util;
using Xunit;

namespace Tur.Test.Util;

public class ConfigUtilTest : TestBase
{
    private readonly string _dir;

    public ConfigUtilTest()
    {
        _dir = Path.Combine(Path.GetTempPath(), GetRandomName());
        _ = Directory.CreateDirectory(_dir);
    }

    [Fact]
    public async Task Test_Case_1()
    {
        // A well-formed config file is fully deserialized.
        var path = Path.Combine(_dir, "tur.json");
        await File.WriteAllTextAsync(path,
            "{\"excludes\":[\"**/*.tmp\"],\"threads\":4,\"minSize\":\"1kb\",\"ignoreError\":true,\"trash\":false}");

        var config = ConfigUtil.Load(path);

        Assert.Equal(new[] { "**/*.tmp" }, config.Excludes);
        Assert.Equal(4, config.Threads);
        Assert.Equal("1kb", config.MinSize);
        Assert.True(config.IgnoreError);
        Assert.False(config.Trash);
        Assert.Null(config.Verbose);
    }

    [Fact]
    public void Test_Case_2()
    {
        // Missing file yields an empty config, never throws.
        var config = ConfigUtil.Load(Path.Combine(_dir, GetRandomName()));
        Assert.Null(config.Excludes);
        Assert.Null(config.Threads);
    }

    [Fact]
    public async Task Test_Case_3()
    {
        // Malformed JSON yields an empty config, never throws.
        var path = Path.Combine(_dir, "bad.json");
        await File.WriteAllTextAsync(path, "{ not json !!!");

        var config = ConfigUtil.Load(path);
        Assert.Null(config.Excludes);
    }

    [Fact]
    public void Test_Case_4()
    {
        // TUR_CONFIG overrides the default location.
        var custom = Path.Combine(_dir, "custom.json");
        Environment.SetEnvironmentVariable("TUR_CONFIG", custom);
        try
        {
            Assert.Equal(custom, ConfigUtil.GetDefaultPath());
        }
        finally
        {
            Environment.SetEnvironmentVariable("TUR_CONFIG", null);
        }

        Assert.NotEqual(custom, ConfigUtil.GetDefaultPath());
    }

    [Fact]
    public void Test_Case_5()
    {
        // Explicit CLI value wins; otherwise config; otherwise CLI default.
        Assert.True(ConfigUtil.MergeBool(true, true, false));
        Assert.False(ConfigUtil.MergeBool(true, false, true));
        Assert.True(ConfigUtil.MergeBool(false, false, true));
        Assert.False(ConfigUtil.MergeBool(false, false, null));
    }

    public override void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}
