using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Player Profile Panel - Shows player stats, avatar, inventory, and achievements.
    /// Essential for player identity and progression tracking.
    /// </summary>
    public class PlayerProfilePanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color activeTabColor = new Color(0.2f, 0.4f, 0.6f);
        
        // UI Elements
        private GameObject _panel;
        private ProfileTab _selectedTab = ProfileTab.Overview;
        private Dictionary<ProfileTab, GameObject> _tabButtons = new Dictionary<ProfileTab, GameObject>();
        private GameObject _contentContainer;
        
        // Player data
        private PlayerProfile _profile;
        
        public static PlayerProfilePanel Instance { get; private set; }
        
        public event Action<PlayerProfile> OnProfileUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadProfile();
        }

        private void Start()
        {
            CreateProfilePanel();
            Hide();
        }

        private void LoadProfile()
        {
            // Load from PlayerPrefs or use demo data
            _profile = new PlayerProfile
            {
                PlayerId = "DEMO_USER_001",
                DisplayName = "Commander Alpha",
                Level = 25,
                Experience = 45000,
                ExperienceToNextLevel = 50000,
                TotalPlayTime = TimeSpan.FromHours(156),
                JoinDate = DateTime.Now.AddMonths(-3),
                
                // Stats
                TotalBattles = 342,
                BattlesWon = 287,
                TotalTroopsLost = 15420,
                TotalTroopsKilled = 28750,
                TerritoriesCaptured = 89,
                TerritoriesLost = 34,
                ResourcesGathered = 2500000,
                BuildingsBuilt = 456,
                QuestsCompleted = 123,
                AchievementsUnlocked = 45,
                
                // Alliance
                AllianceName = "Steel Legion",
                AllianceRank = "Officer",
                AllianceId = "ALLIANCE_001",
                
                // Titles and badges
                CurrentTitle = "The Conqueror",
                SelectedFrame = "gold_frame",
                SelectedAvatar = "warrior_1"
            };
        }

        private void CreateProfilePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("PlayerProfilePanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.1f);
            rect.anchorMax = new Vector2(0.85f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);
            
            VerticalLayoutGroup vlayout = _panel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Header with player summary
            CreateHeader();
            
            // Tab buttons
            CreateTabs();
            
            // Content area
            CreateContentArea();
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 120;
            
            Image headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.1f, 0.15f, 0.25f, 0.8f);
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            // Avatar frame
            CreateAvatarSection(header.transform);
            
            // Player info
            CreatePlayerInfo(header.transform);
            
            // Close button
            CreateCloseButton(header.transform);
        }

        private void CreateAvatarSection(Transform parent)
        {
            GameObject avatarSection = new GameObject("AvatarSection");
            avatarSection.transform.SetParent(parent, false);
            
            LayoutElement le = avatarSection.AddComponent<LayoutElement>();
            le.preferredWidth = 100;
            le.preferredHeight = 100;
            
            // Frame
            Image frame = avatarSection.AddComponent<Image>();
            frame.color = goldColor;
            
            // Inner avatar
            GameObject avatar = new GameObject("Avatar");
            avatar.transform.SetParent(avatarSection.transform, false);
            
            RectTransform avatarRect = avatar.AddComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0.05f, 0.05f);
            avatarRect.anchorMax = new Vector2(0.95f, 0.95f);
            avatarRect.offsetMin = Vector2.zero;
            avatarRect.offsetMax = Vector2.zero;
            
            Image avatarBg = avatar.AddComponent<Image>();
            avatarBg.color = new Color(0.2f, 0.3f, 0.4f);
            
            // Avatar icon placeholder
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(avatar.transform, false);
            
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            
            TextMeshProUGUI iconText = icon.AddComponent<TextMeshProUGUI>();
            iconText.text = "⚔️";
            iconText.fontSize = 40;
            iconText.alignment = TextAlignmentOptions.Center;
            
            // Level badge
            GameObject levelBadge = new GameObject("LevelBadge");
            levelBadge.transform.SetParent(avatarSection.transform, false);
            
            RectTransform levelRect = levelBadge.AddComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.7f, 0f);
            levelRect.anchorMax = new Vector2(1.1f, 0.3f);
            levelRect.offsetMin = Vector2.zero;
            levelRect.offsetMax = Vector2.zero;
            
            Image levelBg = levelBadge.AddComponent<Image>();
            levelBg.color = accentColor;
            
            TextMeshProUGUI levelText = levelBadge.AddComponent<TextMeshProUGUI>();
            levelText.text = _profile.Level.ToString();
            levelText.fontSize = 16;
            levelText.fontStyle = FontStyles.Bold;
            levelText.alignment = TextAlignmentOptions.Center;
        }

        private void CreatePlayerInfo(Transform parent)
        {
            GameObject info = new GameObject("PlayerInfo");
            info.transform.SetParent(parent, false);
            
            LayoutElement le = info.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            VerticalLayoutGroup vlayout = info.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperLeft;
            vlayout.spacing = 5;
            
            // Name and title
            CreateText(info.transform, $"<b>{_profile.DisplayName}</b>", 24, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, $"<color=#{ColorUtility.ToHtmlStringRGB(goldColor)}>\"{_profile.CurrentTitle}\"</color>", 14, TextAlignmentOptions.Left);
            
            // XP bar
            GameObject xpBar = new GameObject("XPBar");
            xpBar.transform.SetParent(info.transform, false);
            
            LayoutElement xpLE = xpBar.AddComponent<LayoutElement>();
            xpLE.preferredHeight = 20;
            
            Image xpBg = xpBar.AddComponent<Image>();
            xpBg.color = new Color(0.15f, 0.15f, 0.2f);
            
            // XP fill
            GameObject xpFill = new GameObject("Fill");
            xpFill.transform.SetParent(xpBar.transform, false);
            
            RectTransform xpFillRect = xpFill.AddComponent<RectTransform>();
            xpFillRect.anchorMin = Vector2.zero;
            xpFillRect.anchorMax = new Vector2((float)_profile.Experience / _profile.ExperienceToNextLevel, 1f);
            xpFillRect.offsetMin = Vector2.zero;
            xpFillRect.offsetMax = Vector2.zero;
            
            Image xpFillImg = xpFill.AddComponent<Image>();
            xpFillImg.color = accentColor;
            
            // XP text
            GameObject xpText = new GameObject("XPText");
            xpText.transform.SetParent(xpBar.transform, false);
            
            RectTransform xpTextRect = xpText.AddComponent<RectTransform>();
            xpTextRect.anchorMin = Vector2.zero;
            xpTextRect.anchorMax = Vector2.one;
            
            TextMeshProUGUI xp = xpText.AddComponent<TextMeshProUGUI>();
            xp.text = $"XP: {_profile.Experience:N0} / {_profile.ExperienceToNextLevel:N0}";
            xp.fontSize = 12;
            xp.alignment = TextAlignmentOptions.Center;
            
            // Alliance
            CreateText(info.transform, $"🛡️ {_profile.AllianceName} • {_profile.AllianceRank}", 12, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.8f));
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(parent, false);
            
            LayoutElement le = closeBtn.AddComponent<LayoutElement>();
            le.preferredWidth = 40;
            le.preferredHeight = 40;
            
            Image bg = closeBtn.AddComponent<Image>();
            bg.color = new Color(0.6f, 0.2f, 0.2f, 0.8f);
            
            Button btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(Hide);
            
            TextMeshProUGUI x = closeBtn.AddComponent<TextMeshProUGUI>();
            x.text = "✕";
            x.fontSize = 24;
            x.alignment = TextAlignmentOptions.Center;
        }

        private void CreateTabs()
        {
            GameObject tabs = new GameObject("Tabs");
            tabs.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = tabs.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = tabs.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 5;
            
            CreateTab(tabs.transform, ProfileTab.Overview, "📊 Overview");
            CreateTab(tabs.transform, ProfileTab.Statistics, "📈 Statistics");
            CreateTab(tabs.transform, ProfileTab.Inventory, "🎒 Inventory");
            CreateTab(tabs.transform, ProfileTab.Titles, "🏆 Titles");
            CreateTab(tabs.transform, ProfileTab.History, "📜 History");
        }

        private void CreateTab(Transform parent, ProfileTab tab, string label)
        {
            GameObject tabObj = new GameObject($"Tab_{tab}");
            tabObj.transform.SetParent(parent, false);
            
            LayoutElement le = tabObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 35;
            
            Image bg = tabObj.AddComponent<Image>();
            bg.color = tab == _selectedTab ? activeTabColor : new Color(0.15f, 0.15f, 0.2f);
            
            Button btn = tabObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectTab(tab));
            
            TextMeshProUGUI text = tabObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            _tabButtons[tab] = tabObj;
        }

        private void CreateContentArea()
        {
            _contentContainer = new GameObject("Content");
            _contentContainer.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _contentContainer.AddComponent<LayoutElement>();
            le.flexibleHeight = 1;
            
            Image bg = _contentContainer.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.5f);
            
            VerticalLayoutGroup vlayout = _contentContainer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            RefreshContent();
        }

        private void RefreshContent()
        {
            foreach (Transform child in _contentContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            switch (_selectedTab)
            {
                case ProfileTab.Overview:
                    CreateOverviewContent();
                    break;
                case ProfileTab.Statistics:
                    CreateStatisticsContent();
                    break;
                case ProfileTab.Inventory:
                    CreateInventoryContent();
                    break;
                case ProfileTab.Titles:
                    CreateTitlesContent();
                    break;
                case ProfileTab.History:
                    CreateHistoryContent();
                    break;
            }
        }

        private void CreateOverviewContent()
        {
            // Quick stats grid
            CreateSectionHeader("⚔️ Combat Overview");
            
            GameObject statsGrid = new GameObject("StatsGrid");
            statsGrid.transform.SetParent(_contentContainer.transform, false);
            
            GridLayoutGroup grid = statsGrid.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(200, 80);
            grid.spacing = new Vector2(15, 15);
            grid.childAlignment = TextAnchor.UpperCenter;
            
            float winRate = _profile.TotalBattles > 0 ? (float)_profile.BattlesWon / _profile.TotalBattles * 100 : 0;
            float kd = _profile.TotalTroopsLost > 0 ? (float)_profile.TotalTroopsKilled / _profile.TotalTroopsLost : 0;
            
            CreateStatCard(statsGrid.transform, "⚔️ Total Battles", _profile.TotalBattles.ToString("N0"));
            CreateStatCard(statsGrid.transform, "🏆 Win Rate", $"{winRate:F1}%");
            CreateStatCard(statsGrid.transform, "⚡ K/D Ratio", $"{kd:F2}");
            CreateStatCard(statsGrid.transform, "🏰 Territories", $"{_profile.TerritoriesCaptured:N0} captured");
            CreateStatCard(statsGrid.transform, "📅 Play Time", $"{(int)_profile.TotalPlayTime.TotalHours}h {_profile.TotalPlayTime.Minutes}m");
            CreateStatCard(statsGrid.transform, "✅ Quests", $"{_profile.QuestsCompleted} completed");
            
            // Recent activity
            CreateSectionHeader("📋 Recent Activity");
            
            CreateActivityItem("Won battle in Northlands (+250 XP)");
            CreateActivityItem("Completed quest: Train 50 Archers");
            CreateActivityItem("Captured territory: Iron Mines");
            CreateActivityItem("Alliance member donated 5000 Gold");
        }

        private void CreateStatisticsContent()
        {
            CreateSectionHeader("⚔️ Combat Statistics");
            
            CreateStatRow("Total Battles", _profile.TotalBattles.ToString("N0"));
            CreateStatRow("Battles Won", _profile.BattlesWon.ToString("N0"));
            CreateStatRow("Battles Lost", (_profile.TotalBattles - _profile.BattlesWon).ToString("N0"));
            CreateStatRow("Troops Trained", (_profile.TotalTroopsKilled + 5000).ToString("N0"));
            CreateStatRow("Troops Lost", _profile.TotalTroopsLost.ToString("N0"));
            CreateStatRow("Enemy Troops Killed", _profile.TotalTroopsKilled.ToString("N0"));
            
            CreateSectionHeader("🏰 Territory Statistics");
            
            CreateStatRow("Territories Captured", _profile.TerritoriesCaptured.ToString("N0"));
            CreateStatRow("Territories Lost", _profile.TerritoriesLost.ToString("N0"));
            CreateStatRow("Current Territories", "5");
            CreateStatRow("Highest Territory Count", "12");
            
            CreateSectionHeader("💰 Economy Statistics");
            
            CreateStatRow("Total Resources Gathered", _profile.ResourcesGathered.ToString("N0"));
            CreateStatRow("Buildings Built", _profile.BuildingsBuilt.ToString("N0"));
            CreateStatRow("Gold Spent", "1,250,000");
            CreateStatRow("Trades Completed", "89");
            
            CreateSectionHeader("🎯 Progression Statistics");
            
            CreateStatRow("Quests Completed", _profile.QuestsCompleted.ToString("N0"));
            CreateStatRow("Achievements Unlocked", $"{_profile.AchievementsUnlocked}/75");
            CreateStatRow("Season Pass Level", "34");
            CreateStatRow("Days Logged In", "45");
        }

        private void CreateInventoryContent()
        {
            CreateSectionHeader("🎒 Items (24/50)");
            
            GameObject inventoryGrid = new GameObject("InventoryGrid");
            inventoryGrid.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = inventoryGrid.AddComponent<LayoutElement>();
            le.flexibleHeight = 1;
            
            GridLayoutGroup grid = inventoryGrid.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(70, 90);
            grid.spacing = new Vector2(10, 10);
            grid.childAlignment = TextAnchor.UpperLeft;
            
            // Sample inventory items
            CreateInventorySlot(inventoryGrid.transform, "⚔️", "Legendary Sword", "rare", 1);
            CreateInventorySlot(inventoryGrid.transform, "🛡️", "Golden Shield", "epic", 1);
            CreateInventorySlot(inventoryGrid.transform, "💎", "Crystal Gems", "common", 150);
            CreateInventorySlot(inventoryGrid.transform, "📜", "XP Scroll", "uncommon", 5);
            CreateInventorySlot(inventoryGrid.transform, "🧪", "Speed Potion", "common", 12);
            CreateInventorySlot(inventoryGrid.transform, "🎁", "Mystery Box", "rare", 3);
            CreateInventorySlot(inventoryGrid.transform, "🏆", "Trophy", "epic", 1);
            CreateInventorySlot(inventoryGrid.transform, "💰", "Gold Chest", "legendary", 2);
            
            // Empty slots
            for (int i = 0; i < 8; i++)
            {
                CreateInventorySlot(inventoryGrid.transform, "", "Empty", "empty", 0);
            }
        }

        private void CreateInventorySlot(Transform parent, string icon, string name, string rarity, int count)
        {
            GameObject slot = new GameObject($"Slot_{name}");
            slot.transform.SetParent(parent, false);
            
            Color rarityColor = rarity switch
            {
                "common" => new Color(0.7f, 0.7f, 0.7f),
                "uncommon" => new Color(0.2f, 0.8f, 0.2f),
                "rare" => new Color(0.2f, 0.4f, 0.9f),
                "epic" => new Color(0.6f, 0.2f, 0.8f),
                "legendary" => goldColor,
                _ => new Color(0.2f, 0.2f, 0.25f)
            };
            
            Image bg = slot.AddComponent<Image>();
            bg.color = rarity == "empty" ? new Color(0.15f, 0.15f, 0.2f) : new Color(0.1f, 0.1f, 0.15f);
            
            // Border
            UnityEngine.UI.Outline outline = slot.AddComponent<Outline>();
            outline.effectColor = rarityColor;
            outline.effectDistance = new Vector2(2, 2);
            
            if (rarity != "empty")
            {
                Button btn = slot.AddComponent<Button>();
                btn.onClick.AddListener(() => Debug.Log($"[Profile] Selected item: {name}"));
            }
            
            // Icon
            if (!string.IsNullOrEmpty(icon))
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(slot.transform, false);
                
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.1f, 0.3f);
                iconRect.anchorMax = new Vector2(0.9f, 0.9f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                
                TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
                iconText.text = icon;
                iconText.fontSize = 28;
                iconText.alignment = TextAlignmentOptions.Center;
            }
            
            // Count
            if (count > 1)
            {
                GameObject countObj = new GameObject("Count");
                countObj.transform.SetParent(slot.transform, false);
                
                RectTransform countRect = countObj.AddComponent<RectTransform>();
                countRect.anchorMin = new Vector2(0.5f, 0f);
                countRect.anchorMax = new Vector2(1f, 0.3f);
                countRect.offsetMin = Vector2.zero;
                countRect.offsetMax = Vector2.zero;
                
                Image countBg = countObj.AddComponent<Image>();
                countBg.color = new Color(0, 0, 0, 0.7f);
                
                TextMeshProUGUI countText = countObj.AddComponent<TextMeshProUGUI>();
                countText.text = count.ToString();
                countText.fontSize = 12;
                countText.alignment = TextAlignmentOptions.Center;
            }
        }

        private void CreateTitlesContent()
        {
            CreateSectionHeader("🏆 Unlocked Titles (12/35)");
            
            CreateTitleItem("The Conqueror", "Capture 50 territories", true, true);
            CreateTitleItem("Battle Hardened", "Win 100 battles", true, false);
            CreateTitleItem("Resource Tycoon", "Gather 1,000,000 resources", true, false);
            CreateTitleItem("Alliance Champion", "Win 10 alliance wars", true, false);
            CreateTitleItem("Strategic Genius", "Win 50 battles without losses", true, false);
            
            CreateSectionHeader("🔒 Locked Titles");
            
            CreateTitleItem("Legendary Commander", "Reach level 50", false, false);
            CreateTitleItem("Apex Predator", "Hold 20 territories at once", false, false);
            CreateTitleItem("Master Builder", "Build 1000 buildings", false, false);
            
            CreateSectionHeader("🖼️ Avatar Frames");
            
            GameObject framesGrid = new GameObject("FramesGrid");
            framesGrid.transform.SetParent(_contentContainer.transform, false);
            
            GridLayoutGroup grid = framesGrid.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(80, 80);
            grid.spacing = new Vector2(10, 10);
            grid.childAlignment = TextAnchor.UpperLeft;
            
            CreateFrameItem(framesGrid.transform, goldColor, "Golden", true, true);
            CreateFrameItem(framesGrid.transform, new Color(0.2f, 0.4f, 0.9f), "Sapphire", true, false);
            CreateFrameItem(framesGrid.transform, new Color(0.6f, 0.2f, 0.8f), "Amethyst", true, false);
            CreateFrameItem(framesGrid.transform, new Color(0.8f, 0.2f, 0.2f), "Crimson", false, false);
            CreateFrameItem(framesGrid.transform, new Color(0.2f, 0.8f, 0.4f), "Emerald", false, false);
        }

        private void CreateTitleItem(string title, string requirement, bool unlocked, bool equipped)
        {
            GameObject item = new GameObject($"Title_{title}");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            
            Image bg = item.AddComponent<Image>();
            bg.color = equipped ? new Color(0.2f, 0.3f, 0.4f) : new Color(0.1f, 0.1f, 0.15f);
            
            if (unlocked)
            {
                Button btn = item.AddComponent<Button>();
                btn.onClick.AddListener(() => EquipTitle(title));
            }
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            // Status icon
            string statusIcon = unlocked ? (equipped ? "✅" : "✓") : "🔒";
            CreateText(item.transform, statusIcon, 20, TextAlignmentOptions.Center);
            
            // Title info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(item.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(info.transform, unlocked ? $"<b>\"{title}\"</b>" : $"<color=#666666>\"{title}\"</color>", 14, TextAlignmentOptions.Left);
            CreateText(info.transform, $"<color=#888888>{requirement}</color>", 10, TextAlignmentOptions.Left);
            
            // Equip button
            if (unlocked && !equipped)
            {
                CreateText(item.transform, "<color=#88aaff>EQUIP</color>", 12, TextAlignmentOptions.Right);
            }
        }

        private void CreateFrameItem(Transform parent, Color frameColor, string name, bool unlocked, bool equipped)
        {
            GameObject item = new GameObject($"Frame_{name}");
            item.transform.SetParent(parent, false);
            
            Image bg = item.AddComponent<Image>();
            bg.color = unlocked ? new Color(0.15f, 0.15f, 0.2f) : new Color(0.1f, 0.1f, 0.1f, 0.5f);
            
            UnityEngine.UI.Outline outline = item.AddComponent<Outline>();
            outline.effectColor = equipped ? Color.white : frameColor;
            outline.effectDistance = new Vector2(3, 3);
            
            if (unlocked)
            {
                Button btn = item.AddComponent<Button>();
                btn.onClick.AddListener(() => Debug.Log($"[Profile] Selected frame: {name}"));
            }
            
            // Inner frame preview
            GameObject preview = new GameObject("Preview");
            preview.transform.SetParent(item.transform, false);
            
            RectTransform previewRect = preview.AddComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.15f, 0.15f);
            previewRect.anchorMax = new Vector2(0.85f, 0.85f);
            previewRect.offsetMin = Vector2.zero;
            previewRect.offsetMax = Vector2.zero;
            
            Image previewImg = preview.AddComponent<Image>();
            previewImg.color = frameColor;
            
            // Lock overlay
            if (!unlocked)
            {
                GameObject lockObj = new GameObject("Lock");
                lockObj.transform.SetParent(item.transform, false);
                
                RectTransform lockRect = lockObj.AddComponent<RectTransform>();
                lockRect.anchorMin = Vector2.zero;
                lockRect.anchorMax = Vector2.one;
                
                TextMeshProUGUI lockText = lockObj.AddComponent<TextMeshProUGUI>();
                lockText.text = "🔒";
                lockText.fontSize = 24;
                lockText.alignment = TextAlignmentOptions.Center;
            }
        }

        private void CreateHistoryContent()
        {
            CreateSectionHeader("📜 Battle History (Last 20)");
            
            CreateHistoryItem("Today 14:32", "⚔️ Victory", "Attacked Northlands", "+250 XP, +1500 Gold");
            CreateHistoryItem("Today 12:15", "⚔️ Victory", "Defended Iron Mines", "+180 XP");
            CreateHistoryItem("Yesterday 18:45", "❌ Defeat", "Attacked Golden Plains", "-50 Troops");
            CreateHistoryItem("Yesterday 16:20", "⚔️ Victory", "Attacked Stone Quarry", "+200 XP, +2000 Stone");
            CreateHistoryItem("Yesterday 11:00", "⚔️ Victory", "Alliance War - West Front", "+500 XP");
            CreateHistoryItem("2 days ago", "⚔️ Victory", "Captured Crystal Cave", "+350 XP, Crystal Mine");
            CreateHistoryItem("2 days ago", "❌ Defeat", "Defended Forest Camp", "Territory Lost");
            
            CreateSectionHeader("🏆 Achievement History");
            
            CreateHistoryItem("Today", "🏆 Unlocked", "Battle Veteran", "Win 200 battles");
            CreateHistoryItem("Yesterday", "🏆 Unlocked", "Resource Collector II", "Gather 500,000 resources");
            CreateHistoryItem("3 days ago", "🏆 Unlocked", "Squad Leader", "Train 1000 troops");
        }

        private void CreateHistoryItem(string time, string type, string action, string result)
        {
            GameObject item = new GameObject("HistoryItem");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f);
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            // Time
            GameObject timeObj = CreateText(item.transform, $"<color=#888888>{time}</color>", 11, TextAlignmentOptions.Left);
            timeObj.AddComponent<LayoutElement>().preferredWidth = 100;
            
            // Type
            Color typeColor = type.Contains("Victory") ? new Color(0.2f, 0.8f, 0.2f) : 
                            type.Contains("Defeat") ? new Color(0.8f, 0.2f, 0.2f) :
                            goldColor;
            GameObject typeObj = CreateText(item.transform, $"<color=#{ColorUtility.ToHtmlStringRGB(typeColor)}>{type}</color>", 12, TextAlignmentOptions.Center);
            typeObj.AddComponent<LayoutElement>().preferredWidth = 100;
            
            // Action
            GameObject actionObj = CreateText(item.transform, action, 12, TextAlignmentOptions.Left);
            actionObj.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Result
            GameObject resultObj = CreateText(item.transform, $"<color=#aaaaaa>{result}</color>", 11, TextAlignmentOptions.Right);
            resultObj.AddComponent<LayoutElement>().preferredWidth = 180;
        }

        #region UI Helpers

        private void CreateSectionHeader(string text)
        {
            GameObject header = new GameObject("SectionHeader");
            header.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            TextMeshProUGUI headerText = header.AddComponent<TextMeshProUGUI>();
            headerText.text = text;
            headerText.fontSize = 16;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = accentColor;
        }

        private void CreateStatCard(Transform parent, string label, string value)
        {
            GameObject card = new GameObject($"StatCard_{label}");
            card.transform.SetParent(parent, false);
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.15f, 0.2f);
            
            VerticalLayoutGroup vlayout = card.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            CreateText(card.transform, value, 24, TextAlignmentOptions.Center, Color.white);
            CreateText(card.transform, label, 12, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
        }

        private void CreateStatRow(string label, string value)
        {
            GameObject row = new GameObject($"StatRow_{label}");
            row.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 28;
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.padding = new RectOffset(15, 15, 2, 2);
            
            CreateText(row.transform, label, 13, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
            CreateText(row.transform, value, 13, TextAlignmentOptions.Right, Color.white);
        }

        private void CreateActivityItem(string text)
        {
            GameObject item = new GameObject("ActivityItem");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 25;
            
            TextMeshProUGUI activityText = item.AddComponent<TextMeshProUGUI>();
            activityText.text = $"• {text}";
            activityText.fontSize = 12;
            activityText.color = new Color(0.8f, 0.8f, 0.8f);
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
            tmp.enableWordWrapping = false;
            
            return obj;
        }

        #endregion

        private void SelectTab(ProfileTab tab)
        {
            _selectedTab = tab;
            
            foreach (var kvp in _tabButtons)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == tab ? activeTabColor : new Color(0.15f, 0.15f, 0.2f);
            }
            
            RefreshContent();
        }

        private void EquipTitle(string title)
        {
            _profile.CurrentTitle = title;
            OnProfileUpdated?.Invoke(_profile);
            Debug.Log($"[Profile] Equipped title: {title}");
        }

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
            if (_panel.activeSelf)
                Hide();
            else
                Show();
        }

        public PlayerProfile GetProfile()
        {
            return _profile;
        }

        public void AddExperience(int amount)
        {
            _profile.Experience += amount;
            while (_profile.Experience >= _profile.ExperienceToNextLevel)
            {
                _profile.Experience -= _profile.ExperienceToNextLevel;
                _profile.Level++;
                _profile.ExperienceToNextLevel = (int)(_profile.ExperienceToNextLevel * 1.15f);
                Debug.Log($"[Profile] Level up! Now level {_profile.Level}");
            }
            OnProfileUpdated?.Invoke(_profile);
        }

        #endregion
    }

    public enum ProfileTab
    {
        Overview,
        Statistics,
        Inventory,
        Titles,
        History
    }

    public class PlayerProfile
    {
        public string PlayerId;
        public string DisplayName;
        public int Level;
        public int Experience;
        public int ExperienceToNextLevel;
        public TimeSpan TotalPlayTime;
        public DateTime JoinDate;
        
        // Combat stats
        public int TotalBattles;
        public int BattlesWon;
        public int TotalTroopsLost;
        public int TotalTroopsKilled;
        public int TerritoriesCaptured;
        public int TerritoriesLost;
        
        // Economy stats
        public long ResourcesGathered;
        public int BuildingsBuilt;
        
        // Progression
        public int QuestsCompleted;
        public int AchievementsUnlocked;
        
        // Alliance
        public string AllianceName;
        public string AllianceRank;
        public string AllianceId;
        
        // Customization
        public string CurrentTitle;
        public string SelectedFrame;
        public string SelectedAvatar;
    }
}
