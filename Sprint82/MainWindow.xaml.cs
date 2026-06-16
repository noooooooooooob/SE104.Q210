using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;
using Sprint82; // dùng chung DataProvider

namespace Sprint8_2
{
    public partial class MainWindow : Window
    {
        // ==================== MODEL ====================
        private class BaoCaoTraTreItem
        {
            public int STT { get; set; }
            public string TenGame { get; set; }
            public string NgayMuon { get; set; }
            public string NgayHetHanMuon { get; set; }
            public int SoNgayTraTre { get; set; }
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

            DateTime ngayThongKe;
            try { ngayThongKe = new DateTime(nam, thang, ngay); }
            catch { MessageBox.Show("Ngày thống kê không hợp lệ!", "Lỗi"); return; }

            try
            {
                var danhSach = LayDuLieuBaoCao(ngayThongKe);

                for (int i = 0; i < danhSach.Count; i++)
                    danhSach[i].STT = i + 1;

                icKetQua.ItemsSource = null;
                icKetQua.ItemsSource = danhSach;

                if (danhSach.Count == 0)
                    MessageBox.Show(
                        $"Không có game nào trả trễ tính đến ngày {ngay:D2}/{thang:D2}/{nam}.",
                        "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi truy vấn: " + ex.Message, "Lỗi");
            }
        }

        // ==================== TRUY VẤN DB ====================
        /*
         * Theo slide Sprint 8.2 - Bước 04:
         * Tạo D4 = lọc theo điều kiện:
         *   - MaPhieuMuon(D2) tương ứng MaPhieuMuon(D3)
         *   - NgayTra(D3) tồn tại (NOT NULL)           → đã trả rồi
         *   - NgayTra(D3) <= ngayThongKe               → trả trước/đúng ngày thống kê
         *   - NgayTra(D3) > NgayHetHanMuon(D2)         → trả trễ
         * Bước 06: SoNgayTraTre = DATEDIFF(NgayTra, NgayHetHanMuon)
         * Bước 07: JOIN CTPHIEUNHAP để lấy TenGame
         */
        private List<BaoCaoTraTreItem> LayDuLieuBaoCao(DateTime ngayThongKe)
        {
            var list = new List<BaoCaoTraTreItem>();

            using (var conn = DataProvider.Instance.GetConnection())
            {
                conn.Open();

                var cmd = new MySqlCommand(
                    @"SELECT
                        g.TenGame,
                        pm.NgayMuon,
                        pm.NgayHetHanMuon,
                        DATEDIFF(ct.NgayTra, pm.NgayHetHanMuon) AS SoNgayTraTre
                      FROM CTPHIEUMUON ct
                      JOIN PHIEUMUON   pm ON pm.MaPhieuMuon = ct.MaPhieuMuon
                      JOIN CTPHIEUNHAP g  ON g.MaCTPN       = ct.MaGame
                      WHERE ct.NgayTra IS NOT NULL
                        AND ct.NgayTra <= @ngayThongKe
                        AND ct.NgayTra >  pm.NgayHetHanMuon
                      ORDER BY SoNgayTraTre DESC", conn);

                cmd.Parameters.AddWithValue("@ngayThongKe", ngayThongKe.ToString("yyyy-MM-dd"));

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var ngayMuon = Convert.ToDateTime(r["NgayMuon"]);
                        var ngayHetHanMuon = Convert.ToDateTime(r["NgayHetHanMuon"]);
                        list.Add(new BaoCaoTraTreItem
                        {
                            TenGame = r["TenGame"].ToString(),
                            NgayMuon = ngayMuon.ToString("dd/MM/yyyy"),
                            NgayHetHanMuon = ngayHetHanMuon.ToString("dd/MM/yyyy"),
                            SoNgayTraTre = Convert.ToInt32(r["SoNgayTraTre"])
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