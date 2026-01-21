// ============================================================================
// APEX CITADELS - DUAL MODE SCENE SETUP
// Editor script to set up a new scene for the dual mode system
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ApexCitadels.Map;
using ApexCitadels.FantasyWorld;
using ApexCitadels.GameModes;

namespace ApexCitadels.Editor
{
    public class DualModeSceneSetup
    {
        [MenuItem("Apex Citadels/Create Dual Mode Scene")]
        public static void CreateDualModeScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "DualModeTest";
            
            Debug.Log("[DualMode] Setting up Dual Mode scene...");
            
            // 1. Create main controller
            var controllerObj = new GameObject("DualModeController");
            var controller = controllerObj.AddComponent<DualModeController>();
            
            // 2. Create camera
            var cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.4f, 0.6f, 0.9f); // Sky blue
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 2000f;
            cameraObj.AddComponent<AudioListener>();
            
            // Position camera for map view
            cameraObj.transform.position = new Vector3(0, 150, -75);
            cameraObj.transform.rotation = Quaternion.Euler(55, 0, 0);
            
            // 3. Create Mapbox tile renderer
            var mapboxObj = new GameObject("MapboxTileRenderer");
            var mapbox = mapboxObj.AddComponent<MapboxTileRenderer>();
            
            // 4. Create Fantasy World Generator
            var fantasyObj = new GameObject("FantasyWorldGenerator");
            var generator = fantasyObj.AddComponent<FantasyWorldGenerator>();
            var mapIntegration = fantasyObj.AddComponent<FantasyMapIntegration>();
            
            // 5. Create directional light (sun)
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            // 6. Create UI
            var uiObj = new GameObject("DualModeUI");
            uiObj.AddComponent<DualModeUI>();
            
            // 7. Create URP Volume (if available)
            CreateURPVolume();
            
            // Mark scene dirty
            EditorSceneManager.MarkSceneDirty(scene);
            
            Debug.Log("[DualMode] ========================================");
            Debug.Log("[DualMode] Dual Mode scene created!");
            Debug.Log("[DualMode] Controls:");
            Debug.Log("[DualMode]   WASD - Move around the map");
            Debug.Log("[DualMode]   Space - Land / Take off");
            Debug.Log("[DualMode]   Mouse - Look around");
            Debug.Log("[DualMode] ========================================");
            
            // Focus on scene view
            Selection.activeGameObject = controllerObj;
        }
        
        private static void CreateURPVolume()
        {
            // Try to create a URP Volume for post-processing
            var volumeObj = new GameObject("Global Volume");
            
            // Add Volume component if available
            var volumeType = System.Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            if (volumeType != null)
            {
                var volume = volumeObj.AddComponent(volumeType) as UnityEngine.Rendering.Volume;
                if (volume != null)
                {
                    volume.isGlobal = true;
                    // Profile would need to be assigned manually
                }
            }
        }
        
        [MenuItem("Apex Citadels/Setup Existing Scene for Dual Mode")]
        public static void SetupExistingScene()
        {
            // Add DualModeController to existing scene
            var existing = Object.FindAnyObjectByType<DualModeController>();
            if (existing != null)
            {
                Debug.LogWarning("[DualMode] DualModeController already exists in scene!");
                Selection.activeGameObject = existing.gameObject;
                return;
            }
            
            // Create controller
            var controllerObj = new GameObject("DualModeController");
            var controller = controllerObj.AddComponent<DualModeController>();
            
            // Add UI
            var uiObj = new GameObject("DualModeUI");
            uiObj.AddComponent<DualModeUI>();
            
            // Find and configure existing components
            var camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
            }
            
            Debug.Log("[DualMode] Added DualModeController to existing scene");
            Debug.Log("[DualMode] Press Play and use SPACE to switch between Map/Ground view");
            
            Selection.activeGameObject = controllerObj;
        }
    }
}
#endif
