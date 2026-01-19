using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using ApexCitadels.Core;

#if FIREBASE_ENABLED
using Firebase.Firestore;
#endif

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// PC-exclusive Market Panel for trading resources and items
    /// </summary>
    public class MarketPanel : MonoBehaviour
    {
        [Header("Tab Navigation")]
        [SerializeField] private Button buyTabButton;
        [SerializeField] private Button sellTabButton;
        [SerializeField] private Button myListingsTabButton;
        [SerializeField] private Button historyTabButton;
        
        [Header("Buy Tab")]
        [SerializeField] private Transform buyListContent;
        [SerializeField] private GameObject marketListingPrefab;
        [SerializeField] private TMP_Dropdown categoryFilterDropdown;
        [SerializeField] private TMP_Dropdown sortDropdown;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Button refreshButton;
        
        [Header("Sell Tab")]
        [SerializeField] private Transform inventorySellContent;
        [SerializeField] private GameObject inventoryItemPrefab;
        [SerializeField] private TMP_InputField priceInput;
        [SerializeField] private TMP_InputField quantityInput;
        [SerializeField] private Button listItemButton;
        [SerializeField] private TMP_Text selectedItemText;
        [SerializeField] private TMP_Text marketPriceText;
        [SerializeField] private TMP_Text listingFeeText;
        
        [Header("My Listings Tab")]
        [SerializeField] private Transform myListingsContent;
        [SerializeField] private TMP_Text totalListingsText;
        [SerializeField] private TMP_Text pendingEarningsText;
        
        [Header("History Tab")]
        [SerializeField] private Transform historyContent;
        [SerializeField] private GameObject historyItemPrefab;
        [SerializeField] private TMP_Text totalTradedText;
        
        [Header("Purchase Dialog")]
        [SerializeField] private GameObject purchaseDialog;
        [SerializeField] private Image purchaseItemIcon;
        [SerializeField] private TMP_Text purchaseItemName;
        [SerializeField] private TMP_Text purchaseQuantityText;
        [SerializeField] private TMP_Text purchaseTotalText;
        [SerializeField] private Slider purchaseQuantitySlider;
        [SerializeField] private Button confirmPurchaseButton;
        [SerializeField] private Button cancelPurchaseButton;
        
        [Header("Price Chart")]
        [SerializeField] private RawImage priceChartImage;
        [SerializeField] private TMP_Text priceChangeText;
        [SerializeField] private TMP_Text volumeText;
        
        [Header("Colors")]
        [SerializeField] private Color profitColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color lossColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color neutralColor = Color.white;
        
        // State
        private MarketTab _currentTab = MarketTab.Buy;
        private List<MarketListing> _currentListings = new List<MarketListing>();
        private List<MarketListing> _myListings = new List<MarketListing>();
        private List<MarketTransaction> _transactionHistory = new List<MarketTransaction>();
        private MarketListing _selectedListing;
        private string _selectedSellItem;
        private int _selectedSellQuantity;
        
        private List<GameObject> _listingItems = new List<GameObject>();
        private List<GameObject> _inventoryItems = new List<GameObject>();
        private List<GameObject> _historyItems = new List<GameObject>();
        
        private const float LISTING_FEE_PERCENT = 0.05f; // 5% fee
        
        private void Awake()
        {
            SetupEventHandlers();
        }
        
        private void SetupEventHandlers()
        {
            // Tab buttons
            buyTabButton?.onClick.AddListener(() => SwitchTab(MarketTab.Buy));
            sellTabButton?.onClick.AddListener(() => SwitchTab(MarketTab.Sell));
            myListingsTabButton?.onClick.AddListener(() => SwitchTab(MarketTab.MyListings));
            historyTabButton?.onClick.AddListener(() => SwitchTab(MarketTab.History));
            
            // Buy tab
            refreshButton?.onClick.AddListener(RefreshListings);
            categoryFilterDropdown?.onValueChanged.AddListener(OnCategoryChanged);
            sortDropdown?.onValueChanged.AddListener(OnSortChanged);
            searchInput?.onValueChanged.AddListener(OnSearchChanged);
            
            // Sell tab
            listItemButton?.onClick.AddListener(OnListItemClicked);
            priceInput?.onValueChanged.AddListener(OnPriceChanged);
            quantityInput?.onValueChanged.AddListener(OnQuantityChanged);
            
            // Purchase dialog
            confirmPurchaseButton?.onClick.AddListener(OnConfirmPurchase);
            cancelPurchaseButton?.onClick.AddListener(ClosePurchaseDialog);
            purchaseQuantitySlider?.onValueChanged.AddListener(OnPurchaseQuantityChanged);
        }
        
        #region Panel Control
        
        public void Show()
        {
            gameObject.SetActive(true);
            SwitchTab(MarketTab.Buy);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        private void SwitchTab(MarketTab tab)
        {
            _currentTab = tab;
            
            // Update tab button visuals
            SetTabButtonState(buyTabButton, tab == MarketTab.Buy);
            SetTabButtonState(sellTabButton, tab == MarketTab.Sell);
            SetTabButtonState(myListingsTabButton, tab == MarketTab.MyListings);
            SetTabButtonState(historyTabButton, tab == MarketTab.History);
            
            // Show appropriate content
            buyListContent?.gameObject.SetActive(tab == MarketTab.Buy);
            inventorySellContent?.transform.parent.gameObject.SetActive(tab == MarketTab.Sell);
            myListingsContent?.gameObject.SetActive(tab == MarketTab.MyListings);
            historyContent?.gameObject.SetActive(tab == MarketTab.History);
            
            // Refresh content
            switch (tab)
            {
                case MarketTab.Buy:
                    RefreshListings();
                    break;
                case MarketTab.Sell:
                    RefreshSellInventory();
                    break;
                case MarketTab.MyListings:
                    RefreshMyListings();
                    break;
                case MarketTab.History:
                    RefreshHistory();
                    break;
            }
        }
        
        private void SetTabButtonState(Button button, bool selected)
        {
            if (button == null) return;
            
            var colors = button.colors;
            colors.normalColor = selected ? new Color(0.3f, 0.6f, 1f) : Color.white;
            button.colors = colors;
        }
        
        #endregion
        
        #region Buy Tab
        
        private async void RefreshListings()
        {
            ClearListingItems();
            
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                
                var query = db.Collection("market_listings")
                    .WhereEqualTo("status", "active")
                    .OrderByDescending("createdAt")
                    .Limit(100);
                    
                // Apply category filter
                if (categoryFilterDropdown != null && categoryFilterDropdown.value > 0)
                {
                    string category = categoryFilterDropdown.options[categoryFilterDropdown.value].text;
                    query = query.WhereEqualTo("category", category);
                }
                
                var snapshot = await query.GetSnapshotAsync();
                
                _currentListings.Clear();
                
                foreach (var doc in snapshot.Documents)
                {
                    var listing = MarketListing.FromFirestore(doc);
                    if (listing != null)
                    {
                        _currentListings.Add(listing);
                    }
                }
                
                // Apply search filter
                string search = searchInput?.text?.ToLower() ?? "";
                if (!string.IsNullOrEmpty(search))
                {
                    _currentListings = _currentListings.FindAll(l =>
                        l.ItemName.ToLower().Contains(search) ||
                        l.SellerName.ToLower().Contains(search));
                }
                
                // Apply sort
                ApplySorting();
                
                // Create listing items
                foreach (var listing in _currentListings)
                {
                    CreateListingItem(listing);
                }
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[Market] Error loading listings: {ex.Message}", ApexLogger.LogCategory.UI);
            }
#else
            // Create mock listings for testing
            CreateMockListings();
#endif
        }
        
        private void CreateMockListings()
        {
            var mockListings = new List<MarketListing>
            {
                new MarketListing { Id = "1", ItemId = "steel_ingot", ItemName = "Steel Ingot", Quantity = 50, PricePerUnit = 100, SellerName = "Player1" },
                new MarketListing { Id = "2", ItemId = "refined_stone", ItemName = "Refined Stone", Quantity = 100, PricePerUnit = 50, SellerName = "Player2" },
                new MarketListing { Id = "3", ItemId = "crystal", ItemName = "Crystal", Quantity = 25, PricePerUnit = 500, SellerName = "Player3" }
            };
            
            _currentListings = mockListings;
            
            foreach (var listing in _currentListings)
            {
                CreateListingItem(listing);
            }
        }
        
        private void CreateListingItem(MarketListing listing)
        {
            if (marketListingPrefab == null || buyListContent == null) return;
            
            var item = Instantiate(marketListingPrefab, buyListContent);
            _listingItems.Add(item);
            
            // Configure display
            var texts = item.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 4)
            {
                texts[0].text = listing.ItemName;
                texts[1].text = $"x{listing.Quantity}";
                texts[2].text = $"{listing.PricePerUnit:N0} each";
                texts[3].text = $"Total: {listing.TotalPrice:N0}";
            }
            
            // Seller info
            if (texts.Length >= 5)
            {
                texts[4].text = $"Seller: {listing.SellerName}";
            }
            
            // Buy button
            var button = item.GetComponentInChildren<Button>();
            if (button != null)
            {
                string listingId = listing.Id;
                button.onClick.AddListener(() => OpenPurchaseDialog(listingId));
            }
        }
        
        private void ClearListingItems()
        {
            foreach (var item in _listingItems)
            {
                if (item != null) Destroy(item);
            }
            _listingItems.Clear();
        }
        
        private void ApplySorting()
        {
            if (sortDropdown == null) return;
            
            switch (sortDropdown.value)
            {
                case 0: // Newest
                    _currentListings.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
                    break;
                case 1: // Price: Low to High
                    _currentListings.Sort((a, b) => a.PricePerUnit.CompareTo(b.PricePerUnit));
                    break;
                case 2: // Price: High to Low
                    _currentListings.Sort((a, b) => b.PricePerUnit.CompareTo(a.PricePerUnit));
                    break;
                case 3: // Quantity
                    _currentListings.Sort((a, b) => b.Quantity.CompareTo(a.Quantity));
                    break;
            }
        }
        
        private void OnCategoryChanged(int value) => RefreshListings();
        private void OnSortChanged(int value) => ApplySorting();
        private void OnSearchChanged(string text) => RefreshListings();
        
        #endregion
        
        #region Purchase Dialog
        
        private void OpenPurchaseDialog(string listingId)
        {
            _selectedListing = _currentListings.Find(l => l.Id == listingId);
            if (_selectedListing == null) return;
            
            purchaseDialog?.SetActive(true);
            
            if (purchaseItemName != null)
                purchaseItemName.text = _selectedListing.ItemName;
                
            if (purchaseQuantitySlider != null)
            {
                purchaseQuantitySlider.minValue = 1;
                purchaseQuantitySlider.maxValue = _selectedListing.Quantity;
                purchaseQuantitySlider.value = _selectedListing.Quantity;
            }
            
            UpdatePurchaseTotal();
        }
        
        private void ClosePurchaseDialog()
        {
            purchaseDialog?.SetActive(false);
            _selectedListing = null;
        }
        
        private void OnPurchaseQuantityChanged(float value)
        {
            UpdatePurchaseTotal();
        }
        
        private void UpdatePurchaseTotal()
        {
            if (_selectedListing == null) return;
            
            int quantity = purchaseQuantitySlider != null 
                ? Mathf.RoundToInt(purchaseQuantitySlider.value) 
                : _selectedListing.Quantity;
                
            long total = (long)quantity * _selectedListing.PricePerUnit;
            
            if (purchaseQuantityText != null)
                purchaseQuantityText.text = $"Quantity: {quantity}";
                
            if (purchaseTotalText != null)
                purchaseTotalText.text = $"Total: {total:N0} Gold";
        }
        
        private async void OnConfirmPurchase()
        {
            if (_selectedListing == null) return;
            
            int quantity = purchaseQuantitySlider != null 
                ? Mathf.RoundToInt(purchaseQuantitySlider.value) 
                : _selectedListing.Quantity;
                
            await ExecutePurchase(_selectedListing.Id, quantity);
            
            ClosePurchaseDialog();
            RefreshListings();
        }
        
        private async Task<bool> ExecutePurchase(string listingId, int quantity)
        {
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
                
                if (string.IsNullOrEmpty(userId)) return false;
                
                // Use transaction for atomic purchase
                return await db.RunTransactionAsync(async transaction =>
                {
                    var listingRef = db.Collection("market_listings").Document(listingId);
                    var listingDoc = await transaction.GetSnapshotAsync(listingRef);
                    
                    if (!listingDoc.Exists) return false;
                    
                    var listing = MarketListing.FromFirestore(listingDoc);
                    if (listing == null || listing.Quantity < quantity) return false;
                    
                    long totalCost = (long)quantity * listing.PricePerUnit;
                    
                    // Update listing
                    int newQuantity = listing.Quantity - quantity;
                    if (newQuantity > 0)
                    {
                        transaction.Update(listingRef, "quantity", newQuantity);
                    }
                    else
                    {
                        transaction.Update(listingRef, new Dictionary<string, object>
                        {
                            { "status", "sold" },
                            { "soldAt", FieldValue.ServerTimestamp }
                        });
                    }
                    
                    // Record transaction
                    var transactionRef = db.Collection("market_transactions").Document();
                    transaction.Set(transactionRef, new Dictionary<string, object>
                    {
                        { "listingId", listingId },
                        { "buyerId", userId },
                        { "sellerId", listing.SellerId },
                        { "itemId", listing.ItemId },
                        { "itemName", listing.ItemName },
                        { "quantity", quantity },
                        { "pricePerUnit", listing.PricePerUnit },
                        { "totalPrice", totalCost },
                        { "timestamp", FieldValue.ServerTimestamp }
                    });
                    
                    // TODO: Transfer resources using cloud function
                    
                    return true;
                });
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[Market] Purchase failed: {ex.Message}", ApexLogger.LogCategory.UI);
                return false;
            }
