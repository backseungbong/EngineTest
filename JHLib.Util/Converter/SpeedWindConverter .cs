using System;

namespace JHLib.Util.Converter
{
    /// <summary> 풍속 센서(MWV 등)에서 사용되는 속도 단위 </summary>
    public enum EnumWindSpeedUnit
    {
        Knots,              // NMEA: 'N'
        MetersPerSecond,    // NMEA: 'M'
        KilometersPerHour,  // NMEA: 'K'
        MilesPerHour        // NMEA: 'S'
    }

    public static class SpeedWindConverter
    {
        private const double MeterPerSecondToKnots = 1.94384;
        private const double KilometerPerHourToKnots = 1.0 / 1.852;
        private const double MilePerHourToKnots = 0.868976;

        /// <summary> NMEA 단위 문자(char)를 EnumWindSpeedUnit으로 변환합니다. </summary>
        public static EnumWindSpeedUnit? ParseUnit(char? unitChar)
        {
            if (!unitChar.HasValue) return null;

            return char.ToUpper(unitChar.Value) switch
            {
                'N' => EnumWindSpeedUnit.Knots,
                'M' => EnumWindSpeedUnit.MetersPerSecond,
                'K' => EnumWindSpeedUnit.KilometersPerHour,
                'S' => EnumWindSpeedUnit.MilesPerHour,
                _ => null
            };
        }

        /// <summary> EnumWindSpeedUnit을 기준으로 풍속을 Knots로 정규화합니다. (핵심 비즈니스 로직) </summary>
        public static double? NormalizeToKnots(double speed, EnumWindSpeedUnit unit)
        {
            return unit switch
            {
                EnumWindSpeedUnit.Knots => speed,
                EnumWindSpeedUnit.MetersPerSecond => speed * MeterPerSecondToKnots,
                EnumWindSpeedUnit.KilometersPerHour => speed * KilometerPerHourToKnots,
                EnumWindSpeedUnit.MilesPerHour => speed * MilePerHourToKnots,
                _ => null
            };
        }

        /// <summary> NMEA 원본 풍속 값과 단위 문자를 입력받아 Knots로 정규화합니다. (Handler 편의용 래퍼) </summary>
        public static double? NormalizeToKnots(double? speed, char? unitChar)
        {
            if (!speed.HasValue)
                return null;

            var unitEnum = ParseUnit(unitChar);
            if (!unitEnum.HasValue)
                return null;

            return NormalizeToKnots(speed.Value, unitEnum.Value);
        }
    }
}