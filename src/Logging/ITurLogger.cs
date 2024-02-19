using System;

namespace Tur.Logging
{
    public interface ITurLogger : IAsyncDisposable
    {
        void Log(string message, TurLogLevel level = TurLogLevel.Information, string prefix = null, bool prefixSurroundWithBrackets = true, string suffix = null, Exception error = null);
    }
}