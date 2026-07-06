using JHLib.Util.ArrayControl;
using JHLib.Util.DataStream;
using JHLib.Util.Helper;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.List
{
    using static JHLib.Util.Helper.RefCommand;
    public class SList<T> where T : unmanaged
    {
        private T[] _buk;
        private int _cap;
        private int _cnt;
        public ref T Ref0 => ref RefT(_buk);
        public ref T Ref(int index) => ref RefT(_buk, index);
        public int Count { get => _cnt; set => _cnt = value; }
        public T this[int i] { get => _buk[i]; set => _buk[i] = value; }
        public T First => _buk[0];
        public T Last => _buk[_cnt - 1];
        public Span<T> ToSpan() => AsSpan(_buk, _cnt);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SList() { _buk = new T[2]; _cap = 2; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SList(int cap) { if (cap < 1) cap = 1; _buk = AC.UninitializedArray<T>(cap); _cap = cap; }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(int add)
        {
            var cnt = _cnt;
            var buk = AC.CopyNew(_buk, MathHelper.RoundUpToPow2(_cap * 2, cnt + add), cnt);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T AddRef()
        {
            var c = _cnt;
            if (c == _cap) Resize(1);
            _cnt = c + 1;
            return ref RefTU(_buk, c);
        }

        public void Add(T item)
        {
            var c = _cnt;
            if (c == _cap) Resize(1);
            _cnt = c + 1;
            RefTU(_buk, c) = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(SList<T> list) => AddRange(list._buk, 0, list._cnt);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(T[] array) => AddRange(array, 0, array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(T[] array, int count) => AddRange(array, 0, count);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddRange(T[] array, int index, int count)
        {
            if (count > 0)
                AC.Copy(ref RefT(array, index), ref Occupy0(count), count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(DataRange<T> range) => AddRange(ref range.Data0, range.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(DataHeaderReader<T> reader) => AddRange(ref reader.Data0, reader.Count);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddRange(ref T source0, int count)
        {
            if (count > 0)
                AC.Copy(ref source0, ref Occupy0(count), count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _cnt = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SList<T> ClearGet() { _cnt = 0; return this; }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public T[] ToArray()
        {
            var c = _cnt;
            return c != 0 ? AC.CopyNew(_buk, c) : null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public T[] ToArrayEmpty()
        {
            var c = _cnt;
            return c != 0 ? AC.CopyNew(_buk, c) : [];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public T[] ToArrayClear()
        {
            var c = _cnt; _cnt = 0;
            return c != 0 ? AC.CopyNew(_buk, c, c) : null;
        }

        public static implicit operator Span<T>(SList<T> list) => list.ToSpan();

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