using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestChecker.Config;

namespace TestChecker.Services
{
    public class GlobalSyncService
    {
        // Singleton: Đảm bảo toàn bộ app chỉ có đúng 1 instance này
        private static GlobalSyncService _instance;
        public static GlobalSyncService Instance => _instance ??= new GlobalSyncService();

        private FirebaseService _firebaseService;

        // Danh sách các bảng đang chờ tin (Dictionary: Tên bảng -> Hàm xử lý)
        private Dictionary<string, List<Action<FirebaseDataEventArgs>>> _subscribers
            = new Dictionary<string, List<Action<FirebaseDataEventArgs>>>();

        private Dictionary<string, List<Action<FirebaseDeleteEventArgs>>> _deleteSubscribers
            = new Dictionary<string, List<Action<FirebaseDeleteEventArgs>>>();

        private GlobalSyncService()
        {
            // Khởi tạo Service kết nối vào ROOT (gốc)
            _firebaseService = new FirebaseService(FirebaseConfig.BASE_URL, FirebaseConfig.API_SECRET);

            // Đăng ký nhận mọi tin tức
            _firebaseService.OnDataChanged += OnGlobalDataChanged;
            _firebaseService.OnItemDeleted += OnGlobalItemDeleted;
        }

        public void Start()
        {
            // Bắt đầu mở Socket lắng nghe toàn bộ Database
            _firebaseService.StartListening();
        }

        // --- ĐĂNG KÝ (CÁC BẢNG CON GỌI HÀM NÀY) ---
        public void Subscribe(string nodeName, Action<FirebaseDataEventArgs> onUpdate, Action<FirebaseDeleteEventArgs> onDelete)
        {
            // Đăng ký Update
            if (!_subscribers.ContainsKey(nodeName))
                _subscribers[nodeName] = new List<Action<FirebaseDataEventArgs>>();
            _subscribers[nodeName].Add(onUpdate);

            // Đăng ký Delete
            if (!_deleteSubscribers.ContainsKey(nodeName))
                _deleteSubscribers[nodeName] = new List<Action<FirebaseDeleteEventArgs>>();
            _deleteSubscribers[nodeName].Add(onDelete);
        }

        // --- PHÂN PHỐI TIN TỨC (DISPATCHER) ---
        private void OnGlobalDataChanged(object sender, FirebaseDataEventArgs e)
        {
            // e.RootNode chính là tên bảng (vd: "products", "customers")
            if (_subscribers.ContainsKey(e.RootNode))
            {
                // Gửi tin cho tất cả ai đang quan tâm đến bảng này
                foreach (var action in _subscribers[e.RootNode])
                {
                    action(e);
                }
            }
        }

        private void OnGlobalItemDeleted(object sender, FirebaseDeleteEventArgs e)
        {
            if (_deleteSubscribers.ContainsKey(e.RootNode))
            {
                foreach (var action in _deleteSubscribers[e.RootNode])
                {
                    action(e);
                }
            }
        }

        // Hàm hỗ trợ tải dữ liệu ban đầu (Load Snapshot)
        public async Task<T> LoadInitialDataAsync<T>(string path)
        {
            return await _firebaseService.GetDataAsync<T>(path);
        }
        // --- PHẦN GỬI DỮ LIỆU (MỚI THÊM) ---

        // 1. Gửi lệnh THÊM MỚI
        public async Task<string> AddDataAsync<T>(string nodeName, T data)
        {
            // Gọi thằng đệ tử _firebaseService làm việc
            return await _firebaseService.AddDataAsync(nodeName, data);
        }

        // 2. Gửi lệnh CẬP NHẬT
        public async Task UpdateDataAsync<T>(string path, T data)
        {
            await _firebaseService.UpdateDataAsync(path, data);
        }

        // 3. Gửi lệnh XÓA
        public async Task DeleteDataAsync(string path)
        {
            await _firebaseService.DeleteDataAsync(path);
        }
    }
}