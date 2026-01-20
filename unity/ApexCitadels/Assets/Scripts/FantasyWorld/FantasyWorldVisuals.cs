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

        private Volume _globalVolume;
        private VolumeProfile _profile;

        private void Start()
        {
            SetupLighting();
            SetupPostProcessing();
            SetupFog();
        }

        [ContextMenu("Apply Visuals")]
        public void ApplyVisuals()
        {
            SetupLighting();
            SetupPostProcessing();
            SetupFog();
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
            _globalVolume = GetComponent<Volume>();
            if (_globalVolume == null)
            {
                _globalVolume = gameObject.AddComponent<Volume>();
                _globalVolume.isGlobal = true;
            }

            // Create a new profile if needed (runtime only to avoid asset clutter)
            if (_profile == null)
            {
                _profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _globalVolume.profile = _profile;
            }

            // 1. BLOOM (The "Glow")
            if (enableBloom)
            {
                Bloom bloom;
                if (!_profile.TryGet(out bloom)) bloom = _profile.Add<Bloom>(true);
                bloom.intensity.Override(1.5f);
                bloom.threshold.Override(0.9f);
                bloom.scatter.Override(0.7f);
                bloom.active = true;
            }

            // 2. TONEMAPPING (Cinema Color)
            if (enableTonemapping)
            {
                Tonemapping tonemapping;
                if (!_profile.TryGet(out tonemapping)) tonemapping = _profile.Add<Tonemapping>(true);
                tonemapping.mode.Override(TonemappingMode.ACES);
                tonemapping.active = true;
            }

            // 3. COLOR ADJUSTMENTS (Vibrancy)
            if (enableColorAdjustments)
            {
                ColorAdjustments colorAdj;
                if (!_profile.TryGet(out colorAdj)) colorAdj = _profile.Add<ColorAdjustments>(true);
                colorAdj.postExposure.Override(0.2f);
                colorAdj.contrast.Override(15f);
                colorAdj.saturation.Override(20f); // Synty assets need saturation!
                colorAdj.active = true;
            }

            // 4. VIGNETTE (Focus)
            if (enableVignette)
            {
                Vignette vignette;
                if (!_profile.TryGet(out vignette)) vignette = _profile.Add<Vignette>(true);
                vignette.intensity.Override(0.25f);
                vignette.smoothness.Override(0.5f);
                vignette.active = true;
            }
        }
    }
}
