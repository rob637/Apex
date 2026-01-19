// ============================================================================
// APEX CITADELS - SKYBOX & ENVIRONMENT SYSTEM
// Dynamic sky, day/night cycle, and environmental lighting
// ============================================================================
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using ApexCitadels.Core;

namespace ApexCitadels.PC.Visual
{
    /// <summary>
    /// Manages the skybox, day/night cycle, and environmental lighting.
    /// Creates a visually stunning atmosphere for the game world.
    /// </summary>
    public class SkyboxEnvironmentSystem : MonoBehaviour
    {
        public static SkyboxEnvironmentSystem Instance { get; private set; }

        [Header("Day/Night Settings")]
        public float dayDuration = 600f; // 10 minutes real time = 1 day
        public float currentTimeOfDay = 0.3f; // 0-1, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset
        public bool enableDayNightCycle = true;

        [Header("Sun")]
        private Light sunLight;
        private Transform sunPivot;

        [Header("Sky Colors")]
        private Color daySkyColor = new Color(0.4f, 0.6f, 0.9f);
        private Color sunsetSkyColor = new Color(0.9f, 0.5f, 0.3f);
        private Color nightSkyColor = new Color(0.05f, 0.05f, 0.15f);
        private Color dawnSkyColor = new Color(0.6f, 0.4f, 0.5f);

        [Header("Ambient Colors")]
        private Color dayAmbient = new Color(0.5f, 0.5f, 0.5f);
        private Color nightAmbient = new Color(0.1f, 0.1f, 0.15f);

        [Header("Fog")]
        private Color dayFogColor = new Color(0.7f, 0.8f, 0.9f);
        private Color nightFogColor = new Color(0.1f, 0.1f, 0.15f);

        [Header("Stars")]
        private GameObject starField;
        private ParticleSystem starsParticle;

        [Header("Clouds")]
        private List<GameObject> clouds = new List<GameObject>();
        private int cloudCount = 15;

        private Material skyboxMaterial;

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
            CreateSkybox();
            SetupSun();
            CreateStars();
            CreateClouds();
            UpdateEnvironment();

            ApexLogger.Log(LogCategory.General, "✅ Skybox and environment system initialized");
        }

        private void Update()
        {
            if (enableDayNightCycle)
            {
                // Advance time
                currentTimeOfDay += Time.deltaTime / dayDuration;
                if (currentTimeOfDay >= 1f) currentTimeOfDay -= 1f;
            }

            UpdateEnvironment();
            UpdateClouds();
        }

        /// <summary>
        /// Creates a procedural gradient skybox
        /// </summary>
        private void CreateSkybox()
        {
            // Try to load HDR skybox from assets first
            Texture2D skyTexture = TryLoadSkyboxTexture();
            
            if (skyTexture != null)
            {
                // Use loaded texture as panoramic skybox
                Shader panoramicShader = Shader.Find("Skybox/Panoramic");
                if (panoramicShader != null)
                {
                    skyboxMaterial = new Material(panoramicShader);
                    skyboxMaterial.SetTexture("_MainTex", skyTexture);
                    skyboxMaterial.SetFloat("_Exposure", 1.2f);
                    RenderSettings.skybox = skyboxMaterial;
                    ApexLogger.LogVerbose(LogCategory.General, "Loaded panoramic skybox from assets");
                    return;
                }
            }

            // Fallback: Create procedural skybox
            Shader gradientShader = Shader.Find("Skybox/Procedural");
            if (gradientShader == null)
            {
                gradientShader = Shader.Find("Skybox/Cubemap");
            }
            
            if (gradientShader != null)
            {
                skyboxMaterial = new Material(gradientShader);
                skyboxMaterial.SetFloat("_SunSize", 0.04f);
                skyboxMaterial.SetFloat("_SunSizeConvergence", 5f);
                skyboxMaterial.SetFloat("_AtmosphereThickness", 1.0f);
                skyboxMaterial.SetColor("_SkyTint", daySkyColor);
                skyboxMaterial.SetColor("_GroundColor", new Color(0.3f, 0.3f, 0.3f));
                skyboxMaterial.SetFloat("_Exposure", 1.3f);
                
                RenderSettings.skybox = skyboxMaterial;
            }
            else
            {
                // Ultimate fallback: solid color
                RenderSettings.skybox = null;
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = daySkyColor;
            }

            ApexLogger.LogVerbose(LogCategory.General, "Created procedural skybox");
        }

        /// <summary>
        /// Tries to load a skybox texture from the Art/Skyboxes folder
        /// </summary>
        private Texture2D TryLoadSkyboxTexture()
        {
            // Try to load from Resources
            string[] skyboxNames = { "SKY02", "SKY04", "SKY17", "SKY19", "SKY32" };
            
            foreach (var name in skyboxNames)
            {
                Texture2D tex = UnityEngine.Resources.Load<Texture2D>($"PC/Skyboxes/{name}");
                if (tex != null) return tex;
            }

            return null;
        }

