using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if FIREBASE_ENABLED
using Firebase.Functions;
#endif
using Newtonsoft.Json;

namespace ApexCitadels.Cosmetics
{
    /// <summary>
    /// Cosmetic item data
    /// </summary>
    [Serializable]
    public class CosmeticItem
    {
        public string Id;
        public string Name;
        public string Description;
        public string Category;
        public string Rarity;
        public string PreviewImage;
        public string Preview3dModel;
        public int PriceGems;
        public int PriceCoins;
        public int OriginalPriceGems;
        public int OriginalPriceCoins;
        public int DiscountPercentage;
        public bool IsLimited;
        public DateTime? AvailableUntil;
        public bool IsOwned;
        public bool IsEquipped;
        public bool IsFavorite;
        public List<string> Tags = new List<string>();
    }

    /// <summary>
    /// Currency balance
    /// </summary>
    [Serializable]
    public class CurrencyBalance
    {
        public int Gems;
        public int EarnedGems;
        public int PremiumGems;
        public int Coins;
    }

    /// <summary>
    /// Shop rotation data
    /// </summary>
    [Serializable]
    public class ShopRotation
    {
        public string Name;
        public DateTime EndDate;
        public int DiscountPercentage;
    }

    /// <summary>
    /// Cosmetics shop manager and UI controller
    /// </summary>
    public class CosmeticsShopManager : MonoBehaviour
    {
        public static CosmeticsShopManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Transform categoryTabsContainer;
        [SerializeField] private Transform itemGridContainer;
        [SerializeField] private GameObject itemCardPrefab;
        [SerializeField] private GameObject categoryTabPrefab;

        [Header("Currency Display")]
        [SerializeField] private TextMeshProUGUI gemsText;
        [SerializeField] private TextMeshProUGUI coinsText;

        [Header("Item Preview Panel")]
        [SerializeField] private GameObject previewPanel;
        [SerializeField] private Image previewImage;
        [SerializeField] private Transform preview3dContainer;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private Image rarityBackground;
        [SerializeField] private TextMeshProUGUI priceGemsText;
        [SerializeField] private TextMeshProUGUI priceCoinsText;
        [SerializeField] private GameObject discountBadge;
        [SerializeField] private TextMeshProUGUI discountText;
        [SerializeField] private TextMeshProUGUI originalPriceText;
        [SerializeField] private GameObject limitedBadge;
        [SerializeField] private TextMeshProUGUI limitedTimerText;

        [Header("Buttons")]
        [SerializeField] private Button purchaseGemsButton;
        [SerializeField] private Button purchaseCoinsButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button unequipButton;
        [SerializeField] private Button favoriteButton;
        [SerializeField] private Button closePreviewButton;
        [SerializeField] private Button closeShopButton;

        [Header("Tabs")]
        [SerializeField] private Button shopTabButton;
        [SerializeField] private Button inventoryTabButton;
        [SerializeField] private Button favoritesTabButton;

        [Header("Featured Section")]
        [SerializeField] private Transform featuredContainer;
        [SerializeField] private TextMeshProUGUI rotationNameText;
        [SerializeField] private TextMeshProUGUI rotationTimerText;

        [Header("Loading")]
        [SerializeField] private GameObject loadingOverlay;

        [Header("Colors")]
        [SerializeField] private Color commonColor = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color uncommonColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color rareColor = new Color(0.2f, 0.4f, 1f);
        [SerializeField] private Color epicColor = new Color(0.6f, 0.2f, 0.8f);
        [SerializeField] private Color legendaryColor = new Color(1f, 0.6f, 0f);

        // Events
        public event Action<CosmeticItem> OnItemPurchased;
        public event Action<CosmeticItem> OnItemEquipped;
        public event Action<CosmeticItem> OnItemUnequipped;
        public event Action<CurrencyBalance> OnBalanceChanged;

