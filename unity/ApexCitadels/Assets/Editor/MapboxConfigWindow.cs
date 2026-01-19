using UnityEngine;
using UnityEditor;
using ApexCitadels.Map;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Editor window for configuring Mapbox API settings
    /// </summary>
    public class MapboxConfigWindow : EditorWindow
    {
        private MapboxConfiguration _config;
        private string _testLatitude = "40.7128";
        private string _testLongitude = "-74.0060";
        private Texture2D _previewTexture;
        private bool _isLoading;
        
        [MenuItem("Apex Citadels/PC/Configure Mapbox API", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<MapboxConfigWindow>("Mapbox Config");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }
        
        private void OnEnable()
        {
            LoadOrCreateConfig();
        }
        
        private void LoadOrCreateConfig()
        {
            // Try to load existing config
            _config = UnityEngine.Resources.Load<MapboxConfiguration>("MapboxConfig");
            
            if (_config == null)
            {
                // Check if it exists in the project
                string[] guids = AssetDatabase.FindAssets("t:MapboxConfiguration");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _config = AssetDatabase.LoadAssetAtPath<MapboxConfiguration>(path);
                }
            }
        }
        
        private void CreateNewConfig()
        {
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            _config = ScriptableObject.CreateInstance<MapboxConfiguration>();
            AssetDatabase.CreateAsset(_config, "Assets/Resources/MapboxConfig.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[Mapbox] Created new MapboxConfiguration at Assets/Resources/MapboxConfig.asset");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // Header
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🗺️ Mapbox Configuration", headerStyle);
            EditorGUILayout.Space(10);
            
            // Info box
            EditorGUILayout.HelpBox(
                "Mapbox provides real-world map tiles for geographic gameplay.\n\n" +
                "Get your free API key at: https://mapbox.com\n" +
                "1. Create an account\n" +
                "2. Go to Account > Access Tokens\n" +
                "3. Copy your default public token (starts with 'pk.')",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            if (_config == null)
            {
                EditorGUILayout.HelpBox("No Mapbox configuration found. Create one to get started.", MessageType.Warning);
                
                if (GUILayout.Button("Create Mapbox Configuration", GUILayout.Height(30)))
                {
                    CreateNewConfig();
                }
                return;
            }
            
            // Show config as serialized object for undo support
            EditorGUI.BeginChangeCheck();
            
            // API Token
            EditorGUILayout.LabelField("API Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Access Token", GUILayout.Width(100));
            _config.AccessToken = EditorGUILayout.PasswordField(_config.AccessToken);
            if (GUILayout.Button("👁", GUILayout.Width(25)))
            {
                EditorGUILayout.TextField(_config.AccessToken);
            }
            EditorGUILayout.EndHorizontal();
            
            // Validation
            if (string.IsNullOrEmpty(_config.AccessToken))
            {
                EditorGUILayout.HelpBox("Enter your Mapbox access token", MessageType.Warning);
            }
            else if (!_config.AccessToken.StartsWith("pk."))
            {
                EditorGUILayout.HelpBox("Token should start with 'pk.' - make sure you're using a public token", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("✓ Token format looks valid", MessageType.None);
            }
            
            EditorGUILayout.Space(10);
            
            // Style
            EditorGUILayout.LabelField("Map Style", EditorStyles.boldLabel);
            _config.Style = (MapboxStyle)EditorGUILayout.EnumPopup("Visual Style", _config.Style);
            
            // Style preview description
            string styleDesc = _config.Style switch
            {
                MapboxStyle.Streets => "Standard street map with roads and labels",
                MapboxStyle.Dark => "Dark theme - great for gaming UI",
                MapboxStyle.Satellite => "Satellite imagery",
                MapboxStyle.SatelliteStreets => "Satellite with street overlays",
                MapboxStyle.Light => "Light minimal theme",
                MapboxStyle.Outdoors => "Topographic/hiking style",
                _ => ""
            };
            EditorGUILayout.LabelField(styleDesc, EditorStyles.miniLabel);
            
            EditorGUILayout.Space(10);
            
            // Default Location
            EditorGUILayout.LabelField("Default Location", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Latitude", GUILayout.Width(70));
            _config.DefaultLatitude = EditorGUILayout.DoubleField(_config.DefaultLatitude);
            EditorGUILayout.LabelField("Longitude", GUILayout.Width(70));
            _config.DefaultLongitude = EditorGUILayout.DoubleField(_config.DefaultLongitude);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New York"))
            {
                _config.DefaultLatitude = 40.7128;
                _config.DefaultLongitude = -74.0060;
            }
            if (GUILayout.Button("London"))
            {
                _config.DefaultLatitude = 51.5074;
                _config.DefaultLongitude = -0.1278;
            }
            if (GUILayout.Button("Tokyo"))
            {
                _config.DefaultLatitude = 35.6762;
                _config.DefaultLongitude = 139.6503;
            }
            if (GUILayout.Button("Sydney"))
            {
                _config.DefaultLatitude = -33.8688;
                _config.DefaultLongitude = 151.2093;
            }
            EditorGUILayout.EndHorizontal();
            
            _config.DefaultZoom = EditorGUILayout.IntSlider("Default Zoom", _config.DefaultZoom, 1, 18);
            
            EditorGUILayout.Space(10);
            
            // Tile Settings
            EditorGUILayout.LabelField("Tile Settings", EditorStyles.boldLabel);
            _config.TileSize = EditorGUILayout.IntPopup("Tile Size", _config.TileSize, 
                new string[] { "256px", "512px" }, new int[] { 256, 512 });
            _config.UseRetinaScale = EditorGUILayout.Toggle("High DPI (Retina)", _config.UseRetinaScale);
            
            EditorGUILayout.Space(10);
            
            // Caching
            EditorGUILayout.LabelField("Caching", EditorStyles.boldLabel);
            _config.MaxCachedTiles = EditorGUILayout.IntSlider("Max Cached Tiles", _config.MaxCachedTiles, 50, 500);
            _config.EnableDiskCache = EditorGUILayout.Toggle("Enable Disk Cache", _config.EnableDiskCache);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_config);
            }
            
            EditorGUILayout.Space(20);
            
            // Test Connection
            EditorGUILayout.LabelField("Test Connection", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _testLatitude = EditorGUILayout.TextField("Test Lat", _testLatitude);
            _testLongitude = EditorGUILayout.TextField("Test Lon", _testLongitude);
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.BeginDisabledGroup(!_config.IsValid || _isLoading);
            if (GUILayout.Button(_isLoading ? "Loading..." : "Test API Connection", GUILayout.Height(30)))
            {
                TestConnection();
            }
            EditorGUI.EndDisabledGroup();
            
            // Preview
            if (_previewTexture != null)
            {
                EditorGUILayout.Space(10);
                float previewHeight = 200;
                float previewWidth = previewHeight * 1.5f;
                Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
                previewRect.x = (position.width - previewWidth) / 2;
                previewRect.width = previewWidth;
                GUI.DrawTexture(previewRect, _previewTexture, ScaleMode.ScaleToFit);
                EditorGUILayout.LabelField("✓ Mapbox connection successful!", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.Space(20);
            
            // Save button
            if (GUILayout.Button("Save Configuration", GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
                Debug.Log("[Mapbox] Configuration saved!");
            }
            
            // Open Mapbox website
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Open Mapbox Website"))
            {
                Application.OpenURL("https://account.mapbox.com/access-tokens/");
            }
        }
        
        private async void TestConnection()
        {
            if (!_config.IsValid)
            {
                Debug.LogError("[Mapbox] Invalid configuration - check your access token");
                return;
            }
            
            _isLoading = true;
            Repaint();
            
            try
            {
                double lat = double.Parse(_testLatitude);
                double lon = double.Parse(_testLongitude);
                
                string url = _config.GetStaticMapUrl(lat, lon, 14, 400, 300);
                Debug.Log($"[Mapbox] Testing URL: {url}");
                
                using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
                {
                    var operation = www.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        await System.Threading.Tasks.Task.Delay(100);
                    }
                    
                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        _previewTexture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                        Debug.Log("[Mapbox] ✓ Connection successful!");
                    }
                    else
                    {
                        Debug.LogError($"[Mapbox] Connection failed: {www.error}");
                        _previewTexture = null;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Mapbox] Test failed: {e.Message}");
            }
            finally
            {
                _isLoading = false;
                Repaint();
            }
        }
    }
}
