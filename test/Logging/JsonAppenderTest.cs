using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Tur.Logging;
using Xunit;

namespace Tur.Test.Logging;

public class JsonAppenderTest
{
    [Fact]
    public async Task Test_Case_1()
    {
        // One NDJSON record per item; trace is gated by verbose; null fields omitted.
        StringWriter writer = new();
        JsonAppender appender = new(false, writer);
        appender.Add(new TurLogItem
        {
            Message = "hello \"world\"",
            LogLevel = TurLogLevel.Information,
            Suffix = "F"
        });
        appender.Add(new TurLogItem
        {
            Message = "trace me",
            LogLevel = TurLogLevel.Trace
        });
        await appender.DisposeAsync();

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        _ = Assert.Single(lines);

        using var doc = JsonDocument.Parse(lines[0].TrimEnd('\r'));
        Assert.Equal("hello \"world\"", doc.RootElement.GetProperty("message").GetString());
        Assert.Equal("information", doc.RootElement.GetProperty("level").GetString());
        Assert.Equal("F", doc.RootElement.GetProperty("suffix").GetString());
        Assert.False(doc.RootElement.TryGetProperty("error", out _));
        Assert.True(doc.RootElement.TryGetProperty("ts", out _));
    }

    [Fact]
    public async Task Test_Case_2()
    {
        // Verbose keeps trace records; errors are serialized in full.
        StringWriter writer = new();
        JsonAppender appender = new(true, writer);
        appender.Add(new TurLogItem
        {
            Message = "trace me",
            LogLevel = TurLogLevel.Trace
        });
        appender.Add(new TurLogItem
        {
            Message = "boom",
            LogLevel = TurLogLevel.Error,
            Error = new InvalidOperationException("bad thing")
        });
        await appender.DisposeAsync();

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);

        using var doc = JsonDocument.Parse(lines[1].TrimEnd('\r'));
        Assert.Equal("error", doc.RootElement.GetProperty("level").GetString());
        Assert.Contains("bad thing", doc.RootElement.GetProperty("error").GetString());
    }
}
