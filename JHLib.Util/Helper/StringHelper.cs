using JHLib.Util.ArrayControl;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Helper
{
    public static class StringHelper
    {
        public static ReadOnlySpan<char> RemoveWhitespace(ReadOnlySpan<char> input)
        {
            var l = input.Length;
            if (l != 0)
            {
                var i = 0;
                ref var s = ref MemoryMarshal.GetReference(input);
                do if (char.IsWhiteSpace(Unsafe.Add(ref s, i))) break;
                while (++i < l);

                if (i < l)
                {
                    ref var d = ref MemoryMarshal.GetArrayDataReference(GC.AllocateUninitializedArray<char>(l));
                    AC.Copy(ref s, ref d, i);

                    var t = i;
                    if (++i < l)
                    {
                        do
                        {
                            if (char.IsWhiteSpace(Unsafe.Add(ref s, i)) == false)
                                Unsafe.Add(ref d, t++) = Unsafe.Add(ref s, i);
                        }
                        while (++i < l);
                    }
                    return MemoryMarshal.CreateReadOnlySpan(ref d, t);
                }
            }
            return input;
        }
    }
}