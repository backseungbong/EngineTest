namespace JHLib.Util.Simd
{
    internal class CopyArray
    {
        /// <summary> source 배열을 dest 배열에 반대로 복사한다 </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void CopyReverse<T>(T[] source, T[] dest) =>
        //    CopyReverse(ref RefT(source), ref RefT(dest), source.Length);

        ///// <summary> source 배열을 dest 배열에 반대로 복사한다 </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void CopyReverse<T>(T[] source, T[] dest, int count) =>
        //    CopyReverse(ref RefT(source), ref RefT(dest), count);

        ///// <summary> source 배열을 dest 배열에 반대로 복사한다 </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void CopyReverse<T>(T[] source, int sourceIndex, T[] dest, int destIndex, int count) =>
        //    CopyReverse(ref RefT(source, sourceIndex), ref RefT(dest, destIndex), count);

        ///// <summary> source 배열을 dest 배열에 반대로 복사한다 </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void CopyReverse<T>(T[] source, ref T dest0, int count) =>
        //    CopyReverse(ref RefT(source), ref dest0, count);

        ///// <summary> source 배열을 dest 배열에 반대로 복사한다 </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void CopyReverse<T>(ref T source0, T[] dest, int destIndex, int count) =>
        //    CopyReverse(ref source0, ref RefT(dest, destIndex), count);

        ///// <summary> source 배열을 dest 배열에 반대로 복사한다 </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void CopyReverse<T>(ref T s0, ref T d0, nint c)
        //{
        //    if (c > 4)
        //    {
        //        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() == false)
        //        {
        //            if (Unsafe.SizeOf<T>() != 1)
        //                if (Unsafe.SizeOf<T>() != 2)
        //                    if (Unsafe.SizeOf<T>() != 4)
        //                    {
        //                        if (Unsafe.SizeOf<T>() == 8)
        //                        {
        //                            CopyReverse8(ref AsB(ref s0), ref AsB(ref d0), c);
        //                            return;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        CopyReverse4(ref AsB(ref s0), ref AsB(ref d0), c);
        //                        return;
        //                    }
        //                else
        //                {
        //                    CopyReverse2(ref AsB(ref s0), ref AsB(ref d0), c);
        //                    return;
        //                }
        //            else
        //            {
        //                CopyReverse1(ref AsB(ref s0), ref AsB(ref d0), c);
        //                return;
        //            }
        //        }
        //        CopyReverseT(ref s0, ref d0, c);
        //    }
        //    else if (c > 0)
        //    {
        //        d0 = AddT(ref s0, c - 1);
        //        if (c > 2)
        //        {
        //            AddT(ref d0, 1) = AddT(ref s0, c - 2);
        //            AddT(ref d0, 2) = AddT(ref s0, c - 3);
        //        }
        //        AddT(ref d0, c - 1) = s0;
        //    }
        //}

        //#region CopyReverse 
        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public static void CopyReverse1(ref byte s0, ref byte d0, nint c)
        //{
        //    ref var d = ref d0;
        //    ref var e = ref AddB(ref d0, c - 4);
        //    ref var s = ref AddB(ref s0, c - 4);

        //    var n = c;
        //    if (n > 32)
        //    {
        //        if (Avx2.IsSupported == false)
        //        {
        //            if (Vector128.IsHardwareAccelerated)
        //            {
        //                s = ref SubB(ref s, 28); // 32 오프셋 해야하지만, 함수 초기에서 이미 4 오프셋 됨
        //                do
        //                {
        //                    var v1 = Vector128.Shuffle(AsT<Vector128<byte>>(ref s), Vector128.Create((byte)
        //                        15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));
        //                    var v2 = Vector128.Shuffle(AsT<Vector128<byte>>(ref s, 16), Vector128.Create((byte)
        //                        15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));
        //                    AsT<Vector128<byte>>(ref d) = v2;
        //                    AsT<Vector128<byte>>(ref d, 16) = v1;
        //                    d = ref AddB(ref d, 32);
        //                    s = ref SubB(ref s, 32);
        //                }
        //                while ((n -= 32) > 32);
        //                s = ref AddB(ref s, 28);
        //            }
        //        }
        //        else
        //        {
        //            s = ref SubB(ref s, 28); // 32 오프셋 해야하지만, 함수 초기에서 이미 4 오프셋 됨
        //            do
        //            {
        //                var v = Avx2.Shuffle(AsT<Vector256<byte>>(ref s), Vector256.Create((byte)
        //                    15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
        //                    15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));
        //                AsT<Vector256<byte>>(ref d) = Avx2.Permute2x128(v, v, 0b00_01);
        //                d = ref AddB(ref d, 32);
        //                s = ref SubB(ref s, 32);
        //            }
        //            while ((n -= 32) > 32);
        //            s = ref AddB(ref s, 28);
        //        }
        //    }

        //    if (n > 4)
        //    {
        //        do
        //        {
        //            AsT<uint>(ref d) = BinaryPrimitives.ReverseEndianness(AsT<uint>(ref s));
        //            d = ref AddB(ref d, 4);
        //            s = ref SubB(ref s, 4);
        //        }
        //        while (LessThan(ref d, ref e));
        //    }
        //    AsT<uint>(ref e) = BinaryPrimitives.ReverseEndianness(AsT<uint>(ref s0));
        //}

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public static void CopyReverse2(ref byte s0, ref byte d0, nint c)
        //{
        //    ref var d = ref d0;
        //    ref var e = ref AddB(ref d0, (c - 4) * 2);
        //    ref var s = ref AddB(ref s0, (c - 4) * 2);

        //    var n = c;
        //    if (n > 16)
        //    {
        //        if (Avx2.IsSupported == false)
        //        {
        //            if (Vector128.IsHardwareAccelerated)
        //            {
        //                s = ref SubB(ref s, 24); // 32 오프셋 해야하지만, 함수 초기에서 이미 8 오프셋 됨
        //                do
        //                {
        //                    var v1 = Vector128.Shuffle(AsT<Vector128<short>>(ref s), Vector128.Create(7, 6, 5, 4, 3, 2, 1, 0));
        //                    var v2 = Vector128.Shuffle(AsT<Vector128<short>>(ref s, 16), Vector128.Create(7, 6, 5, 4, 3, 2, 1, 0));
        //                    AsT<Vector128<short>>(ref d) = v2;
        //                    AsT<Vector128<short>>(ref d, 16) = v1;
        //                    d = ref AddB(ref d, 32);
        //                    s = ref SubB(ref s, 32);
        //                }
        //                while ((n -= 16) > 16);
        //                s = ref AddB(ref s, 24);
        //            }
        //        }
        //        else
        //        {
        //            s = ref SubB(ref s, 24); // 32 오프셋 해야하지만, 함수 초기에서 이미 8 오프셋 됨
        //            do
        //            {
        //                var v = Avx2.Shuffle(AsT<Vector256<byte>>(ref s), Vector256.Create((byte)
        //                    14, 15, 12, 13, 10, 11, 8, 9, 6, 7, 4, 5, 2, 3, 0, 1,
        //                    14, 15, 12, 13, 10, 11, 8, 9, 6, 7, 4, 5, 2, 3, 0, 1));
        //                AsT<Vector256<byte>>(ref d) = Avx2.Permute2x128(v, v, 0b00_01);
        //                d = ref AddB(ref d, 32);
        //                s = ref SubB(ref s, 32);
        //            }
        //            while ((n -= 16) > 16);
        //            s = ref AddB(ref s, 24);
        //        }
        //    }

        //    if (n > 4)
        //    {
        //        do
        //        {
        //            AsT<short>(ref d) = AsT<short>(ref s, 6);
        //            AsT<short>(ref d, 2) = AsT<short>(ref s, 4);
        //            AsT<short>(ref d, 4) = AsT<short>(ref s, 2);
        //            AsT<short>(ref d, 6) = AsT<short>(ref s);
        //            d = ref AddB(ref d, 8);
        //            s = ref SubB(ref s, 8);
        //        }
        //        while (LessThan(ref d, ref e));
        //    }

        //    AsT<short>(ref e) = AsT<short>(ref s0, 6);
        //    AsT<short>(ref e, 2) = AsT<short>(ref s0, 4);
        //    AsT<short>(ref e, 4) = AsT<short>(ref s0, 2);
        //    AsT<short>(ref e, 6) = AsT<short>(ref s0);
        //}

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public static void CopyReverse4(ref byte s0, ref byte d0, nint c)
        //{
        //    ref var d = ref d0;
        //    ref var e = ref AddB(ref d0, (c - 4) * 4);
        //    ref var s = ref AddB(ref s0, (c - 4) * 4);

        //    var n = c;
        //    if (n > 8)
        //    {
        //        if (Avx2.IsSupported == false)
        //        {
        //            if (Vector128.IsHardwareAccelerated)
        //            {
        //                s = ref SubB(ref s, 16); // 32 오프셋 해야하지만, 함수 초기에서 이미 16 오프셋 됨
        //                do
        //                {
        //                    var v1 = Vector128.Shuffle(AsT<Vector128<int>>(ref s), Vector128.Create(3, 2, 1, 0));
        //                    var v2 = Vector128.Shuffle(AsT<Vector128<int>>(ref s, 16), Vector128.Create(3, 2, 1, 0));
        //                    AsT<Vector128<int>>(ref d) = v2;
        //                    AsT<Vector128<int>>(ref d, 16) = v1;
        //                    d = ref AddB(ref d, 32);
        //                    s = ref SubB(ref s, 32);
        //                }
        //                while ((n -= 8) > 8);
        //                s = ref AddB(ref s, 16);
        //            }
        //        }
        //        else
        //        {
        //            s = ref SubB(ref s, 16); // 32 오프셋 해야하지만, 함수 초기에서 이미 16 오프셋 됨
        //            do
        //            {
        //                AsT<Vector256<int>>(ref d) =
        //                    Avx2.PermuteVar8x32(AsT<Vector256<int>>(ref s), Vector256.Create(7, 6, 5, 4, 3, 2, 1, 0));
        //                d = ref AddB(ref d, 32);
        //                s = ref SubB(ref s, 32);
        //            }
        //            while ((n -= 8) > 8);
        //            s = ref AddB(ref s, 16);
        //        }
        //    }

        //    if (n > 4)
        //    {
        //        do
        //        {
        //            AsT<int>(ref d) = AsT<int>(ref s, 12);
        //            AsT<int>(ref d, 4) = AsT<int>(ref s, 8);
        //            AsT<int>(ref d, 8) = AsT<int>(ref s, 4);
        //            AsT<int>(ref d, 12) = AsT<int>(ref s);
        //            d = ref AddB(ref d, 16);
        //            s = ref SubB(ref s, 16);
        //        }
        //        while (LessThan(ref d, ref e));
        //    }

        //    AsT<int>(ref e) = AsT<int>(ref s0, 12);
        //    AsT<int>(ref e, 4) = AsT<int>(ref s0, 8);
        //    AsT<int>(ref e, 8) = AsT<int>(ref s0, 4);
        //    AsT<int>(ref e, 12) = AsT<int>(ref s0);
        //}

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public static void CopyReverse8(ref byte s0, ref byte d0, nint c)
        //{
        //    ref var d = ref d0;
        //    ref var e = ref AddB(ref d0, (c - 4) * 8);
        //    ref var s = ref AddB(ref s0, (c - 4) * 8);

        //    if (Avx2.IsSupported == false)
        //    {
        //        do
        //        {
        //            AsT<long>(ref d) = AsT<long>(ref s, 24);
        //            AsT<long>(ref d, 8) = AsT<long>(ref s, 16);
        //            AsT<long>(ref d, 16) = AsT<long>(ref s, 8);
        //            AsT<long>(ref d, 24) = AsT<long>(ref s);
        //            d = ref AddB(ref d, 32);
        //            s = ref SubB(ref s, 32);
        //        }
        //        while (LessThan(ref d, ref e));
        //    }
        //    else
        //    {
        //        do
        //        {
        //            AsT<Vector256<long>>(ref d) = Avx2.Permute4x64(AsT<Vector256<long>>(ref s), 0b00_01_10_11);
        //            d = ref AddB(ref d, 32);
        //            s = ref SubB(ref s, 32);
        //        }
        //        while (LessThan(ref d, ref e));
        //    }

        //    AsT<long>(ref e) = AsT<long>(ref s0, 24);
        //    AsT<long>(ref e, 8) = AsT<long>(ref s0, 16);
        //    AsT<long>(ref e, 16) = AsT<long>(ref s0, 8);
        //    AsT<long>(ref e, 24) = AsT<long>(ref s0);
        //}

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public static void CopyReverseT<T>(ref T s0, ref T d0, nint c)
        //{
        //    ref var d = ref d0;
        //    ref var e = ref AddT(ref d0, c - 4);
        //    ref var s = ref AddT(ref s0, c - 4);
        //    do
        //    {
        //        d = AddT(ref s, 3);
        //        AddT(ref d, 1) = AddT(ref s, 2);
        //        AddT(ref d, 2) = AddT(ref s, 1);
        //        AddT(ref d, 3) = s;
        //        s = ref SubT(ref s, 4);
        //        d = ref AddT(ref d, 4);
        //    }
        //    while (LessThan(ref d, ref e));
        //    e = AddT(ref s0, 3);
        //    AddT(ref e, 1) = AddT(ref s0, 2);
        //    AddT(ref e, 2) = AddT(ref s0, 1);
        //    AddT(ref e, 3) = s0;
        //}
    }
}