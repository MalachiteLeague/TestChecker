using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestChecker.Helpers
{
    public static class UIExtensions
    {
        // Hàm này giúp bạn viết code cập nhật UI an toàn từ bất kỳ đâu
        public static void SafeInvoke(this Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                // Nếu đang ở luồng khác, nhờ Form chính chạy hộ
                control.Invoke(new MethodInvoker(action));
            }
            else
            {
                // Nếu đang ở đúng luồng rồi thì chạy luôn
                action();
            }
        }
    }
}
