using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ApexCitadels.Core;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Day/Night cycle manager for the map view.
    /// Controls lighting, atmosphere, and ambient effects based on time of day.
    /// Integrates with FantasyAtmosphere for post-processing and MapMagicParticles for effects.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Time Settings")]
        [SerializeField] private bool enableCycle = true;
        [SerializeField] private bool useRealTime = false;
        [SerializeField, Range(0f, 24f)] private float currentTime = 12f;
        [SerializeField] private float dayLengthMinutes = 24f; // Real minutes for a full day
        [SerializeField] private float timeScale = 1f;
        
        [Header("Sun")]
        [SerializeField] private Light sunLight;
        [SerializeField] private float sunIntensityDay = 1.2f;
        [SerializeField] private float sunIntensityNight = 0.05f;
        [SerializeField] private Gradient sunColorGradient;
        [SerializeField] private AnimationCurve sunIntensityCurve;
        
        [Header("Moon")]
        [SerializeField] private Light moonLight;
        [SerializeField] private float moonIntensity = 0.15f;
        [SerializeField] private Color moonColor = new Color(0.7f, 0.8f, 1f);
        
        [Header("Ambient")]
        [SerializeField] private Gradient ambientColorGradient;
        [SerializeField] private Gradient fogColorGradient;
        [SerializeField] private AnimationCurve fogDensityCurve;
        [SerializeField] private float fogDensityMultiplier = 0.01f;
        
        [Header("Skybox")]
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private Gradient skyTintGradient;
        [SerializeField] private Gradient horizonGradient;
        [SerializeField] private AnimationCurve atmosphereThicknessCurve;
        
        [Header("Stars")]
        [SerializeField] private ParticleSystem starSystem;
        [SerializeField] private AnimationCurve starVisibilityCurve;
        [SerializeField] private float starFadeSpeed = 2f;
        
        [Header("Integration")]
        [SerializeField] private bool syncAtmosphere = true;
        [SerializeField] private bool syncParticles = true;
        
        // Time periods
        public static readonly float DAWN_START = 5f;
        public static readonly float DAWN_END = 7f;
        public static readonly float DUSK_START = 17f;
        public static readonly float DUSK_END = 19f;
        
        // Singleton
        private static DayNightCycle _instance;
        public static DayNightCycle Instance => _instance;
        
        // Runtime state
        private float _starAlpha;
        private TimePeriod _currentPeriod;
        private FantasyAtmosphere _atmosphere;
        private MapMagicParticles _particles;
        
        // Events
        public event System.Action<TimePeriod> OnPeriodChanged;
        public event System.Action<float> OnTimeChanged;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeGradients();
        }
        
        private void Start()
        {
            SetupLights();
            SetupStars();
            
            // Find integration targets
            _atmosphere = FindFirstObjectByType<FantasyAtmosphere>();
            _particles = FindFirstObjectByType<MapMagicParticles>();
            
            // Apply initial time
            ApplyTimeOfDay(currentTime);
            
            ApexLogger.Log($"Started at {currentTime:F1}h ({GetTimePeriod(currentTime)})", ApexLogger.LogCategory.Map);
        }
        
        private void Update()
        {
            if (!enableCycle) return;
            
            if (useRealTime)
            {
                // Use actual system time
                currentTime = System.DateTime.Now.Hour + System.DateTime.Now.Minute / 60f;
            }
            else
            {
                // Advance time based on day length setting
                float hoursPerSecond = 24f / (dayLengthMinutes * 60f) * timeScale;
                currentTime += hoursPerSecond * Time.deltaTime;
                
                if (currentTime >= 24f)
                {
                    currentTime -= 24f;
                }
            }
            
            ApplyTimeOfDay(currentTime);
        }
        
        private void InitializeGradients()
        {
            // Sun color gradient (warmer at dawn/dusk)
            if (sunColorGradient == null || sunColorGradient.colorKeys.Length == 0)
            {
                sunColorGradient = new Gradient();
                sunColorGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 0f),       // Midnight
                        new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.25f),      // Dawn
                        new GradientColorKey(new Color(1f, 0.98f, 0.9f), 0.35f),     // Morning
                        new GradientColorKey(new Color(1f, 1f, 0.95f), 0.5f),        // Noon
                        new GradientColorKey(new Color(1f, 0.98f, 0.9f), 0.65f),     // Afternoon
                        new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.75f),      // Dusk
                        new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 1f)        // Night
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }
            
            // Sun intensity curve
            if (sunIntensityCurve == null || sunIntensityCurve.keys.Length == 0)
            {
                sunIntensityCurve = new AnimationCurve(
                    new Keyframe(0, 0),        // Midnight
                    new Keyframe(5, 0),        // Pre-dawn
                    new Keyframe(7, 0.8f),     // Morning
                    new Keyframe(12, 1),       // Noon
                    new Keyframe(17, 0.8f),    // Afternoon
                    new Keyframe(19, 0),       // Post-dusk
                    new Keyframe(24, 0)        // Midnight
                );
                sunIntensityCurve.preWrapMode = WrapMode.Loop;
                sunIntensityCurve.postWrapMode = WrapMode.Loop;
            }
            
            // Ambient color gradient
            if (ambientColorGradient == null || ambientColorGradient.colorKeys.Length == 0)
            {
                ambientColorGradient = new Gradient();
                ambientColorGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0f),    // Midnight
                        new GradientColorKey(new Color(0.3f, 0.25f, 0.35f), 0.25f),  // Dawn
                        new GradientColorKey(new Color(0.5f, 0.5f, 0.55f), 0.35f),   // Morning
                        new GradientColorKey(new Color(0.6f, 0.6f, 0.65f), 0.5f),    // Noon
                        new GradientColorKey(new Color(0.5f, 0.5f, 0.55f), 0.65f),   // Afternoon
                        new GradientColorKey(new Color(0.35f, 0.25f, 0.3f), 0.75f),  // Dusk
                        new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 1f)     // Night
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }
            
            // Fog color gradient
            if (fogColorGradient == null || fogColorGradient.colorKeys.Length == 0)
            {
                fogColorGradient = new Gradient();
                fogColorGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f),       // Midnight
                        new GradientColorKey(new Color(0.7f, 0.5f, 0.4f), 0.25f),    // Dawn
                        new GradientColorKey(new Color(0.8f, 0.85f, 0.9f), 0.5f),    // Day
                        new GradientColorKey(new Color(0.8f, 0.6f, 0.5f), 0.75f),    // Dusk
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1f)        // Night
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }
            
            // Fog density curve (heavier at dawn/dusk)
            if (fogDensityCurve == null || fogDensityCurve.keys.Length == 0)
            {
                fogDensityCurve = new AnimationCurve(
                    new Keyframe(0, 0.5f),     // Midnight
                    new Keyframe(5, 0.8f),     // Pre-dawn (foggy)
                    new Keyframe(8, 0.3f),     // Morning
                    new Keyframe(12, 0.2f),    // Noon (clearest)
                    new Keyframe(16, 0.3f),    // Afternoon
                    new Keyframe(19, 0.7f),    // Dusk (foggy)
                    new Keyframe(24, 0.5f)     // Midnight
                );
                fogDensityCurve.preWrapMode = WrapMode.Loop;
                fogDensityCurve.postWrapMode = WrapMode.Loop;
            }
            
            // Sky tint gradient
            if (skyTintGradient == null || skyTintGradient.colorKeys.Length == 0)
            {
                skyTintGradient = new Gradient();
                skyTintGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f),       // Night
                        new GradientColorKey(new Color(0.6f, 0.4f, 0.5f), 0.25f),    // Dawn
                        new GradientColorKey(new Color(0.5f, 0.7f, 1f), 0.5f),       // Day
                        new GradientColorKey(new Color(0.7f, 0.5f, 0.4f), 0.75f),    // Dusk
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1f)        // Night
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }
            
            // Horizon gradient
            if (horizonGradient == null || horizonGradient.colorKeys.Length == 0)
            {
                horizonGradient = new Gradient();
                horizonGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.15f, 0.15f, 0.25f), 0f),    // Night
                        new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.25f),      // Dawn
                        new GradientColorKey(new Color(0.9f, 0.95f, 1f), 0.5f),      // Day
                        new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.75f),      // Dusk
                        new GradientColorKey(new Color(0.15f, 0.15f, 0.25f), 1f)     // Night
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }
            
            // Star visibility curve
            if (starVisibilityCurve == null || starVisibilityCurve.keys.Length == 0)
            {
                starVisibilityCurve = new AnimationCurve(
                    new Keyframe(0, 1),        // Midnight (visible)
                    new Keyframe(5, 1),        // Pre-dawn
                    new Keyframe(6, 0),        // Dawn (fade out)
                    new Keyframe(18, 0),       // Day (invisible)
                    new Keyframe(19, 0),       // Dusk
                    new Keyframe(20, 1),       // Night (fade in)
                    new Keyframe(24, 1)        // Midnight
                );
                starVisibilityCurve.preWrapMode = WrapMode.Loop;
                starVisibilityCurve.postWrapMode = WrapMode.Loop;
            }
            
            // Atmosphere thickness curve
            if (atmosphereThicknessCurve == null || atmosphereThicknessCurve.keys.Length == 0)
            {
                atmosphereThicknessCurve = new AnimationCurve(
                    new Keyframe(0, 0.5f),
                    new Keyframe(6, 1.5f),     // Dawn - thick atmosphere
                    new Keyframe(12, 0.8f),    // Noon
                    new Keyframe(18, 1.5f),    // Dusk - thick atmosphere
                    new Keyframe(24, 0.5f)
                );
                atmosphereThicknessCurve.preWrapMode = WrapMode.Loop;
                atmosphereThicknessCurve.postWrapMode = WrapMode.Loop;
            }
        }
        
        private void SetupLights()
        {
            // Create or find sun light
            if (sunLight == null)
            {
                var sun = RenderSettings.sun;
                if (sun == null)
                {
                    var sunObj = new GameObject("Sun");
                    sunObj.transform.SetParent(transform);
                    sun = sunObj.AddComponent<Light>();
                    sun.type = LightType.Directional;
                    RenderSettings.sun = sun;
                }
                sunLight = sun;
            }
            
            sunLight.type = LightType.Directional;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.8f;
            
            // Create or find moon light
            if (moonLight == null)
            {
                var moonObj = new GameObject("Moon");
                moonObj.transform.SetParent(transform);
                moonLight = moonObj.AddComponent<Light>();
                moonLight.type = LightType.Directional;
                moonLight.color = moonColor;
                moonLight.intensity = 0f;
                moonLight.shadows = LightShadows.Soft;
                moonLight.shadowStrength = 0.3f;
            }
        }
        
        private void SetupStars()
        {
            if (starSystem != null) return;
            
            var starsObj = new GameObject("Stars");
            starsObj.transform.SetParent(transform);
            starsObj.transform.localPosition = Vector3.zero;
            
            starSystem = starsObj.AddComponent<ParticleSystem>();
            
            var main = starSystem.main;
            main.loop = true;
            main.startLifetime = Mathf.Infinity;
            main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            main.startColor = new Color(1f, 1f, 0.95f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 500;
            
            var emission = starSystem.emission;
            emission.enabled = false; // We emit manually
            
            var shape = starSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 1000f;
            shape.rotation = new Vector3(0, 0, 0);
            
            // Emit all stars at once
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            for (int i = 0; i < 500; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                if (dir.y < 0.1f) dir.y = Mathf.Abs(dir.y) + 0.1f; // Keep above horizon
                
                emitParams.position = dir.normalized * 900f;
                emitParams.startSize = Random.Range(0.5f, 2f);
                emitParams.startColor = new Color(1f, 1f, Random.Range(0.9f, 1f), 1f);
                
                starSystem.Emit(emitParams, 1);
            }
            
            var renderer = starsObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateStarMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
        
        private Material CreateStarMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            
            Material mat = new Material(shader);
            mat.SetFloat("_SurfaceType", 1); // Transparent
            mat.EnableKeyword("_ALPHABLEND_ON");
            return mat;
        }
        
        private void ApplyTimeOfDay(float time)
        {
            float normalizedTime = time / 24f;
            
            // Check for period change
            TimePeriod newPeriod = GetTimePeriod(time);
            if (newPeriod != _currentPeriod)
            {
                _currentPeriod = newPeriod;
                OnPeriodChanged?.Invoke(_currentPeriod);
                OnPeriodChange(_currentPeriod);
            }
            
            OnTimeChanged?.Invoke(time);
            
            // ===== Sun =====
            float sunIntensity = sunIntensityCurve.Evaluate(time);
            Color sunColor = sunColorGradient.Evaluate(normalizedTime);
            
            sunLight.intensity = Mathf.Lerp(sunIntensityNight, sunIntensityDay, sunIntensity);
            sunLight.color = sunColor;
            
            // Rotate sun (rises in east, sets in west)
            float sunAngle = (time - 6f) / 12f * 180f; // 6am = 0°, 12pm = 90°, 6pm = 180°
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
            
            // ===== Moon =====
            float moonVisibility = 1f - sunIntensity;
            moonLight.intensity = moonIntensity * moonVisibility;
            
            // Moon follows opposite path
            float moonAngle = (time + 6f) / 12f * 180f;
            moonLight.transform.rotation = Quaternion.Euler(moonAngle, 150f, 0f);
            
            // ===== Ambient =====
            Color ambientColor = ambientColorGradient.Evaluate(normalizedTime);
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            
            // ===== Fog =====
            Color fogColor = fogColorGradient.Evaluate(normalizedTime);
            float fogDensity = fogDensityCurve.Evaluate(time) * fogDensityMultiplier;
            
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
            
            // ===== Skybox =====
            if (skyboxMaterial != null)
            {
                Color skyTint = skyTintGradient.Evaluate(normalizedTime);
                Color horizon = horizonGradient.Evaluate(normalizedTime);
                float thickness = atmosphereThicknessCurve.Evaluate(time);
                
                skyboxMaterial.SetColor("_SkyTint", skyTint);
                skyboxMaterial.SetColor("_GroundColor", horizon);
                skyboxMaterial.SetFloat("_AtmosphereThickness", thickness);
            }
            
            // ===== Stars =====
            float starTarget = starVisibilityCurve.Evaluate(time);
            _starAlpha = Mathf.MoveTowards(_starAlpha, starTarget, Time.deltaTime * starFadeSpeed);
            
            if (starSystem != null)
            {
                var main = starSystem.main;
                Color starColor = new Color(1f, 1f, 0.95f, _starAlpha);
                main.startColor = starColor;
            }
            
            // ===== Integration =====
            if (syncAtmosphere && _atmosphere != null)
            {
                _atmosphere.SetTimeOfDay(time);
                
                // Switch atmosphere preset based on period
                if (newPeriod != _currentPeriod)
                {
                    var preset = newPeriod switch
                    {
                        TimePeriod.Night => AtmospherePreset.EnchantedNight,
                        TimePeriod.Dawn => AtmospherePreset.GoldenHour,
                        TimePeriod.Day => AtmospherePreset.MagicalDaylight,
                        TimePeriod.Dusk => AtmospherePreset.MysticalTwilight,
                        _ => AtmospherePreset.MagicalDaylight
                    };
                    _atmosphere.ApplyPreset(preset);
                }
            }
            
            if (syncParticles && _particles != null)
            {
                // Enable fireflies at night
                if (_currentPeriod == TimePeriod.Night || _currentPeriod == TimePeriod.Dusk)
                {
                    if (_particles.GetCurrentWeather() == WeatherEffect.Clear)
                    {
                        _particles.SetWeather(WeatherEffect.Fireflies);
                    }
                }
                else if (_particles.GetCurrentWeather() == WeatherEffect.Fireflies)
                {
                    _particles.SetWeather(WeatherEffect.Clear);
                }
            }
        }
        
        private void OnPeriodChange(TimePeriod period)
        {
            ApexLogger.Log($"Period changed to: {period}", ApexLogger.LogCategory.Map);
            
            // Trigger any period-specific effects
            switch (period)
            {
                case TimePeriod.Dawn:
                    // Morning effects - bird sounds, etc.
                    break;
                    
                case TimePeriod.Day:
                    // Full daylight
                    break;
                    
                case TimePeriod.Dusk:
                    // Evening effects
                    break;
                    
                case TimePeriod.Night:
                    // Night effects - owls, etc.
                    break;
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Get current time (0-24)
        /// </summary>
        public float GetCurrentTime() => currentTime;
        
        /// <summary>
        /// Set time of day (0-24)
        /// </summary>
        public void SetTime(float time)
        {
            currentTime = Mathf.Repeat(time, 24f);
            ApplyTimeOfDay(currentTime);
        }
        
        /// <summary>
        /// Jump to specific period
        /// </summary>
        public void JumpToPeriod(TimePeriod period)
        {
            float targetTime = period switch
            {
                TimePeriod.Dawn => 6f,
                TimePeriod.Day => 12f,
                TimePeriod.Dusk => 18f,
                TimePeriod.Night => 0f,
                _ => 12f
            };
            SetTime(targetTime);
        }
        
        /// <summary>
        /// Get time period for a given time
        /// </summary>
        public TimePeriod GetTimePeriod(float time)
        {
            if (time >= DAWN_START && time < DAWN_END)
                return TimePeriod.Dawn;
            if (time >= DAWN_END && time < DUSK_START)
                return TimePeriod.Day;
            if (time >= DUSK_START && time < DUSK_END)
                return TimePeriod.Dusk;
            return TimePeriod.Night;
        }
        
        /// <summary>
        /// Get current period
        /// </summary>
        public TimePeriod GetCurrentPeriod() => _currentPeriod;
        
        /// <summary>
        /// Set time scale (1 = normal, 2 = double speed)
        /// </summary>
        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Max(0f, scale);
        }
        
        /// <summary>
        /// Pause/resume time cycle
        /// </summary>
        public void SetCycleEnabled(bool enabled)
        {
            enableCycle = enabled;
        }
        
        /// <summary>
        /// Toggle real-time mode
        /// </summary>
        public void SetRealTimeMode(bool useReal)
        {
            useRealTime = useReal;
        }
        
        /// <summary>
        /// Get formatted time string (e.g., "12:30 PM")
        /// </summary>
        public string GetFormattedTime()
        {
            int hours = (int)currentTime;
            int minutes = (int)((currentTime - hours) * 60);
            string period = hours >= 12 ? "PM" : "AM";
            int displayHours = hours % 12;
            if (displayHours == 0) displayHours = 12;
            return $"{displayHours}:{minutes:D2} {period}";
        }
        
        /// <summary>
        /// Check if it's daytime
        /// </summary>
        public bool IsDaytime() => currentTime >= DAWN_END && currentTime < DUSK_START;
        
        /// <summary>
        /// Check if it's nighttime
        /// </summary>
        public bool IsNighttime() => currentTime >= DUSK_END || currentTime < DAWN_START;
        
        #endregion
    }
    
    public enum TimePeriod
    {
        Night,
        Dawn,
        Day,
        Dusk
    }
}
