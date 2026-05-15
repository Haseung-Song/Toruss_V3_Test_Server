using System.Windows;
using Toruss_V3_Test_Server.ViewModels;

namespace Toruss_V3_Test_Server
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainVM vm = new MainVM();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = vm;
        }

    }

}
