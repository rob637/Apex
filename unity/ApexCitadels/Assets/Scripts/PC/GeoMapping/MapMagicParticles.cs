using Camera = UnityEngine.Camera;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ApexCitadels.Core;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Magical particle system manager for map ambiance.
    /// Spawns floating orbs, sparkles, mist, and magical effects over territories.
    /// Creates an immersive fantasy atmosphere across the real-world map.
    /// </summary>
    public class MapMagicParticles : MonoBehaviour
    {
        [Header("Global Settings")]
        [SerializeField] private bool enableParticles = true;
        [SerializeField, Range(0f, 1f)] private float globalIntensity = 1f;
        [SerializeField] private Transform particleContainer;
        
        [Header("Floating Orbs")]
        [SerializeField] private bool enableFloatingOrbs = true;
        [SerializeField] private int orbCount = 50;
        [SerializeField] private float orbSpawnRadius = 300f;
        [SerializeField] private Vector2 orbHeightRange = new Vector2(10f, 50f);
        [SerializeField] private Vector2 orbSizeRange = new Vector2(0.5f, 2f);
        [SerializeField] private float orbSpeed = 2f;
        [SerializeField] private Gradient orbColorGradient;
        
        [Header("Sparkles")]
        [SerializeField] private bool enableSparkles = true;
        [SerializeField] private int sparkleRate = 20;
        [SerializeField] private float sparkleRadius = 200f;
        [SerializeField] private float sparkleLifetime = 2f;
        [SerializeField] private Color sparkleColor = new Color(1f, 0.95f, 0.8f, 0.8f);
        
        [Header("Ground Mist")]
        [SerializeField] private bool enableMist = true;
        [SerializeField] private float mistHeight = 5f;
        [SerializeField] private float mistDensity = 0.3f;
        [SerializeField] private Color mistColor = new Color(0.8f, 0.85f, 0.9f, 0.3f);
        [SerializeField] private float mistScrollSpeed = 0.5f;
        
        [Header("Territory Auras")]
        [SerializeField] private bool enableTerritoryAuras = true;
        [SerializeField] private float auraIntensity = 1f;
        [SerializeField] private Color ownedTerritoryColor = new Color(0.3f, 0.8f, 0.4f, 0.5f);
        [SerializeField] private Color enemyTerritoryColor = new Color(0.8f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color neutralTerritoryColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        
        [Header("Magical Trails")]
        [SerializeField] private bool enableMagicTrails = true;
        [SerializeField] private int trailCount = 10;
        [SerializeField] private float trailSpeed = 5f;
        [SerializeField] private float trailLength = 10f;
        
        [Header("Weather Effects")]
        [SerializeField] private WeatherEffect currentWeather = WeatherEffect.Clear;
        [SerializeField] private bool dynamicWeather = true;
        [SerializeField] private float weatherChangeInterval = 300f; // 5 minutes
        
        // Particle systems
        private ParticleSystem _orbSystem;
        private ParticleSystem _sparkleSystem;
        private ParticleSystem _mistSystem;
        private ParticleSystem _rainSystem;
        private ParticleSystem _snowSystem;
        private ParticleSystem _firefliesSystem;
        
        // Active magical objects
        private List<MagicOrb> _activeOrbs = new List<MagicOrb>();
        private List<MagicTrail> _activeTrails = new List<MagicTrail>();
        private Dictionary<string, ParticleSystem> _territoryAuras = new Dictionary<string, ParticleSystem>();
        
        // Runtime state
        private float _weatherTimer;
        private Camera _mainCamera;
        
        // Singleton
        private static MapMagicParticles _instance;
        public static MapMagicParticles Instance => _instance;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeColorGradient();
        }
        
        private void Start()
        {
            _mainCamera = Camera.main;
            
            if (particleContainer == null)
            {
                var container = new GameObject("ParticleContainer");
                container.transform.SetParent(transform);
                particleContainer = container.transform;
            }
            
            StartCoroutine(Initialize());
        }
        
        private IEnumerator Initialize()
        {
            yield return null;
            
            // Create particle systems
            if (enableFloatingOrbs)
            {
                CreateOrbSystem();
                SpawnOrbs();
            }
            
            if (enableSparkles)
            {
                CreateSparkleSystem();
            }
            
            if (enableMist)
            {
                CreateMistSystem();
            }
            
            if (enableMagicTrails)
            {
                SpawnMagicTrails();
            }
            
            // Setup weather systems
            CreateWeatherSystems();
            
            ApexLogger.Log("Initialized with " + _activeOrbs.Count + " orbs", ApexLogger.LogCategory.Map);
        }
        
        private void Update()
        {
            if (!enableParticles) return;
            
            // Update orbs
            UpdateOrbs();
            
            // Update trails
            UpdateTrails();
            
            // Update weather
            if (dynamicWeather)
            {
                _weatherTimer += Time.deltaTime;
                if (_weatherTimer >= weatherChangeInterval)
                {
                    _weatherTimer = 0f;
                    RandomizeWeather();
                }
            }
            
            // Keep particles centered on camera
            if (_mainCamera != null)
            {
                Vector3 camPos = _mainCamera.transform.position;
                particleContainer.position = new Vector3(camPos.x, 0, camPos.z);
            }
        }
        
        private void InitializeColorGradient()
        {
            if (orbColorGradient == null || orbColorGradient.colorKeys.Length == 0)
            {
                orbColorGradient = new Gradient();
                orbColorGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.5f, 0.7f, 1f), 0f),      // Blue
                        new GradientColorKey(new Color(0.8f, 0.5f, 1f), 0.25f),   // Purple
                        new GradientColorKey(new Color(0.4f, 1f, 0.6f), 0.5f),    // Green
                        new GradientColorKey(new Color(1f, 0.8f, 0.4f), 0.75f),   // Gold
                        new GradientColorKey(new Color(0.5f, 0.7f, 1f), 1f)       // Blue
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0.6f, 0f),
                        new GradientAlphaKey(0.8f, 0.5f),
                        new GradientAlphaKey(0.6f, 1f)
                    }
                );
            }
        }
        
        #region Floating Orbs
        
        private void CreateOrbSystem()
        {
            var orbObj = new GameObject("OrbParticles");
            orbObj.transform.SetParent(particleContainer);
            
            _orbSystem = orbObj.AddComponent<ParticleSystem>();
            var main = _orbSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = orbCount * 2;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = _orbSystem.emission;
            emission.enabled = false; // We manually spawn orbs
            
            var renderer = orbObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateGlowMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
        
        private void SpawnOrbs()
        {
            for (int i = 0; i < orbCount; i++)
            {
                var orb = new MagicOrb
                {
                    Position = GetRandomOrbPosition(),
                    Size = Random.Range(orbSizeRange.x, orbSizeRange.y),
                    Color = orbColorGradient.Evaluate(Random.value),
                    Speed = orbSpeed * Random.Range(0.5f, 1.5f),
                    Phase = Random.value * Mathf.PI * 2f,
                    WanderAngle = Random.value * 360f
                };
                
                CreateOrbVisual(orb);
                _activeOrbs.Add(orb);
            }
        }
        
        private Vector3 GetRandomOrbPosition()
        {
            Vector3 center = _mainCamera != null ? _mainCamera.transform.position : Vector3.zero;
            Vector2 offset = Random.insideUnitCircle * orbSpawnRadius;
            float height = Random.Range(orbHeightRange.x, orbHeightRange.y);
            return new Vector3(center.x + offset.x, height, center.z + offset.y);
        }
        
        private void CreateOrbVisual(MagicOrb orb)
        {
            var orbObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orbObj.name = "MagicOrb";
            orbObj.transform.SetParent(particleContainer);
            orbObj.transform.position = orb.Position;
            orbObj.transform.localScale = Vector3.one * orb.Size;
            
            Destroy(orbObj.GetComponent<Collider>());
            
            var renderer = orbObj.GetComponent<Renderer>();
            renderer.material = CreateGlowMaterial();
            renderer.material.color = orb.Color;
            renderer.material.SetColor("_EmissionColor", orb.Color * 2f);
            
            orb.GameObject = orbObj;
            
            // Add point light for glow effect
            var light = orbObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = orb.Color;
            light.intensity = 0.5f * orb.Size;
            light.range = orb.Size * 5f;
            light.shadows = LightShadows.None;
        }
        
        private void UpdateOrbs()
        {
            if (_mainCamera == null) return;
            Vector3 camPos = _mainCamera.transform.position;
            
            foreach (var orb in _activeOrbs)
            {
                if (orb.GameObject == null) continue;
                
                // Gentle floating motion
                orb.Phase += Time.deltaTime * orb.Speed * 0.5f;
                
                // Wander behavior
                orb.WanderAngle += Random.Range(-30f, 30f) * Time.deltaTime;
                Vector3 wanderDir = Quaternion.Euler(0, orb.WanderAngle, 0) * Vector3.forward;
                
                // Calculate new position
                Vector3 newPos = orb.Position;
                newPos += wanderDir * orb.Speed * Time.deltaTime;
                newPos.y = Mathf.Lerp(orbHeightRange.x, orbHeightRange.y, 
                    (Mathf.Sin(orb.Phase) + 1f) * 0.5f);
                
                // Keep within radius of camera
                Vector3 toOrb = newPos - camPos;
                toOrb.y = 0;
                if (toOrb.magnitude > orbSpawnRadius)
                {
                    // Wrap around to other side
                    newPos = camPos - toOrb.normalized * orbSpawnRadius * 0.9f;
                    newPos.y = orb.Position.y;
                }
                
                orb.Position = newPos;
                orb.GameObject.transform.position = newPos;
                
                // Pulse glow
                float pulse = (Mathf.Sin(orb.Phase * 2f) + 1f) * 0.5f;
                var light = orb.GameObject.GetComponent<Light>();
                if (light != null)
                {
                    light.intensity = (0.3f + pulse * 0.4f) * orb.Size * globalIntensity;
                }
                
                // Pulse size
                float sizeMultiplier = 1f + pulse * 0.2f;
                orb.GameObject.transform.localScale = Vector3.one * orb.Size * sizeMultiplier;
            }
        }
        
        #endregion
        
        #region Sparkles
        
        private void CreateSparkleSystem()
        {
            var sparkleObj = new GameObject("SparkleParticles");
            sparkleObj.transform.SetParent(particleContainer);
            
            _sparkleSystem = sparkleObj.AddComponent<ParticleSystem>();
            
            var main = _sparkleSystem.main;
            main.loop = true;
            main.startLifetime = sparkleLifetime;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = sparkleColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = sparkleRate * (int)sparkleLifetime * 2;
            
            var emission = _sparkleSystem.emission;
            emission.rateOverTime = sparkleRate * globalIntensity;
            
            var shape = _sparkleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = sparkleRadius;
            shape.radiusThickness = 1f;
            shape.rotation = new Vector3(-90, 0, 0);
            
            var colorOverLifetime = _sparkleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient sparkleGradient = new Gradient();
            sparkleGradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(sparkleColor, 0f), 
                    new GradientColorKey(sparkleColor, 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0f, 0f), 
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(1f, 0.8f),
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            colorOverLifetime.color = sparkleGradient;
            
            var sizeOverLifetime = _sparkleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.2f, 1),
                new Keyframe(0.8f, 1),
                new Keyframe(1, 0)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            
            var renderer = sparkleObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateGlowMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            _sparkleSystem.Play();
        }
        
        #endregion
        
        #region Mist
        
        private void CreateMistSystem()
        {
            var mistObj = new GameObject("MistParticles");
            mistObj.transform.SetParent(particleContainer);
            mistObj.transform.localPosition = new Vector3(0, mistHeight, 0);
            
            _mistSystem = mistObj.AddComponent<ParticleSystem>();
            
            var main = _mistSystem.main;
            main.loop = true;
            main.startLifetime = 10f;
            main.startSpeed = mistScrollSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(50f, 100f);
            main.startColor = mistColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;
            
            var emission = _mistSystem.emission;
            emission.rateOverTime = 5f * mistDensity * globalIntensity;
            
            var shape = _mistSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(500, 1, 500);
            
            var velocityOverLifetime = _mistSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(mistScrollSpeed * 0.5f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(mistScrollSpeed * 0.3f);
            
            var colorOverLifetime = _mistSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient mistGradient = new Gradient();
            mistGradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(mistColor, 0f), 
                    new GradientColorKey(mistColor, 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0f, 0f), 
                    new GradientAlphaKey(mistColor.a, 0.3f),
                    new GradientAlphaKey(mistColor.a, 0.7f),
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            colorOverLifetime.color = mistGradient;
            
            var noise = _mistSystem.noise;
            noise.enabled = true;
            noise.strength = 2f;
            noise.frequency = 0.2f;
            noise.scrollSpeed = 0.3f;
            
            var renderer = mistObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateMistMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingFudge = 10f;
            
            _mistSystem.Play();
        }
        
        #endregion
        
        #region Magic Trails
        
        private void SpawnMagicTrails()
        {
            for (int i = 0; i < trailCount; i++)
            {
                var trail = new MagicTrail
                {
                    Position = GetRandomTrailPosition(),
                    Target = GetRandomTrailPosition(),
                    Speed = trailSpeed * Random.Range(0.8f, 1.2f),
                    Color = orbColorGradient.Evaluate(Random.value)
                };
                
                CreateTrailVisual(trail);
                _activeTrails.Add(trail);
            }
        }
        
        private Vector3 GetRandomTrailPosition()
        {
            Vector3 center = _mainCamera != null ? _mainCamera.transform.position : Vector3.zero;
            Vector2 offset = Random.insideUnitCircle * orbSpawnRadius;
            float height = Random.Range(15f, 40f);
            return new Vector3(center.x + offset.x, height, center.z + offset.y);
        }
        
        private void CreateTrailVisual(MagicTrail trail)
        {
            var trailObj = new GameObject("MagicTrail");
            trailObj.transform.SetParent(particleContainer);
            trailObj.transform.position = trail.Position;
            
            // Create trail renderer
            var trailRenderer = trailObj.AddComponent<TrailRenderer>();
            trailRenderer.time = trailLength / trailSpeed;
            trailRenderer.startWidth = 0.5f;
            trailRenderer.endWidth = 0f;
            trailRenderer.material = CreateGlowMaterial();
            trailRenderer.startColor = trail.Color;
            trailRenderer.endColor = new Color(trail.Color.r, trail.Color.g, trail.Color.b, 0f);
            trailRenderer.numCapVertices = 5;
            trailRenderer.numCornerVertices = 5;
            
            // Add glowing head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "TrailHead";
            head.transform.SetParent(trailObj.transform);
            head.transform.localPosition = Vector3.zero;
            head.transform.localScale = Vector3.one * 0.5f;
            Destroy(head.GetComponent<Collider>());
            
            var headRenderer = head.GetComponent<Renderer>();
            headRenderer.material = CreateGlowMaterial();
            headRenderer.material.color = trail.Color;
            headRenderer.material.SetColor("_EmissionColor", trail.Color * 2f);
            
            // Add light
            var light = trailObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = trail.Color;
            light.intensity = 1f;
            light.range = 5f;
            light.shadows = LightShadows.None;
            
            trail.GameObject = trailObj;
        }
        
        private void UpdateTrails()
        {
            if (_mainCamera == null) return;
            Vector3 camPos = _mainCamera.transform.position;
            
            foreach (var trail in _activeTrails)
            {
                if (trail.GameObject == null) continue;
                
                // Move towards target
                Vector3 direction = (trail.Target - trail.Position).normalized;
                trail.Position += direction * trail.Speed * Time.deltaTime;
                trail.GameObject.transform.position = trail.Position;
                
                // Check if reached target
                if (Vector3.Distance(trail.Position, trail.Target) < 2f)
                {
                    // Pick new target
                    trail.Target = GetRandomTrailPosition();
                    
                    // Keep within radius
                    Vector3 toTarget = trail.Target - camPos;
                    toTarget.y = 0;
                    if (toTarget.magnitude > orbSpawnRadius)
                    {
                        trail.Target = camPos + toTarget.normalized * orbSpawnRadius * 0.8f;
                        trail.Target.y = Random.Range(15f, 40f);
                    }
                }
            }
        }
        
        #endregion
        
        #region Weather Effects
        
        private void CreateWeatherSystems()
        {
            // Rain
            var rainObj = new GameObject("RainParticles");
            rainObj.transform.SetParent(particleContainer);
            rainObj.transform.localPosition = new Vector3(0, 100, 0);
            
            _rainSystem = rainObj.AddComponent<ParticleSystem>();
            var rainMain = _rainSystem.main;
            rainMain.loop = true;
            rainMain.startLifetime = 2f;
            rainMain.startSpeed = 50f;
            rainMain.startSize = 0.1f;
            rainMain.startColor = new Color(0.7f, 0.8f, 0.9f, 0.5f);
            rainMain.simulationSpace = ParticleSystemSimulationSpace.World;
            rainMain.maxParticles = 5000;
            
            var rainEmission = _rainSystem.emission;
            rainEmission.rateOverTime = 500;
            
            var rainShape = _rainSystem.shape;
            rainShape.shapeType = ParticleSystemShapeType.Box;
            rainShape.scale = new Vector3(300, 1, 300);
            
            var rainRenderer = rainObj.GetComponent<ParticleSystemRenderer>();
            rainRenderer.material = CreateRainMaterial();
            rainRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            rainRenderer.lengthScale = 5f;
            
            _rainSystem.Stop();
            
            // Snow
            var snowObj = new GameObject("SnowParticles");
            snowObj.transform.SetParent(particleContainer);
            snowObj.transform.localPosition = new Vector3(0, 80, 0);
            
            _snowSystem = snowObj.AddComponent<ParticleSystem>();
            var snowMain = _snowSystem.main;
            snowMain.loop = true;
            snowMain.startLifetime = 8f;
            snowMain.startSpeed = 5f;
            snowMain.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            snowMain.startColor = Color.white;
            snowMain.simulationSpace = ParticleSystemSimulationSpace.World;
            snowMain.maxParticles = 2000;
            
            var snowEmission = _snowSystem.emission;
            snowEmission.rateOverTime = 200;
            
            var snowShape = _snowSystem.shape;
            snowShape.shapeType = ParticleSystemShapeType.Box;
            snowShape.scale = new Vector3(300, 1, 300);
            
            var snowNoise = _snowSystem.noise;
            snowNoise.enabled = true;
            snowNoise.strength = 1f;
            snowNoise.frequency = 0.5f;
            
            var snowRenderer = snowObj.GetComponent<ParticleSystemRenderer>();
            snowRenderer.material = CreateGlowMaterial();
            
            _snowSystem.Stop();
            
            // Fireflies (for evening/night)
            var firefliesObj = new GameObject("FirefliesParticles");
            firefliesObj.transform.SetParent(particleContainer);
            firefliesObj.transform.localPosition = new Vector3(0, 10, 0);
            
            _firefliesSystem = firefliesObj.AddComponent<ParticleSystem>();
            var fireflyMain = _firefliesSystem.main;
            fireflyMain.loop = true;
            fireflyMain.startLifetime = 5f;
            fireflyMain.startSpeed = 1f;
            fireflyMain.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            fireflyMain.startColor = new Color(1f, 0.9f, 0.4f, 1f);
            fireflyMain.simulationSpace = ParticleSystemSimulationSpace.World;
            fireflyMain.maxParticles = 100;
            
            var fireflyEmission = _firefliesSystem.emission;
            fireflyEmission.rateOverTime = 10;
            
            var fireflyShape = _firefliesSystem.shape;
            fireflyShape.shapeType = ParticleSystemShapeType.Box;
            fireflyShape.scale = new Vector3(200, 20, 200);
            
            var fireflyNoise = _firefliesSystem.noise;
            fireflyNoise.enabled = true;
            fireflyNoise.strength = 3f;
            fireflyNoise.frequency = 1f;
            
            var fireflyColorOverLifetime = _firefliesSystem.colorOverLifetime;
            fireflyColorOverLifetime.enabled = true;
            Gradient fireflyGradient = new Gradient();
            fireflyGradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f),
                    new GradientColorKey(new Color(1f, 0.9f, 0.4f), 1f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(0f, 0.4f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(0f, 0.9f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            fireflyColorOverLifetime.color = fireflyGradient;
            
            var fireflyRenderer = firefliesObj.GetComponent<ParticleSystemRenderer>();
            fireflyRenderer.material = CreateGlowMaterial();
            
            _firefliesSystem.Stop();
        }
        
        /// <summary>
        /// Set weather effect
        /// </summary>
        public void SetWeather(WeatherEffect weather)
        {
            currentWeather = weather;
            
            // Stop all weather
            _rainSystem?.Stop();
            _snowSystem?.Stop();
            _firefliesSystem?.Stop();
            
            // Start appropriate effect
            switch (weather)
            {
                case WeatherEffect.Rain:
                    _rainSystem?.Play();
                    break;
                    
                case WeatherEffect.Snow:
                    _snowSystem?.Play();
                    break;
                    
                case WeatherEffect.Fireflies:
                    _firefliesSystem?.Play();
                    break;
                    
                case WeatherEffect.Mystical:
                    _firefliesSystem?.Play();
                    // Increase sparkle rate for mystical atmosphere
                    if (_sparkleSystem != null)
                    {
                        var emission = _sparkleSystem.emission;
                        emission.rateOverTime = sparkleRate * 3f;
                    }
                    break;
                    
                case WeatherEffect.Clear:
                default:
                    // Reset sparkle rate
                    if (_sparkleSystem != null)
                    {
                        var emission = _sparkleSystem.emission;
                        emission.rateOverTime = sparkleRate * globalIntensity;
                    }
                    break;
            }
            
            ApexLogger.Log($"Weather changed to: {weather}", ApexLogger.LogCategory.Map);
        }
        
        private void RandomizeWeather()
        {
            var weatherTypes = System.Enum.GetValues(typeof(WeatherEffect));
            var randomWeather = (WeatherEffect)weatherTypes.GetValue(Random.Range(0, weatherTypes.Length));
            SetWeather(randomWeather);
        }
        
        #endregion
        
        #region Territory Auras
        
        /// <summary>
        /// Add aura effect to a territory
        /// </summary>
        public void AddTerritoryAura(string territoryId, Vector3 center, float radius, TerritoryOwnership ownership)
        {
            if (!enableTerritoryAuras) return;
            
            // Remove existing aura if any
            RemoveTerritoryAura(territoryId);
            
            var auraObj = new GameObject($"TerritoryAura_{territoryId}");
            auraObj.transform.SetParent(particleContainer);
            auraObj.transform.position = center;
            
            var auraSystem = auraObj.AddComponent<ParticleSystem>();
            var main = auraSystem.main;
            main.loop = true;
            main.startLifetime = 3f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            Color auraColor = ownership switch
            {
                TerritoryOwnership.Owned => ownedTerritoryColor,
                TerritoryOwnership.Enemy => enemyTerritoryColor,
                _ => neutralTerritoryColor
            };
            main.startColor = auraColor;
            
            var emission = auraSystem.emission;
            emission.rateOverTime = 20 * auraIntensity;
            
            var shape = auraSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * 0.8f;
            shape.radiusThickness = 0.2f;
            shape.rotation = new Vector3(-90, 0, 0);
            
            var velocityOverLifetime = auraSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.y = 2f;
            
            var colorOverLifetime = auraSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient auraGradient = new Gradient();
            auraGradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(auraColor, 0f),
                    new GradientColorKey(auraColor, 1f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(auraColor.a, 0.2f),
                    new GradientAlphaKey(auraColor.a * 0.5f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = auraGradient;
            
            var renderer = auraObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateGlowMaterial();
            
            auraSystem.Play();
            _territoryAuras[territoryId] = auraSystem;
        }
        
        /// <summary>
        /// Remove territory aura
        /// </summary>
        public void RemoveTerritoryAura(string territoryId)
        {
            if (_territoryAuras.TryGetValue(territoryId, out var aura))
            {
                if (aura != null)
                {
                    Destroy(aura.gameObject);
                }
                _territoryAuras.Remove(territoryId);
            }
        }
        
        #endregion
        
        #region Materials
        
        private Material CreateGlowMaterial()
        {
            // Try to find URP particle shader first
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            
            Material mat = new Material(shader);
            mat.SetFloat("_SurfaceType", 1); // Transparent
            mat.SetFloat("_BlendMode", 0); // Alpha
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            
            return mat;
        }
        
        private Material CreateMistMaterial()
        {
            var mat = CreateGlowMaterial();
            mat.SetFloat("_SoftParticlesEnabled", 1);
            mat.SetFloat("_SoftParticlesDistance", 1);
            return mat;
        }
        
        private Material CreateRainMaterial()
        {
            var mat = CreateGlowMaterial();
            mat.color = new Color(0.7f, 0.8f, 0.9f, 0.3f);
            return mat;
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Set global particle intensity (0-1)
        /// </summary>
        public void SetIntensity(float intensity)
        {
            globalIntensity = Mathf.Clamp01(intensity);
            
            // Update sparkle rate
            if (_sparkleSystem != null)
            {
                var emission = _sparkleSystem.emission;
                emission.rateOverTime = sparkleRate * globalIntensity;
            }
            
            // Update mist density
            if (_mistSystem != null)
            {
                var emission = _mistSystem.emission;
                emission.rateOverTime = 5f * mistDensity * globalIntensity;
            }
        }
        
        /// <summary>
        /// Toggle all particles on/off
        /// </summary>
        public void ToggleParticles()
        {
            enableParticles = !enableParticles;
            
            if (!enableParticles)
            {
                _sparkleSystem?.Stop();
                _mistSystem?.Stop();
                _rainSystem?.Stop();
                _snowSystem?.Stop();
                _firefliesSystem?.Stop();
                
                foreach (var orb in _activeOrbs)
                {
                    if (orb.GameObject != null)
                        orb.GameObject.SetActive(false);
                }
                
                foreach (var trail in _activeTrails)
                {
                    if (trail.GameObject != null)
                        trail.GameObject.SetActive(false);
                }
            }
            else
            {
                _sparkleSystem?.Play();
                _mistSystem?.Play();
                SetWeather(currentWeather);
                
                foreach (var orb in _activeOrbs)
                {
                    if (orb.GameObject != null)
                        orb.GameObject.SetActive(true);
                }
                
                foreach (var trail in _activeTrails)
                {
                    if (trail.GameObject != null)
                        trail.GameObject.SetActive(true);
                }
            }
        }
        
        /// <summary>
        /// Get current weather
        /// </summary>
        public WeatherEffect GetCurrentWeather() => currentWeather;
        
        #endregion
    }
    
    #region Data Types
    
    public enum WeatherEffect
    {
        Clear,
        Rain,
        Snow,
        Fireflies,
        Mystical
    }
    
    public enum TerritoryOwnership
    {
        Neutral,
        Owned,
        Enemy,
        Alliance
    }
    
    internal class MagicOrb
    {
        public Vector3 Position;
        public float Size;
        public Color Color;
        public float Speed;
        public float Phase;
        public float WanderAngle;
        public GameObject GameObject;
    }
    
    internal class MagicTrail
    {
        public Vector3 Position;
        public Vector3 Target;
        public float Speed;
        public Color Color;
        public GameObject GameObject;
    }
    
    #endregion
}
