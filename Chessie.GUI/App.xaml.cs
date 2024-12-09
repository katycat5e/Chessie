using System.Configuration;
using System.Data;
using System.Windows;

namespace Chessie.GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
#if DEBUG
        public const bool IsDebugBuild = true;
#else
        public const bool IsDebugBuild = false;
#endif
    }

}
