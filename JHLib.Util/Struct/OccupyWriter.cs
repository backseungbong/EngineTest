using JHLib.Util.ArrayControl;
using JHLib.Util.Helper;
using JHLib.Util.Pool;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    using static JHLib.Util.Helper.RefCommand;
    public unsafe ref struct OccupyWriter
    {
        private readonly ref byte _p0;
        private int _pos;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal OccupyWriter(ref byte occupy0) => _p0 = ref occupy0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(byte v) => AddB(ref _p0, _pos++) = v;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(T v) where T : unmanaged
        {
            var p = _pos;
            AsT<T>(ref _p0, p) = v;
            _pos = sizeof(T) + p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(T v1, T v2) where T : unmanaged
        {
            var p = _pos;
            ref var t = ref AsT<T>(ref _p0, p);
            t = v1;
            AddT(ref t, 1) = v2;
            _pos = sizeof(T) * 2 + p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(T v1, T v2, T v3) where T : unmanaged
        {
            var p = _pos;
            ref var t = ref AsT<T>(ref _p0, p);
            t = v1;
            AddT(ref t, 1) = v2;
            AddT(ref t, 2) = v3;
            _pos = sizeof(T) * 3 + p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(T v1, T v2, T v3, T v4) where T : unmanaged
        {
            var p = _pos;
            ref var t = ref AsT<T>(ref _p0, p);
            t = v1;
            AddT(ref t, 1) = v2;
            AddT(ref t, 2) = v3;
            AddT(ref t, 3) = v4;
            _pos = sizeof(T) * 4 + p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(string str) => Add(str.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(T[] array) where T : unmanaged
        {
            if (array != null)
            {
                var p = _pos;
                AC.Copy(ref MemoryMarshal.GetArrayDataReference(array), ref AsT<T>(ref _p0, p), array.Length);
                _pos = sizeof(T) * array.Length + p;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(in ReadOnlySpan<T> span) where T : unmanaged
        {
            var p = _pos;
            AC.Copy(ref MemoryMarshal.GetReference(span), ref AsT<T>(ref _p0, p), span.Length);
            _pos = sizeof(T) * span.Length + p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in DataRange range)
        {
            var p = _pos;
            AC.Copy(ref range.Data0, ref AddB(ref _p0, p), range.Count);
            _pos = range.Count + p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(PoolStream pool)
        {
            var p = _pos;
            AC.Copy(ref pool.Stream0, ref AddB(ref _p0, p), pool.Position);
            _pos = pool.Position + p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill<T>(T fill, int fillCount) where T : unmanaged
        {            
            var p = _pos;
            AC.Fill(ref AsT<T>(ref _p0, p), fill, fillCount);
            _pos = sizeof(T) * fillCount + p;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Add(PoolKeyIdx pool)
        {
            var p = _pos;
            pool.CopyTo(ref AddB(ref _p0, p));
            _pos = pool.ByteLength + p;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Add(PoolKeyPos pool)
        {
            var p = _pos;
            pool.CopyTo(ref AddB(ref _p0, p));
            _pos = pool.ByteLength + p;
        }
    }
}