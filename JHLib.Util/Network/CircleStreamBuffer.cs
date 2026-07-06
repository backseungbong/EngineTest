using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Network
{
    public class CircleStreamBuffer
    {
        private const int SIZE = 1 << 14; // 16384, 16kb (마스크 연산으로 인해 사이즈는 반드시 2의 제곱수여야함)
        private const int MASK = SIZE - 1;

        private const int LF = 10;
        private const int CR = 13;

        private readonly byte[] _buf = GC.AllocateUninitializedArray<byte>(SIZE);
        private StreamController _controller = new();

        public void Reset() => _controller = new();
        public void PushBytes(byte[] bytes, int count) => _controller.PushBytes(_buf, bytes, count);
        public bool TryNextBytes(out byte[] result) => _controller.TryNextBytes(_buf, out result);
        public bool TryNextSentence(out string result) => _controller.TryNextSentence(_buf, out result);

        private class StreamController
        {
            private volatile int _wdx;
            private int _rdx;
            private int _fdx;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public void PushBytes(byte[] dest, byte[] source, int count)
            {
                if ((uint)(count - 1) < SIZE) // count가 0인 경우도 처리하기위해 -1 처리후 비교
                {
                    ref var d = ref Ref(dest);
                    ref var s = ref Ref(source);

                    var w = _wdx;
                    if (w + count > SIZE)
                    {
                        CopyBytes(ref s, ref Add(ref d, w), SIZE - w);
                        CopyBytes(ref Add(ref s, SIZE - w), ref d, w + count - SIZE);
                    }
                    else
                    {
                        CopyBytes(ref s, ref Add(ref d, w), count);
                    }
                    _wdx = w + count & MASK;
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public bool TryNextBytes(byte[] source, out byte[] result)
            {
                var w = _wdx;
                var r = _rdx;
                if (r != w)
                {
                    _rdx = w;
                    result = SplitAsBytes(ref Ref(source), r, w);
                    return true;
                }
                result = default;
                return false;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public bool TryNextSentence(byte[] source, out string result)
            {
                var w = _wdx;
                var r = _rdx;
                if (r != w)
                {
                    var f = _fdx;
                    ref var s = ref Ref(source);
                    do
                    {
                        if (Add(ref s, r) <= CR && (Add(ref s, r) == CR || Add(ref s, r) == LF))
                        {
                            if (r != f)
                            {
                                _rdx = r + 1 & MASK;
                                _fdx = r + 1 & MASK;
                                result = SplitAsString(ref s, f, r);
                                return true;
                            }
                            f = r + 1 & MASK;
                        }
                        r = r + 1 & MASK;
                    }
                    while (r != w);
                    _rdx = w;
                    _fdx = f;
                }
                result = default;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static byte[] SplitAsBytes(ref byte s, int p1, int p2)
            {
                var l = p2 - p1;
                if (l > 0)
                {
                    var r = new byte[l];
                    CopyBytes(ref Add(ref s, p1), ref Ref(r), l);
                    return r;
                }
                else
                {
                    var r = new byte[SIZE + l];
                    ref var d = ref Ref(r);
                    CopyBytes(ref Add(ref s, p1), ref d, SIZE - p1);
                    if (p2 != 0)
                        CopyBytes(ref s, ref Add(ref d, SIZE - p1), p2);
                    return r;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static string SplitAsString(ref byte s, int p1, int p2)
            {
                var l = p2 - p1;
                if (l > 0)
                {
                    var r = new string(default, l);
                    CopyASCII(ref Add(ref s, p1), ref Ref(r), l);
                    return r;
                }
                else
                {
                    var r = new string(default, SIZE + l);
                    ref var d = ref Ref(r);
                    CopyASCII(ref Add(ref s, p1), ref d, SIZE - p1);
                    if (p2 != 0)
                        CopyASCII(ref s, ref Add(ref d, (SIZE - p1) * sizeof(char)), p2);
                    return r;
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void CopyBytes(ref byte s, ref byte d, int l)
            {
                if (l > 02)
                {
                    if (l > 04)
                    {
                        if (l > 08)
                        {
                            if (l > 16)
                            {
                                if (l > 32)
                                {
                                    do
                                    {
                                        As<ulong>(ref d) = As<ulong>(ref s);
                                        As<ulong>(ref d, 8) = As<ulong>(ref s, 8);
                                        As<ulong>(ref d, 16) = As<ulong>(ref s, 16);
                                        As<ulong>(ref d, 24) = As<ulong>(ref s, 24);
                                        s = ref Add(ref s, 32);
                                        d = ref Add(ref d, 32);
                                    }
                                    while ((l -= 32) > 32);
                                    s = ref Sub(ref s, 32 - l);
                                    d = ref Sub(ref d, 32 - l);
                                    As<ulong>(ref d) = As<ulong>(ref s);
                                    As<ulong>(ref d, 8) = As<ulong>(ref s, 8);
                                    As<ulong>(ref d, 16) = As<ulong>(ref s, 16);
                                    As<ulong>(ref d, 24) = As<ulong>(ref s, 24);
                                    return;
                                }
                                As<ulong>(ref d, 8) = As<ulong>(ref s, 8);
                                As<ulong>(ref d, l - 16) = As<ulong>(ref s, l - 16);
                            }
                            As<ulong>(ref d) = As<ulong>(ref s);
                            As<ulong>(ref d, l - 8) = As<ulong>(ref s, l - 8);
                            return;
                        }
                        As<int>(ref d) = As<int>(ref s);
                        As<int>(ref d, l - 4) = As<int>(ref s, l - 4);
                        return;
                    }
                    As<short>(ref d) = As<short>(ref s);
                    As<short>(ref d, l - 2) = As<short>(ref s, l - 2);
                    return;
                }
                d = s;
                Add(ref d, l - 1) = Add(ref s, l - 1);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void CopyASCII(ref byte s, ref byte d, int l)
            {
                if (l <= 4)
                {
                    d = s;
                    if (l > 2)
                    {
                        Add(ref d, 2) = Add(ref s, 1);
                        Add(ref d, 4) = Add(ref s, 2);
                    }
                    Add(ref d, (l - 1) * 2) = Add(ref s, l - 1);
                }
                else
                {
                    var n = l - 4;
                    do
                    {
                        d = s;
                        Add(ref d, 2) = Add(ref s, 1);
                        Add(ref d, 4) = Add(ref s, 2);
                        Add(ref d, 6) = Add(ref s, 3);
                        s = ref Add(ref s, 4);
                        d = ref Add(ref d, 8);
                    }
                    while ((n -= 4) > 0);

                    s = ref Add(ref s, n);
                    d = ref Add(ref d, n * 2);
                    d = s;
                    Add(ref d, 2) = Add(ref s, 1);
                    Add(ref d, 4) = Add(ref s, 2);
                    Add(ref d, 6) = Add(ref s, 3);
                }
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref byte Ref(byte[] bytes) =>
                ref MemoryMarshal.GetArrayDataReference(bytes);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref byte Ref(string str) =>
                ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference<char>(str));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref byte Add(ref byte ptr, int offset) =>
                ref Unsafe.AddByteOffset(ref ptr, offset);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref byte Sub(ref byte ptr, int offset) =>
                ref Unsafe.SubtractByteOffset(ref ptr, offset);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T As<T>(ref byte ptr) =>
                ref Unsafe.As<byte, T>(ref ptr);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T As<T>(ref byte ptr, int offset) =>
                ref Unsafe.As<byte, T>(ref Unsafe.AddByteOffset(ref ptr, offset));
        }
    }
}