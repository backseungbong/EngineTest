using MemoryPack;
using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Performance
{
    /// <summary> 유효성 검증 헤더, 16바이트 고정</summary>
    [SkipLocalsInit]
    [StructLayout(LayoutKind.Explicit, Pack = 8, Size = SIZE)]
    public struct ValidationHeader
    {
        public const int SIZE = 16;

        /// <summary> 데이터 무결성 검증을 위한 64비트 체크섬 (xxHash3 기반) </summary>
        [FieldOffset(0)]
        public ulong Checksum;

        /// <summary> 페이로드(Body)의 데이터 유형 식별자 </summary>
        [FieldOffset(8)]
        public int BodyType;

        /// <summary> 페이로드(Body)의 데이터 크기 (단위:Byte)</summary>
        [FieldOffset(12)]
        public int BodyLength;

        /// <summary> 체크섬 시드 </summary>
        [FieldOffset(8)]
        internal long Seed;

        internal readonly ref byte Header0 =>
            ref Unsafe.As<ValidationHeader, byte>(ref Unsafe.AsRef(in this));
        public readonly ref byte Body0 =>
            ref Unsafe.AddByteOffset(ref Header0, SIZE);
        public readonly ref T BodyAs<T>(uint bodyHeaderLength = 0) =>
            ref Unsafe.As<byte, T>(ref Unsafe.Add(ref Body0, bodyHeaderLength));

        public static ref ValidationHeader From(byte[] buffer) =>
            ref Unsafe.As<byte, ValidationHeader>(ref MemoryMarshal.GetArrayDataReference(buffer));
        public static ref ValidationHeader From(in ReadOnlySpan<byte> span) =>
            ref Unsafe.As<byte, ValidationHeader>(ref MemoryMarshal.GetReference(span));
        public static unsafe ref ValidationHeader From(nint buffer0) =>
            ref Unsafe.AsRef<ValidationHeader>((void*)buffer0);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckChecksum(byte[] bytes)
        {
            ref var header0 = ref From(bytes);
            return header0.CheckChecksum(bytes.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckChecksum(in ReadOnlySpan<byte> span)
        {
            ref var header0 = ref From(span);
            return header0.CheckChecksum(span.Length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool CheckChecksum(int length)
        {
            if ((long)length - SIZE < (uint)BodyLength)
                return false;

            var span = MemoryMarshal.CreateReadOnlySpan(ref Body0, BodyLength);
            var hash = XxHash3.HashToUInt64(span, Seed);
            return hash == Checksum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadBody<T>(byte[] buffer, out T bodyData)
        {
            Unsafe.SkipInit(out bodyData);
            if (buffer == null || buffer.Length < SIZE) return false;
            return From(buffer).TryReadBody(buffer.Length, out bodyData);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadBody<T>(in ReadOnlySpan<byte> span, out T bodyData)
        {
            Unsafe.SkipInit(out bodyData);
            if (span.Length < SIZE) return false;
            return From(span).TryReadBody(span.Length, out bodyData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBody<T>(int length, out T bodyData) => TryReadBody(length, 0, out bodyData);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBody<T>(int length, [ConstantExpected] int bodyHeaderLength, out T bodyData)
        {
            Unsafe.SkipInit(out bodyData);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                if ((long)length - SIZE < (uint)BodyLength)
                    return false;
                return TryDeserializeVerify(out bodyData, bodyHeaderLength);
            }
            else
            {
                if (length < SIZE + bodyHeaderLength + Unsafe.SizeOf<T>())
                    return false;
                return TryLoadVerify(out bodyData, bodyHeaderLength);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Seal(ref byte body0, int bodyType, int bodyLength)
        {
            var seed = (uint)bodyType | (long)bodyLength << 32;
            var span = MemoryMarshal.CreateReadOnlySpan(ref body0, bodyLength);
            Seed = seed;
            Checksum = XxHash3.HashToUInt64(span, seed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryDeserializeVerify<T>(out T data, int bodyHeaderLength)
        {
            Unsafe.SkipInit(out data);

            ref var body0 = ref Body0;
            var bodyLength = BodyLength;
            var span = MemoryMarshal.CreateReadOnlySpan(ref body0, bodyLength);
            var hash = XxHash3.HashToUInt64(span, Seed);
            if (hash != Checksum)
                return false;

            var item = MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.AddByteOffset(ref body0, (uint)bodyHeaderLength), bodyLength - bodyHeaderLength);

            try { MemoryPackSerializer.Deserialize(item, ref data); return true; }
            catch (Exception) { return false; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryLoadVerify<T>(out T data, int bodyHeaderLength)
        {
            ref var body0 = ref Body0;
            data = Unsafe.ReadUnaligned<T>(ref Unsafe.AddByteOffset(ref body0, (uint)bodyHeaderLength));
            var span = MemoryMarshal.CreateReadOnlySpan(ref body0, bodyHeaderLength + Unsafe.SizeOf<T>());
            return XxHash3.HashToUInt64(span, Seed) == Checksum;
        }

        /// <summary> 고속 메모리 복사 (최소 사이즈가 ValidationHeader 사이즈(16바이트) 이상일때만 사용) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FastCopy16(ref byte d, ref byte s, int l)
        {
            if (Avx.IsSupported)
            {
                if (l <= 32)
                {
                    var a1 = Vector128.LoadUnsafe(ref s, (uint)(l - 16));
                    var a2 = Vector128.LoadUnsafe(ref s, 0);
                    a1.StoreUnsafe(ref d, (uint)(l - 16));
                    a2.StoreUnsafe(ref d, 0);
                }
                else
                {
                    if (l > 64)
                    {
                        if (l > 128)
                        {
                            if (l > 2048)
                            {
                                Unsafe.CopyBlock(ref d, ref s, (uint)l);
                            }
                            else if (Avx512F.IsSupported)
                            {
                                ref var se = ref Unsafe.AddByteOffset(ref s, (uint)(l - 128));
                                ref var de = ref Unsafe.AddByteOffset(ref d, (uint)(l - 128));
                                do
                                {
                                    var b1 = Vector512.LoadUnsafe(ref s, 00);
                                    var b2 = Vector512.LoadUnsafe(ref s, 64);
                                    b1.StoreUnsafe(ref d, 00);
                                    b2.StoreUnsafe(ref d, 64);
                                    s = ref Unsafe.Add(ref s, 128);
                                    d = ref Unsafe.Add(ref d, 128);
                                }
                                while (Unsafe.IsAddressLessThan(ref s, ref se));

                                var c1 = Vector512.LoadUnsafe(ref se, 00);
                                var c2 = Vector512.LoadUnsafe(ref se, 64);
                                c1.StoreUnsafe(ref de, 00);
                                c2.StoreUnsafe(ref de, 64);
                            }
                            else
                            {
                                ref var se = ref Unsafe.AddByteOffset(ref s, (uint)(l - 64));
                                ref var de = ref Unsafe.AddByteOffset(ref d, (uint)(l - 64));
                                do
                                {
                                    var d1 = Vector256.LoadUnsafe(ref s, 00);
                                    var d2 = Vector256.LoadUnsafe(ref s, 32);
                                    d1.StoreUnsafe(ref d, 00);
                                    d2.StoreUnsafe(ref d, 32);
                                    s = ref Unsafe.Add(ref s, 64);
                                    d = ref Unsafe.Add(ref d, 64);
                                }
                                while (Unsafe.IsAddressLessThan(ref s, ref se));

                                var e1 = Vector256.LoadUnsafe(ref se, 00);
                                var e2 = Vector256.LoadUnsafe(ref se, 32);
                                e1.StoreUnsafe(ref de, 00);
                                e2.StoreUnsafe(ref de, 32);
                            }
                            return;
                        }

                        var f1 = Vector256.LoadUnsafe(ref s, (uint)(l - 64));
                        var f2 = Vector256.LoadUnsafe(ref s, 32);
                        f1.StoreUnsafe(ref d, (uint)(l - 64));
                        f2.StoreUnsafe(ref d, 32);
                    }

                    var g1 = Vector256.LoadUnsafe(ref s, (uint)(l - 32));
                    var g2 = Vector256.LoadUnsafe(ref s, 00);
                    g1.StoreUnsafe(ref d, (uint)(l - 32));
                    g2.StoreUnsafe(ref d, 00);
                }
            }
            else
            {
                Unsafe.CopyBlock(ref d, ref s, (uint)l);
            }
        }
    }
}