using Camera = UnityEngine.Camera;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using System.Collections.Generic;

namespace ApexCitadels.PC.Effects
{
    /// <summary>
    /// Shader Effects Manager for PC client.
    /// Coordinates and controls all custom shader effects:
    /// - Territory borders
    /// - Resource highlights
    /// - Combat effects
    /// - Weather effects
    /// - Post-processing integration
    /// </summary>
    public class ShaderEffectsManager : MonoBehaviour
    {
        [Header("Effect Materials")]
        [SerializeField] private Material territoryBorderMaterial;
        [SerializeField] private Material resourceHighlightMaterial;
        [SerializeField] private Material combatEffectsMaterial;
        [SerializeField] private Material weatherEffectsMaterial;
        
        [Header("Global Settings")]
        [SerializeField] private float globalEffectIntensity = 1f;
        [SerializeField] private bool enableShaderEffects = true;
        [SerializeField] private bool enablePostProcessing = true;
        
        [Header("Performance")]
        [SerializeField] private bool useLODEffects = true;
        [SerializeField] private float effectLODDistance = 100f;
        [SerializeField] private int maxActiveEffects = 50;
        
        // Singleton
        private static ShaderEffectsManager _instance;
        public static ShaderEffectsManager Instance => _instance;
        
        // Material property IDs (cached for performance)
        private static class PropertyIDs
        {
            // Territory
            public static readonly int OwnerColor = Shader.PropertyToID("_OwnerColor");
            public static readonly int HostileColor = Shader.PropertyToID("_HostileColor");
            public static readonly int CombatMode = Shader.PropertyToID("_CombatMode");
            public static readonly int BorderWidth = Shader.PropertyToID("_BorderWidth");
            
            // Resource
            public static readonly int ResourceType = Shader.PropertyToID("_ResourceType");
            public static readonly int HighlightIntensity = Shader.PropertyToID("_HighlightIntensity");
            public static readonly int IsHovered = Shader.PropertyToID("_IsHovered");
            public static readonly int IsSelected = Shader.PropertyToID("_IsSelected");
            public static readonly int DiscoveryProgress = Shader.PropertyToID("_DiscoveryProgress");
            
            // Combat
            public static readonly int EffectType = Shader.PropertyToID("_EffectType");
            public static readonly int MainColor = Shader.PropertyToID("_MainColor");
            public static readonly int LifetimeProgress = Shader.PropertyToID("_LifetimeProgress");
            public static readonly int ImpactProgress = Shader.PropertyToID("_ImpactProgress");
            public static readonly int HitPoint = Shader.PropertyToID("_HitPoint");
            public static readonly int HitIntensity = Shader.PropertyToID("_HitIntensity");
            
            // Weather
            public static readonly int WeatherType = Shader.PropertyToID("_WeatherType");
            public static readonly int ParticleDensity = Shader.PropertyToID("_ParticleDensity");
            public static readonly int WindDirection = Shader.PropertyToID("_WindDirection");
            public static readonly int WindStrength = Shader.PropertyToID("_WindStrength");
            public static readonly int FogDensity = Shader.PropertyToID("_FogDensity");
            public static readonly int LightningIntensity = Shader.PropertyToID("_LightningIntensity");
        }
        
        // Active effects tracking
        private List<ActiveEffect> _activeEffects = new List<ActiveEffect>();
        private Queue<ActiveEffect> _effectPool = new Queue<ActiveEffect>();
        
        // Territory borders
        private Dictionary<string, TerritoryBorderData> _territoryBorders = 
            new Dictionary<string, TerritoryBorderData>();
        
        // Current weather state
        private WeatherState _currentWeather = new WeatherState();
        private WeatherState _targetWeather = new WeatherState();
        private float _weatherTransitionProgress = 1f;
        
        // Combat state
        private bool _inCombatMode;
        private float _combatModeTransition;
        
        // Volume for post-processing
        private Volume _effectsVolume;
        
        // Events
        public event Action<WeatherType> OnWeatherChanged;
        public event Action<bool> OnCombatModeChanged;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeEffectPool();
            SetupPostProcessing();
            CacheShaderProperties();
        }
        
