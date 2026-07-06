namespace JHLib.WPFUtil.Gesture
{
    /// <summary> 
    /// 입력 타입(마우스, 터치, 펜(Stylus))의 물리적 입력을 하나의 논리적 제스처로 통합정의<br/>
    /// Pressed 이후 반드시 3가지(Tap, SubTap, Released) 형태로 종료를 보장  <br/>
    /// 각 기능모드에서 Pressed 제스쳐 이후 PanMoved(드래그) 형태로 제스쳐 연계를 하려면 Pressed 이벤트 결과에 핸들링 처리를 해야함 <br/>
    /// 현재 핸들링 처리는 GestureEventHandler 이벤트의 반환값으로 true 지정시 PanMoved(드래그) 제스쳐 연계로 이어짐
    /// </summary>
    [Flags]
    public enum GestureType
    {
        None = 0,

        /// <summary> 
        /// 특정 기능이나 상호작용 세션의 논리적 시작<br/>
        /// 물리적 입력을 처리하기 위한 준비 단계 또는 기능 모드 진입 시 발생<br/>
        /// - [공통] 제스처 처리 모드의 기능 활성화, 세션의 초기화
        /// </summary>
        Enter = 1 << 0,

        /// <summary> 
        /// 특정 기능이나 상호작용 세션의 완전한 종료<br/>
        /// 단일 입력의 해제(Released)를 넘어, 해당 기능 모드 자체가 끝날 때 발생<br/>
        /// - [공통] 제스처 처리 모드의 기능 비활성화, 리소스 해제 및 세션의 최종 종료
        /// </summary>
        Exit = 1 << 1,

        /// <summary> 
        /// 진입 및 사전 안내 (마우스 오버, 팬 오버 상태에서의 상호작용)<br/>
        /// 객체 위로 올라갔을 때 툴팁이나 가이드 라인을 표시<br/>
        /// - [마우스] 마우스 포인터 오버 (Mouse Over)<br/>
        /// - [터치] 해당 없음<br/>
        /// - [펜] 화면 가까이 펜을 가져가기 (Hovering / Halo)
        /// </summary>
        Hover = 1 << 2,

        /// <summary> 
        /// 누름 시작 (마우스 Left Down 및 터치/펜 접촉 발생)<br/>
        /// 제스처(Tap, Pan 등)로 판별되기 전, 화면을 누르거나 클릭한 즉시 발생<br/>
        /// - [마우스] 마우스 버튼을 누른 순간 (Left Mouse Down)<br/>
        /// - [터치] 손가락이 화면에 닿은 순간 (Touch Down)<br/>
        /// - [펜] 펜촉이 화면에 닿은 순간 (Stylus Down)
        /// </summary>
        Pressed = 1 << 3,

        /// <summary> 
        /// 메인 Tap (객체 선택, Pressed상태후 움직인 거리가 거의 없는 상태로 종료됬을경우) <br/>
        /// - [마우스] 좌클릭<br/>
        /// - [터치] 가볍게 화면을 치기 (Tap)<br/>
        /// - [펜] 펜촉으로 화면 탭하기
        /// </summary>
        Tap = 1 << 4,

        /// <summary> 
        /// 보조 Tap (메뉴 팝업, 상세 속성 호출 등)
        /// - [마우스] 우클릭 (Right Click)<br/>
        /// - [터치] 특정 시간 길게 누르기 (Long Press)
        /// - [펜] 사이드 버튼을 누른 채로 화면 탭하기 (or Long Press)
        /// </summary>
        SubTap = 1 << 5,

        /// <summary> 
        /// 패닝/드래그 진행 (Pressed상태에서 특정 움직임 보다 커지면 발생)<br/>        
        /// 이동하는 좌표를 바탕으로 화면 이동(Panning)이나 객체 이동(Drag) 수행
        /// </summary>
        PanMoved = 1 << 6,

        /// <summary> 
        /// 현재 진행 중인 단일 제스처 종료<br/>
        /// 패닝/드래그 종료 or 상태의 비활성화나 화면 이탈 등의 이유로 제스쳐가 끝난 상태
        /// </summary>
        Released = 1 << 7,


        /// 종료 제스쳐 통합 플래그 (type & EndGesture) != 0 형태로 flag 연산으로 활용<br/>
        EndGesture = Tap | SubTap | Released
    }
}