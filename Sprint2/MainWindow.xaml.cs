using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Sprint2
{
    public partial class MainWindow : Window
    {
        private List<Game> dsNhap =
            new List<Game>();

        public MainWindow()
        {
            InitializeComponent();

            cbLoaiGame.ItemsSource =
                DataManager.LoaiGames;

            dpNgayNhap.SelectedDate =
                DateTime.Now;
        }

private void BtnThemGame_Click(
    object sender,
    RoutedEventArgs e)
{
    try
    {
        string tenGame =
            txtTenGame.Text;

        string loaiGame =
            cbLoaiGame.SelectedItem.ToString();

        string nhaPhatHanh =
            txtNhaPhatHanh.Text;

        int namPhatHanh =
            int.Parse(txtNamPhatHanh.Text);

        decimal triGia =
            decimal.Parse(txtTriGia.Text);

        if (!DataManager.LoaiGames.Contains(loaiGame))
        {
            MessageBox.Show(
                "Loại game không tồn tại");

            return;
        }

        int tuoiGame =
            DateTime.Now.Year -
            namPhatHanh;

        if (tuoiGame >
            DataManager.TuoiGameToiDa)
        {
            MessageBox.Show(
                "Game quá tuổi cho phép");

            return;
        }

        if (triGia <= 0)
        {
            MessageBox.Show(
                "Trị giá phải lớn hơn 0");

            return;
        }

        Game game = new Game();

        game.TenGame = tenGame;
        game.LoaiGame = loaiGame;
        game.NhaPhatHanh = nhaPhatHanh;
        game.NamPhatHanh = namPhatHanh;
        game.TriGia = triGia;

        dsNhap.Add(game);

        if (!DataManager.DanhSachGame
            .Any(x => x.TenGame == tenGame))
        {
            DataManager.DanhSachGame
                .Add(game);
        }

        dgGames.ItemsSource = null;
        dgGames.ItemsSource = dsNhap;

        txtTongGiaTri.Text =
            "Tổng giá trị: "
            + dsNhap.Sum(x => x.TriGia);

        ClearInput();
    }
    catch
    {
        MessageBox.Show(
            "Dữ liệu không hợp lệ");
    }
}


private void BtnLapPhieu_Click(
    object sender,
    RoutedEventArgs e)
{
    if (dsNhap.Count == 0)
    {
        MessageBox.Show(
            "Chưa có game nào");

        return;
    }

    decimal tongTien =
        dsNhap.Sum(x => x.TriGia);

    MessageBox.Show(
        $"Lập phiếu thành công\n" +
        $"Tổng giá trị: {tongTien}");
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
}
