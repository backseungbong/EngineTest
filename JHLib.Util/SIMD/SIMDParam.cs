namespace JHLib.Util.Simd
{
    public static class SIMDParam
    {
        public const int NonTemporalStoreThresholdForFill = 4 * 1024 * 1024; // 4MB
        public const int NonTemporalStoreThresholdForCopy = 2 * 1024 * 1024; // 2MB
    }
}
