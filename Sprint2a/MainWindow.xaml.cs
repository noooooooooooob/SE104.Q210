using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySql.Data.MySqlClient;

namespace Sprint2a
{
    public partial class MainWindow : Window
    {
        // ==================== MODEL ====================
        public class RowData
        {
            public string STT { get; set; }
            public string MaCTPN { get; set; }  // thêm dòng này
        }

        // ==================== FIELDS ====================
        private ObservableCollection<RowData> rows = new ObservableCollection<RowData>();
        private int quiDinhTuoiGame = 3;

        // ==================== CONSTRUCTOR ====================
        public MainWindow()
        {
            InitializeComponent();
            LoadData();
        }

        // ==================== BƯỚC 02 + 03: LOAD DATA ====================
        private void LoadData()
        {
            using (var conn = DataProvider.Instance.GetConnection())
            {
                conn.Open();


                // Phát sinh Mã phiếu nhập
                var cmdPN = new MySqlCommand(
                    "SELECT IFNULL(MAX(MaPhieuNhap), 0) + 1 FROM PHIEUNHAP", conn);
                int maPhieuNhapMoi = Convert.ToInt32(cmdPN.ExecuteScalar());
                txtMaPN.Text = "PN" + maPhieuNhapMoi.ToString("D3");

                // Bước 03: Đọc Qui định tuổi game từ THAMSO
                var cmdTS = new MySqlCommand(
                    "SELECT QuiDinhTuoiGame FROM THAMSO", conn);
                var tsVal = cmdTS.ExecuteScalar();
                if (tsVal != null && tsVal != DBNull.Value)
                    quiDinhTuoiGame = Convert.ToInt32(tsVal);

                txtQuiDinhTuoiGame.Text = quiDinhTuoiGame.ToString() + " năm";
            }

            // Set ngày nhập mặc định = hôm nay
            dpNgayNhap.SelectedDate = DateTime.Today;

            // Reset Tổng tiền
            txtTongTien.Text = "0 VNĐ";

            // Init bảng 4 dòng + dòng "+"
            rows.Clear();

            // Đọc mã CT phiếu nhập hiện tại từ DB
            int maCTPNBase;
            using (var conn2 = DataProvider.Instance.GetConnection())
            {
                conn2.Open();
                var cmd2 = new MySqlCommand(
                    "SELECT IFNULL(MAX(MaCTPN), 0) + 1 FROM CTPHIEUNHAP", conn2);
                maCTPNBase = Convert.ToInt32(cmd2.ExecuteScalar());
            }

            rows.Add(new RowData { STT = "1", MaCTPN = "CTPN" + (maCTPNBase).ToString("D3") });
            rows.Add(new RowData { STT = "2", MaCTPN = "CTPN" + (maCTPNBase + 1).ToString("D3") });
            rows.Add(new RowData { STT = "3", MaCTPN = "CTPN" + (maCTPNBase + 2).ToString("D3") });
            rows.Add(new RowData { STT = "4", MaCTPN = "CTPN" + (maCTPNBase + 3).ToString("D3") });
            rows.Add(new RowData { STT = "+", MaCTPN = "" });
            dgGames.ItemsSource = rows;
        }

        // ==================== BƯỚC 02: LOAD COMBOBOX LOẠI GAME ====================
        private void CboLoaiGame_Loaded(object sender, RoutedEventArgs e)
        {
            var cbo = sender as ComboBox;
            if (cbo == null || cbo.Items.Count > 0) return;

            try
            {
                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "SELECT MaLoaiGame, TenLoaiGame FROM LOAIGAME ORDER BY TenLoaiGame", conn);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        cbo.Items.Add(new ComboBoxItem
                        {
                            Content = reader["TenLoaiGame"].ToString(),
                            Tag = reader["MaLoaiGame"]
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi load loại game: " + ex.Message, "Lỗi");
            }
        }

        // ==================== NÚT STT (THÊM DÒNG) ====================
        private void BtnSTT_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            if (btn.Content.ToString() == "+")
            {
                // Lấy mã CT tiếp theo
                int nextMa;
                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "SELECT IFNULL(MAX(MaCTPN), 0) + 1 FROM CTPHIEUNHAP", conn);
                    nextMa = Convert.ToInt32(cmd.ExecuteScalar());
                }
                // Cộng thêm số dòng hiện có (trừ dòng "+")
                int offset = rows.Count - 1; // số dòng thực tế
                nextMa += offset;

