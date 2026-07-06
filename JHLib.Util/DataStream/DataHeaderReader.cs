using JHLib.Util.ArrayControl;
using JHLib.Util.ByteControl;
using JHLib.Util.Helper;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.DataStream
{
    public readonly ref struct DataHeaderReader
    {
        private readonly ref DataHeader _header;
        public readonly bool IsValid => Unsafe.IsNullRef(ref _header) == false;
        public readonly int Code => _header.DataCode;
        public readonly int Count => _header.ItemCount;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T Data0<T>() where T : unmanaged => ref _header.Data0<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T Data0<T>(int byteOffset) where T : unmanaged => ref _header.Data0<T>(byteOffset);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T End<T>() where T : unmanaged => ref Unsafe.Add(ref _header.Data0<T>(), _header.ItemCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T End<T>(int byteOffset) where T : unmanaged => ref Unsafe.Add(ref _header.Data0<T>(byteOffset), _header.ItemCount);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataHeaderReader(ref byte data0, int position) : this(ref Unsafe.AddByteOffset(ref data0, position)) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataHeaderReader(ref byte header0) : this(ref Unsafe.As<byte, DataHeader>(ref header0)) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataHeaderReader(ref DataHeader header) => _header = ref header;


        public bool ParseBool(bool defaultValue = false) => ByteParser.ToBool(ref Data0<byte>(), Count, defaultValue);
        public int ParseInt(int defaultValue = 0) => ByteParser.ToInt(ref Data0<byte>(), Count, defaultValue);
        public uint ParseUInt(uint defaultValue = 0) => ByteParser.ToUInt(ref Data0<byte>(), Count, defaultValue);
        public float ParseFloat(float defaultValue = 0) => ByteParser.ToFloat(ref Data0<byte>(), Count, defaultValue);
        public double ParseDouble(double defaultValue = 0) => ByteParser.ToDouble(ref Data0<byte>(), Count, defaultValue);

        public string ToASCII(string defaultValue = null) => ByteParser.ToASCII(ref Data0<byte>(), Count, defaultValue);
        public string ToUTF8(string defaultValue = null) => ByteParser.ToUTF8(ref Data0<byte>(), Count, defaultValue);
        public string ToUTF16(string defaultValue = null) => ByteParser.ToUTF16(ref Data0<byte>(), Count, defaultValue);

        public int BinaryAsInt() => Data0<int>();
        public uint BinaryAsUInt() => Data0<uint>();
        public ulong BinaryAsULong() => Data0<ulong>();
        public float BinaryAsFloat() => Data0<float>();
        public double BinaryAsDouble() => Data0<double>();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEqual(byte[] data) => SequenceEqual(new ReadOnlySpan<byte>(data));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEqual(ReadOnlySpan<byte> data)
        {
            var c = Count;
            if (c == data.Length)
                return c == 0 || AC.IsEqualUnsafe(ref Data0<byte>(), ref MemoryMarshal.GetReference(data), c);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray<T>() where T : unmanaged => AC.CopyNew(ref Data0<T>(), Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefEnumerator<T> GetEnumerator<T>() where T : unmanaged => Etor.New(ref Data0<T>(), Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> ToSpan() => MemoryMarshal.CreateSpan(ref Data0<byte>(), Count);
    }

    public readonly ref struct DataHeaderReader<T> where T : unmanaged
    {
        private readonly ref DataHeader _header;
        public readonly bool IsValid => Unsafe.IsNullRef(ref _header) == false;
        public readonly int Code => _header.DataCode;
        public readonly int Count => _header.ItemCount;
        public readonly ref T Data0 => ref _header.Data0<T>();
        public readonly ref T End => ref Unsafe.Add(ref _header.Data0<T>(), _header.ItemCount);
        public readonly ref T this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.Add(ref _header.Data0<T>(), i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly DataHeaderReader<T> Offset(int offset) => new(ref _header.Offset(offset));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataHeaderReader(ref byte data0, int position) : this(ref Unsafe.AddByteOffset(ref data0, position)) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataHeaderReader(ref byte header0) : this(ref Unsafe.As<byte, DataHeader>(ref header0)) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataHeaderReader(ref DataHeader header) => _header = ref header;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray() => AC.CopyNew(ref Data0, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> ToSpan() => MemoryMarshal.CreateSpan(ref Data0, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefEnumerator<T> GetEnumerator() => Etor.New(ref Data0, Count);
    }
}