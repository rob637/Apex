using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using ApexCitadels.Core;

namespace ApexCitadels.WorldSeed
{
    /// <summary>
    /// Manages world seed content - pre-populated locations like ruins, resources, landmarks
    /// </summary>
    public class WorldSeedManager : MonoBehaviour
    {
        public static WorldSeedManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private float queryRadius = 1000f;  // meters
        [SerializeField] private float refreshInterval = 30f;
        [SerializeField] private float proximityThreshold = 100f;  // meters to trigger discovery

        [Header("Prefabs")]
        [SerializeField] private GameObject ancientRuinsPrefab;
        [SerializeField] private GameObject abandonedFortPrefab;
        [SerializeField] private GameObject sacredSitePrefab;
        [SerializeField] private GameObject resourceHotspotPrefab;
        [SerializeField] private GameObject landmarkPrefab;
        [SerializeField] private GameObject npcTerritoryPrefab;
        [SerializeField] private GameObject mysteryLocationPrefab;
        [SerializeField] private GameObject historicalSitePrefab;
        [SerializeField] private GameObject defaultSeedPrefab;

        [Header("Visual Settings")]
        [SerializeField] private bool showBeacons = true;
        [SerializeField] private float beaconHeight = 50f;
        [SerializeField] private Color undiscoveredColor = new Color(1f, 0.8f, 0f, 0.8f);
        [SerializeField] private Color discoveredColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        // Events
        public UnityEvent<SeedPoint> OnSeedDiscovered;
        public UnityEvent<SeedPoint, List<ResourceReward>> OnResourcesClaimed;
        public UnityEvent<SeedPoint> OnSeedApproached;

        // State
        private Dictionary<string, SeedPointInstance> _activeSeedPoints = new Dictionary<string, SeedPointInstance>();
        private HashSet<string> _discoveredSeeds = new HashSet<string>();
        private double _lastQueryLat;
        private double _lastQueryLon;
        private float _lastRefreshTime;
        private bool _isRefreshing;

        // Player location
        private double _playerLat;
        private double _playerLon;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            OnSeedDiscovered = new UnityEvent<SeedPoint>();
            OnResourcesClaimed = new UnityEvent<SeedPoint, List<ResourceReward>>();
            OnSeedApproached = new UnityEvent<SeedPoint>();
        }

        private void Start()
        {
            LoadDiscoveredSeeds();
            StartCoroutine(RefreshLoop());
        }

        private void Update()
        {
            // Check proximity to seed points
            CheckSeedProximity();
        }

        #region Public API

        /// <summary>
        /// Update the player's current location
        /// </summary>
        public void UpdatePlayerLocation(double latitude, double longitude)
        {
            _playerLat = latitude;
            _playerLon = longitude;

            // Trigger refresh if moved significantly
            float distance = CalculateDistance(_playerLat, _playerLon, _lastQueryLat, _lastQueryLon);
            if (distance > queryRadius * 0.5f)
            {
                RefreshNearbySeeds();
            }
        }

