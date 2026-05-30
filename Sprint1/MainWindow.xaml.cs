using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

//192.168.192.247

namespace Sprint1
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Server=zephyr.proxy.rlwy.net;Port=58816;Database=railway;Uid=root;Pwd=gYvXqmHUpZYSPXozPtWHmcxTPfArOoyJ;";

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
        }

        // ========== LOAD DỮ LIỆU KHI MỞ MÀN HÌNH ==========
        private void LoadData()
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // 1. Phát sinh mã khách hàng
                var cmdMa = new MySqlCommand("SELECT IFNULL(MAX(MaKhachHang), 0) + 1 FROM KHACHHANG", conn);
                int maMoi = Convert.ToInt32(cmdMa.ExecuteScalar());
                txtMaKH.Text = "KH" + maMoi.ToString("D3");

                // 2. Load danh sách loại khách hàng vào ComboBox
                var cmdLoai = new MySqlCommand("SELECT MaLoaiKhachHang, TenLoaiKhachHang FROM LOAIKHACHHANG", conn);
                var reader = cmdLoai.ExecuteReader();
                cboLoaiKH.Items.Clear();
                while (reader.Read())
                {
                    cboLoaiKH.Items.Add(new ComboBoxItem
                    {
                        Content = reader["TenLoaiKhachHang"].ToString(),
                        Tag = reader["MaLoaiKhachHang"]
                    });
                }
                reader.Close();

                // 3. Load quy định tuổi và thời hạn thẻ từ THAMSO
                var cmdThamSo = new MySqlCommand("SELECT DoTuoiToiThieu, DoTuoiToiDa FROM THAMSO", conn);
                var readerTS = cmdThamSo.ExecuteReader();
                if (readerTS.Read())
                {
                    txtTuoiToiThieu.Text = readerTS["DoTuoiToiThieu"].ToString();
                    txtTuoiToiDa.Text = readerTS["DoTuoiToiDa"].ToString();
                }
                readerTS.Close();

                // 4. Set ngày lập thẻ mặc định là ngày hiện hành
                dpNgayLapThe.SelectedDate = DateTime.Today;
            }
        }

        // ========== TÍNH TUỔI KHI CHỌN NGÀY SINH ==========
        private void dpNgaySinh_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            TinhTuoi();
        }

        // ========== TÍNH NGÀY HẾT HẠN KHI CHỌN NGÀY LẬP THẺ ==========
        private void dpNgayLapThe_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpNgayLapThe.SelectedDate == null)
                return;

            var ngayLapThe = dpNgayLapThe.SelectedDate.Value;

            // Không nhỏ hơn ngày hiện hành
            if (ngayLapThe < DateTime.Today)
            {
                MessageBox.Show("Ngày lập thẻ không được nhỏ hơn ngày hiện hành!", "Lỗi");
                dpNgayLapThe.SelectedDate = null;
                return;
            }


            TinhTuoi();
            TinhNgayHetHan();
        }

        private void TinhTuoi()
        {
            if (dpNgaySinh.SelectedDate == null)
                return;

            var ngaySinh = dpNgaySinh.SelectedDate.Value;
            var ngayTinh = dpNgayLapThe.SelectedDate ?? DateTime.Today;

            int tuoi = ngayTinh.Year - ngaySinh.Year;
            if (ngayTinh < ngaySinh.AddYears(tuoi))
                tuoi--;

            tuoi = Math.Max(0, tuoi);

            txtTuoi.Text = tuoi.ToString();
            txtTuoi.Foreground = System.Windows.Media.Brushes.Black;
            txtTuoi.FontStyle = FontStyles.Normal;
        }

        private void TinhNgayHetHan()
        {
            if (dpNgayLapThe.SelectedDate == null) return;

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT ThoiHanThe FROM THAMSO", conn);
                int thoiHan = (int)cmd.ExecuteScalar();
                var ngayHetHan = dpNgayLapThe.SelectedDate.Value.AddMonths(thoiHan);
                txtNgayHetHan.Text = ngayHetHan.ToString("dd/MM/yyyy");
            }
        }

        // ========== NÚT TIẾP NHẬN ==========
        private void BtnTiepNhan_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra bỏ trống
            if (string.IsNullOrWhiteSpace(txtHoTen.Text))
            { MessageBox.Show("Vui lòng nhập Họ và tên!", "Lỗi"); return; }

            if (dpNgaySinh.SelectedDate == null)
            { MessageBox.Show("Vui lòng chọn Ngày sinh!", "Lỗi"); return; }

            if (dpNgayLapThe.SelectedDate == null)
            { MessageBox.Show("Vui lòng chọn Ngày lập thẻ!", "Lỗi"); return; }

            if (cboLoaiKH.SelectedItem == null)
            { MessageBox.Show("Vui lòng chọn Loại khách hàng!", "Lỗi"); return; }

            // Kiểm tra tuổi
            int tuoi = int.Parse(txtTuoi.Text);
            int tuoiToiThieu = int.Parse(txtTuoiToiThieu.Text);
            int tuoiToiDa = int.Parse(txtTuoiToiDa.Text);

            if (tuoi < tuoiToiThieu)
            { MessageBox.Show($"Tuổi khách hàng phải từ {tuoiToiThieu} tuổi trở lên!", "Lỗi"); return; }

            if (tuoi > tuoiToiDa)
            { MessageBox.Show($"Tuổi khách hàng không được quá {tuoiToiDa} tuổi!", "Lỗi"); return; }

            // Lấy mã loại KH
            var selectedItem = cboLoaiKH.SelectedItem as ComboBoxItem;
            int maLoaiKH = (int)selectedItem.Tag;

            // Tính ngày hết hạn
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                var cmdThoiHan = new MySqlCommand("SELECT ThoiHanThe FROM THAMSO", conn);
                int thoiHan = (int)cmdThoiHan.ExecuteScalar();
                DateTime ngayHetHan = dpNgayLapThe.SelectedDate.Value.AddMonths(thoiHan);

                // Lưu xuống DB
                var cmd = new MySqlCommand(@"
                    INSERT INTO KHACHHANG 
                    (HoTen, CCCD, Email, DiaChi, NgaySinh, SDT, NgayLapThe, NgayHetHan, MaLoaiKhachHang)
                    VALUES 
                    (@HoTen, @CCCD, @Email, @DiaChi, @NgaySinh, @SDT, @NgayLapThe, @NgayHetHan, @MaLoaiKhachHang)",
                    conn);

                cmd.Parameters.AddWithValue("@HoTen", txtHoTen.Text);
                cmd.Parameters.AddWithValue("@CCCD", txtCCCD.Text);
                cmd.Parameters.AddWithValue("@Email", txtEmail.Text);
                cmd.Parameters.AddWithValue("@DiaChi", txtDiaChi.Text);
                cmd.Parameters.AddWithValue("@NgaySinh", dpNgaySinh.SelectedDate.Value);
                cmd.Parameters.AddWithValue("@SDT", txtSDT.Text);
                cmd.Parameters.AddWithValue("@NgayLapThe", dpNgayLapThe.SelectedDate.Value);
                cmd.Parameters.AddWithValue("@NgayHetHan", ngayHetHan);
                cmd.Parameters.AddWithValue("@MaLoaiKhachHang", maLoaiKH);

                cmd.ExecuteNonQuery();

                MessageBox.Show("Tiếp nhận khách hàng thành công!", "Thông báo");

                // Reset form
                LoadData();
                txtHoTen.Text = "";
                txtCCCD.Text = "";
                txtSDT.Text = "";
                txtEmail.Text = "";
                txtDiaChi.Text = "";
                dpNgaySinh.SelectedDate = null;
                dpNgayLapThe.SelectedDate = null;
                cboLoaiKH.SelectedIndex = -1;
                txtTuoi.Text = "<Giá trị xử lý>";
                txtNgayHetHan.Text = "";
            }
        }

        // ========== NÚT THOÁT ==========
        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ========== CHỈ NHẬP SỐ ==========
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }
    }
}