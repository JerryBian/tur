using System;

namespace Tur.Logging
{
    public interface IAppender : IAsyncDisposable
    {
        void Add(TurLogItem item);
    }
}
