using JHLib.Util.ArrayControl;
using JHLib.Util.ByteControl;
using JHLib.Util.Hash;
using JHLib.Util.Message;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace JHLib.Util.Helper
{
    public static class FileHelper
    {
        public static string AddStringToFileName(string filePath, string addString)
        {
            var span = filePath.AsSpan();
            var filename = Path.GetFileName(span);
            var pathNoExt = Path.Join(Path.GetDirectoryName(span), Path.GetFileNameWithoutExtension(filename));
            return string.Concat(pathNoExt, addString, Path.GetExtension(filename));
        }

        public static string CreateUniqueDirectory(string baseDirectory)
        {
            const int MaxRetries = 30;

            string newDirectoryPath;
            try
            {
                var i = 0;
                do
                {
                    if (++i > MaxRetries)
                        throw new Exception("Failed to create a unique folder. The maximum retry limit has been exceeded.");

                    var name = Path.GetRandomFileName();
                    var index = name.IndexOf('.');
                    if (index != -1) name = name[..index];
                    newDirectoryPath = Path.Combine(baseDirectory, name);
                }
                while (Directory.Exists(newDirectoryPath));
                Directory.CreateDirectory(newDirectoryPath);
                return newDirectoryPath;
            }
            catch (Exception e)
            {
                TraceMessage.Message(e.Message);
                return null;
            }
        }

        public static bool WriteAtomic(string path, string content, bool ignoreMetadataErrors = false)
        {
            if (content == null || content.Length == 0)
                return WriteAtomic(path, [], ignoreMetadataErrors);
            else
                return WriteAtomic(path, Encoding.UTF8.GetBytes(content), ignoreMetadataErrors);
        }

        public static bool WriteAtomic(string path, byte[] content, bool ignoreMetadataErrors = false)
        {
            string tmp = null;
            try
            {
                tmp = Path.Combine(Path.GetDirectoryName(path), $"{Guid.NewGuid():N}.tmp");
                File.WriteAllBytes(tmp, content);
                File.Move(tmp, path, true);
                tmp = null;
                return true;
            }
            catch (Exception ex)
            {
                TraceMessage.Message(ex.Message);
                return false;
            }
            finally
            {
                if (tmp != null && File.Exists(tmp))
                {
                    try { File.Delete(tmp); }
                    catch (Exception)
                    {
                        // 의도적으로 무시                        
                        // 임시 파일 삭제 실패는, 주된 실패가 아니므로
                        // 잘못된 실패 상태 인지를 방지
                    }
                }
            }
        }

        public static bool RetryFileCreate(string path, string content = null)
        {
            const int MaxRetries = 10;
            const int RetryInterval = 300;

            var i = 0;
            do
            {
                try
                {
                    File.WriteAllText(path, content);
                    return true;
                }
                catch (Exception e)
                {
                    if (File.Exists(path))
                        return true;

                    TraceMessage.Message(e.Message);
                }
                Thread.Sleep(RetryInterval);
            }
            while (++i < MaxRetries);
            return false;
        }

        public static byte[] RetryFileReadBytes(string path)
        {
            const int MaxRetries = 10;
            const int RetryInterval = 300;

            var i = 0;
            do
            {
                try
                {
                    return File.ReadAllBytes(path);
                }
                catch (Exception e)
                {
                    TraceMessage.Message(e.Message);

                    if (File.Exists(path) == false)
                        break;
                }
                Thread.Sleep(RetryInterval);
            }
            while (++i < MaxRetries);
            return null;
        }

        public static string RetryFileReadText(string path)
        {
            const int MaxRetries = 10;
            const int RetryInterval = 300;

            var i = 0;
            do
            {
                try
                {
                    return File.ReadAllText(path);
                }
                catch (Exception e)
                {
                    TraceMessage.Message(e.Message);

                    if (File.Exists(path) == false)
                        break;
                }
                Thread.Sleep(RetryInterval);
            }
            while (++i < MaxRetries);
            return null;
        }

        public static bool RetryFileWrite(string path, params byte[] value)
        {
            const int MaxRetries = 10;
            const int RetryInterval = 300;

            var i = 0;
            do
            {
                try
                {
                    File.WriteAllBytes(path, value);
                    return true;
                }
                catch (Exception e)
                {
                    TraceMessage.Message(e.Message);
                }
                Thread.Sleep(RetryInterval);
            }
            while (++i < MaxRetries);
            return false;
        }

        public static bool RetryFileCopy(string path, string destPath)
        {
            const int MaxRetries = 10;
            const int RetryInterval = 300;

            var i = 0;
            do
            {
                try
                {
                    File.Copy(path, destPath, true);
                    return true;
                }
                catch (Exception e)
                {
                    TraceMessage.Message(e.Message);

                    if (File.Exists(path) == false)
                        return false;
                }
                Thread.Sleep(RetryInterval);
            }
            while (++i < MaxRetries);
            return false;
        }

        public static bool RetryFileDelete(string path)
        {
            const int MaxRetries = 10;
            const int RetryInterval = 300;

            var i = 0;
            do
            {
                try
                {
                    File.Delete(path);
                    return true;
                }
                catch (Exception e)
                {
                    TraceMessage.Message(e.Message);

                    if (File.Exists(path) == false)
                        return true;
                }
                Thread.Sleep(RetryInterval);
            }
            while (++i < MaxRetries);
            return false;
        }


        public static bool Hash64FileWrite(string path, string content)
        {
            if (content != null)
                if (content.Length != 0)
                    return Hash64FileWrite(path, Encoding.UTF8.GetBytes(content).AsSpan());
                else return Hash64FileWrite(path, default(ReadOnlySpan<byte>));
            else return false;
        }

        public static bool Hash64FileWrite(string path, ReadOnlySpan<byte> content)
        {
            var h64 = XXHash.H64(content);
            var dest = new byte[8 + content.Length];
            ref var dest0 = ref MemoryMarshal.GetArrayDataReference(dest);

            Unsafe.As<byte, long>(ref dest0) = h64;
            AC.Copy(
                ref MemoryMarshal.GetReference(content),
                ref Unsafe.AddByteOffset(ref dest0, 8), content.Length);

            return RetryFileWrite(path, dest);
        }


        public static bool Hash64FileMatch(string path, string matchContent)
        {
            if (Hash64FileRead(path, out ReadOnlySpan<byte> body))
                return Encoding.UTF8.GetString(body) == matchContent;
            return false;
        }

        public static bool Hash64FileRead(string path, out string content)
        {
            if (Hash64FileRead(path, out ReadOnlySpan<byte> body))
            {
                content = Encoding.UTF8.GetString(body);
                return true;
            }
            content = null;
            return false;
        }

        public static bool Hash64FileRead(string path, out ReadOnlySpan<byte> content)
        {
            var bytes = RetryFileReadBytes(path).AsSpan();
            if (bytes.Length >= 8)
            {
                var body = bytes[8..];
                var h64 = Unsafe.As<byte, long>(ref MemoryMarshal.GetReference(bytes));
                if (h64 == XXHash.H64(body))
                {
                    content = body;
                    return true;
                }
            }
            content = default;
            return false;
        }


        /// <summary> 디렉토리를 포함한 모든 내부 파일 및 서브 디렉토리 삭제 </summary>
        public static bool DirectoryDelete(string dirPath)
        {
            try
            {
                Directory.Delete(dirPath, true);
                return true;
            }
            catch (Exception e)
            {
                TraceMessage.Message(e.Message);
                return false;
            }
        }


        /// <summary> 복사될 디렉토리 내부의 파일만 정리후, 파일만 복사한다 </summary>
        public static bool DirectoryFileClearCopy(string sourcePath, string destPath) =>
            DirectoryClear(destPath) && DirectoryCopy(sourcePath, destPath);

        /// <summary> 복사될 디렉토리 내부의 파일과 폴더 모두 정리후, 파일과 폴더 모두 복사한다 </summary>
        public static bool DirectoryAllClearCopy(string sourcePath, string destPath) =>
            DirectoryClear(destPath, true) && DirectoryCopy(sourcePath, destPath, true);


        /// <summary> 디렉토리를 정리한다. withSubDirectory의 인자에 따라 서브 폴더까지 포함할지 선택 </summary>
        public static bool DirectoryClear(string directory, bool withSubDirectory = false)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    var dirInfo = new DirectoryInfo(directory);

                    foreach (var infoFile in dirInfo.EnumerateFiles())
                        infoFile.Delete();

                    if (withSubDirectory)
                    {
                        foreach (var infoDir in dirInfo.EnumerateDirectories())
                            infoDir.Delete(true);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                TraceMessage.Message(e.Message);
                return false;
            }
        }

        /// <summary> 디렉토리내의 모든 서브 디렉토리만 삭제한다 </summary>
        public static bool DeleteSubDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    var dirInfo = new DirectoryInfo(directory);
                    foreach (var infoDir in dirInfo.EnumerateDirectories())
                        infoDir.Delete(true);
                }
                return true;
            }
            catch (Exception e)
            {
                TraceMessage.Message(e.Message);
                return false;
            }
        }


        /// <summary> 디렉토리를 복사한다. withSubDirectory의 인자에 따라 서브 폴더까지 포함할지 선택 </summary>
        public static bool DirectoryCopy(string originDirectory, string destDirectory, bool withSubDirectory = false)
        {
            try
            {
                CopyAll(
                    new DirectoryInfo(originDirectory),
                    new DirectoryInfo(destDirectory), withSubDirectory);
                return true;
            }
            catch (Exception e)
            {
                TraceMessage.Message(e.Message);
                return false;
            }
        }

        private static void CopyAll(DirectoryInfo origin, DirectoryInfo dest, bool withSubDirectory)
        {
            var destDirectory = dest.FullName;

            if (Directory.Exists(destDirectory) == false)
                Directory.CreateDirectory(destDirectory);

            foreach (var fileInfo in origin.EnumerateFiles())
            {
                var destPath = Path.Combine(destDirectory, fileInfo.Name);
                fileInfo.CopyTo(destPath, true);
            }

            if (withSubDirectory)
            {
                foreach (var directoryInfo in origin.EnumerateDirectories())
                {
                    var subDirectory = dest.CreateSubdirectory(directoryInfo.Name);
                    CopyAll(directoryInfo, subDirectory, withSubDirectory);
                }
            }
        }

        public static bool ReadAllBytes(string path, int freeSpace, IResizableStream stream)
        {
            try
            {
                if ((uint)freeSpace > (uint)Array.MaxLength)
                    throw new ArgumentOutOfRangeException(nameof(freeSpace), "Invalid free size");

                var options = OperatingSystem.IsWindows() ? FileOptions.SequentialScan : FileOptions.None;
                using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, options);

                long length;
                try { length = RandomAccess.GetLength(handle); }
                catch (Exception) { length = -1; }

                if (length + freeSpace > Array.MaxLength)
                    throw new IOException("File size is too large");

                var offset = 0;
                if (length > 0)
                {
                    stream.EnsureFreeSpace((int)length + freeSpace);
                    do
                    {
                        var read = RandomAccess.Read(handle, stream.WriteSpan(), offset);
                        if (read <= 0) break;
                        stream.Position += read;
                        offset += read;
                    }
                    while (offset < length);
                }
                else if (length < 0)
                {
                    // 길이가 음수면 파일의 길이를 알 수 없는 경우로 판단하여 파일의 끝이 나올때까지 읽어들인다
                    var grow = 1024;
                    var remain = 0;

                    while (true)
                    {
                        if (remain <= 0)
                        {
                            remain = grow *= 2;
                            stream.EnsureFreeSpace(remain + freeSpace);
                        }

                        var read = RandomAccess.Read(handle, stream.WriteSpan(), offset);
                        if (read <= 0) break;
                        stream.Position += read;
                        offset += read;
                        remain -= read;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                return false;
            }
        }

        public static bool WriteAllBytes(string path, ReadOnlySpan<byte> bytes)
        {
            try
            {
                File.WriteAllBytes(path, bytes);
                return true;
            }
            catch (Exception e)
            {
                TraceMessage.Message(e.Message);
                return false;
            }
        }
    }
}