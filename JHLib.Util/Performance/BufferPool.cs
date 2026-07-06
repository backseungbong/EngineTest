using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Performance
{
    /// <summary>
    /// <see cref="ArrayPool{T}"/>보다 낮은 오버헤드로 작동하도록 설계된 스레드 로컬(ThreadStatic) 기반 경량 풀<br/>
    /// (스레드별로 최대 4개의 버퍼를 캐시)<br/>
    /// </summary>
    /// <remarks>
    /// <strong>[권장 사용 시나리오]</strong><br/>
    /// <list type="bullet">
    ///     <item>작은 크기(4kb ~ 512kb)의 빈번한 할당/해제 (512kb 이상도 문제 없으나 내부적으로 <see cref="ArrayPool{T}"/>값을 그대로 반환함) </item>
    ///     <item>동기(Synchronous) 방식의 짧은 작업 범위</item>
    /// </list>
    /// <br/>
    /// 다음 상황에서는 권장하지 않음 (ArrayPool 보다 더 많은 부하가 생성됨)
    /// <list type="bullet">
    ///     <item>쓰레드 문맥이 바뀔수 있는 상황이거나, 비동기로 던져야 하는경우</item>
    ///     <item>전역변수에 정의해 사용해야하는경우 </item>
    /// </list>
    /// </remarks>
    public static class BufferPool
    {
        private const int MinSize = 1024 * 4; // 4kb
        private const int MaxSize = 1024 * 512; // 512kb

        [ThreadStatic]
        private static BufferCache _cache;

        /// <summary> 최소 사이즈를 만족하는 버퍼 대여 </summary>
        /// <param name="minsize">대여가 필요한 최소 바이트 크기</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferPoolItem Rent(int minsize) => _cache.Rent(minsize);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Return(byte[] buffer) => _cache.Return(buffer);


        [StructLayout(LayoutKind.Sequential, Pack = 64)]
        private struct BufferCache
        {
            [StructLayout(LayoutKind.Sequential, Size = 16)]
            public struct BufferSlot { public byte[] Buffer; public int Size; }

            [InlineArray(4)]
            public struct BufferSlots
            {
                private BufferSlot _e0;

                // 인덱스 대신 바이트 오프셋으로 계산 오버헤드 제거
                internal ref BufferSlot this[uint byteOffset] =>
                    ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in _e0), byteOffset);
            }

            private BufferSlots _slot;
            private uint _idx;
            private int _size;
            private byte[] _buffer;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public BufferPoolItem Rent(int minsize)
            {
                if ((uint)_size > (uint)(minsize - 1))
                {
                    _size = 0;
                    return new(_buffer);
                }
                return new(RentSlow(minsize));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Return(byte[] buffer)
            {
                if (_buffer == buffer)
                    _size = buffer.Length;
                else
                    ReturnSlow(buffer);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private byte[] RentSlow(int need)
            {
                if (need > MaxSize) return RentPool(need);
                if (need < MinSize) need = MinSize;

                var i = _idx;
                ref var d = ref _slot[i];
                d.Size = _size; _size = 0;

                if (d.Buffer != null)
                {
                    i = i + 16 & 63; _idx = i;
                    d = ref _slot[i];
                    if (d.Size >= need) { goto EX; }
                    if (d.Size != 0) ReturnPool(d.Buffer);
                }

                d.Buffer = RentPool(need);
            EX: return _buffer = d.Buffer;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void ReturnSlow(byte[] buffer)
            {
                if (buffer == null) { return; }
                if (buffer.Length <= MaxSize)
                {
                    var i = _idx;
                    _slot[i].Size = _size; _size = buffer.Length;
                    _buffer = buffer;

                    i = i - 16 & 63;
                    if (_slot[i].Buffer != buffer)
                    {
                        i = i - 16 & 63;
                        if (_slot[i].Buffer != buffer)
                        {
                            i = i - 16 & 63;
                            if (_slot[i].Buffer != buffer)
                            {
                                i = i + 32 & 63;
                                if (_slot[i].Size != 0) ReturnPool(_slot[i].Buffer);
                                _slot[i].Buffer = buffer;
                            }
                        }
                    }
                    _slot[i].Size = buffer.Length;
                    _idx = i;
                }
                else
                {
                    ReturnPool(buffer);
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static byte[] RentPool(int need) =>
                ArrayPool<byte>.Shared.Rent(need);

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ReturnPool(byte[] buffer) =>
                ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}