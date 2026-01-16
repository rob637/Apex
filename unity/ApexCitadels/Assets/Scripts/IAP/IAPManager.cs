using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#if FIREBASE_ENABLED
using Firebase.Functions;
#endif
using Newtonsoft.Json;

namespace ApexCitadels.IAP
{
    /// <summary>
    /// Product types
    /// </summary>
    public enum IAPProductType
    {
        Consumable,
        NonConsumable,
        Subscription
    }

    /// <summary>
    /// Product categories for UI organization
    /// </summary>
    public enum IAPProductCategory
    {
        Currency,
        SeasonPass,
        StarterPack,
        ResourcePack,
        Cosmetic,
        SpecialOffer,
        Subscription
    }

    /// <summary>
    /// Product reward data
    /// </summary>
    [Serializable]
    public class IAPReward
    {
        public string Type;
        public string ItemId;
        public int Amount;
    }

    /// <summary>
    /// Product data from server
    /// </summary>
    [Serializable]
    public class IAPProduct
    {
        public string Id;
        public string StoreProductId;
        public string Name;
        public string Description;
        public IAPProductType Type;
        public IAPProductCategory Category;
        public decimal PriceUSD;
        public List<IAPReward> Rewards;
        public bool IsActive;
        public int SortOrder;
        public Dictionary<string, object> Metadata;

        // Runtime data from store
        public string LocalizedPrice;
        public string CurrencyCode;
        public bool IsAvailable;
    }

    /// <summary>
    /// Purchase result
    /// </summary>
    [Serializable]
    public class PurchaseResult
    {
        public bool Success;
        public string PurchaseId;
        public List<IAPReward> Rewards;
        public string ErrorMessage;
        public bool AlreadyProcessed;
    }

    /// <summary>
    /// Entitlement data
    /// </summary>
    [Serializable]
    public class Entitlement
    {
        public string Type;
        public DateTime GrantedAt;
        public DateTime? ExpiresAt;
        public bool IsActive;
        public string Source;
    }

    /// <summary>
    /// Complete In-App Purchase Manager
    /// Handles Unity IAP integration with server-side validation
    /// </summary>
    public class IAPManager : MonoBehaviour, IDetailedStoreListener
    {
        public static IAPManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool useFakeStore = false; // For testing

        // Events
        public event Action OnInitialized;
        public event Action<string> OnInitializationFailed;
        public event Action<IAPProduct> OnPurchaseStarted;
        public event Action<PurchaseResult> OnPurchaseCompleted;
        public event Action<IAPProduct, string> OnPurchaseFailed;
        public event Action<List<IAPProduct>> OnProductsLoaded;
        public event Action<int> OnRestoreCompleted;
        public event Action OnEntitlementsUpdated;

        // State
        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;
#if FIREBASE_ENABLED
        private FirebaseFunctions _functions;
#endif

        private List<IAPProduct> _products = new List<IAPProduct>();
        private Dictionary<string, Entitlement> _entitlements = new Dictionary<string, Entitlement>();
        private IAPProduct _pendingPurchase;
        private bool _isInitialized;
        private bool _isRestoring;

        public bool IsInitialized => _isInitialized;
        public List<IAPProduct> Products => _products;
        public bool HasPremiumPass => _entitlements.ContainsKey("premium_pass") && _entitlements["premium_pass"].IsActive;
        public bool HasVIP => _entitlements.ContainsKey("vip_subscription") && 
                              _entitlements["vip_subscription"].IsActive &&
                              (_entitlements["vip_subscription"].ExpiresAt == null || 
                               _entitlements["vip_subscription"].ExpiresAt > DateTime.UtcNow);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

#if FIREBASE_ENABLED
        private void Start()
        {
            _functions = FirebaseFunctions.DefaultInstance;
            LoadProductCatalog();
        }

        /// <summary>
        /// Load product catalog from server and initialize Unity IAP
        /// </summary>
        public async void LoadProductCatalog()
        {
            try
            {
                Log("Loading product catalog from server...");

                var callable = _functions.GetHttpsCallable("getProductCatalog");
                var result = await callable.CallAsync(null);

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                var productsJson = JsonConvert.SerializeObject(response["products"]);
                _products = JsonConvert.DeserializeObject<List<IAPProduct>>(productsJson);

                Log($"Loaded {_products.Count} products from server");

                // Initialize Unity IAP with these products
                InitializeUnityIAP();

                OnProductsLoaded?.Invoke(_products);
            }
            catch (Exception e)
            {
                LogError($"Failed to load product catalog: {e.Message}");
                OnInitializationFailed?.Invoke(e.Message);
            }
        }
#else
        private void Start()
        {
            Debug.LogWarning("[IAPManager] Firebase SDK not installed. Running in stub mode.");
            InitializeUnityIAP();
        }

