using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;

namespace ApexCitadels.UI
{
    /// <summary>
    /// Centralized error handling system for Apex Citadels.
    /// Catches errors, displays user-friendly notifications, and provides retry logic.
    /// 
    /// Usage:
    ///   ErrorManager.HandleError(exception, "Loading map data", ErrorSeverity.Warning);
    ///   ErrorManager.ShowError("Connection lost", "Please check your internet connection");
    ///   
    /// With retry:
    ///   ErrorManager.HandleWithRetry(async () => await LoadData(), 3, "Loading data");
    /// </summary>
    public class ErrorManager : MonoBehaviour
    {
        public static ErrorManager Instance { get; private set; }

        #region Enums

        public enum ErrorSeverity
        {
            Info,       // Informational, auto-dismiss
            Warning,    // User should know, auto-dismiss
            Error,      // Problem occurred, requires acknowledgment
            Critical    // Major failure, may need app restart
        }

        public enum ErrorCategory
        {
            Network,
            Firebase,
            Authentication,
            Storage,
            GameLogic,
            UI,
            Loading,
            Unknown
        }

        #endregion

        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI detailText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button detailsButton;

        [Header("Toast Container")]
        [SerializeField] private RectTransform toastContainer;

        [Header("Settings")]
        [SerializeField] private float toastDuration = 3f;
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private int maxToasts = 3;

        [Header("Icons")]
        [SerializeField] private Sprite infoIcon;
        [SerializeField] private Sprite warningIcon;
        [SerializeField] private Sprite errorIcon;
        [SerializeField] private Sprite criticalIcon;

        [Header("Colors")]
        [SerializeField] private Color infoColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color errorColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color criticalColor = new Color(0.8f, 0.1f, 0.1f, 1f);

        #endregion

        #region Private Fields

        private Queue<ErrorInfo> _errorQueue = new Queue<ErrorInfo>();
        private List<GameObject> _activeToasts = new List<GameObject>();
        private bool _isShowingError;
        private Action _currentRetryAction;
        private bool _detailsExpanded;
        private List<ErrorLog> _errorHistory = new List<ErrorLog>();
        private const int MaxErrorHistory = 100;

        #endregion

        #region Events

        public static event Action<ErrorInfo> OnErrorOccurred;
        public static event Action<ErrorInfo> OnErrorDismissed;
        public static event Action<ErrorInfo> OnErrorRetried;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Create UI if not assigned
            if (errorPanel == null)
            {
                CreateUI();
            }

            // Setup button listeners
            SetupButtons();

            // Hide at start
            if (errorPanel != null)
                errorPanel.SetActive(false);

            // Subscribe to Unity's log callback for uncaught exceptions
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;

            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Handle an exception with automatic categorization
        /// </summary>
        public static void HandleError(Exception exception, string context = null, ErrorSeverity severity = ErrorSeverity.Error)
        {
            EnsureInstance();
            Instance.HandleErrorInternal(exception, context, severity);
        }

        /// <summary>
        /// Show an error dialog with title and message
        /// </summary>
        public static void ShowError(string title, string message, ErrorSeverity severity = ErrorSeverity.Error, Action retryAction = null)
        {
            EnsureInstance();
            Instance.ShowErrorInternal(title, message, severity, retryAction);
        }

        /// <summary>
        /// Show a toast notification (auto-dismissing)
        /// </summary>
        public static void ShowToast(string message, ErrorSeverity severity = ErrorSeverity.Info)
        {
            EnsureInstance();
            Instance.ShowToastInternal(message, severity);
        }

        /// <summary>
        /// Execute an action with automatic retry on failure
        /// </summary>
        public static void HandleWithRetry(Action action, int maxRetries = 3, string operationName = "Operation", float retryDelay = 1f)
        {
            EnsureInstance();
            Instance.StartCoroutine(Instance.RetryCoroutine(action, maxRetries, operationName, retryDelay));
        }

        /// <summary>
        /// Execute a coroutine with automatic retry on failure
        /// </summary>
        public static Coroutine HandleWithRetryCoroutine(Func<IEnumerator> coroutineFunc, int maxRetries = 3, string operationName = "Operation", float retryDelay = 1f)
        {
            EnsureInstance();
            return Instance.StartCoroutine(Instance.RetryCoroutineWrapper(coroutineFunc, maxRetries, operationName, retryDelay));
        }

