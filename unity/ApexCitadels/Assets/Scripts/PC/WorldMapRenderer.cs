using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Territory;
using ApexCitadels.Map;
using ApexCitadels.PC.WebGL;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Renders the 3D world map view for the PC client.
    /// Displays territories, players, resources, and events on a strategic map.
    /// </summary>
    public class WorldMapRenderer : MonoBehaviour
    {
        public static WorldMapRenderer Instance { get; private set; }

        [Header("Map Settings")]
        [SerializeField] private float tileSize = 100f;          // World units per tile
        [SerializeField] private int viewRadius = 5;             // Number of tiles to render around center
        [SerializeField] private float updateInterval = 1f;      // How often to refresh data

        [Header("Territory Rendering")]
        [SerializeField] private Material ownedTerritoryMaterial;
        [SerializeField] private Material enemyTerritoryMaterial;
        [SerializeField] private Material allianceTerritoryMaterial;
        [SerializeField] private Material neutralTerritoryMaterial;
        [SerializeField] private Material contestedTerritoryMaterial;

        [Header("Terrain Settings")]
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Color waterColor = new Color(0.2f, 0.4f, 0.8f);
        [SerializeField] private Color landColor = new Color(0.3f, 0.5f, 0.3f);
        [SerializeField] private float gridLineWidth = 0.5f;
        [SerializeField] private bool showGridLines = true;

        [Header("Markers")]
        [SerializeField] private GameObject territoryMarkerPrefab;
        [SerializeField] private GameObject playerMarkerPrefab;
        [SerializeField] private GameObject resourceMarkerPrefab;
        [SerializeField] private GameObject eventMarkerPrefab;
        [SerializeField] private float markerHoverHeight = 5f;

        // Events
        public event Action<string> OnTerritoryClicked;
        public event Action<string> OnTerritoryHovered;
        public event Action<double, double> OnMapClicked;
        public event Action OnMapUpdated;

        // State
        private Dictionary<string, GameObject> _territoryObjects = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> _markerObjects = new Dictionary<string, GameObject>();
        private List<Territory.Territory> _visibleTerritories = new List<Territory.Territory>();
        private GameObject _groundPlane;
        private GameObject _gridContainer;
        private float _lastUpdateTime;
        private string _hoveredTerritoryId;
        private string _selectedTerritoryId;

        // Reference to camera for culling
        private PCCameraController _cameraController;

        // Firebase client for real data
        private FirebaseWebClient _firebaseClient;
        private List<TerritorySnapshot> _cachedTerritories = new List<TerritorySnapshot>();
        public List<TerritorySnapshot> AllTerritories => _cachedTerritories;
        private bool _isLoadingTerritories = false;

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
            _cameraController = PCCameraController.Instance;
            CreateGroundPlane();
            if (showGridLines)
                CreateGridLines();

            // Subscribe to input events
            if (PCInputManager.Instance != null)
            {
                PCInputManager.Instance.OnWorldClick += HandleWorldClick;
            }

            // Initialize Firebase client
            InitializeFirebaseClient();
        }

        private void InitializeFirebaseClient()
        {
            // Find or create FirebaseWebClient
            _firebaseClient = FindObjectOfType<FirebaseWebClient>();
            if (_firebaseClient == null)
            {
                var clientObj = new GameObject("FirebaseWebClient");
                _firebaseClient = clientObj.AddComponent<FirebaseWebClient>();
                Debug.Log("[WorldMap] Created FirebaseWebClient");
            }

            // Subscribe to territory updates
            _firebaseClient.OnTerritoriesReceived += OnFirebaseTerritoriesReceived;

            // Load initial territories
            LoadTerritoriesFromFirebase();
        }

        private async void LoadTerritoriesFromFirebase()
        {
            if (_isLoadingTerritories) return;
            _isLoadingTerritories = true;

            try
            {
                Debug.Log("[WorldMap] Loading territories from Firebase...");
                var territories = await _firebaseClient.GetAllTerritories();
                Debug.Log($"[WorldMap] Loaded {territories.Count} territories from Firebase");

                _cachedTerritories = territories;
                RefreshVisibleTerritories();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldMap] Failed to load territories: {ex.Message}");
                // Fall back to mock data
                Debug.Log("[WorldMap] Falling back to mock data");
            }
            finally
            {
                _isLoadingTerritories = false;
            }
        }

        private void OnFirebaseTerritoriesReceived(List<TerritorySnapshot> territories)
        {
            Debug.Log($"[WorldMap] Received {territories.Count} territories from Firebase");
            _cachedTerritories = territories;
            RefreshVisibleTerritories();
        }

        private void Update()
        {
            if (Time.time - _lastUpdateTime > updateInterval)
            {
                RefreshVisibleTerritories();
                _lastUpdateTime = Time.time;
            }

            UpdateHoveredTerritory();
        }

        private void OnDestroy()
        {
            if (PCInputManager.Instance != null)
            {
                PCInputManager.Instance.OnWorldClick -= HandleWorldClick;
            }

            if (_firebaseClient != null)
            {
                _firebaseClient.OnTerritoriesReceived -= OnFirebaseTerritoriesReceived;
            }
        }

        #region Ground & Grid

        private void CreateGroundPlane()
        {
            _groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundPlane.name = "WorldMapGround";
            _groundPlane.transform.parent = transform;
            _groundPlane.transform.localPosition = Vector3.zero;
            _groundPlane.transform.localScale = new Vector3(1000, 1, 1000);

            // Apply material
            Renderer renderer = _groundPlane.GetComponent<Renderer>();
            if (groundMaterial != null)
            {
                renderer.material = groundMaterial;
            }
            else
            {
                // Create a simple procedural material
                Material mat = CreateDefaultMaterial(landColor);
                if (mat != null)
                {
                    renderer.material = mat;
                }
                // If mat is null, keep the primitive's default material
            }
        }

        private void CreateGridLines()
        {
            _gridContainer = new GameObject("GridLines");
            _gridContainer.transform.parent = transform;

            int gridSize = viewRadius * 2 + 1;
            float halfSize = gridSize * tileSize / 2f;

            // Create grid lines using line renderers
            for (int i = 0; i <= gridSize; i++)
            {
                float pos = -halfSize + i * tileSize;

                // Horizontal line
                CreateGridLine($"GridH_{i}", 
                    new Vector3(-halfSize, 0.1f, pos), 
                    new Vector3(halfSize, 0.1f, pos));

                // Vertical line
                CreateGridLine($"GridV_{i}", 
                    new Vector3(pos, 0.1f, -halfSize), 
                    new Vector3(pos, 0.1f, halfSize));
            }
        }

        private void CreateGridLine(string name, Vector3 start, Vector3 end)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.parent = _gridContainer.transform;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPositions(new Vector3[] { start, end });
            lr.startWidth = gridLineWidth;
            lr.endWidth = gridLineWidth;
            
            // Use safe material creation for WebGL compatibility
            lr.material = CreateSafeLineMaterial(new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        private Material CreateSafeLineMaterial(Color color)
        {
            // For LineRenderer, use the default material which is always available
            Material mat = null;
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader != null)
            {
                mat = new Material(shader);
            }

            if (mat == null || mat.shader == null)
            {
                // Fallback: create from a primitive's material
                var temp = GameObject.CreatePrimitive(PrimitiveType.Quad);
                if (temp != null)
                {
                    Renderer r = temp.GetComponent<Renderer>();
                    if (r != null && r.sharedMaterial != null)
                    {
                        mat = new Material(r.sharedMaterial);
                    }
                    DestroyImmediate(temp);
                }
            }
            if (mat != null)
            {
                mat.color = color;
                // Enable transparency
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            return mat;
        }

        #endregion

        #region Territory Rendering

        /// <summary>
        /// Refresh territories visible within camera bounds
        /// </summary>
        public void RefreshVisibleTerritories()
        {
            // Get ALL territories (bypass camera bounds for now to ensure they render)
            List<Territory.Territory> territories = GetAllTerritories();
            
            Debug.Log($"[WorldMap] RefreshVisibleTerritories: {territories.Count} territories to render");

            // Remove territories no longer visible
            List<string> toRemove = new List<string>();
            foreach (var kvp in _territoryObjects)
            {
                bool stillVisible = territories.Exists(t => t.Id == kvp.Key);
                if (!stillVisible)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (string id in toRemove)
            {
                Destroy(_territoryObjects[id]);
                _territoryObjects.Remove(id);
            }

            // Add or update visible territories
            foreach (var territory in territories)
            {
                if (!_territoryObjects.ContainsKey(territory.Id))
                {
                    CreateTerritoryObject(territory);
                }
                else
                {
                    UpdateTerritoryObject(territory);
                }
            }

            _visibleTerritories = territories;
            OnMapUpdated?.Invoke();
        }

        private void CreateTerritoryObject(Territory.Territory territory)
        {
            GameObject territoryObj = new GameObject($"Territory_{territory.Id}");
            territoryObj.transform.parent = transform;

            // Convert GPS to world position
            Vector3 worldPos = GPSToWorldPosition(territory.CenterLatitude, territory.CenterLongitude);
            territoryObj.transform.position = worldPos;

            // Create territory visualization
            // Use a cylinder to represent territory area
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.parent = territoryObj.transform;
            cylinder.transform.localPosition = Vector3.zero;

            // Scale based on territory radius
            float radius = territory.RadiusMeters / 10f; // Scale down for visualization
            cylinder.transform.localScale = new Vector3(radius * 2, 0.5f, radius * 2);

            // Apply material based on ownership
            Renderer renderer = cylinder.GetComponent<Renderer>();
            renderer.material = GetTerritoryMaterial(territory);

            // Add territory data component
            TerritoryVisual visual = territoryObj.AddComponent<TerritoryVisual>();
            visual.Initialize(territory);

            // Add collider for clicking
            CapsuleCollider collider = territoryObj.AddComponent<CapsuleCollider>();
            collider.radius = radius;
            collider.height = 2f;

            _territoryObjects[territory.Id] = territoryObj;

            Debug.Log($"[WorldMap] Created territory object: {territory.Name} at {worldPos}");
        }

        private void UpdateTerritoryObject(Territory.Territory territory)
        {
            if (!_territoryObjects.TryGetValue(territory.Id, out GameObject obj))
                return;

            // Update material if ownership changed
            Renderer renderer = obj.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material = GetTerritoryMaterial(territory);
            }

            // Update visual data
            TerritoryVisual visual = obj.GetComponent<TerritoryVisual>();
            if (visual != null)
            {
                visual.UpdateData(territory);
            }
        }

        private Material GetTerritoryMaterial(Territory.Territory territory)
        {
            // Check current user's ownership
            string currentUserId = Core.GameManager.Instance?.UserId;

            Material mat = null;
            
            // Note: Use explicit null check for Unity objects, not ?? operator
            if (territory.IsContested)
            {
                mat = contestedTerritoryMaterial != null ? contestedTerritoryMaterial : CreateDefaultMaterial(Color.yellow);
            }
            else if (territory.OwnerId == currentUserId)
            {
                mat = ownedTerritoryMaterial != null ? ownedTerritoryMaterial : CreateDefaultMaterial(Color.green);
            }
            else if (!string.IsNullOrEmpty(territory.AllianceId))
            {
                // Check if same alliance (would need alliance manager)
                mat = allianceTerritoryMaterial != null ? allianceTerritoryMaterial : CreateDefaultMaterial(Color.blue);
            }
            else if (!string.IsNullOrEmpty(territory.OwnerId))
            {
                mat = enemyTerritoryMaterial != null ? enemyTerritoryMaterial : CreateDefaultMaterial(Color.red);
            }
            else
            {
                mat = neutralTerritoryMaterial != null ? neutralTerritoryMaterial : CreateDefaultMaterial(Color.gray);
            }
            
            // Final fallback - if still null, use cached base material
            if (mat == null && _cachedBaseMaterial != null)
            {
                mat = new Material(_cachedBaseMaterial);
            }
            
            return mat;
        }

        // Cache the base material loaded from Resources
        private static Material _cachedBaseMaterial;
        
        private Material CreateDefaultMaterial(Color color)
        {
            Material mat = null;
            
            // FIRST: Try to get material from a primitive (most reliable in WebGL)
            // This guarantees we get a shader that's included in the build
            if (_cachedBaseMaterial == null)
            {
                var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (temp != null && temp.GetComponent<Renderer>() != null)
                {
                    _cachedBaseMaterial = temp.GetComponent<Renderer>().sharedMaterial;
                }
                DestroyImmediate(temp);
            }
            
            if (_cachedBaseMaterial != null && _cachedBaseMaterial.shader != null)
            {
                mat = new Material(_cachedBaseMaterial);
            }
            else
            {
                // Try shader names as fallback
                string[] shaderNames = new string[]
                {
                    "Universal Render Pipeline/Lit",
                    "Universal Render Pipeline/Simple Lit",
                    "Standard",
                    "Diffuse",
                    "Sprites/Default",
                    "UI/Default"
                };
                
                foreach (string shaderName in shaderNames)
                {
                    Shader shader = Shader.Find(shaderName);
                    if (shader != null)
                    {
                        mat = new Material(shader);
                        break;
                    }
                }
            }
            
            // If still null, we have a serious problem - log and return null
            if (mat == null)
            {
                Debug.LogError("[WorldMap] CRITICAL: Could not create any material! No shaders available.");
                return null;
            }
            
            Color finalColor = new Color(color.r, color.g, color.b, 1f);
            
            // Try all common color properties
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", finalColor);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", finalColor);
            mat.color = finalColor;
            
            Debug.Log($"[WorldMap] Created material with color {finalColor}, shader: {mat.shader?.name ?? "NULL"}");
            return mat;
        }

        #endregion

        #region Markers

        /// <summary>
        /// Add a marker to the map
        /// </summary>
        public void AddMarker(MapMarker marker)
        {
            if (_markerObjects.ContainsKey(marker.Id))
            {
                UpdateMarker(marker);
                return;
            }

            GameObject prefab = GetMarkerPrefab(marker.Type);
            Vector3 worldPos = GPSToWorldPosition(marker.Latitude, marker.Longitude);
            worldPos.y = markerHoverHeight;

            GameObject markerObj;
            if (prefab != null)
            {
                markerObj = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                // Create default marker
                markerObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                markerObj.transform.position = worldPos;
                markerObj.transform.localScale = Vector3.one * 3f;
                markerObj.GetComponent<Renderer>().material.color = marker.MarkerColor;
            }

            markerObj.name = $"Marker_{marker.Id}";
            _markerObjects[marker.Id] = markerObj;
        }

        /// <summary>
        /// Update an existing marker
        /// </summary>
        public void UpdateMarker(MapMarker marker)
        {
            if (!_markerObjects.TryGetValue(marker.Id, out GameObject obj))
                return;

            Vector3 worldPos = GPSToWorldPosition(marker.Latitude, marker.Longitude);
            worldPos.y = markerHoverHeight;
            obj.transform.position = worldPos;
        }

        /// <summary>
        /// Remove a marker from the map
        /// </summary>
        public void RemoveMarker(string markerId)
        {
            if (_markerObjects.TryGetValue(markerId, out GameObject obj))
            {
                Destroy(obj);
                _markerObjects.Remove(markerId);
            }
        }

        private GameObject GetMarkerPrefab(MapMarkerType type)
        {
            return type switch
            {
                MapMarkerType.OwnedTerritory or 
                MapMarkerType.EnemyTerritory or 
                MapMarkerType.AllianceTerritory or 
                MapMarkerType.NeutralTerritory => territoryMarkerPrefab,
                MapMarkerType.Player => playerMarkerPrefab,
                MapMarkerType.ResourceNode => resourceMarkerPrefab,
                MapMarkerType.EventLocation => eventMarkerPrefab,
                _ => null
            };
        }

        #endregion

        #region Interaction

        private void HandleWorldClick(Vector3 worldPos)
        {
            // Check if clicked on a territory
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 10000f))
            {
                TerritoryVisual visual = hit.collider.GetComponent<TerritoryVisual>() ?? 
                                          hit.collider.GetComponentInParent<TerritoryVisual>();
                if (visual != null)
                {
                    SelectTerritory(visual.TerritoryId);
                    OnTerritoryClicked?.Invoke(visual.TerritoryId);
                    return;
                }
            }

            // Convert world position to GPS and invoke map click
            var gps = WorldToGPSPosition(worldPos);
            OnMapClicked?.Invoke(gps.latitude, gps.longitude);
        }

        private void UpdateHoveredTerritory()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 10000f))
            {
                TerritoryVisual visual = hit.collider.GetComponent<TerritoryVisual>() ?? 
                                          hit.collider.GetComponentInParent<TerritoryVisual>();
                if (visual != null)
                {
                    if (_hoveredTerritoryId != visual.TerritoryId)
                    {
                        _hoveredTerritoryId = visual.TerritoryId;
                        OnTerritoryHovered?.Invoke(_hoveredTerritoryId);
                    }
                    return;
                }
            }

            if (_hoveredTerritoryId != null)
            {
                _hoveredTerritoryId = null;
                OnTerritoryHovered?.Invoke(null);
            }
        }

        /// <summary>
        /// Select a territory
        /// </summary>
        public void SelectTerritory(string territoryId)
        {
            // Deselect previous
            if (!string.IsNullOrEmpty(_selectedTerritoryId) && 
                _territoryObjects.TryGetValue(_selectedTerritoryId, out GameObject oldObj))
            {
                TerritoryVisual oldVisual = oldObj.GetComponent<TerritoryVisual>();
                if (oldVisual != null) oldVisual.SetSelected(false);
            }

            _selectedTerritoryId = territoryId;

            // Select new
            if (!string.IsNullOrEmpty(territoryId) && 
                _territoryObjects.TryGetValue(territoryId, out GameObject newObj))
            {
                TerritoryVisual visual = newObj.GetComponent<TerritoryVisual>();
                if (visual != null) visual.SetSelected(true);
            }
        }

        #endregion

        #region Coordinate Conversion

        // Reference point for centering the map (set to first loaded territory)
        private double _refLatitude = 0;
        private double _refLongitude = 0;
        private bool _refPointSet = false;

        /// <summary>
        /// Set the reference point for coordinate conversion (centers map on this location)
        /// </summary>
        public void SetReferencePoint(double latitude, double longitude)
        {
            _refLatitude = latitude;
            _refLongitude = longitude;
            _refPointSet = true;
            Debug.Log($"[WorldMap] Reference point set to ({latitude}, {longitude})");
        }

        /// <summary>
        /// Convert GPS coordinates to Unity world position
        /// </summary>
        public Vector3 GPSToWorldPosition(double latitude, double longitude)
        {
            // Using simple Mercator projection, centered on reference point
            const double metersPerDegree = 111319.9;
            const double scaleFactor = 0.1; // 1 Unity unit = 10 meters

            // Offset from reference point
            double latOffset = latitude - _refLatitude;
            double lonOffset = longitude - _refLongitude;

            float x = (float)(lonOffset * metersPerDegree * scaleFactor);
            float z = (float)(latOffset * metersPerDegree * scaleFactor);

            return new Vector3(x, 0, z);
        }

        /// <summary>
        /// Convert Unity world position to GPS coordinates
        /// </summary>
        public (double latitude, double longitude) WorldToGPSPosition(Vector3 worldPos)
        {
            const double metersPerDegree = 111319.9;
            const double scaleFactor = 0.1;

            double longitude = _refLongitude + (worldPos.x / scaleFactor) / metersPerDegree;
            double latitude = _refLatitude + (worldPos.z / scaleFactor) / metersPerDegree;

            return (latitude, longitude);
        }

        #endregion

        #region Data Helpers

        /// <summary>
        /// Get ALL territories (no bounds filtering)
        /// </summary>
        private List<Territory.Territory> GetAllTerritories()
        {
            List<Territory.Territory> territories = new List<Territory.Territory>();

            // Use Firebase data if available
            if (_cachedTerritories != null && _cachedTerritories.Count > 0)
            {
                Debug.Log($"[WorldMap] Using {_cachedTerritories.Count} territories from Firebase");
                
                // Set reference point to first territory (centers the map)
                if (!_refPointSet && _cachedTerritories.Count > 0)
                {
                    var first = _cachedTerritories[0];
                    SetReferencePoint(first.latitude, first.longitude);
                }
                
                foreach (var snapshot in _cachedTerritories)
                {
                    var territory = new Territory.Territory
                    {
                        Id = snapshot.id,
                        TerritoryName = snapshot.name ?? $"Territory {snapshot.id}",
                        CenterLatitude = snapshot.latitude,
                        CenterLongitude = snapshot.longitude,
                        RadiusMeters = snapshot.radius > 0 ? snapshot.radius : 100f,
                        OwnerId = snapshot.ownerId,
                        OwnerName = snapshot.ownerName,
                        AllianceId = snapshot.allianceId,
                        Level = snapshot.level > 0 ? snapshot.level : 1,
                        IsContested = snapshot.isContested
                    };
                    territories.Add(territory);
                }
                return territories;
            }

            // Fall back to mock data for testing when no Firebase data
            Debug.Log("[WorldMap] No Firebase data, using mock territories");
            return GetMockTerritories();
        }

        /// <summary>
        /// Get territories within the specified bounds
        /// </summary>
        private List<Territory.Territory> GetTerritoriesInBounds(Bounds bounds)
        {
            // For now, just return all territories
            return GetAllTerritories();
        }

        /// <summary>
        /// Generate mock territories for testing
        /// </summary>
        private List<Territory.Territory> GetMockTerritories()
        {
            List<Territory.Territory> territories = new List<Territory.Territory>();

            // San Francisco area mock territories
            var sfData = new[]
            {
                ("mock-sf-downtown", "SF Downtown", 37.7749, -122.4194, "test-user-1", "TestPlayer1", 3),
                ("mock-sf-embarcadero", "Embarcadero", 37.7955, -122.3937, "test-user-2", "RivalPlayer", 2),
                ("mock-sf-mission", "Mission District", 37.7599, -122.4148, (string)null, (string)null, 1),
            };

            // Vienna, VA area mock territories
            var viennaData = new[]
            {
                ("mock-vienna-downtown", "Vienna Town Green", 38.9012, -77.2653, "test-user-1", "TestPlayer1", 3),
                ("mock-vienna-metro", "Vienna Metro Station", 38.8779, -77.2711, "test-user-2", "RivalPlayer", 2),
                ("mock-vienna-maple", "Maple Avenue", 38.9001, -77.2545, (string)null, (string)null, 2),
                ("mock-tysons", "Tysons Corner", 38.9187, -77.2311, "test-user-2", "RivalPlayer", 4),
                ("mock-wolftrap", "Wolf Trap", 38.9378, -77.2656, (string)null, (string)null, 2),
                ("mock-meadowlark", "Meadowlark Gardens", 38.9394, -77.2803, "test-user-3", "AllianceMember", 1),
                ("mock-oakton", "Oakton", 38.8809, -77.3006, (string)null, (string)null, 1),
            };

            foreach (var (id, name, lat, lng, ownerId, ownerName, level) in sfData)
            {
                territories.Add(new Territory.Territory
                {
                    Id = id,
                    TerritoryName = name,
                    CenterLatitude = lat,
                    CenterLongitude = lng,
                    RadiusMeters = 100f,
                    OwnerId = ownerId,
                    OwnerName = ownerName,
                    Level = level
                });
            }

            foreach (var (id, name, lat, lng, ownerId, ownerName, level) in viennaData)
            {
                territories.Add(new Territory.Territory
                {
                    Id = id,
                    TerritoryName = name,
                    CenterLatitude = lat,
                    CenterLongitude = lng,
                    RadiusMeters = 100f,
                    OwnerId = ownerId,
                    OwnerName = ownerName,
                    Level = level
                });
            }

            return territories;
        }

        /// <summary>
        /// Reload territories from Firebase
        /// </summary>
        public void ReloadFromFirebase()
        {
            LoadTerritoriesFromFirebase();
        }

        #endregion
    }

    /// <summary>
    /// Component attached to territory GameObjects to hold territory data
    /// </summary>
    public class TerritoryVisual : MonoBehaviour
    {
        public string TerritoryId { get; private set; }
        public string TerritoryName { get; private set; }
        public string OwnerId { get; private set; }
        public int Level { get; private set; }

        private bool _isSelected;
        private bool _isHovered;
        private Renderer _renderer;
        private Color _baseColor;

        public void Initialize(Territory.Territory territory)
        {
            TerritoryId = territory.Id;
            TerritoryName = territory.Name;
            OwnerId = territory.OwnerId;
            Level = territory.Level;

            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null)
            {
                _baseColor = _renderer.material.color;
            }
        }

        public void UpdateData(Territory.Territory territory)
        {
            TerritoryName = territory.Name;
            OwnerId = territory.OwnerId;
            Level = territory.Level;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisual();
        }

        public void SetHovered(bool hovered)
        {
            _isHovered = hovered;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (_renderer == null) return;

            if (_isSelected)
            {
                _renderer.material.color = Color.white;
            }
            else if (_isHovered)
            {
                _renderer.material.color = _baseColor * 1.3f;
            }
            else
            {
                _renderer.material.color = _baseColor;
            }
        }

        private void OnMouseEnter()
        {
            SetHovered(true);
        }

        private void OnMouseExit()
        {
            SetHovered(false);
        }
    }
}
