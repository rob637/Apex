// ============================================================================
// APEX CITADELS - PROCEDURAL TERRAIN SYSTEM
// Creates beautiful, dynamic terrain for the world map
// ============================================================================
using UnityEngine;
using System.Collections.Generic;

namespace ApexCitadels.PC.Environment
{
    /// <summary>
    /// Generates procedural terrain with hills, textures, and water.
    /// Creates an immersive world map foundation.
    /// </summary>
    public class ProceduralTerrain : MonoBehaviour
    {
        public static ProceduralTerrain Instance { get; private set; }

        [Header("Terrain Size")]
        [SerializeField] private int terrainSize = 2000;        // World units
        [SerializeField] private int resolution = 256;          // Mesh resolution
        [SerializeField] private float maxHeight = 50f;         // Max terrain height

        [Header("Noise Settings")]
        [SerializeField] private float noiseScale = 0.005f;     // Lower = larger features
        [SerializeField] private int octaves = 4;               // Detail layers
        [SerializeField] private float persistence = 0.5f;      // How much each octave contributes
        [SerializeField] private float lacunarity = 2f;         // Frequency multiplier per octave
        [SerializeField] private int seed = 42;

        [Header("Terrain Colors")]
        [SerializeField] private Color deepWaterColor = new Color(0.1f, 0.2f, 0.4f);
        [SerializeField] private Color shallowWaterColor = new Color(0.2f, 0.4f, 0.6f);
        [SerializeField] private Color sandColor = new Color(0.76f, 0.7f, 0.5f);
        [SerializeField] private Color grassColor = new Color(0.2f, 0.5f, 0.2f);
        [SerializeField] private Color darkGrassColor = new Color(0.15f, 0.4f, 0.15f);
        [SerializeField] private Color rockColor = new Color(0.4f, 0.4f, 0.35f);
        [SerializeField] private Color snowColor = new Color(0.95f, 0.95f, 0.95f);

        [Header("Height Thresholds (0-1)")]
        [SerializeField] private float waterLevel = 0.3f;
        [SerializeField] private float sandLevel = 0.35f;
        [SerializeField] private float grassLevel = 0.6f;
        [SerializeField] private float rockLevel = 0.8f;

        [Header("Water")]
        [SerializeField] private bool createWater = true;
        [SerializeField] private float waterHeight = 0f;
        [SerializeField] private Color waterTint = new Color(0.2f, 0.5f, 0.7f, 0.8f);

        [Header("Grid Overlay")]
        [SerializeField] private bool showGrid = true;
        [SerializeField] private float gridSpacing = 100f;
        [SerializeField] private Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

        // Generated objects
        private GameObject _terrainObject;
        private GameObject _waterObject;
        private GameObject _gridObject;
        private Mesh _terrainMesh;
        private float[,] _heightMap;

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
            GenerateTerrain();
        }

        /// <summary>
        /// Generate the full terrain system
        /// </summary>
        public void GenerateTerrain()
        {
            Debug.Log("[Terrain] Generating procedural terrain...");
            
            // Clean up existing
            if (_terrainObject != null) Destroy(_terrainObject);
            if (_waterObject != null) Destroy(_waterObject);
            if (_gridObject != null) Destroy(_gridObject);

            // Generate heightmap
            _heightMap = GenerateHeightMap();

            // Create terrain mesh
            CreateTerrainMesh();

            // Create water plane
            if (createWater)
            {
                CreateWaterPlane();
            }

            // Create grid overlay
            if (showGrid)
            {
                CreateGridOverlay();
            }

            Debug.Log("[Terrain] Terrain generation complete!");
        }

        /// <summary>
        /// Generate height map using Perlin noise with multiple octaves
        /// </summary>
        private float[,] GenerateHeightMap()
        {
            float[,] map = new float[resolution + 1, resolution + 1];
            
            System.Random prng = new System.Random(seed);
            Vector2[] octaveOffsets = new Vector2[octaves];
            
            for (int i = 0; i < octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000);
                float offsetY = prng.Next(-100000, 100000);
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;

            float halfSize = resolution / 2f;

            for (int y = 0; y <= resolution; y++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    float amplitude = 1f;
                    float frequency = 1f;
                    float noiseHeight = 0f;

                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x - halfSize) * noiseScale * frequency + octaveOffsets[i].x;
                        float sampleY = (y - halfSize) * noiseScale * frequency + octaveOffsets[i].y;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }

                    if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                    if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;

