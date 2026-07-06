using JHLib.Util.ArrayControl;
using JHLib.Util.DataStream;
using JHLib.Util.List;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Geometry
{
    [StructLayout(LayoutKind.Sequential)]
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe struct ClippedPath(Float2D head, Float2D tail, int offset, int length)
    {
        /// <summary> 잘려진 첫 포인트 </summary>
        public readonly Float2D Head = head;
        /// <summary> 잘려진 끝 포인트 </summary>
        public readonly Float2D Tail = tail;
        /// <summary> 잘려진 첫 포인트와 끝 포인트 사이 원본좌표 오프셋 </summary>
        public readonly int Offset = offset;
        /// <summary> 잘려진 첫 포인트와 끝 포인트 사이 원본좌표 길이 </summary>
        public readonly int Length = length;
        public readonly int EndPosition => Offset + Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2D Lerp(ref Float2D path0, double amount)
        {
            fixed (Float2D* p0 = &path0)
                return Lerp(p0, amount);
        }

        /// <summary> ClippedPath의 보간지점 계산 </summary>
        /// <param name="p">원본 path의 시작포인터</param>
        /// <param name="amount">보간지점 amount(0 ~ 1사이)</param>
        /// <returns>보간지점 Float2D 좌표</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Float2D Lerp(Float2D* p, double amount)
        {
            var head = Head;
            var tail = Tail;

            var count = Length;
            if (count > 0)
            {
                var z = p + Offset;
                var t = z;
                var e = z + count;

                var x = head.X;
                var y = head.Y;

                double dx, dy, dis, sum = 0;
                do sum += Math.Sqrt((dx = x - (x = t->X)) * dx + (dy = y - (y = t->Y)) * dy);
                while (++t < e);

                sum += Math.Sqrt((dx = x - (x = tail.X)) * dx + (dy = y - (y = tail.Y)) * dy);
                sum *= 1 - amount;

                do
                {
                    t--;
                    dis = Math.Sqrt((dx = x - (x = t->X)) * dx + (dy = y - (y = t->Y)) * dy);
                    if ((sum -= dis) <= 0) goto ED;
                }
                while (t > z);

                dis = Math.Sqrt((dx = x - (x = head.X)) * dx + (dy = y - (y = head.Y)) * dy);
                sum -= dis;

            ED: return new(x - sum / dis * dx, y - sum / dis * dy);
            }

            return new(
                head.X * (1 - amount) + tail.X * amount,
                head.Y * (1 - amount) + tail.Y * amount);
        }

        /// <summary> ClippedPath의 바운드 박스 계산</summary>
        /// <param name="p">원본 path의 시작포인터</param>
        /// <returns>바운드 박스 FloatRect </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public FloatRect Bounds(Float2D* p)
        {
            var xMin = Head.X;
            var yMin = Head.Y;
            var xMax = xMin;
            var yMax = yMin;

            if (Tail.X < xMin) xMin = Tail.X; else xMax = Tail.X;
            if (Tail.Y < yMin) yMin = Tail.Y; else yMax = Tail.Y;
            if (Length > 0)
            {
                var z = p + Offset;
                var t = z - 1;
                var e = z + Length;
                while (true)
                {
                    if (++t == e) break;
                    if (xMin <= t->X)
                        if (t->X <= xMax)
                        {
                            if (yMin <= t->Y) { if (yMax < t->Y) yMax = t->Y; }
                            else yMin = t->Y;
                        }
                        else
                        {
                            xMax = t->X;
                            if (yMin <= t->Y) { if (yMax < t->Y) yMax = t->Y; }
                            else yMin = t->Y;
                        }
                    else
                    {
                        xMin = t->X;
                        if (yMin <= t->Y) { if (yMax < t->Y) yMax = t->Y; }
                        else yMin = t->Y;
                    }
                }
            }
            return new(xMin, yMin, xMax, yMax);
        }

        /// <summary> y좌표를 기준으로 하는 수평선과 교차하는 모든 x좌표를 구한다</summary>
        /// <param name="p">원본 path의 시작포인터</param>
        /// <param name="y">수직선의 y좌표</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public FList<float> RayCastingHorizontal(Float2D* p, float y)
        {
            var r = default(FList<float>);
            var head = Head;
            var tail = Tail;
            if (Length > 0)
            {
                var t = p + Offset;
                var e = t + Length;

                if (head.Y <= y)
                {
                    if (t->Y > y)
                    {
                        r.Add(head.X + (t->X - head.X) * (y - head.Y) / (t->Y - head.Y));
                        goto B1;
                    }
                    goto A1;
                }
                else
                {
                    if (t->Y <= y)
                    {
                        r.Add(head.X + (t->X - head.X) * (y - head.Y) / (t->Y - head.Y));
                        goto A1;
                    }
                    goto B1;
                }

            A1: do if (++t == e) goto E1; while (t->Y <= y);
                r.Add((t - 1)->X + (t->X - (t - 1)->X) * (y - (t - 1)->Y) / (t->Y - (t - 1)->Y));
            B1: do if (++t == e) goto E2; while (t->Y > y);
                r.Add((t - 1)->X + (t->X - (t - 1)->X) * (y - (t - 1)->Y) / (t->Y - (t - 1)->Y));
                goto A1;

            E1: if (tail.Y > y) r.Add((e - 1)->X + (tail.X - (e - 1)->X) * (y - (e - 1)->Y) / (tail.Y - (e - 1)->Y));
                return r;
            E2: if (tail.Y <= y) r.Add((e - 1)->X + (tail.X - (e - 1)->X) * (y - (e - 1)->Y) / (tail.Y - (e - 1)->Y));
                return r;
            }

            if ((head.Y <= y && y < tail.Y) || (tail.Y <= y && y < head.Y))
                r.Add(head.X + (tail.X - head.X) * (y - head.Y) / (tail.Y - head.Y));
            return r;
        }

        /// <summary> x좌표를 기준으로 하는 수직선과 교차하는 모든 y좌표를 구한다</summary>
        /// <param name="p">원본 path의 시작포인터</param>
        /// <param name="x">수직선의 x좌표</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public FList<float> RayCastingVertical(Float2D* p, float x)
        {
            var r = default(FList<float>);
            var head = Head;
            var tail = Tail;
            if (Length > 0)
            {
                var t = p + Offset;
                var e = t + Length;
                if (head.X <= x)
                {
                    if (t->X > x)
                    {
                        r.Add(head.Y + (t->Y - head.Y) * (x - head.X) / (t->X - head.X));
                        goto B1;
                    }
                    goto A1;
                }
                else
                {
                    if (t->X <= x)
                    {
                        r.Add(head.Y + (t->Y - head.Y) * (x - head.X) / (t->X - head.X));
                        goto A1;
                    }
                    goto B1;
                }

            A1: do if (++t == e) goto E1; while (t->X <= x);
                r.Add((t - 1)->Y + (t->Y - (t - 1)->Y) * (x - (t - 1)->X) / (t->X - (t - 1)->X));
            B1: do if (++t == e) goto E2; while (t->X > x);
                r.Add((t - 1)->Y + (t->Y - (t - 1)->Y) * (x - (t - 1)->X) / (t->X - (t - 1)->X));
                goto A1;

            E1: if (tail.X > x) r.Add((e - 1)->Y + (tail.Y - (e - 1)->Y) * (x - (e - 1)->X) / (tail.X - (e - 1)->X));
                return r;
            E2: if (tail.X <= x) r.Add((e - 1)->Y + (tail.Y - (e - 1)->Y) * (x - (e - 1)->X) / (tail.X - (e - 1)->X));
                return r;
            }

            if ((head.X <= x && x < tail.X) || (tail.X <= x && x < head.X))
                r.Add(head.Y + (tail.Y - head.Y) * (x - head.X) / (tail.X - head.X));
            return r;
        }

        /// <summary> ClippedPath와 Rect의 교차 판단 </summary>
        /// <param name="p0">원본 path의 시작포인터</param>
        /// <param name="rx1">Rect의 x1 좌표 == left</param>
        /// <param name="ry1">Rect의 y1 좌표 == top</param>
        /// <param name="rx2">Rect의 x2 좌표 == right</param>
        /// <param name="ry2">Rect의 y2 좌표 == bottom</param>
        /// <returns>교차유무 boolean</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Intersect(ref Float2D path0, in FloatRect rect)
        {
            fixed (Float2D* p0 = &path0)
            {
                var p = p0 + Offset;
                var l = Length;
                if (l > 0)
                {
                    if (l > 1)
                        return
                            GeometryHelper.LineIntersect(Head, *p, rect) ||
                            GeometryHelper.PathIntersect(p, l, rect) ||
                            GeometryHelper.LineIntersect(*(p + l - 1), Tail, rect);
                    return
                        GeometryHelper.LineIntersect(Head, *p, rect) ||
                        GeometryHelper.LineIntersect(*p, Tail, rect);
                }
                return GeometryHelper.LineIntersect(Head, Tail, rect);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Float2D* pSource, SList<Float2D> dest) => CopyTo(pSource, ref dest.Occupy0(Length + 2));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Float2D* pSource, in DataHeaderWriter dest) => CopyTo(pSource, ref dest.Occupy0<Float2D>(Length + 2));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Float2D* pSource, ref Float2D dest0)
        {
            dest0 = Head;
            AC.Copy(ref Unsafe.Add(ref *pSource, (uint)Offset), ref Unsafe.Add(ref dest0, 1), Length);
            Unsafe.Add(ref dest0, (uint)(Length + 1)) = Tail;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ClippedPathRaw()
    {
        public Float2D Head;
        public Float2D Tail;
        public int Offset;
        public int Length;
    }
}