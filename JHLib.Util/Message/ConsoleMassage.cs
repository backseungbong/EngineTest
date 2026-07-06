using JHLib.Util.ByteControl;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Message
{
    public static class ConsoleMassage
    {
        public static void Error(string info,
            string className, [CallerMemberName] string methodName = "") =>
            System.Console.WriteLine($"{info} {className}.{methodName}");

        public static void IndexOutOfRange(int range,
            string className, [CallerMemberName] string methodName = "") =>
            System.Console.WriteLine($"[IndexOutOfRange > {range}] {className}.{methodName}");

        public static void NotSupported(string info,
            string className, [CallerMemberName] string methodName = "") =>
            System.Console.WriteLine($"[NotSupported] {info} {className}.{methodName}");

        public static void Invalid(string info,
            string className, [CallerMemberName] string methodName = "") =>
            System.Console.WriteLine($"[Invalid] {info} {className}.{methodName}");

        public static void FileNotFound(
            string path, string className, [CallerMemberName] string methodName = "") =>
            System.Console.WriteLine($"[FileNotFound] {path} {className}.{methodName}");

        public static void FileInvalid(string info,
            string path, string className, [CallerMemberName] string methodName = "") =>
            System.Console.WriteLine($"[FileInvalid : {info}] {path} {className}.{methodName}");

        public static void FileIOError(string info,
            string path, string className, [CallerMemberName] string methodName = "") =>
            System.Console.WriteLine($"[FileIOError : {info}] {path} {className}.{methodName}");

        public static void FileTooLarge(long byteLimit,
            string path, string className, [CallerMemberName] string methodName = "") =>
            System.Console.WriteLine($"[FileLengthTooLarge > {ByteSize.ToReadable(byteLimit)}] {path} {className}.{methodName}");
    }
}