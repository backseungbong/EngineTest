using JHLib.Util.ArrayControl;
using JHLib.Util.Cache;
using JHLib.Util.Helper;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Hash
{
    using static JHLib.Util.Hash.HashUtil;
    using static JHLib.Util.Helper.RefCommand;
    public class LinkStrTo<T>
    {
        private const int NULL = -1;
        private struct Entry { public string Key; public int Hash; public int Idx; public int Prev; public int Next; }

        private int[] _buk;
        private Entry[] _ent;
        private T[] _val;
        private int _cap;
        private int _cnt;
        private int _fnt;
        private int _fdx;
        private int _head;
        public int Count => _cnt - _fnt;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LinkStrTo() => Initialize(MinPow2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LinkStrTo(int cap) => Initialize(MathHelper.RoundUpToPow2(MinPow2, cap));

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
        private bool TryAdd(string key, out int idx)
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

            AddLink(ref e, _head, i);

            e = ref Ref0(ref e, i);
            e.Key = key;
            e.Hash = h;
            e.Idx = b - 1; b = i + 1;
            idx = i; return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGet(string key, out int idx)
        {
            ref var key0 = ref Ref0(key);
            var h = XXHash.H32(ref key0, key.Length);
            var i = Ref0(_buk, h & _cap - 1);
            if (i != 0)
            {
                i--;
                ref var e = ref Ref0(_ent);
                do if (Ref0(ref e, i).Hash == h && Cmp(Ref0(ref e, i).Key, ref key0, key.Length)) { idx = i; return true; }
                while ((i = Ref0(ref e, i).Idx) != NULL);
            }
            idx = 0; return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryRmv(string key, out int idx)
        {
            if (_fnt < _cnt)
            {
                ref var key0 = ref Ref0(key);
                var h = XXHash.H32(ref key0, key.Length);
                ref var b = ref Ref0(_buk, h & _cap - 1);
                int i = b, p;
                if (i != 0)
                {
                    i--;
                    ref var e = ref Ref0(_ent);
                    if (Ref0(ref e, i).Hash != h || !Cmp(Ref0(ref e, i).Key, ref key0, key.Length))
                    {
                        do if ((i = Ref0(ref e, p = i).Idx) == NULL) { goto EX; }
                        while (Ref0(ref e, i).Hash != h || !Cmp(Ref0(ref e, i).Key, ref key0, key.Length));
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
        public void Set(string key, in T val)
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
            ref var e = ref Ref0(_ent);

            AddLink(ref e, _head, i);

            e = ref Ref0(ref e, i);
            e.Key = key;
            e.Hash = h;
            e.Idx = b - 1; b = i + 1;
            Ref0(_val, i) = val;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Add(string key, in T val)
        {
            if (TryAdd(key, out var i)) { Ref0(_val, i) = val; return true; }
            return false;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ref T AddRef(string key)
        {
            TryAdd(key, out var i);
            return ref Ref0(_val, i);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool AddOrRefValue(string key, out RefValue<T> refValue)
        {
            var result = TryAdd(key, out var i); refValue = new(_val, i);
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Get(string key, out T val)
        {
            if (TryGet(key, out var i)) { val = Ref0(_val, i); return true; }
            val = default; return false;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public T GetDefault(string key, T defaultValue = default)
        {
            if (TryGet(key, out var i)) { return Ref0(_val, i); }
            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Update(string key, in T val)
        {
            if (TryGet(key, out var i)) { Ref0(_val, i) = val; return true; }
            return false;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Update(string key, in T val, out T oldval)
        {
            if (TryGet(key, out var i)) { RepV(_val, i, val, out oldval); return true; }
            oldval = default; return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Remove(string key)
        {
            if (TryRmv(key, out var i)) { Ref0(_val, i) = default; return true; }
            return false;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Remove(string key, out T val)
        {
            if (TryRmv(key, out var i)) { RepV(_val, i, default, out val); return true; }
            val = default; return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetFirst(out T val)
        {
            if (_fnt < _cnt) { val = _val[_head]; return true; }
            val = default; return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetLast(out T val)
        {
            if (_fnt < _cnt) { val = _val[_ent[_head].Prev]; return true; }
            val = default; return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PopFirst(out T val)
        {
            if (_fnt < _cnt) { return Remove(_ent[_head].Key, out val); }
            val = default; return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PopLast(out T val)
        {
            if (_fnt < _cnt) { return Remove(_ent[_ent[_head].Prev].Key, out val); }
            val = default; return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FirstToLast() => _head = _ent[_head].Next;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LastToFirst() => _head = _ent[_head].Prev;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => Clear(RuntimeHelpers.IsReferenceOrContainsReferences<T>());

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
        public ref struct RefEnumerator(LinkStrTo<T> list)
        {
            private readonly ref Entry _ent = ref Ref0(list._ent);
            private readonly ref T _val = ref Ref0(list._val);
            private int _nxt = list._head;
            private int _cnt = list.Count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => --_cnt >= 0;
            public ref T Current
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
        public ref struct RefEnumeratorKeyValue(LinkStrTo<T> list)
        {
            [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly ref struct KeyValue(string key, ref T refValue)
            {
                public readonly string Key = key;
                public readonly ref T RefValue = ref refValue;
            }

            private readonly ref Entry _ent = ref Ref0(list._ent);
            private readonly ref T _val = ref Ref0(list._val);
            private int _nxt = list._head;
            private int _cnt = list.Count;
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