using System;
using System.IO;
using System.Threading.Tasks;
using Tur.Logging;
using Xunit;

namespace Tur.Test.Logging;

public class FileAppenderTest
{
    [Fact]
    public async Task Test_Case_1()
    {
        // Items are written through the persistent writer and visible after dispose.
        var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            FileAppender appender = new(file);
            appender.Add(new TurLogItem
            {
                Message = "line one",
                LogLevel = TurLogLevel.Information,
                Prefix = Constants.ArrowUnicode,
                PrefixSurroundWithBrackets = false
            });
            appender.Add(new TurLogItem
            {
                Message = "line two",
                LogLevel = TurLogLevel.Warning,
                Suffix = "SKIPPED"
            });
            await appender.DisposeAsync();

            var content = await File.ReadAllTextAsync(file);
            Assert.Contains("line one", content);
            Assert.Contains("line two  SKIPPED", content);
            Assert.Contains(Constants.ArrowUnicode, content);
        }
        finally
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    [Fact]
    public async Task Test_Case_2()
    {
        // Sequential appenders append (FileMode.Append), never truncate.
        var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            FileAppender first = new(file);
            first.Add(new TurLogItem { Message = "aaa", LogLevel = TurLogLevel.Information });
            await first.DisposeAsync();

            FileAppender second = new(file);
            second.Add(new TurLogItem { Message = "bbb", LogLevel = TurLogLevel.Information });
            await second.DisposeAsync();

            var content = await File.ReadAllTextAsync(file);
            Assert.Contains("aaa", content);
            Assert.Contains("bbb", content);
        }
        finally
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    [Fact]
    public async Task Test_Case_3()
    {
        // Many lines through one open handle (the perf-critical path) all land in order.
        var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            FileAppender appender = new(file);
            for (var i = 0; i < 2000; i++)
            {
                appender.Add(new TurLogItem { Message = $"line-{i:0000}", LogLevel = TurLogLevel.Information });
            }

            await appender.DisposeAsync();

            var lines = await File.ReadAllLinesAsync(file);
            Assert.Equal(2000, lines.Length);
            Assert.Equal("line-0000", lines[0]);
            Assert.Equal("line-1999", lines[1999]);
        }
        finally
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
