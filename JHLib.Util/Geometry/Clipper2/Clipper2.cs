using JHLib.Util.DataStream;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Geometry.Clipper2
{
    public class Clipper2(double scale = 1000)
    {
        [ThreadStatic]
        private static Clipper2 _instance;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Clipper2 InitInstance() => _instance = new Clipper2();
        public static Clipper2 ThreadInstance => _instance ?? InitInstance();


        private readonly VertexCreator _vertexs = new();
        private readonly LocalMinimaList _minimas = new();
        private readonly Scanline _scanlines = new();
        private readonly IntersectNodeList _intersects = new();
        private readonly OutRecList _outrecs = new();
        private readonly HorzSegmentList _horzsegs = new();
        private readonly HorzJoinList _horzjoins = new();
        private readonly Clipper2Paths _paths = new();

        private double _scale = scale;
        private int _vertexIndex;
        private int _minimaIndex;
        private long _currentBottomY;

        private Active _actives;
        private Active _sel;

        private ClipType _cliptype;
        private FillRule _fillrule;
        private bool _existOpenPaths;
        private bool _succeeded;
        private bool _dirtySolution;

        public bool IsCleared => _vertexs.Count == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearSolution()
        {
            _scanlines.Clear();
            _intersects.Clear();
            _outrecs.Clear();
            _horzsegs.Clear();
            _horzjoins.Clear();
            _actives = null;
            _dirtySolution = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadyExecute()
        {
            if (_minimas.Count != 0)
            {
                _scanlines.Set(_minimas.Sort());
                _currentBottomY = 0;
                _minimaIndex = 0;
                _actives = null;
                _sel = null;
                _succeeded = true;
                _dirtySolution = true;
            }
        }

        /// <summary>
        /// 캐시 데이타 및 모든 정보를 초기화 한다
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Clear()
        {
            if (_dirtySolution)
                ClearSolution();

            _vertexs.Clear();
            _minimas.Clear();
            _existOpenPaths = false;
        }

        /// <summary>
        /// 클리핑 작업은 내부적으로 64비트 정수형으로 변환해 진행하므로, 입력값의 소수점 정밀도를 높이기위해 배율을 설정할 수 있다 <para/>
        /// 클리핑 결과는 반대로 입력된 배율만큼을 다시 나누어 출력한다<para/>
        /// 배율 설정은 Clear 상태에서만 변경가능하다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetScale(float scale)
        {
            if (_vertexs.Count != 0)
                throw new Exception("Scale value can only be changed in clear state");
            _scale = scale;
        }

        /// <summary>
        /// 클리핑 대상의 path를 추가한다. 클리핑 대상은 열린도형이나, 닫힌도형이 될 수 있다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSubject(Float2D[] path, bool isOpen = false) =>
            AddCompletedPath(path, PathType.Subject, isOpen);

        /// <summary>
        /// 클리핑 대상의 path를 추가한다. 클리핑 대상은 열린도형이나, 닫힌도형이 될 수 있다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSubject(DataHeaderReader<Float2D> reader, bool isOpen = false) =>
            AddCompletedPath(ref reader.Data0, reader.Count, PathType.Subject, isOpen);

        /// <summary>
        /// 클리핑 대상의 path를 추가한다. 클리핑 대상은 열린도형이나, 닫힌도형이 될 수 있다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSubject(ref Float2D path0, int count, bool isOpen = false) =>
            AddCompletedPath(ref path0, count, PathType.Subject, isOpen);

        /// <summary>
        /// 클리핑 대상의 path를 추가한다. 클리핑 대상은 열린도형이나, 닫힌도형이 될 수 있다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSubject(Float2D[][] paths, bool isOpen = false)
        {
            if (paths != null && paths.Length != 0)
            {
                var i = 0;
                do AddCompletedPath(paths[i], PathType.Subject, isOpen);
                while (++i < paths.Length);
            }
        }

        /// <summary>
        /// 클리핑 대상의 path를 추가한다. 클리핑 대상은 열린도형이나, 닫힌도형이 될 수 있다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSubject(List<Float2D[]> paths, bool isOpen = false)
        {
            if (paths != null && paths.Count != 0)
            {
                var i = 0;
                do AddCompletedPath(paths[i], PathType.Subject, isOpen);
                while (++i < paths.Count);
            }
        }

        /// <summary>
        /// 클리핑 영역의 path를 추가한다. 클리핑 영역은 닫힌영역 형태로만 처리된다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSubject(Clipper2Paths paths, bool isOpen = false)
        {
            if (paths.Count != 0)
            {
                var i = 0;
                do AddCompletedPath(paths[i], PathType.Subject, isOpen);
                while (++i < paths.Count);
            }
        }

        /// <summary>
        /// 클리핑 영역의 path를 추가한다. 클리핑 영역은 닫힌영역 형태로만 처리된다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddClip(Float2D[] path) => AddCompletedPath(path, PathType.Clip, false);

        /// <summary>
        /// 클리핑 영역의 path를 추가한다. 클리핑 영역은 닫힌영역 형태로만 처리된다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddClip(DataHeaderReader<Float2D> reader) =>
            AddCompletedPath(ref reader.Data0, reader.Count, PathType.Clip, false);

        /// <summary>
        /// 클리핑 영역의 path를 추가한다. 클리핑 영역은 닫힌영역 형태로만 처리된다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddClip(ref Float2D path0, int count) =>
            AddCompletedPath(ref path0, count, PathType.Clip, false);

        /// <summary>
        /// 클리핑 영역의 path를 추가한다. 클리핑 영역은 닫힌영역 형태로만 처리된다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddClip(Float2D[][] paths)
        {
            if (paths != null && paths.Length != 0)
            {
                var i = 0;
                do AddCompletedPath(paths[i], PathType.Clip, false);
                while (++i < paths.Length);
            }
        }

        /// <summary>
        /// 클리핑 영역의 path를 추가한다. 클리핑 영역은 닫힌영역 형태로만 처리된다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddClip(List<Float2D[]> paths)
        {
            if (paths != null && paths.Count != 0)
            {
                var i = 0;
                do AddCompletedPath(paths[i], PathType.Clip, false);
                while (++i < paths.Count);
            }
        }


        /// <summary>
        /// 클리핑 영역의 path를 추가한다. 클리핑 영역은 닫힌영역 형태로만 처리된다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddClip(Clipper2Paths paths)
        {
            if (paths.Count != 0)
            {
                var i = 0;
                do AddCompletedPath(paths[i], PathType.Clip, false);
                while (++i < paths.Count);
            }
        }

        /// <summary>
        /// 영역 분할 추가를 위해 내부 Vertex 인덱스를 초기화한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartPartialAdd() => _vertexIndex = _vertexs.Count;

        /// <summary>
        /// 영역의 포인트를 추가한다 <para/>
        /// Finish 호출 전, 추가되는 포인트들은 단일 영역(단일 Path)로 합쳐지므로 <para/>
        /// 여러개의 영역을 추가한다면, 영역 만큼의 Finish 호출을 한다
        /// </summary>
        public void PartialAdd(Float2D point)
        {
            var vidx = _vertexIndex;
            ref var v0 = ref _vertexs.EnsureBucket0(vidx + 1);

            var s = _scale;
            var v = Unsafe.Add(ref v0, vidx);

            v.Pt = new((long)(point.X * s), (long)(point.Y * s));
            if (vidx != _vertexs.Count)
            {
                var p = Unsafe.Add(ref v0, vidx - 1);
                if (p.Pt.X == v.Pt.X && p.Pt.Y == v.Pt.Y) return;
                v.Prev = p;
                p.Next = v;
            }
            v.Flags = 0;

            _vertexIndex = vidx + 1;
        }

        /// <summary>
        /// 영역의 포인트들을 추가한다 <para/>
        /// Finish 호출 전, 추가되는 포인트들은 단일 영역(단일 Path)로 합쳐지므로 <para/>
        /// 여러개의 영역을 추가한다면, 영역 만큼의 Finish 호출을 한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PartialAdd(Float2D[] path)
        {
            if (path != null && path.Length != 0)
                PartialAdd(ref MemoryMarshal.GetArrayDataReference(path), path.Length);
        }

        /// <summary>
        /// 영역의 포인트들을 추가한다 <para/>
        /// Finish 호출 전, 추가되는 포인트들은 단일 영역(단일 Path)로 합쳐지므로 <para/>
        /// 여러개의 영역을 추가한다면, 영역 만큼의 Finish 호출을 한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PartialAdd(DataHeaderReader<Float2D> header)
        {
            if (header.Count != 0)
                PartialAdd(ref header.Data0, header.Count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PartialAdd(ref Float2D path0, int count)
        {
            var vidx = _vertexIndex;
            ref var v0 = ref _vertexs.EnsureBucket0(vidx + count);
            ref var p0 = ref path0;
            ref var pe = ref Unsafe.Add(ref path0, count - 1);

            var s = _scale;
            Vertex p, v = Unsafe.Add(ref v0, vidx);

            v.Pt = new((long)(p0.X * s), (long)(p0.Y * s));
            if (vidx != _vertexs.Count)
            {
                p = Unsafe.Add(ref v0, vidx - 1);
                if (p.Pt.X == v.Pt.X && p.Pt.Y == v.Pt.Y) goto NX;
                v.Prev = p;
                p.Next = v;
            }
            v.Flags = 0;
            p = v;
            v = Unsafe.Add(ref v0, ++vidx);

        NX: if (Unsafe.AreSame(ref p0, ref pe) == false)
            {
                do
                {
                    p0 = ref Unsafe.Add(ref p0, 1);
                    v.Pt = new((long)(p0.X * s), (long)(p0.Y * s));
                    if (p.Pt.X != v.Pt.X || p.Pt.Y != v.Pt.Y)
                    {
                        v.Prev = p;
                        p.Next = v;
                        v.Flags = 0;
                        p = v;
                        v = Unsafe.Add(ref v0, ++vidx);
                    }
                }
                while (Unsafe.AreSame(ref p0, ref pe) == false);
            }
            _vertexIndex = vidx;
        }

        /// <summary>
        /// 영역의 포인트들을 역방향으로 추가한다 <para/>
        /// Finish 호출 전, 추가되는 포인트들은 단일 영역(단일 Path)로 합쳐지므로 <para/>
        /// 여러개의 영역을 추가한다면, 영역 만큼의 Finish 호출을 한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PartialAddReverse(Float2D[] path)
        {
            if (path != null && path.Length != 0)
                PartialAddReverse(ref MemoryMarshal.GetArrayDataReference(path), path.Length);
        }

        /// <summary>
        /// 영역의 포인트들을 역방향으로 추가한다 <para/>
        /// Finish 호출 전, 추가되는 포인트들은 단일 영역(단일 Path)로 합쳐지므로 <para/>
        /// 여러개의 영역을 추가한다면, 영역 만큼의 Finish 호출을 한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PartialAddReverse(DataHeaderReader<Float2D> header)
        {
            if (header.Count != 0)
                PartialAddReverse(ref header.Data0, header.Count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PartialAddReverse(ref Float2D path0, int count)
        {
            var vidx = _vertexIndex;
            ref var v0 = ref _vertexs.EnsureBucket0(vidx + count);
            ref var p0 = ref Unsafe.Add(ref path0, count - 1);
            ref var pe = ref path0;

            var s = _scale;
            Vertex p, v = Unsafe.Add(ref v0, vidx);

            v.Pt = new((long)(p0.X * s), (long)(p0.Y * s));
            if (vidx != _vertexs.Count)
            {
                p = Unsafe.Add(ref v0, vidx - 1);
                if (p.Pt.X == v.Pt.X && p.Pt.Y == v.Pt.Y) goto NX;
                v.Prev = p;
                p.Next = v;
            }
            v.Flags = 0;
            p = v;
            v = Unsafe.Add(ref v0, ++vidx);

        NX: if (Unsafe.AreSame(ref p0, ref pe) == false)
            {
                do
                {
                    p0 = ref Unsafe.Subtract(ref p0, 1);
                    v.Pt = new((long)(p0.X * s), (long)(p0.Y * s));
                    if (p.Pt.X != v.Pt.X || p.Pt.Y != v.Pt.Y)
                    {
                        v.Prev = p;
                        p.Next = v;
                        v.Flags = 0;
                        p = v;
                        v = Unsafe.Add(ref v0, ++vidx);
                    }
                }
                while (Unsafe.AreSame(ref p0, ref pe) == false);
            }
            _vertexIndex = vidx;
        }

        /// <summary>
        /// 현재까지 추가된 모든 포인트 데이타를 Clip 영역으로 완성시킨다 <para/>
        /// 이후 추가되는 포인트나 배열은 새로운 영역으로 처리된다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FinishToClip() => FinishClose(PathType.Clip);

        /// <summary>
        /// 현재까지 추가된 모든 포인트 데이타를 단일 Subject 영역으로 완성시킨다<para/>
        /// 이후 추가되는 포인트나 배열은 새로운 영역으로 처리된다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FinishToSubject(bool isOpen = false)
        {
            if (isOpen)
                FinishOpen(PathType.Subject);
            else
                FinishClose(PathType.Subject);
        }

        /// <summary>
        /// 클리핑을 수행한다. 결과는 result(Float2D 2차 동적 배열)이고, 결과 영역이 없거나 실패할경우 리턴값은 false로 반환한다 <para/>
        /// clipType ： 0 = Intersection ， 1 = Union ， 2 = Difference ， 3 = Xor  <para/>
        /// fillRule ： 0 = EvenOdd ， 1 = NonZero ， 2 = Positive ， 3 = Negative <para/>
        /// </summary>
        /// <param name="clipType">0 = Intersection ， 1 = Union ， 2 = Difference ， 3 = Xor</param>
        /// <param name="fillRule">0 = EvenOdd ， 1 = NonZero ， 2 = Positive ， 3 = Negative</param>
        /// <param name="result">클리핑 결과</param>
        /// <returns>클리핑 성공여부</returns>
        public bool Execute(int clipType, int fillRule, out Float2D[][] result)
        {
            var paths = _paths.ClearGet();
            if (ExecuteInternal(clipType, fillRule, paths))
            {
                result = paths.ToArrayClear();
                return true;
            }
            result = null;
            return false;
        }

        /// <summary>
        /// 클리핑을 수행한다. 결과는 result에 초기화 없이 추가되며, 결과 영역이 없거나 실패할경우 리턴값은 false로 반환한다 <para/>
        /// clipType ： 0 = Intersection ， 1 = Union ， 2 = Difference ， 3 = Xor  <para/>
        /// fillRule ： 0 = EvenOdd ， 1 = NonZero ， 2 = Positive ， 3 = Negative <para/>
        /// </summary>
        /// <param name="clipType">0 = Intersection ， 1 = Union ， 2 = Difference ， 3 = Xor</param>
        /// <param name="fillRule">0 = EvenOdd ， 1 = NonZero ， 2 = Positive ， 3 = Negative</param>
        /// <param name="result">클리핑 결과가 저장될 리스트 (결과는 리스트 초기화 없이 추가됨)</param>
        /// <returns>클리핑 성공여부</returns>
        public bool Execute(int clipType, int fillRule, List<Float2D[]> result)
        {
            var paths = _paths.ClearGet();
            if (ExecuteInternal(clipType, fillRule, paths))
            {
                var i = 0;
                do result.Add(paths[i].ToArray());
                while (++i < paths.Count);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 클리핑을 수행한다. 결과는 result에 초기화 없이 추가되며, 결과 영역이 없거나 실패할경우 리턴값은 false로 반환한다 <para/>
        /// clipType ： 0 = Intersection ， 1 = Union ， 2 = Difference ， 3 = Xor  <para/>
        /// fillRule ： 0 = EvenOdd ， 1 = NonZero ， 2 = Positive ， 3 = Negative <para/>
        /// </summary>
        /// <param name="clipType">0 = Intersection ， 1 = Union ， 2 = Difference ， 3 = Xor</param>
        /// <param name="fillRule">0 = EvenOdd ， 1 = NonZero ， 2 = Positive ， 3 = Negative</param>
        /// <param name="result">클리핑 결과가 저장될 리스트 (결과는 리스트 초기화 없이 추가됨)</param>
        /// <returns>클리핑 성공여부</returns>
        public bool Execute(int clipType, int fillRule, Clipper2Paths result) =>
            ExecuteInternal(clipType, fillRule, result);

        /// <summary>
        /// 클리핑을 수행한다. 결과 영역은 생성하지 않고 클리핑 성공 유무만 반환한다 <para/>
        /// 결과 영역을 만들지 않아 더 빠르며 단순 체크용도로 사용 할 수 있다 <para/>
        /// clipType ： 0 = Intersection ， 1 = Union ， 2 = Difference ， 3 = Xor  <para/>
        /// fillRule ： 0 = EvenOdd ， 1 = NonZero ， 2 = Positive ， 3 = Negative <para/>
        /// </summary>
        /// <param name="clipType">0 = Intersection ， 1 = Union ， 2 = Difference ， 3 = Xor</param>
        /// <param name="fillRule">0 = EvenOdd ， 1 = NonZero ， 2 = Positive ， 3 = Negative</param>
        /// <returns>클리핑 성공여부</returns>
        public bool Execute(int clipType, int fillRule) =>
            ExecuteInternal(clipType, fillRule);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddCompletedPath(Float2D[] path, PathType polytype, bool isOpen)
        {
            if (path != null && path.Length >= 2)
                AddCompletedPathInternal(ref MemoryMarshal.GetArrayDataReference(path), path.Length, polytype, isOpen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddCompletedPath(DataRange<Float2D> range, PathType polytype, bool isOpen)
        {
            if (range.Count >= 2)
                AddCompletedPathInternal(ref range.Data0, range.Count, polytype, isOpen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddCompletedPath(ref Float2D path0, int count, PathType polytype, bool isOpen)
        {
            if (count >= 2)
                AddCompletedPathInternal(ref path0, count, polytype, isOpen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddCompletedPathInternal(ref Float2D path0, int count, PathType polytype, bool isOpen)
        {
            if (isOpen)
            {
                _existOpenPaths = true;
                _vertexs.ReadyVertexOpen(ref path0, count, polytype, _scale, _minimas);
            }
            else
            {
                _vertexs.ReadyVertexClose(ref path0, count, polytype, _scale, _minimas);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void FinishOpen(PathType type)
        {
            var vcnt = _vertexs.Count;
            var vidx = _vertexIndex;
            if (vidx - vcnt >= 2)
            {
                var v0 = _vertexs.Bucket[vcnt];
                var vl = _vertexs.Bucket[vidx - 1];
                v0.Prev = vl;
                vl.Next = v0;

                _vertexs.Count = vidx;
                _existOpenPaths = true;
                Utils.InitVertexOnOpen(v0, type, _minimas);
                return;
            }
            _vertexIndex = vcnt;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void FinishClose(PathType type)
        {
            var vcnt = _vertexs.Count;
            var vidx = _vertexIndex;
            if (vidx - vcnt >= 3)
            {
                var v0 = _vertexs.Bucket[vcnt];
                var vl = _vertexs.Bucket[vidx - 1];
                if (vl.Pt == v0.Pt)
                {
                    if (vidx - vcnt == 3) goto EX;
                    vl = vl.Prev;
                }
                v0.Prev = vl;
                vl.Next = v0;

                _vertexs.Count = vidx;
                Utils.InitVertexOnClose(v0, type, _minimas);
                return;
            }
        EX: _vertexIndex = vcnt;
        }

        private bool BuildPaths(Clipper2Paths result)
        {
            var valid = false;
            var list = _outrecs;
            if (list.Count != 0)
            {
                var mulfactor = 1 / _scale;
                var i = 0;
                do
                {
                    var outrec = list[i];
                    if (outrec.Pts != null)
                    {
                        if (outrec.IsOpen)
                        {
                            valid |= result.AddPathOpen(outrec.Pts, mulfactor);
                        }
                        else
                        {
                            CleanCollinear(outrec);
                            valid |= result.AddPathClose(outrec.Pts, mulfactor);
                        }
                    }
                }
                // CleanCollinear내에서 _outrecs 리스트에 아이템을 추가 할 수 있으므로 매번 Count 체크 필요
                while (++i < list.Count);
            }
            return valid;
        }

        private bool ValidPaths()
        {
            var list = _outrecs;
            if (list.Count != 0)
            {
                var i = 0;
                do
                {
                    var outrec = list[i];
                    if (outrec.Pts != null)
                    {
                        if (outrec.IsOpen)
                        {
                            if (ValidPathOpen(outrec.Pts))
                                return true;
                        }
                        else
                        {
                            CleanCollinear(outrec);
                            if (ValidPathClose(outrec.Pts))
                                return true;
                        }
                    }
                }
                // CleanCollinear내에서 _outrecs 리스트에 아이템을 추가 할 수 있으므로 매번 Count 체크 필요
                while (++i < list.Count);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ValidPathClose(OutPt pts)
        {
            if (pts != null)
            {
                var curr = pts.Next;
                var pcnt = 1;
                do { if (++pcnt > 3) return true; }
                while ((curr = curr.Next) != pts);
                return pcnt == 3 && Utils.IsSmallTriangle(pts) == false;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ValidPathOpen(OutPt pts)
        {
            if (pts != null)
            {
                var curr = pts.Next;
                var pcnt = 1;
                do
                {
                    var prev = curr; curr = curr.Next;
                    if (prev.Pt.X == curr.Pt.X && prev.Pt.Y == curr.Pt.Y) continue;
                    if (++pcnt >= 2) return true;
                }
                while (curr != pts);
            }
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static OutRec GetRealOutRec(OutRec outrec)
        {
            if (outrec != null)
            {
                do if (outrec.Pts != null) break;
                while ((outrec = outrec.Owner) != null);
            }
            return outrec;
        }

        private void CleanCollinear(OutRec outrec)
        {
            var rec = GetRealOutRec(outrec);
            if (rec == null || rec.IsOpen || rec.Pts == null)
                return;

            var op0 = rec.Pts;
            if (op0.Next != op0.Prev && Utils.IsSmallTriangle(op0) == false)
            {
                var op = op0;
                do
                {
                R0: if (Utils.Cross0(op.Prev.Pt, op.Pt, op.Next.Pt)) { goto R1; }
                    if ((op = op.Next) == op0) { goto R2; } else { goto R0; }
                R1: if (Utils.Dot(op.Prev.Pt, op.Pt, op.Next.Pt) <= 0) { goto R3; }
                    if ((op = op.Next) != op0) { goto R0; }
                R2: FixSelfIntersects(rec);
                    return;

                R3: if (rec.Pts == op)
                        rec.Pts = op.Prev;

                    op0 = op.Next;
                    op.Prev.Next = op0;
                    op.Next.Prev = op.Prev;
                    op = op0;
                }
                while (op.Next != op.Prev && Utils.IsSmallTriangle(op) == false);
            }
            rec.Pts = null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void FixSelfIntersects(OutRec outrec)
        {
            var op = outrec.Pts;
            if (op.Next.Next != op.Prev)
            {
                while (true)
                {
                    if (Utils.SegsIntersect(op.Prev.Pt, op.Pt, op.Next.Pt, op.Next.Next.Pt) == false)
                    {
                        if ((op = op.Next) == outrec.Pts)
                            break;
                    }
                    else
                    {
                        DoSplitOp(outrec, op);
                        op = outrec.Pts;
                        if (op == null) break;
                        if (op.Next.Next == op.Prev) break;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoSplitOp(OutRec outrec, OutPt splitOp)
        {
            var prevOp = splitOp.Prev;
            var nextnextOp = splitOp.Next.Next;

            outrec.Pts = prevOp;
            Utils.GetIntersectPoint(prevOp.Pt, splitOp.Pt, splitOp.Next.Pt, nextnextOp.Pt, out Long2D ip);

            var area1 = prevOp.Area();
            var area1Abs = Math.Abs(area1);
            if (area1Abs >= 2)
            {
                var area2 = Utils.AreaTriangle(ip, splitOp.Pt, splitOp.Next.Pt);
                var area2Abs = Math.Abs(area2);

                if (ip == prevOp.Pt || ip == nextnextOp.Pt)
                {
                    nextnextOp.Prev = prevOp;
                    prevOp.Next = nextnextOp;
                }
                else
                {
                    var newOp = new OutPt(ip);
                    newOp.Outrec = outrec;
                    newOp.Prev = prevOp;
                    newOp.Next = nextnextOp;
                    nextnextOp.Prev = newOp;
                    prevOp.Next = newOp;
                }

                if (area2Abs > 1 && (area2Abs > area1Abs || area2 > 0 == area1 > 0))
                {
                    var newOutRec = NewOutRec();
                    newOutRec.Owner = outrec.Owner;
                    splitOp.Outrec = newOutRec;
                    splitOp.Next.Outrec = newOutRec;

                    var newOp = new OutPt(ip);
                    newOp.Outrec = newOutRec;
                    newOp.Prev = splitOp.Next;
                    newOp.Next = splitOp;
                    newOutRec.Pts = newOp;
                    splitOp.Prev = newOp;
                    splitOp.Next.Next = newOp;
                }
            }
            else
            {
                outrec.Pts = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private OutRec NewOutRec() => _outrecs.AddGet();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddHorzSegList(OutPt op)
        {
            if (op.Outrec.IsOpen == false)
                _horzsegs.Add(op);
        }

        private bool IsContributingClosed(Active ae)
        {
            var cliptype = _cliptype;
            if (_fillrule != FillRule.EvenOdd)
            {
                if (_fillrule != FillRule.NonZero)
                {
                    if (_fillrule == FillRule.Positive)
                    {
                        if (ae.WindingSubj == 1)
                            if (cliptype != ClipType.Intersection)
                                if (cliptype != ClipType.Union)
                                    if (cliptype != ClipType.Difference) return true;
                                    else return ae.WindingClip <= 0 == ae.IsSubj;
                                else return ae.WindingClip <= 0;
                            else return ae.WindingClip > 0;
                        else return false;
                    }

                    if (ae.WindingSubj == -1)
                        if (cliptype != ClipType.Intersection)
                            if (cliptype != ClipType.Union)
                                if (cliptype != ClipType.Difference) return true;
                                else return ae.WindingClip >= 0 == ae.IsSubj;
                            else return ae.WindingClip >= 0;
                        else return ae.WindingClip < 0;
                    else return false;
                }

                if (Math.Abs(ae.WindingSubj) != 1)
                    return false;
            }

            if (cliptype != ClipType.Intersection)
                if (cliptype != ClipType.Union)
                    if (cliptype != ClipType.Difference) return true;
                    else return ae.WindingClip == 0 == ae.IsSubj;
                else return ae.WindingClip == 0;
            else return ae.WindingClip != 0;
        }

        private bool IsContributingOpen(Active ae)
        {
            var cliptype = _cliptype;
            if (_fillrule > FillRule.NonZero)
            {
                if (_fillrule == FillRule.Positive)
                {
                    if (cliptype != ClipType.Intersection)
                        if (cliptype != ClipType.Union) return ae.WindingClip <= 0;
                        else return ae.WindingSubj <= 0 && ae.WindingClip <= 0;
                    else return ae.WindingClip > 0;
                }

                if (cliptype != ClipType.Intersection)
                    if (cliptype != ClipType.Union) return ae.WindingClip >= 0;
                    else return ae.WindingSubj >= 0 && ae.WindingClip >= 0;
                else return ae.WindingClip < 0;
            }

            if (cliptype != ClipType.Intersection)
                if (cliptype != ClipType.Union) return ae.WindingClip == 0;
                else return ae.WindingSubj == 0 && ae.WindingClip == 0;
            else return ae.WindingClip != 0;
        }

        private void SetWindCountForClosedPathEdge(Active ae)
        {
            var typ = ae.Pathtype;
            var ae2 = ae.PrevAEL;
            if (ae2 != null) while ((ae2.Pathtype != typ || ae2.IsOpen) && (ae2 = ae2.PrevAEL) != null) ;
            if (ae2 != null)
            {
                if (_fillrule == FillRule.EvenOdd)
                {
                    ae.WindingSubj = ae.WindingDx;
                    ae.WindingClip = ae2.WindingClip;
                    ae2 = ae2.NextAEL;
                }
                else
                {
                    if (ae2.WindingSubj * ae2.WindingDx < 0)
                    {
                        if (Math.Abs(ae2.WindingSubj) > 1)
                        {
                            if (ae2.WindingDx * ae.WindingDx < 0)
                                ae.WindingSubj = ae2.WindingSubj;
                            else
                                ae.WindingSubj = ae2.WindingSubj + ae.WindingDx;
                        }
                        else
                            ae.WindingSubj = ae.IsOpen ? 1 : ae.WindingDx;
                    }
                    else
                    {
                        if (ae2.WindingDx * ae.WindingDx < 0)
                            ae.WindingSubj = ae2.WindingSubj;
                        else
                            ae.WindingSubj = ae2.WindingSubj + ae.WindingDx;
                    }
                    ae.WindingClip = ae2.WindingClip;
                    ae2 = ae2.NextAEL;
                }
            }
            else
            {
                ae.WindingSubj = ae.WindingDx;
                ae2 = _actives;
            }

            if (ae2 != ae)
            {
                if (_fillrule == FillRule.EvenOdd)
                {
                    var w = ae.WindingClip != 0;
                    do w ^= ae2.Pathtype != typ & ae2.IsOpen == false;
                    while ((ae2 = ae2.NextAEL) != ae);
                    ae.WindingClip = Unsafe.As<bool, byte>(ref w);
                }
                else
                {
                    var w = 0;
                    do if (ae2.Pathtype != typ && ae2.IsOpen == false) w += ae2.WindingDx;
                    while ((ae2 = ae2.NextAEL) != ae);
                    ae.WindingClip += w;
                }
            }
        }

        private void SetWindCountForOpenPathEdge(Active ae)
        {
            var ae2 = _actives;
            if (ae2 != ae)
            {
                var subj = 0;
                var clip = 0;

                if (_fillrule == FillRule.EvenOdd)
                {
                    do
                    {
                        if (ae2.IsSubj)
                            if (ae2.IsOpen) continue;
                            else subj++;
                        else clip++;
                    }
                    while ((ae2 = ae2.NextAEL) != ae);
                    ae.WindingSubj = subj & 1;
                    ae.WindingClip = clip & 1;
                }
                else
                {
                    do
                    {
                        if (ae2.IsSubj)
                            if (ae2.IsOpen) continue;
                            else subj += ae2.WindingDx;
                        else clip += ae2.WindingDx;
                    }
                    while ((ae2 = ae2.NextAEL) != ae);
                    ae.WindingSubj += subj;
                    ae.WindingClip += clip;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidAelOrder(Active res, Active com)
        {
            if (com.CurrX == res.CurrX)
            {
                var d = Utils.Cross(res.Top, com.Btm, com.Top);
                if (d == 0)
                {
                    if (res.IsMaxima == false && res.Top.Y > com.Top.Y)
                        return Utils.Cross(com.Btm, res.Top, res.NextVertex().Pt) <= 0;

                    if (com.IsMaxima == false && com.Top.Y > res.Top.Y)
                        return Utils.Cross(com.Btm, com.Top, com.NextVertex().Pt) >= 0;

                    if (res.Btm.Y != com.Btm.Y || res.Minima.Vertex.Pt.Y != com.Btm.Y)
                        return com.IsLeftBound;

                    if (res.IsLeftBound != com.IsLeftBound)
                        return com.IsLeftBound;

                    var rppv = res.PrevPrevVertex();
                    if (Utils.Cross0(rppv.Pt, res.Btm, res.Top))
                        return true;

                    var r = Utils.Cross(rppv.Pt, com.Btm, com.PrevPrevVertex().Pt) > 0;
                    return r == com.IsLeftBound;
                }
                return d < 0;
            }
            return com.CurrX > res.CurrX;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InsertLeftEdge(Active ae)
        {
            var ae2 = _actives;
            if (ae2 != null)
            {
                if (IsValidAelOrder(ae2, ae))
                {
                    if (ae2.NextAEL != null)
                    {
                        do if (IsValidAelOrder(ae2.NextAEL, ae) == false) break;
                        while ((ae2 = ae2.NextAEL).NextAEL != null);
                    }

                    if (ae2.JoinWith == JoinWith.Right)
                        ae2 = ae2.NextAEL;

                    ae.NextAEL = ae2.NextAEL;

                    if (ae2.NextAEL != null)
                        ae2.NextAEL.PrevAEL = ae;

                    ae.PrevAEL = ae2;
                    ae2.NextAEL = ae;
                }
                else
                {
                    ae.PrevAEL = null;
                    ae.NextAEL = ae2;
                    _actives.PrevAEL = ae;
                    _actives = ae;
                }
            }
            else
            {
                ae.PrevAEL = null;
                ae.NextAEL = null;
                _actives = ae;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InsertRightEdge(Active ae, Active ae2)
        {
            ae2.NextAEL = ae.NextAEL;

            if (ae.NextAEL != null)
                ae.NextAEL.PrevAEL = ae2;

            ae2.PrevAEL = ae;
            ae.NextAEL = ae2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InsertLocalMinimaIntoAEL(long bottomY)
        {
            var m = _minimas;
            var i = _minimaIndex;
            if (i < m.Count && m[i].Vertex.Pt.Y == bottomY)
            {
                do InsertLocalMinimaIntoAEL(m[i]);
                while (++i < m.Count && m[i].Vertex.Pt.Y == bottomY);
                _minimaIndex = i;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InsertLocalMinimaIntoAEL(LocalMinima minima)
        {
            Active lb, rb;
            if ((minima.Vertex.Flags & VertexFlags.OpenStart) == 0)
            {
                lb = new(minima, minima.Vertex.Prev, -1);

                if ((minima.Vertex.Flags & VertexFlags.OpenEnd) == 0)
                {
                    rb = new(minima, minima.Vertex.Next, 1);

                    if (lb.IsHorizontal == false)
                    {
                        if (rb.IsHorizontal == false)
                        {
                            if (lb.DX < rb.DX)
                                (rb, lb) = (lb, rb);
                        }
                        else
                        {
                            if (rb.IsHeadingLeftHorz)
                                (rb, lb) = (lb, rb);
                        }
                    }
                    else
                    {
                        if (lb.IsHeadingRightHorz)
                            (rb, lb) = (lb, rb);
                    }
                }
                else
                {
                    rb = null;
                }
            }
            else
            {
                lb = new(minima, minima.Vertex.Next, 1);
                rb = null;
            }

            lb.IsLeftBound = true;
            InsertLeftEdge(lb);

            bool contributing;
            if (lb.IsOpen)
            {
                SetWindCountForOpenPathEdge(lb);
                contributing = IsContributingOpen(lb);
            }
            else
            {
                SetWindCountForClosedPathEdge(lb);
                contributing = IsContributingClosed(lb);
            }

            if (rb != null)
            {
                rb.WindingSubj = lb.WindingSubj;
                rb.WindingClip = lb.WindingClip;
                InsertRightEdge(lb, rb);

                if (contributing)
                {
                    AddLocalMinPoly(lb, rb, lb.Btm, true);

                    if (lb.IsHorizontal == false)
                        CheckJoinLeft(lb, lb.Btm);
                }

                if (rb.NextAEL != null)
                {
                    do
                    {
                        if (IsValidAelOrder(rb.NextAEL, rb) == false) break;
                        IntersectEdges(rb, rb.NextAEL, rb.Btm);
                        SwapPositionsInAEL(rb, rb.NextAEL);
                    }
                    while (rb.NextAEL != null);
                }

                if (rb.IsHorizontal == false)
                {
                    CheckJoinRight(rb, rb.Btm);
                    _scanlines.Add(rb.Top.Y);
                }
                else
                {
                    rb.NextSEL = _sel; _sel = rb;
                }
            }
            else
            {
                if (contributing)
                    StartOpenPath(lb, lb.Btm);
            }

            if (lb.IsHorizontal == false)
            {
                _scanlines.Add(lb.Top.Y);
            }
            else
            {
                lb.NextSEL = _sel; _sel = lb;
            }
        }

        private OutPt AddLocalMinPoly(Active ae1, Active ae2, in Long2D pt, bool isNew = false)
        {
            var outrec = NewOutRec();
            ae1.OutRec = outrec;
            ae2.OutRec = outrec;

            if (ae1.IsOpen)
            {
                outrec.IsOpen = true;
                if (ae1.WindingDx > 0)
                {
                    outrec.FrontEdge = ae1;
                    outrec.BackEdge = ae2;
                }
                else
                {
                    outrec.FrontEdge = ae2;
                    outrec.BackEdge = ae1;
                }
            }
            else
            {
                outrec.IsOpen = false;
                var prevHotEdge = ae1.GetPrevHotEdge();
                if (prevHotEdge != null)
                {
                    outrec.Owner = prevHotEdge.OutRec;
                    if (prevHotEdge.IsFront != isNew)
                    {
                        outrec.FrontEdge = ae1;
                        outrec.BackEdge = ae2;
                    }
                    else
                    {
                        outrec.FrontEdge = ae2;
                        outrec.BackEdge = ae1;
                    }
                }
                else
                {
                    if (isNew)
                    {
                        outrec.FrontEdge = ae1;
                        outrec.BackEdge = ae2;
                    }
                    else
                    {
                        outrec.FrontEdge = ae2;
                        outrec.BackEdge = ae1;
                    }
                }
            }

            var op = new OutPt(pt);
            op.Outrec = outrec;
            op.Prev = op;
            op.Next = op;
            outrec.Pts = op;
            return op;
        }

        private OutPt AddLocalMaxPoly(Active ae1, Active ae2, in Long2D pt)
        {
            if (ae1.IsJoined) Split(ae1, pt);
            if (ae2.IsJoined) Split(ae2, pt);

            if (ae1.IsFront == ae2.IsFront)
            {
                if (ae1.IsOpenEnd == false)
                    if (ae2.IsOpenEnd == false) { _succeeded = false; return null; }
                    else ae2.OutRec.SwapFrontBackSides();
                else ae1.OutRec.SwapFrontBackSides();
            }

            var result = AddOutPt(ae1, pt);
            if (ae1.OutRec != ae2.OutRec)
            {
                if (ae1.IsOpen)
                {
                    if (ae1.WindingDx < 0)
                        JoinOutrecPaths(ae1, ae2);
                    else
                        JoinOutrecPaths(ae2, ae1);
                }
                else
                {
                    if (ae1.OutRec.Idx < ae2.OutRec.Idx)
                        JoinOutrecPaths(ae1, ae2);
                    else
                        JoinOutrecPaths(ae2, ae1);
                }
            }
            else
            {
                var outrec = ae1.OutRec;
                outrec.Pts = result;
                outrec.FrontEdge.OutRec = null;
                outrec.BackEdge.OutRec = null;
                outrec.FrontEdge = null;
                outrec.BackEdge = null;
            }
            return result;
        }

        private static void JoinOutrecPaths(Active ae1, Active ae2)
        {
            var p1s = ae1.OutRec.Pts;
            var p2s = ae2.OutRec.Pts;
            var p1e = p1s.Next;
            var p2e = p2s.Next;

            if (ae1.IsFront)
            {
                p2e.Prev = p1s;
                p1s.Next = p2e;
                p2s.Next = p1e;
                p1e.Prev = p2s;

                ae1.OutRec.Pts = p2s;
                ae1.OutRec.FrontEdge = ae2.OutRec.FrontEdge;
                if (ae1.OutRec.FrontEdge != null)
                    ae1.OutRec.FrontEdge.OutRec = ae1.OutRec;
            }
            else
            {
                p1e.Prev = p2s;
                p2s.Next = p1e;
                p1s.Next = p2e;
                p2e.Prev = p1s;

                ae1.OutRec.BackEdge = ae2.OutRec.BackEdge;
                if (ae1.OutRec.BackEdge != null)
                    ae1.OutRec.BackEdge.OutRec = ae1.OutRec;
            }

            ae2.OutRec.FrontEdge = null;
            ae2.OutRec.BackEdge = null;
            ae2.OutRec.Pts = null;

            SetOwner(ae2.OutRec, ae1.OutRec);

            if (ae1.IsOpenEnd)
            {
                ae2.OutRec.Pts = ae1.OutRec.Pts;
                ae1.OutRec.Pts = null;
            }
            ae1.OutRec = null;
            ae2.OutRec = null;
        }

        private OutPt AddOutPt(Active ae, in Long2D pt)
        {
            var outrec = ae.OutRec;
            var toFront = ae.IsFront;
            var opFront = outrec.Pts;
            var opBack = opFront.Next;

            if (toFront)
            {
                if (pt == opFront.Pt)
                    return opFront;
            }
            else
            {
                if (pt == opBack.Pt)
                    return opBack;
            }

            var newOp = new OutPt(pt);
            newOp.Outrec = outrec;
            newOp.Prev = opFront;
            newOp.Next = opBack;
            opBack.Prev = newOp;
            opFront.Next = newOp;

            if (toFront)
                outrec.Pts = newOp;

            return newOp;
        }

        private OutPt StartOpenPath(Active ae, in Long2D pt)
        {
            var outrec = NewOutRec();
            outrec.IsOpen = true;

            if (ae.WindingDx > 0)
                outrec.FrontEdge = ae;
            else
                outrec.BackEdge = ae;
            ae.OutRec = outrec;

            var op = new OutPt(pt);
            op.Outrec = outrec;
            op.Prev = op;
            op.Next = op;
            outrec.Pts = op;
            return op;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateEdgeIntoAEL(Active ae)
        {
            ae.Btm = ae.Top;
            ae.VertexTop = ae.NextVertex();
            ae.Top = ae.VertexTop.Pt;
            ae.CurrX = ae.Btm.X;
            ae.UpdateHorizontal(ae.Btm, ae.Top);

            if (ae.JoinWith != JoinWith.None) Split(ae, ae.Btm);
            if (ae.IsHorizontal == false)
            {
                _scanlines.Add(ae.Top.Y);
                CheckJoinLeft(ae, ae.Btm);
                CheckJoinRight(ae, ae.Btm, true);
            }
        }

        private OutPt IntersectEdges(Active ae1, Active ae2, Long2D pt)
        {
            OutPt result;
            if (_existOpenPaths && (ae1.IsOpen | ae2.IsOpen))
            {
                if (ae1.IsOpen & ae2.IsOpen) return null;
                if (ae2.IsOpen) (ae2, ae1) = (ae1, ae2);
                if (ae2.IsJoined) Split(ae2, pt);

                if (_cliptype == ClipType.Union)
                {
                    if (ae2.IsHotEdge == false)
                        return null;
                }
                else
                {
                    if (ae2.Minima.Pathtype == PathType.Subject)
                        return null;
                }

                if (_fillrule > FillRule.NonZero)
                {
                    if (_fillrule == FillRule.Positive)
                    {
                        if (ae2.WindingSubj != 1)
                            return null;
                    }
                    else
                    {
                        if (ae2.WindingSubj != -1)
                            return null;
                    }
                }
                else
                {
                    if (Math.Abs(ae2.WindingSubj) != 1)
                        return null;
                }

                if (ae1.IsHotEdge == false)
                {
                    if (pt == ae1.Minima.Vertex.Pt && ae1.Minima.Vertex.IsOpenEnd == false)
                    {
                        var ae3 = ae1.FindEdgeWithMatchingLocMin();
                        if (ae3 != null && ae3.IsHotEdge)
                        {
                            ae1.OutRec = ae3.OutRec;
                            if (ae1.WindingDx > 0)
                            {
                                ae3.OutRec.FrontEdge = ae1;
                                ae3.OutRec.BackEdge = ae3;
                            }
                            else
                            {
                                ae3.OutRec.FrontEdge = ae3;
                                ae3.OutRec.BackEdge = ae1;
                            }
                            return ae3.OutRec.Pts;
                        }
                    }
                    return StartOpenPath(ae1, pt);
                }

                result = AddOutPt(ae1, pt);
                if (ae1.IsFront)
                    ae1.OutRec.FrontEdge = null;
                else
                    ae1.OutRec.BackEdge = null;
                ae1.OutRec = null;
                return result;
            }

            if (ae1.IsJoined) Split(ae1, pt);
            if (ae2.IsJoined) Split(ae2, pt);

            if (ae1.Pathtype == ae2.Pathtype)
            {
                if (_fillrule == FillRule.EvenOdd)
                {
                    (ae2.WindingSubj, ae1.WindingSubj) =
                        (ae1.WindingSubj, ae2.WindingSubj);
                }
                else
                {
                    if (ae1.WindingSubj + ae2.WindingDx == 0)
                        ae1.WindingSubj = -ae1.WindingSubj;
                    else
                        ae1.WindingSubj += ae2.WindingDx;

                    if (ae2.WindingSubj - ae1.WindingDx == 0)
                        ae2.WindingSubj = -ae2.WindingSubj;
                    else
                        ae2.WindingSubj -= ae1.WindingDx;
                }
            }
            else
            {
                if (_fillrule == FillRule.EvenOdd)
                {
                    ae1.WindingClip = ae1.WindingClip == 0 ? 1 : 0;
                    ae2.WindingClip = ae2.WindingClip == 0 ? 1 : 0;
                }
                else
                {
                    ae1.WindingClip += ae2.WindingDx;
                    ae2.WindingClip -= ae1.WindingDx;
                }
            }

            int w1s, w2s;
            if (_fillrule > FillRule.NonZero)
                if (_fillrule == FillRule.Positive)
                {
                    w1s = ae1.WindingSubj;
                    w2s = ae2.WindingSubj;
                }
                else
                {
                    w1s = -ae1.WindingSubj;
                    w2s = -ae2.WindingSubj;
                }
            else
            {
                w1s = Math.Abs(ae1.WindingSubj);
                w2s = Math.Abs(ae2.WindingSubj);
            }

            if (ae1.IsHotEdge == false && (w1s & ~1) != 0 ||
                ae2.IsHotEdge == false && (w2s & ~1) != 0) return null;

            if (ae1.IsHotEdge && ae2.IsHotEdge)
            {
                if ((w1s & ~1) == 0 && (w2s & ~1) == 0 &&
                    (ae1.Pathtype == ae2.Pathtype || _cliptype == ClipType.Xor))
                {
                    if (ae1.IsFront || ae1.OutRec == ae2.OutRec)
                    {
                        result = AddLocalMaxPoly(ae1, ae2, pt);
                        AddLocalMinPoly(ae1, ae2, pt);
                        return result;
                    }
                    else
                    {
                        result = AddOutPt(ae1, pt);
                        AddOutPt(ae2, pt);
                        SwapOutrecs(ae1, ae2);
                        return result;
                    }
                }
                return AddLocalMaxPoly(ae1, ae2, pt);
            }
            else if (ae1.IsHotEdge)
            {
                result = AddOutPt(ae1, pt);
                SwapOutrecs(ae1, ae2);
                return result;
            }
            else if (ae2.IsHotEdge)
            {
                result = AddOutPt(ae2, pt);
                SwapOutrecs(ae1, ae2);
                return result;
            }
            else
            {
                if (ae1.Pathtype != ae2.Pathtype)
                    return AddLocalMinPoly(ae1, ae2, pt);

                if (w1s == 1 && w2s == 1)
                {
                    int w1c, w2c;
                    if (_fillrule > FillRule.NonZero)
                        if (_fillrule == FillRule.Positive)
                        {
                            w1c = ae1.WindingClip;
                            w2c = ae2.WindingClip;
                        }
                        else
                        {
                            w1c = -ae1.WindingClip;
                            w2c = -ae2.WindingClip;
                        }
                    else
                    {
                        w1c = Math.Abs(ae1.WindingClip);
                        w2c = Math.Abs(ae2.WindingClip);
                    }

                    if (_cliptype != ClipType.Intersection)
                        if (_cliptype != ClipType.Union)
                            if (_cliptype != ClipType.Difference)
                            {
                                return AddLocalMinPoly(ae1, ae2, pt);
                            }
                            else
                            {
                                if ((ae1.IsClip && w1c > 0 && w2c > 0) ||
                                    (ae1.IsSubj && w1c <= 0 && w2c <= 0))
                                    return AddLocalMinPoly(ae1, ae2, pt);
                            }
                        else
                        {
                            if (w1c <= 0 || w2c <= 0)
                                return AddLocalMinPoly(ae1, ae2, pt);
                        }
                    else
                    {
                        if (w1c > 0 && w2c > 0)
                            return AddLocalMinPoly(ae1, ae2, pt);
                    }
                }
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DeleteFromAEL(Active ae)
        {
            var prev = ae.PrevAEL;
            var next = ae.NextAEL;

            if (prev != null)
            {
                prev.NextAEL = next;
                if (next != null)
                    next.PrevAEL = prev;
            }
            else if (next != null)
            {
                _actives = next;
                next.PrevAEL = null;
            }
            else if (ae == _actives)
            {
                _actives = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AdjustCurrXAndCopyToSEL(long topY)
        {
            var ae = _actives; _sel = ae;
            if (ae != null)
            {
                do
                {
                    ae.PrevSEL = ae.PrevAEL;
                    ae.NextSEL = ae.NextAEL;
                    ae.Jump = ae.NextSEL;

                    if (ae.JoinWith == JoinWith.Left)
                        ae.CurrX = ae.PrevAEL.CurrX;
                    else
                        ae.CurrX = ae.TopX(topY);
                }
                while ((ae = ae.NextAEL) != null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SwapPositionsInAEL(Active ae1, Active ae2)
        {
            var next = ae2.NextAEL;
            if (next != null)
                next.PrevAEL = ae1;

            var prev = ae1.PrevAEL;
            if (prev != null)
                prev.NextAEL = ae2;
            else
                _actives = ae2;

            ae2.PrevAEL = prev;
            ae2.NextAEL = ae1;
            ae1.PrevAEL = ae2;
            ae1.NextAEL = next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ResetHorzDirection(Active horz, Vertex vertexMax, out long leftX, out long rightX)
        {
            if (horz.Btm.X != horz.Top.X)
            {
                if (horz.CurrX < horz.Top.X)
                {
                    leftX = horz.CurrX;
                    rightX = horz.Top.X;
                    return true;
                }
                leftX = horz.Top.X;
                rightX = horz.CurrX;
                return false;
            }
            leftX = horz.CurrX;
            rightX = horz.CurrX;
            var ae = horz.NextAEL;
            if (ae != null)
            {
                do if (ae.VertexTop == vertexMax) return true;
                while ((ae = ae.NextAEL) != null);
            }
            return false;
        }

        private void DoHorizontal(Active horz)
        {
            var Y = horz.Btm.Y;
            var vertexMax = horz.GetCurrYMaximaVertex();
            if (vertexMax != null && horz.IsOpen == false && vertexMax != horz.VertexTop)
                horz.TrimHorz();

            var isLeftToRight = ResetHorzDirection(horz, vertexMax, out long leftX, out long rightX);
            if (horz.IsHotEdge)
                AddHorzSegList(AddOutPt(horz, new(horz.CurrX, Y)));

            while (true)
            {
                var ae = isLeftToRight ? horz.NextAEL : horz.PrevAEL;
                if (ae != null)
                {
                    do
                    {
                        if (ae.VertexTop == vertexMax)
                        {
                            if (horz.IsHotEdge && ae.IsJoined) Split(ae, ae.Top);
                            if (horz.IsHotEdge)
                            {
                                if (horz.VertexTop != vertexMax)
                                {
                                    do
                                    {
                                        AddOutPt(horz, horz.Top);
                                        UpdateEdgeIntoAEL(horz);
                                    }
                                    while (horz.VertexTop != vertexMax);
                                }

                                if (isLeftToRight)
                                    AddLocalMaxPoly(horz, ae, horz.Top);
                                else
                                    AddLocalMaxPoly(ae, horz, horz.Top);
                            }
                            DeleteFromAEL(ae);
                            DeleteFromAEL(horz);
                            return;
                        }

                        Long2D pt;
                        if (vertexMax != horz.VertexTop || horz.IsOpenEnd)
                        {
                            if (isLeftToRight && ae.CurrX > rightX ||
                                isLeftToRight == false && ae.CurrX < leftX)
                                break;

                            if (ae.CurrX == horz.Top.X && ae.IsHorizontal == false)
                            {
                                pt = horz.NextVertex().Pt;
                                if (ae.IsOpen && (ae.Pathtype != horz.Pathtype) && ae.IsHotEdge == false)
                                {
                                    if ((isLeftToRight && ae.TopX(pt.Y) > pt.X) ||
                                        (isLeftToRight == false && ae.TopX(pt.Y) < pt.X))
                                        break;
                                }
                                else
                                {
                                    if ((isLeftToRight && ae.TopX(pt.Y) >= pt.X) ||
                                        (isLeftToRight == false && ae.TopX(pt.Y) <= pt.X))
                                        break;
                                }
                            }
                        }

                        pt = new(ae.CurrX, Y);
                        if (isLeftToRight)
                        {
                            IntersectEdges(horz, ae, pt);
                            SwapPositionsInAEL(horz, ae);
                            CheckJoinLeft(ae, pt);
                            horz.CurrX = ae.CurrX;
                            ae = horz.NextAEL;
                        }
                        else
                        {
                            IntersectEdges(ae, horz, pt);
                            SwapPositionsInAEL(ae, horz);
                            CheckJoinRight(ae, pt);
                            horz.CurrX = ae.CurrX;
                            ae = horz.PrevAEL;
                        }

                        if (horz.IsHotEdge)
                            AddHorzSegList(horz.GetLastOp());
                    }
                    while (ae != null);
                }

                if (horz.IsOpenEnd)
                {
                    if (horz.IsHotEdge)
                    {
                        AddOutPt(horz, horz.Top);
                        if (horz.IsFront)
                            horz.OutRec.FrontEdge = null;
                        else
                            horz.OutRec.BackEdge = null;
                        horz.OutRec = null;
                    }
                    DeleteFromAEL(horz);
                    return;
                }

                if (horz.NextVertex().Pt.Y != horz.Top.Y)
                    break;

                if (horz.IsHotEdge)
                    AddOutPt(horz, horz.Top);

                UpdateEdgeIntoAEL(horz);

                if (horz.IsOpen == false && horz.HorzIsSpike())
                    horz.TrimHorz();

                isLeftToRight = ResetHorzDirection(horz, vertexMax, out leftX, out rightX);
            }

            if (horz.IsHotEdge)
                AddHorzSegList(AddOutPt(horz, horz.Top));

            UpdateEdgeIntoAEL(horz);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoTopOfScanbeam(long y)
        {
            var sel = default(Active);
            var ae = _actives;
            if (ae != null)
            {
                do
                {
                    if (ae.Top.Y == y)
                    {
                        ae.CurrX = ae.Top.X;
                        if (ae.IsMaxima == false)
                        {
                            if (ae.IsHotEdge) AddOutPt(ae, ae.Top); UpdateEdgeIntoAEL(ae);
                            if (ae.IsHorizontal) { ae.NextSEL = sel; sel = ae; }
                            ae = ae.NextAEL;
                        }
                        else
                        {
                            ae = DoMaxima(ae);
                        }
                    }
                    else
                    {
                        ae.CurrX = ae.TopX(y);
                        ae = ae.NextAEL;
                    }
                }
                while (ae != null);
            }
            _sel = sel;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Active DoMaxima(Active ae)
        {
            var prev = ae.PrevAEL;
            var next = ae.NextAEL;

            if (ae.IsOpenEnd)
            {
                if (ae.IsHotEdge) AddOutPt(ae, ae.Top);
                if (ae.IsHorizontal == false)
                {
                    if (ae.IsHotEdge)
                    {
                        if (ae.IsFront)
                            ae.OutRec.FrontEdge = null;
                        else
                            ae.OutRec.BackEdge = null;
                        ae.OutRec = null;
                    }
                    DeleteFromAEL(ae);
                }
                return next;
            }

            var maxPair = ae.GetMaximaPair();
            if (maxPair != null)
            {
                if (ae.IsJoined) Split(ae, ae.Top);
                if (maxPair.IsJoined) Split(maxPair, maxPair.Top);

                if (next != maxPair)
                {
                    do
                    {
                        IntersectEdges(ae, next, ae.Top);
                        SwapPositionsInAEL(ae, next);
                    }
                    while ((next = ae.NextAEL) != maxPair);
                }

                if (ae.IsHotEdge) AddLocalMaxPoly(ae, maxPair, ae.Top);
                if (ae.IsOpen)
                {
                    DeleteFromAEL(maxPair);
                    DeleteFromAEL(ae);
                }
                else
                {
                    DeleteFromAEL(ae);
                    DeleteFromAEL(maxPair);
                }
                return prev == null ? _actives : prev.NextAEL;
            }
            return next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Split(Active e, in Long2D currPt)
        {
            if (e.JoinWith == JoinWith.Right)
            {
                e.JoinWith = JoinWith.None;
                e.NextAEL.JoinWith = JoinWith.None;
                AddLocalMinPoly(e, e.NextAEL, currPt, true);
            }
            else
            {
                e.JoinWith = JoinWith.None;
                e.PrevAEL.JoinWith = JoinWith.None;
                AddLocalMinPoly(e.PrevAEL, e, currPt, true);
            }
        }

        private void CheckJoinLeft(Active e, in Long2D pt, bool checkCurrX = false)
        {
            var p = e.PrevAEL;
            if (p != null)
            {
                if (e.IsOpen == false && e.IsHotEdge && p.IsOpen == false && p.IsHotEdge)
                {
                    if (pt.Y >= e.Top.Y + 2 && pt.Y >= p.Top.Y + 2 || e.Btm.Y <= pt.Y && p.Btm.Y <= pt.Y)
                    {
                        if (checkCurrX)
                        {
                            if (Utils.PerpendicDistFromLineSqrd(pt, p.Btm, p.Top) > 0.25)
                                return;
                        }
                        else
                        {
                            if (e.CurrX != p.CurrX)
                                return;
                        }

                        if (Utils.Cross0(e.Top, pt, p.Top))
                        {
                            if (e.OutRec.Idx == p.OutRec.Idx)
                                AddLocalMaxPoly(p, e, pt);
                            else if (e.OutRec.Idx < p.OutRec.Idx)
                                JoinOutrecPaths(e, p);
                            else
                                JoinOutrecPaths(p, e);

                            p.JoinWith = JoinWith.Right;
                            e.JoinWith = JoinWith.Left;
                        }
                    }
                }
            }
        }

        private void CheckJoinRight(Active e, in Long2D pt, bool checkCurrX = false)
        {
            var n = e.NextAEL;
            if (n != null)
            {
                if (e.IsOpen == false && e.IsHotEdge && e.IsJoined == false && n.IsOpen == false && n.IsHotEdge)
                {
                    if (pt.Y >= e.Top.Y + 2 && pt.Y >= n.Top.Y + 2 || e.Btm.Y <= pt.Y && n.Btm.Y <= pt.Y)
                    {
                        if (checkCurrX)
                        {
                            if (Utils.PerpendicDistFromLineSqrd(pt, n.Btm, n.Top) > 0.25)
                                return;
                        }
                        else
                        {
                            if (e.CurrX != n.CurrX)
                                return;
                        }

                        if (Utils.Cross0(e.Top, pt, n.Top))
                        {
                            if (e.OutRec.Idx == n.OutRec.Idx)
                                AddLocalMaxPoly(e, n, pt);
                            else if (e.OutRec.Idx < n.OutRec.Idx)
                                JoinOutrecPaths(e, n);
                            else
                                JoinOutrecPaths(n, e);

                            e.JoinWith = JoinWith.Right;
                            n.JoinWith = JoinWith.Left;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool SetHorzSegHeadingForward(HorzSegment hs, OutPt opP, OutPt opN)
        {
            if (opP.Pt.X != opN.Pt.X)
            {
                if (opP.Pt.X < opN.Pt.X)
                {
                    hs.LeftOp = opP;
                    hs.RightOp = opN;
                    hs.LeftToRight = true;
                    return true;
                }
                else
                {
                    hs.LeftOp = opN;
                    hs.RightOp = opP;
                    hs.LeftToRight = false;
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UpdateHorzSegment(HorzSegment hs)
        {
            var op = hs.LeftOp;
            var outrec = GetRealOutRec(op.Outrec);
            var currY = op.Pt.Y;
            var opP = op;
            var opN = op;

            if (outrec.FrontEdge != null)
            {
                var opA = outrec.Pts;
                var opZ = opA.Next;

                while (opP != opZ && opP.Prev.Pt.Y == currY)
                    opP = opP.Prev;
                while (opN != opA && opN.Next.Pt.Y == currY)
                    opN = opN.Next;
            }
            else
            {
                while (opP.Prev != opN && opP.Prev.Pt.Y == currY)
                    opP = opP.Prev;
                while (opN.Next != opP && opN.Next.Pt.Y == currY)
                    opN = opN.Next;
            }

            if (SetHorzSegHeadingForward(hs, opP, opN) && hs.LeftOp.Horz == null)
            {
                hs.LeftOp.Horz = hs;
                return true;
            }
            else
            {
                hs.RightOp = null;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static OutPt DuplicateOp(OutPt op, bool insertAfter)
        {
            var result = new OutPt(op.Pt);
            result.Outrec = op.Outrec;

            if (insertAfter)
            {
                result.Next = op.Next;
                result.Next.Prev = result;
                result.Prev = op;
                op.Next = result;
                return result;
            }
            else
            {
                result.Prev = op.Prev;
                result.Prev.Next = result;
                result.Next = op;
                op.Prev = result;
                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SwapOutrecs(Active ae1, Active ae2)
        {
            var or1 = ae1.OutRec;
            var or2 = ae2.OutRec;

            if (or1 != or2)
            {
                if (or1 != null)
                {
                    if (ae1 == or1.FrontEdge)
                        or1.FrontEdge = ae2;
                    else
                        or1.BackEdge = ae2;
                }
                if (or2 != null)
                {
                    if (ae2 == or2.FrontEdge)
                        or2.FrontEdge = ae1;
                    else
                        or2.BackEdge = ae1;
                }
                ae1.OutRec = or2;
                ae2.OutRec = or1;
            }
            else
            {
                (or1.BackEdge, or1.FrontEdge) = (or1.FrontEdge, or1.BackEdge);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetOwner(OutRec outrec, OutRec newOwner)
        {
            if (newOwner.Owner != null)
            {
                do
                {
                    if (newOwner.Owner.Pts != null) break;
                    newOwner.Owner = newOwner.Owner.Owner;
                }
                while (newOwner.Owner != null);
            }

            var tmp = newOwner;
            do if (tmp == outrec) { newOwner.Owner = outrec.Owner; break; }
            while ((tmp = tmp.Owner) != null);
            outrec.Owner = newOwner;
        }

        private bool ExecuteInternal(int clipType, int fillRule, Clipper2Paths result)
        {
            if ((uint)clipType <= (uint)ClipType.Xor &&
                (uint)fillRule <= (uint)FillRule.Negative)
            {
                bool isValid;
                try
                {
                    ExecuteProcess((ClipType)clipType, (FillRule)fillRule);
                    isValid = BuildPaths(result);
                }
                catch
                {
                    isValid = false;
                }
                ClearSolution();
                return isValid;
            }
            return false;
        }

        private bool ExecuteInternal(int clipType, int fillRule)
        {
            if ((uint)clipType <= (uint)ClipType.Xor &&
                (uint)fillRule <= (uint)FillRule.Negative)
            {
                bool isValid;
                try
                {
                    ExecuteProcess((ClipType)clipType, (FillRule)fillRule);
                    isValid = ValidPaths();
                }
                catch
                {
                    isValid = false;
                }
                ClearSolution();
                return isValid;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ExecuteProcess(ClipType cliptype, FillRule fillrule)
        {
            _fillrule = fillrule;
            _cliptype = cliptype;

            ReadyExecute();

            if (_scanlines.Pop(out long y))
            {
                while (true)
                {
                    InsertLocalMinimaIntoAEL(y);
                    DoHorizontals();
                    ConvertHorzSegsToJoins();

                    _currentBottomY = y;

                    if (_scanlines.Pop(out y))
                    {
                        DoIntersections(y);
                        DoTopOfScanbeam(y);
                        DoHorizontals();

                        if (_succeeded == false)
                            break;
                    }
                    else
                    {
                        if (_succeeded && _horzjoins.Count != 0)
                            ProcessHorzJoins();
                        break;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoHorizontals()
        {
            var ae = _sel; _sel = null;
            if (ae != null)
            {
                do DoHorizontal(ae);
                while ((ae = ae.NextSEL) != null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoIntersections(long topY)
        {
            if (BuildIntersectList(topY))
            {
                var list = _intersects.Sort();
                var i = 0;
                var b = list.Bucket;
                do
                {
                    if (b[i].EdgesAdjacentInAEL == false)
                    {
                        var j = i + 1;
                        while (b[j].EdgesAdjacentInAEL == false) j++;
                        (b[j], b[i]) = (b[i], b[j]);
                    }

                    var n = b[i];
                    IntersectEdges(n.Edge1, n.Edge2, n.Pt);
                    SwapPositionsInAEL(n.Edge1, n.Edge2);

                    n.Edge1.CurrX = n.Pt.X;
                    n.Edge2.CurrX = n.Pt.X;
                    CheckJoinLeft(n.Edge2, n.Pt, true);
                    CheckJoinRight(n.Edge1, n.Pt, true);
                }
                while (++i < list.Count);
                list.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ConvertHorzSegsToJoins()
        {
            if (_horzsegs.Count != 0)
            {
                ConvertHorzSegsToJoins(_horzsegs, _horzjoins);
                _horzsegs.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ConvertHorzSegsToJoins(HorzSegmentList horzsegList, HorzJoinList horzjoinList)
        {
            int k = 0;
            var i = 0;
            var c = horzsegList.Count;
            var b = horzsegList.Bucket;

            do if (UpdateHorzSegment(b[i])) k++;
            while (++i < c);

            if (k >= 2)
            {
                horzsegList.Sort();

                for (i = 0; i < k - 1; i++)
                {
                    var h1 = b[i];
                    for (int j = i + 1; j < k; j++)
                    {
                        var h2 = b[j];
                        if (h2.LeftOp.Pt.X < h1.RightOp.Pt.X &&
                            h2.LeftToRight != h1.LeftToRight &&
                            h2.RightOp.Pt.X > h1.LeftOp.Pt.X)
                        {
                            var currY = h1.LeftOp.Pt.Y;

                            if (h1.LeftToRight)
                            {
                                while (h1.LeftOp.Next.Pt.Y == currY && h1.LeftOp.Next.Pt.X <= h2.LeftOp.Pt.X)
                                    h1.LeftOp = h1.LeftOp.Next;
                                while (h2.LeftOp.Prev.Pt.Y == currY && h2.LeftOp.Prev.Pt.X <= h1.LeftOp.Pt.X)
                                    h2.LeftOp = h2.LeftOp.Prev;

                                horzjoinList.Add(DuplicateOp(h1.LeftOp, true), DuplicateOp(h2.LeftOp, false));
                            }
                            else
                            {
                                while (h1.LeftOp.Prev.Pt.Y == currY && h1.LeftOp.Prev.Pt.X <= h2.LeftOp.Pt.X)
                                    h1.LeftOp = h1.LeftOp.Prev;
                                while (h2.LeftOp.Next.Pt.Y == currY && h2.LeftOp.Next.Pt.X <= h1.LeftOp.Pt.X)
                                    h2.LeftOp = h2.LeftOp.Next;

                                horzjoinList.Add(DuplicateOp(h2.LeftOp, true), DuplicateOp(h1.LeftOp, false));
                            }
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ProcessHorzJoins()
        {
            var i = 0;
            var c = _horzjoins.Count;
            var b = _horzjoins.Bucket;
            do
            {
                var j = b[i];
                var or1 = GetRealOutRec(j.Op1.Outrec);
                var or2 = GetRealOutRec(j.Op2.Outrec);

                var op1b = j.Op1.Next;
                var op2b = j.Op2.Prev;
                j.Op1.Next = j.Op2;
                j.Op2.Prev = j.Op1;
                op1b.Prev = op2b;
                op2b.Next = op1b;

                if (or1 != or2) or2.Pts = null;
                else
                {
                    or2 = NewOutRec();
                    or2.Pts = op1b;
                    or2.FixOutRecPts();

                    if (or1.Pts.Outrec == or2)
                    {
                        or1.Pts = j.Op1;
                        or1.Pts.Outrec = or1;
                    }
                }
                or2.Owner = or1;
            }
            while (++i < c);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddNewIntersectNode(Active ae1, Active ae2, long topY)
        {
            if (Utils.GetIntersectPoint(ae1.Btm, ae1.Top, ae2.Btm, ae2.Top, out Long2D ip) == false)
                ip = new(ae1.CurrX, topY);

            if (ip.Y > _currentBottomY || ip.Y < topY)
            {
                var absDx1 = Math.Abs(ae1.DX);
                var absDx2 = Math.Abs(ae2.DX);

                if (absDx1 > 100)
                {
                    if (absDx2 > 100)
                    {
                        if (absDx1 > absDx2)
                            Utils.GetClosestPtOnSegment(ip, ae1.Btm, ae1.Top, out ip);
                        else
                            Utils.GetClosestPtOnSegment(ip, ae2.Btm, ae2.Top, out ip);
                    }
                    else
                    {
                        Utils.GetClosestPtOnSegment(ip, ae1.Btm, ae1.Top, out ip);
                    }
                }
                else
                {
                    if (absDx2 > 100)
                    {
                        Utils.GetClosestPtOnSegment(ip, ae2.Btm, ae2.Top, out ip);
                    }
                    else
                    {
                        var y = ip.Y < topY ? topY : _currentBottomY;
                        ip = new(absDx1 < absDx2 ? ae1.TopX(y) : ae2.TopX(y), y);
                    }
                }
            }
            _intersects.Add(ip, ae1, ae2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Insert1Before2InSEL(Active ae1, Active ae2)
        {
            ae1.PrevSEL = ae2.PrevSEL;
            if (ae1.PrevSEL != null)
                ae1.PrevSEL.NextSEL = ae1;
            ae1.NextSEL = ae2;
            ae2.PrevSEL = ae1;
        }

        private bool BuildIntersectList(long topY)
        {
            if (_actives != null && _actives.NextAEL != null)
            {
                AdjustCurrXAndCopyToSEL(topY);

                var sel = _sel;
                if (sel.Jump != null)
                {
                    do
                    {
                        var l1 = sel;
                        var r1 = sel.Jump;
                        var prevBase = default(Active);
                        var currBase = sel;
                        do
                        {
                            var l2 = r1;
                            var r2 = r1.Jump; l1.Jump = r2;
                            if (l2 != l1 && r2 != r1)
                            {
                                do
                                {
                                    if (r1.CurrX < l1.CurrX)
                                    {
                                        var t = r1;
                                        do AddNewIntersectNode(t = t.PrevSEL, r1, topY);
                                        while (t != l1);

                                        t = r1;
                                        r1 = r1.ExtractFromSEL();
                                        l2 = r1;

                                        Insert1Before2InSEL(t, l1);

                                        if (currBase == l1)
                                        {
                                            currBase = t;
                                            currBase.Jump = r2;

                                            if (prevBase != null)
                                                prevBase.Jump = currBase;
                                            else
                                                sel = currBase;
                                        }
                                    }
                                    else l1 = l1.NextSEL;
                                }
                                while (l2 != l1 && r2 != r1);
                            }
                            if (r2 == null) break;

                            l1 = r2;
                            r1 = r2.Jump;
                            prevBase = currBase;
                            currBase = r2;
                        }
                        while (r1 != null);
                    }
                    while (sel.Jump != null);
                    _sel = sel;
                }
                return _intersects.Count > 0;
            }
            return false;
        }
    }
}