// ============================================================================
// APEX CITADELS - DUAL MODE UI
// Simple UI overlay showing mode and controls
// ============================================================================
using UnityEngine;
using UnityEngine.UI;

namespace ApexCitadels.GameModes
{
    /// <summary>
    /// UI overlay for the dual mode system.
    /// Shows current mode and control hints.
    /// </summary>
    public class DualModeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DualModeController controller;
        
        [Header("UI Elements")]
        [SerializeField] private Text modeText;
        [SerializeField] private Text controlsText;
        [SerializeField] private Text locationText;
        [SerializeField] private Image transitionOverlay;
        
        // Auto-created elements
        private GameObject _uiCanvas;
        private Text _modeLabel;
        private Text _controlsLabel;
        private Text _locationLabel;
        
        private void Start()
        {
            // Find controller
            if (controller == null)
            {
                controller = FindAnyObjectByType<DualModeController>();
            }
            
            // Create UI if not assigned
            if (modeText == null)
            {
                CreateUI();
            }
            
            // Subscribe to events
            if (controller != null)
            {
                controller.OnModeChanged += OnModeChanged;
            }
            
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.OnModeChanged -= OnModeChanged;
            }
        }
        
        private void Update()
        {
            // Update location display
            if (controller != null && _locationLabel != null)
            {
                _locationLabel.text = $"Location: {controller.CurrentLatitude:F4}, {controller.CurrentLongitude:F4}";
            }
        }
        
        private void CreateUI()
        {
            // Create canvas
            _uiCanvas = new GameObject("DualModeUI_Canvas");
            _uiCanvas.transform.SetParent(transform);
            
            var canvas = _uiCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var scaler = _uiCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            _uiCanvas.AddComponent<GraphicRaycaster>();
            
            // Create panel background
            var panel = CreatePanel(_uiCanvas.transform, "InfoPanel", 
                new Vector2(20, -20), new Vector2(400, 200), TextAnchor.UpperLeft);
            
            // Mode label
            _modeLabel = CreateText(panel.transform, "ModeLabel", 
                new Vector2(10, -10), 32, "MAP VIEW");
            _modeLabel.color = new Color(0.2f, 0.8f, 1f);
            _modeLabel.fontStyle = FontStyle.Bold;
            
            // Controls label
            _controlsLabel = CreateText(panel.transform, "ControlsLabel", 
                new Vector2(10, -50), 18, "Controls:\nWASD - Move\nSpace - Land/Take Off");
            _controlsLabel.color = Color.white;
            
            // Location label
            _locationLabel = CreateText(panel.transform, "LocationLabel", 
                new Vector2(10, -140), 14, "Location: 0, 0");
            _locationLabel.color = new Color(0.7f, 0.7f, 0.7f);
        }
        
        private GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, TextAnchor anchor)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            var image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.6f);
            
            return panel;
        }
        
        private Text CreateText(Transform parent, string name, Vector2 position, int fontSize, string content)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent);
            
            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(-20, 100);
            
            var text = textObj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            
            return text;
        }
        
        private void OnModeChanged(ViewMode mode)
        {
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (controller == null) return;
            
            var mode = controller.CurrentMode;
            
            if (_modeLabel != null)
            {
                switch (mode)
                {
                    case ViewMode.MapView:
                        _modeLabel.text = "MAP VIEW";
                        _modeLabel.color = new Color(0.2f, 0.8f, 1f);
                        break;
                    case ViewMode.GroundView:
                        _modeLabel.text = "GROUND VIEW";
                        _modeLabel.color = new Color(0.2f, 1f, 0.4f);
                        break;
                    case ViewMode.Transitioning:
                        _modeLabel.text = "TRANSITIONING...";
                        _modeLabel.color = new Color(1f, 0.8f, 0.2f);
                        break;
                }
            }
            
            if (_controlsLabel != null)
            {
                switch (mode)
                {
                    case ViewMode.MapView:
                        _controlsLabel.text = "Controls:\n" +
                            "WASD - Move\n" +
                            "Q/E - Rotate\n" +
                            "Scroll - Zoom\n" +
                            "Shift - Fast Move\n" +
                            "SPACE - Land Here";
                        break;
                    case ViewMode.GroundView:
                        _controlsLabel.text = "Controls:\n" +
                            "WASD - Walk\n" +
                            "Mouse - Look\n" +
                            "Shift - Run\n" +
                            "Scroll - Camera Zoom\n" +
                            "SPACE - Take Off";
                        break;
                    default:
                        _controlsLabel.text = "Please wait...";
                        break;
                }
            }
        }
    }
}
