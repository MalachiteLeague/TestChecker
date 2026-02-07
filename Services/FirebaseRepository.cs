using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestChecker.Helpers; // Để dùng SafeInvoke
using TestChecker.Sync;  // Để dùng FirebaseSync

namespace TestChecker.Services
{
    // Class này gói gọn mọi thao tác với 1 bảng cụ thể (VD: Bảng Product)
    public class FirebaseRepository<T> where T : class, new()
    {
        private readonly string _nodeName;
        private readonly GlobalSyncService _globalService;

        public FirebaseRepository(string nodeName)
        {
            _nodeName = nodeName;
            _globalService = GlobalSyncService.Instance; // Gọi tổng đài
        }

        // --- 1. HÀM LẮNG NGHE (Tự động đồng bộ về List) ---
        public async Task BindToGridAsync(Control owner, BindingList<T> dataSource)
        {
            // Tận dụng lại class FirebaseSync bạn đã có
            // Lưu ý: Sửa lại FirebaseSync một chút để nhận Repository hoặc dùng logic cũ
            // Ở đây mình gọi trực tiếp logic sync để code gọn trong 1 file

            // 1. Tải dữ liệu cũ
            var initialData = await _globalService.LoadInitialDataAsync<System.Collections.Generic.Dictionary<string, T>>(_nodeName);
            if (initialData != null)
            {
                foreach (var item in initialData)
                {
                    SetId(item.Value, item.Key);
                    dataSource.Add(item.Value);
                }
            }

            // 2. Đăng ký nhận tin từ Tổng đài
            _globalService.Subscribe(_nodeName,
                // Khi có Update/Add
                (e) => {
                    owner.SafeInvoke(() => {
                        var item = e.ToObject<T>();
                        SetId(item, e.Key);

                        // Logic tìm và sửa/thêm vào List
                        var existing = System.Linq.Enumerable.FirstOrDefault(dataSource.Cast<dynamic>(), x => x.Id == e.Key);
                        if (existing != null)
                        {
                            int index = dataSource.IndexOf(existing);
                            dataSource[index] = item;
                        }
                        else
                        {
                            dataSource.Add(item);
                        }
                    });
                },
                // Khi có Delete
                (e) => {
                    owner.SafeInvoke(() => {
                        var item = System.Linq.Enumerable.FirstOrDefault(dataSource.Cast<dynamic>(), x => x.Id == e.TargetId);
                        if (item != null) dataSource.Remove(item);
                    });
                }
            );
        }

        // --- 2. CÁC HÀM GỬI LỆNH (SẴN SÀNG ĐỂ DÙNG) ---

        public async Task Add(T item)
        {
            // Không cần truyền "nodeName" nữa vì đã khai báo ở đầu rồi
            await _globalService.AddDataAsync(_nodeName, item);
        }

        public async Task Update(T item)
        {
            string id = GetId(item);
            if (string.IsNullOrEmpty(id)) throw new Exception("Không tìm thấy ID để cập nhật!");

            await _globalService.UpdateDataAsync($"{_nodeName}/{id}", item);
        }

        public async Task Delete(T item)
        {
            string id = GetId(item);
            if (!string.IsNullOrEmpty(id))
            {
                await _globalService.DeleteDataAsync($"{_nodeName}/{id}");
            }
        }

        // --- Helper lấy ID động ---
        private void SetId(T obj, string id) => typeof(T).GetProperty("Id")?.SetValue(obj, id);
        private string GetId(T obj) => typeof(T).GetProperty("Id")?.GetValue(obj) as string;
    }
}