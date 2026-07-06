using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public struct Int2D(int x, int y) : IEquatable<Int2D>
    {
        public int X = x;
        public int Y = y;
        public readonly ulong AsUInt64() => Unsafe.As<Int2D, ulong>(ref Unsafe.AsRef(in this));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Int2D front, Int2D end) => front.AsUInt64() == end.AsUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Int2D front, Int2D end) => front.AsUInt64() != end.AsUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int2D operator +(Int2D front, Int2D end) => new(front.X + end.X, front.Y + end.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int2D operator -(Int2D front, Int2D end) => new(front.X - end.X, front.Y - end.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int2D operator *(Int2D front, int mul) => new(front.X * mul, front.Y * mul);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int2D operator /(Int2D front, int div) => new(front.X / div, front.Y / div);

        public readonly bool Equals(Int2D other) => AsUInt64() == other.AsUInt64();
        public override readonly bool Equals(object obj) => obj is Int2D d && Equals(d);
        public override readonly int GetHashCode() => X ^ Y;
    }
}