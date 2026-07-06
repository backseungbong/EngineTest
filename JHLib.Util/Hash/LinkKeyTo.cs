using JHLib.Util.ArrayControl;
using JHLib.Util.Cache;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Hash
{
    using static JHLib.Util.Hash.HashUtil;
    using static JHLib.Util.Helper.RefCommand;
    public class LinkKeyTo<TKey, TValue>
    {
        private const int NULL = -1;
        private struct Entry
        {
            public TKey Key; public int Idx; public int Prev; public int Next;
            public readonly bool Equals(TKey key) => EqualityComparer<TKey>.Default.Equals(Key, key);
        }

        private int[] _buk;
        private Entry[] _ent;
        private TValue[] _val;
        private int _cap;
        private int _cnt;
        private int _fnt;
        private int _fdx;
        private int _head;
        public int Count => _cnt - _fnt;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LinkKeyTo() => Initialize(MinPrime);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LinkKeyTo(int cap) => Initialize(GetPrime(cap));

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
            ref var v0 = ref Ref0(_val);
            var cnt = _cap;

            var cap = GetPrime(cnt * 2);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryAdd(TKey key, out int idx)
        {
            ref var b = ref Buk0(_buk, _cap, key);
            ref var e = ref Ref0(_ent);

            var i = b;
            if (i != 0)
            {
                i--;
                do if (Ref0(ref e, i).Equals(key)) { idx = i; return false; }
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

            AddLink(ref e, _head, i);

            e = ref Ref0(ref e, i);
            e.Key = key;
            e.Idx = b - 1; b = i + 1;
            idx = i; return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGet(TKey key, out int idx)
        {
            var i = Buk0(_buk, _cap, key);
            if (i != 0)
            {
                i--;
                ref var e = ref Ref0(_ent);
                do if (Ref0(ref e, i).Equals(key)) { idx = i; return true; }
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
                    if (!Ref0(ref e, i).Equals(key))
                    {
                        do if ((i = Ref0(ref e, p = i).Idx) == NULL) { goto EX; }
                        while (!Ref0(ref e, i).Equals(key));
                        Ref0(ref e, p).Idx = Ref0(ref e, i).Idx;
                    }
                    else { b = Ref0(ref e, i).Idx + 1; }
                    Ref0(ref e, i).Idx = _fdx; _fdx = i; _fnt++;
                    if (_head == i) { _head = Ref0(ref e, i).Next; }
                    RmvLink(ref e, i);
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
                if (_cap == i) Resize();
            }

            ref var b = ref Buk0(_buk, _cap, key);
            ref var e = ref Ref0(_ent);

            AddLink(ref e, _head, i);

            e = ref Ref0(ref e, i);
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
            var result = TryAdd(key, out var i); refValue = new(_val, i);
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetFirst(out TValue val)
        {
            if (_fnt < _cnt) { val = _val[_head]; return true; }
            val = default; return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetLast(out TValue val)
        {
            if (_fnt < _cnt) { val = _val[_ent[_head].Prev]; return true; }
            val = default; return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PopFirst(out TValue val)
        {
            if (_fnt < _cnt) { return Remove(_ent[_head].Key, out val); }
            val = default; return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PopLast(out TValue val)
        {
            if (_fnt < _cnt) { return Remove(_ent[_ent[_head].Prev].Key, out val); }
            val = default; return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FirstToLast() => _head = _ent[_head].Next;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LastToFirst() => _head = _ent[_head].Prev;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => Clear(RuntimeHelpers.IsReferenceOrContainsReferences<TValue>());

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Clear(bool withValueBucket)
        {
            if (_cnt != 0)
            {
                AC.ZeroFill(_buk);
                Ref0(_ent) = default;
                if (withValueBucket) RefClear(_val, _cnt);

                _cnt = 0;
                _fnt = 0;
                _fdx = NULL;
                _head = 0;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ClearWithDispose()
        {
            if (_cnt != 0)
            {
                AC.ZeroFill(_buk);
                Ref0(_ent) = default;

                if (THelper<TValue>.IsDisposable)
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
                _head = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddLink(ref Entry e, int next, int add)
        {
            var prev = Ref0(ref e, next).Prev;
            Ref0(ref e, prev).Next = add;
            Ref0(ref e, next).Prev = add;
            Ref0(ref e, add).Prev = prev;
            Ref0(ref e, add).Next = next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RmvLink(ref Entry e, int rmv)
        {
            var prev = Ref0(ref e, rmv).Prev;
            var next = Ref0(ref e, rmv).Next;
            Ref0(ref e, next).Prev = prev;
            Ref0(ref e, prev).Next = next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefEnumerator GetEnumerator() => new(this);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref struct RefEnumerator(LinkKeyTo<TKey, TValue> list)
        {
            private readonly ref Entry _ent = ref Ref0(list._ent);
            private readonly ref TValue _val = ref Ref0(list._val);
            private int _nxt = list._head;
            private int _cnt = list.Count;
            public readonly int Count => _cnt;
            public readonly RefEnumerator GetEnumerator() => this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => --_cnt >= 0;
            public ref TValue Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    var i = _nxt; _nxt = Ref0(ref _ent, i).Next;
                    return ref Ref0(ref _val, i);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefEnumeratorKeyValue GetEnumeratorKeyValue() => new(this);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref struct RefEnumeratorKeyValue(LinkKeyTo<TKey, TValue> list)
        {
            [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly ref struct KeyValue(TKey key, ref TValue refValue)
            {
                public readonly TKey Key = key;
                public readonly ref TValue RefValue = ref refValue;
            }

            private readonly ref Entry _ent = ref Ref0(list._ent);
            private readonly ref TValue _val = ref Ref0(list._val);
            private int _nxt = list._head;
            private int _cnt = list.Count;
            public readonly int Count => _cnt;
            public readonly RefEnumeratorKeyValue GetEnumerator() => this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => --_cnt >= 0;
            public KeyValue Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    var i = _nxt; _nxt = Ref0(ref _ent, i).Next;
                    return new(Ref0(ref _ent, i).Key, ref Ref0(ref _val, i));
                }
            }
        }
    }
}