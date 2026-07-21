using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace JHLib.MilitaryCoordinateConverter
{
    public static class Wgs84ToMgrsConverter
    {
        private static readonly CoordinateTransformationFactory _transformationFactory;

        static Wgs84ToMgrsConverter()
        {
            _transformationFactory = new CoordinateTransformationFactory();
        }

        public static bool TryConvertWgs84ToMgrs(double latitude, double longitude, out string mgrs)
        {
            mgrs = string.Empty;

            try
            {
                mgrs = ConvertWgs84ToMgrs(latitude, longitude);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// WGS84 위경도 좌표를 MGRS 문자열로 변환합니다.
        /// </summary>
        /// <param name="latitude">위도 (Decimal Degrees)</param>
        /// <param name="longitude">경도 (Decimal Degrees)</param>
        /// <returns>MGRS 좌표 문자열 (예: 52SCH12345678)</returns>
        private static string ConvertWgs84ToMgrs(double latitude, double longitude)
        {
            // 1. WGS84 -> UTM 변환 수행
            var (utmX, utmY, zone, isNorth) = TransformWgs84ToUtm(latitude, longitude);

            // 2. UTM -> MGRS 변환 수행
            string mgrs = ConvertUtmToMgrs(utmX, utmY, zone, latitude);

            return mgrs;
        }

        /// <summary>
        /// 1단계: WGS84 -> UTM 변환
        /// </summary>
        private static (double x, double y, int zone, bool isNorth) TransformWgs84ToUtm(double lat, double lon)
        {
            // 경도를 기준으로 UTM Zone 계산 (1 ~ 60)
            int zone = (int)Math.Floor((lon + 180) / 6) + 1;
            bool isNorth = lat >= 0;

            // WGS84 지리좌표계 정의
            var wgs84Geo = GeographicCoordinateSystem.WGS84;

            // 해당 Zone의 UTM 투영좌표계 생성 (북반구 326XX, 남반구 327XX)
            int epsgCode = isNorth ? 32600 + zone : 32700 + zone;
            var utmProj = ProjectedCoordinateSystem.WGS84_UTM(zone, isNorth);

            // 변환 파이프라인 생성
            var transform = _transformationFactory.CreateFromCoordinateSystems(wgs84Geo, utmProj);

            // 변환 수행 (입력: 경도, 위도 순서)
            double[] utmPoint = transform.MathTransform.Transform(new double[] { lon, lat });

            return (utmPoint[0], utmPoint[1], zone, isNorth);
        }

        /// <summary>
        /// 2단계: UTM -> MGRS 변환 (표준 격자 식별자 매핑)
        /// </summary>
        private static string ConvertUtmToMgrs(double utmX, double utmY, int zone, double lat)
        {
            try
            {
                // 1. UTM 위도대(Latitude Band) 문자 결정
                char latBand = GetUtmLatitudeBand(lat);

                // 2. 100km 격자 식별자(Square Identifier) 계산
                string squareId = Get100kSquareId(zone, utmX, utmY);

                // 3. 미터 단위 평면 좌표를 5자리 정수로 단축 (1m 정밀도 기준 각 5자리, 총 10자리 격자)
                int easting = (int)Math.Floor(utmX % 100000);
                int northing = (int)Math.Floor(utmY % 100000);

                // 포맷팅: Zone + Band + SquareId + Easting(5자리) + Northing(5자리)
                return $"{zone}{latBand}{squareId}{easting:D5}{northing:D5}";
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        private static char GetUtmLatitudeBand(double lat)
        {
            string bands = "CDEFGHJKLMNPQRSTUVWXX"; // I, O 제외
            if (lat < -80 || lat > 84) throw new ArgumentOutOfRangeException("MGRS는 위도 -80도에서 84도 사이만 지원합니다.");

            int index = (int)((lat + 80) / 8);
            if (index == 20) index = 19; // 80도~84도 구간 처리 (X 밴드)
            return bands[index];
        }

        private static string Get100kSquareId(int zone, double utmX, double utmY)
        {
            // 100km 격자 식별용 알파벳 배열 (I, O 제외)
            string eCols = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            string nRows = "ABCDEFGHJKLMNPQRSTUV";

            int eIndex = (int)Math.Floor(utmX / 100000) - 1;
            int nIndex = (int)Math.Floor(utmY / 100000) % 20;

            // Zone 그룹에 따른 Column 세트 시프트 규칙 적용
            int set = (zone - 1) % 3;
            int colIndex = (eIndex + (set * 8)) % 24;

            // 홀수/짝수 Zone에 따른 Row 세트 시프트 규칙 적용
            int rowSet = (zone % 2 == 1) ? 0 : 5;
            int rowIndex = (nIndex + rowSet) % 20;

            if (rowIndex < 0) rowIndex += 20;

            return $"{eCols[colIndex]}{nRows[rowIndex]}";
        }
    }
}
