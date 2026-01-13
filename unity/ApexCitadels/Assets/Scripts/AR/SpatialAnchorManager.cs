using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ApexCitadels.AR
{
    /// <summary>
    /// Stub implementation of SpatialAnchorManager.
    /// Import ARCore Extensions for full Geospatial functionality.
    /// </summary>
    public class SpatialAnchorManager : MonoBehaviour
    {
        public static SpatialAnchorManager Instance { get; private set; }

        [Header("AR Components")]
        [SerializeField] private ARAnchorManager _anchorManager;
        [SerializeField] private ARRaycastManager _raycastManager;

        public bool IsTracking { get; private set; }
        public event Action OnTrackingReady;
        public event Action<ARAnchor> OnAnchorCreated;

        private List<ARAnchor> _activeAnchors = new List<ARAnchor>();

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
            Debug.Log("[SpatialAnchorManager] STUB MODE - Import ARCore Extensions for Geospatial features");
            
            // Simulate tracking ready after a delay
            Invoke(nameof(SimulateTrackingReady), 2f);
        }

        private void SimulateTrackingReady()
        {
            IsTracking = true;
            OnTrackingReady?.Invoke();
            Debug.Log("[SpatialAnchorManager] Tracking ready (simulated)");
        }

        public bool TryGetHitPosition(Vector2 screenPosition, out Vector3 worldPosition, out Quaternion rotation)
        {
            worldPosition = Vector3.zero;
            rotation = Quaternion.identity;

            if (_raycastManager == null)
            {
                // Fallback: project onto a plane at y=0
                var ray = Camera.main.ScreenPointToRay(screenPosition);
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(ray, out float distance))
                {
                    worldPosition = ray.GetPoint(distance);
                    rotation = Quaternion.identity;
                    return true;
                }
                return false;
            }

            var hits = new List<ARRaycastHit>();
            if (_raycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.Planes))
            {
                worldPosition = hits[0].pose.position;
                rotation = hits[0].pose.rotation;
                return true;
            }

            return false;
        }

        public ARAnchor CreateAnchorAtPosition(Vector3 position, Quaternion rotation)
        {
            if (_anchorManager == null)
            {
                Debug.LogWarning("[SpatialAnchorManager] No ARAnchorManager - creating dummy anchor");
                return null;
            }

            var anchorGO = new GameObject("Anchor");
            anchorGO.transform.position = position;
            anchorGO.transform.rotation = rotation;
            
            var anchor = anchorGO.AddComponent<ARAnchor>();
            _activeAnchors.Add(anchor);
            
            OnAnchorCreated?.Invoke(anchor);
            Debug.Log($"[SpatialAnchorManager] Created anchor at {position}");
            
            return anchor;
        }

        public void GetCurrentGeospatialPose(out double latitude, out double longitude, out double altitude)
        {
            // Stub: Return dummy coordinates (San Francisco)
            latitude = 37.7749;
            longitude = -122.4194;
            altitude = 0;
            Debug.Log("[SpatialAnchorManager] STUB: Returning dummy GPS coordinates");
        }

        public void RemoveAnchor(ARAnchor anchor)
        {
            if (anchor != null)
            {
                _activeAnchors.Remove(anchor);
                Destroy(anchor.gameObject);
            }
        }
    }
}
