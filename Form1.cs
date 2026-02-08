using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml.Linq;
using TestChecker.Models;
using TestChecker.Services;

namespace TestChecker
{
    public partial class Form1 : Form
    {
        // Khai báo Repository chuyên quản lý Product
        private FirebaseRepository<Product> _productRepo;
        private BindingList<Product> _products = new BindingList<Product>();

        public Form1()
        {
            InitializeComponent();
            dataGridView1.DataSource = _products;

            // 1. Kích hoạt Tổng đài (chỉ 1 lần duy nhất cho cả app)
            GlobalSyncService.Instance.Start();

            // 2. Khởi tạo Repository cho bảng "products"
            _productRepo = new FirebaseRepository<Product>("products");

            // 3. Kích hoạt tự động đồng bộ
            this.Load += async (s, e) => await _productRepo.BindToGridAsync(this, _products);
        }

        // --- CÁC NÚT BẤM GIỜ CHỈ GỌI HÀM CÓ SẴN ---

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            var p = new Product { Name = "a", Price = 1000, Stock = 5 };

            // Gọi hàm Add có sẵn -> Xong!
            await _productRepo.Add(p);
        }

        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow?.DataBoundItem is Product selected)
            {
                selected.Price += 100;
                // Gọi hàm Update có sẵn -> Xong!
                await _productRepo.Update(selected);
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow?.DataBoundItem is Product selected)
            {
                // Gọi hàm Delete có sẵn -> Xong!
                await _productRepo.Delete(selected);
            }
        }
    }
}