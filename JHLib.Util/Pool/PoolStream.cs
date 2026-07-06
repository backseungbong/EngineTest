using JHLib.Util.ArrayControl;
using JHLib.Util.ByteControl;
using JHLib.Util.DataStream;
using JHLib.Util.Hash;
using JHLib.Util.Helper;
using JHLib.Util.Struct;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Pool
{
    using static JHLib.Util.Helper.RefCommand;
    public class PoolStream : IResizableStream, IDisposable
    {
        private const int MIN_SPACE = 4096;

        private byte[] _stream;
        private int _cap;
        private int _pos;

        public int Capacity => _cap;
        public int Position { get => _pos; set => _pos = value; }

        internal byte[] Stream => _stream;
        public ref byte Stream0 => ref RefT(_stream);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte Ref(int pos) => ref RefT(_stream, (uint)pos);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Ref<T>(int pos) => ref Unsafe.As<byte, T>(ref RefT(_stream, (uint)pos));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataHeaderReader<T> AsReader<T>(int position) where T : unmanaged => new(ref Ref<DataHeader>(position));

        public PoolStream() { }
        public PoolStream(int byteSize) { ResizeInternal(byteSize, 0); }
        public PoolStream(string path, int freeSpace = 0) { ReadFile(path, freeSpace); }
        public void Dispose() { var s = _stream; _stream = null; if (s != null) ArrayPool<byte>.Shared.Return(s); }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeInternal(int resize, int copy)
        {
            var spaceOld = _stream;
            var spaceNew = ArrayPool<byte>.Shared.Rent(resize > MIN_SPACE ? resize : MIN_SPACE);

            if (copy != 0) AC.Copy(spaceOld, spaceNew, copy);

            _stream = spaceNew;
            _cap = spaceNew.Length;

            if (spaceOld != null)
                ArrayPool<byte>.Shared.Return(spaceOld);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize(int resize, int copy) { if (resize > _cap) ResizeInternal(resize, copy); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Ensure(int pos, int ensure) { Resize(pos + ensure, pos); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref byte Ensure0(int pos, int ensure) { Resize(pos + ensure, pos); return ref Ref(pos); }


        [MethodImpl(MethodImplOptions.NoInlining)]
        internal DataHeaderWriter AddHeader(PoolStream spos, int dataCode, int itemCount)
        {
            var pos = _pos; _pos = pos + DataHeader.SIZE;
            AsT<int>(ref spos.Occupy0(4)) = pos;
            AsT<DataHeader>(ref Ensure0(pos, DataHeader.SIZE)) = new(dataCode, itemCount);
            return new(this, pos);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal DataHeaderWriter AddHeader(PoolKeyPos kmap, int key, int dataCode, int itemCount)
        {
            var pos = _pos; _pos = pos + DataHeader.SIZE;
            kmap.Set(key, pos);
            AsT<DataHeader>(ref Ensure0(pos, DataHeader.SIZE)) = new(dataCode, itemCount);
            return new(this, pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataHeaderWriter AddHeader(int dataCode = 0, int itemCount = 0)
        {
            var pos = _pos; _pos = pos + DataHeader.SIZE;
            AsT<DataHeader>(ref Ensure0(pos, DataHeader.SIZE)) = new(dataCode, itemCount);
            return new(this, pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataHeaderWriter NewHeader()
        {
            var pos = _pos; _pos = pos + DataHeader.SIZE;
            AsT<DataHeader>(ref Ensure0(pos, DataHeader.SIZE)) = default;
            return new(this, pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureFreeSpace(int freespace) => Ensure(_pos, freespace);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T EnsureSpace0<T>(int ensureCount, int byteOffset = 0) =>
            ref AsT<T>(ref Ensure0(byteOffset, ensureCount * Unsafe.SizeOf<T>()));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OccupyWriter OccupyWriter(int byteSize) => new(ref Occupy0(byteSize));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OccupyWriter OccupyWriter<T>(int count) => new(ref Occupy0(count * Unsafe.SizeOf<T>()));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Occupy(int byteSize) { var pos = _pos; _pos = pos + byteSize; Ensure(pos, byteSize); return pos; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte Occupy0(int byteSize) { var pos = _pos; _pos = pos + byteSize; return ref Ensure0(pos, byteSize); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Occupy0<T>(int count) where T : unmanaged => ref AsT<T>(ref Occupy0(count * Unsafe.SizeOf<T>()));

        public ref T AddRef<T>() where T : unmanaged => ref AsT<T>(ref Occupy0(Unsafe.SizeOf<T>()));
        public void Add<T>(T item) where T : unmanaged => AddRef<T>() = item;
        public void Add(string item) { if (item != null) Add(ref RefT(item), item.Length); }
        public void Add(DataRange range) => Add(ref range.Data0, range.Count);
        public void Add(PoolStream stream) => Add(ref stream.Stream0, stream.Position);
        public void Add<T>(DataRange<T> range) where T : unmanaged => Add(ref range.Data0, range.Count);
        public void Add<T>(DataHeaderReader<T> reader) where T : unmanaged => Add(ref reader.Data0, reader.Count);
        public void Add<T>(T[] array) where T : unmanaged => Add(array, array.Length);
        public void Add<T>(T[] array, int count) where T : unmanaged => AC.Copy(array, ref Occupy0<T>(count), count);
        public void Add<T>(Span<T> span) where T : unmanaged => Add(ref MemoryMarshal.GetReference(span), span.Length);
        public void Add<T>(ref T pSource, int count) where T : unmanaged => AC.Copy(ref pSource, ref Occupy0<T>(count), count);

        public int F2Dedupe(int position)
        {
            var dedupeCount = AC.F2Dedupe(ref Ref<Float2D>(position), (_pos - position) >> 3); // >> 3 is equel div 8(Float2D.SIZE)
            _pos = position + (dedupeCount << 3);
            return dedupeCount;
        }

        public int F2DedupeV(int position)
        {
            var dedupeCount = AC.F2DedupeV(ref Ref<Float2D>(position), (_pos - position) >> 3); // >> 3 is equel div 8(Float2D.SIZE)
            _pos = position + (dedupeCount << 3);
            return dedupeCount;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AlignPosition4ByteAndFillZero()
        {
            var pos = _pos;
            var align = pos + 3 & ~3;
            if (align != pos)
            {
                ref var s0 = ref Stream0; _pos = align;
                if (align > pos + 2)
                {
                    AsTU<ushort>(ref s0, pos) = 0;
                    AsTU<ushort>(ref s0, align - 2) = 0; return;
                }
                AddBU(ref s0, pos) = 0;
                AddBU(ref s0, align - 1) = 0;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AlignPosition8ByteAndFillZero()
        {
            var pos = _pos;
            var align = pos + 7 & ~7;
            if (align != pos)
            {
                ref var s0 = ref Stream0; _pos = align;
                if (align > pos + 2)
                {
                    if (align > pos + 4)
                    {
                        AsTU<uint>(ref s0, pos) = 0;
                        AsTU<uint>(ref s0, align - 4) = 0; return;
                    }
                    AsTU<ushort>(ref s0, pos) = 0;
                    AsTU<ushort>(ref s0, align - 2) = 0; return;
                }
                AddBU(ref s0, pos) = 0;
                AddBU(ref s0, align - 1) = 0;
            }
        }

        public void Clear() => _pos = 0;
        public PoolStream ClearGet() { _pos = 0; return this; }


        /// <summary>
        /// 스트림의 앞부분 4바이트 이후의 스트림 해시 결과를 스트림의 제일 앞에 4바이트 형식으로 쓴다<para/>
        /// 스트림의 앞 4바이트가 해시결과로 덮혀 쓰여지므로, 반드시 앞 4바이트는 빈값을 넣거나 사용되지 않는 공간이어야한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteXXHash32()
        {
            var pos = _pos;
            if (pos >= 4)
            {
                ref var s0 = ref Stream0;
                AsT<int>(ref s0) = XXHash.H32(ref AddB(ref s0, 4), pos - 4);
            }
        }

        /// <summary>
        /// 스트림의 앞부분 8바이트 이후의 스트림 해시 결과를 스트림의 제일 앞에 8바이트 형식으로 쓴다<para/>
        /// 스트림의 앞 8바이트가 해시결과로 덮혀 쓰여지므로, 반드시 앞 8바이트는 빈값을 넣거나 사용되지 않는 공간이어야한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteXXHash64()
        {
            var pos = _pos;
            if (pos >= 8)
            {
                ref var s0 = ref Stream0;
                AsT<long>(ref s0) = XXHash.H64(ref AddB(ref s0, 8), pos - 8);
            }
        }

        /// <summary>
        /// 앞 4바이트 이후의 스트림 해시 결과가, 앞 4바이트의 해시값과 동일한지 체크한다<para/>
        /// 스트림은 반드시 4바이트 이상이어야 하고, 해시값은 XXHash의 32bit 해시값이어야 한다        
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckXXHash32()
        {
            var pos = _pos;
            if (pos >= 4)
            {
                ref var s0 = ref Stream0;
                return AsT<int>(ref s0) == XXHash.H32(ref AddB(ref s0, 4), pos - 4);
            }
            return false;
        }

        /// <summary>
        /// 앞 8바이트 이후의 스트림 해시 결과가, 앞 8바이트의 해시값과 동일한지 체크한다<para/>
        /// 스트림은 반드시 8바이트 이상이어야 하고, 해시값은 XXHash의 64bit 해시값이어야 한다        
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckXXHash64()
        {
            var pos = _pos;
            if (pos >= 8)
            {
                ref var s0 = ref Stream0;
                return AsT<long>(ref s0) == XXHash.H64(ref AddB(ref s0, 8), pos - 8);
            }
            return false;
        }

        /// <summary>
        /// 파일을 스트림으로 모두 읽어온 후 파일해시를 체크한다 <para/>
        /// 앞 4바이트 이후의 스트림 해시 결과가, 앞 4바이트의 해시값과 동일한지 체크한다<para/>
        /// 스트림은 반드시 4바이트 이상이어야 하고, 해시값은 XXHash의 32bit 해시값이어야 한다       
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckXXHash32(string path) => ReadFile(path) && CheckXXHash32();

        /// <summary>
        /// 파일을 스트림으로 모두 읽어온 후 파일해시를 체크한다 <para/>
        /// 앞 8바이트 이후의 스트림 해시 결과가, 앞 8바이트의 해시값과 동일한지 체크한다<para/>
        /// 스트림은 반드시 8바이트 이상이어야 하고, 해시값은 XXHash의 64bit 해시값이어야 한다       
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckXXHash64(string path) => ReadFile(path) && CheckXXHash64();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataRange ToDataRange(int position, int length) => new(ref Ref(position), length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ToArray()
        {
            var length = _pos;
            var array = GC.AllocateUninitializedArray<byte>(length);
            AC.Copy(ref Stream0, ref RefB(array), length);
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray<T>() where T : unmanaged
        {
            var count = _pos / Unsafe.SizeOf<T>();
            var array = AC.UninitializedArray<T>(count);
            AC.Copy(ref Stream0, ref RefB(array), count * Unsafe.SizeOf<T>());
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => new(AsReadOnlySpan(ref AsT<char>(ref Stream0), _pos / sizeof(char)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> ToSpan() => AsSpan(ref Stream0, _pos);


        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool ReadFile(string path, int freeSpace = 0)
        {
            const int RETRY_NUM = 5;
            const int RETRY_INTERVAL = 200;

            var i = RETRY_NUM;
        RE: _pos = 0;
            if (FileHelper.ReadAllBytes(path, freeSpace, this)) return true;
            if (--i != 0 && File.Exists(path)) { Thread.Sleep(RETRY_INTERVAL); goto RE; }

            _pos = 0;
            Resize(freeSpace, 0);
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool WriteFile(string path)
        {
            const int RETRY_NUM = 10;
            const int RETRY_INTERVAL = 200;

            var i = RETRY_NUM;
        RE: if (FileHelper.WriteAllBytes(path, ToSpan())) return true;
            if (--i != 0) { Thread.Sleep(RETRY_INTERVAL); goto RE; }
            return false;
        }
    }
}