        public void LoadProductCatalog()
        {
            Debug.LogWarning("[IAPManager] Firebase SDK not installed. LoadProductCatalog is a stub.");
            InitializeUnityIAP();
        }
#endif

        /// <summary>
        /// Initialize Unity IAP with product catalog
        /// </summary>
        private void InitializeUnityIAP()
        {
            if (_products.Count == 0)
            {
                LogError("No products to initialize");
                return;
            }

            var builder = ConfigurationBuilder.Instance(
                useFakeStore ? StandardPurchasingModule.Instance(FakeStoreUIMode.StandardUser) 
                             : StandardPurchasingModule.Instance()
            );

            foreach (var product in _products)
            {
                ProductType unityProductType;
                switch (product.Type)
                {
                    case IAPProductType.Consumable:
                        unityProductType = ProductType.Consumable;
                        break;
                    case IAPProductType.Subscription:
                        unityProductType = ProductType.Subscription;
                        break;
                    default:
                        unityProductType = ProductType.NonConsumable;
                        break;
                }

                builder.AddProduct(product.StoreProductId, unityProductType, new IDs
                {
                    { product.StoreProductId, AppleAppStore.Name },
                    { product.StoreProductId, GooglePlay.Name }
                });

                Log($"Added product: {product.StoreProductId} ({unityProductType})");
            }

            UnityPurchasing.Initialize(this, builder);
        }

        #region IDetailedStoreListener Implementation

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Log("Unity IAP initialized successfully");

            _storeController = controller;
            _extensionProvider = extensions;
            _isInitialized = true;

            // Update products with store prices
            foreach (var product in _products)
            {
                var storeProduct = controller.products.WithID(product.StoreProductId);
                if (storeProduct != null && storeProduct.availableToPurchase)
                {
                    product.LocalizedPrice = storeProduct.metadata.localizedPriceString;
                    product.CurrencyCode = storeProduct.metadata.isoCurrencyCode;
                    product.IsAvailable = true;
                }
                else
                {
                    product.IsAvailable = false;
                }
            }

            // Load entitlements
            LoadEntitlements();

            OnInitialized?.Invoke();
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            LogError($"Unity IAP initialization failed: {error}");
            OnInitializationFailed?.Invoke(error.ToString());
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            LogError($"Unity IAP initialization failed: {error} - {message}");
            OnInitializationFailed?.Invoke($"{error}: {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Log($"Processing purchase: {args.purchasedProduct.definition.id}");

            // Find our product
            var product = _products.FirstOrDefault(p => p.StoreProductId == args.purchasedProduct.definition.id);
            if (product == null)
            {
                LogError($"Unknown product: {args.purchasedProduct.definition.id}");
                return PurchaseProcessingResult.Complete;
            }

            // Validate with server
            ValidatePurchaseWithServer(product, args.purchasedProduct);

            // Return pending - we'll confirm after server validation
            return PurchaseProcessingResult.Pending;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            LogError($"Purchase failed: {product.definition.id} - {reason}");

            var iapProduct = _products.FirstOrDefault(p => p.StoreProductId == product.definition.id);
            _pendingPurchase = null;

            OnPurchaseFailed?.Invoke(iapProduct, reason.ToString());

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent(
                Analytics.AnalyticsEvents.PURCHASE_FAILED,
                new Dictionary<string, object>
                {
                    { "product_id", product.definition.id },
                    { "reason", reason.ToString() }
                });
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            LogError($"Purchase failed: {product.definition.id} - {failureDescription.reason}: {failureDescription.message}");

            var iapProduct = _products.FirstOrDefault(p => p.StoreProductId == product.definition.id);
            _pendingPurchase = null;

            OnPurchaseFailed?.Invoke(iapProduct, failureDescription.message);
        }

        #endregion

        /// <summary>
        /// Purchase a product
        /// </summary>
        public void Purchase(string productId)
        {
            var product = _products.FirstOrDefault(p => p.Id == productId || p.StoreProductId == productId);
            if (product == null)
            {
                LogError($"Product not found: {productId}");
                OnPurchaseFailed?.Invoke(null, "Product not found");
                return;
            }

            Purchase(product);
        }

