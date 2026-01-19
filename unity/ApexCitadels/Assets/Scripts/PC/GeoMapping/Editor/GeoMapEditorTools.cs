// ============================================================================
// APEX CITADELS - GEO MAP EDITOR TOOLS
// Editor utilities to set up and test real-world map functionality
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using ApexCitadels.PC.GeoMapping;

namespace ApexCitadels.PC.Editor
{
    /// <summary>
    /// Editor menu items and tools for the real-world map system
    /// </summary>
    public static class GeoMapEditorTools
    {
        [MenuItem("Apex Citadels/GeoMap/Create Real World Map", false, 80)]
        public static void CreateRealWorldMap()
        {
            // Check if already exists
            if (Object.FindFirstObjectByType<RealWorldMapRenderer>() != null)
            {
                if (!EditorUtility.DisplayDialog("Real World Map Exists",
                    "A RealWorldMapRenderer already exists in the scene. Do you want to create another one?",
                    "Yes", "No"))
                {
                    return;
                }
            }

            // Create root object
            GameObject mapRoot = new GameObject("RealWorldMap");
            Undo.RegisterCreatedObjectUndo(mapRoot, "Create Real World Map");

            // Add components
            var mapRenderer = mapRoot.AddComponent<RealWorldMapRenderer>();
            var tileProvider = mapRoot.AddComponent<MapTileProvider>();

            // Create camera
            GameObject cameraObj = new GameObject("GeoMapCamera");
            cameraObj.transform.parent = mapRoot.transform;
            cameraObj.transform.localPosition = new Vector3(0, 300f, -200f);
            cameraObj.transform.localRotation = Quaternion.Euler(60f, 0, 0);

            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.4f, 0.6f, 0.9f); // Sky blue
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 5000f;
            cam.fieldOfView = 60f;

            cameraObj.AddComponent<GeoMapCameraController>();

            // Add audio listener if none exists
            if (Object.FindFirstObjectByType<AudioListener>() == null)
            {
                cameraObj.AddComponent<AudioListener>();
            }

            // Select and frame
            Selection.activeGameObject = mapRoot;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log("[GeoMapEditor] Created Real World Map setup");
            EditorUtility.DisplayDialog("Real World Map Created",
                "Real World Map system created!\n\n" +
                "The map will display real-world geography from OpenStreetMap.\n\n" +
                "Features:\n" +
                "- Real map tiles from CartoDB\n" +
                "- Territory overlays with GPS coordinates\n" +
                "- Pan/zoom/rotate camera controls\n" +
                "- Demo territories in Washington DC\n\n" +
                "Press Play to see it in action!",
                "OK");
        }

        [MenuItem("Apex Citadels/GeoMap/Quick Locations/Washington DC", false, 81)]
        public static void GoToWashingtonDC()
        {
            SetMapLocation(38.8951, -77.0364, "Washington DC");
        }

        [MenuItem("Apex Citadels/GeoMap/Quick Locations/New York City", false, 82)]
        public static void GoToNewYorkCity()
        {
            SetMapLocation(40.7580, -73.9855, "New York City");
        }

        [MenuItem("Apex Citadels/GeoMap/Quick Locations/San Francisco", false, 83)]
        public static void GoToSanFrancisco()
        {
            SetMapLocation(37.7749, -122.4194, "San Francisco");
        }

        [MenuItem("Apex Citadels/GeoMap/Quick Locations/London", false, 84)]
        public static void GoToLondon()
        {
            SetMapLocation(51.5074, -0.1278, "London");
        }

        [MenuItem("Apex Citadels/GeoMap/Quick Locations/Tokyo", false, 85)]
        public static void GoToTokyo()
        {
            SetMapLocation(35.6762, 139.6503, "Tokyo");
        }

        [MenuItem("Apex Citadels/GeoMap/Quick Locations/Sydney", false, 86)]
        public static void GoToSydney()
        {
            SetMapLocation(-33.8688, 151.2093, "Sydney");
        }

