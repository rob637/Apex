using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// World Map Panel - Explore the game world, scout territories, and plan conquests.
    /// Features region exploration, fog of war, resource deposits, and strategic planning.
    /// </summary>
    public class WorldMapPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.3f, 0.7f, 0.5f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color allyColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color enemyColor = new Color(0.9f, 0.3f, 0.3f);
        [SerializeField] private Color neutralColor = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color ownedColor = new Color(0.3f, 0.8f, 0.3f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _mapArea;
        private GameObject _infoPanel;
        private MapRegion _selectedRegion;
        private MapLayer _currentLayer = MapLayer.Political;
        private float _zoomLevel = 1f;
        private Vector2 _panOffset = Vector2.zero;
        
        // Map data
        private List<MapRegion> _regions = new List<MapRegion>();
        private List<MapMarker> _markers = new List<MapMarker>();
        private List<ScoutReport> _scoutReports = new List<ScoutReport>();
        private int _scoutingRange = 5;
        private int _totalRegions = 100;
        private int _exploredRegions = 45;
        
        public static WorldMapPanel Instance { get; private set; }
        
        public event Action<MapRegion> OnRegionSelected;
        public event Action<MapRegion> OnRegionScouted;
        public event Action<MapRegion> OnAttackInitiated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeMapData();
        }

        private void Start()
        {
            CreatePanel();
            Hide();
        }

        private void InitializeMapData()
        {
            // Create map regions (grid-based world map)
            string[] regionNames = { "Northlands", "Dragon Peak", "Crystal Valley", "Iron Mountains", "Shadow Forest", 
                                    "Golden Plains", "Mystic Lake", "Storm Coast", "Ancient Ruins", "Volcanic Wastes" };
            
            _regions = new List<MapRegion>
            {
                // Your territories
                new MapRegion
                {
                    RegionId = "R001",
                    Name = "Dragonspire Citadel",
                    GridX = 5, GridY = 5,
                    Type = RegionType.Citadel,
                    Owner = "You",
                    OwnerType = OwnerType.Player,
                    IsExplored = true,
                    Level = 15,
                    Power = 125000,
                    Resources = new Dictionary<ResourceType, int> { { ResourceType.Gold, 5000 }, { ResourceType.Stone, 3000 } },
                    Description = "Your home base. The heart of your empire.",
                    Garrison = 5000
                },
                new MapRegion
                {
                    RegionId = "R002",
                    Name = "Northern Outpost",
                    GridX = 5, GridY = 7,
                    Type = RegionType.Outpost,
                    Owner = "You",
                    OwnerType = OwnerType.Player,
                    IsExplored = true,
                    Level = 8,
                    Power = 35000,
                    Resources = new Dictionary<ResourceType, int> { { ResourceType.Wood, 2000 } },
                    Description = "Strategic outpost watching the northern border.",
                    Garrison = 1500
                },
                new MapRegion
                {
                    RegionId = "R003",
                    Name = "Eastern Mines",
                    GridX = 7, GridY = 5,
                    Type = RegionType.ResourceNode,
                    Owner = "You",
                    OwnerType = OwnerType.Player,
                    IsExplored = true,
                    Level = 6,
                    Power = 15000,
                    Resources = new Dictionary<ResourceType, int> { { ResourceType.Iron, 500 }, { ResourceType.Gold, 1000 } },
                    Description = "Rich iron deposits with gold veins.",
                    Garrison = 800,
                    ProductionBonus = "+25% Iron production"
                },
                
                // Alliance territories
                new MapRegion
                {
                    RegionId = "R010",
                    Name = "Phoenix Keep",
                    GridX = 4, GridY = 6,
                    Type = RegionType.Citadel,
                    Owner = "DragonSlayer99",
                    Guild = "Dragon Slayers",
                    OwnerType = OwnerType.Ally,
                    IsExplored = true,
                    Level = 18,
                    Power = 180000,
                    Description = "Allied fortress of the Dragon Slayers guild.",
                    Garrison = 8000
                },
                new MapRegion
                {
                    RegionId = "R011",
                    Name = "Storm Harbor",
                    GridX = 3, GridY = 5,
                    Type = RegionType.Port,
                    Owner = "ShadowBlade",
                    Guild = "Dragon Slayers",
                    OwnerType = OwnerType.Ally,
                    IsExplored = true,
                    Level = 12,
                    Power = 75000,
                    Description = "Major trading port controlled by allies.",
                    Garrison = 3000,
                    ProductionBonus = "+15% Trade income"
                },
                
                // Enemy territories
                new MapRegion
                {
                    RegionId = "R020",
                    Name = "Dark Fortress",
                    GridX = 8, GridY = 7,
                    Type = RegionType.Citadel,
                    Owner = "ShadowLord",
                    Guild = "Shadow Legion",
                    OwnerType = OwnerType.Enemy,
                    IsExplored = true,
                    Level = 20,
                    Power = 250000,
                    Description = "Enemy stronghold. Heavily fortified.",
                    Garrison = 12000
                },
                new MapRegion
                {
                    RegionId = "R021",
                    Name = "Bloodstone Pass",
                    GridX = 7, GridY = 6,
                    Type = RegionType.Fortress,
                    Owner = "IronFist",
                    Guild = "Shadow Legion",
                    OwnerType = OwnerType.Enemy,
                    IsExplored = true,
                    Level = 14,
                    Power = 95000,
                    Description = "Strategic mountain pass. Controls trade routes.",
                    Garrison = 5000
                },
                
                // Neutral territories
                new MapRegion
                {
                    RegionId = "R030",
                    Name = "Ancient Temple",
                    GridX = 6, GridY = 8,
                    Type = RegionType.Wonder,
                    Owner = "Neutral",
                    OwnerType = OwnerType.Neutral,
                    IsExplored = true,
                    Level = 10,
                    Power = 50000,
                    Description = "Ancient temple with powerful buffs. Worth capturing!",
                    Garrison = 3000,
                    ProductionBonus = "+10% All production"
                },
                new MapRegion
                {
                    RegionId = "R031",
                    Name = "Crystal Caverns",
                    GridX = 4, GridY = 4,
                    Type = RegionType.ResourceNode,
                    Owner = "Neutral",
                    OwnerType = OwnerType.Neutral,
                    IsExplored = true,
                    Level = 5,
                    Power = 20000,
                    Resources = new Dictionary<ResourceType, int> { { ResourceType.Crystal, 100 } },
                    Description = "Rich crystal deposits. Valuable resource node.",
                    Garrison = 1000,
                    ProductionBonus = "+50% Crystal production"
                },
                new MapRegion
                {
                    RegionId = "R032",
                    Name = "Wild Lands",
                    GridX = 9, GridY = 5,
                    Type = RegionType.Wilderness,
                    Owner = "Neutral",
                    OwnerType = OwnerType.Neutral,
                    IsExplored = true,
                    Level = 3,
                    Power = 8000,
                    Description = "Untamed wilderness. Easy to capture.",
                    Garrison = 500
                },
                
                // Unexplored regions
                new MapRegion
                {
                    RegionId = "R040",
                    Name = "???",
                    GridX = 10, GridY = 8,
                    Type = RegionType.Unknown,
                    Owner = "Unknown",
                    OwnerType = OwnerType.Unknown,
                    IsExplored = false,
                    Description = "Unexplored region. Send scouts to reveal."
                },
                new MapRegion
                {
                    RegionId = "R041",
                    Name = "???",
                    GridX = 2, GridY = 3,
                    Type = RegionType.Unknown,
                    Owner = "Unknown",
                    OwnerType = OwnerType.Unknown,
                    IsExplored = false,
                    Description = "Unexplored region. Send scouts to reveal."
                }
            };
            
            // Map markers (points of interest)
            _markers = new List<MapMarker>
            {
                new MapMarker { MarkerId = "M001", Name = "World Boss: Dragon", GridX = 6, GridY = 9, Type = MarkerType.Boss, Description = "Level 50 World Boss. Spawns daily." },
                new MapMarker { MarkerId = "M002", Name = "Trade Route", GridX = 5, GridY = 4, Type = MarkerType.Trade, Description = "Caravan trade route. +20% gold from trades." },
                new MapMarker { MarkerId = "M003", Name = "Battle Zone", GridX = 7, GridY = 7, Type = MarkerType.Battle, Description = "Active conflict zone. PvP enabled." },
                new MapMarker { MarkerId = "M004", Name = "Treasure Chest", GridX = 8, GridY = 4, Type = MarkerType.Treasure, Description = "Unclaimed treasure. First come, first served!" }
            };
            
            // Scout reports
            _scoutReports = new List<ScoutReport>
            {
                new ScoutReport { ReportId = "SR001", RegionId = "R020", Timestamp = DateTime.Now.AddHours(-2), TroopEstimate = "10,000-15,000", DefenseRating = "Very High", Notes = "Multiple defensive structures detected." },
                new ScoutReport { ReportId = "SR002", RegionId = "R030", Timestamp = DateTime.Now.AddHours(-6), TroopEstimate = "2,000-4,000", DefenseRating = "Medium", Notes = "Neutral guards. Can be captured with medium force." }
            };
        }

        private void CreatePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel (fullscreen)
            _panel = new GameObject("WorldMapPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.03f, 0.05f, 0.98f);
            
            HorizontalLayoutGroup hlayout = _panel.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 0;
            hlayout.padding = new RectOffset(0, 0, 0, 0);
            
            // Left sidebar (controls)
            CreateSidebar();
            
            // Main map area
            CreateMapArea();
            
            // Right info panel
            CreateInfoPanel();
        }

        private void CreateSidebar()
        {
            GameObject sidebar = new GameObject("Sidebar");
            sidebar.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = sidebar.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            le.flexibleHeight = 1;
            
            Image bg = sidebar.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.06f, 0.08f);
            
            VerticalLayoutGroup vlayout = sidebar.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(10, 10, 15, 15);
            
            // Header
            CreateText(sidebar.transform, "🗺️ WORLD MAP", 18, TextAlignmentOptions.Center, accentColor);
            
            // Exploration progress
            CreateExplorationProgress(sidebar.transform);
            
            // Layer buttons
            CreateLayerButtons(sidebar.transform);
            
            // Zoom controls
            CreateZoomControls(sidebar.transform);
            
            // Quick actions
            CreateQuickActions(sidebar.transform);
            
            // Legend
            CreateLegend(sidebar.transform);
            
            // Close button
            CreateCloseButton(sidebar.transform);
        }

        private void CreateExplorationProgress(Transform parent)
        {
            GameObject progress = new GameObject("ExplorationProgress");
            progress.transform.SetParent(parent, false);
            
            LayoutElement le = progress.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = progress.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.08f);
            
            VerticalLayoutGroup vlayout = progress.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            CreateText(progress.transform, "🔍 Exploration", 11, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            CreateText(progress.transform, $"{_exploredRegions}/{_totalRegions} regions", 14, TextAlignmentOptions.Center, accentColor);
            
            // Progress bar
            float prog = (float)_exploredRegions / _totalRegions;
            CreateProgressBar(progress.transform, prog, accentColor);
        }

        private void CreateProgressBar(Transform parent, float progress, Color color)
        {
            GameObject bar = new GameObject("ProgressBar");
            bar.transform.SetParent(parent, false);
            
            LayoutElement le = bar.AddComponent<LayoutElement>();
            le.preferredHeight = 10;
            
            Image bgImg = bar.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = color;
        }

        private void CreateLayerButtons(Transform parent)
        {
            CreateSectionHeader(parent, "📊 MAP LAYERS");
            
            GameObject layers = new GameObject("Layers");
            layers.transform.SetParent(parent, false);
            
            VerticalLayoutGroup vlayout = layers.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;
            
            CreateLayerButton(layers.transform, MapLayer.Political, "🏳️ Political");
            CreateLayerButton(layers.transform, MapLayer.Resources, "💎 Resources");
            CreateLayerButton(layers.transform, MapLayer.Military, "⚔️ Military");
            CreateLayerButton(layers.transform, MapLayer.Trade, "🏪 Trade Routes");
        }

        private void CreateLayerButton(Transform parent, MapLayer layer, string label)
        {
            GameObject btn = new GameObject($"Layer_{layer}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            Color bgColor = layer == _currentLayer ? accentColor : new Color(0.15f, 0.15f, 0.18f);
            
            Image bg = btn.AddComponent<Image>();
            bg.color = bgColor;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => SetLayer(layer));
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 11;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateZoomControls(Transform parent)
        {
            CreateSectionHeader(parent, "🔍 ZOOM");
            
            GameObject zoom = new GameObject("ZoomControls");
            zoom.transform.SetParent(parent, false);
            
            LayoutElement le = zoom.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = zoom.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 5;
            
            CreateZoomButton(zoom.transform, "-", () => ZoomOut());
            CreateText(zoom.transform, $"{(_zoomLevel * 100):F0}%", 12, TextAlignmentOptions.Center, Color.white);
            CreateZoomButton(zoom.transform, "+", () => ZoomIn());
        }

        private void CreateZoomButton(Transform parent, string label, Action onClick)
        {
            GameObject btn = new GameObject($"Zoom_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 35;
            le.preferredHeight = 35;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f);
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateQuickActions(Transform parent)
        {
            CreateSectionHeader(parent, "⚡ QUICK ACTIONS");
            
            GameObject actions = new GameObject("QuickActions");
            actions.transform.SetParent(parent, false);
            
            VerticalLayoutGroup vlayout = actions.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;
            
            CreateActionButton(actions.transform, "🏠 Go to Home", () => GoToHome());
            CreateActionButton(actions.transform, "🔭 Scout Region", () => ScoutSelectedRegion());
            CreateActionButton(actions.transform, "📍 Set Waypoint", () => SetWaypoint());
        }

        private void CreateActionButton(Transform parent, string label, Action onClick)
        {
            GameObject btn = new GameObject($"Action_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = 32;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.2f, 0.15f);
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 11;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateLegend(Transform parent)
        {
            CreateSectionHeader(parent, "📋 LEGEND");
            
            GameObject legend = new GameObject("Legend");
            legend.transform.SetParent(parent, false);
            
            VerticalLayoutGroup vlayout = legend.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 3;
            
            CreateLegendItem(legend.transform, ownedColor, "Your Territory");
            CreateLegendItem(legend.transform, allyColor, "Allied");
            CreateLegendItem(legend.transform, enemyColor, "Enemy");
            CreateLegendItem(legend.transform, neutralColor, "Neutral");
            CreateLegendItem(legend.transform, new Color(0.3f, 0.3f, 0.3f), "Unexplored");
        }

        private void CreateLegendItem(Transform parent, Color color, string label)
        {
            GameObject item = new GameObject($"Legend_{label}");
            item.transform.SetParent(parent, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 20;
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 8;
            
            // Color box
            GameObject colorBox = new GameObject("ColorBox");
            colorBox.transform.SetParent(item.transform, false);
            
            LayoutElement boxLE = colorBox.AddComponent<LayoutElement>();
            boxLE.preferredWidth = 15;
            boxLE.preferredHeight = 15;
            
            Image boxImg = colorBox.AddComponent<Image>();
            boxImg.color = color;
            
            CreateText(item.transform, label, 10, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<LayoutElement>().flexibleHeight = 1;
            
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(parent, false);
            
            LayoutElement le = closeBtn.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            Image bg = closeBtn.AddComponent<Image>();
            bg.color = new Color(0.5f, 0.2f, 0.2f);
            
            Button btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(Hide);
            
            TextMeshProUGUI text = closeBtn.AddComponent<TextMeshProUGUI>();
            text.text = "✕ Close Map";
            text.fontSize = 12;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateMapArea()
        {
            _mapArea = new GameObject("MapArea");
            _mapArea.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _mapArea.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            
            Image bg = _mapArea.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.04f, 0.06f);
            
            // Create grid-based map
            CreateMapGrid();
            
            // Create markers overlay
            CreateMarkersOverlay();
        }

        private void CreateMapGrid()
        {
            // Simplified visual representation of the map
            // In a real implementation, this would be a proper 2D map with zoom/pan
            
            foreach (var region in _regions)
            {
                CreateRegionTile(region);
            }
        }

        private void CreateRegionTile(MapRegion region)
        {
            GameObject tile = new GameObject($"Region_{region.RegionId}");
            tile.transform.SetParent(_mapArea.transform, false);
            
            RectTransform rect = tile.AddComponent<RectTransform>();
            
            // Position based on grid coordinates (simplified positioning)
            float tileSize = 60 * _zoomLevel;
            float x = (region.GridX - 5) * tileSize;
            float y = (region.GridY - 5) * tileSize;
            
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(tileSize - 2, tileSize - 2);
            rect.anchoredPosition = new Vector2(x + _panOffset.x, y + _panOffset.y);
            
            Color tileColor = GetRegionColor(region);
            
            Image bg = tile.AddComponent<Image>();
            bg.color = tileColor;
            
            // Border for selection
            bool isSelected = _selectedRegion?.RegionId == region.RegionId;
            if (isSelected)
            {
                UnityEngine.UI.Outline outline = tile.AddComponent<Outline>();
                outline.effectColor = goldColor;
                outline.effectDistance = new Vector2(3, 3);
            }
            
            Button btn = tile.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectRegion(region));
            
            // Region icon
            string icon = GetRegionIcon(region);
            TextMeshProUGUI iconText = tile.AddComponent<TextMeshProUGUI>();
            iconText.text = icon;
            iconText.fontSize = (int)(20 * _zoomLevel);
            iconText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateMarkersOverlay()
        {
            foreach (var marker in _markers)
            {
                CreateMarkerPin(marker);
            }
        }

        private void CreateMarkerPin(MapMarker marker)
        {
            GameObject pin = new GameObject($"Marker_{marker.MarkerId}");
            pin.transform.SetParent(_mapArea.transform, false);
            
            RectTransform rect = pin.AddComponent<RectTransform>();
            
            float tileSize = 60 * _zoomLevel;
            float x = (marker.GridX - 5) * tileSize;
            float y = (marker.GridY - 5) * tileSize + (tileSize / 2);
            
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(25, 25);
            rect.anchoredPosition = new Vector2(x + _panOffset.x, y + _panOffset.y);
            
            Image bg = pin.AddComponent<Image>();
            bg.color = GetMarkerColor(marker.Type);
            
            Button btn = pin.AddComponent<Button>();
            btn.onClick.AddListener(() => ShowMarkerInfo(marker));
            
            TextMeshProUGUI text = pin.AddComponent<TextMeshProUGUI>();
            text.text = GetMarkerIcon(marker.Type);
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateInfoPanel()
        {
            _infoPanel = new GameObject("InfoPanel");
            _infoPanel.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _infoPanel.AddComponent<LayoutElement>();
            le.preferredWidth = 300;
            le.flexibleHeight = 1;
            
            Image bg = _infoPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.06f, 0.08f);
            
            VerticalLayoutGroup vlayout = _infoPanel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            RefreshInfoPanel();
        }

        private void RefreshInfoPanel()
        {
            foreach (Transform child in _infoPanel.transform)
            {
                Destroy(child.gameObject);
            }
            
            if (_selectedRegion == null)
            {
                CreateText(_infoPanel.transform, "Select a region\nto view details", 14, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
                return;
            }
            
            // Region header
            CreateRegionHeader();
            
            if (!_selectedRegion.IsExplored)
            {
                CreateUnexploredInfo();
                return;
            }
            
            // Owner info
            CreateOwnerInfo();
            
            // Stats
            CreateRegionStats();
            
            // Resources
            if (_selectedRegion.Resources != null && _selectedRegion.Resources.Count > 0)
            {
                CreateResourcesInfo();
            }
            
            // Scout report
            CreateScoutReportInfo();
            
            // Action buttons
            CreateRegionActions();
        }

        private void CreateRegionHeader()
        {
            GameObject header = new GameObject("RegionHeader");
            header.transform.SetParent(_infoPanel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            VerticalLayoutGroup vlayout = header.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            
            // Icon
            string icon = GetRegionIcon(_selectedRegion);
            CreateText(header.transform, icon, 36, TextAlignmentOptions.Center);
            
            // Name
            Color nameColor = GetRegionColor(_selectedRegion);
            CreateText(header.transform, _selectedRegion.Name, 16, TextAlignmentOptions.Center, nameColor);
            
            // Type
            CreateText(header.transform, _selectedRegion.Type.ToString(), 11, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateUnexploredInfo()
        {
            GameObject info = new GameObject("UnexploredInfo");
            info.transform.SetParent(_infoPanel.transform, false);
            
            LayoutElement le = info.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            Image bg = info.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.12f);
            
            VerticalLayoutGroup vlayout = info.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 20, 20);
            
            CreateText(info.transform, "🔒 UNEXPLORED", 14, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            CreateText(info.transform, "Send scouts to reveal this region", 11, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            
            // Scout button
            CreateActionButtonLarge(info.transform, "🔭 Scout Region", () => ScoutRegion(_selectedRegion), accentColor);
        }

        private void CreateOwnerInfo()
        {
            CreateSectionHeader(_infoPanel.transform, "👤 OWNER");
            
            GameObject owner = new GameObject("OwnerInfo");
            owner.transform.SetParent(_infoPanel.transform, false);
            
            LayoutElement le = owner.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            Color ownerColor = GetOwnerTypeColor(_selectedRegion.OwnerType);
            
            Image bg = owner.AddComponent<Image>();
            bg.color = new Color(ownerColor.r * 0.2f, ownerColor.g * 0.2f, ownerColor.b * 0.2f);
            
            VerticalLayoutGroup vlayout = owner.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(10, 10, 8, 8);
            
            CreateText(owner.transform, _selectedRegion.Owner, 14, TextAlignmentOptions.Center, ownerColor);
            
            if (!string.IsNullOrEmpty(_selectedRegion.Guild))
            {
                CreateText(owner.transform, $"[{_selectedRegion.Guild}]", 10, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            }
        }

        private void CreateRegionStats()
        {
            CreateSectionHeader(_infoPanel.transform, "📊 STATISTICS");
            
            GameObject stats = new GameObject("Stats");
            stats.transform.SetParent(_infoPanel.transform, false);
            
            GridLayoutGroup grid = stats.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(130, 35);
            grid.spacing = new Vector2(5, 5);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            
            CreateStatCell(stats.transform, "⭐ Level", _selectedRegion.Level.ToString());
            CreateStatCell(stats.transform, "⚔️ Power", $"{(_selectedRegion.Power / 1000f):F1}K");
            CreateStatCell(stats.transform, "🛡️ Garrison", $"{_selectedRegion.Garrison:N0}");
            CreateStatCell(stats.transform, "📍 Position", $"({_selectedRegion.GridX}, {_selectedRegion.GridY})");
            
            if (!string.IsNullOrEmpty(_selectedRegion.ProductionBonus))
            {
                CreateText(_infoPanel.transform, $"✨ Bonus: {_selectedRegion.ProductionBonus}", 11, TextAlignmentOptions.Center, goldColor);
            }
        }

        private void CreateStatCell(Transform parent, string label, string value)
        {
            GameObject cell = new GameObject("StatCell");
            cell.transform.SetParent(parent, false);
            
            Image bg = cell.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.08f);
            
            VerticalLayoutGroup vlayout = cell.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(5, 5, 5, 5);
            
            CreateText(cell.transform, label, 9, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            CreateText(cell.transform, value, 12, TextAlignmentOptions.Center, Color.white);
        }

        private void CreateResourcesInfo()
        {
            CreateSectionHeader(_infoPanel.transform, "💎 RESOURCES");
            
            GameObject resources = new GameObject("Resources");
            resources.transform.SetParent(_infoPanel.transform, false);
            
            HorizontalLayoutGroup hlayout = resources.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 10;
            
            foreach (var kvp in _selectedRegion.Resources)
            {
                CreateResourceItem(resources.transform, kvp.Key, kvp.Value);
            }
        }

        private void CreateResourceItem(Transform parent, ResourceType resource, int amount)
        {
            GameObject item = new GameObject($"Resource_{resource}");
            item.transform.SetParent(parent, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 40;
            
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.08f);
            
            VerticalLayoutGroup vlayout = item.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(item.transform, GetResourceIcon(resource), 18, TextAlignmentOptions.Center);
            CreateText(item.transform, $"+{amount}/h", 10, TextAlignmentOptions.Center, accentColor);
        }

        private void CreateScoutReportInfo()
        {
            var report = _scoutReports.Find(r => r.RegionId == _selectedRegion.RegionId);
            if (report == null) return;
            
            CreateSectionHeader(_infoPanel.transform, "🔭 SCOUT REPORT");
            
            GameObject reportInfo = new GameObject("ScoutReport");
            reportInfo.transform.SetParent(_infoPanel.transform, false);
            
            LayoutElement le = reportInfo.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Image bg = reportInfo.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.05f);
            
            VerticalLayoutGroup vlayout = reportInfo.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 3;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            CreateText(reportInfo.transform, $"⚔️ Troops: {report.TroopEstimate}", 11, TextAlignmentOptions.Center, Color.white);
            CreateText(reportInfo.transform, $"🛡️ Defense: {report.DefenseRating}", 11, TextAlignmentOptions.Center, Color.white);
            CreateText(reportInfo.transform, report.Notes, 9, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            CreateText(reportInfo.transform, $"📅 {GetTimeAgo(report.Timestamp)}", 9, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateRegionActions()
        {
            GameObject actions = new GameObject("Actions");
            actions.transform.SetParent(_infoPanel.transform, false);
            
            LayoutElement le = actions.AddComponent<LayoutElement>();
            le.preferredHeight = 120;
            
            VerticalLayoutGroup vlayout = actions.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 8;
            
            switch (_selectedRegion.OwnerType)
            {
                case OwnerType.Player:
                    CreateActionButtonLarge(actions.transform, "🏠 View Details", () => ViewRegionDetails(_selectedRegion), accentColor);
                    CreateActionButtonLarge(actions.transform, "🛡️ Reinforce", () => ReinforceRegion(_selectedRegion), allyColor);
                    break;
                    
                case OwnerType.Ally:
                    CreateActionButtonLarge(actions.transform, "🤝 Send Reinforcements", () => SendReinforcements(_selectedRegion), allyColor);
                    CreateActionButtonLarge(actions.transform, "💬 Message Owner", () => MessageOwner(_selectedRegion), new Color(0.5f, 0.5f, 0.6f));
                    break;
                    
                case OwnerType.Enemy:
                    CreateActionButtonLarge(actions.transform, "⚔️ ATTACK", () => AttackRegion(_selectedRegion), enemyColor);
                    CreateActionButtonLarge(actions.transform, "🔭 Scout Again", () => ScoutRegion(_selectedRegion), new Color(0.4f, 0.4f, 0.5f));
                    break;
                    
                case OwnerType.Neutral:
                    CreateActionButtonLarge(actions.transform, "⚔️ Capture", () => CaptureRegion(_selectedRegion), accentColor);
                    CreateActionButtonLarge(actions.transform, "🔭 Scout", () => ScoutRegion(_selectedRegion), new Color(0.4f, 0.4f, 0.5f));
                    break;
            }
        }

        private void CreateActionButtonLarge(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btn = new GameObject($"Action_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 13;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        #region Helpers

        private void CreateSectionHeader(Transform parent, string text)
        {
            GameObject header = new GameObject("SectionHeader");
            header.transform.SetParent(parent, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 20;
            
            TextMeshProUGUI tmp = header.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 11;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = accentColor;
            tmp.alignment = TextAlignmentOptions.Left;
        }

        private GameObject CreateText(Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Color? color = null)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color ?? Color.white;
            tmp.enableWordWrapping = true;
            
            return obj;
        }

        private Color GetRegionColor(MapRegion region)
        {
            if (!region.IsExplored) return new Color(0.2f, 0.2f, 0.25f);
            
            return region.OwnerType switch
            {
                OwnerType.Player => ownedColor,
                OwnerType.Ally => allyColor,
                OwnerType.Enemy => enemyColor,
                OwnerType.Neutral => neutralColor,
                _ => new Color(0.3f, 0.3f, 0.3f)
            };
        }

        private Color GetOwnerTypeColor(OwnerType type)
        {
            return type switch
            {
                OwnerType.Player => ownedColor,
                OwnerType.Ally => allyColor,
                OwnerType.Enemy => enemyColor,
                OwnerType.Neutral => neutralColor,
                _ => Color.white
            };
        }

        private string GetRegionIcon(MapRegion region)
        {
            if (!region.IsExplored) return "❓";
            
            return region.Type switch
            {
                RegionType.Citadel => "🏰",
                RegionType.Fortress => "🏯",
                RegionType.Outpost => "🏕️",
                RegionType.Port => "⚓",
                RegionType.ResourceNode => "⛏️",
                RegionType.Wonder => "🏛️",
                RegionType.Wilderness => "🌲",
                _ => "📍"
            };
        }

        private string GetMarkerIcon(MarkerType type)
        {
            return type switch
            {
                MarkerType.Boss => "👹",
                MarkerType.Trade => "🏪",
                MarkerType.Battle => "⚔️",
                MarkerType.Treasure => "💎",
                _ => "📍"
            };
        }

        private Color GetMarkerColor(MarkerType type)
        {
            return type switch
            {
                MarkerType.Boss => new Color(0.8f, 0.2f, 0.2f),
                MarkerType.Trade => goldColor,
                MarkerType.Battle => new Color(0.9f, 0.5f, 0.2f),
                MarkerType.Treasure => new Color(0.9f, 0.8f, 0.2f),
                _ => Color.white
            };
        }

        private string GetResourceIcon(ResourceType resource)
        {
            return resource switch
            {
                ResourceType.Gold => "💰",
                ResourceType.Stone => "🪨",
                ResourceType.Wood => "🪵",
                ResourceType.Iron => "⚙️",
                ResourceType.Crystal => "💎",
                _ => "📦"
            };
        }

        private string GetTimeAgo(DateTime time)
        {
            TimeSpan diff = DateTime.Now - time;
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return $"{(int)diff.TotalDays}d ago";
        }

        #endregion

        #region Actions

        private void SetLayer(MapLayer layer)
        {
            _currentLayer = layer;
            RefreshMapDisplay();
        }

        private void ZoomIn()
        {
            _zoomLevel = Mathf.Min(_zoomLevel + 0.25f, 2f);
            RefreshMapDisplay();
        }

        private void ZoomOut()
        {
            _zoomLevel = Mathf.Max(_zoomLevel - 0.25f, 0.5f);
            RefreshMapDisplay();
        }

        private void GoToHome()
        {
            _panOffset = Vector2.zero;
            var home = _regions.Find(r => r.OwnerType == OwnerType.Player && r.Type == RegionType.Citadel);
            if (home != null)
            {
                SelectRegion(home);
            }
            RefreshMapDisplay();
        }

        private void SelectRegion(MapRegion region)
        {
            _selectedRegion = region;
            OnRegionSelected?.Invoke(region);
            RefreshMapDisplay();
            RefreshInfoPanel();
        }

        private void ShowMarkerInfo(MapMarker marker)
        {
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"{marker.Name}\n{marker.Description}");
            }
        }

        private void ScoutSelectedRegion()
        {
            if (_selectedRegion != null && !_selectedRegion.IsExplored)
            {
                ScoutRegion(_selectedRegion);
            }
        }

        private void ScoutRegion(MapRegion region)
        {
            region.IsExplored = true;
            _exploredRegions++;
            
            OnRegionScouted?.Invoke(region);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"Scouted {region.Name}!");
            }
            
            RefreshMapDisplay();
            RefreshInfoPanel();
        }

        private void SetWaypoint()
        {
            if (_selectedRegion != null)
            {
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowInfo($"Waypoint set at {_selectedRegion.Name}");
                }
            }
        }

        private void ViewRegionDetails(MapRegion region)
        {
            Debug.Log($"[WorldMap] View details: {region.Name}");
        }

        private void ReinforceRegion(MapRegion region)
        {
            Debug.Log($"[WorldMap] Reinforce: {region.Name}");
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo("Opening reinforcement panel...");
            }
        }

        private void SendReinforcements(MapRegion region)
        {
            Debug.Log($"[WorldMap] Send reinforcements to: {region.Name}");
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Sending reinforcements to {region.Owner}...");
            }
        }

        private void MessageOwner(MapRegion region)
        {
            Debug.Log($"[WorldMap] Message: {region.Owner}");
        }

        private void AttackRegion(MapRegion region)
        {
            OnAttackInitiated?.Invoke(region);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowAlert("Battle!", $"Attacking {region.Name}");
            }
            
            Debug.Log($"[WorldMap] Attack initiated: {region.Name}");
        }

        private void CaptureRegion(MapRegion region)
        {
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Marching to capture {region.Name}...");
            }
            
            Debug.Log($"[WorldMap] Capture: {region.Name}");
        }

        private void RefreshMapDisplay()
        {
            // Clear and recreate map tiles
            foreach (Transform child in _mapArea.transform)
            {
                Destroy(child.gameObject);
            }
            
            CreateMapGrid();
            CreateMarkersOverlay();
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel.SetActive(true);
            RefreshMapDisplay();
            RefreshInfoPanel();
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        public void Toggle()
        {
            if (_panel.activeSelf)
                Hide();
            else
                Show();
        }

        public void FocusOnRegion(string regionId)
        {
            var region = _regions.Find(r => r.RegionId == regionId);
            if (region != null)
            {
                SelectRegion(region);
                Show();
            }
        }

        public List<MapRegion> GetPlayerRegions() => _regions.FindAll(r => r.OwnerType == OwnerType.Player);
        public List<MapRegion> GetExploredRegions() => _regions.FindAll(r => r.IsExplored);
        public int GetExplorationProgress() => _exploredRegions * 100 / _totalRegions;

        #endregion
    }

    #region Data Classes

    public enum MapLayer
    {
        Political,
        Resources,
        Military,
        Trade
    }

    public enum RegionType
    {
        Unknown,
        Citadel,
        Fortress,
        Outpost,
        Port,
        ResourceNode,
        Wonder,
        Wilderness
    }

    public enum OwnerType
    {
        Unknown,
        Player,
        Ally,
        Enemy,
        Neutral
    }

    public enum MarkerType
    {
        Boss,
        Trade,
        Battle,
        Treasure
    }

    public class MapRegion
    {
        public string RegionId;
        public string Name;
        public int GridX;
        public int GridY;
        public RegionType Type;
        public string Owner;
        public string Guild;
        public OwnerType OwnerType;
        public bool IsExplored;
        public int Level;
        public int Power;
        public Dictionary<ResourceType, int> Resources;
        public string Description;
        public int Garrison;
        public string ProductionBonus;
    }

    public class MapMarker
    {
        public string MarkerId;
        public string Name;
        public int GridX;
        public int GridY;
        public MarkerType Type;
        public string Description;
    }

    public class ScoutReport
    {
        public string ReportId;
        public string RegionId;
        public DateTime Timestamp;
        public string TroopEstimate;
        public string DefenseRating;
        public string Notes;
    }

    #endregion
}
