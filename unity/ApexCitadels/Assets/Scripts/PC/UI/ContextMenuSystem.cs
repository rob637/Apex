using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Context menu system for right-click menus.
    /// Features:
    /// - Dynamic menu generation
    /// - Nested submenus
    /// - Keyboard navigation
    /// - Icon support
    /// - Disabled item states
    /// - Separator support
    /// </summary>
    public class ContextMenuSystem : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject menuPanelPrefab;
        [SerializeField] private GameObject menuItemPrefab;
        [SerializeField] private GameObject separatorPrefab;
        
        [Header("Settings")]
        [SerializeField] private float menuPadding = 5f;
        [SerializeField] private float itemHeight = 30f;
        [SerializeField] private float separatorHeight = 10f;
        [SerializeField] private float submenuDelay = 0.3f;
        [SerializeField] private float fadeInDuration = 0.15f;
        [SerializeField] private float fadeOutDuration = 0.1f;
        [SerializeField] private Vector2 screenPadding = new Vector2(10, 10);
        
        [Header("Styling")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        [SerializeField] private Color hoverColor = new Color(0.3f, 0.4f, 0.6f, 1f);
        [SerializeField] private Color disabledTextColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private float menuWidth = 200f;
        
        // Singleton
        private static ContextMenuSystem _instance;
        public static ContextMenuSystem Instance => _instance;
        
        // State
        private List<ContextMenuInstance> _activeMenus = new List<ContextMenuInstance>();
        private ContextMenuItem _hoveredItem;
        private Coroutine _submenuCoroutine;
        private Canvas _canvas;
        private Camera _uiCamera;
        private int _selectedIndex = -1;
        
        // Events
        public event Action OnMenuOpened;
        public event Action OnMenuClosed;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCamera = _canvas.worldCamera;
            }
        }
        
        private void Update()
        {
            // Close menus on click outside
            if (_activeMenus.Count > 0 && Input.GetMouseButtonDown(0))
            {
                if (!IsMouseOverAnyMenu())
                {
                    CloseAllMenus();
                }
            }
            
            // Close on escape
            if (_activeMenus.Count > 0 && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseAllMenus();
            }
            
            // Keyboard navigation
            if (_activeMenus.Count > 0)
            {
                HandleKeyboardNavigation();
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Show context menu at mouse position
        /// </summary>
        public void Show(List<ContextMenuItemData> items)
        {
            Show(items, Input.mousePosition);
        }
        
        /// <summary>
        /// Show context menu at specific position
        /// </summary>
        public void Show(List<ContextMenuItemData> items, Vector2 screenPosition)
        {
            if (items == null || items.Count == 0) return;
            
            CloseAllMenus();
            
            var menu = CreateMenu(items);
            PositionMenu(menu, screenPosition, true);
            StartCoroutine(AnimateMenuShow(menu));
            
            _activeMenus.Add(menu);
            _selectedIndex = -1;
            
            OnMenuOpened?.Invoke();
        }
        
        /// <summary>
        /// Show context menu for a specific target
        /// </summary>
        public void ShowForTarget(List<ContextMenuItemData> items, RectTransform target, 
            MenuAlignment alignment = MenuAlignment.Below)
        {
            if (items == null || items.Count == 0) return;
            
            CloseAllMenus();
            
            var menu = CreateMenu(items);
            PositionMenuRelativeToTarget(menu, target, alignment);
            StartCoroutine(AnimateMenuShow(menu));
            
            _activeMenus.Add(menu);
            _selectedIndex = -1;
            
            OnMenuOpened?.Invoke();
        }
        
        /// <summary>
        /// Close all open menus
        /// </summary>
        public void CloseAllMenus()
        {
            if (_activeMenus.Count == 0) return;
            
            StopSubmenuCoroutine();
            
            foreach (var menu in _activeMenus)
            {
                if (menu.panel != null)
                {
                    StartCoroutine(AnimateMenuHide(menu));
                }
            }
            
            _activeMenus.Clear();
            _hoveredItem = null;
            _selectedIndex = -1;
            
            OnMenuClosed?.Invoke();
        }
        
        /// <summary>
        /// Check if menu is currently open
        /// </summary>
        public bool IsMenuOpen => _activeMenus.Count > 0;
        
        #endregion
        
        #region Menu Creation
        
        private ContextMenuInstance CreateMenu(List<ContextMenuItemData> items)
        {
            // Create panel
            var panelGo = Instantiate(menuPanelPrefab, transform);
            var panel = panelGo.GetComponent<RectTransform>();
            var canvasGroup = panelGo.GetComponent<CanvasGroup>() ?? panelGo.AddComponent<CanvasGroup>();
            
            // Set panel size
            float height = CalculateMenuHeight(items);
            panel.sizeDelta = new Vector2(menuWidth, height);
            
            // Create items
            var menuItems = new List<ContextMenuItem>();
            float yOffset = -menuPadding;
            
            foreach (var itemData in items)
            {
                if (itemData.isSeparator)
                {
                    CreateSeparator(panel, yOffset);
                    yOffset -= separatorHeight;
                }
                else
                {
                    var menuItem = CreateMenuItem(panel, itemData, yOffset);
                    menuItems.Add(menuItem);
                    yOffset -= itemHeight;
                }
            }
            
            return new ContextMenuInstance
            {
                panel = panel,
                canvasGroup = canvasGroup,
                items = menuItems
            };
        }
        
        private ContextMenuItem CreateMenuItem(RectTransform parent, ContextMenuItemData data, float yOffset)
        {
            var itemGo = Instantiate(menuItemPrefab, parent);
            var itemRect = itemGo.GetComponent<RectTransform>();
            
            // Position
            itemRect.anchorMin = new Vector2(0, 1);
            itemRect.anchorMax = new Vector2(1, 1);
            itemRect.pivot = new Vector2(0.5f, 1);
            itemRect.anchoredPosition = new Vector2(0, yOffset);
            itemRect.sizeDelta = new Vector2(0, itemHeight);
            
            // Get components
            var background = itemGo.GetComponent<Image>();
            var text = itemGo.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            var icon = itemGo.transform.Find("Icon")?.GetComponent<Image>();
            var shortcut = itemGo.transform.Find("Shortcut")?.GetComponent<TextMeshProUGUI>();
            var submenuArrow = itemGo.transform.Find("Arrow")?.gameObject;
            
            // Configure
            if (text != null)
            {
                text.text = data.text;
                text.color = data.isEnabled ? Color.white : disabledTextColor;
            }
            
            if (icon != null)
            {
                if (data.icon != null)
                {
                    icon.sprite = data.icon;
                    icon.gameObject.SetActive(true);
                }
                else
                {
                    icon.gameObject.SetActive(false);
                }
            }
            
            if (shortcut != null)
            {
                if (!string.IsNullOrEmpty(data.shortcutText))
                {
                    shortcut.text = data.shortcutText;
                    shortcut.gameObject.SetActive(true);
                }
                else
                {
                    shortcut.gameObject.SetActive(false);
                }
            }
            
            if (submenuArrow != null)
            {
                submenuArrow.SetActive(data.submenuItems != null && data.submenuItems.Count > 0);
            }
            
            // Create menu item instance
            var menuItem = new ContextMenuItem
            {
                data = data,
                rectTransform = itemRect,
                background = background,
                textComponent = text
            };
            
            // Setup interaction
            var eventTrigger = itemGo.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            // Pointer enter
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            enterEntry.callback.AddListener((e) => OnItemPointerEnter(menuItem));
            eventTrigger.triggers.Add(enterEntry);
            
            // Pointer exit
            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
            };
            exitEntry.callback.AddListener((e) => OnItemPointerExit(menuItem));
            eventTrigger.triggers.Add(exitEntry);
            
            // Click
            var clickEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick
            };
            clickEntry.callback.AddListener((e) => OnItemClicked(menuItem));
            eventTrigger.triggers.Add(clickEntry);
            
            return menuItem;
        }
        
        private void CreateSeparator(RectTransform parent, float yOffset)
        {
            var sepGo = Instantiate(separatorPrefab, parent);
            var sepRect = sepGo.GetComponent<RectTransform>();
            
            sepRect.anchorMin = new Vector2(0, 1);
            sepRect.anchorMax = new Vector2(1, 1);
            sepRect.pivot = new Vector2(0.5f, 1);
            sepRect.anchoredPosition = new Vector2(0, yOffset);
            sepRect.sizeDelta = new Vector2(0, separatorHeight);
        }
        
        private float CalculateMenuHeight(List<ContextMenuItemData> items)
        {
            float height = menuPadding * 2;
            
            foreach (var item in items)
            {
                height += item.isSeparator ? separatorHeight : itemHeight;
            }
            
            return height;
        }
        
        #endregion
        
        #region Positioning
        
        private void PositionMenu(ContextMenuInstance menu, Vector2 screenPosition, bool isRootMenu)
        {
            if (_canvas == null) return;
            
            // Convert screen position to local canvas position
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(),
                screenPosition,
                _uiCamera,
                out localPoint
            );
            
            // Get menu size
            Vector2 menuSize = menu.panel.sizeDelta;
            
            // Get canvas size
            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRect.sizeDelta;
            float halfWidth = canvasSize.x * 0.5f;
            float halfHeight = canvasSize.y * 0.5f;
            
            // Adjust position to stay on screen
            // Check right edge
            if (localPoint.x + menuSize.x > halfWidth - screenPadding.x)
            {
                localPoint.x = localPoint.x - menuSize.x;
            }
            
            // Check bottom edge
            if (localPoint.y - menuSize.y < -halfHeight + screenPadding.y)
            {
                localPoint.y = -halfHeight + screenPadding.y + menuSize.y;
            }
            
            // Check left edge
            if (localPoint.x < -halfWidth + screenPadding.x)
            {
                localPoint.x = -halfWidth + screenPadding.x;
            }
            
            // Check top edge
            if (localPoint.y > halfHeight - screenPadding.y)
            {
                localPoint.y = halfHeight - screenPadding.y;
            }
            
            menu.panel.anchoredPosition = localPoint;
        }
        
        private void PositionMenuRelativeToTarget(ContextMenuInstance menu, RectTransform target, MenuAlignment alignment)
        {
            if (_canvas == null || target == null) return;
            
            // Get target corners in screen space
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            
            Vector2 position = Vector2.zero;
            
            switch (alignment)
            {
                case MenuAlignment.Below:
                    position = RectTransformUtility.WorldToScreenPoint(_uiCamera, corners[0]); // Bottom left
                    break;
                case MenuAlignment.Above:
                    position = RectTransformUtility.WorldToScreenPoint(_uiCamera, corners[1]); // Top left
                    position.y += menu.panel.sizeDelta.y;
                    break;
                case MenuAlignment.Right:
                    position = RectTransformUtility.WorldToScreenPoint(_uiCamera, corners[3]); // Top right
                    break;
                case MenuAlignment.Left:
                    position = RectTransformUtility.WorldToScreenPoint(_uiCamera, corners[0]); // Bottom left
                    position.x -= menu.panel.sizeDelta.x;
                    break;
            }
            
            PositionMenu(menu, position, true);
        }
        
        #endregion
        
        #region Animation
        
        private IEnumerator AnimateMenuShow(ContextMenuInstance menu)
        {
            menu.canvasGroup.alpha = 0;
            menu.panel.localScale = new Vector3(1, 0.9f, 1);
            
            float elapsed = 0;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeInDuration;
                
                menu.canvasGroup.alpha = t;
                menu.panel.localScale = new Vector3(1, Mathf.Lerp(0.9f, 1f, t), 1);
                
                yield return null;
            }
            
            menu.canvasGroup.alpha = 1;
            menu.panel.localScale = Vector3.one;
        }
        
        private IEnumerator AnimateMenuHide(ContextMenuInstance menu)
        {
            float elapsed = 0;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeOutDuration;
                
                menu.canvasGroup.alpha = 1 - t;
                
                yield return null;
            }
            
            Destroy(menu.panel.gameObject);
        }
        
        #endregion
        
        #region Item Interaction
        
        private void OnItemPointerEnter(ContextMenuItem item)
        {
            if (!item.data.isEnabled) return;
            
            // Highlight item
            if (item.background != null)
            {
                item.background.color = hoverColor;
            }
            
            _hoveredItem = item;
            
            // Find item index
            if (_activeMenus.Count > 0)
            {
                _selectedIndex = _activeMenus[0].items.IndexOf(item);
            }
            
            // Handle submenu
            if (item.data.submenuItems != null && item.data.submenuItems.Count > 0)
            {
                StopSubmenuCoroutine();
                _submenuCoroutine = StartCoroutine(ShowSubmenuDelayed(item));
            }
            else
            {
                // Close any open submenus
                CloseSubmenus(1);
            }
        }
        
        private void OnItemPointerExit(ContextMenuItem item)
        {
            // Remove highlight
            if (item.background != null)
            {
                item.background.color = normalColor;
            }
            
            if (_hoveredItem == item)
            {
                _hoveredItem = null;
            }
        }
        
        private void OnItemClicked(ContextMenuItem item)
        {
            if (!item.data.isEnabled) return;
            
            // Don't close if has submenu
            if (item.data.submenuItems != null && item.data.submenuItems.Count > 0)
            {
                return;
            }
            
            // Execute action
            item.data.onSelect?.Invoke();
            
            // Close menus
            CloseAllMenus();
        }
        
        private IEnumerator ShowSubmenuDelayed(ContextMenuItem parentItem)
        {
            yield return new WaitForSecondsRealtime(submenuDelay);
            
            // Close existing submenus
            CloseSubmenus(1);
            
            // Create submenu
            var submenu = CreateMenu(parentItem.data.submenuItems);
            
            // Position to the right of parent item
            Vector3[] corners = new Vector3[4];
            parentItem.rectTransform.GetWorldCorners(corners);
            Vector2 position = RectTransformUtility.WorldToScreenPoint(_uiCamera, corners[3]); // Top right
            
            PositionMenu(submenu, position, false);
            StartCoroutine(AnimateMenuShow(submenu));
            
            _activeMenus.Add(submenu);
        }
        
        private void StopSubmenuCoroutine()
        {
            if (_submenuCoroutine != null)
            {
                StopCoroutine(_submenuCoroutine);
                _submenuCoroutine = null;
            }
        }
        
        private void CloseSubmenus(int keepCount)
        {
            while (_activeMenus.Count > keepCount)
            {
                var menu = _activeMenus[_activeMenus.Count - 1];
                _activeMenus.RemoveAt(_activeMenus.Count - 1);
                StartCoroutine(AnimateMenuHide(menu));
            }
        }
        
        #endregion
        
        #region Keyboard Navigation
        
        private void HandleKeyboardNavigation()
        {
            if (_activeMenus.Count == 0) return;
            
            var currentMenu = _activeMenus[_activeMenus.Count - 1];
            var enabledItems = currentMenu.items.FindAll(i => i.data.isEnabled);
            
            if (enabledItems.Count == 0) return;
            
            // Arrow down
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _selectedIndex = (_selectedIndex + 1) % enabledItems.Count;
                HighlightItem(currentMenu, _selectedIndex);
            }
            
            // Arrow up
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _selectedIndex = _selectedIndex <= 0 ? enabledItems.Count - 1 : _selectedIndex - 1;
                HighlightItem(currentMenu, _selectedIndex);
            }
            
            // Arrow right (open submenu)
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (_selectedIndex >= 0 && _selectedIndex < enabledItems.Count)
                {
                    var item = enabledItems[_selectedIndex];
                    if (item.data.submenuItems != null && item.data.submenuItems.Count > 0)
                    {
                        StopSubmenuCoroutine();
                        StartCoroutine(ShowSubmenuDelayed(item));
                    }
                }
            }
            
            // Arrow left (close submenu)
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (_activeMenus.Count > 1)
                {
                    CloseSubmenus(_activeMenus.Count - 1);
                }
            }
            
            // Enter (select)
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_selectedIndex >= 0 && _selectedIndex < enabledItems.Count)
                {
                    OnItemClicked(enabledItems[_selectedIndex]);
                }
            }
        }
        
        private void HighlightItem(ContextMenuInstance menu, int index)
        {
            // Reset all items
            foreach (var item in menu.items)
            {
                if (item.background != null)
                {
                    item.background.color = normalColor;
                }
            }
            
            // Highlight selected
            if (index >= 0 && index < menu.items.Count)
            {
                var selectedItem = menu.items.FindAll(i => i.data.isEnabled)[index];
                if (selectedItem.background != null)
                {
                    selectedItem.background.color = hoverColor;
                }
                _hoveredItem = selectedItem;
            }
        }
        
        #endregion
        
        #region Helpers
        
        private bool IsMouseOverAnyMenu()
        {
            foreach (var menu in _activeMenus)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(menu.panel, Input.mousePosition, _uiCamera))
                {
                    return true;
                }
            }
            return false;
        }
        
        #endregion
    }
    
    #region Data Types
    
    [Serializable]
    public class ContextMenuItemData
    {
        public string text;
        public Sprite icon;
        public string shortcutText;
        public Action onSelect;
        public bool isEnabled = true;
        public bool isSeparator;
        public List<ContextMenuItemData> submenuItems;
        
        // Static helper for separator
        public static ContextMenuItemData Separator => new ContextMenuItemData { isSeparator = true };
    }
    
    public class ContextMenuInstance
    {
        public RectTransform panel;
        public CanvasGroup canvasGroup;
        public List<ContextMenuItem> items;
    }
    
    public class ContextMenuItem
    {
        public ContextMenuItemData data;
        public RectTransform rectTransform;
        public Image background;
        public TextMeshProUGUI textComponent;
    }
    
    public enum MenuAlignment
    {
        Below,
        Above,
        Right,
        Left
    }
    
    #endregion
}
