using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential, Pack = 08, Size = 08)] public readonly struct B08 { }
    [StructLayout(LayoutKind.Sequential, Pack = 16, Size = 16)] public readonly struct B16 { }
    [StructLayout(LayoutKind.Sequential, Pack = 32, Size = 32)] public readonly struct B32 { }
    [StructLayout(LayoutKind.Sequential, Pack = 64, Size = 64)] public readonly struct B64 { }
}