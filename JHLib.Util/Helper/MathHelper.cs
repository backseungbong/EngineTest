using System.Numerics;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Helper
{
    public static class MathHelper
    {
        private static ReadOnlySpan<ulong> DigitTable =>
        [
            04294967296, 08589934582, 08589934582, 08589934582, 12884901788,
            12884901788, 12884901788, 17179868184, 17179868184, 17179868184,
            21474826480, 21474826480, 21474826480, 21474826480, 25769703776,
            25769703776, 25769703776, 30063771072, 30063771072, 30063771072,
            34349738368, 34349738368, 34349738368, 34349738368, 38554705664,
            38554705664, 38554705664, 41949672960, 41949672960, 41949672960,
            42949672960, 42949672960
        ];

        /// <summary> 
        /// 부호없는 32비트 숫자의 자릿수 반환, 조건분기없는 빠른 계산을 위해 작성됨
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDigits(uint x) =>
            (int)(x + DigitTable[31 - BitOperations.LeadingZeroCount(x | 1)] >> 32);

        /// <summary> 
        /// 32비트 정수형 절대값 계산. Math.Abs의 경우 조건분기를 기반 하므로<para/>
        /// 조건분기없는 빠른 절대값 계산에 사용
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Abs(int v) => (uint)((v ^ v >> 31) - (v >> 31));


        /// <summary> 주어진 값을 min과 max 사이의 값으로 제한한다 </summary>
        /// <param name="val">제한할 값</param>
        /// <param name="min">최소값</param>
        /// <param name="max">최대값</param>
        /// <param name="defaultValue">값이 유효하지 않을 경우(NaN) 반환할 기본값</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Clamp<T>(T val, T min, T max, T defaultValue) where T : INumber<T>
        {
            if (val > min)
            {
                if (val < max)
                    return val;
                else
                    return max;
            }
            else if (!T.IsNaN(val)) // 어셈블리 결과상 !IsNaN으로 처리하면 더 효율적인 명령어로 출력됨
                return min;
            else
                return defaultValue;
        }


        /// <summary> 주어진 값을 min부터 max미만의 값으로 순환시킨다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Cycle<T>(T val, T min, T max) where T : INumber<T> => Cycle(val, min, max, min);

        /// <summary> 주어진 값을 min부터 max미만의 값으로 순환시킨다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Cycle<T>(T val, T min, T max, T failValue) where T : INumber<T> =>
            min <= val && val < max ? val : CycleInternal(val, min, max, failValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T CycleInternal<T>(T val, T min, T max, T failValue) where T : INumber<T>
        {
            var rng = max - min;
            var mod = (val - min) % rng;

            if (T.IsFinite(mod))
            {
                if (mod < T.Zero) mod += rng;
                return mod + min;
            }
            return failValue;
        }

        /// <summary>
        /// <paramref name="value"/>와 같거나 큰 2의 제곱수를 반환<para/>
        /// 단, 결과가 <paramref name="floor"/> 미만이면 <paramref name="floor"/>를 그대로 반환한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundUpToPow2(int floor, int value)
        {
            if (floor < value)
                floor = (int)BitOperations.RoundUpToPowerOf2((uint)value);
            return floor;
        }



        /// <summary>
        /// 2차원 벡터를 방위각(0 ~ 360도)으로 변환 <br/>
        /// 벡터가 정북일때 0도로 기준으로 시계 방향(CW)으로 각도가 증가 <br/>
        /// 정북예시) (dx, dy) = (0, 1)
        /// </summary>
        /// <param name="dx">X 벡터</param>
        /// <param name="dy">Y 벡터</param>
        /// <returns>0에서 360 사이의 근사 각도 (Degree)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Vec2DegNorthClockwise(double dx, double dy) => Vec2Deg(dy, dx);

        /// <summary>
        /// 2차원 벡터를 방위각(0 ~ 360도)으로 변환 <br/>
        /// 벡터가 정북일때 0도로 기준으로 시계 방향(CW)으로 각도가 증가 <br/>
        /// 정북예시) (dx, dy) = (0, 1)
        /// </summary>
        /// <param name="dx">X 벡터</param>
        /// <param name="dy">Y 벡터</param>
        /// <returns>0에서 360 사이의 근사 각도 (Degree)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Vec2DegNorthClockwise(float dx, float dy) => Vec2Deg(dy, dx);

        /// <summary>
        /// 2차원 벡터를 방위각(0 ~ 360도)으로 변환 <br/>
        /// 벡터가 동쪽일때 0도로 기준으로 반시계 방향(CCW)으로 각도가 증가 <br/>
        /// 동쪽예시) (dx, dy) = (1, 0)
        /// </summary>
        /// <param name="dx">X 벡터</param>
        /// <param name="dy">Y 벡터</param>
        /// <returns>0에서 360 사이의 근사 각도 (Degree)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Vec2Deg(double dx, double dy) => Vec2Deg((float)dx, (float)dy);

        /// <summary>
        /// 2차원 벡터를 방위각(0 ~ 360도)으로 변환 <br/>
        /// 벡터가 동쪽일때 0도로 기준으로 반시계 방향(CCW)으로 각도가 증가 <br/>
        /// 동쪽예시) (dx, dy) = (1, 0)
        /// </summary>
        /// <param name="dx">X 벡터</param>
        /// <param name="dy">Y 벡터</param>
        /// <returns>0에서 360 사이의 근사 각도 (Degree)</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Vec2Deg(float dx, float dy)
        {
            var ay = MathF.Abs(dy);
            if (ay > 0 == false) // >, false 연산이 어셈블리 라인이 줄어들고 NaN도 잡을수 있음
                return dx < 0 ? 180 : 0;

            var ax = MathF.Abs(dx);
            if (ax > 0 == false) // >, false 연산이 어셈블리 라인이 줄어들고 NaN도 잡을수 있음
                return dy < 0 ? 270 : 90;

            if (ax > ay)
            {
                var basedeg = dx >= 0 ? dy >= 0 ? 0 : 360 : 180;
                return basedeg + FastAtanDegrees(dy / dx);
            }
            else
            {
                var basedeg = dy >= 0 ? 90 : 270;
                return basedeg - FastAtanDegrees(dx / dy);
            }
        }

        // Polynomial approximating arctangenet on the range -1,1.
        // Max error < 0.005 (or 0.29 degrees)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FastAtanDegrees(float z)
        {
            const double RAD2DEG = 180 / Math.PI;

            // 미리 Degree로 변환된 상수값을 사용하여 이후 계산단위를 Degree로 유지            
            const float N1 = (float)(0.97239411 * RAD2DEG);
            const float N2 = (float)(-0.19194795 * RAD2DEG);

            return (N1 + N2 * z * z) * z;
        }
    }
}