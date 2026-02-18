using System.Windows.Forms;

namespace TestChecker
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Gọi hàm khởi động nằm bên file Logic
            // Đây chính là "công tắc" kết nối duy nhất
            KhoiDongUngDung();
        }
    }
}