namespace JHLib.Util.Time
{
    /// <summary>
    /// 시간이 유효한지 체크하는 클래스 <para/>
    /// 유효한 최소,최대 시간과 유효 카운팅 그리고 오차 제한을 지정하여 유효체크한다
    /// </summary>
    /// <param name="min">유효한 최소 시간 (ex DateTime(2000,1,1))</param>
    /// <param name="max">유효한 최대 시간 (ex DateTime(2100,1,1))</param>
    /// <param name="checkValid">유효로 판단할 최소 카운트</param>
    /// <param name="errorLimit">시간 오차 제한(초)</param>
    internal class TimeValidChecker(DateTime min, DateTime max, uint checkValid = 5, uint errorLimit = 600)
    {
        private readonly ulong[] _buk = new ulong[checkValid];
        private readonly ulong _secMin = (ulong)(min.Ticks / TimeSpan.TicksPerSecond);
        private readonly ulong _secMax = (ulong)(max.Ticks / TimeSpan.TicksPerSecond);
        private readonly uint _errorLimit = errorLimit;

        private ulong _secOld;
        private int _idx;
        private int _validCount;
        private int _locker;

        public bool CheckValid(DateTime time)
        {
            if (Interlocked.CompareExchange(ref _locker, 1, 0) != 0)
            {
                var spin = new SpinWait();
                do spin.SpinOnce();
                while (Interlocked.CompareExchange(ref _locker, 1, 0) != 0);
            }

            var secNow = (ulong)(time.Ticks / TimeSpan.TicksPerSecond);
            if (secNow >= _secMin && secNow <= _secMax && secNow != _secOld)
            {
                var secOld = _secOld; _secOld = secNow;
                var buk = _buk;
                var idx = _idx; _idx = (idx + 1) % buk.Length;
                buk[idx] = secNow;

                // 이전 시간이 제한 범위고, 이전 시간 모두 제한 범위 안에 있으면 추가 처리없이 유효
                if (secNow - secOld < _errorLimit && buk.Length == _validCount)
                {
                    _locker = 0;
                    return true;
                }

                // 그렇지 않으면 제한 범위 다시 카운트
                var validCount = 0;
                for (var i = 0; i < buk.Length; i++)
                    if (secNow - buk[i] < _errorLimit)
                        validCount++;

                _validCount = validCount;
                _locker = 0;
                return buk.Length - 1 <= validCount; // 1개의 시간이 튀어도 유효하다고 판단
            }
            _locker = 0;
            return false;
        }
    }
}