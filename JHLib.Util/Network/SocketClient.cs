using System.Net.Sockets;

namespace JHLib.Util.Network
{
    public class SocketClient : PortBase, IDisposable
    {
        private Socket _socket;

        /// <summary>소켓을 연다, 이미 열려 있는 상태라면 기존의 소켓을 닫은뒤에 연다</summary>
        /// <returns>소켓 오픈 성공 유무</returns>
        public bool Open(string ip, int port,
            AddressFamily addressFamily = AddressFamily.InterNetwork,
            SocketType socketType = SocketType.Stream,
            ProtocolType protocolType = ProtocolType.Tcp)
        {
            lock (_locker)
            {
                if (_status == STATUS_DISPOSE) return false;
                if (_status == STATUS_CONNECT) Close(STATUS_RECONNECT);

                try
                {
                    using var token = new CancellationTokenSource(3000);
                    _socket = new Socket(addressFamily, socketType, protocolType);
                    _socket.ConnectAsync(ip, port, token.Token).AsTask().Wait();
                    _status = STATUS_CONNECT;

                    RaiseSocketStatus(StatusType.Open);
                    BeginWorker();
                    BeginReceiver(_socket);
                    return _status == STATUS_CONNECT;
                }
                catch (Exception e)
                {
                    try { _socket?.Dispose(); }
                    catch (Exception) { }
                    _socket = null;
                    _status = 0;

                    RaiseErrorMessage(e.Message);
                    RaiseSocketStatus(StatusType.OpenFailed);
                    return false;
                }
            }
        }

        /// <summary> 스트림 데이타를 보낸다 (실패시 반환값은 false) </summary>
        public bool Write(byte[] data) => Write(data.AsSpan());
        public bool Write(ReadOnlySpan<byte> data)
        {
            try
            {
                var socket = _socket ?? throw new("write failed : socket is null");
                socket.Send(data);
                return true;
            }
            catch (Exception e)
            {
                RaiseErrorMessage(e.Message);
                return false;
            }
        }

        /// <summary> 소켓을 닫는다 (Open을 통한 재연결 가능) </summary>
        public void Close() => Close(STATUS_RECONNECT);

        /// <summary> 소켓을 닫고, 모든 리소스를 해지한다 (Open을 통한 재연결 불가능) </summary>
        public void Dispose() => Close(STATUS_DISPOSE);

        private void Close(int status, Socket check = null, string error = null)
        {
            lock (_locker)
            {
                var socket = _socket;
                if (socket != null && (check == null || check == socket))
                {
                    _status = status;
                    _socket = null;
                    try { socket.Dispose(); } catch (Exception) { }
                    _endReceiver.WaitOne();
                    _stream.Reset();

                    if (error != null) RaiseErrorMessage(error);
                    RaiseSocketStatus(StatusType.Close);
                }

                if (status == STATUS_DISPOSE)
                    DisposeBase();
            }
        }

        private async void BeginReceiver(Socket socket)
        {
            _endReceiver.Reset();
            try
            {
                while (true)
                {
                    var bytesReceived = await socket.ReceiveAsync(_buffer.AsMemory());
                    if (bytesReceived > 0)
                    {
                        _stream.PushBytes(_buffer, bytesReceived);
                        _onWorker.Set();
                    }
                    else
                    {
                        throw new("remote host has closed or socket is no longer connected");
                    }
                }
            }
            catch (Exception e)
            {
                _endReceiver.Set();
                Close(STATUS_RECONNECT, socket, e.Message);
            }
        }
    }
}