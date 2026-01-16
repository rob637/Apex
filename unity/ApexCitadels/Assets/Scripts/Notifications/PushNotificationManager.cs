using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.Notifications
{
    /// <summary>
    /// Push notification types
    /// </summary>
    public enum PushNotificationType
    {
        TerritoryAttacked,
        TerritoryLost,
        TerritoryDefended,
        AttackComplete,
        AllianceWarStarted,
        AllianceWarEnded,
        AllianceInvite,
        AllianceMessage,
        AllianceMemberJoined,
        FriendRequest,
        FriendNearby,
        FriendStartedPlaying,
        DailyRewardAvailable,
        LevelUp,
        AchievementUnlocked,
        ChestReady,
        SeasonTierUnlocked,
        SeasonEndingSoon,
        WorldEventStarting,
        WorldEventEnding,
        ResourcesFull,
        BuildingComplete,
        General
    }

    /// <summary>
    /// Notification settings for user preferences
    /// </summary>
    [Serializable]
    public class NotificationSettings
    {
        public bool EnablePush = true;
        public bool EnableSound = true;
        public bool EnableVibration = true;
        public bool EnableInApp = true;
        public Dictionary<PushNotificationType, bool> TypePreferences = new Dictionary<PushNotificationType, bool>();
        public int QuietHoursStart = 22; // 10 PM
        public int QuietHoursEnd = 8;    // 8 AM
        public bool EnableQuietHours = false;

        // Compatibility properties
        public bool Enabled { get => EnablePush; set => EnablePush = value; }
        public NotificationPreferences Preferences { get; set; } = new NotificationPreferences();
        public QuietHoursSettings QuietHours { get; set; } = new QuietHoursSettings();
        public bool HasToken => true; // Stub always has token
    }

    /// <summary>
    /// Quiet hours settings
    /// </summary>
    [Serializable]
    public class QuietHoursSettings
    {
        public bool Enabled;
        public int StartHour = 22;
        public int EndHour = 8;
    }

    /// <summary>
    /// Received notification data
    /// </summary>
    [Serializable]
    public class ReceivedNotification
    {
        public string Id;
        public string Title;
        public string Body;
        public PushNotificationType Type;
        public DateTime ReceivedAt;
        public Dictionary<string, string> Data;
    }

    /// <summary>
    /// Notification preferences (alias for backwards compatibility)
    /// </summary>
    [Serializable]
    public class NotificationPreferences
    {
        public bool EnablePush = true;
        public bool EnableSound = true;
        public bool EnableVibration = true;
        public Dictionary<PushNotificationType, bool> TypePreferences = new Dictionary<PushNotificationType, bool>();

        // Category preferences
        public bool Combat = true;
        public bool Alliance = true;
        public bool Social = true;
        public bool Rewards = true;
        public bool Events = true;
        public bool Marketing = false;
    }

    /// <summary>
    /// Push Notification Manager - requires Firebase Messaging for full functionality
    /// </summary>
    public class PushNotificationManager : MonoBehaviour
    {
        public static PushNotificationManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableNotifications = true;
        [SerializeField] private bool enableDebugLogs = false;

        public event Action<string> OnTokenReceived;
        public event Action<string, Dictionary<string, string>> OnNotificationReceived;
        public event Action<string, Dictionary<string, string>> OnNotificationOpened;
        public event Action OnPermissionGranted;
        public event Action OnPermissionDenied;
        public event Action<NotificationSettings> OnSettingsUpdated;

        private string _fcmToken;
        private NotificationPreferences _preferences = new NotificationPreferences();
        private NotificationSettings _settings = new NotificationSettings();
        private bool _isInitialized;
        private bool _hasPermission;

        public bool IsInitialized => _isInitialized;
        public bool HasPermission => _hasPermission;
        public string FCMToken => _fcmToken;
        public NotificationPreferences Preferences => _preferences;
        public NotificationSettings Settings => _settings;

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
            Debug.LogWarning("[PushNotificationManager] Firebase Messaging not installed. Running in stub mode.");
            _isInitialized = true;
            _hasPermission = true;
        }

        public void RequestPermission()
        {
            Debug.Log("[PushNotificationManager] Permission requested (stub mode - auto granted)");
            _hasPermission = true;
            OnPermissionGranted?.Invoke();
        }

        public void SubscribeToTopic(string topic)
        {
            Debug.Log($"[PushNotificationManager] Subscribed to topic: {topic} (stub mode)");
        }

        public void UnsubscribeFromTopic(string topic)
        {
            Debug.Log($"[PushNotificationManager] Unsubscribed from topic: {topic} (stub mode)");
        }

        public void SetNotificationPreference(PushNotificationType type, bool enabled)
        {
            _preferences.TypePreferences[type] = enabled;
        }

        public bool IsNotificationTypeEnabled(PushNotificationType type)
        {
            return _preferences.TypePreferences.TryGetValue(type, out bool enabled) ? enabled : true;
        }

        public void SavePreferences()
        {
            PlayerPrefs.SetString("notification_prefs", JsonUtility.ToJson(_preferences));
            PlayerPrefs.Save();
        }

        public void LoadPreferences()
        {
            string json = PlayerPrefs.GetString("notification_prefs", "");
            if (!string.IsNullOrEmpty(json))
            {
                _preferences = JsonUtility.FromJson<NotificationPreferences>(json);
            }
        }

        public void LoadSettings()
        {
            string json = PlayerPrefs.GetString("notification_settings", "");
            if (!string.IsNullOrEmpty(json))
            {
                _settings = JsonUtility.FromJson<NotificationSettings>(json);
            }
            OnSettingsUpdated?.Invoke(_settings);
        }

        public void SetNotificationsEnabled(bool enabled)
        {
            _settings.EnablePush = enabled;
            SaveSettings();
            OnSettingsUpdated?.Invoke(_settings);
        }

        public void UpdatePreferences(NotificationPreferences preferences)
        {
            _preferences = preferences;
            _settings.Preferences = preferences;
            SavePreferences();
            SaveSettings();
            OnSettingsUpdated?.Invoke(_settings);
        }

        public void UpdateQuietHours(QuietHoursSettings quietHours)
        {
            _settings.QuietHours = quietHours;
            SaveSettings();
            OnSettingsUpdated?.Invoke(_settings);
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetString("notification_settings", JsonUtility.ToJson(_settings));
            PlayerPrefs.Save();
        }
    }
}
