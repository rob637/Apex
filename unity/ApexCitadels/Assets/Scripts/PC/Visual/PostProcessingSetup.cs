// ============================================================================
// APEX CITADELS - POST PROCESSING SETUP
// Configures beautiful visual effects: bloom, color grading, SSAO, etc.
// ============================================================================
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ApexCitadels.PC.Visual
{
    /// <summary>
    /// Sets up post-processing effects for a polished visual experience.
    /// Creates bloom, color grading, vignette, and other effects.
    /// </summary>
    public class PostProcessingSetup : MonoBehaviour
    {
        public static PostProcessingSetup Instance { get; private set; }

        [Header("Volume Profile")]
        private Volume globalVolume;
        private VolumeProfile profile;

        [Header("Effects")]
        private Bloom bloom;
        private ColorAdjustments colorAdjustments;
        private Vignette vignette;
        private ChromaticAberration chromaticAberration;
        private DepthOfField depthOfField;
        private MotionBlur motionBlur;
        private FilmGrain filmGrain;
        private LiftGammaGain liftGammaGain;

        [Header("Presets")]
        public VisualPreset currentPreset = VisualPreset.Default;

        public enum VisualPreset
        {
            Default,
            Cinematic,
            Vibrant,
            Muted,
            Night,
            Combat,
            Victory
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            SetupPostProcessing();
            ApplyPreset(VisualPreset.Default);
            
            Debug.Log("[PostProcess] ✅ Post-processing system initialized");
        }

        /// <summary>
        /// Creates and configures the post-processing volume
        /// </summary>
        private void SetupPostProcessing()
        {
            // Create global volume
            GameObject volumeObj = new GameObject("GlobalPostProcessVolume");
            volumeObj.transform.SetParent(transform);
            volumeObj.layer = LayerMask.NameToLayer("Default");

            globalVolume = volumeObj.AddComponent<Volume>();
            globalVolume.isGlobal = true;
            globalVolume.priority = 100;

            // Create volume profile
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            globalVolume.profile = profile;

            // Add effects
            AddBloom();
            AddColorAdjustments();
            AddVignette();
            AddChromaticAberration();
            AddDepthOfField();
            AddMotionBlur();
            AddFilmGrain();
            AddLiftGammaGain();

            // Make sure camera has post-processing enabled
            Camera cam = Camera.main;
            if (cam != null)
            {
                var additionalData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (additionalData == null)
                {
                    additionalData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                }
                additionalData.renderPostProcessing = true;
                additionalData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                additionalData.antialiasingQuality = AntialiasingQuality.High;
            }

            Debug.Log("[PostProcess] Created volume with all effects");
        }

        private void AddBloom()
        {
            bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.5f);
            bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(1f, 1f, 1f));
        }

        private void AddColorAdjustments()
        {
            colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.active = true;
            colorAdjustments.postExposure.Override(0.2f);
            colorAdjustments.contrast.Override(10f);
            colorAdjustments.saturation.Override(10f);
            colorAdjustments.colorFilter.Override(new Color(1f, 0.98f, 0.95f)); // Slight warm tint
        }

        private void AddVignette()
        {
            vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.intensity.Override(0.25f);
            vignette.smoothness.Override(0.4f);
            vignette.rounded.Override(false);
        }

        private void AddChromaticAberration()
        {
            chromaticAberration = profile.Add<ChromaticAberration>(true);
            chromaticAberration.active = false; // Off by default
            chromaticAberration.intensity.Override(0.1f);
        }

        private void AddDepthOfField()
        {
            depthOfField = profile.Add<DepthOfField>(true);
            depthOfField.active = false; // Off by default for strategy game
            depthOfField.mode.Override(DepthOfFieldMode.Bokeh);
            depthOfField.focusDistance.Override(10f);
            depthOfField.aperture.Override(5.6f);
        }

        private void AddMotionBlur()
        {
            motionBlur = profile.Add<MotionBlur>(true);
            motionBlur.active = false; // Off by default
            motionBlur.intensity.Override(0.2f);
            motionBlur.quality.Override(MotionBlurQuality.Medium);
        }

        private void AddFilmGrain()
        {
            filmGrain = profile.Add<FilmGrain>(true);
            filmGrain.active = false; // Off by default
            filmGrain.type.Override(FilmGrainLookup.Medium1);
            filmGrain.intensity.Override(0.2f);
        }

        private void AddLiftGammaGain()
        {
            liftGammaGain = profile.Add<LiftGammaGain>(true);
            liftGammaGain.active = true;
            liftGammaGain.lift.Override(new Vector4(1f, 1f, 1f, 0f));
            liftGammaGain.gamma.Override(new Vector4(1f, 1f, 1f, 0f));
            liftGammaGain.gain.Override(new Vector4(1f, 1f, 1f, 0f));
        }

        #region Preset Application

        /// <summary>
        /// Applies a visual preset
        /// </summary>
        public void ApplyPreset(VisualPreset preset)
        {
            currentPreset = preset;

            switch (preset)
            {
                case VisualPreset.Default:
                    ApplyDefaultPreset();
                    break;
                case VisualPreset.Cinematic:
                    ApplyCinematicPreset();
                    break;
                case VisualPreset.Vibrant:
                    ApplyVibrantPreset();
                    break;
                case VisualPreset.Muted:
                    ApplyMutedPreset();
                    break;
                case VisualPreset.Night:
                    ApplyNightPreset();
                    break;
                case VisualPreset.Combat:
                    ApplyCombatPreset();
                    break;
                case VisualPreset.Victory:
                    ApplyVictoryPreset();
                    break;
            }

            Debug.Log($"[PostProcess] Applied preset: {preset}");
        }

        private void ApplyDefaultPreset()
        {
            bloom.intensity.Override(0.5f);
            bloom.threshold.Override(1f);
            
            colorAdjustments.postExposure.Override(0.2f);
            colorAdjustments.contrast.Override(10f);
            colorAdjustments.saturation.Override(10f);
            colorAdjustments.colorFilter.Override(new Color(1f, 0.98f, 0.95f));
            
            vignette.intensity.Override(0.25f);
            
            chromaticAberration.active = false;
            depthOfField.active = false;
            motionBlur.active = false;
            filmGrain.active = false;
        }

        private void ApplyCinematicPreset()
        {
            bloom.intensity.Override(0.8f);
            bloom.threshold.Override(0.9f);
            
            colorAdjustments.postExposure.Override(0.1f);
            colorAdjustments.contrast.Override(15f);
            colorAdjustments.saturation.Override(5f);
            colorAdjustments.colorFilter.Override(new Color(0.95f, 0.95f, 1f)); // Slight blue
            
            vignette.intensity.Override(0.4f);
            
            filmGrain.active = true;
            filmGrain.intensity.Override(0.15f);
            
            // Subtle letterbox effect (via vignette)
            vignette.rounded.Override(false);
        }

        private void ApplyVibrantPreset()
        {
            bloom.intensity.Override(0.7f);
            bloom.threshold.Override(0.8f);
            bloom.tint.Override(new Color(1f, 0.95f, 0.9f));
            
            colorAdjustments.postExposure.Override(0.3f);
            colorAdjustments.contrast.Override(20f);
            colorAdjustments.saturation.Override(30f);
            colorAdjustments.colorFilter.Override(Color.white);
            
            vignette.intensity.Override(0.2f);
            
            chromaticAberration.active = false;
            filmGrain.active = false;
        }

        private void ApplyMutedPreset()
        {
            bloom.intensity.Override(0.3f);
            bloom.threshold.Override(1.2f);
            
            colorAdjustments.postExposure.Override(0f);
            colorAdjustments.contrast.Override(5f);
            colorAdjustments.saturation.Override(-20f);
            colorAdjustments.colorFilter.Override(new Color(0.9f, 0.9f, 0.85f));
            
            vignette.intensity.Override(0.35f);
        }

        private void ApplyNightPreset()
        {
            bloom.intensity.Override(1f);
            bloom.threshold.Override(0.7f);
            bloom.tint.Override(new Color(0.6f, 0.7f, 1f)); // Blue tint
            
            colorAdjustments.postExposure.Override(-0.5f);
            colorAdjustments.contrast.Override(25f);
            colorAdjustments.saturation.Override(-10f);
            colorAdjustments.colorFilter.Override(new Color(0.7f, 0.75f, 0.9f));
            
            vignette.intensity.Override(0.5f);
            vignette.color.Override(new Color(0.1f, 0.1f, 0.2f));
            
            filmGrain.active = true;
            filmGrain.intensity.Override(0.25f);
        }

        private void ApplyCombatPreset()
        {
            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(0.8f);
            bloom.tint.Override(new Color(1f, 0.9f, 0.8f));
            
            colorAdjustments.postExposure.Override(0.1f);
            colorAdjustments.contrast.Override(25f);
            colorAdjustments.saturation.Override(15f);
            colorAdjustments.colorFilter.Override(new Color(1f, 0.95f, 0.9f));
            
            vignette.intensity.Override(0.35f);
            vignette.color.Override(new Color(0.3f, 0.1f, 0.1f)); // Red tint
            
            chromaticAberration.active = true;
            chromaticAberration.intensity.Override(0.15f);
            
            motionBlur.active = true;
            motionBlur.intensity.Override(0.3f);
        }

        private void ApplyVictoryPreset()
        {
            bloom.intensity.Override(1.5f);
            bloom.threshold.Override(0.6f);
            bloom.tint.Override(new Color(1f, 0.95f, 0.7f)); // Golden
            
            colorAdjustments.postExposure.Override(0.4f);
            colorAdjustments.contrast.Override(15f);
            colorAdjustments.saturation.Override(20f);
            colorAdjustments.colorFilter.Override(new Color(1f, 0.98f, 0.9f));
            
            vignette.intensity.Override(0.2f);
            vignette.color.Override(new Color(0.9f, 0.8f, 0.5f));
        }

        #endregion

        #region Individual Effect Controls

        /// <summary>
        /// Sets bloom intensity
        /// </summary>
        public void SetBloomIntensity(float intensity)
        {
            bloom.intensity.Override(intensity);
        }

        /// <summary>
        /// Sets vignette intensity
        /// </summary>
        public void SetVignetteIntensity(float intensity)
        {
            vignette.intensity.Override(intensity);
        }

        /// <summary>
        /// Sets saturation
        /// </summary>
        public void SetSaturation(float saturation)
        {
            colorAdjustments.saturation.Override(saturation);
        }

        /// <summary>
        /// Sets exposure
        /// </summary>
        public void SetExposure(float exposure)
        {
            colorAdjustments.postExposure.Override(exposure);
        }

        /// <summary>
        /// Enables/disables chromatic aberration (damage effect)
        /// </summary>
        public void SetChromaticAberration(bool enabled, float intensity = 0.2f)
        {
            chromaticAberration.active = enabled;
            if (enabled) chromaticAberration.intensity.Override(intensity);
        }

        /// <summary>
        /// Enables/disables motion blur
        /// </summary>
        public void SetMotionBlur(bool enabled, float intensity = 0.2f)
        {
            motionBlur.active = enabled;
            if (enabled) motionBlur.intensity.Override(intensity);
        }

        /// <summary>
        /// Enables/disables depth of field
        /// </summary>
        public void SetDepthOfField(bool enabled, float focusDistance = 10f)
        {
            depthOfField.active = enabled;
            if (enabled) depthOfField.focusDistance.Override(focusDistance);
        }

        #endregion

        #region Transition Effects

        /// <summary>
        /// Smoothly transitions to a preset over time
        /// </summary>
        public void TransitionToPreset(VisualPreset preset, float duration = 1f)
        {
            StartCoroutine(TransitionCoroutine(preset, duration));
        }

        private System.Collections.IEnumerator TransitionCoroutine(VisualPreset preset, float duration)
        {
            // Store current values
            float startBloom = bloom.intensity.value;
            float startVignette = vignette.intensity.value;
            float startSaturation = colorAdjustments.saturation.value;
            float startExposure = colorAdjustments.postExposure.value;

            // Get target values (apply preset temporarily to read them)
            ApplyPreset(preset);
            float targetBloom = bloom.intensity.value;
            float targetVignette = vignette.intensity.value;
            float targetSaturation = colorAdjustments.saturation.value;
            float targetExposure = colorAdjustments.postExposure.value;

            // Reset to start values
            bloom.intensity.Override(startBloom);
            vignette.intensity.Override(startVignette);
            colorAdjustments.saturation.Override(startSaturation);
            colorAdjustments.postExposure.Override(startExposure);

            // Interpolate
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float progress = t / duration;
                float smooth = Mathf.SmoothStep(0, 1, progress);

                bloom.intensity.Override(Mathf.Lerp(startBloom, targetBloom, smooth));
                vignette.intensity.Override(Mathf.Lerp(startVignette, targetVignette, smooth));
                colorAdjustments.saturation.Override(Mathf.Lerp(startSaturation, targetSaturation, smooth));
                colorAdjustments.postExposure.Override(Mathf.Lerp(startExposure, targetExposure, smooth));

                yield return null;
            }

            // Apply final preset
            ApplyPreset(preset);
        }

        /// <summary>
        /// Creates a damage flash effect
        /// </summary>
        public void DamageFlash()
        {
            StartCoroutine(DamageFlashCoroutine());
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            // Store original
            Color originalTint = vignette.color.value;
            float originalIntensity = vignette.intensity.value;

            // Flash red
            vignette.color.Override(new Color(0.5f, 0f, 0f));
            vignette.intensity.Override(0.5f);
            chromaticAberration.active = true;
            chromaticAberration.intensity.Override(0.3f);

            yield return new WaitForSeconds(0.1f);

            // Fade back
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                float progress = t / 0.3f;
                vignette.color.Override(Color.Lerp(new Color(0.5f, 0f, 0f), originalTint, progress));
                vignette.intensity.Override(Mathf.Lerp(0.5f, originalIntensity, progress));
                chromaticAberration.intensity.Override(Mathf.Lerp(0.3f, 0f, progress));
                yield return null;
            }

            // Restore
            vignette.color.Override(originalTint);
            vignette.intensity.Override(originalIntensity);
            chromaticAberration.active = false;
        }

        #endregion
    }
}
