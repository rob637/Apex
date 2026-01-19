using Camera = UnityEngine.Camera;
// ============================================================================
// APEX CITADELS - SKYBOX & ATMOSPHERIC LIGHTING
// Creates beautiful sky and lighting for immersive gameplay
// ============================================================================
using UnityEngine;
using UnityEngine.Rendering;
using ApexCitadels.Core;

namespace ApexCitadels.PC.Environment
{
    /// <summary>
    /// Manages skybox, sun, and atmospheric effects.
    /// Creates time-of-day lighting for visual appeal.
    /// </summary>
    public class AtmosphericLighting : MonoBehaviour
    {
        public static AtmosphericLighting Instance { get; private set; }

        [Header("Time of Day")]
        [SerializeField] private float timeOfDay = 10f;         // 0-24 hours
        [SerializeField] private float dayDuration = 600f;      // Real seconds per game day
        [SerializeField] private bool autoProgress = false;     // Auto advance time

        [Header("Sun Settings")]
        [SerializeField] private Light sunLight;
        [SerializeField] private float sunIntensity = 1.2f;
        [SerializeField] private Color sunriseColor = new Color(1f, 0.6f, 0.3f);
        [SerializeField] private Color noonColor = new Color(1f, 0.98f, 0.9f);
        [SerializeField] private Color sunsetColor = new Color(1f, 0.4f, 0.2f);
        [SerializeField] private Color nightColor = new Color(0.2f, 0.2f, 0.4f);

        [Header("Ambient Light")]
        [SerializeField] private Color dayAmbient = new Color(0.5f, 0.5f, 0.55f);
        [SerializeField] private Color nightAmbient = new Color(0.1f, 0.1f, 0.2f);

        [Header("Sky Colors")]
        [SerializeField] private Color daySkyColor = new Color(0.4f, 0.6f, 0.9f);
        [SerializeField] private Color sunriseSkyColor = new Color(0.8f, 0.5f, 0.4f);
        [SerializeField] private Color sunsetSkyColor = new Color(0.9f, 0.4f, 0.3f);
        [SerializeField] private Color nightSkyColor = new Color(0.05f, 0.05f, 0.15f);

        [Header("Fog")]
        [SerializeField] private bool enableFog = true;
        [SerializeField] private float fogDensity = 0.0008f;
        [SerializeField] private Color dayFogColor = new Color(0.6f, 0.7f, 0.8f);
        [SerializeField] private Color nightFogColor = new Color(0.1f, 0.1f, 0.2f);

        // Components
        private Camera _mainCamera;
        private Material _skyboxMaterial;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            Initialize();
            UpdateLighting();
        }

        private void Update()
        {
            if (autoProgress)
            {
                timeOfDay += (24f / dayDuration) * Time.deltaTime;
                if (timeOfDay >= 24f) timeOfDay -= 24f;
                UpdateLighting();
            }
        }

