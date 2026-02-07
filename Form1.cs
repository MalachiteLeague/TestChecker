using System;
using System.ComponentModel; // Để dùng BindingList
using System.Windows.Forms;
using TestChecker.Helpers;   // Để dùng lệnh .SyncWithFirebase
using TestChecker.Models;
using TestChecker.Services;// Để dùng class Product

namespace TestChecker
{
    public partial class Form1 : Form
    {
        // 1. Tạo một danh sách sản phẩm (Ban đầu rỗng)
        // BindingList là loại danh sách đặc biệt, nó thay đổi thì Grid tự đổi theo
        BindingList<Product> danhSachSanPham = new BindingList<Product>();

        public Form1()
        {
            InitializeComponent();
            dataGridView1.DataSource = danhSachSanPham;

            // BẬT TỔNG ĐÀI LÊN (Chỉ gọi 1 lần duy nhất trong toàn bộ App)
            GlobalSyncService.Instance.Start();

            this.Load += async (s, e) =>
            {
                // Các lệnh này chỉ đăng ký vào tổng đài chứ không mở socket mới
                await danhSachSanPham.SyncWithFirebase(this, "products");

                // Sau này có thêm bảng khác thì cứ gọi tiếp, vẫn chung 1 đường dây
                // await customers.SyncWithFirebase(this, "customers");
            };
        }
    }
}