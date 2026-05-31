using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Sprint2;

public partial class MainWindow : Window
{
    private List<Game> dsNhap = new();

    public MainWindow()
    {
        InitializeComponent();

        cbLoaiGame.ItemsSource = DataManager.LoaiGames;

        dpNgayNhap.SelectedDate = DateTime.Now;
    }

    private void BtnThemGame_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(txtTenGame.Text))
            {
                MessageBox.Show("Vui lòng nhập tên game");
                return;
            }

            if (cbLoaiGame.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn loại game");
                return;
            }

            string tenGame = txtTenGame.Text.Trim();

            string loaiGame =
                cbLoaiGame.SelectedItem.ToString()!;

            string nhaPhatHanh =
                txtNhaPhatHanh.Text.Trim();

            int namPhatHanh =
                int.Parse(txtNamPhatHanh.Text);

            decimal triGia =
                decimal.Parse(txtTriGia.Text);

            if (namPhatHanh > DateTime.Now.Year)
            {
                MessageBox.Show("Năm phát hành không hợp lệ");
                return;
            }

            int tuoiGame =
                DateTime.Now.Year - namPhatHanh;

            if (tuoiGame > DataManager.TuoiGameToiDa)
            {
                MessageBox.Show(
                    $"Game quá {DataManager.TuoiGameToiDa} năm tuổi");
                return;
            }

            if (triGia <= 0)
            {
                MessageBox.Show(
                    "Trị giá phải lớn hơn 0");
                return;
            }

            Game game = new()
            {
                TenGame = tenGame,
                LoaiGame = loaiGame,
                NhaPhatHanh = nhaPhatHanh,
                NamPhatHanh = namPhatHanh,
                TriGia = triGia
            };

            dsNhap.Add(game);

            if (!DataManager.DanhSachGame
                .Any(x => x.TenGame == tenGame))
            {
                DataManager.DanhSachGame.Add(game);
            }

            dgGames.ItemsSource = null;
            dgGames.ItemsSource = dsNhap;

            txtTongGiaTri.Text =
                $"Tổng giá trị: {dsNhap.Sum(x => x.TriGia):N0} VNĐ";

            ClearInput();
        }
        catch
        {
            MessageBox.Show(
                "Vui lòng nhập đúng định dạng dữ liệu");
        }
    }

    private void BtnLapPhieu_Click(object sender, RoutedEventArgs e)
    {
        if (dsNhap.Count == 0)
        {
            MessageBox.Show(
                "Danh sách game đang trống");
            return;
        }

        decimal tongTien =
            dsNhap.Sum(x => x.TriGia);

        MessageBox.Show(
            $"Lập phiếu nhập thành công!\n\n" +
            $"Số lượng game: {dsNhap.Count}\n" +
            $"Tổng giá trị: {tongTien:N0} VNĐ");
    }

    private void ClearInput()
    {
        txtTenGame.Clear();
        txtNhaPhatHanh.Clear();
        txtNamPhatHanh.Clear();
        txtTriGia.Clear();

        cbLoaiGame.SelectedIndex = -1;
    }
}