        /// <summary>
        /// Get error history for debugging
        /// </summary>
        public static List<ErrorLog> GetErrorHistory()
        {
            return Instance?._errorHistory ?? new List<ErrorLog>();
        }

        /// <summary>
        /// Clear error history
        /// </summary>
        public static void ClearHistory()
        {
            Instance?._errorHistory.Clear();
        }

        /// <summary>
        /// Dismiss current error dialog
        /// </summary>
        public static void Dismiss()
        {
            Instance?.DismissInternal();
        }

        #endregion

        #region Instance Methods

        private void HandleErrorInternal(Exception exception, string context, ErrorSeverity severity)
        {
            var category = CategorizeException(exception);
            var userMessage = GetUserFriendlyMessage(exception, category);

            var info = new ErrorInfo
            {
                Title = GetTitleForCategory(category),
                Message = userMessage,
                Detail = exception.ToString(),
                Severity = severity,
                Category = category,
                Context = context,
                Exception = exception,
                Timestamp = DateTime.Now
            };

            LogError(info);
            ApexLogger.LogError($"[{category}] {context}: {exception.Message}", ApexLogger.LogCategory.General);

            if (severity >= ErrorSeverity.Error)
            {
                QueueError(info);
            }
            else
            {
                ShowToastInternal($"{info.Title}: {info.Message}", severity);
            }

            OnErrorOccurred?.Invoke(info);
        }

        private void ShowErrorInternal(string title, string message, ErrorSeverity severity, Action retryAction)
        {
            var info = new ErrorInfo
            {
                Title = title,
                Message = message,
                Severity = severity,
                Category = ErrorCategory.Unknown,
                Timestamp = DateTime.Now
            };

            LogError(info);

            if (severity >= ErrorSeverity.Error)
            {
                _currentRetryAction = retryAction;
                QueueError(info);
            }
            else
            {
                ShowToastInternal($"{title}: {message}", severity);
            }

            OnErrorOccurred?.Invoke(info);
        }

        private void ShowToastInternal(string message, ErrorSeverity severity)
        {
            StartCoroutine(ShowToastCoroutine(message, severity));
        }

        private IEnumerator ShowToastCoroutine(string message, ErrorSeverity severity)
        {
            // Ensure toast container
            if (toastContainer == null)
                CreateToastContainer();

            // Remove old toasts if at max
            while (_activeToasts.Count >= maxToasts)
            {
                var oldToast = _activeToasts[0];
                _activeToasts.RemoveAt(0);
                StartCoroutine(FadeOutAndDestroy(oldToast, 0.1f));
            }

            // Create toast
            var toast = CreateToast(message, severity);
            _activeToasts.Add(toast);

            // Fade in
            var cg = toast.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    cg.alpha = elapsed / fadeInDuration;
                    yield return null;
                }
                cg.alpha = 1f;
            }

            // Wait
            yield return new WaitForSecondsRealtime(toastDuration);

