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
using Sprint_5;

namespace Sprint5
{
    public partial class MainWindow : Window
    {
        // ==================== MODEL ====================
        private class KhachHangItem
        {
            public int MaKhachHang { get; set; }
            public string HoTen { get; set; }
            public decimal TienNo { get; set; }
            public override string ToString() => HoTen;
        }

        private class GameItem
        {
            public int MaGame { get; set; }
            public string TenGame { get; set; }
            public DateTime NgayMuon { get; set; }
            public DateTime NgayHetHanMuon { get; set; }
            public override string ToString() => TenGame;
        }

        private decimal _quiDinhTienPhat = 0;

        // Map dòng → các control tương ứng
        private ComboBox[] _cbGames;
        private TextBlock[] _txbNgayMuon, _txbSoNgayMuon, _txbTienPhat;

        // ==================== CONSTRUCTOR ====================
        public MainWindow() { InitializeComponent(); }

        // ==================== LOAD ====================
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _cbGames = new[] { cboGame1, cboGame2 };
            _txbNgayMuon = new[] { txbNgayMuon1, txbNgayMuon2 };
            _txbSoNgayMuon = new[] { txbSoNgayMuon1, txbSoNgayMuon2 };
            _txbTienPhat = new[] { txbTienPhat1, txbTienPhat2 };

            dpNgayTra.SelectedDate = DateTime.Today;

            NapQuyDinh();
            NapDanhSachKhachHang();
            CapNhatMaPhieuTraPreview();
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
                        "SELECT QuiDinhTienPhat FROM THAMSO LIMIT 1", conn);
                    var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        _quiDinhTienPhat = Convert.ToDecimal(r["QuiDinhTienPhat"]);
                        txbQuyDinhTienPhat.Text = _quiDinhTienPhat.ToString("N0") + " đ/ngày";
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi đọc quy định: " + ex.Message, "Lỗi"); }
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
                    // Chỉ lấy KH còn game chưa trả
                    var cmd = new MySqlCommand(
                        @"SELECT DISTINCT kh.MaKhachHang, kh.HoTen, kh.TienNo
                          FROM KHACHHANG kh
                          INNER JOIN PHIEUMUON pm ON pm.MaKhachHang = kh.MaKhachHang
                          INNER JOIN CTPHIEUMUON ct ON ct.MaPhieuMuon = pm.MaPhieuMuon
                          WHERE ct.NgayTra IS NULL
                          ORDER BY kh.HoTen", conn);
                    var r = cmd.ExecuteReader();
                    while (r.Read())
                        cboKhachHang.Items.Add(new KhachHangItem
                        {
                            MaKhachHang = Convert.ToInt32(r["MaKhachHang"]),
                            HoTen = r["HoTen"].ToString(),
                            TienNo = r["TienNo"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TienNo"])
                        });
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi nạp khách hàng: " + ex.Message, "Lỗi"); }
        }

        // ==================== NẠP GAME CỦA KH (ĐANG MƯỢN) ====================
        private void NapGameDangMuonCuaKH(int maKH)
        {
            try
            {
                var danhSach = new List<GameItem>();
                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        @"SELECT ct.MaGame, g.TenGame, pm.NgayMuon, pm.NgayHetHanMuon
                          FROM CTPHIEUMUON ct
                          INNER JOIN PHIEUMUON pm ON pm.MaPhieuMuon = ct.MaPhieuMuon
                          INNER JOIN CTPHIEUNHAP g ON g.MaCTPN = ct.MaGame
                          WHERE pm.MaKhachHang = @maKH
                            AND ct.NgayTra IS NULL
                          ORDER BY g.TenGame", conn);
                    cmd.Parameters.AddWithValue("@maKH", maKH);
                    var r = cmd.ExecuteReader();
                    while (r.Read())
                        danhSach.Add(new GameItem
                        {
                            MaGame = Convert.ToInt32(r["MaGame"]),
                            TenGame = r["TenGame"].ToString(),
                            NgayMuon = Convert.ToDateTime(r["NgayMuon"]),
                            NgayHetHanMuon = Convert.ToDateTime(r["NgayHetHanMuon"])
                        });
                }

                // Điền vào tất cả ComboBox game
                foreach (var cb in _cbGames)
                {
                    cb.Items.Clear();
                    cb.Items.Add(new GameItem { MaGame = -1, TenGame = "" });
                    foreach (var g in danhSach)
                        cb.Items.Add(g);
                    cb.SelectedIndex = 0;
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi nạp game đang mượn: " + ex.Message, "Lỗi"); }
        }

        // ==================== CHỌN KHÁCH HÀNG ====================
        private void CboKhachHang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboKhachHang.SelectedItem is KhachHangItem kh)
            {
                txbTienNo.Text = kh.TienNo.ToString("N0") + " đ";
                NapGameDangMuonCuaKH(kh.MaKhachHang);
            }
            else
            {
                txbTienNo.Text = "";
                foreach (var cb in _cbGames) cb.Items.Clear();
            }
            TinhLaiTienPhat();
        }

