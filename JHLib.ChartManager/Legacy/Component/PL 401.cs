namespace Legacy.ECM_Core
{
    public static class PL_401
    {
        public static byte Get_Group(int layer_number)
        {
            return layer_number switch {
                _ when ((10000 <= layer_number) && (layer_number < 20000)) => 1, // Base, Display Base
                22010 => 2, // Standard, Drying line
                21010 => 3, // Unknown Object
                21020 => 4, // Buoys, beacons, aids to navigation, structures, Lights
                _ when ((22200 <= layer_number) && (layer_number <= 22240)) => 4,
                _ when ((27000 <= layer_number) && (layer_number <= 27050)) => 4,
                27060 or 27080 => 4,
                _ when ((27200 <= layer_number) && (layer_number <= 27230)) => 4,
                27070 => 5, // Lights
                23030 or 26050 or 26220 or 26240 or 26250 => 6, // Boundaries and limits
                26000 or 26010 or 26040 => 7, // Prohibited and restricted areas
                21030 => 8, // Chart scale boundaries
                26150 => 9, // Cautionary notes
                _ when ((25010 <= layer_number) && (layer_number <= 25060)) => 10, // Ships routeing systems and ferry routes
                26260 => 11, // Archipelagic sea lanes
                _ when ((20000 <= layer_number) && (layer_number < 30000)) => 12, // Miscellaneous
                33010 => 13, // Spot soundings
                34030 or 34070 => 14, // Submarine cables and pipelines
                34050 or 34051 => 15, // All isolated dangers
                31080 => 16, // Magnetic variation
                33020 => 17, // Depth contours
                34010 or 34020 or 33040 => 18, // Seabed
                33050 or 33060 => 19, // Tidal
                _ when ((30000 <= layer_number) && (layer_number < 40000)) => 20, // Miscellaneous
                _ => 1,
            };
        }
    }
}