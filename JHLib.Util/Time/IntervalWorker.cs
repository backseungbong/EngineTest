using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Time
{
    public abstract class RepeatItem : IDisposable
    {
        internal int Interval;
        internal abstract bool TryWork();
        public abstract void Dispose();
    }

    internal class Worker : RepeatItem
    {
        internal Action Work;
        public override void Dispose() => Work = null;
        internal Worker(Action action, int interval)
        {
            Work = action;
            Interval = interval;
        }
        internal override bool TryWork()
        {
            var work = Work;
            if (work != null) { work(); return true; }
            return false;
        }
    }

    internal class Worker<T> : RepeatItem
    {
        internal Action<T> Work;
        internal T Data;
        public override void Dispose() => Work = null;
        internal Worker(Action<T> action, T data, int interval)
        {
            Work = action;
            Data = data;
            Interval = interval;
        }
        internal override bool TryWork()
        {
            var work = Work;
            if (work != null) { work(Data); return true; }
            return false;
        }
    }

    public static class IntervalWorker
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 12)]
        private readonly struct HeapItem
        {
            public readonly long Tick;
            public readonly int Index;
            public HeapItem(long tick, int index)
            {
                Tick = tick;
                Index = index;
            }
        }

        private static RepeatItem[] _wbuk;
        private static int _wcap;
        private static int _wcnt;
        private static int _wfnt;
        private static int _wfdx;
        private static int _wlck;

        private static HeapItem[] _hbuk;
        private static int _hcap;
        private static int _hcnt;
        private static int _hlck;

        static IntervalWorker()
        {
            _wbuk = new RepeatItem[128];
            _wcap = 128;
            _wcnt = 0;
            _wfnt = 0;
            _wfdx = 0;
            _wlck = 0;

            _hbuk = new HeapItem[128];
            _hcap = 128;
            _hcnt = 0;
            _hlck = 0;

            // Interval.OnInterval += Worker;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Worker()
        {
            if (Interval.Elapsed < _hbuk[0].Tick)
                return;

            while (true)
            {
                while (Interlocked.CompareExchange(ref _hlck, 1, 0) != 0) ;
                var b = _hbuk;
                var c = _hcnt;
                if (c == 0 || b[0].Tick > Interval.Elapsed) { _hlck = 0; return; }
                var i = b[0].Index;
                RmvHeap(b, c - 1);
                _hcnt = c - 1;
                _hlck = 0;

                if (_wbuk[i].TryWork())
                {
                    do
                    {
                        while (Interlocked.CompareExchange(ref _hlck, 1, 0) != 0) ;
                        c = _hcnt; _hcnt = c + 1;
                        if (c == _hcap) _hcap = Resize(ref _hbuk, c);
                        b = _hbuk;
                        AddHeap(b, c, _wbuk[i].Interval, i);
                        if (b[0].Tick > Interval.Elapsed) { _hlck = 0; return; }
                        i = b[0].Index;
                        RmvHeap(b, c);
                        _hcnt = c;
                        _hlck = 0;
                    }
                    while (_wbuk[i].TryWork());
                }

                while (Interlocked.CompareExchange(ref _wlck, 1, 0) != 0) ;
                _wbuk[i].Interval = _wfdx;
                _wfdx = i;
                _wfnt++;
                _wlck = 0;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe int Resize(ref HeapItem[] h, int c)
        {
            var r = new HeapItem[c * 2];
            fixed (HeapItem* s0 = &h[0])
            fixed (HeapItem* d0 = &r[0])
            {
                var s = (byte*)s0;
                var d = (byte*)d0;
                var e = (byte*)d0 + c * sizeof(HeapItem);
                do
                {
                    *(ulong*)(d + 00) = *(ulong*)(s + 00);
                    *(ulong*)(d + 08) = *(ulong*)(s + 08);
                    *(ulong*)(d + 16) = *(ulong*)(s + 16);
                    *(ulong*)(d + 24) = *(ulong*)(s + 24); s += 32;
                }
                while ((d += 32) < e);
            }

            h = r;
            return c * 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe int Resize(ref RepeatItem[] h, int c)
        {
            var r = new RepeatItem[c * 2];
            Array.Copy(h, 0, r, 0, c);

            h = r;
            return c * 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static RepeatItem AddWork(Action work, int interval = 15)
        {
            if (interval < 15) interval = 15;
            return RegisterInternal(new Worker(work, interval));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static RepeatItem AddWork<T>(Action<T> work, T data, int interval = 15)
        {
            if (interval < 15) interval = 15;
            return RegisterInternal(new Worker<T>(work, data, interval));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RepeatItem RegisterInternal(RepeatItem w)
        {
            while (Interlocked.CompareExchange(ref _wlck, 1, 0) != 0) ;
            var i = _wfnt;
            if (i != 0)
            {
                _wfnt = i - 1;
                i = _wfdx; _wfdx = _wbuk[i].Interval;
            }
            else
            {
                i = _wcnt; _wcnt = i + 1;
                if (i == _wcap) _wcap = Resize(ref _wbuk, i);
            }
            _wbuk[i] = w;
            _wlck = 0;

            while (Interlocked.CompareExchange(ref _hlck, 1, 0) != 0) ;
            var c = _hcnt;
            if (c == _hcap) _hcap = Resize(ref _hbuk, c);
            AddHeap(_hbuk, c, w.Interval, i);
            _hcnt = c + 1;
            _hlck = 0;
            return w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static void AddHeap(HeapItem[] b, int l, int v, int x)
        {
            fixed (HeapItem* p = &b[0])
            {
                var t = Interval.Elapsed + v;
                var i = l;
                if (i != 0)
                {
                    var j = i - 1 >> 1;
                    if (t < p[j].Tick)
                    {
                        do { p[i] = p[j]; i = j; }
                        while (j != 0 && t < p[j = i - 1 >> 1].Tick);
                    }
                }
                p[i] = new(t, x);
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static void RmvHeap(HeapItem[] b, int l)
        {
            fixed (HeapItem* p = &b[0])
            {
                var t = p[l];
                var i = 0;
                if (l > 1)
                {
                    var j = 1;
                    while (true)
                    {
                        if (p[j].Tick > p[j + 1].Tick) j++;
                        if (p[j].Tick < t.Tick)
                        {
                            p[i] = p[j]; i = j; j = i * 2 + 1;
                            if (j < l) continue;
                            if (j > l) break;
                            if (p[j].Tick < t.Tick) { p[i] = p[j]; i = j; }
                        }
                        break;
                    }
                }
                p[i] = t;
                return;
            }
        }
    }
}