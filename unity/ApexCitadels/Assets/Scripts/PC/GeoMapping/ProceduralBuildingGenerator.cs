using UnityEngine;
using System;
using System.Collections.Generic;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Procedural Fantasy Building Generator that creates medieval-style buildings
    /// from OSM footprint data. Transforms real-world building shapes into castles,
    /// cottages, taverns, and other fantasy structures.
    /// </summary>
    public class ProceduralBuildingGenerator : MonoBehaviour
    {
        [Header("Building Prefabs")]
        [SerializeField] private BuildingPrefabSet[] prefabSets;
        
        [Header("Procedural Settings")]
        [SerializeField] private bool useProceduralMeshes = true;
        [SerializeField] private Material[] wallMaterials;
        [SerializeField] private Material[] roofMaterials;
        [SerializeField] private Material[] detailMaterials;
        
        [Header("Scale & Proportions")]
        [SerializeField] private float baseFloorHeight = 3.5f;
        [SerializeField] private float cottageScale = 1.0f;
        [SerializeField] private float manorScale = 1.2f;
        [SerializeField] private float castleScale = 1.5f;
        [SerializeField] private float minBuildingHeight = 4f;
        [SerializeField] private float maxBuildingHeight = 50f;
        
        [Header("Detail Settings")]
        [SerializeField] private bool addWindows = true;
        [SerializeField] private bool addDoors = true;
        [SerializeField] private bool addChimneys = true;
        [SerializeField] private bool addTowers = true;
        [SerializeField] private bool addFlags = true;
        [SerializeField] private float detailDensity = 0.5f;
        
        [Header("LOD Settings")]
        [SerializeField] private float lodDistance1 = 100f;
        [SerializeField] private float lodDistance2 = 300f;
        [SerializeField] private float lodCullDistance = 800f;
        
        // Singleton
        private static ProceduralBuildingGenerator _instance;
        public static ProceduralBuildingGenerator Instance => _instance;
        
        // Generated buildings cache
        private Dictionary<long, GeneratedBuilding> _generatedBuildings = new Dictionary<long, GeneratedBuilding>();
        private Transform _buildingContainer;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            _buildingContainer = new GameObject("GeneratedBuildings").transform;
            _buildingContainer.SetParent(transform);
        }
        
        #region Public API
        
        /// <summary>
        /// Generate buildings from OSM data
        /// </summary>
        public void GenerateBuildings(OSMAreaData areaData, Vector3 worldOrigin, float metersPerUnit = 1f)
        {
            if (areaData == null || areaData.buildings == null) return;
            
            foreach (var osmBuilding in areaData.buildings)
            {
                if (_generatedBuildings.ContainsKey(osmBuilding.id)) continue;
                
                var building = GenerateBuilding(osmBuilding, areaData.centerLatitude, areaData.centerLongitude, worldOrigin, metersPerUnit);
                if (building != null)
                {
                    _generatedBuildings[osmBuilding.id] = building;
                }
            }
        }
        
        /// <summary>
        /// Generate a single building
        /// </summary>
        public GeneratedBuilding GenerateBuilding(OSMBuilding osmBuilding, double originLat, double originLon, Vector3 worldOrigin, float metersPerUnit)
        {
            if (osmBuilding.footprint == null || osmBuilding.footprint.Count < 3)
                return null;
            
            // Convert footprint to world coordinates
            List<Vector3> worldFootprint = new List<Vector3>();
            foreach (var point in osmBuilding.footprint)
            {
                Vector3 worldPos = GeoToWorld(point.latitude, point.longitude, originLat, originLon, worldOrigin, metersPerUnit);
                worldFootprint.Add(worldPos);
            }
            
            // Calculate height
            float height = CalculateBuildingHeight(osmBuilding);
            
            // Create building object
            GameObject buildingObj = new GameObject($"Building_{osmBuilding.id}");
            buildingObj.transform.SetParent(_buildingContainer);
            
            Vector3 center = GeoToWorld(osmBuilding.center.latitude, osmBuilding.center.longitude, originLat, originLon, worldOrigin, metersPerUnit);
            buildingObj.transform.position = center;
            
            var building = new GeneratedBuilding
            {
                osmId = osmBuilding.id,
                gameObject = buildingObj,
                fantasyType = osmBuilding.fantasyType,
                height = height
            };
            
            // Generate based on type
            if (useProceduralMeshes)
            {
                GenerateProceduralBuilding(building, worldFootprint, height, osmBuilding.fantasyType);
            }
            else
            {
                InstantiatePrefabBuilding(building, osmBuilding.fantasyType, height);
            }
            
            // Add LOD
            SetupLOD(buildingObj, osmBuilding.fantasyType);
            
            return building;
        }
        
        /// <summary>
        /// Clear all generated buildings
        /// </summary>
        public void ClearBuildings()
        {
            foreach (var kvp in _generatedBuildings)
            {
                if (kvp.Value.gameObject != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            _generatedBuildings.Clear();
        }
        
        /// <summary>
        /// Update LOD based on camera distance
        /// </summary>
        public void UpdateLOD(Vector3 cameraPosition)
        {
            foreach (var kvp in _generatedBuildings)
            {
                if (kvp.Value.gameObject == null) continue;
                
                float distance = Vector3.Distance(cameraPosition, kvp.Value.gameObject.transform.position);
                
                // Culling
                if (distance > lodCullDistance)
                {
                    kvp.Value.gameObject.SetActive(false);
                    continue;
                }
                
                kvp.Value.gameObject.SetActive(true);
                
                // LOD switching
                int targetLOD = distance < lodDistance1 ? 0 : (distance < lodDistance2 ? 1 : 2);
                SetBuildingLOD(kvp.Value, targetLOD);
            }
        }
        
        #endregion
        
        #region Procedural Generation
        
        private void GenerateProceduralBuilding(GeneratedBuilding building, List<Vector3> footprint, float height, FantasyBuildingType type)
        {
            // Create base mesh
            CreateBuildingBase(building, footprint, height, type);
            
            // Add roof
            CreateRoof(building, footprint, height, type);
            
            // Add details
            if (addWindows) AddWindows(building, footprint, height, type);
            if (addDoors) AddDoor(building, footprint, type);
            if (addChimneys && ShouldHaveChimney(type)) AddChimney(building, height);
            if (addTowers && ShouldHaveTower(type)) AddTowers(building, footprint, height);
            if (addFlags && ShouldHaveFlag(type)) AddFlag(building, height);
        }
        
        private void CreateBuildingBase(GeneratedBuilding building, List<Vector3> footprint, float height, FantasyBuildingType type)
        {
            // Create mesh for walls
            MeshFilter meshFilter = building.gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = building.gameObject.AddComponent<MeshRenderer>();
            
            Mesh mesh = new Mesh();
            mesh.name = "BuildingBase";
            
            // Calculate vertices for extruded polygon
            int n = footprint.Count;
            Vector3[] vertices = new Vector3[n * 4 + 2]; // Walls + top/bottom caps
            int[] triangles = new int[n * 6 + (n - 2) * 6]; // Walls + caps
            Vector2[] uvs = new Vector2[vertices.Length];
            
            Vector3 center = building.gameObject.transform.position;
            
            // Bottom vertices
            for (int i = 0; i < n; i++)
            {
                vertices[i] = footprint[i] - center;
                uvs[i] = new Vector2(i / (float)n, 0);
            }
            
            // Top vertices
            for (int i = 0; i < n; i++)
            {
                vertices[n + i] = footprint[i] - center + Vector3.up * height;
                uvs[n + i] = new Vector2(i / (float)n, height / 5f);
            }
            
            // Wall triangles
            int triIdx = 0;
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                
                // First triangle
                triangles[triIdx++] = i;
                triangles[triIdx++] = n + i;
                triangles[triIdx++] = next;
                
                // Second triangle
                triangles[triIdx++] = next;
                triangles[triIdx++] = n + i;
                triangles[triIdx++] = n + next;
            }
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            meshFilter.mesh = mesh;
            
            // Assign material based on type
            meshRenderer.material = GetWallMaterial(type);
            
            building.baseMesh = mesh;
        }
        
        private void CreateRoof(GeneratedBuilding building, List<Vector3> footprint, float height, FantasyBuildingType type)
        {
            RoofStyle roofStyle = GetRoofStyle(type);
            
            GameObject roofObj = new GameObject("Roof");
            roofObj.transform.SetParent(building.gameObject.transform);
            roofObj.transform.localPosition = Vector3.up * height;
            
            MeshFilter meshFilter = roofObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = roofObj.AddComponent<MeshRenderer>();
            
            Mesh roofMesh = null;
            
            switch (roofStyle)
            {
                case RoofStyle.Flat:
                    roofMesh = CreateFlatRoof(footprint, building.gameObject.transform.position);
                    break;
                case RoofStyle.Gabled:
                    roofMesh = CreateGabledRoof(footprint, building.gameObject.transform.position, height * 0.3f);
                    break;
                case RoofStyle.Hipped:
                    roofMesh = CreateHippedRoof(footprint, building.gameObject.transform.position, height * 0.25f);
                    break;
                case RoofStyle.Conical:
                    roofMesh = CreateConicalRoof(footprint, building.gameObject.transform.position, height * 0.5f);
                    break;
                case RoofStyle.Crenellated:
                    roofMesh = CreateCrenellatedRoof(footprint, building.gameObject.transform.position);
                    break;
                default:
                    roofMesh = CreateGabledRoof(footprint, building.gameObject.transform.position, height * 0.3f);
                    break;
            }
            
            meshFilter.mesh = roofMesh;
            meshRenderer.material = GetRoofMaterial(type);
            
            building.roofObject = roofObj;
        }
        
        private Mesh CreateFlatRoof(List<Vector3> footprint, Vector3 center)
        {
            Mesh mesh = new Mesh();
            
            int n = footprint.Count;
            Vector3[] vertices = new Vector3[n];
            
            for (int i = 0; i < n; i++)
            {
                vertices[i] = footprint[i] - center;
            }
            
            // Simple fan triangulation
            int[] triangles = new int[(n - 2) * 3];
            for (int i = 0; i < n - 2; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        private Mesh CreateGabledRoof(List<Vector3> footprint, Vector3 center, float roofHeight)
        {
            // Simplified gabled roof
            Mesh mesh = new Mesh();
            
            // Find longest axis for ridge
            Bounds bounds = new Bounds(footprint[0] - center, Vector3.zero);
            foreach (var p in footprint)
            {
                bounds.Encapsulate(p - center);
            }
            
            bool ridgeAlongX = bounds.size.x > bounds.size.z;
            
            // Create vertices: 4 corners + 2 ridge points
            Vector3[] vertices = new Vector3[6];
            vertices[0] = new Vector3(bounds.min.x, 0, bounds.min.z);
            vertices[1] = new Vector3(bounds.max.x, 0, bounds.min.z);
            vertices[2] = new Vector3(bounds.max.x, 0, bounds.max.z);
            vertices[3] = new Vector3(bounds.min.x, 0, bounds.max.z);
            
            if (ridgeAlongX)
            {
                float midZ = (bounds.min.z + bounds.max.z) / 2;
                vertices[4] = new Vector3(bounds.min.x, roofHeight, midZ);
                vertices[5] = new Vector3(bounds.max.x, roofHeight, midZ);
            }
            else
            {
                float midX = (bounds.min.x + bounds.max.x) / 2;
                vertices[4] = new Vector3(midX, roofHeight, bounds.min.z);
                vertices[5] = new Vector3(midX, roofHeight, bounds.max.z);
            }
            
            int[] triangles;
            if (ridgeAlongX)
            {
                triangles = new int[]
                {
                    0, 4, 1, 1, 4, 5, // Front slope
                    2, 5, 3, 3, 5, 4, // Back slope
                    0, 3, 4, // Left gable
                    1, 5, 2  // Right gable
                };
            }
            else
            {
                triangles = new int[]
                {
                    0, 4, 3, 3, 4, 5, // Left slope
                    1, 2, 4, 2, 5, 4, // Right slope
                    0, 1, 4, // Front gable
                    2, 3, 5  // Back gable
                };
            }
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        private Mesh CreateHippedRoof(List<Vector3> footprint, Vector3 center, float roofHeight)
        {
            // Simplified hipped roof - all sides slope inward
            Mesh mesh = new Mesh();
            
            Bounds bounds = new Bounds(footprint[0] - center, Vector3.zero);
            foreach (var p in footprint)
            {
                bounds.Encapsulate(p - center);
            }
            
            // 4 corners + 1 peak
            Vector3[] vertices = new Vector3[5];
            vertices[0] = new Vector3(bounds.min.x, 0, bounds.min.z);
            vertices[1] = new Vector3(bounds.max.x, 0, bounds.min.z);
            vertices[2] = new Vector3(bounds.max.x, 0, bounds.max.z);
            vertices[3] = new Vector3(bounds.min.x, 0, bounds.max.z);
            vertices[4] = new Vector3(bounds.center.x, roofHeight, bounds.center.z);
            
            int[] triangles = new int[]
            {
                0, 4, 1,
                1, 4, 2,
                2, 4, 3,
                3, 4, 0
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        private Mesh CreateConicalRoof(List<Vector3> footprint, Vector3 center, float roofHeight)
        {
            // For towers - cone shape
            Mesh mesh = new Mesh();
            
            int segments = Mathf.Min(footprint.Count, 16);
            float radius = 0;
            foreach (var p in footprint)
            {
                radius = Mathf.Max(radius, Vector3.Distance(p - center, Vector3.zero));
            }
            
            Vector3[] vertices = new Vector3[segments + 2];
            vertices[0] = Vector3.up * roofHeight; // Peak
            
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            }
            vertices[segments + 1] = Vector3.zero; // Center bottom
            
            int[] triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                
                // Side
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = next + 1;
                
                // Bottom
                triangles[segments * 3 + i * 3] = segments + 1;
                triangles[segments * 3 + i * 3 + 1] = next + 1;
                triangles[segments * 3 + i * 3 + 2] = i + 1;
            }
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        private Mesh CreateCrenellatedRoof(List<Vector3> footprint, Vector3 center)
        {
            // For castles - flat with battlements
            Mesh mesh = CreateFlatRoof(footprint, center);
            
            // TODO: Add battlement details as separate meshes
            
            return mesh;
        }
        
        #endregion
        
        #region Detail Generation
        
        private void AddWindows(GeneratedBuilding building, List<Vector3> footprint, float height, FantasyBuildingType type)
        {
            int floorsCount = Mathf.FloorToInt(height / baseFloorHeight);
            if (floorsCount < 1) return;
            
            // Simple window placement - one per wall segment per floor
            for (int i = 0; i < footprint.Count; i++)
            {
                int next = (i + 1) % footprint.Count;
                Vector3 wallStart = footprint[i];
                Vector3 wallEnd = footprint[next];
                float wallLength = Vector3.Distance(wallStart, wallEnd);
                
                if (wallLength < 3f) continue; // Wall too short
                
                int windowsPerFloor = Mathf.FloorToInt(wallLength / 4f);
                if (windowsPerFloor < 1) continue;
                
                for (int floor = 0; floor < floorsCount; floor++)
                {
                    for (int w = 0; w < windowsPerFloor; w++)
                    {
                        float t = (w + 0.5f) / windowsPerFloor;
                        Vector3 windowPos = Vector3.Lerp(wallStart, wallEnd, t);
                        windowPos.y = (floor + 0.5f) * baseFloorHeight + 1f;
                        
                        CreateWindowDetail(building, windowPos - building.gameObject.transform.position, 
                            Quaternion.LookRotation(wallEnd - wallStart), type);
                    }
                }
            }
        }
        
        private void CreateWindowDetail(GeneratedBuilding building, Vector3 localPos, Quaternion rotation, FantasyBuildingType type)
        {
            if (UnityEngine.Random.value > detailDensity) return;
            
            GameObject window = GameObject.CreatePrimitive(PrimitiveType.Quad);
            window.name = "Window";
            window.transform.SetParent(building.gameObject.transform);
            window.transform.localPosition = localPos;
            window.transform.rotation = rotation;
            window.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
            
            // Dark material for window
            var renderer = window.GetComponent<MeshRenderer>();
            if (detailMaterials != null && detailMaterials.Length > 0)
            {
                renderer.material = detailMaterials[0];
            }
            
            Destroy(window.GetComponent<Collider>());
        }
        
        private void AddDoor(GeneratedBuilding building, List<Vector3> footprint, FantasyBuildingType type)
        {
            if (footprint.Count < 2) return;
            
            // Place door on longest wall segment
            int longestWall = 0;
            float maxLength = 0;
            
            for (int i = 0; i < footprint.Count; i++)
            {
                int next = (i + 1) % footprint.Count;
                float length = Vector3.Distance(footprint[i], footprint[next]);
                if (length > maxLength)
                {
                    maxLength = length;
                    longestWall = i;
                }
            }
            
            Vector3 wallStart = footprint[longestWall];
            Vector3 wallEnd = footprint[(longestWall + 1) % footprint.Count];
            Vector3 doorPos = Vector3.Lerp(wallStart, wallEnd, 0.5f);
            doorPos.y = 1f;
            
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Quad);
            door.name = "Door";
            door.transform.SetParent(building.gameObject.transform);
            door.transform.localPosition = doorPos - building.gameObject.transform.position;
            door.transform.rotation = Quaternion.LookRotation(wallEnd - wallStart);
            door.transform.localScale = new Vector3(1.2f, 2f, 1f);
            
            var renderer = door.GetComponent<MeshRenderer>();
            if (detailMaterials != null && detailMaterials.Length > 1)
            {
                renderer.material = detailMaterials[1];
            }
            
            Destroy(door.GetComponent<Collider>());
        }
        
        private void AddChimney(GeneratedBuilding building, float height)
        {
            GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimney.name = "Chimney";
            chimney.transform.SetParent(building.gameObject.transform);
            
            float chimneyOffset = UnityEngine.Random.Range(-2f, 2f);
            chimney.transform.localPosition = new Vector3(chimneyOffset, height + 1.5f, 0);
            chimney.transform.localScale = new Vector3(0.6f, 3f, 0.6f);
            
            var renderer = chimney.GetComponent<MeshRenderer>();
            if (wallMaterials != null && wallMaterials.Length > 0)
            {
                renderer.material = wallMaterials[Mathf.Min(1, wallMaterials.Length - 1)];
            }
            
            Destroy(chimney.GetComponent<Collider>());
        }
        
        private void AddTowers(GeneratedBuilding building, List<Vector3> footprint, float height)
        {
            // Add towers at corners for castles
            int towerCount = Mathf.Min(4, footprint.Count);
            
            for (int i = 0; i < towerCount; i++)
            {
                if (UnityEngine.Random.value > 0.7f) continue;
                
                Vector3 cornerPos = footprint[i] - building.gameObject.transform.position;
                
                GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tower.name = $"Tower_{i}";
                tower.transform.SetParent(building.gameObject.transform);
                tower.transform.localPosition = cornerPos + Vector3.up * (height * 0.6f);
                tower.transform.localScale = new Vector3(2f, height * 1.2f, 2f);
                
                var renderer = tower.GetComponent<MeshRenderer>();
                if (wallMaterials != null && wallMaterials.Length > 0)
                {
                    renderer.material = wallMaterials[0];
                }
                
                Destroy(tower.GetComponent<Collider>());
                
                // Add conical roof to tower
                GameObject towerRoof = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                towerRoof.name = "TowerRoof";
                towerRoof.transform.SetParent(tower.transform);
                towerRoof.transform.localPosition = Vector3.up * 0.6f;
                towerRoof.transform.localScale = new Vector3(1.5f, 0.3f, 1.5f);
                
                if (roofMaterials != null && roofMaterials.Length > 0)
                {
                    towerRoof.GetComponent<MeshRenderer>().material = roofMaterials[0];
                }
                
                Destroy(towerRoof.GetComponent<Collider>());
            }
        }
        
        private void AddFlag(GeneratedBuilding building, float height)
        {
            GameObject flagpole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flagpole.name = "Flagpole";
            flagpole.transform.SetParent(building.gameObject.transform);
            flagpole.transform.localPosition = Vector3.up * (height + 2f);
            flagpole.transform.localScale = new Vector3(0.1f, 4f, 0.1f);
            
            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flag.name = "Flag";
            flag.transform.SetParent(flagpole.transform);
            flag.transform.localPosition = new Vector3(0.6f, 0.4f, 0);
            flag.transform.localScale = new Vector3(10f, 6f, 1f);
            
            Destroy(flagpole.GetComponent<Collider>());
            Destroy(flag.GetComponent<Collider>());
        }
        
        #endregion
        
        #region Prefab Generation
        
        private void InstantiatePrefabBuilding(GeneratedBuilding building, FantasyBuildingType type, float height)
        {
            BuildingPrefabSet prefabSet = GetPrefabSet(type);
            if (prefabSet == null || prefabSet.prefabs == null || prefabSet.prefabs.Length == 0)
            {
                // Fallback to procedural
                Debug.LogWarning($"No prefab set for {type}, using procedural");
                return;
            }
            
            GameObject prefab = prefabSet.prefabs[UnityEngine.Random.Range(0, prefabSet.prefabs.Length)];
            GameObject instance = Instantiate(prefab, building.gameObject.transform);
            instance.transform.localPosition = Vector3.zero;
            
            // Scale to approximate height
            float prefabHeight = GetPrefabHeight(instance);
            if (prefabHeight > 0)
            {
                float scale = height / prefabHeight;
                instance.transform.localScale = Vector3.one * Mathf.Clamp(scale, 0.5f, 2f);
            }
        }
        
        private BuildingPrefabSet GetPrefabSet(FantasyBuildingType type)
        {
            if (prefabSets == null) return null;
            
            foreach (var set in prefabSets)
            {
                if (set.buildingType == type) return set;
            }
            return null;
        }
        
        private float GetPrefabHeight(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 5f;
            
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }
            
            return bounds.size.y;
        }
        
        #endregion
        
        #region Materials & Styles
        
        private Material GetWallMaterial(FantasyBuildingType type)
        {
            if (wallMaterials == null || wallMaterials.Length == 0)
            {
                return new Material(Shader.Find("Standard"));
            }
            
            int index = type switch
            {
                FantasyBuildingType.Hut or FantasyBuildingType.Cottage => 0,
                FantasyBuildingType.House or FantasyBuildingType.Manor => Mathf.Min(1, wallMaterials.Length - 1),
                FantasyBuildingType.Castle or FantasyBuildingType.Fortress => Mathf.Min(2, wallMaterials.Length - 1),
                FantasyBuildingType.Cathedral => Mathf.Min(3, wallMaterials.Length - 1),
                _ => 0
            };
            
            return wallMaterials[index];
        }
        
        private Material GetRoofMaterial(FantasyBuildingType type)
        {
            if (roofMaterials == null || roofMaterials.Length == 0)
            {
                return new Material(Shader.Find("Standard"));
            }
            
            int index = type switch
            {
                FantasyBuildingType.Hut or FantasyBuildingType.Cottage => 0, // Thatch
                FantasyBuildingType.House or FantasyBuildingType.Tavern => Mathf.Min(1, roofMaterials.Length - 1), // Wood shingle
                FantasyBuildingType.Manor or FantasyBuildingType.Cathedral => Mathf.Min(2, roofMaterials.Length - 1), // Slate
                FantasyBuildingType.Castle or FantasyBuildingType.Fortress => Mathf.Min(3, roofMaterials.Length - 1), // Stone
                _ => 0
            };
            
            return roofMaterials[index];
        }
        
        private RoofStyle GetRoofStyle(FantasyBuildingType type)
        {
            return type switch
            {
                FantasyBuildingType.Hut => RoofStyle.Conical,
                FantasyBuildingType.Cottage or FantasyBuildingType.House => RoofStyle.Gabled,
                FantasyBuildingType.Manor => RoofStyle.Hipped,
                FantasyBuildingType.Castle or FantasyBuildingType.Fortress => RoofStyle.Crenellated,
                FantasyBuildingType.WatchTower => RoofStyle.Conical,
                FantasyBuildingType.Cathedral => RoofStyle.Gabled,
                _ => RoofStyle.Gabled
            };
        }
        
        private bool ShouldHaveChimney(FantasyBuildingType type)
        {
            return type is FantasyBuildingType.Cottage or FantasyBuildingType.House or 
                   FantasyBuildingType.Manor or FantasyBuildingType.Tavern or
                   FantasyBuildingType.Bakery;
        }
        
        private bool ShouldHaveTower(FantasyBuildingType type)
        {
            return type is FantasyBuildingType.Castle or FantasyBuildingType.Fortress or
                   FantasyBuildingType.Cathedral;
        }
        
        private bool ShouldHaveFlag(FantasyBuildingType type)
        {
            return type is FantasyBuildingType.Castle or FantasyBuildingType.Fortress or
                   FantasyBuildingType.Barracks;
        }
        
        #endregion
        
        #region LOD
        
        private void SetupLOD(GameObject buildingObj, FantasyBuildingType type)
        {
            LODGroup lodGroup = buildingObj.AddComponent<LODGroup>();
            
            Renderer[] renderers = buildingObj.GetComponentsInChildren<Renderer>();
            
            LOD[] lods = new LOD[3];
            lods[0] = new LOD(0.6f, renderers);
            lods[1] = new LOD(0.3f, renderers);
            lods[2] = new LOD(0.1f, renderers);
            
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }
        
        private void SetBuildingLOD(GeneratedBuilding building, int lodLevel)
        {
            // Would swap meshes for different LOD levels
            // Simplified implementation just shows/hides details
            foreach (Transform child in building.gameObject.transform)
            {
                bool showDetail = lodLevel == 0;
                if (child.name.Contains("Window") || child.name.Contains("Chimney") || 
                    child.name.Contains("Flag") || child.name.Contains("Door"))
                {
                    child.gameObject.SetActive(showDetail);
                }
            }
        }
        
        #endregion
        
        #region Utilities
        
        private float CalculateBuildingHeight(OSMBuilding building)
        {
            if (building.height > 0) return Mathf.Clamp(building.height, minBuildingHeight, maxBuildingHeight);
            if (building.levels > 0) return Mathf.Clamp(building.levels * baseFloorHeight, minBuildingHeight, maxBuildingHeight);
            
            // Estimate from area and type
            float estimatedHeight = building.fantasyType switch
            {
                FantasyBuildingType.Hut => 3f,
                FantasyBuildingType.Cottage => 5f,
                FantasyBuildingType.House => 7f,
                FantasyBuildingType.Manor => 12f,
                FantasyBuildingType.Castle => 25f,
                FantasyBuildingType.Cathedral => 30f,
                FantasyBuildingType.WatchTower => 15f,
                FantasyBuildingType.Tavern => 8f,
                _ => 6f
            };
            
            return estimatedHeight;
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
    public class GeneratedBuilding
    {
        public long osmId;
        public GameObject gameObject;
        public FantasyBuildingType fantasyType;
        public float height;
        public Mesh baseMesh;
        public GameObject roofObject;
    }
    
    [Serializable]
    public class BuildingPrefabSet
    {
        public FantasyBuildingType buildingType;
        public GameObject[] prefabs;
    }
    
    public enum RoofStyle
    {
        Flat,
        Gabled,
        Hipped,
        Conical,
        Crenellated,
        Domed
    }
    
    #endregion
}