            // Fade out and destroy
            if (toast != null && _activeToasts.Contains(toast))
            {
                _activeToasts.Remove(toast);
                yield return FadeOutAndDestroy(toast, fadeOutDuration);
            }
        }

        private IEnumerator FadeOutAndDestroy(GameObject obj, float duration)
        {
            if (obj == null) yield break;

            var cg = obj.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                float startAlpha = cg.alpha;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    if (obj == null) yield break;
                    elapsed += Time.unscaledDeltaTime;
                    cg.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                    yield return null;
                }
            }

            if (obj != null)
                Destroy(obj);
        }

        private void QueueError(ErrorInfo info)
        {
            _errorQueue.Enqueue(info);

            if (!_isShowingError)
            {
                ProcessNextError();
            }
        }

        private void ProcessNextError()
        {
            if (_errorQueue.Count == 0)
            {
                _isShowingError = false;
                return;
            }

            _isShowingError = true;
            var info = _errorQueue.Dequeue();
            DisplayError(info);
        }

        private void DisplayError(ErrorInfo info)
        {
            if (errorPanel == null) return;

            // Update content
            if (titleText != null)
                titleText.text = info.Title;

            if (messageText != null)
                messageText.text = info.Message;

            if (detailText != null)
            {
                detailText.text = info.Detail ?? "";
                detailText.gameObject.SetActive(false);
                _detailsExpanded = false;
            }

            // Update icon and colors
            UpdateVisuals(info.Severity);

            // Show/hide retry button
            if (retryButton != null)
                retryButton.gameObject.SetActive(_currentRetryAction != null);

            // Show panel
            errorPanel.SetActive(true);
            StartCoroutine(FadeInPanel());
        }

        private void UpdateVisuals(ErrorSeverity severity)
        {
            Color color;
            Sprite icon;

            switch (severity)
            {
                case ErrorSeverity.Info:
                    color = infoColor;
                    icon = infoIcon;
                    break;
                case ErrorSeverity.Warning:
                    color = warningColor;
                    icon = warningIcon;
                    break;
                case ErrorSeverity.Critical:
                    color = criticalColor;
                    icon = criticalIcon;
                    break;
                default:
                    color = errorColor;
                    icon = errorIcon;
                    break;
            }

            if (iconImage != null)
            {
                iconImage.color = color;
                if (icon != null)
                    iconImage.sprite = icon;
            }

            if (titleText != null)
                titleText.color = color;
        }

        private IEnumerator FadeInPanel()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = elapsed / fadeInDuration;
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private void DismissInternal()
        {
            StartCoroutine(DismissCoroutine());
        }

        private IEnumerator DismissCoroutine()
        {
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = 1f - (elapsed / fadeOutDuration);
                    yield return null;
                }
            }

            if (errorPanel != null)
                errorPanel.SetActive(false);

            _currentRetryAction = null;
            ProcessNextError();
        }

        private void OnRetry()
        {
            var retryAction = _currentRetryAction;
            _currentRetryAction = null;

            StartCoroutine(DismissCoroutine());

            if (retryAction != null)
            {
                try
                {
                    retryAction.Invoke();
                }
                catch (Exception ex)
                {
                    HandleErrorInternal(ex, "Retry operation", ErrorSeverity.Error);
                }
            }
        }

        private void OnToggleDetails()
        {
            _detailsExpanded = !_detailsExpanded;

            if (detailText != null)
                detailText.gameObject.SetActive(_detailsExpanded);

            if (detailsButton != null)
            {
                var buttonText = detailsButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = _detailsExpanded ? "Hide Details" : "Show Details";
            }
        }

        private IEnumerator RetryCoroutine(Action action, int maxRetries, string operationName, float retryDelay)
        {
            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;

                try
                {
                    action.Invoke();
                    yield break; // Success
                }
                catch (Exception ex)
                {
                    ApexLogger.LogWarning($"{operationName} failed (attempt {attempt}/{maxRetries}): {ex.Message}");

                    if (attempt >= maxRetries)
                    {
                        HandleErrorInternal(ex, $"{operationName} (after {maxRetries} attempts)", ErrorSeverity.Error);
                        yield break;
                    }

                    ShowToastInternal($"{operationName} failed, retrying... ({attempt}/{maxRetries})", ErrorSeverity.Warning);
                }
                
                if (attempt < maxRetries)
                {
                    yield return new WaitForSecondsRealtime(retryDelay);
                }
            }
        }

        private IEnumerator RetryCoroutineWrapper(Func<IEnumerator> coroutineFunc, int maxRetries, string operationName, float retryDelay)
        {
            int attempt = 0;
            Exception lastException = null;

            while (attempt < maxRetries)
            {
                attempt++;
                lastException = null;

                var enumerator = coroutineFunc();
                bool completed = false;

                while (!completed)
                {
                    bool hasNext;
                    try
                    {
                        hasNext = enumerator.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        break;
                    }

                    if (!hasNext)
                    {
                        completed = true;
                    }
                    else
                    {
                        yield return enumerator.Current;
                    }
                }

                if (lastException == null && completed)
                {
                    yield break; // Success
                }

                if (lastException != null)
                {
                    ApexLogger.LogWarning($"{operationName} failed (attempt {attempt}/{maxRetries}): {lastException.Message}");

                    if (attempt >= maxRetries)
                    {
                        HandleErrorInternal(lastException, $"{operationName} (after {maxRetries} attempts)", ErrorSeverity.Error);
                        yield break;
                    }

                    ShowToastInternal($"{operationName} failed, retrying... ({attempt}/{maxRetries})", ErrorSeverity.Warning);
                    yield return new WaitForSecondsRealtime(retryDelay);
                }
            }
        }

        #endregion

        #region Helpers

        private ErrorCategory CategorizeException(Exception ex)
        {
            string typeName = ex.GetType().Name.ToLower();
            string message = ex.Message.ToLower();

            if (typeName.Contains("network") || message.Contains("network") || message.Contains("connection"))
                return ErrorCategory.Network;

            if (typeName.Contains("firebase") || message.Contains("firebase"))
                return ErrorCategory.Firebase;

            if (typeName.Contains("auth") || message.Contains("authentication") || message.Contains("unauthorized"))
                return ErrorCategory.Authentication;

            if (typeName.Contains("io") || message.Contains("file") || message.Contains("storage") || message.Contains("disk"))
                return ErrorCategory.Storage;

            if (typeName.Contains("ui") || message.Contains("canvas") || message.Contains("ui"))
                return ErrorCategory.UI;

            return ErrorCategory.Unknown;
        }

        private string GetUserFriendlyMessage(Exception ex, ErrorCategory category)
        {
            switch (category)
            {
                case ErrorCategory.Network:
                    return "Unable to connect to the server. Please check your internet connection and try again.";

                case ErrorCategory.Firebase:
                    return "There was a problem communicating with our servers. Please try again in a moment.";

                case ErrorCategory.Authentication:
                    return "Your session has expired. Please sign in again.";

                case ErrorCategory.Storage:
                    return "Unable to access local storage. Please ensure you have enough disk space.";

                case ErrorCategory.Loading:
                    return "Failed to load game data. Please restart the game.";

                default:
                    // Generic but still friendly
                    return "Something went wrong. Please try again.";
            }
        }

        private string GetTitleForCategory(ErrorCategory category)
        {
            return category switch
            {
                ErrorCategory.Network => "Connection Error",
                ErrorCategory.Firebase => "Server Error",
                ErrorCategory.Authentication => "Authentication Error",
                ErrorCategory.Storage => "Storage Error",
                ErrorCategory.Loading => "Loading Error",
                ErrorCategory.UI => "Display Error",
                ErrorCategory.GameLogic => "Game Error",
                _ => "Error"
            };
        }

        private void LogError(ErrorInfo info)
        {
            _errorHistory.Add(new ErrorLog
            {
                Info = info,
                Timestamp = DateTime.Now,
                StackTrace = info.Exception?.StackTrace ?? Environment.StackTrace
            });

            // Trim history
            while (_errorHistory.Count > MaxErrorHistory)
            {
                _errorHistory.RemoveAt(0);
            }
        }

        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            // Only catch unhandled exceptions
            if (type == LogType.Exception)
            {
                var info = new ErrorInfo
                {
                    Title = "Unexpected Error",
                    Message = "An unexpected error occurred. The game may be unstable.",
                    Detail = $"{logString}\n\n{stackTrace}",
                    Severity = ErrorSeverity.Error,
                    Category = ErrorCategory.Unknown,
                    Timestamp = DateTime.Now
                };

                LogError(info);
            }
        }

        private void SetupButtons()
        {
            if (dismissButton != null)
                dismissButton.onClick.AddListener(DismissInternal);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetry);

            if (detailsButton != null)
                detailsButton.onClick.AddListener(OnToggleDetails);
        }

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("ErrorManager");
                Instance = go.AddComponent<ErrorManager>();
            }
        }

        #endregion

        #region UI Creation

        private void CreateUI()
        {
            // Create Canvas
            var canvasGO = new GameObject("ErrorManager_Canvas");
            canvasGO.transform.SetParent(transform);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000; // Above loading overlay

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Error Panel
            errorPanel = CreateErrorPanel(canvasGO.transform);
            canvasGroup = errorPanel.GetComponent<CanvasGroup>();

            // Toast Container
            CreateToastContainer();
        }

        private GameObject CreateErrorPanel(Transform parent)
        {
            // Background overlay
            var overlay = new GameObject("ErrorOverlay");
            overlay.transform.SetParent(parent, false);

            var overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.7f);

            var cg = overlay.AddComponent<CanvasGroup>();

            // Center panel
            var panel = new GameObject("ErrorPanel");
            panel.transform.SetParent(overlay.transform, false);

            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500, 300);

            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Vertical layout
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            // Icon (created programmatically)
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(panel.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(60, 60);
            iconImage = iconGO.AddComponent<Image>();
            iconImage.color = errorColor;

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panel.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(460, 40);
            titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "Error";
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = errorColor;

            // Message
            var messageGO = new GameObject("Message");
            messageGO.transform.SetParent(panel.transform, false);
            var messageRect = messageGO.AddComponent<RectTransform>();
            messageRect.sizeDelta = new Vector2(460, 60);
            messageText = messageGO.AddComponent<TextMeshProUGUI>();
            messageText.text = "An error occurred.";
            messageText.fontSize = 20;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.color = Color.white;

            // Detail (hidden by default)
            var detailGO = new GameObject("Detail");
            detailGO.transform.SetParent(panel.transform, false);
            var detailRect = detailGO.AddComponent<RectTransform>();
            detailRect.sizeDelta = new Vector2(460, 80);
            detailText = detailGO.AddComponent<TextMeshProUGUI>();
            detailText.fontSize = 14;
            detailText.alignment = TextAlignmentOptions.TopLeft;
            detailText.color = new Color(0.6f, 0.6f, 0.6f);
            detailGO.SetActive(false);

            // Button container
            var buttonContainer = new GameObject("Buttons");
            buttonContainer.transform.SetParent(panel.transform, false);
            var buttonRect = buttonContainer.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(460, 50);
            var buttonLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 20;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = false;
            buttonLayout.childControlHeight = false;

            // Dismiss button
            dismissButton = CreateButton(buttonContainer.transform, "OK", new Vector2(100, 40));

            // Retry button
            retryButton = CreateButton(buttonContainer.transform, "Retry", new Vector2(100, 40));
            retryButton.gameObject.SetActive(false);

            // Details button
            detailsButton = CreateButton(buttonContainer.transform, "Details", new Vector2(100, 40));

            return overlay;
        }

        private Button CreateButton(Transform parent, string text, Vector2 size)
        {
            var buttonGO = new GameObject(text + "Button");
            buttonGO.transform.SetParent(parent, false);

            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = size;

            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f);

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            var colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f);
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f);
            button.colors = colors;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return button;
        }

        private void CreateToastContainer()
        {
            var containerGO = new GameObject("ToastContainer");

            // Find or create canvas
            var canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                containerGO.transform.SetParent(canvas.transform, false);
            }
            else
            {
                containerGO.transform.SetParent(transform, false);
            }

            toastContainer = containerGO.AddComponent<RectTransform>();
            toastContainer.anchorMin = new Vector2(0.5f, 1f);
            toastContainer.anchorMax = new Vector2(0.5f, 1f);
            toastContainer.pivot = new Vector2(0.5f, 1f);
            toastContainer.anchoredPosition = new Vector2(0, -20);
            toastContainer.sizeDelta = new Vector2(400, 200);

            var layout = containerGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
        }

        private GameObject CreateToast(string message, ErrorSeverity severity)
        {
            var toast = new GameObject("Toast");
            toast.transform.SetParent(toastContainer, false);

            var rect = toast.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(380, 50);

            var image = toast.AddComponent<Image>();
            image.color = severity switch
            {
                ErrorSeverity.Info => new Color(0.2f, 0.5f, 0.8f, 0.95f),
                ErrorSeverity.Warning => new Color(0.8f, 0.6f, 0.1f, 0.95f),
                _ => new Color(0.8f, 0.2f, 0.2f, 0.95f)
            };

            var cg = toast.AddComponent<CanvasGroup>();

            // Text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(toast.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(15, 5);
            textRect.offsetMax = new Vector2(-15, -5);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            return toast;
        }

        #endregion

        #region Data Types

        public class ErrorInfo
        {
            public string Title;
            public string Message;
            public string Detail;
            public ErrorSeverity Severity;
            public ErrorCategory Category;
            public string Context;
            public Exception Exception;
            public DateTime Timestamp;
        }

        public class ErrorLog
        {
            public ErrorInfo Info;
            public DateTime Timestamp;
            public string StackTrace;
        }

        #endregion
    }
}
