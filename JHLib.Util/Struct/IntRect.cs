namespace JHLib.Util.Struct
{
    public struct IntRect
    {
        public int X1;
        public int Y1;
        public int X2;
        public int Y2;
        public int DX => X2 - X1;
        public int DY => Y2 - Y1;
        public int CenterX => (X1 + X2) / 2;
        public int CenterY => (Y1 + Y2) / 2;
        public Int2D Center => new Int2D((X1 + X2) / 2, (Y1 + Y2) / 2);

        public bool IsPoint => X1 == X2 && Y1 == Y2;
        public bool IsZero => X1 == 0 && Y1 == 0 && X2 == 0 && Y2 == 0;

        public IntRect(int x1, int y1, int x2, int y2)
        {
            if (x1 <= x2) { X1 = x1; X2 = x2; } else { X1 = x2; X2 = x1; }
            if (y1 <= y2) { Y1 = y1; Y2 = y2; } else { Y1 = y2; Y2 = y1; }
        }
        public IntRect(int x, int y)
        {
            X1 = x;
            Y1 = y;
            X2 = x;
            Y2 = y;
        }
        public IntRect(Int2D p)
        {
            X1 = p.X;
            Y1 = p.Y;
            X2 = p.X;
            Y2 = p.Y;
        }
    }
}