// ============================================================================
// APEX CITADELS - FANTASY MAP INTEGRATION
// Integrates Mapbox satellite/street maps as the ground texture
// with Fantasy buildings rendered on top
// ============================================================================
using System.Collections;
using UnityEngine;
using ApexCitadels.Core;
using ApexCitadels.Map;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Integrates real-world map imagery with fantasy world generation.
    /// Replaces the procedural ground with actual Mapbox tiles.
    /// </summary>
    public class FantasyMapIntegration : MonoBehaviour
    {
        [Header("Map Settings")]
        [Tooltip("Use Mapbox for ground textures instead of procedural")]
        public bool useMapboxGround = true;
        
        [Tooltip("Mapbox map style")]
        public MapboxStyle mapStyle = MapboxStyle.Satellite;
        
        [Tooltip("Zoom level (15-17 recommended for neighborhood view)")]
        [Range(14, 18)]
        public int zoomLevel = 16;
        
        [Header("Fantasy Overlay")]
        [Tooltip("Opacity of fantasy effects over the map")]
        [Range(0f, 1f)]
        public float fantasyOverlayStrength = 0.3f;
        
        [Tooltip("Tint color for fantasy feel")]
        public Color fantasyTint = new Color(0.9f, 0.95f, 1.0f, 1f);
        
        [Header("References")]
        public FantasyWorldGenerator generator;
        public MapboxTileRenderer mapboxRenderer;
        
        private MapboxConfiguration _mapboxConfig;
        private bool _isInitialized;
        
        private void Start()
        {
            StartCoroutine(InitializeMapIntegration());
        }
        
        private IEnumerator InitializeMapIntegration()
        {
            // Wait for generator to be ready
            if (generator == null)
                generator = GetComponent<FantasyWorldGenerator>();
            
            // Load or find Mapbox config
            _mapboxConfig = UnityEngine.Resources.Load<MapboxConfiguration>("MapboxConfig");
            
            if (_mapboxConfig == null || !_mapboxConfig.IsValid)
            {
                ApexLogger.LogWarning("[FantasyMap] No valid Mapbox configuration found. Using procedural ground.", ApexLogger.LogCategory.Map);
                ApexLogger.Log("[FantasyMap] To enable real maps: Apex Citadels > PC > Configure Mapbox API", ApexLogger.LogCategory.Map);
                useMapboxGround = false;
                yield break;
            }
            
            ApexLogger.Log("[FantasyMap] Mapbox configuration found! Initializing map integration...", ApexLogger.LogCategory.Map);
            
            if (useMapboxGround)
            {
                // Find or create Mapbox renderer
                if (mapboxRenderer == null)
                {
                    mapboxRenderer = FindAnyObjectByType<MapboxTileRenderer>();
                }
                
                if (mapboxRenderer == null)
                {
                    // Create a new MapboxTileRenderer
                    GameObject mapboxObj = new GameObject("MapboxTileRenderer");
                    mapboxObj.transform.SetParent(transform);
                    mapboxRenderer = mapboxObj.AddComponent<MapboxTileRenderer>();
                    ApexLogger.Log("[FantasyMap] Created MapboxTileRenderer", ApexLogger.LogCategory.Map);
                }
                
                // Wait for map to load
                yield return new WaitUntil(() => !mapboxRenderer.IsLoading || Time.time > 30f);
                
                if (mapboxRenderer.IsLoading)
                {
                    ApexLogger.LogWarning("[FantasyMap] Mapbox loading timed out", ApexLogger.LogCategory.Map);
                }
                else
                {
                    ApexLogger.Log("[FantasyMap] Mapbox map loaded successfully!", ApexLogger.LogCategory.Map);
                    
                    // Disable procedural ground if we have map tiles
                    DisableProceduralGround();
                }
            }
            
            _isInitialized = true;
        }
        
        private void DisableProceduralGround()
        {
            // Find and disable the procedural ground plane
            Transform ground = transform.Find("GeneratedGround");
            if (ground != null)
            {
                ground.gameObject.SetActive(false);
                ApexLogger.Log("[FantasyMap] Disabled procedural ground in favor of Mapbox tiles", ApexLogger.LogCategory.Map);
            }
            
            // Also check in the generator
            if (generator != null)
            {
                Transform genGround = generator.transform.Find("GeneratedGround");
                if (genGround != null)
                {
                    genGround.gameObject.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// Set the map center to a new location
        /// </summary>
        public void SetLocation(double latitude, double longitude)
        {
            if (mapboxRenderer != null)
            {
                // The MapboxTileRenderer should have a method to change location
                // For now, log the intent
                ApexLogger.Log($"[FantasyMap] Location set to {latitude}, {longitude}", ApexLogger.LogCategory.Map);
            }
        }
        
        /// <summary>
        /// Check if Mapbox is properly configured
        /// </summary>
        public static bool IsMapboxConfigured()
        {
            var config = UnityEngine.Resources.Load<MapboxConfiguration>("MapboxConfig");
            return config != null && config.IsValid;
        }
        
        /// <summary>
        /// Get instructions for setting up Mapbox
        /// </summary>
        public static string GetSetupInstructions()
        {
            return @"To enable real-world maps:
1. Go to https://mapbox.com and create a free account
2. Copy your 'Default public token' (starts with 'pk.')
3. In Unity: Apex Citadels > PC > Configure Mapbox API
4. Paste your token and click 'Save Configuration'
5. Re-run the Fantasy World generation";
        }
    }
}
