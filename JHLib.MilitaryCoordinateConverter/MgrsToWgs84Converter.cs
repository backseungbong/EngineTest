using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System.Text.RegularExpressions;

namespace JHLib.MilitaryCoordinateConverter
{
    public static class MgrsToWgs84Converter
    {
        private static readonly CoordinateTransformationFactory _transformationFactory;

        static MgrsToWgs84Converter()
        {
            _transformationFactory = new CoordinateTransformationFactory();
        }

        /// <summary>
        /// MGRS 문자열을 WGS84 위경도 좌표로 변환합니다.
        /// </summary>
        /// <param name="mgrs">MGRS 문자열 (공백 유무 상관없음, 예: "52S CH 12745 90214")</param>
        /// <returns>WGS84 위도, 경도 튜플</returns>
        public static (double Latitude, double Longitude) ConvertMgrsToWgs84(string mgrs)
        {
            try
            {
                // 1. 공백 제거 및 대문자 변환 후 MGRS 포맷 파싱
                string cleanMgrs = mgrs.Replace(" ", "").ToUpper();
                var (zone, latBand, squareId, eastingStr, northingStr) = ParseMgrsString(cleanMgrs);

                // 2. MGRS 격자 식별자를 기반으로 한 표준 UTM Base 평면 좌표($m$) 복원
                var (baseEasting, baseNorthing) = GetUtmBaseFromSquare(zone, squareId, latBand);

                // 3. 입력된 정밀도(숫자 자릿수)에 따른 세부 미터 단위 오프셋 반영
                double utmX = baseEasting + ScaleOffset(eastingStr);
                double utmY = baseNorthing + ScaleOffset(northingStr);

                // 4. UTM -> WGS84 역투영 변환 수행
                bool isNorth = IsNorthHemisphere(latBand);
                var (latitude, longitude) = TransformUtmToWgs84(utmX, utmY, zone, isNorth);

                return (latitude, longitude);

            }
            catch (Exception ex)
            {
                return (0, 0);
            }
        }

        private static (int zone, char latBand, string squareId, string easting, string northing) ParseMgrsString(string mgrs)
        {
            // 정규식을 통해 각 컴포넌트 분리 (Zone: 1~2자리 숫자, Band: 알파벳 1자리, Square: 알파벳 2자리, 나머지 동/북거리는 반반씩)
            var match = Regex.Match(mgrs, @"^([0-9]{1,2})([C-X])([A-Z]{2})([0-9]+)$");
            if (!match.Success)
                throw new ArgumentException("올바르지 않은 MGRS 포맷입니다.");

            int zone = int.Parse(match.Groups[1].Value);
            char latBand = match.Groups[2].Value[0];
            string squareId = match.Groups[3].Value;
            string numPart = match.Groups[4].Value;

            if (numPart.Length % 2 != 0)
                throw new ArgumentException("동거리가 들어가는 숫자 자릿수는 반드시 짝수여야 합니다.");

            int halfLength = numPart.Length / 2;
            string easting = numPart.Substring(0, halfLength);
            string northing = numPart.Substring(halfLength);

            return (zone, latBand, squareId, easting, northing);
        }

        private static double ScaleOffset(string numStr)
        {
            // 자릿수에 따라 미터 단위로 변환 (예: 5자리면 1m 단위, 4자리면 10m 단위이므로 100,000 기준 스케일링)
            double val = double.Parse(numStr);
            return val * Math.Pow(10, 5 - numStr.Length);
        }

        private static bool IsNorthHemisphere(char latBand)
        {
            // N 밴드 이상은 북반구, M 이하(C~M)는 남반구
            return latBand >= 'N';
        }

        private static (double easting, double northing) GetUtmBaseFromSquare(int zone, string squareId, char latBand)
        {
            string eCols = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            string nRows = "ABCDEFGHJKLMNPQRSTUV";

            char eChar = squareId[0];
            char nChar = squareId[1];

            int colIndex = eCols.IndexOf(eChar);
            int rowIndex = nRows.IndexOf(nChar);

            int set = (zone - 1) % 3;
            int eIndex = (colIndex - (set * 8)) % 24;
            if (eIndex < 0) eIndex += 24;
            double baseEasting = (eIndex + 1) * 100000;

            int rowSet = (zone % 2 == 1) ? 0 : 5;
            int nIndex = (rowIndex - rowSet) % 20;
            if (nIndex < 0) nIndex += 20;

            // 해당 Latitude Band가 대략적으로 속하는 위도 영역의 미터 기준점 매핑 계산
            double approxLatitude = (latBand - 'C') * 8 - 80;
            if (latBand > 'I') approxLatitude -= 8; // I 제외 보정
            if (latBand > 'O') approxLatitude -= 8; // O 제외 보정

            double approxNorthing = (approxLatitude >= 0) ? approxLatitude * 111000 : (10000000 + approxLatitude * 111000);
            double baseNorthing = Math.Floor(approxNorthing / 2000000) * 2000000 + (nIndex * 100000);

            return (baseEasting, baseNorthing);
        }

        private static (double lat, double lon) TransformUtmToWgs84(double utmX, double utmY, int zone, bool isNorth)
        {
            var wgs84Geo = GeographicCoordinateSystem.WGS84;
            var utmProj = ProjectedCoordinateSystem.WGS84_UTM(zone, isNorth);

            // UTM에서 WGS84로 변환하기 위해 순서 반대로 파이프라인 생성
            var transform = _transformationFactory.CreateFromCoordinateSystems(utmProj, wgs84Geo);

            // 역변환 수행 (입력: UTM X, UTM Y)
            double[] geoPoint = transform.MathTransform.Transform(new double[] { utmX, utmY });

            // 결과값 반환 (출력 순서: 위도=geoPoint[1], 경도=geoPoint[0])
            return (geoPoint[1], geoPoint[0]);
        }
    }
}
