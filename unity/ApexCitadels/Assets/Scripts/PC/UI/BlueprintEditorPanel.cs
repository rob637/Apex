// ============================================================================
// APEX CITADELS - BLUEPRINT EDITOR PANEL
// Design, save, and share base layouts for citadels
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using ApexCitadels.PC;

namespace ApexCitadels.PC.UI
{
    // Blueprint editor tabs
    public enum BlueprintTab
    {
        MyBlueprints,
        Community,
        Editor,
        Templates
    }

    // Building categories for blueprints
    public enum BlueprintBuildingCategory
    {
        Defense,
        Production,
        Military,
        Storage,
        Decoration,
        Special
    }

    // Blueprint sharing status
    public enum BlueprintShareStatus
    {
        Private,
        FriendsOnly,
        AllianceOnly,
        Public
    }

    // Blueprint building data
    [System.Serializable]
    public class BlueprintBuilding
    {
        public string buildingId;
        public string buildingType;
        public BlueprintBuildingCategory category;
        public Vector2Int gridPosition;
        public int rotation; // 0, 90, 180, 270
        public int level;
        public Vector2Int size;
    }

    // Blueprint data
    [System.Serializable]
    public class BlueprintData
    {
        public string blueprintId;
        public string name;
        public string description;
        public string authorId;
        public string authorName;
        public DateTime createdDate;
        public DateTime lastModified;
        public BlueprintShareStatus shareStatus;
        public int likes;
        public int downloads;
        public int uses;
        public Vector2Int gridSize;
        public List<BlueprintBuilding> buildings = new List<BlueprintBuilding>();
        public string[] tags;
        public string thumbnailId;
        public bool isFavorite;
        public bool isTemplate;
        public int estimatedCost;
        public int defenseRating;
        public int productionRating;
    }

    // Placeable building definition
    [System.Serializable]
    public class PlaceableBuildingDef
    {
        public string buildingType;
        public string displayName;
        public BlueprintBuildingCategory category;
        public Vector2Int size;
        public int baseCost;
        public string iconEmoji;
    }

    public class BlueprintEditorPanel : MonoBehaviour
    {
        public static BlueprintEditorPanel Instance { get; private set; }

        [Header("Runtime References")]
        private Canvas parentCanvas;
        private GameObject panelRoot;
        private bool isVisible = false;

        // UI References
        private RectTransform mainPanel;
        private RectTransform contentPanel;
        private RectTransform blueprintListPanel;
        private RectTransform editorPanel;
        private RectTransform palettePanel;
        private RectTransform propertiesPanel;
        private ScrollRect blueprintScroll;
        private ScrollRect paletteScroll;
        private RectTransform gridView;
        private TextMeshProUGUI headerTitle;
        private TextMeshProUGUI blueprintNameText;
        private TextMeshProUGUI statsText;
        private TMP_InputField searchInput;
        private TMP_InputField nameInput;
        private TMP_InputField descInput;

        // State
        private BlueprintTab currentTab = BlueprintTab.MyBlueprints;
        private List<BlueprintData> myBlueprints = new List<BlueprintData>();
        private List<BlueprintData> communityBlueprints = new List<BlueprintData>();
        private List<BlueprintData> templates = new List<BlueprintData>();
        private BlueprintData currentBlueprint;
        private BlueprintData selectedBlueprint;
        private List<GameObject> blueprintItems = new List<GameObject>();
        private List<GameObject> paletteItems = new List<GameObject>();
        private List<GameObject> gridCells = new List<GameObject>();
        private List<GameObject> placedBuildingIcons = new List<GameObject>();

        // Editor state
        private bool isEditing = false;
        private PlaceableBuildingDef selectedBuildingDef;
        private int currentRotation = 0;
        private Vector2Int hoveredCell = new Vector2Int(-1, -1);
        private const int GRID_SIZE = 20;
        private const float CELL_SIZE = 25f;

        // Building definitions
        private List<PlaceableBuildingDef> buildingDefs = new List<PlaceableBuildingDef>();

