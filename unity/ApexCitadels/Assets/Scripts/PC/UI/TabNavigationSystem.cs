using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Manages tab-based UI navigation with animated transitions.
    /// Features:
    /// - Horizontal and vertical tab layouts
    /// - Tab indicator animation
    /// - Content panel transitions
    /// - Keyboard navigation
    /// - Badge notifications per tab
    /// </summary>
    public class TabNavigationSystem : MonoBehaviour
    {
        [Header("Tab Container")]
        [SerializeField] private RectTransform tabContainer;
        [SerializeField] private RectTransform contentContainer;
        [SerializeField] private RectTransform tabIndicator;
        
        [Header("Tab Settings")]
        [SerializeField] private TabLayout layout = TabLayout.Horizontal;
        [SerializeField] private float tabSpacing = 5f;
        [SerializeField] private float indicatorTransitionDuration = 0.25f;
        [SerializeField] private float contentTransitionDuration = 0.3f;
        
        [Header("Tab Styling")]
        [SerializeField] private Color activeTabColor = new Color(1, 1, 1, 1);
        [SerializeField] private Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 1);
        [SerializeField] private Color hoverTabColor = new Color(0.85f, 0.85f, 0.85f, 1);
        [SerializeField] private float activeTabScale = 1.05f;
        
        [Header("Content Transitions")]
        [SerializeField] private ContentTransition transitionType = ContentTransition.Fade;
        [SerializeField] private AnimationCurve transitionCurve;
        
        // State
        private List<TabInstance> _tabs = new List<TabInstance>();
        private int _currentTabIndex = -1;
        private Coroutine _indicatorAnimation;
        private Coroutine _contentAnimation;
        
        // Events
        public event Action<int, int> OnTabChanged;
        public event Action<TabInstance> OnTabHovered;
        
        private void Awake()
        {
            InitializeTransitionCurve();
        }
        
        private void InitializeTransitionCurve()
        {
            if (transitionCurve == null || transitionCurve.length == 0)
            {
                transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
        }
        
        private void Update()
        {
            HandleKeyboardNavigation();
        }
        
        #region Public API
        
        /// <summary>
        /// Initialize tabs with configuration
        /// </summary>
        public void Initialize(List<TabConfig> tabConfigs)
        {
            ClearTabs();
            
            foreach (var config in tabConfigs)
            {
                CreateTab(config);
            }
            
            // Select first tab
            if (_tabs.Count > 0)
            {
                SelectTab(0, false);
            }
        }
        
        /// <summary>
        /// Add a new tab
        /// </summary>
        public void AddTab(TabConfig config)
        {
            CreateTab(config);
        }
        
        /// <summary>
        /// Remove tab by index
        /// </summary>
        public void RemoveTab(int index)
        {
            if (index < 0 || index >= _tabs.Count) return;
            
            var tab = _tabs[index];
            Destroy(tab.tabButton.gameObject);
            _tabs.RemoveAt(index);
            
            // Adjust current index
            if (_currentTabIndex >= _tabs.Count)
            {
                SelectTab(_tabs.Count - 1, true);
            }
            else if (index < _currentTabIndex)
            {
                _currentTabIndex--;
            }
        }
        
        /// <summary>
        /// Select tab by index
        /// </summary>
        public void SelectTab(int index, bool animate = true)
        {
            if (index < 0 || index >= _tabs.Count) return;
            if (index == _currentTabIndex) return;
            
            int previousIndex = _currentTabIndex;
            _currentTabIndex = index;
            
            // Update tab visuals
            UpdateTabVisuals();
            
            // Animate indicator
            if (tabIndicator != null && animate)
            {
                AnimateIndicator(_tabs[index]);
            }
            else if (tabIndicator != null)
            {
                PositionIndicator(_tabs[index]);
            }
            
            // Transition content
            TransitionContent(previousIndex, index, animate);
            
            OnTabChanged?.Invoke(previousIndex, index);
        }
        
        /// <summary>
        /// Select tab by ID
        /// </summary>
        public void SelectTab(string tabId, bool animate = true)
        {
            int index = _tabs.FindIndex(t => t.config.id == tabId);
            if (index >= 0)
            {
                SelectTab(index, animate);
            }
        }
        
        /// <summary>
        /// Get current tab index
        /// </summary>
        public int CurrentTabIndex => _currentTabIndex;
        
        /// <summary>
        /// Get current tab
        /// </summary>
        public TabInstance CurrentTab => _currentTabIndex >= 0 && _currentTabIndex < _tabs.Count ? 
            _tabs[_currentTabIndex] : null;
        
        /// <summary>
        /// Set badge on tab
        /// </summary>
        public void SetBadge(int tabIndex, string text, bool visible = true)
        {
            if (tabIndex < 0 || tabIndex >= _tabs.Count) return;
            
            var tab = _tabs[tabIndex];
            if (tab.badge != null)
            {
                tab.badge.gameObject.SetActive(visible && !string.IsNullOrEmpty(text));
                
                var badgeText = tab.badge.GetComponentInChildren<TextMeshProUGUI>();
                if (badgeText != null)
                {
                    badgeText.text = text;
                }
            }
        }
        
        /// <summary>
        /// Set badge by tab ID
        /// </summary>
        public void SetBadge(string tabId, string text, bool visible = true)
        {
            int index = _tabs.FindIndex(t => t.config.id == tabId);
            if (index >= 0)
            {
                SetBadge(index, text, visible);
            }
        }
        
        /// <summary>
        /// Enable/disable a tab
        /// </summary>
        public void SetTabEnabled(int tabIndex, bool enabled)
        {
            if (tabIndex < 0 || tabIndex >= _tabs.Count) return;
            
            var tab = _tabs[tabIndex];
            tab.isEnabled = enabled;
            tab.tabButton.interactable = enabled;
            
            if (tab.tabText != null)
            {
                tab.tabText.color = enabled ? inactiveTabColor : new Color(0.4f, 0.4f, 0.4f);
            }
        }
        
        /// <summary>
        /// Get all tabs
        /// </summary>
        public List<TabInstance> GetTabs() => new List<TabInstance>(_tabs);
        
        #endregion
        
        #region Tab Creation
        
        private void CreateTab(TabConfig config)
        {
            // Create tab button
            var tabGo = new GameObject($"Tab_{config.id}");
            tabGo.transform.SetParent(tabContainer, false);
            
            var tabRect = tabGo.AddComponent<RectTransform>();
            var tabImage = tabGo.AddComponent<Image>();
            var tabButton = tabGo.AddComponent<Button>();
            
            tabImage.color = inactiveTabColor;
            
            // Create text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(tabGo.transform, false);
            
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = config.label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 14;
            text.color = inactiveTabColor;
            
            // Create icon if provided
            Image icon = null;
            if (config.icon != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(tabGo.transform, false);
                
                var iconRect = iconGo.AddComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(24, 24);
                iconRect.anchoredPosition = new Vector2(-40, 0);
                
                icon = iconGo.AddComponent<Image>();
                icon.sprite = config.icon;
                icon.preserveAspect = true;
            }
            
            // Create badge
            RectTransform badge = null;
            if (config.showBadge)
            {
                var badgeGo = new GameObject("Badge");
                badgeGo.transform.SetParent(tabGo.transform, false);
                
                badge = badgeGo.AddComponent<RectTransform>();
                badge.anchorMin = new Vector2(1, 1);
                badge.anchorMax = new Vector2(1, 1);
                badge.pivot = new Vector2(0.5f, 0.5f);
                badge.anchoredPosition = new Vector2(-5, -5);
                badge.sizeDelta = new Vector2(20, 20);
                
                var badgeImage = badgeGo.AddComponent<Image>();
                badgeImage.color = new Color(0.9f, 0.2f, 0.2f);
                
                var badgeTextGo = new GameObject("BadgeText");
                badgeTextGo.transform.SetParent(badgeGo.transform, false);
                
                var badgeTextRect = badgeTextGo.AddComponent<RectTransform>();
                badgeTextRect.anchorMin = Vector2.zero;
                badgeTextRect.anchorMax = Vector2.one;
                badgeTextRect.offsetMin = Vector2.zero;
                badgeTextRect.offsetMax = Vector2.zero;
                
                var badgeText = badgeTextGo.AddComponent<TextMeshProUGUI>();
                badgeText.alignment = TextAlignmentOptions.Center;
                badgeText.fontSize = 10;
                
                badgeGo.SetActive(false);
            }
            
            // Create tab instance
            var tabInstance = new TabInstance
            {
                config = config,
                tabRect = tabRect,
                tabButton = tabButton,
                tabImage = tabImage,
                tabText = text,
                tabIcon = icon,
                badge = badge,
                contentPanel = config.contentPanel,
                isEnabled = true
            };
            
            // Setup button click
            int tabIndex = _tabs.Count;
            tabButton.onClick.AddListener(() => SelectTab(tabIndex, true));
            
            // Setup hover events
            var eventTrigger = tabGo.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            enterEntry.callback.AddListener((e) => OnTabHover(tabInstance, true));
            eventTrigger.triggers.Add(enterEntry);
            
            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
            };
            exitEntry.callback.AddListener((e) => OnTabHover(tabInstance, false));
            eventTrigger.triggers.Add(exitEntry);
            
            _tabs.Add(tabInstance);
            
            // Layout
            LayoutTabs();
        }
        
        private void ClearTabs()
        {
            foreach (var tab in _tabs)
            {
                if (tab.tabButton != null)
                {
                    Destroy(tab.tabButton.gameObject);
                }
            }
            _tabs.Clear();
            _currentTabIndex = -1;
        }
        
        private void LayoutTabs()
        {
            float offset = 0;
            
            foreach (var tab in _tabs)
            {
                if (layout == TabLayout.Horizontal)
                {
                    tab.tabRect.anchorMin = new Vector2(0, 0);
                    tab.tabRect.anchorMax = new Vector2(0, 1);
                    tab.tabRect.pivot = new Vector2(0, 0.5f);
                    tab.tabRect.anchoredPosition = new Vector2(offset, 0);
                    tab.tabRect.sizeDelta = new Vector2(tab.config.width, 0);
                    
                    offset += tab.config.width + tabSpacing;
                }
                else
                {
                    tab.tabRect.anchorMin = new Vector2(0, 1);
                    tab.tabRect.anchorMax = new Vector2(1, 1);
                    tab.tabRect.pivot = new Vector2(0.5f, 1);
                    tab.tabRect.anchoredPosition = new Vector2(0, -offset);
                    tab.tabRect.sizeDelta = new Vector2(0, tab.config.height);
                    
                    offset += tab.config.height + tabSpacing;
                }
            }
        }
        
        #endregion
        
        #region Tab Visuals
        
        private void UpdateTabVisuals()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                var tab = _tabs[i];
                bool isActive = i == _currentTabIndex;
                
                // Update colors
                Color targetColor = isActive ? activeTabColor : inactiveTabColor;
                if (tab.tabText != null) tab.tabText.color = targetColor;
                if (tab.tabIcon != null) tab.tabIcon.color = targetColor;
                
                // Update scale
                float targetScale = isActive ? activeTabScale : 1f;
                tab.tabRect.localScale = Vector3.one * targetScale;
            }
        }
        
        private void OnTabHover(TabInstance tab, bool isHovered)
        {
            if (_tabs.IndexOf(tab) == _currentTabIndex) return;
            if (!tab.isEnabled) return;
            
            Color targetColor = isHovered ? hoverTabColor : inactiveTabColor;
            if (tab.tabText != null) tab.tabText.color = targetColor;
            if (tab.tabIcon != null) tab.tabIcon.color = targetColor;
            
            if (isHovered)
            {
                OnTabHovered?.Invoke(tab);
            }
        }
        
        #endregion
        
        #region Indicator Animation
        
        private void AnimateIndicator(TabInstance targetTab)
        {
            if (_indicatorAnimation != null)
            {
                StopCoroutine(_indicatorAnimation);
            }
            
            _indicatorAnimation = StartCoroutine(AnimateIndicatorCoroutine(targetTab));
        }
        
        private IEnumerator AnimateIndicatorCoroutine(TabInstance targetTab)
        {
            Vector2 startPos = tabIndicator.anchoredPosition;
            Vector2 startSize = tabIndicator.sizeDelta;
            
            Vector2 targetPos;
            Vector2 targetSize;
            
            if (layout == TabLayout.Horizontal)
            {
                targetPos = new Vector2(targetTab.tabRect.anchoredPosition.x + targetTab.config.width * 0.5f, 
                    tabIndicator.anchoredPosition.y);
                targetSize = new Vector2(targetTab.config.width, tabIndicator.sizeDelta.y);
            }
            else
            {
                targetPos = new Vector2(tabIndicator.anchoredPosition.x, 
                    targetTab.tabRect.anchoredPosition.y - targetTab.config.height * 0.5f);
                targetSize = new Vector2(tabIndicator.sizeDelta.x, targetTab.config.height);
            }
            
            float elapsed = 0;
            while (elapsed < indicatorTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = transitionCurve.Evaluate(elapsed / indicatorTransitionDuration);
                
                tabIndicator.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                tabIndicator.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
                
                yield return null;
            }
            
            tabIndicator.anchoredPosition = targetPos;
            tabIndicator.sizeDelta = targetSize;
        }
        
        private void PositionIndicator(TabInstance targetTab)
        {
            if (layout == TabLayout.Horizontal)
            {
                tabIndicator.anchoredPosition = new Vector2(
                    targetTab.tabRect.anchoredPosition.x + targetTab.config.width * 0.5f,
                    tabIndicator.anchoredPosition.y);
                tabIndicator.sizeDelta = new Vector2(targetTab.config.width, tabIndicator.sizeDelta.y);
            }
            else
            {
                tabIndicator.anchoredPosition = new Vector2(
                    tabIndicator.anchoredPosition.x,
                    targetTab.tabRect.anchoredPosition.y - targetTab.config.height * 0.5f);
                tabIndicator.sizeDelta = new Vector2(tabIndicator.sizeDelta.x, targetTab.config.height);
            }
        }
        
        #endregion
        
        #region Content Transitions
        
        private void TransitionContent(int fromIndex, int toIndex, bool animate)
        {
            if (_contentAnimation != null)
            {
                StopCoroutine(_contentAnimation);
            }
            
            RectTransform fromContent = fromIndex >= 0 && fromIndex < _tabs.Count ? 
                _tabs[fromIndex].contentPanel : null;
            RectTransform toContent = toIndex >= 0 && toIndex < _tabs.Count ? 
                _tabs[toIndex].contentPanel : null;
            
            if (!animate)
            {
                if (fromContent != null) fromContent.gameObject.SetActive(false);
                if (toContent != null) toContent.gameObject.SetActive(true);
                return;
            }
            
            _contentAnimation = StartCoroutine(AnimateContentTransition(fromContent, toContent, fromIndex < toIndex));
        }
        
        private IEnumerator AnimateContentTransition(RectTransform fromContent, RectTransform toContent, bool slideRight)
        {
            CanvasGroup fromGroup = null;
            CanvasGroup toGroup = null;
            
            if (fromContent != null)
            {
                fromGroup = fromContent.GetComponent<CanvasGroup>() ?? fromContent.gameObject.AddComponent<CanvasGroup>();
            }
            
            if (toContent != null)
            {
                toContent.gameObject.SetActive(true);
                toGroup = toContent.GetComponent<CanvasGroup>() ?? toContent.gameObject.AddComponent<CanvasGroup>();
                toGroup.alpha = 0;
            }
            
            float elapsed = 0;
            
            // Setup positions for slide
            Vector2 fromStartPos = fromContent != null ? fromContent.anchoredPosition : Vector2.zero;
            Vector2 toStartPos = toContent != null ? toContent.anchoredPosition : Vector2.zero;
            
            float slideDistance = 50f;
            Vector2 slideOffset = layout == TabLayout.Horizontal ? 
                new Vector2(slideRight ? -slideDistance : slideDistance, 0) :
                new Vector2(0, slideRight ? slideDistance : -slideDistance);
            
            switch (transitionType)
            {
                case ContentTransition.Fade:
                    while (elapsed < contentTransitionDuration)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float t = transitionCurve.Evaluate(elapsed / contentTransitionDuration);
                        
                        if (fromGroup != null) fromGroup.alpha = 1 - t;
                        if (toGroup != null) toGroup.alpha = t;
                        
                        yield return null;
                    }
                    break;
                    
                case ContentTransition.Slide:
                    if (toContent != null) toContent.anchoredPosition = toStartPos - slideOffset;
                    
                    while (elapsed < contentTransitionDuration)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float t = transitionCurve.Evaluate(elapsed / contentTransitionDuration);
                        
                        if (fromContent != null)
                        {
                            fromContent.anchoredPosition = fromStartPos + slideOffset * t;
                        }
                        if (toContent != null)
                        {
                            toContent.anchoredPosition = Vector2.Lerp(toStartPos - slideOffset, toStartPos, t);
                        }
                        if (fromGroup != null) fromGroup.alpha = 1 - t;
                        if (toGroup != null) toGroup.alpha = t;
                        
                        yield return null;
                    }
                    
                    if (fromContent != null) fromContent.anchoredPosition = fromStartPos;
                    if (toContent != null) toContent.anchoredPosition = toStartPos;
                    break;
                    
                case ContentTransition.Scale:
                    if (toContent != null) toContent.localScale = Vector3.one * 0.9f;
                    
                    while (elapsed < contentTransitionDuration)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float t = transitionCurve.Evaluate(elapsed / contentTransitionDuration);
                        
                        if (fromContent != null)
                        {
                            fromContent.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.9f, t);
                        }
                        if (toContent != null)
                        {
                            toContent.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one, t);
                        }
                        if (fromGroup != null) fromGroup.alpha = 1 - t;
                        if (toGroup != null) toGroup.alpha = t;
                        
                        yield return null;
                    }
                    
                    if (fromContent != null) fromContent.localScale = Vector3.one;
                    if (toContent != null) toContent.localScale = Vector3.one;
                    break;
            }
            
            // Finalize
            if (fromGroup != null) fromGroup.alpha = 1;
            if (toGroup != null) toGroup.alpha = 1;
            if (fromContent != null) fromContent.gameObject.SetActive(false);
        }
        
        #endregion
        
        #region Keyboard Navigation
        
        private void HandleKeyboardNavigation()
        {
            if (_tabs.Count == 0) return;
            
            // Tab key navigation (Ctrl+Tab / Ctrl+Shift+Tab)
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        // Previous tab
                        int prevIndex = _currentTabIndex - 1;
                        if (prevIndex < 0) prevIndex = _tabs.Count - 1;
                        SelectTab(prevIndex, true);
                    }
                    else
                    {
                        // Next tab
                        int nextIndex = (_currentTabIndex + 1) % _tabs.Count;
                        SelectTab(nextIndex, true);
                    }
                }
            }
            
            // Number keys (1-9) for direct tab access
            for (int i = 0; i < Mathf.Min(9, _tabs.Count); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) && 
                    (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                {
                    SelectTab(i, true);
                    break;
                }
            }
        }
        
        #endregion
    }
    
    #region Data Types
    
    [Serializable]
    public class TabConfig
    {
        public string id;
        public string label;
        public Sprite icon;
        public RectTransform contentPanel;
        public float width = 100f;
        public float height = 40f;
        public bool showBadge = true;
    }
    
    public class TabInstance
    {
        public TabConfig config;
        public RectTransform tabRect;
        public Button tabButton;
        public Image tabImage;
        public TextMeshProUGUI tabText;
        public Image tabIcon;
        public RectTransform badge;
        public RectTransform contentPanel;
        public bool isEnabled;
    }
    
    public enum TabLayout
    {
        Horizontal,
        Vertical
    }
    
    public enum ContentTransition
    {
        None,
        Fade,
        Slide,
        Scale
    }
    
    #endregion
}
