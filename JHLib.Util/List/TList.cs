using JHLib.Util.Helper;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.List
{
    using static JHLib.Util.Helper.RefCommand;
    public class TList<T>
    {
        private T[] _buk;
        private int _cap;
        private int _cnt;
        public ref T Ref0 => ref RefT(_buk);
        public int Count { get => _cnt; set => _cnt = value; }
        public T this[int i] { get => _buk[i]; set => _buk[i] = value; }
        public T First => _buk[0];
        public T Last => _buk[_cnt - 1];
        public Span<T> ToSpan() => AsSpan(_buk, _cnt);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TList() { _buk = new T[2]; _cap = 2; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TList(int cap) { if (cap < 1) cap = 1; _buk = new T[cap]; _cap = cap; }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(int add)
        {
            var cnt = _cnt;
            var buk = RefCopyNew(_buk, MathHelper.RoundUpToPow2(_cap * 2, cnt + add), cnt);

            _buk = buk;
            _cap = buk.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Occupy0(int count)
        {
            var c = _cnt;
            if (c + count > _cap) Resize(count);
            _cnt = c + count;
            return ref RefTU(_buk, c);
        }

        public void Add(T item)
        {
            var c = _cnt;
            if (c == _cap) Resize(1);
            _cnt = c + 1;
            RefTU(_buk, c) = item;
        }
        public void Add(T item1, T item2)
        {
            ref var buk0 = ref Occupy0(2);
            buk0 = item1;
            AddT(ref buk0, 1) = item2;
        }
        public void Add(T item1, T item2, T item3)
        {
            ref var buk0 = ref Occupy0(3);
            buk0 = item1;
            AddT(ref buk0, 1) = item2;
            AddT(ref buk0, 2) = item3;
        }
        public void Add(T item1, T item2, T item3, T item4)
        {
            ref var buk0 = ref Occupy0(4);
            buk0 = item1;
            AddT(ref buk0, 1) = item2;
            AddT(ref buk0, 2) = item3;
            AddT(ref buk0, 3) = item4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(TList<T> list) => AddRange(list._buk, 0, list._cnt);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(T[] array) => AddRange(array, 0, array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(T[] array, int count) => AddRange(array, 0, count);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddRange(T[] array, int index, int count)
        {
            if (count > 0)
                RefCopy(ref RefT(array, index), ref Occupy0(count), count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_cnt != 0)
            {
                RefClear(_buk, _cnt);
                _cnt = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TList<T> ClearGet()
        {
            if (_cnt != 0)
            {
                RefClear(_buk, _cnt);
                _cnt = 0;
            }
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray()
        {
            var c = _cnt;
            return c != 0 ? RefCopyNew(_buk, c) : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArrayEmpty()
        {
            var c = _cnt;
            return c != 0 ? RefCopyNew(_buk, c) : [];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArrayClear()
        {
            var c = _cnt; _cnt = 0;
            return c != 0 ? RefCopyNewClear(_buk, c) : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(ref MemoryMarshal.GetArrayDataReference(_buk), _cnt);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref struct Enumerator(ref T buk, int cnt)
        {
            private ref T p = ref buk;
            private readonly ref T e = ref Unsafe.Add(ref buk, (uint)cnt);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly Enumerator GetEnumerator() => this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => Unsafe.IsAddressLessThan(ref p, ref e);
            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref Unsafe.Subtract(ref p = ref Unsafe.Add(ref p, 1), 1);
            }
        }
    }
}