using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Core;
using ApexCitadels.Territory;
using ApexCitadels.Player;

namespace ApexCitadels.Map
{
    /// <summary>
    /// Marker types for map display
    /// </summary>
    public enum MapMarkerType
    {
        OwnedTerritory,
        EnemyTerritory,
        AllianceTerritory,
        NeutralTerritory,
        Player,
        ResourceNode,
        EventLocation
    }

    /// <summary>
    /// Map marker data
    /// </summary>
    [Serializable]
    public class MapMarker
    {
        public string Id;
        public string Name;
        public MapMarkerType Type;
        public double Latitude;
        public double Longitude;
        public string OwnerId;
        public string OwnerName;
        public Color MarkerColor;
        public bool IsSelected;

        public MapMarker() { }

        public MapMarker(string id, string name, MapMarkerType type, double lat, double lon)
        {
            Id = id;
            Name = name;
            Type = type;
            Latitude = lat;
            Longitude = lon;
            MarkerColor = GetColorForType(type);
        }

        public static Color GetColorForType(MapMarkerType type)
        {
            return type switch
            {
                MapMarkerType.OwnedTerritory => new Color(0.2f, 0.8f, 0.2f), // Green
                MapMarkerType.EnemyTerritory => new Color(0.8f, 0.2f, 0.2f), // Red
                MapMarkerType.AllianceTerritory => new Color(0.2f, 0.6f, 0.9f), // Blue
                MapMarkerType.NeutralTerritory => new Color(0.7f, 0.7f, 0.7f), // Gray
                MapMarkerType.Player => new Color(1f, 0.9f, 0.2f), // Yellow
                MapMarkerType.ResourceNode => new Color(0.9f, 0.6f, 0.1f), // Orange
                MapMarkerType.EventLocation => new Color(0.8f, 0.2f, 0.8f), // Purple
                _ => Color.white
            };
        }
    }

    /// <summary>
    /// Manages the 2D map view of territories
    /// </summary>
    public class MapViewController : MonoBehaviour
    {
        public static MapViewController Instance { get; private set; }

        [Header("Map Settings")]
        [SerializeField] private float defaultZoomLevel = 15f;
        [SerializeField] private float minZoomLevel = 10f;
        [SerializeField] private float maxZoomLevel = 20f;
        [SerializeField] private float loadRadius = 5000f; // Meters

        [Header("UI References")]
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private GameObject markerPrefab;
        [SerializeField] private RectTransform playerMarker;

        // Events
        public event Action<MapMarker> OnMarkerSelected;
        public event Action<double, double> OnMapTapped;
        public event Action<float> OnZoomChanged;

        // State
        private double _centerLatitude;
        private double _centerLongitude;
        private float _currentZoom;
        private List<MapMarker> _markers = new List<MapMarker>();
        private Dictionary<string, GameObject> _markerObjects = new Dictionary<string, GameObject>();
        private bool _followPlayer = true;

        public double CenterLatitude => _centerLatitude;
        public double CenterLongitude => _centerLongitude;
        public float CurrentZoom => _currentZoom;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _currentZoom = defaultZoomLevel;
        }

        private void Start()
        {
            // Get initial location
            StartCoroutine(InitializeLocation());
        }

        private System.Collections.IEnumerator InitializeLocation()
        {
            // Request location permission
            if (!Input.location.isEnabledByUser)
            {
                ApexLogger.Log("Location not enabled", LogCategory.Map);
                // Use default location for testing
                SetCenter(37.7749, -122.4194); // San Francisco
                yield break;
            }

            Input.location.Start(5f, 5f);

            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            if (Input.location.status == LocationServiceStatus.Running)
            {
                SetCenter(
                    Input.location.lastData.latitude,
                    Input.location.lastData.longitude
                );
            }
            else
            {
                SetCenter(37.7749, -122.4194); // Default
            }

            // Start continuous updates
            StartCoroutine(UpdatePlayerLocation());
        }

        private System.Collections.IEnumerator UpdatePlayerLocation()
        {
            while (true)
            {
                yield return new WaitForSeconds(5f);

                if (Input.location.status == LocationServiceStatus.Running)
                {
                    double lat = Input.location.lastData.latitude;
                    double lon = Input.location.lastData.longitude;

                    if (_followPlayer)
                    {
                        SetCenter(lat, lon);
                    }

                    UpdatePlayerMarker(lat, lon);
                    LoadNearbyTerritories(lat, lon);
                }
            }
        }

