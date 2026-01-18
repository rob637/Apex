// ============================================================================
// APEX CITADELS - SIEGE WARFARE PANEL
// Large-scale castle siege system with coordinated attacks and defenses
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using ApexCitadels.PC;

namespace ApexCitadels.PC.UI
{
    // Siege view tabs
    public enum SiegeViewTab
    {
        ActiveSieges,
        ScheduledSieges,
        SiegeHistory,
        SiegeDefense,
        SiegeAttack
    }

    // Siege status
    public enum SiegeStatus
    {
        Scheduled,
        PreparationPhase,
        ActiveBattle,
        DefenseVictory,
        AttackVictory,
        Draw,
        Cancelled
    }

    // Siege unit types
    public enum SiegeUnitType
    {
        Infantry,
        Archer,
        Cavalry,
        Catapult,
        BatteringRam,
        SiegeTower,
        Trebuchet,
        Engineer
    }

    // Defense structure types
    public enum DefenseStructureType
    {
        Wall,
        Tower,
        Gate,
        MoatSegment,
        Ballista,
        CauldronPit,
        Barracks,
        SupplyDepot
    }

    // Siege data
    [System.Serializable]
    public class SiegeData
    {
        public string siegeId;
        public string targetCitadelId;
        public string targetCitadelName;
        public string attackingAllianceId;
        public string attackingAllianceName;
        public string defendingAllianceId;
        public string defendingAllianceName;
        public SiegeStatus status;
        public DateTime scheduledTime;
        public DateTime? startTime;
        public DateTime? endTime;
        public int attackerParticipants;
        public int defenderParticipants;
        public int attackerCasualties;
        public int defenderCasualties;
        public int wallsBreached;
        public int towersDestroyed;
        public float citadelHealthPercent;
        public bool isMyAlliance;
        public bool isDefending;
        public List<SiegeUnit> attackerUnits = new List<SiegeUnit>();
        public List<SiegeUnit> defenderUnits = new List<SiegeUnit>();
        public List<DefenseStructure> defenses = new List<DefenseStructure>();
    }

    // Siege unit
    [System.Serializable]
    public class SiegeUnit
    {
        public string unitId;
        public SiegeUnitType type;
        public string ownerName;
        public int count;
        public int health;
        public int maxHealth;
        public int damage;
        public Vector2 position;
        public bool isDeployed;
    }

    // Defense structure
    [System.Serializable]
    public class DefenseStructure
    {
        public string structureId;
        public DefenseStructureType type;
        public int health;
        public int maxHealth;
        public int level;
        public Vector2 position;
        public bool isDestroyed;
    }

    public class SiegeWarfarePanel : MonoBehaviour
    {
        public static SiegeWarfarePanel Instance { get; private set; }

        [Header("Runtime References")]
        private Canvas parentCanvas;
        private GameObject panelRoot;
        private bool isVisible = false;

        // UI References
        private RectTransform mainPanel;
        private RectTransform headerPanel;
        private RectTransform contentPanel;
        private RectTransform battlefieldView;
        private RectTransform unitPanel;
        private RectTransform miniMap;
        private RectTransform actionPanel;
        private ScrollRect siegeListScroll;
        private TMP_Dropdown tabDropdown;
        private TextMeshProUGUI headerTitle;
        private TextMeshProUGUI siegeTimer;
        private TextMeshProUGUI battleStatus;

        // State
        private SiegeViewTab currentTab = SiegeViewTab.ActiveSieges;
        private List<SiegeData> siegeList = new List<SiegeData>();
        private SiegeData currentSiege;
        private SiegeUnit selectedUnit;
        private DefenseStructure selectedDefense;
        private List<GameObject> siegeItems = new List<GameObject>();
        private List<GameObject> unitIcons = new List<GameObject>();
        private List<GameObject> defenseIcons = new List<GameObject>();

        // Battlefield state
        private bool isBattlefieldMode = false;
        private Camera battlefieldCamera;
        private float zoomLevel = 1f;
        private Vector2 viewOffset = Vector2.zero;

