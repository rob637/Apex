using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Territory;
using ApexCitadels.Map;

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
                // Create a simple procedural material using URP
                renderer.material = CreateDefaultMaterial(landColor);
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
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.material.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        }

        #endregion

        #region Territory Rendering

        /// <summary>
        /// Refresh territories visible within camera bounds
        /// </summary>
        public void RefreshVisibleTerritories()
        {
            if (_cameraController == null) return;

            Bounds viewBounds = _cameraController.GetViewBounds();

            // Get territories from TerritoryManager (if available)
            // For now, use mock data
            List<Territory.Territory> territories = GetTerritoriesInBounds(viewBounds);

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

            // Note: Use explicit null check for Unity objects, not ?? operator
            if (territory.IsContested)
            {
                return contestedTerritoryMaterial != null ? contestedTerritoryMaterial : CreateDefaultMaterial(Color.yellow);
            }
            else if (territory.OwnerId == currentUserId)
            {
                return ownedTerritoryMaterial != null ? ownedTerritoryMaterial : CreateDefaultMaterial(Color.green);
            }
            else if (!string.IsNullOrEmpty(territory.AllianceId))
            {
                // Check if same alliance (would need alliance manager)
                return allianceTerritoryMaterial != null ? allianceTerritoryMaterial : CreateDefaultMaterial(Color.blue);
            }
            else if (!string.IsNullOrEmpty(territory.OwnerId))
            {
                return enemyTerritoryMaterial != null ? enemyTerritoryMaterial : CreateDefaultMaterial(Color.red);
            }
            else
            {
                return neutralTerritoryMaterial != null ? neutralTerritoryMaterial : CreateDefaultMaterial(Color.gray);
            }
        }

        // Cache the base material loaded from Resources
        private static Material _baseMaterial;
        
        private Material CreateDefaultMaterial(Color color)
        {
            // Load material from Resources (this ensures shader is included in build)
            if (_baseMaterial == null)
            {
                _baseMaterial = Resources.Load<Material>("DefaultLit");
                if (_baseMaterial == null)
                {
                    Debug.LogWarning("[WorldMap] DefaultLit material not found in Resources, using primitive default");
                    var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _baseMaterial = temp.GetComponent<Renderer>().sharedMaterial;
                    UnityEngine.Object.DestroyImmediate(temp);
                }
            }
            
            // Create instance with color
            Material mat = new Material(_baseMaterial);
            Color finalColor = new Color(color.r, color.g, color.b, 1f);
            
            // Try all common color properties
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", finalColor);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", finalColor);
            mat.color = finalColor;
            
            Debug.Log($"[WorldMap] Created material with color {finalColor}, shader: {mat.shader.name}");
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

        /// <summary>
        /// Convert GPS coordinates to Unity world position
        /// </summary>
        public Vector3 GPSToWorldPosition(double latitude, double longitude)
        {
            // Using simple Mercator projection
            // Reference point at (0,0) latitude/longitude maps to world origin
            const double metersPerDegree = 111319.9;

            float x = (float)(longitude * metersPerDegree / 10); // Scale down
            float z = (float)(latitude * metersPerDegree / 10);  // Scale down

            return new Vector3(x, 0, z);
        }

        /// <summary>
        /// Convert Unity world position to GPS coordinates
        /// </summary>
        public (double latitude, double longitude) WorldToGPSPosition(Vector3 worldPos)
        {
            const double metersPerDegree = 111319.9;

            double longitude = (worldPos.x * 10) / metersPerDegree;
            double latitude = (worldPos.z * 10) / metersPerDegree;

            return (latitude, longitude);
        }

        #endregion

        #region Data Helpers

        /// <summary>
        /// Get territories within the specified bounds
        /// </summary>
        private List<Territory.Territory> GetTerritoriesInBounds(Bounds bounds)
        {
            // TODO: Connect to TerritoryManager to get real data
            // For now, return mock data for testing

            List<Territory.Territory> territories = new List<Territory.Territory>();

            // Create some test territories
            for (int x = -2; x <= 2; x++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    if (x == 0 && z == 0) continue; // Skip center

                    var territory = new Territory.Territory
                    {
                        Id = $"test_{x}_{z}",
                        Name = $"Territory {x},{z}",
                        CenterLatitude = x * 0.001,
                        CenterLongitude = z * 0.001,
                        RadiusMeters = 50f,
                        OwnerId = (x + z) % 2 == 0 ? "player1" : "player2",
                        Level = 1 + Math.Abs(x + z)
                    };
                    territories.Add(territory);
                }
            }

            return territories;
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
