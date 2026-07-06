using System.Data;
using System.Text.RegularExpressions;

namespace JHLib.Util.Converter
{
    public enum EnumPositionUnit
    {
        DoMin,
        DoMinSec
    }

    public static class PositionConverter
    {
        public static EnumPositionUnit DefaultPositionUnit = EnumPositionUnit.DoMin;

        // 현재 설정된 Position Unit 정보
        public static event Action<EnumPositionUnit> OnChangePositionUnit;

        public static void ChangePositionUnit(EnumPositionUnit unit)
        {
            if ((EnumPositionUnit)TotalUnits.PositionUnit == unit) return;
            TotalUnits.Unit.PositionUnit = (byte)unit;
            UnitsManager.SaveUnits();
            OnChangePositionUnit?.Invoke(unit);
        }

        public static string LatLonToString(double posVal, bool isLat) => LatLonToString(posVal, isLat, DefaultPositionUnit);
        public static string LatLonToString(double posVal, bool isLat, EnumPositionUnit positionUnit)
        {
            string rtn;
            if (posVal == 999.9)
            {
                if (positionUnit == EnumPositionUnit.DoMin)
                    rtn = "---°--.---'";
                else
                    rtn = "---°--'--\"";

                return rtn;
            }

            var deg = (int)Math.Abs(posVal);
            var tmp = (Math.Abs(posVal) - deg) * 60.0 + 0.0005; // 0.0005 = 반올림용            
            var min = (int)tmp;
            var dec = (int)((tmp - min) * 1000);
            var sec = (int)((tmp - min) * 60);

            if (isLat)
            {
                if (positionUnit == EnumPositionUnit.DoMin)
                {
                    if (posVal >= 0.0)
                        rtn = string.Format("{0:00}\u00B0{1:00}.{2:000}'N", deg, min, dec);
                    else
                        rtn = string.Format("{0:00}\u00B0{1:00}.{2:000}'S", deg, min, dec);
                }
                else
                {
                    if (posVal >= 0.0)
                        rtn = string.Format("{0:00}\u00B0{1:00}'{2:00}\"N", deg, min, sec);
                    else
                        rtn = string.Format("{0:00}\u00B0{1:00}'{2:00}\"S", deg, min, sec);
                }
            }
            else
            {
                if (positionUnit == EnumPositionUnit.DoMin)
                {
                    if (posVal >= 0.0)
                        rtn = string.Format("{0:000}\u00B0{1:00}.{2:000}'E", deg, min, dec);
                    else
                        rtn = string.Format("{0:000}\u00B0{1:00}.{2:000}'W", deg, min, dec);
                }
                else
                {
                    if (posVal >= 0.0)
                        rtn = string.Format("{0:000}\u00B0{1:00}'{2:00}\"E", deg, min, sec);
                    else
                        rtn = string.Format("{0:000}\u00B0{1:00}'{2:00}\"W", deg, min, sec);
                }
            }

            return rtn;
        }

        public static string LatLonToString(double posVal, bool isLat, out string cardinalDirections) => LatLonToString(posVal, isLat, out cardinalDirections, DefaultPositionUnit);
        public static string LatLonToString(double posVal, bool isLat, out string cardinalDirections, EnumPositionUnit positionUnit)
        {
            string rtn;
            if (posVal == 999.9)
            {
                if (positionUnit == EnumPositionUnit.DoMin)
                    rtn = "---°--.---'";
                else
                    rtn = "---°--'--\"";

                cardinalDirections = "";
                return rtn;
            }

            var deg = (int)Math.Abs(posVal);
            var tmp = (Math.Abs(posVal) - deg) * 60.0 + 0.0005; // 0.0005 = 반올림용            
            var min = (int)tmp;
            var dec = (int)((tmp - min) * 1000);
            var sec = (int)((tmp - min) * 60);

            if (isLat)
            {
                if (positionUnit == EnumPositionUnit.DoMin)
                {
                    if (posVal >= 0.0)
                    {
                        cardinalDirections = "N";
                        rtn = string.Format("{0:00}\u00B0{1:00}.{2:000}'", deg, min, dec);
                    }
                    else
                    {
                        cardinalDirections = "S";
                        rtn = string.Format("{0:00}\u00B0{1:00}.{2:000}'", deg, min, dec);
                    }
                }
                else
                {
                    if (posVal >= 0.0)
                    {
                        cardinalDirections = "N";
                        rtn = string.Format("{0:00}\u00B0{1:00}'{2:00}\"", deg, min, sec);
                    }
                    else
                    {
                        cardinalDirections = "S";
                        rtn = string.Format("{0:00}\u00B0{1:00}'{2:00}\"", deg, min, sec);
                    }
                }
            }
            else
            {
                if (positionUnit == EnumPositionUnit.DoMin)
                {
                    if (posVal >= 0.0)
                    {
                        cardinalDirections = "E";
                        rtn = string.Format("{0:000}\u00B0{1:00}.{2:000}'", deg, min, dec);
                    }
                    else
                    {
                        cardinalDirections = "W";
                        rtn = string.Format("{0:000}\u00B0{1:00}.{2:000}'", deg, min, dec);
                    }
                }
                else
                {
                    if (posVal >= 0.0)
                    {
                        cardinalDirections = "E";
                        rtn = string.Format("{0:000}\u00B0{1:00}'{2:00}\"", deg, min, sec);
                    }
                    else
                    {
                        cardinalDirections = "W";
                        rtn = string.Format("{0:000}\u00B0{1:00}'{2:00}\"", deg, min, sec);
                    }
                }
            }

            return rtn;
        }

