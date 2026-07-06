using JHLib.Util.ArrayControl;
using JHLib.Util.Helper;
using JHLib.Util.Struct;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Hash
{
    using static JHLib.Util.Hash.HashUtil;
    public class KeyArray<TKey, TValue> where TKey : unmanaged, INumber<TKey> where TValue : unmanaged
    {
        private const int NULL = -1;
        private struct Entry { public TKey Key; public int Idx; public int Off; public int Len; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DataRange<TValue> GetDR(TValue[] v, Entry[] e, int i) =>
            new(ref Ref0(v, Ref0(e, i).Off), Ref0(e, i).Len);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DataRange<TValue> GetDR(TValue[] v, ref Entry e, int i) =>
            new(ref Ref0(v, Ref0(ref e, i).Off), Ref0(ref e, i).Len);

        private int[] _buk;
        private Entry[] _ent;
        private int _cap;
        private int _cnt;

        private TValue[] _vuk;
        private int _vap;
        private int _vnt;

        public int Count => _cnt;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TKey EntryKey(int i) => Ref0(_ent, i).Key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataRange<TValue> EntryValue(int i) => GetDR(_vuk, _ent, i);

        public DataRange<TValue> this[TKey key]
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                var i = Buk0(_buk, _cap, key);
                if (i != 0)
                {
                    i--;
                    ref var e = ref Ref0(_ent);
                    do if (Ref0(ref e, i).Key == key) { return GetDR(_vuk, ref e, i); }
                    while ((i = Ref0(ref e, i).Idx) != NULL);
                }
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyArray() => Initialize(MinPrime);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyArray(int cap) => Initialize(GetPrime(cap));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Initialize(int cap)
        {
            _buk = new int[cap];
            _ent = new Entry[cap];
            _cap = cap;

            _vuk = new TValue[2];
            _vap = 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            ref var e0 = ref Ref0(_ent);

            var cnt = _cap;
            var cap = GetPrime(cnt * 2);
            if (cap <= _buk.Length)
            {
                ref var buk0 = ref Ref0(_buk);
                AC.ZeroFill(ref buk0, cnt);

                var i = 0;
                do
                {
                    ref var e = ref Ref0(ref e0, i);
                    ref var b = ref Buk0(ref buk0, cap, e.Key);
                    e.Idx = b - 1; b = i + 1;
                }
                while (++i < cnt);

                _cap = cap;
            }
            else
            {
                var buk = new int[cap];
                var ent = new Entry[cap];

                ref var buk0 = ref Ref0(buk);
                ref var ent0 = ref Ref0(ent);

                var i = 0;
                do
                {
                    ref var e = ref Ref0(ref e0, i);
                    ref var b = ref Buk0(ref buk0, cap, e.Key);
                    e.Idx = b - 1; b = i + 1;
                    Ref0(ref ent0, i) = e;
                }
                while (++i < cnt);

                _buk = buk;
                _ent = ent;
                _cap = cap;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeValueBucket(int add)
        {
            var cnt = _vnt;
            var dst = AC.CopyNew(_vuk, MathHelper.RoundUpToPow2(_vap * 2, cnt + add), cnt);

            _vuk = dst;
            _vap = dst.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int CountInternal(int e) => Ref0(_ent, e).Len;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal ref TValue Occupy0(int e, int l)
        {
            var c = _vnt;
            if (c + l > _vap) ResizeValueBucket(l);
            _vnt = c + l;

            Ref0(_ent, e).Len += l;
            return ref Ref0(_vuk, c);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void AddInternal(int e, TValue d)
        {
            var c = _vnt;
            if (c == _vap) ResizeValueBucket(1);
            _vnt = c + 1;

            Ref0(_ent, e).Len++;
            Ref0(_vuk, c) = d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void AddInInternal(int e, in TValue d)
        {
            var c = _vnt;
            if (c == _vap) ResizeValueBucket(1);
            _vnt = c + 1;

            Ref0(_ent, e).Len++;
            Ref0(_vuk, c) = d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddInternal(int e, TValue[] s, int i, int l) => AddInternal(e, ref Ref0(s, i), l);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void AddInternal(int e, ref TValue s, int l)
        {
            if (l > 0)
            {
                var c = _vnt;
                if (c + l > _vap) ResizeValueBucket(l);
                _vnt = c + l;

                Ref0(_ent, e).Len += l;
                AC.Copy(ref s, _vuk, c, l);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public KeyArrayWriter<TKey, TValue> Set(TKey key)
        {
            var i = _cnt;
            if (i == _cap) Resize();
            _cnt = i + 1;

            ref var b = ref Buk0(_buk, _cap, key);
            ref var e = ref Ref0(_ent, i);
            e.Key = key;
            e.Idx = b - 1; b = i + 1;
            e.Off = _vnt;
            e.Len = 0;
            return new(this, i);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool AddOrWriter(TKey key, out KeyArrayWriter<TKey, TValue> writer)
        {
            ref var b = ref Buk0(_buk, _cap, key);
            ref var e = ref Ref0(_ent);

            var i = b;
            if (i != 0)
            {
                i--;
                do if (Ref0(ref e, i).Key == key) { writer = new(this, i); return false; }
                while ((i = Ref0(ref e, i).Idx) != NULL);
            }

            i = _cnt; _cnt = i + 1;
            if (i == _cap)
            {
                Resize();
                b = ref Buk0(_buk, _cap, key);
                e = ref Ref0(_ent);
            }

            e = ref Ref0(ref e, i);
            e.Key = key;
            e.Idx = b - 1; b = i + 1;
            e.Off = _vnt;
            e.Len = 0;
            writer = new(this, i); return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Get(TKey key, out DataRange<TValue> range)
        {
            var i = Buk0(_buk, _cap, key);
            if (i != 0)
            {
                i--;
                ref var e = ref Ref0(_ent);
                do if (Ref0(ref e, i).Key == key) { range = GetDR(_vuk, ref e, i); return true; }
                while ((i = Ref0(ref e, i).Idx) != NULL);
            }
            range = default; return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Clear()
        {
            if (_cnt != 0)
            {
                AC.ZeroFill(_buk, _cap);

                _cnt = 0;
                _vnt = 0;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ClearEnsureCap(int capacity)
        {
            var cap = GetPrime(capacity);
            if (cap <= _buk.Length)
            {
                if (_cnt != 0)
                    AC.ZeroFill(_buk, _cap);
            }
            else
            {
                _buk = new int[cap];
                _ent = new Entry[cap];
            }
            _cap = cap;
            _cnt = 0;
            _vnt = 0;
        }
    }
}