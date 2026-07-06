using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Simd
{
    public static unsafe class FillClear
    {
        private static readonly FillClearBase Execute;

        /// <summary> SIMD를 활용한 메모리 초기화 (반드시 256바이트 길이보다 큰 메모리 공간을 대상으로 한다) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Run(byte* p, int l) => Run(p, l);

        /// <summary> SIMD를 활용한 메모리 초기화 (반드시 256바이트 길이보다 큰 메모리 공간을 대상으로 한다) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Run(ref byte p, int l)
        {
            fixed (byte* t = &p)
            {
                Execute.Clear(t, l);
                return;
            }
        }

        static FillClear()
        {
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X64:
                case Architecture.X86:
                    if (Avx512F.IsSupported)
                        Execute = new FillClearAvx512();
                    else if (Avx2.IsSupported)
                        Execute = new FillClearAvx2();
                    else if (Sse2.IsSupported)
                        Execute = new FillClearSse2();
                    break;

                case Architecture.Arm64:
                    if (AdvSimd.IsSupported)
                        Execute = new FillClearAdvSIMD();
                    break;
            }

            if (Execute == null)
            {
                if (Vector256.IsHardwareAccelerated)
                    Execute = new FillClearVec256();
                else if (Vector128.IsHardwareAccelerated)
                    Execute = new FillClearVec128();
                else
                    Execute = new FillClearUnsafe();
            }
        }

        private abstract class FillClearBase
        {
            /// <summary> SIMD를 활용한 메모리 초기화 (반드시 256바이트 길이보다 큰 메모리 공간을 대상으로 한다) </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public abstract void Clear(byte* p, int l);
        }

        private class FillClearSse2 : FillClearBase
        {
            public override void Clear(byte* p, int l)
            {
                var e = p + (uint)l;
                var z = Vector128<byte>.Zero;
                Sse2.Store(p, z);

                var t = (byte*)((nint)p + 16 & ~15);
                if (l < SIMDParam.NonTemporalStoreThresholdForFill)
                {
                RE: Sse2.StoreAligned(t + 00, z);
                    Sse2.StoreAligned(t + 16, z);
                    Sse2.StoreAligned(t + 32, z);
                    Sse2.StoreAligned(t + 48, z);
                    if ((t += 64) < e - 64) goto RE;
                }
                else
                {
                RE: Sse2.StoreAlignedNonTemporal(t + 00, z);
                    Sse2.StoreAlignedNonTemporal(t + 16, z);
                    Sse2.StoreAlignedNonTemporal(t + 32, z);
                    Sse2.StoreAlignedNonTemporal(t + 48, z);
                    if ((t += 64) < e - 64) goto RE;
                }

                if (t < e - 16)
                {
                    do Sse2.StoreAligned(t, z);
                    while ((t += 16) < e - 16);
                }
                Sse2.Store(e - 16, z);
            }
        }

        private class FillClearAvx2 : FillClearBase
        {
            public override void Clear(byte* p, int l)
            {
                var e = p + (uint)l;
                var z = Vector256<byte>.Zero;
                Avx.Store(p, z);

                var t = (byte*)((nint)p + 32 & ~31);
                if (l < SIMDParam.NonTemporalStoreThresholdForFill)
                {
                RE: Avx.StoreAligned(t + 00, z);
                    Avx.StoreAligned(t + 32, z);
                    Avx.StoreAligned(t + 64, z);
                    Avx.StoreAligned(t + 96, z);
                    if ((t += 128) < e - 128) goto RE;
                }
                else
                {
                RE: Avx.StoreAlignedNonTemporal(t + 00, z);
                    Avx.StoreAlignedNonTemporal(t + 32, z);
                    Avx.StoreAlignedNonTemporal(t + 64, z);
                    Avx.StoreAlignedNonTemporal(t + 96, z);
                    if ((t += 128) < e - 128) goto RE;
                }

                if (t < e - 32)
                {
                    do Avx.StoreAligned(t, z);
                    while ((t += 32) < e - 32);
                }
                Avx.Store(e - 32, z);
            }
        }

        private class FillClearAvx512 : FillClearBase
        {
            public override void Clear(byte* p, int l)
            {
                var e = p + (uint)l;
                var z = Vector512<byte>.Zero;
                Avx512F.Store(p, z);

                var t = (byte*)((nint)p + 64 & ~63);
                if (t < e - 256)
                {
                    if (l < SIMDParam.NonTemporalStoreThresholdForFill)
                    {
                    RE: Avx512F.StoreAligned(t + 00, z);
                        Avx512F.StoreAligned(t + 64, z);
                        Avx512F.StoreAligned(t + 128, z);
                        Avx512F.StoreAligned(t + 192, z);
                        if ((t += 256) < e - 256) goto RE;
                    }
                    else
                    {
                    RE: Avx512F.StoreAlignedNonTemporal(t + 00, z);
                        Avx512F.StoreAlignedNonTemporal(t + 64, z);
                        Avx512F.StoreAlignedNonTemporal(t + 128, z);
                        Avx512F.StoreAlignedNonTemporal(t + 192, z);
                        if ((t += 256) < e - 256) goto RE;
                    }
                }

                if (t < e - 64)
                {
                    do Avx512F.StoreAligned(t, z);
                    while ((t += 64) < e - 64);
                }
                Avx512F.Store(e - 64, z);
            }
        }

        private class FillClearAdvSIMD : FillClearBase
        {
            public override void Clear(byte* p, int l)
            {
                var e = p + l;
                var z = Vector128<byte>.Zero;
                AdvSimd.Store(p, z);

                var t = (byte*)((nint)p + 16 & ~15);
            RE: AdvSimd.Store(t + 00, z);
                AdvSimd.Store(t + 16, z);
                AdvSimd.Store(t + 32, z);
                AdvSimd.Store(t + 48, z);
                if ((t += 64) < e - 64) goto RE;

                if (t < e - 16)
                {
                    do AdvSimd.Store(t, z);
                    while ((t += 16) < e - 16);
                }
                AdvSimd.Store(e - 16, z);
            }
        }

        private class FillClearVec128 : FillClearBase
        {
            public override void Clear(byte* p, int l)
            {
                var e = p + l;
                var z = Vector128<byte>.Zero;
                Unsafe.WriteUnaligned(p, z);

                var t = (byte*)((nint)p + 16 & ~15);
                if (t < e - 256)
                {
                RE: *(Vector128<byte>*)(t + 00) = z;
                    *(Vector128<byte>*)(t + 16) = z;
                    *(Vector128<byte>*)(t + 32) = z;
                    *(Vector128<byte>*)(t + 48) = z;
                    *(Vector128<byte>*)(t + 64) = z;
                    *(Vector128<byte>*)(t + 80) = z;
                    *(Vector128<byte>*)(t + 96) = z;
                    *(Vector128<byte>*)(t + 112) = z;
                    *(Vector128<byte>*)(t + 128) = z;
                    *(Vector128<byte>*)(t + 144) = z;
                    *(Vector128<byte>*)(t + 160) = z;
                    *(Vector128<byte>*)(t + 176) = z;
                    *(Vector128<byte>*)(t + 192) = z;
                    *(Vector128<byte>*)(t + 208) = z;
                    *(Vector128<byte>*)(t + 224) = z;
                    *(Vector128<byte>*)(t + 240) = z;
                    if ((t += 256) < e - 256) goto RE;
                }
                if (t < e - 128)
                {
                    *(Vector128<byte>*)(t + 00) = z;
                    *(Vector128<byte>*)(t + 16) = z;
                    *(Vector128<byte>*)(t + 32) = z;
                    *(Vector128<byte>*)(t + 48) = z;
                    *(Vector128<byte>*)(t + 64) = z;
                    *(Vector128<byte>*)(t + 80) = z;
                    *(Vector128<byte>*)(t + 96) = z;
                    *(Vector128<byte>*)(t + 112) = z;
                    t += 128;
                }
                if (t < e - 64)
                {
                    *(Vector128<byte>*)(t + 00) = z;
                    *(Vector128<byte>*)(t + 16) = z;
                    *(Vector128<byte>*)(t + 32) = z;
                    *(Vector128<byte>*)(t + 48) = z;
                    t += 64;
                }
                if (t < e - 32)
                {
                    *(Vector128<byte>*)(t + 00) = z;
                    *(Vector128<byte>*)(t + 16) = z;
                    t += 32;
                }
                if (t < e - 16)
                {
                    *(Vector128<byte>*)(t + 00) = z;
                }
                Unsafe.WriteUnaligned(e - 16, z);
            }
        }

        private class FillClearVec256 : FillClearBase
        {
            public override void Clear(byte* p, int l)
            {
                var e = p + l;
                var z = Vector256<byte>.Zero;
                Unsafe.WriteUnaligned(p, z);

                var t = (byte*)((nint)p + 32 & ~31);
                if (t < e - 256)
                {
                RE: *(Vector256<byte>*)(t + 00) = z;
                    *(Vector256<byte>*)(t + 32) = z;
                    *(Vector256<byte>*)(t + 64) = z;
                    *(Vector256<byte>*)(t + 96) = z;
                    *(Vector256<byte>*)(t + 128) = z;
                    *(Vector256<byte>*)(t + 160) = z;
                    *(Vector256<byte>*)(t + 192) = z;
                    *(Vector256<byte>*)(t + 224) = z;
                    if ((t += 256) < e - 256) goto RE;
                }
                if (t < e - 128)
                {
                    *(Vector256<byte>*)(t + 00) = z;
                    *(Vector256<byte>*)(t + 32) = z;
                    *(Vector256<byte>*)(t + 64) = z;
                    *(Vector256<byte>*)(t + 96) = z;
                    t += 128;
                }
                if (t < e - 64)
                {
                    *(Vector256<byte>*)(t + 00) = z;
                    *(Vector256<byte>*)(t + 32) = z;
                    t += 64;
                }
                if (t < e - 32)
                {
                    *(Vector256<byte>*)(t + 00) = z;
                }
                Unsafe.WriteUnaligned(e - 32, z);
            }
        }

        private class FillClearUnsafe : FillClearBase
        {
            public override void Clear(byte* p, int l)
            {
                Unsafe.InitBlock(p, 0, (uint)l);
            }
        }
    }
}