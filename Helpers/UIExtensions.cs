using System;
using System.ComponentModel; // Cần cái này để dùng BindingList
using System.Threading.Tasks;
using System.Windows.Forms;
using TestChecker.Sync; // Gọi file "Người quản lý" bạn vừa tạo lúc nãy

namespace TestChecker.Helpers
{
    public static class UIExtensions
    {
        // Hàm 1: Giúp cập nhật giao diện an toàn (Cũ)
        public static void SafeInvoke(this Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new MethodInvoker(action));
            }
            else
            {
                action();
            }
        }

        // Hàm 2: "Câu lệnh thần thánh" - Đây chính là cái công tắc (Mới)
        // Sau này bạn chỉ cần gọi .SyncWithFirebase() là xong
        public static async Task SyncWithFirebase<T>(this BindingList<T> list, Control owner, string nodeName) where T : class, new()
        {
            // Tự động tạo ra người quản lý và bảo nó bắt đầu làm việc
            var coordinator = new FirebaseSync<T>(owner, list, nodeName);
            await coordinator.StartAsync();
        }
    }
}