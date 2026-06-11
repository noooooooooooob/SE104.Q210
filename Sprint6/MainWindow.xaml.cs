using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

namespace Sprint6
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

        // ==================== CONSTRUCTOR ====================
        public MainWindow() { InitializeComponent(); }

        // ==================== LOAD ====================
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            NapDanhSachKhachHang();
            CapNhatMaPhieuThuPreview();
        }

        // ==================== NẠP KHÁCH HÀNG (CHỈ KH CÓ NỢ > 0) ====================
        private void NapDanhSachKhachHang()
        {
            try
            {
                cboKhachHang.Items.Clear();
                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        @"SELECT MaKhachHang, HoTen, TienNo
                          FROM KHACHHANG
                          WHERE TienNo > 0
                          ORDER BY HoTen", conn);
                    var r = cmd.ExecuteReader();
                    while (r.Read())
                        cboKhachHang.Items.Add(new KhachHangItem
                        {
                            MaKhachHang = Convert.ToInt32(r["MaKhachHang"]),
                            HoTen = r["HoTen"].ToString(),
                            TienNo = Convert.ToDecimal(r["TienNo"])
                        });
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi nạp khách hàng: " + ex.Message, "Lỗi"); }
        }

        // ==================== PHÁT SINH MÃ PHIẾU THU ====================
        private void CapNhatMaPhieuThuPreview()
        {
            try
            {
                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "SELECT AUTO_INCREMENT FROM information_schema.TABLES " +
                        "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PHIEUTHUTIEN'", conn);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        txbMaPhieuThu.Text = Convert.ToInt64(result).ToString();
                        txbMaPhieuThu.Foreground = Brushes.Gray;
                    }
                }
            }
            catch
            {
                txbMaPhieuThu.Text = "<Giá trị phát sinh>";
                txbMaPhieuThu.Foreground = Brushes.Gray;
            }
        }

        // ==================== CHỌN KHÁCH HÀNG → TỰ ĐIỀN TIỀN NỢ ====================
        private void CboKhachHang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboKhachHang.SelectedItem is KhachHangItem kh)
                txbTienNo.Text = kh.TienNo.ToString("N0") + " đ";
            else
                txbTienNo.Text = "";

            txtSoTienThu.Text = "";
            txbSoTienConLai.Text = "";
        }

        // ==================== NHẬP SỐ TIỀN THU → TỰ TÍNH CÒN LẠI ====================
        private void TxtSoTienThu_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(cboKhachHang.SelectedItem is KhachHangItem kh))
            { txbSoTienConLai.Text = ""; return; }

            string raw = txtSoTienThu.Text.Trim();
            if (string.IsNullOrEmpty(raw) ||
                !decimal.TryParse(raw, out decimal soTienThu) || soTienThu <= 0)
            { txbSoTienConLai.Text = ""; return; }

            txbSoTienConLai.Text = (kh.TienNo - soTienThu).ToString("N0") + " đ";
        }

        // ==================== TÌM KHÁCH HÀNG ====================
        private void BtnTimKhachHang_Click(object sender, RoutedEventArgs e)
        {
            //string tuKhoa = Microsoft.VisualBasic.Interaction.InputBox(
            //    "Nhập họ tên khách hàng cần tìm:", "Tìm khách hàng", "");

            //if (string.IsNullOrWhiteSpace(tuKhoa)) return;

            //for (int i = 0; i < cboKhachHang.Items.Count; i++)
            //{
            //    if (cboKhachHang.Items[i] is KhachHangItem kh &&
            //        kh.HoTen.IndexOf(tuKhoa, StringComparison.OrdinalIgnoreCase) >= 0)
            //    {
            //        cboKhachHang.SelectedIndex = i;
            //        return;
            //    }
            //}

            //MessageBox.Show($"Không tìm thấy khách hàng \"{tuKhoa}\"!", "Thông báo");
        }

        // ==================== LẬP PHIẾU THU TIỀN PHẠT ====================
        private void BtnLapPhieu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Bước 03: Kiểm tra KH
                if (!(cboKhachHang.SelectedItem is KhachHangItem kh))
                { MessageBox.Show("Vui lòng chọn khách hàng!", "Thông báo"); return; }

                // Bước 04: Kiểm tra tiền nợ > 0
                if (kh.TienNo <= 0)
                { MessageBox.Show("Khách hàng không có nợ!", "Thông báo"); return; }

                // Bước 05: Kiểm tra số tiền thu > 0
                if (!decimal.TryParse(txtSoTienThu.Text.Trim(), out decimal soTienThu) || soTienThu <= 0)
                { MessageBox.Show("Số tiền thu phải lớn hơn 0!", "Thông báo"); return; }

                // Bước 06: Kiểm tra số tiền thu <= tiền nợ
                if (soTienThu > kh.TienNo)
                {
                    MessageBox.Show(
                        $"Số tiền thu ({soTienThu:N0} đ) không được vượt quá tiền nợ ({kh.TienNo:N0} đ)!",
                        "Lỗi");
                    return;
                }

                // Bước 07: Tính số tiền còn lại
                decimal soTienConLai = kh.TienNo - soTienThu;

                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Bước 08: Lưu PHIEUTHUTIEN
                            var cmdPhieu = new MySqlCommand(
                                @"INSERT INTO PHIEUTHUTIEN (MaKhachHang, TienThu, SoTienConLai)
                                  VALUES (@maKH, @tienThu, @conLai);
                                  SELECT LAST_INSERT_ID();", conn, transaction);
                            cmdPhieu.Parameters.AddWithValue("@maKH", kh.MaKhachHang);
                            cmdPhieu.Parameters.AddWithValue("@tienThu", soTienThu);
                            cmdPhieu.Parameters.AddWithValue("@conLai", soTienConLai);
                            int maPhieuThu = Convert.ToInt32(cmdPhieu.ExecuteScalar());

                            // Bước 09: Cập nhật TienNo KH = SoTienConLai
                            var cmdKH = new MySqlCommand(
                                "UPDATE KHACHHANG SET TienNo = @conLai WHERE MaKhachHang = @maKH",
                                conn, transaction);
                            cmdKH.Parameters.AddWithValue("@conLai", soTienConLai);
                            cmdKH.Parameters.AddWithValue("@maKH", kh.MaKhachHang);
                            cmdKH.ExecuteNonQuery();

                            transaction.Commit();

                            txbMaPhieuThu.Text = maPhieuThu.ToString();
                            txbMaPhieuThu.Foreground = Brushes.Black;

                            MessageBox.Show(
                                $"Lập phiếu thu tiền phạt thành công!\n\n" +
                                $"Mã phiếu thu : {maPhieuThu}\n" +
                                $"Khách hàng   : {kh.HoTen}\n" +
                                $"Tiền nợ      : {kh.TienNo:N0} đ\n" +
                                $"Số tiền thu  : {soTienThu:N0} đ\n" +
                                $"Còn lại      : {soTienConLai:N0} đ",
                                "Thành công");

                            ResetForm();
                        }
                        catch { transaction.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi lập phiếu thu: " + ex.Message, "Lỗi"); }
        }

        // ==================== RESET FORM ====================
        private void ResetForm()
        {
            cboKhachHang.SelectedIndex = -1;
            txtSoTienThu.Text = "";
            txbTienNo.Text = "";
            txbSoTienConLai.Text = "";

            NapDanhSachKhachHang();
            CapNhatMaPhieuThuPreview();
        }

        // ==================== THOÁT ====================
        private void BtnThoat_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}