        /// <summary>
        /// Force refresh of nearby seed points
        /// </summary>
        public async void RefreshNearbySeeds()
        {
            if (_isRefreshing) return;
            if (_playerLat == 0 && _playerLon == 0) return;

            _isRefreshing = true;

            try
            {
                var result = await Backend.FirebaseManager.Instance.CallFunction<NearbySeeds>(
                    "getNearbySeeds",
                    new Dictionary<string, object>
                    {
                        { "latitude", _playerLat },
                        { "longitude", _playerLon },
                        { "radiusMeters", queryRadius }
                    }
                );

                if (result?.seeds != null)
                {
                    ProcessSeedPoints(result.seeds);
                }

                _lastQueryLat = _playerLat;
                _lastQueryLon = _playerLon;
                _lastRefreshTime = Time.time;
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"[WorldSeed] Failed to fetch seeds: {e.Message}", ApexLogger.LogCategory.General);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        /// <summary>
        /// Attempt to discover a seed point (must be nearby)
        /// </summary>
        public async Task<DiscoveryResult> DiscoverSeed(string seedId)
        {
            if (!_activeSeedPoints.TryGetValue(seedId, out SeedPointInstance instance))
            {
                return new DiscoveryResult { success = false, error = "Seed not found" };
            }

            if (_discoveredSeeds.Contains(seedId))
            {
                return new DiscoveryResult { success = false, error = "Already discovered" };
            }

            float distance = CalculateDistance(_playerLat, _playerLon, instance.seed.latitude, instance.seed.longitude);
            if (distance > proximityThreshold)
            {
                return new DiscoveryResult { success = false, error = "Too far away" };
            }

            try
            {
                var result = await Backend.FirebaseManager.Instance.CallFunction<DiscoveryResult>(
                    "discoverSeed",
                    new Dictionary<string, object>
                    {
                        { "seedId", seedId },
                        { "latitude", _playerLat },
                        { "longitude", _playerLon }
                    }
                );

                if (result.success)
                {
                    _discoveredSeeds.Add(seedId);
                    SaveDiscoveredSeeds();
                    UpdateSeedVisual(seedId, true);
                    OnSeedDiscovered?.Invoke(instance.seed);
                }

                return result;
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"[WorldSeed] Discovery failed: {e.Message}", ApexLogger.LogCategory.General);
                return new DiscoveryResult { success = false, error = e.Message };
            }
        }