        // State
#if FIREBASE_ENABLED
        private FirebaseFunctions _functions;
#endif
        private CurrencyBalance _balance;
        private List<CosmeticItem> _shopItems = new List<CosmeticItem>();
        private List<CosmeticItem> _ownedItems = new List<CosmeticItem>();
        private Dictionary<string, string> _equippedItems = new Dictionary<string, string>();
        private List<string> _favorites = new List<string>();
        private CosmeticItem _selectedItem;
        private string _currentCategory = "all";
        private string _currentView = "shop"; // shop, inventory, favorites
        private ShopRotation _currentRotation;

        private readonly string[] _categories = new[]
        {
            "all", "block_skin", "territory_effect", "avatar_frame",
            "avatar_background", "victory_animation", "building_effect",
            "chat_bubble", "title", "emote", "trail_effect"
        };

        private readonly Dictionary<string, string> _categoryNames = new Dictionary<string, string>
        {
            { "all", "All" },
            { "block_skin", "Block Skins" },
            { "territory_effect", "Territory Effects" },
            { "avatar_frame", "Avatar Frames" },
            { "avatar_background", "Backgrounds" },
            { "victory_animation", "Victory Animations" },
            { "building_effect", "Building Effects" },
            { "chat_bubble", "Chat Bubbles" },
            { "title", "Titles" },
            { "emote", "Emotes" },
            { "trail_effect", "Trail Effects" }
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

#if FIREBASE_ENABLED
            _functions = FirebaseFunctions.DefaultInstance;
#else
            Debug.LogWarning("[CosmeticsShopManager] Firebase SDK not installed. Running in stub mode.");
#endif
            SetupButtons();
            CreateCategoryTabs();
        }

        private void SetupButtons()
        {
            purchaseGemsButton?.onClick.AddListener(() => PurchaseItem("gems"));
            purchaseCoinsButton?.onClick.AddListener(() => PurchaseItem("coins"));
            equipButton?.onClick.AddListener(EquipSelectedItem);
            unequipButton?.onClick.AddListener(UnequipSelectedItem);
            favoriteButton?.onClick.AddListener(ToggleFavorite);
            closePreviewButton?.onClick.AddListener(ClosePreview);
            closeShopButton?.onClick.AddListener(CloseShop);

            shopTabButton?.onClick.AddListener(() => SwitchView("shop"));
            inventoryTabButton?.onClick.AddListener(() => SwitchView("inventory"));
            favoritesTabButton?.onClick.AddListener(() => SwitchView("favorites"));
        }

        private void CreateCategoryTabs()
        {
            if (categoryTabsContainer == null || categoryTabPrefab == null) return;

            foreach (Transform child in categoryTabsContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var category in _categories)
            {
                var tab = Instantiate(categoryTabPrefab, categoryTabsContainer);
                var tabText = tab.GetComponentInChildren<TextMeshProUGUI>();
                if (tabText != null)
                {
                    tabText.text = _categoryNames.GetValueOrDefault(category, category);
                }

                var button = tab.GetComponent<Button>();
                string cat = category; // Capture for closure
                button?.onClick.AddListener(() => SelectCategory(cat));
            }
        }

