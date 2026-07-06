using JHLib.Util.Helper;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Performance
{
    /// <summary>
    /// 풀(Pool)로부터 대여된 버퍼 리소스 기반 구조체<br/>
    /// 유효 데이터 뷰 및 슬라이서 제공<br/>
    /// 사용후 반드시 Return() 호출로 버퍼를 반환해야 함
    /// </summary>
    public readonly struct BufferPoolSpan
    {
        private readonly byte[] _buffer;
        private readonly int _length;

        /// <summary> 버퍼가 null인지 여부 </summary>
        public readonly bool IsNull => _buffer == null;

        /// <summary> 유효 데이터 길이 </summary>
        public readonly int Length => _length;

        /// <summary> 유효 데이터 시작 포인터 </summary>
        public ref byte Buffer0 => ref UnsafeEx.Arr0(_buffer);

        /// <summary> 유효 데이터 전체 뷰 </summary>
        public readonly ReadOnlySpan<byte> ReadSpan => UnsafeEx.CreateReadSpan(_buffer, _length);


        /// <summary> 시작점 ~ 지정한 길이만큼의 영역을 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> Limit(int length, bool checkOutOfRange = false) =>
            UnsafeEx.Limit(_buffer, length, checkOutOfRange);

        /// <summary> 오프셋 ~ 끝까지의 영역을 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> Slice(int offset, bool checkOutOfRange = false) =>
            UnsafeEx.Slice(_buffer, offset, _length - offset, checkOutOfRange);

        /// <summary> 오프셋 ~ 지정한 길이만큼의 영역을 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> Slice(int offset, int length, bool checkOutOfRange = false) =>
            UnsafeEx.Slice(_buffer, offset, length, checkOutOfRange);


        /// <summary> 풀로 버퍼 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return() => BufferPool.Return(_buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal BufferPoolSpan(byte[] buffer, int length) { _buffer = buffer; _length = length; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<byte>(BufferPoolSpan span) => span.ReadSpan;
    }
}