        /// <summary>
        /// Purchase a product
        /// </summary>
        public void Purchase(IAPProduct product)
        {
            if (!_isInitialized)
            {
                LogError("IAP not initialized");
                OnPurchaseFailed?.Invoke(product, "Store not initialized");
                return;
            }

            if (!product.IsAvailable)
            {
                LogError($"Product not available: {product.Id}");
                OnPurchaseFailed?.Invoke(product, "Product not available");
                return;
            }

            Log($"Starting purchase: {product.Id}");
            _pendingPurchase = product;

            OnPurchaseStarted?.Invoke(product);

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent(
                Analytics.AnalyticsEvents.PURCHASE_STARTED,
                new Dictionary<string, object>
                {
                    { "product_id", product.Id },
                    { "price_usd", product.PriceUSD }
                });

            _storeController.InitiatePurchase(product.StoreProductId);
        }

        /// <summary>
        /// Validate purchase with server
        /// </summary>
        private async void ValidatePurchaseWithServer(IAPProduct product, Product storeProduct)
        {
            try
            {
                string functionName;
                Dictionary<string, object> data;

#if UNITY_IOS
                functionName = "verifyApplePurchase";
                data = new Dictionary<string, object>
                {
                    { "receiptData", storeProduct.receipt },
                    { "productId", product.StoreProductId },
                    { "transactionId", storeProduct.transactionID }
                };
#elif UNITY_ANDROID
                // Parse Google Play receipt
                var receiptWrapper = JsonConvert.DeserializeObject<Dictionary<string, string>>(storeProduct.receipt);
                var payloadStr = receiptWrapper["Payload"];
                var payload = JsonConvert.DeserializeObject<Dictionary<string, string>>(payloadStr);
                
                functionName = "verifyGooglePurchase";
                data = new Dictionary<string, object>
                {
                    { "productId", product.StoreProductId },
                    { "purchaseToken", payload["purchaseToken"] },
                    { "isSubscription", product.Type == IAPProductType.Subscription }
                };
#else
                // Desktop/Editor - use fake validation
                functionName = "verifyApplePurchase";
                data = new Dictionary<string, object>
                {
                    { "receiptData", "fake_receipt" },
                    { "productId", product.StoreProductId }
                };
#endif

                Log($"Validating purchase with server: {functionName}");

                var callable = _functions.GetHttpsCallable(functionName);
                var result = await callable.CallAsync(data);

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                var purchaseResult = new PurchaseResult
                {
                    Success = response.ContainsKey("success") && (bool)response["success"],
                    PurchaseId = response.ContainsKey("purchaseId") ? response["purchaseId"].ToString() : null,
                    AlreadyProcessed = response.ContainsKey("alreadyProcessed") && (bool)response["alreadyProcessed"]
                };

                if (response.ContainsKey("rewards"))
                {
                    purchaseResult.Rewards = JsonConvert.DeserializeObject<List<IAPReward>>(
                        JsonConvert.SerializeObject(response["rewards"]));
                }

                if (purchaseResult.Success)
                {
                    Log($"Purchase validated successfully: {purchaseResult.PurchaseId}");

                    // Confirm the purchase with Unity IAP
                    _storeController.ConfirmPendingPurchase(storeProduct);

                    // Reload entitlements if this was a non-consumable
                    if (product.Type != IAPProductType.Consumable)
                    {
                        LoadEntitlements();
                    }

                    // Track analytics
                    Analytics.AnalyticsManager.Instance?.TrackPurchase(
                        product.Id, 
                        product.PriceUSD, 
                        product.CurrencyCode ?? "USD", 
                        true);

                    OnPurchaseCompleted?.Invoke(purchaseResult);
                }
                else
                {
                    LogError("Server validation failed");
                    purchaseResult.ErrorMessage = "Server validation failed";
                    OnPurchaseFailed?.Invoke(product, purchaseResult.ErrorMessage);
                }
            }
            catch (Exception e)
            {
                LogError($"Purchase validation error: {e.Message}");
                OnPurchaseFailed?.Invoke(product, e.Message);
            }
            finally
            {
                _pendingPurchase = null;
            }
        }

        /// <summary>
        /// Restore previous purchases (iOS requirement)
        /// </summary>
        public void RestorePurchases()
        {
            if (!_isInitialized)
            {
                LogError("IAP not initialized");
                return;
            }

            Log("Restoring purchases...");
            _isRestoring = true;

#if UNITY_IOS
            var apple = _extensionProvider.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions(OnRestoreTransactionsComplete);
#elif UNITY_ANDROID
            var google = _extensionProvider.GetExtension<IGooglePlayStoreExtensions>();
            google.RestoreTransactions(OnRestoreTransactionsComplete);
#else
            OnRestoreTransactionsComplete(true);
#endif
        }

