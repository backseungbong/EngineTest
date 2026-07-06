namespace JHLib.Util.ThreadSafe
{
    public class DataSwitcher<T>
    {
        private const int STATUS_READ = 1;
        private const int STATUS_WRITE = 2;
        private class DataLocker
        {
            private int _locker;
            public T Data;
            public DataLocker(T data) { Data = data; _locker = 0; }
            public void Unlock() => _locker = 0;
            public bool TryReadLock()
            {
                var status = Interlocked.CompareExchange(ref _locker, STATUS_READ, 0);
                return status == 0 || status == STATUS_READ;
            }
            public bool TryWriteLock()
            {
                var status = Interlocked.CompareExchange(ref _locker, STATUS_WRITE, 0);
                return status == 0 || status == STATUS_WRITE;
            }
        }

        private readonly DataLocker[] _datas;
        private int _index;
        private int _readingIndex;
        private int _writingIndex;
        private bool _isReading;
        private bool _isWriting;

        public DataSwitcher(T switchData1, T switchData2)
        {
            _datas = new DataLocker[2];
            _datas[0] = new(switchData1);
            _datas[1] = new(switchData2);
        }

        /// <summary>
        /// 쓰기 데이타를 가져온다 <para/>
        /// 쓰기 데이타를 사용 후 ReadComplete() 를 호출해야 하며 그렇지 않을경우 읽기 데이타와 스위칭 되지 않는다<para/>
        /// 이 함수는 Thread Safe 하지 않으므로 단일 쓰레드에서 호출되도록 한다 <para/>
        /// </summary>
        public ref T GetWriteData()
        {
            if (_isWriting == false)
            {
                _isWriting = true;

                var d = _datas;
                var i = _index;
                if (d[++i & 1].TryWriteLock() == false &&
                    d[++i & 1].TryWriteLock() == false &&
                    d[++i & 1].TryWriteLock() == false)
                    throw new InvalidOperationException("invalid status");

                _writingIndex = i & 1;
                return ref d[i & 1].Data;
            }
            return ref _datas[_writingIndex].Data;
        }

        /// <summary>
        /// 쓰기 데이타 사용을 완료한다 <para/>
        /// 이 함수는 Thread Safe 하지 않으므로 단일 쓰레드에서 호출되도록 한다 <para/>
        /// </summary>
        public void WriteComplete()
        {
            if (_isWriting)
            {
                _isWriting = false;
                _datas[_writingIndex].Unlock();
                _index = _writingIndex;
            }
        }


        /// <summary>
        /// 읽기 데이타를 가져온다 <para/>
        /// 읽기 데이타를 사용 후 ReadComplete() 를 호출해야 하며 그렇지 않을경우 쓰기 데이타와 스위칭 되지 않는다<para/>
        /// 이 함수는 Thread Safe 하지 않으므로 단일 쓰레드에서 호출되도록 한다 <para/>
        /// </summary>
        public ref T GetReadData()
        {
            if (_isReading == false)
            {
                _isReading = true;

                var d = _datas;
                var i = _index;
                if (d[i & 1].TryReadLock() == false &&
                    d[++i & 1].TryReadLock() == false &&
                    d[++i & 1].TryReadLock() == false)
                    throw new InvalidOperationException("invalid status");

                _readingIndex = i & 1;
                return ref d[i & 1].Data;
            }
            return ref _datas[_readingIndex].Data;
        }

        /// <summary>
        /// 읽기 데이타 사용을 완료한다 <para/>
        /// 이 함수는 Thread Safe 하지 않으므로 단일 쓰레드에서 호출되도록 한다 <para/>
        /// </summary>
        public void ReadComplete()
        {
            if (_isReading)
            {
                _isReading = false;
                _datas[_readingIndex].Unlock();
            }
        }
    }
}