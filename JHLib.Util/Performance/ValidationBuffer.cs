using MemoryPack;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Performance
{
    /// <summary>
    /// MemoryPack 기반 고성능 직렬화 버퍼<br/>
    /// xxHash 체크섬 검증을 포함하며, ThreadStatic을 활용하여 스레드별 버퍼를 재사용한다<br/>
    /// 64바이트 정렬된 고정(pinned) 메모리 위에서 동작하여 캐시 라인 효율을 극대화한다
    /// </summary>
    [SkipLocalsInit]
    public sealed class ValidationBuffer : IBufferWriter<byte>
    {
        [ThreadStatic]
        private static ValidationBuffer _current;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ValidationBuffer Init() => _current = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ValidationBuffer Current() => _current ?? Init();


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ValidationBuffer Serialize<T>(int bodyType, scoped in T bodyData)
        {
            var current = Current();
            current.Write(bodyType, in bodyData);
            return current;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ValidationBuffer Serialize<THeader, T>(
            THeader bodyHeader, int bodyType, scoped in T bodyData) where THeader : unmanaged
        {
            var current = Current();
            current.Write(in bodyHeader, bodyType, in bodyData);
            return current;
        }

        /// <summary>
        /// <see cref="Serialize{T}"/>의 인라인 버전<br/> 
        /// 호출 지점에 코드가 삽입되어 메서드 호출 오버헤드를 제거한다<br/>
        /// T 타입별로 코드가 생성되어 호출 지점의 코드 크기가 증가하므로, 고성능이 요구되는 핫 패스에서만 사용을 권장한다<br/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValidationBuffer SerializeInline<T>(int bodyType, scoped in T bodyData)
        {
            var current = Current();
            current.Write(bodyType, in bodyData);
            return current;
        }
        /// <summary>
        /// <see cref="Serialize{THeader, T}"/>의 인라인 버전<br/> 
        /// 호출 지점에 코드가 삽입되어 메서드 호출 오버헤드를 제거한다<br/>
        /// T 타입별로 코드가 생성되어 호출 지점의 코드 크기가 증가하므로, 고성능이 요구되는 핫 패스에서만 사용을 권장한다<br/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValidationBuffer SerializeInline<THeader, T>(
            THeader bodyHeader, int bodyType, scoped in T bodyData) where THeader : unmanaged
        {
            var current = Current();
            current.Write(in bodyHeader, bodyType, in bodyData);
            return current;
        }

        private const int DefaultBufferSize = 4096;
        private const int DefaultHintSize = 512;
        private const int DefaultAlignment = 64;

        private byte[] _buffer;
        private nint _pointer;
        private int _alignOffset;
        private int _capacity;
        private int _margincap;
        private int _idx;

        private unsafe ref byte Byte0 =>
            ref Unsafe.AsRef<byte>((void*)_pointer);
        private unsafe ref ValidationHeader Header0 =>
            ref Unsafe.AsRef<ValidationHeader>((void*)_pointer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe Span<byte> AsSpan(int idx) =>
            MemoryMarshal.CreateSpan(ref *(byte*)(_pointer + (uint)idx), _capacity - idx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Memory<byte> AsMemory(int idx)
        {
            var buffer = _buffer;
            var offset = _alignOffset + idx;
            return new(buffer, offset, buffer.Length - offset);
        }

        /// <summary>현재까지 기록된 데이터</summary>
        public ReadOnlySpan<byte> WrittenSpan => MemoryMarshal.CreateReadOnlySpan(ref Byte0, _idx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValidationBuffer()
        {
            var pb = new PinnedBuffer(DefaultBufferSize, DefaultAlignment);
            _buffer = pb.Buffer;
            _pointer = pb.Pointer;
            _alignOffset = pb.AlignOffset;
            _capacity = DefaultBufferSize;
            _margincap = DefaultBufferSize - DefaultHintSize;
            _idx = ValidationHeader.SIZE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count) => _idx += count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            var idx = _idx;
            if (idx + sizeHint > _margincap) return ResizeMemory(idx + sizeHint);
            return AsMemory(idx);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            var idx = _idx;
            if (idx + sizeHint > _margincap) return ResizeSpan(idx + sizeHint);
            return AsSpan(idx);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in IBufferWriter<byte> bufferWriter)
        {
            var len = _idx;
            ref var dst0 = ref MemoryMarshal.GetReference(bufferWriter.GetSpan(len));
            ValidationHeader.FastCopy16(ref dst0, ref Byte0, len);
            bufferWriter.Advance(len);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write<T>(int bodyType, scoped in T bodyData)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                WriteWithRef(bodyType, in bodyData);
            else
                WriteNoRef(bodyType, in bodyData);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteWithRef<T>(int bodyType, scoped in T bodyData)
        {
            ReadySize(Unsafe.SizeOf<ValidationHeader>());
            MemoryPackSerializer.Serialize(this, bodyData);

            ref var header0 = ref Header0;
            ref var body0 = ref header0.Body0;
            header0.Seal(ref body0, bodyType, _idx - Unsafe.SizeOf<ValidationHeader>());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteNoRef<T>(int bodyType, scoped in T bodyData)
        {
            ReadySize(Unsafe.SizeOf<ValidationHeader>() + Unsafe.SizeOf<T>());

            ref var header0 = ref Header0;
            ref var body0 = ref header0.Body0;
            Unsafe.WriteUnaligned(ref body0, bodyData);
            header0.Seal(ref body0, bodyType, Unsafe.SizeOf<T>());
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write<THeader, T>(
            scoped in THeader bodyHeader, int bodyType, scoped in T bodyData) where THeader : unmanaged
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                WriteWithRef(in bodyHeader, bodyType, in bodyData);
            else
                WriteNoRef(in bodyHeader, bodyType, in bodyData);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteWithRef<THeader, T>(
            scoped in THeader bodyHeader, int bodyType, scoped in T bodyData) where THeader : unmanaged
        {
            ReadySize(Unsafe.SizeOf<ValidationHeader>() + Unsafe.SizeOf<THeader>());
            MemoryPackSerializer.Serialize(this, bodyData);

            ref var header0 = ref Header0;
            ref var body0 = ref header0.Body0;
            Unsafe.WriteUnaligned(ref body0, bodyHeader);
            header0.Seal(ref body0, bodyType, _idx - Unsafe.SizeOf<ValidationHeader>());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteNoRef<THeader, T>(
            scoped in THeader bodyHeader, int bodyType, scoped in T bodyData) where THeader : unmanaged
        {
            ReadySize(Unsafe.SizeOf<ValidationHeader>() + Unsafe.SizeOf<THeader>() + Unsafe.SizeOf<T>());

            ref var header0 = ref Header0;
            ref var body0 = ref header0.Body0;
            Unsafe.WriteUnaligned(ref body0, bodyHeader);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref body0, Unsafe.SizeOf<THeader>()), bodyData);
            header0.Seal(ref body0, bodyType, Unsafe.SizeOf<THeader>() + Unsafe.SizeOf<T>());
        }

        /// <summary> 쓰기 전 버퍼 용량을 확보하고 커서를 초기화 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadySize(int size)
        {
            // Dead Code Elimination 유도을 위해 두 단계로 확인
            // 1. 상수 비교 (JIT 가드): len이 작으면 이 블록 자체가 삭제됨
            // 2. 변수 비교 (런타임 체크): 삭제 안 된 경우만 실제 용량 확인
            if (size > DefaultBufferSize && size > _capacity)
                Resize(size);
            _idx = size;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Memory<byte> ResizeMemory(int need) { Resize(need); return AsMemory(_idx); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Span<byte> ResizeSpan(int need) { Resize(need); return AsSpan(_idx); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(int need)
        {
            var cap = _capacity * 2;
            if (cap < need)
                cap = (int)BitOperations.RoundUpToPowerOf2((uint)need);

            if ((uint)cap > Array.MaxLength)
                throw new InvalidOperationException("Requested buffer size exceeds maximum array length.");

            var pb = new PinnedBuffer(cap, DefaultAlignment);
            Unsafe.CopyBlock(ref pb.Byte0, ref Byte0, (uint)_idx);

            _buffer = pb.Buffer;
            _pointer = pb.Pointer;
            _alignOffset = pb.AlignOffset;
            _capacity = cap;
            _margincap = cap - DefaultHintSize;
        }
    }
}