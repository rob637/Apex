using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ApexCitadels.PC
{
    /// <summary>
    /// PC Resource Management System.
    /// Handles resource generation, collection, display, and syncing with Firebase.
    /// </summary>
    public class PCResourceSystem : MonoBehaviour
    {
        public static PCResourceSystem Instance { get; private set; }

        [Header("Resource Generation")]
        [SerializeField] private float tickInterval = 10f; // Seconds between resource ticks
        [SerializeField] private float goldPerTick = 5f;
        [SerializeField] private float stonePerTick = 3f;
        [SerializeField] private float woodPerTick = 4f;
        [SerializeField] private float ironPerTick = 1f;
        [SerializeField] private float crystalPerTick = 0.5f;

        [Header("Storage Limits")]
        [SerializeField] private int maxGold = 10000;
        [SerializeField] private int maxStone = 5000;
        [SerializeField] private int maxWood = 5000;
        [SerializeField] private int maxIron = 2000;
        [SerializeField] private int maxCrystal = 1000;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI stoneText;
        [SerializeField] private TextMeshProUGUI woodText;
        [SerializeField] private TextMeshProUGUI ironText;
        [SerializeField] private TextMeshProUGUI crystalText;

        [Header("Effects")]
        [SerializeField] private bool showTickEffects = true;
        [SerializeField] private GameObject resourceGainPrefab;

        // Current resources
        private float _gold;
        private float _stone;
        private float _wood;
        private float _iron;
        private float _crystal;
        
        // Pending (uncollected) resources
        private float _pendingGold;
        private float _pendingStone;
        private float _pendingWood;
        private float _pendingIron;
        private float _pendingCrystal;

        // Production multipliers (from buildings, bonuses, etc.)
        private float _goldMultiplier = 1f;
        private float _stoneMultiplier = 1f;
        private float _woodMultiplier = 1f;
        private float _ironMultiplier = 1f;
        private float _crystalMultiplier = 1f;

        // Timing
        private float _lastTickTime;
        private float _timeSinceLastTick;

        // Events
        public event Action<ResourceType, float> OnResourceGained;
        public event Action<ResourceType, float> OnResourceSpent;
        public event Action OnResourcesUpdated;
        public event Action OnTick;

        // Properties
        public int Gold => Mathf.FloorToInt(_gold);
        public int Stone => Mathf.FloorToInt(_stone);
        public int Wood => Mathf.FloorToInt(_wood);
        public int Iron => Mathf.FloorToInt(_iron);
        public int Crystal => Mathf.FloorToInt(_crystal);
        
        public float PendingGold => _pendingGold;
        public float TimeToNextTick => Mathf.Max(0, tickInterval - _timeSinceLastTick);
        public float TickProgress => _timeSinceLastTick / tickInterval;

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
            // Initialize with starting resources
            _gold = 1000f;
            _stone = 500f;
            _wood = 750f;
            _iron = 100f;
            _crystal = 50f;
            
            FindUIElements();
            UpdateUI();
            
            Debug.Log("[Resources] PCResourceSystem initialized");
        }

        private void Update()
        {
            _timeSinceLastTick += Time.deltaTime;
            
            if (_timeSinceLastTick >= tickInterval)
            {
                ProcessTick();
                _timeSinceLastTick = 0f;
            }
        }

        /// <summary>
        /// Process a resource generation tick
        /// </summary>
        private void ProcessTick()
        {
            // Calculate production based on owned territories and buildings
            float goldProduction = CalculateProduction(ResourceType.Gold);
            float stoneProduction = CalculateProduction(ResourceType.Stone);
            float woodProduction = CalculateProduction(ResourceType.Wood);
            float ironProduction = CalculateProduction(ResourceType.Iron);
            float crystalProduction = CalculateProduction(ResourceType.Crystal);
            
            // Add to resources (capped at max)
            AddResource(ResourceType.Gold, goldProduction, false);
            AddResource(ResourceType.Stone, stoneProduction, false);
            AddResource(ResourceType.Wood, woodProduction, false);
            AddResource(ResourceType.Iron, ironProduction, false);
            AddResource(ResourceType.Crystal, crystalProduction, false);
            
            UpdateUI();
            OnTick?.Invoke();
            
            Debug.Log($"[Resources] Tick: +{goldProduction:F0}g, +{stoneProduction:F0}s, +{woodProduction:F0}w");
        }

        /// <summary>
        /// Calculate production for a resource type based on territories and buildings
        /// </summary>
        private float CalculateProduction(ResourceType type)
        {
            float baseRate = type switch
            {
                ResourceType.Gold => goldPerTick,
                ResourceType.Stone => stonePerTick,
                ResourceType.Wood => woodPerTick,
                ResourceType.Iron => ironPerTick,
                ResourceType.Crystal => crystalPerTick,
                _ => 0f
            };
            
            float multiplier = type switch
            {
                ResourceType.Gold => _goldMultiplier,
                ResourceType.Stone => _stoneMultiplier,
                ResourceType.Wood => _woodMultiplier,
                ResourceType.Iron => _ironMultiplier,
                ResourceType.Crystal => _crystalMultiplier,
                _ => 1f
            };
            
            // Get territory bonuses
            float territoryBonus = GetTerritoryProductionBonus(type);
            
            return baseRate * multiplier * (1f + territoryBonus);
        }

        /// <summary>
        /// Get production bonus from owned territories
        /// </summary>
        private float GetTerritoryProductionBonus(ResourceType type)
        {
            // Each territory adds 10% production per level
            float bonus = 0f;
            
            if (WorldMapRenderer.Instance != null)
            {
                var territories = WorldMapRenderer.Instance.AllTerritories;
                if (territories != null)
                {
                    foreach (var territory in territories)
                    {
                        // Check if we own this territory (simplified - would check against user ID)
                        if (!string.IsNullOrEmpty(territory.OwnerId))
                        {
                            bonus += territory.Level * 0.1f;
                        }
                    }
                }
            }
            
            return bonus;
        }

        /// <summary>
        /// Add resources (from collection, rewards, etc.)
        /// </summary>
        public void AddResource(ResourceType type, float amount, bool showEffect = true)
        {
            float max = GetMaxStorage(type);
            
            switch (type)
            {
                case ResourceType.Gold:
                    _gold = Mathf.Min(_gold + amount, max);
                    break;
                case ResourceType.Stone:
                    _stone = Mathf.Min(_stone + amount, max);
                    break;
                case ResourceType.Wood:
                    _wood = Mathf.Min(_wood + amount, max);
                    break;
                case ResourceType.Iron:
                    _iron = Mathf.Min(_iron + amount, max);
                    break;
                case ResourceType.Crystal:
                    _crystal = Mathf.Min(_crystal + amount, max);
                    break;
            }
            
            if (showEffect && showTickEffects)
            {
                ShowResourceGainEffect(type, amount);
            }
            
            OnResourceGained?.Invoke(type, amount);
            OnResourcesUpdated?.Invoke();
            UpdateUI();
        }

        /// <summary>
        /// Spend resources (returns false if insufficient)
        /// </summary>
        public bool SpendResources(int gold = 0, int stone = 0, int wood = 0, int iron = 0, int crystal = 0)
        {
            // Check if we have enough
            if (_gold < gold || _stone < stone || _wood < wood || _iron < iron || _crystal < crystal)
            {
                Debug.Log("[Resources] Insufficient resources!");
                return false;
            }
            
            _gold -= gold;
            _stone -= stone;
            _wood -= wood;
            _iron -= iron;
            _crystal -= crystal;
            
            if (gold > 0) OnResourceSpent?.Invoke(ResourceType.Gold, gold);
            if (stone > 0) OnResourceSpent?.Invoke(ResourceType.Stone, stone);
            if (wood > 0) OnResourceSpent?.Invoke(ResourceType.Wood, wood);
            if (iron > 0) OnResourceSpent?.Invoke(ResourceType.Iron, iron);
            if (crystal > 0) OnResourceSpent?.Invoke(ResourceType.Crystal, crystal);
            
            OnResourcesUpdated?.Invoke();
            UpdateUI();
            return true;
        }

        /// <summary>
        /// Check if player can afford a cost
        /// </summary>
        public bool CanAfford(int gold = 0, int stone = 0, int wood = 0, int iron = 0, int crystal = 0)
        {
            return _gold >= gold && _stone >= stone && _wood >= wood && _iron >= iron && _crystal >= crystal;
        }

        /// <summary>
        /// Set production multiplier for a resource type
        /// </summary>
        public void SetProductionMultiplier(ResourceType type, float multiplier)
        {
            switch (type)
            {
                case ResourceType.Gold: _goldMultiplier = multiplier; break;
                case ResourceType.Stone: _stoneMultiplier = multiplier; break;
                case ResourceType.Wood: _woodMultiplier = multiplier; break;
                case ResourceType.Iron: _ironMultiplier = multiplier; break;
                case ResourceType.Crystal: _crystalMultiplier = multiplier; break;
            }
        }

        private float GetMaxStorage(ResourceType type)
        {
            return type switch
            {
                ResourceType.Gold => maxGold,
                ResourceType.Stone => maxStone,
                ResourceType.Wood => maxWood,
                ResourceType.Iron => maxIron,
                ResourceType.Crystal => maxCrystal,
                _ => 1000
            };
        }

        private void FindUIElements()
        {
            // Try to find UI elements if not assigned
            if (goldText == null)
            {
                var obj = GameObject.Find("GoldText");
                if (obj != null) goldText = obj.GetComponent<TextMeshProUGUI>();
            }
            
            // Also look for the resource HUD created by PCCompleteSetup
            var resourceBar = GameObject.Find("ResourceBar");
            if (resourceBar != null)
            {
                var goldObj = resourceBar.transform.Find("Gold/Value");
                if (goldObj != null) goldText = goldObj.GetComponent<TextMeshProUGUI>();
                
                var stoneObj = resourceBar.transform.Find("Stone/Value");
                if (stoneObj != null) stoneText = stoneObj.GetComponent<TextMeshProUGUI>();
                
                var woodObj = resourceBar.transform.Find("Wood/Value");
                if (woodObj != null) woodText = woodObj.GetComponent<TextMeshProUGUI>();
            }
        }

        private void UpdateUI()
        {
            if (goldText != null) goldText.text = FormatNumber(Gold);
            if (stoneText != null) stoneText.text = FormatNumber(Stone);
            if (woodText != null) woodText.text = FormatNumber(Wood);
            if (ironText != null) ironText.text = FormatNumber(Iron);
            if (crystalText != null) crystalText.text = FormatNumber(Crystal);
            
            // Also update the simple resource display from PCCompleteSetup
            UpdateSimpleResourceDisplay();
        }

        private void UpdateSimpleResourceDisplay()
        {
            // Find the text created by PCCompleteSetup
            var resourceText = GameObject.Find("ResourceText");
            if (resourceText != null)
            {
                var tmp = resourceText.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = $"Gold: {Gold:N0} | Stone: {Stone:N0} | Wood: {Wood:N0}";
                }
            }
        }

        private string FormatNumber(int value)
        {
            if (value >= 1000000) return $"{value / 1000000f:F1}M";
            if (value >= 1000) return $"{value / 1000f:F1}K";
            return value.ToString();
        }

        private void ShowResourceGainEffect(ResourceType type, float amount)
        {
            // TODO: Spawn floating text showing resource gain
            // For now just log
            Debug.Log($"[Resources] +{amount:F0} {type}");
        }

        /// <summary>
        /// Get current resources as a dictionary (for Firebase sync)
        /// </summary>
        public Dictionary<string, int> GetResourceDict()
        {
            return new Dictionary<string, int>
            {
                { "gold", Gold },
                { "stone", Stone },
                { "wood", Wood },
                { "iron", Iron },
                { "crystal", Crystal }
            };
        }

        /// <summary>
        /// Load resources from dictionary (from Firebase)
        /// </summary>
        public void LoadFromDict(Dictionary<string, int> data)
        {
            if (data.TryGetValue("gold", out int gold)) _gold = gold;
            if (data.TryGetValue("stone", out int stone)) _stone = stone;
            if (data.TryGetValue("wood", out int wood)) _wood = wood;
            if (data.TryGetValue("iron", out int iron)) _iron = iron;
            if (data.TryGetValue("crystal", out int crystal)) _crystal = crystal;
            UpdateUI();
        }
    }

    public enum ResourceType
    {
        Gold,
        Stone,
        Wood,
        Iron,
        Crystal,
        ApexCoins // Premium currency
    }
}
