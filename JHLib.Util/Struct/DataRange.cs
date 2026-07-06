using JHLib.Util.ArrayControl;
using JHLib.Util.ByteControl;
using JHLib.Util.DataStream;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    using static JHLib.Util.Helper.RefCommand;
    public readonly ref struct DataRange
    {
        public readonly ref byte Data0;
        public readonly int Count;
        public readonly ref byte End => ref AddB(ref Data0, Count);
        public readonly byte this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AddB(ref Data0, i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataRange(ref byte header0) : this(ref Unsafe.As<byte, DataHeader>(ref header0)) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataRange(ref DataHeader header) : this(ref header.Data0<byte>(), header.ItemCount) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataRange(ref byte data0, int length)
        {
            Data0 = ref data0;
            Count = length;
        }

        public bool ParseBool(bool defaultValue = false) => ByteParser.ToBool(ref Data0, Count, defaultValue);
        public int ParseInt(int defaultValue = 0) => ByteParser.ToInt(ref Data0, Count, defaultValue);
        public uint ParseUInt(uint defaultValue = 0) => ByteParser.ToUInt(ref Data0, Count, defaultValue);
        public float ParseFloat(float defaultValue = 0) => ByteParser.ToFloat(ref Data0, Count, defaultValue);
        public double ParseDouble(double defaultValue = 0) => ByteParser.ToDouble(ref Data0, Count, defaultValue);

        public string ToASCII(string defaultValue = null) => ByteParser.ToASCII(ref Data0, Count, defaultValue);
        public string ToUTF8(string defaultValue = null) => ByteParser.ToUTF8(ref Data0, Count, defaultValue);
        public string ToUTF16(string defaultValue = null) => ByteParser.ToUTF16(ref Data0, Count, defaultValue);

        public int BinaryAsInt() => AsT<int>(ref Data0);
        public uint BinaryAsUInt() => AsT<uint>(ref Data0);
        public ulong BinaryAsULong() => AsT<ulong>(ref Data0);
        public float BinaryAsFloat() => AsT<float>(ref Data0);
        public double BinaryAsDouble() => AsT<double>(ref Data0);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEqual(byte[] data) => SequenceEqual(new ReadOnlySpan<byte>(data));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEqual(ReadOnlySpan<byte> data)
        {
            var c = Count;
            if (c == data.Length)
                return c == 0 || AC.IsEqualUnsafe(ref Data0, ref MemoryMarshal.GetReference(data), c);
            return false;
        }
    }

    public readonly ref struct DataRange<T> where T : unmanaged
    {
        public readonly ref T Data0;
        public readonly int Count;
        public readonly ref T Last => ref AddT(ref Data0, Count - 1);
        public readonly ref T End => ref AddT(ref Data0, Count);
        public readonly ref T this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref AddT(ref Data0, i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataRange(ref byte header0) : this(ref Unsafe.As<byte, DataHeader>(ref header0)) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataRange(ref DataHeader header) : this(ref header.Data0<T>(), header.ItemCount) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataRange(ref T data0, int count)
        {
            Data0 = ref data0;
            Count = count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray() => AC.CopyNew(ref Data0, Count);
    }
}