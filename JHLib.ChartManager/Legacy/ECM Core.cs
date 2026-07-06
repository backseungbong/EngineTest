using Legacy.ECM_Core.Component;
using Legacy.ECM_Core.Definition;

namespace Legacy.ECM_Core
{
    public partial class ECM_CORE
    {
        public ChartOrganizer Chart_Organizer { get; private set; } = new ChartOrganizer();
        public ChartComposer Chart_Composer { get; private set; } = new ChartComposer();



        public ECM_CORE(string base_directory = "AppData")
        {
            DirectoryDefinition.AppBase_Directory = base_directory;
        }
    }
}