using JHLib.Util.Performance;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Hash
{
    /// <summary>
    /// 매우 빠른 SIMD 기반 해시맵, 아래와 같은 경우에 사용하는것을 권장<br/>
    /// 1. 아이템이 추가만되고 장시간 재사용하는경우<br/>
    /// 2. 메모리 사용량이 크게 중요하지 않고, 갯수가 많지 않은경우 (추가될 아이템의 2배 버킷 사용)<br/>
    /// 테스트 진행후 적용하는것을 권장, 키값은 int32형만 지원됨 (0의 키값은 지원X)<br/>
    /// TryGet : 스레드 안전하므로 여러 쓰레드에서 동시에 읽기 가능<br/>
    /// TryAdd : 스레드 안전하지 않으므로 멀티 스레드 환경에서는 외부에서 동기화(Lock)가 보장되어야 함
    /// </summary>
    public unsafe class SimdKeyValueMap<T>
    {
        private readonly T[] _values;
        private readonly byte[] _buffer;
        private readonly uint* _key0;
        private uint _mask;
        private int _free;
        public bool IsFull => _free == 0;
        public SimdKeyValueMap() : this(4) { }
        public SimdKeyValueMap(int capacity)
        {
            var cap = capacity > 4 ? (int)BitOperations.RoundUpToPowerOf2((uint)capacity) : 4;
            var pb = new PinnedBuffer(cap * 2 * sizeof(uint), 64, true);

            _values = new T[cap * 2];
            _buffer = pb.Buffer;
            _key0 = (uint*)pb.Pointer;
            _mask = Avx2.IsSupported ? (uint)(cap - 4) << 1 : (uint)(cap - 2) << 1;
            _free = cap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T GetValue(uint i) =>
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_values), i);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(int key, T val) => TryAdd((uint)key, val);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool TryAdd(uint key, T val)
        {
            if (_free == 0)
                return false;

            _free--;
            var i = Mix(key) & Volatile.Read(ref _mask);

            if (Avx2.IsSupported)
            {
                var k4 = Vector256<uint>.Zero;
            RE: var l4 = Vector256.LoadAligned(_key0 + i);
                var eq = Avx2.CompareEqual(k4, l4);
                var mm = Avx.MoveMask(eq.AsSingle());
                if (mm == 0) { i = i + 8 & _mask; goto RE; }
                i += (uint)BitOperations.TrailingZeroCount(mm);
                GetValue(i) = val;
                Volatile.Write(ref *(_key0 + i), key);
                return true;
            }
            else if (Sse2.IsSupported)
            {
                var k4 = Vector128<uint>.Zero;
            RE: var l4 = Vector128.LoadAligned(_key0 + i);
                var eq = Sse2.CompareEqual(k4, l4);
                var mm = Sse.MoveMask(eq.AsSingle());
                if (mm == 0) { i = i + 4 & _mask; goto RE; }
                i += (uint)BitOperations.TrailingZeroCount(mm);
                GetValue(i) = val;
                Volatile.Write(ref *(_key0 + i), key);
                return true;
            }
            else
            {
            RE: var k = _key0 + i;
                if (k[0] != 0)
                {
                    if (k[1] != 0)
                    {
                        if (k[2] != 0)
                        {
                            if (k[3] != 0)
                            {
                                i = i + 4 & _mask; goto RE;
                            }
                            i++;
                        }
                        i++;
                    }
                    i++;
                }
                GetValue(i) = val;
                Volatile.Write(ref *(_key0 + i), key);
                return true;
            }
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetIndex(uint key, out uint idx)
        {
            Unsafe.SkipInit(out idx);
            var i = Mix(key) & Volatile.Read(ref _mask);

            if (Avx2.IsSupported)
            {
                var k4 = Vector256.Create(key);
            RE: var l4 = Vector256.LoadAligned(_key0 + i);
                var eq = Avx2.CompareEqual(k4, l4);
                var mm = Avx.MoveMask(eq.AsSingle());
                if (mm == 0)
                {
                    if (*(_key0 + i + 7) == 0) { return false; }
                    i = i + 8 & _mask; goto RE;
                }
                idx = i + (uint)BitOperations.TrailingZeroCount(mm);
                return true;
            }
            else if (Sse2.IsSupported)
            {
                var k4 = Vector128.Create(key);
            RE: var l4 = Vector128.LoadAligned(_key0 + i);
                var eq = Sse2.CompareEqual(k4, l4);
                var mm = Sse.MoveMask(eq.AsSingle());
                if (mm == 0)
                {
                    if (*(_key0 + i + 3) == 0) { return false; }
                    i = i + 4 & _mask; goto RE;
                }
                idx = i + (uint)BitOperations.TrailingZeroCount(mm);
                return true;
            }
            else
            {
            RE: var k = _key0 + i;
                if (k[0] != key)
                {
                    if (k[1] != key)
                    {
                        if (k[2] != key)
                        {
                            if (k[3] != key)
                            {
                                if (k[3] == 0) { return false; }
                                i = i + 4 & _mask; goto RE;
                            }
                            i++;
                        }
                        i++;
                    }
                    i++;
                }
                idx = i;
                return true;
            }
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(int key, out T val) => TryGet((uint)key, out val);

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(uint key, out T val)
        {
            Unsafe.SkipInit(out val);
            if (TryGetIndex(key, out var idx))
            {
                val = GetValue(idx);
                return true;
            }
            return false;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUpdate(int key, in T val) => TryUpdate((uint)key, val);

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUpdate(uint key, in T val)
        {
            if (TryGetIndex(key, out var idx))
            {
                GetValue(idx) = val;
                return true;
            }
            return false;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetOrUpsize(uint key, out T val, ref SimdKeyValueMap<T> upsizeMap) =>
            TryGetOrUpsize((int)key, out val, ref upsizeMap);

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool TryGetOrUpsize(int key, out T val, ref SimdKeyValueMap<T> upsizeMap)
        {
            if (TryGet(key, out val)) return true;
            if (_free == 0) upsizeMap = Upsize();
            return false;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUpdateOrUpsize(uint key, in T val, ref SimdKeyValueMap<T> upsizeMap) =>
            TryUpdateOrUpsize((int)key, in val, ref upsizeMap);

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool TryUpdateOrUpsize(int key, in T val, ref SimdKeyValueMap<T> upsizeMap)
        {
            if (TryUpdate(key, in val)) return true;
            if (_free == 0) upsizeMap = Upsize();
            return false;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private SimdKeyValueMap<T> Upsize()
        {
            var values = _values;
            var newmap = new SimdKeyValueMap<T>(values.Length);

            ref var value0 = ref MemoryMarshal.GetArrayDataReference(values);
            var key0 = _key0;

            var i = 0u;
            do
            {
                var key = key0[i];
                if (key != 0) newmap.TryAdd(key, Unsafe.Add(ref value0, i));
            }
            while (++i < (uint)values.Length);
            return newmap;
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