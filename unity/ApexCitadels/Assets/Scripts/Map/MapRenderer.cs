using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ApexCitadels.Map
{
    /// <summary>
    /// Renders map tiles to a Unity UI canvas with pan/zoom support
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MapRenderer : MonoBehaviour
    {
        public static MapRenderer Instance { get; private set; }

        [Header("Rendering")]
        [SerializeField] private RawImage tileImagePrefab;
        [SerializeField] private RectTransform tilesContainer;
        [SerializeField] private int visibleTilesBuffer = 2;

        [Header("Interaction")]
        [SerializeField] private bool enablePanning = true;
        [SerializeField] private bool enablePinchZoom = true;
        [SerializeField] private float panSensitivity = 1f;
        [SerializeField] private float zoomSensitivity = 0.5f;
        [SerializeField] private float momentumDecay = 0.95f;

        [Header("Zoom")]
        [SerializeField] private int minZoom = 10;
        [SerializeField] private int maxZoom = 19;
        [SerializeField] private int defaultZoom = 15;

        [Header("Overlays")]
        [SerializeField] private RectTransform territoryOverlayContainer;
        [SerializeField] private RectTransform markerContainer;
        [SerializeField] private GameObject territoryOverlayPrefab;

        // State
        private double _centerLat;
        private double _centerLon;
        private int _zoom;
        private float _subZoom; // For smooth zooming
        private Vector2 _panVelocity;
        private bool _isDragging;
        private Vector2 _lastDragPosition;

        // Tile management
        private Dictionary<string, RawImage> _tileImages = new Dictionary<string, RawImage>();
        private HashSet<string> _visibleTileKeys = new HashSet<string>();
        private List<string> _tilesToRemove = new List<string>();

        // Touch handling
        private float _initialPinchDistance;
        private float _pinchZoomStart;

        // Events
        public event Action<double, double> OnMapCenterChanged;
        public event Action<int> OnZoomChanged;
        public event Action<double, double> OnMapTapped;
        public event Action<double, double> OnMapLongPress;

        // Properties
        public double CenterLatitude => _centerLat;
        public double CenterLongitude => _centerLon;
        public int Zoom => _zoom;
        public RectTransform RectTransform => GetComponent<RectTransform>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _zoom = defaultZoom;
            _subZoom = defaultZoom;
        }

        private void Start()
        {
            if (MapTileProvider.Instance != null)
            {
                MapTileProvider.Instance.OnTileLoaded += OnTileLoaded;
                MapTileProvider.Instance.OnProviderChanged += RefreshAllTiles;
            }
        }

        private void OnDestroy()
        {
            if (MapTileProvider.Instance != null)
            {
                MapTileProvider.Instance.OnTileLoaded -= OnTileLoaded;
                MapTileProvider.Instance.OnProviderChanged -= RefreshAllTiles;
            }
        }

        private void Update()
        {
            HandleInput();
            ApplyMomentum();
        }

        #region Map Control

        /// <summary>
        /// Set the map center position
        /// </summary>
        public void SetCenter(double lat, double lon, bool animate = false)
        {
            if (animate)
            {
                StartCoroutine(AnimatePan(_centerLat, _centerLon, lat, lon, 0.3f));
            }
            else
            {
                _centerLat = lat;
                _centerLon = lon;
                RefreshTiles();
                OnMapCenterChanged?.Invoke(_centerLat, _centerLon);
            }
        }

        /// <summary>
        /// Set the zoom level
        /// </summary>
        public void SetZoom(int zoom, bool animate = false)
        {
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);

            if (animate)
            {
                StartCoroutine(AnimateZoom(_zoom, zoom, 0.3f));
            }
            else
            {
                _zoom = zoom;
                _subZoom = zoom;
                RefreshTiles();
                OnZoomChanged?.Invoke(_zoom);
            }
        }

        /// <summary>
        /// Zoom in one level
        /// </summary>
        public void ZoomIn()
        {
            SetZoom(_zoom + 1, true);
        }

        /// <summary>
        /// Zoom out one level
        /// </summary>
        public void ZoomOut()
        {
            SetZoom(_zoom - 1, true);
        }

        /// <summary>
        /// Pan the map by screen delta
        /// </summary>
        public void Pan(Vector2 screenDelta)
        {
            double metersPerPixel = GetMetersPerPixel();
            
            double deltaLat = -screenDelta.y * metersPerPixel / 111320.0;
            double deltaLon = -screenDelta.x * metersPerPixel / (111320.0 * Math.Cos(_centerLat * Math.PI / 180));

            _centerLat += deltaLat;
            _centerLon += deltaLon;

            // Clamp latitude
            _centerLat = Math.Max(-85.05, Math.Min(85.05, _centerLat));
            
            // Wrap longitude
            while (_centerLon > 180) _centerLon -= 360;
            while (_centerLon < -180) _centerLon += 360;

            RefreshTiles();
            OnMapCenterChanged?.Invoke(_centerLat, _centerLon);
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // Mouse/touch input
            if (Input.touchCount == 0)
            {
                HandleMouseInput();
            }
            else if (Input.touchCount == 1)
            {
                HandleSingleTouch();
            }
            else if (Input.touchCount >= 2 && enablePinchZoom)
            {
                HandlePinchZoom();
            }

            // Scroll wheel zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _subZoom += scroll * 5f * zoomSensitivity;
                _subZoom = Mathf.Clamp(_subZoom, minZoom, maxZoom);
                
                int newZoom = Mathf.RoundToInt(_subZoom);
                if (newZoom != _zoom)
                {
                    _zoom = newZoom;
                    RefreshTiles();
                    OnZoomChanged?.Invoke(_zoom);
                }
            }
        }

        private void HandleMouseInput()
        {
            if (!enablePanning) return;

            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverMap(Input.mousePosition))
                {
                    _isDragging = true;
                    _lastDragPosition = Input.mousePosition;
                    _panVelocity = Vector2.zero;
                }
            }
            else if (Input.GetMouseButton(0) && _isDragging)
            {
                Vector2 delta = (Vector2)Input.mousePosition - _lastDragPosition;
                Pan(delta * panSensitivity);
                _panVelocity = delta;
                _lastDragPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }
        }

        private void HandleSingleTouch()
        {
            if (!enablePanning) return;

            Touch touch = Input.GetTouch(0);

            if (!IsPointerOverMap(touch.position)) return;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _isDragging = true;
                    _lastDragPosition = touch.position;
                    _panVelocity = Vector2.zero;
                    break;

                case TouchPhase.Moved:
                    if (_isDragging)
                    {
                        Vector2 delta = touch.position - _lastDragPosition;
                        Pan(delta * panSensitivity);
                        _panVelocity = delta;
                        _lastDragPosition = touch.position;
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    _isDragging = false;
                    break;
            }
        }

        private void HandlePinchZoom()
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                _initialPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                _pinchZoomStart = _subZoom;
            }
            else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                float ratio = currentDistance / _initialPinchDistance;

                _subZoom = _pinchZoomStart + (ratio - 1f) * 3f * zoomSensitivity;
                _subZoom = Mathf.Clamp(_subZoom, minZoom, maxZoom);

                int newZoom = Mathf.RoundToInt(_subZoom);
                if (newZoom != _zoom)
                {
                    _zoom = newZoom;
                    RefreshTiles();
                    OnZoomChanged?.Invoke(_zoom);
                }
            }

            _isDragging = false;
        }

        private void ApplyMomentum()
        {
            if (!_isDragging && _panVelocity.magnitude > 0.1f)
            {
                Pan(_panVelocity * Time.deltaTime * 30f);
                _panVelocity *= momentumDecay;
            }
        }

        private bool IsPointerOverMap(Vector2 screenPos)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(RectTransform, screenPos, null);
        }

        #endregion

        #region Tile Rendering

        private void RefreshTiles()
        {
            if (MapTileProvider.Instance == null || tilesContainer == null) return;

            _visibleTileKeys.Clear();

            // Calculate visible tile range
            Rect containerRect = tilesContainer.rect;
            int tileSize = MapTileProvider.Instance.TileSize;

            var (centerTileX, centerTileY) = MapTileProvider.LatLonToTile(_centerLat, _centerLon, _zoom);

            int tilesX = Mathf.CeilToInt(containerRect.width / tileSize) + visibleTilesBuffer * 2;
            int tilesY = Mathf.CeilToInt(containerRect.height / tileSize) + visibleTilesBuffer * 2;

            int maxTile = (1 << _zoom) - 1;

            for (int dx = -tilesX / 2; dx <= tilesX / 2; dx++)
            {
                for (int dy = -tilesY / 2; dy <= tilesY / 2; dy++)
                {
                    int tileX = centerTileX + dx;
                    int tileY = centerTileY + dy;

                    // Wrap X coordinate
                    while (tileX < 0) tileX += maxTile + 1;
                    while (tileX > maxTile) tileX -= maxTile + 1;

                    // Skip invalid Y
                    if (tileY < 0 || tileY > maxTile) continue;

                    string key = $"{_zoom}/{tileX}/{tileY}";
                    _visibleTileKeys.Add(key);

                    // Request tile
                    MapTileProvider.Instance.GetTile(tileX, tileY, _zoom, null);

                    // Create or update tile image
                    UpdateTileImage(tileX, tileY, _zoom);
                }
            }

            // Remove tiles no longer visible
            _tilesToRemove.Clear();
            foreach (var key in _tileImages.Keys)
            {
                if (!_visibleTileKeys.Contains(key))
                {
                    _tilesToRemove.Add(key);
                }
            }

            foreach (var key in _tilesToRemove)
            {
                if (_tileImages.TryGetValue(key, out RawImage img))
                {
                    Destroy(img.gameObject);
                    _tileImages.Remove(key);
                }
            }
        }

        private void UpdateTileImage(int tileX, int tileY, int zoom)
        {
            string key = $"{zoom}/{tileX}/{tileY}";

            // Create image if needed
            if (!_tileImages.TryGetValue(key, out RawImage tileImage))
            {
                if (tileImagePrefab != null)
                {
                    tileImage = Instantiate(tileImagePrefab, tilesContainer);
                }
                else
                {
                    GameObject go = new GameObject($"Tile_{key}");
                    go.transform.SetParent(tilesContainer, false);
                    tileImage = go.AddComponent<RawImage>();
                }

                tileImage.name = $"Tile_{key}";
                _tileImages[key] = tileImage;
            }

            // Position tile
            PositionTile(tileImage, tileX, tileY, zoom);

            // Set texture if available
            var tile = MapTileProvider.Instance.GetTileCached(tileX, tileY, zoom);
            if (tile != null && tile.Texture != null)
            {
                tileImage.texture = tile.Texture;
                tileImage.color = Color.white;
            }
            else
            {
                tileImage.texture = null;
                tileImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Loading placeholder
            }
        }

        private void PositionTile(RawImage tileImage, int tileX, int tileY, int zoom)
        {
            int tileSize = MapTileProvider.Instance?.TileSize ?? 256;
            RectTransform rt = tileImage.GetComponent<RectTransform>();

            // Get tile bounds
            var (tileLat, tileLon) = MapTileProvider.TileToLatLon(tileX, tileY, zoom);

            // Calculate pixel offset from center
            double metersPerPixel = GetMetersPerPixel();

            double deltaLat = tileLat - _centerLat;
            double deltaLon = tileLon - _centerLon;

            double metersNorth = deltaLat * 111320;
            double metersEast = deltaLon * 111320 * Math.Cos(_centerLat * Math.PI / 180);

            float pixelX = (float)(metersEast / metersPerPixel);
            float pixelY = (float)(metersNorth / metersPerPixel);

            // Size
            rt.sizeDelta = new Vector2(tileSize, tileSize);
            rt.anchoredPosition = new Vector2(pixelX, pixelY);
            rt.pivot = new Vector2(0, 1); // Top-left pivot
        }

        private void OnTileLoaded(MapTile tile)
        {
            string key = tile.Key;

            if (_tileImages.TryGetValue(key, out RawImage tileImage))
            {
                if (tile.Texture != null)
                {
                    tileImage.texture = tile.Texture;
                    tileImage.color = Color.white;
                }
            }
        }

        private void RefreshAllTiles()
        {
            // Clear all tiles and reload
            foreach (var img in _tileImages.Values)
            {
                Destroy(img.gameObject);
            }
            _tileImages.Clear();

            RefreshTiles();
        }

        #endregion

        #region Coordinate Conversion

        /// <summary>
        /// Convert screen position to geographic coordinates
        /// </summary>
        public (double Lat, double Lon) ScreenToGeo(Vector2 screenPos)
        {
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTransform, screenPos, null, out localPos);

            double metersPerPixel = GetMetersPerPixel();

            double deltaLat = localPos.y * metersPerPixel / 111320.0;
            double deltaLon = localPos.x * metersPerPixel / (111320.0 * Math.Cos(_centerLat * Math.PI / 180));

            return (_centerLat + deltaLat, _centerLon + deltaLon);
        }

        /// <summary>
        /// Convert geographic coordinates to screen position
        /// </summary>
        public Vector2 GeoToScreen(double lat, double lon)
        {
            double metersPerPixel = GetMetersPerPixel();

            double deltaLat = lat - _centerLat;
            double deltaLon = lon - _centerLon;

            double metersNorth = deltaLat * 111320;
            double metersEast = deltaLon * 111320 * Math.Cos(_centerLat * Math.PI / 180);

            float pixelX = (float)(metersEast / metersPerPixel);
            float pixelY = (float)(metersNorth / metersPerPixel);

            return new Vector2(pixelX, pixelY);
        }

        /// <summary>
        /// Get meters per pixel at current zoom level
        /// </summary>
        public double GetMetersPerPixel()
        {
            return 156543.03392 * Math.Cos(_centerLat * Math.PI / 180) / Math.Pow(2, _zoom);
        }

        #endregion

        #region Animations

        private System.Collections.IEnumerator AnimatePan(double fromLat, double fromLon, double toLat, double toLon, float duration)
        {
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);

                _centerLat = fromLat + (toLat - fromLat) * t;
                _centerLon = fromLon + (toLon - fromLon) * t;

                RefreshTiles();
                yield return null;
            }

            _centerLat = toLat;
            _centerLon = toLon;
            RefreshTiles();
            OnMapCenterChanged?.Invoke(_centerLat, _centerLon);
        }

        private System.Collections.IEnumerator AnimateZoom(int fromZoom, int toZoom, float duration)
        {
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);

                _subZoom = fromZoom + (toZoom - fromZoom) * t;
                _zoom = Mathf.RoundToInt(_subZoom);

                RefreshTiles();
                yield return null;
            }

            _zoom = toZoom;
            _subZoom = toZoom;
            RefreshTiles();
            OnZoomChanged?.Invoke(_zoom);
        }

        #endregion
    }
}
