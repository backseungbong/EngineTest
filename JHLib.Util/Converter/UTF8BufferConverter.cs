using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace JHLib.Util.Converter
{
    /// <summary>
    /// utf8기반 문자열을 bytes(UTF8) 혹은 string(UTF16)형태로 변환하는 클래스<para/>
    /// string(UTF16)->bytes(UTF8) 혹은 bytes(UTF8)->string(UTF16)으로 변환이 자주 일어나며<para/>
    /// 이 인스턴스를 여러번 재사용되는 상황에서 효율적으로 사용가능하다    
    /// 내부 버퍼를 사용하므로 다중 쓰레드에서는 각각의 인스턴스를 사용해야 한다
    /// </summary>
    public class UTF8BufferConverter
    {
        private byte[] _buffer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> Convert(string text) => Convert(text.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> Convert(ReadOnlySpan<char> text)
        {
            if (text.Length != 0)
            {
                var b = _buffer;
                if (b != null)
                {
                    ref var b0 = ref MemoryMarshal.GetArrayDataReference(b);
                    if (Encoding.UTF8.TryGetBytes(text, MemoryMarshal.CreateSpan(ref b0, b.Length), out var w))
                        return MemoryMarshal.CreateReadOnlySpan(ref b0, w);
                }
                return ConvertResize(text);
            }
            return default;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ReadOnlySpan<byte> ConvertResize(ReadOnlySpan<char> text)
        {
            var b = new byte[MinPow2(64, Encoding.UTF8.GetByteCount(text))];
            _buffer = b;

            ref var b0 = ref MemoryMarshal.GetArrayDataReference(b);
            Encoding.UTF8.TryGetBytes(text, MemoryMarshal.CreateSpan(ref b0, b.Length), out var w);
            return MemoryMarshal.CreateReadOnlySpan(ref b0, w);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> Convert(byte[] bytes) => Convert(bytes.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> Convert(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 0)
            {
                var b = _buffer;
                if (b != null)
                {
                    ref var c0 = ref Unsafe.As<byte, char>(ref MemoryMarshal.GetArrayDataReference(b));
                    if (Encoding.UTF8.TryGetChars(bytes, MemoryMarshal.CreateSpan(ref c0, b.Length >> 1), out var w))
                        return MemoryMarshal.CreateReadOnlySpan(ref c0, w);
                }
                return ConvertResize(bytes);
            }
            return default;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ReadOnlySpan<char> ConvertResize(ReadOnlySpan<byte> bytes)
        {
            var b = new byte[MinPow2(64, Encoding.UTF8.GetCharCount(bytes)) << 1];
            _buffer = b;

            ref var c0 = ref Unsafe.As<byte, char>(ref MemoryMarshal.GetArrayDataReference(b));
            Encoding.UTF8.TryGetChars(bytes, MemoryMarshal.CreateSpan(ref c0, b.Length >> 1), out var w);
            return MemoryMarshal.CreateReadOnlySpan(ref c0, w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MinPow2(int minValue, int value)
        {
            if (value > minValue)
                return (int)BitOperations.RoundUpToPowerOf2((uint)value);
            else
                return minValue;
        }
    }
}