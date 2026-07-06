using Legacy.ECM_Core.Chart;

namespace Legacy.ECM_Core
{
    public partial class ECM_CORE
    {
        public delegate void ReportDelegate(InstallReport report);
        public event ReportDelegate? Reported_Install;


        public bool CoreControl_Accessible { get; internal set; } = true;

        public bool Using_Chart1 { get; internal set; } = false;

        public int Asynchronizing_Size { get; internal set; } = 10;
    }
}