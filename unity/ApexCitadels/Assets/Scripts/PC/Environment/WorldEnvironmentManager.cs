// ============================================================================
// APEX CITADELS - WORLD ENVIRONMENT MANAGER
// Orchestrates all visual systems for the AAA experience
// ============================================================================
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ApexCitadels.Core;
using ApexCitadels.Map;

namespace ApexCitadels.PC.Environment
{
    /// <summary>
    /// Central manager for all environment systems.
    /// Creates the complete visual experience.
    /// </summary>
    public class WorldEnvironmentManager : MonoBehaviour
    {
        public static WorldEnvironmentManager Instance { get; private set; }

        public enum TerrainMode
        {
            Procedural,  // Generated terrain with noise (offline/demo mode)
            Mapbox       // Real-world map tiles from Mapbox API
        }

        [Header("Terrain Mode")]
        [SerializeField] private TerrainMode terrainMode = TerrainMode.Mapbox;
        [Tooltip("Fallback to procedural if Mapbox fails")]
        [SerializeField] private bool fallbackToProcedural = true;

        [Header("Environment Systems")]
        [SerializeField] private bool enableTerrain = true;
        [SerializeField] private bool enableAtmosphere = true;
        [SerializeField] private bool enableEnhancedTerritories = true;
        [SerializeField] private bool enableAAAEffects = true;
        [SerializeField] private bool enableEnvironmentalProps = true;
        [SerializeField] private bool enableTerritoryEffects = true;

        [Header("Demo Settings")]
        [SerializeField] private bool createDemoTerritories = true;
        [SerializeField] private int demoTerritoryCount = 25;