        /// <summary>
        /// Claim resources from a hotspot (must be nearby and discovered)
        /// </summary>
        public async Task<ResourceClaimResult> ClaimResources(string seedId)
        {
            if (!_activeSeedPoints.TryGetValue(seedId, out SeedPointInstance instance))
            {
                return new ResourceClaimResult { success = false, error = "Seed not found" };
            }

            if (instance.seed.type != SeedPointType.ResourceHotspot)
            {
                return new ResourceClaimResult { success = false, error = "Not a resource hotspot" };
            }

            float distance = CalculateDistance(_playerLat, _playerLon, instance.seed.latitude, instance.seed.longitude);
            if (distance > proximityThreshold)
            {
                return new ResourceClaimResult { success = false, error = "Too far away" };
            }

            try
            {
                var result = await Backend.FirebaseManager.Instance.CallFunction<ResourceClaimResult>(
                    "claimSeedResources",
                    new Dictionary<string, object>
                    {
                        { "seedId", seedId },
                        { "latitude", _playerLat },
                        { "longitude", _playerLon }
                    }
                );

                if (result.success && result.rewards != null)
                {
                    OnResourcesClaimed?.Invoke(instance.seed, result.rewards);
                }

                return result;
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"[WorldSeed] Resource claim failed: {e.Message}", ApexLogger.LogCategory.General);
                return new ResourceClaimResult { success = false, error = e.Message };
            }
        }

        /// <summary>
        /// Get seed point by ID
        /// </summary>
        public SeedPoint GetSeedPoint(string seedId)
        {
            return _activeSeedPoints.TryGetValue(seedId, out SeedPointInstance instance) ? instance.seed : null;
        }

        /// <summary>
        /// Get all active seed points
        /// </summary>
        public List<SeedPoint> GetAllSeedPoints()
        {
            List<SeedPoint> seeds = new List<SeedPoint>();
            foreach (var instance in _activeSeedPoints.Values)
            {
                seeds.Add(instance.seed);
            }
            return seeds;
        }

        /// <summary>
        /// Check if a seed has been discovered
        /// </summary>
        public bool IsDiscovered(string seedId)
        {
            return _discoveredSeeds.Contains(seedId);
        }

        #endregion

        #region Processing

        private void ProcessSeedPoints(List<SeedPoint> seeds)
        {
            // Track which seeds are still valid
            HashSet<string> validIds = new HashSet<string>();

            foreach (var seed in seeds)
            {
                validIds.Add(seed.id);

                if (!_activeSeedPoints.ContainsKey(seed.id))
                {
                    // Create new seed point
                    CreateSeedPointInstance(seed);
                }
                else
                {
                    // Update existing
                    _activeSeedPoints[seed.id].seed = seed;
                }

                // Update discovered state from server
                if (seed.discovered)
                {
                    _discoveredSeeds.Add(seed.id);
                }
            }

            // Remove seeds no longer in range
            List<string> toRemove = new List<string>();
            foreach (var id in _activeSeedPoints.Keys)
            {
                if (!validIds.Contains(id))
                {
                    toRemove.Add(id);
                }
            }

            foreach (var id in toRemove)
            {
                RemoveSeedPointInstance(id);
            }
        }

        private void CreateSeedPointInstance(SeedPoint seed)
        {
            GameObject prefab = GetPrefabForType(seed.type);
            GameObject instance = Instantiate(prefab);

            // Position (using AR coordinate system)
            Vector3 position = GeoToWorld(seed.latitude, seed.longitude);
            instance.transform.position = position;

            // Setup visual
            SeedPointVisual visual = instance.GetComponent<SeedPointVisual>();
            if (visual == null)
            {
                visual = instance.AddComponent<SeedPointVisual>();
            }

            visual.Initialize(seed, _discoveredSeeds.Contains(seed.id));

            // Add beacon if enabled
            if (showBeacons)
            {
                CreateBeacon(instance, seed);
            }

            _activeSeedPoints[seed.id] = new SeedPointInstance
            {
                seed = seed,
                gameObject = instance,
                visual = visual
            };

            ApexLogger.Log($"[WorldSeed] Created seed: {seed.name} ({seed.type})", ApexLogger.LogCategory.General);
        }

        private void RemoveSeedPointInstance(string seedId)
        {
            if (_activeSeedPoints.TryGetValue(seedId, out SeedPointInstance instance))
            {
                if (instance.gameObject != null)
                {
                    Destroy(instance.gameObject);
                }
                _activeSeedPoints.Remove(seedId);
            }
        }

        private void UpdateSeedVisual(string seedId, bool discovered)
        {
            if (_activeSeedPoints.TryGetValue(seedId, out SeedPointInstance instance))
            {
                instance.visual?.SetDiscovered(discovered);
            }
        }

        private GameObject GetPrefabForType(SeedPointType type)
        {
            return type switch
            {
                SeedPointType.AncientRuins => ancientRuinsPrefab ?? defaultSeedPrefab ?? CreateDefaultPrefab(),
                SeedPointType.AbandonedFort => abandonedFortPrefab ?? defaultSeedPrefab ?? CreateDefaultPrefab(),
                SeedPointType.SacredSite => sacredSitePrefab ?? defaultSeedPrefab ?? CreateDefaultPrefab(),
                SeedPointType.ResourceHotspot => resourceHotspotPrefab ?? defaultSeedPrefab ?? CreateDefaultPrefab(),
                SeedPointType.Landmark => landmarkPrefab ?? defaultSeedPrefab ?? CreateDefaultPrefab(),
                SeedPointType.NpcTerritory => npcTerritoryPrefab ?? defaultSeedPrefab ?? CreateDefaultPrefab(),
                SeedPointType.MysteryLocation => mysteryLocationPrefab ?? defaultSeedPrefab ?? CreateDefaultPrefab(),
                SeedPointType.HistoricalSite => historicalSitePrefab ?? defaultSeedPrefab ?? CreateDefaultPrefab(),
                _ => defaultSeedPrefab ?? CreateDefaultPrefab()
            };
        }

        private GameObject CreateDefaultPrefab()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.transform.localScale = new Vector3(5f, 2f, 5f);

            // Add glowing material
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader s = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                if (s != null)
                    renderer.material = new Material(s);
                else if (renderer.sharedMaterial != null)
                    renderer.material = new Material(renderer.sharedMaterial); // Fallback
                    
                if (renderer.material.HasProperty("_Color"))
                    renderer.material.color = undiscoveredColor;
                else if (renderer.material.HasProperty("_BaseColor"))
                    renderer.material.SetColor("_BaseColor", undiscoveredColor);
                    
                renderer.material.EnableKeyword("_EMISSION");
                if (renderer.material.HasProperty("_EmissionColor"))
                    renderer.material.SetColor("_EmissionColor", undiscoveredColor * 0.5f);
            }

            return obj;
        }

        private void CreateBeacon(GameObject seedObject, SeedPoint seed)
        {
            GameObject beacon = new GameObject("Beacon");
            beacon.transform.SetParent(seedObject.transform);
            beacon.transform.localPosition = Vector3.zero;

            LineRenderer line = beacon.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, Vector3.up * beaconHeight);
            line.startWidth = 0.5f;
            line.endWidth = 0.1f;

            Color color = _discoveredSeeds.Contains(seed.id) ? discoveredColor : undiscoveredColor;
            
            Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            if (s != null)
                line.material = new Material(s);
            
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0f);
        }

        #endregion

        #region Proximity Detection

        private void CheckSeedProximity()
        {
            foreach (var kvp in _activeSeedPoints)
            {
                var instance = kvp.Value;
                float distance = CalculateDistance(_playerLat, _playerLon, instance.seed.latitude, instance.seed.longitude);

                bool wasNear = instance.isNear;
                instance.isNear = distance <= proximityThreshold;

                if (instance.isNear && !wasNear)
                {
                    OnSeedApproached?.Invoke(instance.seed);

                    // Auto-discover if not yet discovered
                    if (!_discoveredSeeds.Contains(instance.seed.id))
                    {
                        _ = DiscoverSeed(instance.seed.id);
                    }
                }
            }
        }

        #endregion

        #region Persistence

        private void LoadDiscoveredSeeds()
        {
            string json = PlayerPrefs.GetString("DiscoveredSeeds", "[]");
            try
            {
                var ids = JsonUtility.FromJson<StringList>(json);
                if (ids?.items != null)
                {
                    foreach (var id in ids.items)
                    {
                        _discoveredSeeds.Add(id);
                    }
                }
            }
            catch (Exception e)
            {
                ApexLogger.LogWarning($"[WorldSeed] Failed to load discoveries: {e.Message}", ApexLogger.LogCategory.General);
            }
        }

        private void SaveDiscoveredSeeds()
        {
            var list = new StringList { items = new List<string>(_discoveredSeeds) };
            string json = JsonUtility.ToJson(list);
            PlayerPrefs.SetString("DiscoveredSeeds", json);
            PlayerPrefs.Save();
        }

        #endregion

        #region Utilities

        private IEnumerator RefreshLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(refreshInterval);
                RefreshNearbySeeds();
            }
        }

        private float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // Earth radius in meters
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return (float)(R * c);
        }

        private Vector3 GeoToWorld(double lat, double lon)
        {
            // Convert lat/lon offset from player to local Unity coordinates
            double latOffset = lat - _playerLat;
            double lonOffset = lon - _playerLon;

            // Approximate meters per degree
            const double metersPerDegLat = 111000;
            double metersPerDegLon = 111000 * Math.Cos(_playerLat * Math.PI / 180);

            float x = (float)(lonOffset * metersPerDegLon);
            float z = (float)(latOffset * metersPerDegLat);

            return new Vector3(x, 0, z);
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class SeedPoint
    {
        public string id;
        public SeedPointType type;
        public string name;
        public string description;
        public double latitude;
        public double longitude;
        public float radius;
        public int level;
        public List<ResourceReward> resources;
        public List<SeedBonus> bonuses;
        public DiscoveryReward discoveryReward;
        public float respawnHours;
        public bool discovered;
    }

    [Serializable]
    public enum SeedPointType
    {
        AncientRuins,
        AbandonedFort,
        SacredSite,
        ResourceHotspot,
        Landmark,
        NpcTerritory,
        MysteryLocation,
        HistoricalSite
    }

    [Serializable]
    public class ResourceReward
    {
        public string type;
        public int minAmount;
        public int maxAmount;
        public float probability;
        public int amount;  // Actual amount received
    }

    [Serializable]
    public class SeedBonus
    {
        public string type;
        public float value;
        public float durationHours;
        public float radius;
    }

    [Serializable]
    public class DiscoveryReward
    {
        public int xp;
        public List<ResourceReward> resources;
        public string achievement;
    }

    [Serializable]
    public class NearbySeeds
    {
        public List<SeedPoint> seeds;
    }

    [Serializable]
    public class DiscoveryResult
    {
        public bool success;
        public string error;
        public DiscoveryReward rewards;
        public SeedInfo seed;
    }

    [Serializable]
    public class SeedInfo
    {
        public string id;
        public string name;
        public string type;
        public string description;
    }

    [Serializable]
    public class ResourceClaimResult
    {
        public bool success;
        public string error;
        public List<ResourceReward> rewards;
        public int nextClaimIn;
    }

    [Serializable]
    public class StringList
    {
        public List<string> items;
    }

    public class SeedPointInstance
    {
        public SeedPoint seed;
        public GameObject gameObject;
        public SeedPointVisual visual;
        public bool isNear;
    }

    #endregion

    #region Visual Component

    /// <summary>
    /// Visual component for seed point GameObjects
    /// </summary>
    public class SeedPointVisual : MonoBehaviour
    {
        private SeedPoint _seed;
        private bool _discovered;
        private Renderer _renderer;
        private LineRenderer _beacon;

        [Header("Colors")]
        public Color undiscoveredColor = new Color(1f, 0.8f, 0f, 0.8f);
        public Color discoveredColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        public void Initialize(SeedPoint seed, bool discovered)
        {
            _seed = seed;
            _discovered = discovered;

            _renderer = GetComponent<Renderer>();
            _beacon = GetComponentInChildren<LineRenderer>();

            UpdateVisual();

            // Add type-specific visual elements
            SetupTypeVisual();
        }

        public void SetDiscovered(bool discovered)
        {
            _discovered = discovered;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            Color color = _discovered ? discoveredColor : undiscoveredColor;

            if (_renderer != null)
            {
                _renderer.material.color = color;
                if (_renderer.material.HasProperty("_EmissionColor"))
                {
                    _renderer.material.SetColor("_EmissionColor", color * 0.5f);
                }
            }

            if (_beacon != null)
            {
                _beacon.startColor = color;
                _beacon.endColor = new Color(color.r, color.g, color.b, 0f);
            }
        }

        private void SetupTypeVisual()
        {
            // Add floating label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform);
            labelObj.transform.localPosition = Vector3.up * 5f;

            var tmp = labelObj.AddComponent<TMPro.TextMeshPro>();
            tmp.text = _seed.name;
            tmp.fontSize = 4;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = _discovered ? Color.gray : Color.white;

            // Billboard behavior
            labelObj.AddComponent<Billboard>();
        }

        public void OnClick()
        {
            if (_seed == null) return;

            // Show seed info UI
            SeedInfoUI.Show(_seed, _discovered);
        }
    }

    /// <summary>
    /// Simple billboard to face camera
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform);
                transform.Rotate(0, 180, 0);
            }
        }
    }

    #endregion

    #region Info UI

    /// <summary>
    /// UI for displaying seed point information
    /// </summary>
    public static class SeedInfoUI
    {
        public static void Show(SeedPoint seed, bool discovered)
        {
            // This would be implemented with your UI system
            // For now, just log
            ApexLogger.Log($"[SeedInfoUI] {seed.name} (Level {seed.level})", ApexLogger.LogCategory.General);
            ApexLogger.Log($"  Type: {seed.type}", ApexLogger.LogCategory.General);
            ApexLogger.Log($"  Description: {seed.description}", ApexLogger.LogCategory.General);
            ApexLogger.Log($"  Discovered: {discovered}", ApexLogger.LogCategory.General);

            if (seed.resources != null && seed.resources.Count > 0)
            {
                ApexLogger.Log($"  Resources: {seed.resources.Count} types available", ApexLogger.LogCategory.General);
            }

            if (seed.bonuses != null && seed.bonuses.Count > 0)
            {
                foreach (var bonus in seed.bonuses)
                {
                    ApexLogger.Log($"  Bonus: {bonus.type} +{bonus.value}", ApexLogger.LogCategory.General);
                }
            }
        }
    }

    #endregion
}
