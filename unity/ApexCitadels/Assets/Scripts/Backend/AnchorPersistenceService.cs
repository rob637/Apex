using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;

namespace ApexCitadels.Backend
{
    /// <summary>
    /// Firebase service for persisting and retrieving spatial anchors.
    /// This enables the core "persistence" feature - anchors created by one user
    /// can be loaded and displayed to all other users.
    /// </summary>
    public class AnchorPersistenceService : MonoBehaviour
    {
        #region Singleton
        public static AnchorPersistenceService Instance { get; private set; }

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

        #region Private Fields
        private FirebaseFirestore _db;
        private bool _isInitialized = false;
        private const string ANCHORS_COLLECTION = "spatial_anchors";
        #endregion

        #region Events
        public event Action OnInitialized;
        public event Action<string> OnError;
        #endregion

        #region Initialization
        private async void Start()
        {
            await InitializeFirebase();
        }

        private async Task InitializeFirebase()
        {
            try
            {
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
                
                if (dependencyStatus == DependencyStatus.Available)
                {
                    _db = FirebaseFirestore.DefaultInstance;
                    _isInitialized = true;
                    Debug.Log("[AnchorPersistenceService] Firebase initialized successfully");
                    OnInitialized?.Invoke();
                }
                else
                {
                    Debug.LogError($"[AnchorPersistenceService] Firebase dependency error: {dependencyStatus}");
                    OnError?.Invoke($"Firebase initialization failed: {dependencyStatus}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnchorPersistenceService] Firebase initialization error: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }
        #endregion

        #region Save Operations
        /// <summary>
        /// Saves a spatial anchor to Firestore.
        /// The anchor can then be retrieved by any other user.
        /// </summary>
        public async Task<bool> SaveAnchor(AR.SpatialAnchorData anchorData)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AnchorPersistenceService] Firebase not initialized");
                return false;
            }

            try
            {
                var docRef = _db.Collection(ANCHORS_COLLECTION).Document(anchorData.Id);
                
                var data = new Dictionary<string, object>
                {
                    { "id", anchorData.Id },
                    { "latitude", anchorData.Latitude },
                    { "longitude", anchorData.Longitude },
                    { "altitude", anchorData.Altitude },
                    { "rotationX", anchorData.RotationX },
                    { "rotationY", anchorData.RotationY },
                    { "rotationZ", anchorData.RotationZ },
                    { "rotationW", anchorData.RotationW },
                    { "createdAt", Timestamp.FromDateTime(anchorData.CreatedAt) },
                    { "anchorType", anchorData.AnchorType.ToString() },
                    { "ownerId", anchorData.OwnerId ?? "" },
                    { "attachedObjectType", anchorData.AttachedObjectType ?? "" },
                    { "attachedObjectId", anchorData.AttachedObjectId ?? "" },
                    { "geoHash", GeoHashEncoder.Encode(anchorData.Latitude, anchorData.Longitude, 8) }
                };

                await docRef.SetAsync(data);
                Debug.Log($"[AnchorPersistenceService] Anchor saved: {anchorData.Id}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnchorPersistenceService] Error saving anchor: {ex.Message}");
                OnError?.Invoke(ex.Message);
                return false;
            }
        }
        #endregion

        #region Query Operations
        /// <summary>
        /// Retrieves a single anchor by ID.
        /// </summary>
        public async Task<AR.SpatialAnchorData> GetAnchor(string anchorId)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AnchorPersistenceService] Firebase not initialized");
                return null;
            }

