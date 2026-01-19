// ============================================================================
// APEX CITADELS - AAA TROOP TRAINING PANEL
// Full-featured troop training UI with queue, upgrades, and army overview
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using ApexCitadels.Data;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Training panel tabs
    /// </summary>
    public enum TrainingTab
    {
        Train,      // Train new troops
        Army,       // View current army
        Upgrades,   // Research upgrades
        Formations  // Battle formations
    }

    /// <summary>
    /// Troop training queue item
    /// </summary>
    [System.Serializable]
    public class TroopTrainingQueueItem
    {
        public string Id;
        public TroopType Type;
        public int Count;
        public int TotalTimeSeconds;
        public float ElapsedTime;
        public bool IsComplete;

        public float Progress => TotalTimeSeconds > 0 ? ElapsedTime / TotalTimeSeconds : 0f;
        public int RemainingSeconds => Mathf.Max(0, Mathf.CeilToInt(TotalTimeSeconds - ElapsedTime));
    }

    /// <summary>
    /// AAA Troop Training Panel with training queue, army view, and upgrades
    /// </summary>
    public class TroopTrainingPanel : MonoBehaviour
    {
        public static TroopTrainingPanel Instance { get; private set; }

        [Header("Runtime References")]
        private Canvas parentCanvas;
        private GameObject panelRoot;
        private bool isVisible = false;

        // Tab buttons
        private Dictionary<TrainingTab, Button> tabButtons = new Dictionary<TrainingTab, Button>();
        private TrainingTab currentTab = TrainingTab.Train;

        // Main containers
        private RectTransform mainPanel;
        private RectTransform headerPanel;
        private RectTransform contentPanel;
        private RectTransform trainPanel;
        private RectTransform armyPanel;
        private RectTransform upgradesPanel;
        private RectTransform formationsPanel;

        // Training panel elements
        private RectTransform troopCardsContainer;
        private RectTransform queueContainer;
        private ScrollRect queueScroll;
        private List<GameObject> troopCards = new List<GameObject>();
        private List<GameObject> queueItems = new List<GameObject>();

        // Army panel elements
        private RectTransform armyGridContainer;
        private TextMeshProUGUI totalPowerText;
        private TextMeshProUGUI totalTroopsText;
        private List<GameObject> armyCards = new List<GameObject>();

        // Header elements
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI capacityText;

        // State
        private List<TrainingQueueItem> trainingQueue = new List<TrainingQueueItem>();
        private Dictionary<TroopType, int> currentArmy = new Dictionary<TroopType, int>();
        private int maxQueueSize = 5;
        private int maxArmySize = 100;

        // Colors
        private readonly Color PANEL_BG = new Color(0.08f, 0.1f, 0.12f, 0.95f);
        private readonly Color HEADER_BG = new Color(0.12f, 0.15f, 0.18f, 1f);
        private readonly Color CARD_BG = new Color(0.15f, 0.18f, 0.22f, 1f);
        private readonly Color CARD_HOVER = new Color(0.2f, 0.25f, 0.3f, 1f);
        private readonly Color CARD_SELECTED = new Color(0.3f, 0.5f, 0.4f, 1f);
        private readonly Color BUTTON_NORMAL = new Color(0.2f, 0.4f, 0.3f, 1f);
        private readonly Color BUTTON_HOVER = new Color(0.25f, 0.5f, 0.35f, 1f);
        private readonly Color BUTTON_DISABLED = new Color(0.15f, 0.15f, 0.15f, 0.5f);
        private readonly Color TEXT_PRIMARY = new Color(0.95f, 0.95f, 0.95f, 1f);
        private readonly Color TEXT_SECONDARY = new Color(0.7f, 0.7f, 0.7f, 1f);
        private readonly Color TEXT_HIGHLIGHT = new Color(0.4f, 0.9f, 0.5f, 1f);
        private readonly Color PROGRESS_BAR = new Color(0.3f, 0.7f, 0.4f, 1f);

        // Troop icons (emoji)
        private readonly Dictionary<TroopType, string> troopIcons = new Dictionary<TroopType, string>
        {
            { TroopType.Infantry, "⚔️" },
            { TroopType.Archer, "🏹" },
            { TroopType.Cavalry, "🐴" },
            { TroopType.Siege, "💣" },
            { TroopType.Mage, "🔮" },
            { TroopType.Guardian, "🛡️" }
        };

        // Troop colors
        private readonly Dictionary<TroopType, Color> troopColors = new Dictionary<TroopType, Color>
        {
            { TroopType.Infantry, new Color(0.6f, 0.4f, 0.3f) },
            { TroopType.Archer, new Color(0.4f, 0.6f, 0.3f) },
            { TroopType.Cavalry, new Color(0.5f, 0.4f, 0.6f) },
            { TroopType.Siege, new Color(0.6f, 0.5f, 0.3f) },
            { TroopType.Mage, new Color(0.4f, 0.4f, 0.7f) },
            { TroopType.Guardian, new Color(0.5f, 0.5f, 0.5f) }
        };

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
                InitializeArmy();
                CreateUI();
                Hide();
            }
        }

        private void Update()
        {
            if (!isVisible) return;

            UpdateTrainingQueue();
            HandleInput();
        }

        private void InitializeArmy()
        {
            // Initialize army counts
            foreach (TroopType type in Enum.GetValues(typeof(TroopType)))
            {
                currentArmy[type] = 0;
            }

            // Add some starting troops for testing
            currentArmy[TroopType.Infantry] = 20;
            currentArmy[TroopType.Archer] = 10;
            currentArmy[TroopType.Cavalry] = 5;
        }

        #region UI Creation

        private void CreateUI()
        {
            // Main panel root
            panelRoot = new GameObject("TroopTrainingPanel");
            panelRoot.transform.SetParent(parentCanvas.transform, false);
            mainPanel = panelRoot.AddComponent<RectTransform>();

            // Center panel with fixed size
            mainPanel.anchorMin = new Vector2(0.5f, 0.5f);
            mainPanel.anchorMax = new Vector2(0.5f, 0.5f);
            mainPanel.sizeDelta = new Vector2(900, 650);
            mainPanel.anchoredPosition = Vector2.zero;

            Image bg = panelRoot.AddComponent<Image>();
            bg.color = PANEL_BG;

            // Create sections
            CreateHeader();
            CreateTabs();
            CreateTrainPanel();
            CreateArmyPanel();
            CreateUpgradesPanel();
            CreateFormationsPanel();

            // Show default tab
            ShowTab(TrainingTab.Train);
        }

        private void CreateHeader()
        {
            GameObject headerObj = CreateUIObject("Header", mainPanel);
            headerPanel = headerObj.GetComponent<RectTransform>();
            SetAnchors(headerPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(0, 0));

            Image bg = headerObj.AddComponent<Image>();
            bg.color = HEADER_BG;

            // Title
            titleText = CreateText(headerPanel, "⚔️ TROOP TRAINING", 24, FontStyles.Bold);
            SetAnchors(titleText.rectTransform, new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(20, 0), new Vector2(0, 0));
            titleText.alignment = TextAlignmentOptions.MidlineLeft;

            // Capacity indicator
            capacityText = CreateText(headerPanel, "Army: 35/100", 16);
            SetAnchors(capacityText.rectTransform, new Vector2(0.7f, 0), new Vector2(0.95f, 1), new Vector2(0, 0), new Vector2(-20, 0));
            capacityText.alignment = TextAlignmentOptions.MidlineRight;
            capacityText.color = TEXT_SECONDARY;

            // Close button
            Button closeBtn = CreateIconButton(headerPanel, "✕", Hide, "Close");
            SetAnchors(closeBtn.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), 
                new Vector2(-50, -20), new Vector2(-10, 20));
        }

        private void CreateTabs()
        {
            GameObject tabsObj = CreateUIObject("Tabs", mainPanel);
            RectTransform tabsRT = tabsObj.GetComponent<RectTransform>();
            SetAnchors(tabsRT, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -100), new Vector2(0, -60));

            HorizontalLayoutGroup layout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            Image tabsBg = tabsObj.AddComponent<Image>();
            tabsBg.color = new Color(0.1f, 0.12f, 0.14f);

            // Create tab buttons
            CreateTabButton(tabsRT, TrainingTab.Train, "🎖️ Train", "Train new troops");
            CreateTabButton(tabsRT, TrainingTab.Army, "👥 Army", "View your army");
            CreateTabButton(tabsRT, TrainingTab.Upgrades, "⬆️ Upgrades", "Research upgrades");
            CreateTabButton(tabsRT, TrainingTab.Formations, "📋 Formations", "Battle formations");
        }

        private void CreateTabButton(RectTransform parent, TrainingTab tab, string label, string tooltip)
        {
            GameObject btnObj = CreateUIObject($"Tab_{tab}", parent);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = CARD_BG;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = CARD_BG;
            colors.highlightedColor = CARD_HOVER;
            colors.selectedColor = CARD_SELECTED;
            btn.colors = colors;

            TextMeshProUGUI text = CreateText(btnObj.GetComponent<RectTransform>(), label, 14, FontStyles.Bold);
            SetAnchors(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            btn.onClick.AddListener(() => ShowTab(tab));
            tabButtons[tab] = btn;

            AddTooltip(btnObj, tooltip);
        }

        private void CreateTrainPanel()
        {
            GameObject trainObj = CreateUIObject("TrainPanel", mainPanel);
            trainPanel = trainObj.GetComponent<RectTransform>();
            SetAnchors(trainPanel, new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 10), new Vector2(-10, -105));

            // Split into troop selection (top) and queue (bottom)
            CreateTroopSelectionArea();
            CreateTrainingQueueArea();
        }

        private void CreateTroopSelectionArea()
        {
            GameObject selectionObj = CreateUIObject("TroopSelection", trainPanel);
            RectTransform selectionRT = selectionObj.GetComponent<RectTransform>();
            SetAnchors(selectionRT, new Vector2(0, 0.35f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));

            // Header
            TextMeshProUGUI header = CreateText(selectionRT, "SELECT TROOPS TO TRAIN", 14, FontStyles.Bold);
            SetAnchors(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -30), new Vector2(-10, 0));
            header.alignment = TextAlignmentOptions.MidlineLeft;
            header.color = TEXT_SECONDARY;

            // Grid container
            troopCardsContainer = CreateUIObject("TroopCards", selectionRT).GetComponent<RectTransform>();
            SetAnchors(troopCardsContainer, new Vector2(0, 0), new Vector2(1, 1), new Vector2(5, 5), new Vector2(-5, -35));

            GridLayoutGroup grid = troopCardsContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(140, 180);
            grid.spacing = new Vector2(10, 10);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(5, 5, 5, 5);

            // Create troop cards
            foreach (TroopType type in Enum.GetValues(typeof(TroopType)))
            {
                CreateTroopCard(type);
            }
        }

        private void CreateTroopCard(TroopType type)
        {
            var def = TroopConfig.Definitions[type];

            GameObject cardObj = CreateUIObject($"TroopCard_{type}", troopCardsContainer);

            Image bg = cardObj.AddComponent<Image>();
            bg.color = CARD_BG;

            // Make card interactive
            Button btn = cardObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock colors = btn.colors;
            colors.normalColor = CARD_BG;
            colors.highlightedColor = CARD_HOVER;
            btn.colors = colors;

            RectTransform cardRT = cardObj.GetComponent<RectTransform>();

            // Type icon (large)
            string icon = troopIcons.TryGetValue(type, out string i) ? i : "❓";
            TextMeshProUGUI iconText = CreateText(cardRT, icon, 36);
            SetAnchors(iconText.rectTransform, new Vector2(0, 0.65f), new Vector2(1, 0.95f), Vector2.zero, Vector2.zero);

            // Name
            TextMeshProUGUI nameText = CreateText(cardRT, def.DisplayName, 14, FontStyles.Bold);
            SetAnchors(nameText.rectTransform, new Vector2(0, 0.52f), new Vector2(1, 0.65f), Vector2.zero, Vector2.zero);
            nameText.color = troopColors.TryGetValue(type, out Color c) ? c : TEXT_PRIMARY;

            // Stats row
            TextMeshProUGUI statsText = CreateText(cardRT, $"⚔️{def.BaseAttack} 🛡️{def.BaseDefense} ❤️{def.BaseHealth}", 10);
            SetAnchors(statsText.rectTransform, new Vector2(0, 0.40f), new Vector2(1, 0.52f), Vector2.zero, Vector2.zero);
            statsText.color = TEXT_SECONDARY;

            // Cost
            string costStr = FormatCost(def.TrainingCost);
            TextMeshProUGUI costText = CreateText(cardRT, costStr, 9);
            SetAnchors(costText.rectTransform, new Vector2(0, 0.28f), new Vector2(1, 0.40f), new Vector2(5, 0), new Vector2(-5, 0));
            costText.color = TEXT_SECONDARY;

            // Time
            string timeStr = FormatTime(def.TrainingTimeSeconds);
            TextMeshProUGUI timeText = CreateText(cardRT, $"⏱️ {timeStr}", 10);
            SetAnchors(timeText.rectTransform, new Vector2(0, 0.16f), new Vector2(1, 0.28f), Vector2.zero, Vector2.zero);
            timeText.color = TEXT_SECONDARY;

            // Train button
            Button trainBtn = CreateTrainButton(cardRT, type);
            SetAnchors(trainBtn.GetComponent<RectTransform>(), new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.15f), Vector2.zero, Vector2.zero);

            troopCards.Add(cardObj);

            // Tooltip with full info
            string tooltip = $"{def.DisplayName}\n\n" +
                            $"Attack: {def.BaseAttack}\n" +
                            $"Defense: {def.BaseDefense}\n" +
                            $"Health: {def.BaseHealth}\n\n" +
                            $"Strong vs: {string.Join(", ", def.StrongAgainst)}\n" +
                            $"Weak vs: {string.Join(", ", def.WeakAgainst)}";
            AddTooltip(cardObj, tooltip);
        }

        private Button CreateTrainButton(RectTransform parent, TroopType type)
        {
            GameObject btnObj = CreateUIObject("TrainBtn", parent);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = BUTTON_NORMAL;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = BUTTON_NORMAL;
            colors.highlightedColor = BUTTON_HOVER;
            colors.disabledColor = BUTTON_DISABLED;
            btn.colors = colors;

            TextMeshProUGUI text = CreateText(btnObj.GetComponent<RectTransform>(), "TRAIN +1", 11, FontStyles.Bold);
            SetAnchors(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            btn.onClick.AddListener(() => QueueTraining(type, 1));

            return btn;
        }

        private void CreateTrainingQueueArea()
        {
            GameObject queueObj = CreateUIObject("QueueArea", trainPanel);
            RectTransform queueRT = queueObj.GetComponent<RectTransform>();
            SetAnchors(queueRT, new Vector2(0, 0), new Vector2(1, 0.35f), new Vector2(0, 0), new Vector2(0, -5));

            Image bg = queueObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.14f);

            // Header
            TextMeshProUGUI header = CreateText(queueRT, "📋 TRAINING QUEUE (0/5)", 13, FontStyles.Bold);
            SetAnchors(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(15, -25), new Vector2(-15, 0));
            header.alignment = TextAlignmentOptions.MidlineLeft;
            header.color = TEXT_SECONDARY;

            // Scroll area for queue
            GameObject scrollObj = CreateUIObject("QueueScroll", queueRT);
            queueScroll = scrollObj.AddComponent<ScrollRect>();
            RectTransform scrollRT = scrollObj.GetComponent<RectTransform>();
            SetAnchors(scrollRT, new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 10), new Vector2(-10, -30));

            // Viewport
            GameObject viewport = CreateUIObject("Viewport", scrollRT);
            SetAnchors(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image vpMask = viewport.AddComponent<Image>();
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            GameObject content = CreateUIObject("Content", viewport.GetComponent<RectTransform>());
            queueContainer = content.GetComponent<RectTransform>();
            SetAnchors(queueContainer, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            queueContainer.pivot = new Vector2(0.5f, 1);

            HorizontalLayoutGroup layout = content.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            queueScroll.content = queueContainer;
            queueScroll.viewport = viewport.GetComponent<RectTransform>();
            queueScroll.horizontal = true;
            queueScroll.vertical = false;
        }

        private void CreateArmyPanel()
        {
            GameObject armyObj = CreateUIObject("ArmyPanel", mainPanel);
            armyPanel = armyObj.GetComponent<RectTransform>();
            SetAnchors(armyPanel, new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 10), new Vector2(-10, -105));
            armyPanel.gameObject.SetActive(false);

            // Header with stats
            TextMeshProUGUI header = CreateText(armyPanel, "YOUR ARMY", 14, FontStyles.Bold);
            SetAnchors(header.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(10, -30), new Vector2(0, 0));
            header.alignment = TextAlignmentOptions.MidlineLeft;
            header.color = TEXT_SECONDARY;

            // Total power
            totalPowerText = CreateText(armyPanel, "⚡ Total Power: 0", 16, FontStyles.Bold);
            SetAnchors(totalPowerText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.8f, 1), new Vector2(0, -30), new Vector2(0, 0));
            totalPowerText.alignment = TextAlignmentOptions.Center;
            totalPowerText.color = TEXT_HIGHLIGHT;

            // Total troops
            totalTroopsText = CreateText(armyPanel, "👥 Troops: 0/100", 14);
            SetAnchors(totalTroopsText.rectTransform, new Vector2(0.8f, 1), new Vector2(1, 1), new Vector2(0, -30), new Vector2(-10, 0));
            totalTroopsText.alignment = TextAlignmentOptions.MidlineRight;
            totalTroopsText.color = TEXT_SECONDARY;

            // Army grid
            armyGridContainer = CreateUIObject("ArmyGrid", armyPanel).GetComponent<RectTransform>();
            SetAnchors(armyGridContainer, new Vector2(0, 0), new Vector2(1, 1), new Vector2(5, 5), new Vector2(-5, -40));

            GridLayoutGroup grid = armyGridContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(280, 120);
            grid.spacing = new Vector2(10, 10);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(10, 10, 10, 10);

            // Create army cards
            foreach (TroopType type in Enum.GetValues(typeof(TroopType)))
            {
                CreateArmyCard(type);
            }
        }

        private void CreateArmyCard(TroopType type)
        {
            var def = TroopConfig.Definitions[type];

            GameObject cardObj = CreateUIObject($"ArmyCard_{type}", armyGridContainer);

            Image bg = cardObj.AddComponent<Image>();
            Color cardColor = troopColors.TryGetValue(type, out Color c) ? c : CARD_BG;
            bg.color = new Color(cardColor.r * 0.3f, cardColor.g * 0.3f, cardColor.b * 0.3f, 0.9f);

            RectTransform cardRT = cardObj.GetComponent<RectTransform>();

            // Left side - icon
            string icon = troopIcons.TryGetValue(type, out string i) ? i : "❓";
            TextMeshProUGUI iconText = CreateText(cardRT, icon, 40);
            SetAnchors(iconText.rectTransform, new Vector2(0, 0), new Vector2(0.25f, 1), Vector2.zero, Vector2.zero);

            // Right side - info
            // Name + count
            TextMeshProUGUI nameText = CreateText(cardRT, def.DisplayName, 16, FontStyles.Bold);
            SetAnchors(nameText.rectTransform, new Vector2(0.28f, 0.65f), new Vector2(0.7f, 0.95f), Vector2.zero, Vector2.zero);
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.name = $"Name_{type}";

            // Count (large)
            TextMeshProUGUI countText = CreateText(cardRT, "0", 28, FontStyles.Bold);
            SetAnchors(countText.rectTransform, new Vector2(0.7f, 0.5f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-10, -5));
            countText.alignment = TextAlignmentOptions.MidlineRight;
            countText.color = TEXT_HIGHLIGHT;
            countText.name = $"Count_{type}";

            // Stats
            TextMeshProUGUI statsText = CreateText(cardRT, $"⚔️{def.BaseAttack}  🛡️{def.BaseDefense}  ❤️{def.BaseHealth}", 11);
            SetAnchors(statsText.rectTransform, new Vector2(0.28f, 0.35f), new Vector2(1, 0.6f), Vector2.zero, Vector2.zero);
            statsText.alignment = TextAlignmentOptions.MidlineLeft;
            statsText.color = TEXT_SECONDARY;

            // Power contribution
            TextMeshProUGUI powerText = CreateText(cardRT, "⚡ Power: 0", 12);
            SetAnchors(powerText.rectTransform, new Vector2(0.28f, 0.05f), new Vector2(1, 0.35f), Vector2.zero, Vector2.zero);
            powerText.alignment = TextAlignmentOptions.MidlineLeft;
            powerText.color = new Color(0.8f, 0.7f, 0.4f);
            powerText.name = $"Power_{type}";

            armyCards.Add(cardObj);
        }

        private void CreateUpgradesPanel()
        {
            GameObject upgradesObj = CreateUIObject("UpgradesPanel", mainPanel);
            upgradesPanel = upgradesObj.GetComponent<RectTransform>();
            SetAnchors(upgradesPanel, new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 10), new Vector2(-10, -105));
            upgradesPanel.gameObject.SetActive(false);

            // Coming soon message
            TextMeshProUGUI comingSoon = CreateText(upgradesPanel, "🔬 TROOP UPGRADES\n\nComing Soon!\n\nResearch upgrades to improve your troops:\n• Increased Attack\n• Improved Defense\n• Better Health\n• Faster Training", 18);
            SetAnchors(comingSoon.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -20));
            comingSoon.color = TEXT_SECONDARY;
        }

        private void CreateFormationsPanel()
        {
            GameObject formationsObj = CreateUIObject("FormationsPanel", mainPanel);
            formationsPanel = formationsObj.GetComponent<RectTransform>();
            SetAnchors(formationsPanel, new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 10), new Vector2(-10, -105));
            formationsPanel.gameObject.SetActive(false);

            // Coming soon message
            TextMeshProUGUI comingSoon = CreateText(formationsPanel, "📋 BATTLE FORMATIONS\n\nComing Soon!\n\nCreate and save battle formations:\n• Defensive Wall\n• Flanking Assault\n• Siege Formation\n• Balanced Attack", 18);
            SetAnchors(comingSoon.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -20));
            comingSoon.color = TEXT_SECONDARY;
        }

        #endregion

        #region Tab Management

        private void ShowTab(TrainingTab tab)
        {
            currentTab = tab;

            // Update tab button states
            foreach (var kvp in tabButtons)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == tab ? CARD_SELECTED : CARD_BG;
            }

            // Show/hide panels
            trainPanel.gameObject.SetActive(tab == TrainingTab.Train);
            armyPanel.gameObject.SetActive(tab == TrainingTab.Army);
            upgradesPanel.gameObject.SetActive(tab == TrainingTab.Upgrades);
            formationsPanel.gameObject.SetActive(tab == TrainingTab.Formations);

            // Refresh army view when switching to it
            if (tab == TrainingTab.Army)
            {
                UpdateArmyDisplay();
            }
        }

        #endregion

        #region Training Queue

        private void QueueTraining(TroopType type, int count)
        {
            if (trainingQueue.Count >= maxQueueSize)
            {
                Debug.Log("[TroopTraining] Queue is full!");
                return;
            }

            // Check resources
            var cost = TroopConfig.GetTotalTrainingCost(type, count);
            // TODO: Check against actual resources

            var def = TroopConfig.Definitions[type];
            var item = new TrainingQueueItem
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                Count = count,
                TotalTimeSeconds = def.TrainingTimeSeconds * count,
                ElapsedTime = 0,
                IsComplete = false
            };

            trainingQueue.Add(item);
            CreateQueueItemUI(item);
            UpdateQueueHeader();

            Debug.Log($"[TroopTraining] Queued {count} {type} - {item.TotalTimeSeconds}s");
        }

        private void CreateQueueItemUI(TrainingQueueItem item)
        {
            var def = TroopConfig.Definitions[item.Type];

            GameObject itemObj = CreateUIObject($"QueueItem_{item.Id}", queueContainer);
            RectTransform itemRT = itemObj.GetComponent<RectTransform>();
            itemRT.sizeDelta = new Vector2(120, 80);

            Image bg = itemObj.AddComponent<Image>();
            bg.color = CARD_BG;

            // Icon
            string icon = troopIcons.TryGetValue(item.Type, out string i) ? i : "❓";
            TextMeshProUGUI iconText = CreateText(itemRT, icon, 24);
            SetAnchors(iconText.rectTransform, new Vector2(0, 0.5f), new Vector2(0.35f, 1), Vector2.zero, Vector2.zero);

            // Name + count
            TextMeshProUGUI nameText = CreateText(itemRT, $"{def.DisplayName} x{item.Count}", 11, FontStyles.Bold);
            SetAnchors(nameText.rectTransform, new Vector2(0.38f, 0.6f), new Vector2(1, 0.95f), Vector2.zero, Vector2.zero);
            nameText.alignment = TextAlignmentOptions.TopLeft;

            // Progress bar background
            GameObject barBg = CreateUIObject("ProgressBg", itemRT);
            SetAnchors(barBg.GetComponent<RectTransform>(), new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.4f), Vector2.zero, Vector2.zero);
            Image barBgImg = barBg.AddComponent<Image>();
            barBgImg.color = new Color(0.2f, 0.2f, 0.2f);

            // Progress bar fill
            GameObject barFill = CreateUIObject("ProgressFill", barBg.GetComponent<RectTransform>());
            RectTransform fillRT = barFill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0, 1);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            Image fillImg = barFill.AddComponent<Image>();
            fillImg.color = PROGRESS_BAR;
            barFill.name = $"Fill_{item.Id}";

            // Time remaining
            TextMeshProUGUI timeText = CreateText(itemRT, FormatTime(item.RemainingSeconds), 10);
            SetAnchors(timeText.rectTransform, new Vector2(0, 0.02f), new Vector2(1, 0.25f), Vector2.zero, Vector2.zero);
            timeText.color = TEXT_SECONDARY;
            timeText.name = $"Time_{item.Id}";

            // Cancel button
            Button cancelBtn = CreateIconButton(itemRT, "✕", () => CancelTraining(item.Id), "Cancel");
            SetAnchors(cancelBtn.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), 
                new Vector2(-25, -25), new Vector2(-5, -5));

            queueItems.Add(itemObj);
        }

        private void CancelTraining(string itemId)
        {
            var item = trainingQueue.Find(q => q.Id == itemId);
            if (item != null)
            {
                trainingQueue.Remove(item);

                // Refund resources (partial based on progress)
                // TODO: Implement refund

                RefreshQueueUI();
                Debug.Log($"[TroopTraining] Cancelled training: {item.Type}");
            }
        }

        private void UpdateTrainingQueue()
        {
            bool needsRefresh = false;

            foreach (var item in trainingQueue.ToList())
            {
                if (item.IsComplete) continue;

                item.ElapsedTime += Time.deltaTime;

                // Update UI
                UpdateQueueItemUI(item);

                if (item.ElapsedTime >= item.TotalTimeSeconds)
                {
                    item.IsComplete = true;
                    CompleteTraining(item);
                    needsRefresh = true;
                }
            }

            // Remove completed items
            if (needsRefresh)
            {
                trainingQueue.RemoveAll(q => q.IsComplete);
                RefreshQueueUI();
            }
        }

        private void UpdateQueueItemUI(TrainingQueueItem item)
        {
            // Find the fill bar
            var fillObj = queueContainer.Find($"QueueItem_{item.Id}/ProgressBg/Fill_{item.Id}");
            if (fillObj != null)
            {
                RectTransform fillRT = fillObj.GetComponent<RectTransform>();
                fillRT.anchorMax = new Vector2(item.Progress, 1);
            }

            // Find time text
            var timeObj = queueContainer.Find($"QueueItem_{item.Id}/Time_{item.Id}");
            if (timeObj != null)
            {
                var timeText = timeObj.GetComponent<TextMeshProUGUI>();
                if (timeText != null)
                {
                    timeText.text = FormatTime(item.RemainingSeconds);
                }
            }
        }

        private void CompleteTraining(TrainingQueueItem item)
        {
            // Add troops to army
            currentArmy[item.Type] += item.Count;

            // Update army display if visible
            if (currentTab == TrainingTab.Army)
            {
                UpdateArmyDisplay();
            }

            UpdateCapacityDisplay();

            Debug.Log($"[TroopTraining] Completed: {item.Count} {item.Type}");
        }

        private void RefreshQueueUI()
        {
            // Clear existing queue UI
            foreach (var obj in queueItems)
            {
                if (obj != null) Destroy(obj);
            }
            queueItems.Clear();

            // Recreate queue items
            foreach (var item in trainingQueue)
            {
                CreateQueueItemUI(item);
            }

            UpdateQueueHeader();
        }

        private void UpdateQueueHeader()
        {
            // Find and update queue header
            var header = trainPanel.Find("QueueArea")?.GetComponentInChildren<TextMeshProUGUI>();
            if (header != null)
            {
                header.text = $"📋 TRAINING QUEUE ({trainingQueue.Count}/{maxQueueSize})";
            }
        }

        #endregion

        #region Army Display

        private void UpdateArmyDisplay()
        {
            int totalTroops = 0;
            int totalPower = 0;

            foreach (TroopType type in Enum.GetValues(typeof(TroopType)))
            {
                int count = currentArmy.TryGetValue(type, out int c) ? c : 0;
                totalTroops += count;

                var troop = new Troop(type, count, 1);
                int power = TroopConfig.CalculatePower(troop);
                totalPower += power;

                // Update card
                UpdateArmyCard(type, count, power);
            }

            // Update totals
            if (totalPowerText != null)
                totalPowerText.text = $"⚡ Total Power: {totalPower:N0}";

            if (totalTroopsText != null)
                totalTroopsText.text = $"👥 Troops: {totalTroops}/{maxArmySize}";
        }

        private void UpdateArmyCard(TroopType type, int count, int power)
        {
            var cardObj = armyCards.Find(c => c.name == $"ArmyCard_{type}");
            if (cardObj == null) return;

            var countText = cardObj.transform.Find($"Count_{type}")?.GetComponent<TextMeshProUGUI>();
            if (countText != null) countText.text = count.ToString();

            var powerText = cardObj.transform.Find($"Power_{type}")?.GetComponent<TextMeshProUGUI>();
            if (powerText != null) powerText.text = $"⚡ Power: {power:N0}";
        }

        private void UpdateCapacityDisplay()
        {
            int total = currentArmy.Values.Sum();
            if (capacityText != null)
            {
                capacityText.text = $"Army: {total}/{maxArmySize}";
                capacityText.color = total >= maxArmySize ? new Color(1f, 0.4f, 0.4f) : TEXT_SECONDARY;
            }
        }

        #endregion

        #region Input & Show/Hide

        private void HandleInput()
        {
            // Tab shortcuts
            if (Input.GetKeyDown(KeyCode.Alpha1)) ShowTab(TrainingTab.Train);
            if (Input.GetKeyDown(KeyCode.Alpha2)) ShowTab(TrainingTab.Army);
            if (Input.GetKeyDown(KeyCode.Alpha3)) ShowTab(TrainingTab.Upgrades);
            if (Input.GetKeyDown(KeyCode.Alpha4)) ShowTab(TrainingTab.Formations);

            // Escape to close
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }

        public void Show()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                isVisible = true;
                UpdateCapacityDisplay();
                UpdateArmyDisplay();
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

        #endregion

        #region Utility Methods

        private string FormatCost(Data.ResourceCost cost)
        {
            List<string> parts = new List<string>();
            if (cost.Stone > 0) parts.Add($"🪨{cost.Stone}");
            if (cost.Wood > 0) parts.Add($"🪵{cost.Wood}");
            if (cost.Iron > 0) parts.Add($"🔩{cost.Iron}");
            if (cost.Crystal > 0) parts.Add($"💎{cost.Crystal}");
            if (cost.ArcaneEssence > 0) parts.Add($"✨{cost.ArcaneEssence}");
            return string.Join(" ", parts);
        }

        private string FormatTime(int seconds)
        {
            if (seconds < 60) return $"{seconds}s";
            if (seconds < 3600) return $"{seconds / 60}m {seconds % 60}s";
            return $"{seconds / 3600}h {(seconds % 3600) / 60}m";
        }

        private GameObject CreateUIObject(string name, RectTransform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private void SetAnchors(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private TextMeshProUGUI CreateText(RectTransform parent, string text, int fontSize, FontStyles style = FontStyles.Normal)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = TEXT_PRIMARY;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }

        private Button CreateIconButton(RectTransform parent, string icon, Action onClick, string tooltip = "")
        {
            GameObject btnObj = CreateUIObject("IconButton", parent);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            TextMeshProUGUI iconText = CreateText(btnObj.GetComponent<RectTransform>(), icon, 14);
            SetAnchors(iconText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            btn.onClick.AddListener(() => onClick?.Invoke());

            if (!string.IsNullOrEmpty(tooltip))
            {
                AddTooltip(btnObj, tooltip);
            }

            return btn;
        }

        private void AddTooltip(GameObject obj, string text)
        {
            var trigger = obj.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => ShowTooltip(text, Input.mousePosition));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => HideTooltip());
            trigger.triggers.Add(exitEntry);
        }

        private GameObject tooltipObj;
        private TextMeshProUGUI tooltipText;

        private void ShowTooltip(string text, Vector3 position)
        {
            if (tooltipObj == null)
            {
                tooltipObj = CreateUIObject("Tooltip", mainPanel);
                RectTransform rt = tooltipObj.GetComponent<RectTransform>();
                rt.pivot = new Vector2(0, 1);
                rt.sizeDelta = new Vector2(220, 100);

                Image bg = tooltipObj.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

                tooltipText = CreateText(rt, "", 11);
                SetAnchors(tooltipText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 5), new Vector2(-8, -5));
                tooltipText.alignment = TextAlignmentOptions.TopLeft;
            }

            tooltipText.text = text;
            tooltipObj.SetActive(true);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mainPanel, position, null, out Vector2 localPoint);
            tooltipObj.GetComponent<RectTransform>().anchoredPosition = localPoint + new Vector2(15, -15);
        }

        private void HideTooltip()
        {
            if (tooltipObj != null)
            {
                tooltipObj.SetActive(false);
            }
        }

        #endregion
    }
}
