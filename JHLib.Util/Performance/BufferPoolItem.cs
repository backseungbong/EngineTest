using JHLib.Util.Helper;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Performance
{
    /// <summary>
    /// 풀(Pool)로부터 대여된 버퍼 리소스 기반 구조체<br/>
    /// 쓰기 가능한 뷰 및 슬라이서 제공<br/>
    /// 사용후 반드시 Return() 호출로 버퍼를 반환해야 함
    /// </summary>
    public readonly struct BufferPoolItem
    {
        private readonly byte[] _buffer;

        /// <summary> 버퍼가 null인지 여부 </summary>
        public readonly bool IsNull => _buffer == null;

        /// <summary> 대여 버퍼 크기 </summary>
        public readonly int Capacity => _buffer.Length;

        /// <summary> 대여 버퍼 시작 포인터 </summary>
        public ref byte Buffer0 => ref UnsafeEx.Arr0(_buffer);

        /// <summary> 대여 버퍼 전체 뷰 </summary>
        public readonly Span<byte> Span => UnsafeEx.CreateSpan(_buffer);


        /// <summary> 시작점 ~ 지정한 길이만큼의 영역을 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> Limit(int length, bool checkOutOfRange = false) =>
            UnsafeEx.Limit(_buffer, length, checkOutOfRange);

        /// <summary> 오프셋 ~ 끝까지의 영역을 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> Slice(int offset, bool checkOutOfRange = false) =>
            UnsafeEx.Slice(_buffer, offset, checkOutOfRange);

        /// <summary> 오프셋 ~ 지정한 길이만큼의 영역을 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> Slice(int offset, int length, bool checkOutOfRange = false) =>
            UnsafeEx.Slice(_buffer, offset, length, checkOutOfRange);


        /// <summary> 풀로 버퍼 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return() => BufferPool.Return(_buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal BufferPoolItem(byte[] buffer) => _buffer = buffer;

        /// <summary> 유효 데이터 영역을 표현하는 BufferPoolSpan로 변환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferPoolSpan ToPoolSpan(int length) => new(_buffer, length);
    }
}