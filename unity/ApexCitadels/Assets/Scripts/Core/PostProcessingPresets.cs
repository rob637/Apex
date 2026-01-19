using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_PIPELINE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace ApexCitadels.Core
{
    /// <summary>
    /// Post-processing presets manager for scene-specific effects.
    /// Provides presets for menu, map, combat, and editor scenes.
    /// 
    /// Usage:
    ///   PostProcessingPresets.ApplyPreset(ScenePreset.Combat);
    ///   PostProcessingPresets.TransitionTo(ScenePreset.Map, 1.5f);
    ///   PostProcessingPresets.SetIntensity(0.8f);
    /// </summary>
    public class PostProcessingPresets : MonoBehaviour
    {
        public static PostProcessingPresets Instance { get; private set; }

        #region Enums

        public enum ScenePreset
        {
            Menu,
            Map,
            Combat,
            Editor,
            Cinematic,
            Victory,
            Defeat,
            Custom
        }

        #endregion

        #region Serialized Fields

        [Header("Current State")]
        [SerializeField] private ScenePreset currentPreset = ScenePreset.Menu;
        [SerializeField] [Range(0f, 1f)] private float globalIntensity = 1f;

#if UNITY_PIPELINE_URP
        [Header("Volume Reference")]
        [SerializeField] private Volume globalVolume;
        [SerializeField] private VolumeProfile menuProfile;
        [SerializeField] private VolumeProfile mapProfile;
        [SerializeField] private VolumeProfile combatProfile;
        [SerializeField] private VolumeProfile editorProfile;
        [SerializeField] private VolumeProfile cinematicProfile;
#endif

        [Header("Transition Settings")]
        [SerializeField] private float defaultTransitionDuration = 0.5f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Effect Settings")]
        [SerializeField] private PostProcessingSettings menuSettings;
        [SerializeField] private PostProcessingSettings mapSettings;
        [SerializeField] private PostProcessingSettings combatSettings;
        [SerializeField] private PostProcessingSettings editorSettings;
        [SerializeField] private PostProcessingSettings cinematicSettings;
        [SerializeField] private PostProcessingSettings victorySettings;
        [SerializeField] private PostProcessingSettings defeatSettings;

        #endregion

        #region Private Fields

        private Coroutine transitionCoroutine;
        private Dictionary<ScenePreset, PostProcessingSettings> presetMap;

        #endregion

        #region Events

        public static event Action<ScenePreset> OnPresetChanged;
        public static event Action<ScenePreset, ScenePreset, float> OnTransitionProgress;

        #endregion

        #region Properties

        public static ScenePreset CurrentPreset => Instance?.currentPreset ?? ScenePreset.Menu;
        public static float GlobalIntensity => Instance?.globalIntensity ?? 1f;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePresets();
            InitializeVolume();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            // Apply initial preset
            ApplyPresetImmediate(currentPreset);
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Apply preset immediately
        /// </summary>
        public static void ApplyPreset(ScenePreset preset)
        {
            EnsureInstance();
            Instance.ApplyPresetImmediate(preset);
        }

        /// <summary>
        /// Transition to preset over time
        /// </summary>
        public static void TransitionTo(ScenePreset preset, float duration = -1f)
        {
            EnsureInstance();
            if (duration < 0) duration = Instance.defaultTransitionDuration;
            Instance.TransitionToPreset(preset, duration);
        }

        /// <summary>
        /// Set global post-processing intensity
        /// </summary>
        public static void SetIntensity(float intensity)
        {
            EnsureInstance();
            Instance.SetGlobalIntensity(intensity);
        }

        /// <summary>
        /// Get settings for a preset
        /// </summary>
        public static PostProcessingSettings GetSettings(ScenePreset preset)
        {
            return Instance?.GetPresetSettings(preset);
        }

        /// <summary>
        /// Temporarily boost effects (e.g., during combat hit)
        /// </summary>
        public static void PulseEffect(float intensity = 1.2f, float duration = 0.1f)
        {
            EnsureInstance();
            Instance.DoPulseEffect(intensity, duration);
        }

        /// <summary>
        /// Add vignette effect (e.g., for damage)
        /// </summary>
        public static void FlashVignette(Color color, float intensity = 0.5f, float duration = 0.3f)
        {
            EnsureInstance();
            Instance.DoVignetteFlash(color, intensity, duration);
        }

        #endregion

        #region Instance Methods

        private void InitializePresets()
        {
            // Initialize default settings if null
            if (menuSettings == null) menuSettings = CreateMenuSettings();
            if (mapSettings == null) mapSettings = CreateMapSettings();
            if (combatSettings == null) combatSettings = CreateCombatSettings();
            if (editorSettings == null) editorSettings = CreateEditorSettings();
            if (cinematicSettings == null) cinematicSettings = CreateCinematicSettings();
            if (victorySettings == null) victorySettings = CreateVictorySettings();
            if (defeatSettings == null) defeatSettings = CreateDefeatSettings();

            // Build lookup
            presetMap = new Dictionary<ScenePreset, PostProcessingSettings>
            {
                { ScenePreset.Menu, menuSettings },
                { ScenePreset.Map, mapSettings },
                { ScenePreset.Combat, combatSettings },
                { ScenePreset.Editor, editorSettings },
                { ScenePreset.Cinematic, cinematicSettings },
                { ScenePreset.Victory, victorySettings },
                { ScenePreset.Defeat, defeatSettings }
            };
        }

        private void InitializeVolume()
        {
#if UNITY_PIPELINE_URP
            if (globalVolume == null)
            {
                globalVolume = GetComponent<Volume>();
            }

            if (globalVolume == null)
            {
                globalVolume = gameObject.AddComponent<Volume>();
                globalVolume.isGlobal = true;
                globalVolume.priority = 100;
            }
#endif
        }

        private void ApplyPresetImmediate(ScenePreset preset)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            currentPreset = preset;
            var settings = GetPresetSettings(preset);

            if (settings != null)
            {
                ApplySettings(settings, 1f);
            }

            ApexLogger.Log($"Applied post-processing preset: {preset}", ApexLogger.LogCategory.UI);
            OnPresetChanged?.Invoke(preset);
        }

        private void TransitionToPreset(ScenePreset targetPreset, float duration)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }

            transitionCoroutine = StartCoroutine(TransitionCoroutine(currentPreset, targetPreset, duration));
        }

        private System.Collections.IEnumerator TransitionCoroutine(ScenePreset from, ScenePreset to, float duration)
        {
            var fromSettings = GetPresetSettings(from);
            var toSettings = GetPresetSettings(to);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(elapsed / duration);

                ApplyBlendedSettings(fromSettings, toSettings, t);
                OnTransitionProgress?.Invoke(from, to, t);

                yield return null;
            }

            currentPreset = to;
            ApplySettings(toSettings, 1f);

            transitionCoroutine = null;

            ApexLogger.Log($"Transitioned post-processing: {from} -> {to}", ApexLogger.LogCategory.UI);
            OnPresetChanged?.Invoke(to);
        }

        private void ApplySettings(PostProcessingSettings settings, float weight)
        {
            if (settings == null) return;

#if UNITY_PIPELINE_URP
            if (globalVolume == null) return;
            
            // Get or create profile
            if (globalVolume.profile == null)
            {
                globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            }

            var profile = globalVolume.profile;

            // Apply Bloom
            if (profile.TryGet<Bloom>(out var bloom))
            {
                bloom.active = settings.EnableBloom;
                bloom.intensity.value = settings.BloomIntensity * globalIntensity * weight;
                bloom.threshold.value = settings.BloomThreshold;
                bloom.scatter.value = settings.BloomScatter;
            }

            // Apply Vignette
            if (profile.TryGet<Vignette>(out var vignette))
            {
                vignette.active = settings.EnableVignette;
                vignette.intensity.value = settings.VignetteIntensity * globalIntensity * weight;
                vignette.smoothness.value = settings.VignetteSmoothness;
                vignette.color.value = settings.VignetteColor;
            }

            // Apply Color Adjustments
            if (profile.TryGet<ColorAdjustments>(out var colorAdj))
            {
                colorAdj.active = true;
                colorAdj.postExposure.value = settings.Exposure;
                colorAdj.contrast.value = settings.Contrast;
                colorAdj.saturation.value = settings.Saturation;
                colorAdj.colorFilter.value = Color.Lerp(Color.white, settings.ColorTint, weight);
            }

            // Apply Depth of Field
            if (profile.TryGet<DepthOfField>(out var dof))
            {
                dof.active = settings.EnableDOF;
                dof.focusDistance.value = settings.DOFFocusDistance;
            }

            // Apply Motion Blur
            if (profile.TryGet<MotionBlur>(out var motionBlur))
            {
                motionBlur.active = settings.EnableMotionBlur;
                motionBlur.intensity.value = settings.MotionBlurIntensity * weight;
            }

            // Apply Film Grain
            if (profile.TryGet<FilmGrain>(out var grain))
            {
                grain.active = settings.EnableFilmGrain;
                grain.intensity.value = settings.FilmGrainIntensity * weight;
            }

            // Apply Chromatic Aberration
            if (profile.TryGet<ChromaticAberration>(out var chromatic))
            {
                chromatic.active = settings.EnableChromaticAberration;
                chromatic.intensity.value = settings.ChromaticAberrationIntensity * weight;
            }
#endif
        }

        private void ApplyBlendedSettings(PostProcessingSettings from, PostProcessingSettings to, float t)
        {
            if (from == null || to == null) return;

            // Create blended settings
            var blended = new PostProcessingSettings
            {
                EnableBloom = to.EnableBloom,
                BloomIntensity = Mathf.Lerp(from.BloomIntensity, to.BloomIntensity, t),
                BloomThreshold = Mathf.Lerp(from.BloomThreshold, to.BloomThreshold, t),
                BloomScatter = Mathf.Lerp(from.BloomScatter, to.BloomScatter, t),

                EnableVignette = to.EnableVignette,
                VignetteIntensity = Mathf.Lerp(from.VignetteIntensity, to.VignetteIntensity, t),
                VignetteSmoothness = Mathf.Lerp(from.VignetteSmoothness, to.VignetteSmoothness, t),
                VignetteColor = Color.Lerp(from.VignetteColor, to.VignetteColor, t),

                Exposure = Mathf.Lerp(from.Exposure, to.Exposure, t),
                Contrast = Mathf.Lerp(from.Contrast, to.Contrast, t),
                Saturation = Mathf.Lerp(from.Saturation, to.Saturation, t),
                ColorTint = Color.Lerp(from.ColorTint, to.ColorTint, t),

                EnableDOF = to.EnableDOF,
                DOFFocusDistance = Mathf.Lerp(from.DOFFocusDistance, to.DOFFocusDistance, t),

                EnableMotionBlur = to.EnableMotionBlur,
                MotionBlurIntensity = Mathf.Lerp(from.MotionBlurIntensity, to.MotionBlurIntensity, t),

                EnableFilmGrain = to.EnableFilmGrain,
                FilmGrainIntensity = Mathf.Lerp(from.FilmGrainIntensity, to.FilmGrainIntensity, t),

                EnableChromaticAberration = to.EnableChromaticAberration,
                ChromaticAberrationIntensity = Mathf.Lerp(from.ChromaticAberrationIntensity, to.ChromaticAberrationIntensity, t)
            };

            ApplySettings(blended, 1f);
        }

        private void SetGlobalIntensity(float intensity)
        {
            globalIntensity = Mathf.Clamp01(intensity);
            ApplySettings(GetPresetSettings(currentPreset), 1f);
        }

        private PostProcessingSettings GetPresetSettings(ScenePreset preset)
        {
            return presetMap?.GetValueOrDefault(preset, menuSettings) ?? menuSettings;
        }

        private void DoPulseEffect(float intensity, float duration)
        {
            StartCoroutine(PulseCoroutine(intensity, duration));
        }

        private System.Collections.IEnumerator PulseCoroutine(float intensity, float duration)
        {
            float originalIntensity = globalIntensity;
            float elapsed = 0f;
            float halfDuration = duration / 2f;

            // Ramp up
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                globalIntensity = Mathf.Lerp(originalIntensity, intensity, t);
                ApplySettings(GetPresetSettings(currentPreset), 1f);
                yield return null;
            }

            // Ramp down
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                globalIntensity = Mathf.Lerp(intensity, originalIntensity, t);
                ApplySettings(GetPresetSettings(currentPreset), 1f);
                yield return null;
            }

            globalIntensity = originalIntensity;
        }

        private void DoVignetteFlash(Color color, float intensity, float duration)
        {
            StartCoroutine(VignetteFlashCoroutine(color, intensity, duration));
        }

        private System.Collections.IEnumerator VignetteFlashCoroutine(Color color, float intensity, float duration)
        {
#if UNITY_PIPELINE_URP
            if (globalVolume?.profile == null) yield break;

            if (!globalVolume.profile.TryGet<Vignette>(out var vignette))
                yield break;

            Color originalColor = vignette.color.value;
            float originalIntensity = vignette.intensity.value;

            vignette.color.value = color;
            vignette.intensity.value = intensity;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                vignette.intensity.value = Mathf.Lerp(intensity, originalIntensity, t);
                vignette.color.value = Color.Lerp(color, originalColor, t);
                yield return null;
            }

            vignette.color.value = originalColor;
            vignette.intensity.value = originalIntensity;
