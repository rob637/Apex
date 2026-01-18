using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if FIREBASE_ENABLED
using Firebase.Firestore;
using Firebase.Extensions;
#endif

namespace ApexCitadels.Territory
{
    /// <summary>
    /// Manages all territory operations: claiming, querying, defending, attacking.
    /// Works with Firebase to persist territory data.
    /// </summary>
    public class TerritoryManager : MonoBehaviour
    {
        public static TerritoryManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float defaultTerritoryRadius = 10f;
        [SerializeField] private float minDistanceBetweenTerritories = 5f;
        [SerializeField] private int maxTerritoriesPerPlayer = 10;

        [Header("Visuals")]
        [SerializeField] private GameObject territoryBoundaryPrefab;
        [SerializeField] private Material ownedTerritoryMaterial;
        [SerializeField] private Material enemyTerritoryMaterial;
        [SerializeField] private Material contestedTerritoryMaterial;

        // Events
        public event Action<Territory> OnTerritoryClaimed;
        public event Action<Territory> OnTerritoryLost;
        public event Action<Territory> OnTerritoryAttacked;
        public event Action<Territory, int> OnTerritoryDamaged;
        public event Action<string> OnTerritoryUpdated;
        public event Action<string> OnTerritoryUnderAttack;

        // Local cache of territories
        private Dictionary<string, Territory> _territories = new Dictionary<string, Territory>();
        private Dictionary<string, GameObject> _territoryVisuals = new Dictionary<string, GameObject>();

        // Current player info (would come from auth system)
        private string _currentPlayerId = "local_player";
        private string _currentPlayerName = "Player";

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
            // Load nearby territories on start
            LoadNearbyTerritories();
        }

        /// <summary>
        /// Attempt to claim territory at the given location
        /// </summary>
        public async Task<ClaimResult> TryClaimTerritory(double latitude, double longitude)
        {
            Debug.Log($"[TerritoryManager] Attempting to claim at {latitude}, {longitude}");

            // Check if player has reached max territories
            int playerTerritoryCount = GetPlayerTerritoryCount(_currentPlayerId);
            if (playerTerritoryCount >= maxTerritoriesPerPlayer)
            {
                return new ClaimResult(false, "You've reached the maximum number of territories!");
            }

            // Check if location overlaps with existing territory
            Territory overlapping = FindOverlappingTerritory(latitude, longitude, defaultTerritoryRadius);
            if (overlapping != null)
            {
                if (overlapping.OwnerId == _currentPlayerId)
                {
                    return new ClaimResult(false, "You already own this territory!");
                }
                else
                {
                    return new ClaimResult(false, $"This area is claimed by {overlapping.OwnerName}!");
                }
            }

            // Create new territory
            Territory newTerritory = new Territory
            {
                OwnerId = _currentPlayerId,
                OwnerName = _currentPlayerName,
                CenterLatitude = latitude,
                CenterLongitude = longitude,
                RadiusMeters = defaultTerritoryRadius
            };

            // Save to database
            bool saved = await SaveTerritoryToCloud(newTerritory);
            if (!saved)
            {
                return new ClaimResult(false, "Failed to save territory. Check connection.");
            }

            // Add to local cache
            _territories[newTerritory.Id] = newTerritory;

            // Create visual boundary
            CreateTerritoryVisual(newTerritory);

            // Fire event
            OnTerritoryClaimed?.Invoke(newTerritory);

            Debug.Log($"[TerritoryManager] Territory claimed! ID: {newTerritory.Id}");
            return new ClaimResult(true, "Territory claimed!", newTerritory);
        }

        /// <summary>
        /// Find any territory that overlaps with the given location and radius
        /// </summary>
        public Territory FindOverlappingTerritory(double latitude, double longitude, float radius)
        {
            foreach (var territory in _territories.Values)
            {
                float distance = Territory.CalculateDistance(
                    latitude, longitude,
                    territory.CenterLatitude, territory.CenterLongitude
                );

                if (distance < (radius + territory.RadiusMeters - minDistanceBetweenTerritories))
                {
                    return territory;
                }
            }
            return null;
        }