#else
            await Task.Delay(100);
            ApexLogger.Log($"[Market] Mock purchase: {quantity} of {listingId}", ApexLogger.LogCategory.UI);
            return true;
#endif
        }
        
        #endregion
        
        #region Sell Tab
        
        private void RefreshSellInventory()
        {
            foreach (var item in _inventoryItems)
            {
                if (item != null) Destroy(item);
            }
            _inventoryItems.Clear();
            
            // Get inventory from CraftingSystem
            var craftingSystem = CraftingSystem.Instance;
            if (craftingSystem == null) return;
            
            foreach (var kvp in craftingSystem.Inventory)
            {
                CreateSellInventoryItem(kvp.Key, kvp.Value);
            }
            
            // Clear selection
            _selectedSellItem = null;
            UpdateSellUI();
        }
        
        private void CreateSellInventoryItem(string itemId, int quantity)
        {
            if (inventoryItemPrefab == null || inventorySellContent == null) return;
            
            var item = Instantiate(inventoryItemPrefab, inventorySellContent);
            _inventoryItems.Add(item);
            
            var texts = item.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = FormatItemName(itemId);
                texts[1].text = $"x{quantity}";
            }
            
            var button = item.GetComponent<Button>();
            if (button != null)
            {
                string id = itemId;
                int qty = quantity;
                button.onClick.AddListener(() => SelectSellItem(id, qty));
            }
        }
        
        private void SelectSellItem(string itemId, int maxQuantity)
        {
            _selectedSellItem = itemId;
            _selectedSellQuantity = maxQuantity;
            
            if (selectedItemText != null)
                selectedItemText.text = FormatItemName(itemId);
                
            if (quantityInput != null)
                quantityInput.text = maxQuantity.ToString();
                
            // Get market price suggestion
            _ = UpdateMarketPrice(itemId);
            
            UpdateSellUI();
        }
        
        private async Task UpdateMarketPrice(string itemId)
        {
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                
                // Get recent sales of this item
                var query = db.Collection("market_transactions")
                    .WhereEqualTo("itemId", itemId)
                    .OrderByDescending("timestamp")
                    .Limit(10);
                    
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    long totalPrice = 0;
                    int count = 0;
                    
                    foreach (var doc in snapshot.Documents)
                    {
                        totalPrice += doc.GetValue<long>("pricePerUnit");
                        count++;
                    }
                    
                    long avgPrice = totalPrice / count;
                    
                    if (marketPriceText != null)
                        marketPriceText.text = $"Market Avg: {avgPrice:N0}";
                        
                    if (priceInput != null && string.IsNullOrEmpty(priceInput.text))
                        priceInput.text = avgPrice.ToString();
                }
                else
                {
                    if (marketPriceText != null)
                        marketPriceText.text = "No recent sales";
                }
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[Market] Error getting market price: {ex.Message}", ApexLogger.LogCategory.UI);
            }
