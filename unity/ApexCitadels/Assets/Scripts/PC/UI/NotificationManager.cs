using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// In-game notification system with queue management.
    /// Supports multiple notification types and positions:
    /// - Achievement notifications
    /// - System messages
    /// - Combat alerts
    /// - Resource updates
    /// - Player interactions
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        [Header("Notification Containers")]
        [SerializeField] private RectTransform topRightContainer;
        [SerializeField] private RectTransform topCenterContainer;
        [SerializeField] private RectTransform bottomRightContainer;
        [SerializeField] private RectTransform centerContainer;
        
        [Header("Notification Prefabs")]
        [SerializeField] private NotificationPrefab standardNotification;
        [SerializeField] private NotificationPrefab achievementNotification;
        [SerializeField] private NotificationPrefab combatNotification;
        [SerializeField] private NotificationPrefab systemNotification;
        [SerializeField] private NotificationPrefab resourceNotification;
        
        [Header("Settings")]
        [SerializeField] private int maxVisibleNotifications = 5;
        [SerializeField] private float defaultDuration = 4f;
        [SerializeField] private float stackSpacing = 10f;
        [SerializeField] private float entryAnimationDuration = 0.4f;
        [SerializeField] private float exitAnimationDuration = 0.3f;
        
        [Header("Sound Effects")]
        [SerializeField] private AudioClip achievementSound;
        [SerializeField] private AudioClip alertSound;
        [SerializeField] private AudioClip messageSound;
        
        // Singleton
        private static NotificationManager _instance;
        public static NotificationManager Instance => _instance;
        
        // Active notifications per container
        private Dictionary<RectTransform, List<NotificationInstance>> _activeNotifications = 
            new Dictionary<RectTransform, List<NotificationInstance>>();
        
        // Queue for notifications when max visible reached
        private Queue<PendingNotification> _pendingQueue = new Queue<PendingNotification>();
        
        // Object pooling
        private Dictionary<NotifyType, Queue<NotificationInstance>> _notificationPools =
            new Dictionary<NotifyType, Queue<NotificationInstance>>();
        
        // Audio source
        private AudioSource _audioSource;
        
        // Events
        public event Action<NotifyData> OnNotificationShown;
        public event Action<NotifyData> OnNotificationDismissed;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeContainers();
            InitializePools();
            SetupAudioSource();
        }
        
        private void InitializeContainers()
        {
            if (topRightContainer != null)
                _activeNotifications[topRightContainer] = new List<NotificationInstance>();
            if (topCenterContainer != null)
                _activeNotifications[topCenterContainer] = new List<NotificationInstance>();
            if (bottomRightContainer != null)
                _activeNotifications[bottomRightContainer] = new List<NotificationInstance>();
            if (centerContainer != null)
                _activeNotifications[centerContainer] = new List<NotificationInstance>();
        }
        
        private void InitializePools()
        {
            foreach (NotificationType type in Enum.GetValues(typeof(NotificationType)))
            {
                _notificationPools[type] = new Queue<NotificationInstance>();
            }
        }
        
        private void SetupAudioSource()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
        }
        
        #region Public API
        
        /// <summary>
        /// Show a standard notification
        /// </summary>
        public void Show(string message, string title = null, NotifyType type = NotifyType.Standard)
        {
            var data = new NotifyData
            {
                message = message,
                title = title,
                type = type,
                duration = defaultDuration,
                position = GetDefaultPosition(type)
            };
            
            Show(data);
        }
        
        /// <summary>
        /// Show notification with full configuration
        /// </summary>
        public void Show(NotifyData data)
        {
            if (string.IsNullOrEmpty(data.message)) return;
            
            RectTransform container = GetContainer(data.position);
            if (container == null) return;
            
            // Check if we can show immediately
            var activeList = _activeNotifications[container];
            if (activeList.Count >= maxVisibleNotifications)
            {
                // Queue the notification
                _pendingQueue.Enqueue(new PendingNotification { data = data });
                return;
            }
            
            ShowNotificationImmediate(data, container);
        }
        
        /// <summary>
        /// Show achievement notification (special styling)
        /// </summary>
        public void ShowAchievement(string achievementName, string description, Sprite icon = null)
        {
            var data = new NotifyData
            {
                title = "Achievement Unlocked!",
                message = achievementName,
                subtitle = description,
                icon = icon,
                type = NotifyType.Achievement,
                position = NotifyPosition.TopCenter,
                duration = 5f,
                playSound = true
            };
            
            Show(data);
        }
        
        /// <summary>
        /// Show combat alert
        /// </summary>
        public void ShowCombatAlert(string message, CombatAlertType alertType)
        {
            var data = new NotifyData
            {
                message = message,
                type = NotifyType.Combat,
                position = NotifyPosition.Center,
                duration = 3f,
                playSound = true
            };
            
            // Set color based on alert type
            switch (alertType)
            {
                case CombatAlertType.Attack:
                    data.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
                    break;
                case CombatAlertType.Defense:
                    data.backgroundColor = new Color(0.2f, 0.5f, 0.8f, 0.9f);
                    break;
                case CombatAlertType.Victory:
                    data.backgroundColor = new Color(0.2f, 0.8f, 0.3f, 0.9f);
                    break;
                case CombatAlertType.Defeat:
                    data.backgroundColor = new Color(0.6f, 0.1f, 0.1f, 0.9f);
                    break;
            }
            
            Show(data);
        }
        
        /// <summary>
        /// Show resource update notification
        /// </summary>
        public void ShowResourceUpdate(string resourceName, int amount, bool isGain)
        {
            string sign = isGain ? "+" : "-";
            string colorHex = isGain ? "#4CAF50" : "#F44336";
            
            var data = new NotifyData
            {
                message = $"<color={colorHex}>{sign}{amount}</color> {resourceName}",
                type = NotifyType.Resource,
                position = NotifyPosition.TopRight,
                duration = 2.5f,
                playSound = false
            };
            
            Show(data);
        }
        
        /// <summary>
        /// Show system message
        /// </summary>
        public void ShowSystem(string message, SystemMessagePriority priority = SystemMessagePriority.Normal)
        {
            var data = new NotifyData
            {
                title = "System",
                message = message,
                type = NotifyType.System,
                position = NotifyPosition.TopCenter,
                duration = priority == SystemMessagePriority.Critical ? 8f : 4f,
                playSound = priority >= SystemMessagePriority.High
            };
            
            Show(data);
        }
        
        /// <summary>
        /// Dismiss all notifications
        /// </summary>
        public void DismissAll()
        {
            foreach (var container in _activeNotifications.Keys)
            {
                var list = new List<NotificationInstance>(_activeNotifications[container]);
                foreach (var notification in list)
                {
                    DismissNotification(notification, container);
                }
            }
            
            _pendingQueue.Clear();
        }
        
        /// <summary>
        /// Dismiss notifications of specific type
        /// </summary>
        public void DismissType(NotifyType type)
        {
            foreach (var container in _activeNotifications.Keys)
            {
                var toRemove = new List<NotificationInstance>();
                foreach (var notification in _activeNotifications[container])
                {
                    if (notification.data.type == type)
                    {
                        toRemove.Add(notification);
                    }
                }
                
                foreach (var notification in toRemove)
                {
                    DismissNotification(notification, container);
                }
            }
        }
        
        #endregion
        
        #region Internal Methods
        
        private void ShowNotificationImmediate(NotifyData data, RectTransform container)
        {
            // Get or create notification instance
            var instance = GetNotificationInstance(data.type);
            
            // Configure the notification
            ConfigureNotification(instance, data);
            
            // Parent to container
            instance.rectTransform.SetParent(container, false);
            instance.rectTransform.SetAsLastSibling();
            
            // Position
            PositionNewNotification(instance, container);
            
            // Add to active list
            _activeNotifications[container].Add(instance);
            
            // Play animation
            StartCoroutine(AnimateNotificationEntry(instance, container));
            
            // Play sound
            if (data.playSound)
            {
                PlayNotificationSound(data.type);
            }
            
            // Schedule auto-dismiss
            if (data.duration > 0)
            {
                StartCoroutine(AutoDismiss(instance, container, data.duration));
            }
            
            OnNotificationShown?.Invoke(data);
        }
        
        private NotificationInstance GetNotificationInstance(NotificationType type)
        {
            var pool = _notificationPools[type];
            
            if (pool.Count > 0)
            {
                var instance = pool.Dequeue();
                instance.rectTransform.gameObject.SetActive(true);
                return instance;
            }
            
            // Create new instance
            NotificationPrefab prefab = GetPrefabForType(type);
            if (prefab == null || prefab.prefab == null)
            {
                // Fall back to standard
                prefab = standardNotification;
            }
            
            if (prefab == null || prefab.prefab == null)
            {
                Debug.LogError("No notification prefab available");
                return null;
            }
            
            var go = Instantiate(prefab.prefab);
            var newInstance = new NotificationInstance
            {
                rectTransform = go.GetComponent<RectTransform>(),
                canvasGroup = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>(),
                titleText = go.transform.Find("Title")?.GetComponent<TextMeshProUGUI>(),
                messageText = go.transform.Find("Message")?.GetComponent<TextMeshProUGUI>(),
                subtitleText = go.transform.Find("Subtitle")?.GetComponent<TextMeshProUGUI>(),
                iconImage = go.transform.Find("Icon")?.GetComponent<Image>(),
                backgroundImage = go.GetComponent<Image>(),
                closeButton = go.transform.Find("CloseButton")?.GetComponent<Button>()
            };
            
            // Setup close button
            if (newInstance.closeButton != null)
            {
                newInstance.closeButton.onClick.AddListener(() => 
                    OnCloseButtonClicked(newInstance));
            }
            
            return newInstance;
        }
        
        private void ConfigureNotification(NotificationInstance instance, NotifyData data)
        {
            instance.data = data;
            
            if (instance.titleText != null)
            {
                if (!string.IsNullOrEmpty(data.title))
                {
                    instance.titleText.text = data.title;
                    instance.titleText.gameObject.SetActive(true);
                }
                else
                {
                    instance.titleText.gameObject.SetActive(false);
                }
            }
            
            if (instance.messageText != null)
            {
                instance.messageText.text = data.message;
            }
            
            if (instance.subtitleText != null)
            {
                if (!string.IsNullOrEmpty(data.subtitle))
                {
                    instance.subtitleText.text = data.subtitle;
                    instance.subtitleText.gameObject.SetActive(true);
                }
                else
                {
                    instance.subtitleText.gameObject.SetActive(false);
                }
            }
            
            if (instance.iconImage != null)
            {
                if (data.icon != null)
                {
                    instance.iconImage.sprite = data.icon;
                    instance.iconImage.gameObject.SetActive(true);
                }
                else
                {
                    instance.iconImage.gameObject.SetActive(false);
                }
            }
            
            if (instance.backgroundImage != null && data.backgroundColor != default)
            {
                instance.backgroundImage.color = data.backgroundColor;
            }
            
            // Update layout
            LayoutRebuilder.ForceRebuildLayoutImmediate(instance.rectTransform);
        }
        
        private void PositionNewNotification(NotificationInstance instance, RectTransform container)
        {
            var activeList = _activeNotifications[container];
            float yOffset = 0;
            
            // Calculate position based on existing notifications
            foreach (var existing in activeList)
            {
                yOffset -= existing.rectTransform.sizeDelta.y + stackSpacing;
            }
            
            instance.rectTransform.anchoredPosition = new Vector2(0, yOffset);
        }
        
        private IEnumerator AnimateNotificationEntry(NotificationInstance instance, RectTransform container)
        {
            // Slide in from right
            Vector2 startPos = instance.rectTransform.anchoredPosition + new Vector2(300, 0);
            Vector2 endPos = instance.rectTransform.anchoredPosition;
            
            instance.rectTransform.anchoredPosition = startPos;
            instance.canvasGroup.alpha = 0;
            
            // Easing curve for bounce effect
            float elapsed = 0;
            while (elapsed < entryAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / entryAnimationDuration;
                
                // Ease out back
                float overshoot = 1.5f;
                t = t - 1;
                float eased = t * t * ((overshoot + 1) * t + overshoot) + 1;
                
                instance.rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
                instance.canvasGroup.alpha = Mathf.Clamp01(t * 2 + 1);
                
                yield return null;
            }
            
            instance.rectTransform.anchoredPosition = endPos;
            instance.canvasGroup.alpha = 1;
        }
        
        private IEnumerator AutoDismiss(NotificationInstance instance, RectTransform container, float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            
            // Check if still active
            if (_activeNotifications[container].Contains(instance))
            {
                DismissNotification(instance, container);
            }
        }
        
        private void DismissNotification(NotificationInstance instance, RectTransform container)
        {
            if (!_activeNotifications[container].Contains(instance)) return;
            
            _activeNotifications[container].Remove(instance);
            
            StartCoroutine(AnimateNotificationExit(instance, () =>
            {
                // Return to pool
                instance.rectTransform.gameObject.SetActive(false);
                _notificationPools[instance.data.type].Enqueue(instance);
                
                // Reposition remaining notifications
                RepositionNotifications(container);
                
                // Check pending queue
                ProcessPendingQueue(container);
                
                OnNotificationDismissed?.Invoke(instance.data);
            }));
        }
        
        private IEnumerator AnimateNotificationExit(NotificationInstance instance, Action onComplete)
        {
            Vector2 startPos = instance.rectTransform.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(300, 0);
            
            float elapsed = 0;
            while (elapsed < exitAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / exitAnimationDuration;
                
                instance.rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                instance.canvasGroup.alpha = 1 - t;
                
                yield return null;
            }
            
            onComplete?.Invoke();
        }
        
        private void RepositionNotifications(RectTransform container)
        {
            var activeList = _activeNotifications[container];
            float yOffset = 0;
            
            foreach (var notification in activeList)
            {
                Vector2 targetPos = new Vector2(0, yOffset);
                StartCoroutine(AnimateRepositioning(notification, targetPos));
                yOffset -= notification.rectTransform.sizeDelta.y + stackSpacing;
            }
        }
        
        private IEnumerator AnimateRepositioning(NotificationInstance instance, Vector2 targetPos)
        {
            Vector2 startPos = instance.rectTransform.anchoredPosition;
            float duration = 0.2f;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                instance.rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }
            
            instance.rectTransform.anchoredPosition = targetPos;
        }
        
        private void ProcessPendingQueue(RectTransform container)
        {
            if (_pendingQueue.Count == 0) return;
            
            var activeList = _activeNotifications[container];
            if (activeList.Count >= maxVisibleNotifications) return;
            
            var pending = _pendingQueue.Dequeue();
            RectTransform pendingContainer = GetContainer(pending.data.position);
            
            if (pendingContainer == container)
            {
                ShowNotificationImmediate(pending.data, container);
            }
            else
            {
                // Re-queue for correct container
                _pendingQueue.Enqueue(pending);
            }
        }
        
        private void OnCloseButtonClicked(NotificationInstance instance)
        {
            foreach (var container in _activeNotifications.Keys)
            {
                if (_activeNotifications[container].Contains(instance))
                {
                    DismissNotification(instance, container);
                    return;
                }
            }
        }
        
        private void PlayNotificationSound(NotifyType type)
        {
            AudioClip clip = null;
            
            switch (type)
            {
                case NotifyType.Achievement:
                    clip = achievementSound;
                    break;
                case NotifyType.Combat:
                    clip = alertSound;
                    break;
                default:
                    clip = messageSound;
                    break;
            }
            
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
        
        #endregion
        
        #region Helpers
        
        private RectTransform GetContainer(NotifyPosition position)
        {
            switch (position)
            {
                case NotifyPosition.TopRight: return topRightContainer;
                case NotifyPosition.TopCenter: return topCenterContainer;
                case NotifyPosition.BottomRight: return bottomRightContainer;
                case NotifyPosition.Center: return centerContainer;
                default: return topRightContainer;
            }
        }
        
        private NotifyPosition GetDefaultPosition(NotifyType type)
        {
            switch (type)
            {
                case NotifyType.Achievement: return NotifyPosition.TopCenter;
                case NotifyType.Combat: return NotifyPosition.Center;
                case NotifyType.System: return NotifyPosition.TopCenter;
                case NotifyType.Resource: return NotifyPosition.TopRight;
                default: return NotifyPosition.TopRight;
            }
        }
        
        private NotificationPrefab GetPrefabForType(NotifyType type)
        {
            switch (type)
            {
                case NotifyType.Achievement: return achievementNotification;
                case NotifyType.Combat: return combatNotification;
                case NotifyType.System: return systemNotification;
                case NotifyType.Resource: return resourceNotification;
                default: return standardNotification;
            }
        }
        
        #endregion
    }
    
    #region Data Types
    
    [Serializable]
    public class NotifyData
    {
        public string title;
        public string message;
        public string subtitle;
        public Sprite icon;
        public NotifyType type = NotifyType.Standard;
        public NotifyPosition position = NotifyPosition.TopRight;
        public float duration = 4f;
        public bool playSound = true;
        public Color backgroundColor = default;
        public Action onClick;
    }
    
    [Serializable]
    public class NotificationPrefab
    {
        public NotifyType type;
        public GameObject prefab;
    }
    
    public class NotificationInstance
    {
        public RectTransform rectTransform;
        public CanvasGroup canvasGroup;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI messageText;
        public TextMeshProUGUI subtitleText;
        public Image iconImage;
        public Image backgroundImage;
        public Button closeButton;
        public NotifyData data;
    }
    
    public class PendingNotification
    {
        public NotifyData data;
    }
    
    public enum NotifyType
    {
        Standard,
        Achievement,
        Combat,
        System,
        Resource,
        Social,
        Quest
    }
    
    public enum NotifyPosition
    {
        TopRight,
        TopCenter,
        TopLeft,
        BottomRight,
        BottomCenter,
        BottomLeft,
        Center
    }
    
    public enum CombatAlertType
    {
        Attack,
        Defense,
        Victory,
        Defeat
    }
    
    public enum SystemMessagePriority
    {
        Low,
        Normal,
        High,
        Critical
    }
    
    #endregion
}
