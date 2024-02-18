using System;

namespace Tur.Logging
{
    public interface ITurLogger : IAsyncDisposable
    {
        void Write(string message, TurLogLevel level = TurLogLevel.Information, string prefix = null, string suffix = null, Exception error = null);
    }
}