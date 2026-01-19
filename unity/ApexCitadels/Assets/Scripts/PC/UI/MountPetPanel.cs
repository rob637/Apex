// ============================================================================
// APEX CITADELS - MOUNT & PET SYSTEM PANEL
// Companion creatures and mounts for travel, combat, and bonuses
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
    // Mount/Pet view tabs
    public enum CompanionTab
    {
        Mounts,
        Pets,
        Stable,
        Breeding,
        Shop
    }

    // Companion rarity
    public enum CompanionRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    // Mount type
    public enum MountType
    {
        Horse,
        Wolf,
        Bear,
        Griffin,
        Dragon,
        Phoenix,
        Unicorn,
        Mechanical,
        Elemental
    }

    // Pet type
    public enum PetType
    {
        Cat,
        Dog,
        Owl,
        Falcon,
        Fox,
        Spirit,
        Imp,
        Golem,
        Fairy
    }

    // Companion data
    [System.Serializable]
    public class CompanionData
    {
        public string companionId;
        public string name;
        public string customName;
        public bool isMount;
        public MountType mountType;
        public PetType petType;
        public CompanionRarity rarity;
        public int level;
        public int experience;
        public int experienceToNext;
        public int happiness;
        public int hunger;
        public int stamina;
        public int maxStamina;
        public float speedBonus;
        public float combatBonus;
        public float gatherBonus;
        public List<string> abilities = new List<string>();
        public List<string> traits = new List<string>();
        public DateTime lastFed;
        public DateTime acquiredDate;
        public bool isEquipped;
        public bool isInStable;
        public string appearanceId;
    }

    // Breeding pair
    [System.Serializable]
    public class BreedingPair
    {
        public string breedingId;
        public CompanionData parent1;
        public CompanionData parent2;
        public DateTime startTime;
        public DateTime completionTime;
        public bool isComplete;
        public CompanionRarity predictedRarity;
    }

    public class MountPetPanel : MonoBehaviour
    {
        public static MountPetPanel Instance { get; private set; }

        [Header("Runtime References")]
        private Canvas parentCanvas;
        private GameObject panelRoot;
        private bool isVisible = false;

        // UI References
        private RectTransform mainPanel;
        private RectTransform contentPanel;
        private RectTransform companionListPanel;
        private RectTransform previewPanel;
        private RectTransform statsPanel;
        private ScrollRect companionScroll;
        private TextMeshProUGUI headerTitle;
        private TextMeshProUGUI companionNameText;
        private TextMeshProUGUI companionLevelText;
        private TextMeshProUGUI companionStatsText;
        private Image companionPreviewImage;
        private Slider happinessSlider;
        private Slider hungerSlider;
        private Slider staminaSlider;
        private Slider expSlider;

        // State
        private CompanionTab currentTab = CompanionTab.Mounts;
        private List<CompanionData> allCompanions = new List<CompanionData>();
        private List<BreedingPair> breedingPairs = new List<BreedingPair>();
        private CompanionData selectedCompanion;
        private CompanionData equippedMount;
        private CompanionData equippedPet;
        private List<GameObject> companionItems = new List<GameObject>();

        // Colors
        private readonly Color COMMON_COLOR = new Color(0.7f, 0.7f, 0.7f);
        private readonly Color UNCOMMON_COLOR = new Color(0.3f, 0.8f, 0.3f);
        private readonly Color RARE_COLOR = new Color(0.3f, 0.5f, 1f);
        private readonly Color EPIC_COLOR = new Color(0.7f, 0.3f, 0.9f);
        private readonly Color LEGENDARY_COLOR = new Color(1f, 0.6f, 0.1f);
        private readonly Color MYTHIC_COLOR = new Color(1f, 0.3f, 0.5f);

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
                UpdateCompanionStats();
            }

            // Toggle with M key
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (isVisible) Hide();
                else Show();
            }
        }

        private void CreateUI()
        {
            // Panel root
            panelRoot = new GameObject("MountPetPanel_Root");
            panelRoot.transform.SetParent(parentCanvas.transform, false);

            mainPanel = panelRoot.AddComponent<RectTransform>();
            mainPanel.anchorMin = new Vector2(0.1f, 0.1f);
            mainPanel.anchorMax = new Vector2(0.9f, 0.9f);
            mainPanel.offsetMin = Vector2.zero;
            mainPanel.offsetMax = Vector2.zero;

            // Background
            Image mainBg = panelRoot.AddComponent<Image>();
            mainBg.color = new Color(0.08f, 0.1f, 0.12f, 0.98f);

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
            CreateActionBar();

            LoadMockCompanions();
            RefreshCompanionList();
        }

        private void CreateHeader()
        {
            // Header container
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(panelRoot.transform, false);

            RectTransform headerRT = headerObj.AddComponent<RectTransform>();
            LayoutElement headerLE = headerObj.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 60;
            headerLE.flexibleWidth = 1;

            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0.3f, 0.4f, 0.3f, 0.9f);

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
            TextMeshProUGUI iconTMP = iconObj.AddComponent<TextMeshProUGUI>();
            iconTMP.text = "🐎";
            iconTMP.fontSize = 32;
            iconTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 50;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            headerTitle = titleObj.AddComponent<TextMeshProUGUI>();
            headerTitle.text = "MOUNTS & PETS";
            headerTitle.fontSize = 28;
            headerTitle.fontStyle = FontStyles.Bold;
            headerTitle.color = Color.white;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;

            // Currency display
            GameObject currencyObj = new GameObject("Currency");
            currencyObj.transform.SetParent(headerObj.transform, false);
            TextMeshProUGUI currencyTMP = currencyObj.AddComponent<TextMeshProUGUI>();
            currencyTMP.text = "🦴 1,250 | 🌟 50";
            currencyTMP.fontSize = 18;
            currencyTMP.color = new Color(1f, 0.9f, 0.5f);
            currencyTMP.alignment = TextAlignmentOptions.Right;
            LayoutElement currencyLE = currencyObj.AddComponent<LayoutElement>();
            currencyLE.preferredWidth = 150;

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

            string[] tabNames = { "🐎 Mounts", "🐾 Pets", "🏠 Stable", "💕 Breeding", "🛒 Shop" };
            CompanionTab[] tabValues = { CompanionTab.Mounts, CompanionTab.Pets, CompanionTab.Stable, CompanionTab.Breeding, CompanionTab.Shop };

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
            btnBg.color = new Color(0.2f, 0.25f, 0.2f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.25f, 0.2f);
            colors.highlightedColor = new Color(0.3f, 0.4f, 0.3f);
            colors.pressedColor = new Color(0.35f, 0.45f, 0.35f);
            colors.selectedColor = new Color(0.4f, 0.5f, 0.3f);
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 14;
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
            contentBg.color = new Color(0.1f, 0.12f, 0.1f);

            HorizontalLayoutGroup contentLayout = contentObj.AddComponent<HorizontalLayoutGroup>();
            contentLayout.padding = new RectOffset(10, 10, 10, 10);
            contentLayout.spacing = 15;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;

            // Left: Companion list
            CreateCompanionListPanel(contentObj.transform);
            
            // Center: Preview
            CreatePreviewPanel(contentObj.transform);
            
            // Right: Stats
            CreateStatsPanel(contentObj.transform);
        }

        private void CreateCompanionListPanel(Transform parent)
        {
            GameObject listPanel = new GameObject("CompanionList");
            listPanel.transform.SetParent(parent, false);

            companionListPanel = listPanel.AddComponent<RectTransform>();
            LayoutElement listLE = listPanel.AddComponent<LayoutElement>();
            listLE.preferredWidth = 280;
            listLE.flexibleHeight = 1;

            Image listBg = listPanel.AddComponent<Image>();
            listBg.color = new Color(0.08f, 0.1f, 0.08f);

            VerticalLayoutGroup listLayout = listPanel.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(5, 5, 5, 5);
            listLayout.spacing = 5;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;

            // Filter/sort row
            GameObject filterObj = new GameObject("Filter");
            filterObj.transform.SetParent(listPanel.transform, false);
            LayoutElement filterLE = filterObj.AddComponent<LayoutElement>();
            filterLE.preferredHeight = 35;
            filterLE.flexibleWidth = 1;

            // Scroll view
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(listPanel.transform, false);

            RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
            LayoutElement scrollLE = scrollObj.AddComponent<LayoutElement>();
            scrollLE.flexibleHeight = 1;
            scrollLE.flexibleWidth = 1;

            companionScroll = scrollObj.AddComponent<ScrollRect>();
            companionScroll.horizontal = false;
            companionScroll.vertical = true;
            companionScroll.movementType = ScrollRect.MovementType.Elastic;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.06f, 0.08f, 0.06f);

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

            companionScroll.viewport = viewportRT;

            // Content container
            GameObject contentContainerObj = new GameObject("Content");
            contentContainerObj.transform.SetParent(viewport.transform, false);
            RectTransform contentContainerRT = contentContainerObj.AddComponent<RectTransform>();
            contentContainerRT.anchorMin = new Vector2(0, 1);
            contentContainerRT.anchorMax = new Vector2(1, 1);
            contentContainerRT.pivot = new Vector2(0.5f, 1);
            contentContainerRT.offsetMin = Vector2.zero;
            contentContainerRT.offsetMax = Vector2.zero;

            // Grid layout for companion cards
            GridLayoutGroup gridLayout = contentContainerObj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(125, 140);
            gridLayout.spacing = new Vector2(8, 8);
            gridLayout.padding = new RectOffset(5, 5, 5, 5);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;

            ContentSizeFitter csf = contentContainerObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            companionScroll.content = contentContainerRT;
        }

        private void CreatePreviewPanel(Transform parent)
        {
            GameObject previewObj = new GameObject("Preview");
            previewObj.transform.SetParent(parent, false);

            previewPanel = previewObj.AddComponent<RectTransform>();
            LayoutElement previewLE = previewObj.AddComponent<LayoutElement>();
            previewLE.flexibleWidth = 1;
            previewLE.flexibleHeight = 1;

            Image previewBg = previewObj.AddComponent<Image>();
            previewBg.color = new Color(0.12f, 0.15f, 0.12f);

            VerticalLayoutGroup previewLayout = previewObj.AddComponent<VerticalLayoutGroup>();
            previewLayout.padding = new RectOffset(15, 15, 15, 15);
            previewLayout.spacing = 10;
            previewLayout.childAlignment = TextAnchor.UpperCenter;
            previewLayout.childForceExpandWidth = true;
            previewLayout.childForceExpandHeight = false;
            previewLayout.childControlWidth = true;
            previewLayout.childControlHeight = true;

            // Companion name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(previewObj.transform, false);
            companionNameText = nameObj.AddComponent<TextMeshProUGUI>();
            companionNameText.text = "Select a Companion";
            companionNameText.fontSize = 24;
            companionNameText.fontStyle = FontStyles.Bold;
            companionNameText.color = Color.white;
            companionNameText.alignment = TextAlignmentOptions.Center;
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 35;

            // Level
            GameObject levelObj = new GameObject("Level");
            levelObj.transform.SetParent(previewObj.transform, false);
            companionLevelText = levelObj.AddComponent<TextMeshProUGUI>();
            companionLevelText.text = "";
            companionLevelText.fontSize = 16;
            companionLevelText.color = new Color(0.8f, 0.8f, 0.8f);
            companionLevelText.alignment = TextAlignmentOptions.Center;
            LayoutElement levelLE = levelObj.AddComponent<LayoutElement>();
            levelLE.preferredHeight = 25;

            // Preview image area
            GameObject imageObj = new GameObject("PreviewImage");
            imageObj.transform.SetParent(previewObj.transform, false);
            companionPreviewImage = imageObj.AddComponent<Image>();
            companionPreviewImage.color = new Color(0.2f, 0.25f, 0.2f);
            LayoutElement imageLE = imageObj.AddComponent<LayoutElement>();
            imageLE.flexibleHeight = 1;
            imageLE.preferredHeight = 200;

            // Status bars
            CreateStatusBar(previewObj.transform, "Happiness", out happinessSlider, new Color(1f, 0.6f, 0.8f));
            CreateStatusBar(previewObj.transform, "Hunger", out hungerSlider, new Color(0.8f, 0.5f, 0.3f));
            CreateStatusBar(previewObj.transform, "Stamina", out staminaSlider, new Color(0.3f, 0.7f, 0.9f));
            CreateStatusBar(previewObj.transform, "Experience", out expSlider, new Color(0.6f, 0.9f, 0.3f));
        }

        private void CreateStatusBar(Transform parent, string label, out Slider slider, Color fillColor)
        {
            GameObject barObj = new GameObject(label + "Bar");
            barObj.transform.SetParent(parent, false);

            RectTransform barRT = barObj.AddComponent<RectTransform>();
            LayoutElement barLE = barObj.AddComponent<LayoutElement>();
            barLE.preferredHeight = 30;
            barLE.flexibleWidth = 1;

            HorizontalLayoutGroup barLayout = barObj.AddComponent<HorizontalLayoutGroup>();
            barLayout.spacing = 10;
            barLayout.childForceExpandWidth = false;
            barLayout.childForceExpandHeight = true;
            barLayout.childControlWidth = true;
            barLayout.childControlHeight = true;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(barObj.transform, false);
            TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 14;
            labelTMP.color = Color.white;
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 80;

            // Slider background
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(barObj.transform, false);

            RectTransform sliderRT = sliderObj.AddComponent<RectTransform>();
            LayoutElement sliderLE = sliderObj.AddComponent<LayoutElement>();
            sliderLE.flexibleWidth = 1;
            sliderLE.preferredHeight = 20;

            Image sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = new Color(0.15f, 0.15f, 0.15f);

            slider = sliderObj.AddComponent<Slider>();
            slider.interactable = false;
            slider.minValue = 0;
            slider.maxValue = 100;

            // Fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = new Vector2(2, 2);
            fillAreaRT.offsetMax = new Vector2(-2, -2);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRT = fill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;

            slider.fillRect = fillRT;
        }

        private void CreateStatsPanel(Transform parent)
        {
            GameObject statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(parent, false);

            statsPanel = statsObj.AddComponent<RectTransform>();
            LayoutElement statsLE = statsObj.AddComponent<LayoutElement>();
            statsLE.preferredWidth = 220;
            statsLE.flexibleHeight = 1;

            Image statsBg = statsObj.AddComponent<Image>();
            statsBg.color = new Color(0.08f, 0.1f, 0.08f);

            VerticalLayoutGroup statsLayout = statsObj.AddComponent<VerticalLayoutGroup>();
            statsLayout.padding = new RectOffset(10, 10, 10, 10);
            statsLayout.spacing = 8;
            statsLayout.childForceExpandWidth = true;
            statsLayout.childForceExpandHeight = false;
            statsLayout.childControlWidth = true;
            statsLayout.childControlHeight = true;

            // Stats title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(statsObj.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "📊 BONUSES & ABILITIES";
            titleTMP.fontSize = 16;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0.9f, 0.8f, 0.4f);
            titleTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 30;

            // Stats text
            GameObject statsTextObj = new GameObject("StatsText");
            statsTextObj.transform.SetParent(statsObj.transform, false);
            companionStatsText = statsTextObj.AddComponent<TextMeshProUGUI>();
            companionStatsText.text = "Select a companion\nto view stats";
            companionStatsText.fontSize = 14;
            companionStatsText.color = new Color(0.8f, 0.8f, 0.8f);
            companionStatsText.alignment = TextAlignmentOptions.Left;
            LayoutElement statsTextLE = statsTextObj.AddComponent<LayoutElement>();
            statsTextLE.flexibleHeight = 1;
        }

        private void CreateActionBar()
        {
            GameObject actionObj = new GameObject("ActionBar");
            actionObj.transform.SetParent(panelRoot.transform, false);

            RectTransform actionRT = actionObj.AddComponent<RectTransform>();
            LayoutElement actionLE = actionObj.AddComponent<LayoutElement>();
            actionLE.preferredHeight = 60;
            actionLE.flexibleWidth = 1;

            Image actionBg = actionObj.AddComponent<Image>();
            actionBg.color = new Color(0.1f, 0.12f, 0.1f);

            HorizontalLayoutGroup actionLayout = actionObj.AddComponent<HorizontalLayoutGroup>();
            actionLayout.padding = new RectOffset(20, 20, 10, 10);
            actionLayout.spacing = 15;
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = true;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;

            // Action buttons
            CreateActionButton(actionObj.transform, "🎠 Equip", EquipCompanion, new Color(0.3f, 0.5f, 0.3f));
            CreateActionButton(actionObj.transform, "🍖 Feed", FeedCompanion, new Color(0.5f, 0.4f, 0.2f));
            CreateActionButton(actionObj.transform, "✏️ Rename", RenameCompanion, new Color(0.3f, 0.4f, 0.5f));
            CreateActionButton(actionObj.transform, "⬆️ Level Up", LevelUpCompanion, new Color(0.4f, 0.5f, 0.2f));
            CreateActionButton(actionObj.transform, "🏠 To Stable", SendToStable, new Color(0.4f, 0.3f, 0.3f));
            CreateActionButton(actionObj.transform, "💔 Release", ReleaseCompanion, new Color(0.5f, 0.2f, 0.2f));
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
            btnLE.preferredWidth = 110;
            btnLE.preferredHeight = 40;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 13;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }

        private void LoadMockCompanions()
        {
            allCompanions.Clear();

            // Mounts
            allCompanions.Add(new CompanionData
            {
                companionId = "mount_001",
                name = "Shadowmane",
                customName = "Shadow",
                isMount = true,
                mountType = MountType.Horse,
                rarity = CompanionRarity.Rare,
                level = 15,
                experience = 2400,
                experienceToNext = 3000,
                happiness = 85,
                hunger = 60,
                stamina = 90,
                maxStamina = 100,
                speedBonus = 25f,
                combatBonus = 5f,
                gatherBonus = 0f,
                abilities = new List<string> { "Sprint", "Kick" },
                traits = new List<string> { "Swift", "Loyal" },
                acquiredDate = DateTime.Now.AddDays(-30),
                isEquipped = true
            });

            allCompanions.Add(new CompanionData
            {
                companionId = "mount_002",
                name = "Frostfang",
                isMount = true,
                mountType = MountType.Wolf,
                rarity = CompanionRarity.Epic,
                level = 22,
                experience = 5600,
                experienceToNext = 7000,
                happiness = 72,
                hunger = 40,
                stamina = 85,
                maxStamina = 100,
                speedBonus = 30f,
                combatBonus = 15f,
                gatherBonus = 0f,
                abilities = new List<string> { "Howl", "Pack Tactics", "Bite" },
                traits = new List<string> { "Ferocious", "Pack Leader" },
                acquiredDate = DateTime.Now.AddDays(-15)
            });

            allCompanions.Add(new CompanionData
            {
                companionId = "mount_003",
                name = "Emberwing",
                isMount = true,
                mountType = MountType.Griffin,
                rarity = CompanionRarity.Legendary,
                level = 35,
                experience = 12000,
                experienceToNext = 15000,
                happiness = 95,
                hunger = 80,
                stamina = 100,
                maxStamina = 100,
                speedBonus = 50f,
                combatBonus = 20f,
                gatherBonus = 10f,
                abilities = new List<string> { "Flight", "Dive Attack", "Screech", "Wind Shield" },
                traits = new List<string> { "Majestic", "Loyal", "Ancient" },
                acquiredDate = DateTime.Now.AddDays(-60)
            });

            // Pets
            allCompanions.Add(new CompanionData
            {
                companionId = "pet_001",
                name = "Whiskers",
                isMount = false,
                petType = PetType.Cat,
                rarity = CompanionRarity.Common,
                level = 8,
                experience = 1200,
                experienceToNext = 1500,
                happiness = 90,
                hunger = 50,
                stamina = 100,
                maxStamina = 100,
                speedBonus = 0f,
                combatBonus = 2f,
                gatherBonus = 10f,
                abilities = new List<string> { "Lucky Find" },
                traits = new List<string> { "Curious" },
                acquiredDate = DateTime.Now.AddDays(-45),
                isEquipped = true
            });

            allCompanions.Add(new CompanionData
            {
                companionId = "pet_002",
                name = "Hoots",
                isMount = false,
                petType = PetType.Owl,
                rarity = CompanionRarity.Uncommon,
                level = 12,
                experience = 2800,
                experienceToNext = 3500,
                happiness = 75,
                hunger = 65,
                stamina = 80,
                maxStamina = 100,
                speedBonus = 0f,
                combatBonus = 5f,
                gatherBonus = 15f,
                abilities = new List<string> { "Night Vision", "Scout" },
                traits = new List<string> { "Wise", "Vigilant" },
                acquiredDate = DateTime.Now.AddDays(-20)
            });

            allCompanions.Add(new CompanionData
            {
                companionId = "pet_003",
                name = "Sparkle",
                isMount = false,
                petType = PetType.Fairy,
                rarity = CompanionRarity.Mythic,
                level = 50,
                experience = 24000,
                experienceToNext = 25000,
                happiness = 100,
                hunger = 100,
                stamina = 100,
                maxStamina = 100,
                speedBonus = 10f,
                combatBonus = 25f,
                gatherBonus = 50f,
                abilities = new List<string> { "Healing Aura", "Luck Blessing", "Resource Sense", "Magic Shield" },
                traits = new List<string> { "Mythical", "Benevolent", "Eternal" },
                acquiredDate = DateTime.Now.AddDays(-100)
            });

            // Set equipped references
            equippedMount = allCompanions.Find(c => c.isMount && c.isEquipped);
            equippedPet = allCompanions.Find(c => !c.isMount && c.isEquipped);
        }

        private void RefreshCompanionList()
        {
            // Clear existing items
            foreach (var item in companionItems)
            {
                Destroy(item);
            }
            companionItems.Clear();

            // Filter by current tab
            var filtered = FilterCompanionsByTab(currentTab);

            foreach (var companion in filtered)
            {
                CreateCompanionCard(companion);
            }
        }

        private List<CompanionData> FilterCompanionsByTab(CompanionTab tab)
        {
            return tab switch
            {
                CompanionTab.Mounts => allCompanions.FindAll(c => c.isMount && !c.isInStable),
                CompanionTab.Pets => allCompanions.FindAll(c => !c.isMount && !c.isInStable),
                CompanionTab.Stable => allCompanions.FindAll(c => c.isInStable),
                CompanionTab.Breeding => allCompanions, // Show all for breeding selection
                CompanionTab.Shop => new List<CompanionData>(), // Empty for shop (handled separately)
                _ => allCompanions
            };
        }

        private void CreateCompanionCard(CompanionData companion)
        {
            GameObject cardObj = new GameObject(companion.name);
            cardObj.transform.SetParent(companionScroll.content, false);

            RectTransform cardRT = cardObj.AddComponent<RectTransform>();

            // Rarity border color
            Color rarityColor = GetRarityColor(companion.rarity);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.15f, 0.18f, 0.15f);

            // Border - using shadow as outline effect
            Shadow cardShadow = cardObj.AddComponent<Shadow>();
            cardShadow.effectColor = rarityColor;
            cardShadow.effectDistance = new Vector2(2, -2);

            Button cardBtn = cardObj.AddComponent<Button>();
            cardBtn.onClick.AddListener(() => SelectCompanion(companion));

            VerticalLayoutGroup cardLayout = cardObj.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(5, 5, 5, 5);
            cardLayout.spacing = 3;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            // Icon/image
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(cardObj.transform, false);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.color = rarityColor * 0.5f;
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredHeight = 60;
            iconLE.flexibleWidth = 1;

            // Type emoji
            GameObject emojiObj = new GameObject("Emoji");
            emojiObj.transform.SetParent(iconObj.transform, false);
            TextMeshProUGUI emojiTMP = emojiObj.AddComponent<TextMeshProUGUI>();
            emojiTMP.text = GetCompanionEmoji(companion);
            emojiTMP.fontSize = 32;
            emojiTMP.alignment = TextAlignmentOptions.Center;
            RectTransform emojiRT = emojiObj.GetComponent<RectTransform>();
            emojiRT.anchorMin = Vector2.zero;
            emojiRT.anchorMax = Vector2.one;
            emojiRT.offsetMin = Vector2.zero;
            emojiRT.offsetMax = Vector2.zero;

            // Equipped indicator
            if (companion.isEquipped)
            {
                GameObject equippedObj = new GameObject("Equipped");
                equippedObj.transform.SetParent(iconObj.transform, false);
                TextMeshProUGUI equippedTMP = equippedObj.AddComponent<TextMeshProUGUI>();
                equippedTMP.text = "✓";
                equippedTMP.fontSize = 18;
                equippedTMP.color = new Color(0.3f, 1f, 0.3f);
                equippedTMP.alignment = TextAlignmentOptions.TopRight;
                RectTransform equippedRT = equippedObj.GetComponent<RectTransform>();
                equippedRT.anchorMin = Vector2.zero;
                equippedRT.anchorMax = Vector2.one;
                equippedRT.offsetMin = new Vector2(0, 0);
                equippedRT.offsetMax = new Vector2(-5, -5);
            }

            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(cardObj.transform, false);
            TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = string.IsNullOrEmpty(companion.customName) ? companion.name : companion.customName;
            nameTMP.fontSize = 12;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color = rarityColor;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.enableWordWrapping = false;
            nameTMP.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 18;

            // Level
            GameObject levelObj = new GameObject("Level");
            levelObj.transform.SetParent(cardObj.transform, false);
            TextMeshProUGUI levelTMP = levelObj.AddComponent<TextMeshProUGUI>();
            levelTMP.text = $"Lv.{companion.level}";
            levelTMP.fontSize = 11;
            levelTMP.color = new Color(0.7f, 0.7f, 0.7f);
            levelTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement levelLE = levelObj.AddComponent<LayoutElement>();
            levelLE.preferredHeight = 16;

            // Rarity
            GameObject rarityObj = new GameObject("Rarity");
            rarityObj.transform.SetParent(cardObj.transform, false);
            TextMeshProUGUI rarityTMP = rarityObj.AddComponent<TextMeshProUGUI>();
            rarityTMP.text = companion.rarity.ToString();
            rarityTMP.fontSize = 10;
            rarityTMP.color = rarityColor;
            rarityTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement rarityLE = rarityObj.AddComponent<LayoutElement>();
            rarityLE.preferredHeight = 14;

            companionItems.Add(cardObj);
        }

        private string GetCompanionEmoji(CompanionData companion)
        {
            if (companion.isMount)
            {
                return companion.mountType switch
                {
                    MountType.Horse => "🐎",
                    MountType.Wolf => "🐺",
                    MountType.Bear => "🐻",
                    MountType.Griffin => "🦅",
                    MountType.Dragon => "🐉",
                    MountType.Phoenix => "🔥",
                    MountType.Unicorn => "🦄",
                    MountType.Mechanical => "⚙️",
                    MountType.Elemental => "✨",
                    _ => "🐎"
                };
            }
            else
            {
                return companion.petType switch
                {
                    PetType.Cat => "🐱",
                    PetType.Dog => "🐕",
                    PetType.Owl => "🦉",
                    PetType.Falcon => "🦅",
                    PetType.Fox => "🦊",
                    PetType.Spirit => "👻",
                    PetType.Imp => "😈",
                    PetType.Golem => "🗿",
                    PetType.Fairy => "🧚",
                    _ => "🐾"
                };
            }
        }

        private Color GetRarityColor(CompanionRarity rarity)
        {
            return rarity switch
            {
                CompanionRarity.Common => COMMON_COLOR,
                CompanionRarity.Uncommon => UNCOMMON_COLOR,
                CompanionRarity.Rare => RARE_COLOR,
                CompanionRarity.Epic => EPIC_COLOR,
                CompanionRarity.Legendary => LEGENDARY_COLOR,
                CompanionRarity.Mythic => MYTHIC_COLOR,
                _ => Color.white
            };
        }

        private void SelectCompanion(CompanionData companion)
        {
            selectedCompanion = companion;
            UpdatePreview();
            ApexLogger.Log($"[MountPet] Selected: {companion.name} (Lv.{companion.level} {companion.rarity})", ApexLogger.LogCategory.UI);
        }

        private void UpdatePreview()
        {
            if (selectedCompanion == null)
            {
                companionNameText.text = "Select a Companion";
                companionLevelText.text = "";
                companionStatsText.text = "";
                return;
            }

            var c = selectedCompanion;
            Color rarityColor = GetRarityColor(c.rarity);

            companionNameText.text = string.IsNullOrEmpty(c.customName) ? c.name : $"{c.customName} ({c.name})";
            companionNameText.color = rarityColor;

            companionLevelText.text = $"Level {c.level} {c.rarity} {(c.isMount ? "Mount" : "Pet")}";

            happinessSlider.value = c.happiness;
            hungerSlider.value = c.hunger;
            staminaSlider.value = c.stamina;
            expSlider.maxValue = c.experienceToNext;
            expSlider.value = c.experience;

            // Build stats text
            string stats = $"<b>Type:</b> {(c.isMount ? c.mountType.ToString() : c.petType.ToString())}\n\n";
            stats += $"<b>Bonuses:</b>\n";
            if (c.speedBonus > 0) stats += $"  • Speed: +{c.speedBonus}%\n";
            if (c.combatBonus > 0) stats += $"  • Combat: +{c.combatBonus}%\n";
            if (c.gatherBonus > 0) stats += $"  • Gathering: +{c.gatherBonus}%\n";
            
            stats += $"\n<b>Abilities:</b>\n";
            foreach (var ability in c.abilities)
            {
                stats += $"  • {ability}\n";
            }
            
            stats += $"\n<b>Traits:</b>\n";
            foreach (var trait in c.traits)
            {
                stats += $"  • {trait}\n";
            }

            companionStatsText.text = stats;
        }

        private void UpdateCompanionStats()
        {
            // Update real-time stats display
            if (selectedCompanion != null)
            {
                // Simulate hunger decrease over time
                // In real game, this would sync with server
            }
        }

        private void SwitchTab(CompanionTab tab)
        {
            currentTab = tab;
            RefreshCompanionList();
            ApexLogger.Log($"[MountPet] Switched to tab: {tab}", ApexLogger.LogCategory.UI);
        }

        // Action handlers
        private void EquipCompanion()
        {
            if (selectedCompanion == null)
            {
                ApexLogger.Log("[MountPet] No companion selected", ApexLogger.LogCategory.UI);
                return;
            }

            // Unequip current
            if (selectedCompanion.isMount)
            {
                if (equippedMount != null) equippedMount.isEquipped = false;
                equippedMount = selectedCompanion;
            }
            else
            {
                if (equippedPet != null) equippedPet.isEquipped = false;
                equippedPet = selectedCompanion;
            }

            selectedCompanion.isEquipped = true;
            RefreshCompanionList();
            UpdatePreview();
            ApexLogger.Log($"[MountPet] Equipped: {selectedCompanion.name}", ApexLogger.LogCategory.UI);
        }

        private void FeedCompanion()
        {
            if (selectedCompanion == null) return;
            selectedCompanion.hunger = Mathf.Min(100, selectedCompanion.hunger + 30);
            selectedCompanion.happiness = Mathf.Min(100, selectedCompanion.happiness + 10);
            UpdatePreview();
            ApexLogger.Log($"[MountPet] Fed: {selectedCompanion.name}", ApexLogger.LogCategory.UI);
        }

        private void RenameCompanion()
        {
            if (selectedCompanion == null) return;
            ApexLogger.Log($"[MountPet] Rename dialog for: {selectedCompanion.name}", ApexLogger.LogCategory.UI);
        }

        private void LevelUpCompanion()
        {
            if (selectedCompanion == null) return;
            if (selectedCompanion.experience >= selectedCompanion.experienceToNext)
            {
                selectedCompanion.level++;
                selectedCompanion.experience = 0;
                selectedCompanion.experienceToNext = (int)(selectedCompanion.experienceToNext * 1.2f);
                UpdatePreview();
                ApexLogger.Log($"[MountPet] Leveled up: {selectedCompanion.name} to Lv.{selectedCompanion.level}", ApexLogger.LogCategory.UI);
            }
        }

        private void SendToStable()
        {
            if (selectedCompanion == null) return;
            selectedCompanion.isInStable = true;
            selectedCompanion.isEquipped = false;
            RefreshCompanionList();
            ApexLogger.Log($"[MountPet] Sent to stable: {selectedCompanion.name}", ApexLogger.LogCategory.UI);
        }

        private void ReleaseCompanion()
        {
            if (selectedCompanion == null) return;
            ApexLogger.Log($"[MountPet] Release confirmation for: {selectedCompanion.name}", ApexLogger.LogCategory.UI);
        }

        // Public API
        public void Show()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                isVisible = true;
                RefreshCompanionList();
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

        public CompanionData GetEquippedMount() => equippedMount;
        public CompanionData GetEquippedPet() => equippedPet;
        
        public float GetTotalSpeedBonus()
        {
            float bonus = 0;
            if (equippedMount != null) bonus += equippedMount.speedBonus;
            if (equippedPet != null) bonus += equippedPet.speedBonus;
            return bonus;
        }

        public float GetTotalCombatBonus()
        {
            float bonus = 0;
            if (equippedMount != null) bonus += equippedMount.combatBonus;
            if (equippedPet != null) bonus += equippedPet.combatBonus;
            return bonus;
        }

        public float GetTotalGatherBonus()
        {
            float bonus = 0;
            if (equippedMount != null) bonus += equippedMount.gatherBonus;
            if (equippedPet != null) bonus += equippedPet.gatherBonus;
            return bonus;
        }
    }
}
