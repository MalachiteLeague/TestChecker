using System;
using System.ComponentModel; // Cần cái này để dùng BindingList
using System.Threading.Tasks;
using System.Windows.Forms;

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
        // --- HÀM MỚI: Dạy BindingList cách thêm cả danh sách ---
        public static void AddRange<T>(this BindingList<T> bindingList, IEnumerable<T> collection)
        {
            // 1. Tắt tính năng vẽ (để Grid không bị nháy liên tục)
            bool oldRaiseEvents = bindingList.RaiseListChangedEvents;
            bindingList.RaiseListChangedEvents = false;

            try
            {
                // 2. Thêm từng cái (Bên dưới C# vẫn phải chạy loop, nhưng code mình nhìn gọn hơn)
                foreach (var item in collection)
                {
                    bindingList.Add(item);
                }
            }
            finally
            {
                // 3. Bật lại tính năng vẽ
                bindingList.RaiseListChangedEvents = oldRaiseEvents;

                // 4. Báo cho Grid vẽ lại 1 lần duy nhất (Reset)
                if (bindingList.RaiseListChangedEvents)
                {
                    bindingList.ResetBindings();
                }
            }
        }
    }

}