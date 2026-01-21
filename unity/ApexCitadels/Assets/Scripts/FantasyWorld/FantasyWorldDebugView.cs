// ============================================================================
// APEX CITADELS - FANTASY WORLD DEBUG VIEW
// Visual debugging overlay showing OSM data mapping to fantasy types
// Toggle with F3 key to see building footprints, roads, and classifications
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Debug visualization for the fantasy world generation system.
    /// Shows color-coded building footprints and road paths to verify
    /// OSM data is being correctly mapped to fantasy building types.
    /// 
    /// Toggle with F3 key or set showDebugView in Inspector.
    /// </summary>
    public class FantasyWorldDebugView : MonoBehaviour
    {
        [Header("Debug Settings")]
        [Tooltip("Toggle debug view on/off")]
        public bool showDebugView = false;
        
        [Tooltip("Key to toggle debug view")]
        public KeyCode toggleKey = KeyCode.F3;
        
        [Tooltip("Height offset for debug visuals above ground")]
        public float debugHeight = 0.5f;
        
        [Tooltip("Line width for footprints")]
        public float lineWidth = 0.3f;
        
        [Header("Building Type Colors")]
        public Color residentialColor = new Color(0.2f, 0.5f, 1f, 0.8f);      // Blue
        public Color commercialColor = new Color(0.2f, 0.9f, 0.3f, 0.8f);     // Green
        public Color industrialColor = new Color(0.9f, 0.6f, 0.1f, 0.8f);     // Orange
        public Color religiousColor = new Color(0.8f, 0.2f, 0.8f, 0.8f);      // Purple
        public Color civicColor = new Color(0.9f, 0.9f, 0.2f, 0.8f);          // Yellow
        public Color militaryColor = new Color(0.9f, 0.2f, 0.2f, 0.8f);       // Red
        public Color unknownColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);        // Gray
        
        [Header("Road Colors")]
        public Color mainRoadColor = new Color(0.4f, 0.3f, 0.2f, 0.9f);       // Brown
        public Color residentialRoadColor = new Color(0.6f, 0.5f, 0.4f, 0.8f); // Light brown
        public Color footpathColor = new Color(0.7f, 0.7f, 0.6f, 0.7f);       // Beige
        
        [Header("References")]
        public FantasyWorldGenerator generator;
        
        // Cached OSM data for visualization
        private OSMData _osmData;
        private List<GameObject> _debugObjects = new List<GameObject>();
        private GameObject _debugContainer;
        private bool _isVisualized = false;
        
        // Legend UI
        private bool _showLegend = true;
        private GUIStyle _legendStyle;
        private GUIStyle _headerStyle;
        
        private void Start()
        {
            if (generator == null)
            {
                generator = FindAnyObjectByType<FantasyWorldGenerator>();
            }
            
            // Subscribe to generation complete event
            if (generator != null)
            {
                generator.OnGenerationProgress += OnGenerationProgress;
            }
        }
        
        private void OnDestroy()
        {
            if (generator != null)
            {
                generator.OnGenerationProgress -= OnGenerationProgress;
            }
            ClearDebugVisuals();
        }
        
        private void Update()
        {
            // Toggle debug view
            if (Input.GetKeyDown(toggleKey))
            {
                showDebugView = !showDebugView;
                
                if (showDebugView)
                {
                    // Always recreate debug visuals when enabling - buildings may have changed
                    CreateDebugVisuals();
                }
                else if (_debugContainer != null)
                {
                    _debugContainer.SetActive(false);
                }
                
                Debug.Log($"[DebugView] Debug visualization {(showDebugView ? "ENABLED" : "DISABLED")} - Press {toggleKey} to toggle");
            }
            
            // Toggle legend with L key when debug view is active
            if (showDebugView && Input.GetKeyDown(KeyCode.L))
            {
                _showLegend = !_showLegend;
            }
        }
        
        private void OnGenerationProgress(string message)
        {
            if (message == "World generation complete!")
            {
                // Refresh debug visuals when world regenerates
                if (showDebugView)
                {
                    CreateDebugVisuals();
                }
            }
        }
        
        /// <summary>
        /// Set OSM data for visualization (called by generator)
        /// </summary>
        public void SetOSMData(OSMData data)
        {
            _osmData = data;
            if (showDebugView)
            {
                CreateDebugVisuals();
            }
        }
        
        /// <summary>
        /// Create all debug visualization objects
        /// </summary>
        public void CreateDebugVisuals()
        {
            ClearDebugVisuals();
            
            // Create container
            _debugContainer = new GameObject("DebugVisualization");
            _debugContainer.transform.SetParent(transform);
            _debugContainer.transform.localPosition = Vector3.zero;
            
            // Try to get OSM data from generator's current state
            if (_osmData == null && generator != null)
            {
                // We need to access the generator's cached data
                // For now, we'll visualize from the generated objects
                VisualizeFromGeneratedObjects();
            }
            else if (_osmData != null)
            {
                VisualizeFromOSMData();
            }
            
            _isVisualized = true;
            _debugContainer.SetActive(showDebugView);
            
            Debug.Log($"[DebugView] Created debug visualization with {_debugObjects.Count} objects");
        }
        
        /// <summary>
        /// Visualize by reading the FantasyBuildingMeta components on generated buildings
        /// </summary>
        private void VisualizeFromGeneratedObjects()
        {
            // Find all buildings with metadata
            var allMeta = FindObjectsByType<FantasyBuildingMeta>(FindObjectsSortMode.None);
            Debug.Log($"[DebugView] Found {allMeta.Length} buildings with FantasyBuildingMeta components");
            
            int buildingCount = 0;
            foreach (var meta in allMeta)
            {
                // Create footprint outline based on building bounds
                var renderer = meta.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    CreateBuildingFootprint(meta.transform.position, renderer.bounds, meta.BuildingType, meta.OriginalOSMType);
                    buildingCount++;
                }
            }
            
            // Visualize roads from paths parent
            if (generator != null && generator.pathsParent != null)
            {
                int roadCount = 0;
                foreach (Transform roadTransform in generator.pathsParent)
                {
                    var meshFilter = roadTransform.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        CreateRoadDebugOverlay(roadTransform, meshFilter.sharedMesh);
                        roadCount++;
                    }
                }
                Debug.Log($"[DebugView] Visualized {roadCount} roads");
            }
            
            Debug.Log($"[DebugView] Visualized {buildingCount} buildings from generated objects");
        }
        
        /// <summary>
        /// Visualize directly from OSM data
        /// </summary>
        private void VisualizeFromOSMData()
        {
            if (_osmData == null) return;
            
            // Visualize buildings
            foreach (var building in _osmData.Buildings)
            {
                if (building.WorldPoints != null && building.WorldPoints.Count >= 3)
                {
                    CreateBuildingFootprintFromPoints(building.WorldPoints, building.BuildingType);
                }
            }
            
            // Visualize roads
            foreach (var road in _osmData.Roads)
            {
                if (road.Points != null && road.Points.Count >= 2)
                {
                    CreateRoadLine(road.Points, road.RoadType, road.Width);
                }
            }
        }
        
        /// <summary>
        /// Create a colored footprint outline for a building
        /// </summary>
        private void CreateBuildingFootprint(Vector3 position, Bounds bounds, FantasyBuildingType fantasyType, string osmType)
        {
            var footprint = new GameObject($"Debug_Building_{fantasyType}");
            footprint.transform.SetParent(_debugContainer.transform);
            
            // Create line renderer for outline
            var lr = footprint.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            
            // Get color based on fantasy type category
            Color color = GetBuildingColor(fantasyType, osmType);
            lr.material = CreateDebugMaterial(color);
            lr.startColor = color;
            lr.endColor = color;
            
            // Create rectangle around bounds
            float y = position.y + debugHeight;
            Vector3[] corners = new Vector3[]
            {
                new Vector3(bounds.min.x, y, bounds.min.z),
                new Vector3(bounds.max.x, y, bounds.min.z),
                new Vector3(bounds.max.x, y, bounds.max.z),
                new Vector3(bounds.min.x, y, bounds.max.z)
            };
            
            lr.positionCount = 4;
            lr.SetPositions(corners);
            
            // Add label
            CreateLabel(footprint, position + Vector3.up * (debugHeight + 1f), fantasyType.ToString(), color);
            
            _debugObjects.Add(footprint);
        }
        
        /// <summary>
        /// Create footprint from actual polygon points
        /// </summary>
        private void CreateBuildingFootprintFromPoints(List<Vector3> points, string osmType)
        {
            var footprint = new GameObject($"Debug_Building_{osmType}");
            footprint.transform.SetParent(_debugContainer.transform);
            
            var lr = footprint.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            
            // Classify to get color
            var fantasyType = ClassifyOSMType(osmType);
            Color color = GetBuildingColor(fantasyType, osmType);
            lr.material = CreateDebugMaterial(color);
            lr.startColor = color;
            lr.endColor = color;
            
            // Set points with height offset
            lr.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                lr.SetPosition(i, points[i] + Vector3.up * debugHeight);
            }
            
            _debugObjects.Add(footprint);
        }
        
        /// <summary>
        /// Create road visualization overlay
        /// </summary>
        private void CreateRoadDebugOverlay(Transform roadTransform, Mesh mesh)
        {
            var debugRoad = new GameObject($"Debug_{roadTransform.name}");
            debugRoad.transform.SetParent(_debugContainer.transform);
            debugRoad.transform.position = roadTransform.position + Vector3.up * (debugHeight + 0.1f);
            debugRoad.transform.rotation = roadTransform.rotation;
            debugRoad.transform.localScale = roadTransform.localScale;
            
            // Create wireframe mesh
            var mf = debugRoad.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            
            var mr = debugRoad.AddComponent<MeshRenderer>();
            mr.material = CreateDebugMaterial(mainRoadColor);
            mr.material.SetFloat("_Surface", 1); // Transparent
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            _debugObjects.Add(debugRoad);
        }
        
        /// <summary>
        /// Create road line from points
        /// </summary>
        private void CreateRoadLine(List<Vector3> points, string roadType, float width)
        {
            var roadLine = new GameObject($"Debug_Road_{roadType}");
            roadLine.transform.SetParent(_debugContainer.transform);
            
            var lr = roadLine.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = Mathf.Max(width * 0.5f, 1f);
            lr.endWidth = Mathf.Max(width * 0.5f, 1f);
            
            Color color = GetRoadColor(roadType);
            lr.material = CreateDebugMaterial(color);
            lr.startColor = color;
            lr.endColor = color;
            
            lr.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                lr.SetPosition(i, points[i] + Vector3.up * debugHeight);
            }
            
            _debugObjects.Add(roadLine);
        }
        
        /// <summary>
        /// Create a floating text label
        /// </summary>
        private void CreateLabel(GameObject parent, Vector3 position, string text, Color color)
        {
            // Create a simple 3D text using TextMesh
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(parent.transform);
            labelObj.transform.position = position;
            labelObj.transform.localScale = Vector3.one * 0.3f;
            
            var textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 24;
            textMesh.color = color;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            
            // Make it face camera (billboard)
            var billboard = labelObj.AddComponent<BillboardText>();
        }
        
        /// <summary>
        /// Get color for a fantasy building type
        /// </summary>
        private Color GetBuildingColor(FantasyBuildingType type, string osmType)
        {
            // Group by category
            return type switch
            {
                // Residential
                FantasyBuildingType.PeasantHut or
                FantasyBuildingType.Cottage or
                FantasyBuildingType.House or
                FantasyBuildingType.Manor or
                FantasyBuildingType.NobleEstate => residentialColor,
                
                // Commercial
                FantasyBuildingType.Market or
                FantasyBuildingType.GeneralStore or
                FantasyBuildingType.Blacksmith or
                FantasyBuildingType.Tavern or
                FantasyBuildingType.Inn => commercialColor,
                
                // Industrial
                FantasyBuildingType.Workshop or
                FantasyBuildingType.Mill or
                FantasyBuildingType.Warehouse or
                FantasyBuildingType.Barn => industrialColor,
                
                // Religious
                FantasyBuildingType.Chapel or
                FantasyBuildingType.Church or
                FantasyBuildingType.Cathedral or
                FantasyBuildingType.MageTower => religiousColor,
                
                // Civic
                FantasyBuildingType.TownHall or
                FantasyBuildingType.Monument => civicColor,
                
                // Military
                FantasyBuildingType.Barracks or
                FantasyBuildingType.GuardTower or
                FantasyBuildingType.Fortress or
                FantasyBuildingType.Castle => militaryColor,
                
                _ => unknownColor
            };
        }
        
        /// <summary>
        /// Get color for road type
        /// </summary>
        private Color GetRoadColor(string roadType)
        {
            return roadType?.ToLower() switch
            {
                "primary" or "secondary" or "tertiary" => mainRoadColor,
                "residential" or "living_street" => residentialRoadColor,
                "footway" or "path" or "pedestrian" => footpathColor,
                _ => residentialRoadColor
            };
        }
        
        /// <summary>
        /// Simple classification from OSM type string
        /// </summary>
        private FantasyBuildingType ClassifyOSMType(string osmType)
        {
            var type = osmType?.ToLower() ?? "";
            
            if (type.Contains("house") || type.Contains("residential") || type.Contains("apartment"))
                return FantasyBuildingType.House;
            if (type.Contains("commercial") || type.Contains("retail") || type.Contains("shop"))
                return FantasyBuildingType.GeneralStore;
            if (type.Contains("restaurant") || type.Contains("bar") || type.Contains("cafe"))
                return FantasyBuildingType.Tavern;
            if (type.Contains("church") || type.Contains("religious"))
                return FantasyBuildingType.Church;
            if (type.Contains("industrial") || type.Contains("warehouse"))
                return FantasyBuildingType.Warehouse;
                
            return FantasyBuildingType.House; // Default
        }
        
        /// <summary>
        /// Create a simple unlit material for debug visuals
        /// </summary>
        private Material CreateDebugMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? 
                         Shader.Find("Unlit/Color") ?? 
                         Shader.Find("Standard");
                         
            var mat = new Material(shader);
            mat.color = color;
            
            // Try to make it transparent
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);
            }
            
            // Standard shader transparency
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            
            return mat;
        }
        
        /// <summary>
        /// Clear all debug visualization objects
        /// </summary>
        public void ClearDebugVisuals()
        {
            foreach (var obj in _debugObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            _debugObjects.Clear();
            
            if (_debugContainer != null)
            {
                DestroyImmediate(_debugContainer);
                _debugContainer = null;
            }
            
            _isVisualized = false;
        }
        
        /// <summary>
        /// Draw legend UI
        /// </summary>
        private void OnGUI()
        {
            if (!showDebugView || !_showLegend) return;
            
            // Initialize styles
            if (_legendStyle == null)
            {
                _legendStyle = new GUIStyle(GUI.skin.box);
                _legendStyle.fontSize = 14;
                _legendStyle.normal.textColor = Color.white;
                _legendStyle.padding = new RectOffset(10, 10, 5, 5);
                
                _headerStyle = new GUIStyle(GUI.skin.label);
                _headerStyle.fontSize = 16;
                _headerStyle.fontStyle = FontStyle.Bold;
                _headerStyle.normal.textColor = Color.white;
            }
            
            // Legend box
            float boxWidth = 220;
            float boxHeight = 280;
            float margin = 10;
            Rect legendRect = new Rect(Screen.width - boxWidth - margin, margin, boxWidth, boxHeight);
            
            GUI.Box(legendRect, "");
            GUILayout.BeginArea(legendRect);
            GUILayout.Space(10);
            
            GUILayout.Label("📍 DEBUG VIEW (F3)", _headerStyle);
            GUILayout.Label("Press L to toggle legend", GUI.skin.label);
            GUILayout.Space(10);
            
            // Building categories
            GUILayout.Label("BUILDINGS:", _headerStyle);
            DrawLegendItem("🏠 Residential", residentialColor);
            DrawLegendItem("🏪 Commercial", commercialColor);
            DrawLegendItem("🏭 Industrial", industrialColor);
            DrawLegendItem("⛪ Religious", religiousColor);
            DrawLegendItem("🏛️ Civic", civicColor);
            DrawLegendItem("⚔️ Military", militaryColor);
            
            GUILayout.Space(10);
            GUILayout.Label("ROADS:", _headerStyle);
            DrawLegendItem("━ Main Road", mainRoadColor);
            DrawLegendItem("─ Residential", residentialRoadColor);
            DrawLegendItem("╌ Footpath", footpathColor);
            
            GUILayout.EndArea();
        }
        
        private void DrawLegendItem(string label, Color color)
        {
            GUILayout.BeginHorizontal();
            
            // Color swatch
            var originalColor = GUI.color;
            GUI.color = color;
            GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(15));
            GUI.color = originalColor;
            
            GUILayout.Label(label);
            GUILayout.EndHorizontal();
        }
    }
    
    /// <summary>
    /// Simple billboard component to make text face camera
    /// </summary>
    public class BillboardText : MonoBehaviour
    {
        private Camera _camera;
        
        private void Start()
        {
            _camera = Camera.main;
        }
        
        private void LateUpdate()
        {
            if (_camera != null)
            {
                transform.LookAt(transform.position + _camera.transform.rotation * Vector3.forward,
                                 _camera.transform.rotation * Vector3.up);
            }
        }
    }
}