#else
            await Task.Delay(100);
            if (marketPriceText != null)
                marketPriceText.text = "Market Avg: 100";
#endif
        }
        
        private void OnPriceChanged(string text)
        {
            UpdateSellUI();
        }
        
        private void OnQuantityChanged(string text)
        {
            UpdateSellUI();
        }
        
        private void UpdateSellUI()
        {
            bool canSell = !string.IsNullOrEmpty(_selectedSellItem);
            
            if (int.TryParse(priceInput?.text, out int price) &&
                int.TryParse(quantityInput?.text, out int quantity))
            {
                long total = (long)price * quantity;
                long fee = (long)(total * LISTING_FEE_PERCENT);
                
                if (listingFeeText != null)
                    listingFeeText.text = $"Listing Fee: {fee:N0} ({LISTING_FEE_PERCENT * 100}%)";
                    
                canSell = canSell && price > 0 && quantity > 0 && quantity <= _selectedSellQuantity;
            }
            
            if (listItemButton != null)
                listItemButton.interactable = canSell;
        }
        
        private async void OnListItemClicked()
        {
            if (string.IsNullOrEmpty(_selectedSellItem)) return;
            
            if (!int.TryParse(priceInput?.text, out int price) ||
                !int.TryParse(quantityInput?.text, out int quantity))
            {
                return;
            }
            
            await CreateListing(_selectedSellItem, quantity, price);
            
            RefreshSellInventory();
        }
        
        private async Task<bool> CreateListing(string itemId, int quantity, int pricePerUnit)
        {
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
                
                if (string.IsNullOrEmpty(userId)) return false;
                
                // Get user info
                var userDoc = await db.Collection("users").Document(userId).GetSnapshotAsync();
                string userName = userDoc.GetValue<string>("displayName") ?? "Unknown";
                
                // Deduct listing fee
                long fee = (long)(pricePerUnit * quantity * LISTING_FEE_PERCENT);
                
                // Create listing
                await db.Collection("market_listings").AddAsync(new Dictionary<string, object>
                {
                    { "sellerId", userId },
                    { "sellerName", userName },
                    { "itemId", itemId },
                    { "itemName", FormatItemName(itemId) },
                    { "quantity", quantity },
                    { "pricePerUnit", pricePerUnit },
                    { "category", GetItemCategory(itemId) },
                    { "status", "active" },
                    { "fee", fee },
                    { "createdAt", FieldValue.ServerTimestamp }
                });
                
                // Deduct items from inventory
                var craftingSystem = CraftingSystem.Instance;
                craftingSystem?.ConsumeItem(itemId, quantity);
                
                ApexLogger.Log($"[Market] Listed {quantity}x {itemId} at {pricePerUnit} each", ApexLogger.LogCategory.UI);
                return true;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[Market] Error creating listing: {ex.Message}", ApexLogger.LogCategory.UI);
                return false;
            }
