using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Firebase.Functions;
using Newtonsoft.Json;

namespace ApexCitadels.Data
{
    /// <summary>
    /// Network connection states
    /// </summary>
    public enum ConnectionState
    {
        Unknown,
        Offline,
        Online,
        Unstable
    }

    /// <summary>
    /// Sync status for data
    /// </summary>
    public enum SyncStatus
    {
        Synced,
        Pending,
        Failed,
        Conflict
    }

    /// <summary>
    /// Complete Offline Mode Manager
    /// Handles network detection, offline data sync, and conflict resolution
    /// </summary>
    public class OfflineModeManager : MonoBehaviour
    {
        public static OfflineModeManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private float connectivityCheckInterval = 5f;
        [SerializeField] private float syncRetryInterval = 30f;
        [SerializeField] private int maxRetryAttempts = 5;
        [SerializeField] private string connectivityCheckUrl = "https://www.google.com/generate_204";

        [Header("Offline Settings")]
        [SerializeField] private bool allowOfflinePlay = true;
        [SerializeField] private bool queueActionsWhenOffline = true;
        [SerializeField] private bool showOfflineIndicator = true;

        // Events
        public event Action<ConnectionState> OnConnectionStateChanged;
        public event Action OnGoingOnline;
        public event Action OnGoingOffline;
        public event Action<int> OnSyncStarted;
        public event Action<int, int> OnSyncCompleted;
        public event Action<string> OnSyncFailed;
        public event Action<OfflineAction> OnActionQueued;
        public event Action<OfflineAction> OnActionSynced;

        // State
        private FirebaseFunctions _functions;
        private ConnectionState _connectionState = ConnectionState.Unknown;
        private bool _isSyncing;
        private float _lastConnectivityCheck;
        private int _consecutiveFailures;
        private Queue<OfflineAction> _syncQueue = new Queue<OfflineAction>();

        public ConnectionState CurrentState => _connectionState;
        public bool IsOnline => _connectionState == ConnectionState.Online;
        public bool IsOffline => _connectionState == ConnectionState.Offline;
        public bool IsSyncing => _isSyncing;
        public int PendingActionCount => _syncQueue.Count + DataPersistenceManager.Instance?.GetOfflineQueue()?.Count ?? 0;

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
            _functions = FirebaseFunctions.DefaultInstance;

            // Initial connectivity check
            StartCoroutine(CheckConnectivity());

            // Start periodic connectivity checks
            InvokeRepeating(nameof(PeriodicConnectivityCheck), connectivityCheckInterval, connectivityCheckInterval);

            // Listen for application focus changes
            Application.focusChanged += OnApplicationFocusChanged;
        }

