using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Tournament System - Competitive bracket tournaments with prizes.
    /// PvP competitions with rankings, brackets, and spectating.
    /// 
    /// Features:
    /// - Tournament browsing/registration
    /// - Bracket visualization
    /// - Match scheduling
    /// - Live match spectating
    /// - Leaderboards
    /// - Prize pools
    /// - Tournament history
    /// - Auto-matching
    /// </summary>
    public class TournamentPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.85f, 0.5f, 0.2f);
        [SerializeField] private Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color silverColor = new Color(0.75f, 0.75f, 0.8f);
        [SerializeField] private Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        [SerializeField] private Color winColor = new Color(0.3f, 0.7f, 0.3f);
        [SerializeField] private Color loseColor = new Color(0.7f, 0.3f, 0.3f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _tournamentListContainer;
        private GameObject _bracketContainer;
        private GameObject _matchDetailsPanel;
        
        // Tabs
        private Button _activeTab;
        private Button _upcomingTab;
        private Button _historyTab;
        private Button _myMatchesTab;
        private TournamentViewTab _currentTab = TournamentViewTab.Active;
        
        // Tournament details
        private TextMeshProUGUI _tournamentTitle;
        private TextMeshProUGUI _prizePool;
        private TextMeshProUGUI _participantCount;
        private TextMeshProUGUI _status;
        private Button _registerButton;
        private Button _spectateButton;
        
        // State
        private List<TournamentInfo> _tournaments = new List<TournamentInfo>();
        private List<TournamentInfo> _filteredTournaments = new List<TournamentInfo>();
        private TournamentInfo _selectedTournament;
        private TournamentMatch _selectedMatch;
        
        public static TournamentPanel Instance { get; private set; }
        
        // Events
        public event Action<TournamentInfo> OnTournamentRegistered;
        public event Action<TournamentMatch> OnMatchStarted;
        public event Action<TournamentMatch> OnMatchCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            CreateUI();
            GenerateSampleTournaments();
            ShowTab(TournamentViewTab.Active);
            Hide();
        }

        private void CreateUI()
        {
            // Main panel
            _panel = new GameObject("TournamentPanel");
            _panel.transform.SetParent(transform);
            
            RectTransform panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0.08f);
            panelRect.anchorMax = new Vector2(0.92f, 0.92f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            Image panelBg = _panel.AddComponent<Image>();
            panelBg.color = panelColor;
            
            UnityEngine.UI.Outline outline = _panel.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(2, 2);
            
            CreateHeader();
            CreateTabBar();
            CreateMainContent();
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_panel.transform, false);
            
            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 60);
            
            Image headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.12f, 0.12f, 0.18f);
            
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(0.4f, 1);
            titleRect.offsetMin = new Vector2(25, 0);
            titleRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "🏆 TOURNAMENTS";
            title.fontSize = 26;
            title.fontStyle = FontStyles.Bold;
            title.color = accentColor;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Stats row
            CreateHeaderStats(header.transform);
            
            // Close button
            CreateCloseButton(header.transform);
        }

        private void CreateHeaderStats(Transform parent)
        {
            GameObject statsContainer = new GameObject("Stats");
            statsContainer.transform.SetParent(parent, false);
            
            RectTransform statsRect = statsContainer.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.4f, 0.2f);
            statsRect.anchorMax = new Vector2(0.9f, 0.8f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;
            
            HorizontalLayoutGroup hlayout = statsContainer.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 30;
            hlayout.childForceExpandWidth = true;
            
            CreateStatItem(statsContainer.transform, "🏅 Your Rank", "#42");
            CreateStatItem(statsContainer.transform, "🎯 Win Rate", "68%");
            CreateStatItem(statsContainer.transform, "🏆 Tournaments Won", "3");
            CreateStatItem(statsContainer.transform, "💰 Total Winnings", "15,420 Gold");
        }

        private void CreateStatItem(Transform parent, string label, string value)
        {
            GameObject statObj = new GameObject("Stat");
            statObj.transform.SetParent(parent, false);
            
            VerticalLayoutGroup vlayout = statObj.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 2;
            
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(statObj.transform, false);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 10;
            labelText.color = new Color(0.5f, 0.5f, 0.5f);
            labelText.alignment = TextAlignmentOptions.Center;
            
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(statObj.transform, false);
            TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = value;
            valueText.fontSize = 14;
            valueText.fontStyle = FontStyles.Bold;
            valueText.color = accentColor;
            valueText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(parent, false);
            
            RectTransform closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 0.5f);
            closeRect.anchorMax = new Vector2(1, 0.5f);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.anchoredPosition = new Vector2(-15, 0);
            closeRect.sizeDelta = new Vector2(40, 40);
            
            Image closeBg = closeObj.AddComponent<Image>();
            closeBg.color = new Color(0.5f, 0.2f, 0.2f);
            
            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
            
            GameObject closeText = new GameObject("X");
            closeText.transform.SetParent(closeObj.transform, false);
            RectTransform xtRect = closeText.AddComponent<RectTransform>();
            xtRect.anchorMin = Vector2.zero;
            xtRect.anchorMax = Vector2.one;
            xtRect.offsetMin = Vector2.zero;
            xtRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI x = closeText.AddComponent<TextMeshProUGUI>();
            x.text = "✕";
            x.fontSize = 22;
            x.color = Color.white;
            x.alignment = TextAlignmentOptions.Center;
        }

        private void CreateTabBar()
        {
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(_panel.transform, false);
            
            RectTransform tabRect = tabBar.AddComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0, 1);
            tabRect.anchorMax = new Vector2(1, 1);
            tabRect.pivot = new Vector2(0.5f, 1);
            tabRect.anchoredPosition = new Vector2(0, -60);
            tabRect.sizeDelta = new Vector2(0, 45);
            
            Image tabBg = tabBar.AddComponent<Image>();
            tabBg.color = new Color(0.08f, 0.08f, 0.12f);
            
            HorizontalLayoutGroup hlayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 5;
            hlayout.padding = new RectOffset(15, 15, 6, 6);
            hlayout.childForceExpandWidth = true;
            
            _activeTab = CreateTabButton(tabBar.transform, "🔴 Active", () => ShowTab(TournamentViewTab.Active));
            _upcomingTab = CreateTabButton(tabBar.transform, "📅 Upcoming", () => ShowTab(TournamentViewTab.Upcoming));
            _myMatchesTab = CreateTabButton(tabBar.transform, "⚔️ My Matches", () => ShowTab(TournamentViewTab.MyMatches));
            _historyTab = CreateTabButton(tabBar.transform, "📜 History", () => ShowTab(TournamentViewTab.History));
            
            UpdateTabHighlights();
        }

        private Button CreateTabButton(Transform parent, string label, Action onClick)
        {
            GameObject tabObj = new GameObject(label);
            tabObj.transform.SetParent(parent, false);
            
            Image bg = tabObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f);
            
            Button btn = tabObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            
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
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            return btn;
        }

        private void CreateMainContent()
        {
            GameObject content = new GameObject("MainContent");
            content.transform.SetParent(_panel.transform, false);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(10, 10);
            contentRect.offsetMax = new Vector2(-10, -115);
            
            // Left: Tournament list
            CreateTournamentList(content.transform);
            
            // Right: Details / Bracket
            CreateDetailsPanel(content.transform);
        }

        private void CreateTournamentList(Transform parent)
        {
            GameObject listPanel = new GameObject("TournamentList");
            listPanel.transform.SetParent(parent, false);
            
            RectTransform listRect = listPanel.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(0.35f, 1);
            listRect.offsetMin = Vector2.zero;
            listRect.offsetMax = new Vector2(-5, 0);
            
            Image listBg = listPanel.AddComponent<Image>();
            listBg.color = new Color(0.08f, 0.08f, 0.1f);
            
            // Scroll view
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(listPanel.transform, false);
            
            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(5, 5);
            scrollRect.offsetMax = new Vector2(-5, -5);
            
            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30;
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform viewRect = viewport.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = Vector2.zero;
            viewRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = viewRect;
            
            // Content
            _tournamentListContainer = new GameObject("Content");
            _tournamentListContainer.transform.SetParent(viewport.transform, false);
            RectTransform containerRect = _tournamentListContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = Vector2.zero;
            
            VerticalLayoutGroup vlayout = _tournamentListContainer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(5, 5, 5, 5);
            
            ContentSizeFitter fitter = _tournamentListContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = containerRect;
        }

        private void CreateDetailsPanel(Transform parent)
        {
            _matchDetailsPanel = new GameObject("DetailsPanel");
            _matchDetailsPanel.transform.SetParent(parent, false);
            
            RectTransform detailsRect = _matchDetailsPanel.AddComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(0.35f, 0);
            detailsRect.anchorMax = new Vector2(1, 1);
            detailsRect.offsetMin = new Vector2(5, 0);
            detailsRect.offsetMax = Vector2.zero;
            
            Image detailsBg = _matchDetailsPanel.AddComponent<Image>();
            detailsBg.color = new Color(0.06f, 0.06f, 0.09f);
            
            VerticalLayoutGroup vlayout = _matchDetailsPanel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 15;
            vlayout.padding = new RectOffset(20, 20, 20, 20);
            
            // Tournament title
            GameObject titleObj = new GameObject("TournamentTitle");
            titleObj.transform.SetParent(_matchDetailsPanel.transform, false);
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 35;
            _tournamentTitle = titleObj.AddComponent<TextMeshProUGUI>();
            _tournamentTitle.text = "Select a Tournament";
            _tournamentTitle.fontSize = 22;
            _tournamentTitle.fontStyle = FontStyles.Bold;
            _tournamentTitle.color = accentColor;
            _tournamentTitle.alignment = TextAlignmentOptions.Center;
            
            // Info row
            CreateInfoRow(_matchDetailsPanel.transform);
            
            // Prize display
            CreatePrizeDisplay(_matchDetailsPanel.transform);
            
            // Bracket area
            CreateBracketArea(_matchDetailsPanel.transform);
            
            // Action buttons
            CreateActionButtons(_matchDetailsPanel.transform);
        }

        private void CreateInfoRow(Transform parent)
        {
            GameObject infoRow = new GameObject("InfoRow");
            infoRow.transform.SetParent(parent, false);
            
            LayoutElement le = infoRow.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            HorizontalLayoutGroup hlayout = infoRow.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 30;
            hlayout.childForceExpandWidth = true;
            
            // Status
            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(infoRow.transform, false);
            _status = statusObj.AddComponent<TextMeshProUGUI>();
            _status.text = "---";
            _status.fontSize = 14;
            _status.color = new Color(0.6f, 0.6f, 0.6f);
            _status.alignment = TextAlignmentOptions.Center;
            
            // Participants
            GameObject participantsObj = new GameObject("Participants");
            participantsObj.transform.SetParent(infoRow.transform, false);
            _participantCount = participantsObj.AddComponent<TextMeshProUGUI>();
            _participantCount.text = "---";
            _participantCount.fontSize = 14;
            _participantCount.color = new Color(0.6f, 0.6f, 0.6f);
            _participantCount.alignment = TextAlignmentOptions.Center;
        }

        private void CreatePrizeDisplay(Transform parent)
        {
            GameObject prizeContainer = new GameObject("PrizeContainer");
            prizeContainer.transform.SetParent(parent, false);
            
            LayoutElement le = prizeContainer.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Image bg = prizeContainer.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.14f);
            
            VerticalLayoutGroup vlayout = prizeContainer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Prize header
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(prizeContainer.transform, false);
            TextMeshProUGUI header = headerObj.AddComponent<TextMeshProUGUI>();
            header.text = "💰 PRIZE POOL";
            header.fontSize = 12;
            header.color = new Color(0.5f, 0.5f, 0.5f);
            header.alignment = TextAlignmentOptions.Center;
            
            // Prize amount
            GameObject prizeObj = new GameObject("Prize");
            prizeObj.transform.SetParent(prizeContainer.transform, false);
            _prizePool = prizeObj.AddComponent<TextMeshProUGUI>();
            _prizePool.text = "---";
            _prizePool.fontSize = 28;
            _prizePool.fontStyle = FontStyles.Bold;
            _prizePool.color = goldColor;
            _prizePool.alignment = TextAlignmentOptions.Center;
            
            // Prize breakdown
            GameObject breakdownObj = new GameObject("Breakdown");
            breakdownObj.transform.SetParent(prizeContainer.transform, false);
            TextMeshProUGUI breakdown = breakdownObj.AddComponent<TextMeshProUGUI>();
            breakdown.text = "🥇 50% | 🥈 30% | 🥉 20%";
            breakdown.fontSize = 11;
            breakdown.color = new Color(0.5f, 0.5f, 0.5f);
            breakdown.alignment = TextAlignmentOptions.Center;
        }

        private void CreateBracketArea(Transform parent)
        {
            _bracketContainer = new GameObject("BracketArea");
            _bracketContainer.transform.SetParent(parent, false);
            
            LayoutElement le = _bracketContainer.AddComponent<LayoutElement>();
            le.preferredHeight = 250;
            le.flexibleHeight = 1;
            
            Image bg = _bracketContainer.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.07f, 0.1f);
            
            // Placeholder text
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(_bracketContainer.transform, false);
            RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = "🏆\n\nSelect a tournament to view bracket";
            placeholder.fontSize = 16;
            placeholder.color = new Color(0.4f, 0.4f, 0.4f);
            placeholder.alignment = TextAlignmentOptions.Center;
        }

        private void CreateActionButtons(Transform parent)
        {
            GameObject actionsRow = new GameObject("Actions");
            actionsRow.transform.SetParent(parent, false);
            
            LayoutElement le = actionsRow.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            HorizontalLayoutGroup hlayout = actionsRow.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.childForceExpandWidth = true;
            
            _registerButton = CreateActionButton(actionsRow.transform, "📝 Register", OnRegisterClicked, accentColor);
            _spectateButton = CreateActionButton(actionsRow.transform, "👁️ Spectate", OnSpectateClicked, new Color(0.3f, 0.4f, 0.6f));
            
            _registerButton.interactable = false;
            _spectateButton.interactable = false;
        }

        private Button CreateActionButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btnObj = new GameObject(label);
            btnObj.transform.SetParent(parent, false);
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = color;
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            
            var colors = btn.colors;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.25f);
            btn.colors = colors;
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 16;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            return btn;
        }

        #region Data & Logic

        private void GenerateSampleTournaments()
        {
            // Active tournaments
            _tournaments.Add(new TournamentInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Grand Champion's Cup",
                Type = TournamentType.SingleElimination,
                Status = TournamentPanelStatus.InProgress,
                PrizePool = 50000,
                EntryFee = 500,
                MaxParticipants = 64,
                CurrentParticipants = 64,
                StartTime = DateTime.Now.AddHours(-2),
                CurrentRound = 4,
                Description = "The most prestigious tournament of the season. Only the strongest will survive!"
            });
            
            _tournaments.Add(new TournamentInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Weekly Warrior Clash",
                Type = TournamentType.SingleElimination,
                Status = TournamentPanelStatus.InProgress,
                PrizePool = 10000,
                EntryFee = 100,
                MaxParticipants = 32,
                CurrentParticipants = 32,
                StartTime = DateTime.Now.AddHours(-1),
                CurrentRound = 2,
                Description = "Weekly competitive tournament for all skill levels."
            });
            
            // Upcoming
            _tournaments.Add(new TournamentInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Beginner's Battle",
                Type = TournamentType.SingleElimination,
                Status = TournamentPanelStatus.Registration,
                PrizePool = 5000,
                EntryFee = 50,
                MaxParticipants = 16,
                CurrentParticipants = 12,
                StartTime = DateTime.Now.AddHours(3),
                Description = "Perfect for newcomers! Level cap: 20"
            });
            
            _tournaments.Add(new TournamentInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Alliance Wars Championship",
                Type = TournamentType.DoubleElimination,
                Status = TournamentPanelStatus.Registration,
                PrizePool = 100000,
                EntryFee = 1000,
                MaxParticipants = 128,
                CurrentParticipants = 87,
                StartTime = DateTime.Now.AddDays(1),
                Description = "Team-based alliance tournament. Form your alliances!"
            });
            
            // Completed
            _tournaments.Add(new TournamentInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Spring Showdown 2024",
                Type = TournamentType.SingleElimination,
                Status = TournamentPanelStatus.Completed,
                PrizePool = 75000,
                EntryFee = 750,
                MaxParticipants = 64,
                CurrentParticipants = 64,
                StartTime = DateTime.Now.AddDays(-3),
                Winner = "DragonSlayer99",
                Description = "Season finale tournament."
            });
            
            // Generate sample brackets for active tournaments
            foreach (var t in _tournaments.Where(t => t.Status == TournamentPanelStatus.InProgress))
            {
                GenerateSampleBracket(t);
            }
        }

        private void GenerateSampleBracket(TournamentInfo tournament)
        {
            tournament.Matches = new List<TournamentMatch>();
            
            string[] playerNames = {
                "DragonSlayer99", "ShadowWarrior", "IronFist", "StormBringer",
                "NightHunter", "FireMage", "IceQueen", "ThunderGod",
                "DarkKnight", "LightBringer", "DeathDealer", "LifeGiver",
                "WindWalker", "EarthShaker", "WaterBender", "FlameKeeper"
            };
            
            // Round 1 matches
            for (int i = 0; i < 8; i++)
            {
                tournament.Matches.Add(new TournamentMatch
                {
                    MatchId = $"R1-M{i + 1}",
                    Round = 1,
                    Player1 = playerNames[i * 2],
                    Player2 = playerNames[i * 2 + 1],
                    Winner = playerNames[i * 2 + UnityEngine.Random.Range(0, 2)],
                    Score1 = UnityEngine.Random.Range(0, 3),
                    Score2 = UnityEngine.Random.Range(0, 3),
                    Status = MatchStatus.Completed
                });
            }
            
            // Generate subsequent rounds based on winners
            var previousWinners = tournament.Matches.Where(m => m.Round == 1).Select(m => m.Winner).ToList();
            int round = 2;
            
            while (previousWinners.Count > 1)
            {
                var currentMatches = new List<TournamentMatch>();
                for (int i = 0; i < previousWinners.Count / 2; i++)
                {
                    var match = new TournamentMatch
                    {
                        MatchId = $"R{round}-M{i + 1}",
                        Round = round,
                        Player1 = previousWinners[i * 2],
                        Player2 = previousWinners[i * 2 + 1]
                    };
                    
                    if (round < tournament.CurrentRound)
                    {
                        match.Winner = previousWinners[i * 2 + UnityEngine.Random.Range(0, 2)];
                        match.Score1 = UnityEngine.Random.Range(0, 3);
                        match.Score2 = UnityEngine.Random.Range(0, 3);
                        match.Status = MatchStatus.Completed;
                    }
                    else if (round == tournament.CurrentRound)
                    {
                        match.Status = MatchStatus.InProgress;
                        match.Score1 = UnityEngine.Random.Range(0, 2);
                        match.Score2 = UnityEngine.Random.Range(0, 2);
                    }
                    else
                    {
                        match.Status = MatchStatus.Pending;
                    }
                    
                    tournament.Matches.Add(match);
                    currentMatches.Add(match);
                }
                
                previousWinners = currentMatches
                    .Where(m => m.Status == MatchStatus.Completed)
                    .Select(m => m.Winner)
                    .ToList();
                
                if (currentMatches.Any(m => m.Status != MatchStatus.Completed))
                    break;
                    
                round++;
            }
        }

        private void ShowTab(TournamentViewTab tab)
        {
            _currentTab = tab;
            UpdateTabHighlights();
            RefreshTournamentList();
        }

        private void UpdateTabHighlights()
        {
            SetTabHighlight(_activeTab, _currentTab == TournamentViewTab.Active);
            SetTabHighlight(_upcomingTab, _currentTab == TournamentViewTab.Upcoming);
            SetTabHighlight(_myMatchesTab, _currentTab == TournamentViewTab.MyMatches);
            SetTabHighlight(_historyTab, _currentTab == TournamentViewTab.History);
        }

        private void SetTabHighlight(Button tab, bool active)
        {
            if (tab == null) return;
            var image = tab.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? accentColor : new Color(0.12f, 0.12f, 0.18f);
            }
        }

        private void RefreshTournamentList()
        {
            foreach (Transform child in _tournamentListContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            _filteredTournaments = _currentTab switch
            {
                TournamentViewTab.Active => _tournaments.Where(t => t.Status == TournamentPanelStatus.InProgress).ToList(),
                TournamentViewTab.Upcoming => _tournaments.Where(t => t.Status == TournamentPanelStatus.Registration).ToList(),
                TournamentViewTab.History => _tournaments.Where(t => t.Status == TournamentPanelStatus.Completed).ToList(),
                TournamentViewTab.MyMatches => _tournaments.Where(t => t.IsRegistered).ToList(),
                _ => _tournaments
            };
            
            foreach (var tournament in _filteredTournaments)
            {
                CreateTournamentRow(tournament);
            }
        }

        private void CreateTournamentRow(TournamentInfo tournament)
        {
            GameObject row = new GameObject($"Tournament_{tournament.Id}");
            row.transform.SetParent(_tournamentListContainer.transform, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Image bg = row.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.14f);
            
            Button btn = row.AddComponent<Button>();
            var tournament_copy = tournament;
            btn.onClick.AddListener(() => SelectTournament(tournament_copy));
            
            VerticalLayoutGroup vlayout = row.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperLeft;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 3;
            vlayout.padding = new RectOffset(10, 10, 8, 8);
            
            // Title row
            GameObject titleRow = new GameObject("TitleRow");
            titleRow.transform.SetParent(row.transform, false);
            LayoutElement titleLE = titleRow.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 22;
            
            HorizontalLayoutGroup titleHL = titleRow.AddComponent<HorizontalLayoutGroup>();
            titleHL.childAlignment = TextAnchor.MiddleLeft;
            
            // Status indicator
            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(titleRow.transform, false);
            LayoutElement statusLE = statusObj.AddComponent<LayoutElement>();
            statusLE.preferredWidth = 20;
            TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.text = GetStatusIcon(tournament.Status);
            statusText.fontSize = 12;
            
            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(titleRow.transform, false);
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;
            TextMeshProUGUI name = nameObj.AddComponent<TextMeshProUGUI>();
            name.text = tournament.Name;
            name.fontSize = 14;
            name.fontStyle = FontStyles.Bold;
            name.color = Color.white;
            
            // Info row
            GameObject infoRow = new GameObject("InfoRow");
            infoRow.transform.SetParent(row.transform, false);
            LayoutElement infoLE = infoRow.AddComponent<LayoutElement>();
            infoLE.preferredHeight = 18;
            TextMeshProUGUI info = infoRow.AddComponent<TextMeshProUGUI>();
            info.text = $"💰 {tournament.PrizePool:N0} Gold  |  👥 {tournament.CurrentParticipants}/{tournament.MaxParticipants}";
            info.fontSize = 11;
            info.color = new Color(0.6f, 0.6f, 0.6f);
            
            // Time row
            GameObject timeRow = new GameObject("TimeRow");
            timeRow.transform.SetParent(row.transform, false);
            LayoutElement timeLE = timeRow.AddComponent<LayoutElement>();
            timeLE.preferredHeight = 16;
            TextMeshProUGUI time = timeRow.AddComponent<TextMeshProUGUI>();
            time.text = GetTimeDisplay(tournament);
            time.fontSize = 10;
            time.color = tournament.Status == TournamentPanelStatus.InProgress ? winColor : new Color(0.5f, 0.5f, 0.5f);
        }

        private void SelectTournament(TournamentInfo tournament)
        {
            _selectedTournament = tournament;
            
            _tournamentTitle.text = $"🏆 {tournament.Name}";
            _prizePool.text = $"{tournament.PrizePool:N0} Gold";
            _status.text = $"Status: {tournament.Status}";
            _participantCount.text = $"👥 {tournament.CurrentParticipants}/{tournament.MaxParticipants}";
            
            // Update buttons
            _registerButton.interactable = tournament.Status == TournamentPanelStatus.Registration && !tournament.IsRegistered;
            _spectateButton.interactable = tournament.Status == TournamentPanelStatus.InProgress;
            
            // Update bracket
            RefreshBracket(tournament);
        }

        private void RefreshBracket(TournamentInfo tournament)
        {
            // Clear existing bracket
            foreach (Transform child in _bracketContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            if (tournament.Matches == null || tournament.Matches.Count == 0)
            {
                // Show placeholder
                GameObject phObj = new GameObject("Placeholder");
                phObj.transform.SetParent(_bracketContainer.transform, false);
                RectTransform phRect = phObj.AddComponent<RectTransform>();
                phRect.anchorMin = Vector2.zero;
                phRect.anchorMax = Vector2.one;
                phRect.offsetMin = Vector2.zero;
                phRect.offsetMax = Vector2.zero;
                
                TextMeshProUGUI ph = phObj.AddComponent<TextMeshProUGUI>();
                ph.text = tournament.Status == TournamentPanelStatus.Registration 
                    ? "🏆\n\nBracket will be generated when tournament starts" 
                    : "🏆\n\nNo bracket data available";
                ph.fontSize = 14;
                ph.color = new Color(0.4f, 0.4f, 0.4f);
                ph.alignment = TextAlignmentOptions.Center;
                return;
            }
            
            // Create simple text-based bracket display
            GameObject bracketScroll = new GameObject("BracketScroll");
            bracketScroll.transform.SetParent(_bracketContainer.transform, false);
            
            RectTransform scrollRect = bracketScroll.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(10, 10);
            scrollRect.offsetMax = new Vector2(-10, -10);
            
            ScrollRect scroll = bracketScroll.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = true;
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(bracketScroll.transform, false);
            RectTransform viewRect = viewport.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = Vector2.zero;
            viewRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = viewRect;
            
            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.anchoredPosition = Vector2.zero;
            
            HorizontalLayoutGroup hlayout = content.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.UpperLeft;
            hlayout.childForceExpandWidth = false;
            hlayout.childForceExpandHeight = true;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(10, 10, 10, 10);
            
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = contentRect;
            
            // Group matches by round
            var rounds = tournament.Matches.GroupBy(m => m.Round).OrderBy(g => g.Key);
            
            foreach (var round in rounds)
            {
                CreateBracketRound(content.transform, round.Key, round.ToList());
            }
        }

        private void CreateBracketRound(Transform parent, int roundNumber, List<TournamentMatch> matches)
        {
            GameObject roundObj = new GameObject($"Round{roundNumber}");
            roundObj.transform.SetParent(parent, false);
            
            LayoutElement le = roundObj.AddComponent<LayoutElement>();
            le.preferredWidth = 180;
            
            VerticalLayoutGroup vlayout = roundObj.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(0, 0, 5, 5);
            
            // Round header
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(roundObj.transform, false);
            LayoutElement headerLE = headerObj.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 25;
            TextMeshProUGUI header = headerObj.AddComponent<TextMeshProUGUI>();
            header.text = GetRoundName(roundNumber, matches.Count);
            header.fontSize = 12;
            header.fontStyle = FontStyles.Bold;
            header.color = accentColor;
            header.alignment = TextAlignmentOptions.Center;
            
            // Matches
            foreach (var match in matches)
            {
                CreateMatchDisplay(roundObj.transform, match);
            }
        }

        private void CreateMatchDisplay(Transform parent, TournamentMatch match)
        {
            GameObject matchObj = new GameObject($"Match_{match.MatchId}");
            matchObj.transform.SetParent(parent, false);
            
            LayoutElement le = matchObj.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            Image bg = matchObj.AddComponent<Image>();
            bg.color = match.Status == MatchStatus.InProgress 
                ? new Color(0.2f, 0.25f, 0.3f) 
                : new Color(0.1f, 0.1f, 0.12f);
            
            VerticalLayoutGroup vlayout = matchObj.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperLeft;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 2;
            vlayout.padding = new RectOffset(8, 8, 5, 5);
            
            // Player 1
            CreatePlayerRow(matchObj.transform, match.Player1, match.Score1, match.Winner == match.Player1, match.Status);
            
            // Player 2
            CreatePlayerRow(matchObj.transform, match.Player2, match.Score2, match.Winner == match.Player2, match.Status);
        }

        private void CreatePlayerRow(Transform parent, string playerName, int score, bool isWinner, MatchStatus status)
        {
            GameObject row = new GameObject("PlayerRow");
            row.transform.SetParent(parent, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 18;
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            
            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(row.transform, false);
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;
            TextMeshProUGUI name = nameObj.AddComponent<TextMeshProUGUI>();
            name.text = string.IsNullOrEmpty(playerName) ? "TBD" : playerName;
            name.fontSize = 11;
            
            if (status == MatchStatus.Completed)
            {
                name.color = isWinner ? winColor : new Color(0.4f, 0.4f, 0.4f);
                name.fontStyle = isWinner ? FontStyles.Bold : FontStyles.Normal;
            }
            else
            {
                name.color = string.IsNullOrEmpty(playerName) ? new Color(0.3f, 0.3f, 0.3f) : Color.white;
            }
            
            // Score
            if (status != MatchStatus.Pending)
            {
                GameObject scoreObj = new GameObject("Score");
                scoreObj.transform.SetParent(row.transform, false);
                LayoutElement scoreLE = scoreObj.AddComponent<LayoutElement>();
                scoreLE.preferredWidth = 25;
                TextMeshProUGUI scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
                scoreText.text = score.ToString();
                scoreText.fontSize = 11;
                scoreText.fontStyle = FontStyles.Bold;
                scoreText.color = isWinner ? winColor : new Color(0.5f, 0.5f, 0.5f);
                scoreText.alignment = TextAlignmentOptions.MidlineRight;
            }
        }

        private void OnRegisterClicked()
        {
            if (_selectedTournament == null) return;
            
            // Check entry fee
            // In real implementation, check player's gold
            
            _selectedTournament.IsRegistered = true;
            _selectedTournament.CurrentParticipants++;
            
            _registerButton.interactable = false;
            NotificationSystem.Instance?.ShowSuccess($"Registered for {_selectedTournament.Name}!");
            OnTournamentRegistered?.Invoke(_selectedTournament);
            
            RefreshTournamentList();
        }

        private void OnSpectateClicked()
        {
            if (_selectedTournament == null) return;
            
            var liveMatch = _selectedTournament.Matches?.FirstOrDefault(m => m.Status == MatchStatus.InProgress);
            if (liveMatch != null)
            {
                NotificationSystem.Instance?.ShowInfo($"Spectating: {liveMatch.Player1} vs {liveMatch.Player2}");
                // In real implementation, would open spectator view
            }
        }

        #endregion

        #region Helpers

        private string GetStatusIcon(TournamentPanelStatus status)
        {
            return status switch
            {
                TournamentPanelStatus.Registration => "📅",
                TournamentPanelStatus.InProgress => "🔴",
                TournamentPanelStatus.Completed => "✅",
                TournamentPanelStatus.Cancelled => "❌",
                _ => "❓"
            };
        }

        private string GetTimeDisplay(TournamentInfo tournament)
        {
            return tournament.Status switch
            {
                TournamentPanelStatus.Registration => $"Starts: {FormatTimeUntil(tournament.StartTime)}",
                TournamentPanelStatus.InProgress => $"Round {tournament.CurrentRound} in progress",
                TournamentPanelStatus.Completed => $"Winner: {tournament.Winner}",
                _ => ""
            };
        }

        private string FormatTimeUntil(DateTime time)
        {
            var diff = time - DateTime.Now;
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} minutes";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hours";
            return $"{(int)diff.TotalDays} days";
        }

        private string GetRoundName(int round, int matchCount)
        {
            if (matchCount == 1) return "🏆 FINALS";
            if (matchCount == 2) return "Semi-Finals";
            if (matchCount == 4) return "Quarter-Finals";
            return $"Round {round}";
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel?.SetActive(true);
            ShowTab(TournamentViewTab.Active);
        }

        public void Hide()
        {
            _panel?.SetActive(false);
        }

        public void Toggle()
        {
            if (_panel != null)
            {
                if (_panel.activeSelf) Hide();
                else Show();
            }
        }

        #endregion
    }

    #region Data Classes

    public enum TournamentViewTab
    {
        Active,
        Upcoming,
        MyMatches,
        History
    }

    public enum TournamentType
    {
        SingleElimination,
        DoubleElimination,
        RoundRobin,
        Swiss
    }

    // Note: TournamentStatus enum is defined in PvPArenaPanel.cs
    // Using TournamentPanelStatus to avoid conflict
    public enum TournamentPanelStatus
    {
        Registration,
        InProgress,
        Completed,
        Cancelled
    }

    // Note: Tournament class is defined in PvPArenaPanel.cs
    // Using TournamentInfo to avoid conflict
    public class TournamentInfo
    {
        public string Id;
        public string Name;
        public string Description;
        public TournamentType Type;
        public TournamentPanelStatus Status;
        public int PrizePool;
        public int EntryFee;
        public int MaxParticipants;
        public int CurrentParticipants;
        public DateTime StartTime;
        public int CurrentRound;
        public string Winner;
        public bool IsRegistered;
        public List<TournamentMatch> Matches;
    }

    public class TournamentMatch
    {
        public string MatchId;
        public int Round;
        public string Player1;
        public string Player2;
        public string Winner;
        public int Score1;
        public int Score2;
        public MatchStatus Status;
        public DateTime ScheduledTime;
    }

    public enum MatchStatus
    {
        Pending,
        InProgress,
        Completed
    }

    #endregion
}
