using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileSource
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            
            InitializeComponent();
            // 使用 BitmapImage 设置窗口图标
            //BitmapImage bitmapImage = new BitmapImage(new Uri("pack://application:,,,/Resources/filesources.png"));
            //this.Icon = bitmapImage;
        }

        private void OpenNewWindow_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}