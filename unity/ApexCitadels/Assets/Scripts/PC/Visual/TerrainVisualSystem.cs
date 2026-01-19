// ============================================================================
// APEX CITADELS - TERRAIN VISUAL SYSTEM
// Creates beautiful 3D procedural terrain with heightmaps, textures, and foliage
// ============================================================================
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using ApexCitadels.Core;

namespace ApexCitadels.PC.Visual
{
    /// <summary>
    /// Creates a visually stunning 3D terrain for the world map.
    /// Replaces the flat colored plane with mountains, valleys, water, and vegetation.
    /// </summary>
    public class TerrainVisualSystem : MonoBehaviour
    {
        public static TerrainVisualSystem Instance { get; private set; }

        [Header("Terrain Settings")]
        public int terrainSize = 500;
        public int heightmapResolution = 513;
        public float maxHeight = 50f;
        public float waterLevel = 5f;

        [Header("Generated Objects")]
        private Terrain terrain;
        private TerrainData terrainData;
        private GameObject waterPlane;
        private List<GameObject> decorations = new List<GameObject>();

        // Terrain layer colors (will be converted to textures)
        private Color grassColor = new Color(0.2f, 0.5f, 0.1f);
        private Color dirtColor = new Color(0.4f, 0.3f, 0.2f);
        private Color rockColor = new Color(0.4f, 0.4f, 0.45f);
        private Color snowColor = new Color(0.95f, 0.95f, 1f);
        private Color sandColor = new Color(0.76f, 0.7f, 0.5f);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            CreateTerrain();
            CreateWater();
            CreateAtmosphere();
            PlaceDecorations();
            
