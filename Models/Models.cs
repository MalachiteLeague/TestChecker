using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestChecker.Models
{
    public class Product
    {
        // ID của sản phẩm (Key trong Firebase)
        public string Id { get; set; }

        public string Name { get; set; }
        public int Price { get; set; }
        public int Stock { get; set; }

        // Bạn có thể thêm các hàm xử lý nội bộ của class này ở đây
    }
}
