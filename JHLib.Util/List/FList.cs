using JHLib.Util.ArrayControl;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.List
{
    using static JHLib.Util.Helper.RefCommand;

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref struct FList<T>(int cap)
    {
        private T[] _buk = AC.UninitializedArray<T>(cap);
        private int _cap = cap;
        private int _cnt;
        public readonly ref T Ref0 => ref RefT(_buk);
        public readonly ref T Ref(int index) => ref RefT(_buk, index);
        public readonly int Count => _cnt;
        public readonly T this[int i] { get => _buk[i]; set => _buk[i] = value; }
        public readonly T First => _buk[0];
        public readonly T Last => _buk[_cnt - 1];
        public readonly Span<T> ToSpan() => _cnt != 0 ? AsSpan(_buk, _cnt) : default;


        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            var cap = _cap;
            if (cap != 0)
            {
                _buk = RefCopyNew(_buk, cap * 2, cap);
                _cap = cap * 2;
            }
            else
            {
                _buk = new T[2];
                _cap = 2;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T AddRef()
        {
            var c = _cnt;
            if (c == _cap) Resize();
            _cnt = c + 1;
            return ref RefT(_buk, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            var c = _cnt;
            if (c == _cap) Resize();
            _cnt = c + 1;
            _buk[c] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T item)
        {
            var c = _cnt;
            if (c == _cap) Resize();
            _cnt = c + 1;
            _buk[c] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _cnt = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public readonly T[] ToArray()
        {
            var c = _cnt;
            return c != 0 ? RefCopyNew(_buk, c) : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Enumerator GetEnumerator() =>
            _cnt != 0 ? new(ref MemoryMarshal.GetArrayDataReference(_buk), _cnt) : default;

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