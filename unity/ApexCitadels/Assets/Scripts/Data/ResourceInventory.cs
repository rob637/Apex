// ============================================================================
// APEX CITADELS - RESOURCE INVENTORY SYSTEM
// Player resource management with events and persistence
// ============================================================================
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApexCitadels.Core;

namespace ApexCitadels.Data
{
    // Note: ResourceType enum is defined in ResourceTypes.cs
    // Note: ResourceCost class is defined in ResourceTypes.cs

    /// <summary>
    /// Simple resource cost with type name and amount (for serialization)
    /// </summary>
    [System.Serializable]
    public class SimpleResourceCost
    {
        public string Type;
        public int Amount;

        public SimpleResourceCost() { }
        public SimpleResourceCost(string type, int amount)
        {
            Type = type;
            Amount = amount;
        }
    }

    /// <summary>
    /// Resource change event data
    /// </summary>
    public class ResourceChangeEvent
    {
        public ResourceType Type;
        public int OldAmount;
        public int NewAmount;
        public int Delta;
        public string Reason;
    }

    /// <summary>
    /// Manages player resources with change notifications and persistence
    /// </summary>
    public class ResourceInventory : MonoBehaviour
    {
        public static ResourceInventory Instance { get; private set; }

        [Header("Starting Resources")]
        [SerializeField] private int startingStone = 500;
        [SerializeField] private int startingWood = 500;
        [SerializeField] private int startingMetal = 200;
        [SerializeField] private int startingCrystal = 50;
        [SerializeField] private int startingGold = 100;
        [SerializeField] private int startingFood = 100;
        [SerializeField] private int startingEnergy = 100;

        [Header("Capacity")]
        [SerializeField] private int maxStone = 10000;
        [SerializeField] private int maxWood = 10000;
        [SerializeField] private int maxMetal = 5000;
        [SerializeField] private int maxCrystal = 1000;
        [SerializeField] private int maxGold = 50000;
        [SerializeField] private int maxFood = 5000;
        [SerializeField] private int maxEnergy = 1000;

        // Resource storage
        private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
        private Dictionary<ResourceType, int> maxResources = new Dictionary<ResourceType, int>();

        // Events
        public event Action<ResourceChangeEvent> OnResourceChanged;
        public event Action OnResourcesLoaded;
        public event Action<ResourceType> OnResourceDepleted;
        public event Action<ResourceType> OnResourceMaxed;

        // Properties for easy access
        public int Stone => GetResource(ResourceType.Stone);
        public int Wood => GetResource(ResourceType.Wood);
        public int Metal => GetResource(ResourceType.Metal);
        public int Crystal => GetResource(ResourceType.Crystal);
        public int Gold => GetResource(ResourceType.Gold);
        public int Food => GetResource(ResourceType.Food);
        public int Energy => GetResource(ResourceType.Energy);
        public int Influence => GetResource(ResourceType.Influence);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeResources();
        }

        private void Start()
        {
            LoadResources();
        }

        private void InitializeResources()
        {
            // Initialize all resource types to 0
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                resources[type] = 0;
                maxResources[type] = 10000; // Default max
            }

