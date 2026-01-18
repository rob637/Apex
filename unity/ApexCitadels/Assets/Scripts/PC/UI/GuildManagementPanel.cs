using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.UI;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Guild Management Panel - Comprehensive guild system with roles, events, upgrades, and communication.
    /// Features guild banks, permissions, recruitment, and alliance management.
    /// </summary>
    public class GuildManagementPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.4f, 0.7f, 0.3f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color officerColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color leaderColor = new Color(0.9f, 0.6f, 0.2f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _contentArea;
        private GuildTab _selectedTab = GuildTab.Overview;
        private Dictionary<GuildTab, GameObject> _tabs = new Dictionary<GuildTab, GameObject>();
        
        // Guild data
        private GuildInfo _guild;
        private List<GuildMember> _members = new List<GuildMember>();
        private List<GuildEvent> _events = new List<GuildEvent>();
        private List<GuildUpgrade> _upgrades = new List<GuildUpgrade>();
        private List<GuildApplication> _applications = new List<GuildApplication>();
        private List<GuildLogEntry> _activityLog = new List<GuildLogEntry>();
        private GuildBank _bank;
        
        public static GuildManagementPanel Instance { get; private set; }
        
        public event Action<GuildMember, GuildRole> OnRoleChanged;
        public event Action<GuildMember> OnMemberKicked;
        public event Action<GuildApplication, bool> OnApplicationProcessed;
        public event Action<GuildUpgrade> OnUpgradePurchased;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeGuildData();
        }

        private void Start()
        {
            CreatePanel();
            Hide();
        }

        private void InitializeGuildData()
        {
            // Guild info
            _guild = new GuildInfo
            {
                GuildId = "GUILD001",
                Name = "Dragon Slayers",
                Tag = "[DS]",
                Description = "Elite guild of dragon hunters. Glory awaits those who dare!",
                Level = 15,
                Experience = 85000,
                ExperienceToNextLevel = 100000,
                MemberCount = 47,
                MaxMembers = 50,
                Founded = DateTime.Now.AddDays(-180),
                LeaderName = "DragonKing",
                Emblem = "🐉",
                Motto = "Strength in Unity",
                TotalPower = 2450000,
                WeeklyActivity = 89,
                Rank = 42,
                TerritoryCount = 8,
                IsRecruiting = true,
                MinLevelRequired = 20,
                ApplicationType = GuildApplicationType.ApplyToJoin
            };
            
            // Members
            _members = new List<GuildMember>
            {
                new GuildMember { PlayerId = "P001", Name = "DragonKing", Role = GuildRole.Leader, Level = 65, Power = 125000, LastOnline = DateTime.Now, Contribution = 50000, JoinedAt = DateTime.Now.AddDays(-180), IsOnline = true },
                new GuildMember { PlayerId = "P002", Name = "ShadowBlade", Role = GuildRole.Officer, Level = 58, Power = 98000, LastOnline = DateTime.Now.AddMinutes(-5), Contribution = 35000, JoinedAt = DateTime.Now.AddDays(-150), IsOnline = true },
                new GuildMember { PlayerId = "P003", Name = "IronHeart", Role = GuildRole.Officer, Level = 55, Power = 89000, LastOnline = DateTime.Now.AddHours(-2), Contribution = 32000, JoinedAt = DateTime.Now.AddDays(-120), IsOnline = false },
                new GuildMember { PlayerId = "P004", Name = "StormBringer", Role = GuildRole.Elite, Level = 52, Power = 78000, LastOnline = DateTime.Now.AddMinutes(-30), Contribution = 25000, JoinedAt = DateTime.Now.AddDays(-90), IsOnline = true },
                new GuildMember { PlayerId = "P005", Name = "FrostMage", Role = GuildRole.Elite, Level = 50, Power = 72000, LastOnline = DateTime.Now.AddHours(-1), Contribution = 22000, JoinedAt = DateTime.Now.AddDays(-85), IsOnline = true },
                new GuildMember { PlayerId = "P006", Name = "NightWalker", Role = GuildRole.Member, Level = 45, Power = 58000, LastOnline = DateTime.Now.AddHours(-3), Contribution = 15000, JoinedAt = DateTime.Now.AddDays(-60), IsOnline = false },
                new GuildMember { PlayerId = "P007", Name = "FireDancer", Role = GuildRole.Member, Level = 42, Power = 52000, LastOnline = DateTime.Now, Contribution = 12000, JoinedAt = DateTime.Now.AddDays(-45), IsOnline = true },
                new GuildMember { PlayerId = "P008", Name = "ThunderStrike", Role = GuildRole.Member, Level = 40, Power = 48000, LastOnline = DateTime.Now.AddHours(-5), Contribution = 10000, JoinedAt = DateTime.Now.AddDays(-30), IsOnline = false },
                new GuildMember { PlayerId = "YOU", Name = "You", Role = GuildRole.Officer, Level = 48, Power = 65000, LastOnline = DateTime.Now, Contribution = 28000, JoinedAt = DateTime.Now.AddDays(-100), IsOnline = true }
            };
            
            // Events
            _events = new List<GuildEvent>
            {
                new GuildEvent { EventId = "E001", Name = "Guild War: Iron Legion", Type = GuildEventType.War, StartTime = DateTime.Now.AddHours(2), Duration = 120, Participants = 35, MaxParticipants = 40, Rewards = "10,000 Gold, Legendary Chest", IsJoined = true },
                new GuildEvent { EventId = "E002", Name = "Dragon Raid", Type = GuildEventType.Raid, StartTime = DateTime.Now.AddDays(1), Duration = 60, Participants = 15, MaxParticipants = 20, Rewards = "Dragon Scale x5, Epic Gear", IsJoined = false },
                new GuildEvent { EventId = "E003", Name = "Territory Defense", Type = GuildEventType.Defense, StartTime = DateTime.Now.AddHours(6), Duration = 30, Participants = 28, MaxParticipants = 50, Rewards = "Territory Bonus +10%", IsJoined = true },
                new GuildEvent { EventId = "E004", Name = "Weekly Boss Hunt", Type = GuildEventType.BossHunt, StartTime = DateTime.Now.AddDays(2), Duration = 45, Participants = 8, MaxParticipants = 30, Rewards = "Boss Loot, Guild XP", IsJoined = false }
            };
            
            // Upgrades
            _upgrades = new List<GuildUpgrade>
            {
                new GuildUpgrade { UpgradeId = "U001", Name = "Member Capacity", Description = "Increase max members by 10", CurrentLevel = 5, MaxLevel = 10, Cost = 25000, CostType = ResourceType.Gold, Effect = "+10 Members", IsUnlocked = true },
                new GuildUpgrade { UpgradeId = "U002", Name = "Treasury Expansion", Description = "Increase guild bank capacity", CurrentLevel = 3, MaxLevel = 8, Cost = 15000, CostType = ResourceType.Gold, Effect = "+500 Storage", IsUnlocked = true },
                new GuildUpgrade { UpgradeId = "U003", Name = "War Banner", Description = "Bonus damage in guild wars", CurrentLevel = 4, MaxLevel = 10, Cost = 30000, CostType = ResourceType.Gold, Effect = "+5% War Damage", IsUnlocked = true },
                new GuildUpgrade { UpgradeId = "U004", Name = "Resource Production", Description = "Bonus resource gain for members", CurrentLevel = 2, MaxLevel = 5, Cost = 20000, CostType = ResourceType.Gold, Effect = "+3% Resources", IsUnlocked = true },
                new GuildUpgrade { UpgradeId = "U005", Name = "Experience Boost", Description = "Bonus XP for all members", CurrentLevel = 1, MaxLevel = 5, Cost = 35000, CostType = ResourceType.Gold, Effect = "+2% XP Gain", IsUnlocked = true },
                new GuildUpgrade { UpgradeId = "U006", Name = "Alliance Slots", Description = "More alliance partnerships", CurrentLevel = 0, MaxLevel = 3, Cost = 50000, CostType = ResourceType.Gold, Effect = "+1 Alliance Slot", IsUnlocked = false, RequiredGuildLevel = 20 }
            };
            
            // Applications
            _applications = new List<GuildApplication>
            {
                new GuildApplication { ApplicationId = "A001", PlayerName = "NewHero", Level = 25, Power = 35000, Message = "Looking for active guild!", AppliedAt = DateTime.Now.AddHours(-2) },
                new GuildApplication { ApplicationId = "A002", PlayerName = "WarriorX", Level = 32, Power = 48000, Message = "Experienced player, looking to grow!", AppliedAt = DateTime.Now.AddHours(-5) },
                new GuildApplication { ApplicationId = "A003", PlayerName = "MagicWielder", Level = 28, Power = 40000, Message = "Daily active player", AppliedAt = DateTime.Now.AddDays(-1) }
            };
            
            // Bank
            _bank = new GuildBank
            {
                Gold = 250000,
                MaxGold = 500000,
                Items = new List<GuildBankItem>
                {
                    new GuildBankItem { ItemId = "BI001", Name = "Dragon Scale", Icon = "🐲", Quantity = 15, Rarity = "Epic" },
                    new GuildBankItem { ItemId = "BI002", Name = "War Banner", Icon = "🚩", Quantity = 3, Rarity = "Rare" },
                    new GuildBankItem { ItemId = "BI003", Name = "Health Potions", Icon = "🧪", Quantity = 200, Rarity = "Common" },
                    new GuildBankItem { ItemId = "BI004", Name = "Legendary Chest", Icon = "📦", Quantity = 2, Rarity = "Legendary" },
                    new GuildBankItem { ItemId = "BI005", Name = "Crystal Shards", Icon = "💎", Quantity = 500, Rarity = "Rare" }
                }
            };
            
            // Activity log
            _activityLog = new List<GuildLogEntry>
            {
                new GuildLogEntry { Timestamp = DateTime.Now.AddMinutes(-5), Type = GuildLogType.Chat, Actor = "FireDancer", Message = "Ready for the war!" },
                new GuildLogEntry { Timestamp = DateTime.Now.AddMinutes(-15), Type = GuildLogType.Donation, Actor = "StormBringer", Message = "Donated 5,000 Gold" },
                new GuildLogEntry { Timestamp = DateTime.Now.AddMinutes(-30), Type = GuildLogType.Join, Actor = "NewPlayer", Message = "Joined the guild" },
                new GuildLogEntry { Timestamp = DateTime.Now.AddHours(-1), Type = GuildLogType.Upgrade, Actor = "DragonKing", Message = "Upgraded War Banner to Lv.4" },
                new GuildLogEntry { Timestamp = DateTime.Now.AddHours(-2), Type = GuildLogType.War, Actor = "Guild", Message = "Won war against Shadow Clan!" },
                new GuildLogEntry { Timestamp = DateTime.Now.AddHours(-3), Type = GuildLogType.Promotion, Actor = "DragonKing", Message = "Promoted ShadowBlade to Officer" }
            };
        }

        private void CreatePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("GuildManagementPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.05f);
            rect.anchorMax = new Vector2(0.92f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.07f, 0.05f, 0.98f);
            
            VerticalLayoutGroup vlayout = _panel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Header
            CreateHeader();
            
            // Tabs
            CreateTabNavigation();
            
            // Content
            CreateContentArea();
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 90;
            
            Image bg = header.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.08f);
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            // Guild emblem
            CreateGuildEmblem(header.transform);
            
            // Guild info
            CreateGuildInfo(header.transform);
            
            // Stats
            CreateGuildStats(header.transform);
            
            // Close button
            CreateCloseButton(header.transform);
        }

        private void CreateGuildEmblem(Transform parent)
        {
            GameObject emblem = new GameObject("Emblem");
            emblem.transform.SetParent(parent, false);
            
            LayoutElement le = emblem.AddComponent<LayoutElement>();
            le.preferredWidth = 70;
            le.preferredHeight = 70;
            
            Image bg = emblem.AddComponent<Image>();
            bg.color = new Color(accentColor.r * 0.3f, accentColor.g * 0.3f, accentColor.b * 0.3f);
            
            UnityEngine.UI.Outline outline = emblem.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(2, 2);
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(emblem.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = _guild.Emblem;
            text.fontSize = 40;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateGuildInfo(Transform parent)
        {
            GameObject info = new GameObject("GuildInfo");
            info.transform.SetParent(parent, false);
            
            LayoutElement le = info.AddComponent<LayoutElement>();
            le.preferredWidth = 250;
            
            VerticalLayoutGroup vlayout = info.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleLeft;
            vlayout.spacing = 3;
            
            CreateText(info.transform, $"{_guild.Tag} {_guild.Name}", 20, TextAlignmentOptions.Left, accentColor);
            CreateText(info.transform, $"Level {_guild.Level} Guild", 12, TextAlignmentOptions.Left, goldColor);
            CreateText(info.transform, $"\"{_guild.Motto}\"", 10, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
            CreateText(info.transform, $"Leader: {_guild.LeaderName}", 10, TextAlignmentOptions.Left, leaderColor);
        }

        private void CreateGuildStats(Transform parent)
        {
            GameObject stats = new GameObject("Stats");
            stats.transform.SetParent(parent, false);
            
            LayoutElement le = stats.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            HorizontalLayoutGroup hlayout = stats.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 15;
            
            CreateStatBlock(stats.transform, "👥", $"{_guild.MemberCount}/{_guild.MaxMembers}", "Members");
            CreateStatBlock(stats.transform, "⚔️", $"{(_guild.TotalPower / 1000f):F1}K", "Power");
            CreateStatBlock(stats.transform, "🏰", _guild.TerritoryCount.ToString(), "Territories");
            CreateStatBlock(stats.transform, "📊", $"#{_guild.Rank}", "Rank");
            CreateStatBlock(stats.transform, "🔥", $"{_guild.WeeklyActivity}%", "Activity");
        }

        private void CreateStatBlock(Transform parent, string icon, string value, string label)
        {
            GameObject block = new GameObject($"Stat_{label}");
            block.transform.SetParent(parent, false);
            
            LayoutElement le = block.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            Image bg = block.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.06f);
            
            VerticalLayoutGroup vlayout = block.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(5, 5, 5, 5);
            
            CreateText(block.transform, icon, 18, TextAlignmentOptions.Center);
            CreateText(block.transform, value, 14, TextAlignmentOptions.Center, accentColor);
            CreateText(block.transform, label, 9, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(parent, false);
            
            LayoutElement le = closeBtn.AddComponent<LayoutElement>();
            le.preferredWidth = 40;
            le.preferredHeight = 40;
            
            Image bg = closeBtn.AddComponent<Image>();
            bg.color = new Color(0.5f, 0.2f, 0.2f, 0.8f);
            
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
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "✕";
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateTabNavigation()
        {
            GameObject tabs = new GameObject("Tabs");
            tabs.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = tabs.AddComponent<LayoutElement>();
            le.preferredHeight = 35;
            
            HorizontalLayoutGroup hlayout = tabs.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 5;
            
            CreateTab(tabs.transform, GuildTab.Overview, "📋 Overview");
            CreateTab(tabs.transform, GuildTab.Members, "👥 Members");
            CreateTab(tabs.transform, GuildTab.Events, "⚔️ Events");
            CreateTab(tabs.transform, GuildTab.Bank, "🏦 Bank");
            CreateTab(tabs.transform, GuildTab.Upgrades, "⬆️ Upgrades");
            CreateTab(tabs.transform, GuildTab.Applications, "📩 Applications");
            CreateTab(tabs.transform, GuildTab.Settings, "⚙️ Settings");
        }

        private void CreateTab(Transform parent, GuildTab tab, string label)
        {
            GameObject tabObj = new GameObject($"Tab_{tab}");
            tabObj.transform.SetParent(parent, false);
            
            LayoutElement le = tabObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            Color bgColor = tab == _selectedTab ? accentColor : new Color(0.12f, 0.15f, 0.12f);
            
            Image bg = tabObj.AddComponent<Image>();
            bg.color = bgColor;
            
            Button btn = tabObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectTab(tab));
            
            // Badge for applications
            if (tab == GuildTab.Applications && _applications.Count > 0)
            {
                label += $" ({_applications.Count})";
            }
            
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
            text.fontSize = 11;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            _tabs[tab] = tabObj;
        }

        private void CreateContentArea()
        {
            _contentArea = new GameObject("ContentArea");
            _contentArea.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _contentArea.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            
            Image bg = _contentArea.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.04f);
            
            RefreshContent();
        }

        private void RefreshContent()
        {
            foreach (Transform child in _contentArea.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Remove old layout
            var oldLayout = _contentArea.GetComponent<LayoutGroup>();
            if (oldLayout != null) Destroy(oldLayout);
            
            switch (_selectedTab)
            {
                case GuildTab.Overview:
                    CreateOverviewContent();
                    break;
                case GuildTab.Members:
                    CreateMembersContent();
                    break;
                case GuildTab.Events:
                    CreateEventsContent();
                    break;
                case GuildTab.Bank:
                    CreateBankContent();
                    break;
                case GuildTab.Upgrades:
                    CreateUpgradesContent();
                    break;
                case GuildTab.Applications:
                    CreateApplicationsContent();
                    break;
                case GuildTab.Settings:
                    CreateSettingsContent();
                    break;
            }
        }

        private void CreateOverviewContent()
        {
            HorizontalLayoutGroup hlayout = _contentArea.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.UpperCenter;
            hlayout.childForceExpandHeight = true;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Left column - Guild details
            CreateOverviewLeft();
            
            // Right column - Activity log
            CreateOverviewRight();
        }

        private void CreateOverviewLeft()
        {
            GameObject left = new GameObject("LeftColumn");
            left.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = left.AddComponent<LayoutElement>();
            le.flexibleWidth = 2;
            le.flexibleHeight = 1;
            
            VerticalLayoutGroup vlayout = left.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Description
            CreateSectionHeader(left.transform, "📜 ABOUT");
            CreateText(left.transform, _guild.Description, 12, TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.8f));
            
            // Guild XP progress
            CreateSectionHeader(left.transform, "⭐ GUILD LEVEL");
            CreateGuildLevelProgress(left.transform);
            
            // Quick stats
            CreateSectionHeader(left.transform, "📊 STATISTICS");
            CreateQuickStats(left.transform);
            
            // Upcoming event
            CreateSectionHeader(left.transform, "⚔️ NEXT EVENT");
            if (_events.Count > 0)
            {
                CreateUpcomingEventPreview(left.transform, _events[0]);
            }
        }

        private void CreateGuildLevelProgress(Transform parent)
        {
            GameObject progress = new GameObject("LevelProgress");
            progress.transform.SetParent(parent, false);
            
            LayoutElement le = progress.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            VerticalLayoutGroup vlayout = progress.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            
            CreateText(progress.transform, $"Level {_guild.Level} → Level {_guild.Level + 1}", 14, TextAlignmentOptions.Center, goldColor);
            
            // Progress bar
            CreateProgressBar(progress.transform, (float)_guild.Experience / _guild.ExperienceToNextLevel, accentColor);
            
            CreateText(progress.transform, $"{_guild.Experience:N0} / {_guild.ExperienceToNextLevel:N0} XP", 10, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateProgressBar(Transform parent, float progress, Color color)
        {
            GameObject bar = new GameObject("ProgressBar");
            bar.transform.SetParent(parent, false);
            
            LayoutElement le = bar.AddComponent<LayoutElement>();
            le.preferredHeight = 15;
            
            Image bgImg = bar.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.12f, 0.1f);
            
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

        private void CreateQuickStats(Transform parent)
        {
            GameObject stats = new GameObject("QuickStats");
            stats.transform.SetParent(parent, false);
            
            LayoutElement le = stats.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            GridLayoutGroup grid = stats.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(180, 35);
            grid.spacing = new Vector2(10, 5);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            
            CreateStatRow(stats.transform, "📅 Founded", _guild.Founded.ToString("MMM dd, yyyy"));
            CreateStatRow(stats.transform, "🏆 Guild Rank", $"#{_guild.Rank} in Server");
            CreateStatRow(stats.transform, "⚔️ Wars Won", "23");
            CreateStatRow(stats.transform, "💰 Total Donations", "1.2M Gold");
        }

        private void CreateStatRow(Transform parent, string label, string value)
        {
            GameObject row = new GameObject("StatRow");
            row.transform.SetParent(parent, false);
            
            Image bg = row.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.08f);
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            CreateText(row.transform, label, 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            CreateText(row.transform, value, 11, TextAlignmentOptions.Right, Color.white);
        }

        private void CreateUpcomingEventPreview(Transform parent, GuildEvent evt)
        {
            GameObject preview = new GameObject("EventPreview");
            preview.transform.SetParent(parent, false);
            
            LayoutElement le = preview.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = preview.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.05f);
            
            UnityEngine.UI.Outline outline = preview.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = goldColor;
            outline.effectDistance = new Vector2(1, 1);
            
            HorizontalLayoutGroup hlayout = preview.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            CreateText(preview.transform, GetEventIcon(evt.Type), 28, TextAlignmentOptions.Center);
            
            GameObject info = new GameObject("Info");
            info.transform.SetParent(preview.transform, false);
            info.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(info.transform, evt.Name, 13, TextAlignmentOptions.Left, goldColor);
            CreateText(info.transform, $"Starts in {GetTimeUntil(evt.StartTime)}", 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            if (evt.IsJoined)
            {
                CreateText(preview.transform, "✓ Joined", 11, TextAlignmentOptions.Center, new Color(0.4f, 0.8f, 0.4f));
            }
            else
            {
                CreateActionButton(preview.transform, "Join", () => JoinEvent(evt), accentColor, 80);
            }
        }

        private void CreateOverviewRight()
        {
            GameObject right = new GameObject("RightColumn");
            right.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = right.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            
            Image bg = right.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.06f);
            
            VerticalLayoutGroup vlayout = right.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            CreateSectionHeader(right.transform, "📜 ACTIVITY LOG");
            
            foreach (var entry in _activityLog)
            {
                CreateActivityLogEntry(right.transform, entry);
            }
        }

        private void CreateActivityLogEntry(Transform parent, GuildLogEntry entry)
        {
            GameObject entryObj = new GameObject("LogEntry");
            entryObj.transform.SetParent(parent, false);
            
            LayoutElement le = entryObj.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            HorizontalLayoutGroup hlayout = entryObj.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(5, 5, 3, 3);
            
            CreateText(entryObj.transform, GetLogIcon(entry.Type), 14, TextAlignmentOptions.Center);
            CreateText(entryObj.transform, entry.Actor, 10, TextAlignmentOptions.Left, GetLogColor(entry.Type));
            CreateText(entryObj.transform, entry.Message, 10, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
            
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(entryObj.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            CreateText(entryObj.transform, GetTimeAgo(entry.Timestamp), 9, TextAlignmentOptions.Right, new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateMembersContent()
        {
            VerticalLayoutGroup vlayout = _contentArea.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Header
            CreateMembersHeader();
            
            // Member list
            foreach (var member in _members.OrderByDescending(m => (int)m.Role).ThenByDescending(m => m.Power))
            {
                CreateMemberRow(member);
            }
        }

        private void CreateMembersHeader()
        {
            GameObject header = new GameObject("MembersHeader");
            header.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            Image bg = header.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.1f);
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            CreateHeaderCell(header.transform, "Status", 60);
            CreateHeaderCell(header.transform, "Name", 150);
            CreateHeaderCell(header.transform, "Role", 100);
            CreateHeaderCell(header.transform, "Level", 60);
            CreateHeaderCell(header.transform, "Power", 80);
            CreateHeaderCell(header.transform, "Contribution", 100);
            CreateHeaderCell(header.transform, "Last Online", 100);
            CreateHeaderCell(header.transform, "Actions", 120);
        }

        private void CreateHeaderCell(Transform parent, string text, float width)
        {
            GameObject cell = new GameObject("HeaderCell");
            cell.transform.SetParent(parent, false);
            
            LayoutElement le = cell.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            
            TextMeshProUGUI tmp = cell.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 10;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.6f, 0.6f, 0.6f);
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private void CreateMemberRow(GuildMember member)
        {
            GameObject row = new GameObject($"Member_{member.PlayerId}");
            row.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            Color bgColor = member.Name == "You" ? new Color(accentColor.r * 0.2f, accentColor.g * 0.2f, accentColor.b * 0.2f) 
                                                 : new Color(0.06f, 0.08f, 0.06f);
            
            Image bg = row.AddComponent<Image>();
            bg.color = bgColor;
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            // Online status
            string statusIcon = member.IsOnline ? "🟢" : "⚫";
            CreateMemberCell(row.transform, statusIcon, 60, TextAlignmentOptions.Center);
            
            // Name
            CreateMemberCell(row.transform, member.Name, 150, TextAlignmentOptions.Left, member.Name == "You" ? goldColor : Color.white);
            
            // Role
            CreateMemberCell(row.transform, member.Role.ToString(), 100, TextAlignmentOptions.Center, GetRoleColor(member.Role));
            
            // Level
            CreateMemberCell(row.transform, $"Lv.{member.Level}", 60, TextAlignmentOptions.Center);
            
            // Power
            CreateMemberCell(row.transform, $"{(member.Power / 1000f):F1}K", 80, TextAlignmentOptions.Center, accentColor);
            
            // Contribution
            CreateMemberCell(row.transform, $"{member.Contribution:N0}", 100, TextAlignmentOptions.Center, goldColor);
            
            // Last online
            string lastOnline = member.IsOnline ? "Online" : GetTimeAgo(member.LastOnline);
            CreateMemberCell(row.transform, lastOnline, 100, TextAlignmentOptions.Center, member.IsOnline ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.5f, 0.5f, 0.5f));
            
            // Actions
            CreateMemberActions(row.transform, member);
        }

        private void CreateMemberCell(Transform parent, string text, float width, TextAlignmentOptions align, Color? color = null)
        {
            GameObject cell = new GameObject("Cell");
            cell.transform.SetParent(parent, false);
            
            LayoutElement le = cell.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            
            TextMeshProUGUI tmp = cell.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 11;
            tmp.alignment = align;
            tmp.color = color ?? Color.white;
        }

        private void CreateMemberActions(Transform parent, GuildMember member)
        {
            GameObject actions = new GameObject("Actions");
            actions.transform.SetParent(parent, false);
            
            LayoutElement le = actions.AddComponent<LayoutElement>();
            le.preferredWidth = 120;
            
            HorizontalLayoutGroup hlayout = actions.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 5;
            
            if (member.Name != "You" && member.Role != GuildRole.Leader)
            {
                // Can only manage if we're officer/leader
                var myMember = _members.Find(m => m.Name == "You");
                if (myMember != null && (myMember.Role == GuildRole.Leader || myMember.Role == GuildRole.Officer))
                {
                    CreateSmallButton(actions.transform, "⬆️", () => PromoteMember(member), officerColor, 25);
                    CreateSmallButton(actions.transform, "⬇️", () => DemoteMember(member), new Color(0.5f, 0.5f, 0.3f), 25);
                    CreateSmallButton(actions.transform, "❌", () => KickMember(member), new Color(0.5f, 0.2f, 0.2f), 25);
                }
            }
        }

        private void CreateEventsContent()
        {
            VerticalLayoutGroup vlayout = _contentArea.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            CreateSectionHeader(_contentArea.transform, "⚔️ GUILD EVENTS");
            
            foreach (var evt in _events)
            {
                CreateEventCard(evt);
            }
        }

        private void CreateEventCard(GuildEvent evt)
        {
            GameObject card = new GameObject($"Event_{evt.EventId}");
            card.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Color bgColor = evt.IsJoined ? new Color(0.1f, 0.15f, 0.1f) : new Color(0.08f, 0.1f, 0.08f);
            
            Image bg = card.AddComponent<Image>();
            bg.color = bgColor;
            
            if (evt.IsJoined)
            {
                UnityEngine.UI.Outline outline = card.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = accentColor;
                outline.effectDistance = new Vector2(1, 1);
            }
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Icon
            CreateText(card.transform, GetEventIcon(evt.Type), 32, TextAlignmentOptions.Center);
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 3;
            
            CreateText(info.transform, evt.Name, 14, TextAlignmentOptions.Left, goldColor);
            CreateText(info.transform, $"Type: {evt.Type} | Duration: {evt.Duration} min", 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            CreateText(info.transform, $"Rewards: {evt.Rewards}", 10, TextAlignmentOptions.Left, new Color(0.4f, 0.8f, 0.4f));
            
            // Participants
            GameObject parts = new GameObject("Participants");
            parts.transform.SetParent(card.transform, false);
            
            LayoutElement partsLE = parts.AddComponent<LayoutElement>();
            partsLE.preferredWidth = 80;
            
            VerticalLayoutGroup partsVL = parts.AddComponent<VerticalLayoutGroup>();
            partsVL.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(parts.transform, "👥", 18, TextAlignmentOptions.Center);
            CreateText(parts.transform, $"{evt.Participants}/{evt.MaxParticipants}", 12, TextAlignmentOptions.Center);
            
            // Time/Status
            GameObject time = new GameObject("Time");
            time.transform.SetParent(card.transform, false);
            
            LayoutElement timeLE = time.AddComponent<LayoutElement>();
            timeLE.preferredWidth = 100;
            
            VerticalLayoutGroup timeVL = time.AddComponent<VerticalLayoutGroup>();
            timeVL.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(time.transform, "🕐", 16, TextAlignmentOptions.Center);
            CreateText(time.transform, GetTimeUntil(evt.StartTime), 11, TextAlignmentOptions.Center);
            
            // Action button
            if (evt.IsJoined)
            {
                CreateActionButton(card.transform, "✓ Joined", null, new Color(0.3f, 0.5f, 0.3f), 90);
            }
            else if (evt.Participants < evt.MaxParticipants)
            {
                CreateActionButton(card.transform, "Join", () => JoinEvent(evt), accentColor, 90);
            }
            else
            {
                CreateActionButton(card.transform, "Full", null, new Color(0.4f, 0.4f, 0.4f), 90);
            }
        }

        private void CreateBankContent()
        {
            VerticalLayoutGroup vlayout = _contentArea.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 15;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Gold section
            CreateSectionHeader(_contentArea.transform, "💰 GUILD TREASURY");
            CreateTreasurySection();
            
            // Items section
            CreateSectionHeader(_contentArea.transform, "📦 GUILD ITEMS");
            CreateBankItemsGrid();
            
            // Donation section
            CreateSectionHeader(_contentArea.transform, "🎁 DONATE");
            CreateDonationSection();
        }

        private void CreateTreasurySection()
        {
            GameObject treasury = new GameObject("Treasury");
            treasury.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = treasury.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = treasury.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.1f);
            
            VerticalLayoutGroup vlayout = treasury.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(20, 20, 10, 10);
            
            CreateText(treasury.transform, $"💰 {_bank.Gold:N0} / {_bank.MaxGold:N0} Gold", 18, TextAlignmentOptions.Center, goldColor);
            CreateProgressBar(treasury.transform, (float)_bank.Gold / _bank.MaxGold, goldColor);
        }

        private void CreateBankItemsGrid()
        {
            GameObject grid = new GameObject("ItemsGrid");
            grid.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = grid.AddComponent<LayoutElement>();
            le.preferredHeight = 120;
            
            GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(100, 100);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
            
            foreach (var item in _bank.Items)
            {
                CreateBankItem(grid.transform, item);
            }
        }

        private void CreateBankItem(Transform parent, GuildBankItem item)
        {
            GameObject itemObj = new GameObject($"Item_{item.ItemId}");
            itemObj.transform.SetParent(parent, false);
            
            Color rarityColor = GetItemRarityColor(item.Rarity);
            
            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(rarityColor.r * 0.2f, rarityColor.g * 0.2f, rarityColor.b * 0.2f);
            
            UnityEngine.UI.Outline outline = itemObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = rarityColor;
            outline.effectDistance = new Vector2(1, 1);
            
            VerticalLayoutGroup vlayout = itemObj.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 3;
            vlayout.padding = new RectOffset(5, 5, 10, 10);
            
            CreateText(itemObj.transform, item.Icon, 28, TextAlignmentOptions.Center);
            CreateText(itemObj.transform, item.Name, 9, TextAlignmentOptions.Center, rarityColor);
            CreateText(itemObj.transform, $"x{item.Quantity}", 10, TextAlignmentOptions.Center, Color.white);
        }

        private void CreateDonationSection()
        {
            GameObject donation = new GameObject("Donation");
            donation.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = donation.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            HorizontalLayoutGroup hlayout = donation.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 15;
            
            CreateDonationButton(donation.transform, "💰 1,000 Gold", () => Donate(1000));
            CreateDonationButton(donation.transform, "💰 5,000 Gold", () => Donate(5000));
            CreateDonationButton(donation.transform, "💰 10,000 Gold", () => Donate(10000));
        }

        private void CreateDonationButton(Transform parent, string label, Action onClick)
        {
            GameObject btn = new GameObject("DonateBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 40;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = goldColor * 0.6f;
            
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
            text.fontSize = 12;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateUpgradesContent()
        {
            VerticalLayoutGroup vlayout = _contentArea.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            CreateSectionHeader(_contentArea.transform, "⬆️ GUILD UPGRADES");
            
            foreach (var upgrade in _upgrades)
            {
                CreateUpgradeCard(upgrade);
            }
        }

        private void CreateUpgradeCard(GuildUpgrade upgrade)
        {
            GameObject card = new GameObject($"Upgrade_{upgrade.UpgradeId}");
            card.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            bool canUpgrade = upgrade.IsUnlocked && upgrade.CurrentLevel < upgrade.MaxLevel && _bank.Gold >= upgrade.Cost;
            
            Image bg = card.AddComponent<Image>();
            bg.color = upgrade.IsUnlocked ? new Color(0.08f, 0.1f, 0.08f) : new Color(0.05f, 0.05f, 0.05f);
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 2;
            
            CreateText(info.transform, upgrade.Name, 13, TextAlignmentOptions.Left, upgrade.IsUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f));
            CreateText(info.transform, upgrade.Description, 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            CreateText(info.transform, $"Effect: {upgrade.Effect}", 10, TextAlignmentOptions.Left, accentColor);
            
            // Level
            GameObject level = new GameObject("Level");
            level.transform.SetParent(card.transform, false);
            
            LayoutElement levelLE = level.AddComponent<LayoutElement>();
            levelLE.preferredWidth = 80;
            
            VerticalLayoutGroup levelVL = level.AddComponent<VerticalLayoutGroup>();
            levelVL.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(level.transform, $"Lv.{upgrade.CurrentLevel}", 16, TextAlignmentOptions.Center, goldColor);
            CreateText(level.transform, $"/ {upgrade.MaxLevel}", 10, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            
            // Upgrade button
            if (!upgrade.IsUnlocked)
            {
                CreateActionButton(card.transform, $"🔒 Req. Lv.{upgrade.RequiredGuildLevel}", null, new Color(0.3f, 0.3f, 0.3f), 120);
            }
            else if (upgrade.CurrentLevel >= upgrade.MaxLevel)
            {
                CreateActionButton(card.transform, "MAX", null, goldColor * 0.8f, 100);
            }
            else
            {
                Color btnColor = canUpgrade ? accentColor : new Color(0.3f, 0.3f, 0.3f);
                CreateActionButton(card.transform, $"💰 {upgrade.Cost:N0}", canUpgrade ? () => PurchaseUpgrade(upgrade) : (Action)null, btnColor, 100);
            }
        }

        private void CreateApplicationsContent()
        {
            VerticalLayoutGroup vlayout = _contentArea.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            CreateSectionHeader(_contentArea.transform, "📩 PENDING APPLICATIONS");
            
            if (_applications.Count == 0)
            {
                CreateText(_contentArea.transform, "No pending applications", 14, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
                return;
            }
            
            foreach (var app in _applications)
            {
                CreateApplicationCard(app);
            }
        }

        private void CreateApplicationCard(GuildApplication app)
        {
            GameObject card = new GameObject($"Application_{app.ApplicationId}");
            card.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 70;
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.08f);
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Player info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 3;
            
            CreateText(info.transform, app.PlayerName, 14, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, $"Level {app.Level} | Power: {(app.Power / 1000f):F1}K", 11, TextAlignmentOptions.Left, accentColor);
            CreateText(info.transform, $"\"{app.Message}\"", 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            // Time
            CreateText(card.transform, GetTimeAgo(app.AppliedAt), 10, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            
            // Actions
            CreateActionButton(card.transform, "✓ Accept", () => ProcessApplication(app, true), new Color(0.3f, 0.6f, 0.3f), 80);
            CreateActionButton(card.transform, "✕ Decline", () => ProcessApplication(app, false), new Color(0.6f, 0.3f, 0.3f), 80);
        }

        private void CreateSettingsContent()
        {
            VerticalLayoutGroup vlayout = _contentArea.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 15;
            vlayout.padding = new RectOffset(20, 20, 20, 20);
            
            CreateSectionHeader(_contentArea.transform, "⚙️ GUILD SETTINGS");
            
            // Recruitment settings
            CreateSettingToggle("Open Recruitment", _guild.IsRecruiting, (value) => _guild.IsRecruiting = value);
            CreateSettingDropdown("Application Type", new[] { "Open", "Apply to Join", "Invite Only" }, (int)_guild.ApplicationType);
            CreateSettingInput("Min Level Required", _guild.MinLevelRequired.ToString());
            CreateSettingInput("Guild Motto", _guild.Motto);
            
            // Danger zone
            CreateSectionHeader(_contentArea.transform, "⚠️ DANGER ZONE");
            
            GameObject dangerZone = new GameObject("DangerZone");
            dangerZone.transform.SetParent(_contentArea.transform, false);
            
            HorizontalLayoutGroup hzone = dangerZone.AddComponent<HorizontalLayoutGroup>();
            hzone.childAlignment = TextAnchor.MiddleCenter;
            hzone.spacing = 15;
            
            CreateActionButton(dangerZone.transform, "🚪 Leave Guild", () => LeaveGuild(), new Color(0.6f, 0.3f, 0.3f), 150);
        }

        private void CreateSettingToggle(string label, bool value, Action<bool> onChange)
        {
            GameObject setting = new GameObject($"Setting_{label}");
            setting.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = setting.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = setting.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            CreateText(setting.transform, label, 12, TextAlignmentOptions.Left, Color.white);
            
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(setting.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Toggle button
            GameObject toggle = new GameObject("Toggle");
            toggle.transform.SetParent(setting.transform, false);
            
            LayoutElement togLE = toggle.AddComponent<LayoutElement>();
            togLE.preferredWidth = 60;
            togLE.preferredHeight = 30;
            
            Image togBg = toggle.AddComponent<Image>();
            togBg.color = value ? accentColor : new Color(0.3f, 0.3f, 0.3f);
            
            Button togBtn = toggle.AddComponent<Button>();
            togBtn.onClick.AddListener(() => {
                onChange(!value);
                RefreshContent();
            });
            
            TextMeshProUGUI togText = toggle.AddComponent<TextMeshProUGUI>();
            togText.text = value ? "ON" : "OFF";
            togText.fontSize = 11;
            togText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateSettingDropdown(string label, string[] options, int selected)
        {
            GameObject setting = new GameObject($"Setting_{label}");
            setting.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = setting.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = setting.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            CreateText(setting.transform, label, 12, TextAlignmentOptions.Left, Color.white);
            
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(setting.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            CreateText(setting.transform, options[selected], 12, TextAlignmentOptions.Right, accentColor);
        }

        private void CreateSettingInput(string label, string value)
        {
            GameObject setting = new GameObject($"Setting_{label}");
            setting.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = setting.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = setting.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            CreateText(setting.transform, label, 12, TextAlignmentOptions.Left, Color.white);
            
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(setting.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            CreateText(setting.transform, value, 12, TextAlignmentOptions.Right, new Color(0.8f, 0.8f, 0.8f));
        }

        #region UI Helpers

        private void CreateSectionHeader(Transform parent, string text)
        {
            GameObject header = new GameObject("SectionHeader");
            header.transform.SetParent(parent, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 25;
            
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
            tmp.enableWordWrapping = true;
            
            return obj;
        }

        private void CreateSmallButton(Transform parent, string label, Action onClick, Color color, float size)
        {
            GameObject btn = new GameObject("SmallBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            if (onClick != null)
            {
                Button button = btn.AddComponent<Button>();
                button.onClick.AddListener(() => onClick());
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
            text.text = label;
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateActionButton(Transform parent, string label, Action onClick, Color color, float width)
        {
            GameObject btn = new GameObject("ActionBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 35;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            if (onClick != null)
            {
                Button button = btn.AddComponent<Button>();
                button.onClick.AddListener(() => onClick());
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
            text.text = label;
            text.fontSize = 11;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private Color GetRoleColor(GuildRole role)
        {
            return role switch
            {
                GuildRole.Leader => leaderColor,
                GuildRole.Officer => officerColor,
                GuildRole.Elite => new Color(0.6f, 0.3f, 0.9f),
                _ => Color.white
            };
        }

        private string GetEventIcon(GuildEventType type)
        {
            return type switch
            {
                GuildEventType.War => "⚔️",
                GuildEventType.Raid => "🐉",
                GuildEventType.Defense => "🛡️",
                GuildEventType.BossHunt => "👹",
                _ => "📅"
            };
        }

        private string GetLogIcon(GuildLogType type)
        {
            return type switch
            {
                GuildLogType.Chat => "💬",
                GuildLogType.Join => "➡️",
                GuildLogType.Leave => "⬅️",
                GuildLogType.Kick => "🚫",
                GuildLogType.Promotion => "⬆️",
                GuildLogType.Demotion => "⬇️",
                GuildLogType.Donation => "💰",
                GuildLogType.Upgrade => "⬆️",
                GuildLogType.War => "⚔️",
                _ => "📋"
            };
        }

        private Color GetLogColor(GuildLogType type)
        {
            return type switch
            {
                GuildLogType.Chat => Color.white,
                GuildLogType.Join => new Color(0.4f, 0.8f, 0.4f),
                GuildLogType.Leave => new Color(0.8f, 0.4f, 0.4f),
                GuildLogType.Kick => new Color(0.8f, 0.3f, 0.3f),
                GuildLogType.Promotion => officerColor,
                GuildLogType.Donation => goldColor,
                GuildLogType.War => accentColor,
                _ => Color.white
            };
        }

        private Color GetItemRarityColor(string rarity)
        {
            return rarity switch
            {
                "Legendary" => new Color(0.9f, 0.6f, 0.2f),
                "Epic" => new Color(0.6f, 0.3f, 0.9f),
                "Rare" => new Color(0.3f, 0.6f, 0.9f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
        }

        private string GetTimeAgo(DateTime time)
        {
            TimeSpan diff = DateTime.Now - time;
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return $"{(int)diff.TotalDays}d ago";
        }

        private string GetTimeUntil(DateTime time)
        {
            TimeSpan diff = time - DateTime.Now;
            if (diff.TotalMinutes < 0) return "Started";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h";
            return $"{(int)diff.TotalDays}d";
        }

        #endregion

        #region Actions

        private void SelectTab(GuildTab tab)
        {
            _selectedTab = tab;
            
            foreach (var kvp in _tabs)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == tab ? accentColor : new Color(0.12f, 0.15f, 0.12f);
            }
            
            RefreshContent();
        }

        private void JoinEvent(GuildEvent evt)
        {
            evt.IsJoined = true;
            evt.Participants++;
            RefreshContent();
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"Joined {evt.Name}!");
            }
        }

        private void PromoteMember(GuildMember member)
        {
            if (member.Role < GuildRole.Officer)
            {
                member.Role = (GuildRole)((int)member.Role + 1);
                OnRoleChanged?.Invoke(member, member.Role);
                RefreshContent();
                
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowSuccess($"Promoted {member.Name} to {member.Role}!");
                }
            }
        }

        private void DemoteMember(GuildMember member)
        {
            if (member.Role > GuildRole.Member)
            {
                member.Role = (GuildRole)((int)member.Role - 1);
                OnRoleChanged?.Invoke(member, member.Role);
                RefreshContent();
                
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowInfo($"Demoted {member.Name} to {member.Role}");
                }
            }
        }

        private void KickMember(GuildMember member)
        {
            _members.Remove(member);
            _guild.MemberCount--;
            OnMemberKicked?.Invoke(member);
            RefreshContent();
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowWarning($"Kicked {member.Name} from guild");
            }
        }

        private void Donate(int amount)
        {
            _bank.Gold += amount;
            
            var myMember = _members.Find(m => m.Name == "You");
            if (myMember != null)
            {
                myMember.Contribution += amount;
            }
            
            RefreshContent();
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowResourceGained("Gold", -amount);
                NotificationSystem.Instance.ShowSuccess($"Donated {amount:N0} Gold to guild!");
            }
        }

        private void PurchaseUpgrade(GuildUpgrade upgrade)
        {
            if (_bank.Gold >= upgrade.Cost)
            {
                _bank.Gold -= upgrade.Cost;
                upgrade.CurrentLevel++;
                OnUpgradePurchased?.Invoke(upgrade);
                RefreshContent();
                
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowSuccess($"Upgraded {upgrade.Name} to Lv.{upgrade.CurrentLevel}!");
                }
            }
        }

        private void ProcessApplication(GuildApplication app, bool accept)
        {
            if (accept)
            {
                _members.Add(new GuildMember
                {
                    PlayerId = app.ApplicationId,
                    Name = app.PlayerName,
                    Role = GuildRole.Member,
                    Level = app.Level,
                    Power = app.Power,
                    LastOnline = DateTime.Now,
                    JoinedAt = DateTime.Now,
                    IsOnline = true
                });
                _guild.MemberCount++;
            }
            
            _applications.Remove(app);
            OnApplicationProcessed?.Invoke(app, accept);
            RefreshContent();
            
            if (NotificationSystem.Instance != null)
            {
                if (accept)
                    NotificationSystem.Instance.ShowSuccess($"Accepted {app.PlayerName} into the guild!");
                else
                    NotificationSystem.Instance.ShowInfo($"Declined {app.PlayerName}'s application");
            }
        }

        private void LeaveGuild()
        {
            Debug.Log("[Guild] Leave guild requested - would show confirmation dialog");
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowWarning("Are you sure you want to leave?");
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
            if (_panel.activeSelf)
                Hide();
            else
                Show();
        }

        public GuildInfo GetGuildInfo() => _guild;
        public List<GuildMember> GetMembers() => _members;
        public GuildBank GetBank() => _bank;

        #endregion
    }

    #region Data Classes

    public enum GuildTab
    {
        Overview,
        Members,
        Events,
        Bank,
        Upgrades,
        Applications,
        Settings
    }

    public enum GuildRole
    {
        Member,
        Elite,
        Officer,
        Leader
    }

    public enum GuildEventType
    {
        War,
        Raid,
        Defense,
        BossHunt
    }

    public enum GuildLogType
    {
        Chat,
        Join,
        Leave,
        Kick,
        Promotion,
        Demotion,
        Donation,
        Upgrade,
        War
    }

    public enum GuildApplicationType
    {
        Open,
        ApplyToJoin,
        InviteOnly
    }

    public class GuildInfo
    {
        public string GuildId;
        public string Name;
        public string Tag;
        public string Description;
        public int Level;
        public int Experience;
        public int ExperienceToNextLevel;
        public int MemberCount;
        public int MaxMembers;
        public DateTime Founded;
        public string LeaderName;
        public string Emblem;
        public string Motto;
        public int TotalPower;
        public int WeeklyActivity;
        public int Rank;
        public int TerritoryCount;
        public bool IsRecruiting;
        public int MinLevelRequired;
        public GuildApplicationType ApplicationType;
    }

    public class GuildMember
    {
        public string PlayerId;
        public string Name;
        public GuildRole Role;
        public int Level;
        public int Power;
        public DateTime LastOnline;
        public int Contribution;
        public DateTime JoinedAt;
        public bool IsOnline;
    }

    public class GuildEvent
    {
        public string EventId;
        public string Name;
        public GuildEventType Type;
        public DateTime StartTime;
        public int Duration;
        public int Participants;
        public int MaxParticipants;
        public string Rewards;
        public bool IsJoined;
    }

    public class GuildUpgrade
    {
        public string UpgradeId;
        public string Name;
        public string Description;
        public int CurrentLevel;
        public int MaxLevel;
        public int Cost;
        public ResourceType CostType;
        public string Effect;
        public bool IsUnlocked;
        public int RequiredGuildLevel;
    }

    public class GuildApplication
    {
        public string ApplicationId;
        public string PlayerName;
        public int Level;
        public int Power;
        public string Message;
        public DateTime AppliedAt;
    }

    public class GuildBank
    {
        public int Gold;
        public int MaxGold;
        public List<GuildBankItem> Items = new List<GuildBankItem>();
    }

    public class GuildBankItem
    {
        public string ItemId;
        public string Name;
        public string Icon;
        public int Quantity;
        public string Rarity;
    }

    public class GuildLogEntry
    {
        public DateTime Timestamp;
        public GuildLogType Type;
        public string Actor;
        public string Message;
    }

    #endregion
}
