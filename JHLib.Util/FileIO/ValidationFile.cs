using JHLib.Util.Performance;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace JHLib.Util.FileIO
{
    /// <summary>
    /// 파일 읽기,쓰기시 파일 손상을 방지하기 위한 검증파일 기반 읽기 쓰기 클래스<br/>
    /// - 원자적 쓰기: 임시 파일(.tmp)을 통해 원본 파일 깨짐 방지<br/>
    /// - 자동 복구: 검증 파일(.val)을 통해 자동 파일 복구<br/>
    /// [처리 순서]<br/>
    /// 1. 검증 파일(.val) 우선 생성 (복구 기준점 마련)<br/>
    /// 2. 임시 파일(.tmp) 쓰기 -> 원본 파일로 원자적 교체(Move)
    /// </summary>
    public static class ValidationFile
    {
        private const string EXT_VALIDATION = ".__val"; // Validation file extension

        /// <summary> 지정된 경로에 데이타를 저장한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Write(string path, byte[] data) =>
            Write(path, data.AsSpan());

        /// <summary> 지정된 경로에 데이타를 저장한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Write(string path, ReadOnlySpan<byte> data)
        {
            try
            {
                var dirName = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dirName))
                {
                    if (Directory.Exists(dirName) == false)
                        Directory.CreateDirectory(dirName);
                }

                // 검증 파일 생성
                WriteValidationFile(string.Concat(path, EXT_VALIDATION), data);

                // 원본 파일 생성
                FilePrimitive.WriteAtomic(path, data);

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Write failed: {ex.Message}");
                return false;
            }
        }

        /// <summary> 지정된 경로의 데이타를 읽어온다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Read(string path) => File.Exists(path) ? ReadFile(path) : TryRecovery(path);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static byte[] ReadFile(string path)
        {
            var data = FilePrimitive.Read(path);

            var pathval = string.Concat(path, EXT_VALIDATION);
            if (LoadValidationFile(pathval, out var valSpan))
            {
                if (Validation(valSpan.Limit(16), data))
                {
                    valSpan.Return();
                    return data;
                }
                return RecoveryFile(path, valSpan);
            }
            else
            {
                WriteValidationFile(pathval, data);
                return data;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static byte[] TryRecovery(string path)
        {
            var pathval = string.Concat(path, EXT_VALIDATION);
            if (LoadValidationFile(pathval, out var valSpan))
                return RecoveryFile(path, valSpan);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool LoadValidationFile(string pathval, out BufferPoolSpan valSpan)
        {
            if (File.Exists(pathval))
            {
                var span = FilePrimitive.ReadBufferPoolSpan(pathval);
                if (span.Length >= 16 && Validation(span.Limit(16), span.Slice(16)))
                {
                    valSpan = span;
                    return true;
                }
                span.Return();
            }
            Unsafe.SkipInit(out valSpan);
            return false;
        }

        /// <summary> 데이터 본문 복원 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static byte[] RecoveryFile(string path, BufferPoolSpan valSpan)
        {
            var dataSpan = valSpan.Slice(16);
            FilePrimitive.WriteAtomic(path, dataSpan);
            var data = dataSpan.ToArray();
            valSpan.Return();
            return data;
        }

        /// <summary> 검증 파일(해시(16바이트) + 데이터 본문) 생성 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WriteValidationFile(string pathval, ReadOnlySpan<byte> data)
        {
            var len = 16 + data.Length;
            var buffer = BufferPool.Rent(len);

            XxHash128.Hash(data, buffer.Limit(16));
            data.CopyTo(buffer.Slice(16));

            FilePrimitive.WriteAtomic(pathval, buffer.Limit(len));
            buffer.Return();
        }

        /// <summary> 해시와 실제 데이터가 일치하는지 확인 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Validation(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> data) =>
            BinaryPrimitives.ReadUInt128BigEndian(hash) == XxHash128.HashToUInt128(data);
    }
}