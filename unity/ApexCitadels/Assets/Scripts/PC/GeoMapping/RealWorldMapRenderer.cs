// ============================================================================
// APEX CITADELS - REAL WORLD MAP RENDERER
// Renders actual real-world geography with territories overlaid
// "One World - Two Ways to Access" - PC shows the same world AR players stake
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Core;
using ApexCitadels.Territory;
using ApexCitadels.Map;
using ApexCitadels.PC.WebGL;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Renders real-world map tiles with territory overlays.
    /// This is the PC "window" into the same world that AR mobile players explore.
    /// </summary>
    public class RealWorldMapRenderer : MonoBehaviour
    {
        public static RealWorldMapRenderer Instance { get; private set; }

        [Header("Map Configuration")]
        [SerializeField] private GeoCoordinate startLocation = new GeoCoordinate(38.8951, -77.0364); // Washington Monument
        [SerializeField] private int defaultZoom = 16;
        [SerializeField] private int minZoom = 10;
        [SerializeField] private int maxZoom = 19;
        [SerializeField] private int tilesPerDirection = 5; // 5x5 grid = 25 tiles
        [SerializeField] private float tileWorldSize = 100f; // Unity units per tile
        
        [Header("Visual Settings")]
        [SerializeField] private float mapHeight = 0f; // Y position of map plane
        [SerializeField] private bool enableShadowReceive = true;
        [SerializeField] private Material mapTileMaterial;
        
        [Header("Territory Display")]
        [SerializeField] private float territoryHeight = 5f; // How high territories float above map
        [SerializeField] private bool showTerritoryBoundaries = true;
        [SerializeField] private bool showTerritoryLabels = true;
        [SerializeField] private float boundaryLineWidth = 2f;
        
        [Header("Interactive")]
        [SerializeField] private float panSpeed = 50f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minCameraHeight = 50f;
        [SerializeField] private float maxCameraHeight = 2000f;

        [Header("Colors")]
        [SerializeField] private Color ownedColor = new Color(0.2f, 0.8f, 0.3f, 0.6f);
        [SerializeField] private Color allianceColor = new Color(0.2f, 0.5f, 0.9f, 0.6f);
        [SerializeField] private Color enemyColor = new Color(0.9f, 0.2f, 0.2f, 0.6f);
        [SerializeField] private Color neutralColor = new Color(0.7f, 0.7f, 0.7f, 0.4f);
        [SerializeField] private Color contestedColor = new Color(0.9f, 0.7f, 0.1f, 0.6f);

        // State
        private GeoCoordinate _currentCenter;
        private int _currentZoom;
        private Dictionary<string, GameObject> _tileObjects = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> _territoryMarkers = new Dictionary<string, GameObject>();
        private Transform _tilesContainer;
        private Transform _territoriesContainer;
        private MapTileProvider _tileProvider;
        private FirebaseWebClient _firebaseClient;
        private List<TerritorySnapshot> _territories = new List<TerritorySnapshot>();
        private bool _isInitialized = false;

        // Events
        public event Action<GeoCoordinate> OnLocationChanged;
        public event Action<int> OnZoomChanged;
        public event Action<string> OnTerritorySelected;

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
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            ApexLogger.LogMap("Initializing real-world map renderer...");

            // Create containers
            _tilesContainer = new GameObject("MapTiles").transform;
            _tilesContainer.parent = transform;
            _tilesContainer.localPosition = Vector3.zero;

            _territoriesContainer = new GameObject("TerritoryMarkers").transform;
            _territoriesContainer.parent = transform;
            _territoriesContainer.localPosition = new Vector3(0, territoryHeight, 0);

            // Get or create tile provider
            _tileProvider = FindFirstObjectByType<MapTileProvider>();
            if (_tileProvider == null)
            {
                var providerObj = new GameObject("MapTileProvider");
                providerObj.transform.parent = transform;
                _tileProvider = providerObj.AddComponent<MapTileProvider>();
            }
            _tileProvider.OnTileLoaded += OnTileLoaded;
            _tileProvider.OnTileFailed += OnTileFailed;

            // Get Firebase client
            _firebaseClient = FindFirstObjectByType<FirebaseWebClient>();
            if (_firebaseClient != null)
            {
                _firebaseClient.OnTerritoriesReceived += OnTerritoriesReceived;
            }

            // Set up projection reference point
            _currentCenter = startLocation;
            _currentZoom = defaultZoom;
            GeoProjection.SetReferencePoint(_currentCenter);
            GeoProjection.SetScale(1.0); // 1 Unity unit = 1 meter

            yield return null;

            // Load initial tiles
            RefreshMapTiles();
            
            // Load territories
            yield return LoadTerritories();

            _isInitialized = true;
            ApexLogger.LogMap($"Initialized at {_currentCenter}, zoom {_currentZoom}");
        }

        /// <summary>
        /// Move the map center to a new GPS location
        /// </summary>
        public void GoToLocation(GeoCoordinate location)
        {
            _currentCenter = location;
            GeoProjection.SetReferencePoint(_currentCenter);
            RefreshMapTiles();
            RefreshTerritoryMarkers();
            OnLocationChanged?.Invoke(_currentCenter);
            ApexLogger.LogMap($"Moved to {location}");
        }

        /// <summary>
        /// Move the map center to a specific address (requires geocoding service)
        /// </summary>
        public void GoToAddress(string address)
        {
            // TODO: Integrate geocoding service (Nominatim, Google, Mapbox)
            ApexLogger.LogWarning($"GoToAddress not yet implemented: {address}", ApexLogger.LogCategory.Map);
        }

        /// <summary>
        /// Set zoom level (affects detail level and view distance)
        /// </summary>
        public void SetZoom(int zoom)
        {
            _currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            RefreshMapTiles();
            OnZoomChanged?.Invoke(_currentZoom);
        }

        /// <summary>
        /// Zoom in one level
        /// </summary>
        public void ZoomIn()
        {
            SetZoom(_currentZoom + 1);
        }

        /// <summary>
        /// Zoom out one level
        /// </summary>
        public void ZoomOut()
        {
            SetZoom(_currentZoom - 1);
        }

        /// <summary>
        /// Pan the map by a world-space offset
        /// </summary>
        public void Pan(Vector3 worldOffset)
        {
            var geoOffset = GeoProjection.WorldToGeo(worldOffset);
            var newCenter = new GeoCoordinate(
                _currentCenter.Latitude + (geoOffset.Latitude - GeoProjection.ReferencePoint.Latitude),
                _currentCenter.Longitude + (geoOffset.Longitude - GeoProjection.ReferencePoint.Longitude)
            );
            GoToLocation(newCenter);
        }

        /// <summary>
        /// Center map on a specific territory
        /// </summary>
        public void FocusTerritory(string territoryId)
        {
            var territory = _territories.Find(t => t.id == territoryId);
            if (territory != null)
            {
                GoToLocation(new GeoCoordinate(territory.latitude, territory.longitude));
                OnTerritorySelected?.Invoke(territoryId);
            }
        }

        #region Map Tile Management

        private void RefreshMapTiles()
        {
            if (_tileProvider == null) return;

            // Calculate which tiles we need
            var centerTile = TileCoordinate.FromGeo(_currentCenter, _currentZoom);
            HashSet<string> neededTiles = new HashSet<string>();

            int halfGrid = tilesPerDirection / 2;
            for (int dx = -halfGrid; dx <= halfGrid; dx++)
            {
                for (int dy = -halfGrid; dy <= halfGrid; dy++)
                {
                    var tileCoord = new TileCoordinate(
                        centerTile.X + dx,
                        centerTile.Y + dy,
                        _currentZoom
                    );
                    string key = tileCoord.ToString();
                    neededTiles.Add(key);

                    // Create tile if not exists
                    if (!_tileObjects.ContainsKey(key))
                    {
                        CreateTileObject(tileCoord, dx, dy);
                    }
                    else
                    {
                        // Update position
                        UpdateTilePosition(_tileObjects[key], tileCoord, dx, dy);
                    }
                }
            }

            // Remove tiles that are no longer needed
            List<string> toRemove = new List<string>();
            foreach (var kvp in _tileObjects)
            {
                if (!neededTiles.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (string key in toRemove)
            {
                Destroy(_tileObjects[key]);
                _tileObjects.Remove(key);
            }
        }

        private void CreateTileObject(TileCoordinate coord, int dx, int dy)
        {
            // Create a quad for this tile
            GameObject tileObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tileObj.name = $"Tile_{coord}";
            tileObj.transform.parent = _tilesContainer;
            
            // Position the tile
            UpdateTilePosition(tileObj, coord, dx, dy);

            // Rotate to face up
            tileObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            tileObj.transform.localScale = new Vector3(tileWorldSize, tileWorldSize, 1f);

            // Remove collider (we'll add a larger one later for interaction)
            var collider = tileObj.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Set material
            var renderer = tileObj.GetComponent<Renderer>();
            if (mapTileMaterial != null)
            {
                renderer.material = new Material(mapTileMaterial);
            }
            else
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
            }
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = enableShadowReceive;

            // Request tile texture
            var texture = _tileProvider.GetTile(coord);
            if (texture != null)
            {
                renderer.material.mainTexture = texture;
            }
            else
            {
                // Set loading placeholder color
                renderer.material.color = new Color(0.9f, 0.9f, 0.95f);
            }

            _tileObjects[coord.ToString()] = tileObj;
        }

        private void UpdateTilePosition(GameObject tileObj, TileCoordinate coord, int dx, int dy)
        {
            // Position based on grid offset from center
            float x = dx * tileWorldSize;
            float z = -dy * tileWorldSize; // Negative because tile Y increases southward
            tileObj.transform.localPosition = new Vector3(x, mapHeight, z);
        }

        private void OnTileLoaded(TileCoordinate coord, Texture2D texture)
        {
            string key = coord.ToString();
            if (_tileObjects.TryGetValue(key, out var tileObj))
            {
                var renderer = tileObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.mainTexture = texture;
                    renderer.material.color = Color.white;
                }
            }
        }

        private void OnTileFailed(TileCoordinate coord, string error)
        {
            string key = coord.ToString();
            if (_tileObjects.TryGetValue(key, out var tileObj))
            {
                var renderer = tileObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Show error color
                    renderer.material.color = new Color(0.8f, 0.6f, 0.6f);
                }
            }
        }

        #endregion

        #region Territory Management

        private IEnumerator LoadTerritories()
        {
            if (_firebaseClient == null)
            {
                ApexLogger.LogWarning("No Firebase client, using demo territories", ApexLogger.LogCategory.Map);
                CreateDemoTerritories();
                yield break;
            }

            // Load territories in current view area
            var bounds = GetVisibleBounds();
            var territories = _firebaseClient.GetTerritoriesInArea(
                bounds.North, bounds.South, bounds.East, bounds.West
            );

            // Wait for async operation
            while (!territories.IsCompleted)
            {
                yield return null;
            }

            if (territories.IsCompletedSuccessfully && territories.Result != null)
            {
                _territories = territories.Result;
                RefreshTerritoryMarkers();
            }
        }

        private void OnTerritoriesReceived(List<TerritorySnapshot> territories)
        {
            _territories = territories;
            RefreshTerritoryMarkers();
        }

        private void RefreshTerritoryMarkers()
        {
            // Clear existing markers
            foreach (var marker in _territoryMarkers.Values)
            {
                Destroy(marker);
            }
            _territoryMarkers.Clear();

            // Create markers for each territory
            foreach (var territory in _territories)
            {
                CreateTerritoryMarker(territory);
            }

            ApexLogger.LogMap($"Rendered {_territories.Count} territory markers");
        }

        private void CreateTerritoryMarker(TerritorySnapshot territory)
        {
            var geoCoord = new GeoCoordinate(territory.latitude, territory.longitude);
            Vector3 worldPos = GeoProjection.GeoToWorld(geoCoord);
            worldPos.y = territoryHeight;

            // Create marker container
            GameObject markerObj = new GameObject($"Territory_{territory.id}");
            markerObj.transform.parent = _territoriesContainer;
            markerObj.transform.position = worldPos;

            // Get color based on ownership
            Color markerColor = GetTerritoryColor(territory.ownerId);

            // Create territory boundary circle
            if (showTerritoryBoundaries)
            {
                CreateTerritoryBoundary(markerObj.transform, territory.radius, markerColor);
            }

            // Create territory fill (semi-transparent disc)
            CreateTerritoryFill(markerObj.transform, territory.radius, markerColor);

            // Create center citadel marker
            CreateCitadelMarker(markerObj.transform, territory, markerColor);

            // Create label
            if (showTerritoryLabels)
            {
                CreateTerritoryLabel(markerObj.transform, territory);
            }

            // Add interaction
            var interactable = markerObj.AddComponent<TerritoryInteractable>();
            interactable.TerritoryId = territory.id;
            interactable.OnClicked += () => OnTerritorySelected?.Invoke(territory.id);

            _territoryMarkers[territory.id] = markerObj;
        }

        private void CreateTerritoryBoundary(Transform parent, float radiusMeters, Color color)
        {
            GameObject boundaryObj = new GameObject("Boundary");
            boundaryObj.transform.parent = parent;
            boundaryObj.transform.localPosition = Vector3.zero;

            LineRenderer lr = boundaryObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = boundaryLineWidth;
            lr.endWidth = boundaryLineWidth;

            // Create circle points
            int segments = 32;
            lr.positionCount = segments;
            float radius = GeoProjection.MetersToUnits(radiusMeters);
            
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                lr.SetPosition(i, new Vector3(x, 0, z));
            }

            // Set material
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
        }

        private void CreateTerritoryFill(Transform parent, float radiusMeters, Color color)
        {
            GameObject fillObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fillObj.name = "Fill";
            fillObj.transform.parent = parent;
            fillObj.transform.localPosition = new Vector3(0, -0.5f, 0);

            float radius = GeoProjection.MetersToUnits(radiusMeters);
            fillObj.transform.localScale = new Vector3(radius * 2f, 0.5f, radius * 2f);

            // Remove collider
            Destroy(fillObj.GetComponent<Collider>());

            // Semi-transparent material
            var renderer = fillObj.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(color.r, color.g, color.b, 0.3f);
            renderer.material = mat;
        }

        private void CreateCitadelMarker(Transform parent, TerritorySnapshot territory, Color color)
        {
            // Create a simple citadel representation
            GameObject citadel = new GameObject("Citadel");
            citadel.transform.parent = parent;
            citadel.transform.localPosition = Vector3.zero;

            // Main tower (cylinder)
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "Tower";
            tower.transform.parent = citadel.transform;
            tower.transform.localPosition = new Vector3(0, 10f, 0);
            tower.transform.localScale = new Vector3(8f, 10f, 8f);
            Destroy(tower.GetComponent<Collider>());
            
            var towerMat = new Material(Shader.Find("Sprites/Default"));
            towerMat.color = color;
            tower.GetComponent<Renderer>().material = towerMat;

            // Roof (cone-ish using stretched sphere)
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            roof.name = "Roof";
            roof.transform.parent = citadel.transform;
            roof.transform.localPosition = new Vector3(0, 22f, 0);
            roof.transform.localScale = new Vector3(10f, 6f, 10f);
            Destroy(roof.GetComponent<Collider>());
            
            var roofMat = new Material(Shader.Find("Sprites/Default"));
            roofMat.color = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);
            roof.GetComponent<Renderer>().material = roofMat;

            // Add a point light for visibility
            var lightObj = new GameObject("Light");
            lightObj.transform.parent = citadel.transform;
            lightObj.transform.localPosition = new Vector3(0, 30f, 0);
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 2f;
            light.range = 50f;
        }

        private void CreateTerritoryLabel(Transform parent, TerritorySnapshot territory)
        {
            // Create floating label using TextMesh (basic 3D text)
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.parent = parent;
            labelObj.transform.localPosition = new Vector3(0, 35f, 0);

            var textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = territory.name ?? $"Territory {territory.id?.Substring(0, 6)}";
            textMesh.fontSize = 24;
            textMesh.characterSize = 1f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;

            // Billboard (always face camera)
            labelObj.AddComponent<BillboardLabel>();
        }

        private Color GetTerritoryColor(string ownerId)
        {
            // TODO: Check against current player's ID and alliance
            // For now, use neutral for all
            if (string.IsNullOrEmpty(ownerId))
                return neutralColor;

            // Placeholder logic - in real implementation, compare to current user
            return ownedColor; // Default to owned color for demo
        }

        private GeoBounds GetVisibleBounds()
        {
            // Calculate bounds based on current zoom and visible tiles
            float metersPerTile = (float)(40075016.686 * Math.Cos(_currentCenter.Latitude * Math.PI / 180.0) / Math.Pow(2, _currentZoom));
            float visibleRadius = metersPerTile * tilesPerDirection / 2f;
            
            return GeoBounds.FromCenterAndRadius(_currentCenter, visibleRadius);
        }

        private void CreateDemoTerritories()
        {
            // Create demo territories near the start location for testing
            _territories = new List<TerritorySnapshot>
            {
                new TerritorySnapshot
                {
                    id = "demo1",
                    name = "Washington Monument",
                    latitude = 38.8895,
                    longitude = -77.0352,
                    radius = 100,
                    ownerId = "player1"
                },
                new TerritorySnapshot
                {
                    id = "demo2",
                    name = "Lincoln Memorial",
                    latitude = 38.8893,
                    longitude = -77.0502,
                    radius = 150,
                    ownerId = "player2"
                },
                new TerritorySnapshot
                {
                    id = "demo3",
                    name = "Capitol Building",
                    latitude = 38.8899,
                    longitude = -77.0091,
                    radius = 120,
                    ownerId = ""
                },
                new TerritorySnapshot
                {
                    id = "demo4",
                    name = "White House",
                    latitude = 38.8977,
                    longitude = -77.0365,
                    radius = 80,
                    ownerId = "player1"
                },
                new TerritorySnapshot
                {
                    id = "demo5",
                    name = "Jefferson Memorial",
                    latitude = 38.8814,
                    longitude = -77.0365,
                    radius = 100,
                    ownerId = "player3"
                }
            };

            RefreshTerritoryMarkers();
            ApexLogger.LogMap($"Created {_territories.Count} demo territories");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current map center coordinates
        /// </summary>
        public GeoCoordinate CurrentCenter => _currentCenter;

        /// <summary>
        /// Get current zoom level
        /// </summary>
        public int CurrentZoom => _currentZoom;

        /// <summary>
        /// Get all loaded territories
        /// </summary>
        public List<TerritorySnapshot> Territories => _territories;

        /// <summary>
        /// Convert a screen point to GPS coordinates
        /// </summary>
        public GeoCoordinate ScreenToGeo(Vector2 screenPos)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, mapHeight, 0));
            
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                return GeoProjection.WorldToGeo(hitPoint);
            }
            return _currentCenter;
        }

        /// <summary>
        /// Get the GPS coordinates at the center of the screen
        /// </summary>
        public GeoCoordinate GetCenterGeoCoordinate()
        {
            return ScreenToGeo(new Vector2(Screen.width / 2f, Screen.height / 2f));
        }

        #endregion

        private void OnDestroy()
        {
            if (_tileProvider != null)
            {
                _tileProvider.OnTileLoaded -= OnTileLoaded;
                _tileProvider.OnTileFailed -= OnTileFailed;
            }
            if (_firebaseClient != null)
            {
                _firebaseClient.OnTerritoriesReceived -= OnTerritoriesReceived;
            }
        }
    }

    /// <summary>
    /// Makes labels always face the camera
    /// </summary>
    public class BillboardLabel : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (Camera.main != null)
            {
                transform.LookAt(transform.position + Camera.main.transform.forward);
            }
        }
    }

    /// <summary>
    /// Handles click interaction on territory markers
    /// </summary>
    public class TerritoryInteractable : MonoBehaviour
    {
        public string TerritoryId;
        public event Action OnClicked;

        private void OnMouseDown()
        {
            OnClicked?.Invoke();
        }
    }
}
