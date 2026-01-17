using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Territory;
using ApexCitadels.Building;
using ApexCitadels.Core;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Bridges the PC client systems with the core TerritoryManager.
    /// Handles PC-specific territory operations and data transformations.
    /// </summary>
    public class PCTerritoryBridge : MonoBehaviour
    {
        public static PCTerritoryBridge Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float dataRefreshInterval = 5f;
        [SerializeField] private int maxVisibleTerritories = 100;
        [SerializeField] private float loadRadius = 50000f; // 50km for PC overview

        // Events
        public event Action<List<Territory.Territory>> OnTerritoriesLoaded;
        public event Action<Territory.Territory> OnTerritorySelected;
        public event Action<List<BuildingBlock>> OnBuildingsLoaded;

        // State
        private TerritoryManager _territoryManager;
        private BuildingManager _buildingManager;
        private WorldMapRenderer _worldMapRenderer;
        private Dictionary<string, TerritoryPCData> _territoryCache = new Dictionary<string, TerritoryPCData>();
        private string _selectedTerritoryId;
        private float _lastRefreshTime;

        public string SelectedTerritoryId => _selectedTerritoryId;
        public TerritoryPCData SelectedTerritoryData => 
            _selectedTerritoryId != null && _territoryCache.TryGetValue(_selectedTerritoryId, out var data) 
            ? data : null;

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
            // Get references
            _territoryManager = TerritoryManager.Instance;
            _buildingManager = BuildingManager.Instance;
            _worldMapRenderer = WorldMapRenderer.Instance;

            // Subscribe to events
            if (_territoryManager != null)
            {
                _territoryManager.OnTerritoryClaimed += HandleTerritoryClaimed;
                _territoryManager.OnTerritoryLost += HandleTerritoryLost;
                _territoryManager.OnTerritoryAttacked += HandleTerritoryAttacked;
            }

            if (_worldMapRenderer != null)
            {
                _worldMapRenderer.OnTerritoryClicked += SelectTerritory;
            }

            // Initial load
            RefreshTerritoryData();
        }

        private void Update()
        {
            // Periodic refresh
            if (Time.time - _lastRefreshTime > dataRefreshInterval)
            {
                RefreshTerritoryData();
                _lastRefreshTime = Time.time;
            }
        }

        #region Data Loading

        /// <summary>
        /// Refresh all territory data from the manager
        /// </summary>
        public void RefreshTerritoryData()
        {
            if (_territoryManager == null) return;

            List<Territory.Territory> territories = _territoryManager.GetAllTerritories();

            // Update cache with enriched PC data
            foreach (var territory in territories)
            {
                if (!_territoryCache.ContainsKey(territory.Id))
                {
                    _territoryCache[territory.Id] = new TerritoryPCData();
                }

                var pcData = _territoryCache[territory.Id];
                pcData.Territory = territory;
                pcData.WorldPosition = GPSToWorldPosition(territory.CenterLatitude, territory.CenterLongitude);
                pcData.LastUpdated = DateTime.UtcNow;

                // Load building count
                if (_buildingManager != null)
                {
                    pcData.BuildingCount = _buildingManager.GetBlocksInTerritory(territory.Id)?.Count ?? 0;
                }

                // Calculate stats
                pcData.DefenseRating = CalculateDefenseRating(territory);
                pcData.IncomeRate = CalculateIncomeRate(territory);
            }

            OnTerritoriesLoaded?.Invoke(territories);

            // Update world map
            _worldMapRenderer?.RefreshVisibleTerritories();
        }

        /// <summary>
        /// Load detailed data for a specific territory
        /// </summary>
        public async Task<TerritoryPCData> LoadTerritoryDetails(string territoryId)
        {
            if (_territoryManager == null) return null;

            // Get base territory
            var territories = _territoryManager.GetAllTerritories();
            var territory = territories.Find(t => t.Id == territoryId);

            if (territory == null) return null;

            // Ensure cache entry
            if (!_territoryCache.ContainsKey(territoryId))
            {
                _territoryCache[territoryId] = new TerritoryPCData();
            }

            var pcData = _territoryCache[territoryId];
            pcData.Territory = territory;
            pcData.WorldPosition = GPSToWorldPosition(territory.CenterLatitude, territory.CenterLongitude);

            // Load buildings
            if (_buildingManager != null)
            {
                pcData.Buildings = _buildingManager.GetBlocksInTerritory(territoryId);
                pcData.BuildingCount = pcData.Buildings?.Count ?? 0;
            }

            // Load activity log (would come from backend)
            pcData.RecentActivity = await LoadActivityLog(territoryId);

            pcData.LastUpdated = DateTime.UtcNow;
            pcData.IsDetailLoaded = true;

            return pcData;
        }

        /// <summary>
        /// Get all player-owned territories
        /// </summary>
        public List<TerritoryPCData> GetOwnedTerritories()
        {
            string playerId = GameManager.Instance?.UserId;
            if (string.IsNullOrEmpty(playerId)) return new List<TerritoryPCData>();

            List<TerritoryPCData> owned = new List<TerritoryPCData>();
            foreach (var kvp in _territoryCache)
            {
                if (kvp.Value.Territory?.OwnerId == playerId)
                {
                    owned.Add(kvp.Value);
                }
            }
            return owned;
        }

        /// <summary>
        /// Get alliance territories
        /// </summary>
        public List<TerritoryPCData> GetAllianceTerritories(string allianceId)
        {
            List<TerritoryPCData> allianceTerritories = new List<TerritoryPCData>();
            foreach (var kvp in _territoryCache)
            {
                if (kvp.Value.Territory?.AllianceId == allianceId)
                {
                    allianceTerritories.Add(kvp.Value);
                }
            }
            return allianceTerritories;
        }

        #endregion

        #region Selection

        /// <summary>
        /// Select a territory for viewing/editing
        /// </summary>
        public async void SelectTerritory(string territoryId)
        {
            _selectedTerritoryId = territoryId;

            // Load detailed data
            var data = await LoadTerritoryDetails(territoryId);

            if (data != null)
            {
                OnTerritorySelected?.Invoke(data.Territory);

                // Notify UI
                if (PC.UI.PCUIManager.Instance != null)
                {
                    PC.UI.PCUIManager.Instance.ShowTerritoryDetail(data.Territory);
                }
            }
        }

        /// <summary>
        /// Deselect current territory
        /// </summary>
        public void DeselectTerritory()
        {
            _selectedTerritoryId = null;
        }

        #endregion

        #region PC Actions

        /// <summary>
        /// Upgrade territory from PC (if allowed)
        /// </summary>
        public async Task<bool> UpgradeTerritory(string territoryId)
        {
            if (!PlatformManager.IsFeatureAvailable(GameFeature.ManageBuildings))
            {
                Debug.LogWarning("[PCBridge] Upgrade not available on this platform");
                return false;
            }

            if (_territoryManager == null) return false;

            // Get territory
            if (!_territoryCache.TryGetValue(territoryId, out var data))
                return false;

            // Check costs (would verify with ResourceManager)
            // For now, just upgrade
            data.Territory.Upgrade();

            // Refresh
            RefreshTerritoryData();
            return true;
        }

        /// <summary>
        /// Place building from PC editor
        /// </summary>
        public async Task<bool> PlaceBuildingPC(string territoryId, BlockType type, Vector3 localPosition, float rotation)
        {
            if (_buildingManager == null) return false;

            // PC can place buildings (but not first-time in new territory)
            if (!_territoryCache.TryGetValue(territoryId, out var data))
                return false;

            // Create building block
            var block = new BuildingBlock(type)
            {
                TerritoryId = territoryId,
                OwnerId = GameManager.Instance?.UserId,
                LocalPosition = localPosition,
                LocalRotation = Quaternion.Euler(0, rotation, 0)
            };

            // Save through building manager
            var placedObj = _buildingManager.PlaceBlockAt(block);
            bool success = placedObj != null;

            if (success)
            {
                // Update cache
                data.BuildingCount++;
                data.Buildings?.Add(block);

                // Recalculate stats
                data.DefenseRating = CalculateDefenseRating(data.Territory);
            }

            return success;
        }

        /// <summary>
        /// Save blueprint from PC editor
        /// </summary>
        public async Task<bool> SaveBlueprint(string territoryId, string name, string description)
        {
            if (!PlatformManager.IsFeatureAvailable(GameFeature.BlueprintDesigner))
            {
                Debug.LogWarning("[PCBridge] Blueprint designer not available on this platform");
                return false;
            }

            if (!_territoryCache.TryGetValue(territoryId, out var data))
                return false;

            // Create blueprint from current buildings
            var blueprint = new Data.Blueprint
            {
                Id = Guid.NewGuid().ToString(),
                OwnerId = GameManager.Instance?.UserId,
                Name = name,
                Description = description,
                SourceTerritoryId = territoryId,
                Buildings = new List<Data.BuildingPlacement>()
            };

            // Convert buildings to placements
            if (data.Buildings != null)
            {
                foreach (var block in data.Buildings)
                {
                    blueprint.Buildings.Add(new Data.BuildingPlacement
                    {
                        BlockType = block.Type.ToString(),
                        PositionX = block.LocalPosition.x,
                        PositionY = block.LocalPosition.y,
                        PositionZ = block.LocalPosition.z,
                        RotationY = block.LocalRotation.eulerAngles.y,
                        PlacedAt = DateTime.UtcNow
                    });
                }
            }

            // Save to backend (would use BlueprintService)
            Debug.Log($"[PCBridge] Saved blueprint: {name} with {blueprint.Buildings.Count} buildings");
            return true;
        }

        #endregion

        #region Calculations

        private int CalculateDefenseRating(Territory.Territory territory)
        {
            if (territory == null) return 0;

            int baseDefense = territory.Level * 10;
            int healthBonus = (territory.Health * 20) / territory.MaxHealth;

            // Would add building-based defense
            return baseDefense + healthBonus;
        }

        private int CalculateIncomeRate(Territory.Territory territory)
        {
            if (territory == null) return 0;

            int baseIncome = 10 + territory.Level * 5;

            // Would calculate from resource buildings
            return baseIncome;
        }

        private async Task<List<ActivityEntry>> LoadActivityLog(string territoryId)
        {
            // Would load from backend
            return new List<ActivityEntry>
            {
                new ActivityEntry { Message = "Territory defended successfully", Type = "defense", Time = DateTime.UtcNow.AddHours(-2) },
                new ActivityEntry { Message = "+150 Stone collected", Type = "resource", Time = DateTime.UtcNow.AddHours(-4) },
                new ActivityEntry { Message = "Wall upgraded to Level 3", Type = "build", Time = DateTime.UtcNow.AddDays(-1) }
            };
        }

        #endregion

        #region Coordinate Conversion

        private Vector3 GPSToWorldPosition(double latitude, double longitude)
        {
            // Simple mercator projection - matches WorldMapRenderer
            const double metersPerDegree = 111319.9;
            float x = (float)(longitude * metersPerDegree / 10);
            float z = (float)(latitude * metersPerDegree / 10);
            return new Vector3(x, 0, z);
        }

        #endregion

        #region Event Handlers

        private void HandleTerritoryClaimed(Territory.Territory territory)
        {
            // Add to cache
            _territoryCache[territory.Id] = new TerritoryPCData
            {
                Territory = territory,
                WorldPosition = GPSToWorldPosition(territory.CenterLatitude, territory.CenterLongitude),
                LastUpdated = DateTime.UtcNow
            };

            RefreshTerritoryData();
        }

        private void HandleTerritoryLost(Territory.Territory territory)
        {
            _territoryCache.Remove(territory.Id);

            if (_selectedTerritoryId == territory.Id)
            {
                DeselectTerritory();
            }

            RefreshTerritoryData();
        }

        private void HandleTerritoryAttacked(Territory.Territory territory)
        {
            if (_territoryCache.TryGetValue(territory.Id, out var data))
            {
                data.IsUnderAttack = true;
                data.Territory = territory;
            }

            // Notify UI
            PC.UI.PCUIManager.Instance?.ShowNotification(
                $"Territory {territory.Name} is under attack!",
                PC.UI.NotificationType.Combat
            );
        }

        #endregion

        private void OnDestroy()
        {
            if (_territoryManager != null)
            {
                _territoryManager.OnTerritoryClaimed -= HandleTerritoryClaimed;
                _territoryManager.OnTerritoryLost -= HandleTerritoryLost;
                _territoryManager.OnTerritoryAttacked -= HandleTerritoryAttacked;
            }
        }
    }

    /// <summary>
    /// Extended territory data for PC client
    /// </summary>
    [Serializable]
    public class TerritoryPCData
    {
        public Territory.Territory Territory;
        public Vector3 WorldPosition;
        public List<BuildingBlock> Buildings;
        public int BuildingCount;
        public int DefenseRating;
        public int IncomeRate;
        public bool IsUnderAttack;
        public bool IsDetailLoaded;
        public DateTime LastUpdated;
        public List<ActivityEntry> RecentActivity;
    }

    /// <summary>
    /// Activity log entry
    /// </summary>
    [Serializable]
    public class ActivityEntry
    {
        public string Message;
        public string Type;
        public DateTime Time;
    }
}
