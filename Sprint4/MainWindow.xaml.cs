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

using MySql.Data.MySqlClient;
using System.Data.SqlClient;

namespace Sprint4
{
    public partial class MainWindow : Window
    {
        // ==================== MODEL ====================
        private class KhachHangItem
        {
            public int MaKhachHang { get; set; }
            public string HoTen { get; set; }
            public DateTime? NgayHetHan { get; set; }
            public override string ToString() => HoTen;
        }

        private class GameItem
        {
            public int MaCTPN { get; set; }
            public string TenGame { get; set; }
            public string TenLoaiGame { get; set; }
            public string NhaPhatHanh { get; set; }
            public string TinhTrang { get; set; }
            public override string ToString() => TenGame;
        }

        private int _quyDinhSoGame = 3;
        private int _quyDinhSoNgay = 30;

        // Map dòng → các control tương ứng
        private ComboBox[] _cbGames;
        private TextBlock[] _txbLoai, _txbNPH, _txbTT;

        // ==================== CONSTRUCTOR ====================
        public MainWindow()
        {
            InitializeComponent();
        }

        // ==================== LOAD ====================
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Gán mảng control theo dòng
            _cbGames = new[] { cboGame1, cboGame2, cboGame3, cboGame4 };
            _txbLoai = new[] { txbLoai1, txbLoai2, txbLoai3, txbLoai4 };
            _txbNPH = new[] { txbNPH1, txbNPH2, txbNPH3, txbNPH4 };
            _txbTT = new[] { txbTT1, txbTT2, txbTT3, txbTT4 };

            // Ngày mượn mặc định = hôm nay
            dpNgayMuon.SelectedDate = DateTime.Today;

            NapDanhSachKhachHang();
            NapQuyDinh();
            NapDanhSachGame();
        }

        // ==================== NẠP KHÁCH HÀNG ====================
        private void NapDanhSachKhachHang()
        {
            try
            {
                cboKhachHang.Items.Clear();
                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "SELECT MaKhachHang, HoTen, NgayHetHan FROM KHACHHANG ORDER BY HoTen", conn);
                    var r = cmd.ExecuteReader();
                    while (r.Read())
                        cboKhachHang.Items.Add(new KhachHangItem
                        {
                            MaKhachHang = Convert.ToInt32(r["MaKhachHang"]),
                            HoTen = r["HoTen"].ToString(),
                            NgayHetHan = r["NgayHetHan"] == DBNull.Value ? (DateTime?)null
                                            : Convert.ToDateTime(r["NgayHetHan"])
                        });
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi nạp khách hàng: " + ex.Message, "Lỗi"); }
        }

        // ==================== NẠP QUY ĐỊNH ====================
        private void NapQuyDinh()
        {
            try
            {
                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "SELECT QuiDinhSoGameChoMuon, QuiDinhSoNgayMuon FROM THAMSO LIMIT 1", conn);
                    var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        _quyDinhSoGame = Convert.ToInt32(r["QuiDinhSoGameChoMuon"]);
                        _quyDinhSoNgay = Convert.ToInt32(r["QuiDinhSoNgayMuon"]);
                        txbQuyDinhSoGame.Text = _quyDinhSoGame.ToString();
                        txbQuyDinhSoNgay.Text = _quyDinhSoNgay.ToString();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi đọc quy định: " + ex.Message, "Lỗi"); }
        }