        /// <summary>
        /// Open the cosmetics shop
        /// </summary>
        public async void OpenShop()
        {
            shopPanel?.SetActive(true);
            ShowLoading(true);

            try
            {
                await Task.WhenAll(
                    RefreshBalance(),
                    RefreshShopCatalog(),
                    RefreshUserCosmetics()
                );

                SwitchView("shop");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Shop] Failed to open shop: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        /// <summary>
        /// Close the shop
        /// </summary>
        public void CloseShop()
        {
            ClosePreview();
            shopPanel?.SetActive(false);
        }

        /// <summary>
        /// Refresh currency balance
        /// </summary>
        public async Task RefreshBalance()
        {
            try
            {
#if FIREBASE_ENABLED
                var function = _functions.GetHttpsCallable("getCurrencyBalance");
                var result = await function.CallAsync();
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                _balance = new CurrencyBalance
                {
                    Gems = Convert.ToInt32(response.GetValueOrDefault("gems", 0)),
                    EarnedGems = Convert.ToInt32(response.GetValueOrDefault("earnedGems", 0)),
                    PremiumGems = Convert.ToInt32(response.GetValueOrDefault("premiumGems", 0)),
                    Coins = Convert.ToInt32(response.GetValueOrDefault("coins", 0))
                };

                UpdateBalanceDisplay();
                OnBalanceChanged?.Invoke(_balance);
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Shop] Failed to refresh balance: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh shop catalog
        /// </summary>
        public async Task RefreshShopCatalog()
        {
            try
            {
#if FIREBASE_ENABLED
                var function = _functions.GetHttpsCallable("getShopCatalog");
                var data = new Dictionary<string, object>
                {
                    ["includeOwned"] = true
                };

                var result = await function.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                _shopItems.Clear();

                if (response.ContainsKey("items"))
                {
                    var itemsData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                        response["items"].ToString()
                    );

                    foreach (var itemData in itemsData)
                    {
                        _shopItems.Add(ParseCosmeticItem(itemData));
                    }
                }

                // Parse rotation
                if (response.ContainsKey("rotation") && response["rotation"] != null)
                {
                    var rotationData = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        response["rotation"].ToString()
                    );

                    _currentRotation = new ShopRotation
                    {
                        Name = rotationData.GetValueOrDefault("name", "")?.ToString(),
                        DiscountPercentage = Convert.ToInt32(rotationData.GetValueOrDefault("discountPercentage", 0))
                    };

                    if (rotationData.ContainsKey("endDate"))
                    {
                        DateTime.TryParse(rotationData["endDate"].ToString(), out DateTime endDate);
                        _currentRotation.EndDate = endDate;
                    }

                    UpdateRotationDisplay();
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Shop] Failed to refresh catalog: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh user's owned cosmetics
        /// </summary>
        public async Task RefreshUserCosmetics()
        {
            try
            {
#if FIREBASE_ENABLED
                var function = _functions.GetHttpsCallable("getUserCosmetics");
                var result = await function.CallAsync();
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                _ownedItems.Clear();
                _equippedItems.Clear();
                _favorites.Clear();

                if (response.ContainsKey("items"))
                {
                    var itemsData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                        response["items"].ToString()
                    );

                    foreach (var itemData in itemsData)
                    {
                        var item = ParseCosmeticItem(itemData);
                        item.IsOwned = true;
                        _ownedItems.Add(item);
                    }
                }

                if (response.ContainsKey("equipped"))
                {
                    var equippedData = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        response["equipped"].ToString()
                    );
                    foreach (var kvp in equippedData)
                    {
                        if (!string.IsNullOrEmpty(kvp.Value))
                        {
                            _equippedItems[kvp.Key] = kvp.Value;
                        }
                    }
                }

                if (response.ContainsKey("favorites"))
                {
                    var favoritesData = JsonConvert.DeserializeObject<List<string>>(
                        response["favorites"].ToString()
                    );
                    _favorites.AddRange(favoritesData);
                }

                // Update owned status in shop items
                foreach (var item in _shopItems)
                {
                    item.IsOwned = _ownedItems.Exists(o => o.Id == item.Id);
                    item.IsEquipped = _equippedItems.GetValueOrDefault(item.Category) == item.Id;
                    item.IsFavorite = _favorites.Contains(item.Id);
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Shop] Failed to refresh user cosmetics: {ex.Message}");
            }
        }

        private CosmeticItem ParseCosmeticItem(Dictionary<string, object> data)
        {
            var item = new CosmeticItem
            {
                Id = data.GetValueOrDefault("id", "")?.ToString(),
                Name = data.GetValueOrDefault("name", "")?.ToString(),
                Description = data.GetValueOrDefault("description", "")?.ToString(),
                Category = data.GetValueOrDefault("category", "")?.ToString(),
                Rarity = data.GetValueOrDefault("rarity", "common")?.ToString(),
                PreviewImage = data.GetValueOrDefault("previewImage", "")?.ToString(),
                Preview3dModel = data.GetValueOrDefault("preview3dModel", "")?.ToString(),
                PriceGems = Convert.ToInt32(data.GetValueOrDefault("priceGems", 0)),
                PriceCoins = Convert.ToInt32(data.GetValueOrDefault("priceCoins", 0)),
                OriginalPriceGems = Convert.ToInt32(data.GetValueOrDefault("originalPriceGems", 0)),
                OriginalPriceCoins = Convert.ToInt32(data.GetValueOrDefault("originalPriceCoins", 0)),
                DiscountPercentage = Convert.ToInt32(data.GetValueOrDefault("discountPercentage", 0)),
                IsLimited = (bool)data.GetValueOrDefault("isLimited", false),
                IsOwned = (bool)data.GetValueOrDefault("isOwned", false),
                IsEquipped = (bool)data.GetValueOrDefault("isEquipped", false),
                IsFavorite = (bool)data.GetValueOrDefault("isFavorite", false)
            };

            if (data.ContainsKey("availableUntil") && data["availableUntil"] != null)
            {
                DateTime.TryParse(data["availableUntil"].ToString(), out DateTime availableUntil);
                item.AvailableUntil = availableUntil;
            }

            if (data.ContainsKey("tags"))
            {
                item.Tags = JsonConvert.DeserializeObject<List<string>>(data["tags"].ToString());
            }

            return item;
        }

