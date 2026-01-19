// ============================================================================
// APEX CITADELS - GEOSPATIAL MANAGER
// Handles GPS-based AR positioning using ARCore Geospatial API
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ApexCitadels.Core;
using ApexCitadels.Territory;

#if ARCORE_EXTENSIONS_ENABLED
using Google.XR.ARCoreExtensions;
#endif

namespace ApexCitadels.AR
{
    /// <summary>
    /// Manages Geospatial positioning for GPS-anchored AR content.
    /// Enables players to claim real-world territories and place structures
    /// that persist at exact GPS coordinates.
    /// </summary>
    public class GeospatialManager : MonoBehaviour
    {
        public static GeospatialManager Instance { get; private set; }

        [Header("AR Components")]
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private ARRaycastManager raycastManager;
        
        [Header("Geospatial Settings")]
        [SerializeField] private float horizontalAccuracyThreshold = 10f; // meters
        [SerializeField] private float verticalAccuracyThreshold = 10f; // meters
        [SerializeField] private float headingAccuracyThreshold = 15f; // degrees
        [SerializeField] private float initializationTimeout = 60f;
        
        [Header("Territory Settings")]
        [SerializeField] private float territoryRadius = 50f; // meters
        [SerializeField] private float minDistanceToClaimTerritory = 25f; // must be within 25m of center
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private TMPro.TextMeshProUGUI debugText;
        
        // State
        private bool isGeospatialSupported = false;
        private bool isGeospatialReady = false;
        private bool isLocalizing = false;
        private float localizationStartTime;
        
        // Current position
        private GeospatialPosition currentPosition;
        private List<GeospatialAnchorData> placedAnchors = new List<GeospatialAnchorData>();
        
        // Events
        public event Action OnGeospatialReady;
        public event Action OnGeospatialLost;
        public event Action<GeospatialPosition> OnPositionUpdated;
        public event Action<string> OnError;
        
        // Properties
        public bool IsReady => isGeospatialReady;
        public bool IsLocalizing => isLocalizing;
        public GeospatialPosition CurrentPosition => currentPosition;
        public double Latitude => currentPosition.Latitude;
        public double Longitude => currentPosition.Longitude;
        public double Altitude => currentPosition.Altitude;
        public float HorizontalAccuracy => currentPosition.HorizontalAccuracy;

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            StartCoroutine(InitializeGeospatial());
        }

        private void Update()
        {
            if (!isGeospatialReady) return;
            
            UpdateGeospatialTracking();
            
            if (showDebugInfo)
            {
                UpdateDebugDisplay();
            }
        }

        #endregion

        #region Initialization

        private IEnumerator InitializeGeospatial()
        {
            ApexLogger.Log("[GeospatialManager] Initializing Geospatial API...", ApexLogger.LogCategory.AR);
            
            // Check if ARCore Extensions are available
            #if !ARCORE_EXTENSIONS_ENABLED
            ApexLogger.LogWarning("[GeospatialManager] ARCore Extensions not enabled. Running in simulation mode.", ApexLogger.LogCategory.AR);
            isGeospatialSupported = false;
            StartSimulationMode();
            yield break;
            #else
            
            // Wait for AR session to be ready
            yield return new WaitUntil(() => ARSession.state == ARSessionState.SessionTracking);
            
            // Check Geospatial availability
            var availability = AREarthManager.CheckVpsAvailabilityAsync(0, 0);
            yield return availability;
            
            if (availability.Result == VpsAvailability.Available)
            {
                isGeospatialSupported = true;
                ApexLogger.Log("[GeospatialManager] Geospatial API supported!", ApexLogger.LogCategory.AR);
                StartLocalization();
            }
            else
            {
                ApexLogger.LogWarning($"[GeospatialManager] Geospatial not available: {availability.Result}", ApexLogger.LogCategory.AR);
                isGeospatialSupported = false;
                OnError?.Invoke("Geospatial API not available in this location");
            }
            #endif
        }

        private void StartLocalization()
        {
            isLocalizing = true;
            localizationStartTime = Time.time;
            ApexLogger.Log("[GeospatialManager] Starting localization...", ApexLogger.LogCategory.AR);
        }

