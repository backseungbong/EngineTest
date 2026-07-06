namespace JHLib.Util.Converter
{
    public enum EnumDistanceUnit
    {
        NM, km, m, kyd, yd
    }

    public static class DistanceConverter
    {
        public static float NMTokm = 1.852f;
        public static float NMTom = 1852f;
        public static float NMTokyd = 2.02537183f;
        public static float NMToyd = 2025.37183f;

        public static EnumDistanceUnit DefaultDistanceUnit = EnumDistanceUnit.NM;

        // 현재 설정된 Distance Unit 설정
        public static event Action<EnumDistanceUnit, string> OnChangeDistanceUnit;

        public static void ChangeDistanceUnit(EnumDistanceUnit unit)
        {
            if ((EnumDistanceUnit)TotalUnits.DistanceUnit == unit) return;
            TotalUnits.Unit.DistanceUnit = (byte)unit;
            UnitsManager.SaveUnits();
            OnChangeDistanceUnit?.Invoke(unit, GetDistanceUnit(unit));
        }

        // 입력 : NM 거리값, 출력 단위
        // 출력 : 출력 단위에 맞는 값
        public static float GetDistance(float distanceNM) => GetDistance(distanceNM, DefaultDistanceUnit);
        public static float GetDistance(float distanceNM, EnumDistanceUnit outputUnit)
        {
            float fDist = 0.0f;

            if (outputUnit == EnumDistanceUnit.NM) fDist = distanceNM;
            else if (outputUnit == EnumDistanceUnit.km) fDist = distanceNM * NMTokm;
            else if (outputUnit == EnumDistanceUnit.m) fDist = distanceNM * NMTom;
            else if (outputUnit == EnumDistanceUnit.kyd) fDist = distanceNM * NMTokyd;
            else if (outputUnit == EnumDistanceUnit.yd) fDist = distanceNM * NMToyd;

            return fDist;
        }

        // 입력 : 거리값, 입력값에 따른 단위
        // 출력 : NM 거리값
        public static float GetDistanceToNM(float distance) => GetDistance(distance, DefaultDistanceUnit);
        public static float GetDistanceToNM(float distance, EnumDistanceUnit inputUnit)
        {
            float fDistNM = 0.0f;

            if (inputUnit == EnumDistanceUnit.NM) fDistNM = distance;
            else if (inputUnit == EnumDistanceUnit.km) fDistNM = distance / NMTokm;
            else if (inputUnit == EnumDistanceUnit.m) fDistNM = distance / NMTom;
            else if (inputUnit == EnumDistanceUnit.kyd) fDistNM = distance / NMTokyd;
            else if (inputUnit == EnumDistanceUnit.yd) fDistNM = distance / NMToyd;

            return fDistNM;
        }

        // 입력 : NM 거리값, 출력 단위
        // 출력 : 출력 단위에 맞는 문자값
        public static string GetStrDistance(float distanceNM) => GetStrDistance(distanceNM, DefaultDistanceUnit);
        public static string GetStrDistance(float distanceNM, EnumDistanceUnit outputUnit)
        {
            string strDist = null;

            if (outputUnit == EnumDistanceUnit.NM) strDist = string.Format("{0:F2}", distanceNM);
            else if (outputUnit == EnumDistanceUnit.km) strDist = string.Format("{0:F2}", distanceNM * NMTokm);
            else if (outputUnit == EnumDistanceUnit.m) strDist = string.Format("{0:F0}", distanceNM * NMTom);
            else if (outputUnit == EnumDistanceUnit.kyd) strDist = string.Format("{0:F2}", distanceNM * NMTokyd);
            else if (outputUnit == EnumDistanceUnit.yd) strDist = string.Format("{0:F0}", distanceNM * NMToyd);

            return strDist;
        }

        // 거리 단위 출력
        public static string GetDistanceUnit() => GetDistanceUnit(DefaultDistanceUnit);
        public static string GetDistanceUnit(EnumDistanceUnit unit)
        {
            string strUnit = "NM";

            if (unit == EnumDistanceUnit.km) strUnit = "km";
            else if (unit == EnumDistanceUnit.m) strUnit = "m";
            else if (unit == EnumDistanceUnit.kyd) strUnit = "kyd";
            else if (unit == EnumDistanceUnit.yd) strUnit = "yd";

            return strUnit;
        }


        public static string GetDistanceUnitDisplayFormatAndMaxValue(EnumDistanceUnit unit, out double maxValue, out double step)
        {
            string distanceForamt = "0.00";
            maxValue = 9999.99;
            step = 0.01;
            switch (unit)
            {
                case EnumDistanceUnit.km:
                    maxValue = 18520.0;
                    step = 0.1;
                    distanceForamt = "0.0";
                    break;
                case EnumDistanceUnit.m:
                    maxValue = 18520000;
                    step = 1;
                    distanceForamt = "0";
                    break;
                case EnumDistanceUnit.kyd:
                    maxValue = 20253.72;
                    step = 0.01;
                    distanceForamt = "0.00";
                    break;
                case EnumDistanceUnit.yd:
                    maxValue = 20253718.0;
                    step = 1;
                    distanceForamt = "0";
                    break;
            }

            return distanceForamt;
        }

        public static double GetDistanceUnitDisplayFormatAndMaxValue(EnumDistanceUnit unit)
        {
            double maxValue = 9999.99;
            switch (unit)
            {
                case EnumDistanceUnit.km:
                    maxValue = 18520.0;
                    break;
                case EnumDistanceUnit.m:
                    maxValue = 18520000;
                    break;
                case EnumDistanceUnit.kyd:
                    maxValue = 20253.72;
                    break;
                case EnumDistanceUnit.yd:
                    maxValue = 20253718.0;
                    break;
            }

            return maxValue;
        }
    }
}