        /// <summary>
        /// Switch between shop/inventory/favorites view
        /// </summary>
        public void SwitchView(string view)
        {
            _currentView = view;

            // Update tab highlights
            UpdateTabHighlights();
            RefreshItemGrid();
        }

        /// <summary>
        /// Select a category filter
        /// </summary>
        public void SelectCategory(string category)
        {
            _currentCategory = category;
            RefreshItemGrid();
        }

        /// <summary>
        /// Refresh the item grid display
        /// </summary>
        private void RefreshItemGrid()
        {
            if (itemGridContainer == null) return;

            // Clear existing items
            foreach (Transform child in itemGridContainer)
            {
                Destroy(child.gameObject);
            }

            // Get items based on current view
            List<CosmeticItem> items;
            switch (_currentView)
            {
                case "inventory":
                    items = _ownedItems;
                    break;
                case "favorites":
                    items = _ownedItems.FindAll(i => _favorites.Contains(i.Id));
                    break;
                default:
                    items = _shopItems;
                    break;
            }

            // Filter by category
            if (_currentCategory != "all")
            {
                items = items.FindAll(i => i.Category == _currentCategory);
            }

            // Create item cards
            foreach (var item in items)
            {
                CreateItemCard(item);
            }
        }

        /// <summary>
        /// Create an item card in the grid
        /// </summary>
        private void CreateItemCard(CosmeticItem item)
        {
            if (itemCardPrefab == null || itemGridContainer == null) return;

            var card = Instantiate(itemCardPrefab, itemGridContainer);
            var cardComponent = card.GetComponent<CosmeticItemCard>();

            if (cardComponent != null)
            {
                cardComponent.Setup(item, GetRarityColor(item.Rarity));
                cardComponent.OnClicked += () => SelectItem(item);
            }
            else
            {
                // Fallback manual setup
                var nameText = card.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null) nameText.text = item.Name;

                var button = card.GetComponent<Button>();
                button?.onClick.AddListener(() => SelectItem(item));

                // Set rarity border color
                var border = card.transform.Find("Border")?.GetComponent<Image>();
                if (border != null)
                {
                    border.color = GetRarityColor(item.Rarity);
                }

                // Show owned badge
                var ownedBadge = card.transform.Find("OwnedBadge")?.gameObject;
                if (ownedBadge != null)
                {
                    ownedBadge.SetActive(item.IsOwned);
                }

                // Show equipped badge
                var equippedBadge = card.transform.Find("EquippedBadge")?.gameObject;
                if (equippedBadge != null)
                {
                    equippedBadge.SetActive(item.IsEquipped);
                }
            }
        }

        /// <summary>
        /// Select an item to preview
        /// </summary>
        public void SelectItem(CosmeticItem item)
        {
            _selectedItem = item;
            ShowPreview();
        }