            ApexLogger.Log("[OK] Terrain visual system initialized", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Creates the main terrain with procedural heightmap
        /// </summary>
        private void CreateTerrain()
        {
            // Create terrain data
            terrainData = new TerrainData();
            terrainData.heightmapResolution = heightmapResolution;
            terrainData.size = new Vector3(terrainSize, maxHeight, terrainSize);

            // Generate procedural heightmap
            float[,] heights = GenerateHeightmap();
            terrainData.SetHeights(0, 0, heights);

            // Create terrain layers (textures)
            CreateTerrainLayers();

            // Apply splatmap (texture blending)
            ApplySplatmap();

            // Create terrain game object
            GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
            terrainObj.name = "WorldTerrain";
            terrainObj.transform.position = new Vector3(-terrainSize / 2f, 0, -terrainSize / 2f);
            
            terrain = terrainObj.GetComponent<Terrain>();
            terrain.materialTemplate = CreateTerrainMaterial();
            
            // Enable terrain details
            terrain.detailObjectDistance = 150f;
            terrain.treeBillboardDistance = 100f;
            terrain.treeDistance = 200f;

            ApexLogger.LogVerbose("Created terrain with procedural heightmap", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Generates a procedural heightmap using multiple octaves of Perlin noise
        /// </summary>
        private float[,] GenerateHeightmap()
        {
            float[,] heights = new float[heightmapResolution, heightmapResolution];
            
            // Noise settings for varied terrain
            float baseScale = 0.003f;
            int octaves = 6;
            float persistence = 0.5f;
            float lacunarity = 2f;
            
            // Random seed offset
            float seedX = Random.Range(0f, 10000f);
            float seedY = Random.Range(0f, 10000f);

            for (int y = 0; y < heightmapResolution; y++)
            {
                for (int x = 0; x < heightmapResolution; x++)
                {
                    float amplitude = 1f;
                    float frequency = 1f;
                    float noiseValue = 0f;
                    float maxAmplitude = 0f;

                    // Multi-octave noise for natural looking terrain
                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x + seedX) * baseScale * frequency;
                        float sampleY = (y + seedY) * baseScale * frequency;
                        
                        float perlin = Mathf.PerlinNoise(sampleX, sampleY);
                        noiseValue += perlin * amplitude;
                        maxAmplitude += amplitude;
                        
                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }

                    // Normalize
                    noiseValue /= maxAmplitude;

                    // Add some plateaus for citadel placement
                    float distFromCenter = Vector2.Distance(
                        new Vector2(x, y),
                        new Vector2(heightmapResolution / 2f, heightmapResolution / 2f)
                    ) / (heightmapResolution / 2f);

                    // Create flatter areas in the middle for gameplay
                    if (distFromCenter < 0.6f)
                    {
                        noiseValue = Mathf.Lerp(noiseValue, 0.3f, (0.6f - distFromCenter) * 0.5f);
                    }

                    // Create mountain ranges on edges
                    if (distFromCenter > 0.8f)
                    {
                        noiseValue = Mathf.Lerp(noiseValue, 0.8f, (distFromCenter - 0.8f) * 2f);
                    }

                    heights[y, x] = noiseValue;
                }
            }

            return heights;
        }

        /// <summary>
        /// Creates terrain texture layers
        /// </summary>
        private void CreateTerrainLayers()
        {
            TerrainLayer[] layers = new TerrainLayer[4];

            // Grass layer
            layers[0] = CreateTerrainLayer("Grass", grassColor, 15f);
            
            // Dirt/path layer  
            layers[1] = CreateTerrainLayer("Dirt", dirtColor, 10f);
            
            // Rock layer
            layers[2] = CreateTerrainLayer("Rock", rockColor, 20f);
            
            // Snow layer (for mountain peaks)
            layers[3] = CreateTerrainLayer("Snow", snowColor, 25f);

            terrainData.terrainLayers = layers;
        }

        /// <summary>
        /// Creates a single terrain layer with a procedural texture
        /// </summary>
        private TerrainLayer CreateTerrainLayer(string name, Color baseColor, float tileSize)
        {
            TerrainLayer layer = new TerrainLayer();
            layer.name = name;
            layer.tileSize = new Vector2(tileSize, tileSize);
            
            // Create procedural texture
            Texture2D texture = CreateProceduralTexture(baseColor, 256);
            layer.diffuseTexture = texture;
            
            // Create normal map for depth
            Texture2D normalMap = CreateProceduralNormalMap(256);
            layer.normalMapTexture = normalMap;
            layer.normalScale = 0.5f;

            return layer;
        }

        /// <summary>
        /// Creates a procedural texture with noise variation
        /// </summary>
        private Texture2D CreateProceduralTexture(Color baseColor, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.wrapMode = TextureWrapMode.Repeat;
            
            Color[] pixels = new Color[size * size];
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Add noise variation
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    float detailNoise = Mathf.PerlinNoise(x * 0.5f, y * 0.5f) * 0.3f;
                    
                    float variation = 0.8f + (noise + detailNoise) * 0.2f;
                    
                    Color pixelColor = baseColor * variation;
                    pixelColor.a = 1f;
                    
                    pixels[y * size + x] = pixelColor;
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply(true);
            
            return texture;
        }

        /// <summary>
        /// Creates a procedural normal map for surface detail
        /// </summary>
        private Texture2D CreateProceduralNormalMap(int size)
        {
            Texture2D normalMap = new Texture2D(size, size, TextureFormat.RGBA32, true);
            normalMap.wrapMode = TextureWrapMode.Repeat;
            
            Color[] pixels = new Color[size * size];
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Generate height values for normal calculation
                    float h = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    float hL = Mathf.PerlinNoise((x - 1) * 0.1f, y * 0.1f);
                    float hR = Mathf.PerlinNoise((x + 1) * 0.1f, y * 0.1f);
                    float hU = Mathf.PerlinNoise(x * 0.1f, (y + 1) * 0.1f);
                    float hD = Mathf.PerlinNoise(x * 0.1f, (y - 1) * 0.1f);
                    
                    // Calculate normal
                    Vector3 normal = new Vector3(
                        (hL - hR) * 2f,
                        2f,
                        (hD - hU) * 2f
                    ).normalized;
                    
                    // Convert to color (0-1 range)
                    pixels[y * size + x] = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f,
                        1f
                    );
                }
            }
            
            normalMap.SetPixels(pixels);
            normalMap.Apply(true);
            
