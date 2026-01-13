using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;

namespace ApexCitadels.AR
{
    /// <summary>
    /// The Spatial Anchor Manager handles the persistence of AR objects across sessions and devices.
    /// This is the core component that enables the "Persistent Cube Test" - proving that objects
    /// placed in AR can be seen by other users at the exact same real-world location.
    /// 
    /// Architecture:
    /// 1. GeospatialAnchor (ARCore Geospatial API) - Uses Google's VPS for centimeter-accurate positioning
    /// 2. CloudAnchor (ARCore Cloud Anchors) - Fallback for areas without VPS coverage
    /// 3. LocalAnchor (AR Foundation) - Device-local anchoring for immediate feedback
    /// </summary>
    public class SpatialAnchorManager : MonoBehaviour
    {
        #region Singleton
        public static SpatialAnchorManager Instance { get; private set; }
        
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
        #endregion

        #region Inspector Fields
        [Header("AR Components")]
        [SerializeField] private ARAnchorManager _anchorManager;
        [SerializeField] private ARRaycastManager _raycastManager;
        [SerializeField] private AREarthManager _earthManager;
        [SerializeField] private ARCoreExtensions _arCoreExtensions;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject _anchorIndicatorPrefab;
        [SerializeField] private GameObject _placementPreviewPrefab;
        
        [Header("Settings")]
        [SerializeField] private float _geospatialAccuracyThreshold = 25f; // meters
        [SerializeField] private float _headingAccuracyThreshold = 25f; // degrees
        [SerializeField] private int _anchorResolutionTimeoutSeconds = 30;
        #endregion

        #region Events
        public event Action<SpatialAnchorData> OnAnchorCreated;
        public event Action<SpatialAnchorData> OnAnchorResolved;
        public event Action<string> OnAnchorFailed;
        public event Action<GeospatialState> OnGeospatialStateChanged;
        #endregion

        #region Private Fields
        private Dictionary<string, ARAnchor> _activeAnchors = new Dictionary<string, ARAnchor>();
        private Dictionary<string, GameObject> _anchoredObjects = new Dictionary<string, GameObject>();
        private GeospatialState _currentGeospatialState = GeospatialState.Initializing;
        private bool _isTrackingReady = false;
        #endregion

        #region Public Properties
        public bool IsTrackingReady => _isTrackingReady;
        public GeospatialState CurrentGeospatialState => _currentGeospatialState;
        public int ActiveAnchorCount => _activeAnchors.Count;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            StartCoroutine(InitializeGeospatialTracking());
        }

        private void Update()
        {
            UpdateGeospatialState();
        }

        private void OnDestroy()
        {
            // Clean up all anchors
            foreach (var anchor in _activeAnchors.Values)
            {
                if (anchor != null)
                {
                    Destroy(anchor.gameObject);
                }
            }
            _activeAnchors.Clear();
            _anchoredObjects.Clear();
        }
        #endregion

        #region Geospatial Initialization
        private IEnumerator InitializeGeospatialTracking()
        {
            Debug.Log("[SpatialAnchorManager] Initializing Geospatial tracking...");
            
            // Wait for AR session to be ready
            while (ARSession.state != ARSessionState.SessionTracking)
            {
                yield return null;
            }
            
            // Wait for Earth Manager to initialize
            while (_earthManager.EarthTrackingState != TrackingState.Tracking)
            {
                var earthState = _earthManager.EarthTrackingState;
                Debug.Log($"[SpatialAnchorManager] Waiting for Earth tracking. Current state: {earthState}");
                yield return new WaitForSeconds(0.5f);
            }
            
            // Check VPS availability at current location
            var pose = _earthManager.CameraGeospatialPose;
            Debug.Log($"[SpatialAnchorManager] Initial pose - Lat: {pose.Latitude}, Lng: {pose.Longitude}");
            
            // Wait for acceptable accuracy
            yield return StartCoroutine(WaitForAcceptableAccuracy());
            
            _isTrackingReady = true;
            Debug.Log("[SpatialAnchorManager] Geospatial tracking ready!");
        }

        private IEnumerator WaitForAcceptableAccuracy()
        {
            float timeout = 30f;
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                var pose = _earthManager.CameraGeospatialPose;
                
                if (pose.HorizontalAccuracy <= _geospatialAccuracyThreshold &&
                    pose.HeadingAccuracy <= _headingAccuracyThreshold)
                {
                    UpdateState(GeospatialState.Ready);
                    yield break;
                }
                
                UpdateState(GeospatialState.Localizing);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Timeout - use best available
            Debug.LogWarning("[SpatialAnchorManager] Accuracy threshold not met, using best available.");
            UpdateState(GeospatialState.LowAccuracy);
        }

