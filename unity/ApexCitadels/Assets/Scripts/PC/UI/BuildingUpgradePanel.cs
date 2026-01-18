using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Building Upgrade Panel - Manage building construction, upgrades, and production.
    /// Features building trees, requirements, boost options, and queue management.
    /// </summary>
    public class BuildingUpgradePanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.4f, 0.6f, 0.8f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color upgradeColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color maxLevelColor = new Color(0.8f, 0.6f, 0.2f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _buildingList;
        private GameObject _detailPanel;
        private BuildingCategory _selectedCategory = BuildingCategory.All;
        private Building _selectedBuilding;
        private Dictionary<BuildingCategory, GameObject> _categoryTabs = new Dictionary<BuildingCategory, GameObject>();
        
        // Building data
        private List<Building> _buildings = new List<Building>();
        private List<BuildingUpgradeQueue> _upgradeQueue = new List<BuildingUpgradeQueue>();
        private int _maxQueueSlots = 2;
        private int _builderCount = 2;
        
        public static BuildingUpgradePanel Instance { get; private set; }
        
        public event Action<Building> OnUpgradeStarted;
        public event Action<Building> OnUpgradeCompleted;
        public event Action<Building> OnUpgradeBoosted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeBuildingData();
        }

        private void Start()
        {
            CreatePanel();
            Hide();
        }

        private void Update()
        {
            // Update construction timers
            UpdateConstructionTimers();
        }

        private void InitializeBuildingData()
        {
            // Castle/Core buildings
            _buildings.Add(new Building
            {
                BuildingId = "castle",
                Name = "Castle",
                Category = BuildingCategory.Core,
                Icon = "🏰",
                Level = 15,
                MaxLevel = 30,
                Description = "The heart of your citadel. Unlocks new buildings and features.",
                ProductionType = ResourceType.ApexCoins,
                ProductionRate = 10,
                CurrentHP = 50000,
                MaxHP = 50000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 150000 }, { ResourceType.Stone, 80000 }, { ResourceType.Wood, 60000 } },
                UpgradeTime = 28800, // 8 hours
                Requirements = "All core buildings Lv.14+",
                Bonuses = new[] { "+5000 HP", "+2 Builder slots", "+1 Army size" }
            });
            
            _buildings.Add(new Building
            {
                BuildingId = "walls",
                Name = "City Walls",
                Category = BuildingCategory.Defense,
                Icon = "🧱",
                Level = 12,
                MaxLevel = 25,
                Description = "Protects your city from attacks. Higher levels unlock better defenses.",
                CurrentHP = 100000,
                MaxHP = 100000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 50000 }, { ResourceType.Stone, 100000 } },
                UpgradeTime = 14400, // 4 hours
                Bonuses = new[] { "+15000 Wall HP", "+5% Defense bonus" }
            });
            
            // Resource buildings
            _buildings.Add(new Building
            {
                BuildingId = "goldmine",
                Name = "Gold Mine",
                Category = BuildingCategory.Resource,
                Icon = "⛏️",
                Level = 14,
                MaxLevel = 25,
                Description = "Produces gold over time. Upgrade for higher production.",
                ProductionType = ResourceType.Gold,
                ProductionRate = 500,
                CurrentHP = 10000,
                MaxHP = 10000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 30000 }, { ResourceType.Iron, 5000 } },
                UpgradeTime = 7200, // 2 hours
                Bonuses = new[] { "+100 Gold/hour", "+2000 Storage" }
            });
            
            _buildings.Add(new Building
            {
                BuildingId = "lumbermill",
                Name = "Lumber Mill",
                Category = BuildingCategory.Resource,
                Icon = "🪵",
                Level = 13,
                MaxLevel = 25,
                Description = "Processes wood for construction. Higher levels increase output.",
                ProductionType = ResourceType.Wood,
                ProductionRate = 400,
                CurrentHP = 8000,
                MaxHP = 8000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 25000 }, { ResourceType.Wood, 15000 } },
                UpgradeTime = 5400, // 1.5 hours
                Bonuses = new[] { "+80 Wood/hour", "+1500 Storage" }
            });
            
            _buildings.Add(new Building
            {
                BuildingId = "stonequarry",
                Name = "Stone Quarry",
                Category = BuildingCategory.Resource,
                Icon = "🪨",
                Level = 12,
                MaxLevel = 25,
                Description = "Extracts stone for buildings and walls.",
                ProductionType = ResourceType.Stone,
                ProductionRate = 350,
                CurrentHP = 12000,
                MaxHP = 12000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 28000 }, { ResourceType.Stone, 10000 } },
                UpgradeTime = 6000,
                Bonuses = new[] { "+70 Stone/hour", "+1800 Storage" }
            });
            
            _buildings.Add(new Building
            {
                BuildingId = "ironforge",
                Name = "Iron Forge",
                Category = BuildingCategory.Resource,
                Icon = "🔥",
                Level = 10,
                MaxLevel = 20,
                Description = "Smelts iron ore into usable iron. Required for advanced buildings.",
                ProductionType = ResourceType.Iron,
                ProductionRate = 100,
                CurrentHP = 15000,
                MaxHP = 15000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 40000 }, { ResourceType.Iron, 8000 }, { ResourceType.Stone, 20000 } },
                UpgradeTime = 10800, // 3 hours
                Bonuses = new[] { "+25 Iron/hour", "+500 Storage" }
            });
            
            _buildings.Add(new Building
            {
                BuildingId = "crystalcave",
                Name = "Crystal Cave",
                Category = BuildingCategory.Resource,
                Icon = "💎",
                Level = 8,
                MaxLevel = 15,
                Description = "Harvests magical crystals. Rare resource for special buildings.",
                ProductionType = ResourceType.Crystal,
                ProductionRate = 20,
                CurrentHP = 20000,
                MaxHP = 20000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 80000 }, { ResourceType.Crystal, 500 } },
                UpgradeTime = 21600, // 6 hours
                Requirements = "Castle Lv.10+",
                Bonuses = new[] { "+5 Crystal/hour", "+100 Storage" }
            });
            
            // Military buildings
            _buildings.Add(new Building
            {
                BuildingId = "barracks",
                Name = "Barracks",
                Category = BuildingCategory.Military,
                Icon = "⚔️",
                Level = 14,
                MaxLevel = 25,
                Description = "Train infantry and basic troops. More levels unlock new units.",
                CurrentHP = 25000,
                MaxHP = 25000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 45000 }, { ResourceType.Wood, 30000 } },
                UpgradeTime = 10800,
                Bonuses = new[] { "-5% Training time", "Unlock: Elite Infantry" }
            });
            
            _buildings.Add(new Building
            {
                BuildingId = "archeryrange",
                Name = "Archery Range",
                Category = BuildingCategory.Military,
                Icon = "🏹",
                Level = 12,
                MaxLevel = 25,
                Description = "Train archers and ranged units. Higher levels unlock crossbowmen.",
                CurrentHP = 18000,
                MaxHP = 18000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 40000 }, { ResourceType.Wood, 40000 } },
                UpgradeTime = 9000,
                Bonuses = new[] { "-5% Training time", "+10% Archer damage" }
            });
            
            _buildings.Add(new Building
            {
                BuildingId = "stables",
                Name = "Stables",
                Category = BuildingCategory.Military,
                Icon = "🐴",
                Level = 10,
                MaxLevel = 20,
                Description = "Train cavalry units. Fast and powerful in battle.",
                CurrentHP = 20000,
                MaxHP = 20000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 60000 }, { ResourceType.Wood, 25000 }, { ResourceType.Iron, 10000 } },
                UpgradeTime = 14400,
                Requirements = "Barracks Lv.10+",
                Bonuses = new[] { "-10% Training time", "Unlock: Heavy Cavalry" }
            });
            
            _buildings.Add(new Building
            {
                BuildingId = "siegeworkshop",
                Name = "Siege Workshop",
                Category = BuildingCategory.Military,
                Icon = "🎯",
                Level = 8,
                MaxLevel = 15,
                Description = "Build siege weapons. Essential for attacking fortified cities.",
                CurrentHP = 30000,
                MaxHP = 30000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 80000 }, { ResourceType.Iron, 20000 }, { ResourceType.Wood, 50000 } },
                UpgradeTime = 21600,
                Requirements = "Castle Lv.12+",
                Bonuses = new[] { "+15% Siege damage", "Unlock: Trebuchet" }
            });
            
            // Defense buildings
            _buildings.Add(new Building
            {
                BuildingId = "watchtower",
                Name = "Watchtower",
                Category = BuildingCategory.Defense,
                Icon = "🗼",
                Level = 11,
                MaxLevel = 20,
                Description = "Detects incoming attacks. Higher levels give more warning time.",
                CurrentHP = 15000,
                MaxHP = 15000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 25000 }, { ResourceType.Stone, 35000 } },
                UpgradeTime = 7200,
                Bonuses = new[] { "+2min Warning time", "+50% Scout range" }
            });
            
            _buildings.Add(new Building
            {
                BuildingId = "arrowtower",
                Name = "Arrow Tower",
                Category = BuildingCategory.Defense,
                Icon = "🏹",
                Level = 10,
                MaxLevel = 20,
                Description = "Automatically fires at attackers. Place strategically for maximum effect.",
                CurrentHP = 25000,
                MaxHP = 25000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 35000 }, { ResourceType.Stone, 25000 }, { ResourceType.Iron, 5000 } },
                UpgradeTime = 10800,
                Bonuses = new[] { "+500 Arrow damage", "+20% Fire rate" }
            });
            
            _buildings.Add(new Building
            {
                BuildingId = "cannonfort",
                Name = "Cannon Fort",
                Category = BuildingCategory.Defense,
                Icon = "💣",
                Level = 6,
                MaxLevel = 15,
                Description = "Powerful defensive cannons. Devastating against siege weapons.",
                CurrentHP = 40000,
                MaxHP = 40000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 100000 }, { ResourceType.Iron, 30000 } },
                UpgradeTime = 28800,
                Requirements = "Castle Lv.14+",
                Bonuses = new[] { "+2000 Cannon damage", "+10% Range" }
            });
            
            // Special buildings
            _buildings.Add(new Building
            {
                BuildingId = "academy",
                Name = "Academy",
                Category = BuildingCategory.Special,
                Icon = "📚",
                Level = 9,
                MaxLevel = 20,
                Description = "Research technologies. Unlocks powerful upgrades for your kingdom.",
                CurrentHP = 10000,
                MaxHP = 10000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 50000 }, { ResourceType.Crystal, 200 } },
                UpgradeTime = 18000,
                Bonuses = new[] { "-10% Research time", "Unlock: Advanced Tech" }
            });
            
            _buildings.Add(new Building
            {
                BuildingId = "marketplace",
                Name = "Marketplace",
                Category = BuildingCategory.Special,
                Icon = "🏪",
                Level = 12,
                MaxLevel = 20,
                Description = "Trade resources with other players. Higher levels offer better rates.",
                CurrentHP = 8000,
                MaxHP = 8000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 30000 }, { ResourceType.Wood, 20000 } },
                UpgradeTime = 7200,
                Bonuses = new[] { "-5% Trade tax", "+2 Trade slots" }
            });
            
            _buildings.Add(new Building
            {
                BuildingId = "embassy",
                Name = "Embassy",
                Category = BuildingCategory.Special,
                Icon = "🏛️",
                Level = 8,
                MaxLevel = 15,
                Description = "Manages alliances and reinforcements. Receive help from allies.",
                CurrentHP = 12000,
                MaxHP = 12000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 40000 }, { ResourceType.Stone, 30000 } },
                UpgradeTime = 14400,
                Bonuses = new[] { "+5000 Reinforcement cap", "+1 Alliance slot" }
            });
            
            _buildings.Add(new Building
            {
                BuildingId = "hospital",
                Name = "Hospital",
                Category = BuildingCategory.Special,
                Icon = "🏥",
                Level = 11,
                MaxLevel = 20,
                Description = "Heals wounded troops. Reduce losses from battle.",
                CurrentHP = 15000,
                MaxHP = 15000,
                UpgradeCost = new Dictionary<ResourceType, int> { { ResourceType.Gold, 45000 }, { ResourceType.Wood, 15000 } },
                UpgradeTime = 10800,
                Bonuses = new[] { "+5000 Healing capacity", "-10% Healing time" }
            });
            
            // Add some buildings to upgrade queue
            _upgradeQueue.Add(new BuildingUpgradeQueue
            {
                Building = _buildings.Find(b => b.BuildingId == "goldmine"),
                TargetLevel = 15,
                TimeRemaining = 3600, // 1 hour left
                StartTime = DateTime.Now.AddHours(-1)
            });
        }

        private void UpdateConstructionTimers()
        {
            for (int i = _upgradeQueue.Count - 1; i >= 0; i--)
            {
                var queue = _upgradeQueue[i];
                queue.TimeRemaining -= Time.deltaTime;
                
                if (queue.TimeRemaining <= 0)
                {
                    // Complete upgrade
                    queue.Building.Level = queue.TargetLevel;
                    OnUpgradeCompleted?.Invoke(queue.Building);
                    _upgradeQueue.RemoveAt(i);
                    
                    if (NotificationSystem.Instance != null)
                    {
                        NotificationSystem.Instance.ShowSuccess($"{queue.Building.Name} upgraded to Lv.{queue.TargetLevel}!");
                    }
                }
            }
        }

        private void CreatePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("BuildingUpgradePanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.08f);
            rect.anchorMax = new Vector2(0.9f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.08f, 0.98f);
            
            HorizontalLayoutGroup hlayout = _panel.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Left - Building list
            CreateBuildingListSection();
            
            // Right - Building details
            CreateDetailSection();
        }

        private void CreateBuildingListSection()
        {
            _buildingList = new GameObject("BuildingList");
            _buildingList.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _buildingList.AddComponent<LayoutElement>();
            le.flexibleWidth = 1.5f;
            le.flexibleHeight = 1;
            
            Image bg = _buildingList.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.07f);
            
            VerticalLayoutGroup vlayout = _buildingList.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 8;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Header with queue
            CreateListHeader();
            
            // Upgrade queue
            CreateUpgradeQueueSection();
            
            // Category tabs
            CreateCategoryTabs();
            
            // Building grid
            CreateBuildingGrid();
        }

        private void CreateListHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_buildingList.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(header.transform, "🏗️ BUILDINGS", 22, TextAlignmentOptions.Left, accentColor);
            
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(header.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            CreateText(header.transform, $"👷 Builders: {_upgradeQueue.Count}/{_builderCount}", 12, TextAlignmentOptions.Right, 
                      _upgradeQueue.Count < _builderCount ? upgradeColor : new Color(0.8f, 0.6f, 0.2f));
        }

        private void CreateUpgradeQueueSection()
        {
            if (_upgradeQueue.Count == 0) return;
            
            GameObject queue = new GameObject("UpgradeQueue");
            queue.transform.SetParent(_buildingList.transform, false);
            
            LayoutElement le = queue.AddComponent<LayoutElement>();
            le.preferredHeight = 60 * _upgradeQueue.Count;
            
            VerticalLayoutGroup vlayout = queue.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;
            
            foreach (var item in _upgradeQueue)
            {
                CreateQueueItem(queue.transform, item);
            }
        }

        private void CreateQueueItem(Transform parent, BuildingUpgradeQueue item)
        {
            GameObject queueItem = new GameObject($"Queue_{item.Building.BuildingId}");
            queueItem.transform.SetParent(parent, false);
            
            LayoutElement le = queueItem.AddComponent<LayoutElement>();
            le.preferredHeight = 55;
            
            Image bg = queueItem.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.08f);
            
            UnityEngine.UI.Outline outline = queueItem.AddComponent<Outline>();
            outline.effectColor = upgradeColor;
            outline.effectDistance = new Vector2(1, 1);
            
            HorizontalLayoutGroup hlayout = queueItem.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            // Icon
            CreateText(queueItem.transform, item.Building.Icon, 24, TextAlignmentOptions.Center);
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(queueItem.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 2;
            
            CreateText(info.transform, $"{item.Building.Name} → Lv.{item.TargetLevel}", 12, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, $"⏱️ {FormatTime((int)item.TimeRemaining)} remaining", 10, TextAlignmentOptions.Left, new Color(0.6f, 0.8f, 0.6f));
            
            // Progress bar
            float progress = 1f - (item.TimeRemaining / item.Building.UpgradeTime);
            CreateMiniProgressBar(info.transform, progress, upgradeColor);
            
            // Boost button
            CreateBoostButton(queueItem.transform, item);
        }

        private void CreateMiniProgressBar(Transform parent, float progress, Color color)
        {
            GameObject bar = new GameObject("ProgressBar");
            bar.transform.SetParent(parent, false);
            
            LayoutElement le = bar.AddComponent<LayoutElement>();
            le.preferredHeight = 8;
            
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

        private void CreateBoostButton(Transform parent, BuildingUpgradeQueue item)
        {
            GameObject btn = new GameObject("BoostBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            le.preferredHeight = 35;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = goldColor * 0.8f;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => BoostUpgrade(item));
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            int gemCost = CalculateBoostCost(item);
            text.text = $"⚡ {gemCost}";
            text.fontSize = 11;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateCategoryTabs()
        {
            GameObject tabs = new GameObject("CategoryTabs");
            tabs.transform.SetParent(_buildingList.transform, false);
            
            LayoutElement le = tabs.AddComponent<LayoutElement>();
            le.preferredHeight = 35;
            
            HorizontalLayoutGroup hlayout = tabs.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 3;
            
            CreateCategoryTab(tabs.transform, BuildingCategory.All, "All");
            CreateCategoryTab(tabs.transform, BuildingCategory.Core, "Core");
            CreateCategoryTab(tabs.transform, BuildingCategory.Resource, "Resource");
            CreateCategoryTab(tabs.transform, BuildingCategory.Military, "Military");
            CreateCategoryTab(tabs.transform, BuildingCategory.Defense, "Defense");
            CreateCategoryTab(tabs.transform, BuildingCategory.Special, "Special");
        }

        private void CreateCategoryTab(Transform parent, BuildingCategory category, string label)
        {
            GameObject tab = new GameObject($"Tab_{category}");
            tab.transform.SetParent(parent, false);
            
            LayoutElement le = tab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            Color bgColor = category == _selectedCategory ? accentColor : new Color(0.12f, 0.12f, 0.15f);
            
            Image bg = tab.AddComponent<Image>();
            bg.color = bgColor;
            
            Button btn = tab.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectCategory(category));
            
            TextMeshProUGUI text = tab.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 11;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            _categoryTabs[category] = tab;
        }

        private void CreateBuildingGrid()
        {
            GameObject grid = new GameObject("BuildingGrid");
            grid.transform.SetParent(_buildingList.transform, false);
            
            LayoutElement le = grid.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            
            Image bg = grid.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.03f, 0.05f);
            
            GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(100, 110);
            gridLayout.spacing = new Vector2(8, 8);
            gridLayout.padding = new RectOffset(10, 10, 10, 10);
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
            
            RefreshBuildingGrid(grid);
        }

        private void RefreshBuildingGrid(GameObject grid = null)
        {
            if (grid == null)
            {
                grid = _buildingList.transform.Find("BuildingGrid")?.gameObject;
                if (grid == null) return;
            }
            
            foreach (Transform child in grid.transform)
            {
                Destroy(child.gameObject);
            }
            
            var filtered = _selectedCategory == BuildingCategory.All 
                ? _buildings 
                : _buildings.FindAll(b => b.Category == _selectedCategory);
            
            foreach (var building in filtered)
            {
                CreateBuildingCard(grid.transform, building);
            }
        }

        private void CreateBuildingCard(Transform parent, Building building)
        {
            GameObject card = new GameObject($"Building_{building.BuildingId}");
            card.transform.SetParent(parent, false);
            
            bool isSelected = _selectedBuilding?.BuildingId == building.BuildingId;
            bool isMaxLevel = building.Level >= building.MaxLevel;
            bool isUpgrading = _upgradeQueue.Exists(q => q.Building.BuildingId == building.BuildingId);
            
            Color bgColor = isSelected ? new Color(accentColor.r * 0.3f, accentColor.g * 0.3f, accentColor.b * 0.3f) 
                         : isUpgrading ? new Color(0.1f, 0.12f, 0.08f)
                         : new Color(0.08f, 0.08f, 0.1f);
            
            Image bg = card.AddComponent<Image>();
            bg.color = bgColor;
            
            if (isSelected || isMaxLevel)
            {
                UnityEngine.UI.Outline outline = card.AddComponent<Outline>();
                outline.effectColor = isMaxLevel ? maxLevelColor : accentColor;
                outline.effectDistance = new Vector2(2, 2);
            }
            
            Button btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectBuilding(building));
            
            VerticalLayoutGroup vlayout = card.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 3;
            vlayout.padding = new RectOffset(5, 5, 8, 8);
            
            // Icon
            CreateText(card.transform, building.Icon, 32, TextAlignmentOptions.Center);
            
            // Name
            CreateText(card.transform, building.Name, 10, TextAlignmentOptions.Center, Color.white);
            
            // Level
            Color levelColor = isMaxLevel ? maxLevelColor : Color.white;
            string levelText = isMaxLevel ? "MAX" : $"Lv.{building.Level}";
            CreateText(card.transform, levelText, 11, TextAlignmentOptions.Center, levelColor);
            
            // Status indicator
            if (isUpgrading)
            {
                CreateText(card.transform, "🔨", 12, TextAlignmentOptions.Center, upgradeColor);
            }
            else if (building.ProductionRate > 0)
            {
                CreateText(card.transform, $"+{building.ProductionRate}/h", 9, TextAlignmentOptions.Center, new Color(0.5f, 0.8f, 0.5f));
            }
        }

        private void CreateDetailSection()
        {
            _detailPanel = new GameObject("DetailSection");
            _detailPanel.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _detailPanel.AddComponent<LayoutElement>();
            le.preferredWidth = 350;
            le.flexibleHeight = 1;
            
            Image bg = _detailPanel.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.07f, 0.09f);
            
            VerticalLayoutGroup vlayout = _detailPanel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 12;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            RefreshDetailPanel();
        }

        private void RefreshDetailPanel()
        {
            foreach (Transform child in _detailPanel.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Close button
            CreateCloseButton();
            
            if (_selectedBuilding == null)
            {
                CreateText(_detailPanel.transform, "Select a building\nto view details", 14, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
                return;
            }
            
            // Building header
            CreateBuildingHeader();
            
            // Description
            CreateText(_detailPanel.transform, _selectedBuilding.Description, 11, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
            
            // Current stats
            CreateCurrentStats();
            
            // Upgrade info
            if (_selectedBuilding.Level < _selectedBuilding.MaxLevel)
            {
                CreateUpgradeSection();
            }
            else
            {
                CreateMaxLevelSection();
            }
        }

        private void CreateCloseButton()
        {
            GameObject header = new GameObject("CloseHeader");
            header.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleRight;
            
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(header.transform, false);
            
            LayoutElement closele = closeBtn.AddComponent<LayoutElement>();
            closele.preferredWidth = 30;
            closele.preferredHeight = 30;
            
            Image closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = new Color(0.5f, 0.2f, 0.2f, 0.8f);
            
            Button btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(Hide);
            
            TextMeshProUGUI x = closeBtn.AddComponent<TextMeshProUGUI>();
            x.text = "✕";
            x.fontSize = 18;
            x.alignment = TextAlignmentOptions.Center;
        }

        private void CreateBuildingHeader()
        {
            GameObject header = new GameObject("BuildingHeader");
            header.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            VerticalLayoutGroup vlayout = header.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            
            // Icon
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(header.transform, false);
            
            LayoutElement iconLE = icon.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 60;
            iconLE.preferredHeight = 60;
            
            Image iconBg = icon.AddComponent<Image>();
            iconBg.color = new Color(accentColor.r * 0.3f, accentColor.g * 0.3f, accentColor.b * 0.3f);
            
            Outline iconOutline = icon.AddComponent<Outline>();
            iconOutline.effectColor = accentColor;
            iconOutline.effectDistance = new Vector2(2, 2);
            
            TextMeshProUGUI iconText = icon.AddComponent<TextMeshProUGUI>();
            iconText.text = _selectedBuilding.Icon;
            iconText.fontSize = 36;
            iconText.alignment = TextAlignmentOptions.Center;
            
            // Name & Level
            bool isMax = _selectedBuilding.Level >= _selectedBuilding.MaxLevel;
            CreateText(header.transform, _selectedBuilding.Name, 18, TextAlignmentOptions.Center, Color.white);
            CreateText(header.transform, isMax ? "MAX LEVEL" : $"Level {_selectedBuilding.Level} / {_selectedBuilding.MaxLevel}", 
                      12, TextAlignmentOptions.Center, isMax ? maxLevelColor : accentColor);
        }

        private void CreateCurrentStats()
        {
            CreateSectionLabel("📊 CURRENT STATS");
            
            GameObject stats = new GameObject("CurrentStats");
            stats.transform.SetParent(_detailPanel.transform, false);
            
            GridLayoutGroup grid = stats.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150, 30);
            grid.spacing = new Vector2(5, 3);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            
            CreateStatItem(stats.transform, "❤️ HP", $"{_selectedBuilding.CurrentHP:N0} / {_selectedBuilding.MaxHP:N0}");
            
            if (_selectedBuilding.ProductionRate > 0)
            {
                string resourceIcon = GetResourceIcon(_selectedBuilding.ProductionType);
                CreateStatItem(stats.transform, $"{resourceIcon} Production", $"+{_selectedBuilding.ProductionRate}/hour");
            }
            
            CreateStatItem(stats.transform, "📍 Category", _selectedBuilding.Category.ToString());
        }

        private void CreateStatItem(Transform parent, string label, string value)
        {
            GameObject item = new GameObject("Stat");
            item.transform.SetParent(parent, false);
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 5;
            
            CreateText(item.transform, label, 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            CreateText(item.transform, value, 10, TextAlignmentOptions.Left, Color.white);
        }

        private void CreateUpgradeSection()
        {
            bool isUpgrading = _upgradeQueue.Exists(q => q.Building.BuildingId == _selectedBuilding.BuildingId);
            
            if (isUpgrading)
            {
                CreateSectionLabel("🔨 UPGRADING...");
                var queue = _upgradeQueue.Find(q => q.Building.BuildingId == _selectedBuilding.BuildingId);
                
                GameObject progress = new GameObject("UpgradeProgress");
                progress.transform.SetParent(_detailPanel.transform, false);
                
                LayoutElement le = progress.AddComponent<LayoutElement>();
                le.preferredHeight = 50;
                
                Image bg = progress.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.12f, 0.08f);
                
                VerticalLayoutGroup vlayout = progress.AddComponent<VerticalLayoutGroup>();
                vlayout.childAlignment = TextAnchor.MiddleCenter;
                vlayout.padding = new RectOffset(10, 10, 10, 10);
                
                CreateText(progress.transform, $"⏱️ {FormatTime((int)queue.TimeRemaining)} remaining", 14, TextAlignmentOptions.Center, upgradeColor);
                
                float prog = 1f - (queue.TimeRemaining / _selectedBuilding.UpgradeTime);
                CreateMiniProgressBar(progress.transform, prog, upgradeColor);
                
                return;
            }
            
            CreateSectionLabel("⬆️ UPGRADE TO LEVEL " + (_selectedBuilding.Level + 1));
            
            // Requirements
            if (!string.IsNullOrEmpty(_selectedBuilding.Requirements))
            {
                CreateText(_detailPanel.transform, $"⚠️ Requires: {_selectedBuilding.Requirements}", 10, TextAlignmentOptions.Center, new Color(0.8f, 0.6f, 0.3f));
            }
            
            // Cost
            CreateCostSection();
            
            // Time
            CreateText(_detailPanel.transform, $"⏱️ Build Time: {FormatTime(_selectedBuilding.UpgradeTime)}", 11, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            
            // Bonuses
            if (_selectedBuilding.Bonuses != null && _selectedBuilding.Bonuses.Length > 0)
            {
                CreateSectionLabel("✨ BONUSES");
                
                foreach (var bonus in _selectedBuilding.Bonuses)
                {
                    CreateText(_detailPanel.transform, $"• {bonus}", 11, TextAlignmentOptions.Left, upgradeColor);
                }
            }
            
            // Upgrade button
            CreateUpgradeButton();
        }

        private void CreateCostSection()
        {
            GameObject cost = new GameObject("CostSection");
            cost.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = cost.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = cost.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.08f);
            
            HorizontalLayoutGroup hlayout = cost.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 10, 10);
            
            foreach (var kvp in _selectedBuilding.UpgradeCost)
            {
                CreateCostItem(cost.transform, kvp.Key, kvp.Value);
            }
        }

        private void CreateCostItem(Transform parent, ResourceType resource, int amount)
        {
            GameObject item = new GameObject($"Cost_{resource}");
            item.transform.SetParent(parent, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            VerticalLayoutGroup vlayout = item.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 2;
            
            // In a real implementation, check if player has enough
            bool hasEnough = true; // Placeholder
            Color amountColor = hasEnough ? upgradeColor : new Color(0.8f, 0.3f, 0.3f);
            
            CreateText(item.transform, GetResourceIcon(resource), 18, TextAlignmentOptions.Center);
            CreateText(item.transform, $"{amount:N0}", 11, TextAlignmentOptions.Center, amountColor);
            CreateText(item.transform, resource.ToString(), 8, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateUpgradeButton()
        {
            bool canUpgrade = _upgradeQueue.Count < _builderCount;
            
            GameObject btn = new GameObject("UpgradeBtn");
            btn.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = canUpgrade ? upgradeColor : new Color(0.3f, 0.3f, 0.3f);
            
            if (canUpgrade)
            {
                Button button = btn.AddComponent<Button>();
                button.onClick.AddListener(() => StartUpgrade(_selectedBuilding));
            }
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = canUpgrade ? "⬆️ UPGRADE" : "👷 NO BUILDERS AVAILABLE";
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateMaxLevelSection()
        {
            CreateSectionLabel("🏆 MAXIMUM LEVEL REACHED");
            
            GameObject maxInfo = new GameObject("MaxInfo");
            maxInfo.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = maxInfo.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Image bg = maxInfo.AddComponent<Image>();
            bg.color = new Color(maxLevelColor.r * 0.2f, maxLevelColor.g * 0.2f, maxLevelColor.b * 0.2f);
            
            UnityEngine.UI.Outline outline = maxInfo.AddComponent<Outline>();
            outline.effectColor = maxLevelColor;
            outline.effectDistance = new Vector2(2, 2);
            
            VerticalLayoutGroup vlayout = maxInfo.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            CreateText(maxInfo.transform, "🌟", 32, TextAlignmentOptions.Center);
            CreateText(maxInfo.transform, "This building is fully upgraded!", 12, TextAlignmentOptions.Center, maxLevelColor);
            CreateText(maxInfo.transform, "All bonuses are active", 10, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
        }

        #region UI Helpers

        private void CreateSectionLabel(string text)
        {
            GameObject label = new GameObject("Label");
            label.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = label.AddComponent<LayoutElement>();
            le.preferredHeight = 25;
            
            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 12;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = accentColor;
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

        private string GetResourceIcon(ResourceType resource)
        {
            return resource switch
            {
                ResourceType.Gold => "💰",
                ResourceType.Stone => "🪨",
                ResourceType.Wood => "🪵",
                ResourceType.Iron => "⚙️",
                ResourceType.Crystal => "💎",
                ResourceType.ApexCoins => "🪙",
                _ => "📦"
            };
        }

        private string FormatTime(int seconds)
        {
            if (seconds < 60) return $"{seconds}s";
            if (seconds < 3600) return $"{seconds / 60}m";
            if (seconds < 86400) return $"{seconds / 3600}h {(seconds % 3600) / 60}m";
            return $"{seconds / 86400}d {(seconds % 86400) / 3600}h";
        }

        private int CalculateBoostCost(BuildingUpgradeQueue item)
        {
            // 1 gem per minute remaining
            return Mathf.CeilToInt(item.TimeRemaining / 60f);
        }

        #endregion

        #region Actions

        private void SelectCategory(BuildingCategory category)
        {
            _selectedCategory = category;
            
            foreach (var kvp in _categoryTabs)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == category ? accentColor : new Color(0.12f, 0.12f, 0.15f);
            }
            
            RefreshBuildingGrid();
        }

        private void SelectBuilding(Building building)
        {
            _selectedBuilding = building;
            RefreshBuildingGrid();
            RefreshDetailPanel();
        }

        private void StartUpgrade(Building building)
        {
            if (_upgradeQueue.Count >= _builderCount)
            {
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowWarning("No builders available!");
                }
                return;
            }
            
            _upgradeQueue.Add(new BuildingUpgradeQueue
            {
                Building = building,
                TargetLevel = building.Level + 1,
                TimeRemaining = building.UpgradeTime,
                StartTime = DateTime.Now
            });
            
            OnUpgradeStarted?.Invoke(building);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"Started upgrading {building.Name}!");
            }
            
            // Rebuild entire list to show queue
            RebuildBuildingList();
            RefreshDetailPanel();
        }

        private void BoostUpgrade(BuildingUpgradeQueue item)
        {
            int cost = CalculateBoostCost(item);
            
            // Complete immediately
            item.Building.Level = item.TargetLevel;
            _upgradeQueue.Remove(item);
            
            OnUpgradeBoosted?.Invoke(item.Building);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"{item.Building.Name} upgrade complete!");
            }
            
            RebuildBuildingList();
            RefreshDetailPanel();
        }

        private void RebuildBuildingList()
        {
            // Clear and rebuild the list section
            foreach (Transform child in _buildingList.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Remove old layout
            var oldLayout = _buildingList.GetComponent<VerticalLayoutGroup>();
            if (oldLayout != null) Destroy(oldLayout);
            
            VerticalLayoutGroup vlayout = _buildingList.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 8;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            CreateListHeader();
            CreateUpgradeQueueSection();
            CreateCategoryTabs();
            CreateBuildingGrid();
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel.SetActive(true);
            RebuildBuildingList();
            RefreshDetailPanel();
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

        public void ShowBuilding(string buildingId)
        {
            var building = _buildings.Find(b => b.BuildingId == buildingId);
            if (building != null)
            {
                _selectedBuilding = building;
                Show();
            }
        }

        public List<Building> GetBuildings() => _buildings;
        public List<BuildingUpgradeQueue> GetUpgradeQueue() => _upgradeQueue;
        public int GetAvailableBuilders() => _builderCount - _upgradeQueue.Count;

        #endregion
    }

    #region Data Classes

    public enum BuildingCategory
    {
        All,
        Core,
        Resource,
        Military,
        Defense,
        Special
    }

    public class Building
    {
        public string BuildingId;
        public string Name;
        public BuildingCategory Category;
        public string Icon;
        public int Level;
        public int MaxLevel;
        public string Description;
        public ResourceType ProductionType;
        public int ProductionRate;
        public int CurrentHP;
        public int MaxHP;
        public Dictionary<ResourceType, int> UpgradeCost = new Dictionary<ResourceType, int>();
        public int UpgradeTime;
        public string Requirements;
        public string[] Bonuses;
    }

    public class BuildingUpgradeQueue
    {
        public Building Building;
        public int TargetLevel;
        public float TimeRemaining;
        public DateTime StartTime;
    }

    #endregion
}
