using UnityEngine;
using System;
using System.Collections.Generic;
using ApexCitadels.Core;

namespace ApexCitadels.PC.Buildings
{
    /// <summary>
    /// Building Visual Effects Manager for PC client.
    /// Adds atmospheric and interactive visual effects to buildings:
    /// - Ambient lighting (torches, windows)
    /// - Particle effects (smoke, magic auras)
    /// - State-based visuals (damaged, upgrading, producing)
    /// - Interactive highlights (selection, hover)
    /// </summary>
    public class BuildingVisualEffects : MonoBehaviour
    {
        [Header("Ambient Lighting")]
        [SerializeField] private bool enableAmbientLights = true;
        [SerializeField] private float torchFlickerSpeed = 5f;
        [SerializeField] private float torchFlickerAmount = 0.3f;
        [SerializeField] private Color torchColor = new Color(1f, 0.7f, 0.4f);
        [SerializeField] private float windowGlowIntensity = 0.8f;
        [SerializeField] private Color windowGlowColor = new Color(1f, 0.9f, 0.6f);
        
        [Header("Smoke Effects")]
        [SerializeField] private bool enableSmoke = true;
        [SerializeField] private float smokeRate = 10f;
        [SerializeField] private Color smokeColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        
        [Header("Magic Auras")]
        [SerializeField] private bool enableMagicAuras = true;
        [SerializeField] private float auraIntensity = 1f;
        [SerializeField] private float auraPulseSpeed = 1.5f;
        
        [Header("Building States")]
        [SerializeField] private Color constructionColor = new Color(0.8f, 0.6f, 0.2f, 0.5f);
        [SerializeField] private Color damagedColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color producingColor = new Color(0.2f, 0.8f, 0.3f, 0.4f);
        [SerializeField] private Color upgradingColor = new Color(0.5f, 0.5f, 1f, 0.5f);
        
        [Header("Selection Effects")]
        [SerializeField] private bool enableSelectionEffects = true;
        [SerializeField] private Color selectionColor = new Color(0.3f, 0.7f, 1f, 0.5f);
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.5f, 0.3f);
        [SerializeField] private float selectionPulseSpeed = 2f;
        [SerializeField] private float outlineWidth = 0.03f;
        
        [Header("Day/Night Integration")]
        [SerializeField] private bool respondToDayNight = true;
        [SerializeField] private float nightLightMultiplier = 2f;
        
        // Singleton
        private static BuildingVisualEffects _instance;
        public static BuildingVisualEffects Instance => _instance;
        
        // Managed buildings
        private Dictionary<string, BuildingEffectData> _buildingEffects = new Dictionary<string, BuildingEffectData>();
        
        // State
        private string _selectedBuildingId;
        private string _hoveredBuildingId;
        private float _globalTime;
        private bool _isNighttime;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        private void Update()
        {
            _globalTime += Time.deltaTime;
            
            // Update all building effects
            foreach (var kvp in _buildingEffects)
            {
                UpdateBuildingEffects(kvp.Value);
            }
        }
        
        #region Building Registration
        
        /// <summary>
        /// Register a building for visual effects
        /// </summary>
        public void RegisterBuilding(string id, GameObject building, BuildingEffectType defaultType = BuildingEffectType.Standard)
        {
            if (_buildingEffects.ContainsKey(id))
            {
                ApexLogger.LogWarning($"[BuildingVFX] Building already registered: {id}", ApexLogger.LogCategory.Building);
                return;
            }
            
            var effectData = new BuildingEffectData
            {
                Id = id,
                Root = building,
                EffectType = defaultType,
                State = BuildingState.Normal,
                Lights = new List<Light>(),
                ParticleSystems = new List<ParticleSystem>(),
                WindowRenderers = new List<Renderer>(),
                OriginalMaterials = new Dictionary<Renderer, Material[]>()
            };
            
            // Find existing lights
            var lights = building.GetComponentsInChildren<Light>();
            effectData.Lights.AddRange(lights);
            
            // Cache original materials for state effects
            var renderers = building.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                effectData.OriginalMaterials[renderer] = renderer.sharedMaterials;
                
                // Identify windows
                if (renderer.gameObject.name.Contains("Glass") || 
                    renderer.gameObject.name.Contains("Window"))
                {
                    effectData.WindowRenderers.Add(renderer);
                }
            }
            
            // Add default effects based on type
            SetupDefaultEffects(effectData);
            
