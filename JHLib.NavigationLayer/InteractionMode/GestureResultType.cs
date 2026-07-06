namespace JHLib.NavigationLayer.InteractionMode
{
    /// <summary> 제스처 처리 결과 상태 </summary>
    [Flags]
    public enum GestureResultType
    {
        /// <summary> 제스처 처리 없는 기본 상태 </summary>
        None = 0,

        /// <summary>
        /// 제스처가 현재 모드에서 처리되어 소비함을 나타냄<br/>
        /// 이벤트가 더 이상 하위 레이어로 전파되지 않도록 함
        /// </summary>
        Handled = 1 << 0,

        /// <summary>
        /// 제스처 처리 결과로 화면을 다시 그려야 할때 지정<br/>
        /// 이 플래그가 설정되면 현재 프레임이 무효화되고 렌더링이 요청됨
        /// </summary>
        Redraw = 1 << 1,

        /// <summary>
        /// 제스처 처리와 동시에 화면을 다시 그려야 함을 나타냄<br/>
        /// <see cref="Handled"/>와 <see cref="Redraw"/>를 조합한 편의값
        /// </summary>
        HandledWithRedraw = Handled | Redraw
    }
}