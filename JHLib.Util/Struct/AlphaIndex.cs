namespace JHLib.Util.Struct
{
    public readonly struct AlphaIndex
    {
        public readonly static AlphaIndex Null = default;
        public readonly static AlphaIndex Zero = new(0);

        private readonly uint _data;
        public readonly int Index => (int)(_data & 0x00FFFFFF);
        public readonly byte Alpha => (byte)(_data >> 24);
        public readonly bool IsNull => _data == 0;

        public AlphaIndex(int index) =>
            _data = 0xFF000000 | ((uint)index & 0x00FFFFFF);

        public AlphaIndex(int index, float alpha) =>
            _data = (uint)(alpha * 255 + 0.5f) << 24 | (uint)index;

        public AlphaIndex(int index, byte alpha) =>
            _data = (uint)alpha << 24 | (uint)index;
    }
}