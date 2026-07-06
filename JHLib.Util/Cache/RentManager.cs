using JHLib.Util.Hash;
using JHLib.Util.ThreadSafe;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Cache
{
    public class RefData<T> where T : class
    {
        private RefCounter<T> _own;
        internal RefData(RefCounter<T> own) => _own = own;
        public T Data { get => _own.Data; set => _own.Data = value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return() => Interlocked.Exchange(ref _own, null)?.SubRef();
    }

    internal class RefCounter<T> : IDisposable where T : class
    {
        internal readonly int Key;
        internal volatile T Data;
        internal int RefCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RefCounter(int key) { RefCount = 1; Key = key; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RefData<T> WaitingData()
        {
            if (Data == null)
            {
                var spinwait = new SpinWait();
                do spinwait.SpinOnce();
                while (Data == null);
            }
            return new RefData<T>(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddRef() => Interlocked.Increment(ref RefCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SubRef() => Interlocked.Decrement(ref RefCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (Data is IDisposable disposal)
                disposal.Dispose();
        }
    }

    /// <summary>
    /// 키로 관리하는 데이타 관리 매니저 <para/>
    /// 참조상태를 통해 데이타를 재사용할 수 있도록 관리한다 <para/>
    /// 내부에서 가질 수 있는 데이타 최대치를 초과하면, 참조중이 아닌 오래된 데이타부터 삭제시킨다    
    /// </summary>
    public class RentManager<T> where T : class
    {
        private readonly LinkKeyTo<int, RefCounter<T>> _lmap;
        private readonly int _cleanupThreshold;
        private readonly int _cleanupCount;
        private int _locker;

        /// <param name="cleanupThreshold">데이타 정리 발동 임계치</param>
        /// <param name="cleanupCount">데이타 정리 발동시 최대 정리 갯수</param>
        public RentManager(int cleanupThreshold = 18, int cleanupCount = 6)
        {
            const int MIN_THRESHOLD = 3;
            const int MIN_CLEANUP = 2;

            if (cleanupThreshold < MIN_THRESHOLD)
                cleanupThreshold = MIN_THRESHOLD;

            if (cleanupCount > cleanupThreshold) cleanupCount = cleanupThreshold;
            else if (cleanupCount < MIN_CLEANUP) cleanupCount = MIN_CLEANUP;

            _lmap = new();
            _cleanupThreshold = cleanupThreshold;
            _cleanupCount = cleanupCount;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool AddOrRent(int key, out RefData<T> refBucket, bool priorityClean = false)
        {
            Interlocker.Lock(ref _locker);

            var lmap = _lmap;
            if (lmap.AddOrRefValue(key, out var refValue))
            {
                var count = lmap.Count - 1;
                if (count > _cleanupThreshold)
                {
                    var cleanup = _cleanupCount;
                    foreach (var item in lmap)
                    {
                        if (item.RefCount == 0)
                        {
                            lmap.Remove(item.Key);
                            item.Dispose();
                            if (--cleanup == 0) break;
                        }
                        if (--count == 0) break;
                    }
                }

                if (priorityClean)
                    lmap.LastToFirst();

                var refCounter = new RefCounter<T>(key);
                refValue.Value = refCounter;
                Interlocker.Unlock(ref _locker);

                refBucket = new RefData<T>(refCounter);
                return true;
            }
            else
            {
                var refCounter = refValue.Value;
                refCounter.AddRef();
                Interlocker.Unlock(ref _locker);

                refBucket = refCounter.WaitingData();
                return false;
            }
        }

        public void Remove(int key)
        {
            Interlocker.Lock(ref _locker);
            if (_lmap.Remove(key, out var item))
                item.Dispose();
            Interlocker.Unlock(ref _locker);
        }

        public void Clear()
        {
            Interlocker.Lock(ref _locker);
            _lmap.Clear();
            Interlocker.Unlock(ref _locker);
        }

        public void ClearWithDispose()
        {
            Interlocker.Lock(ref _locker);
            _lmap.ClearWithDispose();
            Interlocker.Unlock(ref _locker);
        }
    }
}