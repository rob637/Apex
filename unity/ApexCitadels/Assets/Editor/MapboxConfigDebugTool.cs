using UnityEngine;
using UnityEditor;
using System.IO;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Debug tool to diagnose and fix MapboxConfig issues.
    /// Unity caches ScriptableObjects in memory - this tool helps force refresh them.
    /// </summary>
    public class MapboxConfigDebugTool : EditorWindow
    {
        private Vector2 scrollPos;
        
        [MenuItem("Apex Citadels/Debug/Mapbox Config Debug Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<MapboxConfigDebugTool>("Mapbox Config Debug");
            window.minSize = new Vector2(500, 400);
        }
        
        [MenuItem("Apex Citadels/Debug/Force Reload MapboxConfig %#r")]
        public static void ForceReloadMapboxConfig()
        {
            string assetPath = "Assets/Resources/MapboxConfig.asset";
            
            // Clear any cached references
            Resources.UnloadUnusedAssets();
            
            // Force reimport the asset
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            // Load and verify
            var config = Resources.Load<ScriptableObject>("MapboxConfig");
            if (config != null)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                
                // Log the values
                var so = new SerializedObject(config);
                Debug.Log($"[MapboxConfigDebug] === FORCE RELOADED ===");
                Debug.Log($"[MapboxConfigDebug] Style: {so.FindProperty("Style").intValue}");
                Debug.Log($"[MapboxConfigDebug] DefaultLatitude: {so.FindProperty("DefaultLatitude").doubleValue}");
                Debug.Log($"[MapboxConfigDebug] DefaultLongitude: {so.FindProperty("DefaultLongitude").doubleValue}");
                Debug.Log($"[MapboxConfigDebug] DefaultZoom: {so.FindProperty("DefaultZoom").intValue}");
            }
            else
            {
                Debug.LogError("[MapboxConfigDebug] Could not load MapboxConfig!");
            }
        }
        
        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            GUILayout.Label("Mapbox Config Debug Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            // Section 1: File on Disk
            EditorGUILayout.LabelField("1. VALUES ON DISK (YAML File)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These are the actual values in the .asset file on disk", MessageType.Info);
            
            string filePath = Path.Combine(Application.dataPath, "Resources/MapboxConfig.asset");
            if (File.Exists(filePath))
            {
                string content = File.ReadAllText(filePath);
                
                // Parse key values from YAML
                var lines = content.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("Style:") || 
                        line.Contains("DefaultLatitude:") || 
                        line.Contains("DefaultLongitude:") ||
                        line.Contains("DefaultZoom:"))
                    {
                        EditorGUILayout.TextField(line.Trim());
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"File not found: {filePath}", MessageType.Error);
            }
            
            GUILayout.Space(20);
            
            // Section 2: Values in Memory
            EditorGUILayout.LabelField("2. VALUES IN MEMORY (Unity's Cache)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These are the values Unity is actually using (may be stale!)", MessageType.Warning);
            
            var config = Resources.Load<ScriptableObject>("MapboxConfig");
            if (config != null)
            {
                var so = new SerializedObject(config);
                
                var styleProp = so.FindProperty("Style");
                var latProp = so.FindProperty("DefaultLatitude");
                var lonProp = so.FindProperty("DefaultLongitude");
                var zoomProp = so.FindProperty("DefaultZoom");
                
                if (styleProp != null)
                    EditorGUILayout.LabelField($"Style: {styleProp.intValue} (0=Streets, 4=Satellite, 5=SatelliteStreets)");
                if (latProp != null)
                    EditorGUILayout.LabelField($"DefaultLatitude: {latProp.doubleValue}");
                if (lonProp != null)
                    EditorGUILayout.LabelField($"DefaultLongitude: {lonProp.doubleValue}");
                if (zoomProp != null)
                    EditorGUILayout.LabelField($"DefaultZoom: {zoomProp.intValue}");
                    
                // Check for mismatch
                GUILayout.Space(10);
                
                bool mismatch = false;
                string fileContent = File.Exists(filePath) ? File.ReadAllText(filePath) : "";
                
                if (fileContent.Contains("Style: 4") && styleProp != null && styleProp.intValue != 4)
                    mismatch = true;
                if (fileContent.Contains("38.9065479") && latProp != null && System.Math.Abs(latProp.doubleValue - 38.9065479) > 0.0001)
                    mismatch = true;
                    
                if (mismatch)
                {
                    EditorGUILayout.HelpBox("⚠️ MISMATCH DETECTED! Memory values don't match disk values!\nClick 'Force Reload' below.", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("✓ Values appear to match", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Could not load MapboxConfig from Resources!", MessageType.Error);
            }
            
            GUILayout.Space(20);
            
            // Section 3: Actions
            EditorGUILayout.LabelField("3. ACTIONS", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Force Reload Config\n(Ctrl+Shift+R)", GUILayout.Height(50)))
            {
                ForceReloadMapboxConfig();
            }
            
            if (GUILayout.Button("Open Config in Inspector", GUILayout.Height(50)))
            {
                var loadedConfig = Resources.Load<ScriptableObject>("MapboxConfig");
                if (loadedConfig != null)
                {
                    Selection.activeObject = loadedConfig;
                    EditorGUIUtility.PingObject(loadedConfig);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reimport All Assets\n(Slow but thorough)", GUILayout.Height(50)))
            {
                if (EditorUtility.DisplayDialog("Reimport All?", 
                    "This will reimport ALL assets. It may take a while.\nAre you sure?", 
                    "Yes", "Cancel"))
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    AssetDatabase.ImportAsset("Assets/Resources/MapboxConfig.asset", ImportAssetOptions.ForceUpdate);
                }
            }
            
            if (GUILayout.Button("Clear Library Cache\n(Requires Restart)", GUILayout.Height(50)))
            {
                EditorUtility.DisplayDialog("Clear Cache", 
                    "To fully clear Unity's cache:\n\n" +
                    "1. Close Unity\n" +
                    "2. Delete the 'Library' folder in your project\n" +
                    "3. Reopen Unity\n\n" +
                    "This will force a full reimport of all assets.", 
                    "OK");
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(20);
            
            // Section 4: Set Correct Values Directly
            EditorGUILayout.LabelField("4. SET CORRECT VALUES DIRECTLY", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("If Force Reload doesn't work, click this to set the values directly in memory", MessageType.Info);
            
            if (GUILayout.Button("Set to 504 Mashie Drive + Satellite View", GUILayout.Height(40)))
            {
                var loadedConfig = Resources.Load<ScriptableObject>("MapboxConfig");
                if (loadedConfig != null)
                {
                    var so = new SerializedObject(loadedConfig);
                    
                    var styleProp = so.FindProperty("Style");
                    var latProp = so.FindProperty("DefaultLatitude");
                    var lonProp = so.FindProperty("DefaultLongitude");
                    var zoomProp = so.FindProperty("DefaultZoom");
                    
                    if (styleProp != null) styleProp.intValue = 4; // Satellite
                    if (latProp != null) latProp.doubleValue = 38.9065479;
                    if (lonProp != null) lonProp.doubleValue = -77.247697;
                    if (zoomProp != null) zoomProp.intValue = 16;
                    
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(loadedConfig);
                    AssetDatabase.SaveAssets();
                    
                    Debug.Log("[MapboxConfigDebug] Set values directly: Satellite view at 504 Mashie Drive");
                    Debug.Log("[MapboxConfigDebug] Style=4, Lat=38.9065479, Lon=-77.247697, Zoom=16");
                }
            }
            
            GUILayout.Space(20);
            
            // Section 5: Runtime Logging
            EditorGUILayout.LabelField("5. RUNTIME DIAGNOSTICS", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Add Runtime Config Logger to Scene", GUILayout.Height(30)))
            {
                var existing = FindObjectOfType<MapboxConfigRuntimeLogger>();
                if (existing == null)
                {
                    var go = new GameObject("_MapboxConfigLogger");
                    go.AddComponent<MapboxConfigRuntimeLogger>();
                    Debug.Log("[MapboxConfigDebug] Added runtime logger. Press Play to see config values at startup.");
                }
                else
                {
                    Debug.Log("[MapboxConfigDebug] Runtime logger already exists in scene.");
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
    }
    
    /// <summary>
    /// Runtime component that logs MapboxConfig values at startup
    /// </summary>
    public class MapboxConfigRuntimeLogger : MonoBehaviour
    {
        void Awake()
        {
            LogConfig("Awake");
        }
        
        void Start()
        {
            LogConfig("Start");
        }
        
        void LogConfig(string phase)
        {
            var config = Resources.Load<ScriptableObject>("MapboxConfig");
            if (config == null)
            {
                Debug.LogError($"[RuntimeLogger] [{phase}] MapboxConfig is NULL!");
                return;
            }
            
            // Use reflection to get values
            var type = config.GetType();
            
            var styleField = type.GetField("Style");
            var latField = type.GetField("DefaultLatitude");
            var lonField = type.GetField("DefaultLongitude");
            var zoomField = type.GetField("DefaultZoom");
            
            Debug.Log($"[RuntimeLogger] ========== {phase} ==========");
            Debug.Log($"[RuntimeLogger] Config Object: {config.name} (InstanceID: {config.GetInstanceID()})");
            
            if (styleField != null) Debug.Log($"[RuntimeLogger] Style: {styleField.GetValue(config)}");
            if (latField != null) Debug.Log($"[RuntimeLogger] DefaultLatitude: {latField.GetValue(config)}");
            if (lonField != null) Debug.Log($"[RuntimeLogger] DefaultLongitude: {lonField.GetValue(config)}");
            if (zoomField != null) Debug.Log($"[RuntimeLogger] DefaultZoom: {zoomField.GetValue(config)}");
            
            Debug.Log($"[RuntimeLogger] ============================");
        }
    }
}