        /// <summary>
        /// Get territory at a specific location (if any)
        /// </summary>
        public Territory GetTerritoryAtLocation(double latitude, double longitude)
        {
            foreach (var territory in _territories.Values)
            {
                if (territory.ContainsLocation(latitude, longitude))
                {
                    return territory;
                }
            }
            return null;
        }

        /// <summary>
        /// Get all loaded territories
        /// </summary>
        public List<Territory> GetAllTerritories()
        {
            return new List<Territory>(_territories.Values);
        }

        /// <summary>
        /// Get all territories owned by a player
        /// </summary>
        public List<Territory> GetPlayerTerritories(string playerId)
        {
            List<Territory> result = new List<Territory>();
            foreach (var territory in _territories.Values)
            {
                if (territory.OwnerId == playerId)
                {
                    result.Add(territory);
                }
            }
            return result;
        }

        /// <summary>
        /// Count territories owned by a player
        /// </summary>
        public int GetPlayerTerritoryCount(string playerId)
        {
            int count = 0;
            foreach (var territory in _territories.Values)
            {
                if (territory.OwnerId == playerId)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Attack an enemy territory
        /// </summary>
        public async Task<AttackResult> AttackTerritory(string territoryId, int damage)
        {
            if (!_territories.TryGetValue(territoryId, out Territory territory))
            {
                return new AttackResult(false, "Territory not found!");
            }

            if (territory.OwnerId == _currentPlayerId)
            {
                return new AttackResult(false, "You can't attack your own territory!");
            }

            // Apply damage
            bool destroyed = territory.TakeDamage(damage);
            territory.IsContested = true;
            territory.ContestingPlayerId = _currentPlayerId;

            OnTerritoryDamaged?.Invoke(territory, damage);

            if (destroyed)
            {
                // Territory destroyed - remove from previous owner
                string previousOwner = territory.OwnerId;
                
                // Transfer to attacker
                territory.OwnerId = _currentPlayerId;
                territory.OwnerName = _currentPlayerName;
                territory.Health = territory.MaxHealth / 2; // Start at half health
                territory.IsContested = false;
                territory.ContestingPlayerId = null;

                await SaveTerritoryToCloud(territory);
                UpdateTerritoryVisual(territory);

                OnTerritoryLost?.Invoke(territory);
                OnTerritoryClaimed?.Invoke(territory);

                return new AttackResult(true, "Territory conquered!", territory, true);
            }
            else
            {
                await SaveTerritoryToCloud(territory);
                OnTerritoryAttacked?.Invoke(territory);

                return new AttackResult(true, $"Dealt {damage} damage! ({territory.Health}/{territory.MaxHealth} HP)", territory, false);
            }
        }

        /// <summary>
        /// Repair a territory you own
        /// </summary>
        public async Task<bool> RepairTerritory(string territoryId, int repairAmount)
        {
            if (!_territories.TryGetValue(territoryId, out Territory territory))
            {
                return false;
            }

            if (territory.OwnerId != _currentPlayerId)
            {
                return false;
            }

            territory.Repair(repairAmount);
            await SaveTerritoryToCloud(territory);
            return true;
        }

        /// <summary>
        /// Upgrade a territory you own
        /// </summary>
        public async Task<bool> UpgradeTerritory(string territoryId)
        {
            if (!_territories.TryGetValue(territoryId, out Territory territory))
            {
                return false;
            }

            if (territory.OwnerId != _currentPlayerId)
            {
                return false;
            }

            territory.Upgrade();
            await SaveTerritoryToCloud(territory);
            UpdateTerritoryVisual(territory);
            return true;
        }

        #region Visuals

        private void CreateTerritoryVisual(Territory territory)
        {
            if (territoryBoundaryPrefab == null)
            {
                // Create a simple cylinder as fallback
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = $"Territory_{territory.Id}";
                
                // Remove collider
                Destroy(visual.GetComponent<Collider>());

                // Scale: diameter = radius * 2, height very thin
                float diameter = territory.RadiusMeters * 2f;
                visual.transform.localScale = new Vector3(diameter, 0.05f, diameter);

                // Position at territory center (Y=0 for now, adjust for AR)
                // In AR, this would be positioned at ground level
                visual.transform.position = new Vector3(0, 0.05f, 0);

                // Set material based on ownership
                var renderer = visual.GetComponent<Renderer>();
                renderer.material = GetTerritoryMaterial(territory);
                
                // Make semi-transparent
                Color color = renderer.material.color;
                color.a = 0.3f;
                renderer.material.color = color;

                _territoryVisuals[territory.Id] = visual;
            }
            else
            {
                GameObject visual = Instantiate(territoryBoundaryPrefab);
                visual.name = $"Territory_{territory.Id}";
                
                float diameter = territory.RadiusMeters * 2f;
                visual.transform.localScale = new Vector3(diameter, 1f, diameter);

                var renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = GetTerritoryMaterial(territory);
                }

                _territoryVisuals[territory.Id] = visual;
            }
        }

        private void UpdateTerritoryVisual(Territory territory)
        {
            if (_territoryVisuals.TryGetValue(territory.Id, out GameObject visual))
            {
                // Update scale for radius
                float diameter = territory.RadiusMeters * 2f;
                visual.transform.localScale = new Vector3(diameter, visual.transform.localScale.y, diameter);

                // Update material
                var renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = GetTerritoryMaterial(territory);
                }
            }
        }

