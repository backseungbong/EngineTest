using System.ComponentModel;
using System.Windows;
using JHLib.WPFUtil;
using JHApp.ECDIS.ViewModels;

namespace JHApp.ECDIS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            EcdisApp.Init();
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this)) return;
            DataContext = VM.MainVM;

            ViewGraphicsLayer.Manager = EcdisApp.LayerManager;
            EcdisApp.ModeLayer.GesturePanel = ViewGesturePanel;
        }
    }
}