        public static double ConvertLatStringToDouble(string strLat) => ConvertLatStringToDouble(strLat, DefaultPositionUnit);
        public static double ConvertLatStringToDouble(string strLat, EnumPositionUnit positionUnit)
        {
            double dLat = 999.9;
            if (positionUnit == EnumPositionUnit.DoMin && strLat.Length < 11) return double.NaN;
            if (positionUnit == EnumPositionUnit.DoMinSec && strLat.Length < 10) return double.NaN;

            string[] numbers = Regex.Matches(strLat, @"\d+(\.\d+)?")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToArray();

            // 방위 문자 추출 (N, S, E, W)
            string direction = Regex.Match(strLat, "[NSEW]", RegexOptions.IgnoreCase).Value.ToUpper();

            double deg = 0;
            double min = 0.0f;
            double sec = 0.0f;
            string strUnit = null;

            if (numbers.Length >= 1 && double.TryParse(numbers[0], out double value))
            {
                deg = value;
            }

            if (numbers.Length >= 2 && double.TryParse(numbers[1], out value))
            {
                min = value;
            }

            if (numbers.Length >= 3 && double.TryParse(numbers[2], out value))
            {
                sec = value;
            }

            if(positionUnit == EnumPositionUnit.DoMin)
            {
                dLat = deg + (min / 60.0);
                if (!string.IsNullOrEmpty(direction) && direction == "S") dLat *= -1.0;
            }
            else
            {
                dLat = deg + (min / 60.0) + (sec / 3600.0);
                if (!string.IsNullOrEmpty(direction) && direction == "S") dLat *= -1.0;
            }

            return dLat;
        }

        public static double ConvertLonStringToDouble(string strLon) => ConvertLonStringToDouble(strLon, DefaultPositionUnit);
        public static double ConvertLonStringToDouble(string strLon, EnumPositionUnit positionUnit)
        {
            double dLon = 999.9;
            if (strLon.Length < 11) return double.NaN;

            string[] numbers = Regex.Matches(strLon, @"\d+(\.\d+)?")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToArray();

            // 방위 문자 추출 (N, S, E, W)
            string direction = Regex.Match(strLon, "[NSEW]", RegexOptions.IgnoreCase).Value.ToUpper();

            double deg = 0;
            double min = 0.0f;
            double sec = 0.0f;
            string strUnit = null;

            if (numbers.Length >= 1 && double.TryParse(numbers[0], out double value))
            {
                deg = value;
            }

            if (numbers.Length >= 2 && double.TryParse(numbers[1], out value))
            {
                min = value;
            }

            if (numbers.Length >= 3 && double.TryParse(numbers[2], out value))
            {
                sec = value;
            }

            if (positionUnit == EnumPositionUnit.DoMin)
            {
                dLon = deg + (min / 60.0);
                if (!string.IsNullOrEmpty(direction) && direction == "W") dLon *= -1.0;
            }
            else
            {
                dLon = deg + (min / 60.0) + (sec / 3600.0);
                if (!string.IsNullOrEmpty(direction) && direction == "W") dLon *= -1.0;
            }

            return dLon;
        }
    }
}