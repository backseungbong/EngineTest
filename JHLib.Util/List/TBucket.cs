using JHLib.Util.Helper;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.List
{
    public class TBucket<T> : IList<T>
    {
        private ArrayBucket<T> _bucket;

        public bool IsReadOnly => false;
        public int Capacity => _bucket.Capacity;
        public int Count => _bucket.Count;
        public T this[int i] { get => _bucket[i]; set => _bucket[i] = value; }

        public RefEnumerator<T> GetRefEnumerator() => _bucket.GetRefEnumerator();
        public IReadOnlyList<T> AsReadOnly() => _bucket;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan() => _bucket.AsSpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int count) => _bucket.AsSpan(count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int index, int count) => _bucket.AsSpan(index, count);

        public TBucket() => _bucket = [];
        public TBucket(int cap) => _bucket = new(cap);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TBucket<T> Renew(int size, bool clearBucket = false)
        {
            _bucket.Renew(size, clearBucket, ref _bucket);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Ensure0(int size, bool clearBucket = false)
        {
            _bucket.Renew(size, clearBucket, ref _bucket);
            return ref _bucket.Bucket0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item) => _bucket.Add(item, ref _bucket);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T item) => _bucket.Remove(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index) => _bucket.RemoveAt(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveTarget(ref T target) => _bucket.RemoveTarget(ref target);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => Clear(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(bool clearBucket = false) => _bucket.Clear(clearBucket);

        public bool Contains(T item) => throw new NotImplementedException();
        public void CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();
        public int IndexOf(T item) => throw new NotImplementedException();
        public void Insert(int index, T item) => throw new NotImplementedException();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }

    public class ArrayBucket<T> : IReadOnlyList<T>
    {
        public static readonly ArrayBucket<T> Empty = [];

        private readonly T[] _buk;
        private readonly int _cap;
        private volatile int _cnt;
        public ref T Bucket0 => ref MemoryMarshal.GetArrayDataReference(_buk);
        public int Capacity => _cap;
        public int Count => _cnt;
        public T this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buk[i];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _buk[i] = value;
        }

        public Span<T> AsSpan() =>
            MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_buk), _cnt);
        public Span<T> AsSpan(int count) =>
            MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_buk), count);
        public Span<T> AsSpan(int index, int count) =>
            MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buk), index), count);

        public RefEnumerator<T> GetRefEnumerator() => Etor.New(_buk, _cnt);
        public ArrayBucket() { }
        public ArrayBucket(int cap)
        {
            _buk = new T[cap = MathHelper.RoundUpToPow2(2, cap)];
            _cap = cap;
        }
        private ArrayBucket(T[] buk, int cnt)
        {
            _buk = buk;
            _cap = buk.Length;
            _cnt = cnt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Renew(int size, bool clearBucket, ref ArrayBucket<T> bucket)
        {
            if (size > _cap) bucket = new ArrayBucket<T>(size);
            else
            {
                var cnt = _cnt; _cnt = 0;
                if (cnt != 0 && clearBucket)
                    new Span<T>(_buk, 0, cnt).Clear();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(T item, ref ArrayBucket<T> refBucket)
        {
            var cap = _cap;
            var dst = new T[MathHelper.RoundUpToPow2(2, cap + 1)];
            if (cap != 0) _buk.AsSpan(0, cap).CopyTo(dst.AsSpan());
            dst[cap] = item;
            refBucket = new(dst, cap + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item, ref ArrayBucket<T> refBucket)
        {
            var cnt = _cnt;
            if (cnt == _cap) Resize(item, ref refBucket);
            else { _buk[cnt] = item; _cnt = cnt + 1; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T item)
        {
            var cnt = _cnt;
            if (cnt != 0)
            {
                ref var p = ref MemoryMarshal.GetArrayDataReference(_buk);
                ref var e = ref Unsafe.Add(ref p, cnt);
                do if (EqualityComparer<T>.Default.Equals(item, p)) return RemoveTarget(ref p);
                while (Unsafe.IsAddressLessThan(ref p = ref Unsafe.Add(ref p, 1), ref e));
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveAt(int i) => (uint)i < (uint)_cnt && RemoveTarget(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buk), i));

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool RemoveTarget(ref T target)
        {
            var cnt = _cnt;
            ref var p = ref MemoryMarshal.GetArrayDataReference(_buk);
            ref var e = ref Unsafe.Add(ref p, cnt);

            if (Unsafe.IsAddressLessThan(ref target, ref e) &&
                Unsafe.IsAddressLessThan(ref target, ref p) == false)
            {
                ref var t = ref Unsafe.Add(ref target, 1);
                if (Unsafe.IsAddressLessThan(ref t, ref e))
                {
                    do Unsafe.Subtract(ref t, 1) = t;
                    while (Unsafe.IsAddressLessThan(ref t = ref Unsafe.Add(ref t, 1), ref e));
                }
                _cnt = cnt - 1;
                _buk[cnt - 1] = default;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(bool clearBucket = false)
        {
            var cnt = _cnt;
            if (cnt != 0)
            {
                _cnt = 0;
                if (clearBucket)
                    new Span<T>(_buk, 0, cnt).Clear();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T> GetEnumerator() => new Enumerator(_buk, _cnt);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public struct Enumerator(T[] buk, int cnt) : IEnumerator<T>
        {
            private readonly T[] _buk = buk;
            private readonly int _cnt = cnt;
            private int _idx = -1;

            readonly object IEnumerator.Current => Current;
            public readonly T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _buk[_idx];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_idx < _cnt;
            public void Reset() => _idx = -1;
            public void Dispose() { }
        }
    }
}