        // Colors
        private readonly Color ATTACKER_COLOR = new Color(0.9f, 0.3f, 0.2f);
        private readonly Color DEFENDER_COLOR = new Color(0.2f, 0.6f, 0.9f);
        private readonly Color NEUTRAL_COLOR = new Color(0.6f, 0.6f, 0.6f);
        private readonly Color VICTORY_COLOR = new Color(0.2f, 0.8f, 0.3f);
        private readonly Color DEFEAT_COLOR = new Color(0.8f, 0.2f, 0.2f);
        private readonly Color WALL_COLOR = new Color(0.5f, 0.4f, 0.3f);
        private readonly Color STRUCTURE_COLOR = new Color(0.6f, 0.5f, 0.4f);

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
            if (isVisible && currentSiege != null)
            {
                UpdateSiegeTimer();
                
                if (isBattlefieldMode)
                {
                    UpdateBattlefieldView();
                    HandleBattlefieldInput();
                }
            }

            // Toggle with K key
            if (Input.GetKeyDown(KeyCode.K))
            {
                if (isVisible) Hide();
                else Show();
            }
        }

        private void CreateUI()
        {
            // Panel root
            panelRoot = new GameObject("SiegeWarfarePanel_Root");
            panelRoot.transform.SetParent(parentCanvas.transform, false);

            mainPanel = panelRoot.AddComponent<RectTransform>();
            mainPanel.anchorMin = new Vector2(0.08f, 0.08f);
            mainPanel.anchorMax = new Vector2(0.92f, 0.92f);
            mainPanel.offsetMin = Vector2.zero;
            mainPanel.offsetMax = Vector2.zero;

            // Background
            Image mainBg = panelRoot.AddComponent<Image>();
            mainBg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);

            // Add layout
            VerticalLayoutGroup layout = panelRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.spacing = 10;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            CreateHeader();
            CreateTabBar();
            CreateContentArea();
            CreateActionPanel();