            try
            {
                var docRef = _db.Collection(ANCHORS_COLLECTION).Document(anchorId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    Debug.LogWarning($"[AnchorPersistenceService] Anchor not found: {anchorId}");
                    return null;
                }

                return DocumentToAnchorData(snapshot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnchorPersistenceService] Error getting anchor: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves all anchors within a radius of a given location.
        /// Uses GeoHash for efficient spatial queries.
        /// </summary>
        public async Task<List<AR.SpatialAnchorData>> GetAnchorsInRadius(
            double latitude, 
            double longitude, 
            double radiusMeters)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AnchorPersistenceService] Firebase not initialized");
                return new List<AR.SpatialAnchorData>();
            }

            try
            {
                // Calculate GeoHash bounds for the search area
                var bounds = GeoHashEncoder.GetBoundsForRadius(latitude, longitude, radiusMeters);
                var anchors = new List<AR.SpatialAnchorData>();

                // Query each GeoHash prefix that might contain results
                foreach (var geoHashPrefix in bounds.GeoHashPrefixes)
                {
                    var query = _db.Collection(ANCHORS_COLLECTION)
                        .WhereGreaterThanOrEqualTo("geoHash", geoHashPrefix)
                        .WhereLessThan("geoHash", geoHashPrefix + "~")
                        .Limit(100);

                    var snapshot = await query.GetSnapshotAsync();

                    foreach (var doc in snapshot.Documents)
                    {
                        var anchorData = DocumentToAnchorData(doc);
                        
                        // Post-filter by actual distance (GeoHash is approximate)
                        var distance = HaversineDistance(
                            latitude, longitude,
                            anchorData.Latitude, anchorData.Longitude
                        );

                        if (distance <= radiusMeters)
                        {
                            anchors.Add(anchorData);
                        }
                    }
                }

                Debug.Log($"[AnchorPersistenceService] Found {anchors.Count} anchors in radius");
                return anchors;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnchorPersistenceService] Error querying anchors: {ex.Message}");
                return new List<AR.SpatialAnchorData>();
            }
        }

        /// <summary>
        /// Gets all anchors owned by a specific user.
        /// </summary>
        public async Task<List<AR.SpatialAnchorData>> GetAnchorsByOwner(string ownerId)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AnchorPersistenceService] Firebase not initialized");
                return new List<AR.SpatialAnchorData>();
            }

            try
            {
                var query = _db.Collection(ANCHORS_COLLECTION)
                    .WhereEqualTo("ownerId", ownerId)
                    .Limit(100);

                var snapshot = await query.GetSnapshotAsync();
                var anchors = new List<AR.SpatialAnchorData>();

                foreach (var doc in snapshot.Documents)
                {
                    anchors.Add(DocumentToAnchorData(doc));
                }

                return anchors;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnchorPersistenceService] Error getting user anchors: {ex.Message}");
                return new List<AR.SpatialAnchorData>();
            }
        }
        #endregion

        #region Delete Operations
        /// <summary>
        /// Deletes an anchor from the database.
        /// </summary>
        public async Task<bool> DeleteAnchor(string anchorId)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AnchorPersistenceService] Firebase not initialized");
                return false;
            }

            try
            {
                var docRef = _db.Collection(ANCHORS_COLLECTION).Document(anchorId);
                await docRef.DeleteAsync();
                Debug.Log($"[AnchorPersistenceService] Anchor deleted: {anchorId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnchorPersistenceService] Error deleting anchor: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Real-Time Listeners
        private ListenerRegistration _areaListener;

        /// <summary>
        /// Subscribes to real-time updates for anchors in a given area.
        /// This allows the app to immediately show new Citadels built by other players.
        /// </summary>
        public void SubscribeToAreaUpdates(
            double latitude,
            double longitude,
            double radiusMeters,
            Action<List<AR.SpatialAnchorData>> onUpdate)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AnchorPersistenceService] Firebase not initialized");
                return;
            }

            // Unsubscribe from previous listener
            UnsubscribeFromAreaUpdates();

            var bounds = GeoHashEncoder.GetBoundsForRadius(latitude, longitude, radiusMeters);
            var primaryPrefix = bounds.GeoHashPrefixes[0]; // Use primary geohash for listener

            var query = _db.Collection(ANCHORS_COLLECTION)
                .WhereGreaterThanOrEqualTo("geoHash", primaryPrefix)
                .WhereLessThan("geoHash", primaryPrefix + "~");

            _areaListener = query.Listen(snapshot =>
            {
                var anchors = new List<AR.SpatialAnchorData>();
                
                foreach (var doc in snapshot.Documents)
                {
                    var anchorData = DocumentToAnchorData(doc);
                    var distance = HaversineDistance(
                        latitude, longitude,
                        anchorData.Latitude, anchorData.Longitude
                    );

                    if (distance <= radiusMeters)
                    {
                        anchors.Add(anchorData);
                    }
                }

                onUpdate?.Invoke(anchors);
            });

            Debug.Log("[AnchorPersistenceService] Subscribed to area updates");
        }

        /// <summary>
        /// Unsubscribes from real-time area updates.
        /// </summary>
        public void UnsubscribeFromAreaUpdates()
        {
            _areaListener?.Stop();
            _areaListener = null;
        }
        #endregion

        #region Helpers
        private AR.SpatialAnchorData DocumentToAnchorData(DocumentSnapshot doc)
        {
            var data = doc.ToDictionary();
            
            return new AR.SpatialAnchorData
            {
                Id = data.GetValueOrDefault("id", "")?.ToString() ?? "",
                Latitude = Convert.ToDouble(data.GetValueOrDefault("latitude", 0.0)),
                Longitude = Convert.ToDouble(data.GetValueOrDefault("longitude", 0.0)),
                Altitude = Convert.ToDouble(data.GetValueOrDefault("altitude", 0.0)),
                RotationX = Convert.ToSingle(data.GetValueOrDefault("rotationX", 0f)),
                RotationY = Convert.ToSingle(data.GetValueOrDefault("rotationY", 0f)),
                RotationZ = Convert.ToSingle(data.GetValueOrDefault("rotationZ", 0f)),
                RotationW = Convert.ToSingle(data.GetValueOrDefault("rotationW", 1f)),
                CreatedAt = ((Timestamp)data.GetValueOrDefault("createdAt", Timestamp.GetCurrentTimestamp())).ToDateTime(),
                AnchorType = Enum.TryParse<AR.AnchorType>(
                    data.GetValueOrDefault("anchorType", "Geospatial")?.ToString(),
                    out var anchorType) ? anchorType : AR.AnchorType.Geospatial,
                OwnerId = data.GetValueOrDefault("ownerId", "")?.ToString(),
                AttachedObjectType = data.GetValueOrDefault("attachedObjectType", "")?.ToString(),
                AttachedObjectId = data.GetValueOrDefault("attachedObjectId", "")?.ToString()
            };
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

        private void OnDestroy()
        {
            UnsubscribeFromAreaUpdates();
        }
    }

    #region GeoHash Utility
    /// <summary>
    /// GeoHash encoder for efficient spatial queries.
    /// GeoHash converts lat/lng into a string where nearby locations share prefixes.
    /// </summary>
    public static class GeoHashEncoder
    {
        private const string BASE32 = "0123456789bcdefghjkmnpqrstuvwxyz";

        public static string Encode(double latitude, double longitude, int precision = 8)
        {
            double[] latRange = { -90.0, 90.0 };
            double[] lonRange = { -180.0, 180.0 };
            
            var hash = new System.Text.StringBuilder();
            var isEven = true;
            var bit = 0;
            var ch = 0;

            while (hash.Length < precision)
            {
                double mid;
                
                if (isEven)
                {
                    mid = (lonRange[0] + lonRange[1]) / 2;
                    if (longitude > mid)
                    {
                        ch |= 1 << (4 - bit);
                        lonRange[0] = mid;
                    }
                    else
                    {
                        lonRange[1] = mid;
                    }
                }
                else
                {
                    mid = (latRange[0] + latRange[1]) / 2;
                    if (latitude > mid)
                    {
                        ch |= 1 << (4 - bit);
                        latRange[0] = mid;
                    }
                    else
                    {
                        latRange[1] = mid;
                    }
                }

                isEven = !isEven;
                
                if (bit < 4)
                {
                    bit++;
                }
                else
                {
                    hash.Append(BASE32[ch]);
                    bit = 0;
                    ch = 0;
                }
            }

            return hash.ToString();
        }

        public static GeoHashBounds GetBoundsForRadius(double latitude, double longitude, double radiusMeters)
        {
            // Calculate appropriate precision based on radius
            int precision = radiusMeters switch
            {
                < 100 => 8,      // ~20m precision
                < 500 => 7,      // ~80m precision
                < 2000 => 6,     // ~600m precision
                < 10000 => 5,    // ~2.4km precision
                _ => 4           // ~20km precision
            };

            var centerHash = Encode(latitude, longitude, precision);
            
            // Get neighboring geohashes to ensure we cover the entire radius
            var neighbors = GetNeighbors(centerHash);
            var prefixes = new List<string> { centerHash };
            prefixes.AddRange(neighbors);

            return new GeoHashBounds
            {
                CenterGeoHash = centerHash,
                GeoHashPrefixes = prefixes,
                Precision = precision
            };
        }

        private static List<string> GetNeighbors(string geoHash)
        {
            // Simplified neighbor calculation - returns adjacent geohashes
            // In production, use a proper geohash library
            var neighbors = new List<string>();
            
            if (string.IsNullOrEmpty(geoHash)) return neighbors;

            var lastChar = geoHash[^1];
            var baseHash = geoHash[..^1];
            var index = BASE32.IndexOf(lastChar);

            // Add adjacent characters in base32
            if (index > 0)
                neighbors.Add(baseHash + BASE32[index - 1]);
            if (index < BASE32.Length - 1)
                neighbors.Add(baseHash + BASE32[index + 1]);

            return neighbors;
        }
    }

    public class GeoHashBounds
    {
        public string CenterGeoHash { get; set; }
        public List<string> GeoHashPrefixes { get; set; }
        public int Precision { get; set; }
    }
    #endregion
}
