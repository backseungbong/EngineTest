using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Message
{
    public static class TraceMessage
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Message(string message, string className = "", [CallerMemberName] string callerName = "") =>
            Trace.WriteLine($"{message}, ClassName : {className}, CallerName : {callerName}");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void InvalidPath(string path, string className = "", [CallerMemberName] string callerName = "") =>
            Trace.WriteLine($"Invalid Path : {path}, ClassName : {className}, CallerName : {callerName}");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void InvalidCommand(string command, string className = "", [CallerMemberName] string callerName = "") =>
            Trace.WriteLine($"Invalid Command : {command}, ClassName : {className}, CallerName : {callerName}");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DirectoryNotFound(string path, string className = "", [CallerMemberName] string callerName = "") =>
            Trace.WriteLine($"Directory Not Found : {path}, ClassName : {className}, CallerName : {callerName}");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void FileNotFound(string path, string className = "", [CallerMemberName] string callerName = "") =>
            Trace.WriteLine($"File Not Found : {path}, ClassName : {className}, CallerName : {callerName}");


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void NotSupported(string message, string className = "", [CallerMemberName] string callerName = "") =>
            Trace.WriteLine($"NotSupported : {message}, ClassName : {className}, CallerName : {callerName}");
    }
}