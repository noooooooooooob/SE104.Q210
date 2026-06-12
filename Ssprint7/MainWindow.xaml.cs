using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using Ssprint7;

namespace Sprint7
{
    public partial class MainWindow : Window
    {
        // ==================== MODELS ====================
        private class KhachHangItem
        {
            public int MaKhachHang { get; set; }
            public string HoTen { get; set; }
            public override string ToString() => HoTen;
        }

        private class GameDangMuonItem
        {
            public int MaCTPhieuMuon { get; set; }
            public int MaGame { get; set; }
            public string TenGame { get; set; }
            public DateTime NgayMuon { get; set; }
            public decimal TriGia { get; set; }
            public override string ToString() => TenGame;
        }

        // ==================== FIELD ====================
        private const int SO_DONG = 4;
        private List<ComboBox> _cboGames = new List<ComboBox>();
        private List<TextBlock> _txbNgayMuons = new List<TextBlock>();
        private List<TextBox> _txtTienPhats = new List<TextBox>();

        // ==================== CONSTRUCTOR ====================
        public MainWindow() { InitializeComponent(); }

        // ==================== LOAD ====================
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TaoLuoiDongGame();
            NapDanhSachKhachHang();
            CapNhatMaPhieuMatPreview();
            dtpNgayGhiNhan.SelectedDate = DateTime.Today;
            dtpNgayGhiNhan.DisplayDateEnd = DateTime.Today;
        }

        // ==================== TẠO LƯỚI DÒNG GAME ĐỘNG ====================
        private void TaoLuoiDongGame()
        {
            _cboGames.Clear();
            _txbNgayMuons.Clear();
            _txtTienPhats.Clear();
            pnlDongGame.Children.Clear();

            for (int i = 0; i < SO_DONG; i++)
            {
                int idx = i;
                var row = new Grid { Height = 36, Margin = new Thickness(0, 0, 0, 1) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });

                // STT
                var stt = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(stt, 0);
                row.Children.Add(stt);

                // ComboBox Tên game
                var cbo = new ComboBox
                {
                    FontSize = 13,
                    Margin = new Thickness(4, 2, 4, 2),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    IsEnabled = false
                };
                cbo.SelectionChanged += (s, e2) => CboGame_SelectionChanged(idx);
                Grid.SetColumn(cbo, 1);
                row.Children.Add(cbo);
                _cboGames.Add(cbo);

                // TextBlock Ngày mượn (read-only)
                var borderNgay = new Border
                {
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                    Margin = new Thickness(4, 2, 4, 2),
                    CornerRadius = new CornerRadius(3)
                };
                var txbNgay = new TextBlock
                {
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0)
                };
                borderNgay.Child = txbNgay;
                Grid.SetColumn(borderNgay, 2);
                row.Children.Add(borderNgay);
                _txbNgayMuons.Add(txbNgay);

