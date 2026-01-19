using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Territory;
using ApexCitadels.Map;
using ApexCitadels.PC.WebGL;
using ApexCitadels.UI;
using ApexCitadels.Core;

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

        [Header("Containers")]
        [SerializeField] private Transform territoriesContainer;

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
            
            // CRITICAL: Initialize material helper and ensure lighting exists
            MaterialHelper.Initialize();
            MaterialHelper.EnsureLighting();
            
            // Find or create territories container
            if (territoriesContainer == null)
            {
                var existing = GameObject.Find("Territories");
                if (existing != null)
                {
                    territoriesContainer = existing.transform;
                    ApexLogger.Log("[WorldMap] Found existing Territories container", ApexLogger.LogCategory.Map);
                }
                else
                {
                    var container = new GameObject("Territories");
                    territoriesContainer = container.transform;
                    ApexLogger.Log("[WorldMap] Created new Territories container", ApexLogger.LogCategory.Map);
                }
            }
            
            CreateGroundPlane();
            SetupSkyAndCamera();
            
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
                ApexLogger.Log("[WorldMap] Created FirebaseWebClient", ApexLogger.LogCategory.Map);
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
                ApexLogger.Log("[WorldMap] Loading territories from Firebase...", ApexLogger.LogCategory.Map);
                var territories = await _firebaseClient.GetAllTerritories();
                ApexLogger.Log($"[WorldMap] Loaded {territories.Count} territories from Firebase", ApexLogger.LogCategory.Map);

                _cachedTerritories = territories;
                RefreshVisibleTerritories();
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[WorldMap] Failed to load territories: {ex.Message}", ApexLogger.LogCategory.Map);
                // Fall back to mock data
                ApexLogger.Log("[WorldMap] Falling back to mock data", ApexLogger.LogCategory.Map);
            }
            finally
            {
                _isLoadingTerritories = false;
            }
        }

        private void OnFirebaseTerritoriesReceived(List<TerritorySnapshot> territories)
        {
            ApexLogger.Log($"[WorldMap] Received {territories.Count} territories from Firebase", ApexLogger.LogCategory.Map);
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
            
            // Direct click detection as fallback (in case PCInputManager is not wired)
            if (Input.GetMouseButtonDown(0))
            {
                // Don't click if over UI
                if (UnityEngine.EventSystems.EventSystem.current != null && 
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
                
                ApexLogger.Log("[WorldMap] Direct click detected", ApexLogger.LogCategory.Map);
                
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 10000f))
                {
                    ApexLogger.Log($"[WorldMap] Raycast hit: {hit.collider.gameObject.name}", ApexLogger.LogCategory.Map);
                    
                    TerritoryVisual visual = hit.collider.GetComponent<TerritoryVisual>() ?? 
                                              hit.collider.GetComponentInParent<TerritoryVisual>();
                    if (visual != null)
                    {
                        ApexLogger.Log($"[WorldMap] Territory clicked: {visual.TerritoryId}", ApexLogger.LogCategory.Map);
                        SelectTerritory(visual.TerritoryId);
                        OnTerritoryClicked?.Invoke(visual.TerritoryId);
                    }
                }
            }
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
            _groundPlane.transform.localPosition = new Vector3(0, -1f, 0); // Below territories
            _groundPlane.transform.localScale = new Vector3(200, 1, 200); // 2000 x 2000 units

            // Use a nice terrain-like green color
            Color groundColor = new Color(0.18f, 0.45f, 0.22f, 1f); // Forest green
            Material mat = MaterialHelper.CreateColorMaterial(groundColor);
            
            if (mat != null)
            {
                _groundPlane.GetComponent<Renderer>().material = mat;
                ApexLogger.Log($"[WorldMap] Ground plane created with shader: {mat.shader.name}", ApexLogger.LogCategory.Map);
            }
            else
            {
                ApexLogger.LogError("[WorldMap] Failed to create ground material!", ApexLogger.LogCategory.Map);
            }
        }

        private void SetupSkyAndCamera()
        {
            // Nice gradient sky blue
            Color skyBlue = new Color(0.4f, 0.6f, 0.9f, 1f);
            
            // Force sky color on main camera
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = skyBlue;
                ApexLogger.Log("[WorldMap] Camera background set to blue sky", ApexLogger.LogCategory.Map);
            }
            
            // Also set on all cameras
            foreach (Camera cam in Camera.allCameras)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = skyBlue;
            }
            
            // Setup better ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.7f, 0.8f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.6f, 0.65f, 0.7f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.35f, 0.25f);
            RenderSettings.ambientIntensity = 1.2f;
            
            // Disable skybox - solid color is more performant for WebGL
            RenderSettings.skybox = null;
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
            
            // Use MaterialHelper for reliable line material
            lr.material = MaterialHelper.CreateLineMaterial(new Color(0.5f, 0.5f, 0.5f, 0.3f));
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
            
            ApexLogger.Log($"[WorldMap] RefreshVisibleTerritories: {territories.Count} territories to render", ApexLogger.LogCategory.Map);

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
            territoryObj.transform.parent = territoriesContainer;

            // Convert GPS to world position
            Vector3 worldPos = GPSToWorldPosition(territory.CenterLatitude, territory.CenterLongitude);
            territoryObj.transform.position = worldPos;

            // Get material based on ownership
            Material baseMat = GetTerritoryMaterial(territory);
            
            // Make territories larger and more visible - minimum 25 units radius
            float radius = Mathf.Max(territory.RadiusMeters / 8f, 25f);

            // === HEXAGONAL BASE PLATFORM ===
            // Create a hexagonal platform using a cylinder (looks hex-ish when viewed from above)
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "HexBase";
            baseObj.transform.parent = territoryObj.transform;
            baseObj.transform.localPosition = new Vector3(0, 1f, 0);
            baseObj.transform.localScale = new Vector3(radius * 2.2f, 2f, radius * 2.2f);
            baseObj.GetComponent<Renderer>().material = baseMat;

            // === INNER RING ===
            GameObject innerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerRing.name = "InnerRing";
            innerRing.transform.parent = territoryObj.transform;
            innerRing.transform.localPosition = new Vector3(0, 3f, 0);
            innerRing.transform.localScale = new Vector3(radius * 1.6f, 1f, radius * 1.6f);
            // Slightly brighter version
            Material innerMat = MaterialHelper.CreateColorMaterial(baseMat.color * 1.2f);
            innerRing.GetComponent<Renderer>().material = innerMat;

            // === CITADEL TOWER (Central beacon) ===
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "CitadelTower";
            tower.transform.parent = territoryObj.transform;
            tower.transform.localPosition = new Vector3(0, 30f, 0);
            tower.transform.localScale = new Vector3(6f, 60f, 6f);
            tower.GetComponent<Renderer>().material = baseMat;

            // === TOP BEACON SPHERE ===
            GameObject topSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            topSphere.name = "BeaconTop";
            topSphere.transform.parent = territoryObj.transform;
            topSphere.transform.localPosition = new Vector3(0, 95f, 0);
            topSphere.transform.localScale = new Vector3(12f, 12f, 12f);
            // Glowing bright version
            Material glowMat = MaterialHelper.CreateColorMaterial(baseMat.color * 1.5f);
            topSphere.GetComponent<Renderer>().material = glowMat;

            // === DEFENSE TOWERS (4 corners) ===
            float towerOffset = radius * 0.7f;
            Vector3[] towerPositions = {
                new Vector3(towerOffset, 0, towerOffset),
                new Vector3(-towerOffset, 0, towerOffset),
                new Vector3(towerOffset, 0, -towerOffset),
                new Vector3(-towerOffset, 0, -towerOffset)
            };
            
            foreach (var pos in towerPositions)
            {
                GameObject defenseTower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                defenseTower.name = "DefenseTower";
                defenseTower.transform.parent = territoryObj.transform;
                defenseTower.transform.localPosition = pos + new Vector3(0, 15f, 0);
                defenseTower.transform.localScale = new Vector3(3f, 30f, 3f);
                defenseTower.GetComponent<Renderer>().material = baseMat;
                
                // Small top on each defense tower
                GameObject towerTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                towerTop.name = "TowerTop";
                towerTop.transform.parent = defenseTower.transform;
                towerTop.transform.localPosition = new Vector3(0, 0.6f, 0);
                towerTop.transform.localScale = new Vector3(1.5f, 0.5f, 1.5f);
                towerTop.GetComponent<Renderer>().material = glowMat;
            }

            // === TERRITORY NAME LABEL ===
            GameObject labelObj = new GameObject("TerritoryLabel");
            labelObj.transform.parent = territoryObj.transform;
            labelObj.transform.localPosition = new Vector3(0, 115f, 0);
            
            TextMesh textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = territory.Name;
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.8f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            textMesh.fontStyle = FontStyle.Bold;
            labelObj.AddComponent<BillboardText>();

            // === LEVEL BADGE ===
            GameObject levelObj = new GameObject("LevelBadge");
            levelObj.transform.parent = territoryObj.transform;
            levelObj.transform.localPosition = new Vector3(0, 105f, 0);
            
            TextMesh levelText = levelObj.AddComponent<TextMesh>();
            // Note: TextMesh doesn't support TMP sprites, using text stars
            levelText.text = $"* Level {territory.Level} *";
            levelText.fontSize = 48;
            levelText.characterSize = 0.6f;
            levelText.anchor = TextAnchor.MiddleCenter;
            levelText.alignment = TextAlignment.Center;
            levelText.color = new Color(1f, 0.9f, 0.4f); // Gold color
            levelObj.AddComponent<BillboardText>();

            // === OWNER NAME (if owned) ===
            if (!string.IsNullOrEmpty(territory.OwnerName))
            {
                GameObject ownerObj = new GameObject("OwnerLabel");
                ownerObj.transform.parent = territoryObj.transform;
                ownerObj.transform.localPosition = new Vector3(0, 97f, 0);
                
                TextMesh ownerText = ownerObj.AddComponent<TextMesh>();
                // Note: TextMesh doesn't support TMP sprites, using text crown
                ownerText.text = $"[King] {territory.OwnerName}";
                ownerText.fontSize = 36;
                ownerText.characterSize = 0.5f;
                ownerText.anchor = TextAnchor.MiddleCenter;
                ownerText.alignment = TextAlignment.Center;
                ownerText.color = new Color(0.9f, 0.9f, 1f);
                ownerObj.AddComponent<BillboardText>();
            }

            // Add territory data component
            TerritoryVisual visual = territoryObj.AddComponent<TerritoryVisual>();
            visual.Initialize(territory);

            // Add collider for clicking - covers the whole territory area
            CapsuleCollider collider = territoryObj.AddComponent<CapsuleCollider>();
            collider.radius = radius * 1.2f;
            collider.height = 120f;
            collider.center = new Vector3(0, 50f, 0);

            _territoryObjects[territory.Id] = territoryObj;

            ApexLogger.Log($"[WorldMap] Created territory: {territory.Name} at {worldPos} (radius={radius})", ApexLogger.LogCategory.Map);
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
            
            // Use vibrant, distinct colors for each type
            if (territory.IsContested)
            {
                // Contested = Bright Orange/Yellow with pulsing effect potential
                mat = contestedTerritoryMaterial != null ? contestedTerritoryMaterial 
                    : CreateDefaultMaterial(new Color(1f, 0.7f, 0.1f)); // Orange
            }
            else if (territory.OwnerId == currentUserId)
            {
                // Owned = Rich Emerald Green
                mat = ownedTerritoryMaterial != null ? ownedTerritoryMaterial 
                    : CreateDefaultMaterial(new Color(0.1f, 0.8f, 0.3f)); // Bright green
            }
            else if (!string.IsNullOrEmpty(territory.AllianceId))
            {
                // Alliance = Royal Blue
                mat = allianceTerritoryMaterial != null ? allianceTerritoryMaterial 
                    : CreateDefaultMaterial(new Color(0.2f, 0.4f, 0.95f)); // Bright blue
            }
            else if (!string.IsNullOrEmpty(territory.OwnerId))
            {
                // Enemy = Crimson Red
                mat = enemyTerritoryMaterial != null ? enemyTerritoryMaterial 
                    : CreateDefaultMaterial(new Color(0.9f, 0.15f, 0.15f)); // Red
            }
            else
            {
                // Neutral/Unclaimed = Silver/Gray with hint of purple
                mat = neutralTerritoryMaterial != null ? neutralTerritoryMaterial 
                    : CreateDefaultMaterial(new Color(0.55f, 0.5f, 0.65f)); // Purple-gray
            }
            
            return mat;
        }

        /// <summary>
        /// Create a material with the given color. Uses MaterialHelper for reliable URP/WebGL compatibility.
        /// </summary>
        private Material CreateDefaultMaterial(Color color)
        {
            // Use the centralized MaterialHelper
            Material mat = MaterialHelper.CreateColorMaterial(color);
            
            if (mat == null)
            {
                ApexLogger.LogError($"[WorldMap] Failed to create material for color {color}", ApexLogger.LogCategory.Map);
            }
            else
            {
                ApexLogger.Log($"[WorldMap] Created material: color={color}, shader={mat.shader?.name}", ApexLogger.LogCategory.Map);
            }
            
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
        /// Handle territory click from TerritoryVisual component
        /// </summary>
        public void HandleTerritoryClick(string territoryId)
        {
            ApexLogger.Log($"[WorldMap] Territory clicked via TerritoryVisual: {territoryId}", ApexLogger.LogCategory.Map);
            SelectTerritory(territoryId);
            OnTerritoryClicked?.Invoke(territoryId);
        }

        /// <summary>
        /// Handle territory hover from TerritoryVisual component
        /// </summary>
        public void HandleTerritoryHover(string territoryId)
        {
            if (_hoveredTerritoryId != territoryId)
            {
                _hoveredTerritoryId = territoryId;
                OnTerritoryHovered?.Invoke(territoryId);
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
            ApexLogger.Log($"[WorldMap] Reference point set to ({latitude}, {longitude})", ApexLogger.LogCategory.Map);
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
                ApexLogger.Log($"[WorldMap] Using {_cachedTerritories.Count} territories from Firebase", ApexLogger.LogCategory.Map);
                
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
            ApexLogger.Log("[WorldMap] No Firebase data, using mock territories", ApexLogger.LogCategory.Map);
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
        public string OwnerName { get; private set; }
        public int Level { get; private set; }
        public int TerritoryLevel => Level;

        private bool _isSelected;
        private bool _isHovered;
        private Renderer[] _renderers;
        private Color[] _baseColors;
        private TerritoryInteractionFeedback _feedback;

        private void Awake()
        {
            // Add interaction feedback component
            _feedback = GetComponent<TerritoryInteractionFeedback>();
            if (_feedback == null)
            {
                _feedback = gameObject.AddComponent<TerritoryInteractionFeedback>();
            }
        }

        public void Initialize(Territory.Territory territory)
        {
            TerritoryId = territory.Id;
            TerritoryName = territory.Name;
            OwnerId = territory.OwnerId;
            OwnerName = territory.OwnerName;
            Level = territory.Level;

            // Get all renderers and store base colors
            _renderers = GetComponentsInChildren<Renderer>();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i].material != null)
                {
                    _baseColors[i] = _renderers[i].material.color;
                }
            }
            
            // Tag for raycasting (skip if tag doesn't exist)
            try
            {
                gameObject.tag = "Territory";
            }
            catch (UnityException)
            {
                // Tag not defined - that's okay, we'll use layer or name instead
            }
        }

        public void UpdateData(Territory.Territory territory)
        {
            TerritoryName = territory.Name;
            OwnerId = territory.OwnerId;
            OwnerName = territory.OwnerName;
            Level = territory.Level;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            
            if (selected)
            {
                _feedback?.OnSelect();
            }
            else
            {
                _feedback?.OnDeselect();
            }
        }

        public void SetHovered(bool hovered)
        {
            _isHovered = hovered;
            
            if (hovered)
            {
                _feedback?.OnHoverEnter();
            }
            else
            {
                _feedback?.OnHoverExit();
            }
        }

        private void OnMouseEnter()
        {
            SetHovered(true);
            WorldMapRenderer.Instance?.HandleTerritoryHover(TerritoryId);
        }

        private void OnMouseExit()
        {
            SetHovered(false);
        }

        private void OnMouseDown()
        {
            // Notify world map renderer of click
            WorldMapRenderer.Instance?.HandleTerritoryClick(TerritoryId);
        }
    }
}
