using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

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
    /// In-App Purchase Manager
    /// Requires Unity IAP package to be installed for full functionality.
    /// Install via: Window > Package Manager > Unity IAP
    /// Then add UNITY_IAP_ENABLED to Scripting Define Symbols
    /// </summary>
    public class IAPManager : MonoBehaviour
    {
        public static IAPManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableDebugLogs = true;

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
        private List<IAPProduct> _products = new List<IAPProduct>();
        private Dictionary<string, Entitlement> _entitlements = new Dictionary<string, Entitlement>();
        private bool _isInitialized;

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

        private void Start()
        {
            Debug.LogWarning("[IAPManager] Unity IAP not installed. Running in stub mode. " +
                "Install via Window > Package Manager > Unity IAP, then add UNITY_IAP_ENABLED to Scripting Define Symbols.");
            _isInitialized = true;
            OnInitialized?.Invoke();
        }

        public void Initialize()
        {
            Debug.LogWarning("[IAPManager] Initialize called but Unity IAP is not installed.");
            _isInitialized = true;
            OnInitialized?.Invoke();
        }

        public async Task<bool> LoadProducts()
        {
            Debug.LogWarning("[IAPManager] LoadProducts called but Unity IAP is not installed.");
            await Task.Delay(100);
            OnProductsLoaded?.Invoke(_products);
            return true;
        }

        public IAPProduct GetProduct(string productId)
        {
            return _products.Find(p => p.Id == productId);
        }

        public List<IAPProduct> GetProductsByCategory(IAPProductCategory category)
        {
            return _products.FindAll(p => p.Category == category);
        }

        public async Task<PurchaseResult> PurchaseProduct(string productId)
        {
            Debug.LogWarning($"[IAPManager] PurchaseProduct({productId}) called but Unity IAP is not installed.");
            await Task.Delay(100);
            
            var result = new PurchaseResult
            {
                Success = false,
                ErrorMessage = "Unity IAP is not installed. Please install Unity IAP package."
            };
            
            OnPurchaseFailed?.Invoke(GetProduct(productId), result.ErrorMessage);
            return result;
        }

        public void RestorePurchases()
        {
            Debug.LogWarning("[IAPManager] RestorePurchases called but Unity IAP is not installed.");
            OnRestoreCompleted?.Invoke(0);
        }

        public async Task LoadEntitlements()
        {
            Debug.LogWarning("[IAPManager] LoadEntitlements called but Unity IAP is not installed.");
            await Task.Delay(100);
            OnEntitlementsUpdated?.Invoke();
        }

        public bool HasEntitlement(string entitlementType)
        {
            return _entitlements.ContainsKey(entitlementType) && _entitlements[entitlementType].IsActive;
        }

        public Entitlement GetEntitlement(string entitlementType)
        {
            return _entitlements.TryGetValue(entitlementType, out var ent) ? ent : null;
        }
    }
}
