using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Player Citadel Renderer - Renders player-built citadels in the fantasy world.
    /// Syncs with the AR mobile app to display player fortifications exactly as designed.
    /// </summary>
    public class PlayerCitadelRenderer : MonoBehaviour
    {
        [Header("Citadel Rendering")]
        [SerializeField] private bool renderPlayerCitadels = true;
        [SerializeField] private bool renderAllyCitadels = true;
        [SerializeField] private bool renderEnemyCitadels = true;
        [SerializeField] private float citadelScale = 1f;
        
        [Header("Citadel Prefabs")]
        [SerializeField] private CitadelPrefabSet[] citadelPrefabs;
        [SerializeField] private GameObject placeholderCitadelPrefab;
        
        [Header("Visual Effects")]
        [SerializeField] private Material playerGlowMaterial;
        [SerializeField] private Material allyGlowMaterial;
        [SerializeField] private Material enemyGlowMaterial;
        [SerializeField] private GameObject selectionIndicatorPrefab;
        [SerializeField] private GameObject captureProgressPrefab;
        
        [Header("Banner System")]
        [SerializeField] private bool showPlayerBanners = true;
        [SerializeField] private GameObject bannerPrefab;
        [SerializeField] private float bannerHeight = 15f;
        
        [Header("Territory Visualization")]
        [SerializeField] private bool showTerritoryBorders = true;
        [SerializeField] private Material territoryBorderMaterial;
        [SerializeField] private float territoryBorderWidth = 2f;
        [SerializeField] private Color playerTerritoryColor = new Color(0.2f, 0.6f, 1f, 0.5f);
        [SerializeField] private Color allyTerritoryColor = new Color(0.2f, 1f, 0.2f, 0.5f);
        [SerializeField] private Color enemyTerritoryColor = new Color(1f, 0.2f, 0.2f, 0.5f);
        
        [Header("LOD Settings")]
        [SerializeField] private float highDetailDistance = 100f;
        [SerializeField] private float mediumDetailDistance = 300f;
        [SerializeField] private float iconOnlyDistance = 800f;
        
        // Singleton
        private static PlayerCitadelRenderer _instance;
        public static PlayerCitadelRenderer Instance => _instance;
        
        // Rendered citadels
        private Dictionary<string, RenderedCitadel> _renderedCitadels = new Dictionary<string, RenderedCitadel>();
        private Dictionary<string, GameObject> _territoryBorders = new Dictionary<string, GameObject>();
        
        // Player info
        private string _currentPlayerId;
        private string _currentAllianceId;
        
        // Events
        public event Action<CitadelData> OnCitadelSelected;
        public event Action<CitadelData> OnCitadelDeselected;
        public event Action<string, float> OnCaptureProgress;
        
        private Transform _citadelContainer;
        private RenderedCitadel _selectedCitadel;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            _citadelContainer = new GameObject("PlayerCitadels").transform;
            _citadelContainer.SetParent(transform);
        }
        
        #region Public API
        
        /// <summary>
        /// Initialize with player info
        /// </summary>
        public void Initialize(string playerId, string allianceId = null)
        {
            _currentPlayerId = playerId;
            _currentAllianceId = allianceId;
        }
        
        /// <summary>
        /// Load and render citadels from data
        /// </summary>
        public async Task LoadCitadelsInArea(double minLat, double maxLat, double minLon, double maxLon, Vector3 worldOrigin, double originLat, double originLon)
        {
            // This would normally fetch from Firebase
            // For now, simulate with sample data
            List<CitadelData> citadels = await FetchCitadelsFromBackend(minLat, maxLat, minLon, maxLon);
            
            foreach (var citadel in citadels)
            {
                if (!_renderedCitadels.ContainsKey(citadel.id))
                {
                    RenderCitadel(citadel, originLat, originLon, worldOrigin);
                }
            }
        }
        
        /// <summary>
        /// Render a single citadel
        /// </summary>
        public RenderedCitadel RenderCitadel(CitadelData data, double originLat, double originLon, Vector3 worldOrigin)
        {
            // Determine ownership type
            CitadelOwnership ownership = DetermineOwnership(data.ownerId, data.allianceId);
            
            // Check if should render
            if (!ShouldRenderCitadel(ownership)) return null;
            
            // Convert to world position
            Vector3 worldPos = GeoToWorld(data.latitude, data.longitude, originLat, originLon, worldOrigin);
            
            // Create citadel object
            GameObject citadelObj = CreateCitadelObject(data, ownership, worldPos);
            
            var rendered = new RenderedCitadel
            {
                data = data,
                gameObject = citadelObj,
                ownership = ownership,
                worldPosition = worldPos
            };
            
            // Add glow effect
            AddGlowEffect(rendered);
            
            // Add banner
            if (showPlayerBanners)
            {
                AddBanner(rendered);
            }
            
            // Add territory border
            if (showTerritoryBorders && data.territoryPolygon != null)
            {
                CreateTerritoryBorder(rendered, originLat, originLon, worldOrigin);
            }
            
            _renderedCitadels[data.id] = rendered;
            
            return rendered;
        }
        
        /// <summary>
        /// Update citadel data
        /// </summary>
        public void UpdateCitadel(CitadelData newData)
        {
            if (_renderedCitadels.TryGetValue(newData.id, out var rendered))
            {
                rendered.data = newData;
                
                // Update visuals
                UpdateCitadelVisuals(rendered);
            }
        }
        
        /// <summary>
        /// Remove citadel
        /// </summary>
        public void RemoveCitadel(string citadelId)
        {
            if (_renderedCitadels.TryGetValue(citadelId, out var rendered))
            {
                if (rendered.gameObject != null) Destroy(rendered.gameObject);
                if (rendered.bannerObject != null) Destroy(rendered.bannerObject);
                _renderedCitadels.Remove(citadelId);
                
                if (_territoryBorders.TryGetValue(citadelId, out var border))
                {
                    Destroy(border);
                    _territoryBorders.Remove(citadelId);
                }
            }
        }
        
        /// <summary>
        /// Select a citadel
        /// </summary>
        public void SelectCitadel(string citadelId)
        {
            // Deselect current
            if (_selectedCitadel != null)
            {
                SetCitadelSelected(_selectedCitadel, false);
                OnCitadelDeselected?.Invoke(_selectedCitadel.data);
            }
            
            // Select new
            if (_renderedCitadels.TryGetValue(citadelId, out var citadel))
            {
                _selectedCitadel = citadel;
                SetCitadelSelected(citadel, true);
                OnCitadelSelected?.Invoke(citadel.data);
            }
        }
        
        /// <summary>
        /// Clear selection
        /// </summary>
        public void ClearSelection()
        {
            if (_selectedCitadel != null)
            {
                SetCitadelSelected(_selectedCitadel, false);
                OnCitadelDeselected?.Invoke(_selectedCitadel.data);
                _selectedCitadel = null;
            }
        }
        
        /// <summary>
        /// Get citadel at world position
        /// </summary>
        public RenderedCitadel GetCitadelAtPosition(Vector3 worldPos, float radius = 10f)
        {
            foreach (var kvp in _renderedCitadels)
            {
                if (Vector3.Distance(kvp.Value.worldPosition, worldPos) < radius)
                {
                    return kvp.Value;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Start capture progress visualization
        /// </summary>
        public void ShowCaptureProgress(string citadelId, float progress)
        {
            if (_renderedCitadels.TryGetValue(citadelId, out var citadel))
            {
                if (citadel.captureProgressIndicator == null && captureProgressPrefab != null)
                {
                    citadel.captureProgressIndicator = Instantiate(captureProgressPrefab, citadel.gameObject.transform);
                    citadel.captureProgressIndicator.transform.localPosition = Vector3.up * 20f;
                }
                
                // Update progress (assuming a radial progress script)
                // citadel.captureProgressIndicator.GetComponent<RadialProgress>()?.SetProgress(progress);
                
                OnCaptureProgress?.Invoke(citadelId, progress);
            }
        }
        
        /// <summary>
        /// Update LOD for all citadels
        /// </summary>
        public void UpdateLOD(Vector3 cameraPosition)
        {
            foreach (var kvp in _renderedCitadels)
            {
                float distance = Vector3.Distance(cameraPosition, kvp.Value.worldPosition);
                
                CitadelDetailLevel targetLevel;
                if (distance > iconOnlyDistance)
                {
                    targetLevel = CitadelDetailLevel.IconOnly;
                }
                else if (distance > mediumDetailDistance)
                {
                    targetLevel = CitadelDetailLevel.Low;
                }
                else if (distance > highDetailDistance)
                {
                    targetLevel = CitadelDetailLevel.Medium;
                }
                else
                {
                    targetLevel = CitadelDetailLevel.High;
                }
                
                SetCitadelDetailLevel(kvp.Value, targetLevel);
            }
        }
        
        /// <summary>
        /// Clear all citadels
        /// </summary>
        public void ClearAllCitadels()
        {
            foreach (var kvp in _renderedCitadels)
            {
                if (kvp.Value.gameObject != null) Destroy(kvp.Value.gameObject);
                if (kvp.Value.bannerObject != null) Destroy(kvp.Value.bannerObject);
            }
            _renderedCitadels.Clear();
            
            foreach (var kvp in _territoryBorders)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            _territoryBorders.Clear();
        }
        
        #endregion
        
        #region Citadel Creation
        
        private GameObject CreateCitadelObject(CitadelData data, CitadelOwnership ownership, Vector3 position)
        {
            GameObject citadelObj;
            
            // Try to find matching prefab
            CitadelPrefabSet prefabSet = GetPrefabSet(data.citadelType);
            
            if (prefabSet != null && prefabSet.levelPrefabs != null && data.level <= prefabSet.levelPrefabs.Length)
            {
                citadelObj = Instantiate(prefabSet.levelPrefabs[data.level - 1], position, Quaternion.identity, _citadelContainer);
            }
            else if (placeholderCitadelPrefab != null)
            {
                citadelObj = Instantiate(placeholderCitadelPrefab, position, Quaternion.identity, _citadelContainer);
            }
            else
            {
                // Create basic placeholder
                citadelObj = CreatePlaceholderCitadel(data, position);
            }
            
            citadelObj.name = $"Citadel_{data.ownerName}_{data.citadelType}";
            citadelObj.transform.localScale = Vector3.one * citadelScale * (1f + data.level * 0.1f);
            
            // Add interaction
            var interactable = citadelObj.AddComponent<CitadelInteractable>();
            interactable.Initialize(data.id);
            
            return citadelObj;
        }
        
        private GameObject CreatePlaceholderCitadel(CitadelData data, Vector3 position)
        {
            GameObject citadel = new GameObject("PlaceholderCitadel");
            citadel.transform.position = position;
            citadel.transform.SetParent(_citadelContainer);
            
            // Create main tower
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.transform.SetParent(citadel.transform);
            tower.transform.localPosition = Vector3.up * 5f;
            tower.transform.localScale = new Vector3(5f, 10f, 5f);
            
            // Create walls
            for (int i = 0; i < 4; i++)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.SetParent(citadel.transform);
                
                float angle = i * 90f * Mathf.Deg2Rad;
                wall.transform.localPosition = new Vector3(Mathf.Cos(angle) * 8f, 2f, Mathf.Sin(angle) * 8f);
                wall.transform.localRotation = Quaternion.Euler(0, i * 90f, 0);
                wall.transform.localScale = new Vector3(16f, 4f, 1f);
            }
            
            // Corner towers
            for (int i = 0; i < 4; i++)
            {
                GameObject cornerTower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cornerTower.transform.SetParent(citadel.transform);
                
                float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                cornerTower.transform.localPosition = new Vector3(Mathf.Cos(angle) * 11f, 3f, Mathf.Sin(angle) * 11f);
                cornerTower.transform.localScale = new Vector3(2f, 6f, 2f);
            }
            
            return citadel;
        }
        
        private CitadelPrefabSet GetPrefabSet(CitadelType type)
        {
            if (citadelPrefabs == null) return null;
            
            foreach (var set in citadelPrefabs)
            {
                if (set.type == type) return set;
            }
            return null;
        }
        
        #endregion
        
        #region Visual Effects
        
        private void AddGlowEffect(RenderedCitadel citadel)
        {
            Material glowMat = citadel.ownership switch
            {
                CitadelOwnership.Player => playerGlowMaterial,
                CitadelOwnership.Ally => allyGlowMaterial,
                CitadelOwnership.Enemy => enemyGlowMaterial,
                _ => null
            };
            
            if (glowMat == null) return;
            
            // Add glow ring at base
            GameObject glowRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glowRing.name = "GlowRing";
            glowRing.transform.SetParent(citadel.gameObject.transform);
            glowRing.transform.localPosition = Vector3.up * 0.1f;
            glowRing.transform.localScale = new Vector3(15f, 0.1f, 15f);
            
            var renderer = glowRing.GetComponent<MeshRenderer>();
            renderer.material = glowMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            Destroy(glowRing.GetComponent<Collider>());
            
            citadel.glowObject = glowRing;
        }
        
        private void AddBanner(RenderedCitadel citadel)
        {
            if (bannerPrefab == null)
            {
                // Create simple banner
                GameObject banner = new GameObject("Banner");
                banner.transform.SetParent(citadel.gameObject.transform);
                banner.transform.localPosition = Vector3.up * bannerHeight;
                
                // Pole
                GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.transform.SetParent(banner.transform);
                pole.transform.localPosition = Vector3.zero;
                pole.transform.localScale = new Vector3(0.2f, 5f, 0.2f);
                Destroy(pole.GetComponent<Collider>());
                
                // Flag
                GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Quad);
                flag.transform.SetParent(banner.transform);
                flag.transform.localPosition = new Vector3(1.5f, 4f, 0);
                flag.transform.localScale = new Vector3(3f, 2f, 1f);
                Destroy(flag.GetComponent<Collider>());
                
                // Color based on ownership
                Color bannerColor = citadel.ownership switch
                {
                    CitadelOwnership.Player => Color.blue,
                    CitadelOwnership.Ally => Color.green,
                    CitadelOwnership.Enemy => Color.red,
                    _ => Color.gray
                };
                flag.GetComponent<MeshRenderer>().material.color = bannerColor;
                
                citadel.bannerObject = banner;
            }
            else
            {
                citadel.bannerObject = Instantiate(bannerPrefab, citadel.gameObject.transform);
                citadel.bannerObject.transform.localPosition = Vector3.up * bannerHeight;
            }
        }
        
        private void SetCitadelSelected(RenderedCitadel citadel, bool selected)
        {
            if (selected)
            {
                if (citadel.selectionIndicator == null && selectionIndicatorPrefab != null)
                {
                    citadel.selectionIndicator = Instantiate(selectionIndicatorPrefab, citadel.gameObject.transform);
                    citadel.selectionIndicator.transform.localPosition = Vector3.up * 0.2f;
                }
                else if (citadel.selectionIndicator != null)
                {
                    citadel.selectionIndicator.SetActive(true);
                }
            }
            else
            {
                if (citadel.selectionIndicator != null)
                {
                    citadel.selectionIndicator.SetActive(false);
                }
            }
        }
        
        private void UpdateCitadelVisuals(RenderedCitadel citadel)
        {
            // Update ownership colors if changed
            CitadelOwnership newOwnership = DetermineOwnership(citadel.data.ownerId, citadel.data.allianceId);
            
            if (newOwnership != citadel.ownership)
            {
                citadel.ownership = newOwnership;
                
                // Update glow
                if (citadel.glowObject != null)
                {
                    Destroy(citadel.glowObject);
                }
                AddGlowEffect(citadel);
                
                // Update banner color
                if (citadel.bannerObject != null)
                {
                    var flagRenderer = citadel.bannerObject.GetComponentInChildren<MeshRenderer>();
                    if (flagRenderer != null)
                    {
                        Color bannerColor = citadel.ownership switch
                        {
                            CitadelOwnership.Player => Color.blue,
                            CitadelOwnership.Ally => Color.green,
                            CitadelOwnership.Enemy => Color.red,
                            _ => Color.gray
                        };
                        flagRenderer.material.color = bannerColor;
                    }
                }
            }
        }
        
        #endregion
        
        #region Territory Borders
        
        private void CreateTerritoryBorder(RenderedCitadel citadel, double originLat, double originLon, Vector3 worldOrigin)
        {
            if (citadel.data.territoryPolygon == null || citadel.data.territoryPolygon.Count < 3)
                return;
            
            GameObject borderObj = new GameObject($"TerritoryBorder_{citadel.data.id}");
            borderObj.transform.SetParent(transform);
            
            LineRenderer lineRenderer = borderObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = citadel.data.territoryPolygon.Count + 1;
            lineRenderer.loop = true;
            lineRenderer.startWidth = territoryBorderWidth;
            lineRenderer.endWidth = territoryBorderWidth;
            
            if (territoryBorderMaterial != null)
            {
                lineRenderer.material = new Material(territoryBorderMaterial);
            }
            
            Color borderColor = citadel.ownership switch
            {
                CitadelOwnership.Player => playerTerritoryColor,
                CitadelOwnership.Ally => allyTerritoryColor,
                CitadelOwnership.Enemy => enemyTerritoryColor,
                _ => Color.gray
            };
            lineRenderer.startColor = borderColor;
            lineRenderer.endColor = borderColor;
            
            // Set positions
            for (int i = 0; i < citadel.data.territoryPolygon.Count; i++)
            {
                var point = citadel.data.territoryPolygon[i];
                Vector3 worldPos = GeoToWorld(point.latitude, point.longitude, originLat, originLon, worldOrigin);
                worldPos.y = 0.5f; // Slightly above ground
                lineRenderer.SetPosition(i, worldPos);
            }
            
            // Close the loop
            var firstPoint = citadel.data.territoryPolygon[0];
            Vector3 closePos = GeoToWorld(firstPoint.latitude, firstPoint.longitude, originLat, originLon, worldOrigin);
            closePos.y = 0.5f;
            lineRenderer.SetPosition(citadel.data.territoryPolygon.Count, closePos);
            
            _territoryBorders[citadel.data.id] = borderObj;
        }
        
        #endregion
        
        #region LOD
        
        private void SetCitadelDetailLevel(RenderedCitadel citadel, CitadelDetailLevel level)
        {
            if (citadel.currentDetailLevel == level) return;
            citadel.currentDetailLevel = level;
            
            bool showBanner = level != CitadelDetailLevel.IconOnly;
            bool showGlow = level == CitadelDetailLevel.High || level == CitadelDetailLevel.Medium;
            bool showDetails = level == CitadelDetailLevel.High;
            
            if (citadel.bannerObject != null)
                citadel.bannerObject.SetActive(showBanner);
            
            if (citadel.glowObject != null)
                citadel.glowObject.SetActive(showGlow);
            
            // Hide main model at icon-only distance
            if (citadel.gameObject != null)
            {
                foreach (var renderer in citadel.gameObject.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!renderer.name.Contains("Icon"))
                    {
                        renderer.enabled = level != CitadelDetailLevel.IconOnly;
                    }
                }
            }
        }
        
        #endregion
        
        #region Utilities
        
        private CitadelOwnership DetermineOwnership(string ownerId, string allianceId)
        {
            if (ownerId == _currentPlayerId)
                return CitadelOwnership.Player;
            
            if (!string.IsNullOrEmpty(_currentAllianceId) && allianceId == _currentAllianceId)
                return CitadelOwnership.Ally;
            
            return CitadelOwnership.Enemy;
        }
        
        private bool ShouldRenderCitadel(CitadelOwnership ownership)
        {
            return ownership switch
            {
                CitadelOwnership.Player => renderPlayerCitadels,
                CitadelOwnership.Ally => renderAllyCitadels,
                CitadelOwnership.Enemy => renderEnemyCitadels,
                _ => true
            };
        }
        
        private Vector3 GeoToWorld(double lat, double lon, double originLat, double originLon, Vector3 worldOrigin)
        {
            double latDiff = lat - originLat;
            double lonDiff = lon - originLon;
            
            float metersNorth = (float)(latDiff * 111320);
            float metersEast = (float)(lonDiff * 111320 * Math.Cos(originLat * Math.PI / 180));
            
            return worldOrigin + new Vector3(metersEast, 0, metersNorth);
        }
        
        private async Task<List<CitadelData>> FetchCitadelsFromBackend(double minLat, double maxLat, double minLon, double maxLon)
        {
            // Simulated backend fetch
            await Task.Delay(100);
            
            // Return sample citadels for testing
            return new List<CitadelData>
            {
                new CitadelData
                {
                    id = "citadel_001",
                    ownerId = _currentPlayerId,
                    ownerName = "Player",
                    citadelType = CitadelType.Fortress,
                    level = 5,
                    latitude = (minLat + maxLat) / 2,
                    longitude = (minLon + maxLon) / 2
                }
            };
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    [Serializable]
    public class RenderedCitadel
    {
        public CitadelData data;
        public GameObject gameObject;
        public CitadelOwnership ownership;
        public Vector3 worldPosition;
        public CitadelDetailLevel currentDetailLevel;
        
        public GameObject glowObject;
        public GameObject bannerObject;
        public GameObject selectionIndicator;
        public GameObject captureProgressIndicator;
    }
    
    [Serializable]
    public class CitadelData
    {
        public string id;
        public string ownerId;
        public string ownerName;
        public string allianceId;
        public CitadelType citadelType;
        public int level;
        public double latitude;
        public double longitude;
        public List<Vector2d> territoryPolygon;
        public int garrisonStrength;
        public float healthPercent;
    }
    
    [Serializable]
    public class CitadelPrefabSet
    {
        public CitadelType type;
        public GameObject[] levelPrefabs;
    }
    
    public enum CitadelType
    {
        Outpost,
        Tower,
        Keep,
        Fortress,
        Castle,
        Citadel
    }
    
    public enum CitadelOwnership
    {
        Player,
        Ally,
        Enemy,
        Neutral
    }
    
    public enum CitadelDetailLevel
    {
        High,
        Medium,
        Low,
        IconOnly
    }
    
    /// <summary>
    /// Component for citadel interaction
    /// </summary>
    public class CitadelInteractable : MonoBehaviour
    {
        private string _citadelId;
        
        public void Initialize(string citadelId)
        {
            _citadelId = citadelId;
        }
        
        public void OnClick()
        {
            PlayerCitadelRenderer.Instance?.SelectCitadel(_citadelId);
        }
    }
    
    #endregion
}
