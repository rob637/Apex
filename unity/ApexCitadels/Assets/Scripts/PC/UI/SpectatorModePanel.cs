using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Spectator Mode Panel - Watch live battles between players.
    /// Real-time battle viewing with commentary and controls.
    /// 
    /// Features:
    /// - Browse live battles
    /// - Featured matches
    /// - Free camera controls
    /// - Battle statistics
    /// - Mini-map overview
    /// - Chat integration
    /// - Recording/screenshot
    /// - Follow players
    /// </summary>
    public class SpectatorModePanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.9f, 0.3f, 0.3f);
        [SerializeField] private Color panelColor = new Color(0.08f, 0.08f, 0.12f, 0.9f);
        [SerializeField] private Color liveColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color highlightColor = new Color(0.3f, 0.5f, 0.7f);
        [SerializeField] private Color player1Color = new Color(0.3f, 0.5f, 0.9f);
        [SerializeField] private Color player2Color = new Color(0.9f, 0.4f, 0.3f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _browsePanel;
        private GameObject _watchPanel;
        private GameObject _liveBattlesContainer;
        
        // Browse panel
        private Button _featuredTab;
        private Button _liveTab;
        private Button _friendsTab;
        private SpectatorTab _currentTab = SpectatorTab.Featured;
        
        // Watch panel (spectating UI)
        private TextMeshProUGUI _player1Name;
        private TextMeshProUGUI _player2Name;
        private TextMeshProUGUI _player1Score;
        private TextMeshProUGUI _player2Score;
        private Slider _player1Health;
        private Slider _player2Health;
        private TextMeshProUGUI _battleTimer;
        private TextMeshProUGUI _viewerCount;
        private Button _followPlayer1Button;
        private Button _followPlayer2Button;
        private Button _freeCameraButton;
        private Button _exitButton;
        
        // Controls panel
        private Slider _playbackSpeed;
        private Button _pauseButton;
        private Button _screenshotButton;
        private Button _recordButton;
        
        // State
        private List<LiveBattle> _liveBattles = new List<LiveBattle>();
        private List<LiveBattle> _filteredBattles = new List<LiveBattle>();
        private LiveBattle _currentBattle;
        private SpectatorCameraMode _cameraMode = SpectatorCameraMode.Free;
        private bool _isPaused = false;
        private bool _isRecording = false;
        
        public static SpectatorModePanel Instance { get; private set; }
        
        // Events
        public event Action<LiveBattle> OnStartSpectating;
        public event Action OnStopSpectating;
        public event Action<string> OnFollowPlayer;
        public event Action OnScreenshotTaken;

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
            GenerateSampleBattles();
            ShowBrowsePanel();
            Hide();
        }

        private void Update()
        {
            if (_currentBattle != null && !_isPaused)
            {
                UpdateBattleState();
            }
        }

        private void CreateUI()
        {
            // Main panel
            _panel = new GameObject("SpectatorModePanel");
            _panel.transform.SetParent(transform);
            
            RectTransform panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // Browse panel (for selecting battles)
            CreateBrowsePanel();
            
            // Watch panel (spectating UI overlay)
            CreateWatchPanel();
        }

        private void CreateBrowsePanel()
        {
            _browsePanel = new GameObject("BrowsePanel");
            _browsePanel.transform.SetParent(_panel.transform, false);
            
            RectTransform browseRect = _browsePanel.AddComponent<RectTransform>();
            browseRect.anchorMin = new Vector2(0.2f, 0.15f);
            browseRect.anchorMax = new Vector2(0.8f, 0.85f);
            browseRect.offsetMin = Vector2.zero;
            browseRect.offsetMax = Vector2.zero;
            
            Image browseBg = _browsePanel.AddComponent<Image>();
            browseBg.color = panelColor;
            
            UnityEngine.UI.Outline outline = _browsePanel.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(2, 2);
            
            // Header
            CreateBrowseHeader();
            
            // Tabs
            CreateBrowseTabs();
            
            // Battle list
            CreateBattleList();
        }

        private void CreateBrowseHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_browsePanel.transform, false);
            
            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 60);
            
            Image headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.1f, 0.1f, 0.14f);
            
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.offsetMin = new Vector2(25, 0);
            titleRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "👁️ SPECTATOR MODE";
            title.fontSize = 24;
            title.fontStyle = FontStyles.Bold;
            title.color = accentColor;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Live indicator
            GameObject liveObj = new GameObject("LiveIndicator");
            liveObj.transform.SetParent(header.transform, false);
            
            RectTransform liveRect = liveObj.AddComponent<RectTransform>();
            liveRect.anchorMin = new Vector2(0.5f, 0.3f);
            liveRect.anchorMax = new Vector2(0.65f, 0.7f);
            liveRect.offsetMin = Vector2.zero;
            liveRect.offsetMax = Vector2.zero;
            
            Image liveBg = liveObj.AddComponent<Image>();
            liveBg.color = liveColor;
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(liveObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI liveText = textObj.AddComponent<TextMeshProUGUI>();
            liveText.text = "🔴 LIVE";
            liveText.fontSize = 14;
            liveText.fontStyle = FontStyles.Bold;
            liveText.color = Color.white;
            liveText.alignment = TextAlignmentOptions.Center;
            
            // Battle count
            GameObject countObj = new GameObject("Count");
            countObj.transform.SetParent(header.transform, false);
            
            RectTransform countRect = countObj.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.65f, 0);
            countRect.anchorMax = new Vector2(0.85f, 1);
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI count = countObj.AddComponent<TextMeshProUGUI>();
            count.text = "23 battles in progress";
            count.fontSize = 12;
            count.color = new Color(0.5f, 0.5f, 0.5f);
            count.alignment = TextAlignmentOptions.Center;
            
            // Close button
            CreateCloseButton(header.transform, () => Hide());
        }

        private void CreateCloseButton(Transform parent, Action onClick)
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
            closeBtn.onClick.AddListener(() => onClick());
            
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

        private void CreateBrowseTabs()
        {
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(_browsePanel.transform, false);
            
            RectTransform tabRect = tabBar.AddComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0, 1);
            tabRect.anchorMax = new Vector2(1, 1);
            tabRect.pivot = new Vector2(0.5f, 1);
            tabRect.anchoredPosition = new Vector2(0, -60);
            tabRect.sizeDelta = new Vector2(0, 45);
            
            Image tabBg = tabBar.AddComponent<Image>();
            tabBg.color = new Color(0.07f, 0.07f, 0.1f);
            
            HorizontalLayoutGroup hlayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(20, 20, 6, 6);
            hlayout.childForceExpandWidth = true;
            
            _featuredTab = CreateTabButton(tabBar.transform, "⭐ Featured", () => ShowTab(SpectatorTab.Featured));
            _liveTab = CreateTabButton(tabBar.transform, "🔴 All Live", () => ShowTab(SpectatorTab.Live));
            _friendsTab = CreateTabButton(tabBar.transform, "👥 Friends", () => ShowTab(SpectatorTab.Friends));
            
            UpdateTabHighlights();
        }

        private Button CreateTabButton(Transform parent, string label, Action onClick)
        {
            GameObject tabObj = new GameObject(label);
            tabObj.transform.SetParent(parent, false);
            
            Image bg = tabObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.14f);
            
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

        private void CreateBattleList()
        {
            GameObject listPanel = new GameObject("BattleList");
            listPanel.transform.SetParent(_browsePanel.transform, false);
            
            RectTransform listRect = listPanel.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(1, 1);
            listRect.offsetMin = new Vector2(10, 10);
            listRect.offsetMax = new Vector2(-10, -115);
            
            // Scroll view
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(listPanel.transform, false);
            
            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            
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
            _liveBattlesContainer = new GameObject("Content");
            _liveBattlesContainer.transform.SetParent(viewport.transform, false);
            RectTransform containerRect = _liveBattlesContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = Vector2.zero;
            
            VerticalLayoutGroup vlayout = _liveBattlesContainer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 8;
            vlayout.padding = new RectOffset(5, 5, 5, 5);
            
            ContentSizeFitter fitter = _liveBattlesContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = containerRect;
        }

        private void CreateWatchPanel()
        {
            _watchPanel = new GameObject("WatchPanel");
            _watchPanel.transform.SetParent(_panel.transform, false);
            
            RectTransform watchRect = _watchPanel.AddComponent<RectTransform>();
            watchRect.anchorMin = Vector2.zero;
            watchRect.anchorMax = Vector2.one;
            watchRect.offsetMin = Vector2.zero;
            watchRect.offsetMax = Vector2.zero;
            
            // Top HUD (player info)
            CreateTopHUD();
            
            // Bottom controls
            CreateBottomControls();
            
            // Side controls
            CreateSideControls();
            
            _watchPanel.SetActive(false);
        }

        private void CreateTopHUD()
        {
            GameObject topHUD = new GameObject("TopHUD");
            topHUD.transform.SetParent(_watchPanel.transform, false);
            
            RectTransform topRect = topHUD.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0.15f, 1);
            topRect.anchorMax = new Vector2(0.85f, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.anchoredPosition = new Vector2(0, -10);
            topRect.sizeDelta = new Vector2(0, 80);
            
            Image topBg = topHUD.AddComponent<Image>();
            topBg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
            
            // Player 1 side
            CreatePlayerHUD(topHUD.transform, true);
            
            // VS / Timer center
            CreateCenterInfo(topHUD.transform);
            
            // Player 2 side
            CreatePlayerHUD(topHUD.transform, false);
        }

        private void CreatePlayerHUD(Transform parent, bool isPlayer1)
        {
            GameObject playerHUD = new GameObject(isPlayer1 ? "Player1HUD" : "Player2HUD");
            playerHUD.transform.SetParent(parent, false);
            
            RectTransform playerRect = playerHUD.AddComponent<RectTransform>();
            if (isPlayer1)
            {
                playerRect.anchorMin = new Vector2(0, 0);
                playerRect.anchorMax = new Vector2(0.4f, 1);
            }
            else
            {
                playerRect.anchorMin = new Vector2(0.6f, 0);
                playerRect.anchorMax = new Vector2(1, 1);
            }
            playerRect.offsetMin = new Vector2(10, 10);
            playerRect.offsetMax = new Vector2(-10, -10);
            
            VerticalLayoutGroup vlayout = playerHUD.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = isPlayer1 ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;
            
            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(playerHUD.transform, false);
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 25;
            TextMeshProUGUI name = nameObj.AddComponent<TextMeshProUGUI>();
            name.text = isPlayer1 ? "Player 1" : "Player 2";
            name.fontSize = 18;
            name.fontStyle = FontStyles.Bold;
            name.color = isPlayer1 ? player1Color : player2Color;
            name.alignment = isPlayer1 ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            
            if (isPlayer1) _player1Name = name;
            else _player2Name = name;
            
            // Health bar
            GameObject healthObj = new GameObject("HealthBar");
            healthObj.transform.SetParent(playerHUD.transform, false);
            LayoutElement healthLE = healthObj.AddComponent<LayoutElement>();
            healthLE.preferredHeight = 15;
            
            Image healthBg = healthObj.AddComponent<Image>();
            healthBg.color = new Color(0.2f, 0.2f, 0.2f);
            
            Slider health = healthObj.AddComponent<Slider>();
            health.minValue = 0;
            health.maxValue = 100;
            health.value = 100;
            
            // Fill area
            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(healthObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(2, 2);
            fillAreaRect.offsetMax = new Vector2(-2, -2);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = isPlayer1 ? player1Color : player2Color;
            
            health.fillRect = fillRect;
            
            if (isPlayer1) _player1Health = health;
            else _player2Health = health;
            
            // Score
            GameObject scoreObj = new GameObject("Score");
            scoreObj.transform.SetParent(playerHUD.transform, false);
            LayoutElement scoreLE = scoreObj.AddComponent<LayoutElement>();
            scoreLE.preferredHeight = 20;
            TextMeshProUGUI score = scoreObj.AddComponent<TextMeshProUGUI>();
            score.text = "Score: 0";
            score.fontSize = 12;
            score.color = new Color(0.7f, 0.7f, 0.7f);
            score.alignment = isPlayer1 ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            
            if (isPlayer1) _player1Score = score;
            else _player2Score = score;
        }

        private void CreateCenterInfo(Transform parent)
        {
            GameObject center = new GameObject("CenterInfo");
            center.transform.SetParent(parent, false);
            
            RectTransform centerRect = center.AddComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.4f, 0);
            centerRect.anchorMax = new Vector2(0.6f, 1);
            centerRect.offsetMin = Vector2.zero;
            centerRect.offsetMax = Vector2.zero;
            
            VerticalLayoutGroup vlayout = center.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 3;
            
            // Timer
            GameObject timerObj = new GameObject("Timer");
            timerObj.transform.SetParent(center.transform, false);
            LayoutElement timerLE = timerObj.AddComponent<LayoutElement>();
            timerLE.preferredHeight = 30;
            _battleTimer = timerObj.AddComponent<TextMeshProUGUI>();
            _battleTimer.text = "05:00";
            _battleTimer.fontSize = 24;
            _battleTimer.fontStyle = FontStyles.Bold;
            _battleTimer.color = Color.white;
            _battleTimer.alignment = TextAlignmentOptions.Center;
            
            // VS
            GameObject vsObj = new GameObject("VS");
            vsObj.transform.SetParent(center.transform, false);
            LayoutElement vsLE = vsObj.AddComponent<LayoutElement>();
            vsLE.preferredHeight = 20;
            TextMeshProUGUI vs = vsObj.AddComponent<TextMeshProUGUI>();
            vs.text = "VS";
            vs.fontSize = 14;
            vs.fontStyle = FontStyles.Bold;
            vs.color = accentColor;
            vs.alignment = TextAlignmentOptions.Center;
            
            // Viewer count
            GameObject viewerObj = new GameObject("Viewers");
            viewerObj.transform.SetParent(center.transform, false);
            LayoutElement viewerLE = viewerObj.AddComponent<LayoutElement>();
            viewerLE.preferredHeight = 15;
            _viewerCount = viewerObj.AddComponent<TextMeshProUGUI>();
            _viewerCount.text = "👁️ 0 watching";
            _viewerCount.fontSize = 10;
            _viewerCount.color = new Color(0.5f, 0.5f, 0.5f);
            _viewerCount.alignment = TextAlignmentOptions.Center;
        }

        private void CreateBottomControls()
        {
            GameObject bottomPanel = new GameObject("BottomControls");
            bottomPanel.transform.SetParent(_watchPanel.transform, false);
            
            RectTransform bottomRect = bottomPanel.AddComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0.2f, 0);
            bottomRect.anchorMax = new Vector2(0.8f, 0);
            bottomRect.pivot = new Vector2(0.5f, 0);
            bottomRect.anchoredPosition = new Vector2(0, 10);
            bottomRect.sizeDelta = new Vector2(0, 50);
            
            Image bottomBg = bottomPanel.AddComponent<Image>();
            bottomBg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
            
            HorizontalLayoutGroup hlayout = bottomPanel.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(20, 20, 8, 8);
            hlayout.childForceExpandWidth = false;
            
            // Pause button
            _pauseButton = CreateControlButton(bottomPanel.transform, "⏸️", OnPauseClicked, 40);
            
            // Speed slider
            CreateSpeedSlider(bottomPanel.transform);
            
            // Screenshot
            _screenshotButton = CreateControlButton(bottomPanel.transform, "📷", OnScreenshotClicked, 40);
            
            // Record
            _recordButton = CreateControlButton(bottomPanel.transform, "🔴", OnRecordClicked, 40);
            
            // Exit
            _exitButton = CreateControlButton(bottomPanel.transform, "✕ Exit", OnExitSpectatorClicked, 70);
        }

        private Button CreateControlButton(Transform parent, string label, Action onClick, float width)
        {
            GameObject btnObj = new GameObject(label);
            btnObj.transform.SetParent(parent, false);
            
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 35;
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f);
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            
            // Text as child
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
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            return btn;
        }

        private void CreateSpeedSlider(Transform parent)
        {
            GameObject sliderContainer = new GameObject("SpeedSlider");
            sliderContainer.transform.SetParent(parent, false);
            
            LayoutElement le = sliderContainer.AddComponent<LayoutElement>();
            le.preferredWidth = 150;
            le.preferredHeight = 35;
            
            HorizontalLayoutGroup hlayout = sliderContainer.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 8;
            
            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(sliderContainer.transform, false);
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 30;
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "1x";
            label.fontSize = 12;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            
            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(sliderContainer.transform, false);
            LayoutElement sliderLE = sliderObj.AddComponent<LayoutElement>();
            sliderLE.flexibleWidth = 1;
            
            Image sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = new Color(0.2f, 0.2f, 0.25f);
            
            _playbackSpeed = sliderObj.AddComponent<Slider>();
            _playbackSpeed.minValue = 0.25f;
            _playbackSpeed.maxValue = 2f;
            _playbackSpeed.value = 1f;
            
            // Fill
            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(5, 5);
            fillAreaRect.offsetMax = new Vector2(-5, -5);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = accentColor;
            
            _playbackSpeed.fillRect = fillRect;
            
            _playbackSpeed.onValueChanged.AddListener((value) =>
            {
                label.text = $"{value:F1}x";
                Time.timeScale = value;
            });
        }

        private void CreateSideControls()
        {
            GameObject sidePanel = new GameObject("SideControls");
            sidePanel.transform.SetParent(_watchPanel.transform, false);
            
            RectTransform sideRect = sidePanel.AddComponent<RectTransform>();
            sideRect.anchorMin = new Vector2(0, 0.3f);
            sideRect.anchorMax = new Vector2(0, 0.7f);
            sideRect.pivot = new Vector2(0, 0.5f);
            sideRect.anchoredPosition = new Vector2(10, 0);
            sideRect.sizeDelta = new Vector2(120, 0);
            
            Image sideBg = sidePanel.AddComponent<Image>();
            sideBg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
            
            VerticalLayoutGroup vlayout = sidePanel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(8, 8, 10, 10);
            
            // Camera header
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(sidePanel.transform, false);
            LayoutElement headerLE = headerObj.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 20;
            TextMeshProUGUI header = headerObj.AddComponent<TextMeshProUGUI>();
            header.text = "📹 CAMERA";
            header.fontSize = 11;
            header.fontStyle = FontStyles.Bold;
            header.color = accentColor;
            header.alignment = TextAlignmentOptions.Center;
            
            // Camera buttons
            _followPlayer1Button = CreateSideButton(sidePanel.transform, "Follow P1", () => SetCameraMode(SpectatorCameraMode.FollowPlayer1));
            _followPlayer2Button = CreateSideButton(sidePanel.transform, "Follow P2", () => SetCameraMode(SpectatorCameraMode.FollowPlayer2));
            _freeCameraButton = CreateSideButton(sidePanel.transform, "Free Cam", () => SetCameraMode(SpectatorCameraMode.Free));
        }

        private Button CreateSideButton(Transform parent, string label, Action onClick)
        {
            GameObject btnObj = new GameObject(label);
            btnObj.transform.SetParent(parent, false);
            
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.16f);
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 11;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            return btn;
        }

        #region Data & Logic

        private void GenerateSampleBattles()
        {
            string[] players = {
                "DragonSlayer", "ShadowKnight", "IronMage", "StormBringer",
                "NightHunter", "FireLord", "IceQueen", "ThunderGod",
                "DarkWarrior", "LightBringer", "DeathDealer", "LifeGiver"
            };
            
            for (int i = 0; i < 15; i++)
            {
                var battle = new LiveBattle
                {
                    BattleId = Guid.NewGuid().ToString(),
                    Player1Name = players[UnityEngine.Random.Range(0, players.Length)],
                    Player2Name = players[UnityEngine.Random.Range(0, players.Length)],
                    Player1Health = UnityEngine.Random.Range(30, 100),
                    Player2Health = UnityEngine.Random.Range(30, 100),
                    Player1Score = UnityEngine.Random.Range(0, 5),
                    Player2Score = UnityEngine.Random.Range(0, 5),
                    ViewerCount = UnityEngine.Random.Range(5, 500),
                    TimeRemaining = TimeSpan.FromMinutes(UnityEngine.Random.Range(1, 10)),
                    IsFeatured = i < 3,
                    BattleType = (BattleType)UnityEngine.Random.Range(0, 3)
                };
                
                // Ensure different players
                while (battle.Player1Name == battle.Player2Name)
                {
                    battle.Player2Name = players[UnityEngine.Random.Range(0, players.Length)];
                }
                
                _liveBattles.Add(battle);
            }
        }

        private void ShowTab(SpectatorTab tab)
        {
            _currentTab = tab;
            UpdateTabHighlights();
            RefreshBattleList();
        }

        private void UpdateTabHighlights()
        {
            SetTabHighlight(_featuredTab, _currentTab == SpectatorTab.Featured);
            SetTabHighlight(_liveTab, _currentTab == SpectatorTab.Live);
            SetTabHighlight(_friendsTab, _currentTab == SpectatorTab.Friends);
        }

        private void SetTabHighlight(Button tab, bool active)
        {
            if (tab == null) return;
            var image = tab.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? accentColor : new Color(0.1f, 0.1f, 0.14f);
            }
        }

        private void RefreshBattleList()
        {
            foreach (Transform child in _liveBattlesContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            _filteredBattles = _currentTab switch
            {
                SpectatorTab.Featured => _liveBattles.Where(b => b.IsFeatured).ToList(),
                SpectatorTab.Live => _liveBattles.OrderByDescending(b => b.ViewerCount).ToList(),
                SpectatorTab.Friends => new List<LiveBattle>(), // Would filter by friends list
                _ => _liveBattles
            };
            
            foreach (var battle in _filteredBattles)
            {
                CreateBattleCard(battle);
            }
        }

        private void CreateBattleCard(LiveBattle battle)
        {
            GameObject card = new GameObject($"Battle_{battle.BattleId}");
            card.transform.SetParent(_liveBattlesContainer.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Image bg = card.AddComponent<Image>();
            bg.color = battle.IsFeatured ? new Color(0.15f, 0.12f, 0.08f) : new Color(0.1f, 0.1f, 0.13f);
            
            if (battle.IsFeatured)
            {
                UnityEngine.UI.Outline border = card.AddComponent<UnityEngine.UI.Outline>();
                border.effectColor = new Color(0.9f, 0.7f, 0.2f);
                border.effectDistance = new Vector2(1, 1);
            }
            
            Button btn = card.AddComponent<Button>();
            var battle_copy = battle;
            btn.onClick.AddListener(() => StartSpectating(battle_copy));
            
            // Content layout
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            hlayout.childForceExpandWidth = false;
            
            // Player 1
            GameObject p1Obj = new GameObject("Player1");
            p1Obj.transform.SetParent(card.transform, false);
            LayoutElement p1LE = p1Obj.AddComponent<LayoutElement>();
            p1LE.preferredWidth = 120;
            VerticalLayoutGroup p1VL = p1Obj.AddComponent<VerticalLayoutGroup>();
            p1VL.childAlignment = TextAnchor.MiddleRight;
            
            GameObject p1Name = new GameObject("Name");
            p1Name.transform.SetParent(p1Obj.transform, false);
            TextMeshProUGUI p1NameText = p1Name.AddComponent<TextMeshProUGUI>();
            p1NameText.text = battle.Player1Name;
            p1NameText.fontSize = 14;
            p1NameText.fontStyle = FontStyles.Bold;
            p1NameText.color = player1Color;
            p1NameText.alignment = TextAlignmentOptions.Right;
            
            GameObject p1Score = new GameObject("Score");
            p1Score.transform.SetParent(p1Obj.transform, false);
            TextMeshProUGUI p1ScoreText = p1Score.AddComponent<TextMeshProUGUI>();
            p1ScoreText.text = $"Score: {battle.Player1Score}";
            p1ScoreText.fontSize = 10;
            p1ScoreText.color = new Color(0.6f, 0.6f, 0.6f);
            p1ScoreText.alignment = TextAlignmentOptions.Right;
            
            // VS
            GameObject vsObj = new GameObject("VS");
            vsObj.transform.SetParent(card.transform, false);
            LayoutElement vsLE = vsObj.AddComponent<LayoutElement>();
            vsLE.preferredWidth = 50;
            TextMeshProUGUI vsText = vsObj.AddComponent<TextMeshProUGUI>();
            vsText.text = "⚔️";
            vsText.fontSize = 24;
            vsText.alignment = TextAlignmentOptions.Center;
            
            // Player 2
            GameObject p2Obj = new GameObject("Player2");
            p2Obj.transform.SetParent(card.transform, false);
            LayoutElement p2LE = p2Obj.AddComponent<LayoutElement>();
            p2LE.preferredWidth = 120;
            VerticalLayoutGroup p2VL = p2Obj.AddComponent<VerticalLayoutGroup>();
            p2VL.childAlignment = TextAnchor.MiddleLeft;
            
            GameObject p2Name = new GameObject("Name");
            p2Name.transform.SetParent(p2Obj.transform, false);
            TextMeshProUGUI p2NameText = p2Name.AddComponent<TextMeshProUGUI>();
            p2NameText.text = battle.Player2Name;
            p2NameText.fontSize = 14;
            p2NameText.fontStyle = FontStyles.Bold;
            p2NameText.color = player2Color;
            p2NameText.alignment = TextAlignmentOptions.Left;
            
            GameObject p2Score = new GameObject("Score");
            p2Score.transform.SetParent(p2Obj.transform, false);
            TextMeshProUGUI p2ScoreText = p2Score.AddComponent<TextMeshProUGUI>();
            p2ScoreText.text = $"Score: {battle.Player2Score}";
            p2ScoreText.fontSize = 10;
            p2ScoreText.color = new Color(0.6f, 0.6f, 0.6f);
            p2ScoreText.alignment = TextAlignmentOptions.Left;
            
            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(card.transform, false);
            LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
            spacerLE.flexibleWidth = 1;
            
            // Info column
            GameObject infoObj = new GameObject("Info");
            infoObj.transform.SetParent(card.transform, false);
            LayoutElement infoLE = infoObj.AddComponent<LayoutElement>();
            infoLE.preferredWidth = 100;
            VerticalLayoutGroup infoVL = infoObj.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleRight;
            
            // Viewers
            GameObject viewersObj = new GameObject("Viewers");
            viewersObj.transform.SetParent(infoObj.transform, false);
            TextMeshProUGUI viewers = viewersObj.AddComponent<TextMeshProUGUI>();
            viewers.text = $"👁️ {battle.ViewerCount}";
            viewers.fontSize = 12;
            viewers.color = new Color(0.7f, 0.7f, 0.7f);
            viewers.alignment = TextAlignmentOptions.Right;
            
            // Type
            GameObject typeObj = new GameObject("Type");
            typeObj.transform.SetParent(infoObj.transform, false);
            TextMeshProUGUI type = typeObj.AddComponent<TextMeshProUGUI>();
            type.text = GetBattleTypeLabel(battle.BattleType);
            type.fontSize = 10;
            type.color = new Color(0.5f, 0.5f, 0.5f);
            type.alignment = TextAlignmentOptions.Right;
            
            // Watch button
            GameObject watchObj = new GameObject("Watch");
            watchObj.transform.SetParent(card.transform, false);
            LayoutElement watchLE = watchObj.AddComponent<LayoutElement>();
            watchLE.preferredWidth = 70;
            watchLE.preferredHeight = 35;
            Image watchBg = watchObj.AddComponent<Image>();
            watchBg.color = accentColor;
            TextMeshProUGUI watchText = watchObj.AddComponent<TextMeshProUGUI>();
            watchText.text = "WATCH";
            watchText.fontSize = 12;
            watchText.fontStyle = FontStyles.Bold;
            watchText.color = Color.white;
            watchText.alignment = TextAlignmentOptions.Center;
        }

        private void StartSpectating(LiveBattle battle)
        {
            _currentBattle = battle;
            
            _player1Name.text = battle.Player1Name;
            _player2Name.text = battle.Player2Name;
            _player1Health.value = battle.Player1Health;
            _player2Health.value = battle.Player2Health;
            _player1Score.text = $"Score: {battle.Player1Score}";
            _player2Score.text = $"Score: {battle.Player2Score}";
            _viewerCount.text = $"👁️ {battle.ViewerCount} watching";
            
            _browsePanel.SetActive(false);
            _watchPanel.SetActive(true);
            
            OnStartSpectating?.Invoke(battle);
            NotificationSystem.Instance?.ShowInfo($"Now spectating: {battle.Player1Name} vs {battle.Player2Name}");
        }

        private void ShowBrowsePanel()
        {
            _browsePanel.SetActive(true);
            _watchPanel.SetActive(false);
            RefreshBattleList();
        }

        private void UpdateBattleState()
        {
            if (_currentBattle == null) return;
            
            // Simulate battle progress
            _currentBattle.TimeRemaining -= TimeSpan.FromSeconds(Time.deltaTime);
            if (_currentBattle.TimeRemaining.TotalSeconds <= 0)
            {
                _currentBattle.TimeRemaining = TimeSpan.Zero;
            }
            
            _battleTimer.text = FormatTime(_currentBattle.TimeRemaining);
            
            // Random health changes (simulation)
            if (UnityEngine.Random.value < 0.01f)
            {
                _currentBattle.Player1Health = Mathf.Max(0, _currentBattle.Player1Health - UnityEngine.Random.Range(1, 5));
                _player1Health.value = _currentBattle.Player1Health;
            }
            if (UnityEngine.Random.value < 0.01f)
            {
                _currentBattle.Player2Health = Mathf.Max(0, _currentBattle.Player2Health - UnityEngine.Random.Range(1, 5));
                _player2Health.value = _currentBattle.Player2Health;
            }
        }

        private void SetCameraMode(SpectatorCameraMode mode)
        {
            _cameraMode = mode;
            
            SetButtonHighlight(_followPlayer1Button, mode == SpectatorCameraMode.FollowPlayer1);
            SetButtonHighlight(_followPlayer2Button, mode == SpectatorCameraMode.FollowPlayer2);
            SetButtonHighlight(_freeCameraButton, mode == SpectatorCameraMode.Free);
            
            string playerName = mode switch
            {
                SpectatorCameraMode.FollowPlayer1 => _currentBattle?.Player1Name,
                SpectatorCameraMode.FollowPlayer2 => _currentBattle?.Player2Name,
                _ => null
            };
            
            if (playerName != null)
            {
                OnFollowPlayer?.Invoke(playerName);
            }
        }

        private void SetButtonHighlight(Button btn, bool active)
        {
            if (btn == null) return;
            var image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? accentColor : new Color(0.12f, 0.12f, 0.16f);
            }
        }

        private void OnPauseClicked()
        {
            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0 : _playbackSpeed.value;
            
            var text = _pauseButton.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = _isPaused ? "▶️" : "⏸️";
            }
        }

        private void OnScreenshotClicked()
        {
            // Screenshot using coroutine to capture at end of frame
            StartCoroutine(CaptureScreenshotCoroutine());
        }

        private System.Collections.IEnumerator CaptureScreenshotCoroutine()
        {
            yield return new WaitForEndOfFrame();
            
            string filename = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
            
            // Capture screen to texture
            Texture2D screenTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenTex.Apply();
            
            // Encode and save
            byte[] bytes = screenTex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            
            // Cleanup
            Destroy(screenTex);
            
            NotificationSystem.Instance?.ShowSuccess($"Screenshot saved: {filename}");
            OnScreenshotTaken?.Invoke();
        }

        private void OnRecordClicked()
        {
            _isRecording = !_isRecording;
            
            var text = _recordButton.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = _isRecording ? "⏹️" : "🔴";
            }
            
            NotificationSystem.Instance?.ShowInfo(_isRecording ? "Recording started..." : "Recording stopped");
        }

        private void OnExitSpectatorClicked()
        {
            _currentBattle = null;
            Time.timeScale = 1f;
            _isPaused = false;
            _isRecording = false;
            
            ShowBrowsePanel();
            OnStopSpectating?.Invoke();
        }

        #endregion

        #region Helpers

        private string GetBattleTypeLabel(BattleType type)
        {
            return type switch
            {
                BattleType.Ranked => "🏆 Ranked",
                BattleType.Tournament => "🎖️ Tournament",
                BattleType.Casual => "⚔️ Casual",
                _ => "Battle"
            };
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel?.SetActive(true);
            ShowBrowsePanel();
        }

        public void Hide()
        {
            if (_currentBattle != null)
            {
                OnExitSpectatorClicked();
            }
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

        public void SpectatePlayer(string playerName)
        {
            var battle = _liveBattles.FirstOrDefault(b => 
                b.Player1Name == playerName || b.Player2Name == playerName);
            
            if (battle != null)
            {
                Show();
                StartSpectating(battle);
            }
        }

        #endregion
    }

    #region Data Classes

    public enum SpectatorTab
    {
        Featured,
        Live,
        Friends
    }

    public enum SpectatorCameraMode
    {
        Free,
        FollowPlayer1,
        FollowPlayer2,
        Overview
    }

    public enum BattleType
    {
        Casual,
        Ranked,
        Tournament
    }

    public class LiveBattle
    {
        public string BattleId;
        public string Player1Name;
        public string Player2Name;
        public float Player1Health;
        public float Player2Health;
        public int Player1Score;
        public int Player2Score;
        public int ViewerCount;
        public TimeSpan TimeRemaining;
        public bool IsFeatured;
        public BattleType BattleType;
    }

    #endregion
}
