using JHLib.Util.DataStream;
using JHLib.Util.Pool;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Compression
{
    public unsafe static class LZ4
    {
        private const int LZ4_COMPRESS = 2000000000;
        private const int LZ4_DECOMPRESS = 2000000001;

        private const int KB = 1 << 10;

        private const int LIMIT_64K = (64 * KB) + (MFLIMIT - 1);
        private const int SKIP_TRIGGER = 6;

        private const int MAX_INPUT_SIZE = 0x7E000000;
        private const int MINMATCH = 4;
        private const int WILDCOPYLENGTH = 8;
        private const int LITS = 6;
        private const int MFLIMIT = 12;

        private const int MATCH_SAFEGUARD_DISTANCE = (2 * WILDCOPYLENGTH) - MINMATCH;
        private const int FASTLOOP_SAFE_DISTANCE = 64;
        private const int MIN_LENGTH = MFLIMIT + 1;

        private const int ML_BITS = 4;
        private const int ML_MASK = (1 << ML_BITS) - 1;
        private const int RUN_BITS = 8 - ML_BITS;
        private const int RUN_MASK = (1 << RUN_BITS) - 1;

        private const int DISTANCE_MAX = 65535;
        private const int MEMORY_USAGE = 14;
        private const int HASHLOG = MEMORY_USAGE - 2;
        private const int HASHTABLESIZE = 1 << MEMORY_USAGE;
        private const int HASH_SIZE_U32 = 1 << HASHLOG;
        private const int HASH_SIZE = HASH_SIZE_U32 * 4 + 31 & ~31;

        private const int ARCH_SIZE = 8;
        private const int STEP_SIZE = 8;
        private const int HASH_UNIT = 8;

        private static readonly byte[] HashBucket = new byte[HASH_SIZE];
        private static ReadOnlySpan<uint> Inc32 => [0, 1, 2, 1, 0, 4, 4, 4];
        private static ReadOnlySpan<int> Dec64 => [0, 0, 0, -1, -4, 1, 2, 3];
        private static ReadOnlySpan<byte> DeBruijnBytes => new byte[64]
        {
            0, 0, 0, 0, 0, 1, 1, 2,
            0, 3, 1, 3, 1, 4, 2, 7,
            0, 2, 3, 6, 1, 5, 3, 5,
            1, 3, 4, 4, 2, 5, 6, 7,
            7, 0, 1, 2, 3, 3, 4, 6,
            2, 6, 5, 5, 3, 4, 5, 6,
            7, 1, 2, 4, 6, 4, 4, 5,
            7, 2, 6, 5, 7, 6, 7, 7
        };

        public static void Compress(PoolStream decompData, PoolStream compData)
        {
            //compData.Clear();

            //if (decompData.Position <= 0)
            //    return;

            //ref var compHeader0 = ref compData.EnsureSpace0(LZ4HEADER.SIZE + MaxCompSize(decompData.Position));
            //var compHeader = (LZ4HEADER*)compHeader0;

            //var compData0 = compHeader0 + LZ4HEADER.SIZE;
            //var compLength = CompInternal(ref decompData.Ref0, decompData.Position, compData0);

            //compHeader->Length = compLength;
            //compData.Position = LZ4HEADER.SIZE + compLength;
        }

        public static void Compress(PoolStream dataStream, DataHeaderWriter compSlice)
        {
            //var dataLength = dataStream.Position;
            //if (dataLength > 0)
            //{
            //    var header = compSlice.AddDataHeader(DATACODE_LZ4, dataLength);
            //    ref var dst0 = ref header.EnsureSpace0(MaxCompSize(dataLength));
            //    var compLength = CompInternal(ref datastream.Space0, dataLength, ref dst0);
            //    compSlice.Header = new DataHeader();
            //}
        }

        public static void Decompress(DataHeaderReader compSlice, PoolStream dataStream)
        {
            //var dataLength = compSlice.ItemCount;
            //if (dataLength > 0)
            //{
            //    ref var dst0 = ref dataStream.EnsureSpace0(dataLength);

            //    DecompInternal(ref compSlice.Data0, compSlice.DataRange, decompData0, decompLength);



            //    ref var dst0 = ref header.EnsureSpace0(MaxCompSize(dataLength));
            //    var compLength = CompInternal(ref datastream.Space0, dataLength, ref dst0);
            //    header.DataLength = compLength;
            //}
        }



        private static int DecompInternal(ref byte src0, int srcLen, ref byte dst0, int dstLen)
        {
            fixed (byte* src = &src0, dst = &dst0)
                return DecompInternal(src, srcLen, dst, dstLen);
        }

        private static int DecompInternal(byte* src, int srcLen, byte* dst, int dstLen)
        {
            byte* mt, icpy, ocpy;
            int off, l;

            var ip = src;
            var op = dst;
            var ie = ip + srcLen;
            var oe = op + dstLen;
            var ise = ie - (14 + 02);
            var ose = oe - (14 + 18);

            while (true)
            {
                var tok = *ip; ip++;
                var len = tok >> ML_BITS;

                if (len != RUN_MASK && ip < ise && op <= ose)
                {
                    *(B16*)op = *(B16*)ip;
                    op += len;
                    ip += len;

                    len = tok & ML_MASK;
                    off = *(ushort*)ip; ip += 2;
                    mt = op - off;

                    if (len != ML_MASK && off >= 8 && mt >= dst)
                    {
                        *(ulong*)(op + 0) = *(ulong*)(mt + 0);
                        *(ulong*)(op + 8) = *(ulong*)(mt + 8);
                        *(ushort*)(op + 16) = *(ushort*)(mt + 16);
                        op += len + MINMATCH;
                        continue;
                    }
                }
                else
                {
                    if (len == RUN_MASK)
                    {
                        if (ip < ie - RUN_MASK)
                        {
                            do len += l = *ip;
                            while (++ip < ie - RUN_MASK && l == 255);
                        }
                        else
                        {
                            return -1;
                        }
                    }

                    icpy = ip + len;
                    ocpy = op + len;

                    if (ocpy > oe - MFLIMIT || icpy > ie - (LITS + 2 + 1))
                    {
                        if (ocpy <= oe && icpy == ie)
                        {
                            NativeMemory.Copy(ip, op, (uint)len);
                            return (int)(ocpy - dst);
                        }
                        else
                        {
                            return -1;
                        }
                    }

                    do { *(long*)op = *(long*)ip; op += 8; ip += 8; }
                    while (op < ocpy);

                    ip = icpy;
                    op = ocpy;

                    len = tok & ML_MASK;
                    off = *(ushort*)ip; ip += 2;
                    mt = op - off;
                }

                if (len == ML_MASK)
                {
                    do
                    {
                        if (ip > ie - LITS) return -1;
                        len += l = *ip; ip++;
                    }
                    while (l == 255);
                }

                if (off < 8)
                {
                    op[0] = mt[0];
                    op[1] = mt[1];
                    op[2] = mt[2];
                    op[3] = mt[3];
                    mt += Inc32[off];
                    *(uint*)(op + 4) = *(uint*)mt;
                    mt -= Dec64[off];
                }
                else
                {
                    *(ulong*)op = *(ulong*)mt;
                    mt += 8;
                }

                ocpy = op + len + MINMATCH;
                op += 8;

                if (ocpy <= oe - MATCH_SAFEGUARD_DISTANCE)
                {
                    do { *(long*)op = *(long*)mt; op += 8; mt += 8; }
                    while (op < ocpy);
                    op = ocpy;
                }
                else if (ocpy <= oe - LITS)
                {
                    while (op < oe - 7) { *(long*)op = *(long*)mt; op += 8; mt += 8; }
                    while (op < ocpy) { *op = *mt; op++; mt++; }
                    op = ocpy;
                }
                else
                {
                    return -1;
                }
            }
        }

        public static int MaxCompSize(int size) => (size + (size / 255)) + 16;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CompInternal(ref byte src0, int len, ref byte dst0)
        {
            fixed (byte* src = &src0, dst = &dst0)
            {
                if (len < LIMIT_64K)
                    return CompInternalU16(src, len, dst);
                else
                    return CompInternalU32(src, len, dst);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CompInternal(byte* src, int len, byte* dst)
        {
            if (len < LIMIT_64K)
                return CompInternalU16(src, len, dst);
            else
                return CompInternalU32(src, len, dst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref byte GetClearHashBucket()
        {
            ref var p0 = ref MemoryMarshal.GetArrayDataReference(HashBucket);
            ref var p = ref p0;
            ref var e = ref Unsafe.AddByteOffset(ref p0, HASH_SIZE);
            do
            {
                Unsafe.As<byte, ulong>(ref p) = 0;
                Unsafe.As<byte, ulong>(ref Unsafe.AddByteOffset(ref p, 8)) = 0;
                Unsafe.As<byte, ulong>(ref Unsafe.AddByteOffset(ref p, 16)) = 0;
                Unsafe.As<byte, ulong>(ref Unsafe.AddByteOffset(ref p, 24)) = 0;
            }
            while (Unsafe.IsAddressLessThan(ref p = ref Unsafe.AddByteOffset(ref p, 32), ref e));
            return ref p0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort HashU16(void* p) =>
            (ushort)((*(uint*)p * 2654435761U) >> (MINMATCH * 8) - (HASHLOG + 1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint HashU32(void* p) =>
            (uint)(((*(ulong*)p << 24) * 889523592379UL) >> (64 - HASHLOG));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Match(byte* s, byte* m, byte* e, byte* b)
        {
            var p = s;
            while (true)
            {
                if (p <= e - 8)
                {
                    var d = *(long*)p ^ *(long*)m;
                    if (d != 0)
                        return (uint)(p - s) + b[(ulong)(d & -d) * 0x0218A392CDABBD3F >> 58];

                    p += 8;
                    m += 8;
                }
                else
                {
                    if (p <= e - 4 && *(int*)p == *(int*)m) { p += 4; m += 4; }
                    if (p <= e - 2 && *(short*)p == *(short*)m) { p += 2; m += 2; }
                    if (p <= e - 1 && *(byte*)p == *(byte*)m)
                        return (uint)(p - s) + 1;
                    else
                        return (uint)(p - s);
                }
            }
        }

        private static int CompInternalU16(byte* src, int len, byte* dst)
        {
            fixed (byte* db0 = DeBruijnBytes)
            fixed (byte* ht0 = &GetClearHashBucket())
            {
                byte* mt, tok;
                var ht = (ushort*)ht0;

                var ip = src;
                var ie = src + len;
                var op = dst;

                var anc = src;
                var mfl = ie - MFLIMIT + 1;
                var mll = ie - LITS;

                if (len > MAX_INPUT_SIZE)
                    return 0;

                if (len < MIN_LENGTH)
                    return LastLiterals(ie, dst, op, anc);

                ht[HashU16(ip)] = (ushort)(ip - src);

                while (true)
                {
                    var fh = HashU16(++ip);
                    var fp = ip;
                    var st = 1;
                    var nb = 1 << SKIP_TRIGGER;

                    while (true)
                    {
                        var c = (ushort)(fp - src);
                        var i = ht[fh]; ht[fh] = c;

                        ip = fp;
                        fh = HashU16(fp += st);
                        st = nb++ >> SKIP_TRIGGER;

                        if (fp > mfl)
                            return LastLiterals(ie, dst, op, anc);

                        mt = src + i;
                        if (*(uint*)mt == *(uint*)ip)
                            break;
                    }

                    while (ip > anc && mt > src && ip[-1] == mt[-1]) { ip--; mt--; }
                    tok = op++;

                    var r = (int)(ip - anc);
                    if (r >= RUN_MASK)
                    {
                        *tok = RUN_MASK << ML_BITS;

                        var l = r - RUN_MASK;
                        while (l >= 255) { l -= 255; *op = 255; op++; }
                        *op = (byte)l; op++;
                    }
                    else
                    {
                        *tok = (byte)(r << ML_BITS);
                    }

                    var d = op; op += r;
                    do { *(long*)d = *(long*)anc; d += 8; anc += 8; }
                    while (d < op);

                    while (true)
                    {
                        *(ushort*)op = (ushort)(ip - mt); op += 2;

                        var mc = Match(ip + MINMATCH, mt + MINMATCH, mll, db0); ip += mc + MINMATCH;
                        if (mc >= ML_MASK)
                        {
                            *tok += ML_MASK; mc -= ML_MASK;

                            *(uint*)op = 0xFFFFFFFF;
                            while (mc >= 4 * 255)
                            {
                                mc -= 4 * 255;
                                *(uint*)(op += 4) = 0xFFFFFFFF;
                            }
                            op += mc / 255 + 1;
                            op[-1] = (byte)(mc % 255);
                        }
                        else
                        {
                            *tok += (byte)mc;
                        }

                        if ((anc = ip) < mfl)
                        {
                            ht[HashU16(ip - 2)] = (ushort)(ip - src - 2);

                            var h = HashU16(ip);
                            var c = (ushort)(ip - src);
                            var i = ht[h]; ht[h] = c;

                            mt = src + i;
                            if (*(uint*)mt == *(uint*)ip)
                                *(tok = op++) = 0;
                            else
                                break;
                        }
                        else
                            break;
                    }
                }
            }
        }

        private static int CompInternalU32(byte* src, int len, byte* dst)
        {
            fixed (byte* db0 = DeBruijnBytes)
            fixed (byte* ht0 = &GetClearHashBucket())
            {
                byte* mt, tok;
                var ht = (uint*)ht0;

                var ip = src;
                var ie = src + len;
                var op = dst;

                var anc = src;
                var mfl = ie - MFLIMIT + 1;
                var mll = ie - LITS;

                if (len > MAX_INPUT_SIZE)
                    return 0;

                if (len < MIN_LENGTH)
                    return LastLiterals(ie, dst, op, anc);

                ht[HashU32(ip)] = (uint)(ip - src);

                while (true)
                {
                    var fh = HashU32(++ip);
                    var fp = ip;
                    var st = 1;
                    var nb = 1 << SKIP_TRIGGER;

                    while (true)
                    {
                        var c = (uint)(fp - src);
                        var i = ht[fh]; ht[fh] = c;

                        ip = fp;
                        fh = HashU32(fp += st);
                        st = nb++ >> SKIP_TRIGGER;

                        if (fp <= mfl)
                        {
                            mt = src + i;
                            if (i + DISTANCE_MAX >= c && *(uint*)mt == *(uint*)ip)
                                break;
                        }
                        else
                        {
                            return LastLiterals(ie, dst, op, anc);
                        }
                    }

                    while (ip > anc && mt > src && ip[-1] == mt[-1]) { ip--; mt--; }
                    tok = op++;

                    var r = (int)(ip - anc);
                    if (r >= RUN_MASK)
                    {
                        *tok = RUN_MASK << ML_BITS;

                        var l = r - RUN_MASK;
                        while (l >= 255) { l -= 255; *op = 255; op++; }
                        *op = (byte)l; op++;
                    }
                    else
                    {
                        *tok = (byte)(r << ML_BITS);
                    }

                    var d = op; op += r;
                    do { *(long*)d = *(long*)anc; d += 8; anc += 8; }
                    while (d < op);

                    while (true)
                    {
                        *(ushort*)op = (ushort)(ip - mt); op += 2;

                        var mc = Match(ip + MINMATCH, mt + MINMATCH, mll, db0); ip += mc + MINMATCH;
                        if (mc >= ML_MASK)
                        {
                            *tok += ML_MASK; mc -= ML_MASK;

                            *(uint*)op = 0xFFFFFFFF;
                            while (mc >= 4 * 255)
                            {
                                mc -= 4 * 255;
                                *(uint*)(op += 4) = 0xFFFFFFFF;
                            }
                            op += mc / 255 + 1;
                            op[-1] = (byte)(mc % 255);
                        }
                        else
                        {
                            *tok += (byte)mc;
                        }

                        if ((anc = ip) < mfl)
                        {
                            ht[HashU32(ip - 2)] = (uint)(ip - src - 2);

                            var h = HashU32(ip);
                            var c = (uint)(ip - src);
                            var i = ht[h]; ht[h] = c;

                            mt = src + i;
                            if (i + DISTANCE_MAX >= c && *(uint*)mt == *(uint*)ip)
                                *(tok = op++) = 0;
                            else
                                break;
                        }
                        else
                            break;
                    }
                }
            }
        }

        private static int LastLiterals(byte* ie, byte* dst, byte* op, byte* anc)
        {
            var r = (int)(ie - anc);
            if (r >= RUN_MASK)
            {
                *op = RUN_MASK << ML_BITS; op++;

                var l = r - RUN_MASK;
                while (l >= 255) { l -= 255; *op = 255; op++; }
                *op = (byte)l; op++;
            }
            else
            {
                *op = (byte)(r << ML_BITS); op++;
            }

            NativeMemory.Copy(anc, op, (uint)r);
            return (int)(op + r - dst);
        }
    }
}