using System.ComponentModel;

namespace JHLib.NavigationLayer.InteractionMode
{
    /// <summary> 상호작용 모드 타입 </summary>
    public enum InteractionModeType : int
    {
        [Description("항해 모드")]
        Voyage = 0,
        [Description("제스처 활성화 모드")]
        GestureActive,        
        [Description("차트 조회 모드")]
        ChartQuery,
        [Description("차트 사용자 수정 모드")]
        ChartManualUpdate,
        [Description("사용자차트 편집 모드")]
        UserChartEdit,
        [Description("항적 편집 모드")]
        VoyageLogEdit,
        [Description("항로 편집 모드")]
        RoutePlanEdit,
        [Description("Ebl Vrm 편집 모드")]
        EblVrmEdit,
        [Description("Lop 편집 모드")]
        LopEdit,
        [Description("ParallelLine 편집 모드")]
        ParallelLineEdit,
        [Description("ClearingLine 편집 모드")]
        ClearingLineEdit,
        [Description("Voyage 편집 모드")]
        VoyageEdit,

        // 아래는 임의대로 작성한 샘플이므로, 기능 담당자가 적절히 네이밍으로 모드 타입을 추가
        // Simulation,
        // UserChartEdit,
        // ManualUpdateEdit,
        // RouteEdit,
    }
}