using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Long2D : IEquatable<Long2D>
    {
        public long X;
        public long Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Long2D(long x, long y) { X = x; Y = y; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Long2D a, in Long2D b) => a.X == b.X && a.Y == b.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Long2D a, in Long2D b) => a.X != b.X || a.Y != b.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Long2D operator +(in Long2D a, in Long2D b) => new(a.X + b.X, a.Y + b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Long2D operator -(in Long2D a, in Long2D b) => new(a.X - b.X, a.Y - b.Y);

        public readonly bool Equals(Long2D other) => X == other.X && Y == other.Y;
        public override readonly bool Equals(object obj) => obj is Long2D d && Equals(d);
        public override readonly int GetHashCode() => HashCode.Combine(X, Y);
    }
}