        private Material GetTerritoryMaterial(Territory territory)
        {
            if (territory.IsContested && contestedTerritoryMaterial != null)
            {
                return contestedTerritoryMaterial;
            }
            else if (territory.OwnerId == _currentPlayerId && ownedTerritoryMaterial != null)
            {
                return ownedTerritoryMaterial;
            }
            else if (enemyTerritoryMaterial != null)
            {
                return enemyTerritoryMaterial;
            }

            // Fallback: create basic material with safe shader lookup
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            
            Material mat;
            if (shader != null) {
                mat = new Material(shader);
            } else {
                // If all else fails, try to use a default material from a primitive
                // preventing the ArgumentNullException
                GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Quad);
                mat = new Material(temp.GetComponent<Renderer>().sharedMaterial);
                Destroy(temp);
            }

            if (territory.OwnerId == _currentPlayerId)
            {
                mat.color = new Color(0, 1, 0, 0.3f); // Green for owned
            }
            else
            {
                mat.color = new Color(1, 0, 0, 0.3f); // Red for enemy
            }
            return mat;
        }

        #endregion

        #region Cloud Sync

        private async void LoadNearbyTerritories()
        {
            Debug.Log("[TerritoryManager] Loading nearby territories...");
            
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                
                // For initial load, get all territories (in production, would use geo-queries)
                var snapshot = await db.Collection("territories").GetSnapshotAsync();
                
                foreach (var doc in snapshot.Documents)
                {
                    var territory = Territory.FromFirestore(doc);
                    if (territory != null && !_territories.ContainsKey(territory.Id))
                    {
                        _territories[territory.Id] = territory;
                        CreateTerritoryVisual(territory);
                    }
                }
                
                Debug.Log($"[TerritoryManager] Loaded {snapshot.Count} territories from Firebase");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TerritoryManager] Failed to load territories: {ex.Message}");
            }
#else
            await Task.Delay(100);
            Debug.Log("[TerritoryManager] Territories loaded (stub)");
