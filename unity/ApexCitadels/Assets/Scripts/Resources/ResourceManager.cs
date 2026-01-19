using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Data;
using ApexCitadels.Territory;
using ApexCitadels.Player;
#if FIREBASE_ENABLED
using Firebase.Firestore;
using Firebase.Extensions;
#endif

namespace ApexCitadels.Resources
{
    /// <summary>
    /// Resource node types in the world
    /// </summary>
    public enum ResourceNodeType
    {
        StoneMine,      // Produces Stone
        Forest,         // Produces Wood  
        OreDeposit,     // Produces Metal
        CrystalCave,    // Produces Crystal
        GemMine         // Produces Gems (rare)
    }

    /// <summary>
    /// Resource node that can be harvested
    /// </summary>
    [Serializable]
    public class ResourceNode
    {
        public string Id;
        public string Name;
        public ResourceNodeType Type;
        public double Latitude;
        public double Longitude;
        
        // State
        public int CurrentAmount;
        public int MaxAmount;
        public DateTime LastHarvestTime;
        public DateTime RegenerationTime;
        
        // Ownership (if within a territory)
        public string TerritoryId;
        public string OwnerId;

        public ResourceNode() { }

        public ResourceNode(ResourceNodeType type, double lat, double lon)
        {
            Id = Guid.NewGuid().ToString();
            Type = type;
            Name = GetNameForType(type);
            Latitude = lat;
            Longitude = lon;
            MaxAmount = GetMaxAmountForType(type);
            CurrentAmount = MaxAmount;
            LastHarvestTime = DateTime.MinValue;
            RegenerationTime = DateTime.UtcNow;
        }

        public bool CanHarvest => CurrentAmount > 0;
        public bool IsRegenerating => CurrentAmount < MaxAmount && DateTime.UtcNow >= RegenerationTime;

        public ResourceType GetResourceType()
        {
            return Type switch
            {
                ResourceNodeType.StoneMine => ResourceType.Stone,
                ResourceNodeType.Forest => ResourceType.Wood,
                ResourceNodeType.OreDeposit => ResourceType.Metal,
                ResourceNodeType.CrystalCave => ResourceType.Crystal,
                ResourceNodeType.GemMine => ResourceType.Gems,
                _ => ResourceType.Stone
            };
        }

        private static string GetNameForType(ResourceNodeType type)
        {
            return type switch
            {
                ResourceNodeType.StoneMine => "Stone Mine",
                ResourceNodeType.Forest => "Forest",
                ResourceNodeType.OreDeposit => "Ore Deposit",
                ResourceNodeType.CrystalCave => "Crystal Cave",
                ResourceNodeType.GemMine => "Gem Mine",
                _ => "Resource Node"
            };
        }

        private static int GetMaxAmountForType(ResourceNodeType type)
        {
            return type switch
            {
                ResourceNodeType.StoneMine => 500,
                ResourceNodeType.Forest => 400,
                ResourceNodeType.OreDeposit => 300,
                ResourceNodeType.CrystalCave => 100,
                ResourceNodeType.GemMine => 20,
                _ => 100
            };
        }
    }

