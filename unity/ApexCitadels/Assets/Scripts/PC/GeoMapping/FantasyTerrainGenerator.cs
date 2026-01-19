using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Fantasy Terrain Generator - Creates rolling fantasy hills and terrain
    /// from elevation data. Transforms flat OSM data into immersive 3D landscapes.
    /// </summary>
    public class FantasyTerrainGenerator : MonoBehaviour
    {
        [Header("Terrain Settings")]
        [SerializeField] private int terrainResolution = 257;
        [SerializeField] private float terrainSize = 1000f;
        [SerializeField] private float heightScale = 50f;
        [SerializeField] private float baseTerrainHeight = 0f;
        
        [Header("Fantasy Terrain Features")]
        [SerializeField] private bool addFantasyHills = true;
        [SerializeField] private bool addMagicalGlens = true;
        [SerializeField] private bool addAncientRuins = true;
        [SerializeField] private float fantasyHeightMultiplier = 1.5f;
        [SerializeField] private float hillFrequency = 0.02f;
        [SerializeField] private float detailFrequency = 0.1f;
        
        [Header("Terrain Layers")]
        [SerializeField] private TerrainLayer[] terrainLayers;
        [SerializeField] private float grassSteepnessThreshold = 30f;
        [SerializeField] private float rockSteepnessThreshold = 45f;
        [SerializeField] private float snowHeightThreshold = 40f;
        
        [Header("Vegetation")]
        [SerializeField] private bool placeVegetation = true;
        [SerializeField] private TreePrototype[] treePrototypes;
        [SerializeField] private DetailPrototype[] detailPrototypes;
        [SerializeField] private float treeDensity = 0.3f;
        [SerializeField] private float grassDensity = 0.5f;
        
        [Header("Fantasy Decorations")]
        [SerializeField] private GameObject[] ruinPrefabs;
        [SerializeField] private GameObject[] standingStonesPrefabs;
        [SerializeField] private GameObject[] mysticalTreePrefabs;
        [SerializeField] private float decorationDensity = 0.1f;
        
        [Header("Water")]
        [SerializeField] private Material waterMaterial;
        [SerializeField] private float waterLevel = 0f;
        [SerializeField] private bool createWaterPlane = true;
        
        // Singleton
        private static FantasyTerrainGenerator _instance;
        public static FantasyTerrainGenerator Instance => _instance;
        
        // Generated terrain
        private Terrain _generatedTerrain;
        private TerrainData _terrainData;
        private GameObject _waterPlane;
        private List<GameObject> _decorations = new List<GameObject>();
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        #region Public API
        
        /// <summary>
        /// Generate terrain from elevation data
        /// </summary>
        public async Task<Terrain> GenerateTerrain(ElevationData elevationData, OSMAreaData areaData, Vector3 worldOrigin)
        {
            if (_generatedTerrain != null)
            {
                DestroyTerrain();
            }
            
            // Create terrain data
            _terrainData = new TerrainData();
            _terrainData.heightmapResolution = terrainResolution;
            _terrainData.size = new Vector3(terrainSize, heightScale, terrainSize);
            
            // Generate heightmap
            float[,] heights = await GenerateHeightmap(elevationData, areaData);
            _terrainData.SetHeights(0, 0, heights);
            
            // Apply terrain layers
            if (terrainLayers != null && terrainLayers.Length > 0)
            {
                _terrainData.terrainLayers = terrainLayers;
                ApplyTerrainTextures();
            }
            
            // Create terrain object
            GameObject terrainObj = Terrain.CreateTerrainGameObject(_terrainData);
            terrainObj.name = "FantasyTerrain";
            terrainObj.transform.position = worldOrigin - new Vector3(terrainSize / 2, 0, terrainSize / 2);
            
            _generatedTerrain = terrainObj.GetComponent<Terrain>();
            
            // Add vegetation
            if (placeVegetation)
            {
                await PlaceVegetation(areaData);
            }
            
            // Add fantasy decorations
            if (decorationDensity > 0)
            {
                PlaceFantasyDecorations();
            }
            
            // Create water
            if (createWaterPlane)
            {
                CreateWater(worldOrigin);
            }
            
            return _generatedTerrain;
        }
        
        /// <summary>
        /// Generate terrain without elevation data (procedural only)
        /// </summary>
        public Terrain GenerateProceduralTerrain(Vector3 worldOrigin, Vector2d centerGeo)
        {
            if (_generatedTerrain != null)
            {
                DestroyTerrain();
            }
            
            _terrainData = new TerrainData();
            _terrainData.heightmapResolution = terrainResolution;
            _terrainData.size = new Vector3(terrainSize, heightScale, terrainSize);
            
            // Generate procedural heightmap
            float[,] heights = GenerateProceduralHeightmap(centerGeo);
            _terrainData.SetHeights(0, 0, heights);
            
            // Apply terrain layers
            if (terrainLayers != null && terrainLayers.Length > 0)
            {
                _terrainData.terrainLayers = terrainLayers;
                ApplyTerrainTextures();
            }
            
            // Create terrain object
            GameObject terrainObj = Terrain.CreateTerrainGameObject(_terrainData);
            terrainObj.name = "FantasyTerrain";
            terrainObj.transform.position = worldOrigin - new Vector3(terrainSize / 2, 0, terrainSize / 2);
            
            _generatedTerrain = terrainObj.GetComponent<Terrain>();
            
            // Create water
            if (createWaterPlane)
            {
                CreateWater(worldOrigin);
            }
            
            return _generatedTerrain;
        }
        
        /// <summary>
        /// Destroy generated terrain
        /// </summary>
        public void DestroyTerrain()
        {
            if (_generatedTerrain != null)
            {
                Destroy(_generatedTerrain.gameObject);
                _generatedTerrain = null;
            }
            
            if (_terrainData != null)
            {
                Destroy(_terrainData);
                _terrainData = null;
            }
            
            if (_waterPlane != null)
            {
                Destroy(_waterPlane);
                _waterPlane = null;
            }
            
            foreach (var decoration in _decorations)
            {
                if (decoration != null) Destroy(decoration);
            }
            _decorations.Clear();
        }
        
        /// <summary>
        /// Get terrain height at world position
        /// </summary>
        public float GetHeightAtPosition(Vector3 worldPos)
        {
            if (_generatedTerrain == null) return 0f;
            return _generatedTerrain.SampleHeight(worldPos);
        }
        
        /// <summary>
        /// Flatten terrain area for building placement
        /// </summary>
        public void FlattenArea(Vector3 center, float radius, float targetHeight)
        {
            if (_generatedTerrain == null || _terrainData == null) return;
            
            Vector3 terrainPos = _generatedTerrain.transform.position;
            Vector3 terrainSize = _terrainData.size;
            
            int resolution = _terrainData.heightmapResolution;
            float[,] heights = _terrainData.GetHeights(0, 0, resolution, resolution);
            
            // Convert world position to terrain coordinates
            float normalizedX = (center.x - terrainPos.x) / terrainSize.x;
            float normalizedZ = (center.z - terrainPos.z) / terrainSize.z;
            float normalizedRadius = radius / Mathf.Min(terrainSize.x, terrainSize.z);
            float normalizedHeight = targetHeight / terrainSize.y;
            
            int centerX = Mathf.RoundToInt(normalizedX * (resolution - 1));
            int centerZ = Mathf.RoundToInt(normalizedZ * (resolution - 1));
            int radiusPixels = Mathf.RoundToInt(normalizedRadius * (resolution - 1));
            
            for (int z = -radiusPixels; z <= radiusPixels; z++)
            {
                for (int x = -radiusPixels; x <= radiusPixels; x++)
                {
                    int px = centerX + x;
                    int pz = centerZ + z;
                    
                    if (px < 0 || px >= resolution || pz < 0 || pz >= resolution) continue;
                    
                    float distance = Mathf.Sqrt(x * x + z * z) / radiusPixels;
                    if (distance <= 1f)
                    {
                        float blend = 1f - Mathf.SmoothStep(0.7f, 1f, distance);
                        heights[pz, px] = Mathf.Lerp(heights[pz, px], normalizedHeight, blend);
                    }
                }
            }
            
            _terrainData.SetHeights(0, 0, heights);
        }
        
        #endregion
        
        #region Heightmap Generation
        
        private async Task<float[,]> GenerateHeightmap(ElevationData elevationData, OSMAreaData areaData)
        {
            float[,] heights = new float[terrainResolution, terrainResolution];
            
            await Task.Run(() =>
            {
                float minElevation = float.MaxValue;
                float maxElevation = float.MinValue;
                
                // Find elevation range
                if (elevationData != null && elevationData.elevations != null)
                {
                    foreach (var elev in elevationData.elevations)
                    {
                        minElevation = Mathf.Min(minElevation, elev.elevation);
                        maxElevation = Mathf.Max(maxElevation, elev.elevation);
                    }
                }
                
                if (maxElevation <= minElevation)
                {
                    minElevation = 0;
                    maxElevation = 100;
                }
                
                float elevationRange = maxElevation - minElevation;
                
                for (int z = 0; z < terrainResolution; z++)
                {
                    for (int x = 0; x < terrainResolution; x++)
                    {
                        float normalizedX = x / (float)(terrainResolution - 1);
                        float normalizedZ = z / (float)(terrainResolution - 1);
                        
                        // Base elevation from data
                        float baseHeight = GetInterpolatedElevation(elevationData, normalizedX, normalizedZ, minElevation, elevationRange);
                        
                        // Add fantasy hill features
                        float fantasyHeight = 0;
                        if (addFantasyHills)
                        {
                            fantasyHeight = GenerateFantasyHills(normalizedX, normalizedZ);
                        }
                        
                        // Add magical glen depressions
                        if (addMagicalGlens)
                        {
                            fantasyHeight -= GenerateMagicalGlens(normalizedX, normalizedZ);
                        }
                        
                        // Combine heights
                        float finalHeight = baseHeight + fantasyHeight * fantasyHeightMultiplier;
                        finalHeight = Mathf.Clamp01(finalHeight);
                        
                        // Carve out water areas
                        if (areaData != null)
                        {
                            finalHeight = CarveWaterAreas(areaData, normalizedX, normalizedZ, finalHeight);
                        }
                        
                        heights[z, x] = finalHeight;
                    }
                }
            });
            
            return heights;
        }
        
        private float[,] GenerateProceduralHeightmap(Vector2d centerGeo)
        {
            float[,] heights = new float[terrainResolution, terrainResolution];
            
            // Use geo coordinates as noise seed
            float seedX = (float)(centerGeo.longitude * 1000 % 10000);
            float seedZ = (float)(centerGeo.latitude * 1000 % 10000);
            
            for (int z = 0; z < terrainResolution; z++)
            {
                for (int x = 0; x < terrainResolution; x++)
                {
                    float normalizedX = x / (float)(terrainResolution - 1);
                    float normalizedZ = z / (float)(terrainResolution - 1);
                    
                    float worldX = normalizedX * terrainSize + seedX;
                    float worldZ = normalizedZ * terrainSize + seedZ;
                    
                    // Multi-octave Perlin noise
                    float height = 0;
                    float amplitude = 1f;
                    float frequency = hillFrequency;
                    float maxValue = 0;
                    
                    for (int octave = 0; octave < 4; octave++)
                    {
                        height += Mathf.PerlinNoise(worldX * frequency, worldZ * frequency) * amplitude;
                        maxValue += amplitude;
                        amplitude *= 0.5f;
                        frequency *= 2f;
                    }
                    
                    height /= maxValue;
                    
                    // Add detail noise
                    float detail = Mathf.PerlinNoise(worldX * detailFrequency, worldZ * detailFrequency) * 0.1f;
                    height += detail;
                    
                    // Fantasy features
                    if (addFantasyHills)
                    {
                        height += GenerateFantasyHills(normalizedX, normalizedZ) * 0.3f;
                    }
                    
                    heights[z, x] = Mathf.Clamp01(height);
                }
            }
            
            return heights;
        }
        
        private float GetInterpolatedElevation(ElevationData data, float normalizedX, float normalizedZ, float minElev, float range)
        {
            if (data == null || data.elevations == null || data.elevations.Count == 0)
            {
                return baseTerrainHeight / heightScale;
            }
            
            // Find nearest elevation points and interpolate
            float elevation = 0;
            float totalWeight = 0;
            
            foreach (var point in data.elevations)
            {
                float px = (float)((point.longitude - data.minLongitude) / (data.maxLongitude - data.minLongitude));
                float pz = (float)((point.latitude - data.minLatitude) / (data.maxLatitude - data.minLatitude));
                
                float distance = Mathf.Sqrt((px - normalizedX) * (px - normalizedX) + (pz - normalizedZ) * (pz - normalizedZ));
                
                if (distance < 0.001f)
                {
                    return (point.elevation - minElev) / range;
                }
                
                float weight = 1f / (distance * distance);
                elevation += ((point.elevation - minElev) / range) * weight;
                totalWeight += weight;
            }
            
            if (totalWeight > 0)
            {
                return elevation / totalWeight;
            }
            
            return baseTerrainHeight / heightScale;
        }
        
        private float GenerateFantasyHills(float x, float z)
        {
            // Large rolling hills
            float hills = Mathf.PerlinNoise(x * 3f, z * 3f) * 0.15f;
            
            // Occasional dramatic peaks
            float peaks = Mathf.Max(0, Mathf.PerlinNoise(x * 1.5f + 100, z * 1.5f + 100) - 0.7f) * 0.5f;
            
            return hills + peaks;
        }
        
        private float GenerateMagicalGlens(float x, float z)
        {
            // Small depressions for magical clearings
            float glens = Mathf.Max(0, Mathf.PerlinNoise(x * 5f + 50, z * 5f + 50) - 0.8f) * 0.1f;
            
            return glens;
        }
        
        private float CarveWaterAreas(OSMAreaData areaData, float normalizedX, float normalizedZ, float height)
        {
            if (areaData.naturalAreas == null) return height;
            
            foreach (var natural in areaData.naturalAreas)
            {
                if (natural.fantasyType is not (FantasyNaturalType.MagicalRiver or FantasyNaturalType.EnchantedLake))
                    continue;
                
                // Check if point is inside water polygon (simplified)
                if (IsInsidePolygon(natural.polygon, normalizedX, normalizedZ, areaData))
                {
                    return Mathf.Min(height, waterLevel / heightScale);
                }
            }
            
            return height;
        }
        
        private bool IsInsidePolygon(List<Vector2d> polygon, float normalizedX, float normalizedZ, OSMAreaData areaData)
        {
            if (polygon == null || polygon.Count < 3) return false;
            
            // Convert normalized to geo coords
            double lon = areaData.centerLongitude + (normalizedX - 0.5f) * 0.01;
            double lat = areaData.centerLatitude + (normalizedZ - 0.5f) * 0.01;
            
            // Ray casting algorithm
            bool inside = false;
            int j = polygon.Count - 1;
            
            for (int i = 0; i < polygon.Count; i++)
            {
                if ((polygon[i].latitude < lat && polygon[j].latitude >= lat ||
                     polygon[j].latitude < lat && polygon[i].latitude >= lat) &&
                    (polygon[i].longitude <= lon || polygon[j].longitude <= lon))
                {
                    if (polygon[i].longitude + (lat - polygon[i].latitude) / 
                        (polygon[j].latitude - polygon[i].latitude) * 
                        (polygon[j].longitude - polygon[i].longitude) < lon)
                    {
                        inside = !inside;
                    }
                }
                j = i;
            }
            
            return inside;
        }
        
        #endregion
        
        #region Terrain Textures
        
        private void ApplyTerrainTextures()
        {
            if (_terrainData == null || terrainLayers == null || terrainLayers.Length == 0) return;
            
            int resolution = _terrainData.alphamapResolution;
            float[,,] splatmap = new float[resolution, resolution, terrainLayers.Length];
            
            float[,] heights = _terrainData.GetHeights(0, 0, resolution, resolution);
            
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float height = heights[z, x] * heightScale;
                    
                    // Calculate slope
                    float slope = CalculateSlope(heights, x, z, resolution);
                    
                    // Determine texture weights
                    float[] weights = new float[terrainLayers.Length];
                    
                    if (terrainLayers.Length >= 4)
                    {
                        // Layer 0: Grass (flat areas)
                        // Layer 1: Dirt (medium slopes)
                        // Layer 2: Rock (steep slopes)
                        // Layer 3: Snow (high elevation)
                        
                        if (height > snowHeightThreshold)
                        {
                            weights[3] = 1f;
                        }
                        else if (slope > rockSteepnessThreshold)
                        {
                            weights[2] = 1f;
                        }
                        else if (slope > grassSteepnessThreshold)
                        {
                            float blend = (slope - grassSteepnessThreshold) / (rockSteepnessThreshold - grassSteepnessThreshold);
                            weights[1] = 1f - blend;
                            weights[2] = blend;
                        }
                        else
                        {
                            float blend = slope / grassSteepnessThreshold;
                            weights[0] = 1f - blend;
                            weights[1] = blend;
                        }
                    }
                    else if (terrainLayers.Length > 0)
                    {
                        weights[0] = 1f;
                    }
                    
                    // Normalize weights
                    float total = 0;
                    foreach (var w in weights) total += w;
                    if (total > 0)
                    {
                        for (int i = 0; i < weights.Length; i++)
                        {
                            splatmap[z, x, i] = weights[i] / total;
                        }
                    }
                }
            }
            
            _terrainData.SetAlphamaps(0, 0, splatmap);
        }
        
        private float CalculateSlope(float[,] heights, int x, int z, int resolution)
        {
            float height = heights[z, x];
            float maxDiff = 0;
            
            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    
                    int nx = Mathf.Clamp(x + dx, 0, resolution - 1);
                    int nz = Mathf.Clamp(z + dz, 0, resolution - 1);
                    
                    float diff = Mathf.Abs(heights[nz, nx] - height);
                    maxDiff = Mathf.Max(maxDiff, diff);
                }
            }
            
            // Convert to degrees
            return Mathf.Atan(maxDiff * heightScale / (_terrainData.size.x / resolution)) * Mathf.Rad2Deg;
        }
        
        #endregion
        
        #region Vegetation
        
        private async Task PlaceVegetation(OSMAreaData areaData)
        {
            if (_generatedTerrain == null || _terrainData == null) return;
            
            await Task.Run(() =>
            {
                // Place trees
                if (treePrototypes != null && treePrototypes.Length > 0)
                {
                    _terrainData.treePrototypes = treePrototypes;
                    PlaceTrees(areaData);
                }
                
                // Place detail grass
                if (detailPrototypes != null && detailPrototypes.Length > 0)
                {
                    _terrainData.detailPrototypes = detailPrototypes;
                    PlaceDetailGrass();
                }
            });
        }
        
        private void PlaceTrees(OSMAreaData areaData)
        {
            List<TreeInstance> trees = new List<TreeInstance>();
            
            int treeCount = Mathf.FloorToInt(terrainSize * terrainSize * treeDensity * 0.001f);
            
            for (int i = 0; i < treeCount; i++)
            {
                float x = UnityEngine.Random.value;
                float z = UnityEngine.Random.value;
                
                // Check if in forest area
                bool inForest = IsInForestArea(x, z, areaData);
                if (!inForest && UnityEngine.Random.value > 0.2f) continue;
                
                // Don't place on steep slopes or water
                float height = _terrainData.GetInterpolatedHeight(x, z);
                if (height < waterLevel) continue;
                
                TreeInstance tree = new TreeInstance();
                tree.position = new Vector3(x, 0, z);
                tree.prototypeIndex = UnityEngine.Random.Range(0, treePrototypes.Length);
                tree.widthScale = UnityEngine.Random.Range(0.8f, 1.2f);
                tree.heightScale = UnityEngine.Random.Range(0.8f, 1.2f);
                tree.color = Color.white;
                tree.lightmapColor = Color.white;
                
                trees.Add(tree);
            }
            
            _terrainData.SetTreeInstances(trees.ToArray(), true);
        }
        
        private bool IsInForestArea(float normalizedX, float normalizedZ, OSMAreaData areaData)
        {
            if (areaData == null || areaData.naturalAreas == null) return false;
            
            foreach (var natural in areaData.naturalAreas)
            {
                if (natural.fantasyType is FantasyNaturalType.EnchantedForest or FantasyNaturalType.MysticalGrove)
                {
                    if (IsInsidePolygon(natural.polygon, normalizedX, normalizedZ, areaData))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        private void PlaceDetailGrass()
        {
            int resolution = _terrainData.detailResolution;
            
            for (int layer = 0; layer < detailPrototypes.Length; layer++)
            {
                int[,] details = new int[resolution, resolution];
                
                for (int z = 0; z < resolution; z++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        float normalizedX = x / (float)resolution;
                        float normalizedZ = z / (float)resolution;
                        
                        float height = _terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
                        
                        if (height > waterLevel && UnityEngine.Random.value < grassDensity)
                        {
                            details[z, x] = UnityEngine.Random.Range(1, 5);
                        }
                    }
                }
                
                _terrainData.SetDetailLayer(0, 0, layer, details);
            }
        }
        
        #endregion
        
        #region Fantasy Decorations
        
        private void PlaceFantasyDecorations()
        {
            if (_generatedTerrain == null) return;
            
            int decorationCount = Mathf.FloorToInt(terrainSize * decorationDensity);
            
            for (int i = 0; i < decorationCount; i++)
            {
                float x = UnityEngine.Random.value;
                float z = UnityEngine.Random.value;
                
                Vector3 worldPos = _generatedTerrain.transform.position + 
                    new Vector3(x * terrainSize, 0, z * terrainSize);
                worldPos.y = _generatedTerrain.SampleHeight(worldPos);
                
                if (worldPos.y < waterLevel) continue;
                
                // Choose decoration type
                float roll = UnityEngine.Random.value;
                GameObject prefab = null;
                
                if (roll < 0.3f && ruinPrefabs != null && ruinPrefabs.Length > 0)
                {
                    prefab = ruinPrefabs[UnityEngine.Random.Range(0, ruinPrefabs.Length)];
                }
                else if (roll < 0.6f && standingStonesPrefabs != null && standingStonesPrefabs.Length > 0)
                {
                    prefab = standingStonesPrefabs[UnityEngine.Random.Range(0, standingStonesPrefabs.Length)];
                }
                else if (mysticalTreePrefabs != null && mysticalTreePrefabs.Length > 0)
                {
                    prefab = mysticalTreePrefabs[UnityEngine.Random.Range(0, mysticalTreePrefabs.Length)];
                }
                
                if (prefab != null)
                {
                    GameObject decoration = Instantiate(prefab, worldPos, 
                        Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
                    decoration.transform.SetParent(transform);
                    _decorations.Add(decoration);
                }
            }
        }
        
        #endregion
        
        #region Water
        
        private void CreateWater(Vector3 worldOrigin)
        {
            _waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _waterPlane.name = "WaterPlane";
            _waterPlane.transform.SetParent(transform);
            _waterPlane.transform.position = worldOrigin + Vector3.up * waterLevel;
            _waterPlane.transform.localScale = new Vector3(terrainSize / 10f, 1, terrainSize / 10f);
            
            if (waterMaterial != null)
            {
                _waterPlane.GetComponent<MeshRenderer>().material = waterMaterial;
            }
            
            Destroy(_waterPlane.GetComponent<Collider>());
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    [Serializable]
    public class ElevationData
    {
        public double minLatitude;
        public double maxLatitude;
        public double minLongitude;
        public double maxLongitude;
        public List<ElevationPoint> elevations = new List<ElevationPoint>();
    }
    
    [Serializable]
    public class ElevationPoint
    {
        public double latitude;
        public double longitude;
        public float elevation;
    }
    
    #endregion
}