        /// <summary>
        /// Show item preview panel
        /// </summary>
        private void ShowPreview()
        {
            if (_selectedItem == null || previewPanel == null) return;

            previewPanel.SetActive(true);

            // Set item info
            if (itemNameText != null) itemNameText.text = _selectedItem.Name;
            if (itemDescriptionText != null) itemDescriptionText.text = _selectedItem.Description;
            if (rarityText != null) rarityText.text = _selectedItem.Rarity.ToUpper();
            if (rarityBackground != null) rarityBackground.color = GetRarityColor(_selectedItem.Rarity);

            // Set prices
            if (priceGemsText != null) priceGemsText.text = _selectedItem.PriceGems.ToString("N0");
            if (priceCoinsText != null) priceCoinsText.text = _selectedItem.PriceCoins.ToString("N0");

            // Show discount if applicable
            if (discountBadge != null)
            {
                discountBadge.SetActive(_selectedItem.DiscountPercentage > 0);
                if (discountText != null)
                    discountText.text = $"-{_selectedItem.DiscountPercentage}%";
                if (originalPriceText != null)
                    originalPriceText.text = _selectedItem.OriginalPriceGems.ToString("N0");
            }

            // Show limited timer
            if (limitedBadge != null)
            {
                limitedBadge.SetActive(_selectedItem.IsLimited && _selectedItem.AvailableUntil.HasValue);
                if (limitedTimerText != null && _selectedItem.AvailableUntil.HasValue)
                {
                    var remaining = _selectedItem.AvailableUntil.Value - DateTime.Now;
                    if (remaining.TotalDays >= 1)
                        limitedTimerText.text = $"{(int)remaining.TotalDays}d {remaining.Hours}h";
                    else
                        limitedTimerText.text = $"{remaining.Hours}h {remaining.Minutes}m";
                }
            }

            // Update button states
            UpdatePreviewButtons();

            // Load preview image
            LoadPreviewImage();
        }

        /// <summary>
        /// Update preview panel buttons based on item state
        /// </summary>
        private void UpdatePreviewButtons()
        {
            bool isOwned = _selectedItem.IsOwned;
            bool isEquipped = _selectedItem.IsEquipped;
            bool canAffordGems = _balance != null && _balance.Gems >= _selectedItem.PriceGems;
            bool canAffordCoins = _balance != null && _balance.Coins >= _selectedItem.PriceCoins;

            // Purchase buttons - show only if not owned
            if (purchaseGemsButton != null)
            {
                purchaseGemsButton.gameObject.SetActive(!isOwned);
                purchaseGemsButton.interactable = canAffordGems;
            }

            if (purchaseCoinsButton != null)
            {
                purchaseCoinsButton.gameObject.SetActive(!isOwned);
                purchaseCoinsButton.interactable = canAffordCoins;
            }

            // Equip/Unequip buttons - show only if owned
            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(isOwned && !isEquipped);
            }

            if (unequipButton != null)
            {
                unequipButton.gameObject.SetActive(isOwned && isEquipped);
            }

