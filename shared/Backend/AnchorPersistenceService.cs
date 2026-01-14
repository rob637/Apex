// ===========================================
// SHARED BACKEND COMPONENT
// Copy this file to each Unity project's Assets/Scripts/Backend/ folder
// ===========================================

using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Shared.Backend
{
    /// <summary>
    /// Shared service for persisting AR anchors to the cloud.
    /// Works with Firebase Firestore.
    /// 
    /// Usage: Copy to your Unity project and update namespace as needed.
    /// </summary>
    public class SharedAnchorPersistenceService : MonoBehaviour
    {
        public static SharedAnchorPersistenceService Instance { get; private set; }

        public event Action OnInitialized;

        [Header("Settings")]
        [SerializeField] private string _gameId = "default-game";
        [SerializeField] private float _searchRadiusMeters = 100f;

        private bool _isInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private async void Start()
        {
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            // TODO: Initialize Firebase
            await Task.Delay(100); // Simulated delay
            
            _isInitialized = true;
            OnInitialized?.Invoke();
            Debug.Log($"[{_gameId}] Persistence service initialized");
        }

        /// <summary>
        /// Save an object's position to the cloud
        /// </summary>
        public async Task<string> SaveObjectAsync(
            double latitude, 
            double longitude, 
            double altitude,
            Quaternion rotation,
            string objectType,
            Dictionary<string, object> metadata = null)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("Service not initialized");
                return null;
            }

            // TODO: Implement Firebase save
            string objectId = Guid.NewGuid().ToString();
            Debug.Log($"[{_gameId}] Saved object {objectId} at ({latitude}, {longitude})");
            
            return objectId;
        }

        /// <summary>
        /// Load nearby objects from the cloud
        /// </summary>
        public async Task<List<SavedObjectData>> LoadNearbyObjectsAsync(
            double latitude, 
            double longitude)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("Service not initialized");
                return new List<SavedObjectData>();
            }

            // TODO: Implement Firebase query
            Debug.Log($"[{_gameId}] Loading objects near ({latitude}, {longitude})");
            
            return new List<SavedObjectData>();
        }

        /// <summary>
        /// Delete an object from the cloud
        /// </summary>
        public async Task DeleteObjectAsync(string objectId)
        {
            // TODO: Implement Firebase delete
            Debug.Log($"[{_gameId}] Deleted object {objectId}");
        }
    }

    [Serializable]
    public class SavedObjectData
    {
        public string id;
        public double latitude;
        public double longitude;
        public double altitude;
        public Quaternion rotation;
        public string objectType;
        public string createdBy;
        public DateTime createdAt;
        public Dictionary<string, object> metadata;
    }
}