            LoadMockSieges();
            RefreshSiegeList();
        }

        private void CreateHeader()
        {
            // Header container
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(panelRoot.transform, false);

            headerPanel = headerObj.AddComponent<RectTransform>();
            LayoutElement headerLE = headerObj.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 60;
            headerLE.flexibleWidth = 1;

            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0.6f, 0.3f, 0.2f, 0.9f);

            HorizontalLayoutGroup headerLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(20, 20, 10, 10);
            headerLayout.spacing = 20;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(headerObj.transform, false);
            Image icon = iconObj.AddComponent<Image>();
            icon.color = Color.white;
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 40;
            iconLE.preferredHeight = 40;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            headerTitle = titleObj.AddComponent<TextMeshProUGUI>();
            headerTitle.text = "⚔️ SIEGE WARFARE";
            headerTitle.fontSize = 28;
            headerTitle.fontStyle = FontStyles.Bold;
            headerTitle.color = Color.white;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;

            // Timer
            GameObject timerObj = new GameObject("Timer");
            timerObj.transform.SetParent(headerObj.transform, false);
            siegeTimer = timerObj.AddComponent<TextMeshProUGUI>();
            siegeTimer.text = "00:00:00";
            siegeTimer.fontSize = 24;
            siegeTimer.fontStyle = FontStyles.Bold;
            siegeTimer.color = new Color(1f, 0.9f, 0.3f);
            siegeTimer.alignment = TextAlignmentOptions.Right;
            LayoutElement timerLE = timerObj.AddComponent<LayoutElement>();
            timerLE.preferredWidth = 120;

            // Close button
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(headerObj.transform, false);
            Image closeBg = closeObj.AddComponent<Image>();
            closeBg.color = new Color(0.8f, 0.2f, 0.2f);
            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
            LayoutElement closeLE = closeObj.AddComponent<LayoutElement>();
            closeLE.preferredWidth = 40;
            closeLE.preferredHeight = 40;

            GameObject closeText = new GameObject("X");
            closeText.transform.SetParent(closeObj.transform, false);
            TextMeshProUGUI closeTMP = closeText.AddComponent<TextMeshProUGUI>();
            closeTMP.text = "✕";
            closeTMP.fontSize = 24;
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
            tabLE.preferredHeight = 45;
            tabLE.flexibleWidth = 1;

            HorizontalLayoutGroup tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 5;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;

            string[] tabNames = { "Active Sieges", "Scheduled", "History", "Defense", "Attack" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int tabIndex = i;
                CreateTabButton(tabBar.transform, tabNames[i], () => SwitchTab((SiegeViewTab)tabIndex));
            }
        }

        private void CreateTabButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(text + "Tab");
            btnObj.transform.SetParent(parent, false);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.2f, 0.2f, 0.25f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.25f);
            colors.highlightedColor = new Color(0.4f, 0.3f, 0.25f);
            colors.pressedColor = new Color(0.5f, 0.35f, 0.2f);
            colors.selectedColor = new Color(0.6f, 0.3f, 0.2f);
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
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
            contentBg.color = new Color(0.12f, 0.12f, 0.15f);

            HorizontalLayoutGroup contentLayout = contentObj.AddComponent<HorizontalLayoutGroup>();
            contentLayout.padding = new RectOffset(10, 10, 10, 10);
            contentLayout.spacing = 10;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;

            // Left: Siege list
            CreateSiegeListPanel(contentObj.transform);
            
            // Center: Battlefield view
            CreateBattlefieldPanel(contentObj.transform);
            
            // Right: Unit panel
            CreateUnitPanel(contentObj.transform);
        }

        private void CreateSiegeListPanel(Transform parent)
        {
            GameObject listPanel = new GameObject("SiegeList");
            listPanel.transform.SetParent(parent, false);

            RectTransform listRT = listPanel.AddComponent<RectTransform>();
            LayoutElement listLE = listPanel.AddComponent<LayoutElement>();
            listLE.preferredWidth = 280;
            listLE.flexibleHeight = 1;

            Image listBg = listPanel.AddComponent<Image>();
            listBg.color = new Color(0.1f, 0.1f, 0.12f);

            VerticalLayoutGroup listLayout = listPanel.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(8, 8, 8, 8);
            listLayout.spacing = 5;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;

            // List title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(listPanel.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "SIEGE OPERATIONS";
            titleTMP.fontSize = 16;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0.9f, 0.5f, 0.3f);
            titleTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 30;

            // Scroll view for siege list
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(listPanel.transform, false);

            RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
            LayoutElement scrollLE = scrollObj.AddComponent<LayoutElement>();
            scrollLE.flexibleHeight = 1;
            scrollLE.flexibleWidth = 1;

            siegeListScroll = scrollObj.AddComponent<ScrollRect>();
            siegeListScroll.horizontal = false;
            siegeListScroll.vertical = true;
            siegeListScroll.movementType = ScrollRect.MovementType.Elastic;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.08f, 0.08f, 0.1f);

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

            siegeListScroll.viewport = viewportRT;

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
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.spacing = 8;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;

            ContentSizeFitter csf = contentContainerObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            siegeListScroll.content = contentContainerRT;
        }

        private void CreateBattlefieldPanel(Transform parent)
        {
            GameObject bfPanel = new GameObject("Battlefield");
            bfPanel.transform.SetParent(parent, false);

            battlefieldView = bfPanel.AddComponent<RectTransform>();
            LayoutElement bfLE = bfPanel.AddComponent<LayoutElement>();
            bfLE.flexibleWidth = 1;
            bfLE.flexibleHeight = 1;

            Image bfBg = bfPanel.AddComponent<Image>();
            bfBg.color = new Color(0.15f, 0.18f, 0.15f);

            // Add a raw image for potential render texture
            RawImage bfView = bfPanel.AddComponent<RawImage>();
            bfView.color = new Color(0.2f, 0.25f, 0.2f);

            // Status overlay
            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(bfPanel.transform, false);
            
            RectTransform statusRT = statusObj.AddComponent<RectTransform>();
            statusRT.anchorMin = new Vector2(0, 1);
            statusRT.anchorMax = new Vector2(1, 1);
            statusRT.pivot = new Vector2(0.5f, 1);
            statusRT.anchoredPosition = new Vector2(0, -10);
            statusRT.sizeDelta = new Vector2(0, 60);

            Image statusBg = statusObj.AddComponent<Image>();
            statusBg.color = new Color(0, 0, 0, 0.7f);

            battleStatus = statusObj.AddComponent<TextMeshProUGUI>();
            battleStatus.text = "Select a siege to view battlefield";
            battleStatus.fontSize = 18;
            battleStatus.alignment = TextAlignmentOptions.Center;
            battleStatus.color = Color.white;
            battleStatus.margin = new Vector4(10, 10, 10, 10);

            // Mini map
            CreateMiniMap(bfPanel.transform);
        }

        private void CreateMiniMap(Transform parent)
        {
            GameObject mmObj = new GameObject("MiniMap");
            mmObj.transform.SetParent(parent, false);

            miniMap = mmObj.AddComponent<RectTransform>();
            miniMap.anchorMin = new Vector2(1, 0);
            miniMap.anchorMax = new Vector2(1, 0);
            miniMap.pivot = new Vector2(1, 0);
            miniMap.anchoredPosition = new Vector2(-10, 10);
            miniMap.sizeDelta = new Vector2(150, 150);

            Image mmBg = mmObj.AddComponent<Image>();
            mmBg.color = new Color(0.1f, 0.12f, 0.1f, 0.9f);

            // Border - using shadow as outline effect
            Shadow mmShadow = mmObj.AddComponent<Shadow>();
            mmShadow.effectColor = new Color(0.4f, 0.3f, 0.2f);
            mmShadow.effectDistance = new Vector2(2, -2);
        }

        private void CreateUnitPanel(Transform parent)
        {
            GameObject unitObj = new GameObject("UnitPanel");
            unitObj.transform.SetParent(parent, false);

            unitPanel = unitObj.AddComponent<RectTransform>();
            LayoutElement unitLE = unitObj.AddComponent<LayoutElement>();
            unitLE.preferredWidth = 220;
            unitLE.flexibleHeight = 1;

            Image unitBg = unitObj.AddComponent<Image>();
            unitBg.color = new Color(0.1f, 0.1f, 0.12f);

            VerticalLayoutGroup unitLayout = unitObj.AddComponent<VerticalLayoutGroup>();
            unitLayout.padding = new RectOffset(10, 10, 10, 10);
            unitLayout.spacing = 8;
            unitLayout.childForceExpandWidth = true;
            unitLayout.childForceExpandHeight = false;
            unitLayout.childControlWidth = true;
            unitLayout.childControlHeight = true;

            // Unit title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(unitObj.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "⚔️ YOUR FORCES";
            titleTMP.fontSize = 16;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0.9f, 0.8f, 0.3f);
            titleTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 30;

            // Unit categories
            CreateUnitCategory(unitObj.transform, "Infantry", SiegeUnitType.Infantry);
            CreateUnitCategory(unitObj.transform, "Ranged", SiegeUnitType.Archer);
            CreateUnitCategory(unitObj.transform, "Cavalry", SiegeUnitType.Cavalry);
            CreateUnitCategory(unitObj.transform, "Siege Weapons", SiegeUnitType.Catapult);
        }

        private void CreateUnitCategory(Transform parent, string name, SiegeUnitType type)
        {
            GameObject catObj = new GameObject(name);
            catObj.transform.SetParent(parent, false);

            RectTransform catRT = catObj.AddComponent<RectTransform>();
            LayoutElement catLE = catObj.AddComponent<LayoutElement>();
            catLE.preferredHeight = 70;
            catLE.flexibleWidth = 1;

            Image catBg = catObj.AddComponent<Image>();
            catBg.color = new Color(0.15f, 0.15f, 0.18f);

            VerticalLayoutGroup catLayout = catObj.AddComponent<VerticalLayoutGroup>();
            catLayout.padding = new RectOffset(8, 8, 5, 5);
            catLayout.spacing = 3;
            catLayout.childForceExpandWidth = true;
            catLayout.childForceExpandHeight = false;

            // Category name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(catObj.transform, false);
            TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = name;
            nameTMP.fontSize = 14;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color = Color.white;
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 20;

            // Unit count
            GameObject countObj = new GameObject("Count");
            countObj.transform.SetParent(catObj.transform, false);
            TextMeshProUGUI countTMP = countObj.AddComponent<TextMeshProUGUI>();
            countTMP.text = "Available: 0";
            countTMP.fontSize = 12;
            countTMP.color = new Color(0.7f, 0.7f, 0.7f);
            LayoutElement countLE = countObj.AddComponent<LayoutElement>();
            countLE.preferredHeight = 18;

            // Deploy button
            GameObject deployObj = new GameObject("Deploy");
            deployObj.transform.SetParent(catObj.transform, false);
            Image deployBg = deployObj.AddComponent<Image>();
            deployBg.color = new Color(0.3f, 0.5f, 0.3f);
            Button deployBtn = deployObj.AddComponent<Button>();
            deployBtn.onClick.AddListener(() => DeployUnit(type));
            LayoutElement deployLE = deployObj.AddComponent<LayoutElement>();
            deployLE.preferredHeight = 25;

            GameObject deployText = new GameObject("Text");
            deployText.transform.SetParent(deployObj.transform, false);
            TextMeshProUGUI deployTMP = deployText.AddComponent<TextMeshProUGUI>();
            deployTMP.text = "DEPLOY";
            deployTMP.fontSize = 12;
            deployTMP.alignment = TextAlignmentOptions.Center;
            deployTMP.color = Color.white;
            RectTransform deployTextRT = deployText.GetComponent<RectTransform>();
            deployTextRT.anchorMin = Vector2.zero;
            deployTextRT.anchorMax = Vector2.one;
            deployTextRT.offsetMin = Vector2.zero;
            deployTextRT.offsetMax = Vector2.zero;
        }

        private void CreateActionPanel()
        {
            // Action bar at bottom
            GameObject actionObj = new GameObject("ActionPanel");
            actionObj.transform.SetParent(panelRoot.transform, false);

            actionPanel = actionObj.AddComponent<RectTransform>();
            LayoutElement actionLE = actionObj.AddComponent<LayoutElement>();
            actionLE.preferredHeight = 70;
            actionLE.flexibleWidth = 1;

            Image actionBg = actionObj.AddComponent<Image>();
            actionBg.color = new Color(0.12f, 0.12f, 0.15f);

            HorizontalLayoutGroup actionLayout = actionObj.AddComponent<HorizontalLayoutGroup>();
            actionLayout.padding = new RectOffset(20, 20, 10, 10);
            actionLayout.spacing = 15;
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = true;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;

            // Create action buttons
            CreateActionButton(actionObj.transform, "⚔️ Join Attack", JoinAttack, new Color(0.8f, 0.3f, 0.2f));
            CreateActionButton(actionObj.transform, "🛡️ Join Defense", JoinDefense, new Color(0.2f, 0.5f, 0.8f));
            CreateActionButton(actionObj.transform, "📢 Rally Allies", RallyAllies, new Color(0.7f, 0.5f, 0.2f));
            CreateActionButton(actionObj.transform, "🔄 Retreat", Retreat, new Color(0.5f, 0.3f, 0.3f));
            CreateActionButton(actionObj.transform, "📋 Siege Log", ShowSiegeLog, new Color(0.3f, 0.3f, 0.4f));
        }

        private void CreateActionButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick, Color color)
        {
            GameObject btnObj = new GameObject(text);
            btnObj.transform.SetParent(parent, false);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = color;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            LayoutElement btnLE = btnObj.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 140;
            btnLE.preferredHeight = 45;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }

        private void LoadMockSieges()
        {
            siegeList.Clear();

            // Active siege
            siegeList.Add(new SiegeData
            {
                siegeId = "siege_001",
                targetCitadelId = "citadel_alpha",
                targetCitadelName = "Citadel Alpha",
                attackingAllianceId = "alliance_001",
                attackingAllianceName = "Iron Legion",
                defendingAllianceId = "alliance_002",
                defendingAllianceName = "Azure Guard",
                status = SiegeStatus.ActiveBattle,
                startTime = DateTime.Now.AddMinutes(-25),
                attackerParticipants = 15,
                defenderParticipants = 12,
                attackerCasualties = 234,
                defenderCasualties = 189,
                wallsBreached = 2,
                towersDestroyed = 1,
                citadelHealthPercent = 67,
                isMyAlliance = true,
                isDefending = false
            });

            // Scheduled siege
            siegeList.Add(new SiegeData
            {
                siegeId = "siege_002",
                targetCitadelId = "citadel_beta",
                targetCitadelName = "Citadel Beta",
                attackingAllianceId = "alliance_003",
                attackingAllianceName = "Storm Riders",
                defendingAllianceId = "alliance_001",
                defendingAllianceName = "Iron Legion",
                status = SiegeStatus.Scheduled,
                scheduledTime = DateTime.Now.AddHours(2),
                attackerParticipants = 8,
                defenderParticipants = 5,
                citadelHealthPercent = 100,
                isMyAlliance = true,
                isDefending = true
            });

            // Preparation phase siege
            siegeList.Add(new SiegeData
            {
                siegeId = "siege_003",
                targetCitadelId = "citadel_gamma",
                targetCitadelName = "Citadel Gamma",
                attackingAllianceId = "alliance_001",
                attackingAllianceName = "Iron Legion",
                defendingAllianceId = "alliance_004",
                defendingAllianceName = "Shadow Covenant",
                status = SiegeStatus.PreparationPhase,
                scheduledTime = DateTime.Now.AddMinutes(15),
                attackerParticipants = 20,
                defenderParticipants = 18,
                citadelHealthPercent = 100,
                isMyAlliance = true,
                isDefending = false
            });

            // Historical sieges
            siegeList.Add(new SiegeData
            {
                siegeId = "siege_004",
                targetCitadelId = "citadel_delta",
                targetCitadelName = "Citadel Delta",
                attackingAllianceId = "alliance_002",
                attackingAllianceName = "Azure Guard",
                defendingAllianceId = "alliance_001",
                defendingAllianceName = "Iron Legion",
                status = SiegeStatus.DefenseVictory,
                startTime = DateTime.Now.AddDays(-1),
                endTime = DateTime.Now.AddDays(-1).AddMinutes(45),
                attackerParticipants = 12,
                defenderParticipants = 14,
                attackerCasualties = 456,
                defenderCasualties = 234,
                wallsBreached = 1,
                towersDestroyed = 0,
                citadelHealthPercent = 42,
                isMyAlliance = true,
                isDefending = true
            });
        }

        private void RefreshSiegeList()
        {
            // Clear existing items
            foreach (var item in siegeItems)
            {
                Destroy(item);
            }
            siegeItems.Clear();

            // Filter by current tab
            var filtered = FilterSiegesByTab(currentTab);

            foreach (var siege in filtered)
            {
                CreateSiegeItem(siege);
            }
        }

        private List<SiegeData> FilterSiegesByTab(SiegeViewTab tab)
        {
            return tab switch
            {
                SiegeViewTab.ActiveSieges => siegeList.FindAll(s => s.status == SiegeStatus.ActiveBattle || s.status == SiegeStatus.PreparationPhase),
                SiegeViewTab.ScheduledSieges => siegeList.FindAll(s => s.status == SiegeStatus.Scheduled),
                SiegeViewTab.SiegeHistory => siegeList.FindAll(s => s.status == SiegeStatus.DefenseVictory || s.status == SiegeStatus.AttackVictory || s.status == SiegeStatus.Draw),
                SiegeViewTab.SiegeDefense => siegeList.FindAll(s => s.isMyAlliance && s.isDefending),
                SiegeViewTab.SiegeAttack => siegeList.FindAll(s => s.isMyAlliance && !s.isDefending),
                _ => siegeList
            };
        }

        private void CreateSiegeItem(SiegeData siege)
        {
            GameObject itemObj = new GameObject(siege.targetCitadelName);
            itemObj.transform.SetParent(siegeListScroll.content, false);

            RectTransform itemRT = itemObj.AddComponent<RectTransform>();
            LayoutElement itemLE = itemObj.AddComponent<LayoutElement>();
            itemLE.preferredHeight = 90;
            itemLE.flexibleWidth = 1;

            // Determine background color based on status
            Color bgColor = siege.status switch
            {
                SiegeStatus.ActiveBattle => new Color(0.4f, 0.2f, 0.2f),
                SiegeStatus.PreparationPhase => new Color(0.3f, 0.3f, 0.2f),
                SiegeStatus.Scheduled => new Color(0.2f, 0.25f, 0.3f),
                SiegeStatus.DefenseVictory => new Color(0.2f, 0.35f, 0.2f),
                SiegeStatus.AttackVictory => new Color(0.35f, 0.25f, 0.2f),
                _ => new Color(0.2f, 0.2f, 0.2f)
            };

            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.color = bgColor;

            Button itemBtn = itemObj.AddComponent<Button>();
            itemBtn.onClick.AddListener(() => SelectSiege(siege));

            VerticalLayoutGroup itemLayout = itemObj.AddComponent<VerticalLayoutGroup>();
            itemLayout.padding = new RectOffset(10, 10, 8, 8);
            itemLayout.spacing = 4;
            itemLayout.childForceExpandWidth = true;
            itemLayout.childForceExpandHeight = false;

            // Target name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = $"🏰 {siege.targetCitadelName}";
            nameTMP.fontSize = 16;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color = Color.white;
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 22;

            // Alliances
            GameObject allianceObj = new GameObject("Alliances");
            allianceObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI allianceTMP = allianceObj.AddComponent<TextMeshProUGUI>();
            allianceTMP.text = $"⚔️ {siege.attackingAllianceName} vs 🛡️ {siege.defendingAllianceName}";
            allianceTMP.fontSize = 12;
            allianceTMP.color = new Color(0.8f, 0.8f, 0.8f);
            LayoutElement allianceLE = allianceObj.AddComponent<LayoutElement>();
            allianceLE.preferredHeight = 18;

            // Status line
            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI statusTMP = statusObj.AddComponent<TextMeshProUGUI>();
            
            string statusText = siege.status switch
            {
                SiegeStatus.ActiveBattle => $"🔴 ACTIVE - Health: {siege.citadelHealthPercent}%",
                SiegeStatus.PreparationPhase => $"🟡 PREP PHASE - Starts soon",
                SiegeStatus.Scheduled => $"📅 Scheduled: {siege.scheduledTime:MMM dd HH:mm}",
                SiegeStatus.DefenseVictory => "🛡️ Defense Victory",
                SiegeStatus.AttackVictory => "⚔️ Attack Victory",
                _ => siege.status.ToString()
            };
            
            statusTMP.text = statusText;
            statusTMP.fontSize = 12;
            statusTMP.color = GetStatusColor(siege.status);
            LayoutElement statusLE = statusObj.AddComponent<LayoutElement>();
            statusLE.preferredHeight = 18;

            // Participants
            GameObject participantsObj = new GameObject("Participants");
            participantsObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI participantsTMP = participantsObj.AddComponent<TextMeshProUGUI>();
            participantsTMP.text = $"👥 {siege.attackerParticipants} vs {siege.defenderParticipants}";
            participantsTMP.fontSize = 11;
            participantsTMP.color = new Color(0.6f, 0.6f, 0.6f);
            LayoutElement participantsLE = participantsObj.AddComponent<LayoutElement>();
            participantsLE.preferredHeight = 16;

            siegeItems.Add(itemObj);
        }

        private Color GetStatusColor(SiegeStatus status)
        {
            return status switch
            {
                SiegeStatus.ActiveBattle => new Color(1f, 0.4f, 0.3f),
                SiegeStatus.PreparationPhase => new Color(1f, 0.9f, 0.3f),
                SiegeStatus.Scheduled => new Color(0.5f, 0.7f, 1f),
                SiegeStatus.DefenseVictory => VICTORY_COLOR,
                SiegeStatus.AttackVictory => VICTORY_COLOR,
                SiegeStatus.Draw => NEUTRAL_COLOR,
                _ => Color.white
            };
        }

        private void SelectSiege(SiegeData siege)
        {
            currentSiege = siege;
            isBattlefieldMode = siege.status == SiegeStatus.ActiveBattle || siege.status == SiegeStatus.PreparationPhase;
            
            UpdateBattleStatus();
            Debug.Log($"[SiegeWarfare] Selected siege: {siege.targetCitadelName} - {siege.status}");
        }

        private void UpdateBattleStatus()
        {
            if (currentSiege == null)
            {
                battleStatus.text = "Select a siege to view battlefield";
                return;
            }

            string statusLine = currentSiege.status switch
            {
                SiegeStatus.ActiveBattle => $"⚔️ BATTLE IN PROGRESS - Citadel Health: {currentSiege.citadelHealthPercent}%\n" +
                    $"Walls Breached: {currentSiege.wallsBreached} | Towers Destroyed: {currentSiege.towersDestroyed}\n" +
                    $"Casualties - Attackers: {currentSiege.attackerCasualties} | Defenders: {currentSiege.defenderCasualties}",
                SiegeStatus.PreparationPhase => $"🟡 PREPARATION PHASE\nDeploy your units and prepare defenses!\n" +
                    $"Battle starts in: {GetTimeUntil(currentSiege.scheduledTime)}",
                SiegeStatus.Scheduled => $"📅 SCHEDULED SIEGE\nStarts: {currentSiege.scheduledTime:MMMM dd, HH:mm}\n" +
                    $"Time until siege: {GetTimeUntil(currentSiege.scheduledTime)}",
                _ => $"Siege Result: {currentSiege.status}"
            };

            battleStatus.text = statusLine;
        }

        private void UpdateSiegeTimer()
        {
            if (currentSiege == null) return;

            TimeSpan elapsed;
            if (currentSiege.status == SiegeStatus.ActiveBattle && currentSiege.startTime.HasValue)
            {
                elapsed = DateTime.Now - currentSiege.startTime.Value;
                siegeTimer.text = elapsed.ToString(@"hh\:mm\:ss");
                siegeTimer.color = new Color(1f, 0.4f, 0.3f);
            }
            else if (currentSiege.status == SiegeStatus.Scheduled || currentSiege.status == SiegeStatus.PreparationPhase)
            {
                var remaining = currentSiege.scheduledTime - DateTime.Now;
                siegeTimer.text = remaining.TotalSeconds > 0 ? remaining.ToString(@"hh\:mm\:ss") : "STARTING...";
                siegeTimer.color = new Color(1f, 0.9f, 0.3f);
            }
            else
            {
                siegeTimer.text = "--:--:--";
                siegeTimer.color = Color.gray;
            }
        }

        private string GetTimeUntil(DateTime target)
        {
            var remaining = target - DateTime.Now;
            if (remaining.TotalSeconds < 0) return "Starting now!";
            if (remaining.TotalMinutes < 60) return $"{remaining.Minutes}m {remaining.Seconds}s";
            return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
        }

        private void UpdateBattlefieldView()
        {
            // Update unit positions, health bars, etc.
            // This would render the actual siege battlefield
        }

        private void HandleBattlefieldInput()
        {
            // Pan
            if (Input.GetMouseButton(1)) // Right click drag
            {
                viewOffset += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 5f;
            }

            // Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                zoomLevel = Mathf.Clamp(zoomLevel + scroll * 0.5f, 0.5f, 3f);
            }
        }

        private void SwitchTab(SiegeViewTab tab)
        {
            currentTab = tab;
            RefreshSiegeList();
            Debug.Log($"[SiegeWarfare] Switched to tab: {tab}");
        }

        // Action handlers
        private void DeployUnit(SiegeUnitType type)
        {
            if (currentSiege == null)
            {
                Debug.Log("[SiegeWarfare] No siege selected for deployment");
                return;
            }
            Debug.Log($"[SiegeWarfare] Deploying {type} units");
        }

        private void JoinAttack()
        {
            if (currentSiege == null) return;
            Debug.Log($"[SiegeWarfare] Joining attack on {currentSiege.targetCitadelName}");
        }

        private void JoinDefense()
        {
            if (currentSiege == null) return;
            Debug.Log($"[SiegeWarfare] Joining defense of {currentSiege.targetCitadelName}");
        }

        private void RallyAllies()
        {
            if (currentSiege == null) return;
            Debug.Log($"[SiegeWarfare] Rallying allies for {currentSiege.targetCitadelName}");
        }

        private void Retreat()
        {
            if (currentSiege == null) return;
            Debug.Log($"[SiegeWarfare] Retreating from {currentSiege.targetCitadelName}");
        }

        private void ShowSiegeLog()
        {
            Debug.Log("[SiegeWarfare] Showing siege log");
        }

        // Public API
        public void Show()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                isVisible = true;
                RefreshSiegeList();
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
                isVisible = false;
                isBattlefieldMode = false;
            }
        }

        public void Toggle()
        {
            if (isVisible) Hide();
            else Show();
        }

        public void NotifySiegeStarted(SiegeData siege)
        {
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo(
                    $"⚔️ SIEGE STARTED: {siege.targetCitadelName}"
                );
            }
        }
    }
}
