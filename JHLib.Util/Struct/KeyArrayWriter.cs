using JHLib.Util.Hash;
using JHLib.Util.List;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Struct
{
    public readonly ref struct KeyArrayWriter<TKey, TValue> where TKey : unmanaged, INumber<TKey> where TValue : unmanaged
    {
        private readonly KeyArray<TKey, TValue> _own;
        private readonly int _edx;
        public int Count => _own.CountInternal(_edx);
        internal KeyArrayWriter(KeyArray<TKey, TValue> own, int edx) { _own = own; _edx = edx; }

        public ref TValue Occupy0(int count) => ref _own.Occupy0(_edx, count);
        public void Add(TValue data) => _own.AddInternal(_edx, data);
        public void AddIn(in TValue data) => _own.AddInInternal(_edx, data);

        public void AddRange(TValue[] arr) => _own.AddInternal(_edx, arr, 0, arr.Length);
        public void AddRange(SList<TValue> list) => _own.AddInternal(_edx, ref list.Ref0, list.Count);
        public void AddRange(DataRange<TValue> range) => _own.AddInternal(_edx, ref range.Data0, range.Count);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataRange<TValue> ToDataRange() => _own.EntryValue(_edx);
    }
}