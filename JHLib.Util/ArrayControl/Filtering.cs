using JHLib.Util.Struct;
using System.Runtime.InteropServices;

namespace JHLib.Util.ArrayControl
{
    public static class Filtering
    {
        /// <summary>
        /// 두 번의 EMA(Forward → Backward)로 위상 지연(Phase shift)을 상쇄한 저지연 스무딩 필터
        /// </summary>
        /// <param name="alpha"> 0~1사이의 값, 1에 가까울수록 빠르게 따라가고, 0에 가까울수록 완만해진다 </param>
        public static unsafe void SmoothEMAZeroPhase(Span<Float2D> src, Span<Double2D> dst, double alpha = 0.3d)
        {
            fixed (Float2D* s0 = &MemoryMarshal.GetReference(src))
            fixed (Double2D* d0 = &MemoryMarshal.GetReference(dst))
            {
                var n = Math.Min(src.Length, dst.Length);
                if (n >= 3)
                {
                    var alphai = 1 - alpha;
                    var sp = s0;
                    var dp = d0;
                    var de = d0 + n - 1;

                    // Forward EMA 
                    // 첫 샘플은 지연을 피하기 위해 그대로 복사
                    var x = (double)sp->X;
                    var y = (double)sp->Y;
                    dp->X = x;
                    dp->Y = y;
                    dp++; sp++;
                    do
                    {
                        x = alpha * sp->X + alphai * x;
                        y = alpha * sp->Y + alphai * y;
                        dp->X = x;
                        dp->Y = y;
                        dp++; sp++;
                    }
                    while (dp < de);

                    // Backward EMA (역방향으로 같은 EMA 적용)
                    // Backward EMA 끝나면 위상 지연이 상쇄되어 'Zero-phase'가 됨
                    x = sp->X;
                    y = sp->Y;
                    dp->X = x; // 마지막 값을 백워드 EMA의 초기값으로
                    dp->Y = y;
                    dp--;
                    do
                    {
                        x = alpha * dp->X + alphai * x;
                        y = alpha * dp->Y + alphai * y;
                        dp->X = x;
                        dp->Y = y;
                        dp--;
                    }
                    while (dp > d0);
                }
                else if (n >= 1)
                {
                    d0[0] = s0[0].ToDouble2D();
                    d0[n - 1] = s0[n - 1].ToDouble2D();
                }
                return;
            }
        }
    }
}