#else
            yield break;
#endif
        }

        #endregion

        #region Preset Factories

        private PostProcessingSettings CreateMenuSettings()
        {
            return new PostProcessingSettings
            {
                Name = "Menu",
                EnableBloom = true,
                BloomIntensity = 0.3f,
                BloomThreshold = 1.1f,
                BloomScatter = 0.6f,
                EnableVignette = true,
                VignetteIntensity = 0.25f,
                VignetteSmoothness = 0.4f,
                VignetteColor = Color.black,
                Exposure = 0f,
                Contrast = 5f,
                Saturation = 10f,
                ColorTint = Color.white,
                EnableDOF = true,
                DOFFocusDistance = 5f,
                EnableMotionBlur = false,
                MotionBlurIntensity = 0f,
                EnableFilmGrain = false,
                FilmGrainIntensity = 0f,
                EnableChromaticAberration = false,
                ChromaticAberrationIntensity = 0f
            };
        }

        private PostProcessingSettings CreateMapSettings()
        {
            return new PostProcessingSettings
            {
                Name = "Map",
                EnableBloom = true,
                BloomIntensity = 0.4f,
                BloomThreshold = 1f,
                BloomScatter = 0.7f,
                EnableVignette = true,
                VignetteIntensity = 0.2f,
                VignetteSmoothness = 0.5f,
                VignetteColor = new Color(0.1f, 0.1f, 0.2f),
                Exposure = 0.1f,
                Contrast = 10f,
                Saturation = 15f,
                ColorTint = new Color(1f, 0.98f, 0.95f),
                EnableDOF = false,
                DOFFocusDistance = 10f,
                EnableMotionBlur = false,
                MotionBlurIntensity = 0f,
                EnableFilmGrain = false,
                FilmGrainIntensity = 0f,
                EnableChromaticAberration = false,
                ChromaticAberrationIntensity = 0f
            };
        }

        private PostProcessingSettings CreateCombatSettings()
        {
            return new PostProcessingSettings
            {
                Name = "Combat",
                EnableBloom = true,
                BloomIntensity = 0.6f,
                BloomThreshold = 0.9f,
                BloomScatter = 0.65f,
                EnableVignette = true,
                VignetteIntensity = 0.35f,
                VignetteSmoothness = 0.35f,
                VignetteColor = new Color(0.2f, 0f, 0f),
                Exposure = 0.15f,
                Contrast = 15f,
                Saturation = 5f,
                ColorTint = new Color(1f, 0.95f, 0.9f),
                EnableDOF = true,
                DOFFocusDistance = 3f,
                EnableMotionBlur = true,
                MotionBlurIntensity = 0.15f,
                EnableFilmGrain = true,
                FilmGrainIntensity = 0.1f,
                EnableChromaticAberration = true,
                ChromaticAberrationIntensity = 0.05f
            };
        }

        private PostProcessingSettings CreateEditorSettings()
        {
            return new PostProcessingSettings
            {
                Name = "Editor",
                EnableBloom = true,
                BloomIntensity = 0.2f,
                BloomThreshold = 1.2f,
                BloomScatter = 0.5f,
                EnableVignette = false,
                VignetteIntensity = 0f,
                VignetteSmoothness = 0.5f,
                VignetteColor = Color.black,
                Exposure = 0f,
                Contrast = 0f,
                Saturation = 0f,
                ColorTint = Color.white,
                EnableDOF = false,
                DOFFocusDistance = 10f,
                EnableMotionBlur = false,
                MotionBlurIntensity = 0f,
                EnableFilmGrain = false,
                FilmGrainIntensity = 0f,
                EnableChromaticAberration = false,
                ChromaticAberrationIntensity = 0f
            };
        }

        private PostProcessingSettings CreateCinematicSettings()
        {
            return new PostProcessingSettings
            {
                Name = "Cinematic",
                EnableBloom = true,
                BloomIntensity = 0.5f,
                BloomThreshold = 0.95f,
                BloomScatter = 0.75f,
                EnableVignette = true,
                VignetteIntensity = 0.4f,
                VignetteSmoothness = 0.3f,
                VignetteColor = Color.black,
                Exposure = 0.05f,
                Contrast = 20f,
                Saturation = -5f,
                ColorTint = new Color(0.95f, 0.95f, 1f),
                EnableDOF = true,
                DOFFocusDistance = 4f,
                EnableMotionBlur = true,
                MotionBlurIntensity = 0.25f,
                EnableFilmGrain = true,
                FilmGrainIntensity = 0.15f,
                EnableChromaticAberration = true,
                ChromaticAberrationIntensity = 0.08f
            };
        }

        private PostProcessingSettings CreateVictorySettings()
        {
            return new PostProcessingSettings
            {
                Name = "Victory",
                EnableBloom = true,
                BloomIntensity = 0.8f,
                BloomThreshold = 0.85f,
                BloomScatter = 0.8f,
                EnableVignette = true,
                VignetteIntensity = 0.15f,
                VignetteSmoothness = 0.6f,
                VignetteColor = new Color(0.2f, 0.15f, 0f),
                Exposure = 0.3f,
                Contrast = 5f,
                Saturation = 20f,
                ColorTint = new Color(1f, 0.98f, 0.85f),
                EnableDOF = true,
                DOFFocusDistance = 5f,
                EnableMotionBlur = false,
                MotionBlurIntensity = 0f,
                EnableFilmGrain = false,
                FilmGrainIntensity = 0f,
                EnableChromaticAberration = false,
                ChromaticAberrationIntensity = 0f
            };
        }

        private PostProcessingSettings CreateDefeatSettings()
        {
            return new PostProcessingSettings
            {
                Name = "Defeat",
                EnableBloom = true,
                BloomIntensity = 0.2f,
                BloomThreshold = 1.2f,
                BloomScatter = 0.5f,
                EnableVignette = true,
                VignetteIntensity = 0.5f,
                VignetteSmoothness = 0.25f,
                VignetteColor = new Color(0.15f, 0f, 0f),
                Exposure = -0.2f,
                Contrast = -5f,
                Saturation = -30f,
                ColorTint = new Color(0.85f, 0.85f, 0.9f),
                EnableDOF = true,
                DOFFocusDistance = 2f,
                EnableMotionBlur = true,
                MotionBlurIntensity = 0.3f,
                EnableFilmGrain = true,
                FilmGrainIntensity = 0.25f,
                EnableChromaticAberration = true,
                ChromaticAberrationIntensity = 0.12f
            };
        }

        #endregion

        #region Utility

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("PostProcessingPresets");
                Instance = go.AddComponent<PostProcessingPresets>();
            }
        }

        [ContextMenu("Apply Menu Preset")]
        private void ApplyMenuPreset() => ApplyPreset(ScenePreset.Menu);

        [ContextMenu("Apply Map Preset")]
        private void ApplyMapPreset() => ApplyPreset(ScenePreset.Map);

        [ContextMenu("Apply Combat Preset")]
        private void ApplyCombatPreset() => ApplyPreset(ScenePreset.Combat);

        [ContextMenu("Apply Victory Preset")]
        private void ApplyVictoryPreset() => ApplyPreset(ScenePreset.Victory);

        [ContextMenu("Apply Defeat Preset")]
        private void ApplyDefeatPreset() => ApplyPreset(ScenePreset.Defeat);

        [ContextMenu("Test Damage Flash")]
        private void TestDamageFlash() => FlashVignette(Color.red, 0.6f, 0.4f);

        #endregion
    }

    #region Post-Processing Settings Data

    /// <summary>
    /// Post-processing effect settings
    /// </summary>
    [Serializable]
    public class PostProcessingSettings
    {
        public string Name = "Custom";

        [Header("Bloom")]
        public bool EnableBloom = true;
        [Range(0f, 2f)] public float BloomIntensity = 0.5f;
        [Range(0f, 2f)] public float BloomThreshold = 1f;
        [Range(0f, 1f)] public float BloomScatter = 0.7f;

        [Header("Vignette")]
        public bool EnableVignette = true;
        [Range(0f, 1f)] public float VignetteIntensity = 0.3f;
        [Range(0f, 1f)] public float VignetteSmoothness = 0.4f;
        public Color VignetteColor = Color.black;

        [Header("Color Adjustments")]
        [Range(-2f, 2f)] public float Exposure = 0f;
        [Range(-100f, 100f)] public float Contrast = 0f;
        [Range(-100f, 100f)] public float Saturation = 0f;
        public Color ColorTint = Color.white;

        [Header("Depth of Field")]
        public bool EnableDOF = false;
        [Range(0.1f, 100f)] public float DOFFocusDistance = 10f;

        [Header("Motion Blur")]
        public bool EnableMotionBlur = false;
        [Range(0f, 1f)] public float MotionBlurIntensity = 0.2f;

        [Header("Film Grain")]
        public bool EnableFilmGrain = false;
        [Range(0f, 1f)] public float FilmGrainIntensity = 0.1f;

        [Header("Chromatic Aberration")]
        public bool EnableChromaticAberration = false;
        [Range(0f, 1f)] public float ChromaticAberrationIntensity = 0.05f;
    }

    #endregion
}
