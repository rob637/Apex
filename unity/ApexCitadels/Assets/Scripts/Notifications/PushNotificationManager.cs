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
    /// Notification preferences
    /// </summary>
    [Serializable]
    public class NotificationPreferences
    {
        public bool EnablePush = true;
        public bool EnableSound = true;
        public bool EnableVibration = true;
        public Dictionary<PushNotificationType, bool> TypePreferences = new Dictionary<PushNotificationType, bool>();
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

        private string _fcmToken;
        private NotificationPreferences _preferences = new NotificationPreferences();
        private bool _isInitialized;
        private bool _hasPermission;

        public bool IsInitialized => _isInitialized;
        public bool HasPermission => _hasPermission;
        public string FCMToken => _fcmToken;
        public NotificationPreferences Preferences => _preferences;

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
    }
}
