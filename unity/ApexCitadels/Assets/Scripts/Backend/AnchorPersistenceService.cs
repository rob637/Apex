using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ApexCitadels.Backend
{
    /// <summary>
    /// Stub implementation of AnchorPersistenceService.
    /// Import Firebase SDK to enable full cloud functionality.
    /// </summary>
    public class AnchorPersistenceService : MonoBehaviour
    {
        public static AnchorPersistenceService Instance { get; private set; }
        
        public bool IsInitialized { get; private set; }
        public event Action OnInitialized;
        public event Action<string> OnError;

        private List<AnchorData> _localAnchors = new List<AnchorData>();

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

        private void Start()
        {
            Debug.Log("[AnchorPersistenceService] STUB MODE - Import Firebase SDK for cloud features");
            IsInitialized = true;
            OnInitialized?.Invoke();
        }

        [System.Serializable]
        public class AnchorData
        {
            public string Id;
            public double Latitude;
            public double Longitude;
            public double Altitude;
            public Quaternion Rotation;
            public string OwnerId;
            public DateTime CreatedAt;
        }

        public Task<string> SaveAnchorAsync(double lat, double lon, double alt, Quaternion rot, string ownerId)
        {
            var anchor = new AnchorData
            {
                Id = Guid.NewGuid().ToString(),
                Latitude = lat,
                Longitude = lon,
                Altitude = alt,
                Rotation = rot,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow
            };
            _localAnchors.Add(anchor);
            Debug.Log($"[STUB] Saved anchor locally: {anchor.Id}");
            return Task.FromResult(anchor.Id);
        }

        public Task<List<AnchorData>> LoadAnchorsNearbyAsync(double lat, double lon, double radiusMeters)
        {
            Debug.Log($"[STUB] Loading anchors near ({lat}, {lon})");
            return Task.FromResult(_localAnchors);
        }

        public Task<bool> DeleteAnchorAsync(string anchorId)
        {
            _localAnchors.RemoveAll(a => a.Id == anchorId);
            return Task.FromResult(true);
        }
    }
}
