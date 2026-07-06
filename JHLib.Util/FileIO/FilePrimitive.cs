using JHLib.Util.Performance;
using JHLib.Util.Helper;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace JHLib.Util.FileIO
{
    public static class FilePrimitive
    {
        // UTF-8 without BOM and with error detection. Same as the default encoding for StreamWriter.
        private static Encoding UTF8NoBOM => field ??= new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, false);

        /// <summary>
        /// 지정된 경로의 파일을 읽어 바이트 배열로 반환한다.
        /// <para> 읽기 실패 시 null을 반환한다.</para>
        /// </summary>
        public static byte[] Read(string path)
        {
            try
            {
                using var handle = CreateReadHandle(path);
                var length = RandomAccess.GetLength(handle);

                // Array.MaxLength 보다 큰 파일에 대해선 예외없이 Empty 반환하도록 의도됨
                // 큰 파일은 이 함수가 아닌 DB나 스트림 방식으로 처리해야 함
                if ((ulong)(length - 1) >= (ulong)Array.MaxLength)
                    return [];

                var size = (int)length;
                var data = new byte[size];
                ref var data0 = ref MemoryMarshal.GetArrayDataReference(data);

                var offset = 0L;
                do
                {
                    var span = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref data0, (uint)offset), size);
                    var read = RandomAccess.Read(handle, span, offset);
                    if (read <= 0) break;
                    offset += read;
                    size -= read;
                }
                while (offset < length);
                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"FilePrimitive.Read Exception:{ex.Message} {path}");
                return null;
            }
        }

        /// <summary>
        /// 파일을 읽어 BufferPool에서 대여한 BufferPoolSpan을 반환한다<br/>
        /// 작업이 끝나면 즉시 BufferPoolSpan.Return() 호출을 권장한다
        /// </summary>
        public static BufferPoolSpan ReadBufferPoolSpan(string path)
        {
            var pooldata = default(BufferPoolItem);
            try
            {
                using var handle = CreateReadHandle(path);
                var length = RandomAccess.GetLength(handle);

                // Array.MaxLength 보다 큰 파일에 대해선 예외없이 Empty 반환하도록 의도됨
                // 큰 파일은 이 함수가 아닌 DB나 스트림 방식으로 처리해야 함
                if ((ulong)(length - 1) >= (ulong)Array.MaxLength)
                    return default;

                var size = (int)length;
                var data = BufferPool.Rent(size); pooldata = data;
                ref var data0 = ref data.Buffer0;

                var offset = 0L;
                do
                {
                    var span = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref data0, (uint)offset), size);
                    var read = RandomAccess.Read(handle, span, offset);
                    if (read <= 0) break;
                    offset += read;
                    size -= read;
                }
                while (offset < length);
                return data.ToPoolSpan((int)length);
            }
            catch (Exception ex)
            {
                pooldata.Return();
                Trace.WriteLine($"FilePrimitive.Read Exception:{ex.Message} {path}");
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SafeFileHandle CreateReadHandle(string path)
        {
            return File.OpenHandle(path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                OperatingSystem.IsWindows() ? FileOptions.SequentialScan : FileOptions.None,
                0);
        }




        /// <summary>데이터를 파일로 쓴다 </summary>
        /// <param name="flushToDisk">true일 경우 OS 버퍼를 디스크로 강제 플러시한다</param>
        /// <returns>성공 시 true, 실패 시 false.</returns>
        public static bool Write(string path, string content, bool flushToDisk = false)
        {
            var data = UTF8NoBOM.GetBytes(content);
            return Write(path, UnsafeEx.CreateReadSpan(data), flushToDisk);
        }

        /// <summary>데이터를 파일로 쓴다 </summary>
        /// <param name="flushToDisk">true일 경우 OS 버퍼를 디스크로 강제 플러시한다</param>
        /// <returns>성공 시 true, 실패 시 false.</returns>
        public static bool Write(string path, byte[] data, bool flushToDisk = false) =>
            Write(path, UnsafeEx.CreateReadSpan(data), flushToDisk);

        /// <summary>데이터를 파일로 쓴다 </summary>
        /// <param name="flushToDisk">true일 경우 OS 버퍼를 디스크로 강제 플러시한다</param>
        /// <returns>성공 시 true, 실패 시 false.</returns>
        public static bool Write(string path, ReadOnlySpan<byte> data, bool flushToDisk = false)
        {
            try
            {
                WriteInternal(path, data, flushToDisk);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"FilePrimitive.Write Exception:{ex.Message} {path}");
                return false;
            }
        }

        /// <summary>
        /// 임시 파일을 사용하여 원자적(Atomic) 쓰기를 수행한다.
        /// <para> 원자적 쓰기는 원본 파일의 손상을 방지한다</para>
        /// </summary>
        /// <param name="path">저장할 원본 파일 경로</param>
        /// <param name="content">파일에 기록할 문자열 (UTF-8 인코딩 적용)</param>
        /// <param name="lockWrite">파일에 대한 모든 동시 접근을 차단할지 여부 (기본값: false)</param>
        /// <returns>성공 시 true, 실패 시 false</returns>
        public static bool WriteAtomic(string path, string content, bool lockWrite = false)
        {
            var data = UTF8NoBOM.GetBytes(content);
            return WriteAtomic(path, UnsafeEx.CreateReadSpan(data), lockWrite);
        }

        /// <summary>
        /// 임시 파일을 사용하여 원자적(Atomic) 쓰기를 수행한다.
        /// <para> 원자적 쓰기는 원본 파일의 손상을 방지한다</para>
        /// </summary>
        /// <param name="path">저장할 원본 파일 경로</param>
        /// <param name="data">파일에 기록할 원본 바이트 배열</param>
        /// <param name="lockWrite">파일에 대한 모든 동시 접근을 차단할지 여부 (기본값: false)</param>
        /// <returns>성공 시 true, 실패 시 false</returns>
        public static bool WriteAtomic(string path, byte[] data, bool lockWrite = false) =>
            WriteAtomic(path, UnsafeEx.CreateReadSpan(data), lockWrite);

        /// <summary>
        /// 임시 파일을 사용하여 원자적(Atomic) 쓰기를 수행한다.
        /// <para> 원자적 쓰기는 원본 파일의 손상을 방지한다</para>
        /// </summary>
        /// <param name="path">저장할 원본 파일 경로</param>
        /// <param name="data">파일에 기록할 읽기 전용 바이트 메모리 영역 (Span)</param>
        /// <param name="lockWrite">파일에 대한 모든 동시 접근을 차단할지 여부 (기본값: false)</param>
        /// <returns>성공 시 true, 실패 시 false</returns>
        public static bool WriteAtomic(string path, ReadOnlySpan<byte> data, bool lockWrite = false)
        {
            const string EXT_TEMP = ".temp"; // Temporary file extension     

            using var fileLock = lockWrite ? FileLock.TryAcquire(path) : null;
            if (lockWrite && fileLock == null)
            {
                Trace.WriteLine($"FilePrimitive.WriteAtomic LockFailed {path}");
                return false;
            }

            try
            {
                var tempPath = string.Concat(path, EXT_TEMP);
                WriteInternal(tempPath, data, true);
                File.Move(tempPath, path, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"FilePrimitive.WriteAtomic Exception:{ex.Message} {path}");
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WriteInternal(string path, ReadOnlySpan<byte> data, bool flushToDisk = false)
        {
            using var handle = CreateWriteHandle(path, data.Length);

            RandomAccess.Write(handle, data, 0);

            if (flushToDisk)
                RandomAccess.FlushToDisk(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SafeFileHandle CreateWriteHandle(string path, int length)
        {
            const int DEFAULT_CLUSTERSIZE = 4096;

            return File.OpenHandle(path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                FileOptions.None,
                length > DEFAULT_CLUSTERSIZE ? length : 0);
        }
    }
}