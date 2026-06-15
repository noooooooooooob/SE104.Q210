using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using Sprint81; // dùng chung DataProvider

namespace Sprint8_1
{
    public partial class MainWindow : Window
    {
        // ==================== MODEL ====================
        private class ThongKeItem
        {
            public int STT { get; set; }
            public string TenLoaiGame { get; set; }
            public int SoLuotMuon { get; set; }
            public string TiLe { get; set; }  // hiển thị dạng "xx.xx%"
        }

        // ==================== CONSTRUCTOR ====================
        public MainWindow() { InitializeComponent(); }

        // ==================== LOAD ====================
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Bước phụ 1 & 2: lấy tháng/năm hiện hành
            txtThang.Text = DateTime.Today.Month.ToString();
            txtNam.Text = DateTime.Today.Year.ToString();
        }

        // ==================== NÚT LẬP BẢNG THỐNG KÊ ====================
        private void BtnLapBangThongKe_Click(object sender, RoutedEventArgs e)
        {
            // --- Kiểm tra đầu vào ---
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

            // Kiểm tra tháng/năm phải nhỏ hơn tháng hiện tại
            var thangHienTai = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var thangChon = new DateTime(nam, thang, 1);
            if (thangChon > thangHienTai)
            {
                MessageBox.Show(
                    "Tháng thống kê phải nhỏ hơn hoặc bằng tháng hiện tại!",
                    "Lỗi");
                return;
            }

            try
            {
                var danhSach = LayDuLieuThongKe(thang, nam);

                // Tổng số lượt
                int tongSoLuot = 0;
                foreach (var item in danhSach) tongSoLuot += item.SoLuotMuon;

                // Tính tỉ lệ
                for (int i = 0; i < danhSach.Count; i++)
                {
                    double tiLe = tongSoLuot > 0
                        ? (double)danhSach[i].SoLuotMuon / tongSoLuot * 100.0
                        : 0;
                    danhSach[i].TiLe = tiLe.ToString("F2") + "%";
                    danhSach[i].STT = i + 1;
                }

                // Hiển thị
                txbTongSoLuot.Text = tongSoLuot > 0 ? tongSoLuot.ToString() : "0";
                icKetQua.ItemsSource = null;
                icKetQua.ItemsSource = danhSach;

                if (danhSach.Count == 0)
                    MessageBox.Show(
                        $"Không có lượt mượn nào trong tháng {thang}/{nam}.",
                        "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi truy vấn: " + ex.Message, "Lỗi");
            }
        }

        // ==================== TRUY VẤN DB ====================
        // Bước 02-08: join PHIEUMUON + CTPHIEUMUON + CTPHIEUNHAP + LOAIGAME
        // lọc theo tháng/năm, GROUP BY loại game → đếm số lượt mượn
        private List<ThongKeItem> LayDuLieuThongKe(int thang, int nam)
        {
            var list = new List<ThongKeItem>();

            using (var conn = DataProvider.Instance.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand(
                    @"SELECT lg.TenLoaiGame,
                             COUNT(ct.MaCTPhieuMuon) AS SoLuot
                      FROM CTPHIEUMUON ct
                      JOIN PHIEUMUON pm  ON pm.MaPhieuMuon = ct.MaPhieuMuon
                      JOIN CTPHIEUNHAP g ON g.MaCTPN       = ct.MaGame
                      JOIN LOAIGAME lg   ON lg.MaLoaiGame  = g.MaLoaiGame
                      WHERE MONTH(pm.NgayMuon) = @thang
                        AND YEAR(pm.NgayMuon)  = @nam
                      GROUP BY lg.MaLoaiGame, lg.TenLoaiGame
                      ORDER BY SoLuot DESC", conn);

                cmd.Parameters.AddWithValue("@thang", thang);
                cmd.Parameters.AddWithValue("@nam", nam);

                var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new ThongKeItem
                    {
                        TenLoaiGame = r["TenLoaiGame"].ToString(),
                        SoLuotMuon = Convert.ToInt32(r["SoLuot"])
                    });
            }

            return list;
        }

        // ==================== THOÁT ====================
        private void BtnThoat_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}