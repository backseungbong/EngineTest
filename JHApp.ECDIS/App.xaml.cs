using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows;

namespace JHApp.ECDIS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            Environment.SetEnvironmentVariable("ExeDirectory", Path.GetDirectoryName(exePath));
        }
    }

}