        private void Update()
        {
            if (!enableShaderEffects) return;
            
            // Update active effects
            UpdateActiveEffects();
            
            // Update weather transition
            UpdateWeatherTransition();
            
            // Update combat mode transition
            UpdateCombatMode();
            
            // Update global shader parameters
            UpdateGlobalParameters();
        }
        
        #region Initialization
        
        private void InitializeEffectPool()
        {
            for (int i = 0; i < 20; i++)
            {
                var effect = new ActiveEffect();
                _effectPool.Enqueue(effect);
            }
        }
        
        private void SetupPostProcessing()
        {
            if (!enablePostProcessing) return;
            
            // Find or create effects volume
            var volumeObj = new GameObject("ShaderEffectsVolume");
            volumeObj.transform.SetParent(transform);
            
            _effectsVolume = volumeObj.AddComponent<Volume>();
            _effectsVolume.isGlobal = true;
            _effectsVolume.priority = 10;
            
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _effectsVolume.profile = profile;
            
            // Add default overrides
            // These would be configured based on current effects
        }
        
        private void CacheShaderProperties()
        {
            // Material property IDs are already cached as static readonly
            // This method could pre-warm material property blocks if needed
        }
        
        #endregion
        
        #region Territory Borders
        
        /// <summary>
        /// Create or update territory border
        /// </summary>
        public void SetTerritoryBorder(string territoryId, Vector3[] borderPoints, 
            Color ownerColor, bool isOwned, bool isContested)
        {
            if (!_territoryBorders.TryGetValue(territoryId, out var data))
            {
                data = CreateTerritoryBorder(territoryId);
                _territoryBorders[territoryId] = data;
            }
            
            // Update border mesh
            UpdateBorderMesh(data, borderPoints);
            
            // Update material properties
            data.PropertyBlock.SetColor(PropertyIDs.OwnerColor, ownerColor);
            data.PropertyBlock.SetFloat(PropertyIDs.CombatMode, isContested ? 1 : 0);
            
            // Set vertex colors for ownership state
            UpdateBorderVertexColors(data, isOwned, isContested);
            
            data.Renderer.SetPropertyBlock(data.PropertyBlock);
        }
        
        private TerritoryBorderData CreateTerritoryBorder(string id)
        {
            var borderObj = new GameObject($"TerritoryBorder_{id}");
            borderObj.transform.SetParent(transform);
            
            var filter = borderObj.AddComponent<MeshFilter>();
            filter.mesh = new Mesh();
            
            var renderer = borderObj.AddComponent<MeshRenderer>();
            renderer.material = new Material(territoryBorderMaterial);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            
            return new TerritoryBorderData
            {
                Id = id,
                Object = borderObj,
                MeshFilter = filter,
                Renderer = renderer,
                PropertyBlock = new MaterialPropertyBlock()
            };
        }
        
        private void UpdateBorderMesh(TerritoryBorderData data, Vector3[] points)
        {
            var mesh = data.MeshFilter.mesh;
            mesh.Clear();
            
            if (points.Length < 2) return;
            
            // Create ribbon mesh along border
            int segments = points.Length;
            var vertices = new Vector3[segments * 2];
            var uvs = new Vector2[segments * 2];
            var colors = new Color[segments * 2];
            var triangles = new int[(segments - 1) * 6];
            
            float totalLength = 0;
            for (int i = 1; i < points.Length; i++)
            {
                totalLength += Vector3.Distance(points[i], points[i - 1]);
            }
            
            float currentLength = 0;
            for (int i = 0; i < segments; i++)
            {
                if (i > 0)
                {
                    currentLength += Vector3.Distance(points[i], points[i - 1]);
                }
                
                float u = currentLength / totalLength;
                
                // Bottom vertex (ground level)
                vertices[i * 2] = points[i];
                uvs[i * 2] = new Vector2(u, 0);
                colors[i * 2] = Color.white;
                
                // Top vertex (raised)
                vertices[i * 2 + 1] = points[i] + Vector3.up * 3f;
                uvs[i * 2 + 1] = new Vector2(u, 1);
                colors[i * 2 + 1] = Color.white;
                
                // Triangles
                if (i < segments - 1)
                {
                    int baseIndex = i * 6;
                    int vertBase = i * 2;
                    
                    triangles[baseIndex] = vertBase;
                    triangles[baseIndex + 1] = vertBase + 1;
                    triangles[baseIndex + 2] = vertBase + 2;
                    
                    triangles[baseIndex + 3] = vertBase + 1;
                    triangles[baseIndex + 4] = vertBase + 3;
                    triangles[baseIndex + 5] = vertBase + 2;
                }
            }
            
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
        
        private void UpdateBorderVertexColors(TerritoryBorderData data, bool isOwned, bool isContested)
        {
            var mesh = data.MeshFilter.mesh;
            var colors = mesh.colors;
            
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(
                    isOwned ? 1 : 0,      // R: ownership
                    isContested ? 1 : 0,  // G: contested
                    1,                    // B: strength
                    i % 2 == 0 ? 0 : 1    // A: edge factor (0 at bottom, 1 at top)
                );
            }
            
            mesh.colors = colors;
        }
        