            _buildingEffects[id] = effectData;
        }
        
        /// <summary>
        /// Unregister a building
        /// </summary>
        public void UnregisterBuilding(string id)
        {
            if (_buildingEffects.TryGetValue(id, out var data))
            {
                // Cleanup created effects
                foreach (var ps in data.ParticleSystems)
                {
                    if (ps != null) Destroy(ps.gameObject);
                }
                
                // Restore original materials
                foreach (var kvp in data.OriginalMaterials)
                {
                    if (kvp.Key != null)
                    {
                        kvp.Key.sharedMaterials = kvp.Value;
                    }
                }
                
                if (data.SelectionIndicator != null)
                {
                    Destroy(data.SelectionIndicator);
                }
                
                _buildingEffects.Remove(id);
            }
        }
        
        private void SetupDefaultEffects(BuildingEffectData data)
        {
            switch (data.EffectType)
            {
                case BuildingEffectType.Residential:
                    SetupWindowGlow(data);
                    if (enableSmoke) AddChimneySmoke(data);
                    break;
                    
                case BuildingEffectType.Production:
                    SetupWindowGlow(data);
                    if (enableSmoke) AddProductionSmoke(data);
                    AddActivityParticles(data, producingColor);
                    break;
                    
                case BuildingEffectType.Defense:
                    AddTorches(data);
                    break;
                    
                case BuildingEffectType.Magic:
                    SetupWindowGlow(data);
                    if (enableMagicAuras) AddMagicAura(data);
                    break;
                    
                case BuildingEffectType.Military:
                    AddTorches(data);
                    break;
            }
        }
        
        #endregion
        
        #region Effect Setup
        
        private void SetupWindowGlow(BuildingEffectData data)
        {
            foreach (var windowRenderer in data.WindowRenderers)
            {
                // Create emissive material for windows
                var mat = new Material(windowRenderer.material);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", windowGlowColor * windowGlowIntensity);
                windowRenderer.material = mat;
                
                // Add point light for each window (if night)
                if (respondToDayNight && enableAmbientLights)
                {
                    var lightObj = new GameObject("WindowLight");
                    lightObj.transform.SetParent(windowRenderer.transform);
                    lightObj.transform.localPosition = Vector3.zero;
                    
                    var light = lightObj.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = windowGlowColor;
                    light.intensity = 0; // Start off, enable at night
                    light.range = 3f;
                    light.shadows = LightShadows.None;
                    
                    data.Lights.Add(light);
                    data.WindowLights.Add(light);
                }
            }
        }
        
        private void AddTorches(BuildingEffectData data)
        {
            if (!enableAmbientLights) return;
            
            // Find or create torch positions
            var torchTransforms = new List<Transform>();
            
            foreach (Transform child in data.Root.transform)
            {
                if (child.name.Contains("Torch"))
                {
                    torchTransforms.Add(child);
                }
            }
            
            // If no torches found, add at default positions
            if (torchTransforms.Count == 0)
            {
                var bounds = CalculateBounds(data.Root);
                Vector3[] positions = new Vector3[]
                {
                    new Vector3(bounds.min.x, bounds.center.y, bounds.max.z),
                    new Vector3(bounds.max.x, bounds.center.y, bounds.max.z)
                };
                
                foreach (var pos in positions)
                {
                    var torch = CreateTorchEffect(data.Root.transform, pos - data.Root.transform.position);
                    data.Lights.Add(torch.GetComponent<Light>());
                    data.ParticleSystems.Add(torch.GetComponentInChildren<ParticleSystem>());
                }
            }
            else
            {
                foreach (var torch in torchTransforms)
                {
                    // Add fire particle effect
                    var fire = CreateFireParticles(torch);
                    data.ParticleSystems.Add(fire);
                    
                    // Add flickering light
                    var light = torch.GetComponent<Light>();
                    if (light == null)
                    {
                        light = torch.gameObject.AddComponent<Light>();
                        light.type = LightType.Point;
                        light.color = torchColor;
                        light.intensity = 2f;
                        light.range = 6f;
                    }
                    data.Lights.Add(light);
                }
            }
        }
        
        private GameObject CreateTorchEffect(Transform parent, Vector3 localPos)
        {
            var torch = new GameObject("TorchEffect");
            torch.transform.SetParent(parent);
            torch.transform.localPosition = localPos;
            
            // Light
            var light = torch.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = torchColor;
            light.intensity = 2f;
            light.range = 6f;
            light.shadows = LightShadows.Soft;
            
            // Fire particles
            CreateFireParticles(torch.transform);
            
            return torch;
        }
        
        private ParticleSystem CreateFireParticles(Transform parent)
        {
            var fireObj = new GameObject("FireParticles");
            fireObj.transform.SetParent(parent);
            fireObj.transform.localPosition = Vector3.up * 0.2f;
            
            var ps = fireObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 1f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.8f, 0.2f),
                new Color(1f, 0.4f, 0.1f)
            );
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 50;
            
            var emission = ps.emission;
            emission.rateOverTime = 20;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15;
            shape.radius = 0.1f;
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0),
                    new GradientColorKey(new Color(1f, 0.3f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.5f, 0.1f, 0.05f), 1)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1, 0),
                    new GradientAlphaKey(0.5f, 0.5f),
                    new GradientAlphaKey(0, 1)
                }
            );
            colorOverLifetime.color = gradient;
            
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 
                new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(0.5f, 1f), new Keyframe(1, 0)));
            
            var renderer = fireObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            return ps;
        }
        
        private void AddChimneySmoke(BuildingEffectData data)
        {
            // Find chimney or add smoke at roof
            Transform smokePoint = null;
            foreach (Transform child in data.Root.transform)
            {
                if (child.name.Contains("Chimney"))
                {
                    smokePoint = child;
                    break;
                }
            }
            
            if (smokePoint == null)
            {
                var bounds = CalculateBounds(data.Root);
                var smokeObj = new GameObject("ChimneySmoke");
                smokeObj.transform.SetParent(data.Root.transform);
                smokeObj.transform.localPosition = new Vector3(
                    UnityEngine.Random.Range(-bounds.extents.x * 0.3f, bounds.extents.x * 0.3f),
                    bounds.max.y - data.Root.transform.position.y + 0.5f,
                    UnityEngine.Random.Range(-bounds.extents.z * 0.3f, bounds.extents.z * 0.3f)
                );
                smokePoint = smokeObj.transform;
            }
            
            var smoke = CreateSmokeParticles(smokePoint, smokeColor, smokeRate);
            data.ParticleSystems.Add(smoke);
        }
        
        private void AddProductionSmoke(BuildingEffectData data)
        {
            var bounds = CalculateBounds(data.Root);
            
            // Add multiple smoke stacks for production buildings
            int smokeCount = UnityEngine.Random.Range(1, 3);
            for (int i = 0; i < smokeCount; i++)
            {
                var smokeObj = new GameObject($"ProductionSmoke_{i}");
                smokeObj.transform.SetParent(data.Root.transform);
                smokeObj.transform.localPosition = new Vector3(
                    UnityEngine.Random.Range(-bounds.extents.x * 0.5f, bounds.extents.x * 0.5f),
                    bounds.max.y - data.Root.transform.position.y + 0.5f,
                    UnityEngine.Random.Range(-bounds.extents.z * 0.5f, bounds.extents.z * 0.5f)
                );
                
                var smoke = CreateSmokeParticles(smokeObj.transform, 
                    new Color(0.4f, 0.4f, 0.45f, 0.4f), smokeRate * 2f);
                data.ParticleSystems.Add(smoke);
            }
        }
        
        private ParticleSystem CreateSmokeParticles(Transform parent, Color color, float rate)
        {
            var smokeObj = new GameObject("SmokeParticles");
            smokeObj.transform.SetParent(parent);
            smokeObj.transform.localPosition = Vector3.zero;
            
            var ps = smokeObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;
            
            var emission = ps.emission;
            emission.rateOverTime = rate;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
            
            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.5f;
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(color, 0),
                    new GradientColorKey(color, 1)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0, 0),
                    new GradientAlphaKey(color.a, 0.2f),
                    new GradientAlphaKey(color.a * 0.5f, 0.8f),
                    new GradientAlphaKey(0, 1)
                }
            );
            colorOverLifetime.color = gradient;
            
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 
                new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 2f)));
            
            var renderer = smokeObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial();
            
            return ps;
        }
        
        private void AddMagicAura(BuildingEffectData data)
        {
            var bounds = CalculateBounds(data.Root);
            
            var auraObj = new GameObject("MagicAura");
            auraObj.transform.SetParent(data.Root.transform);
            auraObj.transform.localPosition = Vector3.up * 0.5f;
            
            var ps = auraObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 2f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.5f, 0.5f, 1f, 0.8f),
                new Color(0.8f, 0.5f, 1f, 0.8f)
            );
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 200;
            
            var emission = ps.emission;
            emission.rateOverTime = 30;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(bounds.size.x, 0.5f, bounds.size.z);
            
            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.5f, 0.5f, 1f), 0),
                    new GradientColorKey(new Color(1f, 0.5f, 1f), 1)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0, 0),
                    new GradientAlphaKey(0.8f, 0.3f),
                    new GradientAlphaKey(0, 1)
                }
            );
            colorOverLifetime.color = gradient;
            
            var renderer = auraObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial();
            
            data.ParticleSystems.Add(ps);
            data.MagicAura = ps;
        }
        
        private void AddActivityParticles(BuildingEffectData data, Color color)
        {
            var bounds = CalculateBounds(data.Root);
            
            var activityObj = new GameObject("ActivityParticles");
            activityObj.transform.SetParent(data.Root.transform);
            activityObj.transform.localPosition = Vector3.up * bounds.extents.y;
            
            var ps = activityObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 1f;
            main.startSpeed = 2f;
            main.startSize = 0.2f;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 50;
            
            var emission = ps.emission;
            emission.rateOverTime = 0; // Burst on activity
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0, 5)
            });
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;
            
            var renderer = activityObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial();
            
            data.ParticleSystems.Add(ps);
            data.ActivityParticles = ps;
            ps.Stop();
        }
        
        #endregion
        
        #region Effect Updates
        
        private void UpdateBuildingEffects(BuildingEffectData data)
        {
            if (data.Root == null) return;
            
            // Update torch flickering
            UpdateTorchFlicker(data);
            
            // Update magic aura pulse
            UpdateMagicAura(data);
            
            // Update selection/hover effects
            UpdateSelectionEffects(data);
            
            // Update night lighting
            if (respondToDayNight)
            {
                UpdateNightLighting(data);
            }
        }
        
        private void UpdateTorchFlicker(BuildingEffectData data)
        {
            foreach (var light in data.Lights)
            {
                if (light == null) continue;
                if (data.WindowLights.Contains(light)) continue; // Skip window lights
                
                // Perlin noise-based flicker
                float noise = Mathf.PerlinNoise(_globalTime * torchFlickerSpeed + light.GetHashCode() * 0.01f, 0);
                float flicker = 1f + (noise - 0.5f) * 2f * torchFlickerAmount;
                
                light.intensity = 2f * flicker * (respondToDayNight && _isNighttime ? nightLightMultiplier : 1f);
            }
        }
        
        private void UpdateMagicAura(BuildingEffectData data)
        {
            if (data.MagicAura == null) return;
            
            float pulse = (Mathf.Sin(_globalTime * auraPulseSpeed) + 1f) * 0.5f;
            
            var emission = data.MagicAura.emission;
            emission.rateOverTime = 20 + pulse * 20;
        }
        
        private void UpdateSelectionEffects(BuildingEffectData data)
        {
            bool isSelected = data.Id == _selectedBuildingId;
            bool isHovered = data.Id == _hoveredBuildingId;
            
            if (isSelected || isHovered)
            {
                // Create selection indicator if needed
                if (data.SelectionIndicator == null && enableSelectionEffects)
                {
                    CreateSelectionIndicator(data);
                }
                
                if (data.SelectionIndicator != null)
                {
                    data.SelectionIndicator.SetActive(true);
                    
                    // Update indicator appearance
                    var renderer = data.SelectionIndicator.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Color targetColor = isSelected ? selectionColor : hoverColor;
                        float pulse = (Mathf.Sin(_globalTime * selectionPulseSpeed) + 1f) * 0.5f;
                        targetColor.a = targetColor.a * (0.5f + pulse * 0.5f);
                        renderer.material.color = targetColor;
                    }
                }
            }
            else if (data.SelectionIndicator != null)
            {
                data.SelectionIndicator.SetActive(false);
            }
        }
        
        private void CreateSelectionIndicator(BuildingEffectData data)
        {
            var bounds = CalculateBounds(data.Root);
            
            // Create circular indicator under building
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "SelectionIndicator";
            indicator.transform.SetParent(data.Root.transform);
            indicator.transform.localPosition = new Vector3(0, 0.1f, 0);
            indicator.transform.localScale = new Vector3(
                Mathf.Max(bounds.size.x, bounds.size.z) * 1.2f,
                0.05f,
                Mathf.Max(bounds.size.x, bounds.size.z) * 1.2f
            );
            
            Destroy(indicator.GetComponent<Collider>());
            
            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
            mat.color = selectionColor;
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.renderQueue = 3000;
            indicator.GetComponent<Renderer>().material = mat;
            
            data.SelectionIndicator = indicator;
        }
        
        private void UpdateNightLighting(BuildingEffectData data)
        {
            // Enable/disable window lights based on time
            float intensity = _isNighttime ? windowGlowIntensity * nightLightMultiplier : 0f;
            
            foreach (var light in data.WindowLights)
            {
                if (light != null)
                {
                    light.intensity = Mathf.Lerp(light.intensity, intensity, Time.deltaTime * 2f);
                }
            }
            
            // Update window emission
            foreach (var windowRenderer in data.WindowRenderers)
            {
                if (windowRenderer != null && windowRenderer.material != null)
                {
                    Color emission = _isNighttime ? windowGlowColor * windowGlowIntensity : Color.black;
                    windowRenderer.material.SetColor("_EmissionColor", emission);
                }
            }
        }
        
        #endregion
        
        #region Building State
        
        /// <summary>
        /// Set building state (affects visuals)
        /// </summary>
        public void SetBuildingState(string id, BuildingState state)
        {
            if (!_buildingEffects.TryGetValue(id, out var data)) return;
            
            data.State = state;
            
            // Apply state-specific visuals
            switch (state)
            {
                case BuildingState.Constructing:
                    ApplyStateOverlay(data, constructionColor);
                    break;
                    
                case BuildingState.Damaged:
                    ApplyStateOverlay(data, damagedColor);
                    AddDamageEffects(data);
                    break;
                    
                case BuildingState.Producing:
                    if (data.ActivityParticles != null)
                    {
                        data.ActivityParticles.Play();
                    }
                    break;
                    
                case BuildingState.Upgrading:
                    ApplyStateOverlay(data, upgradingColor);
                    break;
                    
                case BuildingState.Normal:
                default:
                    RemoveStateOverlay(data);
                    break;
            }
        }
        
        private void ApplyStateOverlay(BuildingEffectData data, Color color)
        {
            foreach (var kvp in data.OriginalMaterials)
            {
                if (kvp.Key == null) continue;
                
                var overlayMat = new Material(kvp.Value[0]);
                overlayMat.color = Color.Lerp(overlayMat.color, color, color.a);
                overlayMat.EnableKeyword("_EMISSION");
                overlayMat.SetColor("_EmissionColor", color * 0.3f);
                
                kvp.Key.material = overlayMat;
            }
        }
        
        private void RemoveStateOverlay(BuildingEffectData data)
        {
            foreach (var kvp in data.OriginalMaterials)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.sharedMaterials = kvp.Value;
                }
            }
        }
        
        private void AddDamageEffects(BuildingEffectData data)
        {
            // Add smoke/fire for damaged buildings
            var bounds = CalculateBounds(data.Root);
            
            var damageSmoke = CreateSmokeParticles(
                data.Root.transform,
                new Color(0.2f, 0.2f, 0.2f, 0.6f),
                30f
            );
            damageSmoke.transform.localPosition = Vector3.up * bounds.extents.y;
            data.ParticleSystems.Add(damageSmoke);
        }
        
        #endregion
        
        #region Selection API
        
        /// <summary>
        /// Select a building
        /// </summary>
        public void SelectBuilding(string id)
        {
            _selectedBuildingId = id;
        }
        
        /// <summary>
        /// Deselect current building
        /// </summary>
        public void DeselectBuilding()
        {
            _selectedBuildingId = null;
        }
        
        /// <summary>
        /// Set hovered building
        /// </summary>
        public void SetHoveredBuilding(string id)
        {
            _hoveredBuildingId = id;
        }
        
        /// <summary>
        /// Set day/night state
        /// </summary>
        public void SetNighttime(bool isNight)
        {
            _isNighttime = isNight;
        }
        
        /// <summary>
        /// Trigger activity effect on building
        /// </summary>
        public void TriggerActivity(string id)
        {
            if (_buildingEffects.TryGetValue(id, out var data))
            {
                if (data.ActivityParticles != null)
                {
                    data.ActivityParticles.Play();
                }
            }
        }
        
        #endregion
        
        #region Helpers
        
        private Material CreateParticleMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            
            var mat = new Material(shader);
            mat.SetFloat("_SurfaceType", 1);
            mat.EnableKeyword("_ALPHABLEND_ON");
            return mat;
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
        
        #endregion
    }
    
    #region Data Types
    
    public enum BuildingEffectType
    {
        Standard,
        Residential,
        Production,
        Defense,
        Magic,
        Military
    }
    
    public enum BuildingState
    {
        Normal,
        Constructing,
        Damaged,
        Producing,
        Upgrading,
        Disabled
    }
    
    public class BuildingEffectData
    {
        public string Id;
        public GameObject Root;
        public BuildingEffectType EffectType;
        public BuildingState State;
        public List<Light> Lights;
        public List<Light> WindowLights = new List<Light>();
        public List<ParticleSystem> ParticleSystems;
        public List<Renderer> WindowRenderers;
        public Dictionary<Renderer, Material[]> OriginalMaterials;
        public GameObject SelectionIndicator;
        public ParticleSystem MagicAura;
        public ParticleSystem ActivityParticles;
    }
    
    #endregion
}
