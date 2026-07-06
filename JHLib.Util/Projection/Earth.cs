namespace JHLib.Util.Projection
{
    public static class Earth
    {
        private const double R2D = 180 / Math.PI;
        private const double D2R = Math.PI / 180;

        public const double EARTH_RAD = 6378137; // earth radius (meters)
        public const double EARTH_RADINV = 1 / EARTH_RAD;
        public const double EARTH_RADx2 = EARTH_RAD * 2;
        public const double EARTH_CIRCUMFERENCE = EARTH_RADx2 * Math.PI;

        public const double LAT_1KM = 1000 * R2D / EARTH_RAD;
    }
}