        private async void OnRestoreTransactionsComplete(bool success)
        {
            _isRestoring = false;

            if (success)
            {
                Log("Restore transactions completed");

                // Also call server restore to sync entitlements
                try
                {
                    var callable = _functions.GetHttpsCallable("restorePurchases");
                    
#if UNITY_IOS
                    // Get the receipt for restore
                    var appleExtensions = _extensionProvider.GetExtension<IAppleExtensions>();
                    var receipt = appleExtensions.GetTransactionReceiptForProduct(
                        _storeController.products.all.FirstOrDefault());
                    
                    var data = new Dictionary<string, object>
                    {
                        { "store", "apple" },
                        { "receiptData", receipt }
                    };
#else
                    var data = new Dictionary<string, object>
                    {
                        { "store", "google" }
                    };
#endif

                    var result = await callable.CallAsync(data);
                    var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                    
                    int restoredCount = response.ContainsKey("restoredCount") 
                        ? Convert.ToInt32(response["restoredCount"]) 
                        : 0;

                    Log($"Server restored {restoredCount} purchases");

                    // Reload entitlements
                    LoadEntitlements();

                    OnRestoreCompleted?.Invoke(restoredCount);
                }
                catch (Exception e)
                {
                    LogError($"Server restore failed: {e.Message}");
                    OnRestoreCompleted?.Invoke(0);
                }
            }
            else
            {
                LogError("Restore transactions failed");
                OnRestoreCompleted?.Invoke(-1);
            }
        }

        /// <summary>
        /// Load user entitlements from server
        /// </summary>
        public async void LoadEntitlements()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getEntitlements");
                var result = await callable.CallAsync(null);

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                var entitlementsJson = JsonConvert.SerializeObject(response["entitlements"]);
                var entitlementsList = JsonConvert.DeserializeObject<List<Entitlement>>(entitlementsJson);

                _entitlements.Clear();
                foreach (var ent in entitlementsList)
                {
                    _entitlements[ent.Type] = ent;
                }

                Log($"Loaded {_entitlements.Count} entitlements");

                OnEntitlementsUpdated?.Invoke();
            }
            catch (Exception e)
            {
                LogError($"Failed to load entitlements: {e.Message}");
            }
        }

        /// <summary>
        /// Check if user has a specific entitlement
        /// </summary>
        public bool HasEntitlement(string entitlementType)
        {
            if (!_entitlements.TryGetValue(entitlementType, out var ent))
                return false;

            if (!ent.IsActive)
                return false;

            if (ent.ExpiresAt.HasValue && ent.ExpiresAt.Value < DateTime.UtcNow)
                return false;

            return true;
        }

        /// <summary>
        /// Get products by category
        /// </summary>
        public List<IAPProduct> GetProductsByCategory(IAPProductCategory category)
        {
            return _products.Where(p => p.Category == category && p.IsAvailable)
                           .OrderBy(p => p.SortOrder)
                           .ToList();
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        public IAPProduct GetProduct(string productId)
        {
            return _products.FirstOrDefault(p => p.Id == productId || p.StoreProductId == productId);
        }

        /// <summary>
        /// Get gem packs (currency products)
        /// </summary>
        public List<IAPProduct> GetGemPacks()
        {
            return GetProductsByCategory(IAPProductCategory.Currency);
        }

        /// <summary>
        /// Get starter packs
        /// </summary>
        public List<IAPProduct> GetStarterPacks()
        {
            return GetProductsByCategory(IAPProductCategory.StarterPack);
        }

        /// <summary>
        /// Get special offers
        /// </summary>
        public List<IAPProduct> GetSpecialOffers()
        {
            return GetProductsByCategory(IAPProductCategory.SpecialOffer);
        }

        /// <summary>
        /// Check if a one-time purchase has been made
        /// </summary>
        public bool HasPurchased(string productId)
        {
            var product = GetProduct(productId);
            if (product == null) return false;

            // For non-consumables, check if we have the entitlement
            if (product.Type == IAPProductType.NonConsumable)
            {
                // Check rewards for entitlements
                foreach (var reward in product.Rewards)
                {
                    if (reward.Type == "premium_pass" && HasPremiumPass)
                        return true;
                    if (reward.Type == "cosmetic" && HasEntitlement($"cosmetic_{reward.ItemId}"))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get localized price for a product
        /// </summary>
        public string GetLocalizedPrice(string productId)
        {
            var product = GetProduct(productId);
            return product?.LocalizedPrice ?? $"${product?.PriceUSD:F2}";
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[IAPManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[IAPManager] {message}");
        }
    }
}
