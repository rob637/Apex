using Camera = UnityEngine.Camera;
// ============================================================================
// APEX CITADELS - POST PROCESSING SETUP
// Configures beautiful visual effects using camera-based approach
// Works without URP-specific packages
// ============================================================================
using UnityEngine;
using System.Collections;
using ApexCitadels.Core;

namespace ApexCitadels.PC.Visual
{
    /// <summary>
    /// Sets up post-processing effects for a polished visual experience.
    /// Uses camera and shader-based effects that work with any render pipeline.
    /// </summary>
    public class PostProcessingSetup : MonoBehaviour
    {
        public static PostProcessingSetup Instance { get; private set; }

        [Header("Presets")]
        public VisualPreset currentPreset = VisualPreset.Default;

        [Header("Effect Settings")]
        [Range(0f, 2f)] public float bloomIntensity = 0.5f;
        [Range(0f, 1f)] public float vignetteIntensity = 0.25f;
        [Range(-100f, 100f)] public float saturation = 10f;
        [Range(-2f, 2f)] public float exposure = 0.2f;
        [Range(0f, 50f)] public float contrast = 10f;
        public Color colorTint = new Color(1f, 0.98f, 0.95f);

        // Camera reference
        private Camera mainCamera;
        
        // Overlay for effects
        private GameObject overlayQuad;
        private Material overlayMaterial;
        private bool isFlashing = false;

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
            mainCamera = Camera.main;
            SetupEffects();
            ApplyPreset(VisualPreset.Default);
            
            ApexLogger.Log(ApexLogger.LogCategory.General, "✅ Post-processing system initialized (Camera-based)");
        }

        /// <summary>
        /// Sets up visual effect components
        /// </summary>
        private void SetupEffects()
        {
            // Configure camera for better visuals
            if (mainCamera != null)
            {
                mainCamera.allowHDR = true;
                mainCamera.allowMSAA = true;
                
                // Set a nice default clear color (sky blue gradient fallback)
                mainCamera.clearFlags = CameraClearFlags.Skybox;
            }

            // Create screen overlay for vignette and flash effects
            CreateScreenOverlay();
            
            // Apply initial lighting enhancements
            EnhanceLighting();
            
            ApexLogger.LogVerbose(ApexLogger.LogCategory.General, "Visual effects configured");
        }

        private void CreateScreenOverlay()
        {
            // Find a valid transparent shader
            Shader transparentShader = Shader.Find("Unlit/Transparent");
            if (transparentShader == null)
            {
                transparentShader = Shader.Find("Sprites/Default");
            }
            if (transparentShader == null)
            {
                transparentShader = Shader.Find("UI/Default");
            }
            
            // If no shader found, skip overlay creation to avoid yellow screen
            if (transparentShader == null)
            {
                ApexLogger.LogWarning(ApexLogger.LogCategory.General, "Could not find transparent shader, skipping vignette overlay");
                return;
            }
            
            // Create overlay camera for effects
            GameObject overlayCamObj = new GameObject("EffectsOverlayCamera");
            overlayCamObj.transform.SetParent(transform);
            
            Camera overlayCam = overlayCamObj.AddComponent<Camera>();
            overlayCam.clearFlags = CameraClearFlags.Nothing;
            overlayCam.cullingMask = 0; // Don't render anything
            overlayCam.depth = 100; // Render after main camera
            overlayCam.orthographic = true;
            overlayCam.enabled = false; // Disable by default - enable only when needed
            
            // Create overlay quad in screen space
            overlayQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            overlayQuad.name = "VignetteOverlay";
            overlayQuad.transform.SetParent(overlayCamObj.transform);
            overlayQuad.transform.localPosition = new Vector3(0, 0, 1);
            overlayQuad.transform.localScale = new Vector3(20, 20, 1);
            
            // Remove collider
            Destroy(overlayQuad.GetComponent<Collider>());
            
            // Create vignette material with verified shader
            overlayMaterial = new Material(transparentShader);
            // Create vignette texture
            Texture2D vignetteTex = CreateVignetteTexture(256);
            overlayMaterial.mainTexture = vignetteTex;
            overlayMaterial.color = new Color(0, 0, 0, vignetteIntensity);
            overlayQuad.GetComponent<Renderer>().material = overlayMaterial;
            
            // Initially hide - vignette is subtle effect, not needed by default
            overlayQuad.SetActive(false);
            overlayCam.enabled = false;
        }

