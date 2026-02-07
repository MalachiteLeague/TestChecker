using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestChecker.Helpers;
using TestChecker.Services;

namespace TestChecker.Sync
{
    public class FirebaseSync<T> where T : class, new()
    {
        private readonly BindingList<T> _dataSource;
        private readonly Control _owner;
        private readonly string _nodeName;

        public FirebaseSync(Control owner, BindingList<T> dataSource, string nodeName)
        {
            _owner = owner;
            _dataSource = dataSource;
            _nodeName = nodeName;

            // THAY ĐỔI LỚN NHẤT Ở ĐÂY:
            // Không new FirebaseService nữa, mà đăng ký với GlobalSyncService
            GlobalSyncService.Instance.Subscribe(_nodeName, HandleFirebaseDataChanged, HandleFirebaseItemDeleted);
        }

        public async Task StartAsync()
        {
            // 1. Tải dữ liệu cũ (Vẫn dùng HTTP GET bình thường cho nhẹ)
            var initialData = await GlobalSyncService.Instance.LoadInitialDataAsync<Dictionary<string, T>>(_nodeName);

            if (initialData != null)
            {
                foreach (var item in initialData)
                {
                    SetId(item.Value, item.Key);
                    _dataSource.Add(item.Value);
                }
            }

            // Lưu ý: Không gọi StartListening ở đây nữa.
            // Socket sẽ được bật 1 lần duy nhất ở Form Main hoặc Program.cs
        }

        // --- CÁC HÀM XỬ LÝ (GIỮ NGUYÊN) ---
        private void HandleFirebaseDataChanged(FirebaseDataEventArgs e)
        {
            _owner.SafeInvoke(() => {
                var item = e.ToObject<T>();
                SetId(item, e.Key);

                var existing = _dataSource.Cast<dynamic>().FirstOrDefault(x => x.Id == e.Key);
                if (existing != null)
                {
                    int index = _dataSource.IndexOf(existing);
                    _dataSource[index] = item;
                }
                else
                {
                    _dataSource.Add(item);
                }
            });
        }

        private void HandleFirebaseItemDeleted(FirebaseDeleteEventArgs e)
        {
            _owner.SafeInvoke(() => {
                var item = _dataSource.Cast<dynamic>().FirstOrDefault(x => x.Id == e.TargetId);
                if (item != null) _dataSource.Remove(item);
            });
        }

        private void SetId(T obj, string id)
        {
            var prop = typeof(T).GetProperty("Id");
            prop?.SetValue(obj, id);
        }
    }
}