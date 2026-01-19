using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.UI
{
    /// <summary>
    /// Reusable loading overlay system for async operations.
    /// Shows spinner, progress bar, and status messages during loading.
    /// 
    /// Usage:
    ///   LoadingOverlay.Show("Loading...");
    ///   LoadingOverlay.SetProgress(0.5f, "Loading assets...");
    ///   LoadingOverlay.Hide();
    ///   
    /// Or with auto-hide:
    ///   using (LoadingOverlay.ShowScoped("Loading..."))
    ///   {
    ///       // do async work
    ///   }
    /// </summary>
    public class LoadingOverlay : MonoBehaviour
    {
        public static LoadingOverlay Instance { get; private set; }

        #region Serialized Fields

        [Header("Container")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject overlayPanel;

        [Header("Spinner")]
        [SerializeField] private RectTransform spinnerTransform;
        [SerializeField] private Image spinnerImage;
        [SerializeField] private float spinnerSpeed = 360f;

        [Header("Progress Bar")]
        [SerializeField] private GameObject progressBarContainer;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private Image progressBarBackground;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI detailText;

        [Header("Settings")]
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.15f;
        [SerializeField] private float minimumDisplayTime = 0.5f;
        [SerializeField] private bool blockInput = true;

        [Header("Styles")]
        [SerializeField] private Color spinnerColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color progressBarColor = new Color(0.2f, 0.8f, 0.4f, 1f);
        [SerializeField] private Color backgroundTint = new Color(0f, 0f, 0f, 0.7f);

        #endregion

        #region Private Fields

        private bool _isVisible;
        private bool _isAnimating;
        private float _showTime;
        private float _currentProgress;
        private float _targetProgress;
        private Coroutine _fadeCoroutine;
        private Coroutine _progressCoroutine;
        private Queue<LoadingTask> _taskQueue = new Queue<LoadingTask>();
        private LoadingTask _currentTask;

        #endregion

        #region Public Properties

        public bool IsVisible => _isVisible;
        public float Progress => _currentProgress;

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
            if (overlayPanel == null)
            {
                CreateUI();
            }

            // Start hidden
            SetVisibleImmediate(false);
        }

        private void Update()
        {
            if (!_isVisible) return;

            // Rotate spinner
            if (spinnerTransform != null)
            {
                spinnerTransform.Rotate(0, 0, -spinnerSpeed * Time.unscaledDeltaTime);
            }

            // Smooth progress interpolation
            if (Mathf.Abs(_currentProgress - _targetProgress) > 0.001f)
            {
                _currentProgress = Mathf.Lerp(_currentProgress, _targetProgress, Time.unscaledDeltaTime * 8f);
                UpdateProgressVisuals();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Show the loading overlay with a message
        /// </summary>
        public static void Show(string message = "Loading...", bool showProgressBar = false)
        {
            EnsureInstance();
            Instance.ShowInternal(message, showProgressBar);
        }

        /// <summary>
        /// Show loading overlay and return a disposable scope that hides on dispose
        /// </summary>
        public static LoadingScope ShowScoped(string message = "Loading...", bool showProgressBar = false)
        {
            Show(message, showProgressBar);
            return new LoadingScope();
        }

        /// <summary>
        /// Hide the loading overlay
        /// </summary>
        public static void Hide()
        {
            if (Instance != null)
            {
                Instance.HideInternal();
            }
        }

        /// <summary>
        /// Update the progress bar (0-1)
        /// </summary>
        public static void SetProgress(float progress, string message = null)
        {
            if (Instance != null)
            {
                Instance.SetProgressInternal(progress, message);
            }
        }

        /// <summary>
        /// Update the detail text (smaller text below main message)
        /// </summary>
        public static void SetDetail(string detail)
        {
            if (Instance != null)
            {
                Instance.SetDetailInternal(detail);
            }
        }

        /// <summary>
        /// Queue a loading task
        /// </summary>
        public static void QueueTask(string name, Func<IEnumerator> taskCoroutine)
        {
            EnsureInstance();
            Instance.QueueTaskInternal(name, taskCoroutine);
        }

        /// <summary>
        /// Execute all queued tasks with progress tracking
        /// </summary>
        public static void ExecuteQueue(Action onComplete = null)
        {
            if (Instance != null)
            {
                Instance.StartCoroutine(Instance.ExecuteQueueCoroutine(onComplete));
            }
        }

        #endregion

        #region Instance Methods

        private void ShowInternal(string message, bool showProgressBar)
        {
            _showTime = Time.unscaledTime;
            _currentProgress = 0f;
            _targetProgress = 0f;

            // Update text
            if (messageText != null)
                messageText.text = message;

            if (detailText != null)
                detailText.text = "";

            // Show/hide progress bar
            if (progressBarContainer != null)
                progressBarContainer.SetActive(showProgressBar);

            UpdateProgressVisuals();

            // Fade in
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeIn());
        }

        private void HideInternal()
        {
            // Ensure minimum display time
            float elapsed = Time.unscaledTime - _showTime;
            float delay = Mathf.Max(0, minimumDisplayTime - elapsed);

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOut(delay));
        }

        private void SetProgressInternal(float progress, string message)
        {
            _targetProgress = Mathf.Clamp01(progress);

            if (message != null && messageText != null)
                messageText.text = message;

            // Enable progress bar if not visible
            if (progressBarContainer != null && !progressBarContainer.activeSelf)
                progressBarContainer.SetActive(true);
        }

        private void SetDetailInternal(string detail)
        {
            if (detailText != null)
                detailText.text = detail;
        }

        private void UpdateProgressVisuals()
        {
            if (progressBarFill != null)
                progressBarFill.fillAmount = _currentProgress;

            if (progressText != null)
                progressText.text = $"{Mathf.RoundToInt(_currentProgress * 100)}%";
        }

        private void QueueTaskInternal(string name, Func<IEnumerator> taskCoroutine)
        {
            _taskQueue.Enqueue(new LoadingTask { Name = name, Coroutine = taskCoroutine });
        }

        private IEnumerator ExecuteQueueCoroutine(Action onComplete)
        {
            if (_taskQueue.Count == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            Show("Loading...", true);

            int totalTasks = _taskQueue.Count;
            int completedTasks = 0;

            while (_taskQueue.Count > 0)
            {
                _currentTask = _taskQueue.Dequeue();

                // Update message
                if (messageText != null)
                    messageText.text = _currentTask.Name;

                // Execute task
                yield return StartCoroutine(_currentTask.Coroutine());

                completedTasks++;
                SetProgress((float)completedTasks / totalTasks);
            }

            _currentTask = null;
            Hide();
            onComplete?.Invoke();
        }

        private void SetVisibleImmediate(bool visible)
        {
            _isVisible = visible;

            if (overlayPanel != null)
                overlayPanel.SetActive(visible);

            if (canvasGroup != null)
                canvasGroup.alpha = visible ? 1f : 0f;
        }

        #endregion

        #region Animations

        private IEnumerator FadeIn()
        {
            _isAnimating = true;
            _isVisible = true;

            if (overlayPanel != null)
                overlayPanel.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                float elapsed = 0f;

                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                    yield return null;
                }

                canvasGroup.alpha = 1f;
            }

            if (blockInput && canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            _isAnimating = false;
        }

        private IEnumerator FadeOut(float delay)
        {
            if (delay > 0)
                yield return new WaitForSecondsRealtime(delay);

            _isAnimating = true;

            if (blockInput && canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (canvasGroup != null)
            {
                float startAlpha = canvasGroup.alpha;
                float elapsed = 0f;

                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                    yield return null;
                }

                canvasGroup.alpha = 0f;
            }

            if (overlayPanel != null)
                overlayPanel.SetActive(false);

            _isVisible = false;
            _isAnimating = false;
        }

        #endregion

        #region UI Creation

        private void CreateUI()
        {
            // Create Canvas
            var canvasGO = new GameObject("LoadingOverlay_Canvas");
            canvasGO.transform.SetParent(transform);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // Always on top

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Overlay panel (background)
            overlayPanel = new GameObject("OverlayPanel");
            overlayPanel.transform.SetParent(canvasGO.transform, false);

            var overlayRect = overlayPanel.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            var overlayImage = overlayPanel.AddComponent<Image>();
            overlayImage.color = backgroundTint;

            canvasGroup = overlayPanel.AddComponent<CanvasGroup>();

            // Center container
            var centerGO = new GameObject("CenterContainer");
            centerGO.transform.SetParent(overlayPanel.transform, false);

            var centerRect = centerGO.AddComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.sizeDelta = new Vector2(400, 200);

            var verticalLayout = centerGO.AddComponent<VerticalLayoutGroup>();
            verticalLayout.childAlignment = TextAnchor.MiddleCenter;
            verticalLayout.spacing = 20;
            verticalLayout.childControlWidth = false;
            verticalLayout.childControlHeight = false;

            // Spinner
            var spinnerGO = new GameObject("Spinner");
            spinnerGO.transform.SetParent(centerGO.transform, false);

            spinnerTransform = spinnerGO.AddComponent<RectTransform>();
            spinnerTransform.sizeDelta = new Vector2(80, 80);

            spinnerImage = spinnerGO.AddComponent<Image>();
            spinnerImage.color = spinnerColor;

            // Create spinner sprite (simple circle with gap)
            CreateSpinnerSprite();

            // Message text
            var messageGO = new GameObject("MessageText");
            messageGO.transform.SetParent(centerGO.transform, false);

            var messageRect = messageGO.AddComponent<RectTransform>();
            messageRect.sizeDelta = new Vector2(400, 40);

            messageText = messageGO.AddComponent<TextMeshProUGUI>();
            messageText.text = "Loading...";
            messageText.fontSize = 28;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.color = Color.white;

            // Progress bar container
            progressBarContainer = new GameObject("ProgressBarContainer");
            progressBarContainer.transform.SetParent(centerGO.transform, false);

            var progressRect = progressBarContainer.AddComponent<RectTransform>();
            progressRect.sizeDelta = new Vector2(300, 20);

            // Progress bar background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(progressBarContainer.transform, false);

            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            progressBarBackground = bgGO.AddComponent<Image>();
            progressBarBackground.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Progress bar fill
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(progressBarContainer.transform, false);

            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            progressBarFill = fillGO.AddComponent<Image>();
            progressBarFill.color = progressBarColor;
            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            progressBarFill.fillOrigin = 0;
            progressBarFill.fillAmount = 0f;

            // Progress text
            var progressTextGO = new GameObject("ProgressText");
            progressTextGO.transform.SetParent(progressBarContainer.transform, false);

            var progressTextRect = progressTextGO.AddComponent<RectTransform>();
            progressTextRect.anchorMin = Vector2.zero;
            progressTextRect.anchorMax = Vector2.one;
            progressTextRect.sizeDelta = Vector2.zero;

            progressText = progressTextGO.AddComponent<TextMeshProUGUI>();
            progressText.text = "0%";
            progressText.fontSize = 16;
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.color = Color.white;

            // Detail text
            var detailGO = new GameObject("DetailText");
            detailGO.transform.SetParent(centerGO.transform, false);

            var detailRect = detailGO.AddComponent<RectTransform>();
            detailRect.sizeDelta = new Vector2(400, 30);

            detailText = detailGO.AddComponent<TextMeshProUGUI>();
            detailText.text = "";
            detailText.fontSize = 18;
            detailText.alignment = TextAlignmentOptions.Center;
            detailText.color = new Color(0.7f, 0.7f, 0.7f, 1f);

            progressBarContainer.SetActive(false);
        }

        private void CreateSpinnerSprite()
        {
            // Create a simple arc texture for the spinner
            int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size / 2f, size / 2f);
            float outerRadius = size / 2f - 4;
            float innerRadius = size / 2f - 16;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var pos = new Vector2(x, y);
                    float dist = Vector2.Distance(pos, center);
                    float angle = Mathf.Atan2(pos.y - center.y, pos.x - center.x) * Mathf.Rad2Deg;

                    // Create arc (270 degrees)
                    bool inRing = dist >= innerRadius && dist <= outerRadius;
                    bool inArc = angle > -135 || angle < 135; // Leave 90-degree gap

                    if (inRing && inArc)
                    {
                        // Anti-aliasing
                        float outerAA = Mathf.Clamp01(outerRadius - dist + 1);
                        float innerAA = Mathf.Clamp01(dist - innerRadius + 1);
                        float alpha = Mathf.Min(outerAA, innerAA);

                        texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            spinnerImage.sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("LoadingOverlay");
                Instance = go.AddComponent<LoadingOverlay>();
            }
        }

        #endregion

        #region Helper Types

        private class LoadingTask
        {
            public string Name;
            public Func<IEnumerator> Coroutine;
        }

        /// <summary>
        /// Disposable scope for using statement pattern
        /// </summary>
        public class LoadingScope : IDisposable
        {
            public void Dispose()
            {
                Hide();
            }
        }

        #endregion
    }

    #region Extension Methods

    /// <summary>
    /// Extension methods for common async patterns with loading overlay
    /// </summary>
    public static class LoadingOverlayExtensions
    {
        /// <summary>
        /// Show loading overlay during an async operation
        /// </summary>
        public static IEnumerator WithLoading(this IEnumerator coroutine, string message = "Loading...")
        {
            LoadingOverlay.Show(message);
            yield return coroutine;
            LoadingOverlay.Hide();
        }

        /// <summary>
        /// Show loading overlay with progress during an async operation
        /// </summary>
        public static IEnumerator WithLoadingProgress(this IEnumerator coroutine, string message = "Loading...")
        {
            LoadingOverlay.Show(message, true);
            yield return coroutine;
            LoadingOverlay.Hide();
        }
    }

    #endregion
}
