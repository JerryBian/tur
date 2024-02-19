using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Tur.Logging
{
    public abstract class BlockingAppender : IAppender
    {
        private readonly Thread _thread;
        private readonly BlockingCollection<TurLogItem> _items;

        public BlockingAppender()
        {
            _items = new BlockingCollection<TurLogItem>(Constants.LogItemsCapacity);
            _thread = new Thread(Subscribe)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _thread.Start();
        }

        public void Add(TurLogItem item)
        {
            if (_items.IsCompleted)
            {
                try
                {
                    Handle(item);
                }
                catch { }
            }
            else
            {
                _items.Add(item);
            }
        }

        private void Subscribe()
        {
            foreach (var item in _items.GetConsumingEnumerable())
            {
                try
                {
                    Handle(item);
                }
                catch { }
            }
        }

        protected abstract void Handle(TurLogItem item);

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