        private void StartSimulationMode()
        {
            ApexLogger.Log("[GeospatialManager] Running in GPS simulation mode", ApexLogger.LogCategory.AR);
            
            // Use mock GPS data for testing
            currentPosition = new GeospatialPosition
            {
                Latitude = MockGPSProvider.Instance?.Latitude ?? 37.7749,
                Longitude = MockGPSProvider.Instance?.Longitude ?? -122.4194,
                Altitude = MockGPSProvider.Instance?.Altitude ?? 10.0,
                HorizontalAccuracy = 5f,
                VerticalAccuracy = 3f,
                HeadingAccuracy = 5f
            };
            
            isGeospatialReady = true;
            OnGeospatialReady?.Invoke();
        }

        #endregion

        #region Tracking

        private void UpdateGeospatialTracking()
        {
            #if ARCORE_EXTENSIONS_ENABLED
            var earthTrackingState = AREarthManager.EarthTrackingState;
            
            if (earthTrackingState == TrackingState.Tracking)
            {
                var pose = AREarthManager.CameraGeospatialPose;
                
                currentPosition = new GeospatialPosition
                {
                    Latitude = pose.Latitude,
                    Longitude = pose.Longitude,
                    Altitude = pose.Altitude,
                    HorizontalAccuracy = (float)pose.HorizontalAccuracy,
                    VerticalAccuracy = (float)pose.VerticalAccuracy,
                    HeadingAccuracy = (float)pose.OrientationYawAccuracy,
                    Heading = (float)pose.Heading
                };
                
                // Check if accuracy is good enough
                if (!isGeospatialReady && IsAccuracyAcceptable())
                {
                    isGeospatialReady = true;
                    isLocalizing = false;
                    ApexLogger.Log("[GeospatialManager] Geospatial localization complete!", ApexLogger.LogCategory.AR);
                    OnGeospatialReady?.Invoke();
                }
                
                OnPositionUpdated?.Invoke(currentPosition);
            }
            else if (isGeospatialReady)
            {
                // Lost tracking
                isGeospatialReady = false;
                ApexLogger.LogWarning("[GeospatialManager] Geospatial tracking lost", ApexLogger.LogCategory.AR);
                OnGeospatialLost?.Invoke();
            }
            
            // Check for timeout
            if (isLocalizing && Time.time - localizationStartTime > initializationTimeout)
            {
                isLocalizing = false;
                OnError?.Invoke("Geospatial localization timed out. Move to a location with better GPS/visual features.");
            }
            #else
            // Simulation mode - use mock GPS
            if (MockGPSProvider.Instance != null)
            {
                currentPosition = new GeospatialPosition
                {
                    Latitude = MockGPSProvider.Instance.Latitude,
                    Longitude = MockGPSProvider.Instance.Longitude,
                    Altitude = MockGPSProvider.Instance.Altitude,
                    HorizontalAccuracy = 5f,
                    VerticalAccuracy = 3f,
                    HeadingAccuracy = 5f,
                    Heading = MockGPSProvider.Instance.Heading
                };
                OnPositionUpdated?.Invoke(currentPosition);
            }
            #endif
        }

        private bool IsAccuracyAcceptable()
        {
            return currentPosition.HorizontalAccuracy <= horizontalAccuracyThreshold &&
                   currentPosition.VerticalAccuracy <= verticalAccuracyThreshold &&
                   currentPosition.HeadingAccuracy <= headingAccuracyThreshold;
        }

        #endregion

        #region Territory Operations

        /// <summary>
        /// Check if player is close enough to claim a territory at current location
        /// </summary>
        public bool CanClaimTerritoryHere()
        {
            if (!isGeospatialReady) return false;
            
            // Check if there's already a territory here
            var existing = TerritoryManager.Instance?.FindOverlappingTerritory(
                currentPosition.Latitude, 
                currentPosition.Longitude, 
                territoryRadius
            );
            
            return existing == null;
        }

