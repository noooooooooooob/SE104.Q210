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

namespace Sprint5
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ComboBox[] _cbGames;
        private TextBlock[] _txbNgayMuon, _txbSoNgayMuon, _txbTienPhat;

        public MainWindow() { InitializeComponent(); }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _cbGames = new[] { cboGame1, cboGame2 };
            _txbNgayMuon = new[] { txbNgayMuon1, txbNgayMuon2 };
            _txbSoNgayMuon = new[] { txbSoNgayMuon1, txbSoNgayMuon2 };
            _txbTienPhat = new[] { txbTienPhat1, txbTienPhat2 };

            dpNgayTra.SelectedDate = DateTime.Today;
        }

        private void CboKhachHang_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void DpNgayTra_SelectedDateChanged(object sender, SelectionChangedEventArgs e) { }
        private void CboGame_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void BtnLapPhieu_Click(object sender, RoutedEventArgs e) { }
        private void BtnThoat_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