#else
            await Task.Delay(100);
            ApexLogger.Log($"[Market] Mock listing: {quantity}x {itemId} at {pricePerUnit}", ApexLogger.LogCategory.UI);
            return true;
#endif
        }
        
        #endregion
        
        #region My Listings Tab
        
        private async void RefreshMyListings()
        {
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
                
                if (string.IsNullOrEmpty(userId)) return;
                
                var query = db.Collection("market_listings")
                    .WhereEqualTo("sellerId", userId)
                    .WhereEqualTo("status", "active")
                    .OrderByDescending("createdAt");
                    
                var snapshot = await query.GetSnapshotAsync();
                
                _myListings.Clear();
                
                foreach (var doc in snapshot.Documents)
                {
                    var listing = MarketListing.FromFirestore(doc);
                    if (listing != null)
                    {
                        _myListings.Add(listing);
                    }
                }
                
                // Update UI
                if (totalListingsText != null)
                    totalListingsText.text = $"Active Listings: {_myListings.Count}";
                    
                long pendingEarnings = 0;
                foreach (var listing in _myListings)
                {
                    pendingEarnings += listing.TotalPrice;
                }
                
                if (pendingEarningsText != null)
                    pendingEarningsText.text = $"Potential Earnings: {pendingEarnings:N0}";
                
                // Create listing items
                ClearListingItems();
                foreach (var listing in _myListings)
                {
                    CreateMyListingItem(listing);
                }
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[Market] Error loading my listings: {ex.Message}", ApexLogger.LogCategory.UI);
            }
