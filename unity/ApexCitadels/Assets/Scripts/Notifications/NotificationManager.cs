using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Player;
using ApexCitadels.Territory;

namespace ApexCitadels.Notifications
{
    /// <summary>
    /// Notification types
    /// </summary>
    public enum NotificationType
    {
        TerritoryAttacked,
        TerritoryLost,
        TerritoryCaptured,
        AllianceInvitation,
        AllianceMessage,
        WarStarted,
        WarEnded,
        ResourcesCollected,
        LevelUp,
        Achievement
    }

    /// <summary>
    /// In-game notification
    /// </summary>
    [Serializable]
    public class GameNotification
    {
        public string Id;
        public NotificationType Type;
        public string Title;
        public string Message;
        public DateTime Timestamp;
        public bool IsRead;
        public string RelatedId; // Territory ID, Alliance ID, etc.
        
        // Display
        public Color IconColor;
        public string IconName;

        public GameNotification() { }

        public GameNotification(NotificationType type, string title, string message, string relatedId = "")
        {
            Id = Guid.NewGuid().ToString();
            Type = type;
            Title = title;
            Message = message;
            Timestamp = DateTime.UtcNow;
            IsRead = false;
            RelatedId = relatedId;
            IconColor = GetColorForType(type);
            IconName = GetIconForType(type);
        }

        private static Color GetColorForType(NotificationType type)
        {
            return type switch
            {
                NotificationType.TerritoryAttacked => new Color(0.9f, 0.4f, 0.2f), // Orange
                NotificationType.TerritoryLost => new Color(0.8f, 0.2f, 0.2f), // Red
                NotificationType.TerritoryCaptured => new Color(0.2f, 0.8f, 0.2f), // Green
                NotificationType.AllianceInvitation => new Color(0.2f, 0.6f, 0.9f), // Blue
                NotificationType.AllianceMessage => new Color(0.5f, 0.5f, 0.9f), // Light blue
                NotificationType.WarStarted => new Color(0.8f, 0.2f, 0.5f), // Magenta
                NotificationType.WarEnded => new Color(0.5f, 0.3f, 0.7f), // Purple
                NotificationType.ResourcesCollected => new Color(0.9f, 0.7f, 0.2f), // Gold
                NotificationType.LevelUp => new Color(1f, 0.9f, 0.3f), // Yellow
                NotificationType.Achievement => new Color(0.9f, 0.9f, 0.9f), // Silver
                _ => Color.white
            };
        }

        private static string GetIconForType(NotificationType type)
        {
            return type switch
            {
                NotificationType.TerritoryAttacked => "⚔️",
                NotificationType.TerritoryLost => "💔",
                NotificationType.TerritoryCaptured => "🏆",
                NotificationType.AllianceInvitation => "✉️",
                NotificationType.AllianceMessage => "💬",
                NotificationType.WarStarted => "🔥",
                NotificationType.WarEnded => "🏳️",
                NotificationType.ResourcesCollected => "💰",
                NotificationType.LevelUp => "⬆️",
                NotificationType.Achievement => "🎖️",
                _ => "📢"
            };
        }
    }

    /// <summary>
    /// Manages in-game notifications and push notifications
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxNotifications = 100;
        [SerializeField] private float toastDisplayDuration = 5f;
        [SerializeField] private bool enablePushNotifications = true;

        [Header("UI References")]
        [SerializeField] private GameObject toastPrefab;
        [SerializeField] private Transform toastContainer;

        // Events
        public event Action<GameNotification> OnNotificationReceived;
        public event Action<int> OnUnreadCountChanged;

        // State
        private List<GameNotification> _notifications = new List<GameNotification>();
        private Queue<GameNotification> _toastQueue = new Queue<GameNotification>();
        private bool _isShowingToast;

        public List<GameNotification> AllNotifications => _notifications;
        public int UnreadCount => _notifications.FindAll(n => !n.IsRead).Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Subscribe to game events
            SubscribeToEvents();
            
            // Load notifications from storage
            LoadNotifications();