    /// <summary>
    /// Manages resource nodes and harvesting
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("Harvest Settings")]
        [SerializeField] private float harvestRange = 30f; // Meters
        [SerializeField] private float harvestCooldown = 1f; // Seconds between harvests
        [SerializeField] private int baseHarvestAmount = 10;
        [SerializeField] private float regenerationRate = 60f; // Seconds per unit

        [Header("Node Generation")]
        [SerializeField] private float nodeSpawnRadius = 500f; // Meters
        [SerializeField] private int maxNodesPerArea = 20;

        // Events
        public event Action<ResourceType, int> OnResourceHarvested;
        public event Action<ResourceNode> OnNodeDepleted;
        public event Action<ResourceNode> OnNodeDiscovered;

        // State
        private List<ResourceNode> _nearbyNodes = new List<ResourceNode>();
        private ResourceNode _selectedNode;
        private float _lastHarvestTime;
        private bool _isHarvesting;

        public ResourceNode SelectedNode => _selectedNode;
        public bool IsHarvesting => _isHarvesting;
        public List<ResourceNode> NearbyNodes => _nearbyNodes;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // Auto-regenerate nodes
            RegenerateNodes();
        }

        #region Harvesting

        /// <summary>
        /// Select a resource node for harvesting
        /// </summary>
        public bool SelectNode(string nodeId)
        {
            var node = _nearbyNodes.Find(n => n.Id == nodeId);
            if (node == null)
            {
                Debug.Log("[ResourceManager] Node not found!");
                return false;
            }

            _selectedNode = node;
            Debug.Log($"[ResourceManager] Selected: {node.Name} ({node.CurrentAmount}/{node.MaxAmount})");
            return true;
        }

        /// <summary>
        /// Start harvesting the selected node
        /// </summary>
        public void StartHarvesting()
        {
            if (_selectedNode == null)
            {
                Debug.Log("[ResourceManager] No node selected!");
                return;
            }

            if (!_selectedNode.CanHarvest)
            {
                Debug.Log("[ResourceManager] Node is depleted!");
                return;
            }

            _isHarvesting = true;
            Debug.Log($"[ResourceManager] Started harvesting {_selectedNode.Name}");
        }

        /// <summary>
        /// Stop harvesting
        /// </summary>
        public void StopHarvesting()
        {
            _isHarvesting = false;
            Debug.Log("[ResourceManager] Stopped harvesting");
        }

        /// <summary>
        /// Perform a single harvest action
        /// </summary>
        public (bool Success, int Amount) Harvest()
        {
            if (_selectedNode == null)
            {
                return (false, 0);
            }

            if (!_selectedNode.CanHarvest)
            {
                OnNodeDepleted?.Invoke(_selectedNode);
                StopHarvesting();
                return (false, 0);
            }

            // Check cooldown
            if (Time.time - _lastHarvestTime < harvestCooldown)
            {
                return (false, 0);
            }

            // Calculate harvest amount
            int harvestAmount = CalculateHarvestAmount();
            harvestAmount = Mathf.Min(harvestAmount, _selectedNode.CurrentAmount);

            // Remove from node
            _selectedNode.CurrentAmount -= harvestAmount;
            _selectedNode.LastHarvestTime = DateTime.UtcNow;

            // Set regeneration timer if empty
            if (_selectedNode.CurrentAmount <= 0)
            {
                _selectedNode.RegenerationTime = DateTime.UtcNow.AddSeconds(regenerationRate * 10);
            }

            // Give to player
            var resourceType = _selectedNode.GetResourceType();
            PlayerManager.Instance?.AddResource(resourceType, harvestAmount);

            _lastHarvestTime = Time.time;

            OnResourceHarvested?.Invoke(resourceType, harvestAmount);

            // Award XP
            PlayerManager.Instance?.AwardExperience(harvestAmount / 5);

            // Save node state to cloud
            _ = SaveNodeToCloud(_selectedNode);

            Debug.Log($"[ResourceManager] Harvested {harvestAmount} {resourceType}");
            return (true, harvestAmount);
        }

        private int CalculateHarvestAmount()
        {
            int playerLevel = PlayerManager.Instance?.CurrentPlayer?.Level ?? 1;
            float levelBonus = 1f + (playerLevel - 1) * 0.1f; // 10% per level

            // Bonus for owning the territory
            float ownershipBonus = 1f;
            if (!string.IsNullOrEmpty(_selectedNode.OwnerId))
            {
                string currentPlayerId = PlayerManager.Instance?.GetCurrentPlayerId() ?? "";
                if (_selectedNode.OwnerId == currentPlayerId)
                {
                    ownershipBonus = 1.5f; // 50% bonus on your own territory
                }
            }

            return Mathf.RoundToInt(baseHarvestAmount * levelBonus * ownershipBonus);
        }

        #endregion

        #region Node Management

        /// <summary>
        /// Load nearby resource nodes
        /// </summary>
        public async void LoadNearbyNodes(double latitude, double longitude)
        {
            // Clear old distant nodes
            _nearbyNodes.RemoveAll(n => 
                Territory.Territory.CalculateDistance(n.Latitude, n.Longitude, latitude, longitude) > nodeSpawnRadius * 2);

#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                
                // Query resource nodes within a rough bounding box
                double latDelta = nodeSpawnRadius / 111320.0;
                double lonDelta = nodeSpawnRadius / (111320.0 * Math.Cos(latitude * Math.PI / 180));
                
                var query = db.Collection("resource_nodes")
                    .WhereGreaterThan("latitude", latitude - latDelta)
                    .WhereLessThan("latitude", latitude + latDelta);
                    
                var snapshot = await query.GetSnapshotAsync();
                
                foreach (var doc in snapshot.Documents)
                {
                    // Skip if already loaded
                    if (_nearbyNodes.Exists(n => n.Id == doc.Id))
                        continue;
                        
                    var nodeLon = doc.GetValue<double>("longitude");
                    if (nodeLon < longitude - lonDelta || nodeLon > longitude + lonDelta)
                        continue;
                    
                    var node = new ResourceNode
                    {
                        Id = doc.Id,
                        Name = doc.GetValue<string>("name") ?? "Resource Node",
                        Type = (ResourceNodeType)doc.GetValue<long>("type"),
                        Latitude = doc.GetValue<double>("latitude"),
                        Longitude = nodeLon,
                        CurrentAmount = (int)doc.GetValue<long>("currentAmount"),
                        MaxAmount = (int)doc.GetValue<long>("maxAmount"),
                        OwnerId = doc.GetValue<string>("ownerId") ?? "",
                        TerritoryId = doc.GetValue<string>("territoryId") ?? ""
                    };
                    
                    _nearbyNodes.Add(node);
                    OnNodeDiscovered?.Invoke(node);
                }
                
                Debug.Log($"[ResourceManager] Loaded {snapshot.Count} resource nodes from Firebase");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ResourceManager] Failed to load nodes from Firebase: {ex.Message}");
            }