            // Favorite button - show only if owned
            if (favoriteButton != null)
            {
                favoriteButton.gameObject.SetActive(isOwned);
                // Update favorite icon
                var icon = favoriteButton.GetComponentInChildren<Image>();
                if (icon != null)
                {
                    icon.color = _selectedItem.IsFavorite ? Color.yellow : Color.white;
                }
            }
        }

        /// <summary>
        /// Load preview image asynchronously
        /// </summary>
        private async void LoadPreviewImage()
        {
            if (string.IsNullOrEmpty(_selectedItem.PreviewImage) || previewImage == null) return;

            // For now, assume the image is in Resources
            // In production, use AddressableAssets or async download
            var sprite = UnityEngine.Resources.Load<Sprite>($"Cosmetics/{_selectedItem.Id}");
            if (sprite != null)
            {
                previewImage.sprite = sprite;
            }
        }

        /// <summary>
        /// Close preview panel
        /// </summary>
        public void ClosePreview()
        {
            previewPanel?.SetActive(false);
            _selectedItem = null;
        }

        /// <summary>
        /// Purchase the selected item
        /// </summary>
        public async void PurchaseItem(string currency)
        {
            if (_selectedItem == null) return;

            ShowLoading(true);

            try
            {
                var function = _functions.GetHttpsCallable("purchaseCosmetic");
                var data = new Dictionary<string, object>
                {
                    ["itemId"] = _selectedItem.Id,
                    ["currency"] = currency
                };

                var result = await function.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                bool success = (bool)response.GetValueOrDefault("success", false);

                if (success)
                {
                    // Update local state
                    _selectedItem.IsOwned = true;

                    // Update balance
                    if (response.ContainsKey("newBalance"))
                    {
                        var balanceData = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                            response["newBalance"].ToString()
                        );
                        _balance.Gems = Convert.ToInt32(balanceData.GetValueOrDefault("gems", 0));
                        _balance.Coins = Convert.ToInt32(balanceData.GetValueOrDefault("coins", 0));
                        UpdateBalanceDisplay();
                        OnBalanceChanged?.Invoke(_balance);
                    }

                    // Add to owned list
                    if (!_ownedItems.Exists(i => i.Id == _selectedItem.Id))
                    {
                        _ownedItems.Add(_selectedItem);
                    }

                    // Update shop item
                    var shopItem = _shopItems.Find(i => i.Id == _selectedItem.Id);
                    if (shopItem != null)
                    {
                        shopItem.IsOwned = true;
                    }

                    OnItemPurchased?.Invoke(_selectedItem);
                    UpdatePreviewButtons();
                    RefreshItemGrid();

                    ShowMessage("Purchase successful!");
                }
                else
                {
                    string message = response.GetValueOrDefault("message", "Purchase failed")?.ToString();
                    ShowMessage(message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Shop] Purchase failed: {ex.Message}");
                ShowMessage("Purchase failed. Please try again.");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        /// <summary>
        /// Equip the selected item
        /// </summary>
        public async void EquipSelectedItem()
        {
            if (_selectedItem == null || !_selectedItem.IsOwned) return;

            ShowLoading(true);

            try
            {
                var function = _functions.GetHttpsCallable("equipCosmetic");
                var data = new Dictionary<string, object>
                {
                    ["itemId"] = _selectedItem.Id
                };

                var result = await function.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                bool success = (bool)response.GetValueOrDefault("success", false);

                if (success)
                {
                    // Unequip previous item in same category
                    string previousEquipped = _equippedItems.GetValueOrDefault(_selectedItem.Category);
                    if (!string.IsNullOrEmpty(previousEquipped))
                    {
                        var prevItem = _ownedItems.Find(i => i.Id == previousEquipped);
                        if (prevItem != null) prevItem.IsEquipped = false;

                        var prevShopItem = _shopItems.Find(i => i.Id == previousEquipped);
                        if (prevShopItem != null) prevShopItem.IsEquipped = false;
                    }

                    // Update local state
                    _selectedItem.IsEquipped = true;
                    _equippedItems[_selectedItem.Category] = _selectedItem.Id;

                    OnItemEquipped?.Invoke(_selectedItem);
                    UpdatePreviewButtons();
                    RefreshItemGrid();

                    ShowMessage($"{_selectedItem.Name} equipped!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Shop] Equip failed: {ex.Message}");
                ShowMessage("Failed to equip item.");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        /// <summary>
        /// Unequip the selected item
        /// </summary>
        public async void UnequipSelectedItem()
        {
            if (_selectedItem == null || !_selectedItem.IsEquipped) return;

            ShowLoading(true);

            try
            {
                var function = _functions.GetHttpsCallable("unequipCosmetic");
                var data = new Dictionary<string, object>
                {
                    ["category"] = _selectedItem.Category
                };

                var result = await function.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                bool success = (bool)response.GetValueOrDefault("success", false);

                if (success)
                {
                    _selectedItem.IsEquipped = false;
                    _equippedItems.Remove(_selectedItem.Category);

                    OnItemUnequipped?.Invoke(_selectedItem);
                    UpdatePreviewButtons();
                    RefreshItemGrid();

                    ShowMessage($"{_selectedItem.Name} unequipped!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Shop] Unequip failed: {ex.Message}");
                ShowMessage("Failed to unequip item.");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        /// <summary>
        /// Toggle favorite status
        /// </summary>
        public async void ToggleFavorite()
        {
            if (_selectedItem == null || !_selectedItem.IsOwned) return;

            try
            {
                var function = _functions.GetHttpsCallable("toggleFavorite");
                var data = new Dictionary<string, object>
                {
                    ["itemId"] = _selectedItem.Id
                };

                var result = await function.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                bool success = (bool)response.GetValueOrDefault("success", false);

                if (success)
                {
                    bool isFavorite = (bool)response.GetValueOrDefault("isFavorite", false);
                    _selectedItem.IsFavorite = isFavorite;

                    if (isFavorite)
                        _favorites.Add(_selectedItem.Id);
                    else
                        _favorites.Remove(_selectedItem.Id);

                    UpdatePreviewButtons();

                    if (_currentView == "favorites")
                        RefreshItemGrid();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Shop] Toggle favorite failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Update currency display
        /// </summary>
        private void UpdateBalanceDisplay()
        {
            if (_balance == null) return;

            if (gemsText != null) gemsText.text = _balance.Gems.ToString("N0");
            if (coinsText != null) coinsText.text = _balance.Coins.ToString("N0");
        }

        /// <summary>
        /// Update rotation display
        /// </summary>
        private void UpdateRotationDisplay()
        {
            if (_currentRotation == null) return;

            if (rotationNameText != null)
                rotationNameText.text = _currentRotation.Name;

            if (rotationTimerText != null)
            {
                var remaining = _currentRotation.EndDate - DateTime.Now;
                if (remaining.TotalDays >= 1)
                    rotationTimerText.text = $"Ends in {(int)remaining.TotalDays}d {remaining.Hours}h";
                else
                    rotationTimerText.text = $"Ends in {remaining.Hours}h {remaining.Minutes}m";
            }
        }

        /// <summary>
        /// Update tab highlight states
        /// </summary>
        private void UpdateTabHighlights()
        {
            // Implement tab visual highlighting based on _currentView
            // This depends on your UI implementation
        }

        /// <summary>
        /// Get color for rarity
        /// </summary>
        private Color GetRarityColor(string rarity)
        {
            return rarity?.ToLower() switch
            {
                "common" => commonColor,
                "uncommon" => uncommonColor,
                "rare" => rareColor,
                "epic" => epicColor,
                "legendary" => legendaryColor,
                _ => commonColor
            };
        }

        /// <summary>
        /// Show/hide loading overlay
        /// </summary>
        private void ShowLoading(bool show)
        {
            loadingOverlay?.SetActive(show);
        }

        /// <summary>
        /// Show a message to user
        /// </summary>
        private void ShowMessage(string message)
        {
            // Integrate with your notification system
            Debug.Log($"[Shop] {message}");
        }

        /// <summary>
        /// Get currently equipped item for category
        /// </summary>
        public string GetEquippedItem(string category)
        {
            return _equippedItems.GetValueOrDefault(category);
        }

        /// <summary>
        /// Check if item is owned
        /// </summary>
        public bool IsItemOwned(string itemId)
        {
            return _ownedItems.Exists(i => i.Id == itemId);
        }

        /// <summary>
        /// Get current balance
        /// </summary>
        public CurrencyBalance GetBalance() => _balance;
    }

    /// <summary>
    /// Item card component for the shop grid
    /// </summary>
    public class CosmeticItemCard : MonoBehaviour
    {
        [SerializeField] private Image previewImage;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private GameObject ownedBadge;
        [SerializeField] private GameObject equippedBadge;
        [SerializeField] private GameObject discountBadge;
        [SerializeField] private TextMeshProUGUI discountText;

        public event Action OnClicked;

        private CosmeticItem _item;

        public void Setup(CosmeticItem item, Color rarityColor)
        {
            _item = item;

            if (nameText != null) nameText.text = item.Name;
            if (rarityBorder != null) rarityBorder.color = rarityColor;
            if (priceText != null) priceText.text = item.PriceGems.ToString("N0");
            if (ownedBadge != null) ownedBadge.SetActive(item.IsOwned);
            if (equippedBadge != null) equippedBadge.SetActive(item.IsEquipped);

            if (discountBadge != null)
            {
                discountBadge.SetActive(item.DiscountPercentage > 0);
                if (discountText != null)
                    discountText.text = $"-{item.DiscountPercentage}%";
            }

            var button = GetComponent<Button>();
            button?.onClick.AddListener(() => OnClicked?.Invoke());
        }
    }
}
