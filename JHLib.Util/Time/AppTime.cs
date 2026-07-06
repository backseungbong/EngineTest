using JHLib.Util.FileIO;
using JHLib.Util.Performance;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Time
{
    public static class AppTime
    {
        private const int BufferCount = 8; // 2의 제곱 필수

        private static readonly string AppFolder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create), typeof(AppTime).Namespace);

        private static readonly string LastAppTimePath = Path.Combine(AppFolder, "LastAppTime");

        private readonly static DateTime MinDateTime = new(2025, 1, 1); // AppTime이 지원하는 최소 시간 (2025년 1월 1일)
        private readonly static DateTime MaxDateTime = new(2100, 1, 1); // AppTime이 지원하는 최대 시간 (2100년 1월 1일)
        private readonly static long MinTicks = MinDateTime.Ticks;
        private readonly static long MaxTicks = MaxDateTime.Ticks;
        private readonly static ulong TickLimit = (ulong)(MaxTicks - MinTicks);

        private static readonly bool Is10MHz =
            Stopwatch.Frequency == TimeSpan.TicksPerSecond;

        private static readonly double TickFrequency =
            (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToDateTimeTicks(long counter) => Is10MHz ? counter : ToDateTimeTicksInternal(counter);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long ToDateTimeTicksInternal(long counter) => Sse2.X64.IsSupported ?
            Sse2.X64.ConvertToInt64(Vector128.CreateScalarUnsafe(counter * TickFrequency)) :
            (long)(counter * TickFrequency);


        #region static
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private struct TimeInfo
        {
            private long _baseUtcTicks;
            private long _baseLocTicks;
            private long _lastTickCount;
            private long _lastTimestamp;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(DateTime utc)
            {
                _baseUtcTicks = utc.Ticks;
                _baseLocTicks = utc.ToLocalTime().Ticks;
                _lastTickCount = Environment.TickCount64;
                _lastTimestamp = Stopwatch.GetTimestamp();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public long UtcTicks() =>
                Elapsed(Volatile.Read(ref _baseUtcTicks), Volatile.Read(ref _lastTickCount));

            [MethodImpl(MethodImplOptions.NoInlining)]
            public long LocTicks() =>
                Elapsed(Volatile.Read(ref _baseLocTicks), Volatile.Read(ref _lastTickCount));

            [MethodImpl(MethodImplOptions.NoInlining)]
            public long UtcTicksPrecise() =>
                ElapsedPrecise(Volatile.Read(ref _baseUtcTicks), Volatile.Read(ref _lastTimestamp));

            [MethodImpl(MethodImplOptions.NoInlining)]
            public long LocTicksPrecise() =>
                ElapsedPrecise(Volatile.Read(ref _baseLocTicks), Volatile.Read(ref _lastTimestamp));


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static long Elapsed(long baseTicks, long tickCount)
            {
                var ms = Environment.TickCount64 - tickCount;
                return baseTicks + ms * TimeSpan.TicksPerMillisecond;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static long ElapsedPrecise(long baseTicks, long lastTimestamp)
            {
                var counter = Stopwatch.GetTimestamp() - lastTimestamp;
                return baseTicks + ToDateTimeTicks(counter);
            }
        }

        [InlineArray(BufferCount)]
        private struct Times
        {
            private TimeInfo _e0;
            public ref TimeInfo this[uint i]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref Unsafe.Add(ref Unsafe.AsRef(ref _e0), i & (BufferCount - 1));
            }
        }

        private readonly record struct LastTimeInfo(long SysTicks, long SetTicks);
        private static DateTime AsTime(long ticks) => Unsafe.As<long, DateTime>(ref ticks);
        private static DateTime LoadLastTime()
        {
            const int MaxAttempts = 3;
            const int Interval = 50;

            if (File.Exists(LastAppTimePath))
            {
                // 여러 프로세서가 AppTime클래스를 통해 비동기적 읽기/쓰기로 오류가 발생할 수 있으므로
                // 파일 존재가 확인되면 지정된만큼 재시도해본다
                for (var i = 0; i < MaxAttempts; i++)
                {
                    var content = FilePrimitive.Read(LastAppTimePath);
                    if (ValidationHeader.TryReadBody(content, out LastTimeInfo time) &&
                        (ulong)(time.SysTicks - MinTicks) <= TickLimit &&
                        (ulong)(time.SetTicks - MinTicks) <= TickLimit)
                    {
                        // 설정시간이 앞서 있었다면 추가, 느렸다면 빼줘야함
                        var nowTicks = DateTime.UtcNow.Ticks;
                        var setTicks = nowTicks + (time.SetTicks - time.SysTicks);

                        // 시스템 시간이 정상 범위 내에서 흐르고 있는지 검증
                        // - 현재 시스템 시간이 저장 시점보다는 미래여야 함
                        // - 보정 시간이 저장된 설정 시간보다는 미래여야 함
                        // - 모든 시간들이 지원하는 범위 내에 있어야 함
                        if (nowTicks > time.SysTicks &&
                            setTicks > time.SetTicks &&
                            (ulong)(nowTicks - MinTicks) <= TickLimit &&
                            (ulong)(setTicks - MinTicks) <= TickLimit)
                            return AsTime(setTicks);

                        // 조건 검사해서 실패한경우(배터리 방전등 시간이 비정상), 마지막 셋팅 시간 사용
                        return AsTime(time.SetTicks);
                    }
                    Thread.Sleep(Interval);
                }
            }
            return DateTime.UtcNow;
        }

        private static void SaveLastTime(long sysTicks, long setTicks)
        {
            // 쓰기시 오류는 100% 성공이 필요 없음
            // 실패가 가능하다고 가정하고 예외를 전파하지 않는다
            try
            {
                if (Directory.Exists(AppFolder) == false)
                    Directory.CreateDirectory(AppFolder);

                var timeInfo = new LastTimeInfo(sysTicks, setTicks);
                var buffer = ValidationBuffer.Serialize(0, timeInfo);
                FilePrimitive.WriteAtomic(LastAppTimePath, buffer.WrittenSpan, true);
            }
            catch { }
        }
        #endregion

        private readonly static Lock _lock;
        private static Times _time;
        private static uint _seq;
        private static long _lastOffset;
        private static long _lastSysTicks;


        /// <summary> 현재 Utc 시간을 가져온다 </summary>
        public static long UtcTicks => _time[Volatile.Read(ref _seq)].UtcTicks();

        /// <summary> 현재 Local 시간을 가져온다 </summary>
        public static long LocalTicks => _time[Volatile.Read(ref _seq)].LocTicks();

        /// <summary> 현재 Utc 시간을 가져온다 (고정밀 간격, 연산 비용이 더 큼)
        /// <para>나노초 단위의 정밀도가 필요할 때 사용</para></summary>
        public static long UtcTicksPrecise => _time[Volatile.Read(ref _seq)].UtcTicksPrecise();

        /// <summary> 현재 Local 시간을 가져온다 (고정밀 간격, 연산 비용이 더 큼)
        /// <para>나노초 단위의 정밀도가 필요할 때 사용</para></summary>
        public static long LocalTicksPrecise => _time[Volatile.Read(ref _seq)].LocTicksPrecise();


        /// <summary> 현재 Utc 시간을 DateTime으로 가져온다 </summary>
        public static DateTime Utc => AsTime(UtcTicks);

        /// <summary> 현재 Local 시간을 DateTime으로 가져온다 </summary>
        public static DateTime Local => AsTime(LocalTicks);

        /// <summary> 현재 Utc 시간을 DateTime으로 가져온다 (고정밀 간격, 연산 비용이 더 큼)
        /// <para>나노초 단위의 정밀도가 필요할 때 사용</para></summary>
        public static DateTime UtcPrecise => AsTime(UtcTicksPrecise);

        /// <summary> 현재 Local 시간을 DateTime으로 가져온다 (고정밀 간격, 연산 비용이 더 큼)
        /// <para>나노초 단위의 정밀도가 필요할 때 사용</para></summary>
        public static DateTime LocalPrecise => AsTime(LocalTicksPrecise);

        static AppTime()
        {
            _lock = new Lock();
            _time[0].Set(LoadLastTime());
            _lastOffset = 0;
            _lastSysTicks = 0;
        }

        /// <summary> 현재 시간을 설정한다 (Thread-Safe)
        /// <para>GPS 수신 등 외부 소스로부터 정확한 시간을 받았을 때 호출</para>
        /// </summary>
        /// <param name="utc">수신된 현재의 UTC</param>
        /// <param name="saveTime">설정된 시간을 디스크에 저장할지 여부 (기본값: false) </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Set(DateTime utc, bool saveTime = false)
        {
            var nowTicks = utc.Ticks;
            if ((ulong)(nowTicks - MinTicks) > TickLimit)
                return;

            lock (_lock)
            {
                if (saveTime)
                {
                    var sysTicks = DateTime.UtcNow.Ticks;
                    var offset = sysTicks - nowTicks;

                    // 마지막으로 저장한지 10분 이상 지났거나, 기존 오차와 1분 이상 차이날 때 시간 저장
                    if (sysTicks - _lastSysTicks > TimeSpan.TicksPerMinute * 10 ||
                        Math.Abs(offset - _lastOffset) > TimeSpan.TicksPerMinute)
                    {
                        _lastOffset = offset;
                        _lastSysTicks = sysTicks;
                        SaveLastTime(sysTicks, nowTicks);
                    }
                }

                try
                {
                    var nextseq = _seq + 1;
                    _time[nextseq].Set(utc);
                    Volatile.Write(ref _seq, nextseq);
                }
                catch (Exception) { }
            }
        }
    }
}