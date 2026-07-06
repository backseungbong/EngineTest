using JHLib.Util.ByteControl;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Serial
{
    using static System.Runtime.CompilerServices.Unsafe;
    using static System.Runtime.InteropServices.MemoryMarshal;
    internal class SerialBuffer : IDisposable
    {
        private const int SIZE_MIN = 4096; // 4KB
        private const int SIZE_MAX = 1048576; // 1MB
        private const int LF = ASCII.LINE_FEED;
        private const int CR = ASCII.CARRIAGE_RETURN;

        private byte[] _buk;
        private int _cap;
        private volatile int _wdx;
        private volatile int _rdx;
        private volatile bool _disposed;
        private readonly AutoResetEvent _update;

        public delegate void ReceivedSentence(byte[] sentence);
        public event ReceivedSentence OnReceivedSentence;

        public void Dispose()
        {
            if (_disposed == false)
            {
                _disposed = true;
                _update.Set();
            }
        }

        public SerialBuffer()
        {
            _buk = new byte[SIZE_MIN];
            _cap = SIZE_MIN;
            _wdx = 0;
            _rdx = 0;
            _update = new AutoResetEvent(false);
            new Thread(ProcessCreateSentence) { IsBackground = true }.Start();
        }

        private void ProcessCreateSentence()
        {
        AA: if (_update.WaitOne() && _disposed == false)
            {
                var w = _wdx;
                var r = _rdx;
                if (r >= w)
                    goto AA;

                ref var b = ref Ref(_buk);
                var s = r;
            BB: if (r < w - 4)
                {
                    while (true)
                    {
                        ref var p = ref AddB(ref b, r);
                        if (p > CR)
                            if (AddB(ref p, 1) > CR)
                                if (AddB(ref p, 2) > CR)
                                    if (AddB(ref p, 3) > CR) { if ((r += 4) >= w - 4) break; }
                                    else { r += 3; break; }
                                else { r += 2; break; }
                            else { r += 1; break; }
                        else break;
                    }
                }

            CC: if (AddB(ref b, r) != CR && AddB(ref b, r) != LF)
                {
                    if (++r < w)
                        goto CC;
                    else
                        goto AA;
                }
                else
                {
                    var l = r - s;
                    while (++r < w && (AddB(ref b, r) == CR || AddB(ref b, r) == LF)) ;
                    if (l != 0)
                    {
                        var sen = CopyNew(ref b, s, l);
                        _rdx = r;
                        OnReceivedSentence?.Invoke(sen);
                    }
                    else
                    {
                        _rdx = r;
                    }

                    s = r;
                    if (++r < w)
                        goto BB;
                }
            }
            _update.Dispose();
        }


        public void PushReceivedBytes(byte[] bytes, int count)
        {
            var wdx = _wdx;
            if (wdx <= _rdx)
            {
                wdx = 0;
                _wdx = 0;
                _rdx = 0;
            }

            if (bytes != null && count > 0)
            {
                var siz = wdx + count;
                if (siz > _cap)
                {
                    if (siz > SIZE_MAX)
                    {
                        _wdx = 0;
                        return;
                    }
                    Resize(wdx, count);
                }
                CopyTo(bytes, _buk, wdx, count);

                _wdx = siz;
                _update.Set();
            }
        }

        private void Resize(int wdx, int len)
        {
            // resize to a power of 2
            var resize = wdx + len - 1;
            resize |= resize >> 1;
            resize |= resize >> 2;
            resize |= resize >> 4;
            resize |= resize >> 8;
            resize |= resize >> 16;
            resize++;

            var buk = new byte[resize];
            CopyTo(_buk, buk, wdx);
            _buk = buk;
            _cap = resize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyTo(byte[] source, byte[] dest, int l) =>
            CopyTo(ref Ref(source), ref Ref(dest), l);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyTo(byte[] source, byte[] dest, int destIndex, int l) =>
            CopyTo(ref Ref(source), ref Ref(dest, destIndex), l);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] CopyNew(ref byte source, int sourceIndex, int l)
        {
            var b = new byte[l];
            CopyTo(ref AddB(ref source, sourceIndex), ref Ref(b), l);
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyTo(ref byte source, ref byte dest, int l)
        {
            ref var src = ref source;
            ref var dst = ref dest;
            if (l > 2)
                if (l > 4)
                    if (l > 8)
                        if (l > 2048) AsSpan(ref src, l).CopyTo(AsSpan(ref dst, l));
                        else
                        {
                            ref var srcend = ref AddB(ref src, l - 8);
                            ref var dstend = ref AddB(ref dst, l - 8);
                            do
                            {
                                AsT<ulong>(ref dst) = AsT<ulong>(ref src);
                                dst = ref AddB(ref dst, 8);
                                src = ref AddB(ref src, 8);
                            }
                            while (IsAddressLessThan(ref src, ref srcend));
                            AsT<ulong>(ref dstend) = AsT<ulong>(ref srcend);
                        }
                    else
                    {
                        AsT<uint>(ref dst) = AsT<uint>(ref src);
                        AsT<uint>(ref dst, l - 4) = AsT<uint>(ref src, l - 4);
                    }
                else
                {
                    AsT<ushort>(ref dst) = AsT<ushort>(ref src);
                    AsT<ushort>(ref dst, l - 2) = AsT<ushort>(ref src, l - 2);
                }
            else
            {
                dst = src;
                AddB(ref dst, l - 1) = AddB(ref src, l - 1);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte Ref(byte[] array) => ref GetArrayDataReference(array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte Ref(byte[] array, int byteOffset) => ref AddByteOffset(ref GetArrayDataReference(array), byteOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte AddB(ref byte ptr, nint byteOffset) => ref AddByteOffset(ref ptr, byteOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsT<T>(ref byte ptr) => ref As<byte, T>(ref ptr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsT<T>(ref byte ptr, nint byteOffset) => ref As<byte, T>(ref AddByteOffset(ref ptr, byteOffset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> AsSpan(ref byte ptr, int count) => CreateSpan(ref ptr, count);
    }
}