                    map[x, y] = noiseHeight;
                }
            }

            // Normalize
            for (int y = 0; y <= resolution; y++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    map[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, map[x, y]);
                    
                    // Apply falloff at edges (makes map island-like)
                    float edgeX = Mathf.Abs(x - halfSize) / halfSize;
                    float edgeY = Mathf.Abs(y - halfSize) / halfSize;
                    float edgeFalloff = Mathf.Max(edgeX, edgeY);
                    edgeFalloff = Mathf.Pow(edgeFalloff, 2f);
                    
                    map[x, y] = Mathf.Clamp01(map[x, y] - edgeFalloff * 0.5f);
                }
            }

            return map;
        }

        /// <summary>
        /// Create the terrain mesh with vertex colors
        /// </summary>
        private void CreateTerrainMesh()
        {
            _terrainObject = new GameObject("ProceduralTerrain");
            _terrainObject.transform.parent = transform;
            _terrainObject.transform.localPosition = Vector3.zero;

            MeshFilter meshFilter = _terrainObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = _terrainObject.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = _terrainObject.AddComponent<MeshCollider>();

            _terrainMesh = new Mesh();
            _terrainMesh.name = "TerrainMesh";
            _terrainMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Support large meshes

            // Generate vertices, UVs, and colors
            Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
            Vector2[] uvs = new Vector2[(resolution + 1) * (resolution + 1)];
            Color[] colors = new Color[(resolution + 1) * (resolution + 1)];

            float unitSize = terrainSize / (float)resolution;
            float halfTerrain = terrainSize / 2f;

            for (int y = 0; y <= resolution; y++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    int index = y * (resolution + 1) + x;
                    
                    float height = _heightMap[x, y] * maxHeight;
                    
                    vertices[index] = new Vector3(
                        x * unitSize - halfTerrain,
                        height,
                        y * unitSize - halfTerrain
                    );

                    uvs[index] = new Vector2(x / (float)resolution, y / (float)resolution);
                    colors[index] = GetTerrainColor(_heightMap[x, y]);
                }
            }

            // Generate triangles
            int[] triangles = new int[resolution * resolution * 6];
            int triIndex = 0;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int vertIndex = y * (resolution + 1) + x;

                    // First triangle
                    triangles[triIndex] = vertIndex;
                    triangles[triIndex + 1] = vertIndex + resolution + 1;
                    triangles[triIndex + 2] = vertIndex + 1;

                    // Second triangle
                    triangles[triIndex + 3] = vertIndex + 1;
                    triangles[triIndex + 4] = vertIndex + resolution + 1;
                    triangles[triIndex + 5] = vertIndex + resolution + 2;

                    triIndex += 6;
                }
            }

            _terrainMesh.vertices = vertices;
            _terrainMesh.uv = uvs;
            _terrainMesh.colors = colors;
            _terrainMesh.triangles = triangles;
            _terrainMesh.RecalculateNormals();
            _terrainMesh.RecalculateBounds();

            meshFilter.mesh = _terrainMesh;
            meshCollider.sharedMesh = _terrainMesh;

            // Create terrain material with vertex colors
            Material terrainMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (terrainMat.shader == null || terrainMat.shader.name == "Hidden/InternalErrorShader")
            {
                terrainMat = new Material(Shader.Find("Standard"));
            }
            
            // Enable vertex colors
            terrainMat.EnableKeyword("_VERTEXCOLOR");
            terrainMat.SetFloat("_Smoothness", 0.2f);
            
            meshRenderer.material = terrainMat;
            
            // Set layer for raycasting
            _terrainObject.layer = LayerMask.NameToLayer("Default");

            Debug.Log($"[Terrain] Created mesh with {vertices.Length} vertices");
        }

        /// <summary>
        /// Get terrain color based on height
        /// </summary>
        private Color GetTerrainColor(float normalizedHeight)
        {
            if (normalizedHeight < waterLevel)
            {
                return Color.Lerp(deepWaterColor, shallowWaterColor, normalizedHeight / waterLevel);
            }
            else if (normalizedHeight < sandLevel)
            {
                float t = (normalizedHeight - waterLevel) / (sandLevel - waterLevel);
                return Color.Lerp(shallowWaterColor, sandColor, t);
            }
            else if (normalizedHeight < grassLevel)
            {
                float t = (normalizedHeight - sandLevel) / (grassLevel - sandLevel);
                return Color.Lerp(sandColor, grassColor, t * 0.3f);
            }
            else if (normalizedHeight < rockLevel)
            {
                float t = (normalizedHeight - grassLevel) / (rockLevel - grassLevel);
                return Color.Lerp(grassColor, darkGrassColor, t);
            }
            else
            {
                float t = (normalizedHeight - rockLevel) / (1f - rockLevel);
                return Color.Lerp(rockColor, snowColor, t);
            }
        }

        /// <summary>
        /// Create a water plane at water level
        /// </summary>
        private void CreateWaterPlane()
        {
            _waterObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _waterObject.name = "WaterPlane";
            _waterObject.transform.parent = transform;
            _waterObject.transform.localPosition = new Vector3(0, waterHeight + maxHeight * waterLevel * 0.8f, 0);
            _waterObject.transform.localScale = new Vector3(terrainSize / 10f, 1, terrainSize / 10f);

            // Remove collider (don't block clicks)
            Destroy(_waterObject.GetComponent<Collider>());

            // Create semi-transparent water material
            Material waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (waterMat.shader == null || waterMat.shader.name == "Hidden/InternalErrorShader")
            {
                waterMat = new Material(Shader.Find("Standard"));
            }
            
            waterMat.SetFloat("_Surface", 1); // Transparent
            waterMat.SetFloat("_Blend", 0);   // Alpha blend
            waterMat.SetColor("_BaseColor", waterTint);
            waterMat.SetFloat("_Smoothness", 0.9f);
            waterMat.SetFloat("_Metallic", 0.1f);
            
            // Enable transparency
            waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            waterMat.SetInt("_ZWrite", 0);
            waterMat.DisableKeyword("_ALPHATEST_ON");
            waterMat.EnableKeyword("_ALPHABLEND_ON");
            waterMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            waterMat.renderQueue = 3000;
            
            _waterObject.GetComponent<Renderer>().material = waterMat;
            
            Debug.Log("[Terrain] Water plane created");
        }

        /// <summary>
        /// Create grid overlay for strategy game feel
        /// </summary>
        private void CreateGridOverlay()
        {
            _gridObject = new GameObject("GridOverlay");
            _gridObject.transform.parent = transform;
            _gridObject.transform.localPosition = new Vector3(0, 0.5f, 0); // Slightly above terrain

            // Create line renderer for grid
            int gridLines = Mathf.CeilToInt(terrainSize / gridSpacing) * 2 + 2;
            float halfTerrain = terrainSize / 2f;

            // Use LineRenderer for each grid line (simple approach)
            // For better performance, could use a single mesh with lines
            
            Material lineMat = new Material(Shader.Find("Sprites/Default"));
            lineMat.color = gridColor;

            // Horizontal lines
            for (float z = -halfTerrain; z <= halfTerrain; z += gridSpacing)
            {
                GameObject line = new GameObject($"GridH_{z}");
                line.transform.parent = _gridObject.transform;
                
                LineRenderer lr = line.AddComponent<LineRenderer>();
                lr.material = lineMat;
                lr.startWidth = 0.5f;
                lr.endWidth = 0.5f;
                lr.startColor = gridColor;
                lr.endColor = gridColor;
                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(-halfTerrain, GetTerrainHeight(-halfTerrain, z) + 1f, z));
                lr.SetPosition(1, new Vector3(halfTerrain, GetTerrainHeight(halfTerrain, z) + 1f, z));
                lr.useWorldSpace = true;
            }

            // Vertical lines
            for (float x = -halfTerrain; x <= halfTerrain; x += gridSpacing)
            {
                GameObject line = new GameObject($"GridV_{x}");
                line.transform.parent = _gridObject.transform;
                
                LineRenderer lr = line.AddComponent<LineRenderer>();
                lr.material = lineMat;
                lr.startWidth = 0.5f;
                lr.endWidth = 0.5f;
                lr.startColor = gridColor;
                lr.endColor = gridColor;
                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(x, GetTerrainHeight(x, -halfTerrain) + 1f, -halfTerrain));
                lr.SetPosition(1, new Vector3(x, GetTerrainHeight(x, halfTerrain) + 1f, halfTerrain));
                lr.useWorldSpace = true;
            }

            Debug.Log("[Terrain] Grid overlay created");
        }

        /// <summary>
        /// Get terrain height at world position
        /// </summary>
        public float GetTerrainHeight(float worldX, float worldZ)
        {
            if (_heightMap == null) return 0;

            float halfTerrain = terrainSize / 2f;
            float unitSize = terrainSize / (float)resolution;

            // Convert world pos to heightmap coords
            int x = Mathf.Clamp(Mathf.RoundToInt((worldX + halfTerrain) / unitSize), 0, resolution);
            int z = Mathf.Clamp(Mathf.RoundToInt((worldZ + halfTerrain) / unitSize), 0, resolution);

            return _heightMap[x, z] * maxHeight;
        }

        /// <summary>
        /// Check if position is water
        /// </summary>
        public bool IsWater(float worldX, float worldZ)
        {
            if (_heightMap == null) return false;

            float halfTerrain = terrainSize / 2f;
            float unitSize = terrainSize / (float)resolution;

            int x = Mathf.Clamp(Mathf.RoundToInt((worldX + halfTerrain) / unitSize), 0, resolution);
            int z = Mathf.Clamp(Mathf.RoundToInt((worldZ + halfTerrain) / unitSize), 0, resolution);

            return _heightMap[x, z] < waterLevel;
        }

        /// <summary>
        /// Regenerate with new seed
        /// </summary>
        public void RegenerateWithSeed(int newSeed)
        {
            seed = newSeed;
            GenerateTerrain();
        }

        /// <summary>
        /// Toggle grid visibility
        /// </summary>
        public void SetGridVisible(bool visible)
        {
            showGrid = visible;
            if (_gridObject != null)
            {
                _gridObject.SetActive(visible);
            }
        }
    }
}