        /// <summary>
        /// Initialize lighting system
        /// </summary>
        private void Initialize()
        {
            _mainCamera = Camera.main;

            // Find or create sun light
            if (sunLight == null)
            {
                GameObject sunObj = GameObject.Find("Sun") ?? GameObject.Find("Directional Light");
                if (sunObj != null)
                {
                    sunLight = sunObj.GetComponent<Light>();
                }
                
                if (sunLight == null)
                {
                    sunObj = new GameObject("Sun");
                    sunObj.transform.parent = transform;
                    sunLight = sunObj.AddComponent<Light>();
                    sunLight.type = LightType.Directional;
                    ApexLogger.Log("[Atmosphere] Created sun light", ApexLogger.LogCategory.General);
                }
            }

            // Configure sun
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.8f;
            sunLight.shadowBias = 0.05f;
            sunLight.shadowNormalBias = 0.4f;

            // Create procedural skybox
            CreateProceduralSkybox();

            // Setup fog
            if (enableFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = fogDensity;
            }

            ApexLogger.Log("[Atmosphere] Lighting system initialized", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Create a simple gradient skybox
        /// </summary>
        private void CreateProceduralSkybox()
        {
            // Try to find existing skybox shader
            Shader skyShader = Shader.Find("Skybox/Procedural");
            
            if (skyShader != null)
            {
                _skyboxMaterial = new Material(skyShader);
                _skyboxMaterial.SetFloat("_SunSize", 0.04f);
                _skyboxMaterial.SetFloat("_SunSizeConvergence", 5f);
                _skyboxMaterial.SetFloat("_AtmosphereThickness", 1f);
                _skyboxMaterial.SetColor("_SkyTint", daySkyColor);
                _skyboxMaterial.SetColor("_GroundColor", new Color(0.3f, 0.3f, 0.3f));
                _skyboxMaterial.SetFloat("_Exposure", 1.3f);
                
                RenderSettings.skybox = _skyboxMaterial;
                ApexLogger.Log("[Atmosphere] Procedural skybox created", ApexLogger.LogCategory.General);
            }
            else
            {
                // Fallback to solid color
                if (_mainCamera != null)
                {
                    _mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    _mainCamera.backgroundColor = daySkyColor;
                }
                ApexLogger.Log("[Atmosphere] Using solid color sky (procedural shader not found)", ApexLogger.LogCategory.General);
            }
        }

        /// <summary>
        /// Update all lighting based on time of day
        /// </summary>
        public void UpdateLighting()
        {
            float t = timeOfDay / 24f;  // 0-1 through day
            
            // Sun position (rotate around X axis)
            float sunAngle = (t * 360f) - 90f;  // -90 at midnight, 90 at noon
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
            }

            // Calculate sun color and intensity based on time
            Color sunColor;
            float intensity;
            Color skyColor;
            Color ambientColor;
            Color fogColor;

            if (timeOfDay < 5f || timeOfDay >= 20f)  // Night
            {
                sunColor = nightColor;
                intensity = 0.1f;
                skyColor = nightSkyColor;
                ambientColor = nightAmbient;
                fogColor = nightFogColor;
            }
            else if (timeOfDay < 7f)  // Sunrise
            {
                float sunrise = (timeOfDay - 5f) / 2f;
                sunColor = Color.Lerp(nightColor, sunriseColor, sunrise);
                intensity = Mathf.Lerp(0.1f, 0.8f, sunrise);
                skyColor = Color.Lerp(nightSkyColor, sunriseSkyColor, sunrise);
                ambientColor = Color.Lerp(nightAmbient, dayAmbient, sunrise);
                fogColor = Color.Lerp(nightFogColor, dayFogColor, sunrise);
            }
            else if (timeOfDay < 10f)  // Morning
            {
                float morning = (timeOfDay - 7f) / 3f;
                sunColor = Color.Lerp(sunriseColor, noonColor, morning);
                intensity = Mathf.Lerp(0.8f, sunIntensity, morning);
                skyColor = Color.Lerp(sunriseSkyColor, daySkyColor, morning);
                ambientColor = dayAmbient;
                fogColor = dayFogColor;
            }
            else if (timeOfDay < 16f)  // Midday
            {
                sunColor = noonColor;
                intensity = sunIntensity;
                skyColor = daySkyColor;
                ambientColor = dayAmbient;
                fogColor = dayFogColor;
            }
            else if (timeOfDay < 18f)  // Afternoon
            {
                float afternoon = (timeOfDay - 16f) / 2f;
                sunColor = Color.Lerp(noonColor, sunsetColor, afternoon);
                intensity = Mathf.Lerp(sunIntensity, 0.8f, afternoon);
                skyColor = Color.Lerp(daySkyColor, sunsetSkyColor, afternoon);
                ambientColor = dayAmbient;
                fogColor = dayFogColor;
            }
            else  // Sunset (18-20)
            {
                float sunset = (timeOfDay - 18f) / 2f;
                sunColor = Color.Lerp(sunsetColor, nightColor, sunset);
                intensity = Mathf.Lerp(0.8f, 0.1f, sunset);
                skyColor = Color.Lerp(sunsetSkyColor, nightSkyColor, sunset);
                ambientColor = Color.Lerp(dayAmbient, nightAmbient, sunset);
                fogColor = Color.Lerp(dayFogColor, nightFogColor, sunset);
            }

            // Apply sun settings
            if (sunLight != null)
            {
                sunLight.color = sunColor;
                sunLight.intensity = intensity;
            }

            // Apply ambient
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.ambientMode = AmbientMode.Flat;

            // Apply fog
            if (enableFog)
            {
                RenderSettings.fogColor = fogColor;
            }

            // Apply sky - only if we're using our own skybox material, not SkyboxEnvironmentSystem
            if (_skyboxMaterial != null)
            {
                _skyboxMaterial.SetColor("_SkyTint", skyColor);
            }
            // Don't override camera background if SkyboxEnvironmentSystem is handling it
            // else if (_mainCamera != null)
            // {
            //     _mainCamera.backgroundColor = skyColor;
            // }
        }

        /// <summary>
        /// Set time of day (0-24)
        /// </summary>
        public void SetTimeOfDay(float time)
        {
            timeOfDay = Mathf.Repeat(time, 24f);
            UpdateLighting();
        }

        /// <summary>
        /// Set to specific preset time
        /// </summary>
        public void SetTimePreset(TimePreset preset)
        {
            switch (preset)
            {
                case TimePreset.Dawn:
                    SetTimeOfDay(6f);
                    break;
                case TimePreset.Morning:
                    SetTimeOfDay(9f);
                    break;
                case TimePreset.Noon:
                    SetTimeOfDay(12f);
                    break;
                case TimePreset.Afternoon:
                    SetTimeOfDay(15f);
                    break;
                case TimePreset.Sunset:
                    SetTimeOfDay(18.5f);
                    break;
                case TimePreset.Night:
                    SetTimeOfDay(22f);
                    break;
            }
        }

        /// <summary>
        /// Toggle day/night cycle
        /// </summary>
        public void SetAutoProgress(bool enabled)
        {
            autoProgress = enabled;
        }

        public float CurrentTime => timeOfDay;
        public bool IsNight => timeOfDay < 6f || timeOfDay >= 20f;
    }

    public enum TimePreset
    {
        Dawn,
        Morning,
        Noon,
        Afternoon,
        Sunset,
        Night
    }
}
