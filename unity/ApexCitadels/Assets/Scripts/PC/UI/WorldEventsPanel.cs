using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// World Events Panel - Global events, limited-time challenges, and seasonal content.
    /// Keeps the game fresh and gives players reasons to return daily.
    /// </summary>
    public class WorldEventsPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color urgentColor = new Color(0.9f, 0.3f, 0.3f);
        [SerializeField] private Color rareColor = new Color(0.6f, 0.3f, 0.9f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _contentContainer;
        private EventTab _selectedTab = EventTab.Active;
        private Dictionary<EventTab, GameObject> _tabs = new Dictionary<EventTab, GameObject>();
        
        // Event data
        private List<WorldEvent> _activeEvents = new List<WorldEvent>();
        private List<WorldEvent> _upcomingEvents = new List<WorldEvent>();
        private List<WorldEvent> _completedEvents = new List<WorldEvent>();
        
        public static WorldEventsPanel Instance { get; private set; }
        
        public event Action<WorldEvent> OnEventJoined;
        public event Action<WorldEvent, EventReward> OnRewardClaimed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeEvents();
        }

        private void Start()
        {
            CreatePanel();
            Hide();
        }

        private void InitializeEvents()
        {
            // Active events
            _activeEvents.Add(new WorldEvent
            {
                EventId = "EVENT_001",
                Name = "[*] Dragon's Fury",
                Description = "A legendary dragon has awakened! Rally your troops and defeat the beast before it destroys everything.",
                Type = WorldEventType.WorldBoss,
                StartTime = DateTime.Now.AddHours(-12),
                EndTime = DateTime.Now.AddHours(36),
                Difficulty = EventDifficulty.Epic,
                CurrentParticipants = 1247,
                MaxParticipants = 0,
                PlayerProgress = 45,
                MaxProgress = 100,
                Rewards = new List<EventReward>
                {
                    new EventReward { Name = "Dragon Scale Armor", Icon = "[D]", Rarity = "Legendary", Claimed = false },
                    new EventReward { Name = "10,000 Gold", Icon = "[$]", Rarity = "Common", Claimed = true },
                    new EventReward { Name = "Dragon Egg", Icon = "🥚", Rarity = "Epic", Claimed = false }
                }
            });
            
            _activeEvents.Add(new WorldEvent
            {
                EventId = "EVENT_002",
                Name = "[!] Conquest Week",
                Description = "Double rewards for territory captures! Expand your empire during this limited-time event.",
                Type = WorldEventType.ResourceBoost,
                StartTime = DateTime.Now.AddDays(-3),
                EndTime = DateTime.Now.AddDays(4),
                Difficulty = EventDifficulty.Normal,
                CurrentParticipants = 5623,
                PlayerProgress = 7,
                MaxProgress = 20,
                Rewards = new List<EventReward>
                {
                    new EventReward { Name = "Conqueror Banner", Icon = "🚩", Rarity = "Rare", Claimed = false },
                    new EventReward { Name = "50 Apex Coins", Icon = "[$]", Rarity = "Rare", Claimed = false }
                }
            });
            
            _activeEvents.Add(new WorldEvent
            {
                EventId = "EVENT_003",
                Name = "[T] Alliance Tournament",
                Description = "Compete against other alliances in strategic battles. Earn glory for your alliance!",
                Type = WorldEventType.Tournament,
                StartTime = DateTime.Now.AddDays(-1),
                EndTime = DateTime.Now.AddDays(6),
                Difficulty = EventDifficulty.Hard,
                CurrentParticipants = 48,
                MaxParticipants = 64,
                PlayerProgress = 3,
                MaxProgress = 10,
                CurrentRank = 12,
                Rewards = new List<EventReward>
                {
                    new EventReward { Name = "Tournament Trophy", Icon = "[T]", Rarity = "Epic", Claimed = false },
                    new EventReward { Name = "Champion's Chest", Icon = "[B]", Rarity = "Legendary", Claimed = false }
                }
            });
            
            // Upcoming events
            _upcomingEvents.Add(new WorldEvent
            {
                EventId = "EVENT_004",
                Name = "[C] Winter Siege",
                Description = "The northern armies march south! Defend your territories from the winter invasion.",
                Type = WorldEventType.Seasonal,
                StartTime = DateTime.Now.AddDays(2),
                EndTime = DateTime.Now.AddDays(16),
                Difficulty = EventDifficulty.Hard
            });
            
            _upcomingEvents.Add(new WorldEvent
            {
                EventId = "EVENT_005",
                Name = "[G] Crystal Rush",
                Description = "Crystal deposits have been discovered! Race to gather rare crystals before they disappear.",
                Type = WorldEventType.ResourceBoost,
                StartTime = DateTime.Now.AddDays(5),
                EndTime = DateTime.Now.AddDays(7),
                Difficulty = EventDifficulty.Normal
            });
            
            // Completed events
            _completedEvents.Add(new WorldEvent
            {
                EventId = "EVENT_000",
                Name = "🎃 Harvest Festival",
                Description = "Collected pumpkins and defended the harvest from raiders.",
                Type = WorldEventType.Seasonal,
                StartTime = DateTime.Now.AddDays(-14),
                EndTime = DateTime.Now.AddDays(-7),
                PlayerProgress = 100,
                MaxProgress = 100,
                CurrentRank = 45,
                TotalParticipants = 8500
            });
        }

        private void CreatePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("WorldEventsPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.12f, 0.08f);
            rect.anchorMax = new Vector2(0.88f, 0.92f);
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
            
            // Header
            CreateHeader();
            
            // Featured event banner
            if (_activeEvents.Count > 0)
            {
                CreateFeaturedBanner(_activeEvents[0]);
            }
            
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
            titleText.text = "[W] WORLD EVENTS";
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
            x.fontSize = 24;
            x.alignment = TextAlignmentOptions.Center;
        }

        private void CreateFeaturedBanner(WorldEvent featuredEvent)
        {
            GameObject banner = new GameObject("FeaturedBanner");
            banner.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = banner.AddComponent<LayoutElement>();
            le.preferredHeight = 120;
            
            // Gradient background based on event type
            Color bannerColor = featuredEvent.Type switch
            {
                WorldEventType.WorldBoss => new Color(0.4f, 0.15f, 0.1f),
                WorldEventType.Tournament => new Color(0.15f, 0.25f, 0.4f),
                WorldEventType.Seasonal => new Color(0.15f, 0.3f, 0.25f),
                _ => new Color(0.2f, 0.2f, 0.3f)
            };
            
            Image bg = banner.AddComponent<Image>();
            bg.color = bannerColor;
            
            HorizontalLayoutGroup hlayout = banner.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(25, 25, 15, 15);
            
            // Event icon/badge
            GameObject iconSection = new GameObject("Icon");
            iconSection.transform.SetParent(banner.transform, false);
            
            LayoutElement iconLE = iconSection.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 80;
            iconLE.preferredHeight = 80;
            
            Image iconBg = iconSection.AddComponent<Image>();
            iconBg.color = new Color(0, 0, 0, 0.3f);
            
            // Text as child
            GameObject iconTextObj = new GameObject("Text");
            iconTextObj.transform.SetParent(iconSection.transform, false);
            RectTransform iconTextRect = iconTextObj.AddComponent<RectTransform>();
            iconTextRect.anchorMin = Vector2.zero;
            iconTextRect.anchorMax = Vector2.one;
            iconTextRect.offsetMin = Vector2.zero;
            iconTextRect.offsetMax = Vector2.zero;
            TextMeshProUGUI iconText = iconTextObj.AddComponent<TextMeshProUGUI>();
            iconText.text = "[*]";
            iconText.fontSize = 48;
            iconText.alignment = TextAlignmentOptions.Center;
            
            // Event info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(banner.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 5;
            
            // Title
            CreateText(info.transform, $"<b>FEATURED: {featuredEvent.Name}</b>", 18, TextAlignmentOptions.Left, goldColor);
            
            // Description
            CreateText(info.transform, featuredEvent.Description, 12, TextAlignmentOptions.Left, new Color(0.85f, 0.85f, 0.85f));
            
            // Time remaining
            TimeSpan remaining = featuredEvent.EndTime - DateTime.Now;
            string timeStr = remaining.TotalDays >= 1 
                ? $"{(int)remaining.TotalDays}d {remaining.Hours}h remaining" 
                : $"{remaining.Hours}h {remaining.Minutes}m remaining";
            CreateText(info.transform, $"[T] {timeStr}", 12, TextAlignmentOptions.Left, urgentColor);
            
            // Progress
            CreateProgressDisplay(info.transform, featuredEvent);
            
            // Join button
            CreateJoinButton(banner.transform, featuredEvent);
        }

        private void CreateProgressDisplay(Transform parent, WorldEvent evt)
        {
            GameObject progress = new GameObject("Progress");
            progress.transform.SetParent(parent, false);
            
            LayoutElement le = progress.AddComponent<LayoutElement>();
            le.preferredHeight = 20;
            
            HorizontalLayoutGroup hlayout = progress.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 10;
            
            // Progress bar
            GameObject bar = new GameObject("Bar");
            bar.transform.SetParent(progress.transform, false);
            
            LayoutElement barLE = bar.AddComponent<LayoutElement>();
            barLE.preferredWidth = 200;
            barLE.preferredHeight = 15;
            
            Image barBg = bar.AddComponent<Image>();
            barBg.color = new Color(0.1f, 0.1f, 0.1f);
            
            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            float progress2 = evt.MaxProgress > 0 ? (float)evt.PlayerProgress / evt.MaxProgress : 0;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(progress2, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = accentColor;
            
            // Text
            CreateText(progress.transform, $"{evt.PlayerProgress}/{evt.MaxProgress}", 12, TextAlignmentOptions.Left, Color.white);
            
            // Participants
            if (evt.CurrentParticipants > 0)
            {
                CreateText(progress.transform, $"[P] {evt.CurrentParticipants:N0} participating", 11, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            }
        }

        private void CreateJoinButton(Transform parent, WorldEvent evt)
        {
            GameObject btn = new GameObject("JoinBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 100;
            le.preferredHeight = 50;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = goldColor;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => JoinEvent(evt));
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = evt.PlayerProgress > 0 ? "Continue" : "Join!";
            text.fontSize = 16;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
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
            
            CreateTab(tabs.transform, EventTab.Active, $"[*] Active ({_activeEvents.Count})");
            CreateTab(tabs.transform, EventTab.Upcoming, $"[D] Upcoming ({_upcomingEvents.Count})");
            CreateTab(tabs.transform, EventTab.Completed, "[OK] Completed");
            CreateTab(tabs.transform, EventTab.Rewards, "[?] Rewards");
        }

        private void CreateTab(Transform parent, EventTab tab, string label)
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
            text.fontSize = 14;
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
                case EventTab.Active:
                    CreateActiveEventsContent();
                    break;
                case EventTab.Upcoming:
                    CreateUpcomingEventsContent();
                    break;
                case EventTab.Completed:
                    CreateCompletedEventsContent();
                    break;
                case EventTab.Rewards:
                    CreateRewardsContent();
                    break;
            }
        }

        private void CreateActiveEventsContent()
        {
            if (_activeEvents.Count == 0)
            {
                CreateText(_contentContainer.transform, "No active events right now. Check back soon!", 14, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
                return;
            }
            
            foreach (var evt in _activeEvents)
            {
                CreateEventCard(evt, false);
            }
        }

        private void CreateUpcomingEventsContent()
        {
            if (_upcomingEvents.Count == 0)
            {
                CreateText(_contentContainer.transform, "No upcoming events scheduled.", 14, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
                return;
            }
            
            foreach (var evt in _upcomingEvents)
            {
                CreateEventCard(evt, true);
            }
        }

        private void CreateEventCard(WorldEvent evt, bool upcoming)
        {
            GameObject card = new GameObject($"Event_{evt.EventId}");
            card.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            Color cardColor = evt.Difficulty switch
            {
                EventDifficulty.Easy => new Color(0.1f, 0.15f, 0.1f),
                EventDifficulty.Normal => new Color(0.1f, 0.12f, 0.18f),
                EventDifficulty.Hard => new Color(0.15f, 0.12f, 0.1f),
                EventDifficulty.Epic => new Color(0.18f, 0.1f, 0.15f),
                _ => new Color(0.1f, 0.1f, 0.15f)
            };
            
            Image bg = card.AddComponent<Image>();
            bg.color = cardColor;
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Type badge
            GameObject badge = new GameObject("Badge");
            badge.transform.SetParent(card.transform, false);
            
            LayoutElement badgeLE = badge.AddComponent<LayoutElement>();
            badgeLE.preferredWidth = 60;
            
            VerticalLayoutGroup badgeVL = badge.AddComponent<VerticalLayoutGroup>();
            badgeVL.childAlignment = TextAnchor.MiddleCenter;
            
            string typeIcon = evt.Type switch
            {
                WorldEventType.WorldBoss => "👹",
                WorldEventType.Tournament => "[T]",
                WorldEventType.Seasonal => "🎄",
                WorldEventType.ResourceBoost => "[!]",
                WorldEventType.Challenge => "[+]",
                _ => "[T]"
            };
            CreateText(badge.transform, typeIcon, 32, TextAlignmentOptions.Center);
            CreateText(badge.transform, evt.Difficulty.ToString(), 10, TextAlignmentOptions.Center, GetDifficultyColor(evt.Difficulty));
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 4;
            
            CreateText(info.transform, $"<b>{evt.Name}</b>", 16, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, evt.Description, 11, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
            
            // Time
            if (upcoming)
            {
                TimeSpan until = evt.StartTime - DateTime.Now;
                string startStr = until.TotalDays >= 1 
                    ? $"Starts in {(int)until.TotalDays}d {until.Hours}h" 
                    : $"Starts in {until.Hours}h {until.Minutes}m";
                CreateText(info.transform, $"[D] {startStr}", 11, TextAlignmentOptions.Left, accentColor);
            }
            else
            {
                TimeSpan remaining = evt.EndTime - DateTime.Now;
                string endStr = remaining.TotalDays >= 1 
                    ? $"{(int)remaining.TotalDays}d {remaining.Hours}h left" 
                    : $"{remaining.Hours}h {remaining.Minutes}m left";
                Color timeColor = remaining.TotalHours < 24 ? urgentColor : new Color(0.8f, 0.8f, 0.8f);
                CreateText(info.transform, $"[T] {endStr}", 11, TextAlignmentOptions.Left, timeColor);
            }
            
            // Progress/Rank (for active events)
            if (!upcoming && evt.MaxProgress > 0)
            {
                CreateMiniProgressBar(info.transform, evt.PlayerProgress, evt.MaxProgress);
            }
            
            // Rewards preview
            if (evt.Rewards != null && evt.Rewards.Count > 0)
            {
                string rewardIcons = "";
                foreach (var reward in evt.Rewards)
                {
                    rewardIcons += reward.Icon + " ";
                }
                CreateText(info.transform, $"Rewards: {rewardIcons}", 10, TextAlignmentOptions.Left, goldColor);
            }
            
            // Action button
            if (!upcoming)
            {
                CreateSmallButton(card.transform, evt.PlayerProgress > 0 ? "Continue" : "Join", () => JoinEvent(evt));
            }
            else
            {
                CreateSmallButton(card.transform, "[!] Remind", () => SetReminder(evt), new Color(0.3f, 0.3f, 0.4f));
            }
        }

        private void CreateMiniProgressBar(Transform parent, int current, int max)
        {
            GameObject progress = new GameObject("MiniProgress");
            progress.transform.SetParent(parent, false);
            
            LayoutElement le = progress.AddComponent<LayoutElement>();
            le.preferredHeight = 12;
            
            HorizontalLayoutGroup hlayout = progress.AddComponent<HorizontalLayoutGroup>();
            hlayout.spacing = 5;
            
            // Bar
            GameObject bar = new GameObject("Bar");
            bar.transform.SetParent(progress.transform, false);
            
            LayoutElement barLE = bar.AddComponent<LayoutElement>();
            barLE.preferredWidth = 150;
            barLE.preferredHeight = 10;
            
            Image barBg = bar.AddComponent<Image>();
            barBg.color = new Color(0.15f, 0.15f, 0.2f);
            
            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            float pct = max > 0 ? (float)current / max : 0;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(pct, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = accentColor;
            
            // Text
            CreateText(progress.transform, $"{current}/{max}", 10, TextAlignmentOptions.Left, Color.white);
        }

        private void CreateCompletedEventsContent()
        {
            if (_completedEvents.Count == 0)
            {
                CreateText(_contentContainer.transform, "No completed events yet. Participate in active events!", 14, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
                return;
            }
            
            foreach (var evt in _completedEvents)
            {
                CreateCompletedEventCard(evt);
            }
        }

        private void CreateCompletedEventCard(WorldEvent evt)
        {
            GameObject card = new GameObject($"Completed_{evt.EventId}");
            card.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 70;
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.12f);
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Checkmark
            CreateText(card.transform, "[OK]", 24, TextAlignmentOptions.Center);
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(info.transform, evt.Name, 14, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, evt.Description, 10, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            CreateText(info.transform, evt.EndTime.ToString("MMM dd, yyyy"), 10, TextAlignmentOptions.Left, new Color(0.4f, 0.4f, 0.4f));
            
            // Stats
            GameObject stats = new GameObject("Stats");
            stats.transform.SetParent(card.transform, false);
            
            VerticalLayoutGroup statsVL = stats.AddComponent<VerticalLayoutGroup>();
            statsVL.childAlignment = TextAnchor.MiddleRight;
            
            CreateText(stats.transform, $"Progress: {evt.PlayerProgress}/{evt.MaxProgress}", 11, TextAlignmentOptions.Right, accentColor);
            if (evt.CurrentRank > 0)
            {
                CreateText(stats.transform, $"Rank: #{evt.CurrentRank} of {evt.TotalParticipants:N0}", 10, TextAlignmentOptions.Right, goldColor);
            }
        }

        private void CreateRewardsContent()
        {
            CreateSectionHeader("[?] Unclaimed Rewards");
            
            bool hasUnclaimed = false;
            foreach (var evt in _activeEvents)
            {
                if (evt.Rewards == null) continue;
                foreach (var reward in evt.Rewards)
                {
                    if (!reward.Claimed)
                    {
                        CreateRewardItem(evt, reward);
                        hasUnclaimed = true;
                    }
                }
            }
            
            if (!hasUnclaimed)
            {
                CreateText(_contentContainer.transform, "No unclaimed rewards. Complete event milestones to earn rewards!", 13, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            }
            
            CreateSectionHeader("[S] Claimed Rewards");
            
            foreach (var evt in _activeEvents)
            {
                if (evt.Rewards == null) continue;
                foreach (var reward in evt.Rewards)
                {
                    if (reward.Claimed)
                    {
                        CreateClaimedRewardItem(reward);
                    }
                }
            }
        }

        private void CreateRewardItem(WorldEvent evt, EventReward reward)
        {
            GameObject item = new GameObject($"Reward_{reward.Name}");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Color rarityColor = GetRarityColor(reward.Rarity);
            
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.18f);
            
            UnityEngine.UI.Outline outline = item.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = rarityColor;
            outline.effectDistance = new Vector2(2, 2);
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Icon
            CreateText(item.transform, reward.Icon, 28, TextAlignmentOptions.Center);
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(item.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(info.transform, $"<b>{reward.Name}</b>", 14, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, $"<color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{reward.Rarity}</color> - From: {evt.Name}", 11, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            // Claim button
            CreateSmallButton(item.transform, "Claim!", () => ClaimReward(evt, reward), goldColor);
        }

        private void CreateClaimedRewardItem(EventReward reward)
        {
            GameObject item = new GameObject($"Claimed_{reward.Name}");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            CreateText(item.transform, "[OK]", 16, TextAlignmentOptions.Center, new Color(0.3f, 0.6f, 0.3f));
            CreateText(item.transform, reward.Icon, 20, TextAlignmentOptions.Center);
            
            GameObject nameObj = CreateText(item.transform, reward.Name, 13, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            nameObj.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            CreateText(item.transform, reward.Rarity, 11, TextAlignmentOptions.Right, GetRarityColor(reward.Rarity) * 0.6f);
        }

        #region UI Helpers

        private void CreateSectionHeader(string text)
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            TextMeshProUGUI headerText = header.AddComponent<TextMeshProUGUI>();
            headerText.text = text;
            headerText.fontSize = 16;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = accentColor;
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
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal; //  = false;
            
            return obj;
        }

        private void CreateSmallButton(Transform parent, string label, Action onClick, Color? color = null)
        {
            GameObject btn = new GameObject($"Btn_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            le.preferredHeight = 35;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color ?? accentColor;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
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

        private Color GetDifficultyColor(EventDifficulty difficulty)
        {
            return difficulty switch
            {
                EventDifficulty.Easy => new Color(0.3f, 0.7f, 0.3f),
                EventDifficulty.Normal => accentColor,
                EventDifficulty.Hard => new Color(0.9f, 0.6f, 0.2f),
                EventDifficulty.Epic => rareColor,
                _ => Color.white
            };
        }

        private Color GetRarityColor(string rarity)
        {
            return rarity?.ToLower() switch
            {
                "common" => new Color(0.7f, 0.7f, 0.7f),
                "uncommon" => new Color(0.3f, 0.7f, 0.3f),
                "rare" => accentColor,
                "epic" => rareColor,
                "legendary" => goldColor,
                _ => Color.white
            };
        }

        #endregion

        #region Event Actions

        private void JoinEvent(WorldEvent evt)
        {
            ApexLogger.Log($"[Events] Joining event: {evt.Name}", ApexLogger.LogCategory.UI);
            OnEventJoined?.Invoke(evt);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"Joined {evt.Name}!");
            }
        }

        private void SetReminder(WorldEvent evt)
        {
            ApexLogger.Log($"[Events] Setting reminder for: {evt.Name}", ApexLogger.LogCategory.UI);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Reminder set for {evt.Name}");
            }
        }

        private void ClaimReward(WorldEvent evt, EventReward reward)
        {
            reward.Claimed = true;
            RefreshContent();
            
            OnRewardClaimed?.Invoke(evt, reward);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowAchievementUnlocked(reward.Name, 0);
            }
            
            ApexLogger.Log($"[Events] Claimed reward: {reward.Name}", ApexLogger.LogCategory.UI);
        }

        #endregion

        private void SelectTab(EventTab tab)
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

        public List<WorldEvent> GetActiveEvents() => _activeEvents;
        public int GetActiveEventCount() => _activeEvents.Count;

        #endregion
    }

    #region Data Classes

    public enum EventTab
    {
        Active,
        Upcoming,
        Completed,
        Rewards
    }

    public enum WorldEventType
    {
        WorldBoss,
        Tournament,
        Seasonal,
        ResourceBoost,
        Challenge,
        Special
    }

    public enum EventDifficulty
    {
        Easy,
        Normal,
        Hard,
        Epic
    }

    public class WorldEvent
    {
        public string EventId;
        public string Name;
        public string Description;
        public WorldEventType Type;
        public EventDifficulty Difficulty;
        public DateTime StartTime;
        public DateTime EndTime;
        public int CurrentParticipants;
        public int MaxParticipants;
        public int TotalParticipants;
        public int PlayerProgress;
        public int MaxProgress;
        public int CurrentRank;
        public List<EventReward> Rewards;
    }

    public class EventReward
    {
        public string Name;
        public string Icon;
        public string Rarity;
        public bool Claimed;
    }

    #endregion
}
