using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Notification System - Toast notifications, alerts, and popups.
    /// Essential for keeping players informed without interrupting gameplay.
    /// </summary>
    public class NotificationSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float defaultDuration = 4f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private int maxVisibleNotifications = 5;
        
        [Header("Colors")]
        [SerializeField] private Color infoColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color successColor = new Color(0.2f, 0.7f, 0.3f);
        [SerializeField] private Color warningColor = new Color(0.9f, 0.7f, 0.2f);
        [SerializeField] private Color errorColor = new Color(0.8f, 0.3f, 0.3f);
        [SerializeField] private Color resourceColor = new Color(0.9f, 0.8f, 0.2f);
        [SerializeField] private Color combatColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color allianceColor = new Color(0.5f, 0.3f, 0.8f);
        
        // UI Elements
        private GameObject _toastContainer;
        private GameObject _alertContainer;
        private Queue<ToastNotificationData> _pendingNotifications = new Queue<ToastNotificationData>();
        private List<GameObject> _activeToasts = new List<GameObject>();
        private GameObject _currentAlert;
        
        public static NotificationSystem Instance { get; private set; }
        
        public event Action<ToastNotificationData> OnNotificationShown;
        public event Action<ToastNotificationData> OnNotificationClicked;

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
            CreateContainers();
            StartCoroutine(ProcessNotificationQueue());
        }

        private void CreateContainers()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Toast container (bottom right)
            _toastContainer = new GameObject("ToastContainer");
            _toastContainer.transform.SetParent(canvas.transform, false);
            
            RectTransform toastRect = _toastContainer.AddComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(1f, 0f);
            toastRect.anchorMax = new Vector2(1f, 0.5f);
            toastRect.pivot = new Vector2(1f, 0f);
            toastRect.anchoredPosition = new Vector2(-20, 80);
            toastRect.sizeDelta = new Vector2(350, 400);
            
            VerticalLayoutGroup vlayout = _toastContainer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.LowerRight;
            vlayout.childForceExpandWidth = false;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(0, 0, 10, 10);
            vlayout.reverseArrangement = true;
            
            ContentSizeFitter fitter = _toastContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Alert container (center screen)
            _alertContainer = new GameObject("AlertContainer");
            _alertContainer.transform.SetParent(canvas.transform, false);
            
            RectTransform alertRect = _alertContainer.AddComponent<RectTransform>();
            alertRect.anchorMin = new Vector2(0.3f, 0.3f);
            alertRect.anchorMax = new Vector2(0.7f, 0.7f);
            alertRect.offsetMin = Vector2.zero;
            alertRect.offsetMax = Vector2.zero;
        }

        private IEnumerator ProcessNotificationQueue()
        {
            while (true)
            {
                while (_pendingNotifications.Count > 0 && _activeToasts.Count < maxVisibleNotifications)
                {
                    var notification = _pendingNotifications.Dequeue();
                    ShowToastImmediate(notification);
                    yield return new WaitForSeconds(0.15f); // Stagger notifications
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void ShowToastImmediate(ToastNotificationData data)
        {
            GameObject toast = CreateToast(data);
            _activeToasts.Add(toast);
            
            OnNotificationShown?.Invoke(data);
            
            // Auto-dismiss
            StartCoroutine(DismissToastAfterDelay(toast, data.Duration));
        }

        private GameObject CreateToast(ToastNotificationData data)
        {
            GameObject toast = new GameObject($"Toast_{data.Type}");
            toast.transform.SetParent(_toastContainer.transform, false);
            
            LayoutElement le = toast.AddComponent<LayoutElement>();
            le.preferredWidth = 320;
            le.preferredHeight = GetToastHeight(data);
            
            // Background with accent border
            Image bg = toast.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            
            // Type-based accent
            Color accentColor = GetColorForType(data.Type);
            
            // Left accent bar
            GameObject accent = new GameObject("Accent");
            accent.transform.SetParent(toast.transform, false);
            
            RectTransform accentRect = accent.AddComponent<RectTransform>();
            accentRect.anchorMin = Vector2.zero;
            accentRect.anchorMax = new Vector2(0.015f, 1f);
            accentRect.offsetMin = Vector2.zero;
            accentRect.offsetMax = Vector2.zero;
            
            Image accentImg = accent.AddComponent<Image>();
            accentImg.color = accentColor;
            
            // Content layout
            HorizontalLayoutGroup hlayout = toast.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(15, 10, 8, 8);
            hlayout.childForceExpandWidth = false;
            
            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(toast.transform, false);
            
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 30;
            iconLE.preferredHeight = 30;
            
            TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
            iconText.text = GetIconForType(data.Type);
            iconText.fontSize = 24;
            iconText.alignment = TextAlignmentOptions.Center;
            
            // Text content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(toast.transform, false);
            
            LayoutElement contentLE = content.AddComponent<LayoutElement>();
            contentLE.flexibleWidth = 1;
            
            VerticalLayoutGroup contentVL = content.AddComponent<VerticalLayoutGroup>();
            contentVL.childAlignment = TextAnchor.MiddleLeft;
            contentVL.childForceExpandHeight = false;
            contentVL.spacing = 2;
            
            // Title
            if (!string.IsNullOrEmpty(data.Title))
            {
                GameObject titleObj = new GameObject("Title");
                titleObj.transform.SetParent(content.transform, false);
                
                TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.text = data.Title;
                titleText.fontSize = 14;
                titleText.fontStyle = FontStyles.Bold;
                titleText.color = accentColor;
            }
            
            // Message
            GameObject msgObj = new GameObject("Message");
            msgObj.transform.SetParent(content.transform, false);
            
            TextMeshProUGUI msgText = msgObj.AddComponent<TextMeshProUGUI>();
            msgText.text = data.Message;
            msgText.fontSize = 12;
            msgText.color = new Color(0.85f, 0.85f, 0.85f);
            msgText.enableWordWrapping = true;
            
            // Close button
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(toast.transform, false);
            
            LayoutElement closeBtnLE = closeBtn.AddComponent<LayoutElement>();
            closeBtnLE.preferredWidth = 24;
            closeBtnLE.preferredHeight = 24;
            
            Button btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(() => DismissToast(toast));
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(closeBtn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI closeText = textObj.AddComponent<TextMeshProUGUI>();
            closeText.text = "✕";
            closeText.fontSize = 16;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.color = new Color(0.5f, 0.5f, 0.5f);
            
            // Click handler for actionable notifications
            if (data.OnClick != null)
            {
                Button toastBtn = toast.AddComponent<Button>();
                toastBtn.onClick.AddListener(() =>
                {
                    data.OnClick?.Invoke();
                    OnNotificationClicked?.Invoke(data);
                    DismissToast(toast);
                });
                
                ColorBlock colors = toastBtn.colors;
                colors.highlightedColor = new Color(0.15f, 0.15f, 0.2f);
                toastBtn.colors = colors;
            }
            
            // Animate in
            CanvasGroup cg = toast.AddComponent<CanvasGroup>();
            cg.alpha = 0;
            StartCoroutine(FadeIn(cg));
            
            return toast;
        }

        private int GetToastHeight(ToastNotificationData data)
        {
            int baseHeight = 50;
            if (!string.IsNullOrEmpty(data.Title)) baseHeight += 18;
            if (data.Message.Length > 50) baseHeight += 15;
            return baseHeight;
        }

        private IEnumerator FadeIn(CanvasGroup cg)
        {
            float elapsed = 0;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                cg.alpha = elapsed / 0.2f;
                yield return null;
            }
            cg.alpha = 1;
        }

        private IEnumerator DismissToastAfterDelay(GameObject toast, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (toast != null)
            {
                DismissToast(toast);
            }
        }

        private void DismissToast(GameObject toast)
        {
            if (toast == null || !_activeToasts.Contains(toast)) return;
            
            _activeToasts.Remove(toast);
            StartCoroutine(FadeOutAndDestroy(toast));
        }

        private IEnumerator FadeOutAndDestroy(GameObject toast)
        {
            CanvasGroup cg = toast.GetComponent<CanvasGroup>();
            if (cg == null) cg = toast.AddComponent<CanvasGroup>();
            
            float elapsed = 0;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = 1 - (elapsed / fadeOutDuration);
                yield return null;
            }
            
            Destroy(toast);
        }

        private Color GetColorForType(ToastNotificationType type)
        {
            return type switch
            {
                ToastNotificationType.Info => infoColor,
                ToastNotificationType.Success => successColor,
                ToastNotificationType.Warning => warningColor,
                ToastNotificationType.Error => errorColor,
                ToastNotificationType.Resource => resourceColor,
                ToastNotificationType.Combat => combatColor,
                ToastNotificationType.Alliance => allianceColor,
                ToastNotificationType.Quest => new Color(0.3f, 0.8f, 0.5f),
                ToastNotificationType.Achievement => new Color(1f, 0.84f, 0f),
                ToastNotificationType.LevelUp => new Color(0.8f, 0.6f, 1f),
                _ => infoColor
            };
        }

        private string GetIconForType(ToastNotificationType type)
        {
            return type switch
            {
                ToastNotificationType.Info => "ℹ️",
                ToastNotificationType.Success => "✅",
                ToastNotificationType.Warning => "⚠️",
                ToastNotificationType.Error => "❌",
                ToastNotificationType.Resource => "💰",
                ToastNotificationType.Combat => "⚔️",
                ToastNotificationType.Alliance => "🛡️",
                ToastNotificationType.Quest => "📋",
                ToastNotificationType.Achievement => "🏆",
                ToastNotificationType.LevelUp => "⬆️",
                _ => "📢"
            };
        }

        #region Alert Popups

        public void ShowAlert(string title, string message, Action onConfirm = null, Action onCancel = null)
        {
            // Dismiss existing alert
            if (_currentAlert != null)
            {
                Destroy(_currentAlert);
            }
            
            _currentAlert = CreateAlert(title, message, onConfirm, onCancel);
        }

        private GameObject CreateAlert(string title, string message, Action onConfirm, Action onCancel)
        {
            GameObject alert = new GameObject("Alert");
            alert.transform.SetParent(_alertContainer.transform, false);
            
            RectTransform rect = alert.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Semi-transparent backdrop
            Image backdrop = alert.AddComponent<Image>();
            backdrop.color = new Color(0, 0, 0, 0.7f);
            
            // Alert box
            GameObject box = new GameObject("Box");
            box.transform.SetParent(alert.transform, false);
            
            RectTransform boxRect = box.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.2f, 0.3f);
            boxRect.anchorMax = new Vector2(0.8f, 0.7f);
            boxRect.offsetMin = Vector2.zero;
            boxRect.offsetMax = Vector2.zero;
            
            Image boxBg = box.AddComponent<Image>();
            boxBg.color = new Color(0.12f, 0.12f, 0.18f, 1f);
            
            VerticalLayoutGroup vlayout = box.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 15;
            vlayout.padding = new RectOffset(25, 25, 20, 20);
            
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(box.transform, false);
            
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 35;
            
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 22;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = warningColor;
            
            // Message
            GameObject msgObj = new GameObject("Message");
            msgObj.transform.SetParent(box.transform, false);
            
            LayoutElement msgLE = msgObj.AddComponent<LayoutElement>();
            msgLE.flexibleHeight = 1;
            
            TextMeshProUGUI msgText = msgObj.AddComponent<TextMeshProUGUI>();
            msgText.text = message;
            msgText.fontSize = 16;
            msgText.alignment = TextAlignmentOptions.Center;
            msgText.color = new Color(0.85f, 0.85f, 0.85f);
            msgText.enableWordWrapping = true;
            
            // Buttons
            GameObject buttons = new GameObject("Buttons");
            buttons.transform.SetParent(box.transform, false);
            
            LayoutElement btnLE = buttons.AddComponent<LayoutElement>();
            btnLE.preferredHeight = 45;
            
            HorizontalLayoutGroup btnHL = buttons.AddComponent<HorizontalLayoutGroup>();
            btnHL.childAlignment = TextAnchor.MiddleCenter;
            btnHL.spacing = 20;
            btnHL.childForceExpandWidth = false;
            
            if (onCancel != null)
            {
                CreateAlertButton(buttons.transform, "Cancel", new Color(0.4f, 0.4f, 0.5f), () =>
                {
                    onCancel?.Invoke();
                    DismissAlert();
                });
            }
            
            CreateAlertButton(buttons.transform, onCancel != null ? "Confirm" : "OK", infoColor, () =>
            {
                onConfirm?.Invoke();
                DismissAlert();
            });
            
            return alert;
        }

        private void CreateAlertButton(Transform parent, string label, Color color, Action onClick)
        {
            GameObject btn = new GameObject($"Btn_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 120;
            le.preferredHeight = 40;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            ColorBlock colors = button.colors;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            button.colors = colors;
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void DismissAlert()
        {
            if (_currentAlert != null)
            {
                Destroy(_currentAlert);
                _currentAlert = null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Show a toast notification
        /// </summary>
        public void ShowToast(string message, ToastNotificationType type = ToastNotificationType.Info, string title = null, float duration = 0)
        {
            var data = new ToastNotificationData
            {
                Type = type,
                Title = title,
                Message = message,
                Duration = duration > 0 ? duration : defaultDuration
            };
            
            _pendingNotifications.Enqueue(data);
        }

        /// <summary>
        /// Show a clickable toast notification
        /// </summary>
        public void ShowToastWithAction(string message, Action onClick, ToastNotificationType type = ToastNotificationType.Info, string title = null)
        {
            var data = new ToastNotificationData
            {
                Type = type,
                Title = title,
                Message = message,
                Duration = defaultDuration + 2f, // Longer for clickable
                OnClick = onClick
            };
            
            _pendingNotifications.Enqueue(data);
        }

        // Convenience methods
        public void ShowInfo(string message, string title = null) => ShowToast(message, ToastNotificationType.Info, title);
        public void ShowSuccess(string message, string title = null) => ShowToast(message, ToastNotificationType.Success, title);
        public void ShowWarning(string message, string title = null) => ShowToast(message, ToastNotificationType.Warning, title);
        public void ShowError(string message, string title = null) => ShowToast(message, ToastNotificationType.Error, title);
        
        public void ShowResourceGain(string resource, int amount)
        {
            ShowToast($"+{amount:N0} {resource}", ToastNotificationType.Resource, "Resources Collected");
        }

        // Alias for ShowResourceGain
        public void ShowResourceGained(string resource, int amount) => ShowResourceGain(resource, amount);
        
        public void ShowResourceSpent(string resource, int amount)
        {
            ShowToast($"-{amount:N0} {resource}", ToastNotificationType.Resource);
        }
        
        public void ShowCombatResult(bool victory, string territoryName, int troopsLost = 0)
        {
            if (victory)
                ShowToast($"Captured {territoryName}!", ToastNotificationType.Combat, "Victory!");
            else
                ShowToast($"Attack on {territoryName} failed. {troopsLost} troops lost.", ToastNotificationType.Combat, "Defeat");
        }
        
        public void ShowQuestComplete(string questName, string rewards)
        {
            ShowToast($"{questName}\nReward: {rewards}", ToastNotificationType.Quest, "Quest Complete!");
        }
        
        public void ShowAchievementUnlocked(string achievementName, int points)
        {
            ShowToast($"{achievementName} (+{points} pts)", ToastNotificationType.Achievement, "Achievement Unlocked!");
        }
        
        public void ShowLevelUp(int newLevel)
        {
            ShowToast($"You are now Level {newLevel}!", ToastNotificationType.LevelUp, "Level Up!");
        }
        
        public void ShowAllianceMessage(string playerName, string message)
        {
            ShowToast($"{playerName}: {message}", ToastNotificationType.Alliance, "Alliance");
        }

        /// <summary>
        /// Clear all visible notifications
        /// </summary>
        public void ClearAll()
        {
            foreach (var toast in _activeToasts.ToArray())
            {
                if (toast != null)
                    Destroy(toast);
            }
            _activeToasts.Clear();
            _pendingNotifications.Clear();
            DismissAlert();
        }

        #endregion
    }

    public enum ToastNotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Resource,
        Combat,
        Alliance,
        Quest,
        Achievement,
        LevelUp
    }

    public class ToastNotificationData
    {
        public ToastNotificationType Type;
        public string Title;
        public string Message;
        public float Duration;
        public Action OnClick;
    }
}
