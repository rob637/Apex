// ============================================================================
// DEMO UI - Polished UI for demonstration
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Professional demo UI with location info, controls help, and status
    /// </summary>
    public class DemoUI : MonoBehaviour
    {
        [Header("Settings")]
        public bool showUI = true;
        public bool showControls = true;
        public bool showLocation = true;
        public bool showStats = true;
        
        private Canvas _canvas;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _locationText;
        private TextMeshProUGUI _statusText;
        private TextMeshProUGUI _controlsText;
        private TextMeshProUGUI _statsText;
        
        private FantasyWorldGenerator _generator;
        private RealisticSkySystem _skySystem;
        
        private float _fpsUpdateInterval = 0.5f;
        private float _fpsTimer;
        private int _frameCount;
        private float _currentFPS;
        
        private void Start()
        {
            _generator = FindFirstObjectByType<FantasyWorldGenerator>();
            _skySystem = FindFirstObjectByType<RealisticSkySystem>();
            
            CreateUI();
        }
        
        private void Update()
        {
            UpdateFPS();
            UpdateUI();
            HandleInput();
        }
        
        private void CreateUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("DemoUICanvas");
            canvasObj.transform.SetParent(transform);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Title panel (top-left)
            CreateTitlePanel();
            
            // Controls panel (bottom-left)
            CreateControlsPanel();
            
            // Stats panel (bottom-right)
            CreateStatsPanel();
        }
        
        private void CreateTitlePanel()
        {
            // Background panel
            GameObject panelObj = CreatePanel("TitlePanel", 
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -20), new Vector2(400, 120));
            
            // Title
            _titleText = CreateText(panelObj.transform, "TitleText",
                "<color=#FFD700>APEX CITADELS</color>\n<size=70%>Fantasy World Overlay</size>",
                new Vector2(10, -10), new Vector2(380, 50), 28, TextAlignmentOptions.TopLeft);
            
            // Location info
            _locationText = CreateText(panelObj.transform, "LocationText",
                "Loading location...",
                new Vector2(10, -70), new Vector2(380, 40), 16, TextAlignmentOptions.TopLeft);
        }
        
        private void CreateControlsPanel()
        {
            GameObject panelObj = CreatePanel("ControlsPanel",
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(20, 20), new Vector2(300, 150));
            
            string controlsHelp = @"<color=#FFD700>CONTROLS</color>
<color=#AAA>WASD</color> - Move
<color=#AAA>Mouse</color> - Look (hold RMB)
<color=#AAA>Space/Q</color> - Up/Down
<color=#AAA>Shift</color> - Sprint
<color=#AAA>Tab</color> - Toggle Map
<color=#AAA>Esc</color> - Menu";
            
            _controlsText = CreateText(panelObj.transform, "ControlsText",
                controlsHelp,
                new Vector2(10, -10), new Vector2(280, 130), 14, TextAlignmentOptions.TopLeft);
        }
        
        private void CreateStatsPanel()
        {
            GameObject panelObj = CreatePanel("StatsPanel",
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-20, 20), new Vector2(200, 100));
            
            _statsText = CreateText(panelObj.transform, "StatsText",
                "FPS: --\nBuildings: --\nTime: --:--",
                new Vector2(10, -10), new Vector2(180, 80), 14, TextAlignmentOptions.TopLeft);
        }
        
        private GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax, 
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject panelObj = new GameObject(name);
            panelObj.transform.SetParent(_canvas.transform);
            
            var image = panelObj.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.7f);
            
            var rect = panelObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            return panelObj;
        }
        
        private TextMeshProUGUI CreateText(Transform parent, string name, string content,
            Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent);
            
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = alignment;
            tmp.richText = true;
            
            var rect = tmp.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            return tmp;
        }
        
        private void UpdateFPS()
        {
            _frameCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            
            if (_fpsTimer >= _fpsUpdateInterval)
            {
                _currentFPS = _frameCount / _fpsTimer;
                _frameCount = 0;
                _fpsTimer = 0;
            }
        }
        
        private void UpdateUI()
        {
            if (!showUI) return;
            
            // Update location text
            if (_locationText != null && _generator != null)
            {
                var demo = FindFirstObjectByType<FantasyWorldDemo>();
                if (demo != null)
                {
                    string locationStr = $"<color=#888>Location:</color> {demo.latitude:F4}°N, {Mathf.Abs((float)demo.longitude):F4}°W\n";
                    locationStr += $"<color=#888>Radius:</color> {demo.radiusMeters}m";
                    _locationText.text = locationStr;
                }
            }
            
            // Update stats text
            if (_statsText != null)
            {
                string timeStr = System.DateTime.Now.ToString("HH:mm");
                string fpsColor = _currentFPS > 30 ? "#0F0" : (_currentFPS > 15 ? "#FF0" : "#F00");
                
                int buildingCount = 0;
                var buildings = GameObject.Find("Buildings");
                if (buildings != null) buildingCount = buildings.transform.childCount;
                
                _statsText.text = $"<color={fpsColor}>FPS: {_currentFPS:F0}</color>\n" +
                                 $"Buildings: {buildingCount}\n" +
                                 $"Time: {timeStr}";
                
                if (_skySystem != null)
                {
                    string weather = _skySystem.currentWeather.ToString();
                    _statsText.text += $"\nWeather: {weather}";
                }
            }
        }
        
        private void HandleInput()
        {
            // Toggle UI with F1
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showUI = !showUI;
                _canvas.gameObject.SetActive(showUI);
            }
            
            // Weather controls for demo
            if (_skySystem != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    _skySystem.SetWeather(RealisticSkySystem.WeatherType.Clear);
                if (Input.GetKeyDown(KeyCode.Alpha2))
                    _skySystem.SetWeather(RealisticSkySystem.WeatherType.Cloudy, 0.5f);
                if (Input.GetKeyDown(KeyCode.Alpha3))
                    _skySystem.SetWeather(RealisticSkySystem.WeatherType.Rain, 0.8f);
                if (Input.GetKeyDown(KeyCode.Alpha4))
                    _skySystem.SetWeather(RealisticSkySystem.WeatherType.Fog, 0.3f);
            }
        }
    }
}
