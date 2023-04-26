using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Tur.Model;

namespace Tur.Appender
{
    public abstract class BlockingAppender : IAppender
    {
        private readonly Thread _thread;
        private readonly BlockingCollection<LogItem> _items;

        public BlockingAppender()
        {
            _items = new BlockingCollection<LogItem>(Constants.LogItemsCapacity);
            _thread = new Thread(Subscribe)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _thread.Start();
        }

        public bool TryAdd(LogItem item)
        {
            if (_items.IsCompleted)
            {
                return false;
            }

            _items.Add(item);
            return true;
        }

        private void Subscribe()
        {
            foreach (LogItem item in _items.GetConsumingEnumerable())
            {
                try
                {
                    Handle(item);
                }
                catch { }
            }
        }

        protected abstract void Handle(LogItem item);

        public async ValueTask DisposeAsync()
        {
            _items.CompleteAdding();
            try
            {
                _thread.Join();
            }
            catch { }

            await Task.CompletedTask;
        }
    }
}
