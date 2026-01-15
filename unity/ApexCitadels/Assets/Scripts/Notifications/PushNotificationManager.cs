using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Messaging;
using Firebase.Functions;
using Firebase.Extensions;
using Newtonsoft.Json;

namespace ApexCitadels.Notifications
{
    /// <summary>
    /// Notification types matching backend
    /// </summary>
    public enum PushNotificationType
    {
        // Combat & Territory
        TerritoryAttacked,
        TerritoryLost,
        TerritoryDefended,
        AttackComplete,
        
        // Alliance
        AllianceWarStarted,
        AllianceWarEnded,
        AllianceInvite,
        AllianceMessage,
        AllianceMemberJoined,
        
        // Social
        FriendRequest,
        FriendNearby,
        FriendStartedPlaying,
        
        // Rewards & Progress
        DailyRewardAvailable,
        LevelUp,
        AchievementUnlocked,
        ChestReady,
        
        // Season & Events
        SeasonTierUnlocked,
        SeasonEndingSoon,
        WorldEventStarting,
        WorldEventEnding,
        
        // Economy
        ResourcesFull,
        BuildingComplete,
        VipRewardReady,
        SpecialOffer,
        
        // System
        Maintenance,
        NewUpdate,
        WelcomeBack
    }

    /// <summary>
    /// Notification preference categories
    /// </summary>
    [Serializable]
    public class NotificationPreferences
    {
        public bool Combat = true;
        public bool Alliance = true;
        public bool Social = true;
        public bool Rewards = true;
        public bool Events = true;
        public bool Marketing = false;
    }

    /// <summary>
    /// Quiet hours configuration
    /// </summary>
    [Serializable]
    public class QuietHoursSettings
    {
        public bool Enabled = false;
        public int StartHour = 22;
        public int EndHour = 8;
        public string Timezone = "UTC";
    }

    /// <summary>
    /// Full notification settings
    /// </summary>
    [Serializable]
    public class NotificationSettings
    {
        public bool Enabled = true;
        public bool HasToken = false;
        public NotificationPreferences Preferences = new NotificationPreferences();
        public QuietHoursSettings QuietHours = new QuietHoursSettings();
    }

    /// <summary>
    /// Received notification data
    /// </summary>
    public class ReceivedNotification
    {
        public PushNotificationType Type;
        public string Title;
        public string Body;
        public Dictionary<string, string> Data;
        public DateTime ReceivedAt;
        public bool WasTapped;
    }

    /// <summary>
    /// Complete Push Notification Manager
    /// Handles Firebase Cloud Messaging integration with full platform support
    /// </summary>
    public class PushNotificationManager : MonoBehaviour
    {
        public static PushNotificationManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool autoRegisterOnStart = true;
        [SerializeField] private bool showInAppNotifications = true;

        [Header("Android Notification Channels")]
        [SerializeField] private string defaultChannelId = "default";
        [SerializeField] private string combatChannelId = "combat";
        [SerializeField] private string allianceChannelId = "alliance";
        [SerializeField] private string socialChannelId = "social";
        [SerializeField] private string rewardsChannelId = "rewards";
        [SerializeField] private string eventsChannelId = "events";

        // Events
        public event Action<string> OnTokenReceived;
        public event Action<ReceivedNotification> OnNotificationReceived;
        public event Action<ReceivedNotification> OnNotificationTapped;
        public event Action<NotificationSettings> OnSettingsUpdated;

        // State
        private FirebaseFunctions _functions;
        private string _currentToken;
        private NotificationSettings _settings;
        private Queue<ReceivedNotification> _notificationQueue = new Queue<ReceivedNotification>();
        private List<ReceivedNotification> _recentNotifications = new List<ReceivedNotification>();
        private bool _isInitialized;
        private bool _isPaused;

        public bool IsInitialized => _isInitialized;
        public string CurrentToken => _currentToken;
        public NotificationSettings Settings => _settings;
        public bool NotificationsEnabled => _settings?.Enabled ?? false;

        private const int MAX_RECENT_NOTIFICATIONS = 50;
        private const string PREFS_SETTINGS_KEY = "notification_settings";

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
            