#endif

            // Generate procedural nodes if needed (for areas with no server data)
            if (_nearbyNodes.Count < maxNodesPerArea / 2)
            {
                GenerateNodes(latitude, longitude);
            }
        }

        /// <summary>
        /// Save a resource node to Firebase (after harvesting, etc.)
        /// </summary>
        private async Task SaveNodeToCloud(ResourceNode node)
        {
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                var docRef = db.Collection("resource_nodes").Document(node.Id);
                
                await docRef.SetAsync(new Dictionary<string, object>
                {
                    { "name", node.Name },
                    { "type", (int)node.Type },
                    { "latitude", node.Latitude },
                    { "longitude", node.Longitude },
                    { "currentAmount", node.CurrentAmount },
                    { "maxAmount", node.MaxAmount },
                    { "ownerId", node.OwnerId ?? "" },
                    { "territoryId", node.TerritoryId ?? "" },
                    { "lastHarvestTime", Timestamp.FromDateTime(node.LastHarvestTime.ToUniversalTime()) },
                    { "regenerationTime", Timestamp.FromDateTime(node.RegenerationTime.ToUniversalTime()) }
                }, SetOptions.MergeAll);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ResourceManager] Failed to save node: {ex.Message}");
            }
#else
            await Task.CompletedTask;
