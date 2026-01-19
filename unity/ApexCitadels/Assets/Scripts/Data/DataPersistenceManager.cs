using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using ApexCitadels.Core;

namespace ApexCitadels.Data
{
    /// <summary>
    /// Cache entry with metadata
    /// </summary>
    [Serializable]
    public class CacheEntry<T>
    {
        public T Data;
        public DateTime CachedAt;
        public DateTime? ExpiresAt;
        public string ETag;
        public int Version;

        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }

    /// <summary>
    /// Cache configuration
    /// </summary>
    [Serializable]
    public class CacheConfig
    {
        public TimeSpan DefaultExpiry = TimeSpan.FromHours(1);
        public TimeSpan PlayerDataExpiry = TimeSpan.FromMinutes(5);
        public TimeSpan StaticDataExpiry = TimeSpan.FromDays(1);
        public long MaxCacheSizeBytes = 50 * 1024 * 1024; // 50 MB
        public int MaxEntries = 1000;
    }

    /// <summary>
    /// Complete Data Persistence and Caching System
    /// Provides offline-first data access with automatic sync
    /// </summary>
    public class DataPersistenceManager : MonoBehaviour
    {
        public static DataPersistenceManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool encryptSensitiveData = true;
        [SerializeField] private float autoSaveIntervalSeconds = 30f;
        [SerializeField] private float cacheCleanupIntervalSeconds = 300f;

        [Header("Cache Settings")]
        [SerializeField] private float defaultExpiryHours = 1f;
        [SerializeField] private float playerDataExpiryMinutes = 5f;
        [SerializeField] private float staticDataExpiryDays = 1f;
        [SerializeField] private long maxCacheSizeMB = 50;

        // Events
        public event Action OnDataSaved;
        public event Action OnDataLoaded;
        public event Action<string> OnDataError;
        public event Action<float> OnCacheCleanup;

        // State
        private CacheConfig _config;
        private Dictionary<string, object> _memoryCache = new Dictionary<string, object>();
        private Dictionary<string, DateTime> _cacheTimestamps = new Dictionary<string, DateTime>();
        private HashSet<string> _dirtyKeys = new HashSet<string>();
        private bool _isInitialized;
        private string _persistentPath;
        private string _cachePath;

        // Encryption key (in production, use secure key management)
        private const string ENCRYPTION_KEY = "ApexCitadels2024!";

