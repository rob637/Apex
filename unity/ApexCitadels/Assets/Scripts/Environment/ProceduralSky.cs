// ============================================================================
// APEX CITADELS - PROCEDURAL SKY SYSTEM
// Dynamic atmospheric sky that responds to time and weather
// ============================================================================
using System;
using UnityEngine;
using UnityEngine.Rendering;
using ApexCitadels.Core;

namespace ApexCitadels.Environment
{
    /// <summary>
    /// Procedural sky rendering system that creates dynamic, responsive atmospheres.
    /// Integrates with DayNightCycle and WeatherSystem for seamless visual coherence.
    /// 
    /// Features:
    /// - Rayleigh/Mie scattering simulation
    /// - Dynamic cloud layers
    /// - Sun/moon positioning
    /// - Weather-reactive atmosphere
    /// - Star field with fade
    /// - Aurora for special conditions
    /// </summary>
    public class ProceduralSky : MonoBehaviour
    {
        #region Singleton
        
        private static ProceduralSky _instance;
        public static ProceduralSky Instance => _instance;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Sky Colors")]
        [SerializeField] private Gradient daySkyGradient;
        [SerializeField] private Gradient sunsetSkyGradient;
        [SerializeField] private Gradient nightSkyGradient;
        [SerializeField] private Color zenithColorDay = new Color(0.2f, 0.4f, 0.8f);
        [SerializeField] private Color horizonColorDay = new Color(0.6f, 0.8f, 1f);
        [SerializeField] private Color zenithColorNight = new Color(0.02f, 0.02f, 0.08f);
        [SerializeField] private Color horizonColorNight = new Color(0.05f, 0.08f, 0.15f);
        
        [Header("Sun")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Color sunColorDay = new Color(1f, 0.95f, 0.85f);
        [SerializeField] private Color sunColorSunset = new Color(1f, 0.5f, 0.2f);
        [SerializeField] private Color sunColorSunrise = new Color(1f, 0.6f, 0.4f);
        [SerializeField] private float sunIntensityDay = 1.2f;
        [SerializeField] private float sunIntensityNight = 0f;
        [SerializeField] private AnimationCurve sunIntensityCurve;
        
        [Header("Moon")]
        [SerializeField] private Light moonLight;
        [SerializeField] private Color moonColor = new Color(0.7f, 0.8f, 0.9f);
        [SerializeField] private float moonIntensity = 0.15f;
        
        [Header("Atmosphere")]
        [SerializeField, Range(0, 2)] private float atmosphereThickness = 1f;
        [SerializeField, Range(0, 1)] private float exposure = 1f;
        [SerializeField] private Color groundColor = new Color(0.3f, 0.35f, 0.4f);
        
        [Header("Clouds")]
        [SerializeField] private bool enableClouds = true;
        [SerializeField] private Color cloudColorDay = Color.white;
        [SerializeField] private Color cloudColorNight = new Color(0.2f, 0.2f, 0.25f);
        [SerializeField] private float cloudSpeed = 0.005f;
        [SerializeField, Range(0, 1)] private float cloudCoverage = 0.3f;
        [SerializeField, Range(0, 1)] private float cloudDensity = 0.5f;
        
        [Header("Stars")]
        [SerializeField] private bool enableStars = true;
        [SerializeField] private int starCount = 500;
        [SerializeField] private AnimationCurve starVisibilityCurve;
        [SerializeField] private float starTwinkleSpeed = 2f;
        
        [Header("Fog Integration")]
        [SerializeField] private bool controlFog = true;
        [SerializeField] private float fogDensityDay = 0.001f;
        [SerializeField] private float fogDensityNight = 0.002f;
        [SerializeField] private AnimationCurve fogDensityCurve;
        
        [Header("Ambient Light")]
        [SerializeField] private bool controlAmbient = true;
        [SerializeField] private Color ambientColorDay = new Color(0.5f, 0.55f, 0.6f);
        [SerializeField] private Color ambientColorNight = new Color(0.1f, 0.1f, 0.15f);
        
        #endregion
        
        #region State
        
        private Material _skyMaterial;
        private ParticleSystem _starSystem;
        private float _cloudOffset;
        private float _currentTime = 0.5f; // 0-1 (0.5 = noon)
        
        // Cached references
        private DayNightCycle _dayNightCycle;
        private WeatherSystem _weatherSystem;
        
        // Computed values
        private Color _currentZenithColor;
        private Color _currentHorizonColor;
        private Color _currentSunColor;
        private float _currentSunIntensity;
        private float _currentCloudCoverage;
        private float _currentVisibility;
        
        #endregion
        
        #region Events
        
        public event Action OnSkyUpdated;
        
        #endregion
        
        #region Properties
        
        /// <summary>Current zenith (top of sky) color.</summary>
        public Color ZenithColor => _currentZenithColor;
        
        /// <summary>Current horizon color.</summary>
        public Color HorizonColor => _currentHorizonColor;
        
        /// <summary>Current sun light color.</summary>
        public Color SunColor => _currentSunColor;
        
        /// <summary>Current sun intensity.</summary>
        public float SunIntensity => _currentSunIntensity;
        
        /// <summary>Current effective cloud coverage (weather + base).</summary>
        public float EffectiveCloudCoverage => _currentCloudCoverage;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeDefaults();
        }
        
