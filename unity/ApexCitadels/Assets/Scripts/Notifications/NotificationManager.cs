using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Core;
using ApexCitadels.Player;
using ApexCitadels.Territory;
using Firebase.Firestore;

#if UNITY_ANDROID
using Firebase.Messaging;
#endif

namespace ApexCitadels.Notifications
{
    /// <summary>
    /// Notification types
    /// </summary>
    public enum NotificationType
    {
        System,
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
        /// Show a local notification (convenience method)
        /// </summary>
        public void ShowLocalNotification(string title, string message, string relatedId = "")
        {
            AddNotification(NotificationType.System, title, message, relatedId);
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
            // Note: iOS NotificationServices API was deprecated in Unity 2018+
            // Use Unity Mobile Notifications package for cross-platform notifications
            // For now, we just log that permissions would be requested
            Debug.Log("[NotificationManager] Push notifications permission requested");
        }

        private void SchedulePushNotification(GameNotification notification)
        {
            // This would integrate with Firebase Cloud Messaging or Unity Mobile Notifications
            // For local notifications, use Unity Mobile Notifications package
            Debug.Log($"[NotificationManager] Would schedule notification: {notification.Title}");

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
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // Get FCM token
                var tokenTask = FirebaseMessaging.GetTokenAsync();
                await tokenTask;

                if (tokenTask.IsFaulted)
                {
                    Debug.LogError($"[NotificationManager] Failed to get FCM token: {tokenTask.Exception}");
                    return;
                }

                string fcmToken = tokenTask.Result;
                Debug.Log($"[NotificationManager] FCM Token: {fcmToken.Substring(0, 20)}...");

                // Register token with backend
                var playerId = PlayerManager.Instance?.GetCurrentPlayerId();
                if (!string.IsNullOrEmpty(playerId))
                {
                    var db = FirebaseFirestore.DefaultInstance;
                    var docRef = db.Collection("fcm_tokens").Document(playerId);
                    
                    await docRef.SetAsync(new Dictionary<string, object>
                    {
                        { "playerId", playerId },
                        { "token", fcmToken },
                        { "platform", "android" },
                        { "createdAt", Timestamp.GetCurrentTimestamp() },
                        { "updatedAt", Timestamp.GetCurrentTimestamp() }
                    }, SetOptions.MergeAll);

                    Debug.Log("[NotificationManager] FCM token registered with backend");
                }

                // Subscribe to topics
                await FirebaseMessaging.SubscribeAsync("game_updates");
                Debug.Log("[NotificationManager] Subscribed to game_updates topic");

                // Listen for messages
                FirebaseMessaging.MessageReceived += OnFirebaseMessageReceived;
                FirebaseMessaging.TokenReceived += OnTokenReceived;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NotificationManager] FCM registration error: {ex.Message}");
            }
#else
            await Task.CompletedTask;
            Debug.Log("[NotificationManager] Push notifications only supported on Android");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void OnFirebaseMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Debug.Log($"[NotificationManager] FCM message received from: {e.Message.From}");

            // Extract notification data
            var data = e.Message.Data;
            
            string title = "Notification";
            string body = "";
            string type = "system";
            string relatedId = "";

            if (data.TryGetValue("title", out string titleValue))
                title = titleValue;
            if (data.TryGetValue("body", out string bodyValue))
                body = bodyValue;
            if (data.TryGetValue("type", out string typeValue))
                type = typeValue;
            if (data.TryGetValue("relatedId", out string relatedIdValue))
                relatedId = relatedIdValue;

            // Parse notification type
            NotificationType notificationType = NotificationType.System;
            if (Enum.TryParse(type, true, out NotificationType parsed))
            {
                notificationType = parsed;
            }

            // Add to in-game notifications (on main thread)
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                AddNotification(notificationType, title, body, relatedId);
            });
        }

        private async void OnTokenReceived(object sender, TokenReceivedEventArgs e)
        {
            Debug.Log($"[NotificationManager] New FCM token received");
            
            // Update token on backend
            var playerId = PlayerManager.Instance?.GetCurrentPlayerId();
            if (!string.IsNullOrEmpty(playerId))
            {
                try
                {
                    var db = FirebaseFirestore.DefaultInstance;
                    var docRef = db.Collection("fcm_tokens").Document(playerId);
                    
                    await docRef.UpdateAsync(new Dictionary<string, object>
                    {
                        { "token", e.Token },
                        { "updatedAt", Timestamp.GetCurrentTimestamp() }
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NotificationManager] Failed to update FCM token: {ex.Message}");
                }
            }
        }
#endif

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

                TerritoryManager.Instance.OnTerritoryUnderAttack += (territoryId) =>
                {
                    var territory = TerritoryManager.Instance.GetTerritory(territoryId);
                    AddNotification(
                        NotificationType.TerritoryAttacked,
                        "Under Attack!",
                        $"{territory?.Name ?? territoryId} is being attacked!",
                        territoryId
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

        [Serializable]
        private class NotificationListWrapper
        {
            public List<SerializableNotification> notifications;
        }

        [Serializable]
        private class SerializableNotification
        {
            public string id;
            public string type;
            public string title;
            public string message;
            public string timestamp;
            public bool isRead;
            public string relatedId;
        }

        private void LoadNotifications()
        {
            try
            {
                string json = PlayerPrefs.GetString("notifications", "");
                if (!string.IsNullOrEmpty(json))
                {
                    var wrapper = JsonUtility.FromJson<NotificationListWrapper>(json);
                    if (wrapper?.notifications != null)
                    {
                        _notifications.Clear();
                        foreach (var sn in wrapper.notifications)
                        {
                            var notification = new GameNotification();
                            notification.Id = sn.id;
                            notification.Title = sn.title;
                            notification.Message = sn.message;
                            notification.IsRead = sn.isRead;
                            notification.RelatedId = sn.relatedId;

                            if (Enum.TryParse(sn.type, out NotificationType type))
                            {
                                notification.Type = type;
                            }

                            if (DateTime.TryParse(sn.timestamp, out DateTime ts))
                            {
                                notification.Timestamp = ts;
                            }

                            _notifications.Add(notification);
                        }
                    }
                }
                Debug.Log($"[NotificationManager] Loaded {_notifications.Count} notifications");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NotificationManager] Failed to load notifications: {ex.Message}");
            }
        }

        private void SaveNotifications()
        {
            try
            {
                var wrapper = new NotificationListWrapper
                {
                    notifications = new List<SerializableNotification>()
                };

                foreach (var n in _notifications)
                {
                    wrapper.notifications.Add(new SerializableNotification
                    {
                        id = n.Id,
                        type = n.Type.ToString(),
                        title = n.Title,
                        message = n.Message,
                        timestamp = n.Timestamp.ToString("O"),
                        isRead = n.IsRead,
                        relatedId = n.RelatedId
                    });
                }

                string json = JsonUtility.ToJson(wrapper);
                PlayerPrefs.SetString("notifications", json);
                PlayerPrefs.Save();
                Debug.Log($"[NotificationManager] Saved {_notifications.Count} notifications");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NotificationManager] Failed to save notifications: {ex.Message}");
            }
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