        private void OnDestroy()
        {
            Application.focusChanged -= OnApplicationFocusChanged;
        }

        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (hasFocus)
            {
                // Check connectivity when app regains focus
                StartCoroutine(CheckConnectivity());
            }
        }

        #region Connectivity Detection

        /// <summary>
        /// Periodic connectivity check
        /// </summary>
        private void PeriodicConnectivityCheck()
        {
            StartCoroutine(CheckConnectivity());
        }

        /// <summary>
        /// Check network connectivity
        /// </summary>
        private IEnumerator CheckConnectivity()
        {
            // First check Unity's network reachability
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                SetConnectionState(ConnectionState.Offline);
                yield break;
            }

            // Then do an actual HTTP request to verify
            using (var request = UnityWebRequest.Head(connectivityCheckUrl))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    _consecutiveFailures = 0;
                    SetConnectionState(ConnectionState.Online);
                }
                else
                {
                    _consecutiveFailures++;
                    
                    if (_consecutiveFailures >= 3)
                    {
                        SetConnectionState(ConnectionState.Offline);
                    }
                    else
                    {
                        SetConnectionState(ConnectionState.Unstable);
                    }
                }
            }

            _lastConnectivityCheck = Time.time;
        }

        /// <summary>
        /// Force connectivity check
        /// </summary>
        public void ForceConnectivityCheck()
        {
            StartCoroutine(CheckConnectivity());
        }

        /// <summary>
        /// Set connection state and fire events
        /// </summary>
        private void SetConnectionState(ConnectionState newState)
        {
            if (_connectionState == newState) return;

            var previousState = _connectionState;
            _connectionState = newState;

            Log($"Connection state changed: {previousState} -> {newState}");

            OnConnectionStateChanged?.Invoke(newState);

            if (newState == ConnectionState.Online && previousState != ConnectionState.Online)
            {
                OnGoingOnline?.Invoke();
                
                // Sync queued actions when coming online
                if (queueActionsWhenOffline)
                {
                    StartSync();
                }
            }
            else if (newState == ConnectionState.Offline && previousState == ConnectionState.Online)
            {
                OnGoingOffline?.Invoke();
            }
        }

        #endregion

        #region Offline Action Queue

        /// <summary>
        /// Queue an action for later execution
        /// </summary>
        public void QueueAction(string type, string endpoint, Dictionary<string, object> data)
        {
            var action = new OfflineAction
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                Endpoint = endpoint,
                Data = data,
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0
            };

            DataPersistenceManager.Instance?.QueueOfflineAction(action);
            
            OnActionQueued?.Invoke(action);
            
            Log($"Queued action: {type} -> {endpoint}");

            // If online, try to sync immediately
            if (IsOnline && !_isSyncing)
            {
                StartSync();
            }
        }

        /// <summary>
        /// Execute an action with offline fallback
        /// </summary>
        public async Task<T> ExecuteWithOfflineFallback<T>(
            string type,
            string endpoint,
            Dictionary<string, object> data,
            Func<Task<T>> onlineAction,
            Func<T> offlineFallback = null)
        {
            if (IsOnline)
            {
                try
                {
                    return await onlineAction();
                }
                catch (Exception e)
                {
                    Log($"Online action failed, queuing: {e.Message}");
                    
                    if (queueActionsWhenOffline)
                    {
                        QueueAction(type, endpoint, data);
                    }

                    if (offlineFallback != null)
                    {
                        return offlineFallback();
                    }

                    throw;
                }
            }
            else
            {
                if (queueActionsWhenOffline)
                {
                    QueueAction(type, endpoint, data);
                }

                if (offlineFallback != null)
                {
                    return offlineFallback();
                }

                throw new OfflineException("Device is offline and no fallback provided");
            }
        }

        /// <summary>
        /// Wrapper for common Firebase calls with offline support
        /// </summary>
        public async Task<Dictionary<string, object>> CallFunction(
            string functionName,
            Dictionary<string, object> data,
            bool queueIfOffline = true)
        {
            if (IsOnline)
            {
                try
                {
                    var callable = _functions.GetHttpsCallable(functionName);
                    var result = await callable.CallAsync(data);
                    return JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                }
                catch (Exception e)
                {
                    Log($"Function call failed: {functionName} - {e.Message}");
                    
                    if (queueIfOffline)
                    {
                        QueueAction("function_call", functionName, data);
                    }
                    
                    throw;
                }
            }
            else
            {
                if (queueIfOffline)
                {
                    QueueAction("function_call", functionName, data);
                }
                
                throw new OfflineException($"Cannot call {functionName} while offline");
            }
        }

        #endregion

        #region Sync Operations

        /// <summary>
        /// Start syncing queued actions
        /// </summary>
        public void StartSync()
        {
            if (_isSyncing || !IsOnline) return;

            var pendingActions = DataPersistenceManager.Instance?.GetOfflineQueue() ?? new List<OfflineAction>();
            if (pendingActions.Count == 0) return;

            StartCoroutine(SyncActions(pendingActions));
        }

        /// <summary>
        /// Sync all queued actions
        /// </summary>
        private IEnumerator SyncActions(List<OfflineAction> actions)
        {
            _isSyncing = true;
            
            Log($"Starting sync of {actions.Count} actions");
            OnSyncStarted?.Invoke(actions.Count);

            int successCount = 0;
            int failedCount = 0;

            foreach (var action in actions)
            {
                if (!IsOnline)
                {
                    Log("Lost connectivity during sync, stopping");
                    break;
                }

                var syncTask = SyncAction(action);
                yield return new WaitUntil(() => syncTask.IsCompleted);

                if (syncTask.Result)
                {
                    successCount++;
                    DataPersistenceManager.Instance?.RemoveFromOfflineQueue(action.Id);
                    OnActionSynced?.Invoke(action);
                }
                else
                {
                    failedCount++;
                    action.RetryCount++;
                    action.LastRetryAt = DateTime.UtcNow;

                    if (action.RetryCount >= maxRetryAttempts)
                    {
                        Log($"Action exceeded max retries, removing: {action.Id}");
                        DataPersistenceManager.Instance?.RemoveFromOfflineQueue(action.Id);
                    }
                }

                // Small delay between actions
                yield return new WaitForSeconds(0.5f);
            }

            _isSyncing = false;

            Log($"Sync completed: {successCount} success, {failedCount} failed");
            OnSyncCompleted?.Invoke(successCount, failedCount);

            // If there were failures, schedule retry
            if (failedCount > 0)
            {
                Invoke(nameof(RetrySyncAfterDelay), syncRetryInterval);
            }
        }

        /// <summary>
        /// Sync a single action
        /// </summary>
        private async Task<bool> SyncAction(OfflineAction action)
        {
            try
            {
                Log($"Syncing action: {action.Type} -> {action.Endpoint}");

                switch (action.Type)
                {
                    case "function_call":
                        var callable = _functions.GetHttpsCallable(action.Endpoint);
                        await callable.CallAsync(action.Data);
                        break;

                    case "update_profile":
                        await SyncPlayerProfile(action.Data);
                        break;

                    case "update_resources":
                        await SyncResources(action.Data);
                        break;

                    case "attack":
                        await SyncAttack(action.Data);
                        break;

                    case "collect_reward":
                        await SyncRewardCollection(action.Data);
                        break;

                    default:
                        // Generic function call
                        var genericCallable = _functions.GetHttpsCallable(action.Endpoint);
                        await genericCallable.CallAsync(action.Data);
                        break;
                }

                return true;
            }
            catch (Exception e)
            {
                LogError($"Failed to sync action {action.Id}: {e.Message}");
                OnSyncFailed?.Invoke(e.Message);
                return false;
            }
        }

        private void RetrySyncAfterDelay()
        {
            if (IsOnline && !_isSyncing)
            {
                StartSync();
            }
        }

        #endregion

        #region Specific Sync Operations

        /// <summary>
        /// Sync player profile updates
        /// </summary>
        private async Task SyncPlayerProfile(Dictionary<string, object> data)
        {
            var callable = _functions.GetHttpsCallable("updateProfile");
            await callable.CallAsync(data);
        }

        /// <summary>
        /// Sync resource changes
        /// </summary>
        private async Task SyncResources(Dictionary<string, object> data)
        {
            var callable = _functions.GetHttpsCallable("syncResources");
            await callable.CallAsync(data);
        }

        /// <summary>
        /// Sync attack action
        /// </summary>
        private async Task SyncAttack(Dictionary<string, object> data)
        {
            var callable = _functions.GetHttpsCallable("submitAttackResult");
            await callable.CallAsync(data);
        }

        /// <summary>
        /// Sync reward collection
        /// </summary>
        private async Task SyncRewardCollection(Dictionary<string, object> data)
        {
            var callable = _functions.GetHttpsCallable("claimReward");
            await callable.CallAsync(data);
        }

        #endregion

        #region Conflict Resolution

        /// <summary>
        /// Handle data conflicts between local and server
        /// </summary>
        public async Task<T> ResolveConflict<T>(
            string key,
            T localData,
            T serverData,
            ConflictResolutionStrategy strategy = ConflictResolutionStrategy.ServerWins)
        {
            switch (strategy)
            {
                case ConflictResolutionStrategy.ServerWins:
                    Log($"Conflict resolved for {key}: Server wins");
                    return serverData;

                case ConflictResolutionStrategy.LocalWins:
                    Log($"Conflict resolved for {key}: Local wins");
                    // Push local data to server
                    await PushLocalData(key, localData);
                    return localData;

                case ConflictResolutionStrategy.Merge:
                    Log($"Conflict resolved for {key}: Merging");
                    return MergeData(localData, serverData);

                case ConflictResolutionStrategy.AskUser:
                    Log($"Conflict for {key}: Asking user");
                    // This would trigger a UI dialog
                    return await AskUserForResolution(key, localData, serverData);

                default:
                    return serverData;
            }
        }

        private async Task PushLocalData<T>(string key, T data)
        {
            var callable = _functions.GetHttpsCallable("forceUpdateData");
            await callable.CallAsync(new Dictionary<string, object>
            {
                { "key", key },
                { "data", data }
            });
        }

        private T MergeData<T>(T local, T server)
        {
            // Simple merge - in production, implement proper merge logic
            // For now, prefer server data
            return server;
        }

        private async Task<T> AskUserForResolution<T>(string key, T local, T server)
        {
            // This would show a UI dialog and wait for user choice
            // For now, return server data
            await Task.Delay(0);
            return server;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Get sync status summary
        /// </summary>
        public SyncStatusSummary GetSyncStatus()
        {
            var pending = DataPersistenceManager.Instance?.GetOfflineQueue() ?? new List<OfflineAction>();
            
            return new SyncStatusSummary
            {
                ConnectionState = _connectionState,
                PendingActions = pending.Count,
                IsSyncing = _isSyncing,
                LastSyncAttempt = pending.Count > 0 ? pending[0].LastRetryAt : null,
                OldestPendingAction = pending.Count > 0 ? pending[0].CreatedAt : null
            };
        }

        /// <summary>
        /// Check if a specific feature is available offline
        /// </summary>
        public bool IsFeatureAvailableOffline(string feature)
        {
            // Define which features work offline
            var offlineFeatures = new HashSet<string>
            {
                "view_profile",
                "view_inventory",
                "view_map",
                "tutorial",
                "settings",
                "view_alliance",
                "view_achievements"
            };

            return offlineFeatures.Contains(feature);
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[OfflineMode] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[OfflineMode] {message}");
        }

        #endregion
    }

    /// <summary>
    /// Sync status summary
    /// </summary>
    public class SyncStatusSummary
    {
        public ConnectionState ConnectionState;
        public int PendingActions;
        public bool IsSyncing;
        public DateTime? LastSyncAttempt;
        public DateTime? OldestPendingAction;
    }

    /// <summary>
    /// Conflict resolution strategies
    /// </summary>
    public enum ConflictResolutionStrategy
    {
        ServerWins,
        LocalWins,
        Merge,
        AskUser
    }

    /// <summary>
    /// Exception for offline operations
    /// </summary>
    public class OfflineException : Exception
    {
        public OfflineException(string message) : base(message) { }
    }
}
