// ============================================================================
// APEX CITADELS - REPLAY SYSTEM PANEL
// Record, watch, and share battle replays
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using ApexCitadels.PC;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    // Replay view tabs
    public enum ReplayTab
    {
        MyReplays,
        Bookmarked,
        Featured,
        Search
    }

    // Replay types
    public enum ReplayType
    {
        Battle,
        Siege,
        Tournament,
        DuelArena,
        AllianceWar
    }

    // Replay playback state
    public enum PlaybackState
    {
        Stopped,
        Playing,
        Paused,
        FastForward,
        Rewind
    }

    // Replay data
    [System.Serializable]
    public class ReplayData
    {
        public string replayId;
        public string title;
        public ReplayType type;
        public DateTime recordedDate;
        public float duration; // in seconds
        public string[] participants;
        public string winnerName;
        public bool isVictory;
        public int views;
        public int likes;
        public bool isFeatured;
        public bool isBookmarked;
        public bool isMyReplay;
        public string thumbnailId;
        public int fileSize; // in KB
        public List<ReplayEvent> events = new List<ReplayEvent>();
        public List<ReplayMarker> markers = new List<ReplayMarker>();
    }

    // Individual replay event
    [System.Serializable]
    public class ReplayEvent
    {
        public float timestamp;
        public string eventType;
        public string actorId;
        public string targetId;
        public Vector3 position;
        public string data;
    }

    // User-created markers/bookmarks within replay
    [System.Serializable]
    public class ReplayMarker
    {
        public float timestamp;
        public string label;
        public Color color;
    }

    public class ReplaySystemPanel : MonoBehaviour
    {
        public static ReplaySystemPanel Instance { get; private set; }

        [Header("Runtime References")]
        private Canvas parentCanvas;
        private GameObject panelRoot;
        private bool isVisible = false;

        // UI References
        private RectTransform mainPanel;
        private RectTransform contentPanel;
        private RectTransform replayListPanel;
        private RectTransform playerPanel;
        private RectTransform controlsPanel;
        private RectTransform timelinePanel;
        private ScrollRect replayScroll;
        private TextMeshProUGUI headerTitle;
        private TextMeshProUGUI nowPlayingText;
        private TextMeshProUGUI timeText;
        private TextMeshProUGUI speedText;
        private Slider timelineSlider;
        private Slider volumeSlider;
        private RawImage videoDisplay;

        // State
        private ReplayTab currentTab = ReplayTab.MyReplays;
        private List<ReplayData> allReplays = new List<ReplayData>();
        private ReplayData currentReplay;
        private ReplayData selectedReplay;
        private List<GameObject> replayItems = new List<GameObject>();
        private List<GameObject> markerIcons = new List<GameObject>();

        // Playback state
        private PlaybackState playbackState = PlaybackState.Stopped;
        private float playbackTime = 0f;
        private float playbackSpeed = 1f;
        private bool isRecording = false;
        private float recordingTime = 0f;

        // Speed options
        private readonly float[] SPEED_OPTIONS = { 0.25f, 0.5f, 1f, 1.5f, 2f, 4f, 8f };
        private int currentSpeedIndex = 2; // 1x

        // Colors
        private readonly Color TIMELINE_BG = new Color(0.15f, 0.15f, 0.2f);
        private readonly Color TIMELINE_PLAYED = new Color(0.3f, 0.6f, 0.9f);
        private readonly Color MARKER_COLOR = new Color(1f, 0.8f, 0.2f);
        private readonly Color VICTORY_COLOR = new Color(0.3f, 0.8f, 0.3f);
        private readonly Color DEFEAT_COLOR = new Color(0.8f, 0.3f, 0.3f);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            parentCanvas = FindFirstObjectByType<Canvas>();
            if (parentCanvas != null)
            {
                CreateUI();
                Hide();
            }
        }

        private void Update()
        {
            if (isVisible)
            {
                HandlePlaybackInput();
                UpdatePlayback();
            }

            // Toggle with V key (for Video/replay)
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (isVisible) Hide();
                else Show();
            }

            // Recording indicator
            if (isRecording)
            {
                recordingTime += Time.deltaTime;
            }
        }

        private void CreateUI()
        {
            // Panel root
            panelRoot = new GameObject("ReplaySystemPanel_Root");
            panelRoot.transform.SetParent(parentCanvas.transform, false);

            mainPanel = panelRoot.AddComponent<RectTransform>();
            mainPanel.anchorMin = new Vector2(0.08f, 0.08f);
            mainPanel.anchorMax = new Vector2(0.92f, 0.92f);
            mainPanel.offsetMin = Vector2.zero;
            mainPanel.offsetMax = Vector2.zero;

            // Background
            Image mainBg = panelRoot.AddComponent<Image>();
            mainBg.color = new Color(0.05f, 0.05f, 0.08f, 0.98f);

            // Add layout
            VerticalLayoutGroup layout = panelRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            CreateHeader();
            CreateTabBar();
            CreateContentArea();
            CreatePlaybackControls();

            LoadMockReplays();
            RefreshReplayList();
        }

        private void CreateHeader()
        {
            // Header container
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(panelRoot.transform, false);

            RectTransform headerRT = headerObj.AddComponent<RectTransform>();
            LayoutElement headerLE = headerObj.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 55;
            headerLE.flexibleWidth = 1;

            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0.15f, 0.1f, 0.2f, 0.9f);

            HorizontalLayoutGroup headerLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(15, 15, 8, 8);
            headerLayout.spacing = 15;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(headerObj.transform, false);
            TextMeshProUGUI iconTMP = iconObj.AddComponent<TextMeshProUGUI>();
            iconTMP.text = "🎬";
            iconTMP.fontSize = 28;
            iconTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 40;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            headerTitle = titleObj.AddComponent<TextMeshProUGUI>();
            headerTitle.text = "REPLAY THEATER";
            headerTitle.fontSize = 24;
            headerTitle.fontStyle = FontStyles.Bold;
            headerTitle.color = Color.white;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;

            // Recording indicator
            GameObject recObj = new GameObject("RecIndicator");
            recObj.transform.SetParent(headerObj.transform, false);
            TextMeshProUGUI recTMP = recObj.AddComponent<TextMeshProUGUI>();
            recTMP.text = "⏺ REC";
            recTMP.fontSize = 16;
            recTMP.color = new Color(1f, 0.3f, 0.3f);
            recTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement recLE = recObj.AddComponent<LayoutElement>();
            recLE.preferredWidth = 80;
            recObj.SetActive(false);

            // Close button
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(headerObj.transform, false);
            Image closeBg = closeObj.AddComponent<Image>();
            closeBg.color = new Color(0.8f, 0.2f, 0.2f);
            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
            LayoutElement closeLE = closeObj.AddComponent<LayoutElement>();
            closeLE.preferredWidth = 38;
            closeLE.preferredHeight = 38;

            GameObject closeText = new GameObject("X");
            closeText.transform.SetParent(closeObj.transform, false);
            TextMeshProUGUI closeTMP = closeText.AddComponent<TextMeshProUGUI>();
            closeTMP.text = "✕";
            closeTMP.fontSize = 22;
            closeTMP.alignment = TextAlignmentOptions.Center;
            closeTMP.color = Color.white;
            RectTransform closeTextRT = closeText.GetComponent<RectTransform>();
            closeTextRT.anchorMin = Vector2.zero;
            closeTextRT.anchorMax = Vector2.one;
            closeTextRT.offsetMin = Vector2.zero;
            closeTextRT.offsetMax = Vector2.zero;
        }

        private void CreateTabBar()
        {
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(panelRoot.transform, false);

            RectTransform tabRT = tabBar.AddComponent<RectTransform>();
            LayoutElement tabLE = tabBar.AddComponent<LayoutElement>();
            tabLE.preferredHeight = 40;
            tabLE.flexibleWidth = 1;

            HorizontalLayoutGroup tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 5;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;

            string[] tabNames = { "📁 My Replays", "[*] Bookmarked", "[T] Featured", "🔍 Search" };
            ReplayTab[] tabValues = { ReplayTab.MyReplays, ReplayTab.Bookmarked, ReplayTab.Featured, ReplayTab.Search };

            for (int i = 0; i < tabNames.Length; i++)
            {
                int index = i;
                CreateTabButton(tabBar.transform, tabNames[i], () => SwitchTab(tabValues[index]));
            }
        }

        private void CreateTabButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(text + "Tab");
            btnObj.transform.SetParent(parent, false);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.15f, 0.12f, 0.2f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.12f, 0.2f);
            colors.highlightedColor = new Color(0.25f, 0.2f, 0.3f);
            colors.pressedColor = new Color(0.3f, 0.25f, 0.4f);
            colors.selectedColor = new Color(0.35f, 0.2f, 0.4f);
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }

        private void CreateContentArea()
        {
            // Content container
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(panelRoot.transform, false);

            contentPanel = contentObj.AddComponent<RectTransform>();
            LayoutElement contentLE = contentObj.AddComponent<LayoutElement>();
            contentLE.flexibleHeight = 1;
            contentLE.flexibleWidth = 1;

            Image contentBg = contentObj.AddComponent<Image>();
            contentBg.color = new Color(0.08f, 0.08f, 0.1f);

            HorizontalLayoutGroup contentLayout = contentObj.AddComponent<HorizontalLayoutGroup>();
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.spacing = 10;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;

            // Left: Replay list
            CreateReplayListPanel(contentObj.transform);
            
            // Center/Right: Video player
            CreatePlayerPanel(contentObj.transform);
        }

        private void CreateReplayListPanel(Transform parent)
        {
            GameObject listPanel = new GameObject("ReplayList");
            listPanel.transform.SetParent(parent, false);

            replayListPanel = listPanel.AddComponent<RectTransform>();
            LayoutElement listLE = listPanel.AddComponent<LayoutElement>();
            listLE.preferredWidth = 300;
            listLE.flexibleHeight = 1;

            Image listBg = listPanel.AddComponent<Image>();
            listBg.color = new Color(0.06f, 0.06f, 0.08f);

            VerticalLayoutGroup listLayout = listPanel.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(5, 5, 5, 5);
            listLayout.spacing = 5;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;

            // Title/count
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(listPanel.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "REPLAYS";
            titleTMP.fontSize = 14;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0.7f, 0.6f, 0.9f);
            titleTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 25;

            // Scroll view
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(listPanel.transform, false);

            RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
            LayoutElement scrollLE = scrollObj.AddComponent<LayoutElement>();
            scrollLE.flexibleHeight = 1;
            scrollLE.flexibleWidth = 1;

            replayScroll = scrollObj.AddComponent<ScrollRect>();
            replayScroll.horizontal = false;
            replayScroll.vertical = true;
            replayScroll.movementType = ScrollRect.MovementType.Elastic;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.05f, 0.05f, 0.07f);

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform viewportRT = viewport.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;

            Image viewportMask = viewport.AddComponent<Image>();
            viewportMask.color = Color.white;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            replayScroll.viewport = viewportRT;

            // Content container
            GameObject contentContainerObj = new GameObject("Content");
            contentContainerObj.transform.SetParent(viewport.transform, false);
            RectTransform contentContainerRT = contentContainerObj.AddComponent<RectTransform>();
            contentContainerRT.anchorMin = new Vector2(0, 1);
            contentContainerRT.anchorMax = new Vector2(1, 1);
            contentContainerRT.pivot = new Vector2(0.5f, 1);
            contentContainerRT.offsetMin = Vector2.zero;
            contentContainerRT.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = contentContainerObj.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(3, 3, 3, 3);
            contentLayout.spacing = 5;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;

            ContentSizeFitter csf = contentContainerObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            replayScroll.content = contentContainerRT;
        }

        private void CreatePlayerPanel(Transform parent)
        {
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.SetParent(parent, false);

            playerPanel = playerObj.AddComponent<RectTransform>();
            LayoutElement playerLE = playerObj.AddComponent<LayoutElement>();
            playerLE.flexibleWidth = 1;
            playerLE.flexibleHeight = 1;

            Image playerBg = playerObj.AddComponent<Image>();
            playerBg.color = new Color(0, 0, 0);

            VerticalLayoutGroup playerLayout = playerObj.AddComponent<VerticalLayoutGroup>();
            playerLayout.padding = new RectOffset(0, 0, 0, 0);
            playerLayout.spacing = 0;
            playerLayout.childForceExpandWidth = true;
            playerLayout.childForceExpandHeight = false;
            playerLayout.childControlWidth = true;
            playerLayout.childControlHeight = true;

            // Now playing bar
            CreateNowPlayingBar(playerObj.transform);

            // Video display area
            GameObject displayObj = new GameObject("VideoDisplay");
            displayObj.transform.SetParent(playerObj.transform, false);

            RectTransform displayRT = displayObj.AddComponent<RectTransform>();
            LayoutElement displayLE = displayObj.AddComponent<LayoutElement>();
            displayLE.flexibleWidth = 1;
            displayLE.flexibleHeight = 1;

            videoDisplay = displayObj.AddComponent<RawImage>();
            videoDisplay.color = new Color(0.1f, 0.1f, 0.12f);

            // Placeholder text when no replay
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(displayObj.transform, false);
            TextMeshProUGUI placeholderTMP = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderTMP.text = "🎬\n\nSelect a replay to watch\nor record a new battle";
            placeholderTMP.fontSize = 20;
            placeholderTMP.color = new Color(0.4f, 0.4f, 0.5f);
            placeholderTMP.alignment = TextAlignmentOptions.Center;
            RectTransform placeholderRT = placeholderObj.GetComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = Vector2.zero;
            placeholderRT.offsetMax = Vector2.zero;

            // Timeline
            CreateTimeline(playerObj.transform);
        }

        private void CreateNowPlayingBar(Transform parent)
        {
            GameObject npBar = new GameObject("NowPlayingBar");
            npBar.transform.SetParent(parent, false);

            RectTransform npRT = npBar.AddComponent<RectTransform>();
            LayoutElement npLE = npBar.AddComponent<LayoutElement>();
            npLE.preferredHeight = 40;
            npLE.flexibleWidth = 1;

            Image npBg = npBar.AddComponent<Image>();
            npBg.color = new Color(0.1f, 0.08f, 0.15f);

            HorizontalLayoutGroup npLayout = npBar.AddComponent<HorizontalLayoutGroup>();
            npLayout.padding = new RectOffset(15, 15, 8, 8);
            npLayout.spacing = 10;
            npLayout.childAlignment = TextAnchor.MiddleLeft;
            npLayout.childForceExpandWidth = false;
            npLayout.childForceExpandHeight = true;
            npLayout.childControlWidth = true;
            npLayout.childControlHeight = true;

            // Now playing text
            GameObject npTextObj = new GameObject("NowPlaying");
            npTextObj.transform.SetParent(npBar.transform, false);
            nowPlayingText = npTextObj.AddComponent<TextMeshProUGUI>();
            nowPlayingText.text = "No replay selected";
            nowPlayingText.fontSize = 14;
            nowPlayingText.color = Color.white;
            LayoutElement npTextLE = npTextObj.AddComponent<LayoutElement>();
            npTextLE.flexibleWidth = 1;

            // Speed indicator
            GameObject speedObj = new GameObject("Speed");
            speedObj.transform.SetParent(npBar.transform, false);
            speedText = speedObj.AddComponent<TextMeshProUGUI>();
            speedText.text = "1.0x";
            speedText.fontSize = 14;
            speedText.color = new Color(0.8f, 0.8f, 0.5f);
            speedText.alignment = TextAlignmentOptions.Right;
            LayoutElement speedLE = speedObj.AddComponent<LayoutElement>();
            speedLE.preferredWidth = 50;
        }

        private void CreateTimeline(Transform parent)
        {
            GameObject timelineObj = new GameObject("Timeline");
            timelineObj.transform.SetParent(parent, false);

            timelinePanel = timelineObj.AddComponent<RectTransform>();
            LayoutElement timelineLE = timelineObj.AddComponent<LayoutElement>();
            timelineLE.preferredHeight = 50;
            timelineLE.flexibleWidth = 1;

            Image timelineBg = timelineObj.AddComponent<Image>();
            timelineBg.color = TIMELINE_BG;

            VerticalLayoutGroup timelineLayout = timelineObj.AddComponent<VerticalLayoutGroup>();
            timelineLayout.padding = new RectOffset(15, 15, 5, 5);
            timelineLayout.spacing = 5;
            timelineLayout.childForceExpandWidth = true;
            timelineLayout.childForceExpandHeight = false;
            timelineLayout.childControlWidth = true;
            timelineLayout.childControlHeight = true;

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(timelineObj.transform, false);

            RectTransform sliderRT = sliderObj.AddComponent<RectTransform>();
            LayoutElement sliderLE = sliderObj.AddComponent<LayoutElement>();
            sliderLE.preferredHeight = 20;
            sliderLE.flexibleWidth = 1;

            Image sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = new Color(0.1f, 0.1f, 0.15f);

            timelineSlider = sliderObj.AddComponent<Slider>();
            timelineSlider.minValue = 0;
            timelineSlider.maxValue = 100;
            timelineSlider.onValueChanged.AddListener(OnTimelineChanged);

            // Fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = new Vector2(0, 0);
            fillAreaRT.offsetMax = new Vector2(0, 0);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRT = fill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0, 1);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = TIMELINE_PLAYED;

            timelineSlider.fillRect = fillRT;

            // Handle
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRT = handleArea.AddComponent<RectTransform>();
            handleAreaRT.anchorMin = Vector2.zero;
            handleAreaRT.anchorMax = Vector2.one;
            handleAreaRT.offsetMin = Vector2.zero;
            handleAreaRT.offsetMax = Vector2.zero;

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRT = handle.AddComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(12, 24);

            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;

            timelineSlider.handleRect = handleRT;
            timelineSlider.targetGraphic = handleImage;

            // Time text row
            GameObject timeRow = new GameObject("TimeRow");
            timeRow.transform.SetParent(timelineObj.transform, false);

            RectTransform timeRowRT = timeRow.AddComponent<RectTransform>();
            LayoutElement timeRowLE = timeRow.AddComponent<LayoutElement>();
            timeRowLE.preferredHeight = 18;
            timeRowLE.flexibleWidth = 1;

            HorizontalLayoutGroup timeRowLayout = timeRow.AddComponent<HorizontalLayoutGroup>();
            timeRowLayout.childForceExpandWidth = false;

            // Current time
            GameObject currentTimeObj = new GameObject("CurrentTime");
            currentTimeObj.transform.SetParent(timeRow.transform, false);
            timeText = currentTimeObj.AddComponent<TextMeshProUGUI>();
            timeText.text = "00:00 / 00:00";
            timeText.fontSize = 12;
            timeText.color = new Color(0.7f, 0.7f, 0.7f);
            LayoutElement timeLE = currentTimeObj.AddComponent<LayoutElement>();
            timeLE.flexibleWidth = 1;
        }

        private void CreatePlaybackControls()
        {
            // Controls bar
            GameObject controlsObj = new GameObject("Controls");
            controlsObj.transform.SetParent(panelRoot.transform, false);

            controlsPanel = controlsObj.AddComponent<RectTransform>();
            LayoutElement controlsLE = controlsObj.AddComponent<LayoutElement>();
            controlsLE.preferredHeight = 60;
            controlsLE.flexibleWidth = 1;

            Image controlsBg = controlsObj.AddComponent<Image>();
            controlsBg.color = new Color(0.1f, 0.08f, 0.12f);

            HorizontalLayoutGroup controlsLayout = controlsObj.AddComponent<HorizontalLayoutGroup>();
            controlsLayout.padding = new RectOffset(20, 20, 10, 10);
            controlsLayout.spacing = 8;
            controlsLayout.childAlignment = TextAnchor.MiddleCenter;
            controlsLayout.childForceExpandWidth = false;
            controlsLayout.childForceExpandHeight = true;
            controlsLayout.childControlWidth = true;
            controlsLayout.childControlHeight = true;

            // Playback controls
            CreateControlButton(controlsObj.transform, "⏮", SkipToStart, 40);
            CreateControlButton(controlsObj.transform, "⏪", Rewind, 40);
            CreateControlButton(controlsObj.transform, "⏯", TogglePlayPause, 50);
            CreateControlButton(controlsObj.transform, "⏩", FastForward, 40);
            CreateControlButton(controlsObj.transform, "⏭", SkipToEnd, 40);

            // Spacer
            GameObject spacer1 = new GameObject("Spacer");
            spacer1.transform.SetParent(controlsObj.transform, false);
            LayoutElement spacerLE = spacer1.AddComponent<LayoutElement>();
            spacerLE.preferredWidth = 30;

            // Speed controls
            CreateControlButton(controlsObj.transform, "🐢", SlowerSpeed, 35);
            CreateSpeedDisplay(controlsObj.transform);
            CreateControlButton(controlsObj.transform, "🐰", FasterSpeed, 35);

            // Spacer
            GameObject spacer2 = new GameObject("Spacer2");
            spacer2.transform.SetParent(controlsObj.transform, false);
            LayoutElement spacer2LE = spacer2.AddComponent<LayoutElement>();
            spacer2LE.preferredWidth = 30;

            // Action buttons
            CreateControlButton(controlsObj.transform, "📌", AddMarker, 40);
            CreateControlButton(controlsObj.transform, "[*]", ToggleBookmark, 40);
            CreateControlButton(controlsObj.transform, "[E]", ShareReplay, 40);
            CreateControlButton(controlsObj.transform, "📷", TakeScreenshot, 40);

            // Volume
            CreateVolumeControl(controlsObj.transform);
        }

        private void CreateControlButton(Transform parent, string icon, UnityEngine.Events.UnityAction onClick, float width)
        {
            GameObject btnObj = new GameObject(icon);
            btnObj.transform.SetParent(parent, false);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.2f, 0.18f, 0.25f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            LayoutElement btnLE = btnObj.AddComponent<LayoutElement>();
            btnLE.preferredWidth = width;
            btnLE.preferredHeight = 40;

            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.18f, 0.25f);
            colors.highlightedColor = new Color(0.3f, 0.25f, 0.35f);
            colors.pressedColor = new Color(0.35f, 0.3f, 0.4f);
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = icon;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }

        private void CreateSpeedDisplay(Transform parent)
        {
            GameObject speedObj = new GameObject("SpeedDisplay");
            speedObj.transform.SetParent(parent, false);

            Image speedBg = speedObj.AddComponent<Image>();
            speedBg.color = new Color(0.15f, 0.12f, 0.2f);

            LayoutElement speedLE = speedObj.AddComponent<LayoutElement>();
            speedLE.preferredWidth = 60;
            speedLE.preferredHeight = 40;

            // Speed text is already created in header, but duplicate for controls
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(speedObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "1.0x";
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.9f, 0.9f, 0.6f);

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }

        private void CreateVolumeControl(Transform parent)
        {
            GameObject volObj = new GameObject("Volume");
            volObj.transform.SetParent(parent, false);

            RectTransform volRT = volObj.AddComponent<RectTransform>();
            LayoutElement volLE = volObj.AddComponent<LayoutElement>();
            volLE.preferredWidth = 120;
            volLE.preferredHeight = 40;

            HorizontalLayoutGroup volLayout = volObj.AddComponent<HorizontalLayoutGroup>();
            volLayout.spacing = 5;
            volLayout.childAlignment = TextAnchor.MiddleCenter;
            volLayout.childForceExpandWidth = false;

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(volObj.transform, false);
            TextMeshProUGUI iconTMP = iconObj.AddComponent<TextMeshProUGUI>();
            iconTMP.text = "🔊";
            iconTMP.fontSize = 18;
            iconTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 25;

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(volObj.transform, false);

            RectTransform sliderRT = sliderObj.AddComponent<RectTransform>();
            LayoutElement sliderLE = sliderObj.AddComponent<LayoutElement>();
            sliderLE.flexibleWidth = 1;
            sliderLE.preferredHeight = 15;

            Image sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = new Color(0.2f, 0.2f, 0.25f);

            volumeSlider = sliderObj.AddComponent<Slider>();
            volumeSlider.minValue = 0;
            volumeSlider.maxValue = 1;
            volumeSlider.value = 0.7f;

            // Fill
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = Vector2.zero;
            fillAreaRT.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRT = fill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0, 1);

            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.5f, 0.5f, 0.7f);

            volumeSlider.fillRect = fillRT;
        }

        private void LoadMockReplays()
        {
            allReplays.Clear();

            // My replays
            allReplays.Add(new ReplayData
            {
                replayId = "replay_001",
                title = "Epic Victory vs ThunderClan",
                type = ReplayType.Battle,
                recordedDate = DateTime.Now.AddHours(-2),
                duration = 312,
                participants = new[] { "Me", "ThunderClan_Chief" },
                winnerName = "Me",
                isVictory = true,
                views = 5,
                isMyReplay = true,
                fileSize = 2400
            });

            allReplays.Add(new ReplayData
            {
                replayId = "replay_002",
                title = "Alliance Siege - Castle Valor",
                type = ReplayType.Siege,
                recordedDate = DateTime.Now.AddDays(-1),
                duration = 1856,
                participants = new[] { "Iron Legion", "Shadow Covenant" },
                winnerName = "Iron Legion",
                isVictory = true,
                views = 45,
                isMyReplay = true,
                fileSize = 8500
            });

            allReplays.Add(new ReplayData
            {
                replayId = "replay_003",
                title = "Tournament Finals - Round 3",
                type = ReplayType.Tournament,
                recordedDate = DateTime.Now.AddDays(-3),
                duration = 542,
                participants = new[] { "Me", "ProGamer99" },
                winnerName = "ProGamer99",
                isVictory = false,
                views = 234,
                likes = 45,
                isMyReplay = true,
                fileSize = 4200
            });

            // Featured replays
            allReplays.Add(new ReplayData
            {
                replayId = "replay_featured_001",
                title = "[EPIC] World Championship Finals",
                type = ReplayType.Tournament,
                recordedDate = DateTime.Now.AddDays(-7),
                duration = 2145,
                participants = new[] { "DragonSlayer", "PhoenixKing" },
                winnerName = "DragonSlayer",
                views = 15234,
                likes = 3421,
                isFeatured = true,
                fileSize = 12000
            });

            allReplays.Add(new ReplayData
            {
                replayId = "replay_featured_002",
                title = "100 vs 100 Alliance War!",
                type = ReplayType.AllianceWar,
                recordedDate = DateTime.Now.AddDays(-5),
                duration = 4523,
                participants = new[] { "Empire United", "Rebel Alliance" },
                winnerName = "Empire United",
                views = 8934,
                likes = 1856,
                isFeatured = true,
                fileSize = 25000
            });

            // Bookmarked
            allReplays.Add(new ReplayData
            {
                replayId = "replay_bm_001",
                title = "Perfect Defense Strategy",
                type = ReplayType.Siege,
                recordedDate = DateTime.Now.AddDays(-10),
                duration = 1234,
                participants = new[] { "TacticalGenius", "AggressivePlayer" },
                winnerName = "TacticalGenius",
                views = 5678,
                likes = 890,
                isBookmarked = true,
                fileSize = 6000
            });
        }

        private void RefreshReplayList()
        {
            // Clear existing
            foreach (var item in replayItems)
            {
                Destroy(item);
            }
            replayItems.Clear();

            // Filter by tab
            var filtered = FilterReplaysByTab(currentTab);

            foreach (var replay in filtered)
            {
                CreateReplayItem(replay);
            }
        }

        private List<ReplayData> FilterReplaysByTab(ReplayTab tab)
        {
            return tab switch
            {
                ReplayTab.MyReplays => allReplays.FindAll(r => r.isMyReplay),
                ReplayTab.Bookmarked => allReplays.FindAll(r => r.isBookmarked),
                ReplayTab.Featured => allReplays.FindAll(r => r.isFeatured),
                ReplayTab.Search => allReplays, // Show all for search
                _ => allReplays
            };
        }

        private void CreateReplayItem(ReplayData replay)
        {
            GameObject itemObj = new GameObject(replay.title);
            itemObj.transform.SetParent(replayScroll.content, false);

            RectTransform itemRT = itemObj.AddComponent<RectTransform>();
            LayoutElement itemLE = itemObj.AddComponent<LayoutElement>();
            itemLE.preferredHeight = 80;
            itemLE.flexibleWidth = 1;

            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.color = new Color(0.12f, 0.1f, 0.15f);

            Button itemBtn = itemObj.AddComponent<Button>();
            itemBtn.onClick.AddListener(() => SelectReplay(replay));

            VerticalLayoutGroup itemLayout = itemObj.AddComponent<VerticalLayoutGroup>();
            itemLayout.padding = new RectOffset(10, 10, 6, 6);
            itemLayout.spacing = 3;
            itemLayout.childForceExpandWidth = true;
            itemLayout.childForceExpandHeight = false;

            // Type icon + Title
            GameObject titleRow = new GameObject("TitleRow");
            titleRow.transform.SetParent(itemObj.transform, false);
            LayoutElement titleRowLE = titleRow.AddComponent<LayoutElement>();
            titleRowLE.preferredHeight = 22;

            HorizontalLayoutGroup titleRowLayout = titleRow.AddComponent<HorizontalLayoutGroup>();
            titleRowLayout.spacing = 5;
            titleRowLayout.childForceExpandWidth = false;

            // Type icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(titleRow.transform, false);
            TextMeshProUGUI iconTMP = iconObj.AddComponent<TextMeshProUGUI>();
            iconTMP.text = GetReplayTypeIcon(replay.type);
            iconTMP.fontSize = 16;
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 25;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(titleRow.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = replay.title;
            titleTMP.fontSize = 13;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = Color.white;
            titleTMP.enableWordWrapping = false;
            titleTMP.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;

            // Participants
            GameObject participantsObj = new GameObject("Participants");
            participantsObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI participantsTMP = participantsObj.AddComponent<TextMeshProUGUI>();
            participantsTMP.text = string.Join(" vs ", replay.participants);
            participantsTMP.fontSize = 11;
            participantsTMP.color = new Color(0.7f, 0.7f, 0.7f);
            LayoutElement participantsLE = participantsObj.AddComponent<LayoutElement>();
            participantsLE.preferredHeight = 16;

            // Result + duration
            GameObject resultObj = new GameObject("Result");
            resultObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI resultTMP = resultObj.AddComponent<TextMeshProUGUI>();
            
            string resultText = replay.isMyReplay 
                ? (replay.isVictory ? "✓ Victory" : "✗ Defeat") 
                : $"Winner: {replay.winnerName}";
            resultTMP.text = $"{resultText} • {FormatDuration(replay.duration)}";
            resultTMP.fontSize = 11;
            resultTMP.color = replay.isMyReplay 
                ? (replay.isVictory ? VICTORY_COLOR : DEFEAT_COLOR) 
                : new Color(0.6f, 0.6f, 0.6f);
            LayoutElement resultLE = resultObj.AddComponent<LayoutElement>();
            resultLE.preferredHeight = 16;

            // Stats (views/likes)
            GameObject statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI statsTMP = statsObj.AddComponent<TextMeshProUGUI>();
            statsTMP.text = $"👁 {replay.views:N0} • ❤️ {replay.likes}";
            statsTMP.fontSize = 10;
            statsTMP.color = new Color(0.5f, 0.5f, 0.6f);
            LayoutElement statsLE = statsObj.AddComponent<LayoutElement>();
            statsLE.preferredHeight = 14;

            replayItems.Add(itemObj);
        }

        private string GetReplayTypeIcon(ReplayType type)
        {
            return type switch
            {
                ReplayType.Battle => "[!]",
                ReplayType.Siege => "[C]",
                ReplayType.Tournament => "[T]",
                ReplayType.DuelArena => "🤺",
                ReplayType.AllianceWar => "[!]",
                _ => "🎬"
            };
        }

        private string FormatDuration(float seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.Hours > 0)
                return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        private void HandlePlaybackInput()
        {
            // Space - play/pause
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TogglePlayPause();
            }

            // Arrow keys - seek
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SeekRelative(-10f);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                SeekRelative(10f);
            }

            // +/- speed
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus))
            {
                FasterSpeed();
            }
            if (Input.GetKeyDown(KeyCode.Minus))
            {
                SlowerSpeed();
            }
        }

        private void UpdatePlayback()
        {
            if (currentReplay == null || playbackState != PlaybackState.Playing) return;

            playbackTime += Time.deltaTime * playbackSpeed;
            
            if (playbackTime >= currentReplay.duration)
            {
                playbackTime = currentReplay.duration;
                playbackState = PlaybackState.Paused;
            }

            UpdateTimeDisplay();
        }

        private void UpdateTimeDisplay()
        {
            if (currentReplay == null) return;

            timelineSlider.SetValueWithoutNotify((playbackTime / currentReplay.duration) * 100f);
            timeText.text = $"{FormatDuration(playbackTime)} / {FormatDuration(currentReplay.duration)}";
        }

        private void SelectReplay(ReplayData replay)
        {
            selectedReplay = replay;
            ApexLogger.Log($"[Replay] Selected: {replay.title}", ApexLogger.LogCategory.UI);
        }

        private void LoadReplay(ReplayData replay)
        {
            currentReplay = replay;
            playbackTime = 0;
            playbackState = PlaybackState.Stopped;
            
            nowPlayingText.text = $"▶ {replay.title}";
            timelineSlider.maxValue = 100;
            UpdateTimeDisplay();
            
            ApexLogger.Log($"[Replay] Loaded: {replay.title}", ApexLogger.LogCategory.UI);
        }

        private void SwitchTab(ReplayTab tab)
        {
            currentTab = tab;
            RefreshReplayList();
            ApexLogger.Log($"[Replay] Switched to tab: {tab}", ApexLogger.LogCategory.UI);
        }

        private void OnTimelineChanged(float value)
        {
            if (currentReplay == null) return;
            playbackTime = (value / 100f) * currentReplay.duration;
            UpdateTimeDisplay();
        }

        // Playback controls
        private void TogglePlayPause()
        {
            if (currentReplay == null && selectedReplay != null)
            {
                LoadReplay(selectedReplay);
            }

            if (currentReplay == null) return;

            if (playbackState == PlaybackState.Playing)
            {
                playbackState = PlaybackState.Paused;
            }
            else
            {
                playbackState = PlaybackState.Playing;
            }
            ApexLogger.Log($"[Replay] {playbackState}", ApexLogger.LogCategory.UI);
        }

        private void SkipToStart()
        {
            playbackTime = 0;
            UpdateTimeDisplay();
        }

        private void SkipToEnd()
        {
            if (currentReplay != null)
            {
                playbackTime = currentReplay.duration;
                playbackState = PlaybackState.Paused;
                UpdateTimeDisplay();
            }
        }

        private void Rewind()
        {
            SeekRelative(-30f);
        }

        private void FastForward()
        {
            SeekRelative(30f);
        }

        private void SeekRelative(float seconds)
        {
            if (currentReplay == null) return;
            playbackTime = Mathf.Clamp(playbackTime + seconds, 0, currentReplay.duration);
            UpdateTimeDisplay();
        }

        private void SlowerSpeed()
        {
            if (currentSpeedIndex > 0)
            {
                currentSpeedIndex--;
                playbackSpeed = SPEED_OPTIONS[currentSpeedIndex];
                speedText.text = $"{playbackSpeed}x";
            }
        }

        private void FasterSpeed()
        {
            if (currentSpeedIndex < SPEED_OPTIONS.Length - 1)
            {
                currentSpeedIndex++;
                playbackSpeed = SPEED_OPTIONS[currentSpeedIndex];
                speedText.text = $"{playbackSpeed}x";
            }
        }

        private void AddMarker()
        {
            if (currentReplay == null) return;
            
            var marker = new ReplayMarker
            {
                timestamp = playbackTime,
                label = $"Marker {currentReplay.markers.Count + 1}",
                color = MARKER_COLOR
            };
            currentReplay.markers.Add(marker);
            ApexLogger.Log($"[Replay] Added marker at {FormatDuration(playbackTime)}", ApexLogger.LogCategory.UI);
        }

        private void ToggleBookmark()
        {
            if (selectedReplay != null)
            {
                selectedReplay.isBookmarked = !selectedReplay.isBookmarked;
                ApexLogger.Log($"[Replay] Bookmark: {selectedReplay.isBookmarked}", ApexLogger.LogCategory.UI);
            }
        }

        private void ShareReplay()
        {
            if (currentReplay != null)
            {
                ApexLogger.Log($"[Replay] Share dialog for: {currentReplay.title}", ApexLogger.LogCategory.UI);
            }
        }

        private void TakeScreenshot()
        {
            ApexLogger.Log("[Replay] Screenshot captured", ApexLogger.LogCategory.UI);
        }

        // Recording API
        public void StartRecording(string title, ReplayType type)
        {
            isRecording = true;
            recordingTime = 0;
            ApexLogger.Log($"[Replay] Started recording: {title}", ApexLogger.LogCategory.UI);
        }

        public void StopRecording()
        {
            if (!isRecording) return;
            isRecording = false;
            
            var newReplay = new ReplayData
            {
                replayId = Guid.NewGuid().ToString(),
                title = $"Recording {DateTime.Now:yyyy-MM-dd HH:mm}",
                type = ReplayType.Battle,
                recordedDate = DateTime.Now,
                duration = recordingTime,
                isMyReplay = true,
                participants = new[] { "Me", "Opponent" }
            };
            
            allReplays.Insert(0, newReplay);
            ApexLogger.Log($"[Replay] Recording saved: {newReplay.title}", ApexLogger.LogCategory.UI);
        }

        public void RecordEvent(string eventType, string actorId, string targetId, Vector3 position)
        {
            // Would record actual replay events
        }

        // Public API
        public void Show()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                isVisible = true;
                RefreshReplayList();
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
                isVisible = false;
            }
        }

        public void Toggle()
        {
            if (isVisible) Hide();
            else Show();
        }

        public bool IsRecording => isRecording;
    }
}
