// ===========================================
// SHARED AR COMPONENT
// Copy this file to each Unity project's Assets/Scripts/AR/ folder
// ===========================================

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

namespace Shared.AR
{
    /// <summary>
    /// Shared Spatial Anchor Manager for AR games.
    /// Handles AR raycasting, anchor creation, and geospatial positioning.
    /// 
    /// Usage: Copy to your Unity project and update namespace as needed.
    /// </summary>
    public class SharedSpatialAnchorManager : MonoBehaviour
    {
        public static SharedSpatialAnchorManager Instance { get; private set; }

        [Header("AR Components")]
        [SerializeField] private ARAnchorManager _anchorManager;
        [SerializeField] private ARRaycastManager _raycastManager;

        private List<ARRaycastHit> _raycastHits = new List<ARRaycastHit>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Try to get a hit position from screen tap
        /// </summary>
        public bool TryGetHitPosition(Vector2 screenPosition, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (_raycastManager == null) return false;

            if (_raycastManager.Raycast(screenPosition, _raycastHits, TrackableType.PlaneWithinPolygon))
            {
                var hit = _raycastHits[0];
                position = hit.pose.position;
                rotation = hit.pose.rotation;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Create an AR anchor at the specified pose
        /// </summary>
        public ARAnchor CreateAnchor(Pose pose)
        {
            if (_anchorManager == null) return null;
            
            var anchorGO = new GameObject("Anchor");
            anchorGO.transform.SetPositionAndRotation(pose.position, pose.rotation);
            return anchorGO.AddComponent<ARAnchor>();
        }
    }
}
