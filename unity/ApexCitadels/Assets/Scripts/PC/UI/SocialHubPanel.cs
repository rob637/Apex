using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Social Hub Panel - Friends list, guild management, social features, and multiplayer coordination.
    /// The heart of the game's social experience.
    /// </summary>
    public class SocialHubPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color onlineColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color offlineColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _contentContainer;
        private SocialTab _selectedTab = SocialTab.Friends;
        private Dictionary<SocialTab, GameObject> _tabs = new Dictionary<SocialTab, GameObject>();
        
        // Social data
        private List<Friend> _friends = new List<Friend>();
        private List<Friend> _pendingRequests = new List<Friend>();
        private List<GuildMember> _guildMembers = new List<GuildMember>();
        private GuildInfo _currentGuild;
        private List<SocialActivity> _activityFeed = new List<SocialActivity>();
        
        public static SocialHubPanel Instance { get; private set; }
        
        public event Action<Friend> OnFriendAdded;
        public event Action<Friend> OnFriendRemoved;
        public event Action<string> OnGuildJoined;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeSocialData();
        }

        private void Start()
        {
            CreatePanel();
            Hide();
        }

        private void InitializeSocialData()
        {
            // Friends
            _friends.Add(new Friend
            {
                PlayerId = "P001",
                Name = "DragonSlayer",
                Level = 45,
                Status = OnlineStatus.Online,
                LastSeen = DateTime.Now,
                CurrentActivity = "Attacking Northern Keep",
                AllianceName = "Knights of Valor",
                PowerLevel = 25000,
                IsFavorite = true
            });
            
            _friends.Add(new Friend
            {
                PlayerId = "P002",
                Name = "CrystalMage",
                Level = 38,
                Status = OnlineStatus.Online,
                LastSeen = DateTime.Now,
                CurrentActivity = "In Battle",
                AllianceName = "Mystic Order",
                PowerLevel = 18500
            });
            
            _friends.Add(new Friend
            {
                PlayerId = "P003",
                Name = "SteelWarrior",
                Level = 52,
                Status = OnlineStatus.Away,
                LastSeen = DateTime.Now.AddMinutes(-15),
                CurrentActivity = "AFK",
                AllianceName = "Iron Legion",
                PowerLevel = 32000
            });
            
            _friends.Add(new Friend
            {
                PlayerId = "P004",
                Name = "ShadowNinja",
                Level = 41,
                Status = OnlineStatus.Offline,
                LastSeen = DateTime.Now.AddHours(-5),
                AllianceName = "Dark Empire",
                PowerLevel = 21000
            });
            
            _friends.Add(new Friend
            {
                PlayerId = "P005",
                Name = "MerchantKing",
                Level = 33,
                Status = OnlineStatus.Offline,
                LastSeen = DateTime.Now.AddDays(-2),
                PowerLevel = 15000
            });
            
            // Pending requests
            _pendingRequests.Add(new Friend
            {
                PlayerId = "P006",
                Name = "NewPlayer123",
                Level = 12,
                Status = OnlineStatus.Online,
                PowerLevel = 3500
            });
            
            _pendingRequests.Add(new Friend
            {
                PlayerId = "P007",
                Name = "EliteGamer",
                Level = 55,
                Status = OnlineStatus.Offline,
                PowerLevel = 40000,
                AllianceName = "Top 10 Alliance"
            });
            
            // Guild
            _currentGuild = new GuildInfo
            {
                GuildId = "GUILD_001",
                Name = "Knights of Valor",
                Tag = "[KoV]",
                Level = 15,
                Experience = 45000,
                ExperienceToNext = 60000,
                MemberCount = 48,
                MaxMembers = 50,
                LeaderName = "GrandMaster",
                Description = "A noble guild dedicated to honor and valor. We fight for justice!",
                CreatedAt = DateTime.Now.AddDays(-90),
                TotalPower = 850000,
                WorldRank = 127,
                TerritoryCount = 12,
                WeeklyContribution = 125000,
                Perks = new List<string> { "+10% XP", "+5% Gold", "+15% Defense" }
            };
            
            // Guild members (sample)
            _guildMembers.Add(new GuildMember
            {
                PlayerId = "GM001",
                Name = "GrandMaster",
                Rank = GuildRank.Leader,
                Level = 60,
                PowerLevel = 55000,
                WeeklyContribution = 15000,
                LastActive = DateTime.Now.AddMinutes(-30),
                JoinedAt = DateTime.Now.AddDays(-90)
            });
            
            _guildMembers.Add(new GuildMember
            {
                PlayerId = "GM002",
                Name = "SirLancelot",
                Rank = GuildRank.Officer,
                Level = 55,
                PowerLevel = 42000,
                WeeklyContribution = 12000,
                LastActive = DateTime.Now,
                JoinedAt = DateTime.Now.AddDays(-85)
            });
            
            _guildMembers.Add(new GuildMember
            {
                PlayerId = "GM003",
                Name = "DragonSlayer",
                Rank = GuildRank.Officer,
                Level = 45,
                PowerLevel = 25000,
                WeeklyContribution = 8500,
                LastActive = DateTime.Now,
                JoinedAt = DateTime.Now.AddDays(-60)
            });
            
            for (int i = 4; i <= 10; i++)
            {
                _guildMembers.Add(new GuildMember
                {
                    PlayerId = $"GM00{i}",
                    Name = $"Knight{i}",
                    Rank = i <= 6 ? GuildRank.Veteran : GuildRank.Member,
                    Level = 30 + i,
                    PowerLevel = 15000 + i * 1000,
                    WeeklyContribution = 3000 + i * 500,
                    LastActive = DateTime.Now.AddHours(-i),
                    JoinedAt = DateTime.Now.AddDays(-30 + i)
                });
            }
            
            // Activity feed
            _activityFeed.Add(new SocialActivity
            {
                Type = ActivityType.Achievement,
                PlayerName = "DragonSlayer",
                Message = "earned achievement: Dragon Slayer",
                Timestamp = DateTime.Now.AddMinutes(-5)
            });
            
            _activityFeed.Add(new SocialActivity
            {
                Type = ActivityType.Battle,
                PlayerName = "CrystalMage",
                Message = "won a battle against ShadowKing",
                Timestamp = DateTime.Now.AddMinutes(-15)
            });
            
            _activityFeed.Add(new SocialActivity
            {
                Type = ActivityType.LevelUp,
                PlayerName = "SteelWarrior",
                Message = "reached level 52!",
                Timestamp = DateTime.Now.AddMinutes(-30)
            });
            
            _activityFeed.Add(new SocialActivity
            {
                Type = ActivityType.GuildEvent,
                PlayerName = "Knights of Valor",
                Message = "captured Eastern Fortress!",
                Timestamp = DateTime.Now.AddHours(-1)
            });
            
            _activityFeed.Add(new SocialActivity
            {
                Type = ActivityType.Online,
                PlayerName = "DragonSlayer",
                Message = "came online",
                Timestamp = DateTime.Now.AddHours(-2)
            });
        }

        private void CreatePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("SocialHubPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.08f);
            rect.anchorMax = new Vector2(0.9f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.07f, 0.1f, 0.98f);
            
            VerticalLayoutGroup vlayout = _panel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Header
            CreateHeader();
            
            // Tabs
            CreateTabs();
            
            // Content
            CreateContentArea();
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            
            // Title
            GameObject title = new GameObject("Title");
            title.transform.SetParent(header.transform, false);
            
            TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "👥 SOCIAL HUB";
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = accentColor;
            
            // Close button
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(header.transform, false);
            
            LayoutElement closeLe = closeBtn.AddComponent<LayoutElement>();
            closeLe.preferredWidth = 40;
            closeLe.preferredHeight = 40;
            
            Image closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = new Color(0.6f, 0.2f, 0.2f, 0.8f);
            
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
            
            int onlineCount = _friends.FindAll(f => f.Status == OnlineStatus.Online).Count;
            
            CreateTab(tabs.transform, SocialTab.Friends, $"👥 Friends ({onlineCount}/{_friends.Count})");
            CreateTab(tabs.transform, SocialTab.Guild, $"⚔️ Guild ({_currentGuild?.MemberCount ?? 0})");
            CreateTab(tabs.transform, SocialTab.Requests, $"📬 Requests ({_pendingRequests.Count})");
            CreateTab(tabs.transform, SocialTab.Activity, "📰 Activity");
            CreateTab(tabs.transform, SocialTab.Search, "🔍 Search");
        }

        private void CreateTab(Transform parent, SocialTab tab, string label)
        {
            GameObject tabObj = new GameObject($"Tab_{tab}");
            tabObj.transform.SetParent(parent, false);
            
            LayoutElement le = tabObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 35;
            
            Color bgColor = tab == _selectedTab ? accentColor : new Color(0.15f, 0.15f, 0.2f);
            
            Image bg = tabObj.AddComponent<Image>();
            bg.color = bgColor;
            
            Button btn = tabObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectTab(tab));
            
            TextMeshProUGUI text = tabObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 12;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            _tabs[tab] = tabObj;
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
            vlayout.spacing = 8;
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
                case SocialTab.Friends:
                    CreateFriendsContent();
                    break;
                case SocialTab.Guild:
                    CreateGuildContent();
                    break;
                case SocialTab.Requests:
                    CreateRequestsContent();
                    break;
                case SocialTab.Activity:
                    CreateActivityContent();
                    break;
                case SocialTab.Search:
                    CreateSearchContent();
                    break;
            }
        }

        private void CreateFriendsContent()
        {
            // Sort: Online first, then favorites, then by level
            List<Friend> sorted = new List<Friend>(_friends);
            sorted.Sort((a, b) =>
            {
                if (a.Status != b.Status)
                {
                    if (a.Status == OnlineStatus.Online) return -1;
                    if (b.Status == OnlineStatus.Online) return 1;
                    if (a.Status == OnlineStatus.Away) return -1;
                    if (b.Status == OnlineStatus.Away) return 1;
                }
                if (a.IsFavorite != b.IsFavorite) return b.IsFavorite ? 1 : -1;
                return b.Level.CompareTo(a.Level);
            });
            
            // Quick actions bar
            CreateQuickActionsBar();
            
            foreach (var friend in sorted)
            {
                CreateFriendCard(friend);
            }
        }

        private void CreateQuickActionsBar()
        {
            GameObject bar = new GameObject("QuickActions");
            bar.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = bar.AddComponent<LayoutElement>();
            le.preferredHeight = 35;
            
            HorizontalLayoutGroup hlayout = bar.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            CreateSmallButton(bar.transform, "➕ Add Friend", () => OpenAddFriend(), accentColor);
            CreateSmallButton(bar.transform, "🔗 Share Code", () => ShareFriendCode(), new Color(0.4f, 0.6f, 0.4f));
            
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(bar.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            CreateText(bar.transform, $"Online: {_friends.FindAll(f => f.Status == OnlineStatus.Online).Count}", 12, TextAlignmentOptions.Right, onlineColor);
        }

        private void CreateFriendCard(Friend friend)
        {
            GameObject card = new GameObject($"Friend_{friend.PlayerId}");
            card.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 65;
            
            Color statusColor = GetStatusColor(friend.Status);
            
            Image bg = card.AddComponent<Image>();
            bg.color = friend.Status == OnlineStatus.Online ? new Color(0.1f, 0.15f, 0.1f) : new Color(0.08f, 0.08f, 0.1f);
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(12, 12, 8, 8);
            
            // Status indicator
            GameObject statusDot = new GameObject("Status");
            statusDot.transform.SetParent(card.transform, false);
            
            LayoutElement dotLE = statusDot.AddComponent<LayoutElement>();
            dotLE.preferredWidth = 12;
            dotLE.preferredHeight = 12;
            
            Image dotImg = statusDot.AddComponent<Image>();
            dotImg.color = statusColor;
            
            // Avatar/Level badge
            GameObject avatar = new GameObject("Avatar");
            avatar.transform.SetParent(card.transform, false);
            
            LayoutElement avatarLE = avatar.AddComponent<LayoutElement>();
            avatarLE.preferredWidth = 45;
            avatarLE.preferredHeight = 45;
            
            Image avatarBg = avatar.AddComponent<Image>();
            avatarBg.color = new Color(0.2f, 0.2f, 0.25f);
            
            TextMeshProUGUI levelText = avatar.AddComponent<TextMeshProUGUI>();
            levelText.text = friend.Level.ToString();
            levelText.fontSize = 18;
            levelText.fontStyle = FontStyles.Bold;
            levelText.alignment = TextAlignmentOptions.Center;
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 2;
            
            string favoriteIcon = friend.IsFavorite ? "⭐ " : "";
            CreateText(info.transform, $"{favoriteIcon}<b>{friend.Name}</b>", 14, TextAlignmentOptions.Left, Color.white);
            
            if (!string.IsNullOrEmpty(friend.AllianceName))
            {
                CreateText(info.transform, $"⚔️ {friend.AllianceName}", 10, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.7f));
            }
            
            if (friend.Status == OnlineStatus.Online && !string.IsNullOrEmpty(friend.CurrentActivity))
            {
                CreateText(info.transform, friend.CurrentActivity, 10, TextAlignmentOptions.Left, onlineColor);
            }
            else if (friend.Status != OnlineStatus.Online)
            {
                string lastSeenStr = FormatLastSeen(friend.LastSeen);
                CreateText(info.transform, $"Last seen: {lastSeenStr}", 10, TextAlignmentOptions.Left, offlineColor);
            }
            
            // Power
            CreateText(card.transform, $"⚡{friend.PowerLevel:N0}", 11, TextAlignmentOptions.Right, new Color(0.8f, 0.7f, 0.4f));
            
            // Actions
            GameObject actions = new GameObject("Actions");
            actions.transform.SetParent(card.transform, false);
            
            VerticalLayoutGroup actionsVL = actions.AddComponent<VerticalLayoutGroup>();
            actionsVL.childAlignment = TextAnchor.MiddleCenter;
            actionsVL.spacing = 3;
            
            if (friend.Status == OnlineStatus.Online)
            {
                CreateSmallButton(actions.transform, "💬", () => OpenChat(friend), accentColor);
                CreateSmallButton(actions.transform, "⚔️", () => InviteToBattle(friend), new Color(0.7f, 0.4f, 0.3f));
            }
            else
            {
                CreateSmallButton(actions.transform, "📨", () => SendOfflineMessage(friend), new Color(0.4f, 0.4f, 0.5f));
            }
        }

        private void CreateGuildContent()
        {
            if (_currentGuild == null)
            {
                CreateNoGuildContent();
                return;
            }
            
            // Guild header/info
            CreateGuildHeader();
            
            // Guild stats
            CreateGuildStats();
            
            // Members list
            CreateSectionLabel("👥 MEMBERS");
            
            // Sort by rank then power
            List<GuildMember> sorted = new List<GuildMember>(_guildMembers);
            sorted.Sort((a, b) =>
            {
                if (a.Rank != b.Rank) return a.Rank.CompareTo(b.Rank);
                return b.PowerLevel.CompareTo(a.PowerLevel);
            });
            
            foreach (var member in sorted.Take(8))
            {
                CreateGuildMemberCard(member);
            }
            
            if (_guildMembers.Count > 8)
            {
                CreateText(_contentContainer.transform, $"+ {_guildMembers.Count - 8} more members...", 12, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            }
        }

        private void CreateNoGuildContent()
        {
            CreateText(_contentContainer.transform, "You are not in a guild", 16, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
            CreateText(_contentContainer.transform, "Join a guild to unlock social features, bonuses, and guild wars!", 12, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            
            GameObject actions = new GameObject("Actions");
            actions.transform.SetParent(_contentContainer.transform, false);
            
            HorizontalLayoutGroup hlayout = actions.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            
            CreateActionButton(actions.transform, "🔍 Browse Guilds", BrowseGuilds, accentColor);
            CreateActionButton(actions.transform, "➕ Create Guild", CreateGuild, goldColor);
        }

        private void CreateGuildHeader()
        {
            GameObject header = new GameObject("GuildHeader");
            header.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Image bg = header.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.18f);
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Guild emblem
            GameObject emblem = new GameObject("Emblem");
            emblem.transform.SetParent(header.transform, false);
            
            LayoutElement emblemLE = emblem.AddComponent<LayoutElement>();
            emblemLE.preferredWidth = 60;
            emblemLE.preferredHeight = 60;
            
            Image emblemBg = emblem.AddComponent<Image>();
            emblemBg.color = new Color(0.2f, 0.25f, 0.35f);
            
            TextMeshProUGUI emblemText = emblem.AddComponent<TextMeshProUGUI>();
            emblemText.text = "⚔️";
            emblemText.fontSize = 32;
            emblemText.alignment = TextAlignmentOptions.Center;
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(header.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 3;
            
            CreateText(info.transform, $"<b>{_currentGuild.Tag} {_currentGuild.Name}</b>", 18, TextAlignmentOptions.Left, goldColor);
            CreateText(info.transform, $"Level {_currentGuild.Level} • Rank #{_currentGuild.WorldRank}", 12, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, _currentGuild.Description, 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            // Quick stats
            GameObject stats = new GameObject("Stats");
            stats.transform.SetParent(header.transform, false);
            
            VerticalLayoutGroup statsVL = stats.AddComponent<VerticalLayoutGroup>();
            statsVL.childAlignment = TextAnchor.MiddleRight;
            
            CreateText(stats.transform, $"👥 {_currentGuild.MemberCount}/{_currentGuild.MaxMembers}", 12, TextAlignmentOptions.Right, Color.white);
            CreateText(stats.transform, $"⚡ {_currentGuild.TotalPower:N0}", 12, TextAlignmentOptions.Right, new Color(0.8f, 0.7f, 0.4f));
            CreateText(stats.transform, $"🏰 {_currentGuild.TerritoryCount} territories", 11, TextAlignmentOptions.Right, accentColor);
        }

        private void CreateGuildStats()
        {
            GameObject stats = new GameObject("GuildStats");
            stats.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = stats.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            HorizontalLayoutGroup hlayout = stats.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 10;
            
            CreateGuildStatItem(stats.transform, "📈 XP", $"{_currentGuild.Experience:N0}/{_currentGuild.ExperienceToNext:N0}");
            CreateGuildStatItem(stats.transform, "🎁 Perks", string.Join(", ", _currentGuild.Perks));
            CreateGuildStatItem(stats.transform, "📊 Weekly", $"{_currentGuild.WeeklyContribution:N0}");
        }

        private void CreateGuildStatItem(Transform parent, string label, string value)
        {
            GameObject item = new GameObject("Stat");
            item.transform.SetParent(parent, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f);
            
            VerticalLayoutGroup vlayout = item.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(5, 5, 5, 5);
            
            CreateText(item.transform, label, 10, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            CreateText(item.transform, value, 11, TextAlignmentOptions.Center, Color.white);
        }

        private void CreateGuildMemberCard(GuildMember member)
        {
            GameObject card = new GameObject($"Member_{member.PlayerId}");
            card.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.07f, 0.1f);
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            // Rank badge
            string rankIcon = member.Rank switch
            {
                GuildRank.Leader => "👑",
                GuildRank.Officer => "⭐",
                GuildRank.Veteran => "🎖️",
                _ => "👤"
            };
            CreateText(card.transform, rankIcon, 18, TextAlignmentOptions.Center);
            
            // Name
            GameObject nameObj = CreateText(card.transform, $"<b>{member.Name}</b> Lv.{member.Level}", 12, TextAlignmentOptions.Left, Color.white);
            nameObj.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Power
            CreateText(card.transform, $"⚡{member.PowerLevel:N0}", 10, TextAlignmentOptions.Right, new Color(0.8f, 0.7f, 0.4f));
            
            // Contribution
            CreateText(card.transform, $"📊{member.WeeklyContribution:N0}", 10, TextAlignmentOptions.Right, accentColor);
            
            // Last active
            bool isOnline = (DateTime.Now - member.LastActive).TotalMinutes < 5;
            CreateText(card.transform, isOnline ? "🟢" : "⚫", 12, TextAlignmentOptions.Center);
        }

        private void CreateRequestsContent()
        {
            if (_pendingRequests.Count == 0)
            {
                CreateText(_contentContainer.transform, "No pending friend requests", 14, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
                return;
            }
            
            CreateText(_contentContainer.transform, $"{_pendingRequests.Count} pending requests", 14, TextAlignmentOptions.Center, accentColor);
            
            foreach (var request in _pendingRequests)
            {
                CreateRequestCard(request);
            }
        }

        private void CreateRequestCard(Friend request)
        {
            GameObject card = new GameObject($"Request_{request.PlayerId}");
            card.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.15f);
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Level
            GameObject level = new GameObject("Level");
            level.transform.SetParent(card.transform, false);
            
            LayoutElement levelLE = level.AddComponent<LayoutElement>();
            levelLE.preferredWidth = 40;
            
            TextMeshProUGUI levelText = level.AddComponent<TextMeshProUGUI>();
            levelText.text = $"Lv.{request.Level}";
            levelText.fontSize = 14;
            levelText.alignment = TextAlignmentOptions.Center;
            levelText.color = accentColor;
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(info.transform, $"<b>{request.Name}</b>", 14, TextAlignmentOptions.Left, Color.white);
            
            string details = $"Power: {request.PowerLevel:N0}";
            if (!string.IsNullOrEmpty(request.AllianceName))
                details += $" • {request.AllianceName}";
            CreateText(info.transform, details, 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            // Actions
            CreateSmallButton(card.transform, "✓ Accept", () => AcceptRequest(request), new Color(0.3f, 0.6f, 0.3f));
            CreateSmallButton(card.transform, "✗ Decline", () => DeclineRequest(request), new Color(0.6f, 0.3f, 0.3f));
        }

        private void CreateActivityContent()
        {
            CreateText(_contentContainer.transform, "Recent Activity", 16, TextAlignmentOptions.Center, accentColor);
            
            foreach (var activity in _activityFeed)
            {
                CreateActivityItem(activity);
            }
        }

        private void CreateActivityItem(SocialActivity activity)
        {
            GameObject item = new GameObject("Activity");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            // Icon
            string icon = activity.Type switch
            {
                ActivityType.Achievement => "🏆",
                ActivityType.Battle => "⚔️",
                ActivityType.LevelUp => "⬆️",
                ActivityType.GuildEvent => "🏰",
                ActivityType.Online => "🟢",
                _ => "📌"
            };
            CreateText(item.transform, icon, 16, TextAlignmentOptions.Center);
            
            // Message
            GameObject msgObj = CreateText(item.transform, $"<b>{activity.PlayerName}</b> {activity.Message}", 12, TextAlignmentOptions.Left, Color.white);
            msgObj.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Time
            string timeStr = FormatLastSeen(activity.Timestamp);
            CreateText(item.transform, timeStr, 10, TextAlignmentOptions.Right, new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateSearchContent()
        {
            // Search bar
            GameObject searchBar = new GameObject("SearchBar");
            searchBar.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = searchBar.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            Image bg = searchBar.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f);
            
            HorizontalLayoutGroup hlayout = searchBar.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            CreateText(searchBar.transform, "🔍", 18, TextAlignmentOptions.Center);
            CreateText(searchBar.transform, "Enter player name or friend code...", 13, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            
            // Info
            CreateText(_contentContainer.transform, "Search for players to add as friends", 12, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            CreateText(_contentContainer.transform, "Or use a friend code to add directly", 12, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            
            // Friend code display
            CreateSectionLabel("Your Friend Code");
            
            GameObject codeDisplay = new GameObject("CodeDisplay");
            codeDisplay.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement codeLe = codeDisplay.AddComponent<LayoutElement>();
            codeLe.preferredHeight = 50;
            
            Image codeBg = codeDisplay.AddComponent<Image>();
            codeBg.color = new Color(0.1f, 0.12f, 0.18f);
            
            HorizontalLayoutGroup codeHL = codeDisplay.AddComponent<HorizontalLayoutGroup>();
            codeHL.childAlignment = TextAnchor.MiddleCenter;
            codeHL.spacing = 15;
            
            CreateText(codeDisplay.transform, "APEX-1234-ABCD-5678", 18, TextAlignmentOptions.Center, goldColor);
            CreateSmallButton(codeDisplay.transform, "📋 Copy", () => CopyFriendCode(), accentColor);
        }

        #region UI Helpers

        private void CreateSectionLabel(string text)
        {
            GameObject label = new GameObject("Label");
            label.transform.SetParent(_contentContainer.transform, false);
            
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
            tmp.enableWordWrapping = false;
            
            return obj;
        }

        private void CreateSmallButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btn = new GameObject($"Btn_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            le.preferredHeight = 28;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 11;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateActionButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btn = new GameObject($"ActionBtn_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 150;
            le.preferredHeight = 45;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private Color GetStatusColor(OnlineStatus status)
        {
            return status switch
            {
                OnlineStatus.Online => onlineColor,
                OnlineStatus.Away => new Color(0.9f, 0.7f, 0.2f),
                OnlineStatus.Offline => offlineColor,
                _ => Color.white
            };
        }

        private string FormatLastSeen(DateTime time)
        {
            TimeSpan ago = DateTime.Now - time;
            if (ago.TotalMinutes < 5) return "Just now";
            if (ago.TotalMinutes < 60) return $"{(int)ago.TotalMinutes}m ago";
            if (ago.TotalHours < 24) return $"{(int)ago.TotalHours}h ago";
            return $"{(int)ago.TotalDays}d ago";
        }

        #endregion

        #region Social Actions

        private void OpenAddFriend()
        {
            Debug.Log("[Social] Opening add friend dialog...");
        }

        private void ShareFriendCode()
        {
            Debug.Log("[Social] Sharing friend code...");
            GUIUtility.systemCopyBuffer = "APEX-1234-ABCD-5678";
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess("Friend code copied to clipboard!");
            }
        }

        private void CopyFriendCode()
        {
            ShareFriendCode();
        }

        private void OpenChat(Friend friend)
        {
            Debug.Log($"[Social] Opening chat with {friend.Name}...");
        }

        private void InviteToBattle(Friend friend)
        {
            Debug.Log($"[Social] Inviting {friend.Name} to battle...");
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Battle invite sent to {friend.Name}!");
            }
        }

        private void SendOfflineMessage(Friend friend)
        {
            Debug.Log($"[Social] Sending offline message to {friend.Name}...");
        }

        private void AcceptRequest(Friend request)
        {
            _pendingRequests.Remove(request);
            request.Status = OnlineStatus.Offline;
            _friends.Add(request);
            
            OnFriendAdded?.Invoke(request);
            RefreshContent();
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"{request.Name} is now your friend!");
            }
        }

        private void DeclineRequest(Friend request)
        {
            _pendingRequests.Remove(request);
            RefreshContent();
        }

        private void BrowseGuilds()
        {
            Debug.Log("[Social] Opening guild browser...");
        }

        private void CreateGuild()
        {
            Debug.Log("[Social] Opening guild creation...");
        }

        #endregion

        private void SelectTab(SocialTab tab)
        {
            _selectedTab = tab;
            
            foreach (var kvp in _tabs)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == tab ? accentColor : new Color(0.15f, 0.15f, 0.2f);
            }
            
            RefreshContent();
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

        public int GetOnlineFriendCount()
        {
            return _friends.FindAll(f => f.Status == OnlineStatus.Online).Count;
        }

        public int GetPendingRequestCount()
        {
            return _pendingRequests.Count;
        }

        public GuildInfo GetCurrentGuild() => _currentGuild;

        #endregion
    }

    #region Data Classes

    public enum SocialTab
    {
        Friends,
        Guild,
        Requests,
        Activity,
        Search
    }

    public enum OnlineStatus
    {
        Online,
        Away,
        Offline
    }

    public enum GuildRank
    {
        Leader,
        Officer,
        Veteran,
        Member
    }

    public enum ActivityType
    {
        Achievement,
        Battle,
        LevelUp,
        GuildEvent,
        Online
    }

    public class Friend
    {
        public string PlayerId;
        public string Name;
        public int Level;
        public OnlineStatus Status;
        public DateTime LastSeen;
        public string CurrentActivity;
        public string AllianceName;
        public int PowerLevel;
        public bool IsFavorite;
    }

    public class GuildInfo
    {
        public string GuildId;
        public string Name;
        public string Tag;
        public int Level;
        public int Experience;
        public int ExperienceToNext;
        public int MemberCount;
        public int MaxMembers;
        public string LeaderName;
        public string Description;
        public DateTime CreatedAt;
        public int TotalPower;
        public int WorldRank;
        public int TerritoryCount;
        public int WeeklyContribution;
        public List<string> Perks;
    }

    public class GuildMember
    {
        public string PlayerId;
        public string Name;
        public GuildRank Rank;
        public int Level;
        public int PowerLevel;
        public int WeeklyContribution;
        public DateTime LastActive;
        public DateTime JoinedAt;
    }

    public class SocialActivity
    {
        public ActivityType Type;
        public string PlayerName;
        public string Message;
        public DateTime Timestamp;
    }

    #endregion
}