        // Colors
        private readonly Color GRID_COLOR = new Color(0.2f, 0.25f, 0.2f);
        private readonly Color GRID_LINE_COLOR = new Color(0.3f, 0.35f, 0.3f);
        private readonly Color VALID_PLACE_COLOR = new Color(0.3f, 0.7f, 0.3f, 0.5f);
        private readonly Color INVALID_PLACE_COLOR = new Color(0.7f, 0.3f, 0.3f, 0.5f);
        private readonly Color SELECTED_COLOR = new Color(1f, 0.9f, 0.3f);
        private readonly Color DEFENSE_COLOR = new Color(0.6f, 0.4f, 0.3f);
        private readonly Color PRODUCTION_COLOR = new Color(0.4f, 0.6f, 0.3f);
        private readonly Color MILITARY_COLOR = new Color(0.7f, 0.3f, 0.3f);

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
                InitBuildingDefinitions();
                CreateUI();
                Hide();
            }
        }

        private void Update()
        {
            if (isVisible && isEditing)
            {
                HandleEditorInput();
            }

            // Toggle with B key
            if (Input.GetKeyDown(KeyCode.B))
            {
                if (isVisible) Hide();
                else Show();
            }
        }

        private void InitBuildingDefinitions()
        {
            buildingDefs.Clear();

            // Defense buildings
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "wall", displayName = "Wall", category = BlueprintBuildingCategory.Defense, size = new Vector2Int(1, 1), baseCost = 100, iconEmoji = "🧱" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "tower", displayName = "Tower", category = BlueprintBuildingCategory.Defense, size = new Vector2Int(2, 2), baseCost = 500, iconEmoji = "🗼" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "gate", displayName = "Gate", category = BlueprintBuildingCategory.Defense, size = new Vector2Int(2, 1), baseCost = 300, iconEmoji = "🚪" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "moat", displayName = "Moat", category = BlueprintBuildingCategory.Defense, size = new Vector2Int(1, 1), baseCost = 150, iconEmoji = "🌊" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "ballista", displayName = "Ballista", category = BlueprintBuildingCategory.Defense, size = new Vector2Int(2, 2), baseCost = 800, iconEmoji = "🎯" });

            // Production buildings
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "farm", displayName = "Farm", category = BlueprintBuildingCategory.Production, size = new Vector2Int(3, 3), baseCost = 400, iconEmoji = "🌾" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "mine", displayName = "Mine", category = BlueprintBuildingCategory.Production, size = new Vector2Int(2, 2), baseCost = 600, iconEmoji = "⛏️" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "lumber", displayName = "Lumbermill", category = BlueprintBuildingCategory.Production, size = new Vector2Int(2, 2), baseCost = 350, iconEmoji = "🪓" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "quarry", displayName = "Quarry", category = BlueprintBuildingCategory.Production, size = new Vector2Int(3, 2), baseCost = 550, iconEmoji = "🪨" });

            // Military buildings
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "barracks", displayName = "Barracks", category = BlueprintBuildingCategory.Military, size = new Vector2Int(3, 3), baseCost = 1000, iconEmoji = "⚔️" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "archery", displayName = "Archery Range", category = BlueprintBuildingCategory.Military, size = new Vector2Int(3, 2), baseCost = 800, iconEmoji = "🏹" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "stable", displayName = "Stables", category = BlueprintBuildingCategory.Military, size = new Vector2Int(3, 3), baseCost = 1200, iconEmoji = "🐎" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "siege_workshop", displayName = "Siege Workshop", category = BlueprintBuildingCategory.Military, size = new Vector2Int(4, 3), baseCost = 2000, iconEmoji = "🔧" });

            // Storage buildings
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "warehouse", displayName = "Warehouse", category = BlueprintBuildingCategory.Storage, size = new Vector2Int(3, 2), baseCost = 700, iconEmoji = "📦" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "granary", displayName = "Granary", category = BlueprintBuildingCategory.Storage, size = new Vector2Int(2, 2), baseCost = 450, iconEmoji = "🏛️" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "treasury", displayName = "Treasury", category = BlueprintBuildingCategory.Storage, size = new Vector2Int(2, 2), baseCost = 1500, iconEmoji = "💰" });

            // Special buildings
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "castle", displayName = "Castle", category = BlueprintBuildingCategory.Special, size = new Vector2Int(4, 4), baseCost = 5000, iconEmoji = "🏰" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "market", displayName = "Market", category = BlueprintBuildingCategory.Special, size = new Vector2Int(3, 3), baseCost = 1200, iconEmoji = "🛒" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "academy", displayName = "Academy", category = BlueprintBuildingCategory.Special, size = new Vector2Int(3, 3), baseCost = 2500, iconEmoji = "📚" });

            // Decorations
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "statue", displayName = "Statue", category = BlueprintBuildingCategory.Decoration, size = new Vector2Int(1, 1), baseCost = 200, iconEmoji = "🗿" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "fountain", displayName = "Fountain", category = BlueprintBuildingCategory.Decoration, size = new Vector2Int(2, 2), baseCost = 400, iconEmoji = "⛲" });
            buildingDefs.Add(new PlaceableBuildingDef { buildingType = "garden", displayName = "Garden", category = BlueprintBuildingCategory.Decoration, size = new Vector2Int(2, 2), baseCost = 250, iconEmoji = "🌳" });
        }

        private void CreateUI()
        {
            // Panel root
            panelRoot = new GameObject("BlueprintEditorPanel_Root");
            panelRoot.transform.SetParent(parentCanvas.transform, false);

            mainPanel = panelRoot.AddComponent<RectTransform>();
            mainPanel.anchorMin = new Vector2(0.05f, 0.05f);
            mainPanel.anchorMax = new Vector2(0.95f, 0.95f);
            mainPanel.offsetMin = Vector2.zero;
            mainPanel.offsetMax = Vector2.zero;

            // Background
            Image mainBg = panelRoot.AddComponent<Image>();
            mainBg.color = new Color(0.1f, 0.12f, 0.15f, 0.98f);

            // Add layout
            VerticalLayoutGroup layout = panelRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            CreateHeader();
            CreateTabBar();
            CreateContentArea();
            CreateToolbar();

            LoadMockBlueprints();
            RefreshBlueprintList();
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
            headerBg.color = new Color(0.2f, 0.3f, 0.4f, 0.9f);

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
            iconTMP.text = "📐";
            iconTMP.fontSize = 28;
            iconTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 40;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            headerTitle = titleObj.AddComponent<TextMeshProUGUI>();
            headerTitle.text = "BLUEPRINT EDITOR";
            headerTitle.fontSize = 24;
            headerTitle.fontStyle = FontStyles.Bold;
            headerTitle.color = Color.white;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;

            // Search
            CreateSearchInput(headerObj.transform);

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

        private void CreateSearchInput(Transform parent)
        {
            GameObject searchObj = new GameObject("Search");
            searchObj.transform.SetParent(parent, false);

            RectTransform searchRT = searchObj.AddComponent<RectTransform>();
            LayoutElement searchLE = searchObj.AddComponent<LayoutElement>();
            searchLE.preferredWidth = 200;
            searchLE.preferredHeight = 35;

            Image searchBg = searchObj.AddComponent<Image>();
            searchBg.color = new Color(0.15f, 0.15f, 0.2f);

            searchInput = searchObj.AddComponent<TMP_InputField>();
            searchInput.onValueChanged.AddListener(OnSearchChanged);

            // Text area
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(searchObj.transform, false);
            RectTransform textAreaRT = textArea.AddComponent<RectTransform>();
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.offsetMin = new Vector2(10, 5);
            textAreaRT.offsetMax = new Vector2(-10, -5);

            // Placeholder
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI placeholderTMP = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderTMP.text = "🔍 Search blueprints...";
            placeholderTMP.fontSize = 14;
            placeholderTMP.color = new Color(0.5f, 0.5f, 0.5f);
            placeholderTMP.alignment = TextAlignmentOptions.Left;
            RectTransform placeholderRT = placeholder.GetComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = Vector2.zero;
            placeholderRT.offsetMax = Vector2.zero;

            // Input text
            GameObject inputText = new GameObject("Text");
            inputText.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI inputTMP = inputText.AddComponent<TextMeshProUGUI>();
            inputTMP.fontSize = 14;
            inputTMP.color = Color.white;
            inputTMP.alignment = TextAlignmentOptions.Left;
            RectTransform inputRT = inputText.GetComponent<RectTransform>();
            inputRT.anchorMin = Vector2.zero;
            inputRT.anchorMax = Vector2.one;
            inputRT.offsetMin = Vector2.zero;
            inputRT.offsetMax = Vector2.zero;

            searchInput.textViewport = textAreaRT;
            searchInput.textComponent = inputTMP;
            searchInput.placeholder = placeholderTMP;
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

            string[] tabNames = { "📁 My Blueprints", "🌐 Community", "✏️ Editor", "📋 Templates" };
            BlueprintTab[] tabValues = { BlueprintTab.MyBlueprints, BlueprintTab.Community, BlueprintTab.Editor, BlueprintTab.Templates };

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
            btnBg.color = new Color(0.2f, 0.25f, 0.3f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.25f, 0.3f);
            colors.highlightedColor = new Color(0.3f, 0.35f, 0.4f);
            colors.pressedColor = new Color(0.35f, 0.4f, 0.45f);
            colors.selectedColor = new Color(0.3f, 0.4f, 0.5f);
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
            contentBg.color = new Color(0.12f, 0.14f, 0.16f);

            HorizontalLayoutGroup contentLayout = contentObj.AddComponent<HorizontalLayoutGroup>();
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.spacing = 8;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;

            // Left: Blueprint list
            CreateBlueprintListPanel(contentObj.transform);
            
            // Center: Editor grid
            CreateEditorPanel(contentObj.transform);
            
            // Right: Palette/properties
            CreatePalettePanel(contentObj.transform);
        }

        private void CreateBlueprintListPanel(Transform parent)
        {
            GameObject listPanel = new GameObject("BlueprintList");
            listPanel.transform.SetParent(parent, false);

            blueprintListPanel = listPanel.AddComponent<RectTransform>();
            LayoutElement listLE = listPanel.AddComponent<LayoutElement>();
            listLE.preferredWidth = 240;
            listLE.flexibleHeight = 1;

            Image listBg = listPanel.AddComponent<Image>();
            listBg.color = new Color(0.1f, 0.12f, 0.14f);

            VerticalLayoutGroup listLayout = listPanel.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(5, 5, 5, 5);
            listLayout.spacing = 5;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(listPanel.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "BLUEPRINTS";
            titleTMP.fontSize = 14;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0.7f, 0.8f, 0.9f);
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

            blueprintScroll = scrollObj.AddComponent<ScrollRect>();
            blueprintScroll.horizontal = false;
            blueprintScroll.vertical = true;
            blueprintScroll.movementType = ScrollRect.MovementType.Elastic;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.08f, 0.1f, 0.12f);

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

            blueprintScroll.viewport = viewportRT;

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

            blueprintScroll.content = contentContainerRT;
        }

        private void CreateEditorPanel(Transform parent)
        {
            GameObject editorObj = new GameObject("Editor");
            editorObj.transform.SetParent(parent, false);

            editorPanel = editorObj.AddComponent<RectTransform>();
            LayoutElement editorLE = editorObj.AddComponent<LayoutElement>();
            editorLE.flexibleWidth = 1;
            editorLE.flexibleHeight = 1;

            Image editorBg = editorObj.AddComponent<Image>();
            editorBg.color = new Color(0.15f, 0.18f, 0.2f);

            // Grid container
            GameObject gridContainer = new GameObject("GridContainer");
            gridContainer.transform.SetParent(editorObj.transform, false);

            gridView = gridContainer.AddComponent<RectTransform>();
            gridView.anchorMin = new Vector2(0.5f, 0.5f);
            gridView.anchorMax = new Vector2(0.5f, 0.5f);
            gridView.pivot = new Vector2(0.5f, 0.5f);
            gridView.sizeDelta = new Vector2(GRID_SIZE * CELL_SIZE, GRID_SIZE * CELL_SIZE);

            Image gridBg = gridContainer.AddComponent<Image>();
            gridBg.color = GRID_COLOR;

            // Create grid cells
            CreateGridCells();

            // Blueprint name overlay
            GameObject nameOverlay = new GameObject("NameOverlay");
            nameOverlay.transform.SetParent(editorObj.transform, false);
            
            RectTransform nameRT = nameOverlay.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 1);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.pivot = new Vector2(0.5f, 1);
            nameRT.anchoredPosition = new Vector2(0, -5);
            nameRT.sizeDelta = new Vector2(0, 30);

            Image nameBg = nameOverlay.AddComponent<Image>();
            nameBg.color = new Color(0, 0, 0, 0.6f);

            blueprintNameText = nameOverlay.AddComponent<TextMeshProUGUI>();
            blueprintNameText.text = "New Blueprint";
            blueprintNameText.fontSize = 16;
            blueprintNameText.fontStyle = FontStyles.Bold;
            blueprintNameText.alignment = TextAlignmentOptions.Center;
            blueprintNameText.color = Color.white;

            // Stats overlay
            GameObject statsOverlay = new GameObject("StatsOverlay");
            statsOverlay.transform.SetParent(editorObj.transform, false);
            
            RectTransform statsRT = statsOverlay.AddComponent<RectTransform>();
            statsRT.anchorMin = new Vector2(0, 0);
            statsRT.anchorMax = new Vector2(1, 0);
            statsRT.pivot = new Vector2(0.5f, 0);
            statsRT.anchoredPosition = new Vector2(0, 5);
            statsRT.sizeDelta = new Vector2(0, 40);

            Image statsBg = statsOverlay.AddComponent<Image>();
            statsBg.color = new Color(0, 0, 0, 0.6f);

            statsText = statsOverlay.AddComponent<TextMeshProUGUI>();
            statsText.text = "Buildings: 0 | Cost: 0 | Defense: 0 | Production: 0";
            statsText.fontSize = 14;
            statsText.alignment = TextAlignmentOptions.Center;
            statsText.color = new Color(0.8f, 0.8f, 0.8f);
        }

        private void CreateGridCells()
        {
            for (int y = 0; y < GRID_SIZE; y++)
            {
                for (int x = 0; x < GRID_SIZE; x++)
                {
                    GameObject cellObj = new GameObject($"Cell_{x}_{y}");
                    cellObj.transform.SetParent(gridView, false);

                    RectTransform cellRT = cellObj.AddComponent<RectTransform>();
                    cellRT.anchorMin = Vector2.zero;
                    cellRT.anchorMax = Vector2.zero;
                    cellRT.pivot = Vector2.zero;
                    cellRT.anchoredPosition = new Vector2(x * CELL_SIZE, y * CELL_SIZE);
                    cellRT.sizeDelta = new Vector2(CELL_SIZE - 1, CELL_SIZE - 1);

                    Image cellImg = cellObj.AddComponent<Image>();
                    // Checkerboard pattern
                    cellImg.color = (x + y) % 2 == 0 ? GRID_COLOR : new Color(GRID_COLOR.r + 0.02f, GRID_COLOR.g + 0.02f, GRID_COLOR.b + 0.02f);

                    // Click handler
                    int cellX = x;
                    int cellY = y;
                    Button cellBtn = cellObj.AddComponent<Button>();
                    cellBtn.onClick.AddListener(() => OnCellClicked(cellX, cellY));

                    gridCells.Add(cellObj);
                }
            }
        }

        private void CreatePalettePanel(Transform parent)
        {
            GameObject paletteObj = new GameObject("Palette");
            paletteObj.transform.SetParent(parent, false);

            palettePanel = paletteObj.AddComponent<RectTransform>();
            LayoutElement paletteLE = paletteObj.AddComponent<LayoutElement>();
            paletteLE.preferredWidth = 200;
            paletteLE.flexibleHeight = 1;

            Image paletteBg = paletteObj.AddComponent<Image>();
            paletteBg.color = new Color(0.1f, 0.12f, 0.14f);

            VerticalLayoutGroup paletteLayout = paletteObj.AddComponent<VerticalLayoutGroup>();
            paletteLayout.padding = new RectOffset(5, 5, 5, 5);
            paletteLayout.spacing = 5;
            paletteLayout.childForceExpandWidth = true;
            paletteLayout.childForceExpandHeight = false;
            paletteLayout.childControlWidth = true;
            paletteLayout.childControlHeight = true;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(paletteObj.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "🏗️ BUILDING PALETTE";
            titleTMP.fontSize = 14;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0.9f, 0.8f, 0.5f);
            titleTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 25;

            // Scroll view for palette
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(paletteObj.transform, false);

            RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
            LayoutElement scrollLE = scrollObj.AddComponent<LayoutElement>();
            scrollLE.flexibleHeight = 1;
            scrollLE.flexibleWidth = 1;

            paletteScroll = scrollObj.AddComponent<ScrollRect>();
            paletteScroll.horizontal = false;
            paletteScroll.vertical = true;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.08f, 0.1f, 0.12f);

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

            paletteScroll.viewport = viewportRT;

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
            contentLayout.spacing = 3;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter csf = contentContainerObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            paletteScroll.content = contentContainerRT;

            // Populate palette by category
            PopulatePalette(contentContainerRT);
        }

        private void PopulatePalette(RectTransform parent)
        {
            var categories = new[] {
                BlueprintBuildingCategory.Defense,
                BlueprintBuildingCategory.Production,
                BlueprintBuildingCategory.Military,
                BlueprintBuildingCategory.Storage,
                BlueprintBuildingCategory.Special,
                BlueprintBuildingCategory.Decoration
            };

            foreach (var category in categories)
            {
                var buildings = buildingDefs.FindAll(b => b.category == category);
                if (buildings.Count == 0) continue;

                // Category header
                GameObject catHeader = new GameObject(category.ToString());
                catHeader.transform.SetParent(parent, false);
                TextMeshProUGUI catTMP = catHeader.AddComponent<TextMeshProUGUI>();
                catTMP.text = $"── {category} ──";
                catTMP.fontSize = 12;
                catTMP.fontStyle = FontStyles.Bold;
                catTMP.color = GetCategoryColor(category);
                catTMP.alignment = TextAlignmentOptions.Center;
                LayoutElement catLE = catHeader.AddComponent<LayoutElement>();
                catLE.preferredHeight = 22;

                // Buildings in category
                foreach (var building in buildings)
                {
                    CreatePaletteItem(parent, building);
                }
            }
        }

        private void CreatePaletteItem(RectTransform parent, PlaceableBuildingDef building)
        {
            GameObject itemObj = new GameObject(building.displayName);
            itemObj.transform.SetParent(parent, false);

            RectTransform itemRT = itemObj.AddComponent<RectTransform>();
            LayoutElement itemLE = itemObj.AddComponent<LayoutElement>();
            itemLE.preferredHeight = 40;
            itemLE.flexibleWidth = 1;

            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.color = new Color(0.15f, 0.18f, 0.2f);

            Button itemBtn = itemObj.AddComponent<Button>();
            itemBtn.onClick.AddListener(() => SelectBuildingDef(building));

            HorizontalLayoutGroup itemLayout = itemObj.AddComponent<HorizontalLayoutGroup>();
            itemLayout.padding = new RectOffset(8, 8, 5, 5);
            itemLayout.spacing = 8;
            itemLayout.childAlignment = TextAnchor.MiddleLeft;
            itemLayout.childForceExpandWidth = false;

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI iconTMP = iconObj.AddComponent<TextMeshProUGUI>();
            iconTMP.text = building.iconEmoji;
            iconTMP.fontSize = 20;
            iconTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 30;

            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = building.displayName;
            nameTMP.fontSize = 12;
            nameTMP.color = Color.white;
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;

            // Size
            GameObject sizeObj = new GameObject("Size");
            sizeObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI sizeTMP = sizeObj.AddComponent<TextMeshProUGUI>();
            sizeTMP.text = $"{building.size.x}x{building.size.y}";
            sizeTMP.fontSize = 10;
            sizeTMP.color = new Color(0.6f, 0.6f, 0.6f);
            sizeTMP.alignment = TextAlignmentOptions.Right;
            LayoutElement sizeLE = sizeObj.AddComponent<LayoutElement>();
            sizeLE.preferredWidth = 30;

            paletteItems.Add(itemObj);
        }

        private Color GetCategoryColor(BlueprintBuildingCategory category)
        {
            return category switch
            {
                BlueprintBuildingCategory.Defense => DEFENSE_COLOR,
                BlueprintBuildingCategory.Production => PRODUCTION_COLOR,
                BlueprintBuildingCategory.Military => MILITARY_COLOR,
                BlueprintBuildingCategory.Storage => new Color(0.5f, 0.5f, 0.6f),
                BlueprintBuildingCategory.Special => new Color(0.7f, 0.6f, 0.3f),
                BlueprintBuildingCategory.Decoration => new Color(0.5f, 0.7f, 0.5f),
                _ => Color.white
            };
        }

        private void CreateToolbar()
        {
            GameObject toolbarObj = new GameObject("Toolbar");
            toolbarObj.transform.SetParent(panelRoot.transform, false);

            RectTransform toolbarRT = toolbarObj.AddComponent<RectTransform>();
            LayoutElement toolbarLE = toolbarObj.AddComponent<LayoutElement>();
            toolbarLE.preferredHeight = 50;
            toolbarLE.flexibleWidth = 1;

            Image toolbarBg = toolbarObj.AddComponent<Image>();
            toolbarBg.color = new Color(0.12f, 0.14f, 0.16f);

            HorizontalLayoutGroup toolbarLayout = toolbarObj.AddComponent<HorizontalLayoutGroup>();
            toolbarLayout.padding = new RectOffset(15, 15, 8, 8);
            toolbarLayout.spacing = 10;
            toolbarLayout.childAlignment = TextAnchor.MiddleCenter;
            toolbarLayout.childForceExpandWidth = false;
            toolbarLayout.childForceExpandHeight = true;
            toolbarLayout.childControlWidth = true;
            toolbarLayout.childControlHeight = true;

            // Toolbar buttons
            CreateToolbarButton(toolbarObj.transform, "📄 New", NewBlueprint, new Color(0.3f, 0.4f, 0.5f));
            CreateToolbarButton(toolbarObj.transform, "💾 Save", SaveBlueprint, new Color(0.3f, 0.5f, 0.3f));
            CreateToolbarButton(toolbarObj.transform, "📂 Load", LoadBlueprint, new Color(0.4f, 0.4f, 0.5f));
            CreateToolbarButton(toolbarObj.transform, "🔄 Rotate (R)", RotateBuilding, new Color(0.4f, 0.4f, 0.4f));
            CreateToolbarButton(toolbarObj.transform, "🗑️ Delete (Del)", DeleteBuilding, new Color(0.5f, 0.3f, 0.3f));
            CreateToolbarButton(toolbarObj.transform, "↩️ Undo", UndoAction, new Color(0.4f, 0.35f, 0.3f));
            CreateToolbarButton(toolbarObj.transform, "📤 Share", ShareBlueprint, new Color(0.3f, 0.4f, 0.5f));
            CreateToolbarButton(toolbarObj.transform, "🏗️ Apply", ApplyBlueprint, new Color(0.5f, 0.4f, 0.2f));
        }

        private void CreateToolbarButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick, Color color)
        {
            GameObject btnObj = new GameObject(text);
            btnObj.transform.SetParent(parent, false);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = color;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            LayoutElement btnLE = btnObj.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 100;
            btnLE.preferredHeight = 35;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 12;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }

        private void LoadMockBlueprints()
        {
            myBlueprints.Clear();
            communityBlueprints.Clear();

            // My blueprints
            myBlueprints.Add(new BlueprintData
            {
                blueprintId = "bp_001",
                name = "Fortress Alpha",
                description = "A well-balanced defensive layout",
                authorId = "player_001",
                authorName = "Me",
                createdDate = DateTime.Now.AddDays(-5),
                shareStatus = BlueprintShareStatus.Private,
                gridSize = new Vector2Int(20, 20),
                estimatedCost = 15000,
                defenseRating = 85,
                productionRating = 60,
                tags = new[] { "defense", "balanced" }
            });

            myBlueprints.Add(new BlueprintData
            {
                blueprintId = "bp_002",
                name = "Economy Focus",
                description = "Maximum resource production",
                authorId = "player_001",
                authorName = "Me",
                createdDate = DateTime.Now.AddDays(-3),
                shareStatus = BlueprintShareStatus.AllianceOnly,
                gridSize = new Vector2Int(20, 20),
                estimatedCost = 12000,
                defenseRating = 40,
                productionRating = 95,
                tags = new[] { "economy", "production" }
            });

            // Community blueprints
            communityBlueprints.Add(new BlueprintData
            {
                blueprintId = "bp_comm_001",
                name = "The Impregnable",
                description = "Ultimate defensive layout - unbreakable walls!",
                authorId = "player_042",
                authorName = "CastleMaster",
                createdDate = DateTime.Now.AddDays(-10),
                shareStatus = BlueprintShareStatus.Public,
                likes = 234,
                downloads = 567,
                gridSize = new Vector2Int(20, 20),
                estimatedCost = 25000,
                defenseRating = 98,
                productionRating = 30,
                tags = new[] { "defense", "walls", "competitive" }
            });

            communityBlueprints.Add(new BlueprintData
            {
                blueprintId = "bp_comm_002",
                name = "Starter Layout",
                description = "Perfect for new players",
                authorId = "player_015",
                authorName = "HelpfulVeteran",
                createdDate = DateTime.Now.AddDays(-20),
                shareStatus = BlueprintShareStatus.Public,
                likes = 892,
                downloads = 2341,
                gridSize = new Vector2Int(20, 20),
                estimatedCost = 5000,
                defenseRating = 50,
                productionRating = 70,
                tags = new[] { "starter", "beginner", "tutorial" }
            });

            // Templates
            templates.Add(new BlueprintData
            {
                blueprintId = "template_001",
                name = "Defense Template",
                description = "Official template - heavy defense",
                isTemplate = true,
                defenseRating = 90,
                productionRating = 40,
                gridSize = new Vector2Int(20, 20)
            });

            templates.Add(new BlueprintData
            {
                blueprintId = "template_002",
                name = "Production Template",
                description = "Official template - high production",
                isTemplate = true,
                defenseRating = 40,
                productionRating = 90,
                gridSize = new Vector2Int(20, 20)
            });
        }

        private void RefreshBlueprintList()
        {
            // Clear existing
            foreach (var item in blueprintItems)
            {
                Destroy(item);
            }
            blueprintItems.Clear();

            // Get list based on tab
            var list = currentTab switch
            {
                BlueprintTab.MyBlueprints => myBlueprints,
                BlueprintTab.Community => communityBlueprints,
                BlueprintTab.Templates => templates,
                _ => new List<BlueprintData>()
            };

            foreach (var bp in list)
            {
                CreateBlueprintItem(bp);
            }
        }

        private void CreateBlueprintItem(BlueprintData blueprint)
        {
            GameObject itemObj = new GameObject(blueprint.name);
            itemObj.transform.SetParent(blueprintScroll.content, false);

            RectTransform itemRT = itemObj.AddComponent<RectTransform>();
            LayoutElement itemLE = itemObj.AddComponent<LayoutElement>();
            itemLE.preferredHeight = 80;
            itemLE.flexibleWidth = 1;

            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.color = new Color(0.15f, 0.18f, 0.2f);

            Button itemBtn = itemObj.AddComponent<Button>();
            itemBtn.onClick.AddListener(() => SelectBlueprint(blueprint));

            VerticalLayoutGroup itemLayout = itemObj.AddComponent<VerticalLayoutGroup>();
            itemLayout.padding = new RectOffset(8, 8, 5, 5);
            itemLayout.spacing = 3;
            itemLayout.childForceExpandWidth = true;
            itemLayout.childForceExpandHeight = false;

            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = blueprint.name;
            nameTMP.fontSize = 14;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color = Color.white;
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 20;

            // Author
            GameObject authorObj = new GameObject("Author");
            authorObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI authorTMP = authorObj.AddComponent<TextMeshProUGUI>();
            authorTMP.text = blueprint.isTemplate ? "📋 Official Template" : $"by {blueprint.authorName}";
            authorTMP.fontSize = 11;
            authorTMP.color = new Color(0.6f, 0.6f, 0.6f);
            LayoutElement authorLE = authorObj.AddComponent<LayoutElement>();
            authorLE.preferredHeight = 16;

            // Stats
            GameObject statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI statsTMP = statsObj.AddComponent<TextMeshProUGUI>();
            statsTMP.text = $"🛡️ {blueprint.defenseRating} | ⚙️ {blueprint.productionRating}";
            statsTMP.fontSize = 11;
            statsTMP.color = new Color(0.7f, 0.7f, 0.7f);
            LayoutElement statsLE = statsObj.AddComponent<LayoutElement>();
            statsLE.preferredHeight = 16;

            // Community stats (likes/downloads)
            if (blueprint.shareStatus == BlueprintShareStatus.Public)
            {
                GameObject communityObj = new GameObject("Community");
                communityObj.transform.SetParent(itemObj.transform, false);
                TextMeshProUGUI communityTMP = communityObj.AddComponent<TextMeshProUGUI>();
                communityTMP.text = $"❤️ {blueprint.likes} | ⬇️ {blueprint.downloads}";
                communityTMP.fontSize = 10;
                communityTMP.color = new Color(0.5f, 0.6f, 0.7f);
                LayoutElement communityLE = communityObj.AddComponent<LayoutElement>();
                communityLE.preferredHeight = 14;
            }

            blueprintItems.Add(itemObj);
        }

        private void HandleEditorInput()
        {
            // Rotate with R
            if (Input.GetKeyDown(KeyCode.R))
            {
                RotateBuilding();
            }

            // Delete with Delete key
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                DeleteBuilding();
            }

            // Cancel selection with Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                selectedBuildingDef = null;
            }
        }

        private void OnCellClicked(int x, int y)
        {
            if (!isEditing || selectedBuildingDef == null) return;

            // Check if we can place here
            if (CanPlaceBuilding(x, y, selectedBuildingDef))
            {
                PlaceBuilding(x, y, selectedBuildingDef);
            }
            else
            {
                Debug.Log($"[Blueprint] Cannot place {selectedBuildingDef.displayName} at ({x}, {y})");
            }
        }

        private bool CanPlaceBuilding(int x, int y, PlaceableBuildingDef building)
        {
            // Check bounds
            Vector2Int size = GetRotatedSize(building.size, currentRotation);
            if (x + size.x > GRID_SIZE || y + size.y > GRID_SIZE) return false;
            if (x < 0 || y < 0) return false;

            // Check for overlaps with existing buildings
            if (currentBlueprint != null)
            {
                foreach (var existingBuilding in currentBlueprint.buildings)
                {
                    if (BuildingsOverlap(x, y, size, existingBuilding.gridPosition, existingBuilding.size))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool BuildingsOverlap(int x1, int y1, Vector2Int size1, Vector2Int pos2, Vector2Int size2)
        {
            return !(x1 + size1.x <= pos2.x || 
                     x1 >= pos2.x + size2.x || 
                     y1 + size1.y <= pos2.y || 
                     y1 >= pos2.y + size2.y);
        }

        private Vector2Int GetRotatedSize(Vector2Int size, int rotation)
        {
            if (rotation == 90 || rotation == 270)
                return new Vector2Int(size.y, size.x);
            return size;
        }

        private void PlaceBuilding(int x, int y, PlaceableBuildingDef building)
        {
            if (currentBlueprint == null)
            {
                NewBlueprint();
            }

            var newBuilding = new BlueprintBuilding
            {
                buildingId = Guid.NewGuid().ToString(),
                buildingType = building.buildingType,
                category = building.category,
                gridPosition = new Vector2Int(x, y),
                rotation = currentRotation,
                level = 1,
                size = GetRotatedSize(building.size, currentRotation)
            };

            currentBlueprint.buildings.Add(newBuilding);
            RefreshGridDisplay();
            UpdateStats();
            Debug.Log($"[Blueprint] Placed {building.displayName} at ({x}, {y})");
        }

        private void RefreshGridDisplay()
        {
            // Clear placed building icons
            foreach (var icon in placedBuildingIcons)
            {
                Destroy(icon);
            }
            placedBuildingIcons.Clear();

            if (currentBlueprint == null) return;

            // Draw each building
            foreach (var building in currentBlueprint.buildings)
            {
                var def = buildingDefs.Find(b => b.buildingType == building.buildingType);
                if (def == null) continue;

                GameObject iconObj = new GameObject(building.buildingType);
                iconObj.transform.SetParent(gridView, false);

                RectTransform iconRT = iconObj.AddComponent<RectTransform>();
                iconRT.anchorMin = Vector2.zero;
                iconRT.anchorMax = Vector2.zero;
                iconRT.pivot = Vector2.zero;
                iconRT.anchoredPosition = new Vector2(building.gridPosition.x * CELL_SIZE, building.gridPosition.y * CELL_SIZE);
                iconRT.sizeDelta = new Vector2(building.size.x * CELL_SIZE - 2, building.size.y * CELL_SIZE - 2);

                Image iconBg = iconObj.AddComponent<Image>();
                iconBg.color = GetCategoryColor(building.category) * 0.7f;

                // Emoji label
                GameObject emojiObj = new GameObject("Emoji");
                emojiObj.transform.SetParent(iconObj.transform, false);
                TextMeshProUGUI emojiTMP = emojiObj.AddComponent<TextMeshProUGUI>();
                emojiTMP.text = def.iconEmoji;
                emojiTMP.fontSize = Mathf.Min(building.size.x, building.size.y) * 10;
                emojiTMP.alignment = TextAlignmentOptions.Center;
                RectTransform emojiRT = emojiObj.GetComponent<RectTransform>();
                emojiRT.anchorMin = Vector2.zero;
                emojiRT.anchorMax = Vector2.one;
                emojiRT.offsetMin = Vector2.zero;
                emojiRT.offsetMax = Vector2.zero;

                placedBuildingIcons.Add(iconObj);
            }
        }

        private void UpdateStats()
        {
            if (currentBlueprint == null)
            {
                statsText.text = "Buildings: 0 | Cost: 0 | Defense: 0 | Production: 0";
                return;
            }

            int totalCost = 0;
            int defenseScore = 0;
            int productionScore = 0;

            foreach (var building in currentBlueprint.buildings)
            {
                var def = buildingDefs.Find(b => b.buildingType == building.buildingType);
                if (def != null)
                {
                    totalCost += def.baseCost;
                    if (def.category == BlueprintBuildingCategory.Defense)
                        defenseScore += 10;
                    if (def.category == BlueprintBuildingCategory.Production)
                        productionScore += 10;
                }
            }

            currentBlueprint.estimatedCost = totalCost;
            currentBlueprint.defenseRating = Mathf.Min(100, defenseScore);
            currentBlueprint.productionRating = Mathf.Min(100, productionScore);

            statsText.text = $"Buildings: {currentBlueprint.buildings.Count} | Cost: {totalCost:N0} | 🛡️ {defenseScore} | ⚙️ {productionScore}";
        }

        private void SelectBuildingDef(PlaceableBuildingDef building)
        {
            selectedBuildingDef = building;
            currentRotation = 0;
            Debug.Log($"[Blueprint] Selected building: {building.displayName} ({building.size.x}x{building.size.y})");
        }

        private void SelectBlueprint(BlueprintData blueprint)
        {
            selectedBlueprint = blueprint;
            Debug.Log($"[Blueprint] Selected blueprint: {blueprint.name}");
        }

        private void SwitchTab(BlueprintTab tab)
        {
            currentTab = tab;
            isEditing = tab == BlueprintTab.Editor;
            RefreshBlueprintList();
            Debug.Log($"[Blueprint] Switched to tab: {tab}");
        }

        private void OnSearchChanged(string query)
        {
            // Filter blueprints by search query
            Debug.Log($"[Blueprint] Searching: {query}");
        }

        // Toolbar actions
        private void NewBlueprint()
        {
            currentBlueprint = new BlueprintData
            {
                blueprintId = Guid.NewGuid().ToString(),
                name = "New Blueprint",
                authorId = "player_001",
                authorName = "Me",
                createdDate = DateTime.Now,
                gridSize = new Vector2Int(GRID_SIZE, GRID_SIZE),
                shareStatus = BlueprintShareStatus.Private
            };
            
            isEditing = true;
            blueprintNameText.text = currentBlueprint.name;
            RefreshGridDisplay();
            UpdateStats();
            Debug.Log("[Blueprint] Created new blueprint");
        }

        private void SaveBlueprint()
        {
            if (currentBlueprint == null)
            {
                Debug.Log("[Blueprint] No blueprint to save");
                return;
            }
            
            currentBlueprint.lastModified = DateTime.Now;
            if (!myBlueprints.Contains(currentBlueprint))
            {
                myBlueprints.Add(currentBlueprint);
            }
            Debug.Log($"[Blueprint] Saved: {currentBlueprint.name}");
        }

        private void LoadBlueprint()
        {
            if (selectedBlueprint == null)
            {
                Debug.Log("[Blueprint] No blueprint selected to load");
                return;
            }
            
            currentBlueprint = selectedBlueprint;
            isEditing = true;
            blueprintNameText.text = currentBlueprint.name;
            RefreshGridDisplay();
            UpdateStats();
            Debug.Log($"[Blueprint] Loaded: {currentBlueprint.name}");
        }

        private void RotateBuilding()
        {
            currentRotation = (currentRotation + 90) % 360;
            Debug.Log($"[Blueprint] Rotation: {currentRotation}°");
        }

        private void DeleteBuilding()
        {
            // Would implement selection-based deletion
            Debug.Log("[Blueprint] Delete mode");
        }

        private void UndoAction()
        {
            if (currentBlueprint != null && currentBlueprint.buildings.Count > 0)
            {
                currentBlueprint.buildings.RemoveAt(currentBlueprint.buildings.Count - 1);
                RefreshGridDisplay();
                UpdateStats();
            }
            Debug.Log("[Blueprint] Undo");
        }

        private void ShareBlueprint()
        {
            if (currentBlueprint == null) return;
            Debug.Log($"[Blueprint] Share dialog for: {currentBlueprint.name}");
        }

        private void ApplyBlueprint()
        {
            if (currentBlueprint == null) return;
            Debug.Log($"[Blueprint] Applying blueprint to citadel: {currentBlueprint.name}");
        }

        // Public API
        public void Show()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                isVisible = true;
                RefreshBlueprintList();
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
    }
}
