using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tur.Model;

namespace Tur.Appender
{
    public interface IAppender : IAsyncDisposable
    {
        bool TryAdd(LogItem item);
    }
}