        private Texture2D CreateVignetteTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = size / 2f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float normalizedDist = dist / maxDist;
                    
                    // Vignette falloff
                    float alpha = Mathf.Pow(normalizedDist, 2f) * Mathf.SmoothStep(0.3f, 1f, normalizedDist);
                    pixels[y * size + x] = new Color(0, 0, 0, alpha);
                }
            }
            
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void EnhanceLighting()
        {
            // Find and enhance directional light
            Light sun = null;
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    sun = light;
                    break;
                }
            }

            if (sun != null)
            {
                sun.intensity = 1.2f;
                sun.color = new Color(1f, 0.95f, 0.85f); // Warm sunlight
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.7f;
            }

            // Set ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.6f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.45f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.15f);
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

            ApplySettings();
            ApexLogger.LogVerbose(ApexLogger.LogCategory.General, $"Applied preset: {preset}");
        }

        private void ApplySettings()
        {
            // Update vignette overlay
            if (overlayMaterial != null && overlayQuad != null)
            {
                overlayMaterial.color = new Color(
                    colorTint.r * 0.1f, 
                    colorTint.g * 0.1f, 
                    colorTint.b * 0.1f, 
                    vignetteIntensity
                );
                overlayQuad.SetActive(vignetteIntensity > 0.05f);
            }

            // Update ambient lighting based on preset
            float ambientIntensity = 1f + (exposure * 0.5f);
            RenderSettings.ambientIntensity = Mathf.Clamp(ambientIntensity, 0.5f, 2f);
        }

        private void ApplyDefaultPreset()
        {
            bloomIntensity = 0.5f;
            vignetteIntensity = 0.25f;
            saturation = 10f;
            exposure = 0.2f;
            contrast = 10f;
            colorTint = new Color(1f, 0.98f, 0.95f);
        }

        private void ApplyCinematicPreset()
        {
            bloomIntensity = 0.8f;
            vignetteIntensity = 0.4f;
            saturation = 5f;
            exposure = 0.1f;
            contrast = 15f;
            colorTint = new Color(0.95f, 0.95f, 1f); // Slight blue
        }

        private void ApplyVibrantPreset()
        {
            bloomIntensity = 0.7f;
            vignetteIntensity = 0.2f;
            saturation = 30f;
            exposure = 0.3f;
            contrast = 20f;
            colorTint = Color.white;
        }

        private void ApplyMutedPreset()
        {
            bloomIntensity = 0.3f;
            vignetteIntensity = 0.35f;
            saturation = -20f;
            exposure = 0f;
            contrast = 5f;
            colorTint = new Color(0.9f, 0.9f, 0.85f);
        }

        private void ApplyNightPreset()
        {
            bloomIntensity = 1f;
            vignetteIntensity = 0.5f;
            saturation = -10f;
            exposure = -0.5f;
            contrast = 25f;
            colorTint = new Color(0.7f, 0.75f, 0.9f); // Blue night

            // Darken ambient
            RenderSettings.ambientSkyColor = new Color(0.1f, 0.1f, 0.2f);
            RenderSettings.ambientEquatorColor = new Color(0.05f, 0.05f, 0.1f);
            RenderSettings.ambientGroundColor = new Color(0.02f, 0.02f, 0.05f);
        }

        private void ApplyCombatPreset()
        {
            bloomIntensity = 0.9f;
            vignetteIntensity = 0.35f;
            saturation = 15f;
            exposure = 0.1f;
            contrast = 25f;
            colorTint = new Color(1f, 0.9f, 0.85f); // Warm/red tint

            // Update vignette to red
            if (overlayMaterial != null)
            {
                overlayMaterial.color = new Color(0.3f, 0.05f, 0.05f, vignetteIntensity);
            }
        }

        private void ApplyVictoryPreset()
        {
            bloomIntensity = 1.5f;
            vignetteIntensity = 0.2f;
            saturation = 20f;
            exposure = 0.4f;
            contrast = 15f;
            colorTint = new Color(1f, 0.95f, 0.8f); // Golden

            // Brighten ambient
            RenderSettings.ambientSkyColor = new Color(0.7f, 0.65f, 0.5f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.45f, 0.35f);
        }

        #endregion

        #region Individual Effect Controls

        /// <summary>
        /// Sets bloom intensity
        /// </summary>
        public void SetBloomIntensity(float intensity)
        {
            bloomIntensity = intensity;
        }

        /// <summary>
        /// Sets vignette intensity
        /// </summary>
        public void SetVignetteIntensity(float intensity)
        {
            vignetteIntensity = intensity;
            ApplySettings();
        }

        /// <summary>
        /// Sets saturation
        /// </summary>
        public void SetSaturation(float sat)
        {
            saturation = sat;
        }

        /// <summary>
        /// Sets exposure
        /// </summary>
        public void SetExposure(float exp)
        {
            exposure = exp;
            ApplySettings();
        }

        /// <summary>
        /// Enables/disables chromatic aberration effect (simulated via color shift)
        /// </summary>
        public void SetChromaticAberration(bool enabled, float intensity = 0.2f)
        {
            // Simulated via color tint shift
            if (enabled)
            {
                colorTint = new Color(1f + intensity * 0.1f, 1f, 1f - intensity * 0.1f);
            }
            else
            {
                colorTint = Color.white;
            }
        }

        /// <summary>
        /// Enables/disables motion blur effect (placeholder)
        /// </summary>
        public void SetMotionBlur(bool enabled, float intensity = 0.2f)
        {
            // Motion blur requires render texture - placeholder for now
            ApexLogger.LogVerbose(ApexLogger.LogCategory.General, $"Motion blur {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Enables/disables depth of field effect (placeholder)
        /// </summary>
        public void SetDepthOfField(bool enabled, float focusDistance = 10f)
        {
            // DOF requires post-process shader - placeholder for now
            ApexLogger.LogVerbose(ApexLogger.LogCategory.General, $"Depth of field {(enabled ? "enabled" : "disabled")}");
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

        private IEnumerator TransitionCoroutine(VisualPreset preset, float duration)
        {
            // Store current values
            float startVignette = vignetteIntensity;
            float startSaturation = saturation;
            float startExposure = exposure;

            // Get target values
            ApplyPreset(preset);
            float targetVignette = vignetteIntensity;
            float targetSaturation = saturation;
            float targetExposure = exposure;

            // Reset to start values
            vignetteIntensity = startVignette;
            saturation = startSaturation;
            exposure = startExposure;

            // Interpolate
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float progress = t / duration;
                float smooth = Mathf.SmoothStep(0, 1, progress);

                vignetteIntensity = Mathf.Lerp(startVignette, targetVignette, smooth);
                saturation = Mathf.Lerp(startSaturation, targetSaturation, smooth);
                exposure = Mathf.Lerp(startExposure, targetExposure, smooth);
                
                ApplySettings();
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
            if (!isFlashing)
            {
                StartCoroutine(DamageFlashCoroutine());
            }
        }

        private IEnumerator DamageFlashCoroutine()
        {
            isFlashing = true;
            
            // Store original
            float originalVignette = vignetteIntensity;
            Color originalColor = overlayMaterial != null ? overlayMaterial.color : Color.black;

            // Flash red
            if (overlayMaterial != null && overlayQuad != null)
            {
                overlayQuad.SetActive(true);
                overlayMaterial.color = new Color(0.5f, 0f, 0f, 0.5f);
            }

            yield return new WaitForSeconds(0.1f);

            // Fade back
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                float progress = t / 0.3f;
                if (overlayMaterial != null)
                {
                    overlayMaterial.color = Color.Lerp(
                        new Color(0.5f, 0f, 0f, 0.5f), 
                        originalColor, 
                        progress
                    );
                }
                yield return null;
            }

            // Restore
            vignetteIntensity = originalVignette;
            ApplySettings();
            isFlashing = false;
        }

        /// <summary>
        /// Cycle to next preset (for debug)
        /// </summary>
        public void CyclePreset()
        {
            int current = (int)currentPreset;
            int next = (current + 1) % System.Enum.GetValues(typeof(VisualPreset)).Length;
            ApplyPreset((VisualPreset)next);
        }

        #endregion
    }
}
