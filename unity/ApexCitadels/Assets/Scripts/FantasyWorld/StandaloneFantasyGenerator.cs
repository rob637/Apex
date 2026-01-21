// ============================================================================
// APEX CITADELS - STANDALONE FANTASY KINGDOM GENERATOR
// Beautiful fantasy world generation WITHOUT GPS/Mapbox dependency
// Creates a cohesive fantasy kingdom at proper 1:1 scale
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Configuration for standalone fantasy kingdom generation
    /// </summary>
    [CreateAssetMenu(fileName = "FantasyKingdomConfig", menuName = "Apex Citadels/Fantasy Kingdom Config")]
    public class FantasyKingdomConfig : ScriptableObject
    {
        [Header("=== KINGDOM SIZE ===")]
        [Tooltip("Total kingdom size in meters (will be a square)")]
        public float kingdomSize = 500f;
        
        [Tooltip("Size of the central castle/town area")]
        public float townRadius = 150f;
        
        [Header("=== TERRAIN ===")]
        public Material groundMaterial;
        public Material grassMaterial;
        public Material dirtMaterial;
        public Material stoneMaterial;
        
        [Tooltip("Add gentle hills to terrain")]
        public bool generateHills = true;
        public float hillHeight = 15f;
        public float hillFrequency = 0.02f;
        
        [Header("=== CASTLE & WALLS ===")]
        public bool generateCastle = true;
        public bool generateWalls = true;
        public float wallRadius = 120f;
        public int wallTowerCount = 8;
        
        [Header("=== BUILDINGS ===")]
        [Range(10, 100)]
        public int residentialCount = 40;
        [Range(5, 30)]
        public int commercialCount = 15;
        [Range(2, 10)]
        public int militaryCount = 5;
        [Range(1, 5)]
        public int religiousCount = 2;
        [Range(3, 15)]
        public int industrialCount = 8;
        
        [Header("=== ROADS ===")]
        public bool generateRoads = true;
        public float mainRoadWidth = 8f;
        public float sideRoadWidth = 4f;
        
        [Header("=== VEGETATION ===")]
        [Range(0f, 1f)]
        public float forestDensity = 0.6f;
        [Range(50, 500)]
        public int treeCount = 200;
        [Range(20, 200)]
        public int bushCount = 80;
        public float forestStartRadius = 160f; // Trees start outside walls
        
        [Header("=== PROPS & DETAILS ===")]
        [Range(20, 200)]
        public int propCount = 100;
        public bool generateFountain = true;
        public bool generateMarketSquare = true;
        
        [Header("=== LIGHTING ===")]
        public bool generateTorches = true;
        public int torchCount = 30;
        
        [Header("=== PERFORMANCE ===")]
        public int objectsPerFrame = 10;
        public bool useLOD = true;
    }

    /// <summary>
    /// Standalone Fantasy Kingdom Generator
    /// Creates beautiful fantasy worlds without any GPS/mapping dependencies
    /// Proper 1:1 scale (1 Unity unit = 1 meter)
    /// </summary>
    public class StandaloneFantasyGenerator : MonoBehaviour
    {
        [Header("=== CONFIGURATION ===")]
        [SerializeField] private FantasyKingdomConfig config;
        [SerializeField] private FantasyPrefabLibrary prefabLibrary;
        
        [Header("=== RUNTIME STATE ===")]
        [SerializeField] private bool isGenerating = false;
        [SerializeField] private float generationProgress = 0f;
        
        // Parent transforms for organization
        private Transform terrainParent;
        private Transform buildingsParent;
        private Transform roadsParent;
        private Transform vegetationParent;
        private Transform propsParent;
        private Transform wallsParent;
        
        // Generated data
        private List<Vector3> buildingPositions = new List<Vector3>();
        private List<Vector3> roadPoints = new List<Vector3>();
        private List<Bounds> occupiedAreas = new List<Bounds>();
        
        // Road network for building placement
        private List<RoadSegment> roads = new List<RoadSegment>();
        
        public event Action OnGenerationStarted;
        public event Action<float> OnGenerationProgress;
        public event Action OnGenerationComplete;
        
        private struct RoadSegment
        {
            public Vector3 start;
            public Vector3 end;
            public float width;
            public bool isMainRoad;
        }
        
        private void Awake()
        {
            // Try to load config from Resources if not assigned
            if (config == null)
            {
                config = Resources.Load<FantasyKingdomConfig>("FantasyKingdomConfig");
            }
            
            // Try to load prefab library
            if (prefabLibrary == null)
            {
                prefabLibrary = Resources.Load<FantasyPrefabLibrary>("MainFantasyPrefabLibrary");
            }
        }
        
        /// <summary>
        /// Start generating the fantasy kingdom
        /// </summary>
        public void Generate()
        {
            if (isGenerating)
            {
                Debug.LogWarning("[FantasyKingdom] Generation already in progress");
                return;
            }
            
            StartCoroutine(GenerateKingdomCoroutine());
        }
        
        /// <summary>
        /// Clear all generated content
        /// </summary>
        public void Clear()
        {
            DestroyChildrenOf(terrainParent);
            DestroyChildrenOf(buildingsParent);
            DestroyChildrenOf(roadsParent);
            DestroyChildrenOf(vegetationParent);
            DestroyChildrenOf(propsParent);
            DestroyChildrenOf(wallsParent);
            
            buildingPositions.Clear();
            roadPoints.Clear();
            occupiedAreas.Clear();
            roads.Clear();
            
            Debug.Log("[FantasyKingdom] World cleared");
        }
        
        private void DestroyChildrenOf(Transform parent)
        {
            if (parent == null) return;
            
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
        
        private IEnumerator GenerateKingdomCoroutine()
        {
            isGenerating = true;
            generationProgress = 0f;
            OnGenerationStarted?.Invoke();
            
            Debug.Log("[FantasyKingdom] ========================================");
            Debug.Log("[FantasyKingdom] Starting Fantasy Kingdom Generation");
            Debug.Log($"[FantasyKingdom] Kingdom Size: {config.kingdomSize}m x {config.kingdomSize}m");
            Debug.Log("[FantasyKingdom] ========================================");
            
            // Create parent transforms
            CreateParentTransforms();
            yield return null;
            
            // Step 1: Generate Terrain (10%)
            Debug.Log("[FantasyKingdom] Step 1/7: Generating Terrain...");
            yield return StartCoroutine(GenerateTerrain());
            generationProgress = 0.1f;
            OnGenerationProgress?.Invoke(generationProgress);
            
            // Step 2: Generate Road Network (20%)
            Debug.Log("[FantasyKingdom] Step 2/7: Creating Road Network...");
            yield return StartCoroutine(GenerateRoadNetwork());
            generationProgress = 0.2f;
            OnGenerationProgress?.Invoke(generationProgress);
            
            // Step 3: Generate Castle & Walls (30%)
            if (config.generateCastle || config.generateWalls)
            {
                Debug.Log("[FantasyKingdom] Step 3/7: Building Castle & Walls...");
                yield return StartCoroutine(GenerateCastleAndWalls());
            }
            generationProgress = 0.3f;
            OnGenerationProgress?.Invoke(generationProgress);
            
            // Step 4: Generate Buildings (55%)
            Debug.Log("[FantasyKingdom] Step 4/7: Placing Buildings...");
            yield return StartCoroutine(GenerateBuildings());
            generationProgress = 0.55f;
            OnGenerationProgress?.Invoke(generationProgress);
            
            // Step 5: Generate Roads Visual (70%)
            if (config.generateRoads)
            {
                Debug.Log("[FantasyKingdom] Step 5/7: Laying Cobblestone Roads...");
                yield return StartCoroutine(GenerateRoadVisuals());
            }
            generationProgress = 0.7f;
            OnGenerationProgress?.Invoke(generationProgress);
            
            // Step 6: Generate Vegetation (85%)
            Debug.Log("[FantasyKingdom] Step 6/7: Growing Forest...");
            yield return StartCoroutine(GenerateVegetation());
            generationProgress = 0.85f;
            OnGenerationProgress?.Invoke(generationProgress);
            
            // Step 7: Generate Props & Details (100%)
            Debug.Log("[FantasyKingdom] Step 7/7: Adding Details & Props...");
            yield return StartCoroutine(GenerateProps());
            generationProgress = 1f;
            OnGenerationProgress?.Invoke(generationProgress);
            
            isGenerating = false;
            
            Debug.Log("[FantasyKingdom] ========================================");
            Debug.Log("[FantasyKingdom] Fantasy Kingdom Generation Complete!");
            Debug.Log($"[FantasyKingdom] Buildings: {buildingsParent.childCount}");
            Debug.Log($"[FantasyKingdom] Trees/Bushes: {vegetationParent.childCount}");
            Debug.Log($"[FantasyKingdom] Props: {propsParent.childCount}");
            Debug.Log("[FantasyKingdom] ========================================");
            
            OnGenerationComplete?.Invoke();
        }
        
        private void CreateParentTransforms()
        {
            terrainParent = CreateOrGetChild("Terrain");
            buildingsParent = CreateOrGetChild("Buildings");
            roadsParent = CreateOrGetChild("Roads");
            vegetationParent = CreateOrGetChild("Vegetation");
            propsParent = CreateOrGetChild("Props");
            wallsParent = CreateOrGetChild("Walls");
        }
        
        private Transform CreateOrGetChild(string name)
        {
            var existing = transform.Find(name);
            if (existing != null) return existing;
            
            var child = new GameObject(name);
            child.transform.SetParent(transform);
            child.transform.localPosition = Vector3.zero;
            return child.transform;
        }
        
        // ====================================================================
        // TERRAIN GENERATION
        // ====================================================================
        private IEnumerator GenerateTerrain()
        {
            float halfSize = config.kingdomSize / 2f;
            
            // Create main ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.SetParent(terrainParent);
            ground.transform.localPosition = new Vector3(0, -0.01f, 0);
            
            // Unity plane is 10x10 by default, scale to kingdom size
            float planeScale = config.kingdomSize / 10f;
            ground.transform.localScale = new Vector3(planeScale, 1, planeScale);
            
            // Apply material
            var renderer = ground.GetComponent<Renderer>();
            if (config.groundMaterial != null)
            {
                renderer.material = config.groundMaterial;
            }
            else
            {
                // Create a simple grass-colored material
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.2f, 0.5f, 0.15f); // Grass green
                renderer.material = mat;
            }
            
            // Remove collider, we'll add proper collision
            DestroyImmediate(ground.GetComponent<Collider>());
            
            // Add mesh collider for walking
            var groundCollision = new GameObject("GroundCollision");
            groundCollision.transform.SetParent(terrainParent);
            groundCollision.transform.localPosition = Vector3.zero;
            var meshFilter = groundCollision.AddComponent<MeshFilter>();
            var meshCollider = groundCollision.AddComponent<MeshCollider>();
            
            // Create flat collision mesh (or hilly if enabled)
            Mesh groundMesh = CreateGroundMesh(config.kingdomSize, config.generateHills);
            meshFilter.mesh = groundMesh;
            meshCollider.sharedMesh = groundMesh;
            groundCollision.layer = LayerMask.NameToLayer("Ground");
            
            yield return null;
        }
        
        private Mesh CreateGroundMesh(float size, bool withHills)
        {
            int resolution = 64;
            float halfSize = size / 2f;
            
            var mesh = new Mesh();
            mesh.name = "GroundMesh";
            
            var vertices = new Vector3[(resolution + 1) * (resolution + 1)];
            var uvs = new Vector2[(resolution + 1) * (resolution + 1)];
            var triangles = new int[resolution * resolution * 6];
            
            float step = size / resolution;
            
            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    float worldX = -halfSize + x * step;
                    float worldZ = -halfSize + z * step;
                    float height = 0f;
                    
                    if (withHills)
                    {
                        // Add gentle hills outside town area
                        float distFromCenter = new Vector2(worldX, worldZ).magnitude;
                        if (distFromCenter > config.townRadius)
                        {
                            float hillFactor = Mathf.Clamp01((distFromCenter - config.townRadius) / 100f);
                            height = Mathf.PerlinNoise(
                                worldX * config.hillFrequency + 1000f,
                                worldZ * config.hillFrequency + 1000f
                            ) * config.hillHeight * hillFactor;
                        }
                    }
                    
                    int index = z * (resolution + 1) + x;
                    vertices[index] = new Vector3(worldX, height, worldZ);
                    uvs[index] = new Vector2((float)x / resolution, (float)z / resolution);
                }
            }
            
            int triIndex = 0;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int topLeft = z * (resolution + 1) + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = (z + 1) * (resolution + 1) + x;
                    int bottomRight = bottomLeft + 1;
                    
                    triangles[triIndex++] = topLeft;
                    triangles[triIndex++] = bottomLeft;
                    triangles[triIndex++] = topRight;
                    
                    triangles[triIndex++] = topRight;
                    triangles[triIndex++] = bottomLeft;
                    triangles[triIndex++] = bottomRight;
                }
            }
            
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        // ====================================================================
        // ROAD NETWORK GENERATION
        // ====================================================================
        private IEnumerator GenerateRoadNetwork()
        {
            roads.Clear();
            
            // Main roads: Cross pattern from gates to center
            float r = config.wallRadius;
            
            // North-South main road
            roads.Add(new RoadSegment
            {
                start = new Vector3(0, 0, -r * 1.2f),
                end = new Vector3(0, 0, r * 1.2f),
                width = config.mainRoadWidth,
                isMainRoad = true
            });
            
            // East-West main road
            roads.Add(new RoadSegment
            {
                start = new Vector3(-r * 1.2f, 0, 0),
                end = new Vector3(r * 1.2f, 0, 0),
                width = config.mainRoadWidth,
                isMainRoad = true
            });
            
            // Ring road inside walls
            int ringSegments = 16;
            float ringRadius = config.wallRadius * 0.7f;
            for (int i = 0; i < ringSegments; i++)
            {
                float angle1 = (i * 360f / ringSegments) * Mathf.Deg2Rad;
                float angle2 = ((i + 1) * 360f / ringSegments) * Mathf.Deg2Rad;
                
                roads.Add(new RoadSegment
                {
                    start = new Vector3(Mathf.Cos(angle1) * ringRadius, 0, Mathf.Sin(angle1) * ringRadius),
                    end = new Vector3(Mathf.Cos(angle2) * ringRadius, 0, Mathf.Sin(angle2) * ringRadius),
                    width = config.sideRoadWidth,
                    isMainRoad = false
                });
            }
            
            // Radial roads connecting ring to main roads
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                Vector3 inner = new Vector3(Mathf.Cos(angle) * 20f, 0, Mathf.Sin(angle) * 20f);
                Vector3 outer = new Vector3(Mathf.Cos(angle) * ringRadius, 0, Mathf.Sin(angle) * ringRadius);
                
                roads.Add(new RoadSegment
                {
                    start = inner,
                    end = outer,
                    width = config.sideRoadWidth,
                    isMainRoad = false
                });
            }
            
            // Store road points for building placement
            foreach (var road in roads)
            {
                roadPoints.Add(road.start);
                roadPoints.Add(road.end);
                roadPoints.Add((road.start + road.end) / 2f);
            }
            
            yield return null;
        }
        
        // ====================================================================
        // CASTLE & WALLS
        // ====================================================================
        private IEnumerator GenerateCastleAndWalls()
        {
            int objectsThisFrame = 0;
            
            // Generate Castle at center
            if (config.generateCastle && prefabLibrary != null)
            {
                // Main keep/castle
                var keepPrefab = GetRandomFromArray(prefabLibrary.keeps) ?? 
                                 GetRandomFromArray(prefabLibrary.castles) ??
                                 GetRandomFromArray(prefabLibrary.fortresses);
                
                if (keepPrefab != null)
                {
                    var keep = Instantiate(keepPrefab, Vector3.zero, Quaternion.identity, buildingsParent);
                    keep.name = "Castle_Keep";
                    keep.transform.localScale = Vector3.one; // 1:1 scale!
                    
                    // Mark area as occupied
                    var keepBounds = CalculateBounds(keep);
                    occupiedAreas.Add(keepBounds);
                }
            }
            
            yield return null;
            
            // Generate Walls
            if (config.generateWalls && prefabLibrary != null)
            {
                // Wall segments
                var wallPrefab = GetRandomFromArray(prefabLibrary.walls);
                var towerPrefab = GetRandomFromArray(prefabLibrary.guardTowers);
                var gatePrefab = GetRandomFromArray(prefabLibrary.gates);
                
                if (wallPrefab != null)
                {
                    int wallSegments = 32;
                    float angleStep = 360f / wallSegments;
                    
                    for (int i = 0; i < wallSegments; i++)
                    {
                        float angle = i * angleStep * Mathf.Deg2Rad;
                        Vector3 pos = new Vector3(
                            Mathf.Cos(angle) * config.wallRadius,
                            0,
                            Mathf.Sin(angle) * config.wallRadius
                        );
                        
                        // Skip wall at gate positions (N, S, E, W)
                        bool isGatePosition = (i == 0 || i == 8 || i == 16 || i == 24);
                        
                        if (isGatePosition && gatePrefab != null)
                        {
                            var gate = Instantiate(gatePrefab, pos, Quaternion.Euler(0, i * angleStep + 90, 0), wallsParent);
                            gate.name = $"Gate_{i}";
                        }
                        else
                        {
                            var wall = Instantiate(wallPrefab, pos, Quaternion.Euler(0, i * angleStep + 90, 0), wallsParent);
                            wall.name = $"Wall_{i}";
                        }
                        
                        // Add towers at intervals
                        if (i % (wallSegments / config.wallTowerCount) == 0 && towerPrefab != null && !isGatePosition)
                        {
                            float towerAngle = i * angleStep * Mathf.Deg2Rad;
                            Vector3 towerPos = new Vector3(
                                Mathf.Cos(towerAngle) * (config.wallRadius + 2f),
                                0,
                                Mathf.Sin(towerAngle) * (config.wallRadius + 2f)
                            );
                            var tower = Instantiate(towerPrefab, towerPos, Quaternion.Euler(0, i * angleStep, 0), wallsParent);
                            tower.name = $"Tower_{i}";
                        }
                        
                        objectsThisFrame++;
                        if (objectsThisFrame >= config.objectsPerFrame)
                        {
                            objectsThisFrame = 0;
                            yield return null;
                        }
                    }
                }
            }
        }
        
        // ====================================================================
        // BUILDING GENERATION
        // ====================================================================
        private IEnumerator GenerateBuildings()
        {
            if (prefabLibrary == null) yield break;
            
            int objectsThisFrame = 0;
            float innerRadius = 30f; // Keep center clear for castle
            float maxRadius = config.wallRadius - 15f; // Stay inside walls
            
            // Generate each building type
            yield return StartCoroutine(PlaceBuildingType(FantasyBuildingType.House, config.residentialCount, innerRadius, maxRadius, ref objectsThisFrame));
            yield return StartCoroutine(PlaceBuildingType(FantasyBuildingType.GeneralStore, config.commercialCount / 3, innerRadius, maxRadius * 0.7f, ref objectsThisFrame));
            yield return StartCoroutine(PlaceBuildingType(FantasyBuildingType.Tavern, config.commercialCount / 3, innerRadius, maxRadius * 0.7f, ref objectsThisFrame));
            yield return StartCoroutine(PlaceBuildingType(FantasyBuildingType.Blacksmith, config.commercialCount / 3, innerRadius, maxRadius * 0.8f, ref objectsThisFrame));
            yield return StartCoroutine(PlaceBuildingType(FantasyBuildingType.Barracks, config.militaryCount, maxRadius * 0.6f, maxRadius, ref objectsThisFrame));
            yield return StartCoroutine(PlaceBuildingType(FantasyBuildingType.Chapel, config.religiousCount, innerRadius, maxRadius * 0.5f, ref objectsThisFrame));
            yield return StartCoroutine(PlaceBuildingType(FantasyBuildingType.Barn, config.industrialCount / 2, maxRadius * 0.5f, maxRadius, ref objectsThisFrame));
            yield return StartCoroutine(PlaceBuildingType(FantasyBuildingType.Mill, config.industrialCount / 2, maxRadius * 0.7f, maxRadius * 1.1f, ref objectsThisFrame));
        }
        
        private IEnumerator PlaceBuildingType(FantasyBuildingType type, int count, float minRadius, float maxRadius, ref int objectsThisFrame)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 position = FindValidBuildingPosition(minRadius, maxRadius, 10f);
                if (position == Vector3.zero) continue;
                
                var prefab = prefabLibrary.GetBuilding(BuildingSize.Medium, type);
                if (prefab == null) continue;
                
                float rotation = UnityEngine.Random.Range(0f, 360f);
                
                // Snap rotation to face nearest road
                Vector3 nearestRoadPoint = FindNearestRoadPoint(position);
                if (nearestRoadPoint != Vector3.zero)
                {
                    Vector3 toRoad = nearestRoadPoint - position;
                    rotation = Mathf.Atan2(toRoad.x, toRoad.z) * Mathf.Rad2Deg;
                }
                
                var building = Instantiate(prefab, position, Quaternion.Euler(0, rotation, 0), buildingsParent);
                building.name = $"{type}_{i}";
                building.transform.localScale = Vector3.one; // 1:1 scale!
                
                // Mark area as occupied
                var bounds = CalculateBounds(building);
                bounds.Expand(5f); // Add spacing
                occupiedAreas.Add(bounds);
                buildingPositions.Add(position);
                
                objectsThisFrame++;
                if (objectsThisFrame >= config.objectsPerFrame)
                {
                    objectsThisFrame = 0;
                    yield return null;
                }
            }
        }
        
        private Vector3 FindValidBuildingPosition(float minRadius, float maxRadius, float spacing)
        {
            int maxAttempts = 50;
            
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = UnityEngine.Random.Range(minRadius, maxRadius);
                
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                
                // Check if position is valid
                if (IsPositionValid(pos, spacing))
                {
                    return pos;
                }
            }
            
            return Vector3.zero;
        }
        
        private bool IsPositionValid(Vector3 pos, float spacing)
        {
            // Check against occupied areas
            Bounds testBounds = new Bounds(pos, Vector3.one * spacing);
            foreach (var occupied in occupiedAreas)
            {
                if (occupied.Intersects(testBounds))
                    return false;
            }
            
            // Check not too close to roads (but not too far either)
            float distToRoad = DistanceToNearestRoad(pos);
            if (distToRoad < 3f) return false; // Too close to road
            if (distToRoad > 30f) return false; // Too far from road
            
            return true;
        }
        
        private float DistanceToNearestRoad(Vector3 pos)
        {
            float minDist = float.MaxValue;
            
            foreach (var road in roads)
            {
                float dist = DistanceToLineSegment(pos, road.start, road.end);
                minDist = Mathf.Min(minDist, dist);
            }
            
            return minDist;
        }
        
        private Vector3 FindNearestRoadPoint(Vector3 pos)
        {
            float minDist = float.MaxValue;
            Vector3 nearest = Vector3.zero;
            
            foreach (var point in roadPoints)
            {
                float dist = Vector3.Distance(pos, point);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = point;
                }
            }
            
            return nearest;
        }
        
        private float DistanceToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 line = lineEnd - lineStart;
            float lineLengthSq = line.sqrMagnitude;
            
            if (lineLengthSq == 0) return Vector3.Distance(point, lineStart);
            
            float t = Mathf.Clamp01(Vector3.Dot(point - lineStart, line) / lineLengthSq);
            Vector3 projection = lineStart + t * line;
            
            return Vector3.Distance(point, projection);
        }
        
        // ====================================================================
        // ROAD VISUALS
        // ====================================================================
        private IEnumerator GenerateRoadVisuals()
        {
            if (prefabLibrary == null) yield break;
            
            var cobblePrefab = GetRandomFromArray(prefabLibrary.cobblestoneSegments);
            if (cobblePrefab == null)
            {
                Debug.LogWarning("[FantasyKingdom] No cobblestone prefabs found, skipping road visuals");
                yield break;
            }
            
            int objectsThisFrame = 0;
            float segmentSize = 3f; // Cobblestone segment size in meters
            
            foreach (var road in roads)
            {
                Vector3 direction = (road.end - road.start).normalized;
                float length = Vector3.Distance(road.start, road.end);
                int segments = Mathf.CeilToInt(length / segmentSize);
                
                float rotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                
                for (int i = 0; i < segments; i++)
                {
                    Vector3 pos = road.start + direction * (i * segmentSize + segmentSize / 2f);
                    pos.y = 0.02f; // Slightly above ground
                    
                    // Place multiple segments for wider roads
                    int widthSegments = road.isMainRoad ? 2 : 1;
                    float offset = road.isMainRoad ? segmentSize / 2f : 0f;
                    
                    for (int w = 0; w < widthSegments; w++)
                    {
                        Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x);
                        Vector3 offsetPos = pos + perpendicular * (w - (widthSegments - 1) / 2f) * segmentSize;
                        
                        var segment = Instantiate(cobblePrefab, offsetPos, Quaternion.Euler(0, rotation, 0), roadsParent);
                        segment.name = $"Road_Segment";
                        segment.transform.localScale = Vector3.one;
                        
                        objectsThisFrame++;
                        if (objectsThisFrame >= config.objectsPerFrame * 5) // Roads can be faster
                        {
                            objectsThisFrame = 0;
                            yield return null;
                        }
                    }
                }
            }
        }
        
        // ====================================================================
        // VEGETATION
        // ====================================================================
        private IEnumerator GenerateVegetation()
        {
            if (prefabLibrary == null) yield break;
            
            int objectsThisFrame = 0;
            
            // Trees - mostly outside walls
            for (int i = 0; i < config.treeCount; i++)
            {
                Vector3 pos = GetRandomForestPosition();
                if (pos == Vector3.zero) continue;
                
                var treePrefab = prefabLibrary.GetRandomTree();
                if (treePrefab == null) continue;
                
                float scale = UnityEngine.Random.Range(0.8f, 1.3f);
                float rotation = UnityEngine.Random.Range(0f, 360f);
                
                var tree = Instantiate(treePrefab, pos, Quaternion.Euler(0, rotation, 0), vegetationParent);
                tree.name = $"Tree_{i}";
                tree.transform.localScale = Vector3.one * scale;
                
                // Mark small area around tree
                occupiedAreas.Add(new Bounds(pos, Vector3.one * 3f));
                
                objectsThisFrame++;
                if (objectsThisFrame >= config.objectsPerFrame)
                {
                    objectsThisFrame = 0;
                    yield return null;
                }
            }
            
            // Bushes - near buildings and forest edge
            for (int i = 0; i < config.bushCount; i++)
            {
                Vector3 pos = GetRandomVegetationPosition(config.townRadius * 0.5f, config.kingdomSize / 2f);
                if (pos == Vector3.zero) continue;
                
                var bushPrefab = prefabLibrary.GetRandomBush();
                if (bushPrefab == null) continue;
                
                float scale = UnityEngine.Random.Range(0.7f, 1.2f);
                float rotation = UnityEngine.Random.Range(0f, 360f);
                
                var bush = Instantiate(bushPrefab, pos, Quaternion.Euler(0, rotation, 0), vegetationParent);
                bush.name = $"Bush_{i}";
                bush.transform.localScale = Vector3.one * scale;
                
                objectsThisFrame++;
                if (objectsThisFrame >= config.objectsPerFrame)
                {
                    objectsThisFrame = 0;
                    yield return null;
                }
            }
        }
        
        private Vector3 GetRandomForestPosition()
        {
            int maxAttempts = 30;
            
            for (int i = 0; i < maxAttempts; i++)
            {
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = UnityEngine.Random.Range(config.forestStartRadius, config.kingdomSize / 2f - 10f);
                
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                
                // Check not on road
                if (DistanceToNearestRoad(pos) > 5f)
                {
                    return pos;
                }
            }
            
            return Vector3.zero;
        }
        
        private Vector3 GetRandomVegetationPosition(float minRadius, float maxRadius)
        {
            int maxAttempts = 20;
            
            for (int i = 0; i < maxAttempts; i++)
            {
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = UnityEngine.Random.Range(minRadius, maxRadius);
                
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                
                if (DistanceToNearestRoad(pos) > 2f)
                {
                    return pos;
                }
            }
            
            return Vector3.zero;
        }
        
        // ====================================================================
        // PROPS & DETAILS
        // ====================================================================
        private IEnumerator GenerateProps()
        {
            if (prefabLibrary == null) yield break;
            
            int objectsThisFrame = 0;
            
            // Central fountain/well
            if (config.generateFountain)
            {
                var fountainPrefab = GetRandomFromArray(prefabLibrary.fountains) ?? GetRandomFromArray(prefabLibrary.wells);
                if (fountainPrefab != null)
                {
                    var fountain = Instantiate(fountainPrefab, new Vector3(0, 0, 25), Quaternion.identity, propsParent);
                    fountain.name = "Central_Fountain";
                    fountain.transform.localScale = Vector3.one;
                }
            }
            
            yield return null;
            
            // Market stalls near center
            if (config.generateMarketSquare)
            {
                var stallPrefab = GetRandomFromArray(prefabLibrary.marketStalls);
                if (stallPrefab != null)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = (i * 60f + 30f) * Mathf.Deg2Rad;
                        Vector3 pos = new Vector3(Mathf.Cos(angle) * 35f, 0, Mathf.Sin(angle) * 35f);
                        
                        var stall = Instantiate(stallPrefab, pos, Quaternion.Euler(0, i * 60f, 0), propsParent);
                        stall.name = $"MarketStall_{i}";
                        stall.transform.localScale = Vector3.one;
                        
                        objectsThisFrame++;
                        if (objectsThisFrame >= config.objectsPerFrame)
                        {
                            objectsThisFrame = 0;
                            yield return null;
                        }
                    }
                }
            }
            
            // Random props throughout town
            for (int i = 0; i < config.propCount; i++)
            {
                Vector3 pos = FindValidBuildingPosition(20f, config.wallRadius - 10f, 2f);
                if (pos == Vector3.zero) continue;
                
                var propPrefab = prefabLibrary.GetRandomProp();
                if (propPrefab == null) continue;
                
                float rotation = UnityEngine.Random.Range(0f, 360f);
                
                var prop = Instantiate(propPrefab, pos, Quaternion.Euler(0, rotation, 0), propsParent);
                prop.name = $"Prop_{i}";
                prop.transform.localScale = Vector3.one;
                
                objectsThisFrame++;
                if (objectsThisFrame >= config.objectsPerFrame)
                {
                    objectsThisFrame = 0;
                    yield return null;
                }
            }
            
            // Torches along roads
            if (config.generateTorches)
            {
                var torchPrefab = GetRandomFromArray(prefabLibrary.torches) ?? GetRandomFromArray(prefabLibrary.lanterns);
                if (torchPrefab != null)
                {
                    int torchIndex = 0;
                    foreach (var road in roads)
                    {
                        if (!road.isMainRoad) continue;
                        
                        Vector3 direction = (road.end - road.start).normalized;
                        float length = Vector3.Distance(road.start, road.end);
                        int torchCount = Mathf.CeilToInt(length / 20f);
                        
                        for (int i = 0; i < torchCount && torchIndex < config.torchCount; i++)
                        {
                            Vector3 pos = road.start + direction * (i * 20f + 10f);
                            
                            // Place on both sides of road
                            Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x);
                            
                            foreach (float side in new float[] { -1f, 1f })
                            {
                                Vector3 torchPos = pos + perpendicular * side * (road.width / 2f + 1f);
                                
                                var torch = Instantiate(torchPrefab, torchPos, Quaternion.identity, propsParent);
                                torch.name = $"Torch_{torchIndex++}";
                                torch.transform.localScale = Vector3.one;
                                
                                if (torchIndex >= config.torchCount) break;
                            }
                        }
                    }
                }
            }
        }
        
        // ====================================================================
        // UTILITY METHODS
        // ====================================================================
        private GameObject GetRandomFromArray(GameObject[] array)
        {
            if (array == null || array.Length == 0) return null;
            
            // Filter out nulls
            var valid = new List<GameObject>();
            foreach (var obj in array)
            {
                if (obj != null) valid.Add(obj);
            }
            
            if (valid.Count == 0) return null;
            return valid[UnityEngine.Random.Range(0, valid.Count)];
        }
        
        private Bounds CalculateBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(obj.transform.position, Vector3.one * 5f);
            }
            
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            
            return bounds;
        }
        
        // ====================================================================
        // PUBLIC PROPERTIES
        // ====================================================================
        public bool IsGenerating => isGenerating;
        public float Progress => generationProgress;
        public FantasyKingdomConfig Config => config;
        public FantasyPrefabLibrary PrefabLibrary => prefabLibrary;
    }
}
