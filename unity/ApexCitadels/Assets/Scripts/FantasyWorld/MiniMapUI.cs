// ============================================================================
// MINI MAP UI - Top-down overview of the fantasy world
// ============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Creates and manages a mini-map showing the player's location
    /// </summary>
    public class MiniMapUI : MonoBehaviour
    {
        [Header("Map Settings")]
        public float mapSize = 200f;         // Size in pixels
        public float worldRadius = 300f;     // World units shown on map
        public Vector2 screenPosition = new Vector2(20, 20); // From top-right
        
        [Header("Colors")]
        public Color backgroundColor = new Color(0.1f, 0.15f, 0.1f, 0.8f);
        public Color roadColor = new Color(0.6f, 0.5f, 0.3f);
        public Color buildingColor = new Color(0.4f, 0.3f, 0.2f);
        public Color playerColor = Color.yellow;
        public Color borderColor = new Color(0.3f, 0.25f, 0.15f);
        
        [Header("References")]
        public Transform playerTransform;
        
        private Canvas _canvas;
        private RawImage _mapImage;
        private RawImage _playerMarker;
        private RawImage _northIndicator;
        private Texture2D _mapTexture;
        private List<Vector3> _buildingPositions = new List<Vector3>();
        private List<List<Vector3>> _roadPaths = new List<List<Vector3>>();
        
        private void Start()
        {
            CreateMiniMapCanvas();
            
            // Find player if not assigned
            if (playerTransform == null)
            {
                var cam = Camera.main;
                if (cam != null) playerTransform = cam.transform;
            }
        }
        
        private void Update()
        {
            UpdatePlayerMarker();
            UpdateMapTexture();
        }
        
        private void CreateMiniMapCanvas()
        {
            // Create Canvas if needed
            _canvas = GetComponentInChildren<Canvas>();
            if (_canvas == null)
            {
                GameObject canvasObj = new GameObject("MiniMapCanvas");
                canvasObj.transform.SetParent(transform);
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 100;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // Create map background/border
            GameObject borderObj = new GameObject("MapBorder");
            borderObj.transform.SetParent(_canvas.transform);
            var border = borderObj.AddComponent<RawImage>();
            border.color = borderColor;
            var borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(1, 1);
            borderRect.anchorMax = new Vector2(1, 1);
            borderRect.pivot = new Vector2(1, 1);
            borderRect.anchoredPosition = new Vector2(-screenPosition.x + 5, -screenPosition.y + 5);
            borderRect.sizeDelta = new Vector2(mapSize + 10, mapSize + 10);
            
            // Create map image
            GameObject mapObj = new GameObject("MapImage");
            mapObj.transform.SetParent(_canvas.transform);
            _mapImage = mapObj.AddComponent<RawImage>();
            
            var mapRect = _mapImage.GetComponent<RectTransform>();
            mapRect.anchorMin = new Vector2(1, 1);
            mapRect.anchorMax = new Vector2(1, 1);
            mapRect.pivot = new Vector2(1, 1);
            mapRect.anchoredPosition = new Vector2(-screenPosition.x, -screenPosition.y);
            mapRect.sizeDelta = new Vector2(mapSize, mapSize);
            
            // Create map texture
            int texSize = 256;
            _mapTexture = new Texture2D(texSize, texSize);
            _mapTexture.filterMode = FilterMode.Point;
            _mapImage.texture = _mapTexture;
            
            // Clear to background
            ClearMapTexture();
            
            // Create player marker
            GameObject playerObj = new GameObject("PlayerMarker");
            playerObj.transform.SetParent(_canvas.transform);
            _playerMarker = playerObj.AddComponent<RawImage>();
            _playerMarker.color = playerColor;
            
            var playerRect = _playerMarker.GetComponent<RectTransform>();
            playerRect.anchorMin = new Vector2(1, 1);
            playerRect.anchorMax = new Vector2(1, 1);
            playerRect.pivot = new Vector2(0.5f, 0.5f);
            playerRect.sizeDelta = new Vector2(10, 10);
            
            // Create simple arrow texture for player
            Texture2D arrowTex = CreateArrowTexture();
            _playerMarker.texture = arrowTex;
            
            // North indicator
            GameObject northObj = new GameObject("NorthIndicator");
            northObj.transform.SetParent(_canvas.transform);
            var northText = northObj.AddComponent<Text>();
            northText.text = "N";
            northText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            northText.fontSize = 14;
            northText.fontStyle = FontStyle.Bold;
            northText.color = Color.red;
            northText.alignment = TextAnchor.MiddleCenter;
            
            var northRect = northText.GetComponent<RectTransform>();
            northRect.anchorMin = new Vector2(1, 1);
            northRect.anchorMax = new Vector2(1, 1);
            northRect.pivot = new Vector2(0.5f, 0.5f);
            northRect.anchoredPosition = new Vector2(-screenPosition.x - mapSize / 2, -screenPosition.y + 15);
            northRect.sizeDelta = new Vector2(20, 20);
        }
        
        private Texture2D CreateArrowTexture()
        {
            int size = 16;
            Texture2D tex = new Texture2D(size, size);
            
            Color clear = new Color(0, 0, 0, 0);
            
            // Clear
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);
            
            // Draw arrow pointing up
            int cx = size / 2;
            for (int y = 0; y < size; y++)
            {
                int width = (size - y) / 2;
                for (int x = cx - width; x <= cx + width; x++)
                {
                    if (x >= 0 && x < size)
                        tex.SetPixel(x, y, playerColor);
                }
            }
            
            tex.Apply();
            return tex;
        }
        
        private void ClearMapTexture()
        {
            Color[] pixels = new Color[_mapTexture.width * _mapTexture.height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = backgroundColor;
            _mapTexture.SetPixels(pixels);
            _mapTexture.Apply();
        }
        
        /// <summary>
        /// Register building positions for map display
        /// </summary>
        public void RegisterBuildings(List<Vector3> positions)
        {
            _buildingPositions = positions;
        }
        
        /// <summary>
        /// Register road paths for map display
        /// </summary>
        public void RegisterRoads(List<List<Vector3>> paths)
        {
            _roadPaths = paths;
        }
        
        private void UpdatePlayerMarker()
        {
            if (playerTransform == null || _playerMarker == null) return;
            
            // Position marker at center of map
            var rect = _playerMarker.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(
                -screenPosition.x - mapSize / 2,
                -screenPosition.y - mapSize / 2
            );
            
            // Rotate based on player facing
            float angle = -playerTransform.eulerAngles.y;
            rect.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        private void UpdateMapTexture()
        {
            if (_mapTexture == null || playerTransform == null) return;
            
            // Clear
            ClearMapTexture();
            
            Vector3 playerPos = playerTransform.position;
            int texSize = _mapTexture.width;
            
            // Draw roads
            foreach (var path in _roadPaths)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    DrawLineOnMap(path[i], path[i + 1], playerPos, roadColor, 2);
                }
            }
            
            // Draw buildings
            foreach (var buildingPos in _buildingPositions)
            {
                Vector2 mapPos = WorldToMapPosition(buildingPos, playerPos);
                int px = Mathf.RoundToInt(mapPos.x * texSize);
                int py = Mathf.RoundToInt(mapPos.y * texSize);
                
                // Draw building as small square
                int size = 3;
                for (int dx = -size; dx <= size; dx++)
                {
                    for (int dy = -size; dy <= size; dy++)
                    {
                        int x = px + dx;
                        int y = py + dy;
                        if (x >= 0 && x < texSize && y >= 0 && y < texSize)
                        {
                            _mapTexture.SetPixel(x, y, buildingColor);
                        }
                    }
                }
            }
            
            _mapTexture.Apply();
        }
        
        private Vector2 WorldToMapPosition(Vector3 worldPos, Vector3 playerPos)
        {
            // Get relative position
            float relX = worldPos.x - playerPos.x;
            float relZ = worldPos.z - playerPos.z;
            
            // Normalize to 0-1 range based on world radius
            float mapX = (relX / worldRadius + 1f) * 0.5f;
            float mapY = (relZ / worldRadius + 1f) * 0.5f;
            
            return new Vector2(Mathf.Clamp01(mapX), Mathf.Clamp01(mapY));
        }
        
        private void DrawLineOnMap(Vector3 start, Vector3 end, Vector3 playerPos, Color color, int thickness)
        {
            Vector2 startMap = WorldToMapPosition(start, playerPos);
            Vector2 endMap = WorldToMapPosition(end, playerPos);
            
            int texSize = _mapTexture.width;
            int x0 = Mathf.RoundToInt(startMap.x * texSize);
            int y0 = Mathf.RoundToInt(startMap.y * texSize);
            int x1 = Mathf.RoundToInt(endMap.x * texSize);
            int y1 = Mathf.RoundToInt(endMap.y * texSize);
            
            // Bresenham's line algorithm
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            while (true)
            {
                // Draw thick pixel
                for (int tx = -thickness; tx <= thickness; tx++)
                {
                    for (int ty = -thickness; ty <= thickness; ty++)
                    {
                        int px = x0 + tx;
                        int py = y0 + ty;
                        if (px >= 0 && px < texSize && py >= 0 && py < texSize)
                        {
                            _mapTexture.SetPixel(px, py, color);
                        }
                    }
                }
                
                if (x0 == x1 && y0 == y1) break;
                
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }
    }
}