            // Set specific caps
            maxResources[ResourceType.Stone] = maxStone;
            maxResources[ResourceType.Wood] = maxWood;
            maxResources[ResourceType.Metal] = maxMetal;
            maxResources[ResourceType.Crystal] = maxCrystal;
            maxResources[ResourceType.Gold] = maxGold;
            maxResources[ResourceType.Food] = maxFood;
            maxResources[ResourceType.Energy] = maxEnergy;
            maxResources[ResourceType.Influence] = 1000;
        }

        #region Resource Access

        /// <summary>
        /// Get current amount of a resource
        /// </summary>
        public int GetResource(ResourceType type)
        {
            return resources.TryGetValue(type, out int amount) ? amount : 0;
        }

        /// <summary>
        /// Get resource by string name
        /// </summary>
        public int GetResource(string typeName)
        {
            if (Enum.TryParse<ResourceType>(typeName, true, out ResourceType type))
            {
                return GetResource(type);
            }
            return 0;
        }

        /// <summary>
        /// Get maximum capacity for a resource
        /// </summary>
        public int GetMaxResource(ResourceType type)
        {
            return maxResources.TryGetValue(type, out int max) ? max : 10000;
        }

        /// <summary>
        /// Get fill percentage (0-1)
        /// </summary>
        public float GetFillPercent(ResourceType type)
        {
            int max = GetMaxResource(type);
            return max > 0 ? (float)GetResource(type) / max : 0f;
        }

        /// <summary>
        /// Check if player has at least the specified amount
        /// </summary>
        public bool HasResource(ResourceType type, int amount)
        {
            return GetResource(type) >= amount;
        }

        /// <summary>
        /// Check if player has resource by string name
        /// </summary>
        public bool HasResource(string typeName, int amount)
        {
            if (Enum.TryParse<ResourceType>(typeName, true, out ResourceType type))
            {
                return HasResource(type, amount);
            }
            return false;
        }

        /// <summary>
        /// Check if player can afford a list of costs
        /// </summary>
        public bool CanAfford(List<ResourceCostItem> costs)
        {
            if (costs == null) return true;

            foreach (var cost in costs)
            {
                if (!HasResource(cost.Type, cost.Amount))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check if player can afford a single cost
        /// </summary>
        public bool CanAfford(ResourceType type, int amount)
        {
            return HasResource(type, amount);
        }

        /// <summary>
        /// Get all resources as dictionary
        /// </summary>
        public Dictionary<ResourceType, int> GetAllResources()
        {
            return new Dictionary<ResourceType, int>(resources);
        }

        #endregion

        #region Resource Modification

        /// <summary>
        /// Add resources (capped at maximum)
        /// </summary>
        public int AddResource(ResourceType type, int amount, string reason = "")
        {
            if (amount <= 0) return 0;

            int oldAmount = GetResource(type);
            int max = GetMaxResource(type);
            int newAmount = Mathf.Min(oldAmount + amount, max);
            int actualAdded = newAmount - oldAmount;

            resources[type] = newAmount;

            NotifyChange(type, oldAmount, newAmount, reason);

            if (newAmount >= max)
            {
                OnResourceMaxed?.Invoke(type);
            }

            return actualAdded;
        }

        /// <summary>
        /// Remove resources (returns false if insufficient)
        /// </summary>
        public bool RemoveResource(ResourceType type, int amount, string reason = "")
        {
            if (amount <= 0) return true;

            int oldAmount = GetResource(type);
            if (oldAmount < amount) return false;

            int newAmount = oldAmount - amount;
            resources[type] = newAmount;

            NotifyChange(type, oldAmount, newAmount, reason);

            if (newAmount <= 0)
            {
                OnResourceDepleted?.Invoke(type);
            }

            return true;
        }

        /// <summary>
        /// Remove resource by string name
        /// </summary>
        public bool RemoveResource(string typeName, int amount, string reason = "")
        {
            if (Enum.TryParse<ResourceType>(typeName, true, out ResourceType type))
            {
                return RemoveResource(type, amount, reason);
            }
            return false;
        }

        /// <summary>
        /// Set resource to specific amount
        /// </summary>
        public void SetResource(ResourceType type, int amount, string reason = "")
        {
            int oldAmount = GetResource(type);
            int max = GetMaxResource(type);
            int newAmount = Mathf.Clamp(amount, 0, max);

            resources[type] = newAmount;

            if (oldAmount != newAmount)
            {
                NotifyChange(type, oldAmount, newAmount, reason);
            }
        }

        /// <summary>
        /// Spend multiple resources at once
        /// </summary>
        public bool SpendResources(List<ResourceCostItem> costs, string reason = "")
        {
            if (!CanAfford(costs)) return false;

            foreach (var cost in costs)
            {
                if (!RemoveResource(cost.Type, cost.Amount, reason))
                {
                    // Rollback on failure (shouldn't happen if CanAfford passed)
                    ApexLogger.LogError($"Failed to spend {cost.Amount} {cost.Type}", ApexLogger.LogCategory.Economy);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Transfer resources from this inventory to another
        /// </summary>
        public bool Transfer(ResourceInventory target, ResourceType type, int amount)
        {
            if (!HasResource(type, amount)) return false;

            RemoveResource(type, amount, "Transfer out");
            target.AddResource(type, amount, "Transfer in");

            return true;
        }

        /// <summary>
        /// Set maximum capacity for a resource
        /// </summary>
        public void SetMaxResource(ResourceType type, int max)
        {
            maxResources[type] = max;

            // Cap current value if needed
            if (resources[type] > max)
            {
                SetResource(type, max, "Capacity reduced");
            }
        }

        /// <summary>
        /// Increase max capacity
        /// </summary>
        public void IncreaseCapacity(ResourceType type, int additionalCapacity)
        {
            maxResources[type] += additionalCapacity;
        }

        private void NotifyChange(ResourceType type, int oldAmount, int newAmount, string reason)
        {
            OnResourceChanged?.Invoke(new ResourceChangeEvent
            {
                Type = type,
                OldAmount = oldAmount,
                NewAmount = newAmount,
                Delta = newAmount - oldAmount,
                Reason = reason
            });
        }

        #endregion

        #region Persistence

        /// <summary>
        /// Load resources from backend
        /// </summary>
        public async void LoadResources()
        {
            await Task.CompletedTask;
            try
            {
#if FIREBASE_ENABLED
                // Load from Firebase
                string userId = Core.GameManager.Instance?.UserId;
                if (!string.IsNullOrEmpty(userId))
                {
                    var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
                    var doc = await db.Collection("players").Document(userId).GetSnapshotAsync();
                    
                    if (doc.Exists && doc.TryGetValue("resources", out Dictionary<string, object> res))
                    {
                        foreach (var kvp in res)
                        {
                            if (Enum.TryParse<ResourceType>(kvp.Key, true, out ResourceType type))
                            {
                                resources[type] = Convert.ToInt32(kvp.Value);
                            }
                        }
                        ApexLogger.Log("Loaded resources from Firebase", ApexLogger.LogCategory.Economy);
                        OnResourcesLoaded?.Invoke();
                        return;
                    }
                }
#endif

                // Use starting resources if no save found
                SetStartingResources();
                OnResourcesLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                ApexLogger.LogWarning($"Failed to load resources: {ex.Message}", ApexLogger.LogCategory.Economy);
                SetStartingResources();
                OnResourcesLoaded?.Invoke();
            }
        }

        private void SetStartingResources()
        {
            resources[ResourceType.Stone] = startingStone;
            resources[ResourceType.Wood] = startingWood;
            resources[ResourceType.Metal] = startingMetal;
            resources[ResourceType.Crystal] = startingCrystal;
            resources[ResourceType.Gold] = startingGold;
            resources[ResourceType.Food] = startingFood;
            resources[ResourceType.Energy] = startingEnergy;

            ApexLogger.Log("Using starting resources", ApexLogger.LogCategory.Economy);
        }

        /// <summary>
        /// Save resources to backend
        /// </summary>
        public async Task SaveResources()
        {
#if FIREBASE_ENABLED
            try
            {
                string userId = Core.GameManager.Instance?.UserId;
                if (!string.IsNullOrEmpty(userId))
                {
                    var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
                    
                    var resourceData = new Dictionary<string, object>();
                    foreach (var kvp in resources)
                    {
                        resourceData[kvp.Key.ToString().ToLower()] = kvp.Value;
                    }
                    
                    await db.Collection("players").Document(userId).SetAsync(new Dictionary<string, object>
                    {
                        { "resources", resourceData },
                        { "updatedAt", Firebase.Firestore.FieldValue.ServerTimestamp }
                    }, Firebase.Firestore.SetOptions.MergeAll);
                    
                    ApexLogger.Log("Saved resources to Firebase", ApexLogger.LogCategory.Economy);
                }
            }
            catch (Exception ex)
            {
                ApexLogger.LogWarning($"Failed to save resources: {ex.Message}", ApexLogger.LogCategory.Economy);
            }
#else
            await Task.CompletedTask;
            ApexLogger.LogVerbose("Saved resources (local stub)", ApexLogger.LogCategory.Economy);
#endif
        }

        #endregion

        #region Debug

        /// <summary>
        /// Add resources for testing
        /// </summary>
        [ContextMenu("Debug: Add All Resources")]
        public void DebugAddResources()
        {
            AddResource(ResourceType.Stone, 1000, "Debug");
            AddResource(ResourceType.Wood, 1000, "Debug");
            AddResource(ResourceType.Metal, 500, "Debug");
            AddResource(ResourceType.Crystal, 100, "Debug");
            AddResource(ResourceType.Gold, 1000, "Debug");
        }

        /// <summary>
        /// Log current resources
        /// </summary>
        [ContextMenu("Debug: Log Resources")]
        public void DebugLogResources()
        {
            foreach (var kvp in resources)
            {
                ApexLogger.Log($"{kvp.Key}: {kvp.Value} / {GetMaxResource(kvp.Key)}", ApexLogger.LogCategory.Economy);
            }
        }

        #endregion
    }
}