            // Load cached settings
            LoadCachedSettings();
            
            // Create Android notification channels
            CreateNotificationChannels();

            if (autoRegisterOnStart)
            {
                Initialize();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            _isPaused = paused;
            
            if (!paused)
            {
                // App resumed - process any queued notifications
                ProcessNotificationQueue();
            }
        }

        /// <summary>
        /// Initialize push notifications
        /// </summary>
        public async void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                Log("Initializing Push Notifications...");

                // Subscribe to FCM events
                FirebaseMessaging.TokenReceived += OnTokenReceivedHandler;
                FirebaseMessaging.MessageReceived += OnMessageReceivedHandler;

                // Request permission (iOS)
                await RequestPermission();

                // Get current token
                var tokenTask = FirebaseMessaging.GetTokenAsync();
                _currentToken = await tokenTask;

                if (!string.IsNullOrEmpty(_currentToken))
                {
                    Log($"FCM Token: {_currentToken}");
                    await RegisterTokenWithServer(_currentToken);
                    OnTokenReceived?.Invoke(_currentToken);
                }

                _isInitialized = true;
                Log("Push Notifications initialized successfully");
            }
            catch (Exception e)
            {
                LogError($"Failed to initialize push notifications: {e.Message}");
            }
        }

        /// <summary>
        /// Request notification permission (required for iOS)
        /// </summary>
        private async Task RequestPermission()
        {
#if UNITY_IOS
            // Request permission via Firebase
            await FirebaseMessaging.RequestPermissionAsync();
            Log("iOS notification permission requested");
#else
            await Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Create Android notification channels
        /// </summary>
        private void CreateNotificationChannels()
        {
#if UNITY_ANDROID
            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
            {
                // Permission already granted
            }
            else
            {
                UnityEngine.Android.Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
            }

            // Create channels using Unity's mobile notifications package (if available)
            // Or using AndroidJavaObject for native channel creation
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                using (var notificationManager = context.Call<AndroidJavaObject>("getSystemService", "notification"))
                {
                    // Create channels
                    CreateChannel(notificationManager, defaultChannelId, "General", "General notifications", 3);
                    CreateChannel(notificationManager, combatChannelId, "Combat", "Battle and attack alerts", 4);
                    CreateChannel(notificationManager, allianceChannelId, "Alliance", "Alliance activity", 3);
                    CreateChannel(notificationManager, socialChannelId, "Social", "Friend activity", 2);
                    CreateChannel(notificationManager, rewardsChannelId, "Rewards", "Rewards and achievements", 3);
                    CreateChannel(notificationManager, eventsChannelId, "Events", "World events and seasons", 3);
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to create notification channels: {e.Message}");
            }
#endif
        }

#if UNITY_ANDROID
        private void CreateChannel(AndroidJavaObject manager, string id, string name, string description, int importance)
        {
            try
            {
                using (var channelClass = new AndroidJavaClass("android.app.NotificationChannel"))
                using (var channel = new AndroidJavaObject("android.app.NotificationChannel", id, name, importance))
                {
                    channel.Call("setDescription", description);
                    channel.Call("enableVibration", true);
                    channel.Call("enableLights", true);
                    manager.Call("createNotificationChannel", channel);
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to create channel {id}: {e.Message}");
            }
        }
#endif

        /// <summary>
        /// Handle token received from FCM
        /// </summary>
        private async void OnTokenReceivedHandler(object sender, TokenReceivedEventArgs args)
        {
            Log($"New FCM Token received: {args.Token}");
            
            string oldToken = _currentToken;
            _currentToken = args.Token;

            // Unregister old token if different
            if (!string.IsNullOrEmpty(oldToken) && oldToken != _currentToken)
            {
                await UnregisterTokenFromServer(oldToken);
            }

            // Register new token
            await RegisterTokenWithServer(_currentToken);
            
            OnTokenReceived?.Invoke(_currentToken);
        }

        /// <summary>
        /// Handle message received from FCM
        /// </summary>
        private void OnMessageReceivedHandler(object sender, MessageReceivedEventArgs args)
        {
            Log($"Message received from: {args.Message.From}");

            var notification = ParseNotification(args.Message);
            
            // Add to recent notifications
            _recentNotifications.Insert(0, notification);
            if (_recentNotifications.Count > MAX_RECENT_NOTIFICATIONS)
            {
                _recentNotifications.RemoveAt(_recentNotifications.Count - 1);
            }

            if (_isPaused || !Application.isFocused)
            {
                // App in background - queue for later
                _notificationQueue.Enqueue(notification);
            }
            else
            {
                // App in foreground - process immediately
                ProcessNotification(notification, false);
            }
        }

        /// <summary>
        /// Parse FCM message into notification object
        /// </summary>
        private ReceivedNotification ParseNotification(FirebaseMessage message)
        {
            var notification = new ReceivedNotification
            {
                Title = message.Notification?.Title ?? "Apex Citadels",
                Body = message.Notification?.Body ?? "",
                Data = new Dictionary<string, string>(),
                ReceivedAt = DateTime.Now,
                WasTapped = false
            };

            // Parse data payload
            if (message.Data != null)
            {
                foreach (var kvp in message.Data)
                {
                    notification.Data[kvp.Key] = kvp.Value;
                }

                // Parse notification type
                if (notification.Data.TryGetValue("type", out string typeStr))
                {
                    notification.Type = ParseNotificationType(typeStr);
                }
            }

            return notification;
        }

        /// <summary>
        /// Parse notification type from string
        /// </summary>
        private PushNotificationType ParseNotificationType(string typeStr)
        {
            return typeStr switch
            {
                "territory_attacked" => PushNotificationType.TerritoryAttacked,
                "territory_lost" => PushNotificationType.TerritoryLost,
                "territory_defended" => PushNotificationType.TerritoryDefended,
                "attack_complete" => PushNotificationType.AttackComplete,
                "alliance_war_started" => PushNotificationType.AllianceWarStarted,
                "alliance_war_ended" => PushNotificationType.AllianceWarEnded,
                "alliance_invite" => PushNotificationType.AllianceInvite,
                "alliance_message" => PushNotificationType.AllianceMessage,
                "alliance_member_joined" => PushNotificationType.AllianceMemberJoined,
                "friend_request" => PushNotificationType.FriendRequest,
                "friend_nearby" => PushNotificationType.FriendNearby,
                "friend_started_playing" => PushNotificationType.FriendStartedPlaying,
                "daily_reward_available" => PushNotificationType.DailyRewardAvailable,
                "level_up" => PushNotificationType.LevelUp,
                "achievement_unlocked" => PushNotificationType.AchievementUnlocked,
                "chest_ready" => PushNotificationType.ChestReady,
                "season_tier_unlocked" => PushNotificationType.SeasonTierUnlocked,
                "season_ending_soon" => PushNotificationType.SeasonEndingSoon,
                "world_event_starting" => PushNotificationType.WorldEventStarting,
                "world_event_ending" => PushNotificationType.WorldEventEnding,
                "resources_full" => PushNotificationType.ResourcesFull,
                "building_complete" => PushNotificationType.BuildingComplete,
                "vip_reward_ready" => PushNotificationType.VipRewardReady,
                "special_offer" => PushNotificationType.SpecialOffer,
                "maintenance" => PushNotificationType.Maintenance,
                "new_update" => PushNotificationType.NewUpdate,
                "welcome_back" => PushNotificationType.WelcomeBack,
                _ => PushNotificationType.DailyRewardAvailable
            };
        }

        /// <summary>
        /// Process notification queue when app resumes
        /// </summary>
        private void ProcessNotificationQueue()
        {
            while (_notificationQueue.Count > 0)
            {
                var notification = _notificationQueue.Dequeue();
                ProcessNotification(notification, true);
            }
        }

        /// <summary>
        /// Process a single notification
        /// </summary>
        private void ProcessNotification(ReceivedNotification notification, bool wasTapped)
        {
            notification.WasTapped = wasTapped;

            Log($"Processing notification: {notification.Type} - {notification.Title}");

            // Show in-app notification if enabled and not from tap
            if (showInAppNotifications && !wasTapped)
            {
                ShowInAppNotification(notification);
            }

            // Fire appropriate event
            if (wasTapped)
            {
                OnNotificationTapped?.Invoke(notification);
                HandleNotificationTap(notification);
            }
            else
            {
                OnNotificationReceived?.Invoke(notification);
            }

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent(
                wasTapped ? "notification_tapped" : "notification_received",
                new Dictionary<string, object>
                {
                    { "type", notification.Type.ToString() },
                    { "title", notification.Title }
                });
        }

        /// <summary>
        /// Handle notification tap - navigate to relevant screen
        /// </summary>
        private void HandleNotificationTap(ReceivedNotification notification)
        {
            Log($"Handling notification tap: {notification.Type}");

            switch (notification.Type)
            {
                case PushNotificationType.TerritoryAttacked:
                case PushNotificationType.TerritoryLost:
                case PushNotificationType.TerritoryDefended:
                case PushNotificationType.AttackComplete:
                    // Navigate to map/combat view
                    if (notification.Data.TryGetValue("territoryId", out string territoryId))
                    {
                        NavigateTo("Map", new Dictionary<string, string> { { "territoryId", territoryId } });
                    }
                    break;

                case PushNotificationType.AllianceWarStarted:
                case PushNotificationType.AllianceWarEnded:
                case PushNotificationType.AllianceMemberJoined:
                    // Navigate to alliance view
                    NavigateTo("Alliance", null);
                    break;

                case PushNotificationType.AllianceInvite:
                    // Navigate to alliance invites
                    NavigateTo("AllianceInvites", null);
                    break;

                case PushNotificationType.AllianceMessage:
                    // Navigate to alliance chat
                    if (notification.Data.TryGetValue("allianceId", out string allianceId))
                    {
                        NavigateTo("AllianceChat", new Dictionary<string, string> { { "allianceId", allianceId } });
                    }
                    break;

                case PushNotificationType.FriendRequest:
                    // Navigate to friend requests
                    NavigateTo("FriendRequests", null);
                    break;

                case PushNotificationType.FriendNearby:
                case PushNotificationType.FriendStartedPlaying:
                    // Navigate to friends list
                    NavigateTo("Friends", null);
                    break;

                case PushNotificationType.DailyRewardAvailable:
                    // Navigate to daily rewards
                    NavigateTo("DailyRewards", null);
                    break;

                case PushNotificationType.LevelUp:
                case PushNotificationType.AchievementUnlocked:
                    // Navigate to profile/achievements
                    NavigateTo("Profile", null);
                    break;

                case PushNotificationType.ChestReady:
                    // Navigate to chests
                    NavigateTo("Chests", null);
                    break;

                case PushNotificationType.SeasonTierUnlocked:
                case PushNotificationType.SeasonEndingSoon:
                    // Navigate to season pass
                    NavigateTo("SeasonPass", null);
                    break;

                case PushNotificationType.WorldEventStarting:
                case PushNotificationType.WorldEventEnding:
                    // Navigate to world events
                    if (notification.Data.TryGetValue("eventId", out string eventId))
                    {
                        NavigateTo("WorldEvents", new Dictionary<string, string> { { "eventId", eventId } });
                    }
                    break;

                case PushNotificationType.BuildingComplete:
                    // Navigate to building
                    NavigateTo("Building", null);
                    break;

                case PushNotificationType.VipRewardReady:
                case PushNotificationType.SpecialOffer:
                    // Navigate to store
                    NavigateTo("Store", null);
                    break;

                case PushNotificationType.NewUpdate:
                    // Open app store
                    OpenAppStore();
                    break;

                default:
                    // Default to home
                    NavigateTo("Home", null);
                    break;
            }
        }

        /// <summary>
        /// Navigate to a screen
        /// </summary>
        private void NavigateTo(string screen, Dictionary<string, string> parameters)
        {
            Log($"Navigating to: {screen}");
            
            // This should integrate with your navigation system
            // For example, using a NavigationManager or event system
            
            // Broadcast navigation event
            var navData = new Dictionary<string, object>
            {
                { "screen", screen },
                { "parameters", parameters }
            };
            
            // You could use a static event or message system here
            // NavigationManager.Instance?.NavigateTo(screen, parameters);
        }

        /// <summary>
        /// Show in-app notification banner
        /// </summary>
        private void ShowInAppNotification(ReceivedNotification notification)
        {
            // This should show a toast/banner notification in the UI
            // Integrate with your UI system
            
            Log($"Showing in-app notification: {notification.Title}");
            
            // Example: ToastManager.Instance?.ShowToast(notification.Title, notification.Body, GetIconForType(notification.Type));
        }

        /// <summary>
        /// Open app store for updates
        /// </summary>
        private void OpenAppStore()
        {
#if UNITY_IOS
            Application.OpenURL("itms-apps://apps.apple.com/app/id[YOUR_APP_ID]");
#elif UNITY_ANDROID
            Application.OpenURL("market://details?id=" + Application.identifier);
#endif
        }

        /// <summary>
        /// Register token with server
        /// </summary>
        private async Task RegisterTokenWithServer(string token)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("registerPushToken");
                var data = new Dictionary<string, object>
                {
                    { "token", token },
                    { "platform", GetPlatform() }
                };

                await callable.CallAsync(data);
                Log("Token registered with server");

                _settings.HasToken = true;
                SaveCachedSettings();
            }
            catch (Exception e)
            {
                LogError($"Failed to register token: {e.Message}");
            }
        }

        /// <summary>
        /// Unregister token from server
        /// </summary>
        private async Task UnregisterTokenFromServer(string token)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("unregisterPushToken");
                var data = new Dictionary<string, object> { { "token", token } };

                await callable.CallAsync(data);
                Log("Token unregistered from server");
            }
            catch (Exception e)
            {
                LogError($"Failed to unregister token: {e.Message}");
            }
        }

        /// <summary>
        /// Load notification settings from server
        /// </summary>
        public async void LoadSettings()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getNotificationSettings");
                var result = await callable.CallAsync(null);

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                _settings = new NotificationSettings
                {
                    Enabled = response.ContainsKey("enabled") && (bool)response["enabled"],
                    HasToken = response.ContainsKey("hasToken") && (bool)response["hasToken"]
                };

                if (response.ContainsKey("preferences"))
                {
                    var prefs = JsonConvert.DeserializeObject<NotificationPreferences>(
                        JsonConvert.SerializeObject(response["preferences"]));
                    _settings.Preferences = prefs;
                }

                if (response.ContainsKey("quietHours"))
                {
                    var quietHours = JsonConvert.DeserializeObject<QuietHoursSettings>(
                        JsonConvert.SerializeObject(response["quietHours"]));
                    _settings.QuietHours = quietHours;
                }

                SaveCachedSettings();
                OnSettingsUpdated?.Invoke(_settings);

                Log("Settings loaded from server");
            }
            catch (Exception e)
            {
                LogError($"Failed to load settings: {e.Message}");
            }
        }

        /// <summary>
        /// Update notification preferences
        /// </summary>
        public async Task UpdatePreferences(NotificationPreferences preferences)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("updateNotificationPreferences");
                var data = new Dictionary<string, object>
                {
                    { "preferences", preferences }
                };

                await callable.CallAsync(data);

                _settings.Preferences = preferences;
                SaveCachedSettings();
                OnSettingsUpdated?.Invoke(_settings);

                Log("Preferences updated");
            }
            catch (Exception e)
            {
                LogError($"Failed to update preferences: {e.Message}");
            }
        }

        /// <summary>
        /// Enable or disable notifications globally
        /// </summary>
        public async Task SetNotificationsEnabled(bool enabled)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("updateNotificationPreferences");
                var data = new Dictionary<string, object>
                {
                    { "enabled", enabled }
                };

                await callable.CallAsync(data);

                _settings.Enabled = enabled;
                SaveCachedSettings();
                OnSettingsUpdated?.Invoke(_settings);

                Log($"Notifications {(enabled ? "enabled" : "disabled")}");
            }
            catch (Exception e)
            {
                LogError($"Failed to update enabled state: {e.Message}");
            }
        }

        /// <summary>
        /// Update quiet hours settings
        /// </summary>
        public async Task UpdateQuietHours(QuietHoursSettings quietHours)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("updateNotificationPreferences");
                var data = new Dictionary<string, object>
                {
                    { "quietHours", quietHours }
                };

                await callable.CallAsync(data);

                _settings.QuietHours = quietHours;
                SaveCachedSettings();
                OnSettingsUpdated?.Invoke(_settings);

                Log("Quiet hours updated");
            }
            catch (Exception e)
            {
                LogError($"Failed to update quiet hours: {e.Message}");
            }
        }

        /// <summary>
        /// Subscribe to alliance notifications
        /// </summary>
        public async Task SubscribeToAlliance(string allianceId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("subscribeToAllianceTopic");
                var data = new Dictionary<string, object> { { "allianceId", allianceId } };

                await callable.CallAsync(data);

                // Also subscribe locally
                await FirebaseMessaging.SubscribeAsync($"alliance_{allianceId}");

                Log($"Subscribed to alliance {allianceId}");
            }
            catch (Exception e)
            {
                LogError($"Failed to subscribe to alliance: {e.Message}");
            }
        }

        /// <summary>
        /// Unsubscribe from alliance notifications
        /// </summary>
        public async Task UnsubscribeFromAlliance(string allianceId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("unsubscribeFromAllianceTopic");
                var data = new Dictionary<string, object> { { "allianceId", allianceId } };

                await callable.CallAsync(data);

                // Also unsubscribe locally
                await FirebaseMessaging.UnsubscribeAsync($"alliance_{allianceId}");

                Log($"Unsubscribed from alliance {allianceId}");
            }
            catch (Exception e)
            {
                LogError($"Failed to unsubscribe from alliance: {e.Message}");
            }
        }

        /// <summary>
        /// Get recent notifications
        /// </summary>
        public List<ReceivedNotification> GetRecentNotifications()
        {
            return new List<ReceivedNotification>(_recentNotifications);
        }

        /// <summary>
        /// Clear recent notifications
        /// </summary>
        public void ClearRecentNotifications()
        {
            _recentNotifications.Clear();
        }

        /// <summary>
        /// Check if a category is enabled
        /// </summary>
        public bool IsCategoryEnabled(string category)
        {
            if (_settings?.Preferences == null) return true;

            return category.ToLower() switch
            {
                "combat" => _settings.Preferences.Combat,
                "alliance" => _settings.Preferences.Alliance,
                "social" => _settings.Preferences.Social,
                "rewards" => _settings.Preferences.Rewards,
                "events" => _settings.Preferences.Events,
                "marketing" => _settings.Preferences.Marketing,
                _ => true
            };
        }

        /// <summary>
        /// Get current platform
        /// </summary>
        private string GetPlatform()
        {
#if UNITY_IOS
            return "ios";
#elif UNITY_ANDROID
            return "android";
#else
            return "unknown";
#endif
        }

        /// <summary>
        /// Save settings to local cache
        /// </summary>
        private void SaveCachedSettings()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_settings);
                PlayerPrefs.SetString(PREFS_SETTINGS_KEY, json);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                LogError($"Failed to save cached settings: {e.Message}");
            }
        }

        /// <summary>
        /// Load settings from local cache
        /// </summary>
        private void LoadCachedSettings()
        {
            try
            {
                if (PlayerPrefs.HasKey(PREFS_SETTINGS_KEY))
                {
                    string json = PlayerPrefs.GetString(PREFS_SETTINGS_KEY);
                    _settings = JsonConvert.DeserializeObject<NotificationSettings>(json);
                }
                else
                {
                    _settings = new NotificationSettings();
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to load cached settings: {e.Message}");
                _settings = new NotificationSettings();
            }
        }

        private void OnDestroy()
        {
            FirebaseMessaging.TokenReceived -= OnTokenReceivedHandler;
            FirebaseMessaging.MessageReceived -= OnMessageReceivedHandler;
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PushNotificationManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[PushNotificationManager] {message}");
        }
    }
}