        #region Map Control

        /// <summary>
        /// Set map center position
        /// </summary>
        public void SetCenter(double latitude, double longitude)
        {
            _centerLatitude = latitude;
            _centerLongitude = longitude;
            RefreshMarkers();
        }

        /// <summary>
        /// Zoom in
        /// </summary>
        public void ZoomIn()
        {
            SetZoom(_currentZoom + 1);
        }

        /// <summary>
        /// Zoom out
        /// </summary>
        public void ZoomOut()
        {
            SetZoom(_currentZoom - 1);
        }

        /// <summary>
        /// Set specific zoom level
        /// </summary>
        public void SetZoom(float zoom)
        {
            _currentZoom = Mathf.Clamp(zoom, minZoomLevel, maxZoomLevel);
            OnZoomChanged?.Invoke(_currentZoom);
            RefreshMarkers();
        }

        /// <summary>
        /// Toggle following player location
        /// </summary>
        public void ToggleFollowPlayer()
        {
            _followPlayer = !_followPlayer;
            ApexLogger.Log($"Follow player: {_followPlayer}", LogCategory.Map);
        }

        /// <summary>
        /// Center on player location
        /// </summary>
        public void CenterOnPlayer()
        {
            _followPlayer = true;
            if (Input.location.status == LocationServiceStatus.Running)
            {
                SetCenter(
                    Input.location.lastData.latitude,
                    Input.location.lastData.longitude
                );
            }
        }

        #endregion

        #region Markers

        /// <summary>
        /// Add a marker to the map
        /// </summary>
        public void AddMarker(MapMarker marker)
        {
            if (_markers.Exists(m => m.Id == marker.Id))
            {
                UpdateMarker(marker);
                return;
            }

            _markers.Add(marker);
            CreateMarkerObject(marker);
        }

        /// <summary>
        /// Update an existing marker
        /// </summary>
        public void UpdateMarker(MapMarker marker)
        {
            int index = _markers.FindIndex(m => m.Id == marker.Id);
            if (index >= 0)
            {
                _markers[index] = marker;
                RefreshMarkerObject(marker);
            }
        }

        /// <summary>
        /// Remove a marker from the map
        /// </summary>
        public void RemoveMarker(string markerId)
        {
            _markers.RemoveAll(m => m.Id == markerId);

            if (_markerObjects.TryGetValue(markerId, out var obj))
            {
                Destroy(obj);
                _markerObjects.Remove(markerId);
            }
        }

        /// <summary>
        /// Clear all markers
        /// </summary>
        public void ClearMarkers()
        {
            foreach (var obj in _markerObjects.Values)
            {
                Destroy(obj);
            }
            _markerObjects.Clear();
            _markers.Clear();
        }

        /// <summary>
        /// Select a marker
        /// </summary>
        public void SelectMarker(string markerId)
        {
            foreach (var marker in _markers)
            {
                marker.IsSelected = marker.Id == markerId;
            }

            var selected = _markers.Find(m => m.Id == markerId);
            if (selected != null)
            {
                OnMarkerSelected?.Invoke(selected);
            }
        }

        private void CreateMarkerObject(MapMarker marker)
        {
            if (mapContainer == null || markerPrefab == null) return;

            var obj = Instantiate(markerPrefab, mapContainer);
            _markerObjects[marker.Id] = obj;
            RefreshMarkerObject(marker);
        }

        private void RefreshMarkerObject(MapMarker marker)
        {
            if (!_markerObjects.TryGetValue(marker.Id, out var obj)) return;

            // Convert lat/lon to screen position
            Vector2 screenPos = GeoToScreenPosition(marker.Latitude, marker.Longitude);
            obj.GetComponent<RectTransform>().anchoredPosition = screenPos;

            // Set color
            var image = obj.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = marker.MarkerColor;
            }

