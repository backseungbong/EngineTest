using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Hash
{
    /// <summary>
    /// 매우 빠른 SIMD 기반 키맵(0은 사용 불가)<br/>
    /// 아이템의 갯수가 이미 정해져있고(추가만가능), 함수 내에서 빠르게 키맵을 생성하고 폐기되는경우에 권장<br/>
    /// 멀티스레드 처리 지원하지 않음
    /// </summary>
    public class SimdKeyMap
    {
        private uint[] _keys;
        private uint _mask;
        private uint _free;
        public SimdKeyMap() : this(4) { }
        public SimdKeyMap(int capacity)
        {
            var cap = capacity > 8 ? BitOperations.RoundUpToPowerOf2((uint)capacity) : 8;
            _keys = new uint[cap];
            _mask = cap - 1;
            _free = cap;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool Resize(uint addkey)
        {
            if (addkey == 0)
                return false;

            var keys = _keys;
            var newcap = (uint)(keys.Length * 2);
            _keys = new uint[newcap];
            _mask = newcap - 1;
            _free = newcap;

            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (key != 0) Add(key);
            }
            Add(addkey);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int key) => Add((uint)key);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Add(uint key)
        {
            if (_free == 0) { Resize(key); return; }

            _free--;
            ref var key0 = ref MemoryMarshal.GetArrayDataReference(_keys);
            var i = Mix(key) & _mask;

            if (Avx2.IsSupported)
            {
                var k4 = Vector256<uint>.Zero;
            RE: var l4 = Vector256.LoadUnsafe(ref Unsafe.Add(ref key0, i));
                var eq = Avx2.CompareEqual(k4, l4);
                var mm = Avx.MoveMask(eq.AsSingle());
                if (mm == 0) { i = i + 8 & _mask; goto RE; }
                i += (uint)BitOperations.TrailingZeroCount(mm);
                Unsafe.Add(ref key0, i) = key;
            }
            else if (Sse2.IsSupported)
            {
                var k4 = Vector128<uint>.Zero;
            RE: var l4 = Vector128.LoadUnsafe(ref Unsafe.Add(ref key0, i));
                var eq = Sse2.CompareEqual(k4, l4);
                var mm = Sse.MoveMask(eq.AsSingle());
                if (mm == 0) { i = i + 4 & _mask; goto RE; }
                i += (uint)BitOperations.TrailingZeroCount(mm);
                Unsafe.Add(ref key0, i) = key;
            }
            else
            {
            RE: ref var k = ref Unsafe.Add(ref key0, i);
                if (Unsafe.Add(ref k, 0) != 0)
                {
                    if (Unsafe.Add(ref k, 1) != 0)
                    {
                        if (Unsafe.Add(ref k, 2) != 0)
                        {
                            if (Unsafe.Add(ref k, 3) != 0)
                            {
                                i = i + 4 & _mask; goto RE;
                            }
                            i++;
                        }
                        i++;
                    }
                    i++;
                }
                Unsafe.Add(ref key0, i) = key;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsExists(uint key)
        {
            ref var key0 = ref MemoryMarshal.GetArrayDataReference(_keys);
            var i = Mix(key) & _mask;

            if (Avx2.IsSupported)
            {
                var k4 = Vector256.Create(key);
            RE: var l4 = Vector256.LoadUnsafe(ref Unsafe.Add(ref key0, i));
                var eq = Avx2.CompareEqual(k4, l4);
                var mm = Avx.MoveMask(eq.AsSingle());
                if (mm != 0) return true;
                if (Unsafe.Add(ref key0, i + 7) == 0) return false;
                i = i + 8 & _mask; goto RE;
            }
            else if (Sse2.IsSupported)
            {
                var k4 = Vector128.Create(key);
            RE: var l4 = Vector128.LoadUnsafe(ref Unsafe.Add(ref key0, i));
                var eq = Sse2.CompareEqual(k4, l4);
                var mm = Sse.MoveMask(eq.AsSingle());
                if (mm != 0) return true;
                if (Unsafe.Add(ref key0, i + 3) == 0) return false;
                i = i + 4 & _mask; goto RE;
            }
            else
            {
            RE: ref var k = ref Unsafe.Add(ref key0, i);
                if (Unsafe.Add(ref k, 0) == key) return true;
                if (Unsafe.Add(ref k, 1) == key) return true;
                if (Unsafe.Add(ref k, 2) == key) return true;
                if (Unsafe.Add(ref k, 3) == key) return true;
                if (Unsafe.Add(ref k, 3) == 0) return false;
                i = i + 4 & _mask; goto RE;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Mix(uint x)
        {
            if (Sse42.IsSupported)
                return Sse42.Crc32(0, x);

            // 2026.1 기준으로 가장 낮은 편향 해시 함수
            // https://github.com/skeeto/hash-prospector/issues/19 
            // score: 0.10734781817103507     
            x ^= x >> 16;
            x *= 0x21f0aaad;
            x ^= x >> 15;
            x *= 0xf35a2d97;
            x ^= x >> 15;
            return x;
        }
    }
}