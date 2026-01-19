using System;
using ApexCitadels.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Resource Collection Panel - Manage resource gathering, production buildings, and trade.
    /// Features auto-collection, production queues, and resource management.
    /// </summary>
    public class ResourceCollectionPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.4f, 0.75f, 0.4f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color warningColor = new Color(0.9f, 0.6f, 0.2f);
        [SerializeField] private Color dangerColor = new Color(0.9f, 0.3f, 0.3f);
        
        // UI References
        private GameObject _panel;
        private GameObject _contentArea;
        private ResourceTab _currentTab = ResourceTab.Overview;
        
        // Resource data
        private Dictionary<ResourceType, ResourceData> _resources = new Dictionary<ResourceType, ResourceData>();
        private List<ProductionBuilding> _buildings = new List<ProductionBuilding>();
        private List<ResourceBoost> _activeBoosts = new List<ResourceBoost>();
        private int _storageCapacity = 500000;
        private int _currentStorage = 287500;
        private float _autoCollectTimer = 300f; // 5 minutes
        private bool _autoCollectEnabled = true;
        
        public static ResourceCollectionPanel Instance { get; private set; }
        
        public event Action<ResourceType, int> OnResourceCollected;
        public event Action OnAutoCollect;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeResourceData();
        }

        private void Start()
        {
            CreatePanel();
            Hide();
        }

        private void Update()
        {
            UpdateProductionTimers();
        }

        private void InitializeResourceData()
        {
            _resources = new Dictionary<ResourceType, ResourceData>
            {
                { ResourceType.Gold, new ResourceData {
                    Type = ResourceType.Gold,
                    Icon = "💰",
                    Name = "Gold",
                    Current = 125000,
                    Capacity = 200000,
                    Production = 1250,
                    PendingCollection = 4500,
                    Description = "Primary currency for upgrades and troops."
                }},
                { ResourceType.Stone, new ResourceData {
                    Type = ResourceType.Stone,
                    Icon = "🪨",
                    Name = "Stone",
                    Current = 78500,
                    Capacity = 150000,
                    Production = 850,
                    PendingCollection = 2800,
                    Description = "Used for building and fortifications."
                }},
                { ResourceType.Wood, new ResourceData {
                    Type = ResourceType.Wood,
                    Icon = "🪵",
                    Name = "Wood",
                    Current = 92000,
                    Capacity = 150000,
                    Production = 920,
                    PendingCollection = 3200,
                    Description = "Essential for construction and siege weapons."
                }},
                { ResourceType.Iron, new ResourceData {
                    Type = ResourceType.Iron,
                    Icon = "⚙️",
                    Name = "Iron",
                    Current = 45000,
                    Capacity = 100000,
                    Production = 420,
                    PendingCollection = 1500,
                    Description = "Required for weapons and armor."
                }},
                { ResourceType.Crystal, new ResourceData {
                    Type = ResourceType.Crystal,
                    Icon = "💎",
                    Name = "Crystal",
                    Current = 8500,
                    Capacity = 25000,
                    Production = 85,
                    PendingCollection = 320,
                    Description = "Rare resource for advanced buildings."
                }},
                { ResourceType.ApexCoins, new ResourceData {
                    Type = ResourceType.ApexCoins,
                    Icon = "🌟",
                    Name = "Apex Coins",
                    Current = 1250,
                    Capacity = 99999,
                    Production = 0,
                    PendingCollection = 0,
                    Description = "Premium currency for special items."
                }}
            };
            
            _buildings = new List<ProductionBuilding>
            {
                // Gold production
                new ProductionBuilding {
                    BuildingId = "B001",
                    Name = "Gold Mine",
                    Level = 12,
                    Type = ResourceType.Gold,
                    BaseProduction = 500,
                    BonusProduction = 250,
                    PendingAmount = 2500,
                    MaxPending = 10000,
                    LastCollected = DateTime.Now.AddHours(-2),
                    CollectReadyTime = DateTime.Now.AddHours(2),
                    IsUpgrading = false
                },
                new ProductionBuilding {
                    BuildingId = "B002",
                    Name = "Treasury",
                    Level = 8,
                    Type = ResourceType.Gold,
                    BaseProduction = 300,
                    BonusProduction = 150,
                    PendingAmount = 2000,
                    MaxPending = 6000,
                    LastCollected = DateTime.Now.AddHours(-1),
                    CollectReadyTime = DateTime.Now.AddHours(3),
                    IsUpgrading = false
                },
                
                // Stone production
                new ProductionBuilding {
                    BuildingId = "B003",
                    Name = "Stone Quarry",
                    Level = 10,
                    Type = ResourceType.Stone,
                    BaseProduction = 400,
                    BonusProduction = 200,
                    PendingAmount = 1800,
                    MaxPending = 8000,
                    LastCollected = DateTime.Now.AddMinutes(-90),
                    CollectReadyTime = DateTime.Now.AddHours(1.5),
                    IsUpgrading = false
                },
                new ProductionBuilding {
                    BuildingId = "B004",
                    Name = "Mason's Guild",
                    Level = 6,
                    Type = ResourceType.Stone,
                    BaseProduction = 200,
                    BonusProduction = 50,
                    PendingAmount = 1000,
                    MaxPending = 4000,
                    LastCollected = DateTime.Now.AddHours(-1),
                    CollectReadyTime = DateTime.Now.AddHours(3),
                    IsUpgrading = true,
                    UpgradeCompleteTime = DateTime.Now.AddMinutes(45)
                },
                
                // Wood production
                new ProductionBuilding {
                    BuildingId = "B005",
                    Name = "Lumber Mill",
                    Level = 11,
                    Type = ResourceType.Wood,
                    BaseProduction = 450,
                    BonusProduction = 220,
                    PendingAmount = 2100,
                    MaxPending = 9000,
                    LastCollected = DateTime.Now.AddHours(-1.5),
                    CollectReadyTime = DateTime.Now.AddHours(2),
                    IsUpgrading = false
                },
                new ProductionBuilding {
                    BuildingId = "B006",
                    Name = "Forest Camp",
                    Level = 7,
                    Type = ResourceType.Wood,
                    BaseProduction = 250,
                    BonusProduction = 0,
                    PendingAmount = 1100,
                    MaxPending = 5000,
                    LastCollected = DateTime.Now.AddHours(-2),
                    CollectReadyTime = DateTime.Now.AddHours(1),
                    IsUpgrading = false
                },
                
                // Iron production
                new ProductionBuilding {
                    BuildingId = "B007",
                    Name = "Iron Forge",
                    Level = 9,
                    Type = ResourceType.Iron,
                    BaseProduction = 300,
                    BonusProduction = 120,
                    PendingAmount = 1000,
                    MaxPending = 6000,
                    LastCollected = DateTime.Now.AddMinutes(-40),
                    CollectReadyTime = DateTime.Now.AddHours(4),
                    IsUpgrading = false
                },
                
                // Crystal production
                new ProductionBuilding {
                    BuildingId = "B008",
                    Name = "Crystal Cave",
                    Level = 5,
                    Type = ResourceType.Crystal,
                    BaseProduction = 50,
                    BonusProduction = 35,
                    PendingAmount = 320,
                    MaxPending = 1000,
                    LastCollected = DateTime.Now.AddHours(-3),
                    CollectReadyTime = DateTime.Now.AddHours(6),
                    IsUpgrading = false
                }
            };
            
            _activeBoosts = new List<ResourceBoost>
            {
                new ResourceBoost {
                    BoostId = "BOOST001",
                    Name = "Gold Rush",
                    ResourceType = ResourceType.Gold,
                    BonusPercent = 50,
                    ExpiresAt = DateTime.Now.AddHours(2),
                    Icon = "⚡"
                },
                new ResourceBoost {
                    BoostId = "BOOST002",
                    Name = "VIP Production",
                    ResourceType = null,
                    BonusPercent = 25,
                    ExpiresAt = DateTime.Now.AddDays(3),
                    Icon = "👑"
                }
            };
        }

        private void CreatePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            _panel = new GameObject("ResourceCollectionPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.1f);
            rect.anchorMax = new Vector2(0.85f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.08f, 0.98f);
            
            VerticalLayoutGroup layout = _panel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 0;
            layout.padding = new RectOffset(0, 0, 0, 0);
            
            CreateHeader();
            CreateTabBar();
            CreateContentArea();
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = header.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.1f);
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            // Title
            CreateText(header.transform, "📦 RESOURCE CENTER", 20, TextAlignmentOptions.Left, accentColor);
            
            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(header.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Storage indicator
            CreateStorageIndicator(header.transform);
            
            // Auto-collect toggle
            CreateAutoCollectToggle(header.transform);
            
            // Close button
            CreateHeaderButton(header.transform, "✕", Hide, new Color(0.5f, 0.2f, 0.2f));
        }

        private void CreateStorageIndicator(Transform parent)
        {
            GameObject storage = new GameObject("StorageIndicator");
            storage.transform.SetParent(parent, false);
            
            LayoutElement le = storage.AddComponent<LayoutElement>();
            le.preferredWidth = 180;
            le.preferredHeight = 40;
            
            Image bg = storage.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.12f);
            
            VerticalLayoutGroup vlayout = storage.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(10, 10, 5, 5);
            
            float storagePercent = (float)_currentStorage / _storageCapacity;
            Color storageColor = storagePercent > 0.9f ? dangerColor : (storagePercent > 0.7f ? warningColor : accentColor);
            
            CreateText(storage.transform, $"🏪 Storage: {(storagePercent * 100):F0}%", 11, TextAlignmentOptions.Center, storageColor);
            CreateText(storage.transform, $"{FormatNumber(_currentStorage)}/{FormatNumber(_storageCapacity)}", 10, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateAutoCollectToggle(Transform parent)
        {
            GameObject toggle = new GameObject("AutoCollectToggle");
            toggle.transform.SetParent(parent, false);
            
            LayoutElement le = toggle.AddComponent<LayoutElement>();
            le.preferredWidth = 120;
            le.preferredHeight = 35;
            
            Color bgColor = _autoCollectEnabled ? accentColor : new Color(0.3f, 0.3f, 0.3f);
            
            Image bg = toggle.AddComponent<Image>();
            bg.color = bgColor;
            
            Button btn = toggle.AddComponent<Button>();
            btn.onClick.AddListener(ToggleAutoCollect);
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(toggle.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = _autoCollectEnabled ? "🔄 Auto: ON" : "🔄 Auto: OFF";
            text.fontSize = 11;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateHeaderButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btn = new GameObject("HeaderBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 40;
            le.preferredHeight = 40;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateTabBar()
        {
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = tabBar.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            
            Image bg = tabBar.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.06f);
            
            HorizontalLayoutGroup hlayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 5;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            CreateTab(tabBar.transform, ResourceTab.Overview, "📊 Overview");
            CreateTab(tabBar.transform, ResourceTab.Buildings, "🏗️ Buildings");
            CreateTab(tabBar.transform, ResourceTab.Boosts, "⚡ Boosts");
            CreateTab(tabBar.transform, ResourceTab.Trade, "🔄 Trade");
            CreateTab(tabBar.transform, ResourceTab.History, "📜 History");
        }

        private void CreateTab(Transform parent, ResourceTab tab, string label)
        {
            GameObject tabObj = new GameObject($"Tab_{tab}");
            tabObj.transform.SetParent(parent, false);
            
            bool isActive = tab == _currentTab;
            Color bgColor = isActive ? accentColor : new Color(0.12f, 0.14f, 0.12f);
            
            Image bg = tabObj.AddComponent<Image>();
            bg.color = bgColor;
            
            Button btn = tabObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SetTab(tab));
            
            LayoutElement le = tabObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 35;
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tabObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
        }

        private void CreateContentArea()
        {
            _contentArea = new GameObject("ContentArea");
            _contentArea.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _contentArea.AddComponent<LayoutElement>();
            le.flexibleHeight = 1;
            
            Image bg = _contentArea.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.06f, 0.05f);
            
            RefreshContent();
        }

        private void RefreshContent()
        {
            foreach (Transform child in _contentArea.transform)
            {
                Destroy(child.gameObject);
            }
            
            switch (_currentTab)
            {
                case ResourceTab.Overview:
                    CreateOverviewContent();
                    break;
                case ResourceTab.Buildings:
                    CreateBuildingsContent();
                    break;
                case ResourceTab.Boosts:
                    CreateBoostsContent();
                    break;
                case ResourceTab.Trade:
                    CreateTradeContent();
                    break;
                case ResourceTab.History:
                    CreateHistoryContent();
                    break;
            }
        }

        private void CreateOverviewContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            // Collect all button
            CreateCollectAllSection(content.transform);
            
            // Resource cards
            CreateResourceCardsSection(content.transform);
            
            // Active boosts summary
            CreateActiveBoostsSummary(content.transform);
        }

        private void CreateCollectAllSection(Transform parent)
        {
            GameObject section = new GameObject("CollectAllSection");
            section.transform.SetParent(parent, false);
            
            LayoutElement le = section.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            Image bg = section.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.15f, 0.1f);
            
            HorizontalLayoutGroup hlayout = section.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 30;
            hlayout.padding = new RectOffset(30, 30, 20, 20);
            
            // Pending resources summary
            int totalPending = 0;
            foreach (var res in _resources.Values)
            {
                totalPending += res.PendingCollection;
            }
            
            GameObject pending = new GameObject("PendingSummary");
            pending.transform.SetParent(section.transform, false);
            
            LayoutElement pendingLE = pending.AddComponent<LayoutElement>();
            pendingLE.flexibleWidth = 1;
            
            VerticalLayoutGroup pendingLayout = pending.AddComponent<VerticalLayoutGroup>();
            pendingLayout.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(pending.transform, "📦 READY TO COLLECT", 12, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            CreateText(pending.transform, $"+{FormatNumber(totalPending)}", 28, TextAlignmentOptions.Center, goldColor);
            CreateText(pending.transform, "Total Resources", 10, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            
            // Collect all button
            CreateCollectAllButton(section.transform, totalPending);
        }

        private void CreateCollectAllButton(Transform parent, int amount)
        {
            GameObject btn = new GameObject("CollectAllBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 180;
            le.preferredHeight = 60;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = accentColor;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(CollectAll);
            
            VerticalLayoutGroup vlayout = btn.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(btn.transform, "🎁 COLLECT ALL", 14, TextAlignmentOptions.Center, Color.white);
            CreateText(btn.transform, $"+{FormatNumber(amount)}", 11, TextAlignmentOptions.Center, new Color(0.9f, 0.9f, 0.9f));
        }

        private void CreateResourceCardsSection(Transform parent)
        {
            CreateSectionHeader(parent, "💎 YOUR RESOURCES");
            
            GameObject cards = new GameObject("ResourceCards");
            cards.transform.SetParent(parent, false);
            
            GridLayoutGroup grid = cards.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(200, 130);
            grid.spacing = new Vector2(15, 15);
            grid.padding = new RectOffset(20, 20, 10, 20);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            
            foreach (var res in _resources.Values)
            {
                CreateResourceCard(cards.transform, res);
            }
        }

        private void CreateResourceCard(Transform parent, ResourceData resource)
        {
            GameObject card = new GameObject($"ResourceCard_{resource.Type}");
            card.transform.SetParent(parent, false);
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.08f);
            
            VerticalLayoutGroup vlayout = card.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Icon and name
            GameObject header = new GameObject("Header");
            header.transform.SetParent(card.transform, false);
            
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.spacing = 10;
            
            CreateText(header.transform, resource.Icon, 24, TextAlignmentOptions.Center);
            CreateText(header.transform, resource.Name, 14, TextAlignmentOptions.Left, Color.white);
            
            // Current amount
            float fillPercent = (float)resource.Current / resource.Capacity;
            Color amountColor = fillPercent > 0.9f ? warningColor : Color.white;
            CreateText(card.transform, FormatNumber(resource.Current), 20, TextAlignmentOptions.Center, amountColor);
            
            // Capacity bar
            CreateCapacityBar(card.transform, fillPercent);
            
            // Production rate
            if (resource.Production > 0)
            {
                CreateText(card.transform, $"+{FormatNumber(resource.Production)}/hr", 11, TextAlignmentOptions.Center, accentColor);
            }
            
            // Pending collection
            if (resource.PendingCollection > 0)
            {
                GameObject collectRow = new GameObject("CollectRow");
                collectRow.transform.SetParent(card.transform, false);
                
                HorizontalLayoutGroup rowLayout = collectRow.AddComponent<HorizontalLayoutGroup>();
                rowLayout.childAlignment = TextAnchor.MiddleCenter;
                rowLayout.spacing = 5;
                
                CreateText(collectRow.transform, $"📦 +{FormatNumber(resource.PendingCollection)}", 10, TextAlignmentOptions.Center, goldColor);
                CreateCollectButton(collectRow.transform, resource);
            }
        }

        private void CreateCapacityBar(Transform parent, float fill)
        {
            GameObject bar = new GameObject("CapacityBar");
            bar.transform.SetParent(parent, false);
            
            LayoutElement le = bar.AddComponent<LayoutElement>();
            le.preferredHeight = 8;
            
            Image bgImg = bar.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f);
            
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(bar.transform, false);
            
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(fill), 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Color fillColor = fill > 0.9f ? warningColor : accentColor;
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = fillColor;
        }

        private void CreateCollectButton(Transform parent, ResourceData resource)
        {
            GameObject btn = new GameObject("CollectBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 60;
            le.preferredHeight = 22;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = accentColor;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => CollectResource(resource.Type));
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Collect";
            text.fontSize = 9;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateActiveBoostsSummary(Transform parent)
        {
            if (_activeBoosts.Count == 0) return;
            
            CreateSectionHeader(parent, "⚡ ACTIVE BOOSTS");
            
            GameObject boosts = new GameObject("ActiveBoosts");
            boosts.transform.SetParent(parent, false);
            
            HorizontalLayoutGroup hlayout = boosts.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(20, 20, 10, 20);
            
            foreach (var boost in _activeBoosts)
            {
                CreateBoostBadge(boosts.transform, boost);
            }
        }

        private void CreateBoostBadge(Transform parent, ResourceBoost boost)
        {
            GameObject badge = new GameObject($"Boost_{boost.BoostId}");
            badge.transform.SetParent(parent, false);
            
            LayoutElement le = badge.AddComponent<LayoutElement>();
            le.preferredWidth = 150;
            le.preferredHeight = 50;
            
            Image bg = badge.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.15f, 0.05f);
            
            HorizontalLayoutGroup hlayout = badge.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            CreateText(badge.transform, boost.Icon, 20, TextAlignmentOptions.Center);
            
            GameObject info = new GameObject("BoostInfo");
            info.transform.SetParent(badge.transform, false);
            
            VerticalLayoutGroup vlayout = info.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(info.transform, $"+{boost.BonusPercent}%", 12, TextAlignmentOptions.Left, goldColor);
            CreateText(info.transform, GetTimeRemaining(boost.ExpiresAt), 9, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateBuildingsContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            // Group buildings by resource type
            var groupedBuildings = new Dictionary<ResourceType, List<ProductionBuilding>>();
            foreach (var building in _buildings)
            {
                if (!groupedBuildings.ContainsKey(building.Type))
                    groupedBuildings[building.Type] = new List<ProductionBuilding>();
                groupedBuildings[building.Type].Add(building);
            }
            
            foreach (var kvp in groupedBuildings)
            {
                CreateBuildingGroup(content.transform, kvp.Key, kvp.Value);
            }
        }

        private void CreateBuildingGroup(Transform parent, ResourceType type, List<ProductionBuilding> buildings)
        {
            string icon = _resources.ContainsKey(type) ? _resources[type].Icon : "📦";
            CreateSectionHeader(parent, $"{icon} {type} PRODUCTION");
            
            foreach (var building in buildings)
            {
                CreateBuildingRow(parent, building);
            }
        }

        private void CreateBuildingRow(Transform parent, ProductionBuilding building)
        {
            GameObject row = new GameObject($"Building_{building.BuildingId}");
            row.transform.SetParent(parent, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Image bg = row.AddComponent<Image>();
            bg.color = building.IsUpgrading ? new Color(0.15f, 0.12f, 0.05f) : new Color(0.08f, 0.1f, 0.08f);
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            // Building info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(row.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.preferredWidth = 200;
            
            VerticalLayoutGroup infoLayout = info.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(info.transform, $"🏗️ {building.Name}", 13, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, $"Level {building.Level}", 10, TextAlignmentOptions.Left, accentColor);
            
            if (building.IsUpgrading)
            {
                CreateText(info.transform, $"⏫ Upgrading... {GetTimeRemaining(building.UpgradeCompleteTime)}", 9, TextAlignmentOptions.Left, warningColor);
            }
            
            // Production info
            GameObject production = new GameObject("Production");
            production.transform.SetParent(row.transform, false);
            
            LayoutElement prodLE = production.AddComponent<LayoutElement>();
            prodLE.flexibleWidth = 1;
            
            VerticalLayoutGroup prodLayout = production.AddComponent<VerticalLayoutGroup>();
            prodLayout.childAlignment = TextAnchor.MiddleCenter;
            
            int totalProduction = building.BaseProduction + building.BonusProduction;
            CreateText(production.transform, $"+{FormatNumber(totalProduction)}/hr", 14, TextAlignmentOptions.Center, accentColor);
            
            if (building.BonusProduction > 0)
            {
                CreateText(production.transform, $"(Base: {building.BaseProduction} + Bonus: {building.BonusProduction})", 9, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            }
            
            // Pending amount
            GameObject pending = new GameObject("Pending");
            pending.transform.SetParent(row.transform, false);
            
            LayoutElement pendLE = pending.AddComponent<LayoutElement>();
            pendLE.preferredWidth = 120;
            
            VerticalLayoutGroup pendLayout = pending.AddComponent<VerticalLayoutGroup>();
            pendLayout.childAlignment = TextAnchor.MiddleCenter;
            
            float pendingPercent = (float)building.PendingAmount / building.MaxPending;
            CreateText(pending.transform, $"📦 {FormatNumber(building.PendingAmount)}", 12, TextAlignmentOptions.Center, goldColor);
            CreateCapacityBar(pending.transform, pendingPercent);
            
            // Collect button
            CreateBuildingCollectButton(row.transform, building);
        }

        private void CreateBuildingCollectButton(Transform parent, ProductionBuilding building)
        {
            GameObject btn = new GameObject("CollectBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            le.preferredHeight = 40;
            
            bool canCollect = building.PendingAmount > 0 && !building.IsUpgrading;
            Color bgColor = canCollect ? accentColor : new Color(0.3f, 0.3f, 0.3f);
            
            Image bg = btn.AddComponent<Image>();
            bg.color = bgColor;
            
            if (canCollect)
            {
                Button button = btn.AddComponent<Button>();
                button.onClick.AddListener(() => CollectFromBuilding(building));
            }
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "🎁 Collect";
            text.fontSize = 11;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateBoostsContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            // Active boosts
            CreateSectionHeader(content.transform, "⚡ ACTIVE BOOSTS");
            
            if (_activeBoosts.Count > 0)
            {
                foreach (var boost in _activeBoosts)
                {
                    CreateBoostDetailRow(content.transform, boost);
                }
            }
            else
            {
                CreateText(content.transform, "No active boosts. Purchase from the shop!", 12, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            }
            
            // Available boosts (shop)
            CreateSectionHeader(content.transform, "🛒 AVAILABLE BOOSTS");
            CreateBoostShopItem(content.transform, "1-Hour Gold Rush", "+50% Gold", 100, ResourceType.Gold);
            CreateBoostShopItem(content.transform, "1-Hour Resource Boost", "+25% All Resources", 200, null);
            CreateBoostShopItem(content.transform, "24-Hour Production", "+15% All Production", 500, null);
        }

        private void CreateBoostDetailRow(Transform parent, ResourceBoost boost)
        {
            GameObject row = new GameObject($"BoostDetail_{boost.BoostId}");
            row.transform.SetParent(parent, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = row.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.12f, 0.05f);
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            CreateText(row.transform, boost.Icon, 28, TextAlignmentOptions.Center);
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(row.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoLayout = info.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(info.transform, boost.Name, 14, TextAlignmentOptions.Left, Color.white);
            
            string targetText = boost.ResourceType.HasValue ? $"+{boost.BonusPercent}% {boost.ResourceType}" : $"+{boost.BonusPercent}% All Resources";
            CreateText(info.transform, targetText, 11, TextAlignmentOptions.Left, goldColor);
            
            // Time remaining
            CreateText(row.transform, GetTimeRemaining(boost.ExpiresAt), 12, TextAlignmentOptions.Right, warningColor);
        }

        private void CreateBoostShopItem(Transform parent, string name, string effect, int cost, ResourceType? targetResource)
        {
            GameObject item = new GameObject($"ShopItem_{name}");
            item.transform.SetParent(parent, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 70;
            
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.08f);
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(20, 20, 15, 15);
            
            // Icon
            string icon = targetResource.HasValue ? "⚡" : "🌟";
            CreateText(item.transform, icon, 24, TextAlignmentOptions.Center);
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(item.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoLayout = info.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(info.transform, name, 13, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, effect, 11, TextAlignmentOptions.Left, accentColor);
            
            // Buy button
            CreateBuyButton(item.transform, cost);
        }

        private void CreateBuyButton(Transform parent, int cost)
        {
            GameObject btn = new GameObject("BuyBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 100;
            le.preferredHeight = 40;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = goldColor;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => PurchaseBoost(cost));
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = $"🌟 {cost}";
            text.fontSize = 12;
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateTradeContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            CreateSectionHeader(content.transform, "🔄 RESOURCE EXCHANGE");
            
            // Trade rates
            CreateTradeOption(content.transform, ResourceType.Gold, ResourceType.Stone, 2, 1);
            CreateTradeOption(content.transform, ResourceType.Gold, ResourceType.Wood, 2, 1);
            CreateTradeOption(content.transform, ResourceType.Gold, ResourceType.Iron, 3, 1);
            CreateTradeOption(content.transform, ResourceType.Stone, ResourceType.Gold, 1, 1);
            CreateTradeOption(content.transform, ResourceType.Wood, ResourceType.Gold, 1, 1);
            
            CreateSectionHeader(content.transform, "💎 PREMIUM EXCHANGE");
            CreateTradeOption(content.transform, ResourceType.ApexCoins, ResourceType.Gold, 1, 10000);
            CreateTradeOption(content.transform, ResourceType.ApexCoins, ResourceType.Crystal, 10, 100);
        }

        private void CreateTradeOption(Transform parent, ResourceType from, ResourceType to, int fromAmount, int toAmount)
        {
            GameObject row = new GameObject($"Trade_{from}_{to}");
            row.transform.SetParent(parent, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = row.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.08f);
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(30, 30, 10, 10);
            
            // From
            string fromIcon = _resources.ContainsKey(from) ? _resources[from].Icon : "📦";
            CreateText(row.transform, $"{fromIcon} {FormatNumber(fromAmount)}", 14, TextAlignmentOptions.Center, new Color(0.9f, 0.5f, 0.5f));
            
            // Arrow
            CreateText(row.transform, "➡️", 18, TextAlignmentOptions.Center);
            
            // To
            string toIcon = _resources.ContainsKey(to) ? _resources[to].Icon : "📦";
            CreateText(row.transform, $"{toIcon} {FormatNumber(toAmount)}", 14, TextAlignmentOptions.Center, accentColor);
            
            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Trade button
            CreateTradeButton(row.transform, from, to, fromAmount, toAmount);
        }

        private void CreateTradeButton(Transform parent, ResourceType from, ResourceType to, int fromAmount, int toAmount)
        {
            GameObject btn = new GameObject("TradeBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            le.preferredHeight = 35;
            
            int currentFrom = _resources.ContainsKey(from) ? _resources[from].Current : 0;
            bool canTrade = currentFrom >= fromAmount;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = canTrade ? accentColor : new Color(0.3f, 0.3f, 0.3f);
            
            if (canTrade)
            {
                Button button = btn.AddComponent<Button>();
                button.onClick.AddListener(() => ExecuteTrade(from, to, fromAmount, toAmount));
            }
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Trade";
            text.fontSize = 11;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateHistoryContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            CreateSectionHeader(content.transform, "📜 RECENT TRANSACTIONS");
            
            // Sample history entries
            CreateHistoryEntry(content.transform, "Collected from Gold Mine", "+2,500 Gold", DateTime.Now.AddMinutes(-15));
            CreateHistoryEntry(content.transform, "Building Upgrade Cost", "-15,000 Stone", DateTime.Now.AddMinutes(-30));
            CreateHistoryEntry(content.transform, "Auto-collect", "+8,500 Resources", DateTime.Now.AddHours(-1));
            CreateHistoryEntry(content.transform, "Troop Training", "-5,000 Gold", DateTime.Now.AddHours(-2));
            CreateHistoryEntry(content.transform, "Alliance Donation", "-10,000 Gold", DateTime.Now.AddHours(-3));
            CreateHistoryEntry(content.transform, "Quest Reward", "+1,200 Crystal", DateTime.Now.AddHours(-4));
            CreateHistoryEntry(content.transform, "Resource Trade", "-20,000 Gold → +10,000 Stone", DateTime.Now.AddHours(-5));
        }

        private void CreateHistoryEntry(Transform parent, string action, string amount, DateTime time)
        {
            GameObject entry = new GameObject("HistoryEntry");
            entry.transform.SetParent(parent, false);
            
            LayoutElement le = entry.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            
            Image bg = entry.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.06f);
            
            HorizontalLayoutGroup hlayout = entry.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(20, 20, 8, 8);
            
            // Time
            CreateText(entry.transform, GetTimeAgo(time), 10, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            
            // Action
            GameObject actionObj = new GameObject("Action");
            actionObj.transform.SetParent(entry.transform, false);
            actionObj.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            CreateText(actionObj.transform, action, 12, TextAlignmentOptions.Left, Color.white);
            
            // Amount
            Color amountColor = amount.StartsWith("+") ? accentColor : (amount.StartsWith("-") ? new Color(0.9f, 0.5f, 0.5f) : Color.white);
            CreateText(entry.transform, amount, 12, TextAlignmentOptions.Right, amountColor);
        }

        #region Helpers

        private GameObject CreateScrollView(Transform parent)
        {
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(parent, false);
            
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            
            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            
            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            
            return scrollView;
        }

        private void CreateSectionHeader(Transform parent, string text)
        {
            GameObject header = new GameObject("SectionHeader");
            header.transform.SetParent(parent, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            TextMeshProUGUI tmp = header.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 13;
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
            
            return obj;
        }

        private string FormatNumber(int number)
        {
            if (number >= 1000000) return $"{number / 1000000f:F1}M";
            if (number >= 1000) return $"{number / 1000f:F1}K";
            return number.ToString();
        }

        private string GetTimeRemaining(DateTime target)
        {
            TimeSpan remaining = target - DateTime.Now;
            if (remaining.TotalSeconds <= 0) return "Expired";
            if (remaining.TotalDays >= 1) return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
            if (remaining.TotalHours >= 1) return $"{remaining.Hours}h {remaining.Minutes}m";
            return $"{remaining.Minutes}m {remaining.Seconds}s";
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

        private void SetTab(ResourceTab tab)
        {
            _currentTab = tab;
            CreateTabBar();
            RefreshContent();
        }

        private void ToggleAutoCollect()
        {
            _autoCollectEnabled = !_autoCollectEnabled;
            
            if (NotificationSystem.Instance != null)
            {
                string status = _autoCollectEnabled ? "enabled" : "disabled";
                NotificationSystem.Instance.ShowInfo($"Auto-collect {status}");
            }
            
            CreateHeader();
        }

        private void CollectAll()
        {
            int totalCollected = 0;
            
            foreach (var res in _resources.Values)
            {
                if (res.PendingCollection > 0)
                {
                    res.Current = Mathf.Min(res.Current + res.PendingCollection, res.Capacity);
                    totalCollected += res.PendingCollection;
                    res.PendingCollection = 0;
                }
            }
            
            OnAutoCollect?.Invoke();
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"Collected {FormatNumber(totalCollected)} resources!");
            }
            
            RefreshContent();
        }

        private void CollectResource(ResourceType type)
        {
            if (!_resources.ContainsKey(type)) return;
            
            var res = _resources[type];
            if (res.PendingCollection > 0)
            {
                res.Current = Mathf.Min(res.Current + res.PendingCollection, res.Capacity);
                int collected = res.PendingCollection;
                res.PendingCollection = 0;
                
                OnResourceCollected?.Invoke(type, collected);
                
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowSuccess($"Collected {FormatNumber(collected)} {res.Name}!");
                }
                
                RefreshContent();
            }
        }

        private void CollectFromBuilding(ProductionBuilding building)
        {
            if (building.PendingAmount <= 0 || building.IsUpgrading) return;
            
            if (_resources.ContainsKey(building.Type))
            {
                var res = _resources[building.Type];
                res.Current = Mathf.Min(res.Current + building.PendingAmount, res.Capacity);
                
                OnResourceCollected?.Invoke(building.Type, building.PendingAmount);
                
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowSuccess($"Collected {FormatNumber(building.PendingAmount)} from {building.Name}!");
                }
                
                building.PendingAmount = 0;
                building.LastCollected = DateTime.Now;
                
                RefreshContent();
            }
        }

        private void PurchaseBoost(int cost)
        {
            if (_resources[ResourceType.ApexCoins].Current >= cost)
            {
                _resources[ResourceType.ApexCoins].Current -= cost;
                
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowSuccess("Boost purchased and activated!");
                }
                
                RefreshContent();
            }
            else
            {
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowWarning("Not enough Apex Coins!");
                }
            }
        }

        private void ExecuteTrade(ResourceType from, ResourceType to, int fromAmount, int toAmount)
        {
            if (!_resources.ContainsKey(from) || !_resources.ContainsKey(to)) return;
            
            var fromRes = _resources[from];
            var toRes = _resources[to];
            
            if (fromRes.Current >= fromAmount)
            {
                fromRes.Current -= fromAmount;
                toRes.Current = Mathf.Min(toRes.Current + toAmount, toRes.Capacity);
                
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowSuccess($"Traded {FormatNumber(fromAmount)} {fromRes.Name} for {FormatNumber(toAmount)} {toRes.Name}!");
                }
                
                RefreshContent();
            }
        }

        private void UpdateProductionTimers()
        {
            // Update pending amounts based on production rate
            foreach (var building in _buildings)
            {
                if (!building.IsUpgrading)
                {
                    int totalProduction = building.BaseProduction + building.BonusProduction;
                    float hourlyAdd = totalProduction * Time.deltaTime / 3600f;
                    building.PendingAmount = Mathf.Min(building.PendingAmount + (int)hourlyAdd, building.MaxPending);
                }
            }
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel.SetActive(true);
            RefreshContent();
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        public void Toggle()
        {
            if (_panel.activeSelf) Hide();
            else Show();
        }

        public int GetResource(ResourceType type) => _resources.ContainsKey(type) ? _resources[type].Current : 0;
        public int GetResourceCapacity(ResourceType type) => _resources.ContainsKey(type) ? _resources[type].Capacity : 0;
        public int GetProductionRate(ResourceType type) => _resources.ContainsKey(type) ? _resources[type].Production : 0;

        public void AddResource(ResourceType type, int amount)
        {
            if (_resources.ContainsKey(type))
            {
                _resources[type].Current = Mathf.Min(_resources[type].Current + amount, _resources[type].Capacity);
            }
        }

        public bool SpendResource(ResourceType type, int amount)
        {
            if (_resources.ContainsKey(type) && _resources[type].Current >= amount)
            {
                _resources[type].Current -= amount;
                return true;
            }
            return false;
        }

        #endregion
    }

    #region Data Classes

    public enum ResourceTab
    {
        Overview,
        Buildings,
        Boosts,
        Trade,
        History
    }

    public class ResourceData
    {
        public ResourceType Type;
        public string Icon;
        public string Name;
        public int Current;
        public int Capacity;
        public int Production;
        public int PendingCollection;
        public string Description;
    }

    public class ProductionBuilding
    {
        public string BuildingId;
        public string Name;
        public int Level;
        public ResourceType Type;
        public int BaseProduction;
        public int BonusProduction;
        public int PendingAmount;
        public int MaxPending;
        public DateTime LastCollected;
        public DateTime CollectReadyTime;
        public bool IsUpgrading;
        public DateTime UpgradeCompleteTime;
    }

    public class ResourceBoost
    {
        public string BoostId;
        public string Name;
        public ResourceType? ResourceType;
        public int BonusPercent;
        public DateTime ExpiresAt;
        public string Icon;
    }

    #endregion
}
