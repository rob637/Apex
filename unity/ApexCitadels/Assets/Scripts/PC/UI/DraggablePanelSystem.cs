using Camera = UnityEngine.Camera;
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
    /// Draggable panel system for inventory, equipment, and other UIs.
    /// Features:
    /// - Drag and drop support
    /// - Snap to edges/grid
    /// - Panel minimization
    /// - Size constraints
    /// - Z-order management
    /// </summary>
    public class DraggablePanelSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float snapThreshold = 20f;
        [SerializeField] private bool enableEdgeSnapping = true;
        [SerializeField] private bool enableGridSnapping = false;
        [SerializeField] private float gridSize = 50f;
        [SerializeField] private Vector2 screenPadding = new Vector2(10, 10);
        
        [Header("Animation")]
        [SerializeField] private float minimizeAnimationDuration = 0.3f;
        [SerializeField] private float snapAnimationDuration = 0.2f;
        [SerializeField] private AnimationCurve animationCurve;
        
        // Singleton
        private static DraggablePanelSystem _instance;
        public static DraggablePanelSystem Instance => _instance;
        
        // Registered panels
        private Dictionary<string, DraggablePanel> _panels = new Dictionary<string, DraggablePanel>();
        private List<DraggablePanel> _panelZOrder = new List<DraggablePanel>();
        
        // Drag state
        private DraggablePanel _draggingPanel;
        private Vector2 _dragOffset;
        private bool _isDragging;
        
        // Canvas reference
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Camera _uiCamera;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                _canvasRect = _canvas.GetComponent<RectTransform>();
                if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    _uiCamera = _canvas.worldCamera;
                }
            }
            
            InitializeAnimationCurve();
        }
        
        private void InitializeAnimationCurve()
        {
            if (animationCurve == null || animationCurve.length == 0)
            {
                animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Register a panel as draggable
        /// </summary>
        public DraggablePanel RegisterPanel(string panelId, RectTransform panel, RectTransform dragHandle = null)
        {
            if (_panels.ContainsKey(panelId))
            {
                ApexLogger.LogWarning($"Panel {panelId} already registered", ApexLogger.LogCategory.UI);
                return _panels[panelId];
            }
            
            var draggable = new DraggablePanel
            {
                id = panelId,
                panelRect = panel,
                dragHandle = dragHandle ?? panel,
                canvasGroup = panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>(),
                originalSize = panel.sizeDelta,
                originalPosition = panel.anchoredPosition,
                isMinimized = false
            };
            
            // Setup drag handle events
            SetupDragHandle(draggable);
            
            _panels[panelId] = draggable;
            _panelZOrder.Add(draggable);
            UpdateZOrder();
            
            return draggable;
        }
        
        /// <summary>
        /// Unregister a panel
        /// </summary>
        public void UnregisterPanel(string panelId)
        {
            if (_panels.TryGetValue(panelId, out var panel))
            {
                _panels.Remove(panelId);
                _panelZOrder.Remove(panel);
            }
        }
        
        /// <summary>
        /// Bring panel to front
        /// </summary>
        public void BringToFront(string panelId)
        {
            if (_panels.TryGetValue(panelId, out var panel))
            {
                BringToFront(panel);
            }
        }
        
        /// <summary>
        /// Minimize panel
        /// </summary>
        public void MinimizePanel(string panelId)
        {
            if (_panels.TryGetValue(panelId, out var panel) && !panel.isMinimized)
            {
                StartCoroutine(AnimateMinimize(panel, true));
            }
        }
        
        /// <summary>
        /// Restore minimized panel
        /// </summary>
        public void RestorePanel(string panelId)
        {
            if (_panels.TryGetValue(panelId, out var panel) && panel.isMinimized)
            {
                StartCoroutine(AnimateMinimize(panel, false));
            }
        }
        
        /// <summary>
        /// Toggle panel minimization
        /// </summary>
        public void ToggleMinimize(string panelId)
        {
            if (_panels.TryGetValue(panelId, out var panel))
            {
                StartCoroutine(AnimateMinimize(panel, !panel.isMinimized));
            }
        }
        
        /// <summary>
        /// Reset panel to original position
        /// </summary>
        public void ResetPosition(string panelId)
        {
            if (_panels.TryGetValue(panelId, out var panel))
            {
                StartCoroutine(AnimateToPosition(panel, panel.originalPosition));
            }
        }
        
        /// <summary>
        /// Set panel position
        /// </summary>
        public void SetPosition(string panelId, Vector2 position, bool animate = true)
        {
            if (_panels.TryGetValue(panelId, out var panel))
            {
                if (animate)
                {
                    StartCoroutine(AnimateToPosition(panel, position));
                }
                else
                {
                    panel.panelRect.anchoredPosition = position;
                }
            }
        }
        
        /// <summary>
        /// Get panel by ID
        /// </summary>
        public DraggablePanel GetPanel(string panelId)
        {
            _panels.TryGetValue(panelId, out var panel);
            return panel;
        }
        
        /// <summary>
        /// Save panel positions
        /// </summary>
        public Dictionary<string, PanelSaveData> SaveLayout()
        {
            var layout = new Dictionary<string, PanelSaveData>();
            
            foreach (var kvp in _panels)
            {
                layout[kvp.Key] = new PanelSaveData
                {
                    position = kvp.Value.panelRect.anchoredPosition,
                    isMinimized = kvp.Value.isMinimized
                };
            }
            
            return layout;
        }
        
        /// <summary>
        /// Restore panel positions
        /// </summary>
        public void RestoreLayout(Dictionary<string, PanelSaveData> layout)
        {
            foreach (var kvp in layout)
            {
                if (_panels.TryGetValue(kvp.Key, out var panel))
                {
                    panel.panelRect.anchoredPosition = kvp.Value.position;
                    panel.isMinimized = kvp.Value.isMinimized;
                    
                    if (panel.isMinimized)
                    {
                        // Apply minimized state immediately
                        panel.panelRect.sizeDelta = new Vector2(panel.originalSize.x, 
                            panel.minimizedHeight > 0 ? panel.minimizedHeight : 40);
                    }
                }
            }
        }
        
        #endregion
        
        #region Drag Handling
        
        private void SetupDragHandle(DraggablePanel panel)
        {
            var eventTrigger = panel.dragHandle.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = panel.dragHandle.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }
            
            // Pointer down
            var downEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown
            };
            downEntry.callback.AddListener((data) => OnPointerDown(panel, (UnityEngine.EventSystems.PointerEventData)data));
            eventTrigger.triggers.Add(downEntry);
            
            // Drag
            var dragEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.Drag
            };
            dragEntry.callback.AddListener((data) => OnDrag(panel, (UnityEngine.EventSystems.PointerEventData)data));
            eventTrigger.triggers.Add(dragEntry);
            
            // Pointer up
            var upEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp
            };
            upEntry.callback.AddListener((data) => OnPointerUp(panel, (UnityEngine.EventSystems.PointerEventData)data));
            eventTrigger.triggers.Add(upEntry);
        }
        
        private void OnPointerDown(DraggablePanel panel, UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (panel.isLocked) return;
            
            _draggingPanel = panel;
            _isDragging = true;
            
            // Calculate offset
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, eventData.position, _uiCamera, out localPoint);
            
            _dragOffset = panel.panelRect.anchoredPosition - localPoint;
            
            // Bring to front
            BringToFront(panel);
            
            // Visual feedback
            if (panel.canvasGroup != null)
            {
                panel.canvasGroup.alpha = 0.9f;
            }
            
            panel.OnDragStarted?.Invoke();
        }
        
        private void OnDrag(DraggablePanel panel, UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!_isDragging || panel != _draggingPanel || panel.isLocked) return;
            
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, eventData.position, _uiCamera, out localPoint);
            
            Vector2 newPosition = localPoint + _dragOffset;
            
            // Constrain to screen
            newPosition = ConstrainToScreen(newPosition, panel.panelRect.sizeDelta);
            
            panel.panelRect.anchoredPosition = newPosition;
        }
        
        private void OnPointerUp(DraggablePanel panel, UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!_isDragging || panel != _draggingPanel) return;
            
            _isDragging = false;
            _draggingPanel = null;
            
            // Restore alpha
            if (panel.canvasGroup != null)
            {
                panel.canvasGroup.alpha = 1f;
            }
            
            // Apply snapping
            if (enableEdgeSnapping)
            {
                TrySnapToEdge(panel);
            }
            else if (enableGridSnapping)
            {
                SnapToGrid(panel);
            }
            
            panel.OnDragEnded?.Invoke();
        }
        
        #endregion
        
        #region Snapping
        
        private void TrySnapToEdge(DraggablePanel panel)
        {
            Vector2 pos = panel.panelRect.anchoredPosition;
            Vector2 size = panel.panelRect.sizeDelta;
            Vector2 canvasSize = _canvasRect.sizeDelta;
            
            float halfCanvasW = canvasSize.x * 0.5f;
            float halfCanvasH = canvasSize.y * 0.5f;
            
            bool snapped = false;
            Vector2 targetPos = pos;
            
            // Left edge
            float leftEdge = -halfCanvasW + screenPadding.x;
            if (Mathf.Abs(pos.x - size.x * 0.5f - leftEdge) < snapThreshold)
            {
                targetPos.x = leftEdge + size.x * 0.5f;
                snapped = true;
            }
            
            // Right edge
            float rightEdge = halfCanvasW - screenPadding.x;
            if (Mathf.Abs(pos.x + size.x * 0.5f - rightEdge) < snapThreshold)
            {
                targetPos.x = rightEdge - size.x * 0.5f;
                snapped = true;
            }
            
            // Top edge
            float topEdge = halfCanvasH - screenPadding.y;
            if (Mathf.Abs(pos.y + size.y * 0.5f - topEdge) < snapThreshold)
            {
                targetPos.y = topEdge - size.y * 0.5f;
                snapped = true;
            }
            
            // Bottom edge
            float bottomEdge = -halfCanvasH + screenPadding.y;
            if (Mathf.Abs(pos.y - size.y * 0.5f - bottomEdge) < snapThreshold)
            {
                targetPos.y = bottomEdge + size.y * 0.5f;
                snapped = true;
            }
            
            if (snapped)
            {
                StartCoroutine(AnimateToPosition(panel, targetPos));
            }
        }
        
        private void SnapToGrid(DraggablePanel panel)
        {
            Vector2 pos = panel.panelRect.anchoredPosition;
            
            pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
            pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
            
            pos = ConstrainToScreen(pos, panel.panelRect.sizeDelta);
            
            StartCoroutine(AnimateToPosition(panel, pos));
        }
        
        private Vector2 ConstrainToScreen(Vector2 position, Vector2 panelSize)
        {
            if (_canvasRect == null) return position;
            
            Vector2 canvasSize = _canvasRect.sizeDelta;
            float halfCanvasW = canvasSize.x * 0.5f;
            float halfCanvasH = canvasSize.y * 0.5f;
            float halfPanelW = panelSize.x * 0.5f;
            float halfPanelH = panelSize.y * 0.5f;
            
            // Clamp to screen bounds with padding
            position.x = Mathf.Clamp(position.x,
                -halfCanvasW + halfPanelW + screenPadding.x,
                halfCanvasW - halfPanelW - screenPadding.x);
            
            position.y = Mathf.Clamp(position.y,
                -halfCanvasH + halfPanelH + screenPadding.y,
                halfCanvasH - halfPanelH - screenPadding.y);
            
            return position;
        }
        
        #endregion
        
        #region Z-Order
        
        private void BringToFront(DraggablePanel panel)
        {
            _panelZOrder.Remove(panel);
            _panelZOrder.Add(panel);
            UpdateZOrder();
        }
        
        private void UpdateZOrder()
        {
            for (int i = 0; i < _panelZOrder.Count; i++)
            {
                _panelZOrder[i].panelRect.SetSiblingIndex(i);
            }
        }
        
        #endregion
        
        #region Animation
        
        private IEnumerator AnimateToPosition(DraggablePanel panel, Vector2 targetPosition)
        {
            Vector2 startPos = panel.panelRect.anchoredPosition;
            float elapsed = 0;
            
            while (elapsed < snapAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = animationCurve.Evaluate(elapsed / snapAnimationDuration);
                
                panel.panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPosition, t);
                yield return null;
            }
            
            panel.panelRect.anchoredPosition = targetPosition;
        }
        
        private IEnumerator AnimateMinimize(DraggablePanel panel, bool minimize)
        {
            Vector2 startSize = panel.panelRect.sizeDelta;
            float targetHeight = minimize ? 
                (panel.minimizedHeight > 0 ? panel.minimizedHeight : 40f) : 
                panel.originalSize.y;
            Vector2 targetSize = new Vector2(panel.originalSize.x, targetHeight);
            
            float elapsed = 0;
            while (elapsed < minimizeAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = animationCurve.Evaluate(elapsed / minimizeAnimationDuration);
                
                panel.panelRect.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
                yield return null;
            }
            
            panel.panelRect.sizeDelta = targetSize;
            panel.isMinimized = minimize;
            
            if (minimize)
            {
                panel.OnMinimized?.Invoke();
            }
            else
            {
                panel.OnRestored?.Invoke();
            }
        }
        
        #endregion
    }
    
    #region Data Types
    
    public class DraggablePanel
    {
        public string id;
        public RectTransform panelRect;
        public RectTransform dragHandle;
        public CanvasGroup canvasGroup;
        public Vector2 originalSize;
        public Vector2 originalPosition;
        public float minimizedHeight = 40f;
        public bool isMinimized;
        public bool isLocked;
        
        public Action OnDragStarted;
        public Action OnDragEnded;
        public Action OnMinimized;
        public Action OnRestored;
    }
    
    [Serializable]
    public class PanelSaveData
    {
        public Vector2 position;
        public bool isMinimized;
    }
    
    #endregion
}