        private void Start()
        {
            // Get references
            _dayNightCycle = DayNightCycle.Instance;
            _weatherSystem = WeatherSystem.Instance;
            
            // Create sky
            CreateSkyMaterial();
            CreateSunLight();
            CreateMoonLight();
            
            if (enableStars)
            {
                CreateStarField();
            }
            
            // Initial update
            UpdateSky();
            
            // Subscribe to events
            if (_dayNightCycle != null)
            {
                _dayNightCycle.OnTimeChanged += OnTimeChanged;
            }
            if (_weatherSystem != null)
            {
                _weatherSystem.OnWeatherChanged += OnWeatherChanged;
            }
            
            ApexLogger.Log("[ProceduralSky] Initialized", ApexLogger.LogCategory.Map);
        }
        
        private void Update()
        {
            // Get time from DayNightCycle
            if (_dayNightCycle != null)
            {
                _currentTime = _dayNightCycle.NormalizedTime;
            }
            
            // Update cloud offset
            if (enableClouds)
            {
                float windSpeed = _weatherSystem?.CurrentWeather.WindSpeed ?? 0.2f;
                _cloudOffset += Time.deltaTime * cloudSpeed * (1f + windSpeed * 2f);
            }
            
            UpdateSky();
        }
        
        private void OnDestroy()
        {
            if (_dayNightCycle != null)
            {
                _dayNightCycle.OnTimeChanged -= OnTimeChanged;
            }
            if (_weatherSystem != null)
            {
                _weatherSystem.OnWeatherChanged -= OnWeatherChanged;
            }
            
            if (_instance == this)
                _instance = null;
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeDefaults()
        {
            // Initialize sun intensity curve
            if (sunIntensityCurve == null || sunIntensityCurve.length == 0)
            {
                sunIntensityCurve = new AnimationCurve(
                    new Keyframe(0f, 0f),      // Midnight
                    new Keyframe(0.2f, 0f),    // 4:48 AM
                    new Keyframe(0.25f, 0.3f), // Sunrise
                    new Keyframe(0.35f, 1f),   // Morning
                    new Keyframe(0.5f, 1f),    // Noon
                    new Keyframe(0.65f, 1f),   // Afternoon
                    new Keyframe(0.75f, 0.3f), // Sunset
                    new Keyframe(0.8f, 0f),    // 7:12 PM
                    new Keyframe(1f, 0f)       // Midnight
                );
            }
            
            // Initialize star visibility curve
            if (starVisibilityCurve == null || starVisibilityCurve.length == 0)
            {
                starVisibilityCurve = new AnimationCurve(
                    new Keyframe(0f, 1f),      // Midnight - full stars
                    new Keyframe(0.2f, 1f),    // Early morning
                    new Keyframe(0.25f, 0.3f), // Dawn
                    new Keyframe(0.3f, 0f),    // Day
                    new Keyframe(0.7f, 0f),    // Day
                    new Keyframe(0.75f, 0.3f), // Dusk
                    new Keyframe(0.8f, 1f),    // Evening
                    new Keyframe(1f, 1f)       // Midnight
                );
            }
            
            // Initialize fog density curve
            if (fogDensityCurve == null || fogDensityCurve.length == 0)
            {
                fogDensityCurve = new AnimationCurve(
                    new Keyframe(0f, 1f),      // Night
                    new Keyframe(0.25f, 0.8f), // Dawn
                    new Keyframe(0.5f, 0.5f),  // Noon
                    new Keyframe(0.75f, 0.8f), // Dusk
                    new Keyframe(1f, 1f)       // Night
                );
            }
        }
        
        private void CreateSkyMaterial()
        {
            // Try to use existing procedural skybox or create gradient
            Shader skyShader = Shader.Find("Skybox/Procedural");
            
            if (skyShader != null)
            {
                _skyMaterial = new Material(skyShader);
                _skyMaterial.SetFloat("_SunSize", 0.04f);
                _skyMaterial.SetFloat("_SunSizeConvergence", 5f);
                _skyMaterial.SetFloat("_AtmosphereThickness", atmosphereThickness);
                _skyMaterial.SetFloat("_Exposure", exposure);
                _skyMaterial.SetColor("_GroundColor", groundColor);
            }
            else
            {
                // Fallback to gradient skybox
                skyShader = Shader.Find("Skybox/Cubemap");
                if (skyShader != null)
                {
                    _skyMaterial = new Material(skyShader);
                }
                else
                {
                    ApexLogger.LogWarning("[ProceduralSky] No suitable skybox shader found", ApexLogger.LogCategory.Map);
                    return;
                }
            }
            
            RenderSettings.skybox = _skyMaterial;
        }
        
        private void CreateSunLight()
        {
            if (sunLight != null) return;
            
            // Find existing directional light or create new
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional && light.gameObject.name.Contains("Sun"))
                {
                    sunLight = light;
                    return;
                }
            }
            
