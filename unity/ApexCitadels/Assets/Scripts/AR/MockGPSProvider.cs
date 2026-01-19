// ============================================================================
// APEX CITADELS - MOCK GPS PROVIDER
// Simulates GPS location for desktop/editor testing without a mobile device
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ApexCitadels.AR
{
    /// <summary>
    /// Provides simulated GPS coordinates for testing location-based features
    /// in the Unity Editor without requiring a mobile device.
    /// </summary>
    public class MockGPSProvider : MonoBehaviour
    {
        public static MockGPSProvider Instance { get; private set; }

        [Header("Current Location")]
        [SerializeField] private double latitude = 37.7749;
        [SerializeField] private double longitude = -122.4194;
        [SerializeField] private double altitude = 10.0;
        [SerializeField] private float heading = 0f;

        [Header("Saved Locations")]
        [SerializeField] private List<SavedLocation> savedLocations = new List<SavedLocation>
        {
            new SavedLocation("San Francisco", 37.7749, -122.4194),
            new SavedLocation("New York", 40.7128, -74.0060),
            new SavedLocation("London", 51.5074, -0.1278),
            new SavedLocation("Tokyo", 35.6762, 139.6503),
            new SavedLocation("Sydney", -33.8688, 151.2093),
            new SavedLocation("Home Base", 0, 0) // Player can set this
        };

        [Header("Movement Simulation")]
        [SerializeField] private bool enableWASDMovement = true;
        [SerializeField] private float moveSpeed = 0.0001f; // degrees per second (roughly 11m per 0.0001 deg)
        [SerializeField] private float rotateSpeed = 45f; // degrees per second

        [Header("Debug Display")]
        [SerializeField] private bool showOnScreenDebug = true;

        // Properties
        public double Latitude => latitude;
        public double Longitude => longitude;
        public double Altitude => altitude;
        public float Heading => heading;

        // Events
        public event Action<double, double> OnLocationChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (enableWASDMovement)
            {
                HandleMovementInput();
            }
            HandleHotkeyInput();
        }

        private void HandleMovementInput()
        {
            // WASD for movement
            float latChange = 0;
            float lonChange = 0;

            if (Input.GetKey(KeyCode.W)) latChange += moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) latChange -= moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.A)) lonChange -= moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.D)) lonChange += moveSpeed * Time.deltaTime;

            // Q/E for rotation
            if (Input.GetKey(KeyCode.Q)) heading -= rotateSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) heading += rotateSpeed * Time.deltaTime;

            // Shift for faster movement
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                latChange *= 5f;
                lonChange *= 5f;
            }

            if (latChange != 0 || lonChange != 0)
            {
                SetLocation(latitude + latChange, longitude + lonChange);
            }

            // Normalize heading
            if (heading < 0) heading += 360;
            if (heading >= 360) heading -= 360;
        }

        private void HandleHotkeyInput()
        {
            // Number keys 1-9 for saved locations
            for (int i = 0; i < Mathf.Min(9, savedLocations.Count); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    var loc = savedLocations[i];
                    SetLocation(loc.Latitude, loc.Longitude);
                    ApexLogger.Log($"[MockGPS] Teleported to: {loc.Name}", ApexLogger.LogCategory.AR);
                }
            }

            // F5 to save current location
            if (Input.GetKeyDown(KeyCode.F5))
            {
                SaveCurrentLocation("Saved " + DateTime.Now.ToString("HH:mm:ss"));
            }
        }

        /// <summary>
        /// Set the simulated GPS location
        /// </summary>
        public void SetLocation(double lat, double lon)
        {
            latitude = lat;
            longitude = lon;
            OnLocationChanged?.Invoke(latitude, longitude);
        }

        /// <summary>
        /// Set full location including altitude and heading
        /// </summary>
        public void SetLocation(double lat, double lon, double alt, float head)
        {
            latitude = lat;
            longitude = lon;
            altitude = alt;
            heading = head;
            OnLocationChanged?.Invoke(latitude, longitude);
        }

        /// <summary>
        /// Teleport to a saved location by name
        /// </summary>
        public void TeleportTo(string locationName)
        {
            var loc = savedLocations.Find(l => l.Name.Equals(locationName, StringComparison.OrdinalIgnoreCase));
            if (loc != null)
            {
                SetLocation(loc.Latitude, loc.Longitude);
            }
        }

        /// <summary>
        /// Save current location with a name
        /// </summary>
        public void SaveCurrentLocation(string name)
        {
            savedLocations.Add(new SavedLocation(name, latitude, longitude));
            ApexLogger.Log($"[MockGPS] Saved location: {name} at {latitude}, {longitude}", ApexLogger.LogCategory.AR);
        }

        /// <summary>
        /// Move by a relative distance in meters (approximately)
        /// </summary>
        public void MoveByMeters(float northMeters, float eastMeters)
        {
            // Approximate conversion (1 degree latitude ≈ 111km)
            double latChange = northMeters / 111000.0;
            double lonChange = eastMeters / (111000.0 * Math.Cos(latitude * Math.PI / 180.0));
            
            SetLocation(latitude + latChange, longitude + lonChange);
        }

        /// <summary>
        /// Calculate distance to a point in meters
        /// </summary>
        public float GetDistanceTo(double targetLat, double targetLon)
        {
            return Territory.Territory.CalculateDistance(latitude, longitude, targetLat, targetLon);
        }

        private void OnGUI()
        {
            if (!showOnScreenDebug) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("<b>Mock GPS Provider</b>");
            GUILayout.Label($"Lat: {latitude:F6}");
            GUILayout.Label($"Lon: {longitude:F6}");
            GUILayout.Label($"Alt: {altitude:F1}m");
            GUILayout.Label($"Heading: {heading:F1}°");
            GUILayout.Space(5);
            GUILayout.Label("<color=yellow>WASD</color> = Move | <color=yellow>QE</color> = Rotate");
            GUILayout.Label("<color=yellow>1-9</color> = Teleport | <color=yellow>Shift</color> = Fast");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }

    /// <summary>
    /// Saved GPS location for quick teleportation
    /// </summary>
    [Serializable]
    public class SavedLocation
    {
        public string Name;
        public double Latitude;
        public double Longitude;

        public SavedLocation(string name, double lat, double lon)
        {
            Name = name;
            Latitude = lat;
            Longitude = lon;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom editor for MockGPSProvider with easy controls
    /// </summary>
    [CustomEditor(typeof(MockGPSProvider))]
    public class MockGPSProviderEditor : Editor
    {
        private string newLocationName = "New Location";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MockGPSProvider gps = (MockGPSProvider)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            // Quick teleport buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("San Francisco")) gps.SetLocation(37.7749, -122.4194);
            if (GUILayout.Button("New York")) gps.SetLocation(40.7128, -74.0060);
            if (GUILayout.Button("London")) gps.SetLocation(51.5074, -0.1278);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Tokyo")) gps.SetLocation(35.6762, 139.6503);
            if (GUILayout.Button("Sydney")) gps.SetLocation(-33.8688, 151.2093);
            if (GUILayout.Button("Paris")) gps.SetLocation(48.8566, 2.3522);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Move buttons
            EditorGUILayout.LabelField("Move (100m)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("↑ North")) gps.MoveByMeters(100, 0);
            if (GUILayout.Button("↓ South")) gps.MoveByMeters(-100, 0);
            if (GUILayout.Button("← West")) gps.MoveByMeters(0, -100);
            if (GUILayout.Button("→ East")) gps.MoveByMeters(0, 100);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Save current location
            EditorGUILayout.LabelField("Save Location", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            newLocationName = EditorGUILayout.TextField(newLocationName);
            if (GUILayout.Button("Save", GUILayout.Width(60)))
            {
                gps.SaveCurrentLocation(newLocationName);
            }
            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
#endif
}
