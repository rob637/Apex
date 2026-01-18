using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Fantasy Map Overlay System - Transforms real-world map tiles into fantasy aesthetic
    /// Adds magical glow, parchment texture, stylized colors, and ambient effects.
    /// Makes the map feel like a living fantasy world while preserving geographic accuracy.
    /// </summary>
    public class FantasyMapOverlay : MonoBehaviour
    {
        [Header("Fantasy Style Settings")]
        [SerializeField] private bool enableFantasyOverlay = true;
        [SerializeField] private FantasyMapStyle currentStyle = FantasyMapStyle.AncientParchment;
        [SerializeField, Range(0f, 1f)] private float fantasyIntensity = 0.7f;
        
        [Header("Color Grading")]
        [SerializeField] private Color parchmentTint = new Color(0.95f, 0.88f, 0.72f);
        [SerializeField] private Color magicGlow = new Color(0.4f, 0.6f, 1f, 0.3f);
        [SerializeField] private Color territoryHighlight = new Color(0.8f, 0.4f, 0.1f, 0.5f);
        [SerializeField] private Color waterTint = new Color(0.2f, 0.5f, 0.8f, 0.6f);
        [SerializeField] private Color forestTint = new Color(0.1f, 0.4f, 0.15f, 0.4f);
        
        [Header("Vignette & Borders")]
        [SerializeField] private bool enableVignette = true;
        [SerializeField, Range(0f, 1f)] private float vignetteIntensity = 0.4f;
        [SerializeField] private bool enableOrnamentalBorder = true;
        [SerializeField] private Color borderColor = new Color(0.4f, 0.3f, 0.2f);
        
        [Header("Map Symbols")]
        [SerializeField] private bool showCompassRose = true;
        [SerializeField] private bool showScaleBar = true;
        [SerializeField] private bool showCartographicDecorations = true;
        
        [Header("Fog of War")]
        [SerializeField] private bool enableFogOfWar = true;
        [SerializeField] private float exploredRadius = 500f;
        [SerializeField] private Color unexploredColor = new Color(0.3f, 0.25f, 0.2f, 0.8f);
        [SerializeField] private float fogEdgeSoftness = 100f;
        
        [Header("Magical Effects")]
        [SerializeField] private bool enableMagicalShimmer = true;
        [SerializeField] private float shimmerSpeed = 0.5f;
        [SerializeField] private float shimmerIntensity = 0.1f;
        [SerializeField] private bool enableTerritoryGlow = true;
        [SerializeField] private float glowPulseSpeed = 1.5f;
        
        [Header("Procedural Details")]
        [SerializeField] private bool addProceduralTrees = true;
        [SerializeField] private bool addProceduralMountains = true;
        [SerializeField] private bool addWaveEffect = true;
        [SerializeField] private float waveSpeed = 0.3f;
        
        [Header("References")]
        [SerializeField] private Material fantasyMapMaterial;
        [SerializeField] private Texture2D parchmentTexture;
        [SerializeField] private Texture2D noiseTexture;
        [SerializeField] private Texture2D borderTexture;
        
        // Singleton
        private static FantasyMapOverlay _instance;
        public static FantasyMapOverlay Instance => _instance;
        
        // Runtime state
        private RealWorldMapRenderer _mapRenderer;
        private Dictionary<string, Material> _tileMaterials = new Dictionary<string, Material>();
        private List<Vector3> _exploredLocations = new List<Vector3>();
        private float _shimmerPhase;
        private float _glowPhase;
        private float _wavePhase;
        
        // UI Elements
        private GameObject _compassRose;
        private GameObject _scaleBar;
        private GameObject _mapBorder;
        private GameObject _fogOfWarMesh;
        
        // Events
        public event Action<FantasyMapStyle> OnStyleChanged;
        public event Action<float> OnIntensityChanged;
        
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
            _mapRenderer = FindFirstObjectByType<RealWorldMapRenderer>();
            
            StartCoroutine(Initialize());
        }
        
        private IEnumerator Initialize()
        {
            yield return new WaitForSeconds(0.5f); // Wait for map to initialize
            
            // Create procedural textures if not assigned
            if (parchmentTexture == null)
            {
                parchmentTexture = CreateParchmentTexture();
            }
            
            if (noiseTexture == null)
            {
                noiseTexture = CreateNoiseTexture();
            }
            
            // Create fantasy map material if not assigned
            if (fantasyMapMaterial == null)
            {
                fantasyMapMaterial = CreateFantasyMaterial();
            }
            
            // Setup overlay elements
            if (showCompassRose)
            {
                CreateCompassRose();
            }
            
            if (showScaleBar)
            {
                CreateScaleBar();
            }
            
            if (enableOrnamentalBorder)
            {
                CreateOrnamentalBorder();
            }
            
            if (enableFogOfWar)
            {
                CreateFogOfWar();
            }
            
            // Apply fantasy style to existing tiles
            ApplyFantasyStyleToAllTiles();
            
            Debug.Log($"[FantasyMapOverlay] Initialized with style: {currentStyle}");
        }
        
        private void Update()
        {
            if (!enableFantasyOverlay) return;
            
            // Update shimmer effect
            if (enableMagicalShimmer)
            {
                _shimmerPhase += Time.deltaTime * shimmerSpeed;
                UpdateShimmerEffect();
            }
            
            // Update territory glow
            if (enableTerritoryGlow)
            {
                _glowPhase += Time.deltaTime * glowPulseSpeed;
                UpdateTerritoryGlow();
            }
            
            // Update wave effect
            if (addWaveEffect)
            {
                _wavePhase += Time.deltaTime * waveSpeed;
                UpdateWaveEffect();
            }
            
            // Update fog of war
            if (enableFogOfWar)
            {
                UpdateFogOfWar();
            }
        }
        
        #region Style Management
        
        /// <summary>
        /// Set the fantasy map style
        /// </summary>
        public void SetStyle(FantasyMapStyle style)
        {
            currentStyle = style;
            
            switch (style)
            {
                case FantasyMapStyle.AncientParchment:
                    ApplyParchmentStyle();
                    break;
                    
                case FantasyMapStyle.MysticalGlow:
                    ApplyMysticalStyle();
                    break;
                    
                case FantasyMapStyle.DarkRealm:
                    ApplyDarkRealmStyle();
                    break;
                    
                case FantasyMapStyle.EnchantedForest:
                    ApplyEnchantedForestStyle();
                    break;
                    
                case FantasyMapStyle.FrozenNorth:
                    ApplyFrozenNorthStyle();
                    break;
                    
                case FantasyMapStyle.DesertKingdom:
                    ApplyDesertKingdomStyle();
                    break;
                    
                case FantasyMapStyle.VolcanicWastes:
                    ApplyVolcanicWastesStyle();
                    break;
            }
            
            OnStyleChanged?.Invoke(style);
            Debug.Log($"[FantasyMapOverlay] Style changed to: {style}");
        }
        
        /// <summary>
        /// Set fantasy effect intensity (0-1)
        /// </summary>
        public void SetIntensity(float intensity)
        {
            fantasyIntensity = Mathf.Clamp01(intensity);
            ApplyFantasyStyleToAllTiles();
            OnIntensityChanged?.Invoke(fantasyIntensity);
        }
        
        private void ApplyParchmentStyle()
        {
            parchmentTint = new Color(0.95f, 0.88f, 0.72f);
            magicGlow = new Color(0.6f, 0.5f, 0.3f, 0.2f);
            waterTint = new Color(0.3f, 0.5f, 0.6f, 0.5f);
            forestTint = new Color(0.2f, 0.4f, 0.2f, 0.4f);
            vignetteIntensity = 0.5f;
            ApplyFantasyStyleToAllTiles();
        }
        
        private void ApplyMysticalStyle()
        {
            parchmentTint = new Color(0.85f, 0.88f, 0.95f);
            magicGlow = new Color(0.4f, 0.6f, 1f, 0.4f);
            waterTint = new Color(0.2f, 0.4f, 0.9f, 0.5f);
            forestTint = new Color(0.1f, 0.5f, 0.4f, 0.4f);
            vignetteIntensity = 0.3f;
            enableMagicalShimmer = true;
            shimmerIntensity = 0.15f;
            ApplyFantasyStyleToAllTiles();
        }
        
        private void ApplyDarkRealmStyle()
        {
            parchmentTint = new Color(0.3f, 0.28f, 0.32f);
            magicGlow = new Color(0.6f, 0.2f, 0.8f, 0.4f);
            waterTint = new Color(0.1f, 0.15f, 0.3f, 0.6f);
            forestTint = new Color(0.1f, 0.15f, 0.1f, 0.5f);
            vignetteIntensity = 0.7f;
            unexploredColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
            ApplyFantasyStyleToAllTiles();
        }
        
        private void ApplyEnchantedForestStyle()
        {
            parchmentTint = new Color(0.8f, 0.9f, 0.75f);
            magicGlow = new Color(0.3f, 0.9f, 0.4f, 0.3f);
            waterTint = new Color(0.2f, 0.6f, 0.5f, 0.5f);
            forestTint = new Color(0.1f, 0.6f, 0.2f, 0.5f);
            vignetteIntensity = 0.3f;
            ApplyFantasyStyleToAllTiles();
        }
        
        private void ApplyFrozenNorthStyle()
        {
            parchmentTint = new Color(0.9f, 0.95f, 1f);
            magicGlow = new Color(0.5f, 0.8f, 1f, 0.3f);
            waterTint = new Color(0.4f, 0.6f, 0.9f, 0.5f);
            forestTint = new Color(0.2f, 0.35f, 0.3f, 0.4f);
            vignetteIntensity = 0.4f;
            ApplyFantasyStyleToAllTiles();
        }
        
        private void ApplyDesertKingdomStyle()
        {
            parchmentTint = new Color(1f, 0.9f, 0.7f);
            magicGlow = new Color(1f, 0.7f, 0.3f, 0.3f);
            waterTint = new Color(0.2f, 0.5f, 0.6f, 0.6f);
            forestTint = new Color(0.4f, 0.5f, 0.2f, 0.3f);
            vignetteIntensity = 0.5f;
            ApplyFantasyStyleToAllTiles();
        }
        
        private void ApplyVolcanicWastesStyle()
        {
            parchmentTint = new Color(0.5f, 0.35f, 0.3f);
            magicGlow = new Color(1f, 0.4f, 0.1f, 0.4f);
            waterTint = new Color(0.8f, 0.3f, 0.1f, 0.5f); // Lava!
            forestTint = new Color(0.2f, 0.15f, 0.1f, 0.5f);
            vignetteIntensity = 0.6f;
            ApplyFantasyStyleToAllTiles();
        }
        
        #endregion
        
        #region Material Application
        
        private void ApplyFantasyStyleToAllTiles()
        {
            var tiles = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            
            foreach (var renderer in tiles)
            {
                if (renderer.gameObject.name.StartsWith("Tile_"))
                {
                    ApplyFantasyMaterial(renderer);
                }
            }
        }
        
        /// <summary>
        /// Apply fantasy material to a map tile
        /// </summary>
        public void ApplyFantasyMaterial(Renderer tileRenderer)
        {
            if (!enableFantasyOverlay || tileRenderer == null) return;
            
            Material mat = tileRenderer.material;
            string tileKey = tileRenderer.gameObject.name;
            
            // Get or create modified material
            if (!_tileMaterials.ContainsKey(tileKey))
            {
                _tileMaterials[tileKey] = new Material(mat);
            }
            
            Material fantasyMat = _tileMaterials[tileKey];
            
            // Copy base texture
            if (mat.mainTexture != null)
            {
                fantasyMat.mainTexture = mat.mainTexture;
            }
            
            // Apply fantasy tint
            Color blendedColor = Color.Lerp(Color.white, parchmentTint, fantasyIntensity);
            fantasyMat.color = blendedColor;
            
            // Apply material
            tileRenderer.material = fantasyMat;
        }
        
        private Material CreateFantasyMaterial()
        {
            // Create a material that supports our fantasy effects
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            
            Material mat = new Material(shader);
            mat.name = "FantasyMapMaterial";
            
            return mat;
        }
        
        #endregion
        
        #region Procedural Textures
        
        private Texture2D CreateParchmentTexture()
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Base parchment color with noise
                    float noise1 = Mathf.PerlinNoise(x * 0.02f, y * 0.02f);
                    float noise2 = Mathf.PerlinNoise(x * 0.05f + 100, y * 0.05f + 100) * 0.5f;
                    float noise3 = Mathf.PerlinNoise(x * 0.1f + 200, y * 0.1f + 200) * 0.25f;
                    
                    float combined = (noise1 + noise2 + noise3) / 1.75f;
                    
                    // Parchment color variation
                    float r = 0.92f + combined * 0.08f - 0.04f;
                    float g = 0.85f + combined * 0.1f - 0.05f;
                    float b = 0.7f + combined * 0.1f - 0.05f;
                    
                    // Add some age spots
                    if (noise1 > 0.7f && noise2 > 0.6f)
                    {
                        r -= 0.1f;
                        g -= 0.08f;
                        b -= 0.05f;
                    }
                    
                    tex.SetPixel(x, y, new Color(r, g, b, 1f));
                }
            }
            
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            
            return tex;
        }
        
        private Texture2D CreateNoiseTexture()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    tex.SetPixel(x, y, new Color(noise, noise, noise, 1f));
                }
            }
            
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            
            return tex;
        }
        
        #endregion
        
        #region Map Decorations
        
        private void CreateCompassRose()
        {
            _compassRose = new GameObject("CompassRose");
            _compassRose.transform.SetParent(transform);
            
            // Create compass visual using UI or 3D
            // Position in corner of view
            _compassRose.transform.localPosition = new Vector3(-200, 0.5f, 200);
            
            // Create compass points
            CreateCompassPoint(_compassRose.transform, Vector3.forward, "N", Color.red);
            CreateCompassPoint(_compassRose.transform, Vector3.back, "S", Color.white);
            CreateCompassPoint(_compassRose.transform, Vector3.right, "E", Color.white);
            CreateCompassPoint(_compassRose.transform, Vector3.left, "W", Color.white);
            
            // Create compass circle
            var circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            circle.name = "CompassBase";
            circle.transform.SetParent(_compassRose.transform);
            circle.transform.localPosition = Vector3.zero;
            circle.transform.localScale = new Vector3(30, 0.2f, 30);
            
            var circleRenderer = circle.GetComponent<Renderer>();
            circleRenderer.material = new Material(Shader.Find("Sprites/Default"));
            circleRenderer.material.color = new Color(0.4f, 0.35f, 0.3f, 0.8f);
            
            Destroy(circle.GetComponent<Collider>());
        }
        
        private void CreateCompassPoint(Transform parent, Vector3 direction, string label, Color color)
        {
            var point = new GameObject($"Compass_{label}");
            point.transform.SetParent(parent);
            point.transform.localPosition = direction * 12f;
            
            // Create arrow
            var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.transform.SetParent(point.transform);
            arrow.transform.localPosition = Vector3.up * 0.5f;
            arrow.transform.localScale = new Vector3(2, 1, 8);
            arrow.transform.LookAt(parent.position);
            
            var arrowRenderer = arrow.GetComponent<Renderer>();
            arrowRenderer.material = new Material(Shader.Find("Sprites/Default"));
            arrowRenderer.material.color = color;
            
            Destroy(arrow.GetComponent<Collider>());
            
            // Add text label (would use TextMeshPro in real implementation)
            var textObj = new GameObject($"Label_{label}");
            textObj.transform.SetParent(point.transform);
            textObj.transform.localPosition = direction * 4f + Vector3.up * 2f;
        }
        
        private void CreateScaleBar()
        {
            _scaleBar = new GameObject("ScaleBar");
            _scaleBar.transform.SetParent(transform);
            _scaleBar.transform.localPosition = new Vector3(200, 0.5f, -200);
            
            // Create bar segments
            for (int i = 0; i < 5; i++)
            {
                var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = $"ScaleSegment_{i}";
                segment.transform.SetParent(_scaleBar.transform);
                segment.transform.localPosition = new Vector3(i * 10, 0, 0);
                segment.transform.localScale = new Vector3(9, 0.3f, 3);
                
                var renderer = segment.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = (i % 2 == 0) ? Color.black : Color.white;
                
                Destroy(segment.GetComponent<Collider>());
            }
        }
        
        private void CreateOrnamentalBorder()
        {
            _mapBorder = new GameObject("OrnamentalBorder");
            _mapBorder.transform.SetParent(transform);
            _mapBorder.transform.localPosition = Vector3.zero;
            
            float borderSize = 500f; // Size of the border
            float borderWidth = 20f;
            
            // Create four border edges
            CreateBorderEdge("Top", new Vector3(0, 0.1f, borderSize), new Vector3(borderSize * 2, 1, borderWidth));
            CreateBorderEdge("Bottom", new Vector3(0, 0.1f, -borderSize), new Vector3(borderSize * 2, 1, borderWidth));
            CreateBorderEdge("Left", new Vector3(-borderSize, 0.1f, 0), new Vector3(borderWidth, 1, borderSize * 2));
            CreateBorderEdge("Right", new Vector3(borderSize, 0.1f, 0), new Vector3(borderWidth, 1, borderSize * 2));
            
            // Create corner decorations
            CreateCornerDecoration("TopLeft", new Vector3(-borderSize, 0.2f, borderSize));
            CreateCornerDecoration("TopRight", new Vector3(borderSize, 0.2f, borderSize));
            CreateCornerDecoration("BottomLeft", new Vector3(-borderSize, 0.2f, -borderSize));
            CreateCornerDecoration("BottomRight", new Vector3(borderSize, 0.2f, -borderSize));
        }
        
        private void CreateBorderEdge(string name, Vector3 position, Vector3 scale)
        {
            var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = $"Border_{name}";
            edge.transform.SetParent(_mapBorder.transform);
            edge.transform.localPosition = position;
            edge.transform.localScale = scale;
            
            var renderer = edge.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = borderColor;
            
            Destroy(edge.GetComponent<Collider>());
        }
        
        private void CreateCornerDecoration(string name, Vector3 position)
        {
            var corner = new GameObject($"Corner_{name}");
            corner.transform.SetParent(_mapBorder.transform);
            corner.transform.localPosition = position;
            
            // Create decorative flourish
            var flourish = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flourish.transform.SetParent(corner.transform);
            flourish.transform.localPosition = Vector3.zero;
            flourish.transform.localScale = Vector3.one * 25f;
            
            var renderer = flourish.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = new Color(borderColor.r * 0.8f, borderColor.g * 0.8f, borderColor.b * 0.8f);
            
            Destroy(flourish.GetComponent<Collider>());
        }
        
        #endregion
        
        #region Fog of War
        
        private void CreateFogOfWar()
        {
            _fogOfWarMesh = new GameObject("FogOfWar");
            _fogOfWarMesh.transform.SetParent(transform);
            _fogOfWarMesh.transform.localPosition = new Vector3(0, 1f, 0);
            
            // Create a large quad for the fog
            var fog = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fog.name = "FogQuad";
            fog.transform.SetParent(_fogOfWarMesh.transform);
            fog.transform.localPosition = Vector3.zero;
            fog.transform.localRotation = Quaternion.Euler(90, 0, 0);
            fog.transform.localScale = new Vector3(2000, 2000, 1);
            
            var renderer = fog.GetComponent<Renderer>();
            renderer.material = CreateFogMaterial();
            
            Destroy(fog.GetComponent<Collider>());
            
            // Add player's starting location as explored
            MarkExplored(Vector3.zero);
        }
        
        private Material CreateFogMaterial()
        {
            // Use a transparent shader
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");
            
            Material mat = new Material(shader);
            mat.color = unexploredColor;
            mat.SetFloat("_Mode", 3); // Transparent mode
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
        /// Mark a location as explored (removes fog)
        /// </summary>
        public void MarkExplored(Vector3 worldPosition)
        {
            _exploredLocations.Add(worldPosition);
        }
        
        private void UpdateFogOfWar()
        {
            // In a full implementation, this would update a shader-based fog
            // For now, we'll disable fog near explored areas
            if (_fogOfWarMesh == null) return;
            
            // Simple approach: if camera is near explored area, reduce fog opacity
            if (Camera.main != null)
            {
                Vector3 camPos = Camera.main.transform.position;
                float minDist = float.MaxValue;
                
                foreach (var explored in _exploredLocations)
                {
                    float dist = Vector3.Distance(new Vector3(camPos.x, 0, camPos.z), explored);
                    if (dist < minDist) minDist = dist;
                }
                
                // Fade fog based on distance to nearest explored area
                float fogAlpha = Mathf.Clamp01((minDist - exploredRadius) / fogEdgeSoftness);
                
                var renderer = _fogOfWarMesh.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    Color col = renderer.material.color;
                    col.a = fogAlpha * unexploredColor.a;
                    renderer.material.color = col;
                }
            }
        }
        
        #endregion
        
        #region Animated Effects
        
        private void UpdateShimmerEffect()
        {
            foreach (var kvp in _tileMaterials)
            {
                var mat = kvp.Value;
                if (mat == null) continue;
                
                // Calculate shimmer offset
                float shimmer = Mathf.Sin(_shimmerPhase + kvp.Key.GetHashCode() * 0.01f) * shimmerIntensity;
                
                // Apply subtle color shift
                Color baseColor = Color.Lerp(Color.white, parchmentTint, fantasyIntensity);
                Color shimmerColor = baseColor + new Color(shimmer, shimmer * 0.8f, shimmer * 0.5f, 0);
                mat.color = shimmerColor;
            }
        }
        
        private void UpdateTerritoryGlow()
        {
            // Find all territory markers and pulse their glow
            var territories = FindObjectsByType<TerritoryMarker>(FindObjectsSortMode.None);
            
            float pulse = (Mathf.Sin(_glowPhase) + 1f) * 0.5f; // 0-1 range
            
            foreach (var territory in territories)
            {
                if (territory.TryGetComponent<Renderer>(out var renderer))
                {
                    Color baseColor = territory.IsOwned ? 
                        new Color(0.2f, 0.8f, 0.3f, 0.6f) : 
                        new Color(0.5f, 0.5f, 0.5f, 0.4f);
                    
                    // Pulse the emissive glow
                    Color glowColor = baseColor * (1f + pulse * 0.3f);
                    renderer.material.SetColor("_EmissionColor", glowColor * 0.5f);
                }
            }
        }
        
        private void UpdateWaveEffect()
        {
            // Apply subtle wave distortion to water areas
            // This would ideally be done in a shader
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Toggle fantasy overlay on/off
        /// </summary>
        public void ToggleFantasyOverlay()
        {
            enableFantasyOverlay = !enableFantasyOverlay;
            
            if (enableFantasyOverlay)
            {
                ApplyFantasyStyleToAllTiles();
            }
            else
            {
                // Reset to original style
                ResetToOriginalStyle();
            }
        }
        
        /// <summary>
        /// Reset tiles to original appearance
        /// </summary>
        public void ResetToOriginalStyle()
        {
            var tiles = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            
            foreach (var renderer in tiles)
            {
                if (renderer.gameObject.name.StartsWith("Tile_"))
                {
                    renderer.material.color = Color.white;
                }
            }
        }
        
        /// <summary>
        /// Cycle through fantasy styles
        /// </summary>
        public void CycleStyle()
        {
            int styleCount = Enum.GetValues(typeof(FantasyMapStyle)).Length;
            int nextStyle = ((int)currentStyle + 1) % styleCount;
            SetStyle((FantasyMapStyle)nextStyle);
        }
        
        /// <summary>
        /// Set fog of war enabled
        /// </summary>
        public void SetFogOfWar(bool enabled)
        {
            enableFogOfWar = enabled;
            if (_fogOfWarMesh != null)
            {
                _fogOfWarMesh.SetActive(enabled);
            }
        }
        
        /// <summary>
        /// Set vignette intensity
        /// </summary>
        public void SetVignetteIntensity(float intensity)
        {
            vignetteIntensity = Mathf.Clamp01(intensity);
        }
        
        /// <summary>
        /// Get current style
        /// </summary>
        public FantasyMapStyle GetCurrentStyle()
        {
            return currentStyle;
        }
        
        #endregion
    }
    
    #region Data Types
    
    /// <summary>
    /// Fantasy map visual styles
    /// </summary>
    public enum FantasyMapStyle
    {
        AncientParchment,   // Aged paper look with sepia tones
        MysticalGlow,       // Blue magical shimmer
        DarkRealm,          // Dark gothic style
        EnchantedForest,    // Green ethereal glow
        FrozenNorth,        // Ice and snow theme
        DesertKingdom,      // Sand and gold
        VolcanicWastes      // Fire and brimstone
    }
    
    /// <summary>
    /// Component for marking territories on the map
    /// </summary>
    public class TerritoryMarker : MonoBehaviour
    {
        public string TerritoryId;
        public bool IsOwned;
        public bool IsAlliance;
        public bool IsContested;
    }
    
    #endregion
}
