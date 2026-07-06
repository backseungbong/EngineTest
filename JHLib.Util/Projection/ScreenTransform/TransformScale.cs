using System.Runtime.CompilerServices;

namespace JHLib.Util.Projection.ScreenTransform
{
    public static class TransformScale
    {
        public const int DEFAULT_SCALE_INDEX = 12;
        public static ReadOnlySpan<double> Scales =>
        [
            20000000,
            15000000,
            12500000,
            10000000,
            7500000,
            6000000,
            5000000,
            4000000,
            3000000,
            2500000,
            2000000,
            1500000,
            1250000,
            1000000,
            750000,
            600000,
            500000,
            400000,
            300000,
            250000,
            200000,
            150000,
            125000,
            100000,
            75000,
            60000,
            50000,
            40000,
            30000,
            25000,
            20000,
            15000,
            12500,
            10000,
            7500,
            6000,
            5000,
            4000,
            3000,
            2500,
            2000,
            1500,
            1250,
            1000
        ];

        public static double SCALE_MAX { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Scales[0]; }
        public static double SCALE_MIN { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Scales[^1]; }
        public static double SCALE_DEFAULT { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Scales[DEFAULT_SCALE_INDEX]; }

        public static double ClampScale(double scale)
        {
            double s;
            if (SCALE_MIN <= scale)
            {
                if (scale <= SCALE_MAX)
                {
                    s = scale;
                }
                else
                {
                    s = SCALE_MAX;
                }
            }
            else if (double.IsFinite(scale))
            {
                s = SCALE_MIN;
            }
            else
            {
                s = Scales[DEFAULT_SCALE_INDEX];
            }
            return s;
        }

        public static int ClampScaleIndex(int index)
        {
            var i = 0;
            if (index > 0)
            {
                if (index < Scales.Length)
                    i = index;
                else
                    i = Scales.Length - 1;
            }
            return i;
        }

        public static bool CheckValidScale(double scale) => SCALE_MIN <= scale && scale <= SCALE_MAX;
        public static double GetScaleFromPercent(double percent)
        {
            var b = Scales;
            var l = b.Length - 1;
            var i = l * percent / 100;

            var i1 = (int)i;
            var i2 = i1 + 1;
            if (i1 >= 0 && i2 <= l)
            {
                var t = i - i1;
                return b[i1] * (1 - t) + b[i2] * t;
            }
            return b[^1];
        }

        public static double GetPercentFromScale(double scale)
        {
            var b = Scales;
            if (scale < b[0])
            {
                if (scale > b[^1])
                {
                    for (var i = 1; i < b.Length; i++)
                    {
                        if (scale > b[i])
                        {
                            var l = b.Length - 1;
                            var s1 = b[i - 1];
                            var s2 = b[i];
                            var r1 = (double)(i - 1) / l * 100;
                            var r2 = (double)i / l * 100;

                            var t = 1 - (scale - s2) / (s1 - s2);
                            return t * (r2 - r1) + r1;
                        }
                    }
                }
                return 100;
            }
            return 0;
        }
    }
}