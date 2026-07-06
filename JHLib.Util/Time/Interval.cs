using System.Runtime.CompilerServices;

namespace JHLib.Util.Time
{
    public static class Interval
    {
        private static readonly Timer _interval;
        private static readonly long _elapsed0;
        private static long _elapsed64;
        private static uint _elapsed32;
        private static uint _lastTimeSec10;
        private static uint _lastTimeMin01;
        private static uint _lastTimeMin10;

        static Interval()
        {
            _elapsed0 = Environment.TickCount64;
            _elapsed64 = 0;
            _elapsed32 = 0;
            _lastTimeSec10 = 10;
            _lastTimeMin01 = 60;
            _lastTimeMin10 = 600;
            _interval = new(IntervalLoop, null, 50, 50);
        }

        public static event Action OnIntervalSec01;
        public static event Action OnIntervalSec10;
        public static event Action OnIntervalMin01;
        public static event Action OnIntervalMin10;

        /// <summary> 프로그램이 시작된 이후 경과된 시간(밀리초)을 가져온다 </summary>
        public static long Elapsed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _elapsed64;
        }

        /// <summary> 프로그램이 시작된 이후 경과된 시간(초)을 가져온다 </summary>
        public static uint Elapsed32
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _elapsed32;
        }

        /// <summary> 시스템 타임에 주어진 인터벌을 더한 값을 반환한다 (최대값은 uint.MaxValue 이하로 제한된다) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Timer32(uint interval)
        {
            var now = _elapsed32;
            var sum = now + interval;
            return sum < now ? uint.MaxValue : sum;
        }

        /// <summary> 타임스탬프가 인터벌을 경과 했는지 체크한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckTimestamp(uint timestamp, uint interval) =>
            _elapsed32 - timestamp > interval;

        /// <summary> 
        /// 타임스탬프가 인터벌 경과 시 현재 시간으로 갱신한다 <para/>
        /// 타임스탬프가 경과되지 않으면 false를 반환한다 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool UpdateTimestamp(ref uint timestamp, uint interval)
        {
            var result = false;
            var stamp = timestamp;
            var now = _elapsed32;
            if (now - stamp > interval) { timestamp = now; result = true; }
            return result;
        }

        /// <summary> 
        /// 타임스탬프가 인터벌 경과 시 현재 시간으로 원자적(atomic) 갱신시도한다 <para/>
        /// 타임스탬프가 경과되지 않거나, 원자적 갱신에 실패시 false를 반환한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryUpdateTimestamp(ref uint timestamp, uint interval)
        {
            var result = false;
            var stamp = timestamp;
            var now = _elapsed32;
            if (now - stamp > interval) { result = Interlocked.CompareExchange(ref timestamp, now, stamp) == stamp; }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IntervalLoop(object _)
        {
            var elapsed64 = Environment.TickCount64 - _elapsed0;
            _elapsed64 = elapsed64;

            var elapsed32 = (uint)(elapsed64 / 1000);
            if (elapsed32 != _elapsed32)
            {
                _elapsed32 = elapsed32;
                IntervalExcute(elapsed32);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void IntervalExcute(uint sec)
        {
            if (_lastTimeSec10 <= sec)
            {
                _lastTimeSec10 = sec + 10;
                if (_lastTimeMin01 <= sec)
                {
                    _lastTimeMin01 = sec + 60;
                    if (_lastTimeMin10 <= sec)
                    {
                        _lastTimeMin10 = sec + 600;

                        try { OnIntervalMin10?.Invoke(); }
                        catch (Exception) { }
                    }
                    try { OnIntervalMin01?.Invoke(); }
                    catch (Exception) { }
                }
                try { OnIntervalSec10?.Invoke(); }
                catch (Exception) { }
            }
            try { OnIntervalSec01?.Invoke(); }
            catch (Exception) { }
        }
    }
}