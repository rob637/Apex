// ============================================================================
// APEX CITADELS - FANTASY WORLD DEMO SCENE
// Quick setup component to test fantasy world generation
// ============================================================================
using System;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Demo component for testing fantasy world generation.
    /// Add this to a GameObject in your scene for quick testing.
    /// </summary>
    public class FantasyWorldDemo : MonoBehaviour
    {
        [Header("Location")]
        [Tooltip("Your home location")]
        public double latitude = 38.8977;   // Vienna, VA area (Mashie Drive)
        public double longitude = -77.2520;
        
        [Header("Presets")]
        public LocationPreset preset = LocationPreset.Custom;
        
        [Header("Generation")]
        public float radiusMeters = 500f;
        public bool generateOnStart = true;
        
        [Header("Debug")]
        public bool showDebugInfo = true;
        
        private FantasyWorldGenerator _generator;
        private string _statusMessage = "Ready";
        
        public enum LocationPreset
        {
            Custom,
            ViennaVA,           // Mashie Drive area
            SanFrancisco,       // Union Square
            NewYorkCity,        // Times Square
            London,             // Trafalgar Square
            Paris,              // Champs-Élysées
            Tokyo               // Shibuya
        }
        
        private void Awake()
        {
            // Get or create generator
            _generator = GetComponent<FantasyWorldGenerator>();
            if (_generator == null)
            {
                _generator = gameObject.AddComponent<FantasyWorldGenerator>();
            }
            
            _generator.OnGenerationProgress += (msg) => _statusMessage = msg;
            
            // Setup camera for exploration
            SetupCamera();
        }
        
        private void SetupCamera()
        {
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                // Position camera at eye level in the center
                mainCam.transform.position = new Vector3(0, 5f, 0);
                mainCam.transform.rotation = Quaternion.Euler(10f, 0, 0);
                
                // Add fly camera if not present
                if (mainCam.GetComponent<SimpleFlyCamera>() == null)
                {
                    mainCam.gameObject.AddComponent<SimpleFlyCamera>();
                }
            }
        }
        
        private void Start()
        {
            ApplyPreset();
            
            if (generateOnStart)
            {
                Generate();
            }
        }
        
        private void ApplyPreset()
        {
            switch (preset)
            {
                case LocationPreset.ViennaVA:
                    latitude = 38.9065;
                    longitude = -77.2477;
                    break;
                case LocationPreset.SanFrancisco:
                    latitude = 37.7879;
                    longitude = -122.4074;
                    break;
                case LocationPreset.NewYorkCity:
                    latitude = 40.7580;
                    longitude = -73.9855;
                    break;
                case LocationPreset.London:
                    latitude = 51.5080;
                    longitude = -0.1281;
                    break;
                case LocationPreset.Paris:
                    latitude = 48.8698;
                    longitude = 2.3078;
                    break;
                case LocationPreset.Tokyo:
                    latitude = 35.6595;
                    longitude = 139.7004;
                    break;
            }
        }
        
        [ContextMenu("Generate Fantasy World")]
        public void Generate()
        {
            if (_generator.prefabLibrary == null)
            {
                _statusMessage = "ERROR: Assign prefab library first!";
                ApexLogger.LogError("[Demo] No prefab library assigned to FantasyWorldGenerator!", ApexLogger.LogCategory.Map);
                return;
            }
            
            // Create ground plane if it doesn't exist
            CreateGround();
            
            _statusMessage = "Starting generation...";
            _generator.config.radiusMeters = radiusMeters;
            _generator.Initialize(latitude, longitude);
            _generator.GenerateWorld();
        }
        
        [ContextMenu("Clear World")]
        public void Clear()
        {
            _generator.ClearWorld();
            _statusMessage = "World cleared";
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            
            GUILayout.Label("<b>Fantasy World Generator</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 });
            GUILayout.Space(5);
            
            GUILayout.Label($"Location: {latitude:F4}, {longitude:F4}");
            GUILayout.Label($"Radius: {radiusMeters}m");
            GUILayout.Space(5);
            
            GUILayout.Label($"Status: {_statusMessage}");
            GUILayout.Space(10);
            
            if (_generator.prefabLibrary == null)
            {
                GUILayout.Label("<color=red>⚠ No Prefab Library assigned!</color>", new GUIStyle(GUI.skin.label) { richText = true });
                GUILayout.Label("Create via: Assets > Create > Apex Citadels > Fantasy Prefab Library");
            }
            else
            {
                GUILayout.Label("<color=green>✓ Prefab Library assigned</color>", new GUIStyle(GUI.skin.label) { richText = true });
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Generate", GUILayout.Width(150)))
            {
                Generate();
            }
            
            if (GUILayout.Button("Clear", GUILayout.Width(150)))
            {
                Clear();
            }
            
            GUILayout.EndArea();
        }
        
        private void CreateGround()
        {
            // Check if ground already exists
            if (GameObject.Find("FantasyGround") != null) return;
            
            // Create a large ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "FantasyGround";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(radiusMeters / 5f, 1, radiusMeters / 5f);
            
            // Create a simple grass-colored material
            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material grassMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (grassMat != null)
                {
                    grassMat.color = new Color(0.3f, 0.5f, 0.2f); // Grass green
                    renderer.material = grassMat;
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            // Draw generation radius
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radiusMeters);
        }
    }
}