#endif
        }

        /// <summary>
        /// Load territories within a radius of a GPS location
        /// </summary>
        public async Task LoadTerritoriesNearLocation(double latitude, double longitude, float radiusMeters = 1000f)
        {
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                
                // Calculate bounding box for geo-query
                double latDelta = radiusMeters / 111320.0;
                double lonDelta = radiusMeters / (111320.0 * Math.Cos(latitude * Math.PI / 180));
                
                var query = db.Collection("territories")
                    .WhereGreaterThan("centerLatitude", latitude - latDelta)
                    .WhereLessThan("centerLatitude", latitude + latDelta);
                
                var snapshot = await query.GetSnapshotAsync();
                
                int loadedCount = 0;
                foreach (var doc in snapshot.Documents)
                {
                    var lon = doc.GetValue<double>("centerLongitude");
                    if (lon < longitude - lonDelta || lon > longitude + lonDelta)
                        continue;
                    
                    var territory = Territory.FromFirestore(doc);
                    if (territory != null)
                    {
                        if (!_territories.ContainsKey(territory.Id))
                        {
                            _territories[territory.Id] = territory;
                            CreateTerritoryVisual(territory);
                        }
                        else
                        {
                            // Update existing territory data
                            _territories[territory.Id] = territory;
                            UpdateTerritoryVisual(territory);
                        }
                        loadedCount++;
                    }
                }
                
                Debug.Log($"[TerritoryManager] Loaded {loadedCount} nearby territories");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TerritoryManager] Failed to load nearby territories: {ex.Message}");
            }
#else
            await Task.Delay(100);
#endif
        }

        private async Task<bool> SaveTerritoryToCloud(Territory territory)
        {
            Debug.Log($"[TerritoryManager] Saving territory {territory.Id} to cloud...");
            
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                var docRef = db.Collection("territories").Document(territory.Id);
                
                await docRef.SetAsync(territory.ToFirestoreData(), SetOptions.MergeAll);
                
                Debug.Log($"[TerritoryManager] Territory saved to Firestore");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TerritoryManager] Failed to save territory: {ex.Message}");
                return false;
            }
#else
            await Task.Delay(100);
            Debug.Log($"[TerritoryManager] Territory saved (stub)");
            return true;
#endif
        }

        #endregion

        #region Player Info

        public void SetCurrentPlayer(string playerId, string playerName)
        {
            _currentPlayerId = playerId;
            _currentPlayerName = playerName;
        }

        public string GetCurrentPlayerId() => _currentPlayerId;

        /// <summary>
        /// Get territories near a location
        /// </summary>
        public async System.Threading.Tasks.Task<List<Territory>> GetNearbyTerritories(double latitude, double longitude, float radiusMeters = 1000f)
        {
            var result = new List<Territory>();
            foreach (var territory in _territories.Values)
            {
                float distance = Territory.CalculateDistance(latitude, longitude, territory.CenterLatitude, territory.CenterLongitude);
                if (distance <= radiusMeters)
                {
                    result.Add(territory);
                }
            }
            await System.Threading.Tasks.Task.CompletedTask;
            return result;
        }

        /// <summary>
        /// Get a territory by ID
        /// </summary>
        public Territory GetTerritory(string territoryId)
        {
            _territories.TryGetValue(territoryId, out Territory territory);
            return territory;
        }

        #endregion
    }

    /// <summary>
    /// Result of a territory claim attempt
    /// </summary>
    public class ClaimResult
    {
        public bool Success;
        public string Message;
        public Territory Territory;

        public ClaimResult(bool success, string message, Territory territory = null)
        {
            Success = success;
            Message = message;
            Territory = territory;
        }
    }

    /// <summary>
    /// Result of a territory attack attempt
    /// </summary>
    public class AttackResult
    {
        public bool Success;
        public string Message;
        public Territory Territory;
        public bool Conquered;

        public AttackResult(bool success, string message, Territory territory = null, bool conquered = false)
        {
            Success = success;
            Message = message;
            Territory = territory;
            Conquered = conquered;
        }
    }
}
