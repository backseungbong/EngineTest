using JHLib.Util.ArrayControl;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace JHLib.Util.List
{
    public unsafe class NativeSpace : IDisposable
    {
        private nint _buk;
        private int _cap;
        private int _len;
        public nint Buk0 => _buk;
        public int Capacity => _cap;
        public int Length => _len;
        public int Count<T>() => _len / Unsafe.SizeOf<T>();

        public void Dispose()
        {
            if (_buk != 0)
            {
                NativeMemory.AlignedFree((void*)_buk);
                _buk = 0;
                _cap = 0;
                _len = 0;

                GC.SuppressFinalize(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSpace(int initSize = 256) => Resize(initSize);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(int siz)
        {
            var cap = 256;
            if (siz > 256)
                cap = (int)BitOperations.RoundUpToPowerOf2((uint)siz);

            var buk = NativeMemory.AlignedAlloc((uint)cap, 32);
            var len = _len;
            if (len != 0)
            {
                Unsafe.CopyBlock(buk, (void*)_buk, (uint)(len + 31 & ~31));
                NativeMemory.AlignedFree((void*)_buk);
            }
            _buk = (nint)buk;
            _cap = cap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLength(int len)
        {
            if (len > _cap) Resize(len);
            _len = len;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add<T>(T data) where T : unmanaged
        {
            var idx = _len;
            var len = idx + Unsafe.SizeOf<T>();
            if (len > _cap) Resize(len);
            *(T*)(_buk + (uint)idx) = data;
            _len = len;
            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T* AddRef<T>() where T : unmanaged
        {
            var idx = _len;
            var len = idx + Unsafe.SizeOf<T>();
            if (len > _cap) Resize(len);
            _len = len;
            return (T*)(_buk + (uint)idx);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int AddUTF8NullTerminated(string str)
        {
            var idx = _len;
            var add = 0;
            var bts = default(byte[]);

            if (str != null)
            {
                bts = Encoding.UTF8.GetBytes(str);
                add = bts.Length;
            }

            var len = idx + add + 1;
            if (len > _cap) Resize(len);

            var buk = (byte*)(_buk + (uint)idx);
            if (add != 0)
            {
                CopyInternal(bts, buk);
                buk += (uint)add;
            }
            *buk = 0;

            _len = len;
            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AddUTF8NullTerminated(Span<byte> bytes) =>
            AddUTF8NullTerminated(ref MemoryMarshal.GetReference(bytes), bytes.Length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int AddUTF8NullTerminated(ref byte utf8, int length)
        {
            var idx = _len;
            var len = idx + length + 1;
            if (len > _cap) Resize(len);

            var buk = (byte*)(_buk + (uint)idx);
            if (length > 0)
            {
                AC.Copy(ref utf8, ref *buk, length);
                buk += (uint)length;
            }
            else
            {
                len = idx + 1;
            }
            *buk = 0;

            _len = len;
            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray<T>() where T : unmanaged
        {
            var count = Count<T>();
            var result = new T[count];
            if (count > 0)
            {
                ref var dst = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(result));
                AC.Copy(ref *(byte*)_buk, ref dst, _len);
            }
            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _len = 0;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyInternal(byte[] src, byte* d0)
        {
            AC.Copy(ref MemoryMarshal.GetArrayDataReference(src), ref *d0, src.Length);
        }

        /// <summary>참조 주소를 T타입으로 변환하여 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsT<T>(ref byte ptr) =>
            ref Unsafe.As<byte, T>(ref ptr);

        /// <summary>참조 주소에 지정된 바이트오프셋을 더한 뒤 T타입으로 변환하여 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsT<T>(ref byte ptr, int byteOffset) =>
            ref Unsafe.As<byte, T>(ref Unsafe.AddByteOffset(ref ptr, (uint)byteOffset));
    }
}