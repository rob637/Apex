using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.IAP;

namespace ApexCitadels.UI
{
    /// <summary>
    /// Store tab types
    /// </summary>
    public enum StoreTab
    {
        Featured,
        Currency,
        SeasonPass,
        Packs,
        Subscription
    }

    /// <summary>
    /// In-App Purchase Store UI
    /// Displays products and handles purchase flow
    /// </summary>
    public class IAPStoreUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject storePanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private TextMeshProUGUI errorText;

        [Header("Tabs")]
        [SerializeField] private Button featuredTab;
        [SerializeField] private Button currencyTab;
        [SerializeField] private Button seasonPassTab;
        [SerializeField] private Button packsTab;
        [SerializeField] private Button subscriptionTab;

        [Header("Content Areas")]
        [SerializeField] private GameObject featuredContent;
        [SerializeField] private GameObject currencyContent;
        [SerializeField] private GameObject seasonPassContent;
        [SerializeField] private GameObject packsContent;
        [SerializeField] private GameObject subscriptionContent;

        [Header("Product Containers")]
        [SerializeField] private Transform featuredProductsContainer;
        [SerializeField] private Transform currencyProductsContainer;
        [SerializeField] private Transform packProductsContainer;
        [SerializeField] private Transform subscriptionProductsContainer;

        [Header("Prefabs")]
        [SerializeField] private GameObject productItemPrefab;
        [SerializeField] private GameObject featuredProductPrefab;
        [SerializeField] private GameObject seasonPassProductPrefab;
        [SerializeField] private GameObject subscriptionProductPrefab;

        [Header("Purchase Modal")]
        [SerializeField] private GameObject purchaseConfirmModal;
        [SerializeField] private TextMeshProUGUI confirmProductName;
        [SerializeField] private TextMeshProUGUI confirmProductPrice;
        [SerializeField] private TextMeshProUGUI confirmProductDescription;
        [SerializeField] private Button confirmPurchaseButton;
        [SerializeField] private Button cancelPurchaseButton;

        [Header("Processing Modal")]
        [SerializeField] private GameObject processingModal;

        [Header("Success Modal")]
        [SerializeField] private GameObject successModal;
        [SerializeField] private TextMeshProUGUI successTitle;
        [SerializeField] private TextMeshProUGUI successMessage;
        [SerializeField] private Transform rewardsContainer;
        [SerializeField] private GameObject rewardItemPrefab;
        [SerializeField] private Button successCloseButton;

        [Header("Season Pass Display")]
        [SerializeField] private TextMeshProUGUI seasonPassStatus;
        [SerializeField] private GameObject premiumBadge;
        [SerializeField] private Button buySeasonPassButton;
        [SerializeField] private Button buySeasonPassBundleButton;

        [Header("VIP Display")]
        [SerializeField] private TextMeshProUGUI vipStatus;
        [SerializeField] private TextMeshProUGUI vipExpiryText;
        [SerializeField] private GameObject vipBadge;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI playerGemsText;
        [SerializeField] private TextMeshProUGUI playerCoinsText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button restoreButton;

        private StoreTab _currentTab = StoreTab.Featured;
        private IAPProduct _selectedProduct;
        private Dictionary<string, GameObject> _productItems = new Dictionary<string, GameObject>();

        private void Start()
        {
            SetupEventListeners();
            SetupTabs();

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseStore);

            if (restoreButton != null)
                restoreButton.onClick.AddListener(OnRestorePurchases);

            // Subscribe to IAP events
            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.OnInitialized += OnIAPInitialized;
                IAPManager.Instance.OnProductsLoaded += OnProductsLoaded;
                IAPManager.Instance.OnPurchaseStarted += OnPurchaseStarted;
                IAPManager.Instance.OnPurchaseCompleted += OnPurchaseCompleted;
                IAPManager.Instance.OnPurchaseFailed += OnPurchaseFailed;
                IAPManager.Instance.OnRestoreCompleted += OnRestoreCompleted;
                IAPManager.Instance.OnEntitlementsUpdated += OnEntitlementsUpdated;