        public bool IsInitialized => _isInitialized;

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
                return;
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                // Save all dirty data when app goes to background
                SaveAllDirtyData();
            }
        }

        private void OnApplicationQuit()
        {
            SaveAllDirtyData();
        }

        /// <summary>
        /// Initialize the persistence system
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            Log("Initializing Data Persistence Manager");

            // Setup paths
            _persistentPath = Application.persistentDataPath;
            _cachePath = Path.Combine(_persistentPath, "cache");

            // Create directories
            if (!Directory.Exists(_cachePath))
            {
                Directory.CreateDirectory(_cachePath);
            }

            // Setup config
            _config = new CacheConfig
            {
                DefaultExpiry = TimeSpan.FromHours(defaultExpiryHours),
                PlayerDataExpiry = TimeSpan.FromMinutes(playerDataExpiryMinutes),
                StaticDataExpiry = TimeSpan.FromDays(staticDataExpiryDays),
                MaxCacheSizeBytes = maxCacheSizeMB * 1024 * 1024
            };

            // Start auto-save coroutine
            InvokeRepeating(nameof(AutoSave), autoSaveIntervalSeconds, autoSaveIntervalSeconds);

            // Start cache cleanup
            InvokeRepeating(nameof(CleanupCache), cacheCleanupIntervalSeconds, cacheCleanupIntervalSeconds);

            _isInitialized = true;
            OnDataLoaded?.Invoke();

            Log($"Initialized. Persistent path: {_persistentPath}");
        }

        #region Generic Cache Operations

        /// <summary>
        /// Store data in cache with default expiry
        /// </summary>
        public void Set<T>(string key, T data)
        {
            Set(key, data, _config.DefaultExpiry);
        }

        /// <summary>
        /// Store data in cache with custom expiry
        /// </summary>
        public void Set<T>(string key, T data, TimeSpan expiry)
        {
            var entry = new CacheEntry<T>
            {
                Data = data,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiry),
                Version = 1
            };

            _memoryCache[key] = entry;
            _cacheTimestamps[key] = DateTime.UtcNow;
            _dirtyKeys.Add(key);

            Log($"Cached: {key} (expires in {expiry.TotalMinutes:F0} minutes)");
        }

        /// <summary>
        /// Store data in cache that never expires
        /// </summary>
        public void SetPermanent<T>(string key, T data)
        {
            var entry = new CacheEntry<T>
            {
                Data = data,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = null,
                Version = 1
            };

            _memoryCache[key] = entry;
            _cacheTimestamps[key] = DateTime.UtcNow;
            _dirtyKeys.Add(key);

            Log($"Cached (permanent): {key}");
        }

        /// <summary>
        /// Get data from cache
        /// </summary>
        public T Get<T>(string key, T defaultValue = default)
        {
            // Try memory cache first
            if (_memoryCache.TryGetValue(key, out var cached))
            {
                var entry = cached as CacheEntry<T>;
                if (entry != null && !entry.IsExpired)
                {
                    return entry.Data;
                }
            }

            // Try disk cache
            var diskEntry = LoadFromDisk<T>(key);
            if (diskEntry != null && !diskEntry.IsExpired)
            {
                _memoryCache[key] = diskEntry;
                return diskEntry.Data;
            }

            return defaultValue;
        }

        /// <summary>
        /// Try to get data from cache
        /// </summary>
        public bool TryGet<T>(string key, out T value)
        {
            value = default;

            // Try memory cache first
            if (_memoryCache.TryGetValue(key, out var cached))
            {
                var entry = cached as CacheEntry<T>;
                if (entry != null && !entry.IsExpired)
                {
                    value = entry.Data;
                    return true;
                }
            }

            // Try disk cache
            var diskEntry = LoadFromDisk<T>(key);
            if (diskEntry != null && !diskEntry.IsExpired)
            {
                _memoryCache[key] = diskEntry;
                value = diskEntry.Data;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if key exists and is not expired
        /// </summary>
        public bool HasKey(string key)
        {
            if (_memoryCache.TryGetValue(key, out var cached))
            {
                var entry = cached as CacheEntry<object>;
                return entry != null && !entry.IsExpired;
            }

            return File.Exists(GetCachePath(key));
        }

        /// <summary>
        /// Remove data from cache
        /// </summary>
        public void Remove(string key)
        {
            _memoryCache.Remove(key);
            _cacheTimestamps.Remove(key);
            _dirtyKeys.Remove(key);

            var path = GetCachePath(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            Log($"Removed: {key}");
        }

        /// <summary>
        /// Clear all cached data
        /// </summary>
        public void ClearAll()
        {
            _memoryCache.Clear();
            _cacheTimestamps.Clear();
            _dirtyKeys.Clear();

            if (Directory.Exists(_cachePath))
            {
                var files = Directory.GetFiles(_cachePath);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }

            Log("All cache cleared");
        }

        #endregion

        #region Player Data Operations

        /// <summary>
        /// Save player profile data
        /// </summary>
        public void SavePlayerProfile(Player.PlayerProfile profile)
        {
            Set("player_profile", profile, _config.PlayerDataExpiry);
            SaveToDiskImmediate("player_profile", profile);
        }

        /// <summary>
        /// Load player profile data
        /// </summary>
        public Player.PlayerProfile LoadPlayerProfile()
        {
            return Get<Player.PlayerProfile>("player_profile");
        }

        /// <summary>
        /// Save player resources
        /// </summary>
        public void SavePlayerResources(Dictionary<string, int> resources)
        {
            Set("player_resources", resources, _config.PlayerDataExpiry);
        }

        /// <summary>
        /// Load player resources
        /// </summary>
        public Dictionary<string, int> LoadPlayerResources()
        {
            return Get<Dictionary<string, int>>("player_resources") ?? new Dictionary<string, int>();
        }

        /// <summary>
        /// Save player inventory
        /// </summary>
        public void SavePlayerInventory(List<object> inventory)
        {
            Set("player_inventory", inventory, _config.PlayerDataExpiry);
        }

        /// <summary>
        /// Load player inventory
        /// </summary>
        public List<object> LoadPlayerInventory()
        {
            return Get<List<object>>("player_inventory") ?? new List<object>();
        }

        #endregion

        #region Game State Operations

        /// <summary>
        /// Save game settings
        /// </summary>
        public void SaveSettings(Dictionary<string, object> settings)
        {
            SetPermanent("game_settings", settings);
            SaveToDiskImmediate("game_settings", settings);
        }

        /// <summary>
        /// Load game settings
        /// </summary>
        public Dictionary<string, object> LoadSettings()
        {
            return Get<Dictionary<string, object>>("game_settings") ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Save tutorial progress
        /// </summary>
        public void SaveTutorialProgress(Tutorial.TutorialProgress progress)
        {
            SetPermanent("tutorial_progress", progress);
            SaveToDiskImmediate("tutorial_progress", progress);
        }

        /// <summary>
        /// Load tutorial progress
        /// </summary>
        public Tutorial.TutorialProgress LoadTutorialProgress()
        {
            return Get<Tutorial.TutorialProgress>("tutorial_progress");
        }

        /// <summary>
        /// Save notification settings
        /// </summary>
        public void SaveNotificationSettings(Notifications.NotificationSettings settings)
        {
            SetPermanent("notification_settings", settings);
            SaveToDiskImmediate("notification_settings", settings);
        }

        /// <summary>
        /// Load notification settings
        /// </summary>
        public Notifications.NotificationSettings LoadNotificationSettings()
        {
            return Get<Notifications.NotificationSettings>("notification_settings");
        }

        #endregion

        #region Offline Queue Operations

        /// <summary>
        /// Queue an action for when online
        /// </summary>
        public void QueueOfflineAction(OfflineAction action)
        {
            var queue = Get<List<OfflineAction>>("offline_queue") ?? new List<OfflineAction>();
            queue.Add(action);
            SetPermanent("offline_queue", queue);
            SaveToDiskImmediate("offline_queue", queue);

            Log($"Queued offline action: {action.Type}");
        }

        /// <summary>
        /// Get pending offline actions
        /// </summary>
        public List<OfflineAction> GetOfflineQueue()
        {
            return Get<List<OfflineAction>>("offline_queue") ?? new List<OfflineAction>();
        }

        /// <summary>
        /// Clear processed offline actions
        /// </summary>
        public void ClearOfflineQueue()
        {
            SetPermanent("offline_queue", new List<OfflineAction>());
            SaveToDiskImmediate("offline_queue", new List<OfflineAction>());
        }

        /// <summary>
        /// Remove a specific action from queue
        /// </summary>
        public void RemoveFromOfflineQueue(string actionId)
        {
            var queue = GetOfflineQueue();
            queue.RemoveAll(a => a.Id == actionId);
            SetPermanent("offline_queue", queue);
        }

        #endregion

        #region Disk Operations

        /// <summary>
        /// Save data to disk immediately
        /// </summary>
        private void SaveToDiskImmediate<T>(string key, T data)
        {
            try
            {
                var entry = new CacheEntry<T>
                {
                    Data = data,
                    CachedAt = DateTime.UtcNow,
                    Version = 1
                };

                string json = JsonConvert.SerializeObject(entry);
                
                if (encryptSensitiveData && IsSensitiveKey(key))
                {
                    json = Encrypt(json);
                }

                string path = GetCachePath(key);
                File.WriteAllText(path, json);

                _dirtyKeys.Remove(key);
            }
            catch (Exception e)
            {
                LogError($"Failed to save {key} to disk: {e.Message}");
                OnDataError?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// Load data from disk
        /// </summary>
        private CacheEntry<T> LoadFromDisk<T>(string key)
        {
            try
            {
                string path = GetCachePath(key);
                if (!File.Exists(path)) return null;

                string json = File.ReadAllText(path);

                if (encryptSensitiveData && IsSensitiveKey(key))
                {
                    json = Decrypt(json);
                }

                return JsonConvert.DeserializeObject<CacheEntry<T>>(json);
            }
            catch (Exception e)
            {
                LogError($"Failed to load {key} from disk: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get cache file path for key
        /// </summary>
        private string GetCachePath(string key)
        {
            // Sanitize key for filesystem
            string safeKey = key.Replace("/", "_").Replace("\\", "_").Replace(":", "_");
            return Path.Combine(_cachePath, $"{safeKey}.cache");
        }

        /// <summary>
        /// Check if key contains sensitive data
        /// </summary>
        private bool IsSensitiveKey(string key)
        {
            return key.Contains("player") || 
                   key.Contains("auth") || 
                   key.Contains("token") ||
                   key.Contains("profile");
        }

        #endregion

        #region Auto-Save and Cleanup

        /// <summary>
        /// Auto-save dirty data
        /// </summary>
        private void AutoSave()
        {
            SaveAllDirtyData();
        }

        /// <summary>
        /// Save all data marked as dirty
        /// </summary>
        private void SaveAllDirtyData()
        {
            if (_dirtyKeys.Count == 0) return;

            Log($"Auto-saving {_dirtyKeys.Count} dirty entries");

            var keysToSave = new List<string>(_dirtyKeys);
            foreach (var key in keysToSave)
            {
                if (_memoryCache.TryGetValue(key, out var cached))
                {
                    try
                    {
                        string json = JsonConvert.SerializeObject(cached);
                        
                        if (encryptSensitiveData && IsSensitiveKey(key))
                        {
                            json = Encrypt(json);
                        }

                        string path = GetCachePath(key);
                        File.WriteAllText(path, json);

                        _dirtyKeys.Remove(key);
                    }
                    catch (Exception e)
                    {
                        LogError($"Failed to auto-save {key}: {e.Message}");
                    }
                }
            }

            OnDataSaved?.Invoke();
        }

        /// <summary>
        /// Cleanup expired cache entries
        /// </summary>
        private void CleanupCache()
        {
            Log("Running cache cleanup");

            int removedCount = 0;
            long totalSize = 0;

            // Clean memory cache
            var expiredKeys = new List<string>();
            foreach (var kvp in _memoryCache)
            {
                var entry = kvp.Value as CacheEntry<object>;
                if (entry != null && entry.IsExpired)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _memoryCache.Remove(key);
                removedCount++;
            }

            // Clean disk cache
            if (Directory.Exists(_cachePath))
            {
                var files = Directory.GetFiles(_cachePath);
                foreach (var file in files)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        totalSize += info.Length;

                        // Check if file is expired based on last write time
                        if (info.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-7))
                        {
                            File.Delete(file);
                            removedCount++;
                        }
                    }
                    catch { }
                }
            }

            // If still over size limit, remove oldest entries
            if (totalSize > _config.MaxCacheSizeBytes)
            {
                CleanupBySize();
            }

            float percentCleaned = removedCount > 0 ? (float)removedCount / (_memoryCache.Count + removedCount) * 100 : 0;
            OnCacheCleanup?.Invoke(percentCleaned);

            Log($"Cleanup complete. Removed {removedCount} entries. Total size: {totalSize / 1024}KB");
        }

        /// <summary>
        /// Remove oldest entries to stay under size limit
        /// </summary>
        private void CleanupBySize()
        {
            if (!Directory.Exists(_cachePath)) return;

            var files = new DirectoryInfo(_cachePath).GetFiles();
            Array.Sort(files, (a, b) => a.LastWriteTimeUtc.CompareTo(b.LastWriteTimeUtc));

            long totalSize = 0;
            foreach (var file in files)
            {
                totalSize += file.Length;
            }

            int index = 0;
            while (totalSize > _config.MaxCacheSizeBytes && index < files.Length)
            {
                totalSize -= files[index].Length;
                files[index].Delete();
                index++;
            }

            Log($"Size cleanup: removed {index} old files");
        }

        #endregion

        #region Encryption (Simple XOR - use proper encryption in production)

        private string Encrypt(string text)
        {
            var result = new char[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                result[i] = (char)(text[i] ^ ENCRYPTION_KEY[i % ENCRYPTION_KEY.Length]);
            }
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(new string(result)));
        }

        private string Decrypt(string encrypted)
        {
            var bytes = Convert.FromBase64String(encrypted);
            var text = System.Text.Encoding.UTF8.GetString(bytes);
            var result = new char[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                result[i] = (char)(text[i] ^ ENCRYPTION_KEY[i % ENCRYPTION_KEY.Length]);
            }
            return new string(result);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public CacheStats GetStats()
        {
            long diskSize = 0;
            int diskCount = 0;

            if (Directory.Exists(_cachePath))
            {
                var files = Directory.GetFiles(_cachePath);
                diskCount = files.Length;
                foreach (var file in files)
                {
                    diskSize += new FileInfo(file).Length;
                }
            }

            return new CacheStats
            {
                MemoryEntries = _memoryCache.Count,
                DiskEntries = diskCount,
                DiskSizeBytes = diskSize,
                DirtyEntries = _dirtyKeys.Count
            };
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                ApexLogger.LogVerbose(message, LogCategory.Firebase);
            }
        }

        private void LogError(string message)
        {
            ApexLogger.LogError(message, LogCategory.Firebase);
        }

        #endregion
    }

    /// <summary>
    /// Cache statistics
    /// </summary>
    public class CacheStats
    {
        public int MemoryEntries;
        public int DiskEntries;
        public long DiskSizeBytes;
        public int DirtyEntries;

        public string DiskSizeFormatted => $"{DiskSizeBytes / 1024f / 1024f:F2} MB";
    }

    /// <summary>
    /// Offline action to be synced when online
    /// </summary>
    [Serializable]
    public class OfflineAction
    {
        public string Id;
        public string Type;
        public string Endpoint;
        public Dictionary<string, object> Data;
        public DateTime CreatedAt;
        public int RetryCount;
        public DateTime? LastRetryAt;
    }
}
