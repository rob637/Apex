using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// mood and atmosphere manager.
    /// Applies "AAA" visual polish: Post-processing, Fog, Lighting.
    /// </summary>
    public class FantasyWorldVisuals : MonoBehaviour
    {
        [Header("Atmosphere")]
        [Tooltip("Assign a Skybox Material here (e.g. from Synty folder)")]
        public Material skyboxMaterial;
        [Tooltip("OR Assign a panoramic texture here to auto-create a skybox")]
        public Texture2D skyboxTexture;
        
        public Color fogColor = new Color(0.35f, 0.4f, 0.5f);
        public float fogDensity = 0.005f;
        public float skyboxExposure = 1.0f;

        [Header("Lighting")]
        public Color sunColor = new Color(1f, 0.95f, 0.8f);
        public float sunIntensity = 1.5f;
        public Color shadowColor = new Color(0.2f, 0.2f, 0.3f);

        [Header("Post Processing")]
        public bool enableBloom = true;
        public bool enableTonemapping = true;
        public bool enableVignette = true;
        public bool enableColorAdjustments = true;

        private UnityEngine.Rendering.Volume _globalVolume;
        private UnityEngine.Rendering.VolumeProfile _profile;

        private void Start()
        {
            SetupLighting();
            SetupPostProcessing();
            SetupFog();
            SetupSkybox();
        }

        [ContextMenu("Apply Visuals")]
        public void ApplyVisuals()
        {
            SetupLighting();
            SetupPostProcessing();
            SetupFog();
            SetupSkybox();
        }
        
        private void SetupSkybox()
        {
            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
                return;
            }
            
            if (skyboxTexture != null)
            {
                // Auto-create material
                Shader skyShader = Shader.Find("Skybox/Panoramic");
                if (skyShader != null)
                {
                    Material mat = new Material(skyShader);
                    mat.SetTexture("_MainTex", skyboxTexture);
                    mat.SetFloat("_Exposure", skyboxExposure);
                    mat.SetFloat("_ImageType", 0); // 0 = 360 degrees
                    RenderSettings.skybox = mat;
                    return;
                }
            }
            
            // Try auto-find if missing
            if (RenderSettings.skybox == null || RenderSettings.skybox.name == "Default-Skybox")
            {
                // Look for known skybox names in project (only works if in Resources or specialized loader)
                // Since we can't search assets easily at runtime, we rely on the inspector assignment.
                // However, we can set a nice gradient backup if no skybox.
                RenderSettings.ambientMode = AmbientMode.Trilight;
            }
        }

        private void SetupLighting()
        {
            // internal sun
            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        sun = l;
                        break;
                    }
                }
            }

            if (sun != null)
            {
                sun.color = sunColor;
                sun.intensity = sunIntensity;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.8f;
                // Rotate sun for "Golden Hour" look
                sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(sunColor, Color.blue, 0.5f);
            RenderSettings.ambientEquatorColor = fogColor;
            RenderSettings.ambientGroundColor = shadowColor;
        }

        private void SetupFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
        }

        private void SetupPostProcessing()
        {
            // Find or Create Global Volume
            _globalVolume = GetComponent<UnityEngine.Rendering.Volume>();
            if (_globalVolume == null)
            {
                _globalVolume = gameObject.AddComponent<UnityEngine.Rendering.Volume>();
                _globalVolume.isGlobal = true;
            }

            // Create a new profile if needed (runtime only to avoid asset clutter)
            if (_profile == null)
            {
                _profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
                _globalVolume.profile = _profile;
            }

            // 1. BLOOM (The "Glow")
            if (enableBloom)
            {
                UnityEngine.Rendering.Universal.Bloom bloom;
                if (!_profile.TryGet(out bloom)) bloom = _profile.Add<UnityEngine.Rendering.Universal.Bloom>();
                
                bloom.intensity.value = 1.5f;
                bloom.intensity.overrideState = true;
                
                bloom.threshold.value = 0.9f;
                bloom.threshold.overrideState = true;
                
                bloom.scatter.value = 0.7f;
                bloom.scatter.overrideState = true;
                
                bloom.active = true;
            }

            // 2. TONEMAPPING (Cinema Color)
            if (enableTonemapping)
            {
                UnityEngine.Rendering.Universal.Tonemapping tonemapping;
                if (!_profile.TryGet(out tonemapping)) tonemapping = _profile.Add<UnityEngine.Rendering.Universal.Tonemapping>();
                
                tonemapping.mode.value = TonemappingMode.ACES;
                tonemapping.mode.overrideState = true;
                
                tonemapping.active = true;
            }

            // 3. COLOR ADJUSTMENTS (Vibrancy)
            if (enableColorAdjustments)
            {
                UnityEngine.Rendering.Universal.ColorAdjustments colorAdj;
                if (!_profile.TryGet(out colorAdj)) colorAdj = _profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>();
                
                colorAdj.postExposure.value = 0.2f;
                colorAdj.postExposure.overrideState = true;
                
                colorAdj.contrast.value = 15f;
                colorAdj.contrast.overrideState = true;
                
                colorAdj.saturation.value = 20f; // Synty assets need saturation!
                colorAdj.saturation.overrideState = true;
                
                colorAdj.active = true;
            }

            // 4. VIGNETTE (Focus)
            if (enableVignette)
            {
                UnityEngine.Rendering.Universal.Vignette vignette;
                if (!_profile.TryGet(out vignette)) vignette = _profile.Add<UnityEngine.Rendering.Universal.Vignette>();
                
                vignette.intensity.value = 0.25f;
                vignette.intensity.overrideState = true;
                
                vignette.smoothness.value = 0.5f;
                vignette.smoothness.overrideState = true;
                
                vignette.active = true;
            }
        }
    }
}
