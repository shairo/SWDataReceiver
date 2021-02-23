using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SWDataReceiver
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowModel model;

        public MainWindow()
        {
            InitializeComponent();

            model = new MainWindowModel();
            DataContext = model;
        }

        protected override void OnClosed(EventArgs e)
        {
            model.Dispose();
            model = null;
            DataContext = null;

            base.OnClosed(e);
        }
    }
}
