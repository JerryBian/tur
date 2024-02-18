using System;

namespace Tur.Logging
{
    public interface IAppender : IAsyncDisposable
    {
        bool TryAdd(string message, TurLogLevel level = TurLogLevel.Information, string prefix = null, string suffix = null, Exception error = null);
    }
}
