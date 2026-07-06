using JHLib.Util.Helper;
using JHLib.Util.Simd;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.ArrayControl
{
    using static JHLib.Util.Helper.RefCommand;
    public unsafe static class AC
    {
        public static bool EnableBulk256BitCopy { get; set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ArgumentException() => throw new ArgumentException("invalid range");

        /// <summary>
        /// 지정한 인덱스로 배열의 아이템을 OutOfRange 에러 없이 가져온다 <para/>
        /// 범위가 벗어난 경우 defaultValue를 리턴한다 <para/>
        /// 직접적인 배열 인덱스 접근 방식과 다른점은, 배열 범위 추가 체크제거 및 예외분기 제거를 노린 컴파일러 최적화 함수
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetItem<T>(T[] array, int index, T defaultValue = default)
        {
            if ((uint)index < (uint)array.Length)
                return array[index];
            else
                return defaultValue;
        }

        /// <summary>
        /// Float2D array의 연속하는 중복 좌표를 제거하고 앞쪽으로 정렬한다 <para/>
        /// 리턴값은 제거완료된 좌표의 갯수이다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int F2Dedupe(Float2D[] path) =>
            F2Dedupe(ref MemoryMarshal.GetArrayDataReference(path), path.Length);

        /// <summary> 
        /// Float2D array의 연속하는 중복 좌표를 제거하고 앞쪽으로 정렬한다 <para/>
        /// 리턴값은 제거완료된 좌표의 갯수이다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int F2Dedupe(ref Float2D path0, int pathn)
        {
            fixed (Float2D* p0 = &path0)
            {
                var result = pathn;
                if (result >= 2) result = F2DedupeInternal(p0, (uint)pathn);
                return result;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int F2DedupeInternal(Float2D* p0, uint pn)
        {
            var p = (ulong*)p0 + 1;
            var e = (ulong*)p0 + pn;

            while (*(p - 1) != *p && ++p < e) ;

            var t = p - 1;
            if (++p < e)
            {
                if (p < e - 4)
                {
                RE: if (t[0] != p[0]) *++t = p[0];
                    if (p[0] != p[1]) *++t = p[1];
                    if (p[1] != p[2]) *++t = p[2];
                    if (p[2] != p[3]) *++t = p[3];
                    if ((p += 4) < e - 4) goto RE;
                }
                do if (*t != *p) { *++t = *p; }
                while (++p < e);
            }
            return (int)((nint)(t + 1) - (nint)p0) >> 3;
        }

        /// <summary>
        /// Float2D array의 연속으로 같은 방향으로 나아가는 좌표를 제거하고 앞쪽으로 정렬한다 <para/>
        /// 리턴값은 제거완료된 좌표의 갯수이다
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int F2DedupeV(Float2D[] path)
        {
            if (path != null && path.Length != 0)
            {
                if (path.Length >= 2)
                    return F2DedupeVectorInternal(ref MemoryMarshal.GetArrayDataReference(path), path.Length);
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Float2D array의 연속으로 같은 방향으로 나아가는 좌표를 제거하고 앞쪽으로 정렬한다 <para/>
        /// 리턴값은 제거완료된 좌표의 갯수이다
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int F2DedupeV(ref Float2D source0, int count) =>
            count >= 2 ? F2DedupeVectorInternal(ref source0, count) : count;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int F2DedupeVectorInternal(ref Float2D source0, int count)
        {
            ref var p = ref AsB(ref source0);
            ref var e = ref AddB(ref p, count * Float2D.SIZE);
            ref var t = ref p;

            var p2 = AsT<Float2D>(ref p);
            var deg = -1;

            while (true)
            {
                p = ref AddB(ref p, 8);
                if (LessThan(ref p, ref e) == false) break;

                var p1 = p2; p2 = AsT<Float2D>(ref p);
                if (p2.X != p1.X || p2.Y != p1.Y)
                {
                    if (deg != (deg = (int)(MathHelper.Vec2Deg(p2.X - p1.X, p2.Y - p1.Y) * 2)))
                        t = ref AddB(ref t, 8);
                    AsT<Float2D>(ref t) = p2;
                }
            }
            return (SubRef(ref source0, ref t) >> 3) + 1;
        }

        /// <summary> 초기화 없는 배열 생성을 시도한다 (배열이 클수록 성능상 이점이 크다) </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T[] UninitializedArray<T>(int count)
        {
            if (count < (2048 / Unsafe.SizeOf<T>())) return new T[count];
            return GC.AllocateUninitializedArray<T>(count);
        }

        /// <summary> T source 배열을 새로운 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] CopyNew<T>(T[] source) where T : unmanaged => CopyNew(source, source.Length, source.Length);

        /// <summary> T source 배열을 새로운 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] CopyNew<T>(T[] source, int copySize) where T : unmanaged => CopyNew(source, copySize, copySize);

        /// <summary> T source 배열을 새로운 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] CopyNew<T>(T[] source, int newSize, int copySize) where T : unmanaged
        {
            var dest = UninitializedArray<T>(newSize); Copy(source, dest, copySize);
            return dest;
        }

        /// <summary> T source 배열을 새로운 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] CopyNew<T>(ref T source, int copySize) where T : unmanaged => CopyNew(ref source, copySize, copySize);

        /// <summary> T source 배열을 새로운 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] CopyNew<T>(ref T source, int newSize, int copySize) where T : unmanaged
        {
            var dest = UninitializedArray<T>(newSize); Copy(ref source, ref RefT(dest), copySize);
            return dest;
        }

        /// <summary> source 배열을 dest 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(T[] source, T[] dest) where T : unmanaged =>
            Copy(ref RefT(source), ref RefT(dest), source.Length);

        /// <summary> source 배열을 dest 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(T[] source, T[] dest, int count) where T : unmanaged =>
            Copy(ref RefT(source), ref RefT(dest), count);

        /// <summary> source 배열을 dest 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(T[] source, int sindex, T[] dest, int dindex, int count) where T : unmanaged =>
            Copy(ref RefT(source, sindex), ref RefT(dest, dindex), count);

        /// <summary> source 배열을 dest 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(T[] source, ref T dest0, int count) where T : unmanaged =>
            Copy(ref RefT(source), ref dest0, count);

        /// <summary> source 배열을 dest 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(ref T source0, T[] dest, int destIndex, int count) where T : unmanaged =>
            Copy(ref source0, ref RefT(dest, destIndex), count);


        /// <summary> source 배열을 dest 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(byte[] source, byte[] dest, int count) =>
            Copy(ref RefB(source), ref RefB(dest), count);

        /// <summary> source 배열을 dest 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(byte* source0, byte* dest0, int length) =>
            Copy(ref *source0, ref *dest0, length);


        /// <summary> source 배열을 dest 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(ref T source0, ref T dest0, int count) where T : unmanaged
        {
            if (count > 3) CopyInternal(ref AsB(ref source0), ref AsB(ref dest0), count * Unsafe.SizeOf<T>());
            else if (count > 0)
            {
                // 어셈블리 결과를 통해 아래와 같은 초기화 순서로 최적화
                AddTU(ref dest0, count >> 1) = AddTU(ref source0, count >> 1);
                AddTU(ref dest0, count - 1) = AddTU(ref source0, count - 1);
                dest0 = source0;
            }
        }

        /// <summary> source 배열을 dest 배열에 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(ref byte source0, ref byte dest0, int length)
        {
            if (length > 3) CopyInternal(ref source0, ref dest0, length);
            else if (length > 0)
            {
                // 어셈블리 결과를 통해 아래와 같은 초기화 순서로 최적화
                AddBU(ref dest0, length >> 1) = AddBU(ref source0, length >> 1);
                AddBU(ref dest0, length - 1) = AddBU(ref source0, length - 1);
                dest0 = source0;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CopyInternal(ref byte s0, ref byte d0, int l)
        {
            if (l <= 8)
            {
                AsT<uint>(ref d0) = AsT<uint>(ref s0);
                AsTU<uint>(ref d0, l - 4) = AsTU<uint>(ref s0, l - 4);
            }
            else
            {
                if (l > 16)
                {
                    if (l > 64 && EnableBulk256BitCopy == false)
                    {
                        Unsafe.CopyBlock(ref d0, ref s0, (uint)l);
                        return;
                    }
                    else if (l > 32)
                    {
                        ref var d = ref d0;
                        ref var s = ref s0;
                        ref var e = ref AddBU(ref s0, l - 32);
                        do
                        {
                            AsT<ulong>(ref d) = AsT<ulong>(ref s);
                            AsT<ulong>(ref d, 8) = AsT<ulong>(ref s, 8);
                            AsT<ulong>(ref d, 16) = AsT<ulong>(ref s, 16);
                            AsT<ulong>(ref d, 24) = AsT<ulong>(ref s, 24);
                            d = ref AddB(ref d, 32);
                            s = ref AddB(ref s, 32);
                        }
                        while (LessThan(ref s, ref e));

                        d = ref AddBU(ref d0, l - 32);
                        AsT<ulong>(ref d) = AsT<ulong>(ref e);
                        AsT<ulong>(ref d, 8) = AsT<ulong>(ref e, 8);
                        AsT<ulong>(ref d, 16) = AsT<ulong>(ref e, 16);
                        AsT<ulong>(ref d, 24) = AsT<ulong>(ref e, 24);
                        return;
                    }
                    AsT<ulong>(ref d0, 8) = AsT<ulong>(ref s0, 8);
                    AsTU<ulong>(ref d0, l - 16) = AsTU<ulong>(ref s0, l - 16);
                }
                AsT<ulong>(ref d0) = AsT<ulong>(ref s0);
                AsTU<ulong>(ref d0, l - 8) = AsTU<ulong>(ref s0, l - 8);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqual<T>(T[] a1, T[] a2) where T : unmanaged =>
            a1 == a2 || (a1.Length == a2.Length && IsEqualUnsafe(ref RefB(a1), ref RefB(a2), a1.Length * sizeof(T)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqual<T>(T[] a1, T[] a2, int count) where T : unmanaged =>
            a1 == a2 || IsEqualUnsafe(ref RefB(a1), ref RefB(a2), count * sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqualUnsafe<T>(T[] a1, T[] a2) where T : unmanaged =>
            IsEqualUnsafe(ref RefB(a1), ref RefB(a2), a1.Length * sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqualUnsafe<T>(T[] a1, T[] a2, int count) where T : unmanaged =>
            IsEqualUnsafe(ref RefB(a1), ref RefB(a2), count * sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqual<T>(ref T a1, ref T a2, int count) where T : unmanaged =>
            IsEqualUnsafe(ref AsB(ref a1), ref AsB(ref a2), count * sizeof(T));

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsEqualUnsafe(ref byte a, ref byte b, int l)
        {
            if (l > 2)
            {
                if (l > 4)
                {
                    if (l > 8)
                    {
                        if (l > 32)
                        {
                        RE: if (AsT<ulong>(ref a) == AsT<ulong>(ref b) &&
                                AsT<ulong>(ref a, 8) == AsT<ulong>(ref b, 8) &&
                                AsT<ulong>(ref a, 16) == AsT<ulong>(ref b, 16) &&
                                AsT<ulong>(ref a, 24) == AsT<ulong>(ref b, 24))
                            {
                                a = ref AddB(ref a, 32);
                                b = ref AddB(ref b, 32);
                                if ((l -= 32) > 32) goto RE;

                                if (AsT<ulong>(ref a, l - 8) == AsT<ulong>(ref b, l - 8) &&
                                    AsT<ulong>(ref a, l - 16) == AsT<ulong>(ref b, l - 16))
                                {
                                    if (l <= 16 ||
                                        (AsT<ulong>(ref a, l - 24) == AsT<ulong>(ref b, l - 24) &&
                                         AsT<ulong>(ref a, l - 32) == AsT<ulong>(ref b, l - 32)))
                                        return true;
                                }
                            }
                            return false;
                        }

                        if (AsT<ulong>(ref a) == AsT<ulong>(ref b) &&
                            AsT<ulong>(ref a, l - 8) == AsT<ulong>(ref b, l - 8))
                        {
                            if (l <= 16 ||
                                (AsT<ulong>(ref a, 8) == AsT<ulong>(ref b, 8) &&
                                AsT<ulong>(ref a, l - 16) == AsT<ulong>(ref b, l - 16)))
                                return true;
                        }
                        return false;
                    }

                    if (AsT<uint>(ref a) == AsT<uint>(ref b))
                        return AsT<uint>(ref a, l - 4) == AsT<uint>(ref b, l - 4);
                    return false;
                }

                if (AsT<ushort>(ref a) == AsT<ushort>(ref b))
                    return AsT<ushort>(ref a, l - 2) == AsT<ushort>(ref b, l - 2);
                return false;
            }

            if (l == 0 || (a == b && AddB(ref a, l - 1) == AddB(ref b, l - 1)))
                return true;
            return false;
        }

        /// <summary> 배열을 특정 값으로 채운다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fill<T>(T[] dest, T item) => Fill(dest, item, dest.Length);

        /// <summary> 배열을 특정 값으로 채운다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fill<T>(T[] dest, T item, int count)
        {
            if (count > 0)
                FillInternal(ref RefT(dest), item, count);
        }

        /// <summary> 포인터 공간을 특정 값으로 채운다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fill<T>(ref T dest0, T item, int count)
        {
            if (count > 0)
                FillInternal(ref dest0, item, count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FillInternal<T>(ref T dst0, T item, int cnt)
        {
            if (cnt < 8)
            {
                dst0 = item;
                if (cnt > 2)
                {
                    if (cnt > 4)
                    {
                        AddT(ref dst0, 1) = item;
                        AddT(ref dst0, 2) = item;
                        AddT(ref dst0, 3) = item;
                    }
                    AddTU(ref dst0, cnt - 3) = item;
                    AddTU(ref dst0, cnt - 2) = item;
                }
                AddTU(ref dst0, cnt - 1) = item;
            }
            else
            {
                MemoryMarshal.CreateSpan(ref dst0, cnt).Fill(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroFill<T>(T[] array) where T : unmanaged
        {
            ZeroFill(ref RefT(array), array.Length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroFill<T>(T[] array, int count) where T : unmanaged
        {
            if ((uint)count > (uint)array.Length) ArgumentException();
            ZeroFill(ref RefT(array), count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroFill<T>(T[] array, int index, int count) where T : unmanaged
        {
            if ((ulong)(uint)index + (uint)count > (ulong)array.Length) ArgumentException();
            ZeroFill(ref AddTU(ref RefT(array), index), count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroFill<T>(ref T array0, int count) where T : unmanaged
        {
            if (count > 3) ZeroFillInternal(ref AsB(ref array0), count * sizeof(T));
            else if (count > 0)
            {
                // 어셈블리 결과를 통해 아래와 같은 초기화 순서로 최적화
                AddTU(ref array0, count - 1) = default;
                array0 = default;
                AddTU(ref array0, count >> 1) = default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroFill(byte[] array)
        {
            ZeroFill(ref RefB(array), array.Length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroFill(byte[] array, int count)
        {
            if ((uint)count > (uint)array.Length) ArgumentException();
            ZeroFill(ref RefB(array), count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroFill(byte[] array, int index, int count)
        {
            if ((ulong)(uint)index + (uint)count > (ulong)array.Length) ArgumentException();
            ZeroFill(ref AddBU(ref RefB(array), index), count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroFill(ref byte array0, int count)
        {
            if (count > 3) ZeroFillInternal(ref array0, count);
            else if (count > 0)
            {
                // 어셈블리 결과를 통해 아래와 같은 초기화 순서로 최적화
                AddBU(ref array0, count - 1) = default;
                array0 = default;
                AddBU(ref array0, count >> 1) = default;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ZeroFillInternal(ref byte p, int l)
        {
            if (l >= 16)
            {
                if (l > 32)
                {
                    if (l > 256) { FillClear.Run(ref p, l); }
                    else // 33 ~ 256 바이트 범위 처리
                    {
                        ref var e = ref AddBU(ref p, l - 32);
                        do
                        {
                            AsT<ulong>(ref p) = 0;
                            AsT<ulong>(ref p, 8) = 0;
                            AsT<ulong>(ref p, 16) = 0;
                            AsT<ulong>(ref p, 24) = 0;
                        }
                        while (LessThan(ref p = ref AddB(ref p, 32), ref e));

                        AsT<ulong>(ref e) = 0;
                        AsT<ulong>(ref e, 8) = 0;
                        AsT<ulong>(ref e, 16) = 0;
                        AsT<ulong>(ref e, 24) = 0;
                    }
                }
                else // 16 ~ 32 바이트 범위 처리
                {
                    AsT<ulong>(ref p) = 0;
                    AsT<ulong>(ref p, 8) = 0;
                    AsTU<ulong>(ref p, l - 16) = 0;
                    AsTU<ulong>(ref p, l - 8) = 0;
                }
            }
            else // 4 ~ 15 바이트 범위 처리
            {
                AsT<uint>(ref p) = 0;
                AsTU<uint>(ref p, l - 4 >> 3 << 2) = 0;
                AsTU<uint>(ref p, l - 4 >> 2 << 2) = 0;
                AsTU<uint>(ref p, l - 4) = 0;
            }
        }
    }
}