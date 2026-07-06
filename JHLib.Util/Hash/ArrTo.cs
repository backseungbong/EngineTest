using JHLib.Util.ArrayControl;
using JHLib.Util.Cache;
using JHLib.Util.Helper;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Hash
{
    using static JHLib.Util.Hash.HashUtil;
    using static JHLib.Util.Helper.RefCommand;
    public class ArrTo<T>
    {
        private const int NULL = -1;
        private struct Entry { public byte[] Key; public int Hash; public int Idx; }

        private int[] _buk;
        private Entry[] _ent;
        private T[] _val;
        private int _cap;
        private int _cnt;
        private int _fnt;
        private int _fdx;

        public int Count => _cnt - _fnt;
        public int EntryCount => _cnt;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] EntryKey(int i) => Ref0(_ent, i).Key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T EntryValue(int i) => ref Ref0(_val, i);

        public ref T this[byte[] key]
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                if (TryGet(ref Ref0(key), key.Length, out var i)) { return ref Ref0(_val, i); }
                return ref Empty<T>();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArrTo() => Initialize(MinPow2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArrTo(int cap) => Initialize(MathHelper.RoundUpToPow2(MinPow2, cap));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Initialize(int cap)
        {
            _buk = new int[cap];
            _ent = new Entry[cap];
            _val = new T[cap];
            _cap = cap;
            _fdx = NULL;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            ref var e0 = ref Ref0(_ent);
            ref var v0 = ref Ref0(_val);
            var cnt = _cap;

            var cap = cnt * 2;
            var buk = new int[cap];
            var ent = new Entry[cap];
            var val = new T[cap];
            var msk = cap - 1;

            ref var buk0 = ref Ref0(buk);
            ref var ent0 = ref Ref0(ent);
            ref var val0 = ref Ref0(val);

            var i = 0;
            do
            {
                ref var e = ref Ref0(ref e0, i);
                ref var b = ref Ref0(ref buk0, e.Hash & msk);
                e.Idx = b - 1; b = i + 1;
                Ref0(ref ent0, i) = e;
                Ref0(ref val0, i) = Ref0(ref v0, i);
            }
            while (++i < cnt);

            _buk = buk;
            _ent = ent;
            _val = val;
            _cap = cap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryAdd(byte[] key, out int idx)
        {
            ref var key0 = ref Ref0(key);
            var h = XXHash.H32(ref key0, key.Length);
            ref var b = ref Ref0(_buk, h & _cap - 1);
            ref var e = ref Ref0(_ent);

            var i = b;
            if (i != 0)
            {
                i--;
                do if (Ref0(ref e, i).Hash == h && Cmp(Ref0(ref e, i).Key, ref key0, key.Length)) { idx = i; return false; }
                while ((i = Ref0(ref e, i).Idx) != NULL);
            }

            i = _fnt;
            if (i != 0)
            {
                _fnt = i - 1; i = _fdx;
                _fdx = Ref0(ref e, i).Idx;
            }
            else
            {
                i = _cnt; _cnt = i + 1;
                if (_cap == i)
                {
                    Resize();
                    b = ref Ref0(_buk, h & _cap - 1);
                    e = ref Ref0(_ent);
                }
            }

            e = ref Ref0(ref e, i);
            e.Key = key;
            e.Hash = h;
            e.Idx = b - 1; b = i + 1;
            idx = i; return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGet(ref byte key0, int len, out int idx)
        {
            var h = XXHash.H32(ref key0, len);
            var i = Ref0(_buk, h & _cap - 1);
            if (i != 0)
            {
                i--;
                ref var e = ref Ref0(_ent);
                do if (Ref0(ref e, i).Hash == h && Cmp(Ref0(ref e, i).Key, ref key0, len)) { idx = i; return true; }
                while ((i = Ref0(ref e, i).Idx) != NULL);
            }
            idx = 0; return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryRmv(ref byte key0, int len, out int idx)
        {
            if (_fnt < _cnt)
            {
                var h = XXHash.H32(ref key0, len);
                ref var b = ref Ref0(_buk, h & _cap - 1);
                int i = b, p;
                if (i != 0)
                {
                    i--;
                    ref var e = ref Ref0(_ent);
                    if (Ref0(ref e, i).Hash != h || !Cmp(Ref0(ref e, i).Key, ref key0, len))
                    {
                        do if ((i = Ref0(ref e, p = i).Idx) == NULL) { goto EX; }
                        while (Ref0(ref e, i).Hash != h || !Cmp(Ref0(ref e, i).Key, ref key0, len));
                        Ref0(ref e, p).Idx = Ref0(ref e, i).Idx;
                    }
                    else { b = Ref0(ref e, i).Idx + 1; }
                    Ref0(ref e, i).Idx = _fdx; _fdx = i; _fnt++;
                    idx = i; return true;
                }
            }
        EX: idx = 0; return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Set(byte[] key, in T val)
        {
            var i = _fnt;
            if (i != 0)
            {
                _fnt = i - 1; i = _fdx;
                _fdx = Ref0(_ent, i).Idx;
            }
            else
            {
                i = _cnt; _cnt = i + 1;
                if (_cap == i) Resize();
            }

            var h = XXHash.H32(key);
            ref var b = ref Ref0(_buk, h & _cap - 1);
            ref var e = ref Ref0(_ent, i);

            e.Key = key;
            e.Hash = h;
            e.Idx = b - 1; b = i + 1;
            Ref0(_val, i) = val;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Add(byte[] key, in T val)
        {
            if (TryAdd(key, out var i)) { Ref0(_val, i) = val; return true; }
            return false;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ref T AddRef(byte[] key)
        {
            TryAdd(key, out var i);
            return ref Ref0(_val, i);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool AddOrRefValue(byte[] key, out RefValue<T> refValue)
        {
            var result = TryAdd(key, out var i); refValue = new(_val, i);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(byte[] key, out T val) =>
            Get(ref Ref0(key), key.Length, out val);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(ReadOnlySpan<byte> span, out T val) =>
            Get(ref MemoryMarshal.GetReference(span), span.Length, out val);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(Span<byte> span, out T val) =>
            Get(ref MemoryMarshal.GetReference(span), span.Length, out val);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Get(ref byte key0, int len, out T val)
        {
            if (TryGet(ref key0, len, out var i)) { val = Ref0(_val, i); return true; }
            val = default; return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetDefault(byte[] key, T defaultValue = default) =>
            GetDefault(ref Ref0(key), key.Length, defaultValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetDefault(ReadOnlySpan<byte> span, T defaultValue = default) =>
            GetDefault(ref MemoryMarshal.GetReference(span), span.Length, defaultValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetDefault(Span<byte> span, T defaultValue = default) =>
            GetDefault(ref MemoryMarshal.GetReference(span), span.Length, defaultValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public T GetDefault(ref byte key0, int len, T defaultValue = default)
        {
            if (len > 0 && TryGet(ref key0, len, out var i)) { return Ref0(_val, i); }
            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Update(byte[] key, in T val) => Update(ref Ref0(key), key.Length, val);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Update(ref byte key0, int len, in T val)
        {
            if (TryGet(ref key0, len, out var i)) { Ref0(_val, i) = val; return true; }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Update(byte[] key, in T val, out T oldval) => Update(ref Ref0(key), key.Length, val, out oldval);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Update(ref byte key0, int len, in T val, out T oldval)
        {
            if (TryGet(ref key0, len, out var i)) { RepV(_val, i, val, out oldval); return true; }
            oldval = default; return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(byte[] key) => Remove(ref Ref0(key), key.Length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Remove(ref byte key0, int len)
        {
            if (TryRmv(ref key0, len, out var i)) { Ref0(_val, i) = default; return true; }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(byte[] key, out T val) => Remove(ref Ref0(key), key.Length, out val);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Remove(ref byte key0, int len, out T val)
        {
            if (TryRmv(ref key0, len, out var i)) { RepV(_val, i, default, out val); return true; }
            val = default; return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public T[] ToArrayOnlyValues()
        {
            var f = _fnt;
            var l = _cnt - f;
            if (l == 0)
                return [];

            var rst = new T[l];
            if (f != 0)
            {
                ref var r0 = ref Ref0(rst);
                ref var b0 = ref Ref0(_buk);
                ref var e0 = ref Ref0(_ent);
                ref var v0 = ref Ref0(_val);
                int c = 0, n = 0;

                while (true)
                {
                    var i = Ref0(ref b0, n++);
                    if (i != 0)
                    {
                        i--;
                        do Ref0(ref r0, c++) = Ref0(ref v0, i);
                        while ((i = Ref0(ref e0, i).Idx) != NULL);
                        if (c == l) { break; }
                    }
                }
            }
            else { RefCopy(_val, rst, l); }
            return rst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => Clear(RuntimeHelpers.IsReferenceOrContainsReferences<T>());

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Clear(bool withValueBucket)
        {
            if (_cnt != 0)
            {
                AC.ZeroFill(_buk);
                if (withValueBucket) RefClear(_val, _cnt);

                _cnt = 0;
                _fnt = 0;
                _fdx = NULL;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ClearWithDispose()
        {
            if (_cnt != 0)
            {
                AC.ZeroFill(_buk);

                if (THelper<T>.IsDisposable)
                {
                    ref var v = ref Ref0(_val);
                    ref var e = ref Ref0(ref v, _cnt);
                    do
                    {
                        if (v is IDisposable disposal)
                            disposal.Dispose();
                        v = default;
                        v = ref Ref0(ref v, 1);
                    }
                    while (LessThan(ref v, ref e));
                }
                else
                {
                    RefClear(_val, _cnt);
                }

                _cnt = 0;
                _fnt = 0;
                _fdx = NULL;
            }
        }
    }
}