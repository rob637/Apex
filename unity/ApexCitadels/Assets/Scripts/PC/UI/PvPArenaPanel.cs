using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.UI;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// PvP Arena Panel - Competitive player vs player matchmaking and rankings.
    /// Features 1v1 duels, tournaments, ranked seasons, and leaderboards.
    /// </summary>
    public class PvPArenaPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.9f, 0.3f, 0.3f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color silverColor = new Color(0.75f, 0.75f, 0.75f);
        [SerializeField] private Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        [SerializeField] private Color championColor = new Color(0.9f, 0.2f, 0.9f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _contentArea;
        private ArenaTab _selectedTab = ArenaTab.QuickMatch;
        private Dictionary<ArenaTab, GameObject> _tabs = new Dictionary<ArenaTab, GameObject>();
        
        // Player data
        private ArenaPlayerStats _playerStats;
        private List<RankedMatch> _recentMatches = new List<RankedMatch>();
        private List<ArenaOpponent> _matchmaking = new List<ArenaOpponent>();
        private List<Tournament> _tournaments = new List<Tournament>();
        private List<ArenaLeaderboardEntry> _leaderboard = new List<ArenaLeaderboardEntry>();
        
        // Matchmaking
        private bool _isSearching;
        private float _searchTime;
        private ArenaMode _selectedMode = ArenaMode.Ranked1v1;
        
        public static PvPArenaPanel Instance { get; private set; }
        
        public event Action<ArenaOpponent> OnMatchFound;
        public event Action<RankedMatch> OnMatchComplete;
        public event Action<Tournament> OnTournamentJoined;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeArenaData();
        }

        private void Start()
        {
            CreatePanel();
            Hide();
        }

        private void Update()
        {
            if (_isSearching)
            {
                _searchTime += Time.deltaTime;
                
                // Simulate finding a match after 3-8 seconds
                if (_searchTime >= UnityEngine.Random.Range(3f, 8f))
                {
                    FindMatch();
                }
            }
        }

        private void InitializeArenaData()
        {
            // Player stats
            _playerStats = new ArenaPlayerStats
            {
                Rating = 1847,
                Rank = ArenaRank.Platinum2,
                Wins = 156,
                Losses = 89,
                WinStreak = 4,
                BestStreak = 12,
                SeasonWins = 45,
                SeasonRank = 234,
                TotalMatches = 245,
                Division = "Platinum II",
                NextRankPoints = 53,
                TotalArenaPoints = 12450
            };
            
            // Recent matches
            _recentMatches = new List<RankedMatch>
            {
                new RankedMatch
                {
                    MatchId = "M001",
                    OpponentName = "DragonSlayer99",
                    OpponentRating = 1892,
                    Won = true,
                    RatingChange = 18,
                    Duration = 342,
                    Mode = ArenaMode.Ranked1v1,
                    Timestamp = DateTime.Now.AddHours(-1)
                },
                new RankedMatch
                {
                    MatchId = "M002",
                    OpponentName = "IronFist",
                    OpponentRating = 1801,
                    Won = true,
                    RatingChange = 12,
                    Duration = 256,
                    Mode = ArenaMode.Ranked1v1,
                    Timestamp = DateTime.Now.AddHours(-3)
                },
                new RankedMatch
                {
                    MatchId = "M003",
                    OpponentName = "ShadowKnight",
                    OpponentRating = 1923,
                    Won = false,
                    RatingChange = -15,
                    Duration = 421,
                    Mode = ArenaMode.Ranked1v1,
                    Timestamp = DateTime.Now.AddHours(-5)
                },
                new RankedMatch
                {
                    MatchId = "M004",
                    OpponentName = "CrystalMage",
                    OpponentRating = 1776,
                    Won = true,
                    RatingChange = 11,
                    Duration = 198,
                    Mode = ArenaMode.Ranked1v1,
                    Timestamp = DateTime.Now.AddHours(-8)
                },
                new RankedMatch
                {
                    MatchId = "M005",
                    OpponentName = "WarChief",
                    OpponentRating = 1855,
                    Won = true,
                    RatingChange = 15,
                    Duration = 287,
                    Mode = ArenaMode.Ranked1v1,
                    Timestamp = DateTime.Now.AddDays(-1)
                }
            };
            
            // Available tournaments
            _tournaments = new List<Tournament>
            {
                new Tournament
                {
                    TournamentId = "T001",
                    Name = "Weekend Warrior Cup",
                    Description = "Weekly tournament with amazing prizes!",
                    EntryFee = 500,
                    PrizePool = 50000,
                    Participants = 128,
                    MaxParticipants = 128,
                    CurrentRound = 0,
                    Status = TournamentStatus.Registering,
                    StartTime = DateTime.Now.AddHours(2),
                    Format = "Single Elimination",
                    MinRating = 1500,
                    IsRegistered = false
                },
                new Tournament
                {
                    TournamentId = "T002",
                    Name = "Champions League",
                    Description = "Elite tournament for the best players. Diamond+ only.",
                    EntryFee = 2000,
                    PrizePool = 200000,
                    Participants = 32,
                    MaxParticipants = 64,
                    CurrentRound = 0,
                    Status = TournamentStatus.Registering,
                    StartTime = DateTime.Now.AddDays(1),
                    Format = "Double Elimination",
                    MinRating = 2000,
                    IsRegistered = false
                },
                new Tournament
                {
                    TournamentId = "T003",
                    Name = "Newcomer's Arena",
                    Description = "Tournament for newer players. Great way to get started!",
                    EntryFee = 0,
                    PrizePool = 10000,
                    Participants = 89,
                    MaxParticipants = 256,
                    CurrentRound = 0,
                    Status = TournamentStatus.Registering,
                    StartTime = DateTime.Now.AddHours(6),
                    Format = "Swiss System",
                    MinRating = 0,
                    MaxRating = 1600,
                    IsRegistered = false
                },
                new Tournament
                {
                    TournamentId = "T004",
                    Name = "Grand Championship",
                    Description = "Monthly grand tournament with legendary rewards!",
                    EntryFee = 1000,
                    PrizePool = 500000,
                    Participants = 512,
                    MaxParticipants = 1024,
                    CurrentRound = 3,
                    Status = TournamentStatus.InProgress,
                    StartTime = DateTime.Now.AddHours(-24),
                    Format = "Single Elimination",
                    MinRating = 1200,
                    IsRegistered = true,
                    CurrentPosition = 128
                }
            };
            
            // Leaderboard
            _leaderboard = new List<ArenaLeaderboardEntry>
            {
                new ArenaLeaderboardEntry { Rank = 1, PlayerName = "GrandMaster", Rating = 2847, Wins = 892, Losses = 123, Rank_Title = ArenaRank.Grandmaster },
                new ArenaLeaderboardEntry { Rank = 2, PlayerName = "LegendKing", Rating = 2756, Wins = 756, Losses = 145, Rank_Title = ArenaRank.Grandmaster },
                new ArenaLeaderboardEntry { Rank = 3, PlayerName = "ChampionX", Rating = 2698, Wins = 623, Losses = 134, Rank_Title = ArenaRank.Grandmaster },
                new ArenaLeaderboardEntry { Rank = 4, PlayerName = "EliteWarrior", Rating = 2634, Wins = 589, Losses = 156, Rank_Title = ArenaRank.Master },
                new ArenaLeaderboardEntry { Rank = 5, PlayerName = "DragonLord", Rating = 2589, Wins = 534, Losses = 167, Rank_Title = ArenaRank.Master },
                new ArenaLeaderboardEntry { Rank = 6, PlayerName = "StormBringer", Rating = 2534, Wins = 512, Losses = 178, Rank_Title = ArenaRank.Master },
                new ArenaLeaderboardEntry { Rank = 7, PlayerName = "ShadowFury", Rating = 2478, Wins = 489, Losses = 189, Rank_Title = ArenaRank.Diamond1 },
                new ArenaLeaderboardEntry { Rank = 8, PlayerName = "IronWill", Rating = 2423, Wins = 456, Losses = 201, Rank_Title = ArenaRank.Diamond1 },
                new ArenaLeaderboardEntry { Rank = 9, PlayerName = "FlameHeart", Rating = 2367, Wins = 423, Losses = 212, Rank_Title = ArenaRank.Diamond2 },
                new ArenaLeaderboardEntry { Rank = 10, PlayerName = "FrostKnight", Rating = 2312, Wins = 398, Losses = 223, Rank_Title = ArenaRank.Diamond2 }
            };
            
            // Matchmaking pool
            _matchmaking = new List<ArenaOpponent>
            {
                new ArenaOpponent { PlayerId = "OPP1", Name = "BattleMaster", Rating = 1823, Level = 42, WinRate = 0.62f, Guild = "Shadow Legion" },
                new ArenaOpponent { PlayerId = "OPP2", Name = "WarHammer", Rating = 1867, Level = 45, WinRate = 0.58f, Guild = "Iron Fist" },
                new ArenaOpponent { PlayerId = "OPP3", Name = "NightBlade", Rating = 1801, Level = 39, WinRate = 0.64f, Guild = "Dark Realm" },
                new ArenaOpponent { PlayerId = "OPP4", Name = "ThunderStrike", Rating = 1889, Level = 47, WinRate = 0.55f, Guild = "Storm Guard" }
            };
        }

        private void CreatePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("PvPArenaPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.05f);
            rect.anchorMax = new Vector2(0.9f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.04f, 0.08f, 0.98f);
            
            VerticalLayoutGroup vlayout = _panel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Header with player stats
            CreateHeader();
            
            // Tab navigation
            CreateTabNavigation();
            
            // Content area
            CreateContentArea();
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            Image bg = header.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.05f, 0.1f);
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            // Title
            CreateHeaderTitle(header.transform);
            
            // Player rating card
            CreateRatingCard(header.transform);
            
            // Season progress
            CreateSeasonProgress(header.transform);
            
            // Close button
            CreateCloseButton(header.transform);
        }

        private void CreateHeaderTitle(Transform parent)
        {
            GameObject title = new GameObject("Title");
            title.transform.SetParent(parent, false);
            
            LayoutElement le = title.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            
            VerticalLayoutGroup vlayout = title.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(title.transform, "[!] PVP ARENA", 24, TextAlignmentOptions.Left, accentColor);
            CreateText(title.transform, "Prove your strength!", 12, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateRatingCard(Transform parent)
        {
            GameObject card = new GameObject("RatingCard");
            card.transform.SetParent(parent, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredWidth = 250;
            le.flexibleHeight = 1;
            
            Image bg = card.AddComponent<Image>();
            bg.color = GetRankColor(_playerStats.Rank) * 0.3f;
            
            UnityEngine.UI.Outline outline = card.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = GetRankColor(_playerStats.Rank);
            outline.effectDistance = new Vector2(2, 2);
            
            VerticalLayoutGroup vlayout = card.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(15, 15, 10, 10);
            
            CreateText(card.transform, GetRankIcon(_playerStats.Rank) + " " + _playerStats.Division, 14, TextAlignmentOptions.Center, GetRankColor(_playerStats.Rank));
            CreateText(card.transform, $"Rating: {_playerStats.Rating}", 20, TextAlignmentOptions.Center, Color.white);
            CreateText(card.transform, $"W: {_playerStats.Wins} | L: {_playerStats.Losses} ({(_playerStats.Wins * 100 / (_playerStats.Wins + _playerStats.Losses))}%)", 11, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
            CreateText(card.transform, $"[*] {_playerStats.WinStreak} win streak", 11, TextAlignmentOptions.Center, goldColor);
        }

        private void CreateSeasonProgress(Transform parent)
        {
            GameObject progress = new GameObject("SeasonProgress");
            progress.transform.SetParent(parent, false);
            
            LayoutElement le = progress.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            
            VerticalLayoutGroup vlayout = progress.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            
            CreateText(progress.transform, "[T] SEASON 7", 12, TextAlignmentOptions.Center, goldColor);
            CreateText(progress.transform, $"Season Rank: #{_playerStats.SeasonRank}", 14, TextAlignmentOptions.Center, Color.white);
            CreateText(progress.transform, $"{_playerStats.NextRankPoints} pts to next rank", 11, TextAlignmentOptions.Center, new Color(0.6f, 0.8f, 0.6f));
            
            // Progress bar
            CreateProgressBar(progress.transform, 0.72f, accentColor);
            
            CreateText(progress.transform, $"Arena Points: {_playerStats.TotalArenaPoints:N0}", 10, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateProgressBar(Transform parent, float progress, Color color)
        {
            GameObject bar = new GameObject("ProgressBar");
            bar.transform.SetParent(parent, false);
            
            LayoutElement le = bar.AddComponent<LayoutElement>();
            le.preferredHeight = 12;
            le.preferredWidth = 200;
            
            Image bgImg = bar.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(progress, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = color;
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
            text.text = "[X]";
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateTabNavigation()
        {
            GameObject tabs = new GameObject("Tabs");
            tabs.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = tabs.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = tabs.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 5;
            
            CreateTab(tabs.transform, ArenaTab.QuickMatch, "[!] Quick Match");
            CreateTab(tabs.transform, ArenaTab.Ranked, "[T] Ranked");
            CreateTab(tabs.transform, ArenaTab.Tournaments, "[E] Tournaments");
            CreateTab(tabs.transform, ArenaTab.Leaderboard, "[#] Leaderboard");
            CreateTab(tabs.transform, ArenaTab.History, "[S] History");
            CreateTab(tabs.transform, ArenaTab.Rewards, "[?] Rewards");
        }

        private void CreateTab(Transform parent, ArenaTab tab, string label)
        {
            GameObject tabObj = new GameObject($"Tab_{tab}");
            tabObj.transform.SetParent(parent, false);
            
            LayoutElement le = tabObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            Color bgColor = tab == _selectedTab ? accentColor : new Color(0.15f, 0.1f, 0.18f);
            
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
            text.fontSize = 12;
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
            bg.color = new Color(0.04f, 0.03f, 0.05f);
            
            RefreshContent();
        }

        private void RefreshContent()
        {
            // Guard against null contentArea
            if (_contentArea == null) return;
            
            foreach (Transform child in _contentArea.transform)
            {
                Destroy(child.gameObject);
            }
            
            switch (_selectedTab)
            {
                case ArenaTab.QuickMatch:
                    CreateQuickMatchContent();
                    break;
                case ArenaTab.Ranked:
                    CreateRankedContent();
                    break;
                case ArenaTab.Tournaments:
                    CreateTournamentsContent();
                    break;
                case ArenaTab.Leaderboard:
                    CreateLeaderboardContent();
                    break;
                case ArenaTab.History:
                    CreateHistoryContent();
                    break;
                case ArenaTab.Rewards:
                    CreateRewardsContent();
                    break;
            }
        }

        private void CreateQuickMatchContent()
        {
            if (_contentArea == null) return;
            
            VerticalLayoutGroup vlayout = _contentArea.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 15;
            vlayout.padding = new RectOffset(20, 20, 20, 20);
            
            // Mode selection
            CreateModeSelection();
            
            // Find match button or searching indicator
            if (_isSearching)
            {
                CreateSearchingIndicator();
            }
            else
            {
                CreateFindMatchButton();
            }
            
            // Quick stats
            CreateQuickStats();
        }

        private void CreateModeSelection()
        {
            CreateSectionHeader("SELECT GAME MODE");
            
            GameObject modes = new GameObject("Modes");
            modes.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = modes.AddComponent<LayoutElement>();
            le.preferredHeight = 120;
            
            HorizontalLayoutGroup hlayout = modes.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            CreateModeCard(modes.transform, ArenaMode.Ranked1v1, "[!]", "Ranked 1v1", "Compete for rating", true);
            CreateModeCard(modes.transform, ArenaMode.Casual, "[G]", "Casual", "No rating change", false);
            CreateModeCard(modes.transform, ArenaMode.Draft, "[T]", "Draft Mode", "Random heroes", false);
            CreateModeCard(modes.transform, ArenaMode.Challenge, "[+]", "Challenge", "Fight friends", false);
        }

        private void CreateModeCard(Transform parent, ArenaMode mode, string icon, string title, string desc, bool selected)
        {
            GameObject card = new GameObject($"Mode_{mode}");
            card.transform.SetParent(parent, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            
            Color bgColor = mode == _selectedMode ? new Color(accentColor.r * 0.3f, accentColor.g * 0.3f, accentColor.b * 0.3f) 
                                                 : new Color(0.08f, 0.06f, 0.1f);
            
            Image bg = card.AddComponent<Image>();
            bg.color = bgColor;
            
            if (mode == _selectedMode)
            {
                UnityEngine.UI.Outline outline = card.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = accentColor;
                outline.effectDistance = new Vector2(2, 2);
            }
            
            Button btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectMode(mode));
            
            VerticalLayoutGroup vlayout = card.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            CreateText(card.transform, icon, 32, TextAlignmentOptions.Center);
            CreateText(card.transform, title, 14, TextAlignmentOptions.Center, mode == _selectedMode ? accentColor : Color.white);
            CreateText(card.transform, desc, 10, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateFindMatchButton()
        {
            GameObject container = new GameObject("FindMatchContainer");
            container.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = container.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            VerticalLayoutGroup vlayout = container.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 10;
            
            GameObject btn = new GameObject("FindMatchBtn");
            btn.transform.SetParent(container.transform, false);
            
            LayoutElement btnLE = btn.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 300;
            btnLE.preferredHeight = 60;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = accentColor;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(StartMatchmaking);
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "[!] FIND MATCH";
            text.fontSize = 20;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            CreateText(container.transform, "Estimated wait: ~15 seconds", 11, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateSearchingIndicator()
        {
            GameObject container = new GameObject("SearchingContainer");
            container.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = container.AddComponent<LayoutElement>();
            le.preferredHeight = 150;
            
            VerticalLayoutGroup vlayout = container.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 15;
            
            CreateText(container.transform, "[?]", 48, TextAlignmentOptions.Center);
            CreateText(container.transform, "SEARCHING FOR OPPONENT...", 18, TextAlignmentOptions.Center, accentColor);
            CreateText(container.transform, $"Time: {Mathf.FloorToInt(_searchTime)}s", 14, TextAlignmentOptions.Center, Color.white);
            CreateText(container.transform, $"Rating range: {_playerStats.Rating - 100} - {_playerStats.Rating + 100}", 11, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            
            // Cancel button
            GameObject cancelBtn = new GameObject("CancelBtn");
            cancelBtn.transform.SetParent(container.transform, false);
            
            LayoutElement btnLE = cancelBtn.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 150;
            btnLE.preferredHeight = 40;
            
            Image bg = cancelBtn.AddComponent<Image>();
            bg.color = new Color(0.4f, 0.2f, 0.2f);
            
            Button button = cancelBtn.AddComponent<Button>();
            button.onClick.AddListener(CancelMatchmaking);
            
            TextMeshProUGUI text = cancelBtn.AddComponent<TextMeshProUGUI>();
            text.text = "Cancel";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateQuickStats()
        {
            CreateSectionHeader("YOUR STATS");
            
            GameObject stats = new GameObject("QuickStats");
            stats.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = stats.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            HorizontalLayoutGroup hlayout = stats.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 10;
            
            CreateStatCard(stats.transform, "[T]", "Win Rate", $"{(_playerStats.Wins * 100 / (_playerStats.Wins + _playerStats.Losses))}%");
            CreateStatCard(stats.transform, "[!]", "Total Matches", _playerStats.TotalMatches.ToString());
            CreateStatCard(stats.transform, "[*]", "Best Streak", _playerStats.BestStreak.ToString());
            CreateStatCard(stats.transform, "[#]", "Season Wins", _playerStats.SeasonWins.ToString());
        }

        private void CreateStatCard(Transform parent, string icon, string label, string value)
        {
            GameObject card = new GameObject($"Stat_{label}");
            card.transform.SetParent(parent, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.12f);
            
            VerticalLayoutGroup vlayout = card.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 3;
            
            CreateText(card.transform, icon, 18, TextAlignmentOptions.Center);
            CreateText(card.transform, value, 16, TextAlignmentOptions.Center, accentColor);
            CreateText(card.transform, label, 10, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateRankedContent()
        {
            VerticalLayoutGroup vlayout = _contentArea.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 15;
            vlayout.padding = new RectOffset(20, 20, 20, 20);
            
            // Rank ladder
            CreateRankLadder();
            
            // Season info
            CreateSeasonInfo();
        }

        private void CreateRankLadder()
        {
            CreateSectionHeader("RANK LADDER");
            
            GameObject ladder = new GameObject("RankLadder");
            ladder.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = ladder.AddComponent<LayoutElement>();
            le.preferredHeight = 200;
            
            HorizontalLayoutGroup hlayout = ladder.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 5;
            
            ArenaRank[] ranks = { ArenaRank.Bronze, ArenaRank.Silver, ArenaRank.Gold, ArenaRank.Platinum1, ArenaRank.Diamond1, ArenaRank.Master, ArenaRank.Grandmaster };
            
            foreach (var rank in ranks)
            {
                bool isCurrent = rank == _playerStats.Rank || (rank == ArenaRank.Platinum1 && _playerStats.Rank == ArenaRank.Platinum2);
                CreateRankCard(ladder.transform, rank, isCurrent);
            }
        }

        private void CreateRankCard(Transform parent, ArenaRank rank, bool isCurrent)
        {
            GameObject card = new GameObject($"Rank_{rank}");
            card.transform.SetParent(parent, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            
            Color rankColor = GetRankColor(rank);
            
            Image bg = card.AddComponent<Image>();
            bg.color = isCurrent ? new Color(rankColor.r * 0.4f, rankColor.g * 0.4f, rankColor.b * 0.4f) 
                                : new Color(0.08f, 0.06f, 0.1f);
            
            if (isCurrent)
            {
                UnityEngine.UI.Outline outline = card.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = rankColor;
                outline.effectDistance = new Vector2(2, 2);
            }
            
            VerticalLayoutGroup vlayout = card.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(5, 5, 10, 10);
            
            CreateText(card.transform, GetRankIcon(rank), 28, TextAlignmentOptions.Center);
            CreateText(card.transform, rank.ToString().Replace("1", " I").Replace("2", " II"), 10, TextAlignmentOptions.Center, rankColor);
            CreateText(card.transform, GetRankRatingRange(rank), 8, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            
            if (isCurrent)
            {
                CreateText(card.transform, "YOU", 9, TextAlignmentOptions.Center, goldColor);
            }
        }

        private void CreateSeasonInfo()
        {
            CreateSectionHeader("SEASON 7 - DRAGON'S FURY");
            
            GameObject season = new GameObject("SeasonInfo");
            season.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = season.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            Image bg = season.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.12f);
            
            HorizontalLayoutGroup hlayout = season.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 30;
            hlayout.padding = new RectOffset(30, 30, 15, 15);
            
            // Time remaining
            GameObject timeLeft = new GameObject("TimeLeft");
            timeLeft.transform.SetParent(season.transform, false);
            
            LayoutElement timele = timeLeft.AddComponent<LayoutElement>();
            timele.preferredWidth = 150;
            
            VerticalLayoutGroup timeVL = timeLeft.AddComponent<VerticalLayoutGroup>();
            timeVL.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(timeLeft.transform, "[T] TIME LEFT", 10, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            CreateText(timeLeft.transform, "14 Days", 18, TextAlignmentOptions.Center, accentColor);
            
            // Season rewards preview
            GameObject rewards = new GameObject("SeasonRewards");
            rewards.transform.SetParent(season.transform, false);
            
            LayoutElement rewle = rewards.AddComponent<LayoutElement>();
            rewle.flexibleWidth = 1;
            
            VerticalLayoutGroup rewVL = rewards.AddComponent<VerticalLayoutGroup>();
            rewVL.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(rewards.transform, "[?] SEASON REWARDS", 10, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            CreateText(rewards.transform, "Exclusive Dragon Armor Set + 50,000 Gold", 12, TextAlignmentOptions.Center, goldColor);
            CreateText(rewards.transform, "Reach Platinum to unlock!", 10, TextAlignmentOptions.Center, new Color(0.4f, 0.8f, 0.4f));
        }

        private void CreateTournamentsContent()
        {
            VerticalLayoutGroup vlayout = _contentArea.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            CreateSectionHeader("AVAILABLE TOURNAMENTS");
            
            foreach (var tournament in _tournaments)
            {
                CreateTournamentCard(tournament);
            }
        }

        private void CreateTournamentCard(Tournament tournament)
        {
            GameObject card = new GameObject($"Tournament_{tournament.TournamentId}");
            card.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            bool canJoin = !tournament.IsRegistered && tournament.Status == TournamentStatus.Registering
                          && _playerStats.Rating >= tournament.MinRating
                          && (tournament.MaxRating == 0 || _playerStats.Rating <= tournament.MaxRating);
            
            Image bg = card.AddComponent<Image>();
            bg.color = tournament.IsRegistered ? new Color(0.1f, 0.15f, 0.1f) : new Color(0.08f, 0.06f, 0.1f);
            
            if (tournament.IsRegistered)
            {
                UnityEngine.UI.Outline outline = card.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = new Color(0.3f, 0.8f, 0.3f);
                outline.effectDistance = new Vector2(1, 1);
            }
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Tournament info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 3;
            
            CreateText(info.transform, tournament.Name, 14, TextAlignmentOptions.Left, goldColor);
            CreateText(info.transform, tournament.Description, 10, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
            CreateText(info.transform, $"Format: {tournament.Format} | Min Rating: {tournament.MinRating}", 9, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            
            // Participants
            GameObject participants = new GameObject("Participants");
            participants.transform.SetParent(card.transform, false);
            
            LayoutElement partLE = participants.AddComponent<LayoutElement>();
            partLE.preferredWidth = 100;
            
            VerticalLayoutGroup partVL = participants.AddComponent<VerticalLayoutGroup>();
            partVL.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(participants.transform, "[P]", 20, TextAlignmentOptions.Center);
            CreateText(participants.transform, $"{tournament.Participants}/{tournament.MaxParticipants}", 12, TextAlignmentOptions.Center, Color.white);
            
            // Prize
            GameObject prize = new GameObject("Prize");
            prize.transform.SetParent(card.transform, false);
            
            LayoutElement prizeLE = prize.AddComponent<LayoutElement>();
            prizeLE.preferredWidth = 100;
            
            VerticalLayoutGroup prizeVL = prize.AddComponent<VerticalLayoutGroup>();
            prizeVL.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(prize.transform, "[$]", 20, TextAlignmentOptions.Center);
            CreateText(prize.transform, $"{tournament.PrizePool:N0}", 12, TextAlignmentOptions.Center, goldColor);
            
            // Action button
            if (tournament.IsRegistered)
            {
                if (tournament.Status == TournamentStatus.InProgress)
                {
                    CreateTournamentButton(card.transform, "[G] Play", () => PlayTournamentMatch(tournament), accentColor);
                }
                else
                {
                    CreateTournamentButton(card.transform, "[OK] Registered", null, new Color(0.3f, 0.5f, 0.3f));
                }
            }
            else if (canJoin)
            {
                string btnLabel = tournament.EntryFee > 0 ? $"Join ({tournament.EntryFee})" : "Join Free";
                CreateTournamentButton(card.transform, btnLabel, () => JoinTournament(tournament), accentColor);
            }
            else
            {
                CreateTournamentButton(card.transform, "Locked", null, new Color(0.3f, 0.3f, 0.3f));
            }
        }

        private void CreateTournamentButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btn = new GameObject("TournamentBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 100;
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

        private void CreateLeaderboardContent()
        {
            VerticalLayoutGroup vlayout = _contentArea.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            CreateSectionHeader("TOP PLAYERS - SEASON 7");
            
            // Header row
            CreateLeaderboardHeader();
            
            // Entries
            foreach (var entry in _leaderboard)
            {
                CreateArenaLeaderboardEntry(entry);
            }
            
            // Player's position
            CreatePlayerLeaderboardPosition();
        }

        private void CreateLeaderboardHeader()
        {
            GameObject header = new GameObject("LeaderboardHeader");
            header.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            Image bg = header.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.12f);
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            CreateLeaderboardCell(header.transform, "#", 50, TextAlignmentOptions.Center);
            CreateLeaderboardCell(header.transform, "Player", 200, TextAlignmentOptions.Left);
            CreateLeaderboardCell(header.transform, "Rank", 100, TextAlignmentOptions.Center);
            CreateLeaderboardCell(header.transform, "Rating", 80, TextAlignmentOptions.Center);
            CreateLeaderboardCell(header.transform, "W/L", 100, TextAlignmentOptions.Center);
            CreateLeaderboardCell(header.transform, "Win%", 80, TextAlignmentOptions.Center);
        }

        private void CreateArenaLeaderboardEntry(ArenaLeaderboardEntry entry)
        {
            GameObject row = new GameObject($"Entry_{entry.Rank}");
            row.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 35;
            
            Color bgColor = entry.Rank <= 3 ? new Color(goldColor.r * 0.15f, goldColor.g * 0.15f, goldColor.b * 0.15f) 
                                           : new Color(0.06f, 0.05f, 0.08f);
            
            Image bg = row.AddComponent<Image>();
            bg.color = bgColor;
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            Color rankColor = entry.Rank == 1 ? goldColor : entry.Rank == 2 ? silverColor : entry.Rank == 3 ? bronzeColor : Color.white;
            
            CreateLeaderboardCell(row.transform, GetLeaderboardRankDisplay(entry.Rank), 50, TextAlignmentOptions.Center, rankColor);
            CreateLeaderboardCell(row.transform, entry.PlayerName, 200, TextAlignmentOptions.Left, Color.white);
            CreateLeaderboardCell(row.transform, GetRankIcon(entry.Rank_Title) + " " + entry.Rank_Title.ToString(), 100, TextAlignmentOptions.Center, GetRankColor(entry.Rank_Title));
            CreateLeaderboardCell(row.transform, entry.Rating.ToString(), 80, TextAlignmentOptions.Center, accentColor);
            CreateLeaderboardCell(row.transform, $"{entry.Wins}/{entry.Losses}", 100, TextAlignmentOptions.Center, Color.white);
            
            int winRate = entry.Wins * 100 / (entry.Wins + entry.Losses);
            CreateLeaderboardCell(row.transform, $"{winRate}%", 80, TextAlignmentOptions.Center, winRate >= 60 ? new Color(0.4f, 0.9f, 0.4f) : Color.white);
        }

        private void CreateLeaderboardCell(Transform parent, string text, float width, TextAlignmentOptions align, Color? color = null)
        {
            GameObject cell = new GameObject("Cell");
            cell.transform.SetParent(parent, false);
            
            LayoutElement le = cell.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            
            TextMeshProUGUI tmp = cell.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 11;
            tmp.alignment = align;
            tmp.color = color ?? new Color(0.7f, 0.7f, 0.7f);
        }

        private string GetLeaderboardRankDisplay(int rank)
        {
            return rank switch
            {
                1 => GameIcons.GoldMedal,
                2 => GameIcons.SilverMedal,
                3 => GameIcons.BronzeMedal,
                _ => $"#{rank}"
            };
        }

        private void CreatePlayerLeaderboardPosition()
        {
            GameObject separator = new GameObject("Separator");
            separator.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement sepLE = separator.AddComponent<LayoutElement>();
            sepLE.preferredHeight = 20;
            
            CreateText(separator.transform, "- - -", 12, TextAlignmentOptions.Center, new Color(0.4f, 0.4f, 0.4f));
            
            // Player's row
            GameObject playerRow = new GameObject("PlayerRow");
            playerRow.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = playerRow.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            Image bg = playerRow.AddComponent<Image>();
            bg.color = new Color(accentColor.r * 0.2f, accentColor.g * 0.2f, accentColor.b * 0.2f);
            
            UnityEngine.UI.Outline outline = playerRow.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(1, 1);
            
            HorizontalLayoutGroup hlayout = playerRow.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            CreateLeaderboardCell(playerRow.transform, $"#{_playerStats.SeasonRank}", 50, TextAlignmentOptions.Center, accentColor);
            CreateLeaderboardCell(playerRow.transform, "You", 200, TextAlignmentOptions.Left, goldColor);
            CreateLeaderboardCell(playerRow.transform, GetRankIcon(_playerStats.Rank) + " " + _playerStats.Division, 100, TextAlignmentOptions.Center, GetRankColor(_playerStats.Rank));
            CreateLeaderboardCell(playerRow.transform, _playerStats.Rating.ToString(), 80, TextAlignmentOptions.Center, accentColor);
            CreateLeaderboardCell(playerRow.transform, $"{_playerStats.Wins}/{_playerStats.Losses}", 100, TextAlignmentOptions.Center, Color.white);
            
            int winRate = _playerStats.Wins * 100 / (_playerStats.Wins + _playerStats.Losses);
            CreateLeaderboardCell(playerRow.transform, $"{winRate}%", 80, TextAlignmentOptions.Center, new Color(0.4f, 0.9f, 0.4f));
        }

        private void CreateHistoryContent()
        {
            VerticalLayoutGroup vlayout = _contentArea.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 8;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            CreateSectionHeader("RECENT MATCHES");
            
            foreach (var match in _recentMatches)
            {
                CreateMatchHistoryEntry(match);
            }
        }

        private void CreateMatchHistoryEntry(RankedMatch match)
        {
            GameObject entry = new GameObject($"Match_{match.MatchId}");
            entry.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = entry.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            Color bgColor = match.Won ? new Color(0.1f, 0.15f, 0.1f) : new Color(0.15f, 0.1f, 0.1f);
            
            Image bg = entry.AddComponent<Image>();
            bg.color = bgColor;
            
            HorizontalLayoutGroup hlayout = entry.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 8, 8);
            
            // Result
            string resultIcon = match.Won ? "[OK]" : "[X]";
            Color resultColor = match.Won ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.9f, 0.4f, 0.4f);
            CreateText(entry.transform, resultIcon, 24, TextAlignmentOptions.Center, resultColor);
            
            // Opponent info
            GameObject opponent = new GameObject("Opponent");
            opponent.transform.SetParent(entry.transform, false);
            
            LayoutElement oppLE = opponent.AddComponent<LayoutElement>();
            oppLE.flexibleWidth = 1;
            
            VerticalLayoutGroup oppVL = opponent.AddComponent<VerticalLayoutGroup>();
            oppVL.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(opponent.transform, $"vs {match.OpponentName}", 13, TextAlignmentOptions.Left, Color.white);
            CreateText(opponent.transform, $"Rating: {match.OpponentRating}", 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            // Duration
            int minutes = match.Duration / 60;
            int seconds = match.Duration % 60;
            CreateText(entry.transform, $"[T] {minutes}:{seconds:D2}", 11, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            
            // Rating change
            string ratingText = match.RatingChange > 0 ? $"+{match.RatingChange}" : match.RatingChange.ToString();
            Color ratingColor = match.RatingChange > 0 ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.9f, 0.4f, 0.4f);
            CreateText(entry.transform, ratingText, 16, TextAlignmentOptions.Center, ratingColor);
            
            // Time ago
            string timeAgo = GetTimeAgo(match.Timestamp);
            CreateText(entry.transform, timeAgo, 10, TextAlignmentOptions.Right, new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateRewardsContent()
        {
            VerticalLayoutGroup vlayout = _contentArea.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 15;
            vlayout.padding = new RectOffset(20, 20, 20, 20);
            
            // Arena shop
            CreateSectionHeader("ARENA SHOP");
            CreateText(_contentArea.transform, $"Arena Points: {_playerStats.TotalArenaPoints:N0} [T]", 16, TextAlignmentOptions.Center, goldColor);
            
            CreateArenaShop();
            
            // Milestone rewards
            CreateSectionHeader("MILESTONE REWARDS");
            CreateMilestoneRewards();
        }

        private void CreateArenaShop()
        {
            GameObject shop = new GameObject("Shop");
            shop.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = shop.AddComponent<LayoutElement>();
            le.preferredHeight = 120;
            
            HorizontalLayoutGroup hlayout = shop.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 10;
            
            CreateShopItem(shop.transform, "[!]", "Champion's Blade", 5000, false);
            CreateShopItem(shop.transform, "[D]", "Arena Shield", 3000, false);
            CreateShopItem(shop.transform, "[S]", "XP Scroll x10", 1000, true);
            CreateShopItem(shop.transform, "[P]", "Potions x50", 500, true);
        }

        private void CreateShopItem(Transform parent, string icon, string name, int cost, bool canAfford)
        {
            GameObject item = new GameObject($"Shop_{name}");
            item.transform.SetParent(parent, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.12f);
            
            if (canAfford && _playerStats.TotalArenaPoints >= cost)
            {
                Button btn = item.AddComponent<Button>();
                btn.onClick.AddListener(() => PurchaseItem(name, cost));
            }
            
            VerticalLayoutGroup vlayout = item.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            CreateText(item.transform, icon, 28, TextAlignmentOptions.Center);
            CreateText(item.transform, name, 11, TextAlignmentOptions.Center, Color.white);
            
            Color costColor = _playerStats.TotalArenaPoints >= cost ? goldColor : new Color(0.5f, 0.3f, 0.3f);
            CreateText(item.transform, $"[T] {cost:N0}", 10, TextAlignmentOptions.Center, costColor);
        }

        private void CreateMilestoneRewards()
        {
            GameObject milestones = new GameObject("Milestones");
            milestones.transform.SetParent(_contentArea.transform, false);
            
            GridLayoutGroup grid = milestones.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150, 60);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            
            CreateMilestoneCard(milestones.transform, "10 Wins", "500 Gold", true);
            CreateMilestoneCard(milestones.transform, "50 Wins", "Epic Chest", true);
            CreateMilestoneCard(milestones.transform, "100 Wins", "Legendary Chest", true);
            CreateMilestoneCard(milestones.transform, "200 Wins", "Champion Title", false);
            CreateMilestoneCard(milestones.transform, "Reach Gold", "Gold Frame", true);
            CreateMilestoneCard(milestones.transform, "Reach Diamond", "Diamond Frame", false);
            CreateMilestoneCard(milestones.transform, "10 Win Streak", "Streak Banner", true);
            CreateMilestoneCard(milestones.transform, "Win Tournament", "Trophy Avatar", false);
        }

        private void CreateMilestoneCard(Transform parent, string milestone, string reward, bool claimed)
        {
            GameObject card = new GameObject($"Milestone_{milestone}");
            card.transform.SetParent(parent, false);
            
            Image bg = card.AddComponent<Image>();
            bg.color = claimed ? new Color(0.1f, 0.15f, 0.1f) : new Color(0.1f, 0.08f, 0.12f);
            
            if (claimed)
            {
                UnityEngine.UI.Outline outline = card.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = new Color(0.3f, 0.8f, 0.3f);
                outline.effectDistance = new Vector2(1, 1);
            }
            
            VerticalLayoutGroup vlayout = card.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(5, 5, 5, 5);
            
            CreateText(card.transform, milestone, 10, TextAlignmentOptions.Center, Color.white);
            CreateText(card.transform, reward, 9, TextAlignmentOptions.Center, goldColor);
            CreateText(card.transform, claimed ? "[OK] Claimed" : "[L] Locked", 8, TextAlignmentOptions.Center, 
                      claimed ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.5f, 0.5f, 0.5f));
        }

        #region UI Helpers

        private void CreateSectionHeader(string text)
        {
            GameObject header = new GameObject("SectionHeader");
            header.transform.SetParent(_contentArea.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            TextMeshProUGUI tmp = header.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 14;
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

        private Color GetRankColor(ArenaRank rank)
        {
            return rank switch
            {
                ArenaRank.Bronze => bronzeColor,
                ArenaRank.Silver => silverColor,
                ArenaRank.Gold => goldColor,
                ArenaRank.Platinum1 or ArenaRank.Platinum2 => new Color(0.3f, 0.8f, 0.8f),
                ArenaRank.Diamond1 or ArenaRank.Diamond2 => new Color(0.4f, 0.7f, 1f),
                ArenaRank.Master => new Color(0.8f, 0.4f, 0.9f),
                ArenaRank.Grandmaster => championColor,
                _ => Color.white
            };
        }

        private string GetRankIcon(ArenaRank rank)
        {
            return rank switch
            {
                ArenaRank.Bronze => GameIcons.BronzeMedal,
                ArenaRank.Silver => GameIcons.SilverMedal,
                ArenaRank.Gold => GameIcons.GoldMedal,
                ArenaRank.Platinum1 or ArenaRank.Platinum2 => GameIcons.Gems,
                ArenaRank.Diamond1 or ArenaRank.Diamond2 => GameIcons.Gems,
                ArenaRank.Master => GameIcons.Crown,
                ArenaRank.Grandmaster => GameIcons.Trophy,
                _ => GameIcons.CrossedSwords
            };
        }

        private string GetRankRatingRange(ArenaRank rank)
        {
            return rank switch
            {
                ArenaRank.Bronze => "0-1199",
                ArenaRank.Silver => "1200-1499",
                ArenaRank.Gold => "1500-1799",
                ArenaRank.Platinum1 => "1800-1999",
                ArenaRank.Platinum2 => "1800-1999",
                ArenaRank.Diamond1 => "2000-2299",
                ArenaRank.Diamond2 => "2000-2299",
                ArenaRank.Master => "2300-2599",
                ArenaRank.Grandmaster => "2600+",
                _ => ""
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

        private void SelectTab(ArenaTab tab)
        {
            _selectedTab = tab;
            
            foreach (var kvp in _tabs)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == tab ? accentColor : new Color(0.15f, 0.1f, 0.18f);
            }
            
            // Remove old layout
            if (_contentArea.GetComponent<VerticalLayoutGroup>() != null)
                Destroy(_contentArea.GetComponent<VerticalLayoutGroup>());
            
            RefreshContent();
        }

        private void SelectMode(ArenaMode mode)
        {
            _selectedMode = mode;
            RefreshContent();
        }

        private void StartMatchmaking()
        {
            _isSearching = true;
            _searchTime = 0;
            RefreshContent();
            
            ApexLogger.Log($"[PvPArena] Started matchmaking for {_selectedMode}", ApexLogger.LogCategory.UI);
        }

        private void CancelMatchmaking()
        {
            _isSearching = false;
            
            // Only refresh if content area exists
            if (_contentArea != null)
            {
                RefreshContent();
            }
            
            ApexLogger.Log("[PvPArena] Cancelled matchmaking", ApexLogger.LogCategory.UI);
        }

        private void FindMatch()
        {
            _isSearching = false;
            
            var opponent = _matchmaking[UnityEngine.Random.Range(0, _matchmaking.Count)];
            
            OnMatchFound?.Invoke(opponent);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowAlert("Match Found!", $"Opponent: {opponent.Name} ({opponent.Rating})");
            }
            
            ApexLogger.Log($"[PvPArena] Match found! Opponent: {opponent.Name}", ApexLogger.LogCategory.UI);
            
            RefreshContent();
        }

        private void JoinTournament(Tournament tournament)
        {
            tournament.IsRegistered = true;
            tournament.Participants++;
            
            OnTournamentJoined?.Invoke(tournament);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"Joined {tournament.Name}!");
            }
            
            RefreshContent();
        }

        private void PlayTournamentMatch(Tournament tournament)
        {
            ApexLogger.Log($"[PvPArena] Playing tournament match: {tournament.Name}", ApexLogger.LogCategory.UI);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo("Loading tournament match...");
            }
        }

        private void PurchaseItem(string name, int cost)
        {
            if (_playerStats.TotalArenaPoints >= cost)
            {
                _playerStats.TotalArenaPoints -= cost;
                
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowSuccess($"Purchased {name}!");
                }
                
                RefreshContent();
            }
        }

        #endregion

        #region Public API

        public void Show()
        {
            if (_panel != null)
            {
                _panel.SetActive(true);
                RefreshContent();
            }
        }

        public void Hide()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
            // Only cancel matchmaking if we were actually searching
            if (_isSearching)
            {
                CancelMatchmaking();
            }
        }

        public void Toggle()
        {
            if (_panel.activeSelf)
                Hide();
            else
                Show();
        }

        public ArenaPlayerStats GetPlayerStats() => _playerStats;
        public bool IsSearching => _isSearching;

        #endregion
    }

    #region Data Classes

    public enum ArenaTab
    {
        QuickMatch,
        Ranked,
        Tournaments,
        Leaderboard,
        History,
        Rewards
    }

    public enum ArenaMode
    {
        Ranked1v1,
        Casual,
        Draft,
        Challenge
    }

    public enum ArenaRank
    {
        Bronze,
        Silver,
        Gold,
        Platinum2,
        Platinum1,
        Diamond2,
        Diamond1,
        Master,
        Grandmaster
    }

    public enum TournamentStatus
    {
        Registering,
        InProgress,
        Completed
    }

    public class ArenaPlayerStats
    {
        public int Rating;
        public ArenaRank Rank;
        public int Wins;
        public int Losses;
        public int WinStreak;
        public int BestStreak;
        public int SeasonWins;
        public int SeasonRank;
        public int TotalMatches;
        public string Division;
        public int NextRankPoints;
        public int TotalArenaPoints;
    }

    public class RankedMatch
    {
        public string MatchId;
        public string OpponentName;
        public int OpponentRating;
        public bool Won;
        public int RatingChange;
        public int Duration;
        public ArenaMode Mode;
        public DateTime Timestamp;
    }

    public class ArenaOpponent
    {
        public string PlayerId;
        public string Name;
        public int Rating;
        public int Level;
        public float WinRate;
        public string Guild;
    }

    public class Tournament
    {
        public string TournamentId;
        public string Name;
        public string Description;
        public int EntryFee;
        public int PrizePool;
        public int Participants;
        public int MaxParticipants;
        public int CurrentRound;
        public TournamentStatus Status;
        public DateTime StartTime;
        public string Format;
        public int MinRating;
        public int MaxRating;
        public bool IsRegistered;
        public int CurrentPosition;
    }

    public class ArenaLeaderboardEntry
    {
        public int Rank;
        public string PlayerName;
        public int Rating;
        public int Wins;
        public int Losses;
        public ArenaRank Rank_Title;
    }

    #endregion
}
