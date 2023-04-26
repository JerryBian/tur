using System;
using Tur.Model;

namespace Tur.Appender
{
    public interface IAppender : IAsyncDisposable
    {
        bool TryAdd(LogItem item);
    }
}