        private void UpdateGeospatialState()
        {
            if (_earthManager == null) return;
            
            if (_earthManager.EarthTrackingState != TrackingState.Tracking)
            {
                UpdateState(GeospatialState.NotTracking);
                _isTrackingReady = false;
                return;
            }
            
            var pose = _earthManager.CameraGeospatialPose;
            
            if (pose.HorizontalAccuracy <= _geospatialAccuracyThreshold &&
                pose.HeadingAccuracy <= _headingAccuracyThreshold)
            {
                UpdateState(GeospatialState.Ready);
                _isTrackingReady = true;
            }
            else
            {
                UpdateState(GeospatialState.LowAccuracy);
            }
        }

        private void UpdateState(GeospatialState newState)
        {
            if (_currentGeospatialState != newState)
            {
                _currentGeospatialState = newState;
                OnGeospatialStateChanged?.Invoke(newState);
            }
        }
        #endregion

        #region Anchor Creation
        /// <summary>
        /// Creates a new Geospatial Anchor at the current camera position.
        /// This anchor will be persistent and resolvable by other devices.
        /// </summary>
        public async Task<SpatialAnchorData> CreateAnchorAtCurrentPosition(GameObject objectToAnchor)
        {
            if (!_isTrackingReady)
            {
                Debug.LogError("[SpatialAnchorManager] Tracking not ready. Cannot create anchor.");
                OnAnchorFailed?.Invoke("Tracking not ready");
                return null;
            }

            var pose = _earthManager.CameraGeospatialPose;
            return await CreateGeospatialAnchor(
                pose.Latitude,
                pose.Longitude,
                pose.Altitude,
                pose.EunRotation,
                objectToAnchor
            );
        }

