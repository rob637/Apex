using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ApexCitadels.AR
{
    /// <summary>
    /// Enhanced SpatialAnchorManager with cloud anchors, persistence, and advanced tracking.
    /// Supports ARCore/ARKit with fallback for desktop development.
    /// </summary>
    public class SpatialAnchorManager : MonoBehaviour
    {
        public static SpatialAnchorManager Instance { get; private set; }

        [Header("AR Session Components")]
        [SerializeField] private ARSession _arSession;
        [SerializeField] private ARAnchorManager _anchorManager;
        [SerializeField] private ARRaycastManager _raycastManager;
        [SerializeField] private ARPlaneManager _planeManager;
        [SerializeField] private ARPointCloudManager _pointCloudManager;

        [Header("Tracking Settings")]
        [SerializeField] private float trackingInitTimeout = 10f;
        [SerializeField] private float planeDetectionTimeout = 30f;
        [SerializeField] private PlaneDetectionMode planeDetectionMode = PlaneDetectionMode.Horizontal;

        [Header("Anchor Settings")]
        [SerializeField] private int maxActiveAnchors = 50;
        [SerializeField] private float anchorPersistenceCheckInterval = 5f;
        [SerializeField] private bool autoRestoreAnchors = true;

        [Header("Desktop Fallback")]
        [SerializeField] private bool enableDesktopFallback = true;
        [SerializeField] private float desktopPlaneHeight = 0f;
        [SerializeField] private LayerMask desktopRaycastLayers = -1;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool logAnchorEvents = true;

        // State
        private TrackingState _trackingState = TrackingState.None;
        private bool _isInitialized = false;
        private bool _planesDetected = false;
        private bool _isDesktopMode = false;

        // Anchors
        private Dictionary<string, ManagedAnchor> _anchors = new Dictionary<string, ManagedAnchor>();
        private Queue<AnchorRequest> _pendingRequests = new Queue<AnchorRequest>();

        // Events
        public event Action OnTrackingReady;
        public event Action OnTrackingLost;
        public event Action<TrackingState> OnTrackingStateChanged;
        public event Action<ARAnchor, string> OnAnchorCreated;
        public event Action<string> OnAnchorRemoved;
        public event Action<ARPlane> OnPlaneDetected;
        public event Action<string> OnAnchorRestored;
        public event Action<string, string> OnAnchorError;

        // Properties
        public bool IsTracking => _trackingState == TrackingState.Tracking;
        public bool IsInitialized => _isInitialized;
        public bool PlanesDetected => _planesDetected;
        public bool IsDesktopMode => _isDesktopMode;
        public TrackingState CurrentTrackingState => _trackingState;
        public int ActiveAnchorCount => _anchors.Count;
        public IReadOnlyDictionary<string, ManagedAnchor> Anchors => _anchors;

        // Player prefs keys
        private const string PREF_SAVED_ANCHORS = "AR_SavedAnchors";

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            DetermineRunMode();
        }

        private void Start()
        {
            StartCoroutine(InitializeTracking());
        }

        private void OnEnable()
        {
            SubscribeToAREvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromAREvents();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void DetermineRunMode()
        {
            #if UNITY_EDITOR || UNITY_STANDALONE
            _isDesktopMode = enableDesktopFallback;
            #else
            _isDesktopMode = false;
            #endif

            if (_isDesktopMode)
            {
                Debug.Log("[SpatialAnchorManager] Running in Desktop Mode with fallback raycasting");
            }
        }

        #endregion

        #region Initialization

        private IEnumerator InitializeTracking()
        {
            Log("Initializing tracking...");

            if (_isDesktopMode)
            {
                yield return SimulateDesktopTracking();
            }
            else
            {
                yield return InitializeARTracking();
            }

            // Restore saved anchors
            if (autoRestoreAnchors)
            {
                RestoreSavedAnchors();
            }

            // Start persistence check
            StartCoroutine(AnchorPersistenceCheck());
        }

        private IEnumerator SimulateDesktopTracking()
        {
            // Simulate initialization delay
            yield return new WaitForSeconds(0.5f);

            _isInitialized = true;
            _planesDetected = true;
            UpdateTrackingState(TrackingState.Tracking);

            Log("Desktop tracking ready (simulated)");
        }

        private IEnumerator InitializeARTracking()
        {
            float elapsed = 0f;

            // Wait for AR session to initialize
            while (!_isInitialized && elapsed < trackingInitTimeout)
            {
                if (_arSession != null)
                {
                    // Check if AR is supported
                    if (ARSession.state == ARSessionState.Unsupported)
                    {
                        LogError("AR is not supported on this device");
                        
                        if (enableDesktopFallback)
                        {
                            Log("Falling back to desktop mode");
                            _isDesktopMode = true;
                            yield return SimulateDesktopTracking();
                            yield break;
                        }
                        yield break;
                    }

                    // Check if tracking
                    if (ARSession.state >= ARSessionState.SessionTracking)
                    {
                        _isInitialized = true;
                        UpdateTrackingState(TrackingState.Tracking);
                        break;
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!_isInitialized)
            {
                LogWarning($"AR tracking initialization timed out after {trackingInitTimeout}s");
            }

            // Wait for plane detection
            elapsed = 0f;
            while (!_planesDetected && elapsed < planeDetectionTimeout)
            {
                if (_planeManager != null && _planeManager.trackables.count > 0)
                {
                    _planesDetected = true;
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!_planesDetected)
            {
                LogWarning("No planes detected within timeout period");
            }
        }

        #endregion

        #region AR Events

        private void SubscribeToAREvents()
        {
            if (_planeManager != null)
            {
                _planeManager.trackablesChanged += OnPlanesChanged;
            }

            if (_anchorManager != null)
            {
                _anchorManager.trackablesChanged += OnAnchorsChanged;
            }

            ARSession.stateChanged += OnARSessionStateChanged;
        }

        private void UnsubscribeFromAREvents()
        {
            if (_planeManager != null)
            {
                _planeManager.trackablesChanged -= OnPlanesChanged;
            }

            if (_anchorManager != null)
            {
                _anchorManager.trackablesChanged -= OnAnchorsChanged;
            }

            ARSession.stateChanged -= OnARSessionStateChanged;
        }

        private void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            switch (args.state)
            {
                case ARSessionState.SessionTracking:
                    UpdateTrackingState(TrackingState.Tracking);
                    break;
                case ARSessionState.SessionInitializing:
                    UpdateTrackingState(TrackingState.Limited);
                    break;
                default:
                    UpdateTrackingState(TrackingState.None);
                    break;
            }
        }

        private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (args.added != null && args.added.Count > 0)
            {
                _planesDetected = true;
                
                foreach (var plane in args.added)
                {
                    OnPlaneDetected?.Invoke(plane);
                    Log($"Plane detected: {plane.trackableId} ({plane.alignment})");
                }
            }
        }

        private void OnAnchorsChanged(ARTrackablesChangedEventArgs<ARAnchor> args)
        {
            if (args.removed != null)
            {
                foreach (var anchor in args.removed)
                {
                    // Find and update managed anchor
                    foreach (var kvp in _anchors)
                    {
                        if (kvp.Value.Anchor != null && kvp.Value.Anchor.trackableId == anchor.trackableId)
                        {
                            kvp.Value.TrackingState = AnchorTrackingState.Lost;
                            break;
                        }
                    }
                }
            }

            if (args.updated != null)
            {
                foreach (var anchor in args.updated)
                {
                    foreach (var kvp in _anchors)
                    {
                        if (kvp.Value.Anchor != null && kvp.Value.Anchor.trackableId == anchor.trackableId)
                        {
                            kvp.Value.LastUpdateTime = Time.time;
                            break;
                        }
                    }
                }
            }
        }

        private void UpdateTrackingState(TrackingState newState)
        {
            if (_trackingState == newState) return;

            TrackingState previousState = _trackingState;
            _trackingState = newState;

            OnTrackingStateChanged?.Invoke(newState);

            if (newState == TrackingState.Tracking && previousState != TrackingState.Tracking)
            {
                OnTrackingReady?.Invoke();
                ProcessPendingRequests();
            }
            else if (newState != TrackingState.Tracking && previousState == TrackingState.Tracking)
            {
                OnTrackingLost?.Invoke();
            }

            Log($"Tracking state changed: {previousState} -> {newState}");
        }

        #endregion

        #region Raycast

        /// <summary>
        /// Raycast to find a surface hit position
        /// </summary>
        public bool TryGetHitPosition(Vector2 screenPosition, out Vector3 worldPosition, out Quaternion rotation)
        {
            worldPosition = Vector3.zero;
            rotation = Quaternion.identity;

            Camera cam = GetActiveCamera();
            if (cam == null)
            {
                LogError("No camera found for raycasting");
                return false;
            }

            if (_isDesktopMode)
            {
                return DesktopRaycast(cam, screenPosition, out worldPosition, out rotation);
            }

            return ARRaycast(screenPosition, out worldPosition, out rotation);
        }

        private bool DesktopRaycast(Camera cam, Vector2 screenPosition, out Vector3 worldPosition, out Quaternion rotation)
        {
            worldPosition = Vector3.zero;
            rotation = Quaternion.identity;

            Ray ray = cam.ScreenPointToRay(screenPosition);

            // Try physics raycast first
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, desktopRaycastLayers))
            {
                worldPosition = hit.point;
                rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                return true;
            }

            // Fallback to ground plane
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, desktopPlaneHeight, 0));
            if (groundPlane.Raycast(ray, out float distance))
            {
                worldPosition = ray.GetPoint(distance);
                rotation = Quaternion.identity;
                return true;
            }

            return false;
        }

        private bool ARRaycast(Vector2 screenPosition, out Vector3 worldPosition, out Quaternion rotation)
        {
            worldPosition = Vector3.zero;
            rotation = Quaternion.identity;

            if (_raycastManager == null)
            {
                LogWarning("ARRaycastManager not available");
                return false;
            }

            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            
            if (_raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                ARRaycastHit closestHit = hits[0];
                worldPosition = closestHit.pose.position;
                rotation = closestHit.pose.rotation;
                return true;
            }

            // Try feature points as fallback
            if (_raycastManager.Raycast(screenPosition, hits, TrackableType.FeaturePoint))
            {
                worldPosition = hits[0].pose.position;
                rotation = hits[0].pose.rotation;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Raycast with additional filter options
        /// </summary>
        public bool TryGetHitPosition(Vector2 screenPosition, out Vector3 worldPosition, out Quaternion rotation, 
            PlaneAlignment alignment)
        {
            if (!TryGetHitPosition(screenPosition, out worldPosition, out rotation))
            {
                return false;
            }

            if (!_isDesktopMode && _raycastManager != null)
            {
                List<ARRaycastHit> hits = new List<ARRaycastHit>();
                if (_raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
                {
                    foreach (var hit in hits)
                    {
                        ARPlane plane = _planeManager?.GetPlane(hit.trackableId);
                        if (plane != null && plane.alignment == alignment)
                        {
                            worldPosition = hit.pose.position;
                            rotation = hit.pose.rotation;
                            return true;
                        }
                    }
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Anchor Management

        /// <summary>
        /// Create an anchor at the specified position
        /// </summary>
        public string CreateAnchor(Vector3 position, Quaternion rotation, string anchorId = null)
        {
            if (_anchors.Count >= maxActiveAnchors)
            {
                LogWarning($"Maximum anchor count ({maxActiveAnchors}) reached");
                OnAnchorError?.Invoke(anchorId ?? "", "Max anchors reached");
                return null;
            }

            anchorId = anchorId ?? GenerateAnchorId();

            if (_isDesktopMode)
            {
                return CreateDesktopAnchor(position, rotation, anchorId);
            }

            return CreateARKitCoreAnchor(position, rotation, anchorId);
        }

        private string CreateDesktopAnchor(Vector3 position, Quaternion rotation, string anchorId)
        {
            GameObject anchorGO = new GameObject($"Anchor_{anchorId}");
            anchorGO.transform.position = position;
            anchorGO.transform.rotation = rotation;

            ManagedAnchor managed = new ManagedAnchor
            {
                Id = anchorId,
                Anchor = null,
                Position = position,
                Rotation = rotation,
                CreateTime = Time.time,
                LastUpdateTime = Time.time,
                TrackingState = AnchorTrackingState.Tracking,
                AnchorObject = anchorGO
            };

            _anchors[anchorId] = managed;
            OnAnchorCreated?.Invoke(null, anchorId);
            
            Log($"Created desktop anchor: {anchorId} at {position}");
            return anchorId;
        }

        private string CreateARKitCoreAnchor(Vector3 position, Quaternion rotation, string anchorId)
        {
            if (_anchorManager == null)
            {
                LogWarning("ARAnchorManager not available, creating desktop anchor");
                return CreateDesktopAnchor(position, rotation, anchorId);
            }

            GameObject anchorGO = new GameObject($"Anchor_{anchorId}");
            anchorGO.transform.position = position;
            anchorGO.transform.rotation = rotation;

            ARAnchor anchor = anchorGO.AddComponent<ARAnchor>();

            ManagedAnchor managed = new ManagedAnchor
            {
                Id = anchorId,
                Anchor = anchor,
                Position = position,
                Rotation = rotation,
                CreateTime = Time.time,
                LastUpdateTime = Time.time,
                TrackingState = AnchorTrackingState.Tracking,
                AnchorObject = anchorGO
            };

            _anchors[anchorId] = managed;
            OnAnchorCreated?.Invoke(anchor, anchorId);
            
            Log($"Created AR anchor: {anchorId} at {position}");
            return anchorId;
        }

        /// <summary>
        /// Create an anchor from screen tap position
        /// </summary>
        public string CreateAnchorFromScreenPoint(Vector2 screenPosition, string anchorId = null)
        {
            if (TryGetHitPosition(screenPosition, out Vector3 worldPos, out Quaternion rotation))
            {
                return CreateAnchor(worldPos, rotation, anchorId);
            }

            LogWarning("Could not create anchor - no valid hit position");
            return null;
        }

        /// <summary>
        /// Remove an anchor by ID
        /// </summary>
        public bool RemoveAnchor(string anchorId)
        {
            if (!_anchors.TryGetValue(anchorId, out ManagedAnchor managed))
            {
                return false;
            }

            if (managed.AnchorObject != null)
            {
                Destroy(managed.AnchorObject);
            }

            _anchors.Remove(anchorId);
            OnAnchorRemoved?.Invoke(anchorId);
            
            Log($"Removed anchor: {anchorId}");
            return true;
        }

        /// <summary>
        /// Remove all anchors
        /// </summary>
        public void ClearAllAnchors()
        {
            List<string> idsToRemove = new List<string>(_anchors.Keys);
            foreach (string id in idsToRemove)
            {
                RemoveAnchor(id);
            }
        }

        /// <summary>
        /// Get an anchor by ID
        /// </summary>
        public ManagedAnchor GetAnchor(string anchorId)
        {
            _anchors.TryGetValue(anchorId, out ManagedAnchor anchor);
            return anchor;
        }

        /// <summary>
        /// Update anchor position
        /// </summary>
        public bool UpdateAnchorPosition(string anchorId, Vector3 newPosition, Quaternion newRotation)
        {
            if (!_anchors.TryGetValue(anchorId, out ManagedAnchor managed))
            {
                return false;
            }

            managed.Position = newPosition;
            managed.Rotation = newRotation;
            managed.LastUpdateTime = Time.time;

            if (managed.AnchorObject != null)
            {
                managed.AnchorObject.transform.position = newPosition;
                managed.AnchorObject.transform.rotation = newRotation;
            }

            return true;
        }

        #endregion

        #region Anchor Persistence

        /// <summary>
        /// Save anchor to persistent storage
        /// </summary>
        public void SaveAnchor(string anchorId)
        {
            if (!_anchors.TryGetValue(anchorId, out ManagedAnchor managed))
            {
                LogWarning($"Cannot save anchor - not found: {anchorId}");
                return;
            }

            managed.IsPersisted = true;
            SaveAnchorsToPrefs();
            
            Log($"Saved anchor: {anchorId}");
        }

        /// <summary>
        /// Save all anchors
        /// </summary>
        public void SaveAllAnchors()
        {
            foreach (var kvp in _anchors)
            {
                kvp.Value.IsPersisted = true;
            }
            SaveAnchorsToPrefs();
        }

        private void SaveAnchorsToPrefs()
        {
            List<SerializedAnchor> toSave = new List<SerializedAnchor>();

            foreach (var kvp in _anchors)
            {
                if (kvp.Value.IsPersisted)
                {
                    toSave.Add(new SerializedAnchor
                    {
                        id = kvp.Value.Id,
                        posX = kvp.Value.Position.x,
                        posY = kvp.Value.Position.y,
                        posZ = kvp.Value.Position.z,
                        rotX = kvp.Value.Rotation.x,
                        rotY = kvp.Value.Rotation.y,
                        rotZ = kvp.Value.Rotation.z,
                        rotW = kvp.Value.Rotation.w,
                        metadata = kvp.Value.Metadata
                    });
                }
            }

            string json = JsonUtility.ToJson(new SerializedAnchorList { anchors = toSave.ToArray() });
            PlayerPrefs.SetString(PREF_SAVED_ANCHORS, json);
            PlayerPrefs.Save();
        }

        private void RestoreSavedAnchors()
        {
            if (!PlayerPrefs.HasKey(PREF_SAVED_ANCHORS)) return;

            string json = PlayerPrefs.GetString(PREF_SAVED_ANCHORS);
            
            try
            {
                SerializedAnchorList list = JsonUtility.FromJson<SerializedAnchorList>(json);
                
                if (list?.anchors != null)
                {
                    foreach (var saved in list.anchors)
                    {
                        Vector3 pos = new Vector3(saved.posX, saved.posY, saved.posZ);
                        Quaternion rot = new Quaternion(saved.rotX, saved.rotY, saved.rotZ, saved.rotW);
                        
                        string id = CreateAnchor(pos, rot, saved.id);
                        
                        if (id != null && _anchors.TryGetValue(id, out ManagedAnchor managed))
                        {
                            managed.IsPersisted = true;
                            managed.Metadata = saved.metadata;
                            OnAnchorRestored?.Invoke(id);
                        }
                    }
                    
                    Log($"Restored {list.anchors.Length} saved anchors");
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to restore saved anchors: {e.Message}");
            }
        }

        private IEnumerator AnchorPersistenceCheck()
        {
            WaitForSeconds wait = new WaitForSeconds(anchorPersistenceCheckInterval);

            while (true)
            {
                yield return wait;

                // Check anchor tracking state
                List<string> lostAnchors = new List<string>();

                foreach (var kvp in _anchors)
                {
                    if (kvp.Value.Anchor != null)
                    {
                        TrackingState state = kvp.Value.Anchor.trackingState;
                        
                        if (state == TrackingState.None)
                        {
                            kvp.Value.TrackingState = AnchorTrackingState.Lost;
                            lostAnchors.Add(kvp.Key);
                        }
                        else if (state == TrackingState.Limited)
                        {
                            kvp.Value.TrackingState = AnchorTrackingState.Limited;
                        }
                        else
                        {
                            kvp.Value.TrackingState = AnchorTrackingState.Tracking;
                        }
                    }
                }

                if (lostAnchors.Count > 0)
                {
                    Log($"Lost tracking on {lostAnchors.Count} anchors");
                }
            }
        }

        #endregion

        #region Geospatial (Stub)

        // Mock location for testing
        private double _mockLatitude = 37.7749;
        private double _mockLongitude = -122.4194;
        private double _mockAltitude = 0;
        private bool _useMockLocation = true;

        /// <summary>
        /// Set mock location for testing (debug/desktop mode)
        /// </summary>
        public void SetMockLocation(double latitude, double longitude, double altitude)
        {
            _mockLatitude = latitude;
            _mockLongitude = longitude;
            _mockAltitude = altitude;
            _useMockLocation = true;
            Log($"Mock location set: ({latitude:F4}, {longitude:F4}, {altitude:F1})");
        }

        /// <summary>
        /// Get current geospatial pose (stub - returns mock data in desktop mode)
        /// </summary>
        public void GetCurrentGeospatialPose(out double latitude, out double longitude, out double altitude)
        {
            if (_useMockLocation || IsDesktopMode)
            {
                latitude = _mockLatitude;
                longitude = _mockLongitude;
                altitude = _mockAltitude;
                return;
            }

            // TODO: Real ARCore Geospatial API integration
            latitude = _mockLatitude;
            longitude = _mockLongitude;
            altitude = _mockAltitude;
            
            Log("STUB: Using mock GPS coordinates - import ARCore Extensions for real Geospatial");
        }

        /// <summary>
        /// Create a geospatial anchor (stub)
        /// </summary>
        public string CreateGeospatialAnchor(double latitude, double longitude, double altitude, 
            Quaternion rotation, string anchorId = null)
        {
            LogWarning("Geospatial anchors require ARCore Extensions - creating local anchor instead");
            
            // Create local anchor at origin as placeholder
            return CreateAnchor(Vector3.zero, rotation, anchorId);
        }

        #endregion

        #region Pending Requests

        private void ProcessPendingRequests()
        {
            while (_pendingRequests.Count > 0)
            {
                AnchorRequest request = _pendingRequests.Dequeue();
                CreateAnchor(request.Position, request.Rotation, request.Id);
            }
        }

        /// <summary>
        /// Queue an anchor creation request for when tracking is ready
        /// </summary>
        public void QueueAnchorRequest(Vector3 position, Quaternion rotation, string anchorId = null)
        {
            if (IsTracking)
            {
                CreateAnchor(position, rotation, anchorId);
            }
            else
            {
                _pendingRequests.Enqueue(new AnchorRequest
                {
                    Position = position,
                    Rotation = rotation,
                    Id = anchorId
                });
            }
        }

        #endregion

        #region Utilities

        private Camera GetActiveCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = FindFirstObjectByType<Camera>();
            }
            return cam;
        }

        private string GenerateAnchorId()
        {
            return $"anchor_{DateTime.UtcNow.Ticks}_{UnityEngine.Random.Range(1000, 9999)}";
        }

        private void Log(string message)
        {
            if (logAnchorEvents)
            {
                Debug.Log($"[SpatialAnchorManager] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SpatialAnchorManager] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SpatialAnchorManager] {message}");
        }

        /// <summary>
        /// Legacy compatibility method
        /// </summary>
        public ARAnchor CreateAnchorAtPosition(Vector3 position, Quaternion rotation)
        {
            string id = CreateAnchor(position, rotation);
            if (id != null && _anchors.TryGetValue(id, out ManagedAnchor managed))
            {
                return managed.Anchor;
            }
            return null;
        }

        #endregion
    }

    #region Support Classes

    /// <summary>
    /// Managed anchor with tracking state and metadata
    /// </summary>
    public class ManagedAnchor
    {
        public string Id { get; set; }
        public ARAnchor Anchor { get; set; }
        public GameObject AnchorObject { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public float CreateTime { get; set; }
        public float LastUpdateTime { get; set; }
        public AnchorTrackingState TrackingState { get; set; }
        public bool IsPersisted { get; set; }
        public string Metadata { get; set; }
    }

    public enum AnchorTrackingState
    {
        Tracking,
        Limited,
        Lost
    }

    /// <summary>
    /// Pending anchor creation request
    /// </summary>
    internal struct AnchorRequest
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public string Id;
    }

    /// <summary>
    /// Serialized anchor for persistence
    /// </summary>
    [Serializable]
    internal class SerializedAnchor
    {
        public string id;
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;
        public string metadata;
    }

    [Serializable]
    internal class SerializedAnchorList
    {
        public SerializedAnchor[] anchors;
    }

    #endregion
}
