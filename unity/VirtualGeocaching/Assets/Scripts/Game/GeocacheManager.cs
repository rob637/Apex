using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace VirtualGeocaching.Game
{
    /// <summary>
    /// Main game manager for Virtual Geocaching.
    /// Players hide and find virtual caches in real-world locations.
    /// </summary>
    public class GeocacheManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button _hideCacheButton;
        [SerializeField] private Button _findCachesButton;
        [SerializeField] private Button _viewLogbookButton;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _statsText;
        [SerializeField] private InputField _cacheNameInput;
        [SerializeField] private InputField _cacheHintInput;

        [Header("Prefabs")]
        [SerializeField] private GameObject _smallCachePrefab;
        [SerializeField] private GameObject _mediumCachePrefab;
        [SerializeField] private GameObject _largeCachePrefab;

        [Header("Game Settings")]
        [SerializeField] private float _searchRadius = 500f; // meters
        
        public enum CacheSize { Small, Medium, Large }
        public enum CacheDifficulty { Easy, Medium, Hard, Expert }

        private int _cachesFound = 0;
        private int _cachesHidden = 0;
        private bool _isHidingMode = false;

        [System.Serializable]
        public class GeocacheData
        {
            public string id;
            public string name;
            public string hint;
            public string hiddenBy;
            public System.DateTime hiddenDate;
            public CacheSize size;
            public CacheDifficulty difficulty;
            public double latitude;
            public double longitude;
            public double altitude;
            public List<LogEntry> logbook;
        }

        [System.Serializable]
        public class LogEntry
        {
            public string odPlayerId;
            public string playerName;
            public System.DateTime foundDate;
            public string message;
        }

        private void Start()
        {
            if (_hideCacheButton != null)
                _hideCacheButton.onClick.AddListener(OnHideCacheClicked);
            
            if (_findCachesButton != null)
                _findCachesButton.onClick.AddListener(OnFindCachesClicked);
            
            if (_viewLogbookButton != null)
                _viewLogbookButton.onClick.AddListener(OnViewLogbookClicked);

            UpdateStatus("Ready! Hide a cache or search for nearby caches.");
            UpdateStats();
        }

        private void Update()
        {
            if (_isHidingMode && Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    TryHideCache(touch.position);
                }
            }
        }

        private void OnHideCacheClicked()
        {
            _isHidingMode = true;
            UpdateStatus("Tap on a location to hide your geocache...");
        }

        private void OnFindCachesClicked()
        {
            _isHidingMode = false;
            UpdateStatus("Searching for nearby geocaches...");
            LoadNearbyCaches();
        }

        private void OnViewLogbookClicked()
        {
            // TODO: Show logbook UI
            UpdateStatus("Opening logbook...");
        }

        private void TryHideCache(Vector2 screenPosition)
        {
            _isHidingMode = false;
            
            string cacheName = _cacheNameInput?.text ?? "Unnamed Cache";
            string cacheHint = _cacheHintInput?.text ?? "No hint provided";
            
            // TODO: Implement AR raycast and cache placement
            UpdateStatus($"Cache '{cacheName}' hidden! Share the coordinates with friends.");
            _cachesHidden++;
            UpdateStats();
        }

        private async void LoadNearbyCaches()
        {
            // TODO: Load caches from cloud within search radius
            UpdateStatus("Found caches nearby! Follow the hints to locate them.");
        }

        public void OnCacheFound(GeocacheData cache)
        {
            _cachesFound++;
            UpdateStats();
            UpdateStatus($"You found '{cache.name}'! Sign the logbook!");
            // TODO: Show logbook signing UI
        }

        private void UpdateStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
            Debug.Log($"[VirtualGeocaching] {message}");
        }

        private void UpdateStats()
        {
            if (_statsText != null)
                _statsText.text = $"Found: {_cachesFound} | Hidden: {_cachesHidden}";
        }
    }
}
