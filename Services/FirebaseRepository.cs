using System;
using System.Collections.Generic; // Cần thêm để dùng List
using System.ComponentModel;
using System.Linq; // Cần thêm để dùng FirstOrDefault
using System.Threading.Tasks;
using System.Windows.Forms;
using TestChecker.Helpers;

namespace TestChecker.Services
{
    public class FirebaseRepository<T> where T : class, new()
    {
        private readonly string _nodeName;
        private readonly GlobalSyncService _globalService;

        public FirebaseRepository(string nodeName)
        {
            _nodeName = nodeName;
            _globalService = GlobalSyncService.Instance;
        }

        public async Task BindToGridAsync(Control owner, BindingList<T> dataSource)
        {
            // 1. Tải dữ liệu cũ
            var initialData = await _globalService.LoadInitialDataAsync<Dictionary<string, T>>(_nodeName);

            // Xóa sạch dữ liệu cũ trên lưới trước khi nạp (đề phòng nạp đè)
            owner.SafeInvoke(() => dataSource.Clear());

            if (initialData != null)
            {
                var listItems = new List<T>();
                foreach (var item in initialData)
                {
                    // Bỏ qua các dòng rác không có ID (nếu có)
                    if (item.Value == null) continue;

                    SetId(item.Value, item.Key);

                    // Kiểm tra kỹ hơn: Nếu Id null hoặc rỗng thì không thêm
                    string id = GetId(item.Value);
                    if (!string.IsNullOrEmpty(id) && id != "Batch")
                    {
                        listItems.Add(item.Value);
                    }
                }

                // Sắp xếp giảm dần (Mới nhất lên đầu)
                listItems = listItems.OrderByDescending(x => GetId(x)).ToList();
                dataSource.AddRange(listItems);
            }

            // 2. Đăng ký nhận tin Realtime
            _globalService.Subscribe(_nodeName,
                // A. Khi có Update/Add
                (e) => {
                    owner.SafeInvoke(() => {
                        var item = e.ToObject<T>();
                        SetId(item, e.Key);

                        // Lọc rác realtime
                        if (string.IsNullOrEmpty(GetId(item))) return;

                        var existing = dataSource.FirstOrDefault(x => GetId(x) == e.Key);
                        if (existing != null)
                        {
                            int index = dataSource.IndexOf(existing);
                            dataSource[index] = item;
                        }
                        else
                        {
                            dataSource.Insert(0, item); // Chèn lên đầu cho dễ thấy
                        }
                    });
                },
                // B. Khi có Delete
                (e) => {
                    owner.SafeInvoke(() => {
                        // --- ĐOẠN MỚI CẬP NHẬT Ở ĐÂY ---

                        // Trường hợp 1: Lệnh xóa toàn bộ bảng (Reset)
                        if (e.TargetId == "ALL")
                        {
                            dataSource.Clear(); // Xóa sạch lưới ngay lập tức!
                            return;
                        }

                        // Trường hợp 2: Lệnh xóa từng dòng lẻ
                        var item = dataSource.FirstOrDefault(x => GetId(x) == e.TargetId);
                        if (item != null) dataSource.Remove(item);
                    });
                }
            );
        }

        public async Task Add(T item)
        {
            await _globalService.AddDataAsync(_nodeName, item);
        }

        public async Task Update(T item)
        {
            string id = GetId(item);
            if (string.IsNullOrEmpty(id)) throw new Exception("Không tìm thấy ID!");
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

        // Helper Reflection để lấy/gán ID động
        private void SetId(T obj, string id) => typeof(T).GetProperty("Id")?.SetValue(obj, id);

        private string GetId(T obj)
        {
            return typeof(T).GetProperty("Id")?.GetValue(obj) as string;
        }
    }
}