                if (IAPManager.Instance.IsInitialized)
                {
                    OnIAPInitialized();
                }
            }

            // Hide store by default
            if (storePanel != null)
                storePanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.OnInitialized -= OnIAPInitialized;
                IAPManager.Instance.OnProductsLoaded -= OnProductsLoaded;
                IAPManager.Instance.OnPurchaseStarted -= OnPurchaseStarted;
                IAPManager.Instance.OnPurchaseCompleted -= OnPurchaseCompleted;
                IAPManager.Instance.OnPurchaseFailed -= OnPurchaseFailed;
                IAPManager.Instance.OnRestoreCompleted -= OnRestoreCompleted;
                IAPManager.Instance.OnEntitlementsUpdated -= OnEntitlementsUpdated;
            }
        }

        private void SetupEventListeners()
        {
            if (confirmPurchaseButton != null)
                confirmPurchaseButton.onClick.AddListener(ConfirmPurchase);

            if (cancelPurchaseButton != null)
                cancelPurchaseButton.onClick.AddListener(CancelPurchase);

            if (successCloseButton != null)
                successCloseButton.onClick.AddListener(CloseSuccessModal);

            if (buySeasonPassButton != null)
                buySeasonPassButton.onClick.AddListener(() => SelectProduct("season_pass_premium"));

            if (buySeasonPassBundleButton != null)
                buySeasonPassBundleButton.onClick.AddListener(() => SelectProduct("season_pass_bundle"));
        }

        private void SetupTabs()
        {
            if (featuredTab != null)
                featuredTab.onClick.AddListener(() => SwitchTab(StoreTab.Featured));

            if (currencyTab != null)
                currencyTab.onClick.AddListener(() => SwitchTab(StoreTab.Currency));

            if (seasonPassTab != null)
                seasonPassTab.onClick.AddListener(() => SwitchTab(StoreTab.SeasonPass));

            if (packsTab != null)
                packsTab.onClick.AddListener(() => SwitchTab(StoreTab.Packs));

            if (subscriptionTab != null)
                subscriptionTab.onClick.AddListener(() => SwitchTab(StoreTab.Subscription));
        }

        /// <summary>
        /// Open the store
        /// </summary>
        public void OpenStore()
        {
            if (storePanel != null)
                storePanel.SetActive(true);

            UpdateCurrencyDisplay();
            SwitchTab(StoreTab.Featured);

            if (!IAPManager.Instance.IsInitialized)
            {
                ShowLoading();
            }
        }

        /// <summary>
        /// Close the store
        /// </summary>
        public void CloseStore()
        {
            if (storePanel != null)
                storePanel.SetActive(false);
        }

        /// <summary>
        /// Switch to a tab
        /// </summary>
        public void SwitchTab(StoreTab tab)
        {
            _currentTab = tab;

            // Hide all content
            if (featuredContent != null) featuredContent.SetActive(false);
            if (currencyContent != null) currencyContent.SetActive(false);
            if (seasonPassContent != null) seasonPassContent.SetActive(false);
            if (packsContent != null) packsContent.SetActive(false);
            if (subscriptionContent != null) subscriptionContent.SetActive(false);

            // Update tab visuals
            UpdateTabVisuals();

            // Show selected content
            switch (tab)
            {
                case StoreTab.Featured:
                    if (featuredContent != null) featuredContent.SetActive(true);
                    break;
                case StoreTab.Currency:
                    if (currencyContent != null) currencyContent.SetActive(true);
                    break;
                case StoreTab.SeasonPass:
                    if (seasonPassContent != null) seasonPassContent.SetActive(true);
                    break;
                case StoreTab.Packs:
                    if (packsContent != null) packsContent.SetActive(true);
                    break;
                case StoreTab.Subscription:
                    if (subscriptionContent != null) subscriptionContent.SetActive(true);
                    break;
            }
        }

        private void UpdateTabVisuals()
        {
            // Update tab button states (selected/unselected)
            SetTabSelected(featuredTab, _currentTab == StoreTab.Featured);
            SetTabSelected(currencyTab, _currentTab == StoreTab.Currency);
            SetTabSelected(seasonPassTab, _currentTab == StoreTab.SeasonPass);
            SetTabSelected(packsTab, _currentTab == StoreTab.Packs);
            SetTabSelected(subscriptionTab, _currentTab == StoreTab.Subscription);
        }

        private void SetTabSelected(Button tab, bool selected)
        {
            if (tab == null) return;

            var colors = tab.colors;
            colors.normalColor = selected ? new Color(0.2f, 0.6f, 1f) : Color.white;
            tab.colors = colors;
        }

        private void OnIAPInitialized()
        {
            HideLoading();
            PopulateProducts();
            UpdateEntitlementDisplay();
        }

        private void OnProductsLoaded(List<IAPProduct> products)
        {
            PopulateProducts();
        }

        private void PopulateProducts()
        {
            // Clear existing items
            ClearContainer(featuredProductsContainer);
            ClearContainer(currencyProductsContainer);
            ClearContainer(packProductsContainer);
            ClearContainer(subscriptionProductsContainer);
            _productItems.Clear();

            var products = IAPManager.Instance.Products;

            // Featured items (special offers + best sellers)
            var featured = new List<IAPProduct>();
            featured.AddRange(IAPManager.Instance.GetSpecialOffers());
            featured.AddRange(IAPManager.Instance.GetStarterPacks());

            foreach (var product in featured)
            {
                CreateProductItem(product, featuredProductsContainer, featuredProductPrefab ?? productItemPrefab);
            }

            // Currency (gems)
            foreach (var product in IAPManager.Instance.GetGemPacks())
            {
                CreateProductItem(product, currencyProductsContainer, productItemPrefab);
            }

            // Packs (starter, resource)
            var packs = new List<IAPProduct>();
            packs.AddRange(IAPManager.Instance.GetProductsByCategory(IAPProductCategory.StarterPack));
            packs.AddRange(IAPManager.Instance.GetProductsByCategory(IAPProductCategory.ResourcePack));

            foreach (var product in packs)
            {
                CreateProductItem(product, packProductsContainer, productItemPrefab);
            }

            // Subscriptions
            foreach (var product in IAPManager.Instance.GetProductsByCategory(IAPProductCategory.Subscription))
            {
                CreateProductItem(product, subscriptionProductsContainer, subscriptionProductPrefab ?? productItemPrefab);
            }
        }

        private void CreateProductItem(IAPProduct product, Transform container, GameObject prefab)
        {
            if (container == null || prefab == null) return;

            var item = Instantiate(prefab, container);
            _productItems[product.Id] = item;

            // Setup product display
            var nameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var descText = item.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            var priceText = item.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
            var buyButton = item.transform.Find("BuyButton")?.GetComponent<Button>();
            var valueText = item.transform.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
            var bonusText = item.transform.Find("BonusText")?.GetComponent<TextMeshProUGUI>();
            var iconImage = item.transform.Find("Icon")?.GetComponent<Image>();

            if (nameText != null) nameText.text = product.Name;
            if (descText != null) descText.text = product.Description;
            if (priceText != null) priceText.text = product.LocalizedPrice ?? $"${product.PriceUSD:F2}";

            // Calculate value/bonus for gem packs
            if (product.Category == IAPProductCategory.Currency && product.Rewards.Count > 0)
            {
                var gemReward = product.Rewards.Find(r => r.Type == "gems");
                if (gemReward != null && valueText != null)
                {
                    int gemsPerDollar = (int)(gemReward.Amount / product.PriceUSD);
                    valueText.text = $"{gemsPerDollar} gems/$";

                    // Calculate bonus percentage vs base rate (100 gems/$1)
                    if (bonusText != null && gemsPerDollar > 100)
                    {
                        int bonus = (gemsPerDollar - 100) * 100 / 100;
                        bonusText.text = $"+{bonus}% BONUS";
                        bonusText.gameObject.SetActive(true);
                    }
                }
            }

            // Check if already purchased (for one-time purchases)
            if (product.Type == IAPProductType.NonConsumable && IAPManager.Instance.HasPurchased(product.Id))
            {
                if (buyButton != null)
                {
                    buyButton.interactable = false;
                    var buttonText = buyButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null) buttonText.text = "OWNED";
                }
            }
            else if (buyButton != null)
            {
                buyButton.onClick.AddListener(() => SelectProduct(product.Id));
            }
        }

        private void ClearContainer(Transform container)
        {
            if (container == null) return;

            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        private void SelectProduct(string productId)
        {
            _selectedProduct = IAPManager.Instance.GetProduct(productId);
            if (_selectedProduct == null) return;

            ShowPurchaseConfirm();
        }

        private void ShowPurchaseConfirm()
        {
            if (purchaseConfirmModal == null || _selectedProduct == null) return;

            if (confirmProductName != null)
                confirmProductName.text = _selectedProduct.Name;

            if (confirmProductPrice != null)
                confirmProductPrice.text = _selectedProduct.LocalizedPrice ?? $"${_selectedProduct.PriceUSD:F2}";

            if (confirmProductDescription != null)
            {
                string desc = _selectedProduct.Description;

                // Add reward details
                if (_selectedProduct.Rewards.Count > 0)
                {
                    desc += "\n\nYou will receive:";
                    foreach (var reward in _selectedProduct.Rewards)
                    {
                        desc += $"\n• {reward.Amount:N0} {FormatRewardType(reward.Type)}";
                    }
                }

                confirmProductDescription.text = desc;
            }

            purchaseConfirmModal.SetActive(true);
        }

        private void ConfirmPurchase()
        {
            if (_selectedProduct == null) return;

            HidePurchaseConfirm();
            IAPManager.Instance.Purchase(_selectedProduct.ProductId);
        }

        private void CancelPurchase()
        {
            _selectedProduct = null;
            HidePurchaseConfirm();
        }

        private void HidePurchaseConfirm()
        {
            if (purchaseConfirmModal != null)
                purchaseConfirmModal.SetActive(false);
        }

        private void OnPurchaseStarted(IAPProduct product)
        {
            ShowProcessing();
        }

        private void OnPurchaseCompleted(PurchaseResult result)
        {
            HideProcessing();
            ShowSuccess(result);
            UpdateCurrencyDisplay();
            UpdateEntitlementDisplay();
        }

        private void OnPurchaseFailed(IAPProduct product, string error)
        {
            HideProcessing();
            ShowError($"Purchase failed: {error}");
        }

        private void ShowSuccess(PurchaseResult result)
        {
            if (successModal == null) return;

            if (successTitle != null)
                successTitle.text = "Purchase Complete!";

            if (successMessage != null)
            {
                if (result.AlreadyProcessed)
                    successMessage.text = "This purchase was already processed.";
                else
                    successMessage.text = "Thank you for your purchase!";
            }

            // Show rewards
            if (rewardsContainer != null && rewardItemPrefab != null && result.Rewards != null)
            {
                ClearContainer(rewardsContainer);

                foreach (var reward in result.Rewards)
                {
                    var item = Instantiate(rewardItemPrefab, rewardsContainer);
                    var text = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        text.text = $"+{reward.Amount:N0} {FormatRewardType(reward.Type)}";
                    }
                }
            }

            successModal.SetActive(true);
        }

        private void CloseSuccessModal()
        {
            if (successModal != null)
                successModal.SetActive(false);
        }

        private void OnRestorePurchases()
        {
            ShowProcessing();
            IAPManager.Instance.RestorePurchases();
        }

        private void OnRestoreCompleted(int count)
        {
            HideProcessing();

            if (count >= 0)
            {
                ShowMessage("Restore Complete", $"Restored {count} purchase(s).");
            }
            else
            {
                ShowError("Failed to restore purchases. Please try again.");
            }
        }

        private void OnEntitlementsUpdated()
        {
            UpdateEntitlementDisplay();
            PopulateProducts(); // Refresh to update owned states
        }

        private void UpdateEntitlementDisplay()
        {
            // Premium pass
            if (premiumBadge != null)
                premiumBadge.SetActive(IAPManager.Instance.HasPremiumPass);

            if (seasonPassStatus != null)
            {
                seasonPassStatus.text = IAPManager.Instance.HasPremiumPass 
                    ? "PREMIUM ACTIVE" 
                    : "FREE";
                seasonPassStatus.color = IAPManager.Instance.HasPremiumPass 
                    ? new Color(1f, 0.8f, 0f) 
                    : Color.white;
            }

            if (buySeasonPassButton != null)
                buySeasonPassButton.gameObject.SetActive(!IAPManager.Instance.HasPremiumPass);

            if (buySeasonPassBundleButton != null)
                buySeasonPassBundleButton.gameObject.SetActive(!IAPManager.Instance.HasPremiumPass);

            // VIP subscription
            if (vipBadge != null)
                vipBadge.SetActive(IAPManager.Instance.HasVIP);

            if (vipStatus != null)
            {
                vipStatus.text = IAPManager.Instance.HasVIP ? "VIP ACTIVE" : "Not Subscribed";
            }
        }

        private void UpdateCurrencyDisplay()
        {
            // Get currency from ResourceManager or PlayerManager
            var resourceManager = Resources.ResourceManager.Instance;
            if (resourceManager != null)
            {
                if (playerGemsText != null)
                    playerGemsText.text = resourceManager.GetResource("gems").ToString("N0");

                if (playerCoinsText != null)
                    playerCoinsText.text = resourceManager.GetResource("coins").ToString("N0");
            }
        }

        private void ShowLoading()
        {
            if (loadingPanel != null) loadingPanel.SetActive(true);
            if (errorPanel != null) errorPanel.SetActive(false);
        }

        private void HideLoading()
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }

        private void ShowProcessing()
        {
            if (processingModal != null) processingModal.SetActive(true);
        }

        private void HideProcessing()
        {
            if (processingModal != null) processingModal.SetActive(false);
        }

        private void ShowError(string message)
        {
            if (errorPanel != null)
            {
                errorPanel.SetActive(true);
                if (errorText != null) errorText.text = message;
            }
        }

        private void ShowMessage(string title, string message)
        {
            // Use success modal for general messages
            if (successModal != null)
            {
                if (successTitle != null) successTitle.text = title;
                if (successMessage != null) successMessage.text = message;
                if (rewardsContainer != null) ClearContainer(rewardsContainer);
                successModal.SetActive(true);
            }
        }

        private string FormatRewardType(string type)
        {
            switch (type.ToLower())
            {
                case "gems": return "Gems [G]";
                case "coins": return "Coins [$]";
                case "stone": return "Stone [Q]";
                case "wood": return "Wood [W]";
                case "metal": return "Metal [P]";
                case "crystal": return "Crystals 💠";
                case "xp": return "XP [*]";
                case "season_xp": return "Season XP [*]";
                case "premium_pass": return "Premium Pass 👑";
                case "cosmetic": return "Cosmetic Item 🎨";
                case "chest": return "Chest [B]";
                case "boost": return "Boost [!]";
                default: return type;
            }
        }
    }
}
