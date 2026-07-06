using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Sort
{
    /// <summary> 슬라이딩 윈도우 중앙값, 용량(capacity)은 반드시 5 이상의 홀수만 가능 </summary>
    public unsafe class RollingMedianIQR
    {
        private readonly uint[] _buk;
        private readonly uint _cap;
        private readonly uint _iq1;
        private readonly uint _iq3;
        private uint _idx;

        /// <summary> 중앙값 </summary>
        public uint Median { get; private set; }

        /// <summary>제1사분위수 (25th percentile)</summary>
        public uint Q1 { get; private set; }

        /// <summary>제3사분위수 (75th percentile)</summary>
        public uint Q3 { get; private set; }

        /// <summary>최종 Push 값이 IQR [Q1, Q3] 범위 내에 있는지 여부</summary>
        public bool IsInlier { get; private set; }

        /// <param name="capacity">슬라이딩 윈도우의 크기, 5 이상의 홀수로 지정</param>
        public RollingMedianIQR(int capacity = 5)
        {
            var cap = capacity | 1;
            if (cap < 5) cap = 5;
            _buk = new uint[cap * 2];
            _cap = (uint)cap;
            _iq1 = (uint)cap / 4;
            _iq3 = (uint)((long)cap * 3 / 4);
            _idx = uint.MaxValue;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Push(uint add)
        {
            fixed (uint* buk = &MemoryMarshal.GetArrayDataReference(_buk))
            {
                var cap = _cap;
                var idx = _idx; _idx = (idx + 1) % cap;
                if (idx == uint.MaxValue) { Init(add); return; }

                var rmv = buk[idx]; buk[idx] = add;
                if (rmv == add)
                    return;

                // 바이너리 방식으로 삭제될 아이템 인덱스 탐색
                // 중복값의 경우 가장 왼쪽의 인덱스로 귀결 (lower_bound 탐색)               
                var l = 0u;
                var h = cap - 1;
                var d = buk + cap;
                do
                {
                    var m = (l + h) >> 1;
                    if (d[m] < rmv) l = m + 1;
                    else h = m;
                }
                while (l < h);

                var t = d + l;
                if (add > rmv) // 추가값과 삭제값의 크기에따라 아이템 시프트 방향 분기
                {
                    if (l < cap - 1)
                    {
                        do if (*(t + 1) < add) { *t = *(t + 1); } else { break; }
                        while (++t < d + (cap - 1));
                    }
                }
                else
                {
                    if (l > 0)
                    {
                        do if (*(t - 1) > add) { *t = *(t - 1); } else { break; }
                        while (--t > d);
                    }
                }
                *t = add; // 최종 위치에 삽입

                Median = d[cap >> 1];
                Q1 = d[_iq1];
                Q3 = d[_iq3];
                IsInlier = add >= Q1 && add <= Q3;
                return;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Init(uint add)
        {
            var buk = _buk;
            for (var i = 0; i < buk.Length; i++)
                buk[i] = add;

            Median = add;
            Q1 = add;
            Q3 = add;
            IsInlier = true;
        }
    }
}