using Camera = UnityEngine.Camera;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Ground Level LOD Manager - Handles seamless transitions between aerial map view
    /// and ground-level fantasy rendering. Manages loading/unloading of detail based
    /// on camera altitude and position.
    /// </summary>
    public class GroundLevelLODManager : MonoBehaviour
    {
        [Header("View Transition Settings")]
        [SerializeField] private float aerialViewAltitude = 500f;
        [SerializeField] private float transitionStartAltitude = 200f;
        [SerializeField] private float groundViewAltitude = 50f;
        [SerializeField] private float transitionSpeed = 2f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Chunk System")]
        [SerializeField] private float chunkSize = 200f;
        [SerializeField] private int viewDistanceChunks = 3;
        [SerializeField] private int groundViewDistanceChunks = 2;
        [SerializeField] private float chunkLoadDelay = 0.1f;
        
        [Header("LOD Distances")]
        [SerializeField] private float highDetailDistance = 100f;
        [SerializeField] private float mediumDetailDistance = 300f;
        [SerializeField] private float lowDetailDistance = 600f;
        
        [Header("Components")]
        [SerializeField] private OSMDataPipeline osmPipeline;
        [SerializeField] private ProceduralBuildingGenerator buildingGenerator;
        [SerializeField] private FantasyRoadRenderer roadRenderer;
        [SerializeField] private FantasyTerrainGenerator terrainGenerator;
        
        [Header("Aerial View")]
        [SerializeField] private GameObject aerialMapRoot;
        [SerializeField] private Material aerialMapMaterial;
        
        [Header("Effects")]
        [SerializeField] private bool useFogTransition = true;
        [SerializeField] private Color aerialFogColor = new Color(0.7f, 0.8f, 0.9f);
        [SerializeField] private Color groundFogColor = new Color(0.5f, 0.6f, 0.5f);
        [SerializeField] private float aerialFogDensity = 0.001f;
        [SerializeField] private float groundFogDensity = 0.01f;
        
        // Singleton
        private static GroundLevelLODManager _instance;
        public static GroundLevelLODManager Instance => _instance;
        
        // State
        private ViewMode _currentViewMode = ViewMode.Aerial;
        private float _transitionProgress = 0f;
        private bool _isTransitioning = false;
        
        // Chunks
        private Dictionary<Vector2Int, WorldChunk> _loadedChunks = new Dictionary<Vector2Int, WorldChunk>();
        private HashSet<Vector2Int> _loadingChunks = new HashSet<Vector2Int>();
        private Queue<Vector2Int> _chunkLoadQueue = new Queue<Vector2Int>();
        
        // Camera reference
        private Camera _mainCamera;
        private Transform _playerTransform;
        
        // Geographic center
        private double _centerLatitude;
        private double _centerLongitude;
        private Vector3 _worldOrigin;
        
        // Events
        public event Action<ViewMode> OnViewModeChanged;
        public event Action<float> OnTransitionProgress;
        public event Action<Vector2Int> OnChunkLoaded;
        public event Action<Vector2Int> OnChunkUnloaded;
        
        public ViewMode CurrentViewMode => _currentViewMode;
        public float TransitionProgress => _transitionProgress;
        public bool IsGroundLevel => _currentViewMode == ViewMode.Ground;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            _mainCamera = Camera.main;
        }
        
        private void Start()
        {
            // Find references if not assigned
            if (osmPipeline == null) osmPipeline = FindFirstObjectByType<OSMDataPipeline>();
            if (buildingGenerator == null) buildingGenerator = FindFirstObjectByType<ProceduralBuildingGenerator>();
            if (roadRenderer == null) roadRenderer = FindFirstObjectByType<FantasyRoadRenderer>();
            if (terrainGenerator == null) terrainGenerator = FindFirstObjectByType<FantasyTerrainGenerator>();
            
            StartCoroutine(ChunkLoadingCoroutine());
        }
        
        private void Update()
        {
            if (_mainCamera == null) return;
            
            float altitude = _mainCamera.transform.position.y;
            
            // Auto-transition based on altitude
            UpdateViewModeFromAltitude(altitude);
            
            // Update chunk loading
            UpdateChunks();
            
            // Update LOD for loaded content
            UpdateLOD();
            
            // Update fog
            if (useFogTransition)
            {
                UpdateFog();
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Initialize the LOD system with geographic center
        /// </summary>
        public void Initialize(double latitude, double longitude, Vector3 worldOrigin, Transform player = null)
        {
            _centerLatitude = latitude;
            _centerLongitude = longitude;
            _worldOrigin = worldOrigin;
            _playerTransform = player;
            
            Debug.Log($"[LOD] Initialized at ({latitude:F6}, {longitude:F6})");
        }
        
        /// <summary>
        /// Force transition to specific view mode
        /// </summary>
        public void TransitionToView(ViewMode targetMode, float duration = 1f)
        {
            if (_currentViewMode == targetMode || _isTransitioning) return;
            
            StartCoroutine(TransitionCoroutine(targetMode, duration));
        }
        
        /// <summary>
        /// Toggle between aerial and ground view
        /// </summary>
        public void ToggleView()
        {
            ViewMode target = _currentViewMode == ViewMode.Aerial ? ViewMode.Ground : ViewMode.Aerial;
            TransitionToView(target);
        }
        
        /// <summary>
        /// Force load chunks around position
        /// </summary>
        public void LoadChunksAroundPosition(Vector3 worldPosition)
        {
            Vector2Int centerChunk = WorldToChunk(worldPosition);
            int range = _currentViewMode == ViewMode.Ground ? groundViewDistanceChunks : viewDistanceChunks;
            
            for (int x = -range; x <= range; x++)
            {
                for (int z = -range; z <= range; z++)
                {
                    Vector2Int chunkCoord = new Vector2Int(centerChunk.x + x, centerChunk.y + z);
                    if (!_loadedChunks.ContainsKey(chunkCoord) && !_loadingChunks.Contains(chunkCoord))
                    {
                        _chunkLoadQueue.Enqueue(chunkCoord);
                        _loadingChunks.Add(chunkCoord);
                    }
                }
            }
        }
        
        /// <summary>
        /// Unload all chunks
        /// </summary>
        public void UnloadAllChunks()
        {
            foreach (var chunk in _loadedChunks.Values)
            {
                UnloadChunk(chunk);
            }
            _loadedChunks.Clear();
            _loadingChunks.Clear();
            _chunkLoadQueue.Clear();
        }
        
        /// <summary>
        /// Get chunk at world position
        /// </summary>
        public WorldChunk GetChunkAtPosition(Vector3 worldPosition)
        {
            Vector2Int coord = WorldToChunk(worldPosition);
            return _loadedChunks.TryGetValue(coord, out var chunk) ? chunk : null;
        }
        
        #endregion
        
        #region View Transitions
        
        private void UpdateViewModeFromAltitude(float altitude)
        {
            if (_isTransitioning) return;
            
            if (_currentViewMode == ViewMode.Aerial && altitude < transitionStartAltitude)
            {
                // Start transitioning to ground
                _transitionProgress = Mathf.InverseLerp(transitionStartAltitude, groundViewAltitude, altitude);
                
                if (altitude <= groundViewAltitude)
                {
                    SetViewMode(ViewMode.Ground);
                }
                else
                {
                    SetViewMode(ViewMode.Transitioning);
                }
                
                OnTransitionProgress?.Invoke(_transitionProgress);
            }
            else if (_currentViewMode == ViewMode.Ground && altitude > groundViewAltitude)
            {
                // Start transitioning to aerial
                _transitionProgress = 1f - Mathf.InverseLerp(groundViewAltitude, transitionStartAltitude, altitude);
                
                if (altitude >= transitionStartAltitude)
                {
                    SetViewMode(ViewMode.Aerial);
                }
                else
                {
                    SetViewMode(ViewMode.Transitioning);
                }
                
                OnTransitionProgress?.Invoke(_transitionProgress);
            }
            else if (_currentViewMode == ViewMode.Transitioning)
            {
                _transitionProgress = Mathf.InverseLerp(transitionStartAltitude, groundViewAltitude, altitude);
                
                if (altitude >= transitionStartAltitude)
                {
                    SetViewMode(ViewMode.Aerial);
                }
                else if (altitude <= groundViewAltitude)
                {
                    SetViewMode(ViewMode.Ground);
                }
                
                OnTransitionProgress?.Invoke(_transitionProgress);
            }
        }
        
        private void SetViewMode(ViewMode mode)
        {
            if (_currentViewMode == mode) return;
            
            ViewMode previousMode = _currentViewMode;
            _currentViewMode = mode;
            
            // Update aerial map visibility
            if (aerialMapRoot != null)
            {
                float alpha = 1f - _transitionProgress;
                SetAerialMapAlpha(alpha);
            }
            
            OnViewModeChanged?.Invoke(mode);
            
            Debug.Log($"[LOD] View mode changed: {previousMode} -> {mode}");
        }
        
        private IEnumerator TransitionCoroutine(ViewMode targetMode, float duration)
        {
            _isTransitioning = true;
            
            float startProgress = _transitionProgress;
            float targetProgress = targetMode == ViewMode.Ground ? 1f : 0f;
            float elapsed = 0f;
            
            SetViewMode(ViewMode.Transitioning);
            
            // Move camera if needed
            Vector3 startCamPos = _mainCamera.transform.position;
            float targetAltitude = targetMode == ViewMode.Ground ? groundViewAltitude : aerialViewAltitude;
            Vector3 targetCamPos = new Vector3(startCamPos.x, targetAltitude, startCamPos.z);
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curvedT = transitionCurve.Evaluate(t);
                
                _transitionProgress = Mathf.Lerp(startProgress, targetProgress, curvedT);
                _mainCamera.transform.position = Vector3.Lerp(startCamPos, targetCamPos, curvedT);
                
                // Update aerial map alpha
                if (aerialMapRoot != null)
                {
                    SetAerialMapAlpha(1f - _transitionProgress);
                }
                
                OnTransitionProgress?.Invoke(_transitionProgress);
                
                yield return null;
            }
            
            _transitionProgress = targetProgress;
            SetViewMode(targetMode);
            
            _isTransitioning = false;
        }
        
        private void SetAerialMapAlpha(float alpha)
        {
            if (aerialMapMaterial != null)
            {
                Color color = aerialMapMaterial.color;
                color.a = alpha;
                aerialMapMaterial.color = color;
            }
            
            if (aerialMapRoot != null)
            {
                aerialMapRoot.SetActive(alpha > 0.01f);
            }
        }
        
        #endregion
        
        #region Chunk Management
        
        private void UpdateChunks()
        {
            Vector3 cameraPos = _mainCamera.transform.position;
            Vector2Int centerChunk = WorldToChunk(cameraPos);
            
            int range = _currentViewMode == ViewMode.Ground ? groundViewDistanceChunks : viewDistanceChunks;
            
            // Queue chunks to load
            for (int x = -range; x <= range; x++)
            {
                for (int z = -range; z <= range; z++)
                {
                    Vector2Int chunkCoord = new Vector2Int(centerChunk.x + x, centerChunk.y + z);
                    
                    if (!_loadedChunks.ContainsKey(chunkCoord) && !_loadingChunks.Contains(chunkCoord))
                    {
                        _chunkLoadQueue.Enqueue(chunkCoord);
                        _loadingChunks.Add(chunkCoord);
                    }
                }
            }
            
            // Unload distant chunks
            List<Vector2Int> chunksToUnload = new List<Vector2Int>();
            foreach (var kvp in _loadedChunks)
            {
                int distance = Mathf.Max(Mathf.Abs(kvp.Key.x - centerChunk.x), Mathf.Abs(kvp.Key.y - centerChunk.y));
                if (distance > range + 1)
                {
                    chunksToUnload.Add(kvp.Key);
                }
            }
            
            foreach (var coord in chunksToUnload)
            {
                UnloadChunkAt(coord);
            }
        }
        
        private IEnumerator ChunkLoadingCoroutine()
        {
            while (true)
            {
                if (_chunkLoadQueue.Count > 0)
                {
                    Vector2Int coord = _chunkLoadQueue.Dequeue();
                    yield return StartCoroutine(LoadChunkCoroutine(coord));
                    yield return new WaitForSeconds(chunkLoadDelay);
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        
        private IEnumerator LoadChunkCoroutine(Vector2Int coord)
        {
            // Calculate chunk bounds
            Vector3 chunkCenter = ChunkToWorld(coord);
            Vector2d geoCenter = WorldToGeo(chunkCenter);
            
            // Create chunk
            WorldChunk chunk = new WorldChunk
            {
                coordinate = coord,
                worldCenter = chunkCenter,
                geoCenter = geoCenter,
                state = ChunkState.Loading
            };
            
            // Create container
            chunk.root = new GameObject($"Chunk_{coord.x}_{coord.y}");
            chunk.root.transform.position = chunkCenter;
            chunk.root.transform.SetParent(transform);
            
            // Fetch OSM data for chunk
            if (osmPipeline != null)
            {
                var fetchTask = osmPipeline.FetchBoundingBox(
                    geoCenter.latitude - 0.002, geoCenter.latitude + 0.002,
                    geoCenter.longitude - 0.003, geoCenter.longitude + 0.003
                );
                
                while (!fetchTask.IsCompleted)
                {
                    yield return null;
                }
                
                if (fetchTask.Result != null)
                {
                    chunk.osmData = fetchTask.Result;
                    
                    // Generate buildings
                    if (buildingGenerator != null)
                    {
                        buildingGenerator.GenerateBuildings(chunk.osmData, chunkCenter, 1f);
                    }
                    
                    // Generate roads
                    if (roadRenderer != null)
                    {
                        roadRenderer.RenderRoads(chunk.osmData, chunkCenter, 1f);
                    }
                }
            }
            
            chunk.state = ChunkState.Loaded;
            _loadedChunks[coord] = chunk;
            _loadingChunks.Remove(coord);
            
            OnChunkLoaded?.Invoke(coord);
            
            Debug.Log($"[LOD] Loaded chunk {coord}");
        }
        
        private void UnloadChunkAt(Vector2Int coord)
        {
            if (_loadedChunks.TryGetValue(coord, out var chunk))
            {
                UnloadChunk(chunk);
                _loadedChunks.Remove(coord);
                OnChunkUnloaded?.Invoke(coord);
                
                Debug.Log($"[LOD] Unloaded chunk {coord}");
            }
        }
        
        private void UnloadChunk(WorldChunk chunk)
        {
            if (chunk.root != null)
            {
                Destroy(chunk.root);
            }
            
            chunk.state = ChunkState.Unloaded;
        }
        
        #endregion
        
        #region LOD Updates
        
        private void UpdateLOD()
        {
            if (_mainCamera == null) return;
            
            Vector3 cameraPos = _mainCamera.transform.position;
            
            // Update building LOD
            if (buildingGenerator != null)
            {
                buildingGenerator.UpdateLOD(cameraPos);
            }
            
            // Update road decoration LOD
            if (roadRenderer != null)
            {
                roadRenderer.UpdateDecorationLOD(cameraPos);
            }
            
            // Update chunk detail levels
            foreach (var chunk in _loadedChunks.Values)
            {
                float distance = Vector3.Distance(cameraPos, chunk.worldCenter);
                
                ChunkDetailLevel targetDetail;
                if (distance < highDetailDistance)
                {
                    targetDetail = ChunkDetailLevel.High;
                }
                else if (distance < mediumDetailDistance)
                {
                    targetDetail = ChunkDetailLevel.Medium;
                }
                else
                {
                    targetDetail = ChunkDetailLevel.Low;
                }
                
                if (chunk.detailLevel != targetDetail)
                {
                    SetChunkDetailLevel(chunk, targetDetail);
                }
            }
        }
        
        private void SetChunkDetailLevel(WorldChunk chunk, ChunkDetailLevel level)
        {
            chunk.detailLevel = level;
            
            // Adjust visibility of detail objects
            if (chunk.root != null)
            {
                foreach (Transform child in chunk.root.transform)
                {
                    bool showDetails = level == ChunkDetailLevel.High;
                    
                    if (child.name.Contains("Detail") || child.name.Contains("Decoration"))
                    {
                        child.gameObject.SetActive(showDetails);
                    }
                }
            }
        }
        
        #endregion
        
        #region Fog
        
        private void UpdateFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            
            RenderSettings.fogColor = Color.Lerp(aerialFogColor, groundFogColor, _transitionProgress);
            RenderSettings.fogDensity = Mathf.Lerp(aerialFogDensity, groundFogDensity, _transitionProgress);
        }
        
        #endregion
        
        #region Coordinate Conversion
        
        private Vector2Int WorldToChunk(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos.x - _worldOrigin.x) / chunkSize);
            int z = Mathf.FloorToInt((worldPos.z - _worldOrigin.z) / chunkSize);
            return new Vector2Int(x, z);
        }
        
        private Vector3 ChunkToWorld(Vector2Int chunkCoord)
        {
            float x = _worldOrigin.x + chunkCoord.x * chunkSize + chunkSize / 2;
            float z = _worldOrigin.z + chunkCoord.y * chunkSize + chunkSize / 2;
            return new Vector3(x, 0, z);
        }
        
        private Vector2d WorldToGeo(Vector3 worldPos)
        {
            float metersNorth = worldPos.z - _worldOrigin.z;
            float metersEast = worldPos.x - _worldOrigin.x;
            
            double lat = _centerLatitude + metersNorth / 111320.0;
            double lon = _centerLongitude + metersEast / (111320.0 * Math.Cos(_centerLatitude * Math.PI / 180));
            
            return new Vector2d(lat, lon);
        }
        
        #endregion
    }
    
    #region Enums & Classes
    
    public enum ViewMode
    {
        Aerial,
        Transitioning,
        Ground
    }
    
    public enum ChunkState
    {
        Unloaded,
        Loading,
        Loaded
    }
    
    public enum ChunkDetailLevel
    {
        Low,
        Medium,
        High
    }
    
    [Serializable]
    public class WorldChunk
    {
        public Vector2Int coordinate;
        public Vector3 worldCenter;
        public Vector2d geoCenter;
        public ChunkState state;
        public ChunkDetailLevel detailLevel;
        public GameObject root;
        public OSMAreaData osmData;
    }
    
    #endregion
}