        // ==================== ĐỔI NGÀY TRẢ ====================
        private void DpNgayTra_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            for (int i = 0; i < _cbGames.Length; i++)
                RecalcRow(i);
            TinhLaiTienPhat();
        }

        // ==================== CHỌN GAME → TỰ ĐIỀN THÔNG TIN ====================
        private void CboGame_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)sender;
            int idx = int.Parse(cb.Tag.ToString()) - 1; // Tag="1"→0, Tag="2"→1

            if (cb.SelectedItem is GameItem g && g.MaGame != -1)
            {
                // Kiểm tra trùng game với hàng kia
                for (int i = 0; i < _cbGames.Length; i++)
                {
                    if (i == idx) continue;
                    if (_cbGames[i].SelectedItem is GameItem other && other.MaGame == g.MaGame)
                    {
                        MessageBox.Show("Game này đã được chọn ở hàng khác!", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        cb.SelectedIndex = 0;
                        return;
                    }
                }
                RecalcRow(idx);
            }
            else
            {
                _txbNgayMuon[idx].Text = "";
                _txbSoNgayMuon[idx].Text = "";
                _txbTienPhat[idx].Text = "";
            }
            TinhLaiTienPhat();
        }

        // ==================== TÍNH LẠI 1 HÀNG ====================
        private void RecalcRow(int idx)
        {
            if (!(_cbGames[idx].SelectedItem is GameItem g) || g.MaGame == -1)
            {
                _txbNgayMuon[idx].Text = "";
                _txbSoNgayMuon[idx].Text = "";
                _txbTienPhat[idx].Text = "";
                return;
            }

            DateTime ngayTra = dpNgayTra.SelectedDate ?? DateTime.Today;

            if (ngayTra < g.NgayMuon)
            {
                MessageBox.Show($"Ngày trả phải >= ngày mượn ({g.NgayMuon:dd/MM/yyyy})!",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                _cbGames[idx].SelectedIndex = 0;
                _txbNgayMuon[idx].Text = "";
                _txbSoNgayMuon[idx].Text = "";
                _txbTienPhat[idx].Text = "";
                return;
            }

            int soNgayMuon = (ngayTra - g.NgayMuon).Days;
            int soNgayTre = Math.Max(0, (ngayTra - g.NgayHetHanMuon).Days);
            decimal tienPhat = soNgayTre * _quiDinhTienPhat;

            _txbNgayMuon[idx].Text = g.NgayMuon.ToString("dd/MM/yyyy");
            _txbSoNgayMuon[idx].Text = soNgayMuon.ToString();
            _txbTienPhat[idx].Text = tienPhat.ToString("N0") + " đ";
        }

        // ==================== TÍNH TỔNG TIỀN PHẠT ====================
        private void TinhLaiTienPhat()
        {
            decimal tienPhatKyNay = 0;
            foreach (var txb in _txbTienPhat)
            {
                string raw = txb.Text.Replace("đ", "").Replace(",", "").Trim();
                if (decimal.TryParse(raw, out decimal p)) tienPhatKyNay += p;
            }

            decimal tienNoCu = 0;
            if (cboKhachHang.SelectedItem is KhachHangItem kh)
                tienNoCu = kh.TienNo;

            txbTienPhatKyNay.Text = tienPhatKyNay.ToString("N0") + " đ";
            txbTongNo.Text = (tienNoCu + tienPhatKyNay).ToString("N0") + " đ";
        }

        // ==================== LẬP PHIẾU (XỬ LÝ CHÍNH) ====================
        private void BtnLapPhieu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // --- Validate cơ bản ---
                if (!(cboKhachHang.SelectedItem is KhachHangItem kh))
                { MessageBox.Show("Vui lòng chọn khách hàng!", "Thông báo"); return; }

                if (!dpNgayTra.SelectedDate.HasValue)
                { MessageBox.Show("Vui lòng chọn ngày trả!", "Thông báo"); return; }

                DateTime ngayTra = dpNgayTra.SelectedDate.Value;

                // Thu thập danh sách game được chọn
                var danhSachChon = new List<GameItem>();
                for (int i = 0; i < _cbGames.Length; i++)
                {
                    if (_cbGames[i].SelectedItem is GameItem g && g.MaGame != -1)
                    {
                        if (danhSachChon.Exists(x => x.MaGame == g.MaGame))
                        { MessageBox.Show($"Game \"{g.TenGame}\" được chọn trùng!", "Lỗi"); return; }
                        danhSachChon.Add(g);
                    }
                }

                if (danhSachChon.Count == 0)
                { MessageBox.Show("Vui lòng chọn ít nhất 1 game để trả!", "Thông báo"); return; }

                // Tính tiền phạt từng game
                decimal tienPhatKyNay = 0;
                var danhSachTra = new List<(int MaGame, decimal TienPhat)>();
                foreach (var g in danhSachChon)
                {
                    int soNgayTre = Math.Max(0, (ngayTra - g.NgayHetHanMuon).Days);
                    decimal phat = soNgayTre * _quiDinhTienPhat;
                    tienPhatKyNay += phat;
                    danhSachTra.Add((g.MaGame, phat));
                }
                decimal tongNo = kh.TienNo + tienPhatKyNay;

                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. INSERT PHIEUTRA
                            var cmdPhieu = new MySqlCommand(
                                @"INSERT INTO PHIEUTRA (MaKhachHang, NgayTraPhieu, TienPhatKyNay)
                                  VALUES (@maKH, @ngayTra, @tienPhat);
                                  SELECT LAST_INSERT_ID();", conn, transaction);
                            cmdPhieu.Parameters.AddWithValue("@maKH", kh.MaKhachHang);
                            cmdPhieu.Parameters.AddWithValue("@ngayTra", ngayTra);
                            cmdPhieu.Parameters.AddWithValue("@tienPhat", tienPhatKyNay);
                            int maPhieuTra = Convert.ToInt32(cmdPhieu.ExecuteScalar());

                            foreach (var (maGame, tienPhat) in danhSachTra)
                            {
                                // 2. INSERT CTPHIEUTRA
                                var cmdCT = new MySqlCommand(
                                    @"INSERT INTO CTPHIEUTRA (MaPhieuTra, MaGame, TienPhat)
                                      VALUES (@maPhieuTra, @maGame, @tienPhat)", conn, transaction);
                                cmdCT.Parameters.AddWithValue("@maPhieuTra", maPhieuTra);
                                cmdCT.Parameters.AddWithValue("@maGame", maGame);
                                cmdCT.Parameters.AddWithValue("@tienPhat", tienPhat);
                                cmdCT.ExecuteNonQuery();


                                var cmdNT = new MySqlCommand(
                                    @"UPDATE CTPHIEUMUON
                                      SET NgayTra = @ngayTra
                                      WHERE MaGame = @maGame
                                        AND NgayTra IS NULL
                                        AND MaPhieuMuon IN (
                                            SELECT MaPhieuMuon FROM PHIEUMUON
                                            WHERE MaKhachHang = @maKH
                                        )", conn, transaction);
                                cmdNT.Parameters.AddWithValue("@ngayTra", ngayTra);
                                cmdNT.Parameters.AddWithValue("@maKH", kh.MaKhachHang);
                                cmdNT.Parameters.AddWithValue("@maGame", maGame);
                                cmdNT.ExecuteNonQuery();

                                // 4. Cập nhật TinhTrang game = 'Có sẵn'
                                var cmdTT = new MySqlCommand(
                                    "UPDATE CTPHIEUNHAP SET TinhTrang = N'Có sẵn' WHERE MaCTPN = @maGame",
                                    conn, transaction);
                                cmdTT.Parameters.AddWithValue("@maGame", maGame);
                                cmdTT.ExecuteNonQuery();
                            }

                            // 5. Cập nhật TienNo khách hàng
                            var cmdTN = new MySqlCommand(
                                "UPDATE KHACHHANG SET TienNo = @tongNo WHERE MaKhachHang = @maKH",
                                conn, transaction);
                            cmdTN.Parameters.AddWithValue("@tongNo", tongNo);
                            cmdTN.Parameters.AddWithValue("@maKH", kh.MaKhachHang);
                            cmdTN.ExecuteNonQuery();

                            transaction.Commit();

                            // Hiển thị mã phiếu vừa tạo
                            txbMaPhieuTra.Text = maPhieuTra.ToString();
                            txbMaPhieuTra.Foreground = Brushes.Black;

                            MessageBox.Show(
                                $"Lập phiếu trả thành công!\n\n" +
                                $"Mã phiếu trả : {maPhieuTra}\n" +
                                $"Khách hàng   : {kh.HoTen}\n" +
                                $"Ngày trả     : {ngayTra:dd/MM/yyyy}\n" +
                                $"Tiền phạt    : {tienPhatKyNay:N0} đ\n" +
                                $"Tổng nợ      : {tongNo:N0} đ",
                                "Thành công");

                            ResetForm();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi lập phiếu trả: " + ex.Message, "Lỗi"); }
        }

        // ==================== RESET FORM ====================
        private void ResetForm()
        {
            cboKhachHang.SelectedIndex = -1;
            dpNgayTra.SelectedDate = DateTime.Today;

            foreach (var cb in _cbGames) cb.Items.Clear();
            foreach (var txb in _txbNgayMuon) txb.Text = "";
            foreach (var txb in _txbSoNgayMuon) txb.Text = "";
            foreach (var txb in _txbTienPhat) txb.Text = "";

            txbTienNo.Text = "";
            txbTienPhatKyNay.Text = "";
            txbTongNo.Text = "";

            NapDanhSachKhachHang();
            CapNhatMaPhieuTraPreview();
        }

        // ==================== LẤY MÃ PHIẾU TIẾP THEO ====================
        private void CapNhatMaPhieuTraPreview()
        {
            try
            {
                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "SELECT AUTO_INCREMENT FROM information_schema.TABLES " +
                        "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PHIEUTRA'", conn);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        txbMaPhieuTra.Text = Convert.ToInt64(result).ToString();
                        txbMaPhieuTra.Foreground = Brushes.Gray;
                    }
                }
            }
            catch
            {
                txbMaPhieuTra.Text = "<Giá trị phát sinh>";
                txbMaPhieuTra.Foreground = Brushes.Gray;
            }
        }

        // ==================== THOÁT ====================
        private void BtnThoat_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}