        // ==================== NẠP GAME (CHỈ "CÓ SẴN") ====================
        private void NapDanhSachGame()
        {
            try
            {
                var danhSach = new List<GameItem>();
                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        @"SELECT ct.MaCTPN, ct.TenGame, lg.TenLoaiGame, ct.NhaPhatHanh, ct.TinhTrang
                          FROM CTPHIEUNHAP ct
                          INNER JOIN LOAIGAME lg ON ct.MaLoaiGame = lg.MaLoaiGame
                          WHERE ct.TinhTrang = N'Có sẵn'
                          ORDER BY ct.TenGame", conn);
                    var r = cmd.ExecuteReader();
                    while (r.Read())
                        danhSach.Add(new GameItem
                        {
                            MaCTPN = Convert.ToInt32(r["MaCTPN"]),
                            TenGame = r["TenGame"].ToString(),
                            TenLoaiGame = r["TenLoaiGame"].ToString(),
                            NhaPhatHanh = r["NhaPhatHanh"].ToString(),
                            TinhTrang = r["TinhTrang"].ToString()
                        });
                }

                // Điền vào tất cả ComboBox game, thêm item trống đầu tiên
                foreach (var cb in _cbGames)
                {
                    cb.Items.Clear();
                    cb.Items.Add(new GameItem { MaCTPN = -1, TenGame = "" });
                    foreach (var g in danhSach)
                        cb.Items.Add(g);
                    cb.SelectedIndex = 0;
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi nạp game: " + ex.Message, "Lỗi"); }
        }

        // ==================== CHỌN KHÁCH HÀNG ====================
        private void CboKhachHang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboKhachHang.SelectedItem is KhachHangItem kh)
            {
                txbNgayHetHan.Text = kh.NgayHetHan.HasValue
                    ? kh.NgayHetHan.Value.ToString("dd/MM/yyyy") : "(Không có)";
            }
            else
            {
                txbNgayHetHan.Text = "";
            }
            TinhNgayHetHanMuon();
        }

        // ==================== ĐỔI NGÀY MƯỢN ====================
        private void DpNgayMuon_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            TinhNgayHetHanMuon();
        }

        private void TinhNgayHetHanMuon()
        {
            if (dpNgayMuon.SelectedDate.HasValue)
                txbNgayHetHanMuon.Text = dpNgayMuon.SelectedDate.Value
                    .AddDays(_quyDinhSoNgay).ToString("dd/MM/yyyy");
            else
                txbNgayHetHanMuon.Text = "";
        }

        // ==================== CHỌN GAME → TỰ ĐIỀN THÔNG TIN ====================
        private void CboGame_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)sender;
            int idx = int.Parse(cb.Tag.ToString()) - 1; // 0-based

            if (cb.SelectedItem is GameItem g && g.MaCTPN != -1)
            {
                _txbLoai[idx].Text = g.TenLoaiGame;
                _txbNPH[idx].Text = g.NhaPhatHanh;
                _txbTT[idx].Text = g.TinhTrang;
            }
            else
            {
                _txbLoai[idx].Text = "";
                _txbNPH[idx].Text = "";
                _txbTT[idx].Text = "";
            }
        }

        // ==================== LẬP PHIẾU (XỬ LÝ CHÍNH) ====================
        private void BtnLapPhieu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // --- Kiểm tra đầu vào cơ bản ---
                if (cboKhachHang.SelectedItem == null)
                { MessageBox.Show("Vui lòng chọn khách hàng!", "Thông báo"); return; }

                if (!dpNgayMuon.SelectedDate.HasValue)
                { MessageBox.Show("Vui lòng chọn ngày mượn!", "Thông báo"); return; }

                // Thu thập danh sách game được chọn (bỏ qua dòng trống)
                var danhSachChon = new List<GameItem>();
                for (int i = 0; i < _cbGames.Length; i++)
                {
                    if (_cbGames[i].SelectedItem is GameItem g && g.MaCTPN != -1)
                    {
                        // Không cho chọn trùng game
                        if (danhSachChon.Exists(x => x.MaCTPN == g.MaCTPN))
                        {
                            MessageBox.Show($"Game \"{g.TenGame}\" được chọn trùng nhiều dòng!", "Lỗi");
                            return;
                        }
                        danhSachChon.Add(g);
                    }
                }

                if (danhSachChon.Count == 0)
                { MessageBox.Show("Vui lòng chọn ít nhất 1 game!", "Thông báo"); return; }

                var kh = (KhachHangItem)cboKhachHang.SelectedItem;
                DateTime ngayMuon = dpNgayMuon.SelectedDate.Value;
                DateTime ngayHetHanMuon = ngayMuon.AddDays(_quyDinhSoNgay);

                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();

                    // Bước 03: Kiểm tra KH tồn tại
                    var cmdKH = new MySqlCommand(
                        "SELECT COUNT(*) FROM KHACHHANG WHERE MaKhachHang=@id", conn);
                    cmdKH.Parameters.AddWithValue("@id", kh.MaKhachHang);
                    if ((long)cmdKH.ExecuteScalar() == 0)
                    { MessageBox.Show("Khách hàng không tồn tại!", "Lỗi"); return; }

                    // Bước 04: Kiểm tra thẻ còn hạn
                    if (!kh.NgayHetHan.HasValue || kh.NgayHetHan.Value < ngayMuon)
                    {
                        MessageBox.Show(
                            "Thẻ khách hàng đã hết hạn!\nNgày hết hạn: " +
                            (kh.NgayHetHan.HasValue ? kh.NgayHetHan.Value.ToString("dd/MM/yyyy") : "Không có"),
                            "Lỗi thẻ hết hạn");
                        return;
                    }

                    // Bước 06-07: Kiểm tra từng game tồn tại và tình trạng "Có sẵn"
                    foreach (var g in danhSachChon)
                    {
                        var cmdG = new MySqlCommand(
                            "SELECT TinhTrang FROM CTPHIEUNHAP WHERE MaCTPN=@id", conn);
                        cmdG.Parameters.AddWithValue("@id", g.MaCTPN);
                        object tt = cmdG.ExecuteScalar();
                        if (tt == null)
                        { MessageBox.Show($"Game \"{g.TenGame}\" không tồn tại!", "Lỗi"); return; }
                        if (tt.ToString() != "Có sẵn")
                        { MessageBox.Show($"Game \"{g.TenGame}\" không có sẵn! Tình trạng: {tt}", "Lỗi"); return; }
                    }

                    // Bước 10: Kiểm tra game quá hạn
                    var cmdQH = new MySqlCommand(
                        @"SELECT COUNT(*) FROM CTPHIEUMUON ctm
                          INNER JOIN PHIEUMUON pm ON ctm.MaPhieuMuon = pm.MaPhieuMuon
                          WHERE pm.MaKhachHang=@id
                            AND ctm.NgayTra IS NULL
                            AND pm.NgayHetHanMuon < @ngay", conn);
                    cmdQH.Parameters.AddWithValue("@id", kh.MaKhachHang);
                    cmdQH.Parameters.AddWithValue("@ngay", ngayMuon);
                    if ((long)cmdQH.ExecuteScalar() > 0)
                    {
                        MessageBox.Show("Khách hàng có game mượn QUÁ HẠN chưa trả!\nVui lòng trả game trước.", "Lỗi");
                        return;
                    }

                    // Bước 13-15: Kiểm tra số lượng game không vượt quy định
                    var cmdDM = new MySqlCommand(
                        @"SELECT COUNT(*) FROM CTPHIEUMUON ctm
                          INNER JOIN PHIEUMUON pm ON ctm.MaPhieuMuon = pm.MaPhieuMuon
                          WHERE pm.MaKhachHang=@id AND ctm.NgayTra IS NULL", conn);
                    cmdDM.Parameters.AddWithValue("@id", kh.MaKhachHang);
                    long dangMuon = (long)cmdDM.ExecuteScalar();

                    if (dangMuon + danhSachChon.Count > _quyDinhSoGame)
                    {
                        MessageBox.Show(
                            $"Khách hàng đang mượn {dangMuon} game.\n" +
                            $"Muốn mượn thêm {danhSachChon.Count} game nữa = {dangMuon + danhSachChon.Count} game.\n" +
                            $"Quy định tối đa {_quyDinhSoGame} game!",
                            "Lỗi vượt quy định");
                        return;
                    }

                    // Bước 16: Lưu PHIEUMUON
                    var cmdPM = new MySqlCommand(
                        @"INSERT INTO PHIEUMUON (MaKhachHang, NgayMuon, NgayHetHanMuon)
                          VALUES (@kh, @ngay, @hethan);
                          SELECT LAST_INSERT_ID();", conn);
                    cmdPM.Parameters.AddWithValue("@kh", kh.MaKhachHang);
                    cmdPM.Parameters.AddWithValue("@ngay", ngayMuon);
                    cmdPM.Parameters.AddWithValue("@hethan", ngayHetHanMuon);
                    long maPhieuMuon = Convert.ToInt64(cmdPM.ExecuteScalar());

                    // Lưu từng CTPHIEUMUON + cập nhật tình trạng game
                    foreach (var g in danhSachChon)
                    {
                        // Insert CT
                        var cmdCT = new MySqlCommand(
                            @"INSERT INTO CTPHIEUMUON (MaPhieuMuon, MaGame, NgayTra)
                              VALUES (@pm, @game, NULL)", conn);
                        cmdCT.Parameters.AddWithValue("@pm", maPhieuMuon);
                        cmdCT.Parameters.AddWithValue("@game", g.MaCTPN);
                        cmdCT.ExecuteNonQuery();

                        // Bước 17: Cập nhật tình trạng
                        var cmdUp = new MySqlCommand(
                            "UPDATE CTPHIEUNHAP SET TinhTrang=N'Đang được cho mượn' WHERE MaCTPN=@id", conn);
                        cmdUp.Parameters.AddWithValue("@id", g.MaCTPN);
                        cmdUp.ExecuteNonQuery();
                    }

                    // Hiển thị Mã phiếu mượn vừa tạo
                    txbMaPhieuMuon.Text = maPhieuMuon.ToString();
                    txbMaPhieuMuon.Foreground = System.Windows.Media.Brushes.Black;
                }

                MessageBox.Show(
                    $"Lập phiếu cho mượn thành công!\n\n" +
                    $"Mã phiếu    : {txbMaPhieuMuon.Text}\n" +
                    $"Khách hàng  : {kh.HoTen}\n" +
                    $"Ngày mượn   : {ngayMuon:dd/MM/yyyy}\n" +
                    $"Hạn trả     : {ngayHetHanMuon:dd/MM/yyyy}\n" +
                    $"Số game     : {danhSachChon.Count}",
                    "Thành công");

                ResetForm();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi lập phiếu: " + ex.Message, "Lỗi"); }
        }

        // ==================== RESET FORM ====================
        private void ResetForm()
        {
            cboKhachHang.SelectedIndex = -1;
            txbNgayHetHan.Text = "";
            txbNgayHetHanMuon.Text = "";
            dpNgayMuon.SelectedDate = DateTime.Today;
            txbMaPhieuMuon.Text = "<Giá trị phát sinh>";
            txbMaPhieuMuon.Foreground = System.Windows.Media.Brushes.Gray;

            NapDanhSachGame(); // load lại game vì tình trạng đã thay đổi
        }

        // ==================== THOÁT ====================
        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
