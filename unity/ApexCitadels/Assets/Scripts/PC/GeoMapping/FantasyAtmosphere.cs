using Camera = UnityEngine.Camera;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Post-processing volume controller for fantasy map atmosphere.
    /// Adds magical glow, color grading, bloom, and vignette effects
    /// that adapt to the current fantasy map style.
    /// </summary>
    public class FantasyAtmosphere : MonoBehaviour
    {
        [Header("Volume Reference")]
        [SerializeField] private Volume postProcessVolume;
        
        [Header("Atmosphere Presets")]
        [SerializeField] private AtmospherePreset currentPreset = AtmospherePreset.MagicalDaylight;
        
        [Header("Color Grading")]
        [SerializeField, Range(-1f, 1f)] private float temperature = 0.1f;
        [SerializeField, Range(-1f, 1f)] private float tint = 0f;
        [SerializeField, Range(-100f, 100f)] private float saturation = 10f;
        [SerializeField, Range(-100f, 100f)] private float contrast = 5f;
        [SerializeField] private Color colorFilter = Color.white;
        
        [Header("Bloom")]
        [SerializeField] private bool enableBloom = true;
        [SerializeField, Range(0f, 5f)] private float bloomIntensity = 0.8f;
        [SerializeField, Range(0f, 1f)] private float bloomThreshold = 0.9f;
        [SerializeField, Range(1f, 10f)] private float bloomScatter = 5f;
        [SerializeField] private Color bloomTint = new Color(1f, 0.95f, 0.9f);
        
        [Header("Vignette")]
        [SerializeField] private bool enableVignette = true;
        [SerializeField, Range(0f, 1f)] private float vignetteIntensity = 0.35f;
        [SerializeField, Range(0f, 1f)] private float vignetteSmoothness = 0.4f;
        [SerializeField] private Color vignetteColor = new Color(0.2f, 0.15f, 0.1f);
        
        [Header("Ambient Occlusion")]
        [SerializeField] private bool enableAO = true;
        [SerializeField, Range(0f, 4f)] private float aoIntensity = 0.5f;
        
        [Header("Motion Blur")]
        [SerializeField] private bool enableMotionBlur = false;
        [SerializeField, Range(0f, 1f)] private float motionBlurIntensity = 0.2f;
        
        [Header("Film Grain")]
        [SerializeField] private bool enableFilmGrain = true;
        [SerializeField, Range(0f, 1f)] private float filmGrainIntensity = 0.15f;
        
        [Header("Chromatic Aberration")]
        [SerializeField] private bool enableChromaticAberration = false;
        [SerializeField, Range(0f, 1f)] private float chromaticIntensity = 0.1f;
        
        [Header("Lens Distortion")]
        [SerializeField] private bool enableLensDistortion = false;
        [SerializeField, Range(-1f, 1f)] private float lensDistortionIntensity = -0.1f;
        
        [Header("Day/Night Transition")]
        [SerializeField] private bool enableDayNightCycle = true;
        [SerializeField, Range(0f, 24f)] private float timeOfDay = 12f;
        [SerializeField] private AnimationCurve dayNightCurve;
        [SerializeField] private Gradient skyColorGradient;
        
        // Volume profile effects
        private ColorAdjustments _colorAdjustments;
        private Bloom _bloom;
        private Vignette _vignette;
        private FilmGrain _filmGrain;
        private ChromaticAberration _chromaticAberration;
        private LensDistortion _lensDistortion;
        
        // Singleton
        private static FantasyAtmosphere _instance;
        public static FantasyAtmosphere Instance => _instance;
        
        // Runtime state
        private float _transitionTime;
        private AtmospherePreset _targetPreset;
        private bool _isTransitioning;
        
        // Cached preset values for transitions
        private AtmosphereSettings _currentSettings;
        private AtmosphereSettings _targetSettings;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeDefaultCurves();
        }
        
        private void Start()
        {
            SetupPostProcessVolume();
            ApplyPreset(currentPreset, immediate: true);
        }
        
        private void Update()
        {
            if (_isTransitioning)
            {
                UpdateTransition();
            }
            
            if (enableDayNightCycle)
            {
                UpdateDayNightCycle();
            }
        }
        
        private void InitializeDefaultCurves()
        {
            if (dayNightCurve == null || dayNightCurve.keys.Length == 0)
            {
                dayNightCurve = new AnimationCurve(
                    new Keyframe(0, 0),      // Midnight - dark
                    new Keyframe(6, 0.3f),   // Dawn
                    new Keyframe(12, 1),     // Noon - bright
                    new Keyframe(18, 0.5f),  // Dusk
                    new Keyframe(24, 0)      // Midnight
                );
            }
            
            if (skyColorGradient == null || skyColorGradient.colorKeys.Length == 0)
            {
                skyColorGradient = new Gradient();
                skyColorGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0f),      // Midnight
                        new GradientColorKey(new Color(0.6f, 0.4f, 0.3f), 0.25f),      // Dawn
                        new GradientColorKey(new Color(0.9f, 0.95f, 1f), 0.5f),        // Noon
                        new GradientColorKey(new Color(0.8f, 0.5f, 0.3f), 0.75f),      // Dusk
                        new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 1f)       // Midnight
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                );
            }
        }
        
        private void SetupPostProcessVolume()
        {
            // Find or create post process volume
            if (postProcessVolume == null)
            {
                postProcessVolume = FindFirstObjectByType<Volume>();
                
                if (postProcessVolume == null)
                {
                    var volumeObj = new GameObject("FantasyAtmosphereVolume");
                    volumeObj.transform.SetParent(transform);
                    postProcessVolume = volumeObj.AddComponent<Volume>();
                    postProcessVolume.isGlobal = true;
                    postProcessVolume.priority = 100;
                }
            }
            
            // Create volume profile if needed
            if (postProcessVolume.profile == null)
            {
                postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            }
            
            var profile = postProcessVolume.profile;
            
            // Setup Color Adjustments
            if (!profile.TryGet(out _colorAdjustments))
            {
                _colorAdjustments = profile.Add<ColorAdjustments>(true);
            }
            
            // Setup Bloom
            if (!profile.TryGet(out _bloom))
            {
                _bloom = profile.Add<Bloom>(true);
            }
            
            // Setup Vignette
            if (!profile.TryGet(out _vignette))
            {
                _vignette = profile.Add<Vignette>(true);
            }
            
            // Setup Film Grain
            if (!profile.TryGet(out _filmGrain))
            {
                _filmGrain = profile.Add<FilmGrain>(true);
            }
            
            // Setup Chromatic Aberration
            if (!profile.TryGet(out _chromaticAberration))
            {
                _chromaticAberration = profile.Add<ChromaticAberration>(true);
            }
            
            // Setup Lens Distortion
            if (!profile.TryGet(out _lensDistortion))
            {
                _lensDistortion = profile.Add<LensDistortion>(true);
            }
            
            Debug.Log("[FantasyAtmosphere] Post-processing volume configured");
        }
        
        #region Preset Application
        
        /// <summary>
        /// Apply atmosphere preset
        /// </summary>
        public void ApplyPreset(AtmospherePreset preset, bool immediate = false)
        {
            _targetPreset = preset;
            _targetSettings = GetSettingsForPreset(preset);
            
            if (immediate)
            {
                _currentSettings = _targetSettings;
                ApplySettings(_currentSettings);
                currentPreset = preset;
            }
            else
            {
                if (_currentSettings == null)
                {
                    _currentSettings = GetSettingsForPreset(currentPreset);
                }
                _isTransitioning = true;
                _transitionTime = 0f;
            }
        }
        
        private void UpdateTransition()
        {
            _transitionTime += Time.deltaTime / 2f; // 2 second transition
            
            if (_transitionTime >= 1f)
            {
                _transitionTime = 1f;
                _isTransitioning = false;
                currentPreset = _targetPreset;
            }
            
            // Lerp between settings
            var interpolated = LerpSettings(_currentSettings, _targetSettings, _transitionTime);
            ApplySettings(interpolated);
            
            if (!_isTransitioning)
            {
                _currentSettings = _targetSettings;
            }
        }
        
        private AtmosphereSettings GetSettingsForPreset(AtmospherePreset preset)
        {
            return preset switch
            {
                AtmospherePreset.MagicalDaylight => new AtmosphereSettings
                {
                    Temperature = 0.1f,
                    Tint = 0f,
                    Saturation = 15f,
                    Contrast = 5f,
                    ColorFilter = new Color(1f, 0.98f, 0.95f),
                    BloomIntensity = 0.8f,
                    BloomThreshold = 0.9f,
                    BloomTint = new Color(1f, 0.95f, 0.9f),
                    VignetteIntensity = 0.3f,
                    VignetteColor = new Color(0.2f, 0.18f, 0.15f),
                    FilmGrainIntensity = 0.1f
                },
                
                AtmospherePreset.GoldenHour => new AtmosphereSettings
                {
                    Temperature = 0.4f,
                    Tint = 0.1f,
                    Saturation = 20f,
                    Contrast = 10f,
                    ColorFilter = new Color(1f, 0.9f, 0.75f),
                    BloomIntensity = 1.2f,
                    BloomThreshold = 0.85f,
                    BloomTint = new Color(1f, 0.85f, 0.6f),
                    VignetteIntensity = 0.4f,
                    VignetteColor = new Color(0.3f, 0.15f, 0.1f),
                    FilmGrainIntensity = 0.15f
                },
                
                AtmospherePreset.MysticalTwilight => new AtmosphereSettings
                {
                    Temperature = -0.2f,
                    Tint = 0.1f,
                    Saturation = 5f,
                    Contrast = 8f,
                    ColorFilter = new Color(0.85f, 0.88f, 1f),
                    BloomIntensity = 1.0f,
                    BloomThreshold = 0.8f,
                    BloomTint = new Color(0.7f, 0.8f, 1f),
                    VignetteIntensity = 0.5f,
                    VignetteColor = new Color(0.1f, 0.1f, 0.2f),
                    FilmGrainIntensity = 0.2f
                },
                
                AtmospherePreset.DarkFantasy => new AtmosphereSettings
                {
                    Temperature = -0.1f,
                    Tint = 0f,
                    Saturation = -10f,
                    Contrast = 15f,
                    ColorFilter = new Color(0.8f, 0.78f, 0.85f),
                    BloomIntensity = 0.6f,
                    BloomThreshold = 0.95f,
                    BloomTint = new Color(0.6f, 0.5f, 0.7f),
                    VignetteIntensity = 0.6f,
                    VignetteColor = new Color(0.05f, 0.05f, 0.1f),
                    FilmGrainIntensity = 0.25f
                },
                
                AtmospherePreset.EnchantedNight => new AtmosphereSettings
                {
                    Temperature = -0.3f,
                    Tint = 0.15f,
                    Saturation = -5f,
                    Contrast = 5f,
                    ColorFilter = new Color(0.6f, 0.65f, 0.9f),
                    BloomIntensity = 1.5f,
                    BloomThreshold = 0.75f,
                    BloomTint = new Color(0.5f, 0.6f, 1f),
                    VignetteIntensity = 0.55f,
                    VignetteColor = new Color(0.02f, 0.02f, 0.08f),
                    FilmGrainIntensity = 0.18f
                },
                
                AtmospherePreset.AncientRealm => new AtmosphereSettings
                {
                    Temperature = 0.2f,
                    Tint = 0.05f,
                    Saturation = -15f,
                    Contrast = 8f,
                    ColorFilter = new Color(0.95f, 0.9f, 0.8f),
                    BloomIntensity = 0.5f,
                    BloomThreshold = 0.92f,
                    BloomTint = new Color(1f, 0.95f, 0.85f),
                    VignetteIntensity = 0.5f,
                    VignetteColor = new Color(0.25f, 0.2f, 0.15f),
                    FilmGrainIntensity = 0.3f
                },
                
                AtmospherePreset.CombatIntense => new AtmosphereSettings
                {
                    Temperature = 0.15f,
                    Tint = -0.05f,
                    Saturation = 25f,
                    Contrast = 20f,
                    ColorFilter = new Color(1f, 0.95f, 0.9f),
                    BloomIntensity = 1.3f,
                    BloomThreshold = 0.8f,
                    BloomTint = new Color(1f, 0.8f, 0.6f),
                    VignetteIntensity = 0.45f,
                    VignetteColor = new Color(0.3f, 0.1f, 0.1f),
                    FilmGrainIntensity = 0.12f
                },
                
                _ => new AtmosphereSettings
                {
                    Temperature = 0f,
                    Saturation = 0f,
                    Contrast = 0f,
                    ColorFilter = Color.white,
                    BloomIntensity = 0.5f,
                    BloomThreshold = 0.9f,
                    BloomTint = Color.white,
                    VignetteIntensity = 0.2f,
                    VignetteColor = Color.black,
                    FilmGrainIntensity = 0.1f
                }
            };
        }
        
        private AtmosphereSettings LerpSettings(AtmosphereSettings a, AtmosphereSettings b, float t)
        {
            return new AtmosphereSettings
            {
                Temperature = Mathf.Lerp(a.Temperature, b.Temperature, t),
                Tint = Mathf.Lerp(a.Tint, b.Tint, t),
                Saturation = Mathf.Lerp(a.Saturation, b.Saturation, t),
                Contrast = Mathf.Lerp(a.Contrast, b.Contrast, t),
                ColorFilter = Color.Lerp(a.ColorFilter, b.ColorFilter, t),
                BloomIntensity = Mathf.Lerp(a.BloomIntensity, b.BloomIntensity, t),
                BloomThreshold = Mathf.Lerp(a.BloomThreshold, b.BloomThreshold, t),
                BloomTint = Color.Lerp(a.BloomTint, b.BloomTint, t),
                VignetteIntensity = Mathf.Lerp(a.VignetteIntensity, b.VignetteIntensity, t),
                VignetteColor = Color.Lerp(a.VignetteColor, b.VignetteColor, t),
                FilmGrainIntensity = Mathf.Lerp(a.FilmGrainIntensity, b.FilmGrainIntensity, t)
            };
        }
        
        private void ApplySettings(AtmosphereSettings settings)
        {
            if (_colorAdjustments != null)
            {
                _colorAdjustments.colorFilter.Override(settings.ColorFilter);
                _colorAdjustments.postExposure.Override(0f);
                _colorAdjustments.contrast.Override(settings.Contrast);
                _colorAdjustments.saturation.Override(settings.Saturation);
            }
            
            if (_bloom != null && enableBloom)
            {
                _bloom.active = true;
                _bloom.intensity.Override(settings.BloomIntensity);
                _bloom.threshold.Override(settings.BloomThreshold);
                _bloom.tint.Override(settings.BloomTint);
                _bloom.scatter.Override(bloomScatter);
            }
            
            if (_vignette != null && enableVignette)
            {
                _vignette.active = true;
                _vignette.intensity.Override(settings.VignetteIntensity);
                _vignette.smoothness.Override(vignetteSmoothness);
                _vignette.color.Override(settings.VignetteColor);
            }
            
            if (_filmGrain != null && enableFilmGrain)
            {
                _filmGrain.active = true;
                _filmGrain.intensity.Override(settings.FilmGrainIntensity);
                _filmGrain.type.Override(FilmGrainLookup.Medium1);
            }
            
            if (_chromaticAberration != null)
            {
                _chromaticAberration.active = enableChromaticAberration;
                _chromaticAberration.intensity.Override(chromaticIntensity);
            }
            
            if (_lensDistortion != null)
            {
                _lensDistortion.active = enableLensDistortion;
                _lensDistortion.intensity.Override(lensDistortionIntensity);
            }
        }
        
        #endregion
        
        #region Day/Night Cycle
        
        private void UpdateDayNightCycle()
        {
            // Get light intensity from curve
            float normalizedTime = timeOfDay / 24f;
            float lightIntensity = dayNightCurve.Evaluate(timeOfDay);
            
            // Get sky color
            Color skyColor = skyColorGradient.Evaluate(normalizedTime);
            
            // Apply to rendering
            if (_colorAdjustments != null)
            {
                // Adjust exposure based on time
                float exposure = Mathf.Lerp(-1f, 0.5f, lightIntensity);
                _colorAdjustments.postExposure.Override(exposure);
                
                // Subtle color shift
                Color timeFilter = Color.Lerp(_currentSettings?.ColorFilter ?? Color.white, skyColor, 0.3f);
                _colorAdjustments.colorFilter.Override(timeFilter);
            }
            
            // Update vignette intensity (darker at night)
            if (_vignette != null)
            {
                float nightVignette = Mathf.Lerp(0.6f, 0.3f, lightIntensity);
                _vignette.intensity.Override(nightVignette);
            }
            
            // Update main directional light if exists
            var sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.intensity = Mathf.Lerp(0.1f, 1.2f, lightIntensity);
                sun.color = skyColor;
                
                // Rotate sun based on time
                float sunAngle = (timeOfDay - 6f) / 12f * 180f; // 6am = horizon, 12pm = zenith
                sun.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
            }
        }
        
        /// <summary>
        /// Set time of day (0-24)
        /// </summary>
        public void SetTimeOfDay(float time)
        {
            timeOfDay = Mathf.Repeat(time, 24f);
        }
        
        /// <summary>
        /// Advance time by hours
        /// </summary>
        public void AdvanceTime(float hours)
        {
            SetTimeOfDay(timeOfDay + hours);
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Set bloom intensity
        /// </summary>
        public void SetBloomIntensity(float intensity)
        {
            bloomIntensity = Mathf.Clamp(intensity, 0f, 5f);
            if (_bloom != null) _bloom.intensity.Override(bloomIntensity);
        }
        
        /// <summary>
        /// Set vignette intensity
        /// </summary>
        public void SetVignetteIntensity(float intensity)
        {
            vignetteIntensity = Mathf.Clamp01(intensity);
            if (_vignette != null) _vignette.intensity.Override(vignetteIntensity);
        }
        
        /// <summary>
        /// Toggle film grain
        /// </summary>
        public void ToggleFilmGrain()
        {
            enableFilmGrain = !enableFilmGrain;
            if (_filmGrain != null) _filmGrain.active = enableFilmGrain;
        }
        
        /// <summary>
        /// Get current preset
        /// </summary>
        public AtmospherePreset GetCurrentPreset() => currentPreset;
        
        /// <summary>
        /// Trigger combat atmosphere (temporarily intensifies effects)
        /// </summary>
        public void TriggerCombatAtmosphere()
        {
            ApplyPreset(AtmospherePreset.CombatIntense);
        }
        
        /// <summary>
        /// Return to normal atmosphere
        /// </summary>
        public void RestoreNormalAtmosphere()
        {
            ApplyPreset(AtmospherePreset.MagicalDaylight);
        }
        
        #endregion
    }
    
    #region Data Types
    
    /// <summary>
    /// Atmosphere visual presets
    /// </summary>
    public enum AtmospherePreset
    {
        MagicalDaylight,    // Bright fantasy daytime
        GoldenHour,         // Warm sunset colors
        MysticalTwilight,   // Purple/blue dusk
        DarkFantasy,        // Desaturated, high contrast
        EnchantedNight,     // Magical night with glows
        AncientRealm,       // Sepia/aged look
        CombatIntense       // High saturation for battles
    }
    
    /// <summary>
    /// Internal settings container for transitions
    /// </summary>
    public class AtmosphereSettings
    {
        public float Temperature;
        public float Tint;
        public float Saturation;
        public float Contrast;
        public Color ColorFilter;
        public float BloomIntensity;
        public float BloomThreshold;
        public Color BloomTint;
        public float VignetteIntensity;
        public Color VignetteColor;
        public float FilmGrainIntensity;
    }
    
    #endregion
}
