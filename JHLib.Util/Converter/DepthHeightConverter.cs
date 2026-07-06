namespace JHLib.Util.Converter
{
    public enum EnumDepthHeightUnit
    {
        m, feet, fathom
    }

    public static class DepthHeightConverter
    {
        public static float MeterToft = 3.2808399f;       // 1 m = 1 / 0.3048 ft
        public static float MeterTofathom = 0.5468066f;   // 1 m = 1 / 1.8288 fathom

        public static EnumDepthHeightUnit DefaultDepthHeightUnit = EnumDepthHeightUnit.m;

        // 현재 설정된 Depth/Height Unit 설정
        public static event Action<EnumDepthHeightUnit, string> OnChangeDepthHeightUnit;

        public static void ChangeDepthHeightUnit(EnumDepthHeightUnit unit)
        {
            if ((EnumDepthHeightUnit)TotalUnits.DepthHeightUnit == unit) return;
            TotalUnits.Unit.DepthHeightUnit = (byte)unit;
            UnitsManager.SaveUnits();
            OnChangeDepthHeightUnit?.Invoke(unit, GetDepthHeightUnit(unit));
        }

        // 수심,높이 단위 출력
        public static string GetDepthHeightUnit() => GetDepthHeightUnit(DefaultDepthHeightUnit);
        public static string GetDepthHeightUnit(EnumDepthHeightUnit unit)
        {
            string strUnit = "m";

            if (unit == EnumDepthHeightUnit.m) strUnit = "m";
            else if (unit == EnumDepthHeightUnit.feet) strUnit = "ft";
            else if (unit == EnumDepthHeightUnit.fathom) strUnit = "fm";

            return strUnit;
        }

        // 입력 : m 거리값, 출력 단위
        // 출력 : 출력 단위에 맞는 값
        //public static float GetDepthHeight(float distanceM) => GetDepthHeight(distanceM, DefaultDepthHeightUnit);
        public static float GetDepthHeight(float distanceM, EnumDepthHeightUnit outputUnit)
        {
            float fDist = 0.0f;

            if (outputUnit == EnumDepthHeightUnit.m) fDist = distanceM;
            else if (outputUnit == EnumDepthHeightUnit.feet) fDist = distanceM * MeterToft;
            else if (outputUnit == EnumDepthHeightUnit.fathom) fDist = distanceM * MeterTofathom;

            return fDist;
        }

        // 입력 : 거리값, 입력값에 따른 단위
        // 출력 : m 거리값
        //public static float GetDepthHeighToM(float distance) => GetDepthHeighToM(distance, DefaultDepthHeightUnit);
        public static float GetDepthHeighToM(float distance, EnumDepthHeightUnit inputUnit)
        {
            float fDistM = 0.0f;

            if (inputUnit == EnumDepthHeightUnit.m) fDistM = distance;
            else if (inputUnit == EnumDepthHeightUnit.feet) fDistM = distance / MeterToft;
            else if (inputUnit == EnumDepthHeightUnit.fathom) fDistM = distance / MeterTofathom;

            return fDistM;
        }

        // 입력 : m 거리값, 출력 단위
        // 출력 : 출력 단위에 맞는 문자값
        //public static string GetStrDepthHeight(float distanceM) => GetStrDepthHeight(distanceM, DefaultDepthHeightUnit);
        public static string GetStrDepthHeight(float distanceM, EnumDepthHeightUnit outputUnit)
        {
            string strDist = null;

            if (outputUnit == EnumDepthHeightUnit.m) strDist = string.Format("{0:F2}", distanceM);
            else if (outputUnit == EnumDepthHeightUnit.feet) strDist = string.Format("{0:F2}", distanceM * MeterToft);
            else if (outputUnit == EnumDepthHeightUnit.fathom) strDist = string.Format("{0:F0}", distanceM * MeterTofathom);

            return strDist;
        }
    }
}
