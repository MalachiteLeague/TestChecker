using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestChecker.Config;

namespace TestChecker.Services
{
    public class GlobalSyncService
    {
        private static GlobalSyncService _instance;
        public static GlobalSyncService Instance => _instance ??= new GlobalSyncService();

        private FirebaseService _firebaseService;
        private Dictionary<string, List<Action<FirebaseDataEventArgs>>> _subscribers
            = new Dictionary<string, List<Action<FirebaseDataEventArgs>>>();

        private Dictionary<string, List<Action<FirebaseDeleteEventArgs>>> _deleteSubscribers
            = new Dictionary<string, List<Action<FirebaseDeleteEventArgs>>>();

        private GlobalSyncService()
        {
            _firebaseService = new FirebaseService(FirebaseConfig.BASE_URL, FirebaseConfig.API_SECRET);
            _firebaseService.OnDataChanged += OnGlobalDataChanged;
            _firebaseService.OnItemDeleted += OnGlobalItemDeleted;
        }

        public void Start() => _firebaseService.StartListening();

        public void Subscribe(string nodeName, Action<FirebaseDataEventArgs> onUpdate, Action<FirebaseDeleteEventArgs> onDelete)
        {
            if (!_subscribers.ContainsKey(nodeName)) _subscribers[nodeName] = new List<Action<FirebaseDataEventArgs>>();
            _subscribers[nodeName].Add(onUpdate);

            if (!_deleteSubscribers.ContainsKey(nodeName)) _deleteSubscribers[nodeName] = new List<Action<FirebaseDeleteEventArgs>>();

            // Chỉ thêm vào danh sách nếu hành động đó không bị Null
            if (onDelete != null)
            {
                _deleteSubscribers[nodeName].Add(onDelete);
            }
        }

        private void OnGlobalDataChanged(object sender, FirebaseDataEventArgs e)
        {
            if (_subscribers.ContainsKey(e.RootNode))
            {
                // Dùng ToList() để tránh lỗi "Collection was modified" nếu list bị đổi khi đang chạy
                var actions = new List<Action<FirebaseDataEventArgs>>(_subscribers[e.RootNode]);
                foreach (var action in actions) action?.Invoke(e);
            }
        }

        // --- HÀM VỪA SỬA ---
        private void OnGlobalItemDeleted(object sender, FirebaseDeleteEventArgs e)
        {
            if (_deleteSubscribers.ContainsKey(e.RootNode))
            {
                var actions = new List<Action<FirebaseDeleteEventArgs>>(_deleteSubscribers[e.RootNode]);
                foreach (var action in actions)
                {
                    // Thêm ?.Invoke để an toàn tuyệt đối
                    action?.Invoke(e);
                }
            }
        }

        public async Task<T> LoadInitialDataAsync<T>(string path) => await _firebaseService.GetDataAsync<T>(path);
        public async Task<string> AddDataAsync<T>(string nodeName, T data) => await _firebaseService.AddDataAsync(nodeName, data);
        public async Task UpdateDataAsync<T>(string path, T data) => await _firebaseService.UpdateDataAsync(path, data);
        public async Task DeleteDataAsync(string path) => await _firebaseService.DeleteDataAsync(path);
    }
}