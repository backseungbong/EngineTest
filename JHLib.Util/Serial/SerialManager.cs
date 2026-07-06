using JHLib.Util.Time;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace JHLib.Util.Serial
{
    public class SerialManager : IDisposable
    {
        private const int IDLE = 0;
        private const int BUSY = 1;
        private const int PRECLOSE_START = 2;
        private const int PRECLOSE_END = 3;
        private const int ALLCLOSE_START = 4;
        private const int ALLCLOSE_END = 5;

        #region SerialPortManager
        private class SerialPortManager : IDisposable
        {
            //private readonly SerialPort _port;
            //private readonly int _number;
            //private readonly int _baudRate;
            //private Encoding _encode;

            //private ReadDataProcessingDelegate _callback;
            //private IWork _iworkParser;
            //private int _status;

            //private readonly SerialBuffer _buffer;
            //private readonly SentenceList _bufferSentence;
            //private bool _sentenceCallback;

            //public SerialPort Port => _port;
            //public int Number => _number;
            //public int BaudRate => _baudRate;
            //public Encoding Encode => _encode;
            //public bool SentenceCallback => _sentenceCallback;

            //public SerialPortManager(byte number, SerialPort port, ReadDataProcessingDelegate callback)
            //{
            //    _port = port;
            //    _number = number;
            //    _baudRate = port.BaudRate;
            //    _encode = port.Encoding;

            //    _callback = callback;
            //    _iworkParser = Interval.AddWork(IntervalParser, 15);
            //    _status = IDLE;

            //    _buffer = new SerialBuffer();
            //    _bufferSentence = new SentenceList();
            //    _sentenceCallback = true;

            //    _port.DataReceived += OnDataReceived;
            //}

            //private void OnDataReceived(object sender, SerialDataReceivedEventArgs e) =>
            //    _buffer.PushReceivedBytes(_port);

            //private void IntervalParser()
            //{
            //    var buffer = _buffer;
            //    if (buffer.ExistReceivedBytes)
            //    {
            //        if (Interlocked.CompareExchange(ref _status, BUSY, IDLE) == IDLE)
            //        {
            //            if (_sentenceCallback)
            //            {
            //                var result = _bufferSentence;
            //                if (buffer.GetSentences(_encode, result))
            //                {
            //                    var b = result.Bucket;
            //                    var i = 0;
            //                    do { _callback(_number, _baudRate, b[i], null); b[i] = null; }
            //                    while (++i < result.Count);
            //                    result.Clear();
            //                }
            //            }
            //            else
            //            {
            //                if (buffer.GetBytes(out var bytes))
            //                    _callback(_number, _baudRate, null, bytes);
            //            }
            //            _status = IDLE;
            //        }
            //    }
            //}

            //public void SetEncoding(Encoding encode)
            //{
            //    while (true)
            //    {
            //        if (Interlocked.CompareExchange(ref _status, BUSY, IDLE) == IDLE)
            //        {
            //            _port.Encoding = encode;
            //            _encode = encode;
            //            _status = IDLE;
            //            return;
            //        }
            //        if (_status > BUSY) break;
            //        Thread.Yield();
            //    }
            //}

            //public void SetSentenceCallback(bool isSentenceCallback)
            //{
            //    _sentenceCallback = isSentenceCallback;
            //}

            //public void Write(byte[] buffer, int offset, int count)
            //{
            //    while (true)
            //    {
            //        if (Interlocked.CompareExchange(ref _status, BUSY, IDLE) == IDLE)
            //        {
            //            _port.Write(buffer, offset, count);
            //            _status = IDLE;
            //            return;
            //        }
            //        if (_status > BUSY) break;
            //        Thread.Yield();
            //    }
            //}

            //public void Write(char[] buffer, int offset, int count)
            //{
            //    while (true)
            //    {
            //        if (Interlocked.CompareExchange(ref _status, BUSY, IDLE) == IDLE)
            //        {
            //            _port.Write(buffer, offset, count);
            //            _status = IDLE;
            //            return;
            //        }
            //        if (_status > BUSY) break;
            //        Thread.Yield();
            //    }
            //}

            //public void Write(string text)
            //{
            //    while (true)
            //    {
            //        if (Interlocked.CompareExchange(ref _status, BUSY, IDLE) == IDLE)
            //        {
            //            _port.Write(text);
            //            _status = IDLE;
            //            return;
            //        }
            //        if (_status > BUSY) break;
            //        Thread.Yield();
            //    }
            //}

            //public void WriteLine(string text)
            //{
            //    while (true)
            //    {
            //        if (Interlocked.CompareExchange(ref _status, BUSY, IDLE) == IDLE)
            //        {
            //            _port.WriteLine(text);
            //            _status = IDLE;
            //            return;
            //        }
            //        if (_status > BUSY) break;
            //        Thread.Yield();
            //    }
            //}

            public void Dispose() => TryClose();
            public bool TryClose()
            {
                //while (true)
                //{
                //    if (Interlocked.CompareExchange(ref _status, PRECLOSE_START, IDLE) == IDLE)
                //    {
                //        _iworkParser.Dispose();
                //        _port.DataReceived -= OnDataReceived;
                //        _port.DiscardInBuffer();
                //        _port.DiscardOutBuffer();
                //        _status = PRECLOSE_END;
                //        break;
                //    }
                //    if (_status > BUSY) break;
                //    Thread.Yield();
                //}

                //if (Interlocked.CompareExchange(ref _status, ALLCLOSE_START, PRECLOSE_END) == PRECLOSE_END)
                //{
                //    try
                //    {
                //        Thread.Sleep(15);
                //        _port.Dispose();
                //        Thread.Sleep(30);
                //        _status = ALLCLOSE_END;
                //    }
                //    catch
                //    {
                //        Trace.WriteLine($"An error occurred while closing the COM{_number}");
                //        _status = PRECLOSE_END;
                //    }
                //}
                //return _status == ALLCLOSE_END;
                return false;
            }
        }
        #endregion

        public delegate void ReadDataProcessingDelegate(int portNumber, int baudRate, string text, byte[] bytes);

        //private SerialPortManager[] _buk = new SerialPortManager[256];
        //private int _cnt;
        //private int _lock;

        //public int Count => _cnt;
        //~SerialManager() => Dispose();
        public void Dispose() => CloseAll();
        public void CloseAll()
        {
            //while (true)
            //{
            //    if (Interlocked.CompareExchange(ref _lock, BUSY, IDLE) == IDLE)
            //    {
            //        var b = _buk;
            //        var c = _cnt;
            //        var i = 0;
            //        do
            //        {
            //            if (b[i] != null)
            //            {
            //                try
            //                {
            //                    if (b[i].TryClose())
            //                    {
            //                        b[i] = null;
            //                        c--;
            //                    }
            //                }
            //                catch { }
            //            }
            //        }
            //        while (++i < 256);
            //        _cnt = c;
            //        _lock = IDLE;
            //        return;
            //    }
            //    Thread.Yield();
            //}
        }

        //public bool Open(
        //    ReadDataProcessingDelegate callback,
        //    byte portNumber,
        //    int baudRate = 4800,
        //    Parity parity = Parity.None,
        //    int dataBits = 8,
        //    StopBits stopBits = StopBits.One)
        //{

        //    while (true)
        //    {
        //        if (Interlocked.CompareExchange(ref _lock, BUSY, IDLE) == IDLE)
        //        {
        //            if (_buk[portNumber] == null)
        //            {
        //                var port = default(SerialPort);
        //                try
        //                {
        //                    port = new SerialPort($"COM{portNumber}", baudRate, parity, dataBits, stopBits)
        //                    {
        //                        RtsEnable = true,
        //                        Encoding = Encoding.ASCII
        //                    };

        //                    port.Open();
        //                    _buk[portNumber] = new SerialPortManager(portNumber, port, callback);
        //                    _cnt++;
        //                    _lock = IDLE;
        //                    return true;
        //                }
        //                catch
        //                {
        //                    port?.Close();
        //                    _lock = IDLE;
        //                    return false;
        //                }
        //            }
        //            Trace.WriteLine($"COM{portNumber} port already exists");
        //            _lock = IDLE;
        //            return false;
        //        }
        //        Thread.Yield();
        //    }
        //}

        //public void Close(byte portNumber)
        //{
        //    while (true)
        //    {
        //        if (Interlocked.CompareExchange(ref _lock, BUSY, IDLE) == IDLE)
        //        {
        //            ref var t = ref _buk[portNumber];
        //            if (t != null && t.TryClose()) t = null;
        //            _lock = IDLE;
        //            return;
        //        }
        //        Thread.Yield();
        //    }
        //}

        //public void SetSentenceCallback(byte portNumber, bool isSentenceCallback) =>
        //    _buk[portNumber]?.SetSentenceCallback(isSentenceCallback);

        //public void SetEncoding(byte portNumber, Encoding encode) =>
        //    _buk[portNumber]?.SetEncoding(encode);

        //public void Write(byte portNumber, byte[] buffer, int offset, int count) =>
        //    _buk[portNumber]?.Write(buffer, offset, count);

        //public void Write(byte portNumber, char[] buffer, int offset, int count) =>
        //    _buk[portNumber]?.Write(buffer, offset, count);

        //public void Write(byte portNumber, string text) =>
        //    _buk[portNumber]?.Write(text);

        //public void WriteLine(byte portNumber, string text) =>
        //    _buk[portNumber]?.WriteLine(text);
    }
}