            // Set scale for selected
            float scale = marker.IsSelected ? 1.5f : 1f;
            obj.transform.localScale = Vector3.one * scale;
        }

        private void RefreshMarkers()
        {
            foreach (var marker in _markers)
            {
                RefreshMarkerObject(marker);
            }
        }

        private void UpdatePlayerMarker(double latitude, double longitude)
        {
            if (playerMarker == null) return;

            Vector2 screenPos = GeoToScreenPosition(latitude, longitude);
            playerMarker.anchoredPosition = screenPos;
        }

        #endregion

        #region Coordinate Conversion

        /// <summary>
        /// Convert geographic coordinates to screen position
        /// </summary>
        public Vector2 GeoToScreenPosition(double latitude, double longitude)
        {
            if (mapContainer == null) return Vector2.zero;

            // Calculate pixel offset from center
            double metersPerPixel = GetMetersPerPixel();
            
            double deltaLat = latitude - _centerLatitude;
            double deltaLon = longitude - _centerLongitude;

            // Convert to meters (approximate)
            double metersNorth = deltaLat * 111320;
            double metersEast = deltaLon * 111320 * Math.Cos(_centerLatitude * Math.PI / 180);

            // Convert to pixels
            float x = (float)(metersEast / metersPerPixel);
            float y = (float)(metersNorth / metersPerPixel);

            return new Vector2(x, y);
        }

        /// <summary>
        /// Convert screen position to geographic coordinates
        /// </summary>
        public (double Latitude, double Longitude) ScreenToGeoPosition(Vector2 screenPos)
        {
            double metersPerPixel = GetMetersPerPixel();

            double metersEast = screenPos.x * metersPerPixel;
            double metersNorth = screenPos.y * metersPerPixel;

            double latitude = _centerLatitude + (metersNorth / 111320);
            double longitude = _centerLongitude + (metersEast / (111320 * Math.Cos(_centerLatitude * Math.PI / 180)));

            return (latitude, longitude);
        }

        private double GetMetersPerPixel()
        {
            // At zoom level 15, roughly 4.77 meters per pixel
            // Doubles/halves for each zoom level
            return 156543.03392 * Math.Cos(_centerLatitude * Math.PI / 180) / Math.Pow(2, _currentZoom);
        }

        /// <summary>
        /// Calculate distance between two points in meters
        /// </summary>
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371e3; // Earth radius in meters
            double phi1 = lat1 * Math.PI / 180;
            double phi2 = lat2 * Math.PI / 180;
            double deltaPhi = (lat2 - lat1) * Math.PI / 180;
            double deltaLambda = (lon2 - lon1) * Math.PI / 180;

            double a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                       Math.Cos(phi1) * Math.Cos(phi2) *
                       Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        #endregion

        #region Territory Loading

        private async void LoadNearbyTerritories(double latitude, double longitude)
        {
            if (TerritoryManager.Instance == null) return;

            var territories = await TerritoryManager.Instance.GetNearbyTerritories(latitude, longitude, loadRadius);
            
            string currentPlayerId = PlayerManager.Instance?.GetCurrentPlayerId() ?? "";
            string allianceId = Alliance.AllianceManager.Instance?.CurrentAlliance?.Id ?? "";

            foreach (var territory in territories)
            {
                MapMarkerType type;
                if (string.IsNullOrEmpty(territory.OwnerId))
                {
                    type = MapMarkerType.NeutralTerritory;
                }
                else if (territory.OwnerId == currentPlayerId)
                {
                    type = MapMarkerType.OwnedTerritory;
                }
                else if (territory.AllianceId == allianceId && !string.IsNullOrEmpty(allianceId))
                {
                    type = MapMarkerType.AllianceTerritory;
                }
                else
                {
                    type = MapMarkerType.EnemyTerritory;
                }

                AddMarker(new MapMarker(territory.Id, territory.Name, type, territory.Latitude, territory.Longitude)
                {
                    OwnerId = territory.OwnerId,
                    OwnerName = territory.OwnerName
                });
            }
        }

        #endregion

        #region Map Interaction

        /// <summary>
        /// Handle tap on map
        /// </summary>
        public void OnMapTap(Vector2 tapPosition)
        {
            // Check if tapped on a marker
            foreach (var kvp in _markerObjects)
            {
                var rect = kvp.Value.GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, tapPosition))
                {
                    SelectMarker(kvp.Key);
                    return;
                }
            }

            // Convert to geo coordinates
            var localPos = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(mapContainer, tapPosition, null, out localPos);
            var (lat, lon) = ScreenToGeoPosition(localPos);

            OnMapTapped?.Invoke(lat, lon);
        }

        #endregion
    }
}
