namespace JHLib.Util.Geometry
{
    /// <summary> 지오메트리 교차 관계 </summary>
    [Flags]
    public enum GeoRelation : int
    {
        /// <summary> 비교 지오메트리와 교차하지 않는 상태 </summary>
        Disjoint = 0,
        /// <summary> 비교 지오메트리와 교차하는 상태 </summary>
        Overlap = 1 << 0,
        /// <summary> 비교 지오메트리에 완전히 포함되는 상태 </summary>
        Contains = 1 << 1,
        /// <summary> 비교 지오메트리를 완전히 포함하는 상태 </summary>
        ContainedBy = 1 << 2,

        /// <summary> 비교 지오메트리와 경로가 교차하는 상태 </summary>
        PathIntersect = Overlap | Contains,
        /// <summary> 비교 지오메트리와 영역이 교차하는 상태 </summary>
        AreaIntersect = Overlap | Contains | ContainedBy,
    }

    /// <summary> 지오메트리 방향 </summary>
    [Flags]
    public enum GeoDirection : int
    {
        /// <summary> 초기값 </summary>
        None = 0,
        /// <summary> 지오메트리의 Path 구성 방향이 시계방향 (시계 방향이면 오른쪽이 내부) </summary>
        Clockwise = 1,
        /// <summary> 지오메트리의 Path 구성 방향이 반시계방향 (반시계 방향이면 왼쪽이 내부)  </summary>
        CounterClockwise = 2,
    }

    /// <summary> 특정 위치가 사각형의 어느 경계를 넘었는지 식별하는 비트 플래그. Cohen-Sutherland 클리핑 알고리즘을 응용하여 사용됨 </summary>
    [Flags]
    public enum OutCode
    {
        // None -> Left -> Right -> Top -> Bottom 순으로 비트 플레그가 설정되며 
        // 이 순서는 비트 연산 합이나 SIMD 처리에 사용되므로 순서변경 불가 !!!

        Inside = 0,
        Left = 1 << 0, // 1
        Top = 1 << 1, // 2
        Right = 1 << 2, // 4
        Bottom = 1 << 3, // 8

        LT = Left | Top, // 3 (>>2 == 0)
        RT = Right | Top, // 6 (>>2 == 1)
        LB = Left | Bottom, // 9 (>>2 == 2)
        RB = Right | Bottom, // 12 (>>2 == 3)

        XAxis = Left | Right, // 5 (>>3 == 0)
        YAxis = Top | Bottom, // 10 (>>3 == 1)
        All = Left | Top | Right | Bottom
    }
}