#endif
        }
        
        private void CreateMyListingItem(MarketListing listing)
        {
            if (marketListingPrefab == null || myListingsContent == null) return;
            
            var item = Instantiate(marketListingPrefab, myListingsContent);
            _listingItems.Add(item);
            
            var texts = item.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 4)
            {
                texts[0].text = listing.ItemName;
                texts[1].text = $"x{listing.Quantity}";
                texts[2].text = $"{listing.PricePerUnit:N0} each";
                texts[3].text = $"Total: {listing.TotalPrice:N0}";
            }
            
            // Cancel button
            var buttons = item.GetComponentsInChildren<Button>();
            if (buttons.Length > 0)
            {
                buttons[0].GetComponentInChildren<TMP_Text>().text = "Cancel";
                string listingId = listing.Id;
                buttons[0].onClick.AddListener(async () =>
                {
                    await CancelListing(listingId);
                    RefreshMyListings();
                });
            }
        }
        
        private async Task<bool> CancelListing(string listingId)
        {
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                
                // Get listing details
                var doc = await db.Collection("market_listings").Document(listingId).GetSnapshotAsync();
                var listing = MarketListing.FromFirestore(doc);
                
                if (listing == null) return false;
                
                // Update status
                await db.Collection("market_listings").Document(listingId).UpdateAsync(new Dictionary<string, object>
                {
                    { "status", "cancelled" },
                    { "cancelledAt", FieldValue.ServerTimestamp }
                });
                
                // Return items to inventory
                var craftingSystem = CraftingSystem.Instance;
                craftingSystem?.AddItem(listing.ItemId, listing.Quantity);
                
                ApexLogger.Log($"[Market] Cancelled listing {listingId}", ApexLogger.LogCategory.UI);
                return true;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[Market] Error cancelling listing: {ex.Message}", ApexLogger.LogCategory.UI);
                return false;
            }
