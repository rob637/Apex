using UnityEngine;
using System.Collections;
using ApexCitadels.Core;
using ApexCitadels.Map;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Handles procedural visual effects for the PC client:
    /// - Gradient skybox
    /// - Ground texturing with grid
    /// - Ambient particles
    /// - Citadel glow effects
    /// - Post-processing setup
    /// </summary>
    public class VisualEnhancements : MonoBehaviour
    {
        public static VisualEnhancements Instance { get; private set; }

        [Header("Sky Settings")]
        [SerializeField] private Color skyTop = new Color(0.2f, 0.35f, 0.7f);
        [SerializeField] private Color skyMiddle = new Color(0.5f, 0.65f, 0.9f);
        [SerializeField] private Color skyBottom = new Color(0.7f, 0.8f, 0.95f);
        [SerializeField] private Color horizonColor = new Color(1f, 0.95f, 0.85f);
        
        [Header("Ground Settings")]
        [SerializeField] private Color groundBaseColor = new Color(0.15f, 0.4f, 0.2f);
        [SerializeField] private Color gridLineColor = new Color(0.2f, 0.5f, 0.25f);
        [SerializeField] private float gridSize = 50f;
        [SerializeField] private float gridLineWidth = 1f;
        
        [Header("Atmosphere")]
        [SerializeField] private Color fogColor = new Color(0.7f, 0.8f, 0.9f);
        [SerializeField] private float fogDensity = 0.001f;
        [SerializeField] private bool enableFog = true;
        
        [Header("Particles")]
        [SerializeField] private bool enableAmbientParticles = true;
        [SerializeField] private int particleCount = 200;
        
        private Material _skyMaterial;
        private Material _groundMaterial;
        private ParticleSystem _ambientParticles;
        private GameObject _groundPlane;
        
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
            // Check if new visual systems are present - if so, skip legacy ground plane
            bool hasNewVisualSystem = FindAnyObjectByType<Visual.TerrainVisualSystem>() != null 
                                   || FindAnyObjectByType<Visual.VisualWorldManager>() != null;
            
            if (!hasNewVisualSystem)
            {
                SetupProceduralSky();
                SetupEnhancedGround();
                SetupFog();
                
                if (enableAmbientParticles)
                {
                    CreateAmbientParticles();
                }
                
                ApexLogger.Log("Visual enhancements initialized (legacy mode)", ApexLogger.LogCategory.General);
            }
            else
            {
                // New visual system handles everything - disable this
                ApexLogger.Log("New VisualWorldManager detected - skipping legacy visuals", ApexLogger.LogCategory.General);
                
                // Destroy old ground plane if it exists
                var oldGround = GameObject.Find("GroundPlane");
                if (oldGround != null)
                {
                    Destroy(oldGround);
                }
            }
        }

        /// <summary>
        /// Creates a procedural gradient sky using camera background and render settings
        /// </summary>
        private void SetupProceduralSky()
        {
            // Skip if SkyboxEnvironmentSystem is handling the sky
            var skyboxSystem = FindFirstObjectByType<Visual.SkyboxEnvironmentSystem>();
            if (skyboxSystem != null && skyboxSystem.gameObject.activeInHierarchy)
            {
                Debug.Log("[VisualEnhancements] SkyboxEnvironmentSystem active - skipping sky setup");
                return;
            }
            
            // Try to create a procedural skybox material
            // For WebGL, we'll use a gradient approach via render settings
            
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // Use solid color as base, but configure gradient via ambient
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = skyMiddle;
            }
            
            // Setup tri-light ambient for sky gradient effect
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = skyTop;
            RenderSettings.ambientEquatorColor = skyMiddle;
            RenderSettings.ambientGroundColor = groundBaseColor * 0.5f;
            RenderSettings.ambientIntensity = 1.3f;
            
            // Setup sun for dramatic lighting
            Light sun = FindMainDirectionalLight();
            if (sun != null)
            {
                sun.color = horizonColor;
                sun.intensity = 1.4f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.6f;
                
                // Angle for dramatic shadows
                sun.transform.rotation = Quaternion.Euler(45f, 30f, 0f);
            }
            
            ApexLogger.Log("Sky configured with gradient ambient lighting", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Creates an enhanced ground plane with procedural grid texture
        /// Only if Mapbox isn't providing the ground
        /// </summary>
        private void SetupEnhancedGround()
        {
            // Skip if Mapbox is providing the ground
            var mapbox = FindFirstObjectByType<MapboxTileRenderer>();
            if (mapbox != null && mapbox.gameObject.activeInHierarchy)
            {
                // Also destroy any existing ground plane that might have been created
                var existingGround = GameObject.Find("GroundPlane");
                if (existingGround != null)
                {
                    Debug.Log("[VisualEnhancements] Destroying existing GroundPlane since Mapbox is active");
                    Destroy(existingGround);
                }
                ApexLogger.Log("[VisualEnhancements] Mapbox active - skipping ground plane", ApexLogger.LogCategory.General);
                return;
            }
            
            // Find existing ground or create new
            _groundPlane = GameObject.Find("GroundPlane");
            
            if (_groundPlane == null)
            {
                _groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                _groundPlane.name = "GroundPlane";
                _groundPlane.transform.position = Vector3.zero;
                _groundPlane.transform.localScale = new Vector3(200f, 1f, 200f);
            }
            
            // Create procedural ground material
            _groundMaterial = CreateProceduralGroundMaterial();
            
            Renderer rend = _groundPlane.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = _groundMaterial;
            }
            
            // Add subtle grid overlay
            CreateGridOverlay();
            
            ApexLogger.Log("Enhanced ground plane created", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Creates a material with procedural ground coloring
        /// </summary>
        private Material CreateProceduralGroundMaterial()
        {
            // Use URP Lit shader if available, fallback to Standard
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            
            Material mat = new Material(shader);
            mat.name = "ProceduralGround";
            mat.color = groundBaseColor;
            
            // Set metallic/smoothness for better look
            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", 0.0f);
            }
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0.2f);
            }
            
            return mat;
        }

        /// <summary>
        /// Creates visible grid lines on the ground for strategic planning
        /// </summary>
        private void CreateGridOverlay()
        {
            GameObject gridContainer = new GameObject("GridOverlay");
            gridContainer.transform.parent = transform;
            gridContainer.transform.position = new Vector3(0, 0.5f, 0); // Slightly above ground
            
            float halfExtent = 1000f; // Grid extends 1km in each direction
            int lineCount = Mathf.RoundToInt(halfExtent * 2 / gridSize);
            
            // Create grid line material
            Material lineMat = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
            lineMat.color = gridLineColor;
            
            // Create vertical lines
            for (int i = 0; i <= lineCount; i++)
            {
                float x = -halfExtent + i * gridSize;
                CreateLine(gridContainer.transform, $"GridV_{i}", 
                    new Vector3(x, 0, -halfExtent), 
                    new Vector3(x, 0, halfExtent), 
                    lineMat);
            }
            
            // Create horizontal lines
            for (int i = 0; i <= lineCount; i++)
            {
                float z = -halfExtent + i * gridSize;
                CreateLine(gridContainer.transform, $"GridH_{i}", 
                    new Vector3(-halfExtent, 0, z), 
                    new Vector3(halfExtent, 0, z), 
                    lineMat);
            }
            
            ApexLogger.Log($"Created {lineCount * 2} grid lines", ApexLogger.LogCategory.General);
        }

        private void CreateLine(Transform parent, string name, Vector3 start, Vector3 end, Material mat)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.parent = parent;
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPositions(new Vector3[] { start, end });
            lr.startWidth = gridLineWidth;
            lr.endWidth = gridLineWidth;
            lr.material = mat;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }

        /// <summary>
        /// Setup distance fog for atmosphere
        /// </summary>
        private void SetupFog()
        {
            RenderSettings.fog = enableFog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            
            ApexLogger.Log("Fog configured", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Creates floating ambient particles for magical atmosphere
        /// </summary>
        private void CreateAmbientParticles()
        {
            GameObject particleObj = new GameObject("AmbientParticles");
            particleObj.transform.parent = transform;
            particleObj.transform.position = new Vector3(0, 50f, 0);
            
            _ambientParticles = particleObj.AddComponent<ParticleSystem>();
            
            // Main module
            var main = _ambientParticles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 10f;
            main.startSpeed = 1f;
            main.startSize = 3f;
            main.maxParticles = particleCount;
            main.startColor = new Color(1f, 1f, 0.8f, 0.3f); // Soft yellow glow
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            // Emission
            var emission = _ambientParticles.emission;
            emission.rateOverTime = particleCount / 10f;
            
            // Shape - large box around the camera
            var shape = _ambientParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(500f, 200f, 500f);
            
            // Color over lifetime - fade in and out
            var colorOverLifetime = _ambientParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.white, 0f), 
                    new GradientColorKey(Color.white, 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0f, 0f), 
                    new GradientAlphaKey(0.5f, 0.3f),
                    new GradientAlphaKey(0.5f, 0.7f),
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            colorOverLifetime.color = gradient;
            
            // Velocity for gentle drift
            var velocity = _ambientParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            velocity.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
            
            // Renderer - use simple material
            var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
            Material particleMat = new Material(Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Unlit/Color"));
            particleMat.color = new Color(1f, 1f, 0.8f, 0.3f);
            particleMat.SetFloat("_Mode", 2); // Fade mode
            renderer.material = particleMat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            _ambientParticles.Play();
            
            ApexLogger.Log("Ambient particles created", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Add glow effect to a citadel/territory object
        /// </summary>
        public void AddCitadelGlow(GameObject citadel, Color glowColor, float intensity = 1.5f)
        {
            if (citadel == null) return;
            
            // Create a point light at the beacon
            Transform beacon = citadel.transform.Find("BeaconTop");
            if (beacon != null)
            {
                GameObject lightObj = new GameObject("BeaconLight");
                lightObj.transform.parent = beacon;
                lightObj.transform.localPosition = Vector3.zero;
                
                Light pointLight = lightObj.AddComponent<Light>();
                pointLight.type = LightType.Point;
                pointLight.color = glowColor;
                pointLight.intensity = intensity;
                pointLight.range = 50f;
                pointLight.shadows = LightShadows.None;
            }
            
            // Enhance beacon material to be emissive
            Renderer beaconRenderer = beacon?.GetComponent<Renderer>();
            if (beaconRenderer != null && beaconRenderer.material != null)
            {
                Material mat = beaconRenderer.material;
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", glowColor * intensity);
                }
            }
        }

        /// <summary>
        /// Apply visual enhancements to all existing territories
        /// </summary>
        public void EnhanceAllTerritories()
        {
            WorldMapRenderer worldMap = WorldMapRenderer.Instance;
            if (worldMap == null) return;
            
            // Find all territory objects and add glow
            GameObject[] territories = GameObject.FindGameObjectsWithTag("Territory");
            foreach (var territory in territories)
            {
                Renderer rend = territory.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    AddCitadelGlow(territory, rend.material.color);
                }
            }
            
            ApexLogger.Log($"Enhanced {territories.Length} territories with glow effects", ApexLogger.LogCategory.General);
        }

        private Light FindMainDirectionalLight()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    return light;
                }
            }
            return null;
        }

        /// <summary>
        /// Update sky colors based on time of day (called by DayNightCycle)
        /// </summary>
        public void UpdateSkyColors(float timeOfDay)
        {
            // timeOfDay: 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset
            
            Color currentSkyTop, currentSkyMiddle, currentHorizon;
            
            if (timeOfDay < 0.25f)
            {
                // Night to dawn
                float t = timeOfDay / 0.25f;
                currentSkyTop = Color.Lerp(new Color(0.05f, 0.05f, 0.15f), skyTop, t);
                currentSkyMiddle = Color.Lerp(new Color(0.1f, 0.1f, 0.2f), skyMiddle, t);
                currentHorizon = Color.Lerp(new Color(0.2f, 0.15f, 0.3f), new Color(1f, 0.6f, 0.4f), t);
            }
            else if (timeOfDay < 0.5f)
            {
                // Dawn to noon
                float t = (timeOfDay - 0.25f) / 0.25f;
                currentSkyTop = Color.Lerp(skyTop, new Color(0.3f, 0.5f, 0.8f), t);
                currentSkyMiddle = Color.Lerp(skyMiddle, new Color(0.6f, 0.75f, 0.95f), t);
                currentHorizon = Color.Lerp(new Color(1f, 0.6f, 0.4f), horizonColor, t);
            }
            else if (timeOfDay < 0.75f)
            {
                // Noon to sunset
                float t = (timeOfDay - 0.5f) / 0.25f;
                currentSkyTop = Color.Lerp(new Color(0.3f, 0.5f, 0.8f), new Color(0.4f, 0.3f, 0.5f), t);
                currentSkyMiddle = Color.Lerp(new Color(0.6f, 0.75f, 0.95f), new Color(0.8f, 0.5f, 0.4f), t);
                currentHorizon = Color.Lerp(horizonColor, new Color(1f, 0.4f, 0.2f), t);
            }
            else
            {
                // Sunset to night
                float t = (timeOfDay - 0.75f) / 0.25f;
                currentSkyTop = Color.Lerp(new Color(0.4f, 0.3f, 0.5f), new Color(0.05f, 0.05f, 0.15f), t);
                currentSkyMiddle = Color.Lerp(new Color(0.8f, 0.5f, 0.4f), new Color(0.1f, 0.1f, 0.2f), t);
                currentHorizon = Color.Lerp(new Color(1f, 0.4f, 0.2f), new Color(0.2f, 0.15f, 0.3f), t);
            }
            
            // Apply to render settings
            RenderSettings.ambientSkyColor = currentSkyTop;
            RenderSettings.ambientEquatorColor = currentSkyMiddle;
            
            // Update camera background
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = currentSkyMiddle;
            }
            
            // Update fog color
            RenderSettings.fogColor = Color.Lerp(currentSkyMiddle, currentHorizon, 0.5f);
        }
    }
}
