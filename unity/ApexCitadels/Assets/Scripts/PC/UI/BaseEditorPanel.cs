// ============================================================================
// APEX CITADELS - AAA BASE EDITOR PANEL
// Full-featured building editor UI with block palette, templates, and tools
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using ApexCitadels.Building;
using ApexCitadels.BuildingTemplates;
using ApexCitadels.Core;
using ApexCitadels.Data;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Block categories for organized palette display
    /// </summary>
    public enum BlockCategory
    {
        Basic,          // Stone, Wood, Metal, Glass
        Walls,          // Various wall types
        Defense,        // Towers, Turrets, Traps
        Production,     // Mines, Farms, Generators
        Military,       // Barracks, Armory, Training
        Storage,        // Storage buildings
        Decoration,     // Flags, Banners, Lights
        Special,        // Unique buildings
        Templates       // Pre-built structures
    }

    /// <summary>
    /// Editor tools for building manipulation
    /// </summary>
    public enum EditorTool
    {
        Select,
        Place,
        Move,
        Rotate,
        Delete,
        Copy,
        MultiSelect,
        Measure
    }

    /// <summary>
    /// Block item data for palette display
    /// </summary>
    [System.Serializable]
    public class BlockPaletteItem
    {
        public BlockType Type;
        public string Name;
        public string Description;
        public BlockCategory Category;
        public int ResourceCost;
        public string IconEmoji;
        public int MinLevel;
        public bool Unlocked;
    }

    /// <summary>
    /// AAA-quality Base Editor Panel with full building tools
    /// </summary>
    public class BaseEditorPanel : MonoBehaviour
    {
        public static BaseEditorPanel Instance { get; private set; }

        [Header("Runtime References")]
        private Canvas parentCanvas;
        private GameObject panelRoot;
        private bool isVisible = false;

        // Main containers
        private RectTransform mainPanel;
        private RectTransform leftPanel;      // Categories and palette
        private RectTransform rightPanel;     // Tools and properties
        private RectTransform topBar;         // Header and resource display
        private RectTransform bottomBar;      // Status and quick actions

        // Category tabs
        private RectTransform categoryContainer;
        private Dictionary<BlockCategory, Button> categoryButtons = new Dictionary<BlockCategory, Button>();
        private BlockCategory currentCategory = BlockCategory.Basic;

        // Block palette
        private RectTransform paletteContainer;
        private ScrollRect paletteScroll;
        private RectTransform paletteContent;
        private List<GameObject> paletteItems = new List<GameObject>();

        // Tool buttons
        private RectTransform toolContainer;
        private Dictionary<EditorTool, Button> toolButtons = new Dictionary<EditorTool, Button>();
        private EditorTool currentTool = EditorTool.Select;

        // Properties panel
        private RectTransform propertiesContainer;
        private TextMeshProUGUI selectedBlockName;
        private TextMeshProUGUI selectedBlockDesc;
        private TextMeshProUGUI selectedBlockStats;
        private Slider rotationSlider;
        private TMP_InputField scaleInput;

        // Resource display
        private RectTransform resourceContainer;
        private TextMeshProUGUI stoneText;
        private TextMeshProUGUI woodText;
        private TextMeshProUGUI metalText;
        private TextMeshProUGUI crystalText;

        // Templates panel
        private RectTransform templatesPanel;
        private ScrollRect templatesScroll;
        private List<GameObject> templateItems = new List<GameObject>();
        private bool templatesExpanded = false;

        // Preview panel
        private RectTransform previewPanel;
        private RawImage previewImage;
        private TextMeshProUGUI previewName;
        private TextMeshProUGUI previewCost;

        // Status bar
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI gridStatusText;
        private TextMeshProUGUI blockCountText;

        // Quick action buttons
        private Button undoButton;
        private Button redoButton;
        private Button saveButton;
        private Button loadButton;
        private Button clearButton;
        private Button exitButton;

        // State
        private BlockType? selectedBlockType;
        private BuildingTemplate selectedTemplate;
        private BaseEditor baseEditor;
        private ApexCitadels.Data.ResourceInventory playerResources;

        // Palette data
        private List<BlockPaletteItem> allBlockItems = new List<BlockPaletteItem>();
        private Dictionary<BlockCategory, List<BlockPaletteItem>> blocksByCategory = new Dictionary<BlockCategory, List<BlockPaletteItem>>();

        // Colors
        private readonly Color PANEL_BG = new Color(0.08f, 0.1f, 0.12f, 0.95f);
        private readonly Color HEADER_BG = new Color(0.12f, 0.15f, 0.18f, 1f);
        private readonly Color BUTTON_NORMAL = new Color(0.15f, 0.18f, 0.22f, 1f);
        private readonly Color BUTTON_HOVER = new Color(0.2f, 0.25f, 0.3f, 1f);
        private readonly Color BUTTON_SELECTED = new Color(0.3f, 0.6f, 0.4f, 1f);
        private readonly Color BUTTON_DISABLED = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        private readonly Color TEXT_PRIMARY = new Color(0.95f, 0.95f, 0.95f, 1f);
        private readonly Color TEXT_SECONDARY = new Color(0.7f, 0.7f, 0.7f, 1f);
        private readonly Color TEXT_HIGHLIGHT = new Color(0.4f, 0.9f, 0.5f, 1f);
        private readonly Color RESOURCE_STONE = new Color(0.6f, 0.6f, 0.65f, 1f);
        private readonly Color RESOURCE_WOOD = new Color(0.7f, 0.5f, 0.3f, 1f);
        private readonly Color RESOURCE_METAL = new Color(0.5f, 0.55f, 0.7f, 1f);
        private readonly Color RESOURCE_CRYSTAL = new Color(0.6f, 0.4f, 0.8f, 1f);

        // Emoji icons for categories
        private readonly Dictionary<BlockCategory, string> categoryIcons = new Dictionary<BlockCategory, string>
        {
            { BlockCategory.Basic, "🧱" },
            { BlockCategory.Walls, "[C]" },
            { BlockCategory.Defense, "🗼" },
            { BlockCategory.Production, "[W]" },
            { BlockCategory.Military, "[!]" },
            { BlockCategory.Storage, "[B]" },
            { BlockCategory.Decoration, "🎨" },
            { BlockCategory.Special, "[*]" },
            { BlockCategory.Templates, "[T]" }
        };

        // Emoji icons for tools
        private readonly Dictionary<EditorTool, string> toolIcons = new Dictionary<EditorTool, string>
        {
            { EditorTool.Select, "[^]" },
            { EditorTool.Place, "[+]" },
            { EditorTool.Move, "[H]" },
            { EditorTool.Rotate, "[R]" },
            { EditorTool.Delete, "[D]" },
            { EditorTool.Copy, "[T]" },
            { EditorTool.MultiSelect, "[]" },
            { EditorTool.Measure, "📏" }
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
                InitializeBlockPalette();
                CreateUI();
                Hide();

                // Subscribe to BaseEditor events
                if (BaseEditor.Instance != null)
                {
                    baseEditor = BaseEditor.Instance;
                    baseEditor.OnEditorModeEntered += Show;
                    baseEditor.OnEditorModeExited += Hide;
                    baseEditor.OnBlockPlaced += OnBlockPlaced;
                    baseEditor.OnBlockRemoved += OnBlockRemoved;
                    baseEditor.OnBlockSelected += OnBlockSelected;
                }
            }
        }

        private void Update()
        {
            if (!isVisible) return;

            HandleKeyboardShortcuts();
            UpdateStatusBar();
        }

        private void OnDestroy()
        {
            if (baseEditor != null)
            {
                baseEditor.OnEditorModeEntered -= Show;
                baseEditor.OnEditorModeExited -= Hide;
                baseEditor.OnBlockPlaced -= OnBlockPlaced;
                baseEditor.OnBlockRemoved -= OnBlockRemoved;
                baseEditor.OnBlockSelected -= OnBlockSelected;
            }
        }

        #region Initialization

        private void InitializeBlockPalette()
        {
            allBlockItems.Clear();
            blocksByCategory.Clear();

            // Initialize category lists
            foreach (BlockCategory cat in Enum.GetValues(typeof(BlockCategory)))
            {
                blocksByCategory[cat] = new List<BlockPaletteItem>();
            }

            // === BASIC BLOCKS ===
            AddBlockItem(BlockType.Stone, "Stone Block", "Solid stone foundation block", BlockCategory.Basic, 10, "[Q]", 1);
            AddBlockItem(BlockType.Wood, "Wood Block", "Lightweight wooden block", BlockCategory.Basic, 5, "[W]", 1);
            AddBlockItem(BlockType.Metal, "Metal Block", "Reinforced metal block", BlockCategory.Basic, 25, "🔩", 2);
            AddBlockItem(BlockType.Glass, "Glass Block", "Transparent glass panel", BlockCategory.Basic, 15, "🪟", 2);
            AddBlockItem(BlockType.Brick, "Brick Block", "Classic brick construction", BlockCategory.Basic, 12, "🧱", 1);
            AddBlockItem(BlockType.Concrete, "Concrete Block", "Heavy-duty concrete", BlockCategory.Basic, 20, "[]", 3);

            // === WALLS ===
            AddBlockItem(BlockType.Wall, "Basic Wall", "Standard defensive wall", BlockCategory.Walls, 50, "[C]", 1);
            AddBlockItem(BlockType.WallCorner, "Corner Wall", "Wall corner piece", BlockCategory.Walls, 60, "📐", 1);
            AddBlockItem(BlockType.WallGate, "Gate Wall", "Wall with gate opening", BlockCategory.Walls, 100, "[G]", 2);
            AddBlockItem(BlockType.WallWindow, "Window Wall", "Wall with arrow slits", BlockCategory.Walls, 70, "🪟", 2);
            AddBlockItem(BlockType.WallReinforced, "Reinforced Wall", "Extra-strong wall", BlockCategory.Walls, 150, "[D]", 3);
            AddBlockItem(BlockType.Battlement, "Battlements", "Crenellated wall top", BlockCategory.Walls, 80, "🏯", 2);

            // === DEFENSE ===
            AddBlockItem(BlockType.Tower, "Watch Tower", "Scout tower for visibility", BlockCategory.Defense, 100, "🗼", 2);
            AddBlockItem(BlockType.Turret, "Defense Turret", "Auto-targeting turret", BlockCategory.Defense, 200, "[+]", 3);
            AddBlockItem(BlockType.TowerArcher, "Archer Tower", "Ranged attack tower", BlockCategory.Defense, 175, "[A]", 2);
            AddBlockItem(BlockType.TowerCannon, "Cannon Tower", "Siege defense tower", BlockCategory.Defense, 300, "[X]", 4);
            AddBlockItem(BlockType.TowerMage, "Mage Tower", "Magical defense tower", BlockCategory.Defense, 400, "🔮", 5);
            AddBlockItem(BlockType.Moat, "Moat Section", "Water defense moat", BlockCategory.Defense, 75, "[~]", 2);
            AddBlockItem(BlockType.Spikes, "Spike Trap", "Ground spike trap", BlockCategory.Defense, 50, "[!]", 1);
            AddBlockItem(BlockType.TrapFire, "Fire Trap", "Flame trap trigger", BlockCategory.Defense, 120, "[*]", 3);
            AddBlockItem(BlockType.TrapPit, "Pit Trap", "Hidden pit trap", BlockCategory.Defense, 80, "🕳", 2);

            // === PRODUCTION ===
            AddBlockItem(BlockType.Mine, "Mining Station", "Extract stone resources", BlockCategory.Production, 150, "[M]", 2);
            AddBlockItem(BlockType.Sawmill, "Sawmill", "Process wood resources", BlockCategory.Production, 120, "🪚", 2);
            AddBlockItem(BlockType.Foundry, "Metal Foundry", "Smelt metal ores", BlockCategory.Production, 250, "🏭", 3);
            AddBlockItem(BlockType.Farm, "Farm Plot", "Grow food resources", BlockCategory.Production, 80, "[F]", 1);
            AddBlockItem(BlockType.Generator, "Power Generator", "Generate energy", BlockCategory.Production, 300, "[!]", 4);
            AddBlockItem(BlockType.ResourceNode, "Resource Node", "Resource collection point", BlockCategory.Production, 100, "[G]", 2);

            // === MILITARY ===
            AddBlockItem(BlockType.Barracks, "Barracks", "Train infantry units", BlockCategory.Military, 200, "[M]", 2);
            AddBlockItem(BlockType.Armory, "Armory", "Store and upgrade weapons", BlockCategory.Military, 175, "[!]", 2);
            AddBlockItem(BlockType.TrainingGround, "Training Ground", "Train elite units", BlockCategory.Military, 250, "🏋", 3);
            AddBlockItem(BlockType.Workshop, "War Workshop", "Build siege engines", BlockCategory.Military, 350, "[W]", 4);
            AddBlockItem(BlockType.Stable, "Stables", "House cavalry units", BlockCategory.Military, 225, "[H]", 3);

            // === STORAGE ===
            AddBlockItem(BlockType.Storage, "Storage Vault", "General resource storage", BlockCategory.Storage, 100, "[B]", 1);
            AddBlockItem(BlockType.Warehouse, "Warehouse", "Large capacity storage", BlockCategory.Storage, 200, "🏢", 2);
            AddBlockItem(BlockType.Treasury, "Treasury", "Secure gold storage", BlockCategory.Storage, 300, "[$]", 3);
            AddBlockItem(BlockType.Silo, "Resource Silo", "Bulk material storage", BlockCategory.Storage, 175, "[B]", 2);

            // === DECORATION ===
            AddBlockItem(BlockType.Flag, "Territory Flag", "Mark your territory", BlockCategory.Decoration, 15, "🚩", 1);
            AddBlockItem(BlockType.Banner, "Banner", "Decorative banner", BlockCategory.Decoration, 20, "🎌", 1);
            AddBlockItem(BlockType.Torch, "Wall Torch", "Lighting decoration", BlockCategory.Decoration, 10, "[*]", 1);
            AddBlockItem(BlockType.Beacon, "Beacon", "Glowing beacon light", BlockCategory.Decoration, 75, "[!]", 2);
            AddBlockItem(BlockType.Statue, "Statue", "Decorative statue", BlockCategory.Decoration, 100, "[*]", 2);
            AddBlockItem(BlockType.Fountain, "Fountain", "Water fountain", BlockCategory.Decoration, 150, "[O]", 3);
            AddBlockItem(BlockType.Garden, "Garden Plot", "Decorative garden", BlockCategory.Decoration, 50, "[T]", 1);
            AddBlockItem(BlockType.Lamp, "Street Lamp", "Area lighting", BlockCategory.Decoration, 25, "🏮", 1);

            // === SPECIAL ===
            AddBlockItem(BlockType.CommandCenter, "Command Center", "Main base headquarters", BlockCategory.Special, 500, "[R]", 1);
            AddBlockItem(BlockType.Portal, "Portal Gate", "Teleportation point", BlockCategory.Special, 1000, "🌀", 5);
            AddBlockItem(BlockType.Shrine, "Ancient Shrine", "Magical buff shrine", BlockCategory.Special, 400, "[T]", 4);
            AddBlockItem(BlockType.Forge, "Legendary Forge", "Craft epic items", BlockCategory.Special, 600, "[H]", 5);
            AddBlockItem(BlockType.Altar, "Altar of Power", "Special abilities", BlockCategory.Special, 750, "[*]", 5);

            ApexLogger.Log($"Initialized {allBlockItems.Count} block types across {blocksByCategory.Count} categories", ApexLogger.LogCategory.Building);
        }

        private void AddBlockItem(BlockType type, string name, string desc, BlockCategory category, int cost, string icon, int minLevel)
        {
            var item = new BlockPaletteItem
            {
                Type = type,
                Name = name,
                Description = desc,
                Category = category,
                ResourceCost = cost,
                IconEmoji = icon,
                MinLevel = minLevel,
                Unlocked = true // TODO: Check player progression
            };

            allBlockItems.Add(item);
            blocksByCategory[category].Add(item);
        }

        #endregion

        #region UI Creation

        private void CreateUI()
        {
            // Main panel root
            panelRoot = new GameObject("BaseEditorPanel");
            panelRoot.transform.SetParent(parentCanvas.transform, false);
            mainPanel = panelRoot.AddComponent<RectTransform>();
            mainPanel.anchorMin = Vector2.zero;
            mainPanel.anchorMax = Vector2.one;
            mainPanel.offsetMin = Vector2.zero;
            mainPanel.offsetMax = Vector2.zero;

            // Create main sections
            CreateTopBar();
            CreateLeftPanel();
            CreateRightPanel();
            CreateBottomBar();
            CreateTemplatesPanel();
            CreatePreviewPanel();

            // Initial category
            SelectCategory(BlockCategory.Basic);
        }

        private void CreateTopBar()
        {
            // Top bar container
            GameObject topBarObj = CreateUIObject("TopBar", mainPanel);
            topBar = topBarObj.GetComponent<RectTransform>();
            SetAnchors(topBar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(0, 0));

            Image bg = topBarObj.AddComponent<Image>();
            bg.color = HEADER_BG;

            // Title
            TextMeshProUGUI title = CreateText(topBar, "[B] BASE EDITOR", 24, FontStyles.Bold);
            SetAnchors(title.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(20, 0), new Vector2(250, 0));
            title.alignment = TextAlignmentOptions.MidlineLeft;

            // Resource display
            resourceContainer = CreateUIObject("Resources", topBar).GetComponent<RectTransform>();
            SetAnchors(resourceContainer, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(-200, 5), new Vector2(200, -5));

            HorizontalLayoutGroup layout = resourceContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            stoneText = CreateResourceDisplay(resourceContainer, "[Q]", "0", RESOURCE_STONE);
            woodText = CreateResourceDisplay(resourceContainer, "[W]", "0", RESOURCE_WOOD);
            metalText = CreateResourceDisplay(resourceContainer, "🔩", "0", RESOURCE_METAL);
            crystalText = CreateResourceDisplay(resourceContainer, "[G]", "0", RESOURCE_CRYSTAL);

            // Quick action buttons (right side)
            float btnX = -20;
            float btnSize = 40;
            float btnSpacing = 5;

            exitButton = CreateIconButton(topBar, "[X]", () => ExitEditor(), "Exit Editor");
            SetAnchors(exitButton.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), 
                new Vector2(btnX - btnSize, -btnSize/2), new Vector2(btnX, btnSize/2));

            btnX -= btnSize + btnSpacing;
            saveButton = CreateIconButton(topBar, "[S]", () => SaveBlueprint(), "Save Blueprint");
            SetAnchors(saveButton.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(btnX - btnSize, -btnSize/2), new Vector2(btnX, btnSize/2));

            btnX -= btnSize + btnSpacing;
            loadButton = CreateIconButton(topBar, "[L]", () => LoadBlueprint(), "Load Blueprint");
            SetAnchors(loadButton.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(btnX - btnSize, -btnSize/2), new Vector2(btnX, btnSize/2));

            btnX -= btnSize + btnSpacing + 10;
            redoButton = CreateIconButton(topBar, "->", () => baseEditor?.Redo(), "Redo (Ctrl+Y)");
            SetAnchors(redoButton.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(btnX - btnSize, -btnSize/2), new Vector2(btnX, btnSize/2));

            btnX -= btnSize + btnSpacing;
            undoButton = CreateIconButton(topBar, "[U]", () => baseEditor?.Undo(), "Undo (Ctrl+Z)");
            SetAnchors(undoButton.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(btnX - btnSize, -btnSize/2), new Vector2(btnX, btnSize/2));
        }

        private TextMeshProUGUI CreateResourceDisplay(RectTransform parent, string icon, string value, Color color)
        {
            GameObject container = CreateUIObject("Resource", parent);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 40);

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;

            TextMeshProUGUI iconText = CreateText(rt, icon, 18);
            iconText.rectTransform.sizeDelta = new Vector2(25, 40);

            TextMeshProUGUI valueText = CreateText(rt, value, 16, FontStyles.Bold);
            valueText.rectTransform.sizeDelta = new Vector2(50, 40);
            valueText.color = color;

            return valueText;
        }

        private void CreateLeftPanel()
        {
            // Left panel (categories + palette)
            GameObject leftObj = CreateUIObject("LeftPanel", mainPanel);
            leftPanel = leftObj.GetComponent<RectTransform>();
            SetAnchors(leftPanel, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 40), new Vector2(280, -60));

            Image bg = leftObj.AddComponent<Image>();
            bg.color = PANEL_BG;

            // Category tabs
            CreateCategoryTabs();

            // Block palette
            CreateBlockPalette();
        }

        private void CreateCategoryTabs()
        {
            categoryContainer = CreateUIObject("Categories", leftPanel).GetComponent<RectTransform>();
            SetAnchors(categoryContainer, new Vector2(0, 1), new Vector2(1, 1), new Vector2(5, -50), new Vector2(-5, -5));

            GridLayoutGroup grid = categoryContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(55, 40);
            grid.spacing = new Vector2(3, 3);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            foreach (BlockCategory category in Enum.GetValues(typeof(BlockCategory)))
            {
                if (category == BlockCategory.Templates) continue; // Templates has its own panel

                Button btn = CreateCategoryButton(categoryContainer, category);
                categoryButtons[category] = btn;
            }
        }

        private Button CreateCategoryButton(RectTransform parent, BlockCategory category)
        {
            GameObject btnObj = CreateUIObject($"Cat_{category}", parent);
            RectTransform rt = btnObj.GetComponent<RectTransform>();

            Image bg = btnObj.AddComponent<Image>();
            bg.color = BUTTON_NORMAL;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = BUTTON_NORMAL;
            colors.highlightedColor = BUTTON_HOVER;
            colors.selectedColor = BUTTON_SELECTED;
            colors.pressedColor = BUTTON_SELECTED;
            btn.colors = colors;

            // Icon
            string icon = categoryIcons.TryGetValue(category, out string i) ? i : "?";
            TextMeshProUGUI iconText = CreateText(rt, icon, 18);
            SetAnchors(iconText.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 10), new Vector2(0, 0));
            iconText.alignment = TextAlignmentOptions.Center;

            // Label
            TextMeshProUGUI label = CreateText(rt, category.ToString().Substring(0, Mathf.Min(5, category.ToString().Length)), 9);
            SetAnchors(label.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 2), new Vector2(0, 14));
            label.alignment = TextAlignmentOptions.Center;
            label.color = TEXT_SECONDARY;

            btn.onClick.AddListener(() => SelectCategory(category));

            // Tooltip
            AddTooltip(btnObj, $"{category}\n{blocksByCategory[category].Count} blocks");

            return btn;
        }

        private void CreateBlockPalette()
        {
            // Palette scroll area
            GameObject scrollObj = CreateUIObject("PaletteScroll", leftPanel);
            paletteScroll = scrollObj.AddComponent<ScrollRect>();
            RectTransform scrollRT = scrollObj.GetComponent<RectTransform>();
            SetAnchors(scrollRT, new Vector2(0, 0), new Vector2(1, 1), new Vector2(5, 5), new Vector2(-5, -55));

            // Viewport
            GameObject viewportObj = CreateUIObject("Viewport", scrollRT);
            RectTransform viewport = viewportObj.GetComponent<RectTransform>();
            SetAnchors(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image viewportMask = viewportObj.AddComponent<Image>();
            viewportMask.color = Color.white;
            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            GameObject contentObj = CreateUIObject("Content", viewport);
            paletteContent = contentObj.GetComponent<RectTransform>();
            SetAnchors(paletteContent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));
            paletteContent.pivot = new Vector2(0.5f, 1);

            GridLayoutGroup grid = contentObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(85, 100);
            grid.spacing = new Vector2(5, 5);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(5, 5, 5, 5);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            paletteScroll.content = paletteContent;
            paletteScroll.viewport = viewport;
            paletteScroll.horizontal = false;
            paletteScroll.vertical = true;
            paletteScroll.scrollSensitivity = 30;
        }

        private void CreateRightPanel()
        {
            // Right panel (tools + properties)
            GameObject rightObj = CreateUIObject("RightPanel", mainPanel);
            rightPanel = rightObj.GetComponent<RectTransform>();
            SetAnchors(rightPanel, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-240, 40), new Vector2(0, -60));

            Image bg = rightObj.AddComponent<Image>();
            bg.color = PANEL_BG;

            // Tools section
            CreateToolsSection();

            // Properties section
            CreatePropertiesSection();
        }

        private void CreateToolsSection()
        {
            GameObject toolsObj = CreateUIObject("Tools", rightPanel);
            toolContainer = toolsObj.GetComponent<RectTransform>();
            SetAnchors(toolContainer, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -130), new Vector2(-10, -10));

            // Header
            TextMeshProUGUI header = CreateText(toolContainer, "🛠 TOOLS", 14, FontStyles.Bold);
            SetAnchors(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -25), new Vector2(0, 0));
            header.alignment = TextAlignmentOptions.Center;

            // Tool grid
            GameObject gridObj = CreateUIObject("ToolGrid", toolContainer);
            RectTransform gridRT = gridObj.GetComponent<RectTransform>();
            SetAnchors(gridRT, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -30));

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(50, 50);
            grid.spacing = new Vector2(5, 5);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(5, 5, 5, 5);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            foreach (EditorTool tool in Enum.GetValues(typeof(EditorTool)))
            {
                Button btn = CreateToolButton(gridRT, tool);
                toolButtons[tool] = btn;
            }
        }

        private Button CreateToolButton(RectTransform parent, EditorTool tool)
        {
            GameObject btnObj = CreateUIObject($"Tool_{tool}", parent);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = BUTTON_NORMAL;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = BUTTON_NORMAL;
            colors.highlightedColor = BUTTON_HOVER;
            colors.selectedColor = BUTTON_SELECTED;
            btn.colors = colors;

            string icon = toolIcons.TryGetValue(tool, out string i) ? i : "?";
            TextMeshProUGUI iconText = CreateText(btnObj.GetComponent<RectTransform>(), icon, 22);
            SetAnchors(iconText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            iconText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(() => SelectTool(tool));

            AddTooltip(btnObj, GetToolDescription(tool));

            return btn;
        }

        private string GetToolDescription(EditorTool tool)
        {
            return tool switch
            {
                EditorTool.Select => "Select (V)\nClick to select blocks",
                EditorTool.Place => "Place (P)\nPlace selected block",
                EditorTool.Move => "Move (M)\nDrag blocks to move",
                EditorTool.Rotate => "Rotate (R)\nRotate selected block",
                EditorTool.Delete => "Delete (X/Del)\nRemove selected block",
                EditorTool.Copy => "Copy (C)\nDuplicate selection",
                EditorTool.MultiSelect => "Multi-Select (Shift)\nSelect multiple blocks",
                EditorTool.Measure => "Measure\nMeasure distances",
                _ => tool.ToString()
            };
        }

        private void CreatePropertiesSection()
        {
            propertiesContainer = CreateUIObject("Properties", rightPanel).GetComponent<RectTransform>();
            SetAnchors(propertiesContainer, new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 10), new Vector2(-10, -140));

            // Header
            TextMeshProUGUI header = CreateText(propertiesContainer, "[T] PROPERTIES", 14, FontStyles.Bold);
            SetAnchors(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -25), new Vector2(0, 0));
            header.alignment = TextAlignmentOptions.Center;

            // Selected block info
            selectedBlockName = CreateText(propertiesContainer, "No Selection", 16, FontStyles.Bold);
            SetAnchors(selectedBlockName.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(5, -55), new Vector2(-5, -30));
            selectedBlockName.color = TEXT_HIGHLIGHT;

            selectedBlockDesc = CreateText(propertiesContainer, "Select a block to view properties", 12);
            SetAnchors(selectedBlockDesc.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(5, -90), new Vector2(-5, -55));
            selectedBlockDesc.color = TEXT_SECONDARY;

            selectedBlockStats = CreateText(propertiesContainer, "", 11);
            SetAnchors(selectedBlockStats.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(5, -160), new Vector2(-5, -90));
            selectedBlockStats.color = TEXT_SECONDARY;

            // Rotation slider
            CreateRotationControl();

            // Scale input
            CreateScaleControl();
        }

        private void CreateRotationControl()
        {
            GameObject rotObj = CreateUIObject("RotationControl", propertiesContainer);
            RectTransform rotRT = rotObj.GetComponent<RectTransform>();
            SetAnchors(rotRT, new Vector2(0, 0), new Vector2(1, 0), new Vector2(5, 80), new Vector2(-5, 120));

            TextMeshProUGUI label = CreateText(rotRT, "[R] Rotation", 11);
            SetAnchors(label.rectTransform, new Vector2(0, 0.5f), new Vector2(0.3f, 1), Vector2.zero, Vector2.zero);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            // Slider
            GameObject sliderObj = CreateUIObject("Slider", rotRT);
            RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
            SetAnchors(sliderRT, new Vector2(0.3f, 0.2f), new Vector2(1, 0.8f), new Vector2(5, 0), new Vector2(-5, 0));

            rotationSlider = sliderObj.AddComponent<Slider>();
            rotationSlider.minValue = 0;
            rotationSlider.maxValue = 360;
            rotationSlider.wholeNumbers = true;

            // Background
            GameObject bgObj = CreateUIObject("Background", sliderRT);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f);
            SetAnchors(bgObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Fill
            GameObject fillArea = CreateUIObject("Fill Area", sliderRT);
            SetAnchors(fillArea.GetComponent<RectTransform>(), new Vector2(0, 0.25f), new Vector2(1, 0.75f), new Vector2(5, 0), new Vector2(-5, 0));

            GameObject fill = CreateUIObject("Fill", fillArea.GetComponent<RectTransform>());
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = BUTTON_SELECTED;
            SetAnchors(fill.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);

            // Handle
            GameObject handleArea = CreateUIObject("Handle Slide Area", sliderRT);
            SetAnchors(handleArea.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 0), new Vector2(-10, 0));

            GameObject handle = CreateUIObject("Handle", handleArea.GetComponent<RectTransform>());
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            RectTransform handleRT = handle.GetComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(20, 20);

            rotationSlider.fillRect = fill.GetComponent<RectTransform>();
            rotationSlider.handleRect = handleRT;
            rotationSlider.targetGraphic = handleImg;

            rotationSlider.onValueChanged.AddListener(OnRotationChanged);
        }

        private void CreateScaleControl()
        {
            GameObject scaleObj = CreateUIObject("ScaleControl", propertiesContainer);
            RectTransform scaleRT = scaleObj.GetComponent<RectTransform>();
            SetAnchors(scaleRT, new Vector2(0, 0), new Vector2(1, 0), new Vector2(5, 40), new Vector2(-5, 75));

            TextMeshProUGUI label = CreateText(scaleRT, "📐 Scale", 11);
            SetAnchors(label.rectTransform, new Vector2(0, 0), new Vector2(0.3f, 1), Vector2.zero, Vector2.zero);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            // Input field
            GameObject inputObj = CreateUIObject("ScaleInput", scaleRT);
            RectTransform inputRT = inputObj.GetComponent<RectTransform>();
            SetAnchors(inputRT, new Vector2(0.3f, 0.1f), new Vector2(1, 0.9f), new Vector2(5, 0), new Vector2(-5, 0));

            Image inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(0.15f, 0.15f, 0.15f);

            scaleInput = inputObj.AddComponent<TMP_InputField>();
            scaleInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            scaleInput.text = "1.0";

            // Text area
            GameObject textArea = CreateUIObject("Text Area", inputRT);
            SetAnchors(textArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));

            TextMeshProUGUI inputText = CreateText(textArea.GetComponent<RectTransform>(), "", 12);
            SetAnchors(inputText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            inputText.alignment = TextAlignmentOptions.MidlineLeft;

            scaleInput.textComponent = inputText;
            scaleInput.textViewport = textArea.GetComponent<RectTransform>();

            scaleInput.onEndEdit.AddListener(OnScaleChanged);
        }

        private void CreateBottomBar()
        {
            // Bottom status bar
            GameObject bottomObj = CreateUIObject("BottomBar", mainPanel);
            bottomBar = bottomObj.GetComponent<RectTransform>();
            SetAnchors(bottomBar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 35));

            Image bg = bottomObj.AddComponent<Image>();
            bg.color = HEADER_BG;

            // Status text
            statusText = CreateText(bottomBar, "Ready", 12);
            SetAnchors(statusText.rectTransform, new Vector2(0, 0), new Vector2(0.3f, 1), new Vector2(15, 0), new Vector2(0, 0));
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            statusText.color = TEXT_SECONDARY;

            // Grid status
            gridStatusText = CreateText(bottomBar, "Grid: 0.5m | Snap: ON", 12);
            SetAnchors(gridStatusText.rectTransform, new Vector2(0.4f, 0), new Vector2(0.6f, 1), Vector2.zero, Vector2.zero);
            gridStatusText.alignment = TextAlignmentOptions.Center;
            gridStatusText.color = TEXT_SECONDARY;

            // Block count
            blockCountText = CreateText(bottomBar, "Blocks: 0", 12);
            SetAnchors(blockCountText.rectTransform, new Vector2(0.85f, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-15, 0));
            blockCountText.alignment = TextAlignmentOptions.MidlineRight;
            blockCountText.color = TEXT_SECONDARY;

            // Keyboard shortcuts hint
            TextMeshProUGUI shortcuts = CreateText(bottomBar, "Ctrl+Z: Undo | Ctrl+Y: Redo | Q/E: Rotate | Esc: Exit", 10);
            SetAnchors(shortcuts.rectTransform, new Vector2(0.3f, 0), new Vector2(0.7f, 1), Vector2.zero, Vector2.zero);
            shortcuts.alignment = TextAlignmentOptions.Center;
            shortcuts.color = new Color(0.5f, 0.5f, 0.5f);
        }

        private void CreateTemplatesPanel()
        {
            // Collapsible templates panel (bottom-left)
            GameObject templatesObj = CreateUIObject("TemplatesPanel", mainPanel);
            templatesPanel = templatesObj.GetComponent<RectTransform>();
            SetAnchors(templatesPanel, new Vector2(0, 0), new Vector2(0, 0), new Vector2(285, 40), new Vector2(535, 200));

            Image bg = templatesObj.AddComponent<Image>();
            bg.color = PANEL_BG;

            // Header with toggle
            GameObject headerObj = CreateUIObject("Header", templatesPanel);
            RectTransform headerRT = headerObj.GetComponent<RectTransform>();
            SetAnchors(headerRT, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -30), new Vector2(0, 0));

            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = HEADER_BG;

            Button toggleBtn = headerObj.AddComponent<Button>();
            toggleBtn.targetGraphic = headerBg;
            toggleBtn.onClick.AddListener(ToggleTemplatesPanel);

            TextMeshProUGUI headerText = CreateText(headerRT, "[T] QUICK TEMPLATES v", 12, FontStyles.Bold);
            SetAnchors(headerText.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
            headerText.alignment = TextAlignmentOptions.MidlineLeft;

            // Templates scroll
            GameObject scrollObj = CreateUIObject("TemplatesScroll", templatesPanel);
            templatesScroll = scrollObj.AddComponent<ScrollRect>();
            RectTransform scrollRT = scrollObj.GetComponent<RectTransform>();
            SetAnchors(scrollRT, new Vector2(0, 0), new Vector2(1, 1), new Vector2(5, 5), new Vector2(-5, -35));

            // Viewport
            GameObject viewport = CreateUIObject("Viewport", scrollRT);
            SetAnchors(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image vpMask = viewport.AddComponent<Image>();
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            GameObject content = CreateUIObject("Content", viewport.GetComponent<RectTransform>());
            RectTransform contentRT = content.GetComponent<RectTransform>();
            SetAnchors(contentRT, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            contentRT.pivot = new Vector2(0.5f, 1);

            HorizontalLayoutGroup layout = content.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            templatesScroll.content = contentRT;
            templatesScroll.viewport = viewport.GetComponent<RectTransform>();
            templatesScroll.horizontal = true;
            templatesScroll.vertical = false;

            // Add template items
            PopulateTemplates(contentRT);
        }

        private void PopulateTemplates(RectTransform content)
        {
            if (BuildingTemplateManager.Instance == null) return;

            var templates = BuildingTemplateManager.Instance.GetAllTemplates();
            foreach (var template in templates.Take(8)) // Show first 8
            {
                CreateTemplateItem(content, template);
            }
        }

        private void CreateTemplateItem(RectTransform parent, BuildingTemplate template)
        {
            GameObject itemObj = CreateUIObject($"Template_{template.Id}", parent);
            RectTransform itemRT = itemObj.GetComponent<RectTransform>();
            itemRT.sizeDelta = new Vector2(120, 120);

            Image bg = itemObj.AddComponent<Image>();
            bg.color = BUTTON_NORMAL;

            Button btn = itemObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => SelectTemplate(template));

            // Icon
            string icon = GetTemplateIcon(template.Category);
            TextMeshProUGUI iconText = CreateText(itemRT, icon, 32);
            SetAnchors(iconText.rectTransform, new Vector2(0, 0.4f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            iconText.alignment = TextAlignmentOptions.Center;

            // Name
            TextMeshProUGUI nameText = CreateText(itemRT, template.Name, 10);
            SetAnchors(nameText.rectTransform, new Vector2(0, 0.15f), new Vector2(1, 0.4f), Vector2.zero, Vector2.zero);
            nameText.alignment = TextAlignmentOptions.Center;

            // Cost
            int totalCost = template.ResourceCost?.Sum(r => r.Amount) ?? 0;
            TextMeshProUGUI costText = CreateText(itemRT, $"[$] {totalCost}", 9);
            SetAnchors(costText.rectTransform, new Vector2(0, 0), new Vector2(1, 0.15f), Vector2.zero, Vector2.zero);
            costText.alignment = TextAlignmentOptions.Center;
            costText.color = TEXT_SECONDARY;

            templateItems.Add(itemObj);
        }

        private string GetTemplateIcon(TemplateCategory category)
        {
            return category switch
            {
                TemplateCategory.Starter => "[H]",
                TemplateCategory.Defensive => "[C]",
                TemplateCategory.Economy => "[$]",
                TemplateCategory.Advanced => "[P]",
                TemplateCategory.Decorative => "🎨",
                _ => "[B]"
            };
        }

        private void CreatePreviewPanel()
        {
            // Block preview panel (above properties when block selected)
            previewPanel = CreateUIObject("PreviewPanel", rightPanel).GetComponent<RectTransform>();
            SetAnchors(previewPanel, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(10, -60), new Vector2(-10, 60));

            Image bg = previewPanel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.15f, 0.9f);

            // Preview image
            GameObject imgObj = CreateUIObject("PreviewImage", previewPanel);
            previewImage = imgObj.AddComponent<RawImage>();
            previewImage.color = new Color(0.3f, 0.3f, 0.3f);
            SetAnchors(imgObj.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0.4f, 1), new Vector2(5, 5), new Vector2(-5, -5));

            // Name
            previewName = CreateText(previewPanel, "", 14, FontStyles.Bold);
            SetAnchors(previewName.rectTransform, new Vector2(0.42f, 0.6f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-5, -5));
            previewName.alignment = TextAlignmentOptions.TopLeft;
            previewName.color = TEXT_HIGHLIGHT;

            // Cost
            previewCost = CreateText(previewPanel, "", 12);
            SetAnchors(previewCost.rectTransform, new Vector2(0.42f, 0), new Vector2(1, 0.5f), new Vector2(0, 5), new Vector2(-5, 0));
            previewCost.alignment = TextAlignmentOptions.TopLeft;
            previewCost.color = TEXT_SECONDARY;

            previewPanel.gameObject.SetActive(false);
        }

        #endregion

        #region UI Updates

        private void SelectCategory(BlockCategory category)
        {
            currentCategory = category;

            // Update button states
            foreach (var kvp in categoryButtons)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == category ? BUTTON_SELECTED : BUTTON_NORMAL;
            }

            // Populate palette
            PopulatePalette(category);
        }

        private void PopulatePalette(BlockCategory category)
        {
            // Clear existing items
            foreach (var item in paletteItems)
            {
                Destroy(item);
            }
            paletteItems.Clear();

            // Add items for category
            var blocks = blocksByCategory[category];
            foreach (var block in blocks)
            {
                CreatePaletteItem(block);
            }
        }

        private void CreatePaletteItem(BlockPaletteItem block)
        {
            GameObject itemObj = CreateUIObject($"Block_{block.Type}", paletteContent);

            Image bg = itemObj.AddComponent<Image>();
            bg.color = block.Unlocked ? BUTTON_NORMAL : BUTTON_DISABLED;

            Button btn = itemObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.interactable = block.Unlocked;

            ColorBlock colors = btn.colors;
            colors.normalColor = BUTTON_NORMAL;
            colors.highlightedColor = BUTTON_HOVER;
            colors.selectedColor = BUTTON_SELECTED;
            colors.disabledColor = BUTTON_DISABLED;
            btn.colors = colors;

            btn.onClick.AddListener(() => SelectBlock(block));

            // Icon
            TextMeshProUGUI iconText = CreateText(itemObj.GetComponent<RectTransform>(), block.IconEmoji, 28);
            SetAnchors(iconText.rectTransform, new Vector2(0, 0.45f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            iconText.alignment = TextAlignmentOptions.Center;

            // Name
            TextMeshProUGUI nameText = CreateText(itemObj.GetComponent<RectTransform>(), block.Name, 10);
            SetAnchors(nameText.rectTransform, new Vector2(0, 0.2f), new Vector2(1, 0.45f), new Vector2(2, 0), new Vector2(-2, 0));
            nameText.alignment = TextAlignmentOptions.Center;

            // Cost
            TextMeshProUGUI costText = CreateText(itemObj.GetComponent<RectTransform>(), $"[$] {block.ResourceCost}", 9);
            SetAnchors(costText.rectTransform, new Vector2(0, 0), new Vector2(1, 0.2f), Vector2.zero, Vector2.zero);
            costText.alignment = TextAlignmentOptions.Center;
            costText.color = CanAfford(block.ResourceCost) ? TEXT_SECONDARY : new Color(1f, 0.4f, 0.4f);

            // Lock overlay if not unlocked
            if (!block.Unlocked)
            {
                TextMeshProUGUI lockIcon = CreateText(itemObj.GetComponent<RectTransform>(), "[L]", 24);
                SetAnchors(lockIcon.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                lockIcon.alignment = TextAlignmentOptions.Center;
            }

            AddTooltip(itemObj, $"{block.Name}\n{block.Description}\nCost: {block.ResourceCost}\nLevel {block.MinLevel}+");

            paletteItems.Add(itemObj);
        }

        private void SelectTool(EditorTool tool)
        {
            currentTool = tool;

            // Update button states
            foreach (var kvp in toolButtons)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == tool ? BUTTON_SELECTED : BUTTON_NORMAL;
            }

            // Apply tool to BaseEditor
            ApplyTool(tool);

            SetStatus($"Tool: {tool}");
        }

        private void ApplyTool(EditorTool tool)
        {
            if (baseEditor == null) return;

            switch (tool)
            {
                case EditorTool.Place:
                    if (selectedBlockType.HasValue)
                    {
                        baseEditor.SelectBlockType(selectedBlockType.Value);
                    }
                    break;
                case EditorTool.Select:
                    baseEditor.CancelPlacement();
                    break;
                case EditorTool.Delete:
                    // Delete mode handled by BaseEditor
                    break;
            }
        }

        private void SelectBlock(BlockPaletteItem block)
        {
            selectedBlockType = block.Type;
            selectedTemplate = null;

            // Show preview
            ShowBlockPreview(block);

            // Enter placement mode
            SelectTool(EditorTool.Place);
            baseEditor?.SelectBlockType(block.Type);

            SetStatus($"Selected: {block.Name}");
        }

        private void SelectTemplate(BuildingTemplate template)
        {
            selectedTemplate = template;
            selectedBlockType = null;

            SetStatus($"Template: {template.Name}");

            // TODO: Preview and place template through BaseEditor
        }

        private void ShowBlockPreview(BlockPaletteItem block)
        {
            previewPanel.gameObject.SetActive(true);
            previewName.text = $"{block.IconEmoji} {block.Name}";
            previewCost.text = $"Cost: {block.ResourceCost}\n{block.Description}";
        }

        private void UpdateStatusBar()
        {
            if (baseEditor == null) return;

            // Block count
            // blockCountText.text = $"Blocks: {baseEditor.PlacedBlockCount}";

            // Grid status
            gridStatusText.text = $"Grid: {baseEditor.IsEditorActive} | Snap: ON";
        }

        public void UpdateResources(int stone, int wood, int metal, int crystal)
        {
            if (stoneText != null) stoneText.text = FormatNumber(stone);
            if (woodText != null) woodText.text = FormatNumber(wood);
            if (metalText != null) metalText.text = FormatNumber(metal);
            if (crystalText != null) crystalText.text = FormatNumber(crystal);
        }

        private string FormatNumber(int value)
        {
            if (value >= 1000000) return $"{value / 1000000f:F1}M";
            if (value >= 1000) return $"{value / 1000f:F1}K";
            return value.ToString();
        }

        private bool CanAfford(int cost)
        {
            // TODO: Check actual player resources
            return true;
        }

        private void SetStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
        }

        #endregion

        #region Event Handlers

        private void OnBlockPlaced(BuildingBlock block)
        {
            SetStatus($"Placed: {block.Type}");
            // Update block count
        }

        private void OnBlockRemoved(BuildingBlock block)
        {
            SetStatus($"Removed: {block.Type}");
        }

        private void OnBlockSelected(BuildingBlock block)
        {
            if (block == null)
            {
                selectedBlockName.text = "No Selection";
                selectedBlockDesc.text = "Select a block to view properties";
                selectedBlockStats.text = "";
                return;
            }

            var def = BlockDefinitions.Get(block.Type);
            selectedBlockName.text = def?.DisplayName ?? block.Type.ToString();
            selectedBlockDesc.text = def?.Description ?? "";
            selectedBlockStats.text = $"Position: {block.LocalPosition:F1}\n" +
                                      $"Rotation: {block.LocalRotation.eulerAngles.y:F0}°\n" +
                                      $"Health: {block.Health}/{block.MaxHealth}";

            rotationSlider.value = block.LocalRotation.eulerAngles.y;
        }

        private void OnRotationChanged(float value)
        {
            // TODO: Apply rotation to selected block
        }

        private void OnScaleChanged(string value)
        {
            // TODO: Apply scale to selected block
        }

        private void HandleKeyboardShortcuts()
        {
            // Tool shortcuts
            if (Input.GetKeyDown(KeyCode.V)) SelectTool(EditorTool.Select);
            if (Input.GetKeyDown(KeyCode.P)) SelectTool(EditorTool.Place);
            if (Input.GetKeyDown(KeyCode.M)) SelectTool(EditorTool.Move);
            if (Input.GetKeyDown(KeyCode.R)) SelectTool(EditorTool.Rotate);
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Delete)) SelectTool(EditorTool.Delete);
            if (Input.GetKeyDown(KeyCode.C)) SelectTool(EditorTool.Copy);

            // Category shortcuts (1-8)
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectCategory(BlockCategory.Basic);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectCategory(BlockCategory.Walls);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectCategory(BlockCategory.Defense);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SelectCategory(BlockCategory.Production);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SelectCategory(BlockCategory.Military);
            if (Input.GetKeyDown(KeyCode.Alpha6)) SelectCategory(BlockCategory.Storage);
            if (Input.GetKeyDown(KeyCode.Alpha7)) SelectCategory(BlockCategory.Decoration);
            if (Input.GetKeyDown(KeyCode.Alpha8)) SelectCategory(BlockCategory.Special);

            // Escape to close
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentTool != EditorTool.Select)
                {
                    SelectTool(EditorTool.Select);
                }
            }
        }

        private void ToggleTemplatesPanel()
        {
            templatesExpanded = !templatesExpanded;
            // TODO: Animate panel expansion
        }

        #endregion

        #region Actions

        private void SaveBlueprint()
        {
            if (baseEditor == null) return;

            var blueprint = baseEditor.SaveAsBlueprint("My Blueprint");
            SetStatus($"Saved: {blueprint.Name}");
        }

        private void LoadBlueprint()
        {
            // TODO: Open blueprint browser
            SetStatus("Load blueprint...");
        }

        private void ExitEditor()
        {
            baseEditor?.ExitEditorMode(true);
        }

        #endregion

        #region Show/Hide

        public void Show()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                isVisible = true;
                SetStatus("Editor ready");
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

        #region UI Helpers

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
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }

        private Button CreateIconButton(RectTransform parent, string icon, Action onClick, string tooltip = "")
        {
            GameObject btnObj = CreateUIObject("IconButton", parent);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = BUTTON_NORMAL;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = BUTTON_NORMAL;
            colors.highlightedColor = BUTTON_HOVER;
            colors.pressedColor = BUTTON_SELECTED;
            btn.colors = colors;

            TextMeshProUGUI iconText = CreateText(btnObj.GetComponent<RectTransform>(), icon, 18);
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
            // Simple tooltip via event triggers
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
                rt.sizeDelta = new Vector2(200, 60);

                Image bg = tooltipObj.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

                tooltipText = CreateText(rt, "", 11);
                SetAnchors(tooltipText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 5), new Vector2(-8, -5));
                tooltipText.alignment = TextAlignmentOptions.TopLeft;
            }

            tooltipText.text = text;
            tooltipObj.SetActive(true);

            // Position near mouse
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