#else
            await Task.Delay(100);
            return true;
#endif
        }
        
        #endregion
        
        #region History Tab
        
        private async void RefreshHistory()
        {
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
                
                if (string.IsNullOrEmpty(userId)) return;
                
                // Get transactions where user was buyer or seller
                var buyerQuery = db.Collection("market_transactions")
                    .WhereEqualTo("buyerId", userId)
                    .OrderByDescending("timestamp")
                    .Limit(50);
                    
                var sellerQuery = db.Collection("market_transactions")
                    .WhereEqualTo("sellerId", userId)
                    .OrderByDescending("timestamp")
                    .Limit(50);
                
                var buyerSnapshot = await buyerQuery.GetSnapshotAsync();
                var sellerSnapshot = await sellerQuery.GetSnapshotAsync();
                
                _transactionHistory.Clear();
                
                foreach (var doc in buyerSnapshot.Documents)
                {
                    var tx = MarketTransaction.FromFirestore(doc, true);
                    if (tx != null) _transactionHistory.Add(tx);
                }
                
                foreach (var doc in sellerSnapshot.Documents)
                {
                    var tx = MarketTransaction.FromFirestore(doc, false);
                    if (tx != null) _transactionHistory.Add(tx);
                }
                
                // Sort by timestamp
                _transactionHistory.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
                
                // Take top 50
                if (_transactionHistory.Count > 50)
                {
                    _transactionHistory = _transactionHistory.GetRange(0, 50);
                }
                
                // Calculate totals
                long totalBought = 0;
                long totalSold = 0;
                
                foreach (var tx in _transactionHistory)
                {
                    if (tx.WasBuyer)
                        totalBought += tx.TotalPrice;
                    else
                        totalSold += tx.TotalPrice;
                }
                
                if (totalTradedText != null)
                    totalTradedText.text = $"Bought: {totalBought:N0} | Sold: {totalSold:N0}";
                
                // Create history items
                foreach (var item in _historyItems)
                {
                    if (item != null) Destroy(item);
                }
                _historyItems.Clear();
                
                foreach (var tx in _transactionHistory)
                {
                    CreateHistoryItem(tx);
                }
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[Market] Error loading history: {ex.Message}", ApexLogger.LogCategory.UI);
            }
