using ClientLauncher.ViewModels;
using System.Windows;

namespace ClientLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}