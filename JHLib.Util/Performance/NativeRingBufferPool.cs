using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Performance
{
    // 싱글 생산자-소비자(SPSC) 패턴의 고성능 Native 버퍼 풀
    public unsafe sealed class NativeRingBufferPool : IDisposable
    {
        [StructLayout(LayoutKind.Explicit, Size = 768)]
        public struct RingBufferState
        {
            // 'False Sharing' 문제를 방지하기 위해 메모리 앞,뒤,중간에 패딩처리
            // 생산자측 캐시 라인
            [FieldOffset(256)] public uint Head;

            // 소비자측 캐시 라인(아래 다른 필드들은 소비자측에서 주로 접근하므로 같이 둠)
            [FieldOffset(512)] public volatile uint Tail;
            [FieldOffset(520)] public uint Mask;
        }

        private nint _state;
        private uint _capac;
        private uint _cachedTail;
        private Queue<nint> _oldlist;
        private bool _checkOldList;

        public void Dispose()
        {
            var state = Interlocked.Exchange(ref _state, 0);
            if (state != 0)
            {
                var oldlist = _oldlist;
                if (oldlist != null)
                {
                    while (oldlist.TryDequeue(out var oldstate))
                        NativeMemory.AlignedFree((void*)oldstate);
                }
                NativeMemory.AlignedFree((void*)state);
                GC.SuppressFinalize(this);
            }
        }

        ~NativeRingBufferPool() => Dispose();
        public NativeRingBufferPool(uint capacity = 4096) => Realloc(BitOperations.RoundUpToPowerOf2(capacity));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private RingBufferState* Realloc(uint capac)
        {
            var alloc = (uint)Unsafe.SizeOf<RingBufferState>() + capac;
            var state = (RingBufferState*)NativeMemory.AlignedAlloc(alloc, 256);
            state->Head = 0;
            state->Tail = 0;
            state->Mask = capac - 1;

            _state = (nint)state;
            _capac = capac;
            _cachedTail = 0;
            return state;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RingBufferItem Rent(uint size)
        {
            var capac = _capac;
            var state = (RingBufferState*)_state;
            var head = state->Head;

            var len = (size + 15) & ~15u;
            var rem = capac - (head & (capac - 1));
            if (rem < len) { head += rem; }

            var nextHead = head + len;
            if (nextHead - _cachedTail > capac)
            {
                var tail = state->Tail;
                if (nextHead - tail > capac)
                    return RentInternal(len);

                _cachedTail = tail;

                if (_checkOldList)
                    CheckOldList();
            }
            return new((nint)state, head, nextHead);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private RingBufferItem RentInternal(uint len)
        {
            var oldstate = (RingBufferState*)_state;
            if (oldstate->Head == oldstate->Tail)
            {
                NativeMemory.AlignedFree(oldstate);
            }
            else
            {
                (_oldlist ??= new(4)).Enqueue((nint)oldstate);
                CheckOldList();
            }

            var capac = _capac * 2;
            if (capac < len)
                capac = BitOperations.RoundUpToPowerOf2(len);

            return new((nint)Realloc(capac), 0, len);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CheckOldList()
        {
            var oldlist = _oldlist;
            while (oldlist.TryPeek(out var peek))
            {
                var oldstate = (RingBufferState*)peek;
                if (oldstate->Head != oldstate->Tail)
                {
                    _checkOldList = true;
                    return;
                }
                NativeMemory.AlignedFree(oldstate);
                oldlist.Dequeue();
            }
            _checkOldList = false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe readonly record struct RingBufferItem
    {
        private readonly nint _state;
        private readonly uint _head;
        private readonly uint _next;

        /// <summary> 버퍼 사이즈 </summary>
        public readonly uint Length => _next - _head;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RingBufferItem(nint state, uint head, uint next)
        {
            ((NativeRingBufferPool.RingBufferState*)state)->Head = next;
            _state = state;
            _head = head;
            _next = next;
        }

        /// <summary> 버퍼 참조 주소 <br/>
        /// Length가 0인 경우 Memory Access Violation 발생하므로 호출전 Length체크 필수 </summary>
        public ref byte Buffer0
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref *(byte*)Address0();
        }

        /// <summary> 버퍼 포인터 주소 <br/>
        /// Length가 0인 경우 Memory Access Violation 발생하므로 호출전 Length체크 필수 </summary>
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nint Address0()
        {
            var pos = _head & ((NativeRingBufferPool.RingBufferState*)_state)->Mask;
            return _state + Unsafe.SizeOf<NativeRingBufferPool.RingBufferState>() + (nint)pos;
        }

        /// <summary> 버퍼 반환 </summary>
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return()
        {
            var state = (NativeRingBufferPool.RingBufferState*)_state;
            if (state != null) state->Tail = _next;
        }
    }
}