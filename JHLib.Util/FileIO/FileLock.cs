namespace JHLib.Util.FileIO
{
    /// <summary>
    /// 프로세스 간 동기화(Lock)용 파일 핸들<br/>
    /// IDisposable을 구현하여 using 블록 종료 시 OS 레벨 잠금을 해제한다
    /// </summary>
    public sealed class FileLockHandle(FileStream stream) : IDisposable
    {
        private FileStream _stream = stream;
        public void Dispose() { _stream?.Dispose(); _stream = null; }
    }

    /// <summary>
    /// 크로스 플랫폼(Windows, Linux, macOS) 다중 프로세스 파일 접근 제어 유틸리티
    /// </summary>
    public static class FileLock
    {
        /// <summary>
        /// 원본 파일에 ".lock"을 붙인 보조 파일로 독점적 접근 권한(Lock) 획득을 시도한다
        /// </summary>
        /// <param name="path">원본 파일 경로</param>
        /// <returns>성공 시 핸들 반환, 실패 시 null 반환</returns>
        public static FileLockHandle TryAcquire(string path)
        {
            const string EXT_LOCK = ".lock"; // Lock file extension     

            try
            {
                var lockPath = string.Concat(path, EXT_LOCK);
                var stream = new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    options: FileOptions.None);

                return new FileLockHandle(stream);
            }
            catch { }
            return null;
        }
    }
}