#endif
        }
        
        private void CreateHistoryItem(MarketTransaction transaction)
        {
            if (historyItemPrefab == null || historyContent == null) return;
            
            var item = Instantiate(historyItemPrefab, historyContent);
            _historyItems.Add(item);
            
            var texts = item.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 3)
            {
                string action = transaction.WasBuyer ? "Bought" : "Sold";
                texts[0].text = $"{action} {transaction.Quantity}x {transaction.ItemName}";
                texts[1].text = $"{transaction.TotalPrice:N0} Gold";
                texts[2].text = transaction.Timestamp.ToString("MMM dd, HH:mm");
                
                texts[0].color = transaction.WasBuyer ? lossColor : profitColor;
            }
        }
        
        #endregion
        
        #region Utility
        
        private string FormatItemName(string itemId)
        {
            var words = itemId.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }
            return string.Join(" ", words);
        }
        
        private string GetItemCategory(string itemId)
        {
            // Determine category based on item
            if (itemId.Contains("ingot") || itemId.Contains("stone") || itemId.Contains("wood"))
                return "Materials";
            if (itemId.Contains("wall") || itemId.Contains("tower") || itemId.Contains("arrow"))
                return "Defense";
            if (itemId.Contains("bomb") || itemId.Contains("banner"))
                return "Offense";
            if (itemId.Contains("potion") || itemId.Contains("elixir"))
                return "Consumables";
            return "Other";
        }
        
        #endregion
    }
    
    #region Data Classes
    
    public enum MarketTab
    {
        Buy,
        Sell,
        MyListings,
        History
    }
    
    [Serializable]
    public class MarketListing
    {
        public string Id;
        public string SellerId;
        public string SellerName;
        public string ItemId;
        public string ItemName;
        public string Category;
        public int Quantity;
        public int PricePerUnit;
        public string Status;
        public DateTime CreatedAt;
        
        public long TotalPrice => (long)Quantity * PricePerUnit;
        
#if FIREBASE_ENABLED
        public static MarketListing FromFirestore(DocumentSnapshot doc)
        {
            try
            {
                return new MarketListing
                {
                    Id = doc.Id,
                    SellerId = doc.GetValue<string>("sellerId"),
                    SellerName = doc.GetValue<string>("sellerName"),
                    ItemId = doc.GetValue<string>("itemId"),
                    ItemName = doc.GetValue<string>("itemName"),
                    Category = doc.GetValue<string>("category"),
                    Quantity = doc.GetValue<int>("quantity"),
                    PricePerUnit = doc.GetValue<int>("pricePerUnit"),
                    Status = doc.GetValue<string>("status"),
                    CreatedAt = doc.ContainsField("createdAt") 
                        ? doc.GetValue<Timestamp>("createdAt").ToDateTime() 
                        : DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[MarketListing] Parse error: {ex.Message}", ApexLogger.LogCategory.UI);
                return null;
            }
        }
#endif
    }
    
    [Serializable]
    public class MarketTransaction
    {
        public string Id;
        public string ItemId;
        public string ItemName;
        public int Quantity;
        public int PricePerUnit;
        public long TotalPrice;
        public DateTime Timestamp;
        public bool WasBuyer;
        
#if FIREBASE_ENABLED
        public static MarketTransaction FromFirestore(DocumentSnapshot doc, bool wasBuyer)
        {
            try
            {
                return new MarketTransaction
                {
                    Id = doc.Id,
                    ItemId = doc.GetValue<string>("itemId"),
                    ItemName = doc.GetValue<string>("itemName"),
                    Quantity = doc.GetValue<int>("quantity"),
                    PricePerUnit = doc.GetValue<int>("pricePerUnit"),
                    TotalPrice = doc.GetValue<long>("totalPrice"),
                    Timestamp = doc.ContainsField("timestamp") 
                        ? doc.GetValue<Timestamp>("timestamp").ToDateTime() 
                        : DateTime.UtcNow,
                    WasBuyer = wasBuyer
                };
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[MarketTransaction] Parse error: {ex.Message}", ApexLogger.LogCategory.UI);
                return null;
            }
        }
#endif
    }
    
    #endregion
}
