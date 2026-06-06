using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sprint6
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow() { InitializeComponent(); }

        private void Window_Loaded(object sender, RoutedEventArgs e) { }

        private void CboKhachHang_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void TxtSoTienThu_TextChanged(object sender, TextChangedEventArgs e) { }
        private void BtnLapPhieu_Click(object sender, RoutedEventArgs e) { }
        private void BtnThoat_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
