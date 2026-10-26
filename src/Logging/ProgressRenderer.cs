using System;
using System.Diagnostics;

namespace Tur.Logging
{
    /// <summary>
    /// Renders one transient status line on interactive terminals only.
    /// Automatically disabled when stdout/stdin is redirected, so subprocess
    /// consumers never see carriage returns or partially drawn lines.
    /// </summary>
    public sealed class ProgressRenderer : IDisposable
    {
        public static ProgressRenderer Current { get; private set; }

        private readonly bool _enabled;
        private readonly object _lock = new();
        private readonly Stopwatch _throttle = Stopwatch.StartNew();
        private bool _lineActive;
        private bool _disposed;

        public ProgressRenderer(bool requested)
        {
            try
            {
                _enabled = requested && !Console.IsOutputRedirected && !Console.IsInputRedirected;
            }
            catch (Exception)
            {
                _enabled = false;
            }

            if (_enabled)
            {
                Current = this;
            }
        }

        public void Report(string text)
        {
            if (!_enabled || _disposed || text == null)
            {
                return;
            }

            lock (_lock)
            {
                if (_lineActive && _throttle.ElapsedMilliseconds < 150)
                {
                    return;
                }

                _throttle.Restart();
                try
                {
                    ClearLineCore();
                    var width = GetWindowWidth();
                    if (width > 1 && text.Length > width - 1)
                    {
                        text = text.Substring(0, width - 1);
                    }

                    Console.Write(text);
                    _lineActive = true;
                }
                catch (Exception) { }
            }
        }

        /// <summary>Clears the transient line, if any, before regular log output.</summary>
        public static void ClearActive()
        {
            var current = Current;
            current?.ClearLine();
        }

        public void ClearLine()
        {
            if (!_enabled || _disposed)
            {
                return;
            }

            lock (_lock)
            {
                ClearLineCore();
            }
        }

        private void ClearLineCore()
        {
            if (!_lineActive)
            {
                return;
            }

            try
            {
                var width = GetWindowWidth();
                if (width > 1)
                {
                    Console.Write('\r');
                    Console.Write(new string(' ', width - 1));
                    Console.Write('\r');
                }
            }
            catch (Exception) { }

            _lineActive = false;
        }

        private static int GetWindowWidth()
        {
            // Queried fresh on every render so a window resize can never desync us.
            try
            {
                return Console.WindowWidth;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ClearLine();
            if (ReferenceEquals(Current, this))
            {
                Current = null;
            }
        }
    }
}
