using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Network
{
    public delegate void ErrorMessageHandler(string error);
    public delegate void UpdateStatusHandler(StatusType type);
    public delegate void ReceivedBytesHandler(byte[] bytes);
    public delegate void ReceivedSentenceHandler(string sentence);

    public enum StatusType { Open, Close, OpenFailed }
    public enum OutputType { Bytes, Sentence }

    public abstract class PortBase
    {
        protected const int BUFFER_SIZE = 4096;

        protected const int STATUS_RECONNECT = 1;
        protected const int STATUS_CONNECT = 2;
        protected const int STATUS_DISPOSE = 3;

        protected readonly Lock _locker = new();
        protected readonly CircleStreamBuffer _stream = new();
        protected readonly byte[] _buffer = new byte[BUFFER_SIZE];

        protected volatile int _status = 0;
        protected readonly EventWaitHandle _onWorker = new(false, EventResetMode.AutoReset);
        protected readonly EventWaitHandle _endWorker = new(false, EventResetMode.ManualReset);
        protected readonly EventWaitHandle _endReceiver = new(true, EventResetMode.ManualReset);

        private bool _disposed;
        private Thread _worker;
        private OutputType _output;
        public OutputType OutputType { get => _output; set => _output = value; }

        public event ErrorMessageHandler OnErrorMessage;
        public event UpdateStatusHandler OnUpdateStatus;
        public event ReceivedBytesHandler OnReceivedBytes;
        public event ReceivedSentenceHandler OnReceivedSentence;

        protected void DisposeBase()
        {
            if (_disposed)
                return;

            _status = STATUS_DISPOSE;
            _disposed = true;

            Task.Run(() =>
            {
                _onWorker.Set();
                _endWorker.WaitOne();

                _onWorker.Dispose();
                _endWorker.Dispose();
                _endReceiver.Dispose();
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void BeginWorker()
        {
            if (_worker == null)
            {
                _worker = new(Worker) { IsBackground = true };
                _worker.Start();
            }
        }

        private void Worker()
        {
            while (true)
            {
                _onWorker.WaitOne();

                if (_status == STATUS_CONNECT)
                {
                    if (_output == OutputType.Bytes)
                    {
                        while (_stream.TryNextBytes(out var bytes))
                            RaiseReceivedBytes(bytes);
                    }
                    else
                    {
                        while (_stream.TryNextSentence(out var sentence))
                            RaiseReceivedSentence(sentence);
                    }
                }
                else if (_status == STATUS_DISPOSE)
                {
                    _endWorker.Set();
                    return;
                }
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void RaiseErrorMessage(string error)
        {
            try { OnErrorMessage?.Invoke(error); }
            catch (Exception e) { Trace.WriteLine(e); }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void RaiseSocketStatus(StatusType status)
        {
            try { OnUpdateStatus?.Invoke(status); }
            catch (Exception e) { Trace.WriteLine(e); }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void RaiseReceivedBytes(byte[] bytes)
        {
            try { OnReceivedBytes?.Invoke(bytes); }
            catch (Exception e) { Trace.WriteLine(e); }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void RaiseReceivedSentence(string sentence)
        {
            try { OnReceivedSentence?.Invoke(sentence); }
            catch (Exception e) { Trace.WriteLine(e); }
        }
    }
}