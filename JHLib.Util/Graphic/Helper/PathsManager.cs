using JHLib.Util.ArrayControl;
using JHLib.Util.Struct;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Graphic.Helper
{
    /// <summary>
    /// 그래픽 개채의 패스를 관리하는 클래스 <para/>
    /// 화면에 그려지기전 포인트 데이타를 변환 및 저장하거나 Path 그룹을 관리한다
    /// </summary>
    public class PathsManager
    {
        private Float2D[] _points;
        private int _pointCapacity;
        private int _pointCount;

        private OffsetRange[] _ranges;
        private int _rangeCapacity;
        private int _rangeCount;

        public PathsManager()
        {
            _points = GC.AllocateUninitializedArray<Float2D>(1024);
            _pointCapacity = 1024;
            _ranges = new OffsetRange[32];
            _rangeCapacity = 32;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizePoints(int add)
        {
            var cnt = _pointCount;
            var cap = (int)BitOperations.RoundUpToPowerOf2((uint)(cnt + add));
            var buk = GC.AllocateUninitializedArray<Float2D>(cap);

            if (cnt != 0) _points.AsSpan(0, cnt).CopyTo(buk);

            _points = buk;
            _pointCapacity = cap;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizePaths()
        {
            var cnt = _rangeCount;
            var cap = cnt * 2;
            var buk = GC.AllocateUninitializedArray<OffsetRange>(cap);

            if (cnt != 0) _ranges.AsSpan().CopyTo(buk);

            _ranges = buk;
            _rangeCapacity = cap;
        }

        /// <summary> 패스 데이타를 모두 초기화 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PathsManager Clear() { _pointCount = 0; _rangeCount = 0; return this; }

        /// <summary> 포인트를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPoint(Float2D point)
        {
            var pointCount = _pointCount;
            if (pointCount == _pointCapacity) ResizePoints(1);
            _pointCount = pointCount + 1;
            _points[pointCount] = point;
        }

        /// <summary> 특정 길이를 가지는 패스를 추가한다. 패스 데이타는 미리 확보한 메모리공간에 정의된 상태여야 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPath(int length)
        {
            if (length >= 2)
            {
                var pointCount = _pointCount;
                if (pointCount + length > _pointCapacity) ThrowInvalidPreparePath();
                _pointCount = pointCount + length;

                var rangeCount = _rangeCount;
                if (rangeCount == _rangeCapacity) ResizePaths();
                _ranges[rangeCount] = new OffsetRange(pointCount, length);
                _rangeCount = rangeCount + 1;
            }
        }

        /// <summary> 패스를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPath(ref Float2D path0, int pathn)
        {
            if (pathn >= 2)
            {
                ref var dst = ref AddPath0Unsafe(pathn);
                AC.Copy(ref path0, ref dst, pathn);
            }
        }

        /// <summary> 패스를 추가한다. 실제 패스는 반환된 메모리 공간에 정의한다. length 체크를 하지 않으므로 반드시 2이상이 보장되면 호출한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Float2D AddPath0Unsafe(int length)
        {
            var pointCount = _pointCount;
            if (pointCount + length > _pointCapacity) ResizePoints(length);
            _pointCount = pointCount + length;

            var rangeCount = _rangeCount;
            if (rangeCount == _rangeCapacity) ResizePaths();
            _ranges[rangeCount] = new OffsetRange(pointCount, length);
            _rangeCount = rangeCount + 1;

            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_points), pointCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(
            ref MemoryMarshal.GetArrayDataReference(_points),
            ref MemoryMarshal.GetArrayDataReference(_ranges), _rangeCount);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref struct Enumerator(ref Float2D path0, ref OffsetRange range0, int count)
        {
            private readonly ref Float2D p = ref path0;
            private readonly ref OffsetRange e = ref Unsafe.Add(ref range0, (uint)count);
            private ref OffsetRange r = ref range0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly Enumerator GetEnumerator() => this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => Unsafe.IsAddressLessThan(ref r, ref e);
            public Span<Float2D> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    var r = Unsafe.Subtract(ref this.r = ref Unsafe.Add(ref this.r, 1), 1);
                    return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref p, (uint)r.Offset), r.Length);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidPreparePath() =>
            throw new Exception("Valid path data must be prepared to specify the path data");
    }
}