                rows.RemoveAt(rows.Count - 1);
                rows.Add(new RowData
                {
                    STT = (rows.Count + 1).ToString(),
                    MaCTPN = "CTPN" + nextMa.ToString("D3")
                });
                rows.Add(new RowData { STT = "+", MaCTPN = "" });
            }
        }

        // ==================== TỔNG TIỀN REALTIME ====================
        private void TxtTriGia_TextChanged(object sender, TextChangedEventArgs e)
        {
            TinhTongTienRealtime();
        }

        private void TinhTongTienRealtime()
        {
            decimal tong = 0;
            foreach (var item in dgGames.Items)
            {
                var row = dgGames.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row == null) continue;
                var data = item as RowData;
                if (data == null || data.STT == "+") continue;

                string triGiaStr = LayTextTuCell(row, 7);
                if (decimal.TryParse(triGiaStr, out decimal triGia))
                    tong += triGia;
            }
            txtTongTien.Text = tong.ToString("N0") + " VNĐ";
        }

        // ==================== NÚT LẬP PHIẾU NHẬP ====================
        private void BtnLapPhieuNhap_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra Ngày nhập
            if (dpNgayNhap.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn Ngày nhập!", "Lỗi");
                return;
            }
            DateTime ngayNhap = dpNgayNhap.SelectedDate.Value;

            // Đọc dữ liệu từ DataGrid
            var danhSachGame = new System.Collections.Generic.List<GameRow>();

            foreach (var item in dgGames.Items)
            {
                var row = dgGames.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row == null) continue;
                var data = item as RowData;
                if (data == null || data.STT == "+") continue;

                string tenGame = LayTextTuCell(row, 2);
                string loaiGame = LayComboBoxTuCell(row, 3);
                int maLoaiGame = LayMaLoaiGameTuCell(row, 3);
                string nhaPhatHanh = LayTextTuCell(row, 4);
                string namPhatHanhStr = LayTextTuCell(row, 5);
                                                                 // cột 6 = TUỔI GAME (read-only, bỏ qua)
                string triGiaStr = LayTextTuCell(row, 7);

                // Bỏ qua dòng trống hoàn toàn
                if (string.IsNullOrWhiteSpace(tenGame) &&
                    string.IsNullOrWhiteSpace(loaiGame) &&
                    string.IsNullOrWhiteSpace(namPhatHanhStr) &&
                    string.IsNullOrWhiteSpace(triGiaStr))
                    continue;

                // Validate
                if (string.IsNullOrWhiteSpace(tenGame))
                { MessageBox.Show($"Dòng {data.STT}: Vui lòng nhập Tên game!", "Lỗi"); return; }

                // Bước 04: Kiểm tra Loại game thuộc danh sách
                if (string.IsNullOrWhiteSpace(loaiGame) || maLoaiGame == -1)
                { MessageBox.Show($"Dòng {data.STT}: Vui lòng chọn Loại game hợp lệ!", "Lỗi"); return; }

                if (!int.TryParse(namPhatHanhStr, out int namPhatHanh))
                { MessageBox.Show($"Dòng {data.STT}: Năm phát hành không hợp lệ!", "Lỗi"); return; }

                if (!decimal.TryParse(triGiaStr, out decimal triGia))
                { MessageBox.Show($"Dòng {data.STT}: Trị giá không hợp lệ!", "Lỗi"); return; }

                // Bước 05: Tính Số tuổi = Năm hiện tại - Năm phát hành
                int soTuoi = ngayNhap.Year - namPhatHanh;

                // Bước 06: Kiểm tra Số tuổi <= Qui định tuổi game
                if (soTuoi > quiDinhTuoiGame)
                {
                    MessageBox.Show(
                        $"Dòng {data.STT}: Game \"{tenGame}\" phát hành năm {namPhatHanh}\n" +
                        $"Số tuổi = {soTuoi} > Qui định {quiDinhTuoiGame} năm. Không được nhập!",
                        "Lỗi");
                    return;
                }

                // Bước 07: Kiểm tra Trị giá > 0
                if (triGia <= 0)
                {
                    MessageBox.Show($"Dòng {data.STT}: Trị giá phải lớn hơn 0!", "Lỗi");
                    return;
                }

                danhSachGame.Add(new GameRow
                {
                    TenGame = tenGame,
                    MaLoaiGame = maLoaiGame,
                    NhaPhatHanh = nhaPhatHanh,
                    NamPhatHanh = namPhatHanh,
                    SoTuoi = soTuoi,
                    TriGia = triGia
                });
            }

            if (danhSachGame.Count == 0)
            {
                MessageBox.Show("Vui lòng nhập ít nhất 1 game!", "Lỗi");
                return;
            }

            // Bước 09: Tính Tổng trị giá
            decimal tongTriGia = 0;
            foreach (var g in danhSachGame) tongTriGia += g.TriGia;

            // Bước 10: Lưu xuống CSDL
            try
            {
                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    var transaction = conn.BeginTransaction();
                    try
                    {
                        // Lưu PHIEUNHAP
                        var cmdPN = new MySqlCommand(
                            "INSERT INTO PHIEUNHAP (NgayNhap, TongTriGia) VALUES (@NgayNhap, @TongTriGia)",
                            conn, transaction);
                        cmdPN.Parameters.AddWithValue("@NgayNhap", ngayNhap);
                        cmdPN.Parameters.AddWithValue("@TongTriGia", tongTriGia);
                        cmdPN.ExecuteNonQuery();
                        long newMaPhieuNhap = cmdPN.LastInsertedId;

                        // Lưu từng CTPHIEUNHAP
                        foreach (var g in danhSachGame)
                        {
                            var cmdCT = new MySqlCommand(@"
                                INSERT INTO CTPHIEUNHAP
                                    (MaPhieuNhap, MaLoaiGame, TenGame, NhaPhatHanh, NamPhatHanh, TriGia, TinhTrang)
                                VALUES
                                    (@MaPhieuNhap, @MaLoaiGame, @TenGame, @NhaPhatHanh, @NamPhatHanh, @TriGia, @TinhTrang)",
                                conn, transaction);

                            cmdCT.Parameters.AddWithValue("@MaPhieuNhap", newMaPhieuNhap);
                            cmdCT.Parameters.AddWithValue("@MaLoaiGame", g.MaLoaiGame);
                            cmdCT.Parameters.AddWithValue("@TenGame", g.TenGame);
                            cmdCT.Parameters.AddWithValue("@NhaPhatHanh", g.NhaPhatHanh);
                            cmdCT.Parameters.AddWithValue("@NamPhatHanh", g.NamPhatHanh);
                            cmdCT.Parameters.AddWithValue("@TriGia", g.TriGia);
                            cmdCT.Parameters.AddWithValue("@TinhTrang", "Có sẵn");
                            cmdCT.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        MessageBox.Show(
                            $"Lập phiếu nhập thành công!\nTổng trị giá: {tongTriGia:N0} VNĐ",
                            "Thông báo");

                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.Message, "Lỗi");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể kết nối database: " + ex.Message, "Lỗi");
            }
        }

        // ==================== HÀM HỖ TRỢ ĐỌC CONTROL TỪ CELL ====================

        private string LayTextTuCell(DataGridRow row, int colIndex)
        {
            var presenter = FindVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>(row);
            if (presenter == null) return "";
            var cell = presenter.ItemContainerGenerator.ContainerFromIndex(colIndex) as DataGridCell;
            if (cell == null) return "";
            var tb = FindVisualChild<TextBox>(cell);
            return tb?.Text.Trim() ?? "";
        }

        private string LayComboBoxTuCell(DataGridRow row, int colIndex)
        {
            var presenter = FindVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>(row);
            if (presenter == null) return "";
            var cell = presenter.ItemContainerGenerator.ContainerFromIndex(colIndex) as DataGridCell;
            if (cell == null) return "";
            var cb = FindVisualChild<ComboBox>(cell);
            if (cb == null) return "";
            var selected = cb.SelectedItem as ComboBoxItem;
            return selected?.Content?.ToString() ?? "";
        }

        private int LayMaLoaiGameTuCell(DataGridRow row, int colIndex)
        {
            var presenter = FindVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>(row);
            if (presenter == null) return -1;
            var cell = presenter.ItemContainerGenerator.ContainerFromIndex(colIndex) as DataGridCell;
            if (cell == null) return -1;
            var cb = FindVisualChild<ComboBox>(cell);
            if (cb == null) return -1;
            var selected = cb.SelectedItem as ComboBoxItem;
            if (selected?.Tag == null) return -1;
            return Convert.ToInt32(selected.Tag);
        }

        private T FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        // ==================== NÚT THOÁT ====================
        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ==================== CHỈ NHẬP SỐ ====================
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        // ==================== CLASS GAMEROW ====================
        private class GameRow
        {
            public string TenGame { get; set; }
            public int MaLoaiGame { get; set; }
            public string NhaPhatHanh { get; set; }
            public int NamPhatHanh { get; set; }
            public int SoTuoi { get; set; }
            public decimal TriGia { get; set; }
        }









        // ==================== TÍNH TUỔI GAME REALTIME ====================
        private void TxtNamPhatHanh_TextChanged(object sender, TextChangedEventArgs e)
        {
            var txtNam = sender as TextBox;
            if (txtNam == null) return;

            var row = FindVisualParent<DataGridRow>(txtNam);
            if (row == null) return;

            CapNhatTuoiChoRow(row, txtNam.Text.Trim());
        }

        private void DpNgayNhap_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgGames == null) return;
            foreach (var item in dgGames.Items)
            {
                var row = dgGames.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row == null) continue;
                var data = item as RowData;
                if (data == null || data.STT == "+") continue;

                string namStr = LayTextTuCell(row, 5);
                CapNhatTuoiChoRow(row, namStr);
            }
        }

        private void CapNhatTuoiChoRow(DataGridRow row, string namPhatHanhStr)
        {
            var presenter = FindVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>(row);
            if (presenter == null) return;
            var cellTuoi = presenter.ItemContainerGenerator.ContainerFromIndex(6) as DataGridCell;
            if (cellTuoi == null) return;
            var tbTuoi = FindVisualChild<TextBlock>(cellTuoi);
            if (tbTuoi == null) return;

            int namNhap = (dpNgayNhap.SelectedDate ?? DateTime.Today).Year;

            if (int.TryParse(namPhatHanhStr, out int namPhatHanh) && namPhatHanh > 0)
            {
                int soTuoi = namNhap - namPhatHanh;
                tbTuoi.Text = soTuoi >= 0 ? soTuoi.ToString() : "";
                tbTuoi.Foreground = soTuoi > quiDinhTuoiGame
                    ? System.Windows.Media.Brushes.Red
                    : System.Windows.Media.Brushes.Black;
            }
            else
            {
                tbTuoi.Text = "";
                tbTuoi.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        // ==================== TÌM VISUAL PARENT ====================
        private T FindVisualParent<T>(System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T result) return result;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
}