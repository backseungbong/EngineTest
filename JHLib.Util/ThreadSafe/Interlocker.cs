using System.Runtime.CompilerServices;

namespace JHLib.Util.ThreadSafe
{
    /// <summary> Interlocked을 통한 간단한 스레드 동기화를 위해 작성 </summary>
    /// <param name="initToReady"> 
    /// 초기값을 잠금 준비상태로 설정할지에 대한 유무, 준비상태로 설정이 안되면 모든 잠금 실패
    /// </param>
    public class Interlocker(bool initToReady = false)
    {
        private const int NONE = 0;
        private const int READY = 1;
        private const int BUSY = 2;
        private const int CLOSE = 3;

        private int _status = initToReady ? READY : NONE;
        private int _thread = -1;
        private uint _entcnt = 0;

        /// <summary> 잠금을 준비상태로 설정한다 </summary>
        public void SetReady() => Interlocked.CompareExchange(ref _status, READY, NONE);

        /// <summary> 닫기 상태를 기다린다 </summary>
        /// <returns> IDLE 상태에서 첫번째 CLOSE된 경우 true </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Close()
        {
            if (_status == CLOSE)
                return false;

            while (true)
            {
                var status = Interlocked.CompareExchange(ref _status, CLOSE, READY);
                if (status == BUSY) Thread.Sleep(1);
                else return status == READY;
            }
        }

        /// <summary> 잠금을 해지한다. 반드시 잠금 이후 호출하여야 한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Unlock()
        {
            var entcnt = _entcnt;
            if (entcnt != 0)
            {
                _entcnt = entcnt - 1;

                if (entcnt == 1)
                {
                    _thread = -1;
                    Volatile.Write(ref _status, READY);
                }
            }
        }

        /// <summary>
        /// 한번 잠금 시도 후 Action을 실행한다<para/>
        /// 잠금 성공시 action 실행 및 true 반환, 실패시 false 반환<para/>        
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLockAction(Action action)
        {
            if (Interlocked.CompareExchange(ref _status, BUSY, READY) == READY)
            {
                try { action(); } catch { }
                Volatile.Write(ref _status, READY);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 잠금을 Thread.Sleep() 기반으로 시도하며, 잠금 이후 action을 실행한다 <para/>
        /// 주의! 잠금동안 await Delay()와 같은 대기 함수사용 불가 (Thread ID가 변동되는 상황에 대응 불가) <para/>           
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool TrySleepLock(Action action)
        {
            if (TrySleepLock())
            {
                try { action(); } catch { }
                Unlock();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 잠금을 SpinWait.SpinOnce() 기반으로 시도하며, 잠금 이후 action을 실행한다 <para/>
        /// 주의! 잠금동안 await Delay()와 같은 대기 함수사용 불가 (Thread ID가 변동되는 상황에 대응 불가) <para/>      
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool TrySpinLock(Action action)
        {
            if (TrySpinLock())
            {
                try { action(); } catch { }
                Unlock();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 잠금을 Thread.Sleep() 기반으로 시도한다. 잠금 이후 반드시 Unlock을 호출해야 한다 <para/>
        /// 주의! 잠금동안 await Delay()와 같은 대기 함수사용 불가 (Thread ID가 변동되는 상황에 대응 불가) <para/>        
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySleepLock()
        {
            var stat = Interlocked.CompareExchange(ref _status, BUSY, READY);
            if (stat != READY) return WaitSleep(stat);
            _thread = Environment.CurrentManagedThreadId;
            _entcnt = 1;
            return true;
        }

        /// <summary>
        /// 잠금을 SpinWait.SpinOnce() 기반으로 시도한다. 잠금 이후 반드시 Unlock을 호출해야 한다 <para/>
        /// 주의! 잠금동안 await Delay()와 같은 대기 함수사용 불가 (Thread ID가 변동되는 상황에 대응 불가) <para/>   
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySpinLock()
        {
            var stat = Interlocked.CompareExchange(ref _status, BUSY, READY);
            if (stat != READY) return WaitSpin(stat);
            _thread = Environment.CurrentManagedThreadId;
            _entcnt = 1;
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool WaitSleep(int stat)
        {
            if (_thread != Environment.CurrentManagedThreadId)
            {
                do
                {
                    if (stat != BUSY)
                        return false;

                    Thread.Sleep(1);
                    stat = Interlocked.CompareExchange(ref _status, BUSY, READY);
                }
                while (stat != READY);
                _thread = Environment.CurrentManagedThreadId;
            }
            _entcnt++;
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool WaitSpin(int stat)
        {
            if (_thread != Environment.CurrentManagedThreadId)
            {
                var spin = new SpinWait();
                do
                {
                    if (stat != BUSY)
                        return false;

                    spin.SpinOnce();
                    stat = Interlocked.CompareExchange(ref _status, BUSY, READY);
                }
                while (stat != READY);
                _thread = Environment.CurrentManagedThreadId;
            }
            _entcnt++;
            return true;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryLock(ref int locker) => Interlocked.CompareExchange(ref locker, 1, 0) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Lock(ref int locker) { if (Interlocked.CompareExchange(ref locker, 1, 0) != 0) Spinlocker(ref locker); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unlock(ref int locker) => Volatile.Write(ref locker, 0);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Spinlocker(ref int locker)
        {
            var spin = new SpinWait();
            do spin.SpinOnce();
            while (Interlocked.CompareExchange(ref locker, 1, 0) != 0);
        }
    }
}