        // References
        private ProceduralTerrain _terrain;
        private MapboxTileRenderer _mapboxRenderer;
        private AtmosphericLighting _atmosphere;
        private AAAVisualEffects _aaaEffects;
        private EnvironmentalProps _props;
        private List<EnhancedTerritoryVisual> _territories = new List<EnhancedTerritoryVisual>();

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
            Debug.Log($\"[WorldEnv] === Starting with TerrainMode: {terrainMode} ===\");\n            StartCoroutine(InitializeEnvironment());\n        }\n\n        /// <summary>\n        /// Initialize all environment systems in order\n        /// </summary>\n        private IEnumerator InitializeEnvironment()\n        {\n            Debug.Log($\"[WorldEnv] Initializing environment. TerrainMode={terrainMode}, EnableTerrain={enableTerrain}\");\n\n            // Step 1: Create terrain\n            if (enableTerrain)\n            {\n                CreateTerrain();\n                yield return new WaitForSeconds(0.1f); // Wait for terrain to generate\n            }\n\n            // Step 2: Setup atmospheric lighting\n            if (enableAtmosphere)\n            {\n                CreateAtmosphere();\n                yield return null;\n            }\n\n            // Step 3: Create AAA visual effects (post-processing, particles, etc.)\n            if (enableAAAEffects)\n            {\n                CreateAAAEffects();\n                yield return null;\n            }\n\n            // Step 4: Create environmental props (trees, rocks, etc.) - SKIP for Mapbox mode\n            if (enableEnvironmentalProps && terrainMode != TerrainMode.Mapbox)
            {
                CreateEnvironmentalProps();
                yield return new WaitForSeconds(0.1f);
            }

            // Step 5: Create demo territories if enabled
            if (createDemoTerritories)
            {
                CreateDemoTerritories();
                yield return null;
            }

            ApexLogger.Log("AAA environment initialization complete!", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Create the terrain system based on selected mode
        /// </summary>
        private void CreateTerrain()
        {
            if (terrainMode == TerrainMode.Mapbox)
            {
                CreateMapboxTerrain();
            }
            else
            {
                CreateProceduralTerrain();
            }
        }

        /// <summary>
        /// Create Mapbox real-world map tiles
        /// </summary>
        private void CreateMapboxTerrain()
        {
            // Check if Mapbox renderer already exists
            _mapboxRenderer = FindFirstObjectByType<MapboxTileRenderer>();
            
            if (_mapboxRenderer == null)
            {
                // Check if MapboxConfig exists
                var config = UnityEngine.Resources.Load<MapboxConfiguration>("MapboxConfig");
                if (config == null || !config.IsValid)
                {
                    ApexLogger.LogWarning("[WorldEnv] No valid Mapbox config found! Go to: Apex Citadels > PC > Setup Mapbox (Auto)", ApexLogger.LogCategory.Map);
                    
                    if (fallbackToProcedural)
                    {
                        ApexLogger.Log("[WorldEnv] Falling back to procedural terrain", ApexLogger.LogCategory.General);
                        CreateProceduralTerrain();
                        return;
                    }
                }
                
                GameObject mapboxObj = new GameObject("MapboxTileRenderer");
                mapboxObj.transform.parent = transform;
                _mapboxRenderer = mapboxObj.AddComponent<MapboxTileRenderer>();
                ApexLogger.Log("[WorldEnv] Created Mapbox tile renderer for real-world maps", ApexLogger.LogCategory.Map);
            }
        }

        /// <summary>
        /// Create procedural terrain (fallback/offline mode)
        /// </summary>
        private void CreateProceduralTerrain()
        {
            // Check if terrain already exists
            _terrain = FindFirstObjectByType<ProceduralTerrain>();
            
            if (_terrain == null)
            {
                GameObject terrainObj = new GameObject("ProceduralTerrainSystem");
                terrainObj.transform.parent = transform;
                _terrain = terrainObj.AddComponent<ProceduralTerrain>();
                ApexLogger.Log("Created procedural terrain", ApexLogger.LogCategory.General);
            }
        }

        /// <summary>
        /// Create atmospheric lighting system
        /// </summary>
        private void CreateAtmosphere()
        {
            _atmosphere = FindFirstObjectByType<AtmosphericLighting>();
            
            if (_atmosphere == null)
            {
                GameObject atmosObj = new GameObject("AtmosphericLighting");
                atmosObj.transform.parent = transform;
                _atmosphere = atmosObj.AddComponent<AtmosphericLighting>();
                ApexLogger.Log("Created atmospheric lighting", ApexLogger.LogCategory.General);
            }
        }

        /// <summary>
        /// Create AAA visual effects system
        /// </summary>
        private void CreateAAAEffects()
        {
            _aaaEffects = FindFirstObjectByType<AAAVisualEffects>();
            
            if (_aaaEffects == null)
            {
                GameObject effectsObj = new GameObject("AAAVisualEffects");
                effectsObj.transform.parent = transform;
                _aaaEffects = effectsObj.AddComponent<AAAVisualEffects>();
                ApexLogger.Log("Created AAA visual effects", ApexLogger.LogCategory.General);
            }
        }

        /// <summary>
        /// Create environmental props (trees, rocks, etc.)
        /// </summary>
        private void CreateEnvironmentalProps()
        {
            _props = FindFirstObjectByType<EnvironmentalProps>();
            
            if (_props == null)
            {
                GameObject propsObj = new GameObject("EnvironmentalProps");
                propsObj.transform.parent = transform;
                _props = propsObj.AddComponent<EnvironmentalProps>();
                ApexLogger.Log("Created environmental props", ApexLogger.LogCategory.General);
            }
        }

        /// <summary>
        /// Create demo territories for visual testing
        /// </summary>
        private void CreateDemoTerritories()
        {
            ApexLogger.Log($"Creating {demoTerritoryCount} demo territories...", ApexLogger.LogCategory.General);

            // Clear existing demo territories
            foreach (var territory in _territories)
            {
                if (territory != null)
                {
                    Destroy(territory.gameObject);
                }
            }
            _territories.Clear();

            // Territory names for variety
            string[] prefixes = { "Shadow", "Iron", "Crystal", "Dragon", "Storm", "Frost", "Fire", "Ancient", "Golden", "Dark" };
            string[] suffixes = { "Keep", "Fortress", "Citadel", "Hold", "Tower", "Castle", "Peak", "Valley", "Haven", "Watch" };

            // Create territories in a spread pattern
            for (int i = 0; i < demoTerritoryCount; i++)
            {
                // Position in a loose grid with randomness
                float angle = i * (360f / demoTerritoryCount) * Mathf.Deg2Rad;
                float radius = 200f + Random.Range(-50f, 100f);
                
                // Add some clustering
                if (i % 3 == 0) radius *= 0.5f;
                if (i % 5 == 0) radius *= 1.5f;

                float x = Mathf.Cos(angle) * radius + Random.Range(-30f, 30f);
                float z = Mathf.Sin(angle) * radius + Random.Range(-30f, 30f);

                // Get terrain height at this position
                float y = 0f;
                if (_terrain != null)
                {
                    y = _terrain.GetTerrainHeight(x, z);
                    
                    // Skip if in water
                    if (_terrain.IsWater(x, z))
                    {
                        // Move to higher ground
                        x += 50f;
                        z += 50f;
                        y = _terrain.GetTerrainHeight(x, z);
                    }
                }

                // Create territory
                GameObject territoryObj = new GameObject($"Territory_{i}");
                territoryObj.transform.parent = transform;
                territoryObj.transform.position = new Vector3(x, y, z);

                EnhancedTerritoryVisual visual = territoryObj.AddComponent<EnhancedTerritoryVisual>();
                
                // Assign random properties
                visual.TerritoryId = $"territory_{i}";
                visual.OwnerName = prefixes[Random.Range(0, prefixes.Length)] + suffixes[Random.Range(0, suffixes.Length)];
                visual.Level = Random.Range(1, 8);
                
                // Assign ownership with distribution:
                // 20% owned, 15% alliance, 25% enemy, 10% contested, 30% neutral
                float ownershipRoll = Random.value;
                if (ownershipRoll < 0.20f)
                    visual.Ownership = TerritoryOwnership.Owned;
                else if (ownershipRoll < 0.35f)
                    visual.Ownership = TerritoryOwnership.Alliance;
                else if (ownershipRoll < 0.60f)
                    visual.Ownership = TerritoryOwnership.Enemy;
                else if (ownershipRoll < 0.70f)
                    visual.Ownership = TerritoryOwnership.Contested;
                else
                    visual.Ownership = TerritoryOwnership.Neutral;

                visual.BuildVisual();
                
                // Add territory effects for visual flair
                if (enableTerritoryEffects)
                {
                    territoryObj.AddComponent<TerritoryEffects>();
                }
                
                _territories.Add(visual);
            }

            ApexLogger.Log($"Created {_territories.Count} demo territories", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Set time of day
        /// </summary>
        public void SetTimeOfDay(float time)
        {
            if (_atmosphere != null)
            {
                _atmosphere.SetTimeOfDay(time);
            }
            
            // Update post-processing for time of day
            if (_aaaEffects != null)
            {
                _aaaEffects.SetTimeOfDayEffects(time);
            }
        }

        /// <summary>
        /// Set time preset
        /// </summary>
        public void SetTimePreset(TimePreset preset)
        {
            if (_atmosphere != null)
            {
                _atmosphere.SetTimePreset(preset);
            }
        }

        /// <summary>
        /// Regenerate terrain with new seed
        /// </summary>
        public void RegenerateTerrain(int seed)
        {
            if (_terrain != null)
            {
                _terrain.RegenerateWithSeed(seed);
            }
        }

        /// <summary>
        /// Toggle grid visibility
        /// </summary>
        public void SetGridVisible(bool visible)
        {
            if (_terrain != null)
            {
                _terrain.SetGridVisible(visible);
            }
        }

        /// <summary>
        /// Get territory at world position
        /// </summary>
        public EnhancedTerritoryVisual GetTerritoryAt(Vector3 worldPos, float radius = 30f)
        {
            foreach (var territory in _territories)
            {
                if (territory != null)
                {
                    float dist = Vector3.Distance(territory.transform.position, worldPos);
                    if (dist < radius)
                    {
                        return territory;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get all territories
        /// </summary>
        public List<EnhancedTerritoryVisual> GetAllTerritories()
        {
            return _territories;
        }
    }
}