            // Create sun
            GameObject sunObj = new GameObject("Sun");
            sunObj.transform.parent = transform;
            sunLight = sunObj.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = sunColorDay;
            sunLight.intensity = sunIntensityDay;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.8f;
            sunLight.shadowBias = 0.05f;
            sunLight.shadowNormalBias = 0.4f;
        }
        
        private void CreateMoonLight()
        {
            if (moonLight != null) return;
            
            // Create moon
            GameObject moonObj = new GameObject("Moon");
            moonObj.transform.parent = transform;
            moonLight = moonObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = moonColor;
            moonLight.intensity = 0f;
            moonLight.shadows = LightShadows.Soft;
            moonLight.shadowStrength = 0.3f;
        }
        
        private void CreateStarField()
        {
            if (_starSystem != null) return;
            
            GameObject starsObj = new GameObject("Stars");
            starsObj.transform.parent = transform;
            starsObj.transform.localPosition = Vector3.zero;
            
            _starSystem = starsObj.AddComponent<ParticleSystem>();
            var main = _starSystem.main;
            main.maxParticles = starCount;
            main.startLifetime = Mathf.Infinity;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.5f),
                new Color(0.9f, 0.95f, 1f, 0.8f)
            );
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = true;
            main.loop = true;
            
            var emission = _starSystem.emission;
            emission.enabled = false;
            
            var shape = _starSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 500f;
            
            // Emit all stars at once
            _starSystem.Emit(starCount);
            
            // Add twinkle with noise
            var noise = _starSystem.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = starTwinkleSpeed;
            noise.sizeAmount = 0.5f;
            
            // Renderer settings
            var renderer = starsObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.SetColor("_Color", Color.white);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
        
        #endregion
        
        #region Sky Update
        
        private void UpdateSky()
        {
            // Get weather influence
            float weatherCloudCoverage = _weatherSystem?.CurrentWeather.CloudCoverage ?? 0f;
            float weatherVisibility = _weatherSystem?.CurrentWeather.Visibility ?? 1f;
            
            _currentCloudCoverage = Mathf.Max(cloudCoverage, weatherCloudCoverage);
            _currentVisibility = weatherVisibility;
            
            // Update components
            UpdateSkyColors();
            UpdateSunPosition();
            UpdateMoonPosition();
            UpdateStars();
            UpdateFog();
            UpdateAmbient();
            UpdateSkyMaterial();
            
            OnSkyUpdated?.Invoke();
        }
        
        private void UpdateSkyColors()
        {
            // Determine time-of-day colors
            bool isDawn = _currentTime >= 0.2f && _currentTime < 0.3f;
            bool isDusk = _currentTime >= 0.7f && _currentTime < 0.8f;
            bool isDay = _currentTime >= 0.25f && _currentTime < 0.75f;
            
            // Calculate base colors from time
            if (isDawn)
            {
                float t = (_currentTime - 0.2f) / 0.1f;
                _currentZenithColor = Color.Lerp(zenithColorNight, zenithColorDay, t);
                _currentHorizonColor = Color.Lerp(horizonColorNight, sunColorSunrise, t * 0.5f);
                _currentSunColor = Color.Lerp(sunColorSunrise, sunColorDay, t);
            }
            else if (isDusk)
            {
                float t = (_currentTime - 0.7f) / 0.1f;
                _currentZenithColor = Color.Lerp(zenithColorDay, zenithColorNight, t);
                _currentHorizonColor = Color.Lerp(sunColorSunset, horizonColorNight, t * 0.5f);
                _currentSunColor = Color.Lerp(sunColorDay, sunColorSunset, t);
            }
            else if (isDay)
            {
                _currentZenithColor = zenithColorDay;
                _currentHorizonColor = horizonColorDay;
                _currentSunColor = sunColorDay;
            }
            else
            {
                _currentZenithColor = zenithColorNight;
                _currentHorizonColor = horizonColorNight;
                _currentSunColor = moonColor;
            }
            
            // Apply weather influence
            if (_currentCloudCoverage > 0.5f)
            {
                // Overcast sky - desaturate and darken
                float overcastFactor = (_currentCloudCoverage - 0.5f) * 2f;
                Color overcastTint = new Color(0.6f, 0.65f, 0.7f);
                _currentZenithColor = Color.Lerp(_currentZenithColor, overcastTint, overcastFactor * 0.5f);
                _currentHorizonColor = Color.Lerp(_currentHorizonColor, overcastTint, overcastFactor * 0.3f);
            }
            
            // Apply visibility reduction
            if (_currentVisibility < 1f)
            {
                Color fogColor = RenderSettings.fogColor;
                float fogInfluence = 1f - _currentVisibility;
                _currentZenithColor = Color.Lerp(_currentZenithColor, fogColor, fogInfluence * 0.3f);
                _currentHorizonColor = Color.Lerp(_currentHorizonColor, fogColor, fogInfluence * 0.5f);
            }
        }
        
        private void UpdateSunPosition()
        {
            if (sunLight == null) return;
            
            // Calculate sun position based on time (0 = midnight, 0.5 = noon)
            // Sun rises at 0.25 (6 AM) and sets at 0.75 (6 PM)
            float sunAngle = (_currentTime - 0.25f) * 360f; // 0° at sunrise, 180° at sunset
            
            // Sun rotation (rises in east, arcs south, sets in west)
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
            
            // Calculate intensity
            float baseIntensity = sunIntensityCurve.Evaluate(_currentTime);
            
            // Reduce intensity based on cloud coverage
            float cloudReduction = 1f - (_currentCloudCoverage * 0.7f);
            
            _currentSunIntensity = baseIntensity * sunIntensityDay * cloudReduction;
            
            // Apply to light
            sunLight.intensity = _currentSunIntensity;
            sunLight.color = _currentSunColor;
            
            // Adjust shadow strength based on clouds
            sunLight.shadowStrength = Mathf.Lerp(0.3f, 0.8f, cloudReduction);
        }
        
        private void UpdateMoonPosition()
        {
            if (moonLight == null) return;
            
            // Moon is opposite the sun
            float moonAngle = ((_currentTime + 0.5f) % 1f - 0.25f) * 360f;
            moonLight.transform.rotation = Quaternion.Euler(moonAngle, 150f, 0f);
            
            // Moon visibility (inverse of sun)
            float moonVisibility = 1f - sunIntensityCurve.Evaluate(_currentTime);
            float cloudReduction = 1f - (_currentCloudCoverage * 0.9f);
            
            moonLight.intensity = moonIntensity * moonVisibility * cloudReduction;
            moonLight.color = moonColor;
        }
        
        private void UpdateStars()
        {
            if (_starSystem == null) return;
            
            float starVisibility = starVisibilityCurve.Evaluate(_currentTime);
            
            // Reduce stars with clouds
            starVisibility *= (1f - _currentCloudCoverage);
            
            // Update particle alpha
            var main = _starSystem.main;
            Color startColor = main.startColor.color;
            startColor.a = starVisibility;
            main.startColor = startColor;
        }
        
        private void UpdateFog()
        {
            if (!controlFog) return;
            
            // Base fog from time
            float baseDensity = Mathf.Lerp(fogDensityDay, fogDensityNight, fogDensityCurve.Evaluate(_currentTime));
            
            // Weather influence
            float visibilityFactor = 1f / Mathf.Max(0.1f, _currentVisibility);
            float finalDensity = baseDensity * visibilityFactor;
            
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = finalDensity;
            
            // Fog color blends between sky colors
            Color fogColor = Color.Lerp(_currentHorizonColor, _currentZenithColor, 0.3f);
            RenderSettings.fogColor = fogColor;
        }
        
        private void UpdateAmbient()
        {
            if (!controlAmbient) return;
            
            // Base ambient from time
            bool isDay = _currentTime >= 0.25f && _currentTime < 0.75f;
            Color baseAmbient = isDay ? ambientColorDay : ambientColorNight;
            
            // Transition during dawn/dusk
            if (_currentTime >= 0.2f && _currentTime < 0.3f)
            {
                float t = (_currentTime - 0.2f) / 0.1f;
                baseAmbient = Color.Lerp(ambientColorNight, ambientColorDay, t);
            }
            else if (_currentTime >= 0.7f && _currentTime < 0.8f)
            {
                float t = (_currentTime - 0.7f) / 0.1f;
                baseAmbient = Color.Lerp(ambientColorDay, ambientColorNight, t);
            }
            
            // Weather influence - darker with heavy clouds
            float cloudDarkening = 1f - (_currentCloudCoverage * 0.4f);
            baseAmbient *= cloudDarkening;
            
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = baseAmbient;
        }
        
        private void UpdateSkyMaterial()
        {
            if (_skyMaterial == null) return;
            
            // Update procedural skybox parameters
            if (_skyMaterial.HasProperty("_AtmosphereThickness"))
            {
                // Thicker atmosphere at sunrise/sunset for red tones
                float timeFromNoon = Mathf.Abs(_currentTime - 0.5f) * 2f;
                float thicknessBoost = 1f + timeFromNoon * 0.5f;
                _skyMaterial.SetFloat("_AtmosphereThickness", atmosphereThickness * thicknessBoost);
            }
            
            if (_skyMaterial.HasProperty("_Exposure"))
            {
                // Reduce exposure with cloud coverage
                float weatherExposure = exposure * (1f - _currentCloudCoverage * 0.3f);
                _skyMaterial.SetFloat("_Exposure", weatherExposure);
            }
            
            if (_skyMaterial.HasProperty("_SkyTint"))
            {
                _skyMaterial.SetColor("_SkyTint", _currentZenithColor);
            }
            
            if (_skyMaterial.HasProperty("_GroundColor"))
            {
                _skyMaterial.SetColor("_GroundColor", _currentHorizonColor);
            }
            
            // Trigger reflection probe update
            DynamicGI.UpdateEnvironment();
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnTimeChanged(float time)
        {
            // Time is in hours, convert to normalized
            _currentTime = time / 24f;
        }
        
        private void OnWeatherChanged(WeatherState weather)
        {
            // Weather system will update _currentCloudCoverage through normal update
            ApexLogger.Log($"[ProceduralSky] Weather changed to {weather.Type}", ApexLogger.LogCategory.Map);
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Force update the sky immediately.
        /// </summary>
        public void ForceUpdate()
        {
            UpdateSky();
        }
        
        /// <summary>
        /// Set time directly (0-1, 0.5 = noon).
        /// </summary>
        public void SetTime(float normalizedTime)
        {
            _currentTime = Mathf.Clamp01(normalizedTime);
            UpdateSky();
        }
        
        /// <summary>
        /// Override cloud coverage (for testing/cinematics).
        /// </summary>
        public void SetCloudCoverage(float coverage)
        {
            cloudCoverage = Mathf.Clamp01(coverage);
        }
        
        /// <summary>
        /// Get the current sky color for UI elements.
        /// </summary>
        public Color GetSkyColorForUI()
        {
            return Color.Lerp(_currentHorizonColor, _currentZenithColor, 0.5f);
        }
        
        #endregion
    }
}
