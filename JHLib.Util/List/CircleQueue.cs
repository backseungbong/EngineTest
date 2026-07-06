using JHLib.Util.Helper;
using System.Runtime.CompilerServices;

namespace JHLib.Util.List
{
    /// <summary>
    /// Enqueue 되는 쪽의 쓰레드와 Dequeue 되는 쪽의 쓰레드가 다른 상황에서 LockFree로 사용할 수 있는 서클큐 자료구조 <para/>
    /// 잠금을 하지 않고도 두 쓰레드간 빠르고 가벼운 사용이 가능하다 <para/>
    /// 다만, 각 쓰레드는 아래와 같이 함수들을 격리하여 사용해야 한다<para/>
    /// 생산자 쓰레드(Producer Thread) = Enqueue(), Clear() <para/>
    /// 소비자 쓰레드(Consumer Thread) = Dequeue(), Clear() <para/>
    /// Dequeue함수에서 추가로 out cleared를 반환하는 함수는, Queue가 클리어가 됬는지 추가로 확인가능하며 <para/>
    /// cleared가 true일 경우에는, out data는 default, 반환 결과는 true이다  <para/>
    /// </summary>
    public class CircleQueue<T>
    {
        private const int MIN_CAPACITY = 8;
        private class QueueIndex
        {
            private const int CLEARED = unchecked((int)0b_1000_0000_0000_0000_0000_0000_0000_0000);
            private volatile int _enq;
            private int _deq;
            public bool IsExist => (_enq & int.MaxValue) != (_deq & int.MaxValue);
            public QueueIndex() { _enq = CLEARED; _deq = CLEARED; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Enqueue(T[] b, int m, T d)
            {
                var i = _enq & m;
                b[i] = d;
                _enq = i + 1 & m;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public bool Dequeue(T[] b, int m, out T d)
            {
                var deq = _deq & m;
                if (deq != (_enq & m))
                {
                    _deq = deq + 1 & m;
                    d = b[deq]; b[deq] = default;
                    return true;
                }
                d = default;
                return false;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public bool Dequeue(T[] b, int m, out T d, out bool c)
            {
                var deq = _deq;
                if (deq >= 0)
                {
                    if (deq != (_enq & m))
                    {
                        _deq = deq + 1 & m;
                        d = b[deq]; b[deq] = default;
                        c = false;
                        return true;
                    }
                    d = default;
                    c = false;
                    return false;
                }
                _deq = 0;
                d = default;
                c = true;
                return true;
            }
        }

        private readonly T[] _buk;
        private readonly int _msk;
        private QueueIndex _qix;
        public bool IsExist => _qix.IsExist;
        public CircleQueue(int cap = MIN_CAPACITY)
        {
            cap = MathHelper.RoundUpToPow2(MIN_CAPACITY, cap);

            _buk = new T[cap];
            _msk = cap - 1;
            _qix = new();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _qix = new();

        // ====================== Only Producer Thread Use ======================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T data) =>
            _qix.Enqueue(_buk, _msk, data);


        // ====================== Only Consumer Thread Use ======================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Dequeue(out T data) =>
            _qix.Dequeue(_buk, _msk, out data);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Dequeue(out T data, out bool cleared) =>
            _qix.Dequeue(_buk, _msk, out data, out cleared);
    }
}