        /// <summary>
        /// Sets up the directional light as the sun
        /// </summary>
        private void SetupSun()
        {
            // Create sun pivot for rotation
            sunPivot = new GameObject("SunPivot").transform;
            sunPivot.position = Vector3.zero;

            // Find or create sun light
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    sunLight = light;
                    break;
                }
            }

            if (sunLight == null)
            {
                GameObject sunObj = new GameObject("Sun");
                sunLight = sunObj.AddComponent<Light>();
                sunLight.type = LightType.Directional;
            }

            sunLight.transform.SetParent(sunPivot);
            sunLight.transform.localPosition = Vector3.forward * 100f;
            sunLight.transform.LookAt(sunPivot.position);

            // Sun settings
            sunLight.intensity = 1.2f;
            sunLight.color = new Color(1f, 0.95f, 0.85f);
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.8f;
            sunLight.shadowBias = 0.05f;
            sunLight.shadowNormalBias = 0.4f;

            ApexLogger.LogVerbose(LogCategory.General, "Sun light configured");
        }

        /// <summary>
        /// Creates a star field particle system for night sky
        /// </summary>
        private void CreateStars()
        {
            starField = new GameObject("StarField");
            starField.transform.position = Vector3.up * 200f;

            starsParticle = starField.AddComponent<ParticleSystem>();
            var main = starsParticle.main;
            main.loop = true;
            main.startLifetime = float.MaxValue;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startColor = new Color(1f, 1f, 1f, 0.8f);
            main.maxParticles = 500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = starsParticle.emission;
            emission.enabled = true;
            emission.rateOverTime = 0; // Burst only

            var shape = starsParticle.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 300f;
            shape.radiusThickness = 0f;

            // Emit all stars at once
            starsParticle.Emit(500);
            starsParticle.Pause();

            // Initially hidden
            starField.SetActive(false);

            ApexLogger.LogVerbose(LogCategory.General, "Star field created");
        }

        /// <summary>
        /// Creates volumetric cloud objects
        /// </summary>
        private void CreateClouds()
        {
            GameObject cloudParent = new GameObject("Clouds");
            
            for (int i = 0; i < cloudCount; i++)
            {
                GameObject cloud = CreateCloud();
                cloud.transform.SetParent(cloudParent.transform);
                
                // Random position in sky
                float radius = Random.Range(100f, 300f);
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float height = Random.Range(80f, 150f);
                
                cloud.transform.position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    height,
                    Mathf.Sin(angle) * radius
                );
                
                clouds.Add(cloud);
            }

            ApexLogger.LogVerbose(LogCategory.General, $"Created {cloudCount} clouds");
        }

        /// <summary>
        /// Creates a single cloud from multiple spheres
        /// </summary>
        private GameObject CreateCloud()
        {
            GameObject cloud = new GameObject("Cloud");
            
            // Cloud material
            Material cloudMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            cloudMat.color = new Color(1f, 1f, 1f, 0.85f);
            cloudMat.SetFloat("_Surface", 1); // Transparent
            cloudMat.renderQueue = 3000;
            
            // Create puffy cloud from overlapping spheres
            int puffCount = Random.Range(4, 8);
            for (int i = 0; i < puffCount; i++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "CloudPuff";
                puff.transform.SetParent(cloud.transform);
                
                puff.transform.localPosition = new Vector3(
                    Random.Range(-15f, 15f),
                    Random.Range(-3f, 3f),
                    Random.Range(-8f, 8f)
                );
                
                float scale = Random.Range(8f, 20f);
                puff.transform.localScale = new Vector3(scale, scale * 0.5f, scale * 0.7f);
                
                puff.GetComponent<Renderer>().material = cloudMat;
                puff.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                puff.GetComponent<Renderer>().receiveShadows = false;
                
                Destroy(puff.GetComponent<Collider>());
            }
            
            // Random cloud scale
            float cloudScale = Random.Range(0.8f, 1.5f);
            cloud.transform.localScale = Vector3.one * cloudScale;

            return cloud;
        }

        /// <summary>
        /// Updates environment based on time of day
        /// </summary>
        private void UpdateEnvironment()
        {
            // Rotate sun based on time (0 = midnight, 0.5 = noon)
            float sunAngle = (currentTimeOfDay - 0.25f) * 360f;
            sunPivot.rotation = Quaternion.Euler(sunAngle, -30f, 0);

            // Calculate where we are in the day cycle
            float dayProgress = currentTimeOfDay;
            bool isNight = dayProgress < 0.25f || dayProgress > 0.75f;
            bool isSunrise = dayProgress >= 0.2f && dayProgress < 0.3f;
            bool isSunset = dayProgress >= 0.7f && dayProgress < 0.8f;

            // Sky color
            Color targetSkyColor;
            Color targetAmbient;
            Color targetFogColor;
            float sunIntensity;

            if (isNight)
            {
                targetSkyColor = nightSkyColor;
                targetAmbient = nightAmbient;
                targetFogColor = nightFogColor;
                sunIntensity = 0.1f;
                
                // Show stars at night
                if (starField != null) starField.SetActive(true);
            }
            else if (isSunrise)
            {
                float t = (dayProgress - 0.2f) / 0.1f;
                targetSkyColor = Color.Lerp(dawnSkyColor, daySkyColor, t);
                targetAmbient = Color.Lerp(nightAmbient, dayAmbient, t);
                targetFogColor = Color.Lerp(nightFogColor, dayFogColor, t);
                sunIntensity = Mathf.Lerp(0.3f, 1.2f, t);
                
                // Fade out stars
                if (starField != null) starField.SetActive(t < 0.5f);
            }
            else if (isSunset)
            {
                float t = (dayProgress - 0.7f) / 0.1f;
                targetSkyColor = Color.Lerp(daySkyColor, sunsetSkyColor, t);
                targetAmbient = Color.Lerp(dayAmbient, nightAmbient * 1.5f, t);
                targetFogColor = Color.Lerp(dayFogColor, sunsetSkyColor, t);
                sunIntensity = Mathf.Lerp(1.2f, 0.5f, t);
                
                // Stars appear at sunset
                if (starField != null) starField.SetActive(t > 0.7f);
            }
            else
            {
                // Daytime
                targetSkyColor = daySkyColor;
                targetAmbient = dayAmbient;
                targetFogColor = dayFogColor;
                sunIntensity = 1.2f;
                
                if (starField != null) starField.SetActive(false);
            }

            // Apply sun settings
            if (sunLight != null)
            {
                sunLight.intensity = sunIntensity;
                
                // Sun color based on time
                if (isSunrise || isSunset)
                {
                    sunLight.color = new Color(1f, 0.7f, 0.4f);
                }
                else if (isNight)
                {
                    sunLight.color = new Color(0.5f, 0.5f, 0.7f); // Moonlight
                }
                else
                {
                    sunLight.color = new Color(1f, 0.95f, 0.9f);
                }
            }

            // Apply sky color to procedural skybox
            if (skyboxMaterial != null && skyboxMaterial.HasProperty("_SkyTint"))
            {
                skyboxMaterial.SetColor("_SkyTint", targetSkyColor);
            }

            // Apply ambient
            RenderSettings.ambientSkyColor = targetSkyColor;
            RenderSettings.ambientEquatorColor = targetAmbient;
            RenderSettings.ambientGroundColor = targetAmbient * 0.5f;

            // Apply fog
            RenderSettings.fogColor = targetFogColor;

            // Update cloud brightness
            UpdateCloudBrightness(isNight ? 0.3f : 1f);
        }

        /// <summary>
        /// Updates cloud brightness based on time of day
        /// </summary>
        private void UpdateCloudBrightness(float brightness)
        {
            foreach (var cloud in clouds)
            {
                Renderer[] renderers = cloud.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    if (r.material != null)
                    {
                        r.material.color = new Color(brightness, brightness, brightness, 0.85f);
                    }
                }
            }
        }

        /// <summary>
        /// Slowly drifts clouds across the sky
        /// </summary>
        private void UpdateClouds()
        {
            float windSpeed = 2f;
            
            foreach (var cloud in clouds)
            {
                cloud.transform.position += Vector3.right * windSpeed * Time.deltaTime;
                
                // Wrap clouds
                if (cloud.transform.position.x > 350f)
                {
                    cloud.transform.position = new Vector3(
                        -350f,
                        cloud.transform.position.y,
                        cloud.transform.position.z
                    );
                }
            }
        }

        /// <summary>
        /// Sets the time of day (0-1)
        /// </summary>
        public void SetTimeOfDay(float time)
        {
            currentTimeOfDay = Mathf.Clamp01(time);
            UpdateEnvironment();
        }

        /// <summary>
        /// Gets a readable time string
        /// </summary>
        public string GetTimeString()
        {
            float hours = currentTimeOfDay * 24f;
            int hour = Mathf.FloorToInt(hours);
            int minute = Mathf.FloorToInt((hours - hour) * 60f);
            return $"{hour:D2}:{minute:D2}";
        }

        /// <summary>
        /// Checks if it's currently daytime
        /// </summary>
        public bool IsDaytime()
        {
            return currentTimeOfDay >= 0.25f && currentTimeOfDay < 0.75f;
        }
    }
}
