using JHLib.Util.DataStream;
using JHLib.Util.List;
using JHLib.Util.Pool;
using JHLib.Util.Sort;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Geometry
{
    public unsafe static partial class GeometryHelper
    {
        /// <summary>
        /// 폴리곤의 Centroid를 Raycasting 기반 방식으로 구한다.
        /// 기준점에서 십자형식으로 Raycasting을 하여 인터섹트된 두지점중 가장 넒은 위치를 반환한다
        /// 자식이 있는 구멍난 폴리곤이나 한쪽으로 치우친 폴리곤도 상관없이 Centroid를 구할 수 있다
        /// 알고리즘이 간단하고 성능은 빠르지만, 정확하지 않은 대략적인 위치를 요할때 사용한다
        /// </summary>
        public static bool GetRaycastingCentroid(PoolStreamBucket sb, DataIndexer<FloatRect> ms, Float2D centroid, out Float2D result)
        {
            float x1, x2, y1, y2, cx, cy;
            int i, n, m;

            var exist = false;
            result = default;

            var max = 0f;
            var listx = new SList<float>();
            var listy = new SList<float>();

            i = 0; listx.Clear();
            do
            {
                if (ms[i].Y1 <= centroid.Y && centroid.Y <= ms[i].Y2)
                    RayCastingHorizontal(sb.AsReader<Float2D>(i), centroid.Y, listx, true);
            }
            while (++i < sb.Count);

            if (listx.Count >= 2)
            {
                QuickSort.Run(listx);
                n = 0;
                do
                {
                    x1 = listx[n];
                    x2 = listx[n + 1];
                    cx = (x1 + x2) * 0.5f;

                    i = 0; listy.Clear();
                    do
                    {
                        if (ms[i].X1 <= cx && cx <= ms[i].X2)
                            RayCastingVertical(sb.AsReader<Float2D>(i), cx, listy, true);
                    }
                    while (++i < sb.Count);

                    if (listy.Count >= 2)
                    {
                        QuickSort.Run(listy);
                        m = 0;
                        do
                        {
                            y1 = listy[m];
                            y2 = listy[m + 1];
                            if (y1 <= centroid.Y && centroid.Y <= y2)
                            {
                                if (max < (x2 - x1) * (y2 - y1))
                                {
                                    max = (x2 - x1) * (y2 - y1);
                                    result = new(cx, (y1 + y2) * 0.5f);
                                    exist = true;
                                }
                                break;
                            }
                        }
                        while ((m += 2) < listy.Count);
                    }
                }
                while ((n += 2) < listx.Count);
            }

            i = 0; listy.Clear();
            do
            {
                if (ms[i].X1 <= centroid.X && centroid.X <= ms[i].X2)
                    RayCastingVertical(sb.AsReader<Float2D>(i), centroid.X, listy, true);
            }
            while (++i < sb.Count);

            if (listy.Count >= 2)
            {
                QuickSort.Run(listy);
                n = 0;
                do
                {
                    y1 = listy[n];
                    y2 = listy[n + 1];
                    cy = (y1 + y2) * 0.5f;

                    i = 0; listx.Clear();
                    do
                    {
                        if (ms[i].Y1 <= cy && cy <= ms[i].Y2)
                            RayCastingHorizontal(sb.AsReader<Float2D>(i), cy, listx, true);
                    }
                    while (++i < sb.Count);

                    if (listx.Count >= 2)
                    {
                        QuickSort.Run(listx);
                        m = 0;
                        do
                        {
                            x1 = listx[m];
                            x2 = listx[m + 1];
                            if (x1 <= centroid.X && centroid.X <= x2)
                            {
                                if (max < (x2 - x1) * (y2 - y1))
                                {
                                    max = (x2 - x1) * (y2 - y1);
                                    result = new((x1 + x2) * 0.5f, cy);
                                    exist = true;
                                }
                                break;
                            }
                        }
                        while ((m += 2) < listx.Count);
                    }
                }
                while ((n += 2) < listy.Count);
            }
            return exist;
        }

        /// <summary> y좌표를 기준으로 하는 수평선과 교차하는 모든 x좌표를 구한다 </summary>
        /// <param name="y"> 기준되는 y 좌표</param>
        /// <param name="isForceClose">폴리곤의 첫번째 점과 마지막점의 연결유무</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RayCastingHorizontal(Float2D[] path, float y, SList<float> result, bool isForceClose = false) =>
            RayCastingHorizontal(path, 0, path.Length, y, result, isForceClose);

        /// <summary> y좌표를 기준으로 하는 수평선과 교차하는 모든 x좌표를 구한다 </summary>
        /// <param name="y"> 기준되는 y 좌표</param>
        /// <param name="isForceClose">폴리곤의 첫번째 점과 마지막점의 연결유무</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RayCastingHorizontal(Float2D[] path, int index, int count, float y, SList<float> result, bool isForceClose = false)
        {
            if (count > 1)
                fixed (Float2D* p = &path[index])
                    RayCastingHorizontalInternal(p, count, y, result, isForceClose);
        }

        /// <summary> y좌표를 기준으로 하는 수평선과 교차하는 모든 x좌표를 구한다 </summary>
        /// <param name="y"> 기준되는 y 좌표</param>
        /// <param name="isForceClose">폴리곤의 첫번째 점과 마지막점의 연결유무</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RayCastingHorizontal(DataRange<Float2D> range, float y, SList<float> result, bool isForceClose = false) =>
            RayCastingHorizontal(ref range.Data0, range.Count, y, result, isForceClose);

        /// <summary> y좌표를 기준으로 하는 수평선과 교차하는 모든 x좌표를 구한다 </summary>
        /// <param name="y"> 기준되는 y 좌표</param>
        /// <param name="isForceClose">폴리곤의 첫번째 점과 마지막점의 연결유무</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RayCastingHorizontal(DataHeaderReader<Float2D> reader, float y, SList<float> result, bool isForceClose = false) =>
            RayCastingHorizontal(ref reader.Data0, reader.Count, y, result, isForceClose);

        /// <summary> y좌표를 기준으로 하는 수평선과 교차하는 모든 x좌표를 구한다 </summary>
        /// <param name="y"> 기준되는 y 좌표</param>
        /// <param name="isForceClose">폴리곤의 첫번째 점과 마지막점의 연결유무</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RayCastingHorizontal(ref Float2D path0, int pathCount, float y, SList<float> result, bool isForceClose = false)
        {
            if (pathCount > 1)
                fixed (Float2D* p = &path0)
                    RayCastingHorizontalInternal(p, pathCount, y, result, isForceClose);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RayCastingHorizontalInternal(Float2D* p, int c, float y, SList<float> r, bool close)
        {
            var t = p + 1;
            var e = p + c;
            var zx = p->X;
            var zy = p->Y;
            if (zy > y) { goto A2; }

        A1: if (t <= e - 4)
            {
                do
                {
                    if (t->Y <= y)
                    {
                        if ((t + 1)->Y <= y)
                            if ((t + 2)->Y <= y)
                                if ((t + 3)->Y <= y) continue;
                                else t += 3;
                            else t += 2;
                        else t += 1;
                    }
                    r.Add((t - 1)->X + (t->X - (t - 1)->X) * (y - (t - 1)->Y) / (t->Y - (t - 1)->Y));
                    t++; goto A2;
                }
                while ((t += 4) <= e - 4);
            }
        B1: if (t < e)
            {
                if (t->Y <= y)
                {
                    if (t == e - 1) goto E1;
                    if ((t + 1)->Y <= y) { if (t == e - 2 || (t + 2)->Y <= y) goto E1; t++; }
                    t++;
                }
                r.Add((t - 1)->X + (t->X - (t - 1)->X) * (y - (t - 1)->Y) / (t->Y - (t - 1)->Y));
                t++; goto B2;
            }
        E1: if (close && zy > y)
                r.Add((e - 1)->X + (zx - (e - 1)->X) * (y - (e - 1)->Y) / (zy - (e - 1)->Y));
            return;


        A2: if (t <= e - 4)
            {
                do
                {
                    if (t->Y > y)
                    {
                        if ((t + 1)->Y > y)
                            if ((t + 2)->Y > y)
                                if ((t + 3)->Y > y) continue;
                                else t += 3;
                            else t += 2;
                        else t += 1;
                    }
                    r.Add((t - 1)->X + (t->X - (t - 1)->X) * (y - (t - 1)->Y) / (t->Y - (t - 1)->Y));
                    t++; goto A1;
                }
                while ((t += 4) <= e - 4);
            }
        B2: if (t < e)
            {
                if (t->Y > y)
                {
                    if (t == e - 1) goto E2;
                    if ((t + 1)->Y > y) { if (t == e - 2 || (t + 2)->Y > y) goto E2; t++; }
                    t++;
                }
                r.Add((t - 1)->X + (t->X - (t - 1)->X) * (y - (t - 1)->Y) / (t->Y - (t - 1)->Y));
                t++; goto B1;
            }
        E2: if (close && zy <= y)
                r.Add((e - 1)->X + (zx - (e - 1)->X) * (y - (e - 1)->Y) / (zy - (e - 1)->Y));
            return;
        }



        /// <summary> x좌표를 기준으로 하는 수직선과 교차하는 모든 y좌표를 구한다 </summary>
        /// <param name="x"> 기준되는 x 좌표</param>
        /// <param name="isForceClose">폴리곤의 첫번째 점과 마지막점의 연결유무</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RayCastingVertical(Float2D[] path, float x, SList<float> result, bool isForceClose = false) =>
            RayCastingVertical(path, 0, path.Length, x, result, isForceClose);

        /// <summary> x좌표를 기준으로 하는 수직선과 교차하는 모든 y좌표를 구한다 </summary>
        /// <param name="x"> 기준되는 x 좌표</param>
        /// <param name="isForceClose">폴리곤의 첫번째 점과 마지막점의 연결유무</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RayCastingVertical(Float2D[] path, int index, int count, float x, SList<float> result, bool isForceClose = false)
        {
            if (count > 1)
                fixed (Float2D* p = &path[index])
                    RayCastingVerticalInternal(p, count, x, result, isForceClose);
        }

        /// <summary> x좌표를 기준으로 하는 수직선과 교차하는 모든 y좌표를 구한다 </summary>
        /// <param name="x"> 기준되는 x 좌표</param>
        /// <param name="isForceClose">폴리곤의 첫번째 점과 마지막점의 연결유무</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RayCastingVertical(DataRange<Float2D> range, float x, SList<float> result, bool isForceClose = false) =>
            RayCastingVertical(ref range.Data0, range.Count, x, result, isForceClose);

        /// <summary> x좌표를 기준으로 하는 수직선과 교차하는 모든 y좌표를 구한다 </summary>
        /// <param name="x"> 기준되는 x 좌표</param>
        /// <param name="isForceClose">폴리곤의 첫번째 점과 마지막점의 연결유무</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RayCastingVertical(DataHeaderReader<Float2D> reader, float x, SList<float> result, bool isForceClose = false) =>
            RayCastingVertical(ref reader.Data0, reader.Count, x, result, isForceClose);

        /// <summary> x좌표를 기준으로 하는 수직선과 교차하는 모든 y좌표를 구한다 </summary>
        /// <param name="x"> 기준되는 x 좌표</param>
        /// <param name="isForceClose">폴리곤의 첫번째 점과 마지막점의 연결유무</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RayCastingVertical(ref Float2D path0, int pathCount, float x, SList<float> result, bool isForceClose = false)
        {
            if (pathCount > 1)
                fixed (Float2D* p = &path0)
                    RayCastingVerticalInternal(p, pathCount, x, result, isForceClose);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RayCastingVerticalInternal(Float2D* p, int c, float x, SList<float> r, bool close)
        {
            var t = p + 1;
            var e = p + c;
            var zx = p->X;
            var zy = p->Y;
            if (zx > x) { goto A2; }

        A1: if (t <= e - 4)
            {
                do
                {
                    if (t->X <= x)
                    {
                        if ((t + 1)->X <= x)
                            if ((t + 2)->X <= x)
                                if ((t + 3)->X <= x) continue;
                                else t += 3;
                            else t += 2;
                        else t += 1;
                    }
                    r.Add((t - 1)->Y + (t->Y - (t - 1)->Y) * (x - (t - 1)->X) / (t->X - (t - 1)->X));
                    t++; goto A2;
                }
                while ((t += 4) <= e - 4);
            }
        B1: if (t < e)
            {
                if (t->X <= x)
                {
                    if (t == e - 1) goto E1;
                    if ((t + 1)->X <= x) { if (t == e - 2 || (t + 2)->X <= x) goto E1; t++; }
                    t++;
                }
                r.Add((t - 1)->Y + (t->Y - (t - 1)->Y) * (x - (t - 1)->X) / (t->X - (t - 1)->X));
                t++; goto B2;
            }
        E1: if (close && zx > x)
                r.Add((e - 1)->Y + (zy - (e - 1)->Y) * (x - (e - 1)->X) / (zx - (e - 1)->X));
            return;

        A2: if (t <= e - 4)
            {
                do
                {
                    if (t->X > x)
                    {
                        if ((t + 1)->X > x)
                            if ((t + 2)->X > x)
                                if ((t + 3)->X > x) continue;
                                else t += 3;
                            else t += 2;
                        else t += 1;
                    }
                    r.Add((t - 1)->Y + (t->Y - (t - 1)->Y) * (x - (t - 1)->X) / (t->X - (t - 1)->X));
                    t++; goto A1;
                }
                while ((t += 4) <= e - 4);
            }
        B2: if (t < e)
            {
                if (t->X > x)
                {
                    if (t == e - 1) goto E2;
                    if ((t + 1)->X > x) { if (t == e - 2 || (t + 2)->X > x) goto E2; t++; }
                    t++;
                }
                r.Add((t - 1)->Y + (t->Y - (t - 1)->Y) * (x - (t - 1)->X) / (t->X - (t - 1)->X));
                t++; goto B1;
            }
        E2: if (close && zx <= x)
                r.Add((e - 1)->Y + (zy - (e - 1)->Y) * (x - (e - 1)->X) / (zx - (e - 1)->X));
            return;
        }
    }
}