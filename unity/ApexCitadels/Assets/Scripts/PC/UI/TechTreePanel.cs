using System;
using ApexCitadels.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.UI;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Tech Tree Panel - Research technologies to unlock new buildings, troops, and abilities.
    /// The strategic layer that provides long-term progression and customization.
    /// </summary>
    public class TechTreePanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color unlockedColor = new Color(0.3f, 0.7f, 0.3f);
        [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f);
        [SerializeField] private Color researchingColor = new Color(0.9f, 0.7f, 0.2f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _treeContainer;
        private GameObject _detailPanel;
        private TechBranch _selectedBranch = TechBranch.Military;
        private Technology _selectedTech;
        private Dictionary<TechBranch, GameObject> _branchTabs = new Dictionary<TechBranch, GameObject>();
        
        // Tech data
        private Dictionary<string, Technology> _technologies = new Dictionary<string, Technology>();
        private string _currentResearchId;
        private float _researchProgress;
        private float _researchSpeed = 1f;
        
        public static TechTreePanel Instance { get; private set; }
        
        public event Action<Technology> OnResearchStarted;
        public event Action<Technology> OnResearchCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeTechTree();
        }

        private void Start()
        {
            CreatePanel();
            Hide();
        }

        private void Update()
        {
            if (!string.IsNullOrEmpty(_currentResearchId))
            {
                UpdateResearch();
            }
        }

        private void InitializeTechTree()
        {
            // === MILITARY BRANCH ===
            AddTech(new Technology
            {
                TechId = "MIL_001",
                Name = "Basic Warfare",
                Description = "Unlocks basic military units and combat strategies.",
                Icon = "[ATK]",
                Branch = TechBranch.Military,
                Tier = 1,
                State = TechState.Unlocked,
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 500 } },
                ResearchTime = 60
            });
            
            AddTech(new Technology
            {
                TechId = "MIL_002",
                Name = "Improved Weapons",
                Description = "+10% attack power for all infantry units.",
                Icon = "[W]",
                Branch = TechBranch.Military,
                Tier = 2,
                State = TechState.Available,
                Prerequisites = new List<string> { "MIL_001" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 1000 }, { ResourceType.Iron, 200 } },
                ResearchTime = 180,
                Effects = new List<TechEffect>
                {
                    new TechEffect { Type = EffectType.UnitAttack, TargetUnit = TroopType.Infantry, Value = 10 }
                }
            });
            
            AddTech(new Technology
            {
                TechId = "MIL_003",
                Name = "Cavalry Tactics",
                Description = "Unlocks Cavalry units with +20% movement speed.",
                Icon = "[H]",
                Branch = TechBranch.Military,
                Tier = 2,
                State = TechState.Available,
                Prerequisites = new List<string> { "MIL_001" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 1200 }, { ResourceType.Wood, 300 } },
                ResearchTime = 240,
                UnlocksUnits = new List<TroopType> { TroopType.Cavalry }
            });
            
            AddTech(new Technology
            {
                TechId = "MIL_004",
                Name = "Siege Engineering",
                Description = "Unlocks Siege weapons effective against buildings.",
                Icon = "[C]",
                Branch = TechBranch.Military,
                Tier = 3,
                State = TechState.Locked,
                Prerequisites = new List<string> { "MIL_002", "MIL_003" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 2500 }, { ResourceType.Iron, 500 }, { ResourceType.Stone, 400 } },
                ResearchTime = 420,
                UnlocksUnits = new List<TroopType> { TroopType.Siege }
            });
            
            AddTech(new Technology
            {
                TechId = "MIL_005",
                Name = "Elite Warriors",
                Description = "Unlocks Elite units with powerful abilities.",
                Icon = "[K]",
                Branch = TechBranch.Military,
                Tier = 4,
                State = TechState.Locked,
                Prerequisites = new List<string> { "MIL_004" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 5000 }, { ResourceType.Crystal, 200 } },
                ResearchTime = 600,
                UnlocksUnits = new List<TroopType> { TroopType.Elite }
            });
            
            AddTech(new Technology
            {
                TechId = "MIL_006",
                Name = "Battle Formations",
                Description = "+15% defense when defending your territory.",
                Icon = "[DEF]",
                Branch = TechBranch.Military,
                Tier = 3,
                State = TechState.Locked,
                Prerequisites = new List<string> { "MIL_002" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 2000 }, { ResourceType.Iron, 350 } },
                ResearchTime = 300,
                Effects = new List<TechEffect>
                {
                    new TechEffect { Type = EffectType.DefenseBonus, Value = 15 }
                }
            });
            
            // === ECONOMY BRANCH ===
            AddTech(new Technology
            {
                TechId = "ECO_001",
                Name = "Basic Economics",
                Description = "Unlocks basic resource production buildings.",
                Icon = "[$]",
                Branch = TechBranch.Economy,
                Tier = 1,
                State = TechState.Unlocked,
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 300 } },
                ResearchTime = 45
            });
            
            AddTech(new Technology
            {
                TechId = "ECO_002",
                Name = "Efficient Mining",
                Description = "+25% Stone and Iron production.",
                Icon = "[M]",
                Branch = TechBranch.Economy,
                Tier = 2,
                State = TechState.Available,
                Prerequisites = new List<string> { "ECO_001" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 800 }, { ResourceType.Stone, 150 } },
                ResearchTime = 150,
                Effects = new List<TechEffect>
                {
                    new TechEffect { Type = EffectType.ResourceProduction, TargetResource = ResourceType.Stone, Value = 25 },
                    new TechEffect { Type = EffectType.ResourceProduction, TargetResource = ResourceType.Iron, Value = 25 }
                }
            });
            
            AddTech(new Technology
            {
                TechId = "ECO_003",
                Name = "Forestry",
                Description = "+30% Wood production.",
                Icon = "🌲",
                Branch = TechBranch.Economy,
                Tier = 2,
                State = TechState.Available,
                Prerequisites = new List<string> { "ECO_001" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 600 }, { ResourceType.Wood, 200 } },
                ResearchTime = 120,
                Effects = new List<TechEffect>
                {
                    new TechEffect { Type = EffectType.ResourceProduction, TargetResource = ResourceType.Wood, Value = 30 }
                }
            });
            
            AddTech(new Technology
            {
                TechId = "ECO_004",
                Name = "Trade Routes",
                Description = "Unlocks Market building. +10% Gold income.",
                Icon = "[M]",
                Branch = TechBranch.Economy,
                Tier = 3,
                State = TechState.Locked,
                Prerequisites = new List<string> { "ECO_002", "ECO_003" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 1500 }, { ResourceType.Wood, 300 } },
                ResearchTime = 240,
                UnlocksBuildings = new List<string> { "Market" },
                Effects = new List<TechEffect>
                {
                    new TechEffect { Type = EffectType.ResourceProduction, TargetResource = ResourceType.Gold, Value = 10 }
                }
            });
            
            AddTech(new Technology
            {
                TechId = "ECO_005",
                Name = "Treasury Management",
                Description = "+20% storage capacity. +5% interest on stored gold.",
                Icon = "🏦",
                Branch = TechBranch.Economy,
                Tier = 4,
                State = TechState.Locked,
                Prerequisites = new List<string> { "ECO_004" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 3000 }, { ResourceType.Crystal, 100 } },
                ResearchTime = 360,
                Effects = new List<TechEffect>
                {
                    new TechEffect { Type = EffectType.StorageCapacity, Value = 20 }
                }
            });
            
            AddTech(new Technology
            {
                TechId = "ECO_006",
                Name = "Crystal Synthesis",
                Description = "Unlocks Crystal production. Essential for advanced tech.",
                Icon = "[G]",
                Branch = TechBranch.Economy,
                Tier = 5,
                State = TechState.Locked,
                Prerequisites = new List<string> { "ECO_005" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 5000 }, { ResourceType.Iron, 1000 } },
                ResearchTime = 600,
                UnlocksBuildings = new List<string> { "Crystal Mine" }
            });
            
            // === DEFENSE BRANCH ===
            AddTech(new Technology
            {
                TechId = "DEF_001",
                Name = "Basic Fortifications",
                Description = "Unlocks basic walls and defensive structures.",
                Icon = "🧱",
                Branch = TechBranch.Defense,
                Tier = 1,
                State = TechState.Unlocked,
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Stone, 200 } },
                ResearchTime = 60
            });
            
            AddTech(new Technology
            {
                TechId = "DEF_002",
                Name = "Watch Towers",
                Description = "Unlocks towers that attack enemies automatically.",
                Icon = "🗼",
                Branch = TechBranch.Defense,
                Tier = 2,
                State = TechState.Available,
                Prerequisites = new List<string> { "DEF_001" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Stone, 400 }, { ResourceType.Wood, 200 } },
                ResearchTime = 180,
                UnlocksBuildings = new List<string> { "Watch Tower" }
            });
            
            AddTech(new Technology
            {
                TechId = "DEF_003",
                Name = "Reinforced Walls",
                Description = "+50% wall HP. Walls slow enemy advancement.",
                Icon = "[G]",
                Branch = TechBranch.Defense,
                Tier = 3,
                State = TechState.Locked,
                Prerequisites = new List<string> { "DEF_002" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Stone, 800 }, { ResourceType.Iron, 300 } },
                ResearchTime = 300,
                Effects = new List<TechEffect>
                {
                    new TechEffect { Type = EffectType.WallHealth, Value = 50 }
                }
            });
            
            AddTech(new Technology
            {
                TechId = "DEF_004",
                Name = "Trap Systems",
                Description = "Unlocks hidden traps that damage attackers.",
                Icon = "[!]",
                Branch = TechBranch.Defense,
                Tier = 3,
                State = TechState.Locked,
                Prerequisites = new List<string> { "DEF_002" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 1000 }, { ResourceType.Iron, 200 } },
                ResearchTime = 240,
                UnlocksBuildings = new List<string> { "Spike Trap", "Fire Trap" }
            });
            
            AddTech(new Technology
            {
                TechId = "DEF_005",
                Name = "Citadel Upgrade",
                Description = "Unlock the ultimate fortress upgrade for your citadel.",
                Icon = "[C]",
                Branch = TechBranch.Defense,
                Tier = 5,
                State = TechState.Locked,
                Prerequisites = new List<string> { "DEF_003", "DEF_004" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 8000 }, { ResourceType.Stone, 2000 }, { ResourceType.Crystal, 300 } },
                ResearchTime = 900,
                UnlocksBuildings = new List<string> { "Citadel" }
            });
            
            // === EXPANSION BRANCH ===
            AddTech(new Technology
            {
                TechId = "EXP_001",
                Name = "Scouting",
                Description = "Reveals nearby territories. Unlocks Scout unit.",
                Icon = "[T]",
                Branch = TechBranch.Expansion,
                Tier = 1,
                State = TechState.Unlocked,
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 200 } },
                ResearchTime = 30
            });
            
            AddTech(new Technology
            {
                TechId = "EXP_002",
                Name = "Territory Claim",
                Description = "+1 maximum territory. Faster capture speed.",
                Icon = "🚩",
                Branch = TechBranch.Expansion,
                Tier = 2,
                State = TechState.Available,
                Prerequisites = new List<string> { "EXP_001" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 1000 }, { ResourceType.Wood, 300 } },
                ResearchTime = 200,
                Effects = new List<TechEffect>
                {
                    new TechEffect { Type = EffectType.MaxTerritories, Value = 1 }
                }
            });
            
            AddTech(new Technology
            {
                TechId = "EXP_003",
                Name = "Outpost Networks",
                Description = "Unlocks Outposts. Provides vision and fast travel.",
                Icon = "[C]",
                Branch = TechBranch.Expansion,
                Tier = 3,
                State = TechState.Locked,
                Prerequisites = new List<string> { "EXP_002" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 1500 }, { ResourceType.Stone, 400 } },
                ResearchTime = 300,
                UnlocksBuildings = new List<string> { "Outpost" }
            });
            
            AddTech(new Technology
            {
                TechId = "EXP_004",
                Name = "Empire Logistics",
                Description = "+2 maximum territories. -20% troop travel time.",
                Icon = GameIcons.Map,
                Branch = TechBranch.Expansion,
                Tier = 4,
                State = TechState.Locked,
                Prerequisites = new List<string> { "EXP_003" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 3000 }, { ResourceType.Crystal, 150 } },
                ResearchTime = 480,
                Effects = new List<TechEffect>
                {
                    new TechEffect { Type = EffectType.MaxTerritories, Value = 2 },
                    new TechEffect { Type = EffectType.TroopSpeed, Value = 20 }
                }
            });
            
            // === ALLIANCE BRANCH ===
            AddTech(new Technology
            {
                TechId = "ALL_001",
                Name = "Diplomacy",
                Description = "Unlocks alliance features and basic cooperation.",
                Icon = "[H]",
                Branch = TechBranch.Alliance,
                Tier = 1,
                State = TechState.Unlocked,
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 400 } },
                ResearchTime = 60
            });
            
            AddTech(new Technology
            {
                TechId = "ALL_002",
                Name = "Resource Sharing",
                Description = "Share resources with alliance members. +10% shared bonus.",
                Icon = "[B]",
                Branch = TechBranch.Alliance,
                Tier = 2,
                State = TechState.Available,
                Prerequisites = new List<string> { "ALL_001" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 800 } },
                ResearchTime = 150
            });
            
            AddTech(new Technology
            {
                TechId = "ALL_003",
                Name = "Joint Operations",
                Description = "Unlocks coordinated attacks with alliance members.",
                Icon = "[+]",
                Branch = TechBranch.Alliance,
                Tier = 3,
                State = TechState.Locked,
                Prerequisites = new List<string> { "ALL_002" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 2000 }, { ResourceType.Iron, 400 } },
                ResearchTime = 300
            });
            
            AddTech(new Technology
            {
                TechId = "ALL_004",
                Name = "Alliance Stronghold",
                Description = "Contribute to a shared alliance fortress.",
                Icon = "🏯",
                Branch = TechBranch.Alliance,
                Tier = 4,
                State = TechState.Locked,
                Prerequisites = new List<string> { "ALL_003" },
                Costs = new Dictionary<ResourceType, int> { { ResourceType.Gold, 4000 }, { ResourceType.Stone, 1000 }, { ResourceType.Crystal, 200 } },
                ResearchTime = 600,
                UnlocksBuildings = new List<string> { "Alliance Stronghold" }
            });
        }

        private void AddTech(Technology tech)
        {
            _technologies[tech.TechId] = tech;
        }

        private void CreatePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("TechTreePanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.05f);
            rect.anchorMax = new Vector2(0.92f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.1f, 0.98f);
            
            // Layout
            HorizontalLayoutGroup hlayout = _panel.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Left side - Tree view
            CreateTreeSection();
            
            // Right side - Detail panel
            CreateDetailSection();
        }

        private void CreateTreeSection()
        {
            GameObject treeSection = new GameObject("TreeSection");
            treeSection.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = treeSection.AddComponent<LayoutElement>();
            le.flexibleWidth = 2;
            le.flexibleHeight = 1;
            
            Image bg = treeSection.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f);
            
            VerticalLayoutGroup vlayout = treeSection.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Header with tabs
            CreateTreeHeader(treeSection.transform);
            
            // Tech tree view
            CreateTreeView(treeSection.transform);
        }

        private void CreateTreeHeader(Transform parent)
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(parent, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            VerticalLayoutGroup vlayout = header.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.spacing = 10;
            
            // Title
            GameObject title = new GameObject("Title");
            title.transform.SetParent(header.transform, false);
            
            TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "[R] TECHNOLOGY RESEARCH";
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = accentColor;
            
            // Branch tabs
            GameObject tabs = new GameObject("Tabs");
            tabs.transform.SetParent(header.transform, false);
            
            LayoutElement tabsLE = tabs.AddComponent<LayoutElement>();
            tabsLE.preferredHeight = 35;
            
            HorizontalLayoutGroup tabsHL = tabs.AddComponent<HorizontalLayoutGroup>();
            tabsHL.childAlignment = TextAnchor.MiddleCenter;
            tabsHL.childForceExpandWidth = true;
            tabsHL.spacing = 5;
            
            CreateBranchTab(tabs.transform, TechBranch.Military, $"{GameIcons.Battle} Military");
            CreateBranchTab(tabs.transform, TechBranch.Economy, $"{GameIcons.Gold} Economy");
            CreateBranchTab(tabs.transform, TechBranch.Defense, $"{GameIcons.Shield} Defense");
            CreateBranchTab(tabs.transform, TechBranch.Expansion, $"{GameIcons.Map} Expansion");
            CreateBranchTab(tabs.transform, TechBranch.Alliance, $"{GameIcons.Alliance} Alliance");
        }

        private void CreateBranchTab(Transform parent, TechBranch branch, string label)
        {
            GameObject tab = new GameObject($"Tab_{branch}");
            tab.transform.SetParent(parent, false);
            
            LayoutElement le = tab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            Color bgColor = branch == _selectedBranch ? accentColor : new Color(0.15f, 0.15f, 0.2f);
            
            Image bg = tab.AddComponent<Image>();
            bg.color = bgColor;
            
            Button btn = tab.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectBranch(branch));
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tab.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 12;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            _branchTabs[branch] = tab;
        }

        private void CreateTreeView(Transform parent)
        {
            _treeContainer = new GameObject("TreeView");
            _treeContainer.transform.SetParent(parent, false);
            
            LayoutElement le = _treeContainer.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            
            Image bg = _treeContainer.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.03f, 0.05f);
            
            RefreshTreeView();
        }

        private void RefreshTreeView()
        {
            foreach (Transform child in _treeContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Get techs for selected branch
            List<Technology> branchTechs = new List<Technology>();
            foreach (var kvp in _technologies)
            {
                if (kvp.Value.Branch == _selectedBranch)
                    branchTechs.Add(kvp.Value);
            }
            
            // Sort by tier
            branchTechs.Sort((a, b) => a.Tier.CompareTo(b.Tier));
            
            // Create tier rows
            int maxTier = 0;
            foreach (var tech in branchTechs)
            {
                if (tech.Tier > maxTier) maxTier = tech.Tier;
            }
            
            VerticalLayoutGroup vlayout = _treeContainer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = true;
            vlayout.spacing = 15;
            vlayout.padding = new RectOffset(20, 20, 20, 20);
            
            for (int tier = 1; tier <= maxTier; tier++)
            {
                CreateTierRow(tier, branchTechs);
            }
        }

        private void CreateTierRow(int tier, List<Technology> allTechs)
        {
            List<Technology> tierTechs = allTechs.FindAll(t => t.Tier == tier);
            if (tierTechs.Count == 0) return;
            
            GameObject row = new GameObject($"Tier_{tier}");
            row.transform.SetParent(_treeContainer.transform, false);
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 20;
            
            // Tier label
            GameObject tierLabel = new GameObject("TierLabel");
            tierLabel.transform.SetParent(row.transform, false);
            
            LayoutElement tierLE = tierLabel.AddComponent<LayoutElement>();
            tierLE.preferredWidth = 60;
            
            TextMeshProUGUI tierText = tierLabel.AddComponent<TextMeshProUGUI>();
            tierText.text = $"T{tier}";
            tierText.fontSize = 14;
            tierText.fontStyle = FontStyles.Bold;
            tierText.alignment = TextAlignmentOptions.Center;
            tierText.color = new Color(0.5f, 0.5f, 0.5f);
            
            // Tech nodes
            foreach (var tech in tierTechs)
            {
                CreateTechNode(row.transform, tech);
            }
        }

        private void CreateTechNode(Transform parent, Technology tech)
        {
            GameObject node = new GameObject($"Tech_{tech.TechId}");
            node.transform.SetParent(parent, false);
            
            LayoutElement le = node.AddComponent<LayoutElement>();
            le.preferredWidth = 120;
            le.preferredHeight = 80;
            
            Color nodeColor = tech.State switch
            {
                TechState.Unlocked => unlockedColor,
                TechState.Available => accentColor,
                TechState.Researching => researchingColor,
                _ => lockedColor
            };
            
            Image bg = node.AddComponent<Image>();
            bg.color = new Color(nodeColor.r * 0.3f, nodeColor.g * 0.3f, nodeColor.b * 0.3f);
            
            UnityEngine.UI.Outline outline = node.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = nodeColor;
            outline.effectDistance = new Vector2(2, 2);
            
            Button btn = node.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectTechnology(tech));
            
            VerticalLayoutGroup vlayout = node.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(5, 5, 5, 5);
            
            // Icon
            CreateText(node.transform, tech.Icon, 28, TextAlignmentOptions.Center);
            
            // Name
            CreateText(node.transform, tech.Name, 10, TextAlignmentOptions.Center, Color.white);
            
            // Status indicator
            string statusStr = tech.State switch
            {
                TechState.Unlocked => "[OK]",
                TechState.Researching => "[T]",
                TechState.Available => "",
                _ => "[L]"
            };
            if (!string.IsNullOrEmpty(statusStr))
            {
                CreateText(node.transform, statusStr, 14, TextAlignmentOptions.Center, nodeColor);
            }
        }

        private void CreateDetailSection()
        {
            _detailPanel = new GameObject("DetailSection");
            _detailPanel.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _detailPanel.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            
            Image bg = _detailPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f);
            
            VerticalLayoutGroup vlayout = _detailPanel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 15;
            vlayout.padding = new RectOffset(20, 20, 20, 20);
            
            RefreshDetailPanel();
        }

        private void RefreshDetailPanel()
        {
            foreach (Transform child in _detailPanel.transform)
            {
                Destroy(child.gameObject);
            }
            
            if (_selectedTech == null)
            {
                CreateText(_detailPanel.transform, "Select a technology to view details", 14, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
                return;
            }
            
            // Header with close button
            CreateDetailHeader();
            
            // Tech icon and name
            CreateTechHeader();
            
            // Description
            CreateText(_detailPanel.transform, _selectedTech.Description, 13, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f));
            
            // Costs
            CreateCostSection();
            
            // Prerequisites
            if (_selectedTech.Prerequisites != null && _selectedTech.Prerequisites.Count > 0)
            {
                CreatePrerequisitesSection();
            }
            
            // Effects
            if (_selectedTech.Effects != null && _selectedTech.Effects.Count > 0)
            {
                CreateEffectsSection();
            }
            
            // Unlocks
            CreateUnlocksSection();
            
            // Action button
            CreateActionButton();
        }

        private void CreateDetailHeader()
        {
            GameObject header = new GameObject("DetailHeader");
            header.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleRight;
            
            // Close button
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(header.transform, false);
            
            LayoutElement closele = closeBtn.AddComponent<LayoutElement>();
            closele.preferredWidth = 30;
            closele.preferredHeight = 30;
            
            Image closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = new Color(0.5f, 0.2f, 0.2f, 0.8f);
            
            Button btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(Hide);
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(closeBtn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI x = textObj.AddComponent<TextMeshProUGUI>();
            x.text = "[X]";
            x.fontSize = 18;
            x.alignment = TextAlignmentOptions.Center;
        }

        private void CreateTechHeader()
        {
            GameObject header = new GameObject("TechHeader");
            header.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            VerticalLayoutGroup vlayout = header.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            
            Color stateColor = _selectedTech.State switch
            {
                TechState.Unlocked => unlockedColor,
                TechState.Available => accentColor,
                TechState.Researching => researchingColor,
                _ => lockedColor
            };
            
            CreateText(header.transform, _selectedTech.Icon, 48, TextAlignmentOptions.Center);
            CreateText(header.transform, _selectedTech.Name, 20, TextAlignmentOptions.Center, stateColor);
            CreateText(header.transform, $"Tier {_selectedTech.Tier} - {_selectedTech.Branch}", 12, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateCostSection()
        {
            CreateSectionLabel("[T] RESEARCH COST");
            
            GameObject costs = new GameObject("Costs");
            costs.transform.SetParent(_detailPanel.transform, false);
            
            HorizontalLayoutGroup hlayout = costs.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            
            if (_selectedTech.Costs != null)
            {
                foreach (var kvp in _selectedTech.Costs)
                {
                    string icon = GetResourceIcon(kvp.Key);
                    CreateText(costs.transform, $"{icon} {kvp.Value:N0}", 14, TextAlignmentOptions.Center, GetResourceColor(kvp.Key));
                }
            }
            
            // Time
            TimeSpan time = TimeSpan.FromSeconds(_selectedTech.ResearchTime);
            string timeStr = time.TotalHours >= 1 ? $"{(int)time.TotalHours}h {time.Minutes}m" : $"{time.Minutes}m {time.Seconds}s";
            CreateText(costs.transform, $"[T] {timeStr}", 14, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
        }

        private void CreatePrerequisitesSection()
        {
            CreateSectionLabel("[L] PREREQUISITES");
            
            GameObject prereqs = new GameObject("Prerequisites");
            prereqs.transform.SetParent(_detailPanel.transform, false);
            
            VerticalLayoutGroup vlayout = prereqs.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            
            foreach (var prereqId in _selectedTech.Prerequisites)
            {
                if (_technologies.TryGetValue(prereqId, out Technology prereq))
                {
                    Color color = prereq.State == TechState.Unlocked ? unlockedColor : urgentColor;
                    string check = prereq.State == TechState.Unlocked ? "[OK]" : "[X]";
                    CreateText(prereqs.transform, $"{check} {prereq.Icon} {prereq.Name}", 12, TextAlignmentOptions.Center, color);
                }
            }
        }

        private void CreateEffectsSection()
        {
            CreateSectionLabel("[!] EFFECTS");
            
            GameObject effects = new GameObject("Effects");
            effects.transform.SetParent(_detailPanel.transform, false);
            
            VerticalLayoutGroup vlayout = effects.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            
            foreach (var effect in _selectedTech.Effects)
            {
                string effectStr = effect.Type switch
                {
                    EffectType.UnitAttack => $"+{effect.Value}% {effect.TargetUnit} Attack",
                    EffectType.UnitDefense => $"+{effect.Value}% {effect.TargetUnit} Defense",
                    EffectType.ResourceProduction => $"+{effect.Value}% {effect.TargetResource} Production",
                    EffectType.StorageCapacity => $"+{effect.Value}% Storage Capacity",
                    EffectType.DefenseBonus => $"+{effect.Value}% Territory Defense",
                    EffectType.WallHealth => $"+{effect.Value}% Wall HP",
                    EffectType.MaxTerritories => $"+{effect.Value} Max Territories",
                    EffectType.TroopSpeed => $"+{effect.Value}% Troop Speed",
                    _ => $"+{effect.Value}%"
                };
                CreateText(effects.transform, $"- {effectStr}", 12, TextAlignmentOptions.Center, accentColor);
            }
        }

        private void CreateUnlocksSection()
        {
            bool hasUnlocks = (_selectedTech.UnlocksBuildings != null && _selectedTech.UnlocksBuildings.Count > 0) ||
                              (_selectedTech.UnlocksUnits != null && _selectedTech.UnlocksUnits.Count > 0);
            
            if (!hasUnlocks) return;
            
            CreateSectionLabel("[U] UNLOCKS");
            
            GameObject unlocks = new GameObject("Unlocks");
            unlocks.transform.SetParent(_detailPanel.transform, false);
            
            VerticalLayoutGroup vlayout = unlocks.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            
            if (_selectedTech.UnlocksBuildings != null)
            {
                foreach (var building in _selectedTech.UnlocksBuildings)
                {
                    CreateText(unlocks.transform, $"[H] {building}", 12, TextAlignmentOptions.Center, goldColor);
                }
            }
            
            if (_selectedTech.UnlocksUnits != null)
            {
                foreach (var unit in _selectedTech.UnlocksUnits)
                {
                    string unitIcon = unit switch
                    {
                        TroopType.Infantry => "[ATK]",
                        TroopType.Archer => "[A]",
                        TroopType.Cavalry => "[H]",
                        TroopType.Siege => "[C]",
                        TroopType.Elite => "[K]",
                        _ => "[U]"
                    };
                    CreateText(unlocks.transform, $"{unitIcon} {unit}", 12, TextAlignmentOptions.Center, goldColor);
                }
            }
        }

        private void CreateActionButton()
        {
            GameObject btnContainer = new GameObject("ActionBtn");
            btnContainer.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = btnContainer.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            string label;
            Color color;
            bool interactable = true;
            
            switch (_selectedTech.State)
            {
                case TechState.Unlocked:
                    label = "[OK] Researched";
                    color = unlockedColor * 0.6f;
                    interactable = false;
                    break;
                case TechState.Researching:
                    label = $"Researching... {_researchProgress:F0}%";
                    color = researchingColor;
                    interactable = false;
                    break;
                case TechState.Available:
                    label = "[R] Start Research";
                    color = accentColor;
                    break;
                default:
                    label = "[L] Locked";
                    color = lockedColor;
                    interactable = false;
                    break;
            }
            
            Image bg = btnContainer.AddComponent<Image>();
            bg.color = color;
            
            Button btn = btnContainer.AddComponent<Button>();
            btn.interactable = interactable;
            btn.onClick.AddListener(() => StartResearch(_selectedTech));
            
            TextMeshProUGUI text = btnContainer.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 16;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

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
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.6f, 0.6f, 0.7f);
        }

        private Color urgentColor = new Color(0.9f, 0.3f, 0.3f);

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

        private string GetResourceIcon(ResourceType type)
        {
            return type switch
            {
                ResourceType.Gold => "[$]",
                ResourceType.Stone => "[Q]",
                ResourceType.Wood => "[W]",
                ResourceType.Iron => "[P]",
                ResourceType.Crystal => "[G]",
                ResourceType.ApexCoins => "[$]",
                _ => "[B]"
            };
        }

        private Color GetResourceColor(ResourceType type)
        {
            return type switch
            {
                ResourceType.Gold => goldColor,
                ResourceType.Stone => new Color(0.6f, 0.6f, 0.6f),
                ResourceType.Wood => new Color(0.6f, 0.4f, 0.2f),
                ResourceType.Iron => new Color(0.5f, 0.5f, 0.6f),
                ResourceType.Crystal => new Color(0.7f, 0.4f, 0.9f),
                ResourceType.ApexCoins => new Color(0.3f, 0.6f, 0.9f),
                _ => Color.white
            };
        }

        #region Research Logic

        private void StartResearch(Technology tech)
        {
            if (tech.State != TechState.Available) return;
            
            // Check if already researching something
            if (!string.IsNullOrEmpty(_currentResearchId))
            {
                ApexLogger.Log("[TechTree] Already researching another technology!", ApexLogger.LogCategory.UI);
                return;
            }
            
            // TODO: Check and deduct costs
            
            tech.State = TechState.Researching;
            _currentResearchId = tech.TechId;
            _researchProgress = 0;
            
            OnResearchStarted?.Invoke(tech);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Started researching {tech.Name}");
            }
            
            RefreshTreeView();
            RefreshDetailPanel();
            
            ApexLogger.Log($"[TechTree] Started research: {tech.Name}", ApexLogger.LogCategory.UI);
        }

        private void UpdateResearch()
        {
            if (!_technologies.TryGetValue(_currentResearchId, out Technology tech))
            {
                _currentResearchId = null;
                return;
            }
            
            float progressPerSecond = 100f / tech.ResearchTime * _researchSpeed;
            _researchProgress += progressPerSecond * Time.deltaTime;
            
            if (_researchProgress >= 100f)
            {
                CompleteResearch(tech);
            }
        }

        private void CompleteResearch(Technology tech)
        {
            tech.State = TechState.Unlocked;
            _currentResearchId = null;
            _researchProgress = 0;
            
            // Unlock dependent techs
            foreach (var kvp in _technologies)
            {
                var otherTech = kvp.Value;
                if (otherTech.State == TechState.Locked && otherTech.Prerequisites != null)
                {
                    bool allPrereqsMet = true;
                    foreach (var prereqId in otherTech.Prerequisites)
                    {
                        if (_technologies.TryGetValue(prereqId, out Technology prereq))
                        {
                            if (prereq.State != TechState.Unlocked)
                            {
                                allPrereqsMet = false;
                                break;
                            }
                        }
                    }
                    
                    if (allPrereqsMet)
                    {
                        otherTech.State = TechState.Available;
                    }
                }
            }
            
            OnResearchCompleted?.Invoke(tech);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowAchievementUnlocked($"Researched: {tech.Name}", 0);
            }
            
            RefreshTreeView();
            RefreshDetailPanel();
            
            ApexLogger.Log($"[TechTree] Research complete: {tech.Name}", ApexLogger.LogCategory.UI);
        }

        #endregion

        private void SelectBranch(TechBranch branch)
        {
            _selectedBranch = branch;
            _selectedTech = null;
            
            foreach (var kvp in _branchTabs)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == branch ? accentColor : new Color(0.15f, 0.15f, 0.2f);
            }
            
            RefreshTreeView();
            RefreshDetailPanel();
        }

        private void SelectTechnology(Technology tech)
        {
            _selectedTech = tech;
            RefreshDetailPanel();
        }

        #region Public API

        public void Show()
        {
            _panel.SetActive(true);
            RefreshTreeView();
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

        public bool IsTechUnlocked(string techId)
        {
            return _technologies.TryGetValue(techId, out Technology tech) && tech.State == TechState.Unlocked;
        }

        public float GetResearchProgress() => _researchProgress;
        public string GetCurrentResearchId() => _currentResearchId;

        #endregion
    }

    #region Data Classes

    public enum TechBranch
    {
        Military,
        Economy,
        Defense,
        Expansion,
        Alliance
    }

    public enum TechState
    {
        Locked,
        Available,
        Researching,
        Unlocked
    }

    public enum EffectType
    {
        UnitAttack,
        UnitDefense,
        ResourceProduction,
        StorageCapacity,
        DefenseBonus,
        WallHealth,
        MaxTerritories,
        TroopSpeed
    }

    public class Technology
    {
        public string TechId;
        public string Name;
        public string Description;
        public string Icon;
        public TechBranch Branch;
        public int Tier;
        public TechState State;
        public List<string> Prerequisites;
        public Dictionary<ResourceType, int> Costs;
        public int ResearchTime; // seconds
        public List<TechEffect> Effects;
        public List<string> UnlocksBuildings;
        public List<TroopType> UnlocksUnits;
    }

    public class TechEffect
    {
        public EffectType Type;
        public float Value;
        public TroopType TargetUnit;
        public ResourceType TargetResource;
    }

    #endregion
}
