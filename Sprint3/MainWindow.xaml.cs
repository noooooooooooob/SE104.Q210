using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
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

namespace Sprint3
{
    public partial class MainWindow : Window
    {
        // ==================== MODEL ====================
        public class KetQuaRow
        {
            public int STT { get; set; }
            public string NgayNhap { get; set; }
            public string TenGame { get; set; }
            public string TenLoaiGame { get; set; }
            public string NhaPhatHanh { get; set; }
            public int NamPhatHanh { get; set; }
            public string TriGia { get; set; }
            public string TinhTrang { get; set; }
        }

        // ==================== CONSTRUCTOR ====================
        public MainWindow()
        {
            InitializeComponent();
        }

        // ==================== XỬ LÝ PHỤ 1: NẠP DANH SÁCH LOẠI GAME ====================
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            NapDanhSachLoaiGame();
        }

        private void NapDanhSachLoaiGame()
        {
            try
            {
                cboTenLoaiGame.Items.Clear();

                // Thêm item "Tất cả" mặc định
                cboTenLoaiGame.Items.Add(new ComboBoxItem
                {
                    Content = "Tất cả",
                    Tag = -1
                });

                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "SELECT MaLoaiGame, TenLoaiGame FROM LOAIGAME ORDER BY TenLoaiGame", conn);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        cboTenLoaiGame.Items.Add(new ComboBoxItem
                        {
                            Content = reader["TenLoaiGame"].ToString(),
                            Tag = reader["MaLoaiGame"]
                        });
                    }
                }

                cboTenLoaiGame.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi load loại game: " + ex.Message, "Lỗi");
            }
        }

        // ==================== XỬ LÝ CHÍNH: TRA CỨU GAME ====================
        private void BtnTraCuu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ---- Đọc giá trị từ form ----
                string maCTPN = txtMaCTPN.Text.Trim();
                DateTime? ngayNhap = dpNgayNhap.SelectedDate;
                string tenGame = txtTenGame.Text.Trim();
                string nhaPhatHanh = txtNhaPhatHanh.Text.Trim();
                string namTuStr = txtNamTu.Text.Trim();
                string namDenStr = txtNamDen.Text.Trim();
                string triGiaTuStr = txtTriGiaTu.Text.Trim();
                string triGiaDenStr = txtTriGiaDen.Text.Trim();
                string tongTuStr = txtTongTriGiaTu.Text.Trim();
                string tongDenStr = txtTongTriGiaDen.Text.Trim();

                // Lấy MaLoaiGame từ ComboBox
                int maLoaiGame = -1; // -1 = Tất cả
                if (cboTenLoaiGame.SelectedItem is ComboBoxItem selected && selected.Tag != null)
                {
                    int.TryParse(selected.Tag.ToString(), out maLoaiGame);
                }

                // ---- Xây dựng câu truy vấn động ----
                var sql = @"
                    SELECT
                        ct.MaCTPN,
                        pn.NgayNhap,
                        ct.TenGame,
                        lg.TenLoaiGame,
                        ct.NhaPhatHanh,
                        ct.NamPhatHanh,
                        ct.TriGia,
                        ct.TinhTrang
                    FROM CTPHIEUNHAP ct
                    INNER JOIN PHIEUNHAP pn ON ct.MaPhieuNhap = pn.MaPhieuNhap
                    INNER JOIN LOAIGAME  lg ON ct.MaLoaiGame  = lg.MaLoaiGame
                    WHERE 1=1";

                using (var conn = DataProvider.Instance.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand();
                    cmd.Connection = conn;

                    // -- Lọc theo Mã CT phiếu nhập (có chứa)
                    if (!string.IsNullOrEmpty(maCTPN))
                    {
                        // Hỗ trợ nhập số hoặc chuỗi "CTPN001"
                        if (int.TryParse(maCTPN, out int maCTPNInt))
                        {
                            sql += " AND ct.MaCTPN = @MaCTPN";
                            cmd.Parameters.AddWithValue("@MaCTPN", maCTPNInt);
                        }
                        else
                        {
                            // Nếu nhập dạng "CTPN001" thì lấy phần số
                            string soStr = maCTPN.Replace("CTPN", "").Replace("ctpn", "").Trim();
                            if (int.TryParse(soStr, out int maCTPNParsed))
                            {
                                sql += " AND ct.MaCTPN = @MaCTPN";
                                cmd.Parameters.AddWithValue("@MaCTPN", maCTPNParsed);
                            }
                        }
                    }

                    // -- Lọc theo Ngày nhập
                    if (ngayNhap.HasValue)
                    {
                        sql += " AND DATE(pn.NgayNhap) = @NgayNhap";
                        cmd.Parameters.AddWithValue("@NgayNhap", ngayNhap.Value.Date);
                    }

                    // -- Lọc theo Tên game (có chứa)
                    if (!string.IsNullOrEmpty(tenGame))
                    {
                        sql += " AND ct.TenGame LIKE @TenGame";
                        cmd.Parameters.AddWithValue("@TenGame", "%" + tenGame + "%");
                    }

                    // -- Lọc theo Tên loại game
                    if (maLoaiGame != -1)
                    {
                        sql += " AND ct.MaLoaiGame = @MaLoaiGame";
                        cmd.Parameters.AddWithValue("@MaLoaiGame", maLoaiGame);
                    }

                    // -- Lọc theo Nhà phát hành (có chứa)
                    if (!string.IsNullOrEmpty(nhaPhatHanh))
                    {
                        sql += " AND ct.NhaPhatHanh LIKE @NhaPhatHanh";
                        cmd.Parameters.AddWithValue("@NhaPhatHanh", "%" + nhaPhatHanh + "%");
                    }

                    // -- Lọc theo Năm phát hành từ
                    if (int.TryParse(namTuStr, out int namTu))
                    {
                        sql += " AND ct.NamPhatHanh >= @NamTu";
                        cmd.Parameters.AddWithValue("@NamTu", namTu);
                    }

                    // -- Lọc theo Năm phát hành đến
                    if (int.TryParse(namDenStr, out int namDen))
                    {
                        sql += " AND ct.NamPhatHanh <= @NamDen";
                        cmd.Parameters.AddWithValue("@NamDen", namDen);
                    }

                    // -- Lọc theo Trị giá từ
                    if (decimal.TryParse(triGiaTuStr, out decimal triGiaTu))
                    {
                        sql += " AND ct.TriGia >= @TriGiaTu";
                        cmd.Parameters.AddWithValue("@TriGiaTu", triGiaTu);
                    }

                    // -- Lọc theo Trị giá đến
                    if (decimal.TryParse(triGiaDenStr, out decimal triGiaDen))
                    {
                        sql += " AND ct.TriGia <= @TriGiaDen";
                        cmd.Parameters.AddWithValue("@TriGiaDen", triGiaDen);
                    }

                    // -- Lọc theo Tổng trị giá phiếu nhập từ
                    if (decimal.TryParse(tongTuStr, out decimal tongTu))
                    {
                        sql += " AND pn.TongTriGia >= @TongTu";
                        cmd.Parameters.AddWithValue("@TongTu", tongTu);
                    }

                    // -- Lọc theo Tổng trị giá phiếu nhập đến
                    if (decimal.TryParse(tongDenStr, out decimal tongDen))
                    {
                        sql += " AND pn.TongTriGia <= @TongDen";
                        cmd.Parameters.AddWithValue("@TongDen", tongDen);
                    }

                    sql += " ORDER BY pn.NgayNhap DESC, ct.MaCTPN ASC";
                    cmd.CommandText = sql;

                    // ---- Đọc kết quả ----
                    var results = new ObservableCollection<KetQuaRow>();
                    var reader = cmd.ExecuteReader();
                    int stt = 1;

                    while (reader.Read())
                    {
                        var row = new KetQuaRow
                        {
                            STT = stt++,
                            NgayNhap = Convert.ToDateTime(reader["NgayNhap"]).ToString("dd/MM/yyyy"),
                            TenGame = reader["TenGame"].ToString(),
                            TenLoaiGame = reader["TenLoaiGame"].ToString(),
                            NhaPhatHanh = reader["NhaPhatHanh"].ToString(),
                            NamPhatHanh = Convert.ToInt32(reader["NamPhatHanh"]),
                            TriGia = Convert.ToDecimal(reader["TriGia"]).ToString("N0") + " VNĐ",
                            TinhTrang = reader["TinhTrang"].ToString()
                        };
                        results.Add(row);
                    }

                    dgKetQua.ItemsSource = results;

                    if (results.Count == 0)
                        MessageBox.Show("Không tìm thấy game nào phù hợp!", "Thông báo");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tra cứu: " + ex.Message, "Lỗi");
            }
        }

        // ==================== XỬ LÝ PHỤ: THOÁT ====================
        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ==================== CHỈ NHẬP SỐ ====================
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }
    }
}