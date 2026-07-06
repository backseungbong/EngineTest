using JHLib.Util.List;
using System.Runtime.CompilerServices;

namespace JHLib.Graphics
{
    public static class RenderingThread
    {
        private static readonly Lock _locker;
        private static readonly List<GraphicsLayerManager> _pendingManagers;
        private static readonly LList<WeakReference<GraphicsLayerManager>> _weckrefManagers;

        private static readonly AutoResetEvent _rerendering;
        private static readonly Thread _renderWorker;
        private static int _dedupeChecker;

        static RenderingThread()
        {
            _locker = new Lock();
            _pendingManagers = new();
            _weckrefManagers = new();

            _rerendering = new AutoResetEvent(false);
            _renderWorker = new Thread(Worker) { IsBackground = true };
            _renderWorker.Start();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Rerendering()
        {
            if (Interlocked.Exchange(ref _dedupeChecker, 1) == 0)
                _rerendering.Set();
        }

        private static void Worker()
        {
            while (true)
            {
                _rerendering.WaitOne();
                Volatile.Write(ref _dedupeChecker, 0);

                if (_pendingManagers.Count != 0)
                {
                    lock (_locker)
                    {
                        foreach (var item in _pendingManagers)
                            _weckrefManagers.AddLast(new WeakReference<GraphicsLayerManager>(item));
                        _pendingManagers.Clear();
                    }
                }

                foreach (var info in _weckrefManagers)
                {
                    if (info.Value.TryGetTarget(out var manager) && manager.IsDisposed == false)
                    {
                        if (manager.IsChanged)
                            manager.UpdateLayer();
                    }
                    else
                    {
                        _weckrefManagers.Remove(info.Index);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Register(GraphicsLayerManager manager)
        {
            lock (_locker)
            {
                _pendingManagers.Add(manager);
            }
        }
    }
}