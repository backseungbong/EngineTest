using System.Runtime.CompilerServices;

namespace JHLib.Util.ThreadSafe
{
    /// <summary> 지연실행되는 Action 함수를 관리한다 </summary>
    public class DelayAction
    {
        private const int MIN_DELAY = 100; // 100ms
        private const int MAX_DELAY = 10000; // 10 sec

        private readonly int _delay;
        private readonly Action _action;
        private int _time;

        /// <summary> 지연실행되는 Action 함수를 초기화한다 </summary>
        /// <param name="delay">지연 시간(ms, 1000 = 1초)</param>
        /// <param name="action">실행 함수</param>
        public DelayAction(int delay, Action action)
        {
            int d;
            if (delay > MIN_DELAY)
                if (delay < MAX_DELAY) d = delay;
                else d = MAX_DELAY;
            else d = MIN_DELAY;

            _delay = d;
            _action = action;
        }

        /// <summary> 지연실행 함수를 취소한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop() => Volatile.Write(ref _time, 0);

        /// <summary> 지연실행 함수를 실행한다, 이미 진행 상태일경우 기존 지연시간을 연장한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Action()
        {
            if (Interlocked.Exchange(ref _time, Environment.TickCount | 1) == 0)
                Worker();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async void Worker()
        {
            int t;
            do
            {
                if ((t = Volatile.Read(ref _time)) != 0)
                {
                    var elap = (Environment.TickCount | 1) - t;
                    if ((uint)elap < (uint)_delay)
                        await Task.Delay(_delay - elap).ConfigureAwait(false);
                }
            }
            while (Interlocked.CompareExchange(ref _time, 0, t) != t);

            if (t != 0)
                _action();
        }
    }
}