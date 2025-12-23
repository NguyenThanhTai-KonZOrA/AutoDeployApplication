using ClientLauncher.ViewModels;
using System.Windows;

namespace ClientLauncher.Windows
{
    public partial class LaunchWindow : Window
    {
        public LaunchWindow(string appCode)
        {
            InitializeComponent();
            DataContext = new LaunchViewModel(appCode, this);
        }
    }
}