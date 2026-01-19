// ============================================================================
// APEX CITADELS - RESOURCE SPENDING MANAGER
// Unified resource economy: spending, generation, transactions, and validation
// ============================================================================
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ApexCitadels.Core;
using ApexCitadels.Data;

namespace ApexCitadels.PC.Economy
{
    // Note: ResourceType is now defined in ApexCitadels.Data.ResourceTypes

    /// <summary>
    /// Resource cost structure for any purchase
    /// </summary>
    [Serializable]
    public class ResourceCost
    {
        public int Stone;
        public int Wood;
        public int Iron;
        public int Crystal;
        public int ArcaneEssence;
        public int Gems;
        public int Gold;
        public int Food;
        public int Energy;

        public ResourceCost() { }

        public ResourceCost(int stone = 0, int wood = 0, int iron = 0, int crystal = 0,
                           int arcaneEssence = 0, int gems = 0, int gold = 0, 
                           int food = 0, int energy = 0)
        {
            Stone = stone;
            Wood = wood;
            Iron = iron;
            Crystal = crystal;
            ArcaneEssence = arcaneEssence;
            Gems = gems;
            Gold = gold;
            Food = food;
            Energy = energy;
        }

        public int GetAmount(ResourceType type)
        {
            return type switch
            {
                ResourceType.Stone => Stone,
                ResourceType.Wood => Wood,
                ResourceType.Iron => Iron,
                ResourceType.Crystal => Crystal,
                ResourceType.ArcaneEssence => ArcaneEssence,
                ResourceType.Gems => Gems,
                ResourceType.Gold => Gold,
                ResourceType.Food => Food,
                ResourceType.Energy => Energy,
                _ => 0
            };
        }

        public void SetAmount(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Stone: Stone = amount; break;
                case ResourceType.Wood: Wood = amount; break;
                case ResourceType.Iron: Iron = amount; break;
                case ResourceType.Crystal: Crystal = amount; break;
                case ResourceType.ArcaneEssence: ArcaneEssence = amount; break;
                case ResourceType.Gems: Gems = amount; break;
                case ResourceType.Gold: Gold = amount; break;
                case ResourceType.Food: Food = amount; break;
                case ResourceType.Energy: Energy = amount; break;
            }
        }

        public bool IsZero()
        {
            return Stone == 0 && Wood == 0 && Iron == 0 && Crystal == 0 &&
                   ArcaneEssence == 0 && Gems == 0 && Gold == 0 && Food == 0 && Energy == 0;
        }

        public static ResourceCost operator +(ResourceCost a, ResourceCost b)
        {
            return new ResourceCost(
                a.Stone + b.Stone, a.Wood + b.Wood, a.Iron + b.Iron,
                a.Crystal + b.Crystal, a.ArcaneEssence + b.ArcaneEssence,
                a.Gems + b.Gems, a.Gold + b.Gold, a.Food + b.Food, a.Energy + b.Energy
            );
        }