        [MenuItem("Apex Citadels/GeoMap/Quick Locations/Paris", false, 87)]
        public static void GoToParis()
        {
            SetMapLocation(48.8566, 2.3522, "Paris");
        }

        [MenuItem("Apex Citadels/GeoMap/Quick Locations/Vienna VA (Test Data)", false, 88)]
        public static void GoToViennaVA()
        {
            SetMapLocation(38.9010, -77.2642, "Vienna, VA (Test Data Location)");
        }

        private static void SetMapLocation(double lat, double lon, string name)
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Not in Play Mode",
                    $"Location: {name}\nLat: {lat}, Lon: {lon}\n\nStart Play Mode to navigate the map.",
                    "OK");
                return;
            }

            var mapRenderer = Object.FindFirstObjectByType<RealWorldMapRenderer>();
            if (mapRenderer != null)
            {
                mapRenderer.GoToLocation(new GeoCoordinate(lat, lon));
                Debug.Log($"[GeoMapEditor] Moved to {name} ({lat}, {lon})");
            }
            else
            {
                EditorUtility.DisplayDialog("No Map Found",
                    "No RealWorldMapRenderer found in scene.\n" +
                    "Use Tools -> Apex Citadels -> GeoMap -> Create Real World Map first.",
                    "OK");
            }
        }

        [MenuItem("Apex Citadels/GeoMap/Change Provider/CartoDB Voyager (Default)", false, 89)]
        public static void SetProviderCartoDBVoyager()
        {
            SetMapProvider(MapProvider.CartoDBVoyager);
        }

        [MenuItem("Apex Citadels/GeoMap/Change Provider/CartoDB Dark", false, 90)]
        public static void SetProviderCartoDBDark()
        {
            SetMapProvider(MapProvider.CartoDBDarkMatter);
        }

        [MenuItem("Apex Citadels/GeoMap/Change Provider/CartoDB Light", false, 91)]
        public static void SetProviderCartoDBLight()
        {
            SetMapProvider(MapProvider.CartoDBPositron);
        }

        [MenuItem("Apex Citadels/GeoMap/Change Provider/OpenStreetMap", false, 92)]
        public static void SetProviderOSM()
        {
            SetMapProvider(MapProvider.OpenStreetMap);
        }

        [MenuItem("Apex Citadels/GeoMap/Change Provider/Esri Satellite", false, 93)]
        public static void SetProviderEsriSatellite()
        {
            SetMapProvider(MapProvider.EsriWorldImagery);
        }

        [MenuItem("Apex Citadels/GeoMap/Change Provider/Stamen Terrain", false, 94)]
        public static void SetProviderStamenTerrain()
        {
            SetMapProvider(MapProvider.StamenTerrain);
        }

        [MenuItem("Apex Citadels/GeoMap/Change Provider/Stamen Watercolor (Fantasy)", false, 95)]
        public static void SetProviderStamenWatercolor()
        {
            SetMapProvider(MapProvider.StamenWatercolor);
        }

        private static void SetMapProvider(MapProvider provider)
        {
            // This would need to modify the MapTileProvider serialized field
            // For now, just log the instruction
            Debug.Log($"[GeoMapEditor] To change provider to {provider}:");
            Debug.Log("1. Select MapTileProvider in the scene");
            Debug.Log("2. Change the 'Provider' field in the Inspector");
            Debug.Log("3. If in Play mode, clear cache and reload tiles");
            
            EditorUtility.DisplayDialog("Change Map Provider",
                $"To change to {provider}:\n\n" +
                "1. Select the MapTileProvider in the hierarchy\n" +
                "2. In Inspector, change 'Provider' field\n" +
                "3. If playing, the map will update automatically",
                "OK");
        }

        [MenuItem("Apex Citadels/GeoMap/Documentation", false, 99)]
        public static void OpenDocumentation()
        {
            Debug.Log(@"
=== APEX CITADELS REAL WORLD MAP SYSTEM ===

OVERVIEW
--------
The Real World Map system displays actual geography from map tile providers,
with player territories overlaid at their real GPS coordinates.

This is the 'PC window' into the same world that AR mobile players explore.
'One World - Two Ways to Access'

COMPONENTS
----------
- GeoCoordinates.cs - GPS coordinate utilities and projections
- MapTileProvider.cs - Fetches map tiles from various providers
- RealWorldMapRenderer.cs - Renders tiles and territories
- GeoMapCameraController.cs - Camera navigation controls

CONTROLS (Play Mode)
--------------------
- WASD / Arrows - Pan map
- Mouse Scroll - Zoom in/out
- Right Mouse Drag - Rotate view
- Middle Mouse Drag - Pan map
- Q/E - Rotate yaw
- R/F - Adjust pitch
- Home - Reset view
- Page Up/Down - Zoom

MAP PROVIDERS
-------------
- CartoDB Voyager (default) - Colorful, detailed
- CartoDB Light - Minimal light theme
- CartoDB Dark - Minimal dark theme
- OpenStreetMap - Standard OSM tiles
- Esri World Imagery - Satellite photos
- Stamen Terrain - Topographic
- Stamen Watercolor - Artistic (fantasy-style)
- Mapbox - Requires API key

TERRITORY DATA
--------------
Territories are loaded from Firebase Firestore with:
- id - Unique identifier
- name - Display name
- latitude/longitude - GPS coordinates
- radius - Territory size in meters
- ownerId - Player who owns it
- allianceId - Alliance affiliation

ARCHITECTURE
------------
               +----------------------+
               |   AR Mobile Client   |
               |   (Stakes territory  |
               |    at GPS location)  |
               +----------------------+
                          |
                          v
               +----------------------+
               |      Firebase        |
               |   (territories,      |
               |    players, etc.)    |
               +----------------------+
                          |
                          v
               +----------------------+
               |   PC Map Client      |
               |   (Shows real-world  |
               |    map with same     |
               |    territories)      |
               +----------------------+

");
        }
    }

    /// <summary>
    /// Custom inspector for RealWorldMapRenderer
    /// </summary>
    [CustomEditor(typeof(RealWorldMapRenderer))]
    public class RealWorldMapRendererEditor : UnityEditor.Editor
    {
        private double _inputLat = 38.8951;
        private double _inputLon = -77.0364;
        private string _searchAddress = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Navigation", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _inputLat = EditorGUILayout.DoubleField("Latitude", _inputLat);
            _inputLon = EditorGUILayout.DoubleField("Longitude", _inputLon);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Go To Coordinates"))
            {
                if (Application.isPlaying)
                {
                    var renderer = (RealWorldMapRenderer)target;
                    renderer.GoToLocation(new GeoCoordinate(_inputLat, _inputLon));
                }
                else
                {
                    EditorUtility.DisplayDialog("Play Mode Required",
                        "Enter Play mode to navigate the map.", "OK");
                }
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Washington DC")) { _inputLat = 38.8951; _inputLon = -77.0364; }
            if (GUILayout.Button("NYC")) { _inputLat = 40.7580; _inputLon = -73.9855; }
            if (GUILayout.Button("SF")) { _inputLat = 37.7749; _inputLon = -122.4194; }
            if (GUILayout.Button("London")) { _inputLat = 51.5074; _inputLon = -0.1278; }
            EditorGUILayout.EndHorizontal();

            if (Application.isPlaying)
            {
                var renderer = (RealWorldMapRenderer)target;
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Center: {renderer.CurrentCenter}");
                EditorGUILayout.LabelField($"Zoom: {renderer.CurrentZoom}");
                EditorGUILayout.LabelField($"Territories: {renderer.Territories?.Count ?? 0}");
            }
        }
    }
}
#endif
