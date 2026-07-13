using JHLib.ChartManager.API;
using JHLib.ChartManager.Record;
using JHLib.Graphics;
using JHLib.Graphics.SkiaExtention;
using JHLib.S57.Catalogue;
using JHLib.S57.Chart;
using JHLib.S57ManualUpdate.ManualUpdate;
using JHLib.Util.Projection;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Weather;
using SkiaSharp;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace JHLib.S57.ChartObject
{
    public class ChartMain
    {
        public ChartMain(string exePath, S57ChartInfo chartInfo, S57ChartRenderer chartRenderer)
        {
			ChartRenderer = chartRenderer;
            ChartInfo = chartInfo;

            _exePath = exePath;
            _chartPath = Path.Combine(exePath, S57PathName.s57Dir, S57PathName.encDir, S57PathName.sencDir);
            _updatePath = Path.Combine(exePath, S57PathName.s57Dir, S57PathName.encDir, S57PathName.updateDir);

            var layer = new ChartLayer[9];
            for (var i = 0; i < 9; i++)  layer[i] = new ChartLayer();
            Layer = layer;

            ListMetaScaleBoundary.Clear();
        }

        private string _exePath = "";
        // 차트 경로 저장
        private string _chartPath;
        // Update 경로 저장
        private string _updatePath;

        // S57Chart정보를 저장할 인스턴스 
        public S57ChartRenderer ChartRenderer;
        // 차트 정보를 저장할 인스턴스 
        public S57ChartInfo ChartInfo;

        // Layer별로 저장하기 위한 어레이
        public ChartLayer[] Layer;

        // Text / Inform / Update를 그리기 위한 구조체
        public ST_OVER Over;

        // CS 처리 정보를 저장할 어레이
        public List<ST_CS> ListDepareCS = new();
        public List<ST_CS> ListDrgareCS = new();
        public List<ST_CS> ListObstrnCS = new();
        public List<ST_CS> ListWrecksCS = new();

        // Ownship의 위치와 Overlap될 가능성이 있는 Center Symbol의 위치를 저장할 어레이
        public List<ST_OVERLAP> ListOverlap = new();

        // Chart를 설치시 Sequence에 맞지 않거나, Edition Num의 잘못으로 Update가 실패한 경우에 
        // "Chart information not up to date"라고 표시하기 위한 Flag를 추가함
        public bool IsUpToDate = false;

        // 하나의 차트에 Compilation Scale이 다른 Meta Data를 저장할 변수
        public List<META> ListMetaScaleBoundary = new();

        // Manual Update된 정보를 저장할 변수 
        public List<MUmain> ListManualUpdate = new();

        public void Dispose()
        {
            ListDepareCS.Clear();
            ListDrgareCS.Clear();
            ListObstrnCS.Clear();
            ListWrecksCS.Clear();

            foreach (var layer in Layer)
                layer.Dispose();

            if (Over.ListText != null) Over.ListText.Clear();
            if (Over.ListInform != null) Over.ListInform.Clear();
        }

        // SENC를 읽어오는 함수 
        public bool LoadChart(string chartName, bool worldMap = false)
        {
            Over.Agency = -1;

            var strFile = Path.Combine(_chartPath, chartName + S57PathName.sencExt);
            if (File.Exists(strFile))
            {
                var data = File.ReadAllBytes(strFile);
                ref var data0 = ref MemoryMarshal.GetArrayDataReference(data);

                // Up To Date 확인
                byte lastByte = Unsafe.Add(ref data0, data.Length - 1);
                IsUpToDate = lastByte == 1 ? true : false;

                if (worldMap)
                {
                    SerializeWorldMap(ref data0);
                }
                else
                {
                    SerializeSENC(ref data0);
                    // Manual Update 정보 가져오기
                    ChartRenderer.ManualUpdate?.LoadManualUpdate(chartName, out ListManualUpdate);
                }
                return true;
            }

            return false;
        }

        [StructLayout(LayoutKind.Sequential, Size = 16)] public readonly struct B16 { }
        [StructLayout(LayoutKind.Sequential, Size = 32)] public readonly struct B32 { }
        [StructLayout(LayoutKind.Sequential, Size = 64)] public readonly struct B64 { }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref byte add(ref byte ptr, nint add) => ref Unsafe.AddByteOffset(ref ptr, add);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref T add<T>(ref T ptr, nint add) => ref Unsafe.Add(ref ptr, add);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref T get<T>(ref byte ptr) => ref Unsafe.As<byte, T>(ref ptr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref T get<T>(ref byte ptr, nint add) => ref Unsafe.As<byte, T>(ref Unsafe.AddByteOffset(ref ptr, add));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static T AsType<T>(ref byte data0, ref uint off) where T : unmanaged
        {
            var f = off; off = f + (uint)Unsafe.SizeOf<T>();
            return Unsafe.As<byte, T>(ref Unsafe.AddByteOffset(ref data0, f));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe T[] AsArray<T>(ref byte data0, ref uint off, int count) where T : unmanaged
        {
            var l = Unsafe.SizeOf<T>() * count;
            var f = off; off = f + (uint)l;
            var r = GC.AllocateUninitializedArray<T>(count);
            ref var d0 = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(r));
            ref var s0 = ref Unsafe.AddByteOffset(ref data0, f);

            if (l <= 64)
                if (l > 02)
                    if (l > 04)
                        if (l > 08)
                            if (l > 16)
                                if (l > 32)
                                {
                                    get<B32>(ref d0) = get<B32>(ref s0);
                                    get<B32>(ref d0, l - 32) = get<B32>(ref s0, l - 32);
                                }
                                else
                                {
                                    get<B16>(ref d0) = get<B16>(ref s0);
                                    get<B16>(ref d0, l - 16) = get<B16>(ref s0, l - 16);
                                }
                            else
                            {
                                get<long>(ref d0) = get<long>(ref s0);
                                get<long>(ref d0, l - 08) = get<long>(ref s0, l - 8);
                            }
                        else
                        {
                            get<int>(ref d0) = get<int>(ref s0);
                            get<int>(ref d0, l - 04) = get<int>(ref s0, l - 04);
                        }
                    else
                    {
                        get<short>(ref d0) = get<short>(ref s0);
                        get<short>(ref d0, l - 02) = get<short>(ref s0, l - 02);
                    }
                else
                {
                    if (l > 0)
                    {
                        d0 = s0;
                        add(ref d0, l - 01) = add(ref s0, l - 01);
                    }
                }
            else if (l <= 2048)
            {
                ref var s = ref s0;
                ref var d = ref d0;
                ref var e = ref add(ref s0, l - 64);
                do
                {
                    get<B64>(ref d) = get<B64>(ref s);
                    s = ref add(ref s, 64);
                    d = ref add(ref d, 64);
                }
                while (Unsafe.IsAddressLessThan(ref s, ref e));
                get<B64>(ref d0, l - 64) = get<B64>(ref e);
            }
            else
            {
                NativeMemory.Copy(Unsafe.AsPointer(ref s0), Unsafe.AsPointer(ref d0), (nuint)l);
            }
            return r;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string AsASCII(ref byte data0, ref uint off, int count)
        {
            var f = off; off = f + (uint)count;
            var result = new string(default, count);
            ref var d0 = ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(result.AsSpan()));
            ref var s0 = ref Unsafe.AddByteOffset(ref data0, f);

            if (count > 4)
            {
                ref var d = ref d0;
                ref var s = ref s0;
                ref var e = ref Unsafe.AddByteOffset(ref s, count - 4);
                do
                {
                    d = s;
                    Unsafe.AddByteOffset(ref d, 2) = Unsafe.AddByteOffset(ref s, 1);
                    Unsafe.AddByteOffset(ref d, 4) = Unsafe.AddByteOffset(ref s, 2);
                    Unsafe.AddByteOffset(ref d, 6) = Unsafe.AddByteOffset(ref s, 3);
                    d = ref Unsafe.AddByteOffset(ref d, 8);
                    s = ref Unsafe.AddByteOffset(ref s, 4);
                }
                while (Unsafe.IsAddressLessThan(ref s, ref e));
                Unsafe.AddByteOffset(ref d0, (count - 4) * 2) = e;
                Unsafe.AddByteOffset(ref d0, (count - 3) * 2) = Unsafe.AddByteOffset(ref e, 1);
                Unsafe.AddByteOffset(ref d0, (count - 2) * 2) = Unsafe.AddByteOffset(ref e, 2);
                Unsafe.AddByteOffset(ref d0, (count - 1) * 2) = Unsafe.AddByteOffset(ref e, 3);
                return result;
            }
            d0 = s0;
            if (count > 2)
            {
                Unsafe.AddByteOffset(ref d0, 2) = Unsafe.AddByteOffset(ref s0, 1);
                Unsafe.AddByteOffset(ref d0, 4) = Unsafe.AddByteOffset(ref s0, 2);
            }
            Unsafe.AddByteOffset(ref d0, (count - 1) * 2) = Unsafe.AddByteOffset(ref s0, count - 1);
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe string AsUTF8(ref byte data0, ref uint off, int count)
        {
            var f = off; off = f + (uint)count;
            return Encoding.UTF8.GetString((byte*)Unsafe.AsPointer(ref Unsafe.AddByteOffset(ref data0, f)), count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe string AsSByteString(ref byte data0, ref uint off, int count)
        {
            var f = off; off = f + (uint)count;
            return new string((sbyte*)Unsafe.AsPointer(ref Unsafe.AddByteOffset(ref data0, f)), 0, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static FloatRect GetBound(ref Float2D point0, int count)
        {
            var xMin = point0.X;
            var yMin = point0.Y;
            var xMax = xMin;
            var yMax = yMin;

            ref var pt = ref point0;
            ref var pe = ref add(ref point0, count);

            while (true)
            {
                pt = ref add(ref pt, 1);
                if (Unsafe.IsAddressLessThan(ref pt, ref pe) == false) break;
                if (xMin <= pt.X)
                    if (pt.X <= xMax)
                    {
                        if (yMin <= pt.Y) { if (yMax < pt.Y) yMax = pt.Y; }
                        else yMin = pt.Y;
                    }
                    else
                    {
                        xMax = pt.X;
                        if (yMin <= pt.Y) { if (yMax < pt.Y) yMax = pt.Y; }
                        else yMin = pt.Y;
                    }
                else
                {
                    xMin = pt.X;
                    if (yMin <= pt.Y) { if (yMax < pt.Y) yMax = pt.Y; }
                    else yMin = pt.Y;
                }
            }
            return new FloatRect(xMin, yMin, xMax, yMax);
        }



        // 전자해도 Parsing
        void SerializeSENC(ref byte data0)
        {
            // 32 byte 이동시킴
            data0 = ref Unsafe.Add(ref data0, 32);

            var offset = 0u;
            var size = AsType<int>(ref data0, ref offset);
            if (size > 0)
            {
                var header = AsArray<ST_OBJ_HEADER>(ref data0, ref offset, size);
                var i = 0;
                do
                {
                    var start = header[i].Start + offset;
                    switch (header[i].OBJL)
                    {
                        case 42:        // DEPARE
                            SerializeDEPARE(ref data0, start, header[i].Priority);
                            break;
                        case 71:        // LNDARE
                            SerializeLNDARE(ref data0, start, header[i].Priority);
                            break;
                        case 46:        // DRGARE
                            SerializeDRGARE(ref data0, start, header[i].Priority);
                            break;
                        case 154:       // UNSARE
                            SerializeUNSARE(ref data0, start, header[i].Priority);
                            break;
                        case 43:        // DEPCNT
                            SerializeDEPCNT(ref data0, start, header[i].Priority);
                            break;
                        case 86:        // OBSTRN
                            SerializeOBSTRN(ref data0, start, header[i].Priority);
                            break;
                        case 159:       // WRECKS
                            SerializeWRECKS(ref data0, start, header[i].Priority);
                            break;
                        case 75:        // LIGHTS
                            SerializeLIGHTS(ref data0, start, header[i].Priority);
                            break;
                        case 129:       // SOUNDG
                            SerializeSOUNDG(ref data0, start, header[i].Priority);
                            break;
                        case 122:       // SLCONS
                            SerializeSLCONS(ref data0, start, header[i].Priority);
                            break;
                        default:
                            {
                                if (300 <= header[i].OBJL && header[i].OBJL <= 312)
                                    SerializeMETA(ref data0, start, header[i].Priority);
                                else
                                    SerializeOBJECT(ref data0, start, header[i].Priority);
                            }
                            break;
                    }
                }
                while (++i < size);
            }
        }

        // World map Parsing
        void SerializeWorldMap(ref byte data0)
        {
            data0 = ref Unsafe.Add(ref data0, 32);

            var offset = 0u;
            var size = AsType<int>(ref data0, ref offset);
            if (size > 0)
            {
                var header = AsArray<ST_OBJ_HEADER>(ref data0, ref offset, size);
                var i = 0;
                do SerializeLNDARE(ref data0, header[i].Start + offset, header[i].Priority);
                while (++i < size);
            }
        }

        // Update File 정보 읽어오기 
        //void SerializeUpdate(ref byte data0)
        //{
        //    var offset = 0u;
        //    var size = AsType<int>(ref data0, ref offset);
        //    if (size > 0)
        //    {
        //        var update = GC.AllocateUninitializedArray<ST_UPDATE>(size);
        //        var i = 0;
        //        do
        //        {
        //            update[i].RCID = AsType<int>(ref data0, ref offset);
        //            update[i].PRIM = AsType<byte>(ref data0, ref offset);
        //            update[i].RUIN = AsType<byte>(ref data0, ref offset);
        //            var count = AsType<int>(ref data0, ref offset);
        //            update[i].PathPT = AsArray<Int2D>(ref data0, ref offset, count);
        //        }
        //        while (++i < size);
        //        Over.ListUpdate = update;
        //    }
        //}

        // 공통 Parsing함수들
        void SerializePoints(ref byte data0, ref uint offset, ref ST_POINTS points, ref Float2D[] pathWorld, ref FloatRect[] arrRect)
        {
            var pointHeader = AsType<ST_POINTS_HEADER>(ref data0, ref offset);
            var pointVec = default(ST_POINTS_VEC);

            var nPoint = pointHeader.Pt;
            if (nPoint > 0)
            {
                var arrInt2D = AsArray<Int2D>(ref data0, ref offset, nPoint);
                var arrWorld = GC.AllocateUninitializedArray<Float2D>(nPoint);

                ref var pWorld = ref MemoryMarshal.GetArrayDataReference(arrWorld);
                ref var pInt2D = ref MemoryMarshal.GetArrayDataReference(arrInt2D);
                ref var pEnd = ref add(ref pInt2D, nPoint);
                do
                {
                    const double MUL_FACTOR = 1d / 10000000;

                    // [bsb 막음]
                    //pWorld = EPSG3857.ToWorld(pInt2D.X * MUL_FACTOR, pInt2D.Y * MUL_FACTOR);
                    pWorld = ChartRenderer.ToWorld(pInt2D.X * MUL_FACTOR, pInt2D.Y * MUL_FACTOR);
                    pWorld = ref add(ref pWorld, 1);
                    pInt2D = ref add(ref pInt2D, 1);
                }
                while (Unsafe.IsAddressLessThan(ref pInt2D, ref pEnd));
                pathWorld = arrWorld;

                pointVec.PathPoint = arrInt2D;
            }
            else
                pointVec.PathPoint = Array.Empty<Int2D>();


            var edgeCount = pointHeader.Edge;
            if (edgeCount > 0)
                pointVec.EdgeArr = AsArray<ST_EDGE_INFO>(ref data0, ref offset, edgeCount);
            else
                pointVec.EdgeArr = Array.Empty<ST_EDGE_INFO>();


            var nShape = pointHeader.Shape;
            if (nShape > 0)
            {
                var arrShape = AsArray<ST_SHAPE_INFO>(ref data0, ref offset, nShape);
                var arrBound = GC.AllocateUninitializedArray<FloatRect>(nShape);

                ref var pWorld = ref MemoryMarshal.GetArrayDataReference(pathWorld);
                ref var pBound = ref MemoryMarshal.GetArrayDataReference(arrBound);
                ref var pShape = ref MemoryMarshal.GetArrayDataReference(arrShape);
                ref var pEnd = ref add(ref pShape, nShape);
                do
                {
                    pBound = GetBound(ref pWorld, pShape.PointCount);
                    pWorld = ref add(ref pWorld, pShape.PointCount);
                    pBound = ref add(ref pBound, 1);
                    pShape = ref add(ref pShape, 1);
                }
                while (Unsafe.IsAddressLessThan(ref pShape, ref pEnd));
                arrRect = arrBound;

                pointVec.ShapeArr = arrShape;
            }
            else
                pointVec.ShapeArr = Array.Empty<ST_SHAPE_INFO>();

			points.PointsHeader = pointHeader;
			points.Shape = pointVec;
        }

        unsafe void SerializeCommand(ref byte data0, ref uint offset, ref ST_COM com)
        {
            com.ComSize = AsType<int>(ref data0, ref offset);

            if (com.ComSize > 0)
            {
                com.ArrComInfo = AsArray<ST_COM_INFO>(ref data0, ref offset, com.ComSize);
                com.TotalInfo = AsType<ST_COM_TOTAL_INFO>(ref data0, ref offset);

                if (com.TotalInfo.TotalSY > 0)
                    com.ArrSY = AsArray<ST_COM_SY>(ref data0, ref offset, com.TotalInfo.TotalSY);

                if (com.TotalInfo.TotalLS > 0)
                    com.ArrLS = AsArray<ST_COM_LS>(ref data0, ref offset, com.TotalInfo.TotalLS);

                if (com.TotalInfo.TotalLC > 0)
                    com.ArrLC = AsArray<byte>(ref data0, ref offset, com.TotalInfo.TotalLC);

                if (com.TotalInfo.TotalAC > 0)
                    com.ArrAC = AsArray<ST_COM_AC>(ref data0, ref offset, com.TotalInfo.TotalAC);

                if (com.TotalInfo.TotalAP > 0)
                    com.ArrAP = AsArray<byte>(ref data0, ref offset, com.TotalInfo.TotalAP);

                var nTX = com.TotalInfo.TotalTX;
                if (nTX > 0)
                {
                    var arrTX = GC.AllocateUninitializedArray<ST_COM_TX>(nTX);

                    ref var pTX = ref MemoryMarshal.GetArrayDataReference(arrTX);
                    ref var pEnd = ref add(ref pTX, nTX);
                    do
                    {
                        pTX.Offset = AsType<Int2D>(ref data0, ref offset);
                        pTX.TextAlign = AsType<byte>(ref data0, ref offset);
                        pTX.TextGroup = AsType<byte>(ref data0, ref offset);
                        pTX.TextColorIndex = AsType<byte>(ref data0, ref offset);

                        var nText1 = AsType<int>(ref data0, ref offset);
                        if (nText1 > 0)
                            pTX.Text = AsUTF8(ref data0, ref offset, nText1);

                        var nText2 = AsType<int>(ref data0, ref offset);
                        if (nText2 > 0)
                            pTX.NationalText = AsUTF8(ref data0, ref offset, nText2);

                        pTX = ref add(ref pTX, 1);
                    }
                    while (Unsafe.IsAddressLessThan(ref pTX, ref pEnd));
                    com.ArrTX = arrTX;
                }
            }
        }

        void SerializeEdgeCommand(ref byte data0, ref uint offset, ref List<ST_EDGE_COM> listEdgeCom)
        {
            var size = AsType<int>(ref data0, ref offset);
            if (size > 0)
            {
                var i = 0;
                do
                {
                    var com = new ST_EDGE_COM();

                    com.SY = AsType<byte>(ref data0, ref offset) > 0;
                    if (com.SY == true)
                        com.ComSyIndex = AsType<short>(ref data0, ref offset);

                    com.LS = AsType<byte>(ref data0, ref offset) > 0;
                    if (com.LS == true)
                        com.ComLS = AsType<ST_COM_LS>(ref data0, ref offset);

                    com.LC = AsType<byte>(ref data0, ref offset) > 0;
                    if (com.LC == true)
                        com.ComLcIndex = AsType<byte>(ref data0, ref offset);

                    listEdgeCom.Add(com);
                }
                while (++i < size);
            }
        }

        void SerializeEdgeAttr(ref byte data0, ref uint offset, int edgeSize, ref ST_EDGE_ATTR[] arrEdgeAttr)
        {
            if (edgeSize > 0)
                arrEdgeAttr = AsArray<ST_EDGE_ATTR>(ref data0, ref offset, edgeSize);
            else
                arrEdgeAttr = Array.Empty<ST_EDGE_ATTR>();
        }

        void SerializeEdgeMask(ref byte data0, ref uint offset, ref ST_EDGE_MASK[] arrEdgeMask)
        {
            var size = AsType<int>(ref data0, ref offset);
            if (size > 0)
                arrEdgeMask = AsArray<ST_EDGE_MASK>(ref data0, ref offset, size);
            else
                arrEdgeMask = Array.Empty<ST_EDGE_MASK>();
        }

        void SerializeDangerAttr(ref byte data0, ref uint offset, ref ST_DANGER_ATTR dangerAttr)
        {
            dangerAttr = AsType<ST_DANGER_ATTR>(ref data0, ref offset);
        }

        void SerializeLightsAttr(ref byte data0, ref uint offset, ref ST_LIGHTS_ATTR lightsAttr, ref string litdsn)
        {
            lightsAttr = AsType<ST_LIGHTS_ATTR>(ref data0, ref offset);

            var size = AsType<int>(ref data0, ref offset);
            if (size > 0)
                //litdsn = AsASCII(ref data0, ref offset, size);
                litdsn = AsUTF8(ref data0, ref offset, size);
        }

        // DEPARE를 Parsing하는 함수 
        void SerializeDEPARE(ref byte data0, uint start, byte priority)
        {
            var offset = start;
            var depare = new DEPARE(ChartRenderer)
            {
                Usage = ChartInfo.Usage,
                Header = AsType<ST_DEPARE_HEADER>(ref data0, ref offset)
            };

            SerializePoints(ref data0, ref offset, ref depare.Points, ref depare.PathWorld, ref depare.WorldRect);
            SerializeCommand(ref data0, ref offset, ref depare.Com);
            SerializeEdgeAttr(ref data0, ref offset, depare.Points.PointsHeader.Edge, ref depare.EdgeAttr);

            byte CSPriority = 0;
            DepareChangeContourValue(depare, ref CSPriority);
            Layer[priority].ListDepare.Add(depare);

            if (CSPriority != 0)
            {
                ListDepareCS.Add(new ST_CS
                {
                    LayerNum = priority,
                    Index = Layer[priority].ListDepare.Count - 1,
                    RCID = depare.Header.RCID
                });
            }
        }

        // DRGARE를 Parsing하는 함수 
        void SerializeDRGARE(ref byte data0, uint start, byte priority)
        {
            var offset = start;
            var drgare = new DRGARE(ChartRenderer)
            {
                Usage = ChartInfo.Usage,
                Header = AsType<ST_DRGARE_HEADER>(ref data0, ref offset)
            };

            SerializePoints(ref data0, ref offset, ref drgare.Points, ref drgare.PathWorld, ref drgare.WorldRect);
            SerializeCommand(ref data0, ref offset, ref drgare.Com);
            SerializeEdgeAttr(ref data0, ref offset, drgare.Points.PointsHeader.Edge, ref drgare.EdgeAttr);

            byte csPriority = 0;
            DrgareChangeContourValue(drgare, ref csPriority);
            Layer[priority].ListDrgare.Add(drgare);

            if (csPriority != 0)
            {
                ListDrgareCS.Add(new ST_CS
                {
                    LayerNum = priority,
                    Index = Layer[priority].ListDrgare.Count - 1,
                    RCID = drgare.Header.RCID
                });
            }
        }

        // LNDARE를 Parsing하는 함수 
        void SerializeLNDARE(ref byte data0, uint start, byte priority)
        {
            var offset = start;
            var lndare = new LNDARE(ChartRenderer)
            {
                Usage = ChartInfo.Usage,
                Header = AsType<ST_LNDARE_HEADER>(ref data0, ref offset)
            };

            // Point가 아니면
            if (lndare.Header.PRIM != 1)
                SerializePoints(ref data0, ref offset, ref lndare.Points, ref lndare.PathWorld, ref lndare.WorldRect);

            SerializeCommand(ref data0, ref offset, ref lndare.Com);

            Layer[priority].ListLndare.Add(lndare);
        }

        // UNSARE를 Parsing하는 함수 
        void SerializeUNSARE(ref byte data0, uint start, byte priority)
        {
            var offset = start;
            var unsare = new UNSARE(ChartRenderer)
            {
                Usage = ChartInfo.Usage,
                Header = AsType<ST_UNSARE_HEADER>(ref data0, ref offset)
            };

            SerializePoints(ref data0, ref offset, ref unsare.Points, ref unsare.PathWorld, ref unsare.WorldRect);
            SerializeCommand(ref data0, ref offset, ref unsare.Com);

            Layer[priority].ListUnsare.Add(unsare);
        }

        // DEPCNT를 Parsing하는 함수 
        void SerializeDEPCNT(ref byte data0, uint start, byte priority)
        {
            var offset = start;
            var depcnt = new DEPCNT(ChartRenderer)
            {
                Usage = ChartInfo.Usage,
                Header = AsType<ST_DEPCNT_HEADER>(ref data0, ref offset)
            };

            if (depcnt.Header.VALDCO == float.MaxValue)
                depcnt.Header.VALDCO = 0.0f;

            SerializePoints(ref data0, ref offset, ref depcnt.Points, ref depcnt.PathWorld, ref depcnt.WorldRect);

            Layer[priority].ListDepcnt.Add(depcnt);
        }

        // OBSTRN를 Parsing하는 함수 
        void SerializeOBSTRN(ref byte data0, uint start, byte priority)
        {
            var offset = start;
            var obstrn = new OBSTRN(ChartRenderer)
            {
                Usage = ChartInfo.Usage,
                Header = AsType<ST_OBSTRN_HEADER>(ref data0, ref offset),
                Priority = priority
            };

            if (obstrn.Header.ViewingGroup > 100)
            {
                obstrn.IsLinkEdgeMaskOK = true;
                obstrn.Header.ViewingGroup -= 100;
            }

            // 원본 ViewingGroup을 가지고 있어야 추후 UDWHAZ05 통해 변경된 ViewingGroup을 복원한다.
            obstrn.OriViewingGroup = obstrn.Header.ViewingGroup;

            if (obstrn.Header.PRIM != 1)
                SerializePoints(ref data0, ref offset, ref obstrn.Points, ref obstrn.PathWorld, ref obstrn.WorldRect);

            SerializeCommand(ref data0, ref offset, ref obstrn.Com);
            SerializeDangerAttr(ref data0, ref offset, ref obstrn.DangerAttr);

            obstrn.IsDanger = UDWHAZ05(obstrn, S57ChartSafetyValue.SafetyContour, S57ChartOption.ShallowWaterDangers);
            Layer[priority].ListObstrn.Add(obstrn);

            if (obstrn.IsChangePriority == true)
            {
                ListObstrnCS.Add(new ST_CS
                {
                    LayerNum = priority,
                    Index = Layer[priority].ListObstrn.Count - 1,
                    RCID = obstrn.Header.RCID
                });
            }
        }

        // WRECKS를 Parsing하는 함수 
        void SerializeWRECKS(ref byte data0, uint start, byte priority)
        {
            var offset = start;
            var wrecks = new OBSTRN(ChartRenderer)
            {
                Usage = ChartInfo.Usage,
                IsOBSTRN = false,
                Header = AsType<ST_OBSTRN_HEADER>(ref data0, ref offset),
                Priority = priority
            };

            if (wrecks.Header.ViewingGroup > 100)
            {
                wrecks.IsLinkEdgeMaskOK = true;
                wrecks.Header.ViewingGroup -= 100;
            }

            // 원본 ViewingGroup을 가지고 있어야 추후 UDWHAZ05 통해 변경된 ViewingGroup을 복원한다.
            wrecks.OriViewingGroup = wrecks.Header.ViewingGroup;

            if (wrecks.Header.PRIM != 1)
                SerializePoints(ref data0, ref offset, ref wrecks.Points, ref wrecks.PathWorld, ref wrecks.WorldRect);

            SerializeCommand(ref data0, ref offset, ref wrecks.Com);
            SerializeDangerAttr(ref data0, ref offset, ref wrecks.DangerAttr);

            wrecks.IsDanger = UDWHAZ05(wrecks, S57ChartSafetyValue.SafetyContour, S57ChartOption.ShallowWaterDangers);
            Layer[priority].ListWrecks.Add(wrecks);

            if (wrecks.IsChangePriority == true)
            {
                ListWrecksCS.Add(new ST_CS
                {
                    LayerNum = priority,
                    Index = Layer[priority].ListWrecks.Count - 1,
                    RCID = wrecks.Header.RCID
                });
            }
        }

        // LIGHTS를 Parsing하는 함수 
        void SerializeLIGHTS(ref byte data0, uint start, byte priority)
        {
            var offset = start;
            var lights = new LIGHTS(ChartRenderer)
            {
                Usage = ChartInfo.Usage,
                Header = AsType<ST_LIGHTS_HEADER>(ref data0, ref offset)
            };

            if (lights.Header.RCID == 908 && ChartInfo.ChartName == "GB5X01SW")
            {
                int a = 0;
            }


            SerializeLightsAttr(ref data0, ref offset, ref lights.LightsAttr, ref lights.LITDSN);

            Layer[priority].ListLights.Add(lights);
        }

        // SOUNDG를 Parsing하는 함수 
        void SerializeSOUNDG(ref byte data0, uint start, byte priority)
        {
            var offset = start;
            var soundg = new SOUNDG(ChartRenderer)
            {
                Usage = ChartInfo.Usage,
                Header = AsType<ST_SOUNDG_HEADER>(ref data0, ref offset)
            };

            var size = AsType<int>(ref data0, ref offset);
            if (size > 0)
                soundg.Sounds = AsArray<ST_SOUND>(ref data0, ref offset, size);

            Layer[priority].ListSoundg.Add(soundg);
        }

        // SLCONS를 Parsing하는 함수 
        void SerializeSLCONS(ref byte data0, uint start, byte priority)
        {
            var offset = start;
            var slcons = new SLCONS(ChartRenderer)
            {
                Usage = ChartInfo.Usage,
                Header = AsType<ST_SLCONS_HEADER>(ref data0, ref offset)
            };

            if (slcons.Header.PRIM != 1)
                SerializePoints(ref data0, ref offset, ref slcons.Points, ref slcons.PathWorld, ref slcons.WorldRect);

            SerializeCommand(ref data0, ref offset, ref slcons.Com);
            SerializeEdgeCommand(ref data0, ref offset, ref slcons.ListEdgeCom);

            Layer[priority].ListSlcons.Add(slcons);
        }

        // OBJECT를 Parsing하는 함수 
        void SerializeOBJECT(ref byte data0, uint start, byte priority)
        {
            var offset = start;
            var obj = new OBJECT(ChartRenderer)
            {
                Usage = ChartInfo.Usage,
                Header = AsType<ST_OBJECT_HEADER>(ref data0, ref offset)
            };

            if (obj.Header.GroupLayer > 100)
            {
                obj.Header.GroupLayer -= 100;
                obj.IsLinkEdgeMaskOK = true;
            }

            if (obj.Header.PRIM != 1)
            {
                SerializePoints(ref data0, ref offset, ref obj.Points, ref obj.PathWorld, ref obj.WorldRect);
                SerializeCommand(ref data0, ref offset, ref obj.Com);
                SerializeEdgeMask(ref data0, ref offset, ref obj.EdgeMask);
            }
            else
            {
                SerializeCommand(ref data0, ref offset, ref obj.Com);
            }
            Layer[priority].ListObject.Add(obj);
        }

        // META를 Parsing하는 함수 
        void SerializeMETA(ref byte data0, uint start, byte priority)
        {
            var offset = start;
            var meta = new META(ChartRenderer)
            {
                Usage = ChartInfo.Usage,
                Header = AsType<ST_META_HEADER>(ref data0, ref offset)
            };

            if (meta.Header.ViewingGroup > 100)
            {
                meta.IsLinkEdgeMaskOK = true;
                meta.Header.ViewingGroup -= 100;
            }

            SerializePoints(ref data0, ref offset, ref meta.Points, ref meta.PathWorld, ref meta.WorldRect);
            SerializeCommand(ref data0, ref offset, ref meta.Com);

            // Compilpation Scale이 다른 Meta정보 저장 (M_CSCL)
            if (meta.Header.OBJL == 301 && meta.Header.Cscale > 0 && meta.Header.Cscale != ChartInfo.Scale)
            {
                ListMetaScaleBoundary.Add(meta);
            }

            Layer[priority].ListMeta.Add(meta);
        }

        public bool IsChagePriority(int rcid, byte type)
        {
            bool result = false;
            switch (type)
            {
                case 1: // DRGARE
                    var drgare = ListDrgareCS.Where(p => p.RCID == rcid).FirstOrDefault();
                    result = drgare.Equals(default(ST_CS)) ? false : true;
                    break;
                case 2: // OBSTRN
                    var obstrn = ListObstrnCS.Where(p => p.RCID == rcid).FirstOrDefault();
                    result = obstrn.Equals(default(ST_CS)) ? false : true;
                    break;
                case 3:  // WRECKS
                    var wrecks = ListWrecksCS.Where(p => p.RCID == rcid).FirstOrDefault();
                    result = wrecks.Equals(default(ST_CS)) ? false : true;
                    break;
            }

            return result;
        }

        public void DrawChart(GraphicsContext context, SKCanvasEx canvasUnder, SKCanvasEx canvasOver = null)
        {
            Over.ListText = new();
            Over.ListInform = new();

            for (int i = 1; i <= 8; i++) DrawLayer(context, i, canvasUnder, canvasOver);
        }


        public void DrawWorldMap(GraphicsContext context)
        {
            foreach (var lndare in Layer[1].ListLndare)
            {
                lndare.DrawWorldMap(context);
            }
        }

        public void DrawLayer(GraphicsContext context, int index, SKCanvasEx canvasUnder, SKCanvasEx canvasOver = null)
        {
            foreach (var depare in Layer[index].ListDepare)
            {
                if (canvasOver != null && depare.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                depare.Draw(context);
            }
            if (index == 8) DrawDepareCS(context, canvasUnder, canvasOver);

            foreach (var lndare in Layer[index].ListLndare)
            {
                if (canvasOver != null && lndare.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                lndare.Draw(context, ref Over);
            }

            foreach (var unsare in Layer[index].ListUnsare)
            {
                if (canvasOver != null && unsare.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                unsare.Draw(context);
            }

            foreach (var drgare in Layer[index].ListDrgare)
            {
                if (canvasOver != null && drgare.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                drgare.Draw(context, ref Over);
            }
            if (index == 8) DrawDrgareCS(context, canvasUnder, canvasOver);

            foreach (var obj in Layer[index].ListObject)
            {
                if (canvasOver != null && obj.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                if (obj.EdgeMask != null)
                {
                    foreach (var edge in obj.EdgeMask)
                    {
                        if(IsChagePriority((int)edge.LinkRCID, edge.Type))
                        {
                            obj.IsEdgeMask = true;
                            obj.MaskEdgeNum = edge.EdgeNum;
                            break;
                        }
                    }
                }

                obj.Draw(context, ref Over);
            }

            foreach (var obstrn in Layer[index].ListObstrn)
            {
                if (canvasOver != null && obstrn.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                obstrn.Draw(index, context, ref Over);
            }
            if (index == 8) DrawObstrnCS(context, ref Over, canvasUnder, canvasOver);

            foreach (var wrecks in Layer[index].ListWrecks)
            {
                if (canvasOver != null && wrecks.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                wrecks.Draw(index, context, ref Over);
            }
            if (index == 8) DrawWrecksCS(context, ref Over, canvasUnder, canvasOver);

            foreach (var depcnt in Layer[index].ListDepcnt)
            {
                if (canvasOver != null && depcnt.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                depcnt.Draw(context);
            }

            foreach (var slcons in Layer[index].ListSlcons)
            {
                if (canvasOver != null && slcons.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                slcons.Draw(context, ref Over);
            }

            foreach (var soundg in Layer[index].ListSoundg)
            {
                if (canvasOver != null && soundg.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                soundg.Draw(context, ref Over);
            }

            foreach (var meta in Layer[index].ListMeta)
            {
                if (canvasOver != null && meta.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                meta.Draw(context, ref Over);
            }

            if (index == 3)
            {
                if (canvasOver != null && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                // Overscale Pattern은 Layer 3번이다.
                DrawOverscalePattern(context);
                // Non Official Data 표시
                DrawNonOfficialData(context);
                // Scale Boundary 그리기
                DrawScaleBoundary(context);

            }
        }


        public void DrawDepareCS(GraphicsContext context, SKCanvasEx canvasUnder, SKCanvasEx canvasOver = null)
        {
            foreach (var cs in ListDepareCS)
            {
                var depare = Layer[cs.LayerNum].ListDepare[cs.Index] as DEPARE;

                if (canvasOver != null && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                depare.Draw_CS(context);
            }
        }

        public void DrawDrgareCS(GraphicsContext context, SKCanvasEx canvasUnder, SKCanvasEx canvasOver = null)
        {
            foreach (var cs in ListDrgareCS)
            {
                var drgare = Layer[cs.LayerNum].ListDrgare[cs.Index] as DRGARE;

                if (canvasOver != null && drgare.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                drgare.Draw_CS(context);
            }
        }

        public void DrawObstrnCS(GraphicsContext context, ref ST_OVER over, SKCanvasEx canvasUnder, SKCanvasEx canvasOver = null)
        {
            foreach (var cs in ListObstrnCS)
            {
                var obstrn = Layer[cs.LayerNum].ListObstrn[cs.Index] as OBSTRN;

                if (canvasOver != null && obstrn.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                obstrn.Draw_CS(8, context, ref over);
            }
        }

        public void DrawWrecksCS(GraphicsContext context, ref ST_OVER over, SKCanvasEx canvasUnder, SKCanvasEx canvasOver = null)
        {
            foreach (var cs in ListWrecksCS)
            {
                var wrecks = Layer[cs.LayerNum].ListWrecks[cs.Index] as OBSTRN;

                if (canvasOver != null && wrecks.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                wrecks.Draw_CS(8, context, ref over);
            }
        }

        public void DrawOver(GraphicsContext context, SKCanvasEx canvasUnder, SKCanvasEx canvasOver = null)
        {
            foreach (var light in Layer[8].ListLights)
            {
                if (canvasOver != null && light.Header.RadarOverlay != 0 && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
                else context.TargetCanvas = canvasUnder;

                light.Draw(context, ref Over);
            }

            if (canvasOver != null && S57ChartOption.RadarOverlayOn == true) context.TargetCanvas = canvasOver;
            else context.TargetCanvas = canvasUnder;

            // Manual Update 그리기
            ChartRenderer.DrawManualUpdate(context, ListManualUpdate);

            DrawTextObject(context);
            DrawInformation(context);
        }

        public void DrawTextObject(GraphicsContext context)
        {
            foreach (var txt in Over.ListText)
            {
                float fOffsetX = txt.ComTX.Offset.X * 15.0f;
                float fOffsetY = txt.ComTX.Offset.Y * 15.0f;

                var rgb = WeatherColor.GetColor(txt.ComTX.TextColorIndex);
                context.SetTextColor(new SKColor(rgb.R, rgb.G, rgb.B));

                string strText = "";
                if (S57ChartOption.NationalLanguage == true) strText = txt.ComTX.NationalText;
                else strText = txt.ComTX.Text;

                SKTextHorizental hori = SKTextHorizental.Center;
                SKTextVertical verti = SKTextVertical.Center;

                int ten = txt.ComTX.TextAlign / 10;
                int rem = txt.ComTX.TextAlign % 10;
                switch (ten)
                {
                    case 1://TA_CENTER;
                        hori = SKTextHorizental.Center;
                        break;
                    case 2://TA_RIGHT;
                        hori = SKTextHorizental.Left;
                        break;
                    case 3://TA_LEFT;
                        hori = SKTextHorizental.Right;
                        break;
                }

                switch (rem)
                {
                    case 1://TA_BOTTOM;
                        verti = SKTextVertical.Down;
                        break;
                    case 2://TA_BASELINE;
                        verti = SKTextVertical.Center;
                        break;
                    case 3://TA_TOP;
                        verti = SKTextVertical.Up;
                        break;
                }

                context.DrawText(strText, 15, txt.Pivot.X + fOffsetX, txt.Pivot.Y + fOffsetY, hori, verti);
            }
        }

        public void DrawInformation(GraphicsContext context)
        {
            foreach (var inform in Over.ListInform)
            {
                if (inform.Type == 0 || inform.Type == 1) ChartRenderer.DrawSymbol(context, 182, 0.0f, inform.Pivot, inform.UpdateType);
                else if (inform.Type == 2) ChartRenderer.DrawSymbol(context, 530, 0.0f, inform.Pivot, inform.UpdateType);
                else if (inform.Type == 3)
                {
                    if (inform.ManualUpdateReview == true) ChartRenderer.DrawSymbol(context, 528, 0.0f, inform.Pivot, inform.UpdateType);
                    else ChartRenderer.DrawSymbol(context, 72, 0.0f, inform.Pivot, inform.UpdateType);
                }
            }
        }

        public void DrawOverscalePattern(GraphicsContext context)
        {
            if (S57ChartOption.OverScalePattern == false) return;
            if (ChartRenderer.DisplayLevel == 0) return;
            // DNV에서 Over Scale에 대해서 Standard의 Chart Scale Boundaries와 묶기로 하였다.
            if (ChartRenderer.FindViewingGroup(8) == false) return;
            if (ChartInfo.IsOverScale == false) return;
            // OverScale이 안된 차트이면 빠져나간다.
            //if (ListMetaScaleBoundary.Count <= 0 && ChartInfo.IsOverScale == false) return;

            //var overscale = (ChartInfo.Scale / context.Transform.Scale);
            //if (overscale < 2.0) return;

            //var paths = new List<Float2D[]>();
            //if (ListMetaScaleBoundary.Count > 0)
            //{
            //    Clipper2 clip = new();
            //    foreach (var meta in ListMetaScaleBoundary)
            //    {
            //        clip.AddSubject(ChartInfo.PathsChart);
            //        clip.AddClip(meta.PathWorld);
            //        clip.Execute(2, 0, paths);
            //        break;
            //    }
            //}
            //else paths = ChartInfo.PathsChart.ToList();

            var chartPath = new ChartSKPath();
            int size = ChartInfo.PathsChart.Length;
            //int size = paths.Count;
            for (int i = 0; i < size; i++)
            {
                bool bEnd = false;
                if (i == size - 1) bEnd = true;
                chartPath.CreateSKPathGroup(context.Transform, ChartInfo.PathsChart[i], bEnd);
                //chartPath.CreateSKPathGroup(context.Transform, paths[i], bEnd);
            }

            if (chartPath.MainSkPath != null) ChartRenderer.DrawPattern(context, 17, chartPath.MainSkPath);
            chartPath.Dispose();
        }

        public void DrawScaleBoundary(GraphicsContext context)
        {
            if (ChartRenderer.DisplayLevel == 0) return;
            // DNV에서 Over Scale에 대해서 Standard의 Chart Scale Boundaries와 묶기로 하였다.
            if (ChartRenderer.FindViewingGroup(8) == false) return;

            var rgb = WeatherColor.GetColor("CHGRD");
            context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
            context.SetStrokeWidth(1);

            for (int usage = 0; usage < 6; usage++)
            {
                if (ChartRenderer.DicScaleBoundary.TryGetValue(usage, out var value) == false) continue;
                if (value == null) continue;

                if(value.Length > 0)
                {
                    foreach (var bound in value)
                    {
                        var path = WorldToScreen(context.Transform, bound);
                        if(path != null)
                        {
                            context.DrawPath(path, true);
                        }
                    }
                }
            }

            // 하나의 차트 안에 Compilation Scale이 다른 영역(M_CSCL)이 존재할 때 LS(SOLID, 1, CHGRD)로 Edge를 그리라는 이슈
            foreach(var meta in ListMetaScaleBoundary)
            {
                var path = WorldToScreen(context.Transform, meta.PathWorld);
                if (path != null)  context.DrawPath(path, false);
            }
        }

        public void DrawNonOfficialData(GraphicsContext context)
        {
            if (ChartFinder.IsNonOfficialData(Over.Agency) == true)
            {
                if (ChartInfo.PathsChart != null && ChartInfo.PathsChart.Length > 0)
                {
                    foreach(var pathChart in ChartInfo.PathsChart)
                    {
                        var path = WorldToScreen(context.Transform, pathChart);
                        if (path != null) ChartRenderer.DrawSymbolizedLine(context, 32, path);
                    }
                }
            }
        }

        // DEPARE의 CS처리 함수
        public void DepareChangeContourValue(DEPARE depare, ref byte csPriority)
        {
            float shallow = S57ChartSafetyValue.ShallowContour;
            float safety = S57ChartSafetyValue.SafetyContour;
            float deep = S57ChartSafetyValue.DeepContour;
            bool twoShade = !S57ChartOption.FourDepthShade;
            bool showContourLabel = S57ChartOption.ContourLabel;

            float drval1 = depare.Header.DRVAL1;
            float drval2 = depare.Header.DRVAL2;
            if (drval1 == float.MaxValue)
            {
                drval1 = -1.0f;
            }

            if (drval2 == float.MaxValue)
            {
                drval2 = drval1 + 0.01f;
            }

            ST_COM_AC ac;
            ac.Trans = 0;
            ac.ColorIndex = 255;
            depare.IsShallowPattern = CS_SEABED01(drval1, drval2, shallow, safety, deep, twoShade, ref ac.ColorIndex);
            depare.ListComCsAC.Clear();
            if (ac.ColorIndex != 255) depare.ListComCsAC.Add(ac);

            var edgeCount = depare.EdgeAttr.Length;
            depare.ListEdgeComCS.Clear();
            int index = 0;
            foreach (var edge in depare.EdgeAttr)
            {
                bool isSafe = false;
                bool isUnsafe = false;
                bool isLocSafety = false;

                if (drval1 < safety) isUnsafe = true;
                else isSafe = true;

                if (edge.VALDCO == safety) isLocSafety = true;
                else
                {
                    if (edge.DRVAL1 != float.MaxValue)
                    {
                        if (edge.DRVAL1 < safety) isUnsafe = true;
                        else isSafe = true;
                    }
                    else
                    {
                        if (edge.UNSAFE == true) isUnsafe = true;
                    }
                }

                if (isLocSafety == false)
                {
                    if (isUnsafe == true && isSafe == true) isLocSafety = true;
                }

                if (isLocSafety == true)
                {
                    csPriority = 8;

                    ST_EDGE_COM_CS edgeComCS = new ST_EDGE_COM_CS();
                    edgeComCS.ComLS.ColorIndex = (byte)WeatherColor.GetNameIndex("DEPSC");
                    edgeComCS.ComLS.Width = 2;
                    edgeComCS.ComLS.Style = 0;
                    edgeComCS.EdgeIndex = index;
                    edgeComCS.VALDCO = float.MaxValue;

                    if (index < depare.Points.Shape.EdgeArr.Length && depare.Points.Shape.EdgeArr[index].Quapos == false) edgeComCS.ComLS.Style = 1;

                    if (showContourLabel == true)
                    {
                        if (edge.VALDCO != float.MaxValue)
                        {
                            edgeComCS.VALDCO = edge.VALDCO;
                        }
                    }

                    depare.ListEdgeComCS.Add(edgeComCS);
                }

                index++;
            }
        }

        // DRGARE의 CS처리 함수
        public void DrgareChangeContourValue(DRGARE drgare, ref byte csPriority)
        {
            float shallow = S57ChartSafetyValue.ShallowContour;
            float safety = S57ChartSafetyValue.SafetyContour;
            float deep = S57ChartSafetyValue.DeepContour;
            bool twoShade = !S57ChartOption.FourDepthShade;
            bool showContourLabel = S57ChartOption.ContourLabel;

            ST_COM_AC ac;
            ac.Trans = 0;
            ac.ColorIndex = 255;
            drgare.IsShallowPattern = CS_SEABED01(drgare.Header.DRVAL1, drgare.Header.DRVAL1 + 0.01f, shallow, safety, deep, twoShade, ref ac.ColorIndex);
            drgare.ListComCsAC.Clear();
            if (ac.ColorIndex != 255) drgare.ListComCsAC.Add(ac);

            var edgeCount = drgare.EdgeAttr.Length;
            drgare.LiedgeComCS.Clear();
            int index = 0;
            foreach (var edge in drgare.EdgeAttr)
            {
                bool isSafe = false;
                bool isUnsafe = false;
                bool isLocSafety = false;

                if (drgare.Header.DRVAL1 < safety) isUnsafe = true;
                else isSafe = true;

                if (edge.VALDCO == safety) isLocSafety = true;
                else
                {
                    if (edge.DRVAL1 != float.MaxValue)
                    {
                        if (edge.DRVAL1 < safety) isUnsafe = true;
                        else isSafe = true;
                    }
                    else
                    {
                        if (edge.UNSAFE == true) isUnsafe = true;
                    }
                }

                if (isLocSafety == false)
                {
                    if (isUnsafe == true && isSafe == true) isLocSafety = true;
                }

                if (isLocSafety == true)
                {
                    csPriority = 8;

                    var edgeComCS = new ST_EDGE_COM_CS();
                    edgeComCS.ComLS.ColorIndex = (byte)WeatherColor.GetNameIndex("DEPSC");
                    edgeComCS.ComLS.Width = 2;
                    edgeComCS.ComLS.Style = 0;
                    edgeComCS.EdgeIndex = index;
                    edgeComCS.VALDCO = float.MaxValue;

                    if (index < drgare.Points.Shape.EdgeArr.Length)
                    {
                        if (drgare.Points.Shape.EdgeArr[index].Quapos == false) edgeComCS.ComLS.Style = 1;
                    }

                    if (showContourLabel == true)
                    {
                        if (edge.VALDCO != float.MaxValue)
                        {
                            edgeComCS.VALDCO = edge.VALDCO;
                        }
                    }

                    drgare.LiedgeComCS.Add(edgeComCS);
                }

                index++;
            }
        }

        // CS 관련 처리 함수들
        bool CS_SEABED01(float drval1, float drval2, float shallow, float safety, float deep, bool twoShade, ref byte colorIndex)
        {
            string color = "DEPIT";
            bool isShallow = true;

            if ((drval1 >= 0.0f) && (drval2 > 0.0f))
            {
                color = "DEPVS";
            }

            if (twoShade == true)
            {
                if ((drval1 >= safety) && (drval2 > safety))
                {
                    color = "DEPDW";
                    isShallow = false;
                }
            }
            else
            {
                if ((drval1 >= shallow) && (drval2 > shallow))
                {
                    color = "DEPMS";
                }

                if ((drval1 >= safety) && (drval2 > safety))
                {
                    color = "DEPMD";
                    isShallow = false;
                }

                if ((drval1 >= deep) && (drval2 > deep))
                {
                    color = "DEPDW";
                    isShallow = false;
                }
            }

            colorIndex = (byte)WeatherColor.GetNameIndex(color);

            return isShallow;
        }

        public bool UDWHAZ05(OBSTRN obstrn, float safetyContour, bool showIsolateDanger)
        {
            bool bDanger = false;

            // 초기화 시켜 놓음
            obstrn.IsChangePriority = false;

            if (obstrn.DangerAttr.DEPTH_VALUE <= safetyContour)
            {
                if (obstrn.DangerAttr.DRVAL1 != float.MaxValue && obstrn.DangerAttr.DRVAL1 >= safetyContour)
                {
                    bDanger = true;
                }

                if (bDanger == true)
                {
                    if (obstrn.DangerAttr.WATLEV_1_2 == true)
                    {
                        // No Isolated Danger, Display Base category, Display Priority = 8, Viewing Group = 14050
                        obstrn.Header.ViewingGroup = 1;
                        obstrn.IsChangePriority = true;
                        bDanger = false;
                    }
                    else
                    {
                        // Select = ISODGR01, Display Base category, SCAMIN = infinite, Display Priority = 8, Radar Flag = 'O', Viewing Group = 14010
                        obstrn.Header.RadarOverlay = 1;
                        obstrn.Header.ViewingGroup = 1;
                        obstrn.IsChangePriority = true;
                        obstrn.Header.ScaleMin = int.MaxValue;
                    }
                }
                else
                {
                    if (showIsolateDanger == true)
                    {
                        if (obstrn.DangerAttr.DRVAL1 != float.MaxValue && (obstrn.DangerAttr.DRVAL1 >= 0.0f && obstrn.DangerAttr.DRVAL1 < safetyContour))
                        {
                            bDanger = true;
                        }

                        if (bDanger == true)
                        {
                            if (obstrn.DangerAttr.WATLEV_1_2 == true)
                            {
                                // No Isolated Danger, Display Standard category, Display Priority = 8, Viewing Group = 24050
                                obstrn.Header.ViewingGroup = 12;
                                obstrn.IsChangePriority = true;
                                bDanger = false;
                            }
                            else
                            {
                                // Display Standard category, Display Priority = 8, Radar Flag = 'O', Viewing Group = 24020
                                obstrn.Header.RadarOverlay = 1;
                                obstrn.Header.ViewingGroup = 12;
                                obstrn.IsChangePriority = true;
                                obstrn.Header.ScaleMin = int.MaxValue;
                            }
                        }
                    }
                }
            }

            return bDanger;
        }

        // 새로운 JSON Attribute를 처리하기 위한 함수들
        public bool Query(Transform projection, Float2D point, List<QueryInfo> listPoint, List<QueryInfo> listLine, List<QueryInfo> listArea)
        {
            // 차트명에 대한 Attribute정보를 가져옴
            var dicAttribute = ChartManagerApi.ReadFeatureAttributes(ChartInfo.ChartName);
            if(dicAttribute.Count>0)
            {
                for (int layer = 8; layer >= 0; layer--)  QueryLayer(projection, layer, point, dicAttribute, listPoint, listLine, listArea);

                return listPoint.Count > 0 || listLine.Count > 0 || listArea.Count > 0;
            }

            return false;
        }

        public void QueryLayer(Transform projection, int layerIndex, Float2D point, Dictionary<uint, FeatureAttributeRecord> dicAttributes, List<QueryInfo> listPoint, List<QueryInfo> listLine, List<QueryInfo> listArea)
        {
            foreach (var soundg in Layer[layerIndex].ListSoundg)
            {
                if (soundg.Query(projection, point) == true)
                {
                    if (dicAttributes.TryGetValue((uint)soundg.Header.RCID, out var att))
                    {
                        var info = new QueryInfo((uint)soundg.Header.RCID, ChartInfo.ChartName, $"{att.objectName}({att.objectAcronym})", att.ToReportText(ChartInfo.ChartName), att.TextValue, att.IsPicture);
                        listPoint.Add(info);
                    }
                }
            }

            foreach (var lights in Layer[layerIndex].ListLights)
            {
                if (lights.Query(projection, point) == true)
                {
                    if (dicAttributes.TryGetValue((uint)lights.Header.RCID, out var att))
                    {
                        var info = new QueryInfo((uint)lights.Header.RCID, ChartInfo.ChartName, $"{att.objectName}({att.objectAcronym})", att.ToReportText(ChartInfo.ChartName), att.TextValue, att.IsPicture);
                        listPoint.Add(info);
                    }
                }
            }

            foreach (var slcons in Layer[layerIndex].ListSlcons)
            {
                if (slcons.Query(projection, point) == true)
                {
                    if (dicAttributes.TryGetValue((uint)slcons.Header.RCID, out var att))
                    {
                        var info = new QueryInfo((uint)slcons.Header.RCID, ChartInfo.ChartName, $"{att.objectName}({att.objectAcronym})", att.ToReportText(ChartInfo.ChartName), att.TextValue, att.IsPicture);
                        if (slcons.Header.PRIM == 1) listPoint.Add(info);
                        else if (slcons.Header.PRIM == 2) listLine.Add(info);
                        else if (slcons.Header.PRIM == 3) listArea.Add(info);
                    }
                }
            }

            foreach (var depcnt in Layer[layerIndex].ListDepcnt)
            {
                if (depcnt.Query(projection, point) == true)
                {
                    if (dicAttributes.TryGetValue((uint)depcnt.Header.RCID, out var att))
                    {
                        var info = new QueryInfo((uint)depcnt.Header.RCID, ChartInfo.ChartName, $"{att.objectName}({att.objectAcronym})", att.ToReportText(ChartInfo.ChartName), att.TextValue, att.IsPicture);
                        listLine.Add(info);
                    }
                }
            }

            foreach (var wrecks in Layer[layerIndex].ListWrecks)
            {
                if (wrecks.Query(projection, point) == true)
                {
                    if (dicAttributes.TryGetValue((uint)wrecks.Header.RCID, out var att))
                    {
                        var info = new QueryInfo((uint)wrecks.Header.RCID, ChartInfo.ChartName, $"{att.objectName}({att.objectAcronym})", att.ToReportText(ChartInfo.ChartName), att.TextValue, att.IsPicture);
                        if (wrecks.Header.PRIM == 1) listPoint.Add(info);
                        else if (wrecks.Header.PRIM == 2) listLine.Add(info);
                        else if (wrecks.Header.PRIM == 3) listArea.Add(info);
                    }
                }
            }

            foreach (var obstrn in Layer[layerIndex].ListObstrn)
            {
                if (obstrn.Query(projection, point) == true)
                {
                    if (dicAttributes.TryGetValue((uint)obstrn.Header.RCID, out var att))
                    {
                        var info = new QueryInfo((uint)obstrn.Header.RCID, ChartInfo.ChartName, $"{att.objectName}({att.objectAcronym})", att.ToReportText(ChartInfo.ChartName), att.TextValue, att.IsPicture);
                        if (obstrn.Header.PRIM == 1) listPoint.Add(info);
                        else if (obstrn.Header.PRIM == 2) listLine.Add(info);
                        else if (obstrn.Header.PRIM == 3) listArea.Add(info);
                    }
                }
            }

            foreach (var obj in Layer[layerIndex].ListObject)
            {
                if (obj.Query(projection, point) == true)
                {
                    if (dicAttributes.TryGetValue((uint)obj.Header.RCID, out var att))
                    {
                        var info = new QueryInfo((uint)obj.Header.RCID, ChartInfo.ChartName, $"{att.objectName}({att.objectAcronym})", att.ToReportText(ChartInfo.ChartName), att.TextValue, att.IsPicture);
                        if (obj.Header.PRIM == 1) listPoint.Add(info);
                        else if (obj.Header.PRIM == 2) listLine.Add(info);
                        else if (obj.Header.PRIM == 3) listArea.Add(info);
                    }
                }
            }

            foreach (var drgare in Layer[layerIndex].ListDrgare)
            {
                if (drgare.Query(projection, point) == true)
                {
                    if (dicAttributes.TryGetValue((uint)drgare.Header.RCID, out var att))
                    {
                        var info = new QueryInfo((uint)drgare.Header.RCID, ChartInfo.ChartName, $"{att.objectName}({att.objectAcronym})", att.ToReportText(ChartInfo.ChartName), att.TextValue, att.IsPicture);
                        listArea.Add(info);
                    }
                }
            }

            foreach (var unsare in Layer[layerIndex].ListUnsare)
            {
                if (unsare.Query(projection, point) == true)
                {
                    if (dicAttributes.TryGetValue((uint)unsare.Header.RCID, out var att))
                    {
                        var info = new QueryInfo((uint)unsare.Header.RCID, ChartInfo.ChartName, $"{att.objectName}({att.objectAcronym})", att.ToReportText(ChartInfo.ChartName), att.TextValue, att.IsPicture);
                        listArea.Add(info);
                    }
                }
            }

            foreach (var lndare in Layer[layerIndex].ListLndare)
            {
                if (lndare.Query(projection, point) == true)
                {
                    if (dicAttributes.TryGetValue((uint)lndare.Header.RCID, out var att))
                    {
                        var info = new QueryInfo((uint)lndare.Header.RCID, ChartInfo.ChartName, $"{att.objectName}({att.objectAcronym})", att.ToReportText(ChartInfo.ChartName), att.TextValue, att.IsPicture);
                        if (lndare.Header.PRIM == 1) listPoint.Add(info);
                        else if (lndare.Header.PRIM == 2) listLine.Add(info);
                        else if (lndare.Header.PRIM == 3) listArea.Add(info);
                    }
                }
            }

            foreach (var depare in Layer[layerIndex].ListDepare)
            {
                if (depare.Query(projection, point) == true)
                {
                    if(dicAttributes.TryGetValue((uint)depare.Header.RCID, out var att))
                    {
                        var info = new QueryInfo((uint)depare.Header.RCID, ChartInfo.ChartName, $"{att.objectName}({att.objectAcronym})", att.ToReportText(ChartInfo.ChartName), att.TextValue, att.IsPicture);
                        listArea.Add(info);
                    }
                }
            }

            foreach (var meta in Layer[layerIndex].ListMeta)
            {
                if (meta.Query(projection, point) == true)
                {
                    if (dicAttributes.TryGetValue((uint)meta.Header.RCID, out var att))
                    {
                        var info = new QueryInfo((uint)meta.Header.RCID, ChartInfo.ChartName, $"{att.objectName}({att.objectAcronym})", att.ToReportText(ChartInfo.ChartName), att.TextValue, att.IsPicture);
                        if (meta.Header.PRIM == 1) listPoint.Add(info);
                        else if (meta.Header.PRIM == 2) listLine.Add(info);
                        else if (meta.Header.PRIM == 3) listArea.Add(info);
                    }
                }
            }
        }

        public void ResetQuery()
        {
            ChartRenderer.IsQuery = false;

            for (int i = 1; i <= 8; i++) ResetQueryLayer(i);
        }

        public void ResetQueryLayer(int index)
        {
            foreach (var depare in Layer[index].ListDepare) depare.ResetQuery();
            foreach (var lndare in Layer[index].ListLndare) lndare.ResetQuery();
            foreach (var drgare in Layer[index].ListDrgare) drgare.ResetQuery();
            foreach (var unsare in Layer[index].ListUnsare) unsare.ResetQuery();
            foreach (var obj in Layer[index].ListObject) obj.ResetQuery();
            foreach (var obstrn in Layer[index].ListObstrn) obstrn.ResetQuery();
            foreach (var wrecks in Layer[index].ListWrecks) wrecks.ResetQuery();
            foreach (var depcnt in Layer[index].ListDepcnt) depcnt.ResetQuery();
            foreach (var slcons in Layer[index].ListSlcons) slcons.ResetQuery();
            foreach (var lights in Layer[index].ListLights) lights.ResetQuery();
            foreach (var soundg in Layer[index].ListSoundg) soundg.ResetQuery();
            foreach (var meta in Layer[index].ListMeta) meta.ResetQuery();
        }

        // Manual Update Query를 처리하기 위한 함수들
        public bool MUquery(Transform projection, string chartName, Float2D point, List<ManualUpdateQueryInfo> listPoint)
        {
            for (int layer = 8; layer >= 0; layer--) MUqueryLayer(projection, layer, chartName, point, listPoint);
            return listPoint.Count > 0;
        }

        public void MUqueryLayer(Transform projection, int layerIndex, string chartName, Float2D point, List<ManualUpdateQueryInfo> listPoint)
        {
            int index = -1;
            Float2D pivot = new();
            foreach (var soundg in Layer[layerIndex].ListSoundg)
            {
                if (soundg.MUquery(projection, point, ref index, ref pivot) == true)
                {
                    var info = new ManualUpdateQueryInfo(chartName, ObjectCat.GetObjectName(129), ObjectCat.GetObjectACNM(129));
                    info.PointObj.Pivot = pivot;
                    soundg.GetMUqueryResult(index, info.PointObj.SymbolInfos);
                    listPoint.Add(info);
                }
            }

            foreach (var lights in Layer[layerIndex].ListLights)
            {
                if (lights.MUquery(projection, point, ref pivot) == true)
                {
                    var info = new ManualUpdateQueryInfo(chartName, ObjectCat.GetObjectName(75), ObjectCat.GetObjectACNM(75));
                    info.PointObj.Pivot = pivot;
                    lights.GetMUqueryResult(info.PointObj.SymbolInfos);
                    listPoint.Add(info);
                }
            }

            foreach (var wrecks in Layer[layerIndex].ListWrecks)
            {
                if (wrecks.MUquery(projection, point, ref pivot) == true)
                {
                    var info = new ManualUpdateQueryInfo(chartName, ObjectCat.GetObjectName(wrecks.Header.OBJL), ObjectCat.GetObjectACNM(wrecks.Header.OBJL));
                    info.PointObj.Pivot = pivot;
                    wrecks.GetMUqueryResult(info.PointObj.SymbolInfos);
                    listPoint.Add(info);
                }
            }

            foreach (var obstrn in Layer[layerIndex].ListObstrn)
            {
                if (obstrn.MUquery(projection, point, ref pivot) == true)
                {
                    var info = new ManualUpdateQueryInfo(chartName, ObjectCat.GetObjectName(obstrn.Header.OBJL), ObjectCat.GetObjectACNM(obstrn.Header.OBJL));
                    info.PointObj.Pivot = pivot;
                    obstrn.GetMUqueryResult(info.PointObj.SymbolInfos);
                    listPoint.Add(info);
                }
            }

            foreach (var obj in Layer[layerIndex].ListObject)
            {
                if (obj.MUquery(projection, point, ref pivot) == true)
                {
                    var info = new ManualUpdateQueryInfo(chartName, ObjectCat.GetObjectName(obj.Header.OBJL), ObjectCat.GetObjectACNM(obj.Header.OBJL));
                    info.PointObj.Pivot = pivot;
                    obj.GetMUqueryResult(info.PointObj.SymbolInfos);
                    listPoint.Add(info);
                }
            }
        }

        public Float2D[] WorldToScreen(Transform projection, Float2D[] pathWorld)
        {
            if (pathWorld.Length <= 1) return null;

            const float TOLERANCE = 2f;
            var pathScreen = new Float2D[pathWorld.Length];

            int index = 0;
            var prevPoint = projection.WorldToScreen(pathWorld[index]);
            pathScreen[index++] = prevPoint;
            for (var i = 1; i < pathWorld.Length; i++)
            {
                var nextPoint = projection.WorldToScreen(pathWorld[i]);
                if ((int)(prevPoint.X * TOLERANCE) != (int)(nextPoint.X * TOLERANCE) ||
                    (int)(prevPoint.Y * TOLERANCE) != (int)(nextPoint.Y * TOLERANCE))
                {
                    pathScreen[index++] = nextPoint;
                    prevPoint = nextPoint;
                }
            }

            unsafe
            {
                if (index < 2) return null;

                var result = new Float2D[index];
                fixed (Float2D* pDest = &result[0])
                fixed (Float2D* pSource = &pathScreen[0])
                    Buffer.MemoryCopy(pSource, pDest, 8 * index, 8 * index);
                return result;
            }
        }
    }
}
