using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Fantasy Road Renderer - Transforms OSM road data into medieval paths, 
    /// cobblestone streets, royal highways, and dirt trails.
    /// Uses procedural mesh generation with spline-based road placement.
    /// </summary>
    public class FantasyRoadRenderer : MonoBehaviour
    {
        [Header("Road Materials")]
        [SerializeField] private Material cobbleStoneMaterial;
        [SerializeField] private Material dirtPathMaterial;
        [SerializeField] private Material royalHighwayMaterial;
        [SerializeField] private Material bridgeMaterial;
        [SerializeField] private Material forestTrailMaterial;
        [SerializeField] private Material marketStreetMaterial;
        
        [Header("Road Widths")]
        [SerializeField] private float royalHighwayWidth = 12f;
        [SerializeField] private float mainRoadWidth = 8f;
        [SerializeField] private float sideStreetWidth = 5f;
        [SerializeField] private float alleyWidth = 3f;
        [SerializeField] private float pathWidth = 2f;
        [SerializeField] private float trailWidth = 1.5f;
        
        [Header("Road Decoration")]
        [SerializeField] private bool addCurbstones = true;
        [SerializeField] private bool addStreetLamps = true;
        [SerializeField] private bool addMilestones = true;
        [SerializeField] private bool addBanners = true;
        [SerializeField] private float lampSpacing = 20f;
        [SerializeField] private float milestoneSpacing = 100f;
        
        [Header("Decoration Prefabs")]
        [SerializeField] private GameObject streetLampPrefab;
        [SerializeField] private GameObject milestonePrefab;
        [SerializeField] private GameObject bannerPrefab;
        [SerializeField] private GameObject cartPrefab;
        [SerializeField] private GameObject barrelPrefab;
        [SerializeField] private GameObject cratePrefab;
        
        [Header("Road Settings")]
        [SerializeField] private float roadHeight = 0.1f;
        [SerializeField] private int roadSegmentResolution = 5;
        [SerializeField] private float uvTileSize = 5f;
        [SerializeField] private bool smoothCorners = true;
        [SerializeField] private float cornerSmoothRadius = 2f;
        
        [Header("LOD Settings")]
        [SerializeField] private float decorationCullDistance = 200f;
        [SerializeField] private float roadSimplifyDistance = 500f;
        
        // Singleton
        private static FantasyRoadRenderer _instance;
        public static FantasyRoadRenderer Instance => _instance;
        
        // Generated roads
        private Dictionary<long, GeneratedRoad> _generatedRoads = new Dictionary<long, GeneratedRoad>();
        private Transform _roadContainer;
        private Transform _decorationContainer;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            _roadContainer = new GameObject("Roads").transform;
            _roadContainer.SetParent(transform);
            
            _decorationContainer = new GameObject("RoadDecorations").transform;
            _decorationContainer.SetParent(transform);
        }
        
        #region Public API
        
        /// <summary>
        /// Render all roads from OSM data
        /// </summary>
        public void RenderRoads(OSMAreaData areaData, Vector3 worldOrigin, float metersPerUnit = 1f)
        {
            if (areaData == null || areaData.roads == null) return;
            
            foreach (var road in areaData.roads)
            {
                if (_generatedRoads.ContainsKey(road.id)) continue;
                
                var generatedRoad = RenderRoad(road, areaData.centerLatitude, areaData.centerLongitude, worldOrigin, metersPerUnit);
                if (generatedRoad != null)
                {
                    _generatedRoads[road.id] = generatedRoad;
                }
            }
        }
        
        /// <summary>
        /// Render a single road
        /// </summary>
        public GeneratedRoad RenderRoad(OSMRoad osmRoad, double originLat, double originLon, Vector3 worldOrigin, float metersPerUnit)
        {
            if (osmRoad.points == null || osmRoad.points.Count < 2)
                return null;
            
            // Convert to world coordinates
            List<Vector3> worldPoints = new List<Vector3>();
            foreach (var point in osmRoad.points)
            {
                Vector3 worldPos = GeoToWorld(point.latitude, point.longitude, originLat, originLon, worldOrigin, metersPerUnit);
                worldPoints.Add(worldPos);
            }
            
            // Smooth corners if enabled
            if (smoothCorners && worldPoints.Count > 2)
            {
                worldPoints = SmoothPath(worldPoints, cornerSmoothRadius);
            }
            
            // Create road object
            GameObject roadObj = new GameObject($"Road_{osmRoad.id}_{osmRoad.name}");
            roadObj.transform.SetParent(_roadContainer);
            
            var road = new GeneratedRoad
            {
                osmId = osmRoad.id,
                name = osmRoad.name,
                gameObject = roadObj,
                fantasyStyle = osmRoad.fantasyStyle,
                worldPoints = worldPoints
            };
            
            // Generate road mesh
            float width = GetRoadWidth(osmRoad.fantasyStyle);
            Material material = GetRoadMaterial(osmRoad.fantasyStyle);
            
            CreateRoadMesh(road, worldPoints, width, material);
            
            // Add decorations
            if (ShouldHaveDecorations(osmRoad.fantasyStyle))
            {
                AddRoadDecorations(road, worldPoints, osmRoad.fantasyStyle);
            }
            
            return road;
        }
        
        /// <summary>
        /// Clear all roads
        /// </summary>
        public void ClearRoads()
        {
            foreach (var kvp in _generatedRoads)
            {
                if (kvp.Value.gameObject != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            _generatedRoads.Clear();
            
            // Clear decorations
            foreach (Transform child in _decorationContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        /// <summary>
        /// Update decoration visibility based on camera
        /// </summary>
        public void UpdateDecorationLOD(Vector3 cameraPosition)
        {
            foreach (Transform decoration in _decorationContainer)
            {
                float distance = Vector3.Distance(cameraPosition, decoration.position);
                decoration.gameObject.SetActive(distance < decorationCullDistance);
            }
        }
        
        #endregion
        
        #region Mesh Generation
        
        private void CreateRoadMesh(GeneratedRoad road, List<Vector3> points, float width, Material material)
        {
            MeshFilter meshFilter = road.gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = road.gameObject.AddComponent<MeshRenderer>();
            
            Mesh mesh = GenerateRoadMesh(points, width);
            meshFilter.mesh = mesh;
            meshRenderer.material = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = true;
            
            road.mesh = mesh;
        }
        
        private Mesh GenerateRoadMesh(List<Vector3> centerLine, float width)
        {
            Mesh mesh = new Mesh();
            mesh.name = "RoadMesh";
            
            int pointCount = centerLine.Count;
            int vertexCount = pointCount * 2;
            
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[(pointCount - 1) * 6];
            
            float totalLength = 0;
            List<float> lengths = new List<float> { 0 };
            
            for (int i = 1; i < pointCount; i++)
            {
                totalLength += Vector3.Distance(centerLine[i], centerLine[i - 1]);
                lengths.Add(totalLength);
            }
            
            // Generate vertices along the path
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 point = centerLine[i];
                Vector3 forward, right;
                
                if (i == 0)
                {
                    forward = (centerLine[1] - centerLine[0]).normalized;
                }
                else if (i == pointCount - 1)
                {
                    forward = (centerLine[i] - centerLine[i - 1]).normalized;
                }
                else
                {
                    forward = ((centerLine[i + 1] - centerLine[i]).normalized + 
                              (centerLine[i] - centerLine[i - 1]).normalized).normalized;
                }
                
                right = Vector3.Cross(Vector3.up, forward).normalized;
                
                // Left and right vertices
                vertices[i * 2] = point - right * (width / 2) + Vector3.up * roadHeight;
                vertices[i * 2 + 1] = point + right * (width / 2) + Vector3.up * roadHeight;
                
                // UV coordinates
                float v = lengths[i] / uvTileSize;
                uvs[i * 2] = new Vector2(0, v);
                uvs[i * 2 + 1] = new Vector2(1, v);
            }
            
            // Generate triangles
            for (int i = 0; i < pointCount - 1; i++)
            {
                int baseIndex = i * 6;
                int vertBase = i * 2;
                
                // First triangle
                triangles[baseIndex] = vertBase;
                triangles[baseIndex + 1] = vertBase + 2;
                triangles[baseIndex + 2] = vertBase + 1;
                
                // Second triangle
                triangles[baseIndex + 3] = vertBase + 1;
                triangles[baseIndex + 4] = vertBase + 2;
                triangles[baseIndex + 5] = vertBase + 3;
            }
            
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        private List<Vector3> SmoothPath(List<Vector3> points, float radius)
        {
            if (points.Count < 3) return points;
            
            List<Vector3> smoothed = new List<Vector3>();
            smoothed.Add(points[0]);
            
            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector3 prev = points[i - 1];
                Vector3 curr = points[i];
                Vector3 next = points[i + 1];
                
                Vector3 toPrev = (prev - curr).normalized;
                Vector3 toNext = (next - curr).normalized;
                
                float angle = Vector3.Angle(toPrev, toNext);
                
                if (angle < 170f) // Sharp corner
                {
                    float actualRadius = Mathf.Min(radius, 
                        Vector3.Distance(prev, curr) / 2,
                        Vector3.Distance(next, curr) / 2);
                    
                    // Add intermediate points for smoothing
                    Vector3 start = curr + toPrev * actualRadius;
                    Vector3 end = curr + toNext * actualRadius;
                    
                    int segments = Mathf.Max(2, Mathf.FloorToInt(angle / 30f));
                    for (int s = 0; s <= segments; s++)
                    {
                        float t = s / (float)segments;
                        // Quadratic bezier
                        Vector3 p = Vector3.Lerp(Vector3.Lerp(start, curr, t), Vector3.Lerp(curr, end, t), t);
                        smoothed.Add(p);
                    }
                }
                else
                {
                    smoothed.Add(curr);
                }
            }
            
            smoothed.Add(points[points.Count - 1]);
            
            return smoothed;
        }
        
        #endregion
        
        #region Decorations
        
        private void AddRoadDecorations(GeneratedRoad road, List<Vector3> points, FantasyRoadStyle style)
        {
            float pathLength = CalculatePathLength(points);
            
            // Street lamps
            if (addStreetLamps && style is FantasyRoadStyle.RoyalHighway or FantasyRoadStyle.CobblestoneStreet)
            {
                AddStreetLamps(road, points, pathLength);
            }
            
            // Milestones
            if (addMilestones && style == FantasyRoadStyle.RoyalHighway)
            {
                AddMilestones(road, points, pathLength);
            }
            
            // Banners
            if (addBanners && style == FantasyRoadStyle.RoyalHighway)
            {
                AddBanners(road, points, pathLength);
            }
            
            // Market street decorations
            if (style == FantasyRoadStyle.MarketSquare)
            {
                AddMarketDecorations(road, points);
            }
            
            // Curbstones
            if (addCurbstones && style is FantasyRoadStyle.CobblestoneStreet or FantasyRoadStyle.RoyalHighway)
            {
                AddCurbstones(road, points, GetRoadWidth(style));
            }
        }
        
        private void AddStreetLamps(GeneratedRoad road, List<Vector3> points, float pathLength)
        {
            int lampCount = Mathf.FloorToInt(pathLength / lampSpacing);
            
            for (int i = 0; i <= lampCount; i++)
            {
                float t = i / (float)Mathf.Max(1, lampCount);
                Vector3 position = GetPointOnPath(points, t);
                Vector3 forward = GetForwardOnPath(points, t);
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                
                // Lamps on both sides
                PlaceDecoration(streetLampPrefab, position + right * (GetRoadWidth(road.fantasyStyle) / 2 + 0.5f), 
                    Quaternion.LookRotation(-right), "StreetLamp");
                PlaceDecoration(streetLampPrefab, position - right * (GetRoadWidth(road.fantasyStyle) / 2 + 0.5f), 
                    Quaternion.LookRotation(right), "StreetLamp");
            }
        }
        
        private void AddMilestones(GeneratedRoad road, List<Vector3> points, float pathLength)
        {
            int count = Mathf.FloorToInt(pathLength / milestoneSpacing);
            
            for (int i = 1; i <= count; i++)
            {
                float t = i * milestoneSpacing / pathLength;
                Vector3 position = GetPointOnPath(points, t);
                Vector3 forward = GetForwardOnPath(points, t);
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                
                PlaceDecoration(milestonePrefab, position + right * (GetRoadWidth(road.fantasyStyle) / 2 + 1f), 
                    Quaternion.LookRotation(forward), "Milestone");
            }
        }
        
        private void AddBanners(GeneratedRoad road, List<Vector3> points, float pathLength)
        {
            int count = Mathf.FloorToInt(pathLength / (lampSpacing * 2));
            
            for (int i = 0; i <= count; i++)
            {
                float t = i / (float)Mathf.Max(1, count);
                Vector3 position = GetPointOnPath(points, t);
                Vector3 forward = GetForwardOnPath(points, t);
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                
                // Banner poles on alternating sides
                Vector3 offset = (i % 2 == 0 ? right : -right) * (GetRoadWidth(road.fantasyStyle) / 2 + 0.5f);
                PlaceDecoration(bannerPrefab, position + offset + Vector3.up * 4f, 
                    Quaternion.LookRotation(forward), "Banner");
            }
        }
        
        private void AddMarketDecorations(GeneratedRoad road, List<Vector3> points)
        {
            // Add random market props
            for (int i = 0; i < points.Count - 1; i++)
            {
                if (UnityEngine.Random.value > 0.3f) continue;
                
                Vector3 forward = (points[i + 1] - points[i]).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                
                float side = UnityEngine.Random.value > 0.5f ? 1 : -1;
                Vector3 position = points[i] + right * side * (GetRoadWidth(road.fantasyStyle) / 2 - 0.5f);
                
                GameObject prefab = UnityEngine.Random.value > 0.5f ? barrelPrefab : cratePrefab;
                if (prefab != null)
                {
                    PlaceDecoration(prefab, position, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0), "MarketProp");
                }
            }
        }
        
        private void AddCurbstones(GeneratedRoad road, List<Vector3> points, float width)
        {
            // Create curb mesh along road edges
            GameObject leftCurb = new GameObject("LeftCurb");
            leftCurb.transform.SetParent(road.gameObject.transform);
            
            GameObject rightCurb = new GameObject("RightCurb");
            rightCurb.transform.SetParent(road.gameObject.transform);
            
            List<Vector3> leftEdge = new List<Vector3>();
            List<Vector3> rightEdge = new List<Vector3>();
            
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 forward;
                if (i == 0)
                    forward = (points[1] - points[0]).normalized;
                else if (i == points.Count - 1)
                    forward = (points[i] - points[i - 1]).normalized;
                else
                    forward = ((points[i + 1] - points[i]).normalized + (points[i] - points[i - 1]).normalized).normalized;
                
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                leftEdge.Add(points[i] - right * (width / 2));
                rightEdge.Add(points[i] + right * (width / 2));
            }
            
            CreateCurbMesh(leftCurb, leftEdge, true);
            CreateCurbMesh(rightCurb, rightEdge, false);
        }
        
        private void CreateCurbMesh(GameObject curbObj, List<Vector3> edgePoints, bool isLeft)
        {
            MeshFilter mf = curbObj.AddComponent<MeshFilter>();
            MeshRenderer mr = curbObj.AddComponent<MeshRenderer>();
            
            float curbWidth = 0.2f;
            float curbHeight = 0.15f;
            
            Mesh mesh = new Mesh();
            int n = edgePoints.Count;
            
            Vector3[] vertices = new Vector3[n * 4];
            int[] triangles = new int[(n - 1) * 24];
            
            for (int i = 0; i < n; i++)
            {
                Vector3 forward;
                if (i == 0)
                    forward = (edgePoints[1] - edgePoints[0]).normalized;
                else if (i == n - 1)
                    forward = (edgePoints[i] - edgePoints[i - 1]).normalized;
                else
                    forward = ((edgePoints[i + 1] - edgePoints[i]) + (edgePoints[i] - edgePoints[i - 1])).normalized;
                
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                float sign = isLeft ? -1 : 1;
                
                vertices[i * 4] = edgePoints[i] + Vector3.up * roadHeight;
                vertices[i * 4 + 1] = edgePoints[i] + right * sign * curbWidth + Vector3.up * roadHeight;
                vertices[i * 4 + 2] = edgePoints[i] + right * sign * curbWidth + Vector3.up * (roadHeight + curbHeight);
                vertices[i * 4 + 3] = edgePoints[i] + Vector3.up * (roadHeight + curbHeight);
            }
            
            // Triangles for curb segments
            for (int i = 0; i < n - 1; i++)
            {
                int b = i * 24;
                int v = i * 4;
                
                // Bottom face
                triangles[b] = v; triangles[b + 1] = v + 4; triangles[b + 2] = v + 1;
                triangles[b + 3] = v + 1; triangles[b + 4] = v + 4; triangles[b + 5] = v + 5;
                
                // Outer face
                triangles[b + 6] = v + 1; triangles[b + 7] = v + 5; triangles[b + 8] = v + 2;
                triangles[b + 9] = v + 2; triangles[b + 10] = v + 5; triangles[b + 11] = v + 6;
                
                // Top face
                triangles[b + 12] = v + 2; triangles[b + 13] = v + 6; triangles[b + 14] = v + 3;
                triangles[b + 15] = v + 3; triangles[b + 16] = v + 6; triangles[b + 17] = v + 7;
                
                // Inner face
                triangles[b + 18] = v + 3; triangles[b + 19] = v + 7; triangles[b + 20] = v;
                triangles[b + 21] = v; triangles[b + 22] = v + 7; triangles[b + 23] = v + 4;
            }
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            mf.mesh = mesh;
            mr.material = cobbleStoneMaterial != null ? cobbleStoneMaterial : new Material(Shader.Find("Standard"));
        }
        
        private void PlaceDecoration(GameObject prefab, Vector3 position, Quaternion rotation, string name)
        {
            if (prefab == null)
            {
                // Create placeholder
                GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                placeholder.name = name;
                placeholder.transform.SetParent(_decorationContainer);
                placeholder.transform.position = position;
                placeholder.transform.rotation = rotation;
                placeholder.transform.localScale = new Vector3(0.3f, 2f, 0.3f);
                Destroy(placeholder.GetComponent<Collider>());
                return;
            }
            
            GameObject obj = Instantiate(prefab, position, rotation, _decorationContainer);
            obj.name = name;
        }
        
        #endregion
        
        #region Utilities
        
        private float GetRoadWidth(FantasyRoadStyle style)
        {
            return style switch
            {
                FantasyRoadStyle.RoyalHighway => royalHighwayWidth,
                FantasyRoadStyle.CobblestoneStreet => mainRoadWidth,
                FantasyRoadStyle.MarketSquare => mainRoadWidth,
                FantasyRoadStyle.Alley => alleyWidth,
                FantasyRoadStyle.DirtPath => pathWidth,
                FantasyRoadStyle.ForestTrail => trailWidth,
                _ => sideStreetWidth
            };
        }
        
        private Material GetRoadMaterial(FantasyRoadStyle style)
        {
            Material mat = style switch
            {
                FantasyRoadStyle.RoyalHighway => royalHighwayMaterial,
                FantasyRoadStyle.CobblestoneStreet => cobbleStoneMaterial,
                FantasyRoadStyle.MarketSquare => marketStreetMaterial,
                FantasyRoadStyle.DirtPath => dirtPathMaterial,
                FantasyRoadStyle.ForestTrail => forestTrailMaterial,
                FantasyRoadStyle.Bridge => bridgeMaterial,
                _ => cobbleStoneMaterial
            };
            
            return mat != null ? mat : new Material(Shader.Find("Standard"));
        }
        
        private bool ShouldHaveDecorations(FantasyRoadStyle style)
        {
            return style is FantasyRoadStyle.RoyalHighway or 
                   FantasyRoadStyle.CobblestoneStreet or 
                   FantasyRoadStyle.MarketSquare;
        }
        
        private float CalculatePathLength(List<Vector3> points)
        {
            float length = 0;
            for (int i = 1; i < points.Count; i++)
            {
                length += Vector3.Distance(points[i], points[i - 1]);
            }
            return length;
        }
        
        private Vector3 GetPointOnPath(List<Vector3> points, float t)
        {
            if (points.Count < 2) return points[0];
            
            float totalLength = CalculatePathLength(points);
            float targetLength = t * totalLength;
            float currentLength = 0;
            
            for (int i = 1; i < points.Count; i++)
            {
                float segmentLength = Vector3.Distance(points[i], points[i - 1]);
                if (currentLength + segmentLength >= targetLength)
                {
                    float segmentT = (targetLength - currentLength) / segmentLength;
                    return Vector3.Lerp(points[i - 1], points[i], segmentT);
                }
                currentLength += segmentLength;
            }
            
            return points[points.Count - 1];
        }
        
        private Vector3 GetForwardOnPath(List<Vector3> points, float t)
        {
            if (points.Count < 2) return Vector3.forward;
            
            float totalLength = CalculatePathLength(points);
            float targetLength = t * totalLength;
            float currentLength = 0;
            
            for (int i = 1; i < points.Count; i++)
            {
                float segmentLength = Vector3.Distance(points[i], points[i - 1]);
                if (currentLength + segmentLength >= targetLength)
                {
                    return (points[i] - points[i - 1]).normalized;
                }
                currentLength += segmentLength;
            }
            
            return (points[points.Count - 1] - points[points.Count - 2]).normalized;
        }
        
        private Vector3 GeoToWorld(double lat, double lon, double originLat, double originLon, Vector3 worldOrigin, float metersPerUnit)
        {
            double latDiff = lat - originLat;
            double lonDiff = lon - originLon;
            
            float metersNorth = (float)(latDiff * 111320);
            float metersEast = (float)(lonDiff * 111320 * Math.Cos(originLat * Math.PI / 180));
            
            return worldOrigin + new Vector3(metersEast / metersPerUnit, 0, metersNorth / metersPerUnit);
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    [Serializable]
    public class GeneratedRoad
    {
        public long osmId;
        public string name;
        public GameObject gameObject;
        public FantasyRoadStyle fantasyStyle;
        public List<Vector3> worldPoints;
        public Mesh mesh;
    }
    
    #endregion
}