#endif
        }

        private void GenerateNodes(double centerLat, double centerLon)
        {
            // Generate random nodes around the player
            System.Random random = new System.Random();
            int nodesToGenerate = maxNodesPerArea - _nearbyNodes.Count;

            for (int i = 0; i < nodesToGenerate; i++)
            {
                // Random offset in meters
                double offsetLat = (random.NextDouble() - 0.5) * 2 * nodeSpawnRadius / 111320;
                double offsetLon = (random.NextDouble() - 0.5) * 2 * nodeSpawnRadius / 
                                   (111320 * Math.Cos(centerLat * Math.PI / 180));

                double lat = centerLat + offsetLat;
                double lon = centerLon + offsetLon;

                // Random type (weighted)
                ResourceNodeType type = GetRandomNodeType(random);

                var node = new ResourceNode(type, lat, lon);
                _nearbyNodes.Add(node);
                OnNodeDiscovered?.Invoke(node);
            }

            Debug.Log($"[ResourceManager] Generated {nodesToGenerate} resource nodes");
        }

        private ResourceNodeType GetRandomNodeType(System.Random random)
        {
            int roll = random.Next(100);

            if (roll < 30) return ResourceNodeType.StoneMine;      // 30%
            if (roll < 55) return ResourceNodeType.Forest;          // 25%
            if (roll < 75) return ResourceNodeType.OreDeposit;      // 20%
            if (roll < 90) return ResourceNodeType.CrystalCave;     // 15%
            return ResourceNodeType.GemMine;                         // 10%
        }

        private void RegenerateNodes()
        {
            foreach (var node in _nearbyNodes)
            {
                if (node.CurrentAmount < node.MaxAmount && DateTime.UtcNow >= node.RegenerationTime)
                {
                    // Regenerate 1 unit
                    node.CurrentAmount++;
                    node.RegenerationTime = DateTime.UtcNow.AddSeconds(regenerationRate);
                }
            }
        }

        #endregion

        #region Passive Income

        /// <summary>
        /// Calculate passive resource income from owned nodes
        /// </summary>
        public Dictionary<ResourceType, int> CalculatePassiveIncome()
        {
            var income = new Dictionary<ResourceType, int>
            {
                { ResourceType.Stone, 0 },
                { ResourceType.Wood, 0 },
                { ResourceType.Metal, 0 },
                { ResourceType.Crystal, 0 },
                { ResourceType.Gems, 0 }
            };

            string playerId = PlayerManager.Instance?.GetCurrentPlayerId() ?? "";

            foreach (var node in _nearbyNodes)
            {
                if (node.OwnerId == playerId)
                {
                    var resourceType = node.GetResourceType();
                    
                    // Base income per hour
                    int hourlyIncome = node.Type switch
                    {
                        ResourceNodeType.StoneMine => 100,
                        ResourceNodeType.Forest => 80,
                        ResourceNodeType.OreDeposit => 50,
                        ResourceNodeType.CrystalCave => 20,
                        ResourceNodeType.GemMine => 2,
                        _ => 10
                    };

                    income[resourceType] += hourlyIncome;
                }
            }

            return income;
        }

        /// <summary>
        /// Get the current amount of a specific resource by name
        /// </summary>
        public int GetResource(string resourceName)
        {
            // Delegate to PlayerManager for actual resource counts
            var player = PlayerManager.Instance?.CurrentPlayer;
            if (player == null) return 0;

            return resourceName.ToLower() switch
            {
                "gems" => player.Gems,
                "coins" or "gold" => player.Gems, // Use gems as currency
                "stone" => player.Stone,
                "wood" => player.Wood,
                "metal" or "iron" => player.Metal,
                "crystal" => player.Crystal,
                _ => 0
            };
        }

        /// <summary>
        /// Collect passive income (called periodically)
        /// </summary>
        public void CollectPassiveIncome(float hoursElapsed)
        {
            var income = CalculatePassiveIncome();

            foreach (var kvp in income)
            {
                if (kvp.Value > 0)
                {
                    int amount = Mathf.RoundToInt(kvp.Value * hoursElapsed);
                    if (amount > 0)
                    {
                        PlayerManager.Instance?.AddResource(kvp.Key, amount);
                        Debug.Log($"[ResourceManager] Collected {amount} {kvp.Key} (passive)");
                    }
                }
            }
        }

        #endregion
    }
}
