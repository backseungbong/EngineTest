using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace JHLib.Util.ProcessManagement
{
    /// <summary>프로세스 시작 시 호출되는 콜백</summary>
    public delegate void ProcessStartedHandler(Process process, string tag);

    /// <summary> 프로세스 와치독. 지정된 실행 파일을 실행하고, 비정상 종료 시 자동 재실행</summary>
    public sealed class ProcessWatchdog : IDisposable
    {
        private readonly Lock _locker;
        private readonly ProcessStartInfo _startInfo;
        private readonly string _processName;
        private readonly string _tag;
        private readonly bool _killExisting;

        private Process _process;
        private volatile bool _isRunning;
        private int _disposed;


        /// <summary>프로세스 재시작 대기 시간</summary>
        public TimeSpan RestartDelay { get; init; } = TimeSpan.FromMilliseconds(500);

        /// <summary>현재 실행 중인 프로세스 (실행중이 아닐때 null 반환)</summary>
        public Process RunningProcess => _isRunning ? _process : null;

        /// <summary>프로세스 이름 (확장자 제외)</summary>
        public string ProcessName => _processName;


        /// <summary>프로세스 시작 시 발생</summary>
        public event ProcessStartedHandler ProcessStarted;

        public void Dispose()
        {
            lock (_locker)
            {
                if (_disposed == 0)
                {
                    _disposed = 1;
                    StopCore(null);
                }
            }
        }


        /// <param name="filePath">실행할 프로세스 경로</param>
        /// <param name="killExisting">시작 시 동일 이름 기존 프로세스 강제 종료 여부</param>
        /// <param name="tag">식별용 태그. 미지정 시 프로세스 이름 사용</param>
        public ProcessWatchdog(string filePath, bool killExisting = false, string tag = null) :
            this(new ProcessStartInfo(filePath), killExisting, tag)
        { }

        /// <param name="startInfo">실행할 프로세스 정보</param>
        /// <param name="killExisting">시작 시 동일 이름 기존 프로세스 강제 종료 여부</param>
        /// <param name="tag">식별용 태그. 미지정 시 프로세스 이름 사용</param>
        public ProcessWatchdog(ProcessStartInfo startInfo, bool killExisting = false, string tag = null)
        {
            var filePath = startInfo.FileName;
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Executable path cannot be null or whitespace", nameof(startInfo.FileName));

            var processName = Path.GetFileNameWithoutExtension(filePath);
            if (string.IsNullOrWhiteSpace(processName))
                throw new ArgumentException("Invalid executable path", filePath);

            if (File.Exists(filePath) == false)
                throw new FileNotFoundException("Executable not found", filePath);

            _locker = new();
            _startInfo = startInfo;
            _processName = processName;
            _killExisting = killExisting;
            _tag = tag ?? processName;
        }


        /// <summary>프로세스 시작. 이미 실행 중이면 종료 후 자동 재시작</summary>
        public void Start() => StartCore(null);

        /// <summary>현재 프로세스 종료 후 자동 재시작</summary>
        public void Restart() => StartCore(null);

        /// <summary>프로세스 종료 및 자동 재시작 중단</summary>
        public void Stop() => StopCore(null);


        [MethodImpl(MethodImplOptions.NoInlining)]
        private void StartCore(Process expected)
        {
            lock (_locker)
            {
                if (_disposed != 0)
                    return;

                if (expected != null && _process != expected)
                    return;

                if (_killExisting)
                    ProcessHelper.Kill(_processName);

                var process = new Process { StartInfo = _startInfo, EnableRaisingEvents = true };
                process.Exited += OnExited;
                StopCore(process);

                try
                {
                    if (process.Start())
                    {
                        _isRunning = true;
                        ProcessStarted?.Invoke(process, _tag);
                        return;
                    }
                }
                catch { }

                Task.Delay(RestartDelay).ContinueWith(_ => StartCore(process));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void StopCore(Process replace)
        {
            lock (_locker)
            {
                _isRunning = false;

                var process = _process; _process = replace;
                if (process != null)
                {
                    process.Exited -= OnExited;
                    try
                    {
                        process.Kill(true);
                        process.WaitForExit(3000);
                    }
                    catch { }
                    process.Dispose();
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnExited(object sender, EventArgs e)
        {
            lock (_locker)
            {
                _isRunning = false;

                var process = (Process)sender;
                if (process == _process)
                    Task.Delay(RestartDelay).ContinueWith(_ => StartCore(process));
            }
        }
    }
}