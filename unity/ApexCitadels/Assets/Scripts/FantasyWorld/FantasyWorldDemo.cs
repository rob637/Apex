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
    [RequireComponent(typeof(FantasyWorldVisuals))]
    public class FantasyWorldDemo : MonoBehaviour
    {
        [Header("Location")]
        [Tooltip("Your home location - 504 Mashie Drive, Vienna, VA")]
        public double latitude = 38.9065;   // 504 Mashie Drive, Vienna, VA
        public double longitude = -77.2477;
        
        [Header("Presets")]
        [Tooltip("Select ViennaVA for 504 Mashie Drive")]
        public LocationPreset preset = LocationPreset.ViennaVA;
        
        [Header("Generation")]
        public float radiusMeters = 500f;
        public bool generateOnStart = true;
        
        [Header("Controls")]
        public bool useFirstPersonController = true;
        
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
            // Add Realistic Sky (replaces skybox)
            if (GetComponent<RealisticSkySystem>() == null)
            {
                var sky = gameObject.AddComponent<RealisticSkySystem>();
                sky.latitude = latitude;
                sky.longitude = longitude;
            }
            
            // Add Demo UI
            if (GetComponent<DemoUI>() == null)
            {
                gameObject.AddComponent<DemoUI>();
            }
            
            // Add Mini Map
            if (GetComponent<MiniMapUI>() == null)
            {
                gameObject.AddComponent<MiniMapUI>();
            }

            // Get or create generator
            _generator = GetComponent<FantasyWorldGenerator>();
            if (_generator == null)
            {
                _generator = gameObject.AddComponent<FantasyWorldGenerator>();
            }
            
            _generator.OnGenerationProgress += (msg) => _statusMessage = msg;
        }
        
        private void Start()
        {
            ApplyPreset();
            
            if (generateOnStart)
            {
                Generate();
            }
        }
        
        private void SetupControls()
        {
            var mainCam = Camera.main;
            
            if (useFirstPersonController)
            {
                // Create Player Object if not exists
                var player = FindAnyObjectByType<FirstPersonPlayer>();
                if (player == null)
                {
                    GameObject playerObj = new GameObject("Player_FPS");
                    player = playerObj.AddComponent<FirstPersonPlayer>();
                    
                    // Add CharacterController properties
                    CharacterController cc = playerObj.GetComponent<CharacterController>();
                    cc.center = new Vector3(0, 1, 0);
                    cc.radius = 0.5f;
                    cc.height = 2f;
                    
                    // Initial Position
                    playerObj.transform.position = new Vector3(0, 15f, 0); // Drop in
                }

                // If Main Camera exists, attach it to player
                if (mainCam != null)
                {
                    mainCam.transform.SetParent(player.transform);
                    mainCam.transform.localPosition = new Vector3(0, 1.6f, 0);
                    mainCam.transform.localRotation = Quaternion.identity;
                    
                    // Remove FlyCam if it exists
                    var flyCam = mainCam.GetComponent<SimpleFlyCamera>();
                    if (flyCam != null) Destroy(flyCam);
                }
            }
            else
            {
                if (mainCam != null)
                {
                    // Position camera for Fly Mode
                    mainCam.transform.SetParent(null);
                    mainCam.transform.position = new Vector3(0, 50f, -50f); 
                    mainCam.transform.rotation = Quaternion.Euler(45f, 0, 0);
                    
                    // Add fly camera if not present
                    if (mainCam.GetComponent<SimpleFlyCamera>() == null)
                    {
                        mainCam.gameObject.AddComponent<SimpleFlyCamera>();
                    }
                    
                    // Remove Player if exists
                    var player = FindAnyObjectByType<FirstPersonPlayer>();
                    if (player != null) Destroy(player.gameObject);
                }
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
                    longitude = -122.4075;
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
                    longitude = 2.3075;
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
                ApexLogger.LogError("[Demo] No prefab library assigned!", ApexLogger.LogCategory.Map);
                return;
            }
            
            _generator.config.radiusMeters = radiusMeters;
            _generator.Initialize(latitude, longitude);
            _generator.GenerateWorld();
            
            SetupControls();
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
            
            if (GUILayout.Button("Generate", GUILayout.Width(150)))
            {
                Generate();
            }
            
            if (GUILayout.Button("Clear", GUILayout.Width(150)))
            {
                Clear();
            }
            
            GUILayout.Space(10);
            GUILayout.Label(useFirstPersonController ? "Mode: FPS (WASD + Mouse)" : "Mode: Fly Camera (Right Click + WASD)");
            
            GUILayout.EndArea();
            
            // Crosshair for FPS
            if (useFirstPersonController)
            {
                float x = Screen.width / 2;
                float y = Screen.height / 2;
                var style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 20;
                GUI.Label(new Rect(x - 10, y - 10, 20, 20), "+", style);
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radiusMeters);
        }
    }
}
