using JHLib.Util.ArrayControl;
using JHLib.Util.Helper;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Hash
{
    using static JHLib.Util.Hash.HashUtil;
    public class LinkStrMap
    {
        private const int NULL = -1;
        private struct Entry { public string Key; public int Hash; public int Idx; public int Prev; public int Next; }

        private int[] _buk;
        private Entry[] _ent;
        private int _cap;
        private int _cnt;
        private int _fnt;
        private int _fdx;
        private int _head;
        public int Count => _cnt - _fnt;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LinkStrMap() => Initialize(MinPow2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LinkStrMap(int cap) => Initialize(MathHelper.RoundUpToPow2(MinPow2, cap));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Initialize(int cap)
        {
            _buk = new int[cap];
            _ent = new Entry[cap];
            _cap = cap;
            _fdx = NULL;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            ref var e0 = ref Ref0(_ent);
            var cnt = _cap;

            var cap = cnt * 2;
            var buk = new int[cap];
            var ent = new Entry[cap];
            var msk = cap - 1;

            ref var buk0 = ref Ref0(buk);
            ref var ent0 = ref Ref0(ent);

            var i = 0;
            do
            {
                ref var e = ref Ref0(ref e0, i);
                ref var b = ref Ref0(ref buk0, e.Hash & msk);
                e.Idx = b - 1; b = i + 1;
                Ref0(ref ent0, i) = e;
            }
            while (++i < cnt);

            _buk = buk;
            _ent = ent;
            _cap = cap;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Set(string key)
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
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Add(string key)
        {
            ref var key0 = ref Ref0(key);
            var h = XXHash.H32(ref key0, key.Length);
            ref var b = ref Ref0(_buk, h & _cap - 1);
            ref var e = ref Ref0(_ent);

            var i = b;
            if (i != 0)
            {
                i--;
                do if (Ref0(ref e, i).Hash == h && Cmp(Ref0(ref e, i).Key, ref key0, key.Length)) { return false; }
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
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Exist(string key)
        {
            ref var key0 = ref Ref0(key);
            var h = XXHash.H32(ref key0, key.Length);
            var i = Ref0(_buk, h & _cap - 1);
            if (i != 0)
            {
                i--;
                ref var e = ref Ref0(_ent);
                do if (Ref0(ref e, i).Hash == h && Cmp(Ref0(ref e, i).Key, ref key0, key.Length)) { return true; }
                while ((i = Ref0(ref e, i).Idx) != NULL);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Remove(string key)
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
                    return true;
                }
            }
        EX: return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetFirst(out string val)
        {
            if (_fnt < _cnt) { val = _ent[_head].Key; return true; }
            val = default; return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetLast(out string val)
        {
            if (_fnt < _cnt) { val = _ent[_ent[_head].Prev].Key; return true; }
            val = default; return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PopFirst(out string val)
        {
            if (_fnt < _cnt) { return Remove(val = _ent[_head].Key); }
            val = default; return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PopLast(out string val)
        {
            if (_fnt < _cnt) { return Remove(val = _ent[_ent[_head].Prev].Key); }
            val = default; return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Clear()
        {
            if (_cnt != 0)
            {
                AC.ZeroFill(_buk);
                Ref0(_ent) = default;

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
        public ref struct RefEnumerator(LinkStrMap list)
        {
            private readonly ref Entry _ent = ref Ref0(list._ent);
            private int _nxt = list._head;
            private int _cnt = list.Count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => --_cnt >= 0;
            public string Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    var i = _nxt; _nxt = Ref0(ref _ent, i).Next;
                    return Ref0(ref _ent, i).Key;
                }
            }
        }
    }
}