        /// <summary>
        /// Remove territory border
        /// </summary>
        public void RemoveTerritoryBorder(string territoryId)
        {
            if (_territoryBorders.TryGetValue(territoryId, out var data))
            {
                Destroy(data.Object);
                _territoryBorders.Remove(territoryId);
            }
        }
        
        #endregion
        
        #region Resource Highlights
        
        /// <summary>
        /// Apply resource highlight to object
        /// </summary>
        public void ApplyResourceHighlight(GameObject target, ResourceEffectType resourceType)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;
            
            // Create instance material
            var mat = new Material(resourceHighlightMaterial);
            mat.SetFloat(PropertyIDs.ResourceType, (float)resourceType);
            
            renderer.material = mat;
            
            // Track for updates
            var highlight = new ResourceHighlightData
            {
                Target = target,
                Renderer = renderer,
                ResourceType = resourceType,
                PropertyBlock = new MaterialPropertyBlock()
            };
            
            // Store in component
            var tracker = target.GetComponent<ResourceHighlightTracker>();
            if (tracker == null)
            {
                tracker = target.AddComponent<ResourceHighlightTracker>();
            }
            tracker.Data = highlight;
        }
        
        /// <summary>
        /// Set hover state on resource
        /// </summary>
        public void SetResourceHovered(GameObject target, bool isHovered)
        {
            var tracker = target.GetComponent<ResourceHighlightTracker>();
            if (tracker == null) return;
            
            tracker.Data.PropertyBlock.SetFloat(PropertyIDs.IsHovered, isHovered ? 1 : 0);
            tracker.Data.Renderer.SetPropertyBlock(tracker.Data.PropertyBlock);
        }
        
        /// <summary>
        /// Set selection state on resource
        /// </summary>
        public void SetResourceSelected(GameObject target, bool isSelected)
        {
            var tracker = target.GetComponent<ResourceHighlightTracker>();
            if (tracker == null) return;
            
            tracker.Data.PropertyBlock.SetFloat(PropertyIDs.IsSelected, isSelected ? 1 : 0);
            tracker.Data.Renderer.SetPropertyBlock(tracker.Data.PropertyBlock);
        }
        
        /// <summary>
        /// Animate resource discovery
        /// </summary>
        public void AnimateResourceDiscovery(GameObject target, float duration = 2f)
        {
            var tracker = target.GetComponent<ResourceHighlightTracker>();
            if (tracker == null) return;
            
            StartCoroutine(DiscoveryAnimation(tracker, duration));
        }
        
        private System.Collections.IEnumerator DiscoveryAnimation(ResourceHighlightTracker tracker, float duration)
        {
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                tracker.Data.PropertyBlock.SetFloat(PropertyIDs.DiscoveryProgress, progress);
                tracker.Data.Renderer.SetPropertyBlock(tracker.Data.PropertyBlock);
                
                yield return null;
            }
            
            tracker.Data.PropertyBlock.SetFloat(PropertyIDs.DiscoveryProgress, 1);
            tracker.Data.Renderer.SetPropertyBlock(tracker.Data.PropertyBlock);
        }
        
        #endregion
        
        #region Combat Effects
        
        /// <summary>
        /// Spawn combat effect
        /// </summary>
        public void SpawnCombatEffect(CombatEffectType type, Vector3 position, 
            Color color, float duration, Vector3? target = null)
        {
            if (_activeEffects.Count >= maxActiveEffects)
            {
                // Remove oldest effect
                var oldest = _activeEffects[0];
                ReturnEffect(oldest);
                _activeEffects.RemoveAt(0);
            }
            
            var effect = GetEffect();
            effect.Type = EffectCategory.Combat;
            effect.CombatType = type;
            effect.Position = position;
            effect.TargetPosition = target ?? position;
            effect.Color = color;
            effect.Duration = duration;
            effect.StartTime = Time.time;
            effect.IsActive = true;
            
            // Create visual
            CreateCombatEffectVisual(effect);
            
            _activeEffects.Add(effect);
        }
        
        private void CreateCombatEffectVisual(ActiveEffect effect)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = $"CombatEffect_{effect.CombatType}";
            obj.transform.position = effect.Position;
            obj.transform.localScale = Vector3.one * 3f;
            
            Destroy(obj.GetComponent<Collider>());
            
            var mat = new Material(combatEffectsMaterial);
            mat.SetFloat(PropertyIDs.EffectType, (float)effect.CombatType);
            mat.SetColor(PropertyIDs.MainColor, effect.Color);
            
            // Enable correct keyword
            mat.EnableKeyword($"_EFFECTTYPE_{effect.CombatType.ToString().ToUpper()}");
            
            obj.GetComponent<Renderer>().material = mat;
            
            // Make it face camera
            var billboard = obj.AddComponent<BillboardEffect>();
            
            effect.VisualObject = obj;
            effect.Material = mat;
        }
        
        /// <summary>
        /// Shield hit effect
        /// </summary>
        public void PlayShieldHit(GameObject shield, Vector3 hitPoint, float intensity = 1f)
        {
            var renderer = shield.GetComponent<Renderer>();
            if (renderer == null) return;
            
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            
            block.SetVector(PropertyIDs.HitPoint, hitPoint);
            block.SetFloat(PropertyIDs.HitIntensity, intensity);
            
            renderer.SetPropertyBlock(block);
            
            // Fade out hit over time
            StartCoroutine(FadeShieldHit(renderer, block));
        }
        
        private System.Collections.IEnumerator FadeShieldHit(Renderer renderer, MaterialPropertyBlock block)
        {
            float elapsed = 0;
            float duration = 0.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float intensity = 1f - elapsed / duration;
                
                block.SetFloat(PropertyIDs.HitIntensity, intensity);
                renderer.SetPropertyBlock(block);
                
                yield return null;
            }
            
            block.SetFloat(PropertyIDs.HitIntensity, 0);
            renderer.SetPropertyBlock(block);
        }
        
        /// <summary>
        /// Enter/exit combat mode (intensifies all effects)
        /// </summary>
        public void SetCombatMode(bool inCombat)
        {
            _inCombatMode = inCombat;
            OnCombatModeChanged?.Invoke(inCombat);
        }
        
        #endregion
        
        #region Weather Effects
        
        /// <summary>
        /// Set weather type with transition
        /// </summary>
        public void SetWeather(WeatherType type, float transitionDuration = 3f)
        {
            _targetWeather = GetWeatherState(type);
            _weatherTransitionProgress = 0;
            
            OnWeatherChanged?.Invoke(type);
            
            StartCoroutine(TransitionWeather(transitionDuration));
        }
        
        private WeatherState GetWeatherState(WeatherType type)
        {
            var state = new WeatherState { Type = type };
            
            switch (type)
            {
                case WeatherType.Clear:
                    state.ParticleDensity = 0;
                    state.FogDensity = 0;
                    state.WindStrength = 0.2f;
                    break;
                    
                case WeatherType.Rain:
                    state.ParticleDensity = 50;
                    state.FogDensity = 0.1f;
                    state.WindStrength = 1f;
                    state.WindDirection = new Vector3(0.5f, 0, 0.3f);
                    break;
                    
                case WeatherType.HeavyRain:
                    state.ParticleDensity = 80;
                    state.FogDensity = 0.2f;
                    state.WindStrength = 2f;
                    state.WindDirection = new Vector3(0.7f, 0, 0.4f);
                    break;
                    
                case WeatherType.Storm:
                    state.ParticleDensity = 100;
                    state.FogDensity = 0.3f;
                    state.WindStrength = 3f;
                    state.WindDirection = new Vector3(1f, 0, 0.5f);
                    state.LightningIntensity = 3f;
                    break;
                    
                case WeatherType.Snow:
                    state.ParticleDensity = 60;
                    state.FogDensity = 0.15f;
                    state.WindStrength = 0.5f;
                    state.WindDirection = new Vector3(0.3f, 0, 0.2f);
                    break;
                    
                case WeatherType.Blizzard:
                    state.ParticleDensity = 90;
                    state.FogDensity = 0.4f;
                    state.WindStrength = 2.5f;
                    state.WindDirection = new Vector3(0.8f, 0, 0.6f);
                    break;
                    
                case WeatherType.Fog:
                    state.ParticleDensity = 0;
                    state.FogDensity = 0.6f;
                    state.WindStrength = 0.1f;
                    break;
                    
                case WeatherType.DustStorm:
                    state.ParticleDensity = 70;
                    state.FogDensity = 0.3f;
                    state.WindStrength = 2f;
                    state.WindDirection = new Vector3(1f, 0, 0);
                    break;
            }
            
            return state;
        }
        
        private System.Collections.IEnumerator TransitionWeather(float duration)
        {
            WeatherState startWeather = new WeatherState
            {
                Type = _currentWeather.Type,
                ParticleDensity = _currentWeather.ParticleDensity,
                FogDensity = _currentWeather.FogDensity,
                WindStrength = _currentWeather.WindStrength,
                WindDirection = _currentWeather.WindDirection,
                LightningIntensity = _currentWeather.LightningIntensity
            };
            
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _weatherTransitionProgress = elapsed / duration;
                
                // Interpolate weather values
                _currentWeather.ParticleDensity = Mathf.Lerp(
                    startWeather.ParticleDensity, 
                    _targetWeather.ParticleDensity, 
                    _weatherTransitionProgress);
                    
                _currentWeather.FogDensity = Mathf.Lerp(
                    startWeather.FogDensity, 
                    _targetWeather.FogDensity, 
                    _weatherTransitionProgress);
                    
                _currentWeather.WindStrength = Mathf.Lerp(
                    startWeather.WindStrength, 
                    _targetWeather.WindStrength, 
                    _weatherTransitionProgress);
                    
                _currentWeather.WindDirection = Vector3.Lerp(
                    startWeather.WindDirection, 
                    _targetWeather.WindDirection, 
                    _weatherTransitionProgress);
                    
                _currentWeather.LightningIntensity = Mathf.Lerp(
                    startWeather.LightningIntensity, 
                    _targetWeather.LightningIntensity, 
                    _weatherTransitionProgress);
                
                yield return null;
            }
            
            _currentWeather = _targetWeather;
            _weatherTransitionProgress = 1f;
        }
        
        private void UpdateWeatherTransition()
        {
            if (weatherEffectsMaterial == null) return;
            
            weatherEffectsMaterial.SetFloat(PropertyIDs.ParticleDensity, _currentWeather.ParticleDensity);
            weatherEffectsMaterial.SetFloat(PropertyIDs.FogDensity, _currentWeather.FogDensity);
            weatherEffectsMaterial.SetFloat(PropertyIDs.WindStrength, _currentWeather.WindStrength);
            weatherEffectsMaterial.SetVector(PropertyIDs.WindDirection, _currentWeather.WindDirection);
            weatherEffectsMaterial.SetFloat(PropertyIDs.LightningIntensity, _currentWeather.LightningIntensity);
        }
        
        #endregion
        
        #region Update Logic
        
        private void UpdateActiveEffects()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                
                float elapsed = Time.time - effect.StartTime;
                float progress = elapsed / effect.Duration;
                
                if (progress >= 1f)
                {
                    // Effect complete
                    ReturnEffect(effect);
                    _activeEffects.RemoveAt(i);
                    continue;
                }
                
                // Update effect properties
                UpdateEffectProgress(effect, progress);
            }
        }
        
        private void UpdateEffectProgress(ActiveEffect effect, float progress)
        {
            if (effect.Material == null) return;
            
            effect.Material.SetFloat(PropertyIDs.LifetimeProgress, progress);
            
            if (effect.CombatType == CombatEffectType.Impact)
            {
                effect.Material.SetFloat(PropertyIDs.ImpactProgress, progress);
            }
        }
        
        private void UpdateCombatMode()
        {
            float targetValue = _inCombatMode ? 1f : 0f;
            _combatModeTransition = Mathf.MoveTowards(_combatModeTransition, targetValue, Time.deltaTime * 2f);
            
            // Update all territory borders
            foreach (var border in _territoryBorders.Values)
            {
                border.PropertyBlock.SetFloat(PropertyIDs.CombatMode, _combatModeTransition);
                border.Renderer.SetPropertyBlock(border.PropertyBlock);
            }
        }
        
        private void UpdateGlobalParameters()
        {
            // Update global shader parameters
            Shader.SetGlobalFloat("_GlobalEffectIntensity", globalEffectIntensity);
            Shader.SetGlobalFloat("_GlobalTime", Time.time);
        }
        
        #endregion
        
        #region Pool Management
        
        private ActiveEffect GetEffect()
        {
            if (_effectPool.Count > 0)
            {
                return _effectPool.Dequeue();
            }
            return new ActiveEffect();
        }
        
        private void ReturnEffect(ActiveEffect effect)
        {
            if (effect.VisualObject != null)
            {
                Destroy(effect.VisualObject);
            }
            
            effect.IsActive = false;
            effect.VisualObject = null;
            effect.Material = null;
            
            _effectPool.Enqueue(effect);
        }
        
        #endregion
    }
    
    #region Helper Components
    
    public class BillboardEffect : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
    
    public class ResourceHighlightTracker : MonoBehaviour
    {
        public ResourceHighlightData Data;
    }
    
    #endregion
    
    #region Data Types
    
    public enum EffectCategory
    {
        Territory,
        Resource,
        Combat,
        Weather
    }
    
    public enum ResourceEffectType
    {
        Gold = 0,
        Wood = 1,
        Stone = 2,
        Food = 3,
        Mana = 4,
        Rare = 5
    }
    
    public enum CombatEffectType
    {
        Projectile = 0,
        Impact = 1,
        Shield = 2,
        Damage = 3,
        Heal = 4,
        Buff = 5,
        Debuff = 6
    }
    
    public enum WeatherType
    {
        Clear,
        Rain,
        HeavyRain,
        Storm,
        Snow,
        Blizzard,
        Fog,
        DustStorm
    }
    
    public class TerritoryBorderData
    {
        public string Id;
        public GameObject Object;
        public MeshFilter MeshFilter;
        public MeshRenderer Renderer;
        public MaterialPropertyBlock PropertyBlock;
    }
    
    public class ResourceHighlightData
    {
        public GameObject Target;
        public Renderer Renderer;
        public ResourceEffectType ResourceType;
        public MaterialPropertyBlock PropertyBlock;
    }
    
    public class ActiveEffect
    {
        public EffectCategory Type;
        public CombatEffectType CombatType;
        public Vector3 Position;
        public Vector3 TargetPosition;
        public Color Color;
        public float Duration;
        public float StartTime;
        public bool IsActive;
        public GameObject VisualObject;
        public Material Material;
    }
    
    public class WeatherState
    {
        public WeatherType Type;
        public float ParticleDensity;
        public float FogDensity;
        public float WindStrength;
        public Vector3 WindDirection;
        public float LightningIntensity;
    }
    
    #endregion
}
