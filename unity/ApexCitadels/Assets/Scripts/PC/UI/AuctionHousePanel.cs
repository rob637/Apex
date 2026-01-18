using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Auction House Panel - Player-to-player trading marketplace.
    /// Buy and sell items, resources, equipment, and more.
    /// 
    /// Features:
    /// - Browse listings by category
    /// - Search and filter
    /// - Create sell orders
    /// - Create buy orders
    /// - Price history charts
    /// - Watchlist
    /// - Transaction history
    /// - Market statistics
    /// </summary>
    public class AuctionHousePanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.9f, 0.7f, 0.2f); // Gold
        [SerializeField] private Color panelColor = new Color(0.12f, 0.12f, 0.18f, 0.95f);
        [SerializeField] private Color buyColor = new Color(0.3f, 0.7f, 0.4f);
        [SerializeField] private Color sellColor = new Color(0.8f, 0.4f, 0.3f);
        [SerializeField] private Color highlightColor = new Color(0.3f, 0.35f, 0.5f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _listingsContainer;
        private TMP_InputField _searchInput;
        private TMP_Dropdown _categoryDropdown;
        private TMP_Dropdown _sortDropdown;
        private TextMeshProUGUI _balanceText;
        private TextMeshProUGUI _selectedItemName;
        private TextMeshProUGUI _selectedItemPrice;
        private TextMeshProUGUI _selectedItemDetails;
        private Button _buyButton;
        private Button _sellButton;
        private Button _watchlistButton;
        
        // Tabs
        private Button _browseTab;
        private Button _sellTab;
        private Button _ordersTab;
        private Button _historyTab;
        private Button _watchlistTab;
        private AuctionTab _currentTab = AuctionTab.Browse;
        
        // State
        private List<AuctionListing> _listings = new List<AuctionListing>();
        private List<AuctionListing> _filteredListings = new List<AuctionListing>();
        private List<AuctionListing> _myListings = new List<AuctionListing>();
        private List<AuctionListing> _watchlist = new List<AuctionListing>();
        private List<AuctionTransaction> _transactionHistory = new List<AuctionTransaction>();
        private AuctionListing _selectedListing;
        private AuctionCategory _selectedCategory = AuctionCategory.All;
        private string _searchQuery = "";
        private AuctionSortMode _sortMode = AuctionSortMode.PriceLowToHigh;
        
        // Player balance
        private int _playerGold = 10000;
        
        public static AuctionHousePanel Instance { get; private set; }
        
        // Events
        public event Action<AuctionListing> OnItemPurchased;
        public event Action<AuctionListing> OnItemListed;
        public event Action<AuctionListing> OnListingCancelled;
        public event Action<AuctionListing> OnWatchlistAdded;

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
            GenerateSampleListings();
            RefreshListings();
            Hide();
        }

        private void CreateUI()
        {
            // Main panel
            _panel = new GameObject("AuctionHousePanel");
            _panel.transform.SetParent(transform);
            
            RectTransform panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.9f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            Image panelBg = _panel.AddComponent<Image>();
            panelBg.color = panelColor;
            
            // Add outline
            UnityEngine.UI.Outline outline = _panel.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(2, 2);
            
            // Header
            CreateHeader();
            
            // Tab bar
            CreateTabBar();
            
            // Main content area with left (listings) and right (details) panels
            CreateMainContent();
            
            // Footer with action buttons
            CreateFooter();
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
            headerBg.color = new Color(0.15f, 0.15f, 0.22f);
            
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(0.3f, 1);
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "🏛️ AUCTION HOUSE";
            title.fontSize = 24;
            title.fontStyle = FontStyles.Bold;
            title.color = accentColor;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Balance display
            GameObject balanceObj = new GameObject("Balance");
            balanceObj.transform.SetParent(header.transform, false);
            
            RectTransform balanceRect = balanceObj.AddComponent<RectTransform>();
            balanceRect.anchorMin = new Vector2(0.5f, 0);
            balanceRect.anchorMax = new Vector2(0.7f, 1);
            balanceRect.offsetMin = Vector2.zero;
            balanceRect.offsetMax = Vector2.zero;
            
            _balanceText = balanceObj.AddComponent<TextMeshProUGUI>();
            _balanceText.text = $"💰 {_playerGold:N0} Gold";
            _balanceText.fontSize = 18;
            _balanceText.color = accentColor;
            _balanceText.alignment = TextAlignmentOptions.Center;
            
            // Search bar
            CreateSearchBar(header.transform);
            
            // Close button
            CreateCloseButton(header.transform);
        }

        private void CreateSearchBar(Transform parent)
        {
            GameObject searchContainer = new GameObject("SearchBar");
            searchContainer.transform.SetParent(parent, false);
            
            RectTransform searchRect = searchContainer.AddComponent<RectTransform>();
            searchRect.anchorMin = new Vector2(0.7f, 0.15f);
            searchRect.anchorMax = new Vector2(0.92f, 0.85f);
            searchRect.offsetMin = Vector2.zero;
            searchRect.offsetMax = Vector2.zero;
            
            Image searchBg = searchContainer.AddComponent<Image>();
            searchBg.color = new Color(0.08f, 0.08f, 0.12f);
            
            GameObject inputObj = new GameObject("SearchInput");
            inputObj.transform.SetParent(searchContainer.transform, false);
            
            RectTransform inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = new Vector2(10, 2);
            inputRect.offsetMax = new Vector2(-10, -2);
            
            _searchInput = inputObj.AddComponent<TMP_InputField>();
            
            // Text area
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = Vector2.zero;
            textAreaRect.offsetMax = Vector2.zero;
            
            // Input text
            GameObject inputTextObj = new GameObject("Text");
            inputTextObj.transform.SetParent(textArea.transform, false);
            RectTransform inputTextRect = inputTextObj.AddComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI inputText = inputTextObj.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 14;
            inputText.color = Color.white;
            
            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = "🔍 Search items...";
            placeholder.fontSize = 14;
            placeholder.color = new Color(0.5f, 0.5f, 0.5f);
            placeholder.fontStyle = FontStyles.Italic;
            
            _searchInput.textViewport = textAreaRect;
            _searchInput.textComponent = inputText;
            _searchInput.placeholder = placeholder;
            _searchInput.onValueChanged.AddListener(OnSearchChanged);
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(parent, false);
            
            RectTransform closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 0.5f);
            closeRect.anchorMax = new Vector2(1, 0.5f);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.anchoredPosition = new Vector2(-10, 0);
            closeRect.sizeDelta = new Vector2(40, 40);
            
            Image closeBg = closeObj.AddComponent<Image>();
            closeBg.color = new Color(0.6f, 0.2f, 0.2f);
            
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
            x.fontSize = 24;
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
            tabBg.color = new Color(0.1f, 0.1f, 0.15f);
            
            HorizontalLayoutGroup hlayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 5;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            hlayout.childForceExpandWidth = true;
            hlayout.childForceExpandHeight = true;
            
            _browseTab = CreateTabButton(tabBar.transform, "📋 Browse", () => SwitchTab(AuctionTab.Browse));
            _sellTab = CreateTabButton(tabBar.transform, "💰 Sell", () => SwitchTab(AuctionTab.Sell));
            _ordersTab = CreateTabButton(tabBar.transform, "📝 My Orders", () => SwitchTab(AuctionTab.MyOrders));
            _historyTab = CreateTabButton(tabBar.transform, "📜 History", () => SwitchTab(AuctionTab.History));
            _watchlistTab = CreateTabButton(tabBar.transform, "⭐ Watchlist", () => SwitchTab(AuctionTab.Watchlist));
            
            UpdateTabHighlights();
        }

        private Button CreateTabButton(Transform parent, string label, Action onClick)
        {
            GameObject tabObj = new GameObject(label);
            tabObj.transform.SetParent(parent, false);
            
            Image bg = tabObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f);
            
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
            GameObject content = new GameObject("Content");
            content.transform.SetParent(_panel.transform, false);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(10, 60);
            contentRect.offsetMax = new Vector2(-10, -115);
            
            // Left side - listings
            CreateListingsPanel(content.transform);
            
            // Right side - details
            CreateDetailsPanel(content.transform);
        }

        private void CreateListingsPanel(Transform parent)
        {
            GameObject listingsPanel = new GameObject("ListingsPanel");
            listingsPanel.transform.SetParent(parent, false);
            
            RectTransform listRect = listingsPanel.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(0.65f, 1);
            listRect.offsetMin = Vector2.zero;
            listRect.offsetMax = new Vector2(-5, 0);
            
            Image listBg = listingsPanel.AddComponent<Image>();
            listBg.color = new Color(0.08f, 0.08f, 0.12f);
            
            // Filter bar
            CreateFilterBar(listingsPanel.transform);
            
            // Scroll view for listings
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(listingsPanel.transform, false);
            
            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(5, 5);
            scrollRect.offsetMax = new Vector2(-5, -45);
            
            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
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
            _listingsContainer = new GameObject("Content");
            _listingsContainer.transform.SetParent(viewport.transform, false);
            
            RectTransform containerRect = _listingsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = Vector2.zero;
            
            VerticalLayoutGroup vlayout = _listingsContainer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(5, 5, 5, 5);
            
            ContentSizeFitter fitter = _listingsContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = containerRect;
        }

        private void CreateFilterBar(Transform parent)
        {
            GameObject filterBar = new GameObject("FilterBar");
            filterBar.transform.SetParent(parent, false);
            
            RectTransform filterRect = filterBar.AddComponent<RectTransform>();
            filterRect.anchorMin = new Vector2(0, 1);
            filterRect.anchorMax = new Vector2(1, 1);
            filterRect.pivot = new Vector2(0.5f, 1);
            filterRect.anchoredPosition = Vector2.zero;
            filterRect.sizeDelta = new Vector2(0, 40);
            
            HorizontalLayoutGroup hlayout = filterBar.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            hlayout.childForceExpandWidth = false;
            hlayout.childForceExpandHeight = true;
            
            // Category dropdown
            CreateCategoryDropdown(filterBar.transform);
            
            // Sort dropdown
            CreateSortDropdown(filterBar.transform);
        }

        private void CreateCategoryDropdown(Transform parent)
        {
            GameObject dropObj = new GameObject("CategoryDropdown");
            dropObj.transform.SetParent(parent, false);
            
            LayoutElement le = dropObj.AddComponent<LayoutElement>();
            le.preferredWidth = 150;
            
            Image bg = dropObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f);
            
            _categoryDropdown = dropObj.AddComponent<TMP_Dropdown>();
            
            // Create template
            CreateDropdownTemplate(_categoryDropdown, dropObj);
            
            // Add options
            _categoryDropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>();
            foreach (AuctionCategory cat in Enum.GetValues(typeof(AuctionCategory)))
            {
                options.Add(new TMP_Dropdown.OptionData(GetCategoryName(cat)));
            }
            _categoryDropdown.AddOptions(options);
            
            _categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
        }

        private void CreateSortDropdown(Transform parent)
        {
            GameObject dropObj = new GameObject("SortDropdown");
            dropObj.transform.SetParent(parent, false);
            
            LayoutElement le = dropObj.AddComponent<LayoutElement>();
            le.preferredWidth = 150;
            
            Image bg = dropObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f);
            
            _sortDropdown = dropObj.AddComponent<TMP_Dropdown>();
            
            CreateDropdownTemplate(_sortDropdown, dropObj);
            
            _sortDropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("Price: Low to High"),
                new TMP_Dropdown.OptionData("Price: High to Low"),
                new TMP_Dropdown.OptionData("Time: Ending Soon"),
                new TMP_Dropdown.OptionData("Time: Recently Listed"),
                new TMP_Dropdown.OptionData("Name: A-Z")
            };
            _sortDropdown.AddOptions(options);
            
            _sortDropdown.onValueChanged.AddListener(OnSortChanged);
        }

        private void CreateDropdownTemplate(TMP_Dropdown dropdown, GameObject dropObj)
        {
            // Caption
            GameObject captionObj = new GameObject("Label");
            captionObj.transform.SetParent(dropObj.transform, false);
            RectTransform capRect = captionObj.AddComponent<RectTransform>();
            capRect.anchorMin = Vector2.zero;
            capRect.anchorMax = Vector2.one;
            capRect.offsetMin = new Vector2(10, 0);
            capRect.offsetMax = new Vector2(-25, 0);
            
            TextMeshProUGUI caption = captionObj.AddComponent<TextMeshProUGUI>();
            caption.fontSize = 12;
            caption.color = Color.white;
            caption.alignment = TextAlignmentOptions.MidlineLeft;
            
            dropdown.captionText = caption;
            
            // Arrow
            GameObject arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(dropObj.transform, false);
            RectTransform arrRect = arrowObj.AddComponent<RectTransform>();
            arrRect.anchorMin = new Vector2(1, 0.5f);
            arrRect.anchorMax = new Vector2(1, 0.5f);
            arrRect.pivot = new Vector2(1, 0.5f);
            arrRect.anchoredPosition = new Vector2(-5, 0);
            arrRect.sizeDelta = new Vector2(20, 20);
            
            TextMeshProUGUI arrow = arrowObj.AddComponent<TextMeshProUGUI>();
            arrow.text = "▼";
            arrow.fontSize = 10;
            arrow.color = Color.white;
            arrow.alignment = TextAlignmentOptions.Center;
            
            // Template (minimal)
            GameObject template = new GameObject("Template");
            template.transform.SetParent(dropObj.transform, false);
            template.SetActive(false);
            
            RectTransform tempRect = template.AddComponent<RectTransform>();
            tempRect.anchorMin = new Vector2(0, 0);
            tempRect.anchorMax = new Vector2(1, 0);
            tempRect.pivot = new Vector2(0.5f, 1);
            tempRect.anchoredPosition = Vector2.zero;
            tempRect.sizeDelta = new Vector2(0, 150);
            
            Image tempBg = template.AddComponent<Image>();
            tempBg.color = new Color(0.1f, 0.1f, 0.15f);
            
            ScrollRect sr = template.AddComponent<ScrollRect>();
            
            // Viewport
            GameObject vp = new GameObject("Viewport");
            vp.transform.SetParent(template.transform, false);
            RectTransform vpRect = vp.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            vp.AddComponent<Image>().color = Color.clear;
            vp.AddComponent<Mask>();
            sr.viewport = vpRect;
            
            // Content
            GameObject cont = new GameObject("Content");
            cont.transform.SetParent(vp.transform, false);
            RectTransform contRect = cont.AddComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0, 1);
            contRect.anchorMax = new Vector2(1, 1);
            contRect.pivot = new Vector2(0.5f, 1);
            contRect.sizeDelta = new Vector2(0, 0);
            sr.content = contRect;
            
            // Item
            GameObject item = new GameObject("Item");
            item.transform.SetParent(cont.transform, false);
            RectTransform itemRect = item.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 30);
            
            Toggle toggle = item.AddComponent<Toggle>();
            
            // Item background
            GameObject itemBg = new GameObject("Item Background");
            itemBg.transform.SetParent(item.transform, false);
            RectTransform ibRect = itemBg.AddComponent<RectTransform>();
            ibRect.anchorMin = Vector2.zero;
            ibRect.anchorMax = Vector2.one;
            ibRect.offsetMin = Vector2.zero;
            ibRect.offsetMax = Vector2.zero;
            Image ibImg = itemBg.AddComponent<Image>();
            ibImg.color = new Color(0.15f, 0.15f, 0.2f);
            
            // Item label
            GameObject itemLabel = new GameObject("Item Label");
            itemLabel.transform.SetParent(item.transform, false);
            RectTransform ilRect = itemLabel.AddComponent<RectTransform>();
            ilRect.anchorMin = Vector2.zero;
            ilRect.anchorMax = Vector2.one;
            ilRect.offsetMin = new Vector2(10, 0);
            ilRect.offsetMax = Vector2.zero;
            TextMeshProUGUI ilText = itemLabel.AddComponent<TextMeshProUGUI>();
            ilText.fontSize = 12;
            ilText.color = Color.white;
            ilText.alignment = TextAlignmentOptions.MidlineLeft;
            
            dropdown.template = tempRect;
            dropdown.itemText = ilText;
        }

        private void CreateDetailsPanel(Transform parent)
        {
            GameObject detailsPanel = new GameObject("DetailsPanel");
            detailsPanel.transform.SetParent(parent, false);
            
            RectTransform detailsRect = detailsPanel.AddComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(0.65f, 0);
            detailsRect.anchorMax = new Vector2(1, 1);
            detailsRect.offsetMin = new Vector2(5, 0);
            detailsRect.offsetMax = Vector2.zero;
            
            Image detailsBg = detailsPanel.AddComponent<Image>();
            detailsBg.color = new Color(0.1f, 0.1f, 0.14f);
            
            VerticalLayoutGroup vlayout = detailsPanel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Item icon placeholder
            GameObject iconObj = CreateDetailRow(detailsPanel.transform, "🎁", 60);
            
            // Item name
            GameObject nameObj = new GameObject("ItemName");
            nameObj.transform.SetParent(detailsPanel.transform, false);
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 30;
            _selectedItemName = nameObj.AddComponent<TextMeshProUGUI>();
            _selectedItemName.text = "Select an item";
            _selectedItemName.fontSize = 20;
            _selectedItemName.fontStyle = FontStyles.Bold;
            _selectedItemName.color = accentColor;
            _selectedItemName.alignment = TextAlignmentOptions.Center;
            
            // Price
            GameObject priceObj = new GameObject("ItemPrice");
            priceObj.transform.SetParent(detailsPanel.transform, false);
            LayoutElement priceLE = priceObj.AddComponent<LayoutElement>();
            priceLE.preferredHeight = 35;
            _selectedItemPrice = priceObj.AddComponent<TextMeshProUGUI>();
            _selectedItemPrice.text = "---";
            _selectedItemPrice.fontSize = 24;
            _selectedItemPrice.color = accentColor;
            _selectedItemPrice.alignment = TextAlignmentOptions.Center;
            
            // Details text
            GameObject detailsObj = new GameObject("ItemDetails");
            detailsObj.transform.SetParent(detailsPanel.transform, false);
            LayoutElement detailsLE = detailsObj.AddComponent<LayoutElement>();
            detailsLE.preferredHeight = 150;
            detailsLE.flexibleHeight = 1;
            _selectedItemDetails = detailsObj.AddComponent<TextMeshProUGUI>();
            _selectedItemDetails.text = "Browse listings or search for items to see details.";
            _selectedItemDetails.fontSize = 13;
            _selectedItemDetails.color = new Color(0.7f, 0.7f, 0.7f);
            _selectedItemDetails.alignment = TextAlignmentOptions.TopLeft;
            
            // Buy button
            _buyButton = CreateActionButton(detailsPanel.transform, "💰 BUY NOW", buyColor, OnBuyClicked);
            _buyButton.interactable = false;
            
            // Watchlist button
            _watchlistButton = CreateActionButton(detailsPanel.transform, "⭐ Add to Watchlist", highlightColor, OnWatchlistClicked);
            _watchlistButton.interactable = false;
        }

        private GameObject CreateDetailRow(Transform parent, string icon, int iconSize)
        {
            GameObject row = new GameObject("IconRow");
            row.transform.SetParent(parent, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = iconSize + 20;
            
            TextMeshProUGUI text = row.AddComponent<TextMeshProUGUI>();
            text.text = icon;
            text.fontSize = iconSize;
            text.alignment = TextAlignmentOptions.Center;
            
            return row;
        }

        private Button CreateActionButton(Transform parent, string label, Color color, Action onClick)
        {
            GameObject btnObj = new GameObject(label);
            btnObj.transform.SetParent(parent, false);
            
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = color;
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            
            var colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = new Color(color.r + 0.1f, color.g + 0.1f, color.b + 0.1f);
            colors.pressedColor = new Color(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f);
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

        private void CreateFooter()
        {
            GameObject footer = new GameObject("Footer");
            footer.transform.SetParent(_panel.transform, false);
            
            RectTransform footerRect = footer.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0, 0);
            footerRect.anchorMax = new Vector2(1, 0);
            footerRect.pivot = new Vector2(0.5f, 0);
            footerRect.anchoredPosition = Vector2.zero;
            footerRect.sizeDelta = new Vector2(0, 50);
            
            Image footerBg = footer.AddComponent<Image>();
            footerBg.color = new Color(0.1f, 0.1f, 0.15f);
            
            HorizontalLayoutGroup hlayout = footer.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(20, 20, 8, 8);
            hlayout.childForceExpandWidth = false;
            
            // Market stats
            CreateStatLabel(footer.transform, "📊 24h Volume:", "125,432 Gold");
            CreateStatLabel(footer.transform, "📈 Active Listings:", "2,847");
            CreateStatLabel(footer.transform, "👥 Online Traders:", "156");
        }

        private void CreateStatLabel(Transform parent, string label, string value)
        {
            GameObject statObj = new GameObject("Stat");
            statObj.transform.SetParent(parent, false);
            
            LayoutElement le = statObj.AddComponent<LayoutElement>();
            le.preferredWidth = 180;
            
            HorizontalLayoutGroup hlayout = statObj.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 5;
            
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(statObj.transform, false);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 12;
            labelText.color = new Color(0.6f, 0.6f, 0.6f);
            
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(statObj.transform, false);
            TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = value;
            valueText.fontSize = 12;
            valueText.fontStyle = FontStyles.Bold;
            valueText.color = accentColor;
        }

        #region Data & Logic

        private void GenerateSampleListings()
        {
            string[] itemNames = {
                "Iron Sword", "Steel Shield", "Leather Armor", "Health Potion",
                "Mana Crystal", "Gold Ore", "Magic Staff", "Dragon Scale",
                "Phoenix Feather", "Ancient Rune", "Enchanted Ring", "War Horse",
                "Siege Ram", "Crossbow", "Battle Axe", "Royal Crown"
            };
            
            string[] icons = { "⚔️", "🛡️", "🎽", "🧪", "💎", "🪨", "🪄", "🐉", "🪶", "📜", "💍", "🐴", "🪵", "🏹", "🪓", "👑" };
            
            AuctionCategory[] categories = {
                AuctionCategory.Weapons, AuctionCategory.Armor, AuctionCategory.Armor, AuctionCategory.Consumables,
                AuctionCategory.Materials, AuctionCategory.Materials, AuctionCategory.Weapons, AuctionCategory.Materials,
                AuctionCategory.Materials, AuctionCategory.Miscellaneous, AuctionCategory.Equipment, AuctionCategory.Mounts,
                AuctionCategory.Equipment, AuctionCategory.Weapons, AuctionCategory.Weapons, AuctionCategory.Miscellaneous
            };
            
            for (int i = 0; i < 50; i++)
            {
                int idx = UnityEngine.Random.Range(0, itemNames.Length);
                var listing = new AuctionListing
                {
                    ListingId = Guid.NewGuid().ToString(),
                    ItemName = itemNames[idx],
                    ItemIcon = icons[idx],
                    Category = categories[idx],
                    Price = UnityEngine.Random.Range(50, 10000),
                    Quantity = UnityEngine.Random.Range(1, 20),
                    SellerName = $"Player{UnityEngine.Random.Range(1000, 9999)}",
                    TimeRemaining = TimeSpan.FromHours(UnityEngine.Random.Range(1, 48)),
                    Rarity = (ItemRarity)UnityEngine.Random.Range(0, 5),
                    ItemLevel = UnityEngine.Random.Range(1, 100)
                };
                
                _listings.Add(listing);
            }
        }

        private void RefreshListings()
        {
            // Clear existing
            foreach (Transform child in _listingsContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Filter and sort
            _filteredListings = _listings
                .Where(l => _selectedCategory == AuctionCategory.All || l.Category == _selectedCategory)
                .Where(l => string.IsNullOrEmpty(_searchQuery) || 
                           l.ItemName.ToLower().Contains(_searchQuery.ToLower()))
                .ToList();
            
            // Sort
            _filteredListings = _sortMode switch
            {
                AuctionSortMode.PriceLowToHigh => _filteredListings.OrderBy(l => l.Price).ToList(),
                AuctionSortMode.PriceHighToLow => _filteredListings.OrderByDescending(l => l.Price).ToList(),
                AuctionSortMode.TimeEndingSoon => _filteredListings.OrderBy(l => l.TimeRemaining).ToList(),
                AuctionSortMode.TimeRecentlyListed => _filteredListings.OrderByDescending(l => l.TimeRemaining).ToList(),
                AuctionSortMode.NameAZ => _filteredListings.OrderBy(l => l.ItemName).ToList(),
                _ => _filteredListings
            };
            
            // Create listing rows
            foreach (var listing in _filteredListings.Take(50))
            {
                CreateListingRow(listing);
            }
        }

        private void CreateListingRow(AuctionListing listing)
        {
            GameObject row = new GameObject($"Listing_{listing.ListingId}");
            row.transform.SetParent(_listingsContainer.transform, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            Image bg = row.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.16f);
            
            Button btn = row.AddComponent<Button>();
            var listing_copy = listing; // Capture for closure
            btn.onClick.AddListener(() => SelectListing(listing_copy));
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            hlayout.childForceExpandWidth = false;
            
            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(row.transform, false);
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 35;
            TextMeshProUGUI icon = iconObj.AddComponent<TextMeshProUGUI>();
            icon.text = listing.ItemIcon;
            icon.fontSize = 24;
            icon.alignment = TextAlignmentOptions.Center;
            
            // Name & quantity
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(row.transform, false);
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;
            TextMeshProUGUI name = nameObj.AddComponent<TextMeshProUGUI>();
            name.text = $"{listing.ItemName} x{listing.Quantity}";
            name.fontSize = 14;
            name.color = GetRarityColor(listing.Rarity);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Price
            GameObject priceObj = new GameObject("Price");
            priceObj.transform.SetParent(row.transform, false);
            LayoutElement priceLE = priceObj.AddComponent<LayoutElement>();
            priceLE.preferredWidth = 100;
            TextMeshProUGUI price = priceObj.AddComponent<TextMeshProUGUI>();
            price.text = $"💰 {listing.Price:N0}";
            price.fontSize = 14;
            price.color = accentColor;
            price.alignment = TextAlignmentOptions.Center;
            
            // Time remaining
            GameObject timeObj = new GameObject("Time");
            timeObj.transform.SetParent(row.transform, false);
            LayoutElement timeLE = timeObj.AddComponent<LayoutElement>();
            timeLE.preferredWidth = 70;
            TextMeshProUGUI time = timeObj.AddComponent<TextMeshProUGUI>();
            time.text = FormatTimeRemaining(listing.TimeRemaining);
            time.fontSize = 12;
            time.color = listing.TimeRemaining.TotalHours < 2 ? sellColor : new Color(0.6f, 0.6f, 0.6f);
            time.alignment = TextAlignmentOptions.Center;
        }

        private void SelectListing(AuctionListing listing)
        {
            _selectedListing = listing;
            
            _selectedItemName.text = $"{listing.ItemIcon} {listing.ItemName}";
            _selectedItemName.color = GetRarityColor(listing.Rarity);
            
            _selectedItemPrice.text = $"💰 {listing.Price:N0} Gold";
            
            string details = $"<b>Rarity:</b> {listing.Rarity}\n" +
                           $"<b>Item Level:</b> {listing.ItemLevel}\n" +
                           $"<b>Quantity:</b> {listing.Quantity}\n" +
                           $"<b>Seller:</b> {listing.SellerName}\n" +
                           $"<b>Time Left:</b> {FormatTimeRemaining(listing.TimeRemaining)}\n\n" +
                           $"<b>Category:</b> {GetCategoryName(listing.Category)}";
            
            _selectedItemDetails.text = details;
            
            _buyButton.interactable = _playerGold >= listing.Price;
            _watchlistButton.interactable = true;
        }

        private void OnSearchChanged(string value)
        {
            _searchQuery = value;
            RefreshListings();
        }

        private void OnCategoryChanged(int index)
        {
            _selectedCategory = (AuctionCategory)index;
            RefreshListings();
        }

        private void OnSortChanged(int index)
        {
            _sortMode = (AuctionSortMode)index;
            RefreshListings();
        }

        private void SwitchTab(AuctionTab tab)
        {
            _currentTab = tab;
            UpdateTabHighlights();
            
            // Refresh content based on tab
            switch (tab)
            {
                case AuctionTab.Browse:
                    RefreshListings();
                    break;
                case AuctionTab.Sell:
                    ShowSellInterface();
                    break;
                case AuctionTab.MyOrders:
                    ShowMyOrders();
                    break;
                case AuctionTab.History:
                    ShowHistory();
                    break;
                case AuctionTab.Watchlist:
                    ShowWatchlist();
                    break;
            }
        }

        private void UpdateTabHighlights()
        {
            SetTabHighlight(_browseTab, _currentTab == AuctionTab.Browse);
            SetTabHighlight(_sellTab, _currentTab == AuctionTab.Sell);
            SetTabHighlight(_ordersTab, _currentTab == AuctionTab.MyOrders);
            SetTabHighlight(_historyTab, _currentTab == AuctionTab.History);
            SetTabHighlight(_watchlistTab, _currentTab == AuctionTab.Watchlist);
        }

        private void SetTabHighlight(Button tab, bool active)
        {
            if (tab == null) return;
            var image = tab.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? accentColor : new Color(0.15f, 0.15f, 0.2f);
            }
        }

        private void ShowSellInterface()
        {
            Debug.Log("[AuctionHouse] Opening sell interface...");
        }

        private void ShowMyOrders()
        {
            Debug.Log("[AuctionHouse] Showing my orders...");
        }

        private void ShowHistory()
        {
            Debug.Log("[AuctionHouse] Showing transaction history...");
        }

        private void ShowWatchlist()
        {
            Debug.Log("[AuctionHouse] Showing watchlist...");
        }

        private void OnBuyClicked()
        {
            if (_selectedListing == null) return;
            if (_playerGold < _selectedListing.Price)
            {
                NotificationSystem.Instance?.ShowError("Not enough gold!");
                return;
            }
            
            _playerGold -= _selectedListing.Price;
            _balanceText.text = $"💰 {_playerGold:N0} Gold";
            
            _transactionHistory.Add(new AuctionTransaction
            {
                Listing = _selectedListing,
                Type = TransactionType.Purchase,
                Timestamp = DateTime.Now
            });
            
            _listings.Remove(_selectedListing);
            RefreshListings();
            
            NotificationSystem.Instance?.ShowSuccess($"Purchased {_selectedListing.ItemName}!");
            OnItemPurchased?.Invoke(_selectedListing);
            
            _selectedListing = null;
            _buyButton.interactable = false;
            _watchlistButton.interactable = false;
        }

        private void OnWatchlistClicked()
        {
            if (_selectedListing == null) return;
            
            if (!_watchlist.Contains(_selectedListing))
            {
                _watchlist.Add(_selectedListing);
                NotificationSystem.Instance?.ShowInfo($"Added {_selectedListing.ItemName} to watchlist");
                OnWatchlistAdded?.Invoke(_selectedListing);
            }
        }

        #endregion

        #region Helpers

        private string GetCategoryName(AuctionCategory category)
        {
            return category switch
            {
                AuctionCategory.All => "📦 All Categories",
                AuctionCategory.Weapons => "⚔️ Weapons",
                AuctionCategory.Armor => "🛡️ Armor",
                AuctionCategory.Equipment => "🎒 Equipment",
                AuctionCategory.Consumables => "🧪 Consumables",
                AuctionCategory.Materials => "🪨 Materials",
                AuctionCategory.Mounts => "🐴 Mounts",
                AuctionCategory.Miscellaneous => "📜 Miscellaneous",
                _ => "Unknown"
            };
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => new Color(0.7f, 0.7f, 0.7f),
                ItemRarity.Rare => new Color(0.3f, 0.5f, 0.9f),
                ItemRarity.Epic => new Color(0.7f, 0.3f, 0.9f),
                ItemRarity.Legendary => new Color(1f, 0.6f, 0.1f),
                _ => Color.white
            };
        }

        private string FormatTimeRemaining(TimeSpan time)
        {
            if (time.TotalDays >= 1)
                return $"{(int)time.TotalDays}d {time.Hours}h";
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{time.Minutes}m";
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel?.SetActive(true);
            RefreshListings();
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

    public enum AuctionTab
    {
        Browse,
        Sell,
        MyOrders,
        History,
        Watchlist
    }

    public enum AuctionCategory
    {
        All,
        Weapons,
        Armor,
        Equipment,
        Consumables,
        Materials,
        Mounts,
        Miscellaneous
    }

    public enum AuctionSortMode
    {
        PriceLowToHigh,
        PriceHighToLow,
        TimeEndingSoon,
        TimeRecentlyListed,
        NameAZ
    }

    public class AuctionListing
    {
        public string ListingId;
        public string ItemName;
        public string ItemIcon;
        public AuctionCategory Category;
        public int Price;
        public int Quantity;
        public string SellerName;
        public TimeSpan TimeRemaining;
        public ItemRarity Rarity;
        public int ItemLevel;
        public string Description;
    }

    public class AuctionTransaction
    {
        public AuctionListing Listing;
        public TransactionType Type;
        public DateTime Timestamp;
    }

    public enum TransactionType
    {
        Purchase,
        Sale,
        ListingCreated,
        ListingCancelled,
        ListingExpired
    }

    #endregion
}
