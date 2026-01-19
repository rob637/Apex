using Camera = UnityEngine.Camera;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApexCitadels.PC.Buildings
{
    /// <summary>
    /// Building LOD (Level of Detail) Manager for PC client.
    /// Handles dynamic model switching based on camera distance for performance.
    /// Supports multiple LOD levels and billboard fallbacks for distant buildings.
    /// </summary>
    public class BuildingLODManager : MonoBehaviour
    {
        [Header("LOD Settings")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private int maxActiveHighDetailBuildings = 50;
        
        [Header("LOD Distances")]
        [SerializeField] private float lod0Distance = 30f;   // Full detail
        [SerializeField] private float lod1Distance = 80f;   // Medium detail
        [SerializeField] private float lod2Distance = 200f;  // Low detail
        [SerializeField] private float cullDistance = 500f;   // Billboard/hidden
        
        [Header("Transition")]
        [SerializeField] private bool smoothTransitions = true;
        [SerializeField] private float transitionDuration = 0.3f;
        
        [Header("Billboard Settings")]
        [SerializeField] private bool useBillboards = true;
        [SerializeField] private int billboardResolution = 256;
        [SerializeField] private float billboardFadeDistance = 50f;
        
        [Header("Performance")]
        [SerializeField] private int buildingsPerFrame = 10;
        [SerializeField] private bool useAsyncLoading = true;
        
        // Singleton
        private static BuildingLODManager _instance;
        public static BuildingLODManager Instance => _instance;
        
        // Managed buildings
        private Dictionary<string, LODBuilding> _buildings = new Dictionary<string, LODBuilding>();
        private List<LODBuilding> _sortedByDistance = new List<LODBuilding>();
        
        // State
        private Camera _mainCamera;
        private float _lastUpdateTime;
        private int _currentProcessIndex;
        
        // Statistics
        public int TotalBuildings => _buildings.Count;
        public int HighDetailCount { get; private set; }
        public int MediumDetailCount { get; private set; }
        public int LowDetailCount { get; private set; }
        public int BillboardCount { get; private set; }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        private void Start()
        {
            _mainCamera = Camera.main;
        }
        
        private void Update()
        {
            if (!enableLOD || _mainCamera == null) return;
            
            if (Time.time - _lastUpdateTime >= updateInterval)
            {
                _lastUpdateTime = Time.time;
                UpdateLODLevels();
            }
            
            // Process smooth transitions
            if (smoothTransitions)
            {
                UpdateTransitions();
            }
        }
        
        #region Building Registration
        
        /// <summary>
        /// Register a building for LOD management
        /// </summary>
        public void RegisterBuilding(string id, GameObject buildingRoot, 
            GameObject[] lodLevels = null, Sprite billboardSprite = null)
        {
            if (_buildings.ContainsKey(id))
            {
                Debug.LogWarning($"[BuildingLOD] Building already registered: {id}");
                return;
            }
            
            var lodBuilding = new LODBuilding
            {
                Id = id,
                Root = buildingRoot,
                CurrentLOD = LODLevel.LOD0,
                TargetLOD = LODLevel.LOD0,
                LODObjects = new Dictionary<LODLevel, GameObject>()
            };
            
            // Store LOD objects
            if (lodLevels != null)
            {
                for (int i = 0; i < lodLevels.Length && i < 4; i++)
                {
                    lodBuilding.LODObjects[(LODLevel)i] = lodLevels[i];
                }
            }
            else
            {
                // Use root as LOD0 if no specific LODs provided
                lodBuilding.LODObjects[LODLevel.LOD0] = buildingRoot;
                
                // Generate simplified LODs procedurally
                if (buildingRoot != null)
                {
                    lodBuilding.LODObjects[LODLevel.LOD1] = GenerateLOD1(buildingRoot);
                    lodBuilding.LODObjects[LODLevel.LOD2] = GenerateLOD2(buildingRoot);
                }
            }
            
            // Create billboard
            if (useBillboards)
            {
                lodBuilding.Billboard = CreateBillboard(buildingRoot, billboardSprite);
                if (lodBuilding.Billboard != null)
                {
                    lodBuilding.Billboard.SetActive(false);
                }
            }
            
            _buildings[id] = lodBuilding;
            _sortedByDistance.Add(lodBuilding);
            
            // Set initial state
            SetBuildingLOD(lodBuilding, LODLevel.LOD0, immediate: true);
        }
        
        /// <summary>
        /// Unregister a building from LOD management
        /// </summary>
        public void UnregisterBuilding(string id)
        {
            if (_buildings.TryGetValue(id, out var building))
            {
                // Cleanup generated LODs
                foreach (var kvp in building.LODObjects)
                {
                    if (kvp.Key != LODLevel.LOD0 && kvp.Value != null)
                    {
                        Destroy(kvp.Value);
                    }
                }
                
                if (building.Billboard != null)
                {
                    Destroy(building.Billboard);
                }
                
                _buildings.Remove(id);
                _sortedByDistance.Remove(building);
            }
        }
        
        #endregion
        
        #region LOD Generation
        
        private GameObject GenerateLOD1(GameObject source)
        {
            // Create simplified version - remove small details
            var lod1 = new GameObject($"{source.name}_LOD1");
            lod1.transform.SetParent(source.transform.parent);
            lod1.transform.localPosition = source.transform.localPosition;
            lod1.transform.localRotation = source.transform.localRotation;
            lod1.SetActive(false);
            
            // Copy major geometry only (walls, roof)
            var renderers = source.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                // Skip small objects
                if (renderer.bounds.size.magnitude < 0.5f) continue;
                
                // Skip decoration objects
                if (renderer.gameObject.name.Contains("Window") ||
                    renderer.gameObject.name.Contains("Torch") ||
                    renderer.gameObject.name.Contains("Flag") ||
                    renderer.gameObject.name.Contains("Timber"))
                    continue;
                
                // Create simplified copy
                var copy = CreateSimplifiedCopy(renderer.gameObject, lod1.transform);
                if (copy != null)
                {
                    // Reduce mesh complexity (would use proper mesh simplification in production)
                    copy.transform.localScale *= 0.99f; // Slight scale to prevent z-fighting
                }
            }
            
            return lod1;
        }
        
        private GameObject GenerateLOD2(GameObject source)
        {
            // Create very simplified version - just main shapes
            var lod2 = new GameObject($"{source.name}_LOD2");
            lod2.transform.SetParent(source.transform.parent);
            lod2.transform.localPosition = source.transform.localPosition;
            lod2.transform.localRotation = source.transform.localRotation;
            lod2.SetActive(false);
            
            // Calculate bounds of source
            var bounds = CalculateBounds(source);
            
            // Create simple box representation
            var boxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxObj.name = "LOD2_Box";
            boxObj.transform.SetParent(lod2.transform);
            boxObj.transform.localPosition = bounds.center - source.transform.position;
            boxObj.transform.localScale = bounds.size;
            
            // Apply average color from source
            var avgColor = CalculateAverageColor(source);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = avgColor;
            boxObj.GetComponent<Renderer>().material = mat;
            
            Destroy(boxObj.GetComponent<Collider>());
            
            return lod2;
        }
        
        private GameObject CreateSimplifiedCopy(GameObject source, Transform parent)
        {
            var meshFilter = source.GetComponent<MeshFilter>();
            var renderer = source.GetComponent<Renderer>();
            
            if (meshFilter == null || renderer == null) return null;
            
            var copy = new GameObject(source.name);
            copy.transform.SetParent(parent);
            copy.transform.localPosition = source.transform.localPosition;
            copy.transform.localRotation = source.transform.localRotation;
            copy.transform.localScale = source.transform.localScale;
            
            // Copy mesh (would simplify in production)
            var newFilter = copy.AddComponent<MeshFilter>();
            newFilter.sharedMesh = meshFilter.sharedMesh;
            
            var newRenderer = copy.AddComponent<MeshRenderer>();
            newRenderer.sharedMaterials = renderer.sharedMaterials;
            
            return copy;
        }
        
        private GameObject CreateBillboard(GameObject source, Sprite sprite = null)
        {
            if (source == null) return null;
            
            var billboard = new GameObject($"{source.name}_Billboard");
            billboard.transform.SetParent(source.transform.parent);
            
            var bounds = CalculateBounds(source);
            billboard.transform.localPosition = source.transform.localPosition + 
                new Vector3(0, bounds.size.y / 2f, 0);
            
            // Add billboard component
            var billboardQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            billboardQuad.name = "BillboardQuad";
            billboardQuad.transform.SetParent(billboard.transform);
            billboardQuad.transform.localPosition = Vector3.zero;
            billboardQuad.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1f);
            
            Destroy(billboardQuad.GetComponent<Collider>());
            
            // Material
            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
            if (sprite != null)
            {
                mat.mainTexture = sprite.texture;
            }
            else
            {
                // Generate billboard texture from source (simplified)
                mat.color = CalculateAverageColor(source);
            }
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            billboardQuad.GetComponent<Renderer>().material = mat;
            
            // Add billboard rotation script
            billboard.AddComponent<BillboardFaceCamera>();
            
            return billboard;
        }
        
        private Bounds CalculateBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(obj.transform.position, Vector3.one);
            }
            
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }
        
        private Color CalculateAverageColor(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return Color.gray;
            
            Color total = Color.black;
            int count = 0;
            
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial != null)
                {
                    total += renderer.sharedMaterial.color;
                    count++;
                }
            }
            
            return count > 0 ? total / count : Color.gray;
        }
        
        #endregion
        
        #region LOD Updates
        
        private void UpdateLODLevels()
        {
            if (_mainCamera == null) return;
            
            Vector3 cameraPos = _mainCamera.transform.position;
            
            // Reset statistics
            HighDetailCount = 0;
            MediumDetailCount = 0;
            LowDetailCount = 0;
            BillboardCount = 0;
            
            // Update distances and sort
            foreach (var building in _buildings.Values)
            {
                if (building.Root == null) continue;
                building.Distance = Vector3.Distance(cameraPos, building.Root.transform.position);
            }
            
            _sortedByDistance.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            
            // Assign LOD levels
            int highDetailAssigned = 0;
            
            foreach (var building in _sortedByDistance)
            {
                if (building.Root == null) continue;
                
                LODLevel targetLOD;
                
                if (building.Distance < lod0Distance && highDetailAssigned < maxActiveHighDetailBuildings)
                {
                    targetLOD = LODLevel.LOD0;
                    highDetailAssigned++;
                    HighDetailCount++;
                }
                else if (building.Distance < lod1Distance)
                {
                    targetLOD = LODLevel.LOD1;
                    MediumDetailCount++;
                }
                else if (building.Distance < lod2Distance)
                {
                    targetLOD = LODLevel.LOD2;
                    LowDetailCount++;
                }
                else if (building.Distance < cullDistance)
                {
                    targetLOD = LODLevel.Billboard;
                    BillboardCount++;
                }
                else
                {
                    targetLOD = LODLevel.Culled;
                }
                
                if (building.TargetLOD != targetLOD)
                {
                    building.TargetLOD = targetLOD;
                    building.TransitionProgress = 0f;
                }
            }
        }
        
        private void UpdateTransitions()
        {
            int processed = 0;
            
            foreach (var building in _buildings.Values)
            {
                if (processed >= buildingsPerFrame) break;
                
                if (building.CurrentLOD != building.TargetLOD)
                {
                    if (smoothTransitions && transitionDuration > 0)
                    {
                        building.TransitionProgress += Time.deltaTime / transitionDuration;
                        
                        if (building.TransitionProgress >= 1f)
                        {
                            SetBuildingLOD(building, building.TargetLOD, immediate: true);
                        }
                        else
                        {
                            // Cross-fade between LODs
                            UpdateLODCrossfade(building);
                        }
                    }
                    else
                    {
                        SetBuildingLOD(building, building.TargetLOD, immediate: true);
                    }
                    
                    processed++;
                }
            }
        }
        
        private void SetBuildingLOD(LODBuilding building, LODLevel level, bool immediate)
        {
            building.CurrentLOD = level;
            building.TransitionProgress = 1f;
            
            // Hide all LOD objects
            foreach (var kvp in building.LODObjects)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(false);
                }
            }
            
            if (building.Billboard != null)
            {
                building.Billboard.SetActive(false);
            }
            
            // Show appropriate LOD
            switch (level)
            {
                case LODLevel.LOD0:
                case LODLevel.LOD1:
                case LODLevel.LOD2:
                    if (building.LODObjects.TryGetValue(level, out var lodObj) && lodObj != null)
                    {
                        lodObj.SetActive(true);
                    }
                    else if (building.LODObjects.TryGetValue(LODLevel.LOD0, out var fallback) && fallback != null)
                    {
                        // Fallback to LOD0 if specific LOD not available
                        fallback.SetActive(true);
                    }
                    break;
                    
                case LODLevel.Billboard:
                    if (building.Billboard != null)
                    {
                        building.Billboard.SetActive(true);
                    }
                    break;
                    
                case LODLevel.Culled:
                    // Everything stays hidden
                    break;
            }
        }
        
        private void UpdateLODCrossfade(LODBuilding building)
        {
            // For smooth transitions, show both LODs with alpha fade
            // This is simplified - full implementation would use material properties
            
            float t = building.TransitionProgress;
            
            // Show current LOD with fading out
            if (building.LODObjects.TryGetValue(building.CurrentLOD, out var currentObj) && currentObj != null)
            {
                currentObj.SetActive(true);
                SetObjectAlpha(currentObj, 1f - t);
            }
            
            // Show target LOD with fading in
            if (building.LODObjects.TryGetValue(building.TargetLOD, out var targetObj) && targetObj != null)
            {
                targetObj.SetActive(true);
                SetObjectAlpha(targetObj, t);
            }
        }
        
        private void SetObjectAlpha(GameObject obj, float alpha)
        {
            // Simplified alpha setting
            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.material != null)
                {
                    var color = renderer.material.color;
                    color.a = alpha;
                    renderer.material.color = color;
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Force update all LOD levels immediately
        /// </summary>
        public void ForceUpdateAll()
        {
            UpdateLODLevels();
            
            foreach (var building in _buildings.Values)
            {
                SetBuildingLOD(building, building.TargetLOD, immediate: true);
            }
        }
        
        /// <summary>
        /// Set LOD distances
        /// </summary>
        public void SetLODDistances(float lod0, float lod1, float lod2, float cull)
        {
            lod0Distance = lod0;
            lod1Distance = lod1;
            lod2Distance = lod2;
            cullDistance = cull;
        }
        
        /// <summary>
        /// Get building's current LOD level
        /// </summary>
        public LODLevel GetBuildingLOD(string id)
        {
            return _buildings.TryGetValue(id, out var building) ? building.CurrentLOD : LODLevel.Culled;
        }
        
        /// <summary>
        /// Get building's distance from camera
        /// </summary>
        public float GetBuildingDistance(string id)
        {
            return _buildings.TryGetValue(id, out var building) ? building.Distance : float.MaxValue;
        }
        
        #endregion
    }
    
    #region Helper Classes
    
    public enum LODLevel
    {
        LOD0,       // Full detail
        LOD1,       // Medium detail
        LOD2,       // Low detail
        Billboard,  // 2D sprite
        Culled      // Hidden
    }
    
    public class LODBuilding
    {
        public string Id;
        public GameObject Root;
        public Dictionary<LODLevel, GameObject> LODObjects;
        public GameObject Billboard;
        public LODLevel CurrentLOD;
        public LODLevel TargetLOD;
        public float Distance;
        public float TransitionProgress;
    }
    
    /// <summary>
    /// Makes an object always face the camera
    /// </summary>
    public class BillboardFaceCamera : MonoBehaviour
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
                transform.LookAt(transform.position + _camera.transform.forward);
            }
        }
    }
    
    #endregion
}
