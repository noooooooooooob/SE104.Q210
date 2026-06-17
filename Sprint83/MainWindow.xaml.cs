using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;
using Sprint83; // dùng chung DataProvider

namespace Sprint8_3
{
    public partial class MainWindow : Window
    {
        // ==================== MODEL ====================
        private class KhachHangNoItem
        {
            public int STT { get; set; }
            public string TenKhachHang { get; set; }
            public string TienNo { get; set; }  // hiển thị dạng có định dạng tiền
        }

        // ==================== CONSTRUCTOR ====================
        public MainWindow() { InitializeComponent(); }

        // ==================== LOAD ====================
        // Bước phụ 1, 2, 3: lấy ngày/tháng/năm hiện hành
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtNgay.Text = DateTime.Today.Day.ToString();
            txtThang.Text = DateTime.Today.Month.ToString();
            txtNam.Text = DateTime.Today.Year.ToString();
        }

        // ==================== NÚT LẬP BÁO CÁO ====================
        private void BtnLapBaoCao_Click(object sender, RoutedEventArgs e)
        {
            // --- Validate đầu vào ---
            if (!int.TryParse(txtNgay.Text.Trim(), out int ngay) || ngay < 1 || ngay > 31)
            {
                MessageBox.Show("Ngày thống kê không hợp lệ! (1 – 31)", "Lỗi");
                return;
            }
            if (!int.TryParse(txtThang.Text.Trim(), out int thang) || thang < 1 || thang > 12)
            {
                MessageBox.Show("Tháng thống kê không hợp lệ! (1 – 12)", "Lỗi");
                return;
            }
            if (!int.TryParse(txtNam.Text.Trim(), out int nam) || nam < 2000 || nam > 9999)
            {
                MessageBox.Show("Năm thống kê không hợp lệ!", "Lỗi");
                return;
            }

            // Kiểm tra ngày hợp lệ trong tháng
            try
            {
                var _ = new DateTime(nam, thang, ngay);
            }
            catch
            {
                MessageBox.Show("Ngày thống kê không hợp lệ!", "Lỗi");
                return;
            }

            try
            {
                // Bước 02, 03: truy vấn DB
                var danhSach = LayDuLieuBaoCao(out decimal tongTienNo);

                // Gán STT
                for (int i = 0; i < danhSach.Count; i++)
                    danhSach[i].STT = i + 1;

                // Hiển thị
                icKetQua.ItemsSource = null;
                icKetQua.ItemsSource = danhSach;
                txbTongTienNo.Text = tongTienNo.ToString("N0") + " đ";

                if (danhSach.Count == 0)
                    MessageBox.Show(
                        "Không có khách hàng nào đang nợ tiền phạt.",
                        "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi truy vấn: " + ex.Message, "Lỗi");
            }
        }

        // ==================== TRUY VẤN DB ====================
        /*
         * Thuật toán (theo slide):
         * Bước 02: Đọc D2 (Danh sách Khách hàng) từ CSDL Khách hàng.
         * Bước 03: Tạo D3 (Danh sách Khách hàng đang nợ gồm:
         *          Mã khách hàng, Tiền nợ hiện tại) với điều kiện lọc:
         *          "Tiền nợ hiện tại" của khách hàng đó > 0.
         * Bước 04: Kết thúc.
         */
        private List<KhachHangNoItem> LayDuLieuBaoCao(out decimal tongTienNo)
        {
            var list = new List<KhachHangNoItem>();
            tongTienNo = 0;

            using (var conn = DataProvider.Instance.GetConnection())
            {
                conn.Open();

                var cmd = new MySqlCommand(
                    @"SELECT HoTen, TienNo
                      FROM KHACHHANG
                      WHERE TienNo > 0
                      ORDER BY TienNo DESC", conn);

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal tienNo = Convert.ToDecimal(r["TienNo"]);
                        tongTienNo += tienNo;

                        list.Add(new KhachHangNoItem
                        {
                            TenKhachHang = r["HoTen"].ToString(),
                            TienNo = tienNo.ToString("N0") + " đ"
                        });
                    }
                }
            }

            return list;
        }

        // ==================== THOÁT ====================
        private void BtnThoat_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}