        public static ResourceCost operator *(ResourceCost a, int multiplier)
        {
            return new ResourceCost(
                a.Stone * multiplier, a.Wood * multiplier, a.Iron * multiplier,
                a.Crystal * multiplier, a.ArcaneEssence * multiplier,
                a.Gems * multiplier, a.Gold * multiplier, a.Food * multiplier, 
                a.Energy * multiplier
            );
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (Stone > 0) parts.Add($"🪨{Stone}");
            if (Wood > 0) parts.Add($"🪵{Wood}");
            if (Iron > 0) parts.Add($"🔩{Iron}");
            if (Crystal > 0) parts.Add($"💎{Crystal}");
            if (ArcaneEssence > 0) parts.Add($"✨{ArcaneEssence}");
            if (Gems > 0) parts.Add($"💠{Gems}");
            if (Gold > 0) parts.Add($"🪙{Gold}");
            if (Food > 0) parts.Add($"🍖{Food}");
            if (Energy > 0) parts.Add($"⚡{Energy}");
            return string.Join(" ", parts);
        }
    }

    /// <summary>
    /// Transaction record for history and rollback
    /// </summary>
    [Serializable]
    public class ResourceTransaction
    {
        public string Id;
        public string Type; // "spend", "earn", "refund", "transfer"
        public string Reason;
        public ResourceCost Amount;
        public DateTime Timestamp;
        public bool Success;
    }

    /// <summary>
    /// Resource change event for UI updates
    /// </summary>
    public class ResourceChangeEventArgs : EventArgs
    {
        public ResourceType Type;
        public int OldAmount;
        public int NewAmount;
        public int Delta;
        public string Reason;
    }

    /// <summary>
    /// Central manager for all resource transactions
    /// </summary>
    public class ResourceSpendingManager : MonoBehaviour
    {
        public static ResourceSpendingManager Instance { get; private set; }

        [Header("Starting Resources")]
        [SerializeField] private int startStone = 1000;
        [SerializeField] private int startWood = 1000;
        [SerializeField] private int startIron = 500;
        [SerializeField] private int startCrystal = 100;
        [SerializeField] private int startArcaneEssence = 20;
        [SerializeField] private int startGems = 50;
        [SerializeField] private int startGold = 500;
        [SerializeField] private int startFood = 200;
        [SerializeField] private int startEnergy = 100;

        [Header("Maximum Capacity")]
        [SerializeField] private int maxStone = 50000;
        [SerializeField] private int maxWood = 50000;
        [SerializeField] private int maxIron = 25000;
        [SerializeField] private int maxCrystal = 10000;
        [SerializeField] private int maxArcaneEssence = 5000;
        [SerializeField] private int maxGems = 100000; // Premium currency
        [SerializeField] private int maxGold = 100000;
        [SerializeField] private int maxFood = 10000;
        [SerializeField] private int maxEnergy = 200;

        [Header("Generation Rates (per minute)")]
        [SerializeField] private float stonePerMinute = 10f;
        [SerializeField] private float woodPerMinute = 10f;
        [SerializeField] private float ironPerMinute = 5f;
        [SerializeField] private float crystalPerMinute = 1f;
        [SerializeField] private float goldPerMinute = 2f;
        [SerializeField] private float foodPerMinute = 3f;
        [SerializeField] private float energyPerMinute = 1f;

        // Current resources
        private Dictionary<ResourceType, float> resources = new Dictionary<ResourceType, float>();
        private Dictionary<ResourceType, int> maxResources = new Dictionary<ResourceType, int>();
        private Dictionary<ResourceType, float> generationRates = new Dictionary<ResourceType, float>();

        // Transaction history
        private List<ResourceTransaction> transactionHistory = new List<ResourceTransaction>();
        private const int MAX_HISTORY = 100;

        // Generation tracking
        private float lastGenerationTime;
        private bool generationEnabled = true;

        // Events
        public event EventHandler<ResourceChangeEventArgs> OnResourceChanged;
        public event Action OnResourcesLoaded;
        public event Action<ResourceTransaction> OnTransactionComplete;
        public event Action<ResourceType> OnResourceDepleted;
        public event Action<ResourceType> OnResourceMaxed;
        public event Action<ResourceCost> OnInsufficientResources;

        // Persistence
        private const string SAVE_KEY = "player_resources";
        private const string HISTORY_KEY = "transaction_history";
        private bool isDirty = false;
        private float saveInterval = 30f;
        private float lastSaveTime;

        #region Properties

        public int Stone => GetResource(ResourceType.Stone);
        public int Wood => GetResource(ResourceType.Wood);
        public int Iron => GetResource(ResourceType.Iron);
        public int Crystal => GetResource(ResourceType.Crystal);
        public int ArcaneEssence => GetResource(ResourceType.ArcaneEssence);
        public int Gems => GetResource(ResourceType.Gems);
        public int Gold => GetResource(ResourceType.Gold);
        public int Food => GetResource(ResourceType.Food);
        public int Energy => GetResource(ResourceType.Energy);

        #endregion

        #region Initialization

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeResources();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadResources();
            lastGenerationTime = Time.time;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SaveResources();
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                SaveResources();
            }
            else
            {
                // Calculate offline generation
                CalculateOfflineGeneration();
            }
        }

        private void InitializeResources()
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                resources[type] = 0;
                maxResources[type] = 10000;
                generationRates[type] = 0;
            }

            // Set maximums
            maxResources[ResourceType.Stone] = maxStone;
            maxResources[ResourceType.Wood] = maxWood;
            maxResources[ResourceType.Iron] = maxIron;
            maxResources[ResourceType.Crystal] = maxCrystal;
            maxResources[ResourceType.ArcaneEssence] = maxArcaneEssence;
            maxResources[ResourceType.Gems] = maxGems;
            maxResources[ResourceType.Gold] = maxGold;
            maxResources[ResourceType.Food] = maxFood;
            maxResources[ResourceType.Energy] = maxEnergy;

            // Set generation rates
            generationRates[ResourceType.Stone] = stonePerMinute;
            generationRates[ResourceType.Wood] = woodPerMinute;
            generationRates[ResourceType.Iron] = ironPerMinute;
            generationRates[ResourceType.Crystal] = crystalPerMinute;
            generationRates[ResourceType.Gold] = goldPerMinute;
            generationRates[ResourceType.Food] = foodPerMinute;
            generationRates[ResourceType.Energy] = energyPerMinute;
            // Gems and ArcaneEssence don't auto-generate
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            if (generationEnabled)
            {
                GenerateResources();
            }

            // Auto-save
            if (isDirty && Time.time - lastSaveTime > saveInterval)
            {
                SaveResources();
            }
        }

        private void GenerateResources()
        {
            float deltaTime = Time.deltaTime;
            float minutesFraction = deltaTime / 60f;

            foreach (var kvp in generationRates)
            {
                if (kvp.Value > 0)
                {
                    float generated = kvp.Value * minutesFraction;
                    AddResourceInternal(kvp.Key, generated, "Passive generation", false);
                }
            }
        }

        private void CalculateOfflineGeneration()
        {
            // Check last save time and calculate generation
            if (PlayerPrefs.HasKey("last_save_time"))
            {
                long lastTicks = Convert.ToInt64(PlayerPrefs.GetString("last_save_time"));
                DateTime lastTime = new DateTime(lastTicks);
                TimeSpan elapsed = DateTime.UtcNow - lastTime;

                // Cap at 8 hours of offline generation
                float minutes = Mathf.Min((float)elapsed.TotalMinutes, 480f);

                if (minutes > 1)
                {
                    foreach (var kvp in generationRates)
                    {
                        if (kvp.Value > 0)
                        {
                            float generated = kvp.Value * minutes;
                            AddResourceInternal(kvp.Key, generated, "Offline generation", false);
                        }
                    }

                    ApexLogger.LogVerbose(ApexLogger.LogCategory.Economy, $"Calculated {minutes:F1} minutes of offline generation");
                }
            }
        }

        #endregion

        #region Resource Access

        public int GetResource(ResourceType type)
        {
            return Mathf.FloorToInt(resources.TryGetValue(type, out float amount) ? amount : 0);
        }

        public int GetMaxResource(ResourceType type)
        {
            return maxResources.TryGetValue(type, out int max) ? max : 10000;
        }

        public float GetGenerationRate(ResourceType type)
        {
            return generationRates.TryGetValue(type, out float rate) ? rate : 0;
        }

        public float GetFillPercent(ResourceType type)
        {
            int max = GetMaxResource(type);
            return max > 0 ? (float)GetResource(type) / max : 0f;
        }

        public Dictionary<ResourceType, int> GetAllResources()
        {
            var result = new Dictionary<ResourceType, int>();
            foreach (var kvp in resources)
            {
                result[kvp.Key] = Mathf.FloorToInt(kvp.Value);
            }
            return result;
        }

        #endregion

        #region Affordability Checks

        public bool CanAfford(ResourceCost cost)
        {
            if (cost == null) return true;

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                int required = cost.GetAmount(type);
                if (required > 0 && GetResource(type) < required)
                {
                    return false;
                }
            }
            return true;
        }

        public bool CanAfford(ResourceType type, int amount)
        {
            return GetResource(type) >= amount;
        }

        /// <summary>
        /// Get missing resources for a cost
        /// </summary>
        public ResourceCost GetMissingResources(ResourceCost cost)
        {
            var missing = new ResourceCost();
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                int required = cost.GetAmount(type);
                int have = GetResource(type);
                if (required > have)
                {
                    missing.SetAmount(type, required - have);
                }
            }
            return missing;
        }

        #endregion

        #region Spending & Earning

        /// <summary>
        /// Spend resources (returns false if insufficient)
        /// </summary>
        public bool Spend(ResourceCost cost, string reason = "Purchase")
        {
            if (!CanAfford(cost))
            {
                OnInsufficientResources?.Invoke(cost);
                RecordTransaction("spend", reason, cost, false);
                return false;
            }

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                int amount = cost.GetAmount(type);
                if (amount > 0)
                {
                    RemoveResourceInternal(type, amount, reason);
                }
            }

            RecordTransaction("spend", reason, cost, true);
            isDirty = true;
            return true;
        }

        /// <summary>
        /// Spend a single resource type
        /// </summary>
        public bool Spend(ResourceType type, int amount, string reason = "Purchase")
        {
            var cost = new ResourceCost();
            cost.SetAmount(type, amount);
            return Spend(cost, reason);
        }

        /// <summary>
        /// Earn resources (capped at maximum)
        /// </summary>
        public void Earn(ResourceCost reward, string reason = "Reward")
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                int amount = reward.GetAmount(type);
                if (amount > 0)
                {
                    AddResourceInternal(type, amount, reason, true);
                }
            }

            RecordTransaction("earn", reason, reward, true);
            isDirty = true;
        }

        /// <summary>
        /// Earn a single resource type
        /// </summary>
        public void Earn(ResourceType type, int amount, string reason = "Reward")
        {
            var reward = new ResourceCost();
            reward.SetAmount(type, amount);
            Earn(reward, reason);
        }

        /// <summary>
        /// Refund resources (partial or full)
        /// </summary>
        public void Refund(ResourceCost amount, float ratio = 1f, string reason = "Refund")
        {
            var actualRefund = new ResourceCost();
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                int original = amount.GetAmount(type);
                int refunded = Mathf.FloorToInt(original * ratio);
                if (refunded > 0)
                {
                    actualRefund.SetAmount(type, refunded);
                    AddResourceInternal(type, refunded, reason, true);
                }
            }

            RecordTransaction("refund", reason, actualRefund, true);
            isDirty = true;
        }

        private void AddResourceInternal(ResourceType type, float amount, string reason, bool notify)
        {
            int oldAmount = GetResource(type);
            int max = GetMaxResource(type);
            
            resources[type] = Mathf.Min(resources[type] + amount, max);
            int newAmount = GetResource(type);

            if (notify && oldAmount != newAmount)
            {
                NotifyChange(type, oldAmount, newAmount, reason);

                if (newAmount >= max)
                {
                    OnResourceMaxed?.Invoke(type);
                }
            }
        }

        private void RemoveResourceInternal(ResourceType type, int amount, string reason)
        {
            int oldAmount = GetResource(type);
            resources[type] = Mathf.Max(0, resources[type] - amount);
            int newAmount = GetResource(type);

            NotifyChange(type, oldAmount, newAmount, reason);

            if (newAmount <= 0)
            {
                OnResourceDepleted?.Invoke(type);
            }
        }

        private void NotifyChange(ResourceType type, int oldAmount, int newAmount, string reason)
        {
            OnResourceChanged?.Invoke(this, new ResourceChangeEventArgs
            {
                Type = type,
                OldAmount = oldAmount,
                NewAmount = newAmount,
                Delta = newAmount - oldAmount,
                Reason = reason
            });
        }

        #endregion

        #region Transaction History

        private void RecordTransaction(string type, string reason, ResourceCost amount, bool success)
        {
            var transaction = new ResourceTransaction
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                Reason = reason,
                Amount = amount,
                Timestamp = DateTime.UtcNow,
                Success = success
            };

            transactionHistory.Insert(0, transaction);
            
            // Trim history
            while (transactionHistory.Count > MAX_HISTORY)
            {
                transactionHistory.RemoveAt(transactionHistory.Count - 1);
            }

            OnTransactionComplete?.Invoke(transaction);
        }

        public List<ResourceTransaction> GetRecentTransactions(int count = 10)
        {
            return transactionHistory.Take(count).ToList();
        }

        #endregion

        #region Capacity Management

        public void SetMaxResource(ResourceType type, int max)
        {
            maxResources[type] = max;
            
            // Cap current if needed
            if (resources[type] > max)
            {
                resources[type] = max;
            }
        }

        public void IncreaseCapacity(ResourceType type, int additionalCapacity)
        {
            maxResources[type] += additionalCapacity;
        }

        public void SetGenerationRate(ResourceType type, float ratePerMinute)
        {
            generationRates[type] = ratePerMinute;
        }

        public void IncreaseGenerationRate(ResourceType type, float additionalRate)
        {
            generationRates[type] += additionalRate;
        }

        #endregion

        #region Persistence

        private void SaveResources()
        {
            try
            {
                var saveData = new ResourceSaveData
                {
                    resources = new List<ResourceSaveEntry>(),
                    savedAt = DateTime.UtcNow.Ticks
                };

                foreach (var kvp in resources)
                {
                    saveData.resources.Add(new ResourceSaveEntry
                    {
                        type = kvp.Key.ToString(),
                        amount = Mathf.FloorToInt(kvp.Value)
                    });
                }

                string json = JsonUtility.ToJson(saveData);
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.SetString("last_save_time", DateTime.UtcNow.Ticks.ToString());
                PlayerPrefs.Save();

                lastSaveTime = Time.time;
                isDirty = false;

                ApexLogger.LogVerbose(ApexLogger.LogCategory.Economy, "Saved resources");
            }
            catch (Exception ex)
            {
                ApexLogger.LogError(ApexLogger.LogCategory.Economy, $"Save failed: {ex.Message}");
            }
        }

        private void LoadResources()
        {
            try
            {
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(SAVE_KEY);
                    var saveData = JsonUtility.FromJson<ResourceSaveData>(json);

                    if (saveData?.resources != null)
                    {
                        foreach (var entry in saveData.resources)
                        {
                            if (Enum.TryParse<ResourceType>(entry.type, out var type))
                            {
                                resources[type] = entry.amount;
                            }
                        }

                        OnResourcesLoaded?.Invoke();
                        ApexLogger.Log(ApexLogger.LogCategory.Economy, "Loaded resources from storage");
                        return;
                    }
                }

                // No save - use starting resources
                SetStartingResources();
            }
            catch (Exception ex)
            {
                ApexLogger.LogError(ApexLogger.LogCategory.Economy, $"Load failed: {ex.Message}");
                SetStartingResources();
            }
        }

        private void SetStartingResources()
        {
            resources[ResourceType.Stone] = startStone;
            resources[ResourceType.Wood] = startWood;
            resources[ResourceType.Iron] = startIron;
            resources[ResourceType.Crystal] = startCrystal;
            resources[ResourceType.ArcaneEssence] = startArcaneEssence;
            resources[ResourceType.Gems] = startGems;
            resources[ResourceType.Gold] = startGold;
            resources[ResourceType.Food] = startFood;
            resources[ResourceType.Energy] = startEnergy;

            OnResourcesLoaded?.Invoke();
            ApexLogger.Log(ApexLogger.LogCategory.Economy, "Set starting resources");
        }

        /// <summary>
        /// Reset to starting resources (for testing)
        /// </summary>
        [ContextMenu("Reset Resources")]
        public void ResetResources()
        {
            SetStartingResources();
            transactionHistory.Clear();
            SaveResources();
        }

        /// <summary>
        /// Add debug resources
        /// </summary>
        [ContextMenu("Add Debug Resources")]
        public void AddDebugResources()
        {
            Earn(new ResourceCost(1000, 1000, 500, 100, 50, 100, 500, 200, 50), "Debug bonus");
        }

        #endregion
    }

    #region Save Data

    [Serializable]
    public class ResourceSaveData
    {
        public List<ResourceSaveEntry> resources;
        public long savedAt;
    }

    [Serializable]
    public class ResourceSaveEntry
    {
        public string type;
        public int amount;
    }

    #endregion
}
