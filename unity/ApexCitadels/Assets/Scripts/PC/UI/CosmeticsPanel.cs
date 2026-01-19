using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Cosmetics System - Visual customization for players.
    /// Skins, themes, effects, and personalization options.
    /// 
    /// Features:
    /// - Character skins
    /// - Building themes
    /// - Troop skins
    /// - UI themes
    /// - Particle effects
    /// - Emotes
    /// - Titles/Badges
    /// - Name colors
    /// </summary>
    public class CosmeticsPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.8f, 0.4f, 0.8f);
        [SerializeField] private Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        [SerializeField] private Color commonColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color rareColor = new Color(0.3f, 0.5f, 0.9f);
        [SerializeField] private Color epicColor = new Color(0.7f, 0.3f, 0.9f);
        [SerializeField] private Color legendaryColor = new Color(1f, 0.6f, 0.1f);
        [SerializeField] private Color ownedColor = new Color(0.3f, 0.6f, 0.3f);
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _itemsContainer;
        private GameObject _previewPanel;
        
        // Category buttons
        private Button _skinsTab;
        private Button _themesTab;
        private Button _effectsTab;
        private Button _emotesTab;
        private Button _titlesTab;
        private CosmeticCategory _currentCategory = CosmeticCategory.Skins;
        
        // Preview
        private TextMeshProUGUI _itemName;
        private TextMeshProUGUI _itemDescription;
        private TextMeshProUGUI _itemRarity;
        private TextMeshProUGUI _itemPrice;
        private GameObject _previewArea;
        private Button _equipButton;
        private Button _purchaseButton;
        
        // State
        private List<CosmeticItem> _allItems = new List<CosmeticItem>();
        private List<CosmeticItem> _filteredItems = new List<CosmeticItem>();
        private CosmeticItem _selectedItem;
        private Dictionary<CosmeticCategory, string> _equippedItems = new Dictionary<CosmeticCategory, string>();
        private int _playerGems = 2500;
        
        public static CosmeticsPanel Instance { get; private set; }
        
        // Events
        public event Action<CosmeticItem> OnItemEquipped;
        public event Action<CosmeticItem> OnItemPurchased;
        public event Action<CosmeticItem> OnItemPreviewed;

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
            GenerateSampleCosmetics();
            ShowCategory(CosmeticCategory.Skins);
            Hide();
        }

        private void CreateUI()
        {
            // Main panel
            _panel = new GameObject("CosmeticsPanel");
            _panel.transform.SetParent(transform);
            
            RectTransform panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.08f);
            panelRect.anchorMax = new Vector2(0.9f, 0.92f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            Image panelBg = _panel.AddComponent<Image>();
            panelBg.color = panelColor;
            
            UnityEngine.UI.Outline outline = _panel.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(2, 2);
            
            CreateHeader();
            CreateCategoryTabs();
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
            title.text = "[*] COSMETICS";
            title.fontSize = 26;
            title.fontStyle = FontStyles.Bold;
            title.color = accentColor;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Currency display
            CreateCurrencyDisplay(header.transform);
            
            // Close button
            CreateCloseButton(header.transform);
        }

        private void CreateCurrencyDisplay(Transform parent)
        {
            GameObject currencyObj = new GameObject("Currency");
            currencyObj.transform.SetParent(parent, false);
            
            RectTransform currencyRect = currencyObj.AddComponent<RectTransform>();
            currencyRect.anchorMin = new Vector2(0.5f, 0.2f);
            currencyRect.anchorMax = new Vector2(0.75f, 0.8f);
            currencyRect.offsetMin = Vector2.zero;
            currencyRect.offsetMax = Vector2.zero;
            
            Image bg = currencyObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f);
            
            HorizontalLayoutGroup hlayout = currencyObj.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            // Gem icon
            GameObject gemIcon = new GameObject("GemIcon");
            gemIcon.transform.SetParent(currencyObj.transform, false);
            LayoutElement gemLE = gemIcon.AddComponent<LayoutElement>();
            gemLE.preferredWidth = 25;
            TextMeshProUGUI gem = gemIcon.AddComponent<TextMeshProUGUI>();
            gem.text = "[G]";
            gem.fontSize = 20;
            gem.alignment = TextAlignmentOptions.Center;
            
            // Amount
            GameObject amountObj = new GameObject("Amount");
            amountObj.transform.SetParent(currencyObj.transform, false);
            TextMeshProUGUI amount = amountObj.AddComponent<TextMeshProUGUI>();
            amount.text = _playerGems.ToString("N0");
            amount.fontSize = 18;
            amount.fontStyle = FontStyles.Bold;
            amount.color = accentColor;
            amount.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Buy more button
            GameObject buyObj = new GameObject("BuyMore");
            buyObj.transform.SetParent(currencyObj.transform, false);
            LayoutElement buyLE = buyObj.AddComponent<LayoutElement>();
            buyLE.preferredWidth = 30;
            Image buyBg = buyObj.AddComponent<Image>();
            buyBg.color = new Color(0.3f, 0.6f, 0.3f);
            Button buyBtn = buyObj.AddComponent<Button>();
            buyBtn.onClick.AddListener(OnBuyGemsClicked);
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buyObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI buyText = textObj.AddComponent<TextMeshProUGUI>();
            buyText.text = "+";
            buyText.fontSize = 18;
            buyText.fontStyle = FontStyles.Bold;
            buyText.color = Color.white;
            buyText.alignment = TextAlignmentOptions.Center;
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
            x.text = "[X]";
            x.fontSize = 22;
            x.color = Color.white;
            x.alignment = TextAlignmentOptions.Center;
        }

        private void CreateCategoryTabs()
        {
            GameObject tabBar = new GameObject("CategoryTabs");
            tabBar.transform.SetParent(_panel.transform, false);
            
            RectTransform tabRect = tabBar.AddComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0, 1);
            tabRect.anchorMax = new Vector2(1, 1);
            tabRect.pivot = new Vector2(0.5f, 1);
            tabRect.anchoredPosition = new Vector2(0, -60);
            tabRect.sizeDelta = new Vector2(0, 50);
            
            Image tabBg = tabBar.AddComponent<Image>();
            tabBg.color = new Color(0.08f, 0.08f, 0.1f);
            
            HorizontalLayoutGroup hlayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 8;
            hlayout.padding = new RectOffset(15, 15, 8, 8);
            hlayout.childForceExpandWidth = true;
            
            _skinsTab = CreateCategoryButton(tabBar.transform, "[U] Skins", () => ShowCategory(CosmeticCategory.Skins));
            _themesTab = CreateCategoryButton(tabBar.transform, "[C] Themes", () => ShowCategory(CosmeticCategory.Themes));
            _effectsTab = CreateCategoryButton(tabBar.transform, "[*] Effects", () => ShowCategory(CosmeticCategory.Effects));
            _emotesTab = CreateCategoryButton(tabBar.transform, "🎭 Emotes", () => ShowCategory(CosmeticCategory.Emotes));
            _titlesTab = CreateCategoryButton(tabBar.transform, "[M] Titles", () => ShowCategory(CosmeticCategory.Titles));
            
            UpdateCategoryHighlights();
        }

        private Button CreateCategoryButton(Transform parent, string label, Action onClick)
        {
            GameObject btnObj = new GameObject(label);
            btnObj.transform.SetParent(parent, false);
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.16f);
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
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
            contentRect.offsetMax = new Vector2(-10, -120);
            
            // Left: Item grid
            CreateItemGrid(content.transform);
            
            // Right: Preview panel
            CreatePreviewPanel(content.transform);
        }

        private void CreateItemGrid(Transform parent)
        {
            GameObject gridPanel = new GameObject("ItemGrid");
            gridPanel.transform.SetParent(parent, false);
            
            RectTransform gridRect = gridPanel.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0, 0);
            gridRect.anchorMax = new Vector2(0.6f, 1);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = new Vector2(-5, 0);
            
            Image gridBg = gridPanel.AddComponent<Image>();
            gridBg.color = new Color(0.07f, 0.07f, 0.1f);
            
            // Scroll view
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(gridPanel.transform, false);
            
            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(10, 10);
            scrollRect.offsetMax = new Vector2(-10, -10);
            
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
            
            // Content with grid layout
            _itemsContainer = new GameObject("Content");
            _itemsContainer.transform.SetParent(viewport.transform, false);
            RectTransform containerRect = _itemsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = Vector2.zero;
            
            GridLayoutGroup glayout = _itemsContainer.AddComponent<GridLayoutGroup>();
            glayout.cellSize = new Vector2(110, 130);
            glayout.spacing = new Vector2(10, 10);
            glayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            glayout.childAlignment = TextAnchor.UpperLeft;
            glayout.constraint = GridLayoutGroup.Constraint.Flexible;
            glayout.padding = new RectOffset(5, 5, 5, 5);
            
            ContentSizeFitter fitter = _itemsContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = containerRect;
        }

        private void CreatePreviewPanel(Transform parent)
        {
            _previewPanel = new GameObject("PreviewPanel");
            _previewPanel.transform.SetParent(parent, false);
            
            RectTransform previewRect = _previewPanel.AddComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.6f, 0);
            previewRect.anchorMax = new Vector2(1, 1);
            previewRect.offsetMin = new Vector2(5, 0);
            previewRect.offsetMax = Vector2.zero;
            
            Image previewBg = _previewPanel.AddComponent<Image>();
            previewBg.color = new Color(0.08f, 0.08f, 0.12f);
            
            VerticalLayoutGroup vlayout = _previewPanel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Preview area (placeholder for 3D model)
            _previewArea = new GameObject("PreviewArea");
            _previewArea.transform.SetParent(_previewPanel.transform, false);
            LayoutElement previewLE = _previewArea.AddComponent<LayoutElement>();
            previewLE.preferredHeight = 200;
            previewLE.flexibleHeight = 1;
            
            Image previewAreaBg = _previewArea.AddComponent<Image>();
            previewAreaBg.color = new Color(0.05f, 0.05f, 0.08f);
            
            // Placeholder text
            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(_previewArea.transform, false);
            RectTransform phRect = phObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            TextMeshProUGUI ph = phObj.AddComponent<TextMeshProUGUI>();
            ph.text = "[U]\n\nSelect an item to preview";
            ph.fontSize = 16;
            ph.color = new Color(0.4f, 0.4f, 0.4f);
            ph.alignment = TextAlignmentOptions.Center;
            
            // Item name
            GameObject nameObj = new GameObject("ItemName");
            nameObj.transform.SetParent(_previewPanel.transform, false);
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 30;
            _itemName = nameObj.AddComponent<TextMeshProUGUI>();
            _itemName.text = "Select an item";
            _itemName.fontSize = 20;
            _itemName.fontStyle = FontStyles.Bold;
            _itemName.color = Color.white;
            _itemName.alignment = TextAlignmentOptions.Center;
            
            // Rarity
            GameObject rarityObj = new GameObject("Rarity");
            rarityObj.transform.SetParent(_previewPanel.transform, false);
            LayoutElement rarityLE = rarityObj.AddComponent<LayoutElement>();
            rarityLE.preferredHeight = 20;
            _itemRarity = rarityObj.AddComponent<TextMeshProUGUI>();
            _itemRarity.text = "";
            _itemRarity.fontSize = 14;
            _itemRarity.fontStyle = FontStyles.Bold;
            _itemRarity.alignment = TextAlignmentOptions.Center;
            
            // Description
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(_previewPanel.transform, false);
            LayoutElement descLE = descObj.AddComponent<LayoutElement>();
            descLE.preferredHeight = 60;
            _itemDescription = descObj.AddComponent<TextMeshProUGUI>();
            _itemDescription.text = "";
            _itemDescription.fontSize = 13;
            _itemDescription.color = new Color(0.7f, 0.7f, 0.7f);
            _itemDescription.alignment = TextAlignmentOptions.Center;
            
            // Price
            GameObject priceObj = new GameObject("Price");
            priceObj.transform.SetParent(_previewPanel.transform, false);
            LayoutElement priceLE = priceObj.AddComponent<LayoutElement>();
            priceLE.preferredHeight = 30;
            _itemPrice = priceObj.AddComponent<TextMeshProUGUI>();
            _itemPrice.text = "";
            _itemPrice.fontSize = 18;
            _itemPrice.fontStyle = FontStyles.Bold;
            _itemPrice.color = accentColor;
            _itemPrice.alignment = TextAlignmentOptions.Center;
            
            // Action buttons
            CreatePreviewActions(_previewPanel.transform);
        }

        private void CreatePreviewActions(Transform parent)
        {
            GameObject actionsRow = new GameObject("Actions");
            actionsRow.transform.SetParent(parent, false);
            
            LayoutElement le = actionsRow.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            HorizontalLayoutGroup hlayout = actionsRow.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.childForceExpandWidth = true;
            
            _equipButton = CreateActionButton(actionsRow.transform, "[OK] Equip", OnEquipClicked, ownedColor);
            _purchaseButton = CreateActionButton(actionsRow.transform, "[G] Purchase", OnPurchaseClicked, accentColor);
            
            _equipButton.interactable = false;
            _purchaseButton.interactable = false;
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
            text.fontSize = 15;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            return btn;
        }

        #region Data & Logic

        private void GenerateSampleCosmetics()
        {
            // Character Skins
            _allItems.AddRange(new[]
            {
                new CosmeticItem { Id = "skin_knight", Name = "Royal Knight", Category = CosmeticCategory.Skins, Rarity = CosmeticRarity.Common, Price = 100, Icon = "[D]", Description = "A classic knight armor set.", IsOwned = true },
                new CosmeticItem { Id = "skin_mage", Name = "Arcane Mage", Category = CosmeticCategory.Skins, Rarity = CosmeticRarity.Rare, Price = 500, Icon = "🧙", Description = "Mystical robes imbued with ancient magic.", IsOwned = true },
                new CosmeticItem { Id = "skin_samurai", Name = "Shadow Samurai", Category = CosmeticCategory.Skins, Rarity = CosmeticRarity.Epic, Price = 1200, Icon = "[!]", Description = "Legendary armor of the Eastern warriors." },
                new CosmeticItem { Id = "skin_dragon", Name = "Dragon Lord", Category = CosmeticCategory.Skins, Rarity = CosmeticRarity.Legendary, Price = 2500, Icon = "🐉", Description = "Forged from dragon scales, this armor commands respect." },
                new CosmeticItem { Id = "skin_cyber", Name = "Cyber Warrior", Category = CosmeticCategory.Skins, Rarity = CosmeticRarity.Epic, Price = 1500, Icon = "[B]", Description = "Futuristic combat suit with advanced tech." },
                new CosmeticItem { Id = "skin_pirate", Name = "Pirate Captain", Category = CosmeticCategory.Skins, Rarity = CosmeticRarity.Rare, Price = 600, Icon = "[F][X]", Description = "Arr! Set sail for adventure!" },
            });
            
            // Building Themes
            _allItems.AddRange(new[]
            {
                new CosmeticItem { Id = "theme_medieval", Name = "Medieval Stone", Category = CosmeticCategory.Themes, Rarity = CosmeticRarity.Common, Price = 200, Icon = "[C]", Description = "Classic medieval architecture.", IsOwned = true },
                new CosmeticItem { Id = "theme_oriental", Name = "Eastern Palace", Category = CosmeticCategory.Themes, Rarity = CosmeticRarity.Rare, Price = 800, Icon = "🏯", Description = "Elegant Eastern-style buildings." },
                new CosmeticItem { Id = "theme_fantasy", Name = "Enchanted Forest", Category = CosmeticCategory.Themes, Rarity = CosmeticRarity.Epic, Price = 1500, Icon = "[T]", Description = "Buildings made from living trees and magic." },
                new CosmeticItem { Id = "theme_crystal", Name = "Crystal Kingdom", Category = CosmeticCategory.Themes, Rarity = CosmeticRarity.Legendary, Price = 3000, Icon = "[G]", Description = "Structures made of pure crystal that glow at night." },
                new CosmeticItem { Id = "theme_volcanic", Name = "Volcanic Fortress", Category = CosmeticCategory.Themes, Rarity = CosmeticRarity.Epic, Price = 1800, Icon = "🌋", Description = "Dark obsidian structures with flowing lava." },
            });
            
            // Visual Effects
            _allItems.AddRange(new[]
            {
                new CosmeticItem { Id = "fx_sparkle", Name = "Sparkle Trail", Category = CosmeticCategory.Effects, Rarity = CosmeticRarity.Common, Price = 150, Icon = "[*]", Description = "Leaves a trail of sparkles as you move.", IsOwned = true },
                new CosmeticItem { Id = "fx_fire", Name = "Fire Aura", Category = CosmeticCategory.Effects, Rarity = CosmeticRarity.Rare, Price = 400, Icon = "[*]", Description = "Surrounded by dancing flames." },
                new CosmeticItem { Id = "fx_lightning", Name = "Lightning Storm", Category = CosmeticCategory.Effects, Rarity = CosmeticRarity.Epic, Price = 1000, Icon = "[!]", Description = "Crackling electricity surrounds you." },
                new CosmeticItem { Id = "fx_void", Name = "Void Walker", Category = CosmeticCategory.Effects, Rarity = CosmeticRarity.Legendary, Price = 2000, Icon = "🌀", Description = "Dark void particles swirl around you." },
                new CosmeticItem { Id = "fx_rainbow", Name = "Rainbow Glow", Category = CosmeticCategory.Effects, Rarity = CosmeticRarity.Rare, Price = 500, Icon = "[R]", Description = "A colorful aura that shifts through the spectrum." },
            });
            
            // Emotes
            _allItems.AddRange(new[]
            {
                new CosmeticItem { Id = "emote_wave", Name = "Friendly Wave", Category = CosmeticCategory.Emotes, Rarity = CosmeticRarity.Common, Price = 50, Icon = "👋", Description = "Wave hello to friends and foes.", IsOwned = true },
                new CosmeticItem { Id = "emote_dance", Name = "Victory Dance", Category = CosmeticCategory.Emotes, Rarity = CosmeticRarity.Rare, Price = 300, Icon = "💃", Description = "Celebrate your victories in style!" },
                new CosmeticItem { Id = "emote_bow", Name = "Honorable Bow", Category = CosmeticCategory.Emotes, Rarity = CosmeticRarity.Common, Price = 75, Icon = "🙇", Description = "Show respect to your opponents.", IsOwned = true },
                new CosmeticItem { Id = "emote_flex", Name = "Power Flex", Category = CosmeticCategory.Emotes, Rarity = CosmeticRarity.Rare, Price = 350, Icon = "[+]", Description = "Show off your strength!" },
                new CosmeticItem { Id = "emote_throne", Name = "Summon Throne", Category = CosmeticCategory.Emotes, Rarity = CosmeticRarity.Legendary, Price = 1500, Icon = "🪑", Description = "Summon a majestic throne to sit upon." },
            });
            
            // Titles & Badges
            _allItems.AddRange(new[]
            {
                new CosmeticItem { Id = "title_warrior", Name = "The Warrior", Category = CosmeticCategory.Titles, Rarity = CosmeticRarity.Common, Price = 100, Icon = "[!]", Description = "A title for those who fight bravely.", IsOwned = true },
                new CosmeticItem { Id = "title_conqueror", Name = "The Conqueror", Category = CosmeticCategory.Titles, Rarity = CosmeticRarity.Rare, Price = 500, Icon = "[K]", Description = "For those who have conquered many territories." },
                new CosmeticItem { Id = "title_legend", Name = "The Legend", Category = CosmeticCategory.Titles, Rarity = CosmeticRarity.Legendary, Price = 2500, Icon = "[*]", Description = "A title reserved for the greatest players." },
                new CosmeticItem { Id = "title_defender", Name = "The Defender", Category = CosmeticCategory.Titles, Rarity = CosmeticRarity.Rare, Price = 400, Icon = "[D]", Description = "Successfully defended against 100 attacks." },
                new CosmeticItem { Id = "title_wealthy", Name = "The Wealthy", Category = CosmeticCategory.Titles, Rarity = CosmeticRarity.Epic, Price = 1000, Icon = "[$]", Description = "Accumulated great riches." },
            });
            
            // Set default equipped items
            _equippedItems[CosmeticCategory.Skins] = "skin_knight";
            _equippedItems[CosmeticCategory.Themes] = "theme_medieval";
            _equippedItems[CosmeticCategory.Effects] = "fx_sparkle";
            _equippedItems[CosmeticCategory.Emotes] = "emote_wave";
            _equippedItems[CosmeticCategory.Titles] = "title_warrior";
        }

        private void ShowCategory(CosmeticCategory category)
        {
            _currentCategory = category;
            UpdateCategoryHighlights();
            RefreshItemGrid();
            ClearPreview();
        }

        private void UpdateCategoryHighlights()
        {
            SetCategoryHighlight(_skinsTab, _currentCategory == CosmeticCategory.Skins);
            SetCategoryHighlight(_themesTab, _currentCategory == CosmeticCategory.Themes);
            SetCategoryHighlight(_effectsTab, _currentCategory == CosmeticCategory.Effects);
            SetCategoryHighlight(_emotesTab, _currentCategory == CosmeticCategory.Emotes);
            SetCategoryHighlight(_titlesTab, _currentCategory == CosmeticCategory.Titles);
        }

        private void SetCategoryHighlight(Button btn, bool active)
        {
            if (btn == null) return;
            var image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? accentColor : new Color(0.12f, 0.12f, 0.16f);
            }
        }

        private void RefreshItemGrid()
        {
            foreach (Transform child in _itemsContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            _filteredItems = _allItems.Where(i => i.Category == _currentCategory).ToList();
            
            foreach (var item in _filteredItems)
            {
                CreateItemCard(item);
            }
        }

        private void CreateItemCard(CosmeticItem item)
        {
            GameObject card = new GameObject($"Item_{item.Id}");
            card.transform.SetParent(_itemsContainer.transform, false);
            
            Image bg = card.AddComponent<Image>();
            bg.color = item.IsOwned ? new Color(0.12f, 0.15f, 0.12f) : new Color(0.1f, 0.1f, 0.14f);
            
            Button btn = card.AddComponent<Button>();
            var item_copy = item;
            btn.onClick.AddListener(() => SelectItem(item_copy));
            
            // Rarity border
            UnityEngine.UI.Outline rarityBorder = card.AddComponent<UnityEngine.UI.Outline>();
            rarityBorder.effectColor = GetRarityColor(item.Rarity);
            rarityBorder.effectDistance = new Vector2(2, 2);
            
            VerticalLayoutGroup vlayout = card.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 3;
            vlayout.padding = new RectOffset(5, 5, 8, 5);
            
            // Equipped indicator
            bool isEquipped = _equippedItems.TryGetValue(item.Category, out string equipped) && equipped == item.Id;
            if (isEquipped)
            {
                GameObject equippedObj = new GameObject("Equipped");
                equippedObj.transform.SetParent(card.transform, false);
                LayoutElement eqLE = equippedObj.AddComponent<LayoutElement>();
                eqLE.preferredHeight = 16;
                TextMeshProUGUI eqText = equippedObj.AddComponent<TextMeshProUGUI>();
                eqText.text = "[OK] EQUIPPED";
                eqText.fontSize = 9;
                eqText.fontStyle = FontStyles.Bold;
                eqText.color = ownedColor;
                eqText.alignment = TextAlignmentOptions.Center;
            }
            
            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(card.transform, false);
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredHeight = 45;
            TextMeshProUGUI icon = iconObj.AddComponent<TextMeshProUGUI>();
            icon.text = item.Icon;
            icon.fontSize = 36;
            icon.alignment = TextAlignmentOptions.Center;
            
            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(card.transform, false);
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 30;
            TextMeshProUGUI name = nameObj.AddComponent<TextMeshProUGUI>();
            name.text = item.Name;
            name.fontSize = 11;
            name.fontStyle = FontStyles.Bold;
            name.color = GetRarityColor(item.Rarity);
            name.alignment = TextAlignmentOptions.Center;
            
            // Status/Price
            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(card.transform, false);
            LayoutElement statusLE = statusObj.AddComponent<LayoutElement>();
            statusLE.preferredHeight = 18;
            TextMeshProUGUI status = statusObj.AddComponent<TextMeshProUGUI>();
            
            if (item.IsOwned)
            {
                status.text = "[OK] Owned";
                status.color = ownedColor;
            }
            else
            {
                status.text = $"[G] {item.Price}";
                status.color = accentColor;
            }
            status.fontSize = 10;
            status.alignment = TextAlignmentOptions.Center;
        }

        private void SelectItem(CosmeticItem item)
        {
            _selectedItem = item;
            
            _itemName.text = $"{item.Icon} {item.Name}";
            _itemRarity.text = item.Rarity.ToString().ToUpper();
            _itemRarity.color = GetRarityColor(item.Rarity);
            _itemDescription.text = item.Description;
            
            bool isEquipped = _equippedItems.TryGetValue(item.Category, out string equipped) && equipped == item.Id;
            
            if (item.IsOwned)
            {
                _itemPrice.text = isEquipped ? "Currently Equipped" : "Owned";
                _itemPrice.color = ownedColor;
                _equipButton.interactable = !isEquipped;
                _purchaseButton.interactable = false;
            }
            else
            {
                _itemPrice.text = $"[G] {item.Price} Gems";
                _itemPrice.color = _playerGems >= item.Price ? accentColor : new Color(0.7f, 0.3f, 0.3f);
                _equipButton.interactable = false;
                _purchaseButton.interactable = _playerGems >= item.Price;
            }
            
            OnItemPreviewed?.Invoke(item);
        }

        private void ClearPreview()
        {
            _selectedItem = null;
            _itemName.text = "Select an item";
            _itemRarity.text = "";
            _itemDescription.text = "";
            _itemPrice.text = "";
            _equipButton.interactable = false;
            _purchaseButton.interactable = false;
        }

        private void OnEquipClicked()
        {
            if (_selectedItem == null || !_selectedItem.IsOwned) return;
            
            _equippedItems[_selectedItem.Category] = _selectedItem.Id;
            NotificationSystem.Instance?.ShowSuccess($"Equipped: {_selectedItem.Name}");
            OnItemEquipped?.Invoke(_selectedItem);
            
            RefreshItemGrid();
            SelectItem(_selectedItem); // Refresh preview
        }

        private void OnPurchaseClicked()
        {
            if (_selectedItem == null || _selectedItem.IsOwned) return;
            if (_playerGems < _selectedItem.Price)
            {
                NotificationSystem.Instance?.ShowError("Not enough gems!");
                return;
            }
            
            _playerGems -= _selectedItem.Price;
            _selectedItem.IsOwned = true;
            
            NotificationSystem.Instance?.ShowSuccess($"Purchased: {_selectedItem.Name}");
            OnItemPurchased?.Invoke(_selectedItem);
            
            RefreshItemGrid();
            SelectItem(_selectedItem);
            UpdateCurrencyDisplay();
        }

        private void OnBuyGemsClicked()
        {
            NotificationSystem.Instance?.ShowInfo("Store coming soon!");
            // Would open gem purchase dialog
        }

        private void UpdateCurrencyDisplay()
        {
            // Update the currency text
            var amountText = _panel.GetComponentsInChildren<TextMeshProUGUI>()
                .FirstOrDefault(t => t.gameObject.name == "Amount");
            if (amountText != null)
            {
                amountText.text = _playerGems.ToString("N0");
            }
        }

        #endregion

        #region Helpers

        private Color GetRarityColor(CosmeticRarity rarity)
        {
            return rarity switch
            {
                CosmeticRarity.Common => commonColor,
                CosmeticRarity.Rare => rareColor,
                CosmeticRarity.Epic => epicColor,
                CosmeticRarity.Legendary => legendaryColor,
                _ => Color.white
            };
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel?.SetActive(true);
            ShowCategory(CosmeticCategory.Skins);
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

        public string GetEquippedItem(CosmeticCategory category)
        {
            return _equippedItems.TryGetValue(category, out string id) ? id : null;
        }

        public CosmeticItem GetItemById(string itemId)
        {
            return _allItems.FirstOrDefault(i => i.Id == itemId);
        }

        #endregion
    }

    #region Data Classes

    public enum CosmeticCategory
    {
        Skins,
        Themes,
        Effects,
        Emotes,
        Titles
    }

    public enum CosmeticRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public class CosmeticItem
    {
        public string Id;
        public string Name;
        public CosmeticCategory Category;
        public CosmeticRarity Rarity;
        public int Price;
        public string Icon;
        public string Description;
        public bool IsOwned;
        public string ModelPath;
        public string EffectPath;
    }

    #endregion
}
