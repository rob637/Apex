using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ApexCitadels.Data
{
    /// <summary>
    /// Manages offline mode and data synchronization.
    /// Requires Firebase SDK for full functionality.
    /// </summary>
    public class OfflineModeManager : MonoBehaviour
    {
        public static OfflineModeManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableOfflineMode = true;
        [SerializeField] private float syncInterval = 60f;
        [SerializeField] private bool enableDebugLogs = false;

        public event Action<bool> OnConnectivityChanged;
        public event Action OnSyncStarted;
        public event Action OnSyncCompleted;
        public event Action<string> OnSyncFailed;

        private bool _isOnline = true;
        private bool _isSyncing;
        private Queue<object> _pendingOperations = new Queue<object>();

        public bool IsOnline => _isOnline;
        public bool IsOfflineModeEnabled => enableOfflineMode;
        public bool IsSyncing => _isSyncing;
        public int PendingOperationsCount => _pendingOperations.Count;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            Debug.LogWarning("[OfflineModeManager] Firebase SDK not installed. Running in stub mode.");
            CheckConnectivity();
        }

        public void CheckConnectivity()
        {
            bool wasOnline = _isOnline;
            _isOnline = Application.internetReachability != NetworkReachability.NotReachable;
            
            if (wasOnline != _isOnline)
            {
                OnConnectivityChanged?.Invoke(_isOnline);
            }
        }

        public async Task SyncPendingOperations()
        {
            if (_isSyncing || _pendingOperations.Count == 0) return;

            _isSyncing = true;
            OnSyncStarted?.Invoke();

            await Task.Delay(100); // Stub delay

            _isSyncing = false;
            OnSyncCompleted?.Invoke();
        }

        public void QueueOperation(object operation)
        {
            _pendingOperations.Enqueue(operation);
        }

        public void ClearPendingOperations()
        {
            _pendingOperations.Clear();
        }
    }
}
