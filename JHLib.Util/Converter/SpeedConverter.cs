namespace JHLib.Util.Converter
{
    public enum EnumSpeedUnit
    {
        kn, kph
    }

    public static class SpeedConverter
    {
        private static float _knTokph = 1.852f;

        public static EnumSpeedUnit DefaultSpeedUnit = EnumSpeedUnit.kn;

        // 현재 설정된 Speed Unit 설정
        public static event Action<EnumSpeedUnit, string> OnChangeSpeedUnit;

        public static void ChangeSpeedUnit(EnumSpeedUnit unit)
        {
            if ((EnumSpeedUnit)TotalUnits.SpeedUnit == unit) return;
            TotalUnits.Unit.SpeedUnit = (byte)unit;
            UnitsManager.SaveUnits();
            OnChangeSpeedUnit?.Invoke(unit, GetSpeedUnit(unit));
        }

        // 입력 : Kn 속도값, 출력 단위
        // 출력 : 출력 단위에 맞는 값
        public static float GetSpeed(float fSpeedKn) => GetSpeed(fSpeedKn, DefaultSpeedUnit);
        public static float GetSpeed(float fSpeedKn, EnumSpeedUnit outputUnit)
        {
            float fSpeed = 0.0f;

            if (outputUnit == EnumSpeedUnit.kn) fSpeed = fSpeedKn;
            else if (outputUnit == EnumSpeedUnit.kph) fSpeed = fSpeedKn * _knTokph;

            return fSpeed;
        }

        // 입력 : 속도값, 입력값에 따른 단위
        // 출력 : KN 속도값
        public static float GetSpeedToKN(float fSpeed) => GetSpeedToKN(fSpeed, DefaultSpeedUnit);
        public static float GetSpeedToKN(float fSpeed, EnumSpeedUnit inputUnit)
        {
            float fSpeedKN = 0.0f;

            if (inputUnit == EnumSpeedUnit.kn) fSpeedKN = fSpeed;
            else if (inputUnit == EnumSpeedUnit.kph) fSpeedKN = fSpeed / _knTokph;

            return fSpeedKN;
        }

        // 입력 : KN 속도값, 출력 단위
        // 출력 : 출력 단위에 맍는 문자값
        public static string GetStrSpeed(float fSpeedKN) => GetStrSpeed(fSpeedKN, DefaultSpeedUnit);
        public static string GetStrSpeed(float fSpeedKN, EnumSpeedUnit outputUnit)
        {
            string strSpeed = null;

            if (outputUnit == EnumSpeedUnit.kn) strSpeed = string.Format("{0:F1}", fSpeedKN);
            else if (outputUnit == EnumSpeedUnit.kph) strSpeed = string.Format("{0:F2}", fSpeedKN * _knTokph);

            return strSpeed;
        }

        // 속도 단위 출력
        public static string GetSpeedUnit() => GetSpeedUnit(DefaultSpeedUnit);
        public static string GetSpeedUnit(EnumSpeedUnit unit)
        {
            string strUnit = "kn";

            if (unit == EnumSpeedUnit.kph) strUnit = "kph";

            return strUnit;
        }
    }
}
