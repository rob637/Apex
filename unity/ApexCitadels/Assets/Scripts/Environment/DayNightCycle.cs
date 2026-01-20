// ============================================================================
// APEX CITADELS - AAA DAY/NIGHT CYCLE
// Unified day/night system combining the best features from all implementations.
// This is the SINGLE source of truth for time-of-day in the game.
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.Environment
{
    /// <summary>
    /// Time periods of the day for gameplay and visual changes.
    /// </summary>
    public enum TimePeriod
    {
        Night,
        Dawn,
        Day,
        Dusk
    }
    
    /// <summary>
    /// AAA Day/Night Cycle Manager - Central time-of-day control for Apex Citadels.
    /// 
    /// Features:
    /// - Sun with intensity curve, color gradient, realistic rotation
    /// - Moon with separate light tracking opposite path
    /// - Procedural star field with visibility curves
    /// - Advanced fog with density curves
    /// - Skybox shader integration
    /// - Real-time sync, game-time, or override modes
    /// - Period events (sunrise, sunset, period changes)
    /// - FantasyAtmosphere and weather integration
    /// - Update throttling for performance
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        #region Singleton
        
        private static DayNightCycle _instance;
        public static DayNightCycle Instance => _instance;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Time Settings")]
        [SerializeField] private bool enableCycle = true;
        [SerializeField] private TimeMode timeMode = TimeMode.RealTime;
        [SerializeField, Range(0f, 24f)] private float currentTime = 12f;
        [SerializeField] private float dayLengthMinutes = 24f; // Real minutes for a full day
        [SerializeField] private float timeScale = 1f;
        [SerializeField] private float updateInterval = 0.5f; // Performance throttling
        
        [Header("Override")]
        [SerializeField] private bool useOverrideTime = false;
        [SerializeField, Range(0f, 24f)] private float overrideHour = 12f;
        
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
        [SerializeField] private int starCount = 500;
        
        [Header("Integration")]
        [SerializeField] private bool syncAtmosphere = true;
        [SerializeField] private bool syncParticles = true;
        [SerializeField] private bool syncVisualEnhancements = true;
        
        #endregion
        
        #region Time Thresholds
        
        public static readonly float DAWN_START = 5f;
        public static readonly float DAWN_END = 7f;
        public static readonly float DUSK_START = 17f;
        public static readonly float DUSK_END = 19f;
        
        #endregion
        
        #region State
        
        private float _starAlpha;
        private TimePeriod _currentPeriod;
        private float _lastUpdateTime;
        private float _gameTimeHours;
        private bool _wasDaytime;
        
        // Integration references (using MonoBehaviour to avoid PC assembly dependency)
        private MonoBehaviour _atmosphere;
        private MonoBehaviour _particles;
        
        #endregion
        
        #region Events
        
        public event Action<TimePeriod> OnPeriodChanged;
        public event Action<float> OnTimeChanged;
        public event Action OnSunrise;
        public event Action OnSunset;
        
        #endregion
        
        #region Properties
        
        /// <summary>Current time in 24-hour format (0-24).</summary>
        public float CurrentHour => currentTime;
        
        /// <summary>Normalized time (0-1, where 0=midnight, 0.5=noon).</summary>
        public float NormalizedTime => currentTime / 24f;
        
        /// <summary>True if between dawn end and dusk start.</summary>
        public bool IsDay => currentTime >= DAWN_END && currentTime < DUSK_START;
        
        /// <summary>True if between dusk end and dawn start.</summary>
        public bool IsNight => currentTime >= DUSK_END || currentTime < DAWN_START;
        
        /// <summary>Current time period.</summary>
        public TimePeriod CurrentPeriod => _currentPeriod;
        
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
            
            InitializeGradients();
        }
        
        private void Start()
        {
            SetupLights();
            SetupStars();
            
            // Find integration targets by name (to avoid cross-assembly dependencies)
            _atmosphere = FindFirstObjectByTypeName("FantasyAtmosphere");
            _particles = FindFirstObjectByTypeName("MapMagicParticles");
            
            // Initialize time
            if (timeMode == TimeMode.RealTime)
            {
                currentTime = (float)DateTime.Now.TimeOfDay.TotalHours;
            }
            _gameTimeHours = currentTime;
            _wasDaytime = IsDay;
            
            // Apply initial state
            ApplyTimeOfDay(currentTime);
            
            ApexLogger.Log($"[DayNightCycle] Started at {currentTime:F1}h ({GetTimePeriod(currentTime)})", ApexLogger.LogCategory.Map);
        }
        
        private void Update()
        {
            if (!enableCycle) return;
            
            // Performance throttling
            if (Time.time - _lastUpdateTime < updateInterval) return;
            _lastUpdateTime = Time.time;
            
            UpdateTime();
            ApplyTimeOfDay(currentTime);
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region Time Update
        
        private void UpdateTime()
        {
            float previousHour = currentTime;
            bool wasDaytime = IsDay;
            
            if (useOverrideTime)
            {
                currentTime = overrideHour;
            }
            else
            {
                switch (timeMode)
                {
                    case TimeMode.RealTime:
                        currentTime = (float)DateTime.Now.TimeOfDay.TotalHours;
                        break;
                        
                    case TimeMode.GameTime:
                        float hoursPerSecond = 24f / (dayLengthMinutes * 60f) * timeScale;
                        _gameTimeHours += hoursPerSecond * updateInterval;
                        if (_gameTimeHours >= 24f) _gameTimeHours -= 24f;
                        currentTime = _gameTimeHours;
                        break;
                        
                    case TimeMode.Paused:
                        // Don't update time
                        break;
                }
            }
            
            // Check for sunrise/sunset events
            if (!wasDaytime && IsDay)
            {
                OnSunrise?.Invoke();
                ApexLogger.Log("[DayNightCycle] Sunrise!", ApexLogger.LogCategory.Map);
            }
            if (wasDaytime && !IsDay && currentTime >= DUSK_START)
            {
                OnSunset?.Invoke();
                ApexLogger.Log("[DayNightCycle] Sunset!", ApexLogger.LogCategory.Map);
            }
            
            OnTimeChanged?.Invoke(currentTime);
        }
        
        #endregion
        
        #region Apply Time of Day
        
        private void ApplyTimeOfDay(float time)
        {
            float normalizedTime = time / 24f;
            
            // Check for period change
            TimePeriod newPeriod = GetTimePeriod(time);
            if (newPeriod != _currentPeriod)
            {
                var oldPeriod = _currentPeriod;
                _currentPeriod = newPeriod;
                OnPeriodChanged?.Invoke(_currentPeriod);
                OnPeriodChange(oldPeriod, newPeriod);
            }
            
            // ===== Sun =====
            if (sunLight != null)
            {
                float sunIntensity = sunIntensityCurve.Evaluate(time);
                Color sunColor = sunColorGradient.Evaluate(normalizedTime);
                
                sunLight.intensity = Mathf.Lerp(sunIntensityNight, sunIntensityDay, sunIntensity);
                sunLight.color = sunColor;
                
                // Rotate sun (rises in east, sets in west)
                float sunAngle = (time - 6f) / 12f * 180f;
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
            }
            
            // ===== Moon =====
            if (moonLight != null)
            {
                float moonVisibility = 1f - sunIntensityCurve.Evaluate(time);
                moonLight.intensity = moonIntensity * moonVisibility;
                
                float moonAngle = (time + 6f) / 12f * 180f;
                moonLight.transform.rotation = Quaternion.Euler(moonAngle, 150f, 0f);
            }
            
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
            if (starSystem != null)
            {
                float starTarget = starVisibilityCurve.Evaluate(time);
                _starAlpha = Mathf.MoveTowards(_starAlpha, starTarget, updateInterval * starFadeSpeed);
                
                var main = starSystem.main;
                Color starColor = new Color(1f, 1f, 0.95f, _starAlpha);
                main.startColor = starColor;
            }
            
            // ===== Integrations =====
            ApplyIntegrations(time, normalizedTime);
        }
        
        private void ApplyIntegrations(float time, float normalizedTime)
        {
            // FantasyAtmosphere - use reflection to avoid cross-assembly dependency
            if (syncAtmosphere && _atmosphere != null)
            {
                InvokeMethod(_atmosphere, "SetTimeOfDay", time);
            }
            
            // MapMagicParticles - disabled due to cross-assembly dependency
            // If needed, implement via SendMessage or reflection
            
            // VisualEnhancements
            if (syncVisualEnhancements)
            {
                var visualEnhancements = FindFirstObjectByTypeName("VisualEnhancements");
                if (visualEnhancements != null)
                {
                    InvokeMethod(visualEnhancements, "UpdateSkyColors", normalizedTime);
                }
            }
        }
        
        private void OnPeriodChange(TimePeriod oldPeriod, TimePeriod newPeriod)
        {
            ApexLogger.Log($"[DayNightCycle] Period: {oldPeriod} -> {newPeriod}", ApexLogger.LogCategory.Map);
            
            // Switch atmosphere preset based on period (use reflection to avoid enum dependency)
            if (syncAtmosphere && _atmosphere != null)
            {
                string presetName = newPeriod switch
                {
                    TimePeriod.Night => "EnchantedNight",
                    TimePeriod.Dawn => "GoldenHour",
                    TimePeriod.Day => "MagicalDaylight",
                    TimePeriod.Dusk => "MysticalTwilight",
                    _ => "MagicalDaylight"
                };
                InvokeMethodByPresetName(_atmosphere, "ApplyPreset", presetName);
            }
        }
        
        /// <summary>
        /// Find MonoBehaviour by type name to avoid cross-assembly dependencies.
        /// </summary>
        private MonoBehaviour FindFirstObjectByTypeName(string typeName)
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb.GetType().Name == typeName)
                    return mb;
            }
            return null;
        }
        
        /// <summary>
        /// Invoke a method on an object using reflection.
        /// </summary>
        private void InvokeMethod(MonoBehaviour target, string methodName, params object[] args)
        {
            if (target == null) return;
            var method = target.GetType().GetMethod(methodName);
            method?.Invoke(target, args);
        }
        
        /// <summary>
        /// Invoke ApplyPreset with enum conversion.
        /// </summary>
        private void InvokeMethodByPresetName(MonoBehaviour target, string methodName, string presetName)
        {
            if (target == null) return;
            var method = target.GetType().GetMethod(methodName);
            if (method == null) return;
            
            var paramType = method.GetParameters()[0].ParameterType;
            if (paramType.IsEnum)
            {
                var enumValue = System.Enum.Parse(paramType, presetName);
                method.Invoke(target, new[] { enumValue });
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeGradients()
        {
            // Sun color gradient
            if (sunColorGradient == null || sunColorGradient.colorKeys.Length == 0)
            {
                sunColorGradient = new Gradient();
                sunColorGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 0f),
                        new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.25f),
                        new GradientColorKey(new Color(1f, 0.98f, 0.9f), 0.35f),
                        new GradientColorKey(new Color(1f, 1f, 0.95f), 0.5f),
                        new GradientColorKey(new Color(1f, 0.98f, 0.9f), 0.65f),
                        new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.75f),
                        new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 1f)
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }
            
            // Sun intensity curve
            if (sunIntensityCurve == null || sunIntensityCurve.keys.Length == 0)
            {
                sunIntensityCurve = new AnimationCurve(
                    new Keyframe(0, 0),
                    new Keyframe(5, 0),
                    new Keyframe(7, 0.8f),
                    new Keyframe(12, 1),
                    new Keyframe(17, 0.8f),
                    new Keyframe(19, 0),
                    new Keyframe(24, 0)
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
                        new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0f),
                        new GradientColorKey(new Color(0.3f, 0.25f, 0.35f), 0.25f),
                        new GradientColorKey(new Color(0.5f, 0.5f, 0.55f), 0.35f),
                        new GradientColorKey(new Color(0.6f, 0.6f, 0.65f), 0.5f),
                        new GradientColorKey(new Color(0.5f, 0.5f, 0.55f), 0.65f),
                        new GradientColorKey(new Color(0.35f, 0.25f, 0.3f), 0.75f),
                        new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 1f)
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
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f),
                        new GradientColorKey(new Color(0.7f, 0.5f, 0.4f), 0.25f),
                        new GradientColorKey(new Color(0.8f, 0.85f, 0.9f), 0.5f),
                        new GradientColorKey(new Color(0.8f, 0.6f, 0.5f), 0.75f),
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1f)
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }
            
            // Fog density curve
            if (fogDensityCurve == null || fogDensityCurve.keys.Length == 0)
            {
                fogDensityCurve = new AnimationCurve(
                    new Keyframe(0, 0.5f),
                    new Keyframe(5, 0.8f),
                    new Keyframe(8, 0.3f),
                    new Keyframe(12, 0.2f),
                    new Keyframe(16, 0.3f),
                    new Keyframe(19, 0.7f),
                    new Keyframe(24, 0.5f)
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
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f),
                        new GradientColorKey(new Color(0.6f, 0.4f, 0.5f), 0.25f),
                        new GradientColorKey(new Color(0.5f, 0.7f, 1f), 0.5f),
                        new GradientColorKey(new Color(0.7f, 0.5f, 0.4f), 0.75f),
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1f)
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
                        new GradientColorKey(new Color(0.15f, 0.15f, 0.25f), 0f),
                        new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.25f),
                        new GradientColorKey(new Color(0.9f, 0.95f, 1f), 0.5f),
                        new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.75f),
                        new GradientColorKey(new Color(0.15f, 0.15f, 0.25f), 1f)
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }
            
            // Star visibility curve
            if (starVisibilityCurve == null || starVisibilityCurve.keys.Length == 0)
            {
                starVisibilityCurve = new AnimationCurve(
                    new Keyframe(0, 1),
                    new Keyframe(5, 1),
                    new Keyframe(6, 0),
                    new Keyframe(18, 0),
                    new Keyframe(19, 0),
                    new Keyframe(20, 1),
                    new Keyframe(24, 1)
                );
                starVisibilityCurve.preWrapMode = WrapMode.Loop;
                starVisibilityCurve.postWrapMode = WrapMode.Loop;
            }
            
            // Atmosphere thickness curve
            if (atmosphereThicknessCurve == null || atmosphereThicknessCurve.keys.Length == 0)
            {
                atmosphereThicknessCurve = new AnimationCurve(
                    new Keyframe(0, 0.5f),
                    new Keyframe(6, 1.5f),
                    new Keyframe(12, 0.8f),
                    new Keyframe(18, 1.5f),
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
            main.maxParticles = starCount;
            
            var emission = starSystem.emission;
            emission.enabled = false;
            
            var shape = starSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 1000f;
            shape.rotation = new Vector3(0, 0, 0);
            
            // Emit all stars at once
            for (int i = 0; i < starCount; i++)
            {
                Vector3 dir = UnityEngine.Random.onUnitSphere;
                if (dir.y < 0.1f) dir.y = Mathf.Abs(dir.y) + 0.1f;
                
                var emitParams = new ParticleSystem.EmitParams
                {
                    position = dir.normalized * 900f,
                    startSize = UnityEngine.Random.Range(0.5f, 2f),
                    startColor = new Color(1f, 1f, UnityEngine.Random.Range(0.9f, 1f), 1f)
                };
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
            mat.SetFloat("_SurfaceType", 1);
            mat.EnableKeyword("_ALPHABLEND_ON");
            return mat;
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>Get current time (0-24).</summary>
        public float GetCurrentTime() => currentTime;
        
        /// <summary>Set time of day (0-24).</summary>
        public void SetTime(float time)
        {
            currentTime = Mathf.Repeat(time, 24f);
            _gameTimeHours = currentTime;
            ApplyTimeOfDay(currentTime);
        }
        
        /// <summary>Set time with override mode (for testing/debugging).</summary>
        public void SetOverrideTime(float hour)
        {
            useOverrideTime = true;
            overrideHour = Mathf.Clamp(hour, 0f, 24f);
            currentTime = overrideHour;
            ApplyTimeOfDay(currentTime);
        }
        
        /// <summary>Jump to specific period.</summary>
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
        
        /// <summary>Get time period for a given time.</summary>
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
        
        /// <summary>Get current period.</summary>
        public TimePeriod GetCurrentPeriod() => _currentPeriod;
        
        /// <summary>Set time scale (1 = normal, 2 = double speed).</summary>
        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Max(0f, scale);
        }
        
        /// <summary>Pause/resume time cycle.</summary>
        public void SetCycleEnabled(bool enabled)
        {
            enableCycle = enabled;
        }
        
        /// <summary>Set time mode.</summary>
        public void SetTimeMode(TimeMode mode)
        {
            timeMode = mode;
            useOverrideTime = false;
        }
        
        /// <summary>Resume real-time sync.</summary>
        public void ResumeRealTime()
        {
            useOverrideTime = false;
            timeMode = TimeMode.RealTime;
        }
        
        /// <summary>Start accelerated game-time.</summary>
        public void StartGameTime(float hoursPerSecond = 1f)
        {
            useOverrideTime = false;
            timeMode = TimeMode.GameTime;
            timeScale = hoursPerSecond;
            _gameTimeHours = currentTime;
        }
        
        /// <summary>Get formatted time string (e.g., "12:30 PM").</summary>
        public string GetFormattedTime()
        {
            int hours = (int)currentTime;
            int minutes = (int)((currentTime - hours) * 60);
            string period = hours >= 12 ? "PM" : "AM";
            int displayHours = hours % 12;
            if (displayHours == 0) displayHours = 12;
            return $"{displayHours}:{minutes:D2} {period}";
        }
        
        /// <summary>Check if it's daytime.</summary>
        public bool IsDaytime() => IsDay;
        
        /// <summary>Check if it's nighttime.</summary>
        public bool IsNighttime() => IsNight;
        
        #endregion
    }
    
    /// <summary>
    /// Time update mode for the day/night cycle.
    /// </summary>
    public enum TimeMode
    {
        RealTime,   // Sync with system clock
        GameTime,   // Accelerated in-game time
        Paused      // Time doesn't advance
    }
}
