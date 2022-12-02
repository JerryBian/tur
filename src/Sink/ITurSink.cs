using System;
using System.Threading.Tasks;

namespace Tur.Sink;

public interface ITurSink : IAsyncDisposable
{
    Task NewLineAsync(bool verboseOnly = false, int count = 1);

    Task DefaultAsync(string message, bool verboseOnly = false);

    Task DefaultLineAsync(string message, bool verboseOnly = false);

    Task LightAsync(string message, bool verboseOnly = false);

    Task LightLineAsync(string message, bool verboseOnly = false);

    Task InfoAsync(string message, bool verboseOnly = false);

    Task InfoLineAsync(string message, bool verboseOnly = false);

    Task WarnAsync(string message, bool verboseOnly = false);

    Task WarnLineAsync(string message, bool verboseOnly = false);

    Task ErrorAsync(string message, bool verboseOnly = false, Exception ex = null);

    Task ErrorLineAsync(string message, bool verboseOnly = false, Exception ex = null);

    Task ClearLineAsync(bool verboseOnly = false, int cursorTop = -1);
}