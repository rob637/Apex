using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// PC-specific Notification System with enhanced visuals.
    /// Features:
    /// - Toast notifications (top/bottom)
    /// - Notification center with history
    /// - Category filtering
    /// - Priority-based display
    /// - Sound/vibration integration
    /// - Action buttons on notifications
    /// </summary>
    public class PCNotificationSystem : MonoBehaviour
    {
        [Header("Toast Container")]
        [SerializeField] private RectTransform toastContainer;
        [SerializeField] private GameObject toastPrefab;
        [SerializeField] private int maxVisibleToasts = 5;
        [SerializeField] private float toastSpacing = 10f;
        
        [Header("Notification Center")]
        [SerializeField] private GameObject notificationCenterPanel;
        [SerializeField] private RectTransform notificationListContainer;
        [SerializeField] private GameObject notificationItemPrefab;
        [SerializeField] private Button notificationBellButton;
        [SerializeField] private TextMeshProUGUI unreadCountText;
        [SerializeField] private Image unreadBadge;
        
        [Header("Filter Buttons")]
        [SerializeField] private Button filterAllButton;
        [SerializeField] private Button filterCombatButton;
        [SerializeField] private Button filterSocialButton;
        [SerializeField] private Button filterResourceButton;
        [SerializeField] private Button filterSystemButton;
        
        [Header("Toast Settings")]
        [SerializeField] private float defaultToastDuration = 5f;
        [SerializeField] private float toastAnimationDuration = 0.3f;
        [SerializeField] private AnimationCurve toastAnimationCurve;
        
        [Header("Sound")]
        [SerializeField] private bool enableSounds = true;
        [SerializeField] private AudioClip defaultNotificationSound;
        [SerializeField] private AudioClip urgentNotificationSound;
        [SerializeField] private AudioClip socialNotificationSound;
        [SerializeField] private AudioSource audioSource;
        
        // Singleton
        private static PCNotificationSystem _instance;
        public static PCNotificationSystem Instance => _instance;
        
        // State
        private List<PCNotification> _notifications = new List<PCNotification>();
        private List<PCToastInstance> _activeToasts = new List<PCToastInstance>();
        private Queue<PCNotification> _toastQueue = new Queue<PCNotification>();
        private PCNotificationCategory _currentFilter = PCNotificationCategory.All;
        private int _unreadCount;
        private bool _isProcessingQueue;
        
        // Events
        public event Action<PCNotification> OnNotificationReceived;
        public event Action<PCNotification> OnNotificationClicked;
        public event Action<PCNotification> OnNotificationDismissed;
        public event Action<int> OnUnreadCountChanged;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            if (toastAnimationCurve == null || toastAnimationCurve.length == 0)
            {
                toastAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
            
            InitializeButtons();
        }
        
        private void InitializeButtons()
        {
            notificationBellButton?.onClick.AddListener(ToggleNotificationCenter);
            filterAllButton?.onClick.AddListener(() => SetFilter(PCNotificationCategory.All));
            filterCombatButton?.onClick.AddListener(() => SetFilter(PCNotificationCategory.Combat));
            filterSocialButton?.onClick.AddListener(() => SetFilter(PCNotificationCategory.Social));
            filterResourceButton?.onClick.AddListener(() => SetFilter(PCNotificationCategory.Resource));
            filterSystemButton?.onClick.AddListener(() => SetFilter(PCNotificationCategory.System));
        }
        
        private void Start()
        {
            // Demo notifications
            ShowNotification(new PCNotification
            {
                id = Guid.NewGuid().ToString(),
                title = "Territory Under Attack!",
                message = "Northern Fortress is being attacked by Shadow Council",
                category = PCNotificationCategory.Combat,
                priority = PCNotificationPriority.Urgent,
                timestamp = DateTime.Now,
                actionType = PCNotificationAction.OpenMap,
                actionData = "northern_fortress"
            });
            
            StartCoroutine(DemoNotifications());
        }
        
        private IEnumerator DemoNotifications()
        {
            yield return new WaitForSeconds(3f);
            
            ShowNotification(new PCNotification
            {
                id = Guid.NewGuid().ToString(),
                title = "Resources Collected",
                message = "You received 500 Gold from your territories",
                category = PCNotificationCategory.Resource,
                priority = PCNotificationPriority.Low,
                timestamp = DateTime.Now
            });
            
            yield return new WaitForSeconds(5f);
            
            ShowNotification(new PCNotification
            {
                id = Guid.NewGuid().ToString(),
                title = "Alliance Request",
                message = "DragonSlayer wants to join your alliance",
                category = PCNotificationCategory.Social,
                priority = PCNotificationPriority.Normal,
                timestamp = DateTime.Now,
                actionType = PCNotificationAction.OpenAlliance
            });
        }
        
        #region Public API
        
        /// <summary>
        /// Show a notification
        /// </summary>
        public void ShowNotification(PCNotification notification)
        {
            if (notification == null) return;
            
            // Assign ID if not present
            if (string.IsNullOrEmpty(notification.id))
            {
                notification.id = Guid.NewGuid().ToString();
            }
            
            // Add to history
            _notifications.Insert(0, notification);
            
            // Increment unread
            if (!notification.read)
            {
                _unreadCount++;
                UpdateUnreadDisplay();
            }
            
            // Queue toast
            _toastQueue.Enqueue(notification);
            if (!_isProcessingQueue)
            {
                StartCoroutine(ProcessToastQueue());
            }
            
            // Play sound
            PlayNotificationSound(notification);
            
            // Fire event
            OnNotificationReceived?.Invoke(notification);
            
            // Update notification center if open
            if (notificationCenterPanel != null && notificationCenterPanel.activeInHierarchy)
            {
                RefreshNotificationList();
            }
        }
        
        /// <summary>
        /// Show a simple toast message
        /// </summary>
        public void ShowToast(string message, PCNotificationCategory category = PCNotificationCategory.System)
        {
            ShowNotification(new PCNotification
            {
                title = "",
                message = message,
                category = category,
                priority = PCNotificationPriority.Low,
                timestamp = DateTime.Now
            });
        }
        
        /// <summary>
        /// Show an urgent notification
        /// </summary>
        public void ShowUrgent(string title, string message, PCNotificationAction action = PCNotificationAction.None, string actionData = null)
        {
            ShowNotification(new PCNotification
            {
                title = title,
                message = message,
                category = PCNotificationCategory.System,
                priority = PCNotificationPriority.Urgent,
                timestamp = DateTime.Now,
                actionType = action,
                actionData = actionData
            });
        }
        
        /// <summary>
        /// Mark notification as read
        /// </summary>
        public void MarkAsRead(string notificationId)
        {
            var notification = _notifications.Find(n => n.id == notificationId);
            if (notification != null && !notification.read)
            {
                notification.read = true;
                _unreadCount = Mathf.Max(0, _unreadCount - 1);
                UpdateUnreadDisplay();
            }
        }
        
        /// <summary>
        /// Mark all as read
        /// </summary>
        public void MarkAllAsRead()
        {
            foreach (var n in _notifications)
            {
                n.read = true;
            }
            _unreadCount = 0;
            UpdateUnreadDisplay();
        }
        
        /// <summary>
        /// Clear all notifications
        /// </summary>
        public void ClearAll()
        {
            _notifications.Clear();
            _unreadCount = 0;
            UpdateUnreadDisplay();
            RefreshNotificationList();
        }
        
        /// <summary>
        /// Dismiss a notification
        /// </summary>
        public void DismissNotification(string notificationId)
        {
            var notification = _notifications.Find(n => n.id == notificationId);
            if (notification != null)
            {
                _notifications.Remove(notification);
                OnNotificationDismissed?.Invoke(notification);
                RefreshNotificationList();
            }
        }
        
        /// <summary>
        /// Get notifications by category
        /// </summary>
        public List<PCNotification> GetNotifications(PCNotificationCategory category = PCNotificationCategory.All)
        {
            if (category == PCNotificationCategory.All)
            {
                return new List<PCNotification>(_notifications);
            }
            
            return _notifications.FindAll(n => n.category == category);
        }
        
        /// <summary>
        /// Get unread count
        /// </summary>
        public int GetUnreadCount()
        {
            return _unreadCount;
        }
        
        #endregion
        
        #region Toast Display
        
        private IEnumerator ProcessToastQueue()
        {
            _isProcessingQueue = true;
            
            while (_toastQueue.Count > 0)
            {
                // Wait if max toasts visible
                while (_activeToasts.Count >= maxVisibleToasts)
                {
                    yield return null;
                }
                
                var notification = _toastQueue.Dequeue();
                CreateToast(notification);
                
                yield return new WaitForSeconds(0.2f);
            }
            
            _isProcessingQueue = false;
        }
        
        private void CreateToast(PCNotification notification)
        {
            if (toastPrefab == null || toastContainer == null) return;
            
            var toastObj = Instantiate(toastPrefab, toastContainer);
            var toast = toastObj.GetComponent<PCNotificationToast>();
            
            if (toast == null)
            {
                toast = toastObj.AddComponent<PCNotificationToast>();
            }
            
            float duration = notification.priority == PCNotificationPriority.Urgent 
                ? defaultToastDuration * 1.5f 
                : defaultToastDuration;
            
            toast.Setup(notification, duration);
            toast.OnClicked += () => HandleToastClick(notification);
            toast.OnDismissed += () => HandleToastDismiss(toast);
            
            var instance = new PCToastInstance
            {
                notification = notification,
                toast = toast,
                gameObject = toastObj
            };
            
            _activeToasts.Add(instance);
            
            // Animate in
            StartCoroutine(AnimateToastIn(toast));
            
            // Reposition existing toasts
            RepositionToasts();
        }
        
        private IEnumerator AnimateToastIn(PCNotificationToast toast)
        {
            if (toast == null) yield break;
            
            var rect = toast.GetComponent<RectTransform>();
            var canvasGroup = toast.GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = toast.gameObject.AddComponent<CanvasGroup>();
            }
            
            Vector2 startPos = new Vector2(400, rect.anchoredPosition.y);
            Vector2 endPos = rect.anchoredPosition;
            
            rect.anchoredPosition = startPos;
            canvasGroup.alpha = 0;
            
            float elapsed = 0;
            while (elapsed < toastAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = toastAnimationCurve.Evaluate(elapsed / toastAnimationDuration);
                
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                canvasGroup.alpha = t;
                
                yield return null;
            }
            
            rect.anchoredPosition = endPos;
            canvasGroup.alpha = 1;
        }
        
        private void HandleToastClick(PCNotification notification)
        {
            MarkAsRead(notification.id);
            OnNotificationClicked?.Invoke(notification);
            
            // Handle action
            ExecuteNotificationAction(notification);
        }
        
        private void HandleToastDismiss(PCNotificationToast toast)
        {
            var instance = _activeToasts.Find(t => t.toast == toast);
            if (instance != null)
            {
                _activeToasts.Remove(instance);
                StartCoroutine(AnimateToastOut(toast.gameObject));
            }
            
            RepositionToasts();
        }
        
        private IEnumerator AnimateToastOut(GameObject toastObj)
        {
            if (toastObj == null) yield break;
            
            var rect = toastObj.GetComponent<RectTransform>();
            var canvasGroup = toastObj.GetComponent<CanvasGroup>();
            
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = new Vector2(400, rect.anchoredPosition.y);
            
            float elapsed = 0;
            while (elapsed < toastAnimationDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / (toastAnimationDuration * 0.5f);
                
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1 - t;
                }
                
                yield return null;
            }
            
            Destroy(toastObj);
        }
        
        private void RepositionToasts()
        {
            float yOffset = 0;
            
            for (int i = 0; i < _activeToasts.Count; i++)
            {
                var rect = _activeToasts[i].gameObject.GetComponent<RectTransform>();
                if (rect != null)
                {
                    float targetY = -yOffset;
                    StartCoroutine(AnimateToPosition(rect, new Vector2(0, targetY)));
                    yOffset += rect.sizeDelta.y + toastSpacing;
                }
            }
        }
        
        private IEnumerator AnimateToPosition(RectTransform rect, Vector2 targetPos)
        {
            Vector2 startPos = rect.anchoredPosition;
            
            float elapsed = 0;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsed / 0.2f);
                yield return null;
            }
            
            rect.anchoredPosition = targetPos;
        }
        
        #endregion
        
        #region Notification Center
        
        private void ToggleNotificationCenter()
        {
            if (notificationCenterPanel == null) return;
            
            bool show = !notificationCenterPanel.activeInHierarchy;
            notificationCenterPanel.SetActive(show);
            
            if (show)
            {
                RefreshNotificationList();
            }
        }
        
        private void SetFilter(PCNotificationCategory category)
        {
            _currentFilter = category;
            RefreshNotificationList();
            
            // Update filter button states
            UpdateFilterButtons();
        }
        
        private void UpdateFilterButtons()
        {
            // Would update button visuals to show selected filter
        }
        
        private void RefreshNotificationList()
        {
            if (notificationListContainer == null || notificationItemPrefab == null) return;
            
            // Clear existing
            foreach (Transform child in notificationListContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Get filtered notifications
            var filtered = GetNotifications(_currentFilter);
            
            // Create items
            foreach (var notification in filtered)
            {
                var itemObj = Instantiate(notificationItemPrefab, notificationListContainer);
                var item = itemObj.GetComponent<PCNotificationListItem>();
                
                if (item == null)
                {
                    item = itemObj.AddComponent<PCNotificationListItem>();
                }
                
                item.Setup(notification);
                item.OnClicked += () =>
                {
                    MarkAsRead(notification.id);
                    OnNotificationClicked?.Invoke(notification);
                    ExecuteNotificationAction(notification);
                };
                item.OnDismissed += () => DismissNotification(notification.id);
            }
        }
        
        private void UpdateUnreadDisplay()
        {
            if (unreadCountText != null)
            {
                unreadCountText.text = _unreadCount > 99 ? "99+" : _unreadCount.ToString();
            }
            
            if (unreadBadge != null)
            {
                unreadBadge.gameObject.SetActive(_unreadCount > 0);
            }
            
            OnUnreadCountChanged?.Invoke(_unreadCount);
        }
        
        #endregion
        
        #region Sound
        
        private void PlayNotificationSound(PCNotification notification)
        {
            if (!enableSounds || audioSource == null) return;
            
            AudioClip clip = notification.priority switch
            {
                PCNotificationPriority.Urgent => urgentNotificationSound,
                _ when notification.category == PCNotificationCategory.Social => socialNotificationSound,
                _ => defaultNotificationSound
            };
            
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        #endregion
        
        #region Actions
        
        private void ExecuteNotificationAction(PCNotification notification)
        {
            switch (notification.actionType)
            {
                case PCNotificationAction.OpenMap:
                    ApexLogger.Log($"Opening map at: {notification.actionData}", ApexLogger.LogCategory.UI);
                    break;
                    
                case PCNotificationAction.OpenAlliance:
                    ApexLogger.Log("Opening alliance panel", ApexLogger.LogCategory.UI);
                    break;
                    
                case PCNotificationAction.OpenEvent:
                    ApexLogger.Log($"Opening event: {notification.actionData}", ApexLogger.LogCategory.UI);
                    break;
                    
                case PCNotificationAction.OpenChat:
                    ApexLogger.Log($"Opening chat: {notification.actionData}", ApexLogger.LogCategory.UI);
                    break;
                    
                case PCNotificationAction.OpenSettings:
                    ApexLogger.Log("Opening settings", ApexLogger.LogCategory.UI);
                    break;
                    
                case PCNotificationAction.OpenURL:
                    if (!string.IsNullOrEmpty(notification.actionData))
                    {
                        Application.OpenURL(notification.actionData);
                    }
                    break;
            }
        }
        
        #endregion
        
        private class PCToastInstance
        {
            public PCNotification notification;
            public PCNotificationToast toast;
            public GameObject gameObject;
        }
    }
    
    #region Data Classes
    
    [Serializable]
    public class PCNotification
    {
        public string id;
        public string title;
        public string message;
        public PCNotificationCategory category;
        public PCNotificationPriority priority;
        public DateTime timestamp;
        public bool read;
        public Sprite icon;
        public PCNotificationAction actionType;
        public string actionData;
    }
    
    public enum PCNotificationCategory
    {
        All,
        Combat,
        Social,
        Resource,
        Event,
        Achievement,
        System
    }
    
    public enum PCNotificationPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }
    
    public enum PCNotificationAction
    {
        None,
        OpenMap,
        OpenAlliance,
        OpenEvent,
        OpenChat,
        OpenSettings,
        OpenURL,
        Custom
    }
    
    #endregion
    
    /// <summary>
    /// Individual toast notification
    /// </summary>
    public class PCNotificationToast : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image categoryStrip;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI timestampText;
        [SerializeField] private Button clickArea;
        [SerializeField] private Button dismissButton;
        
        [Header("Priority Colors")]
        [SerializeField] private Color normalColor = new Color(0.15f, 0.15f, 0.2f);
        [SerializeField] private Color urgentColor = new Color(0.4f, 0.15f, 0.15f);
        
        [Header("Category Colors")]
        [SerializeField] private Color combatColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color socialColor = new Color(0.2f, 0.6f, 0.8f);
        [SerializeField] private Color resourceColor = new Color(0.9f, 0.7f, 0.2f);
        [SerializeField] private Color systemColor = new Color(0.5f, 0.5f, 0.5f);
        
        private PCNotification _notification;
        private float _duration;
        private float _timeRemaining;
        
        public event Action OnClicked;
        public event Action OnDismissed;
        
        private void Awake()
        {
            clickArea?.onClick.AddListener(() => OnClicked?.Invoke());
            dismissButton?.onClick.AddListener(Dismiss);
        }
        
        private void Update()
        {
            if (_duration > 0)
            {
                _timeRemaining -= Time.unscaledDeltaTime;
                if (_timeRemaining <= 0)
                {
                    Dismiss();
                }
            }
        }
        
        public void Setup(PCNotification notification, float duration)
        {
            _notification = notification;
            _duration = duration;
            _timeRemaining = duration;
            
            // Title
            if (titleText != null)
            {
                titleText.text = notification.title;
                titleText.gameObject.SetActive(!string.IsNullOrEmpty(notification.title));
            }
            
            // Message
            if (messageText != null)
            {
                messageText.text = notification.message;
            }
            
            // Timestamp
            if (timestampText != null)
            {
                timestampText.text = FormatTimestamp(notification.timestamp);
            }
            
            // Background based on priority
            if (backgroundImage != null)
            {
                backgroundImage.color = notification.priority == PCNotificationPriority.Urgent 
                    ? urgentColor 
                    : normalColor;
            }
            
            // Category strip
            if (categoryStrip != null)
            {
                categoryStrip.color = GetCategoryColor(notification.category);
            }
            
            // Icon
            if (iconImage != null && notification.icon != null)
            {
                iconImage.sprite = notification.icon;
            }
        }
        
        private Color GetCategoryColor(PCNotificationCategory category)
        {
            return category switch
            {
                PCNotificationCategory.Combat => combatColor,
                PCNotificationCategory.Social => socialColor,
                PCNotificationCategory.Resource => resourceColor,
                _ => systemColor
            };
        }
        
        private string FormatTimestamp(DateTime timestamp)
        {
            var diff = DateTime.Now - timestamp;
            
            if (diff.TotalMinutes < 1)
                return "Just now";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}h ago";
            
            return timestamp.ToString("MMM dd");
        }
        
        public void Dismiss()
        {
            OnDismissed?.Invoke();
        }
        
        /// <summary>
        /// Pause auto-dismiss (on hover)
        /// </summary>
        public void PauseTimer()
        {
            _duration = 0;
        }
        
        /// <summary>
        /// Resume auto-dismiss
        /// </summary>
        public void ResumeTimer(float remaining)
        {
            _duration = remaining;
            _timeRemaining = remaining;
        }
    }
    
    /// <summary>
    /// Notification item in the notification center list
    /// </summary>
    public class PCNotificationListItem : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image unreadIndicator;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI timestampText;
        [SerializeField] private Button clickButton;
        [SerializeField] private Button dismissButton;
        
        private PCNotification _notification;
        
        public event Action OnClicked;
        public event Action OnDismissed;
        
        private void Awake()
        {
            clickButton?.onClick.AddListener(() => OnClicked?.Invoke());
            dismissButton?.onClick.AddListener(() => OnDismissed?.Invoke());
        }
        
        public void Setup(PCNotification notification)
        {
            _notification = notification;
            
            if (titleText != null)
            {
                titleText.text = notification.title;
            }
            
            if (messageText != null)
            {
                messageText.text = notification.message;
            }
            
            if (timestampText != null)
            {
                timestampText.text = FormatTimestamp(notification.timestamp);
            }
            
            if (unreadIndicator != null)
            {
                unreadIndicator.gameObject.SetActive(!notification.read);
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = notification.read 
                    ? new Color(0.1f, 0.1f, 0.15f) 
                    : new Color(0.15f, 0.15f, 0.25f);
            }
        }
        
        private string FormatTimestamp(DateTime timestamp)
        {
            var diff = DateTime.Now - timestamp;
            
            if (diff.TotalMinutes < 1)
                return "Just now";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} hours ago";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} days ago";
            
            return timestamp.ToString("MMM dd, yyyy");
        }
    }
    
    /// <summary>
    /// Settings for notification preferences
    /// </summary>
    public class PCNotificationSettings : MonoBehaviour
    {
        [Header("Toggle Preferences")]
        [SerializeField] private Toggle enableAllToggle;
        [SerializeField] private Toggle enableCombatToggle;
        [SerializeField] private Toggle enableSocialToggle;
        [SerializeField] private Toggle enableResourceToggle;
        [SerializeField] private Toggle enableSystemToggle;
        
        [Header("Sound Settings")]
        [SerializeField] private Toggle enableSoundToggle;
        [SerializeField] private Slider soundVolumeSlider;
        
        [Header("Do Not Disturb")]
        [SerializeField] private Toggle dndToggle;
        [SerializeField] private TMP_Dropdown dndStartDropdown;
        [SerializeField] private TMP_Dropdown dndEndDropdown;
        
        private PCNotificationPreferences _prefs;
        
        public event Action<PCNotificationPreferences> OnPreferencesChanged;
        
        private void Start()
        {
            LoadPreferences();
            BindControls();
        }
        
        private void LoadPreferences()
        {
            // Load from PlayerPrefs
            _prefs = new PCNotificationPreferences
            {
                enableAll = PlayerPrefs.GetInt("NotifEnableAll", 1) == 1,
                enableCombat = PlayerPrefs.GetInt("NotifEnableCombat", 1) == 1,
                enableSocial = PlayerPrefs.GetInt("NotifEnableSocial", 1) == 1,
                enableResource = PlayerPrefs.GetInt("NotifEnableResource", 1) == 1,
                enableSystem = PlayerPrefs.GetInt("NotifEnableSystem", 1) == 1,
                enableSound = PlayerPrefs.GetInt("NotifEnableSound", 1) == 1,
                soundVolume = PlayerPrefs.GetFloat("NotifSoundVolume", 1f),
                dndEnabled = PlayerPrefs.GetInt("NotifDND", 0) == 1,
                dndStartHour = PlayerPrefs.GetInt("NotifDNDStart", 22),
                dndEndHour = PlayerPrefs.GetInt("NotifDNDEnd", 8)
            };
            
            ApplyPreferencesToUI();
        }
        
        private void ApplyPreferencesToUI()
        {
            if (enableAllToggle != null) enableAllToggle.isOn = _prefs.enableAll;
            if (enableCombatToggle != null) enableCombatToggle.isOn = _prefs.enableCombat;
            if (enableSocialToggle != null) enableSocialToggle.isOn = _prefs.enableSocial;
            if (enableResourceToggle != null) enableResourceToggle.isOn = _prefs.enableResource;
            if (enableSystemToggle != null) enableSystemToggle.isOn = _prefs.enableSystem;
            if (enableSoundToggle != null) enableSoundToggle.isOn = _prefs.enableSound;
            if (soundVolumeSlider != null) soundVolumeSlider.value = _prefs.soundVolume;
            if (dndToggle != null) dndToggle.isOn = _prefs.dndEnabled;
        }
        
        private void BindControls()
        {
            enableAllToggle?.onValueChanged.AddListener(v => { _prefs.enableAll = v; SavePreferences(); });
            enableCombatToggle?.onValueChanged.AddListener(v => { _prefs.enableCombat = v; SavePreferences(); });
            enableSocialToggle?.onValueChanged.AddListener(v => { _prefs.enableSocial = v; SavePreferences(); });
            enableResourceToggle?.onValueChanged.AddListener(v => { _prefs.enableResource = v; SavePreferences(); });
            enableSystemToggle?.onValueChanged.AddListener(v => { _prefs.enableSystem = v; SavePreferences(); });
            enableSoundToggle?.onValueChanged.AddListener(v => { _prefs.enableSound = v; SavePreferences(); });
            soundVolumeSlider?.onValueChanged.AddListener(v => { _prefs.soundVolume = v; SavePreferences(); });
            dndToggle?.onValueChanged.AddListener(v => { _prefs.dndEnabled = v; SavePreferences(); });
        }
        
        private void SavePreferences()
        {
            PlayerPrefs.SetInt("NotifEnableAll", _prefs.enableAll ? 1 : 0);
            PlayerPrefs.SetInt("NotifEnableCombat", _prefs.enableCombat ? 1 : 0);
            PlayerPrefs.SetInt("NotifEnableSocial", _prefs.enableSocial ? 1 : 0);
            PlayerPrefs.SetInt("NotifEnableResource", _prefs.enableResource ? 1 : 0);
            PlayerPrefs.SetInt("NotifEnableSystem", _prefs.enableSystem ? 1 : 0);
            PlayerPrefs.SetInt("NotifEnableSound", _prefs.enableSound ? 1 : 0);
            PlayerPrefs.SetFloat("NotifSoundVolume", _prefs.soundVolume);
            PlayerPrefs.SetInt("NotifDND", _prefs.dndEnabled ? 1 : 0);
            PlayerPrefs.SetInt("NotifDNDStart", _prefs.dndStartHour);
            PlayerPrefs.SetInt("NotifDNDEnd", _prefs.dndEndHour);
            PlayerPrefs.Save();
            
            OnPreferencesChanged?.Invoke(_prefs);
        }
        
        public PCNotificationPreferences GetPreferences()
        {
            return _prefs;
        }
    }
    
    [Serializable]
    public class PCNotificationPreferences
    {
        public bool enableAll = true;
        public bool enableCombat = true;
        public bool enableSocial = true;
        public bool enableResource = true;
        public bool enableSystem = true;
        public bool enableSound = true;
        public float soundVolume = 1f;
        public bool dndEnabled;
        public int dndStartHour = 22;
        public int dndEndHour = 8;
    }
}
