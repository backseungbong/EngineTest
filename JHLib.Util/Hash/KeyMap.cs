using JHLib.Util.ArrayControl;
using JHLib.Util.DataStream;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Hash
{
    using static JHLib.Util.Hash.HashUtil;
    public class KeyMap<TKey> where TKey : unmanaged, INumber<TKey>
    {
        private const int START_EXIST_INDEX = -1;
        private const int START_EMPTY_INDEX = -3;
        private struct Entry { public TKey Key; public int Idx; }

        private int[] _buk;
        private Entry[] _ent;
        private int _cap;
        private int _cnt;
        private int _fnt;
        private int _fdx;

        public int Count => _cnt - _fnt;
        public int EntryCount => _cnt;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TKey EntryKey(int i) => Ref0(_ent, i).Key;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyMap() => Initialize(MinPrime);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyMap(int cap) => Initialize(GetPrime(cap));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Initialize(int cap)
        {
            _buk = new int[cap];
            _ent = new Entry[cap];
            _cap = cap;
            _fdx = START_EXIST_INDEX;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            ref var e0 = ref Ref0(_ent);

            var cnt = _cap;
            var cap = GetPrime(cnt * 2);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(TKey[] keys) => Map(ref Ref0(keys), keys.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(DataHeaderReader<TKey> reader) => Map(ref reader.Data0, reader.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(Span<TKey> span) => Map(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Map(ref TKey key0, int count)
        {
            if (count <= _cap)
            {
                if (_cnt != 0)
                    AC.ZeroFill(_buk);
            }
            else
            {
                var cap = GetPrime(count);
                _buk = new int[cap];
                _ent = new Entry[cap];
                _cap = cap;
            }

            if (count > 0)
            {
                ref var b0 = ref Ref0(_buk);
                ref var e0 = ref Ref0(_ent);
                var cap = _cap;

                var i = 0;
                do
                {
                    var key = Ref0(ref key0, i);
                    ref var b = ref Buk0(ref b0, cap, key);
                    ref var e = ref Ref0(ref e0, i);
                    e.Key = key;
                    e.Idx = b - 1; b = i + 1;
                }
                while (++i < count);
                _cnt = count;
                _fnt = 0;
                _fdx = START_EXIST_INDEX;
            }
            else
            {
                _cnt = 0;
                _fnt = 0;
                _fdx = START_EXIST_INDEX;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Set(TKey key)
        {
            var i = _fnt;
            if (i != 0)
            {
                _fnt = i - 1; i = _fdx;
                _fdx = START_EMPTY_INDEX - Ref0(_ent, i).Idx;
            }
            else
            {
                i = _cnt; _cnt = i + 1;
                if (_cap == i) Resize();
            }

            ref var b = ref Buk0(_buk, _cap, key);
            ref var e = ref Ref0(_ent, i);
            e.Key = key;
            e.Idx = b - 1; b = i + 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Add(TKey key)
        {
            ref var b = ref Buk0(_buk, _cap, key);
            ref var e = ref Ref0(_ent);

            var i = b;
            if (i != 0)
            {
                i--;
                do if (Ref0(ref e, i).Key == key) { return false; }
                while ((i = Ref0(ref e, i).Idx) >= 0);
            }

            i = _fnt;
            if (i != 0)
            {
                _fnt = i - 1; i = _fdx;
                _fdx = START_EMPTY_INDEX - Ref0(ref e, i).Idx;
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
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Exist(TKey key)
        {
            var i = Buk0(_buk, _cap, key);
            if (i != 0)
            {
                i--;
                ref var e = ref Ref0(_ent);
                do if (Ref0(ref e, i).Key == key) { return true; }
                while ((i = Ref0(ref e, i).Idx) >= 0);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ExistInline(TKey key)
        {
            var i = Buk0(_buk, _cap, key);
            if (i != 0)
            {
                i--;
                ref var e = ref Ref0(_ent);
                do if (Ref0(ref e, i).Key == key) { return true; }
                while ((i = Ref0(ref e, i).Idx) >= 0);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Remove(TKey key)
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
                        do if ((i = Ref0(ref e, p = i).Idx) < 0) { goto EX; }
                        while (Ref0(ref e, i).Key != key);
                        Ref0(ref e, p).Idx = Ref0(ref e, i).Idx;
                    }
                    else { b = Ref0(ref e, i).Idx + 1; }
                    Ref0(ref e, i).Idx = START_EMPTY_INDEX - _fdx; _fdx = i; _fnt++;
                    return true;
                }
            }
        EX: return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Clear()
        {
            if (_cnt != 0)
            {
                AC.ZeroFill(_buk);

                _cnt = 0;
                _fnt = 0;
                _fdx = START_EXIST_INDEX;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public TKey[] ToArray()
        {
            var c = _cnt - _fnt;
            if (c == 0)
                return [];

            var rst = GC.AllocateUninitializedArray<TKey>(c);
            ref var e = ref Unsafe.Subtract(ref Ref0(_ent), 1);
            ref var r = ref Ref0(rst);

            var i = 0;
            do
            {
                while ((e = ref Ref0(ref e, 1)).Idx < START_EXIST_INDEX) ;
                Ref0(ref r, i) = e.Key;
            }
            while (++i < c);
            return rst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(this);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref struct Enumerator(KeyMap<TKey> list)
        {
            private int _cnt = list.Count;
            private ref Entry _ent = ref Unsafe.Subtract(ref MemoryMarshal.GetArrayDataReference(list._ent), 1u);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => --_cnt >= 0;
            public TKey Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    while ((_ent = ref Ref0(ref _ent, 1)).Idx < START_EXIST_INDEX) ;
                    return _ent.Key;
                }
            }
        }
    }
}