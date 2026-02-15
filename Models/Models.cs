using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestChecker.Models
{
    public interface IModel
    {
        string Id { get; set; }
    }
    public class Product : IModel
    {
        // ID của sản phẩm (Key trong Firebase)
        public string Id { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public int Stock { get; set; }

        // Bạn có thể thêm các hàm xử lý nội bộ của class này ở đây
    }
    // Class Vé Giao Việc
    public class TestJob : IModel
    {
        public string Id { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Content { get; set; }
        public string Status { get; set; }
        public string Result { get; set; }

    }
}