        /// <summary>
        /// Claim territory at current GPS position
        /// </summary>
        public async void ClaimTerritoryAtCurrentLocation()
        {
            if (!CanClaimTerritoryHere())
            {
                OnError?.Invoke("Cannot claim territory here!");
                return;
            }
            
            ApexLogger.Log($"[GeospatialManager] Claiming territory at {currentPosition.Latitude}, {currentPosition.Longitude}", ApexLogger.LogCategory.Territory);
            
            var result = await TerritoryManager.Instance.TryClaimTerritory(
                currentPosition.Latitude, 
                currentPosition.Longitude
            );
            
            if (result.Success)
            {
                ApexLogger.Log("[GeospatialManager] Territory claimed successfully!", ApexLogger.LogCategory.Territory);
                // Place visual anchor
                PlaceGeospatialAnchor(currentPosition.Latitude, currentPosition.Longitude, currentPosition.Altitude, "territory_marker");
            }
            else
            {
                OnError?.Invoke(result.Message);
            }
        }

        /// <summary>
        /// Check distance to a GPS coordinate
        /// </summary>
        public float GetDistanceTo(double latitude, double longitude)
        {
            return Territory.Territory.CalculateDistance(
                currentPosition.Latitude, currentPosition.Longitude,
                latitude, longitude
            );
        }

        /// <summary>
        /// Check if player is within range of their territory
        /// </summary>
        public bool IsWithinTerritory(Territory.Territory territory)
        {
            if (territory == null || !isGeospatialReady) return false;
            
            float distance = GetDistanceTo(territory.CenterLatitude, territory.CenterLongitude);
            return distance <= territory.RadiusMeters;
        }

        #endregion

        #region Anchor Placement

        /// <summary>
        /// Place a geospatial anchor at GPS coordinates
        /// </summary>
        public void PlaceGeospatialAnchor(double latitude, double longitude, double altitude, string anchorId)
        {
            #if ARCORE_EXTENSIONS_ENABLED
            var pose = new GeospatialPose
            {
                Latitude = latitude,
                Longitude = longitude,
                Altitude = altitude,
                EunRotation = Quaternion.identity
            };
            
            var anchor = ARAnchorManagerExtensions.AddAnchor(
                anchorManager, 
                latitude, 
                longitude, 
                altitude, 
                Quaternion.identity
            );
            
            if (anchor != null)
            {
                placedAnchors.Add(new GeospatialAnchorData
                {
                    Id = anchorId,
                    Latitude = latitude,
                    Longitude = longitude,
                    Altitude = altitude,
                    Anchor = anchor.gameObject
                });
                
                ApexLogger.Log($"[GeospatialManager] Placed anchor at {latitude}, {longitude}", ApexLogger.LogCategory.AR);
            }
            #else
            ApexLogger.Log($"[GeospatialManager] (Simulation, ApexLogger.LogCategory.AR) Would place anchor at {latitude}, {longitude}");
            #endif
        }

        /// <summary>
        /// Remove a geospatial anchor
        /// </summary>
        public void RemoveAnchor(string anchorId)
        {
            var anchor = placedAnchors.Find(a => a.Id == anchorId);
            if (anchor != null)
            {
                if (anchor.Anchor != null)
                {
                    Destroy(anchor.Anchor);
                }
                placedAnchors.Remove(anchor);
            }
        }

        #endregion

        #region Debug

        private void UpdateDebugDisplay()
        {
            if (debugText == null) return;
            
            string status = isGeospatialReady ? "<color=green>READY</color>" : 
                           isLocalizing ? "<color=yellow>LOCALIZING...</color>" : 
                           "<color=red>NOT READY</color>";
            
            debugText.text = $"Geospatial: {status}\n" +
                            $"Lat: {currentPosition.Latitude:F6}\n" +
                            $"Lng: {currentPosition.Longitude:F6}\n" +
                            $"Alt: {currentPosition.Altitude:F1}m\n" +
                            $"H.Acc: {currentPosition.HorizontalAccuracy:F1}m\n" +
                            $"V.Acc: {currentPosition.VerticalAccuracy:F1}m\n" +
                            $"Heading: {currentPosition.Heading:F1}°";
        }

        #endregion
    }

    /// <summary>
    /// GPS position data
    /// </summary>
    [Serializable]
    public struct GeospatialPosition
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;
        public float HorizontalAccuracy;
        public float VerticalAccuracy;
        public float HeadingAccuracy;
        public float Heading;
    }

    /// <summary>
    /// Placed anchor tracking data
    /// </summary>
    [Serializable]
    public class GeospatialAnchorData
    {
        public string Id;
        public double Latitude;
        public double Longitude;
        public double Altitude;
        public GameObject Anchor;
    }
}
