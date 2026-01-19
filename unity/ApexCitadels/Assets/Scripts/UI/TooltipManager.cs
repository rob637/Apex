using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ApexCitadels.UI
{
    /// <summary>
    /// Unified tooltip system for Apex Citadels.
    /// Provides rich tooltips with title, body, icon, and smart positioning.
    /// 
    /// Usage:
    ///   TooltipManager.Show("Simple tooltip");
    ///   TooltipManager.Show("Title", "Body text with more details");
    ///   TooltipManager.ShowWithIcon("Title", "Body", someSprite);
    ///   TooltipManager.Hide();
    ///   
    /// With TooltipTrigger component:
    ///   Add TooltipTrigger to any UI element, set tooltip text in inspector.
    /// </summary>
    public class TooltipManager : MonoBehaviour
    {
        public static TooltipManager Instance { get; private set; }

        #region Serialized Fields

        [Header("Tooltip Panel")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform tooltipRect;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject iconContainer;
        [SerializeField] private TextMeshProUGUI hotkeyText;
        [SerializeField] private GameObject hotkeyContainer;

        [Header("Settings")]
        [SerializeField] private float showDelay = 0.5f;
        [SerializeField] private float fadeInDuration = 0.15f;
        [SerializeField] private float fadeOutDuration = 0.1f;
        [SerializeField] private float padding = 15f;
        [SerializeField] private float screenEdgePadding = 10f;
        [SerializeField] private bool followMouse = true;
        [SerializeField] private Vector2 offset = new Vector2(15f, -15f);

        [Header("Style")]
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color bodyColor = new Color(0.85f, 0.85f, 0.85f);
        [SerializeField] private Color hotkeyColor = new Color(0.6f, 0.8f, 1f);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        [SerializeField] private int titleFontSize = 20;
        [SerializeField] private int bodyFontSize = 16;
        [SerializeField] private int maxWidth = 350;

        #endregion

        #region Private Fields

        private bool _isVisible;
        private bool _isShowing;
        private Coroutine _showCoroutine;
        private Coroutine _fadeCoroutine;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private TooltipData _currentData;
        private TooltipData _pendingData;
        private float _pendingShowTime;

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

            if (tooltipPanel == null)
            {
                CreateTooltipUI();
            }

            // Hide at start
            SetVisible(false);
        }

        private void Update()
        {
            // Check for pending tooltip
            if (_pendingData != null && Time.unscaledTime >= _pendingShowTime)
            {
                ShowTooltipImmediate(_pendingData);
                _pendingData = null;
            }

            // Follow mouse
            if (_isVisible && followMouse)
            {
                UpdatePosition(Input.mousePosition);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Static Methods - Simple

        /// <summary>
        /// Show a simple tooltip with just text
        /// </summary>
        public static void Show(string text)
        {
            EnsureInstance();
            Instance.ShowInternal(new TooltipData { Body = text });
        }

        /// <summary>
        /// Show a tooltip with title and body
        /// </summary>
        public static void Show(string title, string body)
        {
            EnsureInstance();
            Instance.ShowInternal(new TooltipData { Title = title, Body = body });
        }

        /// <summary>
        /// Show a tooltip with title, body, and hotkey hint
        /// </summary>
        public static void Show(string title, string body, string hotkey)
        {
            EnsureInstance();
            Instance.ShowInternal(new TooltipData { Title = title, Body = body, Hotkey = hotkey });
        }

        /// <summary>
        /// Show a tooltip with icon
        /// </summary>
        public static void ShowWithIcon(string title, string body, Sprite icon)
        {
            EnsureInstance();
            Instance.ShowInternal(new TooltipData { Title = title, Body = body, Icon = icon });
        }

        /// <summary>
        /// Show a tooltip with full data
        /// </summary>
        public static void Show(TooltipData data)
        {
            EnsureInstance();
            Instance.ShowInternal(data);
        }

        /// <summary>
        /// Show tooltip immediately without delay
        /// </summary>
        public static void ShowImmediate(string title, string body)
        {
            EnsureInstance();
            Instance.ShowTooltipImmediate(new TooltipData { Title = title, Body = body });
        }

        /// <summary>
        /// Hide the tooltip
        /// </summary>
        public static void Hide()
        {
            if (Instance != null)
            {
                Instance.HideInternal();
            }
        }

        /// <summary>
        /// Update position manually (useful for drag operations)
        /// </summary>
        public static void UpdatePositionTo(Vector2 screenPosition)
        {
            Instance?.UpdatePosition(screenPosition);
        }

        #endregion

        #region Instance Methods

        private void ShowInternal(TooltipData data)
        {
            // Cancel any pending show
            _pendingData = data;
            _pendingShowTime = Time.unscaledTime + showDelay;
        }

        private void ShowTooltipImmediate(TooltipData data)
        {
            _currentData = data;
            _pendingData = null;

            // Update content
            UpdateContent(data);

            // Position tooltip
            UpdatePosition(Input.mousePosition);

            // Show with fade
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeIn());
        }

        private void HideInternal()
        {
            _pendingData = null;

            if (!_isVisible) return;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOut());
        }

        private void UpdateContent(TooltipData data)
        {
            // Title
            if (titleText != null)
            {
                bool hasTitle = !string.IsNullOrEmpty(data.Title);
                titleText.gameObject.SetActive(hasTitle);
                if (hasTitle)
                {
                    titleText.text = data.Title;
                    titleText.color = data.TitleColor ?? titleColor;
                }
            }

            // Body
            if (bodyText != null)
            {
                bool hasBody = !string.IsNullOrEmpty(data.Body);
                bodyText.gameObject.SetActive(hasBody);
                if (hasBody)
                {
                    bodyText.text = data.Body;
                    bodyText.color = data.BodyColor ?? bodyColor;
                }
            }

            // Icon
            if (iconContainer != null)
            {
                bool hasIcon = data.Icon != null;
                iconContainer.SetActive(hasIcon);
                if (hasIcon && iconImage != null)
                {
                    iconImage.sprite = data.Icon;
                }
            }

            // Hotkey
            if (hotkeyContainer != null)
            {
                bool hasHotkey = !string.IsNullOrEmpty(data.Hotkey);
                hotkeyContainer.SetActive(hasHotkey);
                if (hasHotkey && hotkeyText != null)
                {
                    hotkeyText.text = $"[{data.Hotkey}]";
                    hotkeyText.color = hotkeyColor;
                }
            }

            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        }

        private void UpdatePosition(Vector2 screenPosition)
        {
            if (tooltipRect == null || _canvas == null) return;

            // Get tooltip size
            Vector2 tooltipSize = tooltipRect.sizeDelta;

            // Calculate position with offset
            Vector2 targetPos = screenPosition + offset;

            // Convert to canvas space
            Vector2 canvasPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                targetPos,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                out canvasPos
            );

            // Get canvas bounds
            Vector2 canvasSize = _canvasRect.sizeDelta;
            float canvasScale = _canvas.scaleFactor;

            // Clamp to screen edges
            float minX = -canvasSize.x * 0.5f + screenEdgePadding + tooltipSize.x * tooltipRect.pivot.x;
            float maxX = canvasSize.x * 0.5f - screenEdgePadding - tooltipSize.x * (1f - tooltipRect.pivot.x);
            float minY = -canvasSize.y * 0.5f + screenEdgePadding + tooltipSize.y * tooltipRect.pivot.y;
            float maxY = canvasSize.y * 0.5f - screenEdgePadding - tooltipSize.y * (1f - tooltipRect.pivot.y);

            canvasPos.x = Mathf.Clamp(canvasPos.x, minX, maxX);
            canvasPos.y = Mathf.Clamp(canvasPos.y, minY, maxY);

            // Flip if would go off screen
            if (targetPos.x + tooltipSize.x > Screen.width - screenEdgePadding)
            {
                canvasPos.x -= tooltipSize.x + offset.x * 2f;
            }
            if (targetPos.y - tooltipSize.y < screenEdgePadding)
            {
                canvasPos.y += tooltipSize.y - offset.y * 2f;
            }

            tooltipRect.anchoredPosition = canvasPos;
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;

            if (tooltipPanel != null)
                tooltipPanel.SetActive(visible);

            if (canvasGroup != null)
                canvasGroup.alpha = visible ? 1f : 0f;
        }

        #endregion

        #region Animations

        private IEnumerator FadeIn()
        {
            _isShowing = true;
            _isVisible = true;

            if (tooltipPanel != null)
                tooltipPanel.SetActive(true);

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

            _isShowing = false;
        }

        private IEnumerator FadeOut()
        {
            _isShowing = true;

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

            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);

            _isVisible = false;
            _isShowing = false;
        }

        #endregion

        #region UI Creation

        private void CreateTooltipUI()
        {
            // Create Canvas
            var canvasGO = new GameObject("TooltipManager_Canvas");
            canvasGO.transform.SetParent(transform);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10001; // Above everything

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            _canvasRect = _canvas.GetComponent<RectTransform>();

            // Tooltip panel
            tooltipPanel = new GameObject("TooltipPanel");
            tooltipPanel.transform.SetParent(canvasGO.transform, false);

            tooltipRect = tooltipPanel.AddComponent<RectTransform>();
            tooltipRect.pivot = new Vector2(0, 1); // Top-left pivot
            tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);

            var panelImage = tooltipPanel.AddComponent<Image>();
            panelImage.color = backgroundColor;

            canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;

            // Layout
            var verticalLayout = tooltipPanel.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            verticalLayout.spacing = 5;
            verticalLayout.childAlignment = TextAnchor.UpperLeft;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = true;
            verticalLayout.childForceExpandWidth = false;
            verticalLayout.childForceExpandHeight = false;

            var sizeFitter = tooltipPanel.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header (icon + title)
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(tooltipPanel.transform, false);

            var headerLayout = headerGO.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 10;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = false;
            headerLayout.childControlHeight = false;

            // Icon container
            iconContainer = new GameObject("IconContainer");
            iconContainer.transform.SetParent(headerGO.transform, false);
            var iconRect = iconContainer.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(32, 32);
            var iconLayoutElem = iconContainer.AddComponent<LayoutElement>();
            iconLayoutElem.preferredWidth = 32;
            iconLayoutElem.preferredHeight = 32;

            iconImage = iconContainer.AddComponent<Image>();
            iconContainer.SetActive(false);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(headerGO.transform, false);

            var titleRect = titleGO.AddComponent<RectTransform>();
            titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "Title";
            titleText.fontSize = titleFontSize;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = titleColor;

            var titleLayoutElem = titleGO.AddComponent<LayoutElement>();
            titleLayoutElem.preferredWidth = maxWidth - 60;

            // Body
            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(tooltipPanel.transform, false);

            bodyText = bodyGO.AddComponent<TextMeshProUGUI>();
            bodyText.text = "Body text";
            bodyText.fontSize = bodyFontSize;
            bodyText.color = bodyColor;
            bodyText.textWrappingMode = TMPro.TextWrappingModes.Normal; //  = true;

            var bodyLayoutElem = bodyGO.AddComponent<LayoutElement>();
            bodyLayoutElem.preferredWidth = maxWidth;

            // Hotkey container
            hotkeyContainer = new GameObject("HotkeyContainer");
            hotkeyContainer.transform.SetParent(tooltipPanel.transform, false);

            hotkeyText = hotkeyContainer.AddComponent<TextMeshProUGUI>();
            hotkeyText.text = "[Hotkey]";
            hotkeyText.fontSize = bodyFontSize - 2;
            hotkeyText.fontStyle = FontStyles.Italic;
            hotkeyText.color = hotkeyColor;
            hotkeyText.alignment = TextAlignmentOptions.Right;
            hotkeyContainer.SetActive(false);
        }

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("TooltipManager");
                Instance = go.AddComponent<TooltipManager>();
            }
        }

        #endregion
    }

    #region Data Types

    /// <summary>
    /// Data structure for tooltip content
    /// </summary>
    [Serializable]
    public class TooltipData
    {
        public string Title;
        public string Body;
        public string Hotkey;
        public Sprite Icon;
        public Color? TitleColor;
        public Color? BodyColor;
    }

    #endregion

    #region Tooltip Trigger Component

    /// <summary>
    /// Add this component to any UI element to show a tooltip on hover
    /// </summary>
    [AddComponentMenu("Apex Citadels/UI/Tooltip Trigger")]
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Content")]
        [SerializeField] private string title;
        [TextArea(2, 5)]
        [SerializeField] private string body;
        [SerializeField] private string hotkey;
        [SerializeField] private Sprite icon;

        [Header("Settings")]
        [SerializeField] private bool useCustomDelay;
        [SerializeField] private float customDelay = 0.5f;

        public string Title
        {
            get => title;
            set => title = value;
        }

        public string Body
        {
            get => body;
            set => body = value;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            TooltipManager.Show(new TooltipData
            {
                Title = title,
                Body = body,
                Hotkey = hotkey,
                Icon = icon
            });
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipManager.Hide();
        }

        private void OnDisable()
        {
            TooltipManager.Hide();
        }
    }

    #endregion

    #region Extension Methods

    /// <summary>
    /// Extension methods for adding tooltips to UI elements
    /// </summary>
    public static class TooltipExtensions
    {
        /// <summary>
        /// Add a simple tooltip to a GameObject
        /// </summary>
        public static TooltipTrigger AddTooltip(this GameObject go, string text)
        {
            var trigger = go.GetComponent<TooltipTrigger>() ?? go.AddComponent<TooltipTrigger>();
            trigger.Body = text;
            return trigger;
        }

        /// <summary>
        /// Add a tooltip with title and body to a GameObject
        /// </summary>
        public static TooltipTrigger AddTooltip(this GameObject go, string title, string body)
        {
            var trigger = go.GetComponent<TooltipTrigger>() ?? go.AddComponent<TooltipTrigger>();
            trigger.Title = title;
            trigger.Body = body;
            return trigger;
        }

        /// <summary>
        /// Add a simple tooltip to a Button
        /// </summary>
        public static TooltipTrigger AddTooltip(this Button button, string text)
        {
            return button.gameObject.AddTooltip(text);
        }

        /// <summary>
        /// Add a tooltip with title and body to a Button
        /// </summary>
        public static TooltipTrigger AddTooltip(this Button button, string title, string body)
        {
            return button.gameObject.AddTooltip(title, body);
        }
    }

    #endregion
}
