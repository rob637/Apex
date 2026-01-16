using UnityEngine;

namespace ApexCitadels.Backend
{
    /// <summary>
    /// Service locator that provides access to all backend services.
    /// Initializes and wires up all Firebase service implementations.
    /// </summary>
    public class ServiceLocator : MonoBehaviour
    {
        public static ServiceLocator Instance { get; private set; }

        [Header("Service References")]
        [SerializeField] private BattleService _battleService;
        [SerializeField] private ProtectionService _protectionService;
        [SerializeField] private BlueprintService _blueprintService;
        [SerializeField] private AllianceWarService _allianceWarService;
        [SerializeField] private LocationService _locationService;
        [SerializeField] private AnchorPersistenceService _anchorService;

        // Public accessors
        public static IBattleService Battle => Instance?._battleService;
        public static IProtectionService Protection => Instance?._protectionService;
        public static IBlueprintService Blueprint => Instance?._blueprintService;
        public static IAllianceWarService AllianceWar => Instance?._allianceWarService;
        public static ILocationService Location => Instance?._locationService;
        public static AnchorPersistenceService Anchor => Instance?._anchorService;

        // Quick access properties
        public static bool IsInitialized => Instance != null && Instance._isInitialized;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeServices();
        }

        private void InitializeServices()
        {
            // Auto-create services if not assigned
            if (_battleService == null)
                _battleService = GetOrCreateService<BattleService>("BattleService");

            if (_protectionService == null)
                _protectionService = GetOrCreateService<ProtectionService>("ProtectionService");

            if (_blueprintService == null)
                _blueprintService = GetOrCreateService<BlueprintService>("BlueprintService");

            if (_allianceWarService == null)
                _allianceWarService = GetOrCreateService<AllianceWarService>("AllianceWarService");

            if (_locationService == null)
                _locationService = GetOrCreateService<LocationService>("LocationService");

            if (_anchorService == null)
                _anchorService = GetOrCreateService<AnchorPersistenceService>("AnchorPersistenceService");

            _isInitialized = true;
            Debug.Log("[ServiceLocator] All backend services initialized");
        }

        private T GetOrCreateService<T>(string name) where T : MonoBehaviour
        {
            // Check if already exists
            var existing = FindFirstObjectByType<T>();
            if (existing != null)
                return existing;

            // Create as child of ServiceLocator
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            return go.AddComponent<T>();
        }

        /// <summary>
        /// Get a service by interface type
        /// </summary>
        public static T GetService<T>() where T : class
        {
            if (Instance == null)
            {
                Debug.LogError("[ServiceLocator] Not initialized!");
                return null;
            }

            if (typeof(T) == typeof(IBattleService))
                return Instance._battleService as T;
            if (typeof(T) == typeof(IProtectionService))
                return Instance._protectionService as T;
            if (typeof(T) == typeof(IBlueprintService))
                return Instance._blueprintService as T;
            if (typeof(T) == typeof(IAllianceWarService))
                return Instance._allianceWarService as T;
            if (typeof(T) == typeof(ILocationService))
                return Instance._locationService as T;
            if (typeof(T) == typeof(AnchorPersistenceService))
                return Instance._anchorService as T;

            Debug.LogWarning($"[ServiceLocator] Unknown service type: {typeof(T).Name}");
            return null;
        }

        /// <summary>
        /// Invalidate all service caches (useful after login/logout)
        /// </summary>
        public static void InvalidateAllCaches()
        {
            Instance?._protectionService?.InvalidateCache();
            Instance?._blueprintService?.InvalidateCache();
            Instance?._allianceWarService?.InvalidateCache();
            Instance?._locationService?.ClearCache();
            Debug.Log("[ServiceLocator] All caches invalidated");
        }
    }
}