        /// <summary>
        /// Creates a Geospatial Anchor at a specific world location.
        /// </summary>
        public async Task<SpatialAnchorData> CreateGeospatialAnchor(
            double latitude,
            double longitude,
            double altitude,
            Quaternion rotation,
            GameObject objectToAnchor)
        {
            Debug.Log($"[SpatialAnchorManager] Creating Geospatial Anchor at ({latitude}, {longitude})");

            try
            {
                // Create the ARGeospatialAnchor
                var anchor = _anchorManager.AddAnchor(
                    latitude,
                    longitude,
                    altitude,
                    rotation
                );

                if (anchor == null)
                {
                    Debug.LogError("[SpatialAnchorManager] Failed to create Geospatial Anchor");
                    OnAnchorFailed?.Invoke("Failed to create anchor");
                    return null;
                }

                // Generate unique ID
                string anchorId = GenerateAnchorId();
                
                // Create anchor data
                var anchorData = new SpatialAnchorData
                {
                    Id = anchorId,
                    Latitude = latitude,
                    Longitude = longitude,
                    Altitude = altitude,
                    RotationX = rotation.x,
                    RotationY = rotation.y,
                    RotationZ = rotation.z,
                    RotationW = rotation.w,
                    CreatedAt = DateTime.UtcNow,
                    AnchorType = AnchorType.Geospatial
                };

                // Store anchor reference
                _activeAnchors[anchorId] = anchor;
                
                // Parent the object to the anchor
                if (objectToAnchor != null)
                {
                    objectToAnchor.transform.SetParent(anchor.transform);
                    objectToAnchor.transform.localPosition = Vector3.zero;
                    objectToAnchor.transform.localRotation = Quaternion.identity;
                    _anchoredObjects[anchorId] = objectToAnchor;
                }

                // Add visual indicator
                if (_anchorIndicatorPrefab != null)
                {
                    var indicator = Instantiate(_anchorIndicatorPrefab, anchor.transform);
                    indicator.transform.localPosition = Vector3.zero;
                }

                Debug.Log($"[SpatialAnchorManager] Anchor created successfully: {anchorId}");
                OnAnchorCreated?.Invoke(anchorData);
                
                return anchorData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpatialAnchorManager] Error creating anchor: {ex.Message}");
                OnAnchorFailed?.Invoke(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Creates an anchor from a raycast hit on a detected plane.
        /// Useful for placing objects on surfaces (tables, floors, walls).
        /// </summary>
        public async Task<SpatialAnchorData> CreateAnchorFromRaycast(
            Vector2 screenPosition,
            GameObject objectToAnchor)
        {
            var hits = new List<ARRaycastHit>();
            
            if (!_raycastManager.Raycast(screenPosition, hits, TrackableTypes.PlaneWithinPolygon))
            {
                Debug.LogWarning("[SpatialAnchorManager] No plane detected at raycast position");
                OnAnchorFailed?.Invoke("No surface detected");
                return null;
            }

            var hit = hits[0];
            var pose = hit.pose;
            
            // Get geospatial coordinates for this position
            if (_isTrackingReady)
            {
                // Convert AR pose to geospatial coordinates
                var geoPose = _earthManager.Convert(pose);
                
                return await CreateGeospatialAnchor(
                    geoPose.Latitude,
                    geoPose.Longitude,
                    geoPose.Altitude,
                    pose.rotation,
                    objectToAnchor
                );
            }
            else
            {
                // Fallback to local anchor only
                return CreateLocalAnchor(pose, objectToAnchor);
            }
        }

        /// <summary>
        /// Creates a local-only anchor (not persistent across devices).
        /// Used as fallback when Geospatial is unavailable.
        /// </summary>
        private SpatialAnchorData CreateLocalAnchor(Pose pose, GameObject objectToAnchor)
        {
            var anchor = _anchorManager.AddAnchor(pose);
            
            if (anchor == null)
            {
                OnAnchorFailed?.Invoke("Failed to create local anchor");
                return null;
            }

            string anchorId = GenerateAnchorId();
            
            var anchorData = new SpatialAnchorData
            {
                Id = anchorId,
                CreatedAt = DateTime.UtcNow,
                AnchorType = AnchorType.Local
            };

            _activeAnchors[anchorId] = anchor;
            
            if (objectToAnchor != null)
            {
                objectToAnchor.transform.SetParent(anchor.transform);
                objectToAnchor.transform.localPosition = Vector3.zero;
                _anchoredObjects[anchorId] = objectToAnchor;
            }

            OnAnchorCreated?.Invoke(anchorData);
            return anchorData;
        }
        #endregion

        #region Anchor Resolution
        /// <summary>
        /// Resolves a Geospatial Anchor from stored data.
        /// This is how other devices "see" anchors created by different users.
        /// </summary>
        public async Task<bool> ResolveAnchor(SpatialAnchorData anchorData, GameObject objectToAttach)
        {
            if (anchorData.AnchorType != AnchorType.Geospatial)
            {
                Debug.LogWarning("[SpatialAnchorManager] Can only resolve Geospatial anchors");
                return false;
            }

            if (!_isTrackingReady)
            {
                Debug.LogError("[SpatialAnchorManager] Tracking not ready. Cannot resolve anchor.");
                return false;
            }

            Debug.Log($"[SpatialAnchorManager] Resolving anchor: {anchorData.Id}");

            try
            {
                var rotation = new Quaternion(
                    anchorData.RotationX,
                    anchorData.RotationY,
                    anchorData.RotationZ,
                    anchorData.RotationW
                );

                var anchor = _anchorManager.AddAnchor(
                    anchorData.Latitude,
                    anchorData.Longitude,
                    anchorData.Altitude,
                    rotation
                );

                if (anchor == null)
                {
                    Debug.LogError("[SpatialAnchorManager] Failed to resolve Geospatial anchor");
                    return false;
                }

                // Wait for anchor to stabilize
                await WaitForAnchorStability(anchor);

                _activeAnchors[anchorData.Id] = anchor;

                if (objectToAttach != null)
                {
                    objectToAttach.transform.SetParent(anchor.transform);
                    objectToAttach.transform.localPosition = Vector3.zero;
                    objectToAttach.transform.localRotation = Quaternion.identity;
                    _anchoredObjects[anchorData.Id] = objectToAttach;
                }

                Debug.Log($"[SpatialAnchorManager] Anchor resolved successfully: {anchorData.Id}");
                OnAnchorResolved?.Invoke(anchorData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpatialAnchorManager] Error resolving anchor: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resolves multiple anchors in the given area.
        /// Used when a player enters a new zone to load all nearby Citadels.
        /// </summary>
        public async Task<int> ResolveAnchorsInArea(
            IEnumerable<SpatialAnchorData> anchors,
            Func<SpatialAnchorData, GameObject> objectFactory)
        {
            int resolved = 0;

            foreach (var anchorData in anchors)
            {
                // Skip already resolved anchors
                if (_activeAnchors.ContainsKey(anchorData.Id))
                {
                    resolved++;
                    continue;
                }

                var obj = objectFactory?.Invoke(anchorData);
                if (await ResolveAnchor(anchorData, obj))
                {
                    resolved++;
                }

                // Small delay between resolutions to prevent overwhelming the system
                await Task.Delay(100);
            }

            Debug.Log($"[SpatialAnchorManager] Resolved {resolved} anchors in area");
            return resolved;
        }

        private async Task WaitForAnchorStability(ARAnchor anchor)
        {
            float elapsed = 0f;
            float timeout = _anchorResolutionTimeoutSeconds;

            while (elapsed < timeout)
            {
                if (anchor.trackingState == TrackingState.Tracking)
                {
                    return;
                }

                await Task.Delay(100);
                elapsed += 0.1f;
            }

            Debug.LogWarning("[SpatialAnchorManager] Anchor stability timeout reached");
        }
        #endregion

        #region Anchor Management
        /// <summary>
        /// Removes an anchor and its associated object.
        /// </summary>
        public void RemoveAnchor(string anchorId)
        {
            if (_activeAnchors.TryGetValue(anchorId, out var anchor))
            {
                if (anchor != null)
                {
                    Destroy(anchor.gameObject);
                }
                _activeAnchors.Remove(anchorId);
            }

            if (_anchoredObjects.TryGetValue(anchorId, out var obj))
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
                _anchoredObjects.Remove(anchorId);
            }

            Debug.Log($"[SpatialAnchorManager] Anchor removed: {anchorId}");
        }

        /// <summary>
        /// Gets the GameObject attached to an anchor.
        /// </summary>
        public GameObject GetAnchoredObject(string anchorId)
        {
            _anchoredObjects.TryGetValue(anchorId, out var obj);
            return obj;
        }

        /// <summary>
        /// Checks if an anchor is currently active.
        /// </summary>
        public bool IsAnchorActive(string anchorId)
        {
            return _activeAnchors.ContainsKey(anchorId);
        }

        /// <summary>
        /// Gets the current tracking state of an anchor.
        /// </summary>
        public TrackingState GetAnchorTrackingState(string anchorId)
        {
            if (_activeAnchors.TryGetValue(anchorId, out var anchor))
            {
                return anchor?.trackingState ?? TrackingState.None;
            }
            return TrackingState.None;
        }
        #endregion

        #region Utilities
        private string GenerateAnchorId()
        {
            return $"anchor_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Gets the current device's geospatial pose.
        /// </summary>
        public GeospatialPose GetCurrentGeospatialPose()
        {
            if (_earthManager != null && _earthManager.EarthTrackingState == TrackingState.Tracking)
            {
                return _earthManager.CameraGeospatialPose;
            }
            return default;
        }

        /// <summary>
        /// Calculates the distance between the device and a geospatial coordinate.
        /// </summary>
        public double GetDistanceToCoordinate(double latitude, double longitude)
        {
            var currentPose = GetCurrentGeospatialPose();
            return HaversineDistance(
                currentPose.Latitude, currentPose.Longitude,
                latitude, longitude
            );
        }

        private double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // Earth's radius in meters
            
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return R * c;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
        #endregion
    }

    #region Data Structures
    /// <summary>
    /// Serializable anchor data for Firebase storage and network transmission.
    /// </summary>
    [Serializable]
    public class SpatialAnchorData
    {
        public string Id;
        public double Latitude;
        public double Longitude;
        public double Altitude;
        public float RotationX;
        public float RotationY;
        public float RotationZ;
        public float RotationW;
        public DateTime CreatedAt;
        public AnchorType AnchorType;
        public string OwnerId;
        public string AttachedObjectType; // e.g., "Citadel", "ResourceNode"
        public string AttachedObjectId;
    }

    public enum AnchorType
    {
        Local,      // Device-local only, not persistent
        Geospatial, // ARCore Geospatial API (VPS-based)
        Cloud       // ARCore Cloud Anchors (visual feature-based)
    }

    public enum GeospatialState
    {
        Initializing,
        Localizing,
        Ready,
        LowAccuracy,
        NotTracking,
        Error
    }
    #endregion
}