            return normalMap;
        }

        /// <summary>
        /// Applies texture blending based on height and slope
        /// </summary>
        private void ApplySplatmap()
        {
            int alphamapRes = terrainData.alphamapResolution;
            float[,,] splatmap = new float[alphamapRes, alphamapRes, 4];
            
            float[,] heights = terrainData.GetHeights(0, 0, alphamapRes, alphamapRes);

            for (int y = 0; y < alphamapRes; y++)
            {
                for (int x = 0; x < alphamapRes; x++)
                {
                    float height = heights[y, x];
                    
                    // Calculate slope
                    float slope = CalculateSlope(heights, x, y, alphamapRes);
                    
                    // Blend factors
                    float grassWeight = 0f;
                    float dirtWeight = 0f;
                    float rockWeight = 0f;
                    float snowWeight = 0f;

                    // Height-based blending
                    if (height < 0.2f)
                    {
                        // Low areas: mostly grass
                        grassWeight = 1f - height * 3f;
                        dirtWeight = height * 3f;
                    }
                    else if (height < 0.5f)
                    {
                        // Mid areas: grass and rock
                        float t = (height - 0.2f) / 0.3f;
                        grassWeight = 1f - t;
                        rockWeight = t * 0.5f;
                        dirtWeight = t * 0.5f;
                    }
                    else if (height < 0.75f)
                    {
                        // High areas: rock
                        float t = (height - 0.5f) / 0.25f;
                        rockWeight = 1f - t * 0.3f;
                        snowWeight = t * 0.3f;
                    }
                    else
                    {
                        // Mountain peaks: snow
                        float t = (height - 0.75f) / 0.25f;
                        rockWeight = 1f - t;
                        snowWeight = t;
                    }

                    // Steep slopes get more rock
                    if (slope > 0.4f)
                    {
                        float slopeInfluence = (slope - 0.4f) * 2f;
                        rockWeight += slopeInfluence;
                        grassWeight -= slopeInfluence * 0.5f;
                        dirtWeight -= slopeInfluence * 0.5f;
                    }

                    // Normalize weights
                    float total = grassWeight + dirtWeight + rockWeight + snowWeight;
                    if (total > 0)
                    {
                        splatmap[y, x, 0] = Mathf.Max(0, grassWeight / total);
                        splatmap[y, x, 1] = Mathf.Max(0, dirtWeight / total);
                        splatmap[y, x, 2] = Mathf.Max(0, rockWeight / total);
                        splatmap[y, x, 3] = Mathf.Max(0, snowWeight / total);
                    }
                    else
                    {
                        splatmap[y, x, 0] = 1f;
                    }
                }
            }

            terrainData.SetAlphamaps(0, 0, splatmap);
        }

        /// <summary>
        /// Calculates terrain slope at a point
        /// </summary>
        private float CalculateSlope(float[,] heights, int x, int y, int resolution)
        {
            float h = heights[y, x];
            float hL = x > 0 ? heights[y, x - 1] : h;
            float hR = x < resolution - 1 ? heights[y, x + 1] : h;
            float hU = y < resolution - 1 ? heights[y + 1, x] : h;
            float hD = y > 0 ? heights[y - 1, x] : h;

            float dx = (hR - hL) * 0.5f;
            float dy = (hU - hD) * 0.5f;

            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Creates terrain material with proper shading
        /// </summary>
        private Material CreateTerrainMaterial()
        {
            // Try shaders in order of preference for WebGL compatibility
            string[] shaderNames = {
                "Universal Render Pipeline/Terrain/Lit",
                "Nature/Terrain/Standard",
                "Nature/Terrain/Diffuse",
                "Mobile/Diffuse",
                "Unlit/Color",
                "Standard"
            };

            Shader terrainShader = null;
            foreach (var shaderName in shaderNames)
            {
                terrainShader = Shader.Find(shaderName);
                if (terrainShader != null)
                {
                    ApexLogger.LogVerbose($"Using terrain shader: {shaderName}", ApexLogger.LogCategory.General);
                    break;
                }
            }

            if (terrainShader == null)
            {
                ApexLogger.LogWarning("No suitable terrain shader found!", ApexLogger.LogCategory.General);
                terrainShader = Shader.Find("Diffuse");
            }

            Material mat = new Material(terrainShader);
            // Set a base green color for the terrain
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", new Color(0.3f, 0.5f, 0.2f));
            }
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", new Color(0.3f, 0.5f, 0.2f));
            }
            return mat;
        }

        /// <summary>
        /// Creates water plane with reflective material
        /// </summary>
        private void CreateWater()
        {
            waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterPlane.name = "WaterPlane";
            waterPlane.transform.position = new Vector3(0, waterLevel, 0);
            waterPlane.transform.localScale = new Vector3(terrainSize / 8f, 1, terrainSize / 8f);
            
            // Disable collider for water
            Destroy(waterPlane.GetComponent<Collider>());
            
            // Create water material
            Material waterMat = CreateWaterMaterial();
            waterPlane.GetComponent<Renderer>().material = waterMat;

            ApexLogger.LogVerbose("Created water plane", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Creates stylized water material
        /// </summary>
        private Material CreateWaterMaterial()
        {
            // Try shaders for water
            string[] shaderNames = {
                "Universal Render Pipeline/Lit",
                "Standard",
                "Legacy Shaders/Transparent/Diffuse",
                "Mobile/Diffuse"
            };

            Shader shader = null;
            foreach (var shaderName in shaderNames)
            {
                shader = Shader.Find(shaderName);
                if (shader != null) break;
            }
            
            if (shader == null) shader = Shader.Find("Diffuse");
            
            Material mat = new Material(shader);
            Color waterColor = new Color(0.1f, 0.3f, 0.5f, 0.7f);
            
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", waterColor);
            }
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", waterColor);
            }
            
            // Try to enable transparency if the shader supports it
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1); // Transparent
            }
            if (mat.HasProperty("_Blend"))
            {
                mat.SetFloat("_Blend", 0); // Alpha
            }
            
            // Metallic/smoothness for reflections
            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", 0.2f);
            }
            if (mat.HasProperty("_Smoothness") || mat.HasProperty("_Glossiness"))
            {
                mat.SetFloat("_Smoothness", 0.9f);
                mat.SetFloat("_Glossiness", 0.9f);
            }
            
            // Enable transparency rendering
            mat.renderQueue = 3000;
            mat.SetOverrideTag("RenderType", "Transparent");
            
            return mat;
        }

        /// <summary>
        /// Sets up atmospheric effects (fog, ambient)
        /// </summary>
        private void CreateAtmosphere()
        {
            // Enable distance fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.7f, 0.8f, 0.9f);
            RenderSettings.fogStartDistance = 100f;
            RenderSettings.fogEndDistance = 400f;

            // Ambient lighting
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.6f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.45f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.25f, 0.2f);

            ApexLogger.LogVerbose("Created atmospheric effects", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Places decorative objects (trees, rocks, etc.)
        /// </summary>
        private void PlaceDecorations()
        {
            // Place some basic tree representations
            int treeCount = 200;
            
            for (int i = 0; i < treeCount; i++)
            {
                Vector3 pos = GetRandomTerrainPosition(0.15f, 0.5f); // Between water and midheight
                if (pos != Vector3.zero)
                {
                    CreateTree(pos);
                }
            }

            // Place rocks at higher elevations
            int rockCount = 100;
            for (int i = 0; i < rockCount; i++)
            {
                Vector3 pos = GetRandomTerrainPosition(0.4f, 0.7f);
                if (pos != Vector3.zero)
                {
                    CreateRock(pos);
                }
            }

            ApexLogger.LogVerbose($"Placed {decorations.Count} decorations", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Gets a random position on terrain within height range
        /// </summary>
        private Vector3 GetRandomTerrainPosition(float minHeightNorm, float maxHeightNorm)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float x = Random.Range(-terrainSize / 2f + 10f, terrainSize / 2f - 10f);
                float z = Random.Range(-terrainSize / 2f + 10f, terrainSize / 2f - 10f);
                
                // Sample terrain height
                float normX = (x + terrainSize / 2f) / terrainSize;
                float normZ = (z + terrainSize / 2f) / terrainSize;
                
                float height = terrain.terrainData.GetInterpolatedHeight(normX, normZ);
                float normHeight = height / maxHeight;
                
                if (normHeight >= minHeightNorm && normHeight <= maxHeightNorm)
                {
                    return new Vector3(x, height, z);
                }
            }
            
            return Vector3.zero;
        }

        /// <summary>
        /// Creates a simple tree representation
        /// </summary>
        private void CreateTree(Vector3 position)
        {
            // Tree trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Tree";
            trunk.transform.position = position + Vector3.up * 2f;
            trunk.transform.localScale = new Vector3(0.3f, 4f, 0.3f);
            
            Material trunkMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            trunkMat.color = new Color(0.4f, 0.25f, 0.15f);
            trunk.GetComponent<Renderer>().material = trunkMat;
            
            // Foliage (cone shape)
            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = "Foliage";
            foliage.transform.SetParent(trunk.transform);
            foliage.transform.localPosition = new Vector3(0, 0.6f, 0);
            foliage.transform.localScale = new Vector3(8f, 3f, 8f);
            
            Material foliageMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            foliageMat.color = new Color(0.15f + Random.Range(0f, 0.1f), 0.4f + Random.Range(0f, 0.15f), 0.1f);
            foliage.GetComponent<Renderer>().material = foliageMat;
            
            // Random rotation and scale
            trunk.transform.Rotate(0, Random.Range(0f, 360f), 0);
            float scale = Random.Range(0.7f, 1.3f);
            trunk.transform.localScale *= scale;
            
            // Disable colliders
            Destroy(trunk.GetComponent<Collider>());
            Destroy(foliage.GetComponent<Collider>());
            
            decorations.Add(trunk);
        }

        /// <summary>
        /// Creates a simple rock representation
        /// </summary>
        private void CreateRock(Vector3 position)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Rock";
            rock.transform.position = position;
            
            // Squash and rotate for rock-like shape
            float scaleX = Random.Range(1f, 3f);
            float scaleY = Random.Range(0.5f, 1.5f);
            float scaleZ = Random.Range(1f, 3f);
            rock.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            rock.transform.Rotate(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-20f, 20f));
            
            Material rockMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            float gray = Random.Range(0.3f, 0.5f);
            rockMat.color = new Color(gray, gray, gray + 0.05f);
            rock.GetComponent<Renderer>().material = rockMat;
            
            Destroy(rock.GetComponent<Collider>());
            
            decorations.Add(rock);
        }

        /// <summary>
        /// Gets terrain height at world position
        /// </summary>
        public float GetHeightAt(Vector3 worldPos)
        {
            if (terrain == null) return 0f;
            
            float normX = (worldPos.x + terrainSize / 2f) / terrainSize;
            float normZ = (worldPos.z + terrainSize / 2f) / terrainSize;
            
            normX = Mathf.Clamp01(normX);
            normZ = Mathf.Clamp01(normZ);
            
            return terrain.terrainData.GetInterpolatedHeight(normX, normZ);
        }

        /// <summary>
        /// Gets a flat position suitable for placing a citadel
        /// </summary>
        public Vector3 GetFlatPositionNear(Vector3 worldPos, float radius = 20f)
        {
            Vector3 bestPos = worldPos;
            float bestFlatness = float.MaxValue;
            
            for (int i = 0; i < 20; i++)
            {
                Vector3 testPos = worldPos + new Vector3(
                    Random.Range(-radius, radius),
                    0,
                    Random.Range(-radius, radius)
                );
                
                float height = GetHeightAt(testPos);
                
                // Check surrounding points for flatness
                float flatness = 0f;
                for (int j = 0; j < 8; j++)
                {
                    float angle = j * 45f * Mathf.Deg2Rad;
                    Vector3 checkPos = testPos + new Vector3(Mathf.Cos(angle) * 5f, 0, Mathf.Sin(angle) * 5f);
                    float checkHeight = GetHeightAt(checkPos);
                    flatness += Mathf.Abs(checkHeight - height);
                }
                
                if (flatness < bestFlatness && height > waterLevel + 2f)
                {
                    bestFlatness = flatness;
                    bestPos = new Vector3(testPos.x, height, testPos.z);
                }
            }
            
            return bestPos;
        }
    }
}
