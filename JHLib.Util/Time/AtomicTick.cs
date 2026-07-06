using System.Runtime.CompilerServices;

namespace JHLib.Util.Time
{
    /// <summary>
    /// 32bit 시스템에서 64bit 데이타를 참조할때 원자적이지 않으므로 <para/>
    /// 다중쓰레드 접근이 예상되는 곳에서 원자적으로 접근하기 위해 4개의 값과 이를 참조하는 인덱스를 사용한다     
    /// </summary>
    public class AtomicTick
    {
        private unsafe struct Values
        {
            private fixed long _val[4];
            public long this[uint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _val[index];
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set => _val[index] = value;
            }
        }

        private Values _values;
        private volatile uint _index;
        public long Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values[_index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var i = _index + 1 & 3;
                _values[i] = value;
                _index = i;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AtomicTick(long tick)
        {
            _index = 0;
            _values[0] = tick;
        }
    }
}