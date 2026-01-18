using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIOutline = UnityEngine.UI.Outline;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Mini-map showing territory control overview.
    /// Essential for strategy gameplay awareness.
    /// </summary>
    public class MiniMapPanel : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float mapSize = 200f;
        [SerializeField] private float worldSize = 1000f; // Size of game world
        [SerializeField] private float pingDuration = 3f;
        
        [Header("Colors")]
        [SerializeField] private Color ownedColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color enemyColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color neutralColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color allyColor = new Color(0.2f, 0.5f, 0.8f);
        [SerializeField] private Color playerMarkerColor = Color.yellow;
        
        // UI
        private GameObject _panel;
        private RectTransform _mapRect;
        private RawImage _mapBackground;
        private GameObject _playerMarker;
        private GameObject _pingContainer;
        private Dictionary<string, GameObject> _territoryMarkers = new Dictionary<string, GameObject>();
        private List<PingMarker> _activePings = new List<PingMarker>();
        
        // State
        private bool _isExpanded = false;
        
        public static MiniMapPanel Instance { get; private set; }
        
        public System.Action<Vector2> OnMapClick;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            CreateMiniMapUI();
            
            // Listen for territory updates
            if (WorldMapRenderer.Instance != null)
            {
                // Would subscribe to territory change events here
            }
        }

        private void Update()
        {
            UpdatePlayerMarker();
            UpdatePings();
        }

        /// <summary>
        /// Toggle between small and expanded minimap
        /// </summary>
        public void ToggleExpanded()
        {
            _isExpanded = !_isExpanded;
            UpdateMapSize();
        }

        /// <summary>
        /// Add a ping at world position
        /// </summary>
        public void AddPing(Vector3 worldPos, Color color, string label = "")
        {
            Vector2 mapPos = WorldToMapPosition(worldPos);
            CreatePingMarker(mapPos, color, label);
        }

        /// <summary>
        /// Update or add a territory marker
        /// </summary>
        public void UpdateTerritoryMarker(string territoryId, Vector3 worldPos, TerritoryOwnership ownership)
        {
            Vector2 mapPos = WorldToMapPosition(worldPos);
            Color markerColor = GetOwnershipColor(ownership);
            
            if (_territoryMarkers.TryGetValue(territoryId, out var marker))
            {
                // Update existing
                marker.GetComponent<RectTransform>().anchoredPosition = mapPos;
                marker.GetComponent<Image>().color = markerColor;
            }
            else
            {
                // Create new
                CreateTerritoryMarker(territoryId, mapPos, markerColor);
            }
        }

        /// <summary>
        /// Remove a territory marker
        /// </summary>
        public void RemoveTerritoryMarker(string territoryId)
        {
            if (_territoryMarkers.TryGetValue(territoryId, out var marker))
            {
                Destroy(marker);
                _territoryMarkers.Remove(territoryId);
            }
        }

        private void CreateMiniMapUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel (bottom right corner)
            _panel = new GameObject("MiniMapPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-10, 60); // Above bottom bar
            rect.sizeDelta = new Vector2(mapSize + 10, mapSize + 35);
            
            // Background frame
            Image frameBg = _panel.AddComponent<Image>();
            frameBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            
            // Outline
            UnityEngine.UI.Outline outline = _panel.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.3f, 0.5f, 0.7f, 0.8f);
            outline.effectDistance = new Vector2(2, 2);
            
            // Title bar
            CreateTitleBar();
            
            // Map area
            CreateMapArea();
            
            // Player marker
            CreatePlayerMarker();
            
            // Ping container
            CreatePingContainer();
            
            // Demo territories
            CreateDemoTerritories();
        }

        private void CreateTitleBar()
        {
            GameObject titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(_panel.transform, false);
            
            RectTransform rect = titleBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 25);
            
            HorizontalLayoutGroup layout = titleBar.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.padding = new RectOffset(5, 5, 2, 2);
            
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(titleBar.transform, false);
            
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "🗺️ MAP";
            titleText.fontSize = 14;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.color = Color.white;
            
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;
            
            // Expand button
            GameObject expandBtn = new GameObject("ExpandBtn");
            expandBtn.transform.SetParent(titleBar.transform, false);
            
            LayoutElement btnLE = expandBtn.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 25;
            btnLE.preferredHeight = 20;
            
            Image btnBg = expandBtn.AddComponent<Image>();
            btnBg.color = new Color(0.3f, 0.3f, 0.4f);
            
            Button btn = expandBtn.AddComponent<Button>();
            btn.onClick.AddListener(ToggleExpanded);
            
            GameObject btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(expandBtn.transform, false);
            
            RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "⬆";
            btnText.fontSize = 12;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;
        }

        private void CreateMapArea()
        {
            GameObject mapArea = new GameObject("MapArea");
            mapArea.transform.SetParent(_panel.transform, false);
            
            _mapRect = mapArea.AddComponent<RectTransform>();
            _mapRect.anchorMin = new Vector2(0, 0);
            _mapRect.anchorMax = new Vector2(1, 1);
            _mapRect.offsetMin = new Vector2(5, 5);
            _mapRect.offsetMax = new Vector2(-5, -30);
            
            // Map background (dark terrain)
            _mapBackground = mapArea.AddComponent<RawImage>();
            _mapBackground.color = new Color(0.15f, 0.2f, 0.15f);
            
            // Make clickable
            Button mapBtn = mapArea.AddComponent<Button>();
            mapBtn.onClick.AddListener(() => HandleMapClick(Input.mousePosition));
            
            // Mask for markers
            Mask mask = mapArea.AddComponent<Mask>();
            mask.showMaskGraphic = true;
        }

        private void CreatePlayerMarker()
        {
            _playerMarker = new GameObject("PlayerMarker");
            _playerMarker.transform.SetParent(_mapRect, false);
            
            RectTransform rect = _playerMarker.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(12, 12);
            rect.anchoredPosition = Vector2.zero;
            
            // Arrow pointing up (player direction)
            Image img = _playerMarker.AddComponent<Image>();
            img.color = playerMarkerColor;
            
            // Add glow effect
            UnityEngine.UI.Outline glow = _playerMarker.AddComponent<UnityEngine.UI.Outline>();
            glow.effectColor = new Color(1f, 1f, 0f, 0.5f);
            glow.effectDistance = new Vector2(2, 2);
            
            // Player icon text
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(_playerMarker.transform, false);
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
            iconText.text = "▲";
            iconText.fontSize = 12;
            iconText.alignment = TextAlignmentOptions.Center;
            iconText.color = Color.black;
        }

        private void CreatePingContainer()
        {
            _pingContainer = new GameObject("Pings");
            _pingContainer.transform.SetParent(_mapRect, false);
            
            RectTransform rect = _pingContainer.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void CreateTerritoryMarker(string id, Vector2 mapPos, Color color)
        {
            GameObject marker = new GameObject($"Territory_{id}");
            marker.transform.SetParent(_mapRect, false);
            
            RectTransform rect = marker.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(8, 8);
            rect.anchoredPosition = mapPos;
            
            Image img = marker.AddComponent<Image>();
            img.color = color;
            
            _territoryMarkers[id] = marker;
        }

        private void CreatePingMarker(Vector2 mapPos, Color color, string label)
        {
            GameObject ping = new GameObject("Ping");
            ping.transform.SetParent(_pingContainer.transform, false);
            
            RectTransform rect = ping.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(20, 20);
            rect.anchoredPosition = mapPos;
            
            Image img = ping.AddComponent<Image>();
            img.color = color;
            
            // Pulsing animation will be handled in Update
            _activePings.Add(new PingMarker
            {
                Object = ping,
                StartTime = Time.time,
                Duration = pingDuration,
                Color = color
            });
            
            // Label if provided
            if (!string.IsNullOrEmpty(label))
            {
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(ping.transform, false);
                
                RectTransform labelRect = labelObj.AddComponent<RectTransform>();
                labelRect.anchoredPosition = new Vector2(0, 15);
                
                TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
                labelText.text = label;
                labelText.fontSize = 10;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.color = color;
            }
        }

        private void CreateDemoTerritories()
        {
            // Create demo territory markers
            string[] ids = { "t1", "t2", "t3", "t4", "t5", "t6", "t7", "t8", "t9", "t10" };
            
            for (int i = 0; i < ids.Length; i++)
            {
                float angle = i * (360f / ids.Length) * Mathf.Deg2Rad;
                float radius = 60 + (i % 3) * 20;
                Vector2 pos = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                
                TerritoryOwnership ownership = i switch
                {
                    0 or 1 or 2 => TerritoryOwnership.Owned,
                    3 or 4 => TerritoryOwnership.Ally,
                    5 or 6 => TerritoryOwnership.Enemy,
                    _ => TerritoryOwnership.Neutral
                };
                
                CreateTerritoryMarker(ids[i], pos, GetOwnershipColor(ownership));
            }
        }

        private void UpdatePlayerMarker()
        {
            if (_playerMarker == null || Camera.main == null) return;
            
            Vector3 camPos = Camera.main.transform.position;
            Vector2 mapPos = WorldToMapPosition(camPos);
            
            RectTransform rect = _playerMarker.GetComponent<RectTransform>();
            rect.anchoredPosition = mapPos;
            
            // Rotate based on camera direction (Y rotation)
            float yRot = Camera.main.transform.eulerAngles.y;
            rect.rotation = Quaternion.Euler(0, 0, -yRot);
        }

        private void UpdatePings()
        {
            for (int i = _activePings.Count - 1; i >= 0; i--)
            {
                var ping = _activePings[i];
                float elapsed = Time.time - ping.StartTime;
                
                if (elapsed >= ping.Duration)
                {
                    Destroy(ping.Object);
                    _activePings.RemoveAt(i);
                    continue;
                }
                
                // Pulsing effect
                float pulse = 1f + 0.3f * Mathf.Sin(elapsed * 8f);
                ping.Object.transform.localScale = Vector3.one * pulse;
                
                // Fade out in last second
                if (elapsed > ping.Duration - 1f)
                {
                    float alpha = 1f - (elapsed - (ping.Duration - 1f));
                    var img = ping.Object.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = new Color(ping.Color.r, ping.Color.g, ping.Color.b, alpha);
                    }
                }
            }
        }

        private void UpdateMapSize()
        {
            if (_panel == null) return;
            
            RectTransform rect = _panel.GetComponent<RectTransform>();
            float newSize = _isExpanded ? mapSize * 2 : mapSize;
            rect.sizeDelta = new Vector2(newSize + 10, newSize + 35);
        }

        private void HandleMapClick(Vector3 screenPos)
        {
            // Convert screen position to map position, then to world
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapRect, screenPos, null, out Vector2 localPoint);
            
            Vector3 worldPos = MapToWorldPosition(localPoint);
            OnMapClick?.Invoke(localPoint);
            
            // Move camera to clicked location
            if (Camera.main != null)
            {
                // PCCameraController would handle smooth transition
                Debug.Log($"[MiniMap] Clicked map at world pos: {worldPos}");
            }
        }

        private Vector2 WorldToMapPosition(Vector3 worldPos)
        {
            float halfWorld = worldSize / 2f;
            float halfMap = mapSize / 2f;
            
            float x = (worldPos.x / halfWorld) * halfMap;
            float y = (worldPos.z / halfWorld) * halfMap;
            
            return new Vector2(x, y);
        }

        private Vector3 MapToWorldPosition(Vector2 mapPos)
        {
            float halfWorld = worldSize / 2f;
            float halfMap = mapSize / 2f;
            
            float x = (mapPos.x / halfMap) * halfWorld;
            float z = (mapPos.y / halfMap) * halfWorld;
            
            return new Vector3(x, 0, z);
        }

        private Color GetOwnershipColor(TerritoryOwnership ownership)
        {
            return ownership switch
            {
                TerritoryOwnership.Owned => ownedColor,
                TerritoryOwnership.Enemy => enemyColor,
                TerritoryOwnership.Ally => allyColor,
                _ => neutralColor
            };
        }
    }

    public enum TerritoryOwnership
    {
        Neutral,
        Owned,
        Enemy,
        Ally
    }

    public class PingMarker
    {
        public GameObject Object;
        public float StartTime;
        public float Duration;
        public Color Color;
    }
}
