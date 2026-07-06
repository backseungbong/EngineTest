using System.Runtime.CompilerServices;

namespace JHLib.Util.ThreadSafe
{
    /// <summary> 
    /// 함수실행을 특정 시간 간격으로 조절한다 <para/>
    /// 함수실행을 0초부터 1초 간격으로 총 8회 발생하는 상황일때 <para/>
    /// 조절시간이 5초라면 0초, 5초, 10초에 총 3회 발생한다 <para/>
    /// Interlocked을 통해 내부 상태를 관리하므로, 멀티쓰레드 환경에서 사용 가능하다
    /// </summary>
    /// <remarks> 함수실행을 특정 시간 간격으로 조절한다 </remarks>
    /// <param name="delay">실행 조절 간격(ms, 1000 = 1초)</param>
    /// <param name="action">실행 함수</param>
    /// <param name="condition"> 실행조건함수, 함수실행전 실행 유무에 대해 체크한다. null이라면 실행조건 없이 발동한다</param>
    public class ThrottlingAction
    {
        private const int MIN_DELAY = 100;
        private const int MAX_DELAY = 3600000;

        private readonly int _delay;
        private readonly Func<ValueTask> _action;
        private readonly Func<bool> _condition;
        private int _state;

        /// <summary> 동기 Action용 </summary>
        public ThrottlingAction(int delay, Action action, Func<bool> condition = null)
            : this(delay, () => { action(); return ValueTask.CompletedTask; }, condition) { }

        /// <summary> 비동기 Action용 </summary>
        public ThrottlingAction(int delay, Func<ValueTask> action, Func<bool> condition = null)
        {
            _delay = Math.Clamp(delay, MIN_DELAY, MAX_DELAY);
            _action = action;
            _condition = condition;
        }

        /// <summary> 간격실행 함수를 실행한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Action()
        {
            if (Interlocked.Exchange(ref _state, 1) == 0)
                Worker();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async void Worker()
        {
            try
            {
                do
                {
                    Volatile.Write(ref _state, 2);

                    if (_condition == null || _condition())
                    {
                        await _action().ConfigureAwait(false);
                        await Task.Delay(_delay).ConfigureAwait(false);
                    }
                }
                while (Interlocked.CompareExchange(ref _state, 0, 2) != 2);
            }
            catch (Exception)
            {
                Volatile.Write(ref _state, 0);
            }
        }
    }
}