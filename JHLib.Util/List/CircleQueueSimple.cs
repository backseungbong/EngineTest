namespace JHLib.Util.List
{
    public class CircleQueueSimple<T>
    {
        private readonly T[] _buk;
        private readonly int _msk;
        private volatile int _enq;
        private int _deq;
        public bool IsExist => _enq != _deq;
        public CircleQueueSimple(int cap)
        {
            var n = cap - 1;
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;
            _buk = new T[n + 1];
            _msk = n;
        }

        public void Enq(T data)
        {
            var i = _enq;
            _buk[i] = data;
            _enq = _msk & i + 1;
        }

        public bool Enq(List<T> data)
        {
            if (data != null && data.Count != 0)
            {
                var index = 0;
                do
                {
                    var i = _enq;
                    _buk[i] = data[index];
                    _enq = _msk & i + 1;
                }
                while (++index < data.Count);
                return true;
            }
            return false;
        }

        public bool Deq(out T data)
        {
            var i = _deq;
            if (i != _enq)
            {
                _deq = _msk & i + 1;
                data = _buk[i]; _buk[i] = default;
                return true;
            }
            data = default;
            return false;
        }
    }
}