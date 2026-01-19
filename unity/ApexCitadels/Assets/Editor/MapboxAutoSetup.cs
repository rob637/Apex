using UnityEngine;
using UnityEditor;
using ApexCitadels.Map;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Automatically creates and configures MapboxConfiguration on editor load
    /// </summary>
    [InitializeOnLoad]
    public static class MapboxAutoSetup
    {
        // Your Mapbox API key
        private const string MAPBOX_API_KEY = "pk.eyJ1IjoicnBmbGVnaGEiLCJhIjoiY21rbGtua2l5MDY0czNkbzl6bHM0eW53OSJ9.iQu8VXGO5dxKzVaBui9bwA";
        
        static MapboxAutoSetup()
        {
            // Delay to ensure Unity is ready
            EditorApplication.delayCall += SetupMapboxConfig;
        }
        
        private static void SetupMapboxConfig()
        {
            // Try to load existing config
            MapboxConfiguration config = UnityEngine.Resources.Load<MapboxConfiguration>("MapboxConfig");
            
            if (config == null)
            {
                // Check if it exists elsewhere
                string[] guids = AssetDatabase.FindAssets("t:MapboxConfiguration");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    config = AssetDatabase.LoadAssetAtPath<MapboxConfiguration>(path);
                }
            }
            
            if (config == null)
            {
                // Create new config
                CreateMapboxConfig();
            }
            else if (string.IsNullOrEmpty(config.AccessToken) || !config.AccessToken.StartsWith("pk."))
            {
                // Update existing config with API key
                config.AccessToken = MAPBOX_API_KEY;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                Debug.Log("[Mapbox] Updated API key in existing configuration");
            }
        }
        
        [MenuItem("Apex Citadels/PC/Setup Mapbox (Auto)", false, 101)]
        public static void CreateMapboxConfig()
        {
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            // Check if already exists
            MapboxConfiguration existing = UnityEngine.Resources.Load<MapboxConfiguration>("MapboxConfig");
            if (existing != null)
            {
                existing.AccessToken = MAPBOX_API_KEY;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                Debug.Log("[Mapbox] ✓ Updated MapboxConfig with API key");
                Selection.activeObject = existing;
                
                EditorUtility.DisplayDialog("✅ Mapbox Updated",
                    $"MapboxConfig already exists and has been updated with API key.\n\n" +
                    $"Location: Assets/Resources/MapboxConfig.asset\n\n" +
                    $"The asset is now selected in the Project window.",
                    "OK");
                return;
            }
            
            // Create new
            MapboxConfiguration config = ScriptableObject.CreateInstance<MapboxConfiguration>();
            config.AccessToken = MAPBOX_API_KEY;
            config.Style = MapboxStyle.Streets;  // Streets style for visible roads
            config.DefaultLatitude = 38.9012;  // Vienna, VA
            config.DefaultLongitude = -77.2653;
            config.DefaultZoom = 14;
            config.TileSize = 512;
            config.UseRetinaScale = true;
            config.MaxCachedTiles = 100;
            config.EnableDiskCache = true;
            
            AssetDatabase.CreateAsset(config, "Assets/Resources/MapboxConfig.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[Mapbox] ✓ Created MapboxConfig with your API key at Assets/Resources/MapboxConfig.asset");
            Selection.activeObject = config;
            
            EditorUtility.DisplayDialog("✅ Mapbox Configured!",
                $"Created MapboxConfig.asset with your API key.\n\n" +
                $"Location: Assets/Resources/MapboxConfig.asset\n\n" +
                $"Style: Streets (shows roads clearly)\n" +
                $"Default Location: Vienna, VA (38.9012, -77.2653)\n\n" + +
                $"The asset is now selected in the Project window.\n" +
                $"You can change the location in the Inspector.",
                "OK");
        }
    }
}
