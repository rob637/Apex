// ============================================================================
// APEX CITADELS - GPS FANTASY SCENE
// Scene setup for the GPS-based fantasy kingdom
// ============================================================================
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Sets up the GPS Fantasy scene with all required components
    /// </summary>
    public class GPSFantasySceneSetup : MonoBehaviour
    {
        [Header("=== YOUR LOCATION ===")]
        [Tooltip("Your home coordinates - 504 Mashie Drive, Vienna, VA")]
        public double homeLatitude = 38.9065479;
        public double homeLongitude = -77.2476970;
        
        [Header("=== SAVED LOCATIONS ===")]
        public SavedLocation[] savedLocations = new SavedLocation[]
        {
            new SavedLocation("Home - 504 Mashie Dr", 38.9065479, -77.2476970),
            new SavedLocation("Vienna Town Green", 38.9032, -77.2646),
            new SavedLocation("Wolf Trap", 38.9381, -77.2653),
            new SavedLocation("Tysons Corner", 38.9187, -77.2311),
            new SavedLocation("Great Falls Park", 38.9985, -77.2525),
        };
        
        [Header("=== SETTINGS ===")]
        public bool useDeviceGPS = false; // Set true for mobile
        public float worldRadius = 200f;
        
        private GPSFantasyGenerator generator;
        
        [System.Serializable]
        public class SavedLocation
        {
            public string name;
            public double latitude;
            public double longitude;
            
            public SavedLocation(string n, double lat, double lon)
            {
                name = n;
                latitude = lat;
                longitude = lon;
            }
        }
        
        private void Start()
        {
            SetupScene();
        }
        
        private void SetupScene()
        {
            // Find or create GPS generator
            generator = FindObjectOfType<GPSFantasyGenerator>();
            if (generator == null)
            {
                var genObj = new GameObject("GPSFantasyGenerator");
                generator = genObj.AddComponent<GPSFantasyGenerator>();
            }
            
            // Set location to home by default
            generator.SetLocation(homeLatitude, homeLongitude);
            
            Debug.Log($"[GPSFantasy] Scene setup complete. Location: {homeLatitude}, {homeLongitude}");
        }
        
        /// <summary>
        /// Teleport to a saved location
        /// </summary>
        public void TeleportToLocation(int index)
        {
            if (index >= 0 && index < savedLocations.Length)
            {
                var loc = savedLocations[index];
                generator.SetLocation(loc.latitude, loc.longitude);
                Debug.Log($"[GPSFantasy] Teleporting to {loc.name}");
            }
        }
        
        /// <summary>
        /// Use device GPS (mobile only)
        /// </summary>
        public void UseRealGPS()
        {
            generator.UseDeviceGPS();
        }
        
        private void OnGUI()
        {
            // Simple debug UI
            GUILayout.BeginArea(new Rect(10, 10, 250, 300));
            GUILayout.Box("GPS Fantasy Kingdom");
            
            GUILayout.Label($"Location: {homeLatitude:F4}, {homeLongitude:F4}");
            
            GUILayout.Space(10);
            GUILayout.Label("Teleport to:");
            
            for (int i = 0; i < savedLocations.Length; i++)
            {
                if (GUILayout.Button(savedLocations[i].name))
                {
                    TeleportToLocation(i);
                }
            }
            
            GUILayout.Space(10);
            
            #if UNITY_ANDROID || UNITY_IOS
            if (GUILayout.Button("Use Device GPS"))
            {
                UseRealGPS();
            }
            #endif
            
            GUILayout.EndArea();
        }
    }
}