                // TextBox Tiền phạt (nhập được)
                var borderTien = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
                    BorderThickness = new Thickness(1),
                    Background = Brushes.White,
                    Margin = new Thickness(4, 2, 4, 2),
                    CornerRadius = new CornerRadius(3)
                };
                var txt = new TextBox
                {
                    FontSize = 13,
                    BorderThickness = new Thickness(0),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0),
                    IsEnabled = false
                };
                txt.TextChanged += (s, e2) => TinhTongTienPhat();
                borderTien.Child = txt;
                Grid.SetColumn(borderTien, 3);
                row.Children.Add(borderTien);
                _txtTienPhats.Add(txt);

                pnlDongGame.Children.Add(row);
            }
        }

        // ==================== NẠP KHÁCH HÀNG (có game đang mượn) ====================
        private void NapDanhSachKhachHang()
        {
            try
            {
                cboKhachHang.Items.Clear();
                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    // Chỉ lấy KH đang có game mượn chưa trả
                    var cmd = new MySqlCommand(
                        @"SELECT DISTINCT k.MaKhachHang, k.HoTen
                          FROM KHACHHANG k
                          JOIN PHIEUMUON pm ON pm.MaKhachHang = k.MaKhachHang
                          JOIN CTPHIEUMUON ct ON ct.MaPhieuMuon = pm.MaPhieuMuon
                          WHERE ct.NgayTra IS NULL
                          ORDER BY k.HoTen", conn);
                    var r = cmd.ExecuteReader();
                    while (r.Read())
                        cboKhachHang.Items.Add(new KhachHangItem
                        {
                            MaKhachHang = Convert.ToInt32(r["MaKhachHang"]),
                            HoTen = r["HoTen"].ToString()
                        });
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi nạp khách hàng: " + ex.Message, "Lỗi"); }
        }

        // ==================== PHÁT SINH MÃ PHIẾU MẤT ====================
        private void CapNhatMaPhieuMatPreview()
        {
            try
            {
                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "SELECT AUTO_INCREMENT FROM information_schema.TABLES " +
                        "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PHIEUMAT'", conn);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        txbMaPhieuMat.Text = Convert.ToInt64(result).ToString();
                        txbMaPhieuMat.Foreground = Brushes.Gray;
                    }
                }
            }
            catch
            {
                txbMaPhieuMat.Text = "<Giá trị phát sinh>";
                txbMaPhieuMat.Foreground = Brushes.Gray;
            }
        }

        // ==================== CHỌN KHÁCH HÀNG → NẠP GAME ĐANG MƯỢN ====================
        private void CboKhachHang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reset hết các dòng
            foreach (var cbo in _cboGames) { cbo.Items.Clear(); cbo.IsEnabled = false; }
            foreach (var txb in _txbNgayMuons) txb.Text = "";
            foreach (var txt in _txtTienPhats) { txt.Text = ""; txt.IsEnabled = false; }
            txbTongTienPhat.Text = "";

            if (!(cboKhachHang.SelectedItem is KhachHangItem kh)) return;

            try
            {
                var danhSachGame = LayDanhSachGameDangMuon(kh.MaKhachHang);
                foreach (var cbo in _cboGames)
                {
                    cbo.Items.Clear();
                    foreach (var g in danhSachGame)
                        cbo.Items.Add(g);
                    cbo.IsEnabled = danhSachGame.Count > 0;
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi nạp game: " + ex.Message, "Lỗi"); }
        }

        // ==================== LẤY DANH SÁCH GAME ĐANG MƯỢN CỦA KH ====================
        private List<GameDangMuonItem> LayDanhSachGameDangMuon(int maKhachHang)
        {
            var list = new List<GameDangMuonItem>();
            using (var conn = DataProvider.Instance.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand(
                    @"SELECT ct.MaCTPhieuMuon, ct.MaGame, g.TenGame, pm.NgayMuon, g.TriGia
                      FROM CTPHIEUMUON ct
                      JOIN PHIEUMUON pm ON pm.MaPhieuMuon = ct.MaPhieuMuon
                      JOIN CTPHIEUNHAP g ON g.MaCTPN = ct.MaGame
                      WHERE pm.MaKhachHang = @maKH AND ct.NgayTra IS NULL
                      ORDER BY g.TenGame", conn);
                cmd.Parameters.AddWithValue("@maKH", maKhachHang);
                var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new GameDangMuonItem
                    {
                        MaCTPhieuMuon = Convert.ToInt32(r["MaCTPhieuMuon"]),
                        MaGame = Convert.ToInt32(r["MaGame"]),
                        TenGame = r["TenGame"].ToString(),
                        NgayMuon = Convert.ToDateTime(r["NgayMuon"]),
                        TriGia = Convert.ToDecimal(r["TriGia"])
                    });
            }
            return list;
        }

        // ==================== CHỌN GAME → TỰ ĐIỀN NGÀY MƯỢN, BẬT TIỀN PHẠT ====================
        private void CboGame_SelectionChanged(int idx)
        {
            if (_cboGames[idx].SelectedItem is GameDangMuonItem g)
            {
                _txbNgayMuons[idx].Text = g.NgayMuon.ToString("dd/MM/yyyy");
                _txtTienPhats[idx].IsEnabled = true;
                // Gợi ý mặc định = TriGia
                if (string.IsNullOrEmpty(_txtTienPhats[idx].Text))
                    _txtTienPhats[idx].Text = g.TriGia.ToString("N0");
            }
            else
            {
                _txbNgayMuons[idx].Text = "";
                _txtTienPhats[idx].Text = "";
                _txtTienPhats[idx].IsEnabled = false;
            }
            TinhTongTienPhat();
        }

        // ==================== TÍNH TỔNG TIỀN PHẠT ====================
        private void TinhTongTienPhat()
        {
            decimal tong = 0;
            for (int i = 0; i < SO_DONG; i++)
            {
                if (_cboGames[i].SelectedItem == null) continue;
                if (decimal.TryParse(_txtTienPhats[i].Text.Replace(",", "").Trim(), out decimal val) && val > 0)
                    tong += val;
            }
            txbTongTienPhat.Text = tong > 0 ? tong.ToString("N0") + " đ" : "";
        }

        // ==================== TÌM KHÁCH HÀNG ====================
        private void BtnTimKhachHang_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder – implement nếu cần
        }

        // ==================== LẬP PHIẾU GHI NHẬN MẤT GAME (XỬ LÝ CHÍNH) ====================
        private void BtnLapPhieu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Bước 03: Kiểm tra KH có trong danh sách phiếu mượn
                if (!(cboKhachHang.SelectedItem is KhachHangItem kh))
                { MessageBox.Show("Vui lòng chọn khách hàng!", "Thông báo"); return; }

                // Kiểm tra ngày ghi nhận
                if (dtpNgayGhiNhan.SelectedDate == null)
                { MessageBox.Show("Vui lòng chọn ngày ghi nhận!", "Thông báo"); return; }
                DateTime ngayGhiNhan = dtpNgayGhiNhan.SelectedDate.Value;

                // Thu thập danh sách game được chọn
                var dsDongHopLe = new List<(GameDangMuonItem game, decimal tienPhat)>();
                var dsMaGameDaChon = new HashSet<int>();

                for (int i = 0; i < SO_DONG; i++)
                {
                    if (!(_cboGames[i].SelectedItem is GameDangMuonItem g)) continue;

                    // Không cho chọn trùng game
                    if (dsMaGameDaChon.Contains(g.MaGame))
                    { MessageBox.Show($"Game \"{g.TenGame}\" bị chọn trùng!", "Lỗi"); return; }

                    // Bước 06: Kiểm tra mã game thuộc D4
                    // (đã được đảm bảo vì ComboBox chỉ chứa game đang mượn của KH)

                    // Bước 07: Ngày ghi nhận >= Ngày mượn
                    if (ngayGhiNhan < g.NgayMuon)
                    {
                        MessageBox.Show(
                            $"Ngày ghi nhận phải >= ngày mượn của game \"{g.TenGame}\" ({g.NgayMuon:dd/MM/yyyy})!",
                            "Lỗi");
                        return;
                    }

                    // Bước 09: Tiền phạt >= Trị giá game (QĐ7)
                    string rawTien = _txtTienPhats[i].Text.Replace(",", "").Trim();
                    if (!decimal.TryParse(rawTien, out decimal tienPhat) || tienPhat <= 0)
                    { MessageBox.Show($"Tiền phạt của game \"{g.TenGame}\" không hợp lệ!", "Lỗi"); return; }

                    if (tienPhat < g.TriGia)
                    {
                        MessageBox.Show(
                            $"Tiền phạt của \"{g.TenGame}\" ({tienPhat:N0} đ) " +
                            $"không được nhỏ hơn trị giá ({g.TriGia:N0} đ)!",
                            "Lỗi");
                        return;
                    }

                    dsMaGameDaChon.Add(g.MaGame);
                    dsDongHopLe.Add((g, tienPhat));
                }

                if (dsDongHopLe.Count == 0)
                { MessageBox.Show("Vui lòng chọn ít nhất 1 game bị mất!", "Thông báo"); return; }

                // Bước 10: Tổng tiền phạt
                decimal tongTienPhat = 0;
                foreach (var d in dsDongHopLe) tongTienPhat += d.tienPhat;

                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // Bước 11-12: Đọc tiền nợ hiện tại → tính tiền nợ mới
                            var cmdKHRead = new MySqlCommand(
                                "SELECT TienNo FROM KHACHHANG WHERE MaKhachHang = @maKH",
                                conn, trans);
                            cmdKHRead.Parameters.AddWithValue("@maKH", kh.MaKhachHang);
                            decimal tienNoHienTai = Convert.ToDecimal(cmdKHRead.ExecuteScalar());
                            decimal tienNoMoi = tienNoHienTai + tongTienPhat;

                            // Bước 13: Lưu PHIEUMAT
                            var cmdPhieu = new MySqlCommand(
                                @"INSERT INTO PHIEUMAT (MaKhachHang, NgayGhiNhan, TongTienPhatMatGame)
                                  VALUES (@maKH, @ngay, @tong);
                                  SELECT LAST_INSERT_ID();", conn, trans);
                            cmdPhieu.Parameters.AddWithValue("@maKH", kh.MaKhachHang);
                            cmdPhieu.Parameters.AddWithValue("@ngay", ngayGhiNhan);
                            cmdPhieu.Parameters.AddWithValue("@tong", tongTienPhat);
                            int maPhieuMat = Convert.ToInt32(cmdPhieu.ExecuteScalar());

                            foreach (var (game, tienPhat) in dsDongHopLe)
                            {
                                // Lưu CTPHIEUMAT
                                var cmdCT = new MySqlCommand(
                                    @"INSERT INTO CTPHIEUMAT (MaPhieuMat, MaGame, TienPhatMatGame)
                                      VALUES (@maPM, @maGame, @tien)", conn, trans);
                                cmdCT.Parameters.AddWithValue("@maPM", maPhieuMat);
                                cmdCT.Parameters.AddWithValue("@maGame", game.MaGame);
                                cmdCT.Parameters.AddWithValue("@tien", tienPhat);
                                cmdCT.ExecuteNonQuery();

                                // Bước 14: Cập nhật NgayTra trong CTPHIEUMUON
                                var cmdTra = new MySqlCommand(
                                    @"UPDATE CTPHIEUMUON SET NgayTra = @ngay
                                      WHERE MaCTPhieuMuon = @maCT", conn, trans);
                                cmdTra.Parameters.AddWithValue("@ngay", ngayGhiNhan);
                                cmdTra.Parameters.AddWithValue("@maCT", game.MaCTPhieuMuon);
                                cmdTra.ExecuteNonQuery();

                                // Bước 15: Cập nhật TinhTrang = 'Mất' trong CTPHIEUNHAP
                                var cmdTT = new MySqlCommand(
                                    @"UPDATE CTPHIEUNHAP SET TinhTrang = N'Mất'
                                      WHERE MaCTPN = @maGame", conn, trans);
                                cmdTT.Parameters.AddWithValue("@maGame", game.MaGame);
                                cmdTT.ExecuteNonQuery();
                            }

                            // Bước 16: Cập nhật TienNo KH
                            var cmdKHUpdate = new MySqlCommand(
                                "UPDATE KHACHHANG SET TienNo = @tienNoMoi WHERE MaKhachHang = @maKH",
                                conn, trans);
                            cmdKHUpdate.Parameters.AddWithValue("@tienNoMoi", tienNoMoi);
                            cmdKHUpdate.Parameters.AddWithValue("@maKH", kh.MaKhachHang);
                            cmdKHUpdate.ExecuteNonQuery();

                            trans.Commit();

                            txbMaPhieuMat.Text = maPhieuMat.ToString();
                            txbMaPhieuMat.Foreground = Brushes.Black;

                            MessageBox.Show(
                                $"Lập phiếu ghi nhận mất game thành công!\n\n" +
                                $"Mã phiếu mất  : {maPhieuMat}\n" +
                                $"Khách hàng    : {kh.HoTen}\n" +
                                $"Ngày ghi nhận : {ngayGhiNhan:dd/MM/yyyy}\n" +
                                $"Số game mất   : {dsDongHopLe.Count}\n" +
                                $"Tổng tiền phạt: {tongTienPhat:N0} đ",
                                "Thành công");

                            ResetForm();
                        }
                        catch { trans.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi lập phiếu: " + ex.Message, "Lỗi"); }
        }

        // ==================== RESET FORM ====================
        private void ResetForm()
        {
            cboKhachHang.SelectedIndex = -1;
            dtpNgayGhiNhan.SelectedDate = DateTime.Today;
            txbTongTienPhat.Text = "";
            foreach (var cbo in _cboGames) { cbo.Items.Clear(); cbo.IsEnabled = false; }
            foreach (var txb in _txbNgayMuons) txb.Text = "";
            foreach (var txt in _txtTienPhats) { txt.Text = ""; txt.IsEnabled = false; }

            NapDanhSachKhachHang();
            CapNhatMaPhieuMatPreview();
        }

        // ==================== THOÁT ====================
        private void BtnThoat_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}