            // Request push notification permission
            if (enablePushNotifications)
            {
                RequestPushPermission();
            }
        }

        private void Update()
        {
            // Process toast queue
            if (!_isShowingToast && _toastQueue.Count > 0)
            {
                ShowToast(_toastQueue.Dequeue());
            }
        }

        #region Notification Creation

        /// <summary>
        /// Add a new notification
        /// </summary>
        public void AddNotification(NotificationType type, string title, string message, string relatedId = "")
        {
            var notification = new GameNotification(type, title, message, relatedId);
            _notifications.Insert(0, notification); // Add to front

            // Trim old notifications
            while (_notifications.Count > maxNotifications)
            {
                _notifications.RemoveAt(_notifications.Count - 1);
            }

            // Queue toast
            _toastQueue.Enqueue(notification);

            // Fire event
            OnNotificationReceived?.Invoke(notification);
            OnUnreadCountChanged?.Invoke(UnreadCount);

            // Schedule push notification if app is backgrounded
            if (!Application.isFocused)
            {
                SchedulePushNotification(notification);
            }

            Debug.Log($"[NotificationManager] New notification: {title}");
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        public void MarkAsRead(string notificationId)
        {
            var notification = _notifications.Find(n => n.Id == notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                OnUnreadCountChanged?.Invoke(UnreadCount);
            }
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        public void MarkAllAsRead()
        {
            foreach (var notification in _notifications)
            {
                notification.IsRead = true;
            }
            OnUnreadCountChanged?.Invoke(0);
        }

        /// <summary>
        /// Clear a notification
        /// </summary>
        public void ClearNotification(string notificationId)
        {
            _notifications.RemoveAll(n => n.Id == notificationId);
            OnUnreadCountChanged?.Invoke(UnreadCount);
        }

        /// <summary>
        /// Clear all notifications
        /// </summary>
        public void ClearAllNotifications()
        {
            _notifications.Clear();
            OnUnreadCountChanged?.Invoke(0);
        }

        #endregion

        #region Toast Display

        private async void ShowToast(GameNotification notification)
        {
            if (toastPrefab == null || toastContainer == null) return;

            _isShowingToast = true;

            var toast = Instantiate(toastPrefab, toastContainer);
            
            // Configure toast (would set text, icon, color via component)
            // toast.GetComponent<ToastUI>().Setup(notification);

            // Animate in
            toast.transform.localScale = Vector3.zero;
            float elapsed = 0;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                toast.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, elapsed / 0.3f);
                await Task.Yield();
            }

            // Display
            await Task.Delay((int)(toastDisplayDuration * 1000));

            // Animate out
            elapsed = 0;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                toast.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, elapsed / 0.3f);
                await Task.Yield();
            }

            Destroy(toast);
            _isShowingToast = false;
        }

        #endregion

        #region Push Notifications

        private void RequestPushPermission()
        {
#if UNITY_IOS
            UnityEngine.iOS.NotificationServices.RegisterForNotifications(
                UnityEngine.iOS.NotificationType.Alert |
                UnityEngine.iOS.NotificationType.Badge |
                UnityEngine.iOS.NotificationType.Sound
            );
#endif
            // Android doesn't require explicit permission request for notifications
            // but you'd configure Firebase Cloud Messaging here
            
            Debug.Log("[NotificationManager] Push notifications enabled");
        }

        private void SchedulePushNotification(GameNotification notification)
        {
            // This would integrate with Firebase Cloud Messaging
            // For local notifications:
            
#if UNITY_IOS && !UNITY_EDITOR
            var localNotification = new UnityEngine.iOS.LocalNotification
            {
                alertBody = notification.Message,
                alertAction = notification.Title,
                fireDate = System.DateTime.Now,
                soundName = UnityEngine.iOS.LocalNotification.defaultSoundName,
                applicationIconBadgeNumber = UnreadCount
            };
            UnityEngine.iOS.NotificationServices.ScheduleLocalNotification(localNotification);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            // Would use Unity Mobile Notifications package
            // var notification = new AndroidNotification
            // {
            //     Title = notification.Title,
            //     Text = notification.Message,
            //     SmallIcon = "icon_small",
            //     LargeIcon = "icon_large",
            //     FireTime = System.DateTime.Now
            // };
            // AndroidNotificationCenter.SendNotification(notification, "default_channel");
#endif
        }

        /// <summary>
        /// Register for push notifications with Firebase
        /// </summary>
        public async void RegisterForPushNotifications()
        {
            await Task.Delay(100);
            // TODO: Get FCM token and register with backend
            // Firebase.Messaging.FirebaseMessaging.GetTokenAsync().ContinueWith(task => {
            //     string token = task.Result;
            //     // Send token to your server
            // });
            
            Debug.Log("[NotificationManager] Registered for push notifications");
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            // Territory events
            if (TerritoryManager.Instance != null)
            {
                TerritoryManager.Instance.OnTerritoryClaimed += (territory) =>
                {
                    AddNotification(
                        NotificationType.TerritoryCaptured,
                        "Territory Claimed!",
                        $"You claimed {territory.Name}",
                        territory.Id
                    );
                };

                TerritoryManager.Instance.OnTerritoryLost += (territory) =>
                {
                    AddNotification(
                        NotificationType.TerritoryLost,
                        "Territory Lost!",
                        $"{territory.Name} was captured by an enemy!",
                        territory.Id
                    );
                };

                TerritoryManager.Instance.OnTerritoryUnderAttack += (territory) =>
                {
                    AddNotification(
                        NotificationType.TerritoryAttacked,
                        "Under Attack!",
                        $"{territory.Name} is being attacked!",
                        territory.Id
                    );
                };
            }

            // Player events
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.OnLevelUp += (newLevel) =>
                {
                    AddNotification(
                        NotificationType.LevelUp,
                        "Level Up!",
                        $"You reached level {newLevel}!"
                    );
                };
            }

            // Alliance events
            if (Alliance.AllianceManager.Instance != null)
            {
                Alliance.AllianceManager.Instance.OnInvitationReceived += (invitation) =>
                {
                    AddNotification(
                        NotificationType.AllianceInvitation,
                        "Alliance Invitation",
                        $"{invitation.InviterName} invited you to join {invitation.AllianceName}",
                        invitation.Id
                    );
                };

                Alliance.AllianceManager.Instance.OnWarStarted += (war) =>
                {
                    AddNotification(
                        NotificationType.WarStarted,
                        "War Declared!",
                        $"War between {war.AttackingAllianceName} and {war.DefendingAllianceName}!",
                        war.Id
                    );
                };
            }
        }

        #endregion

        #region Storage

        private void LoadNotifications()
        {
            // Load from PlayerPrefs or local storage
            string json = PlayerPrefs.GetString("notifications", "[]");
            // TODO: Deserialize
            Debug.Log("[NotificationManager] Notifications loaded");
        }

        private void SaveNotifications()
        {
            // Save to PlayerPrefs or local storage
            // string json = JsonUtility.ToJson(new NotificationList { notifications = _notifications });
            // PlayerPrefs.SetString("notifications", json);
            Debug.Log("[NotificationManager] Notifications saved");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveNotifications();
            }
        }

        #endregion
    }
}
