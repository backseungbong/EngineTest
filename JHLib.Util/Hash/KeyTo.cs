using JHLib.Util.ArrayControl;
using JHLib.Util.Struct;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Hash
{
    using static JHLib.Util.Hash.HashUtil;
    using static JHLib.Util.Helper.RefCommand;
    public class KeyTo<TKey, TValue> where TKey : unmanaged, INumber<TKey>
    {
        private const int NULL = -1;
        private struct Entry { public TKey Key; public int Idx; }

        private int[] _buk;
        private Entry[] _ent;
        private TValue[] _val;
        private int _cap;
        private int _cnt;
        private int _fnt;
        private int _fdx;

        public int Count => _cnt - _fnt;
        public int EntryCount => _cnt;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TKey EntryKey(int i) => Ref0(_ent, i).Key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue EntryValue(int i) => ref Ref0(_val, i);

        public ref TValue this[TKey key]
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                if (TryGet(key, out var i)) { return ref Ref0(_val, i); }
                return ref Empty<TValue>();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyTo() => Initialize(MinPrime);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyTo(int cap) => Initialize(GetPrime(cap));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Initialize(int cap)
        {
            _buk = new int[cap];
            _ent = new Entry[cap];
            _val = new TValue[cap];
            _cap = cap;
            _fdx = NULL;
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
                RefClear(_val, cnt, cap - cnt);

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
                ref var v0 = ref Ref0(_val);

                var buk = new int[cap];
                var ent = new Entry[cap];
                var val = new TValue[cap];

                ref var buk0 = ref Ref0(buk);
                ref var ent0 = ref Ref0(ent);
                ref var val0 = ref Ref0(val);

                var i = 0;
                do
                {
                    ref var e = ref Ref0(ref e0, i);
                    ref var b = ref Buk0(ref buk0, cap, e.Key);
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryAdd(TKey key, out int idx)
        {
            ref var b = ref Buk0(_buk, _cap, key);
            ref var e = ref Ref0(_ent);

            var i = b;
            if (i != 0)
            {
                i--;
                do if (Ref0(ref e, i).Key == key) { idx = i; return false; }
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
                    b = ref Buk0(_buk, _cap, key);
                    e = ref Ref0(_ent);
                }
            }

            e = ref Ref0(ref e, i);
            e.Key = key;
            e.Idx = b - 1; b = i + 1;
            idx = i; return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryGet(TKey key, out int idx)
        {
            var i = Buk0(_buk, _cap, key);
            if (i != 0)
            {
                i--;
                ref var e = ref Ref0(_ent);
                do if (Ref0(ref e, i).Key == key) { idx = i; return true; }
                while ((i = Ref0(ref e, i).Idx) != NULL);
            }
            idx = 0; return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryRmv(TKey key, out int idx)
        {
            if (_fnt < _cnt)
            {
                ref var b = ref Buk0(_buk, _cap, key);
                int i = b, p;
                if (i != 0)
                {
                    i--;
                    ref var e = ref Ref0(_ent);
                    if (Ref0(ref e, i).Key != key)
                    {
                        do if ((i = Ref0(ref e, p = i).Idx) == NULL) { goto EX; }
                        while (Ref0(ref e, i).Key != key);
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
        public void Set(TKey key, in TValue val)
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
                if (i == _cap) Resize();
            }

            ref var b = ref Buk0(_buk, _cap, key);
            ref var e = ref Ref0(_ent, i);
            e.Key = key;
            e.Idx = b - 1; b = i + 1;
            Ref0(_val, i) = val;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Add(TKey key, in TValue val)
        {
            if (TryAdd(key, out var i)) { Ref0(_val, i) = val; return true; }
            return false;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ref TValue AddRef(TKey key)
        {
            TryAdd(key, out var i);
            return ref Ref0(_val, i);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool AddOrRefValue(TKey key, out RefValue<TValue> refValue)
        {
            var result = TryAdd(key, out var i); refValue = new(ref Ref0(_val, i));
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(TKey key, out TValue val)
        {
            if (TryGet(key, out var i)) { val = Ref0(_val, i); return true; }
            val = default; return false;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public TValue GetDefault(TKey key, TValue defaultValue = default)
        {
            if (TryGet(key, out var i)) { return Ref0(_val, i); }
            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Update(TKey key, in TValue val)
        {
            if (TryGet(key, out var i)) { Ref0(_val, i) = val; return true; }
            return false;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Update(TKey key, in TValue val, out TValue oldval)
        {
            if (TryGet(key, out var i)) { RepV(_val, i, val, out oldval); return true; }
            oldval = default; return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Remove(TKey key)
        {
            if (TryRmv(key, out var i)) { Ref0(_val, i) = default; return true; }
            return false;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Remove(TKey key, out TValue val)
        {
            if (TryRmv(key, out var i)) { RepV(_val, i, default, out val); return true; }
            val = default; return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public TValue[] ToArrayValues()
        {
            var f = _fnt;
            var l = _cnt - f;
            if (l == 0)
                return [];

            var rst = new TValue[l];
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
        public void FillZeroValues()
        {
            if (_cnt != 0)
                RefClear(_val, _cnt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => Clear(RuntimeHelpers.IsReferenceOrContainsReferences<TValue>());

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Clear(bool withValueBucket)
        {
            if (_cnt != 0)
            {
                AC.ZeroFill(_buk, _cap);
                if (withValueBucket) RefClear(_val, _cnt);
                _cnt = 0;
                _fnt = 0;
                _fdx = NULL;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ClearEnsureCap(int capacity, bool withValues = true)
        {
            var cap = GetPrime(capacity);
            if (cap <= _buk.Length)
            {
                if (_cnt != 0)
                {
                    AC.ZeroFill(_buk, _cap);
                    if (withValues) RefClear(_val, _cnt);
                }
            }
            else
            {
                _buk = new int[cap];
                _ent = new Entry[cap];
                _val = new TValue[cap];
            }
            _cap = cap;
            _cnt = 0;
            _fnt = 